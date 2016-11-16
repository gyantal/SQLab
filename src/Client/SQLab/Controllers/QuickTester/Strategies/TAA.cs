using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using SQCommon.MathNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    enum RebalancingPeriodicity { Daily, Weekly, Monthly };

    public class DailyTaaData
    {
        public DateTime Date { get; set; }
        public double PvClosePrice { get; set; }

        public double[,] AssetPctChannels { get; set; }    // for assets and for each 
        public double HistVol { get; set; }
    }
    public class TAA
    {
        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, Dictionary<string, StringValues> p_allParamsDict)
        {
            if (p_strategyName != "TAA")
                return null;
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();

            // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
            string assetsStr = p_allParamsDict["Assets"][0];                                         // "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT"
            string assetsConstantLeverageStr = p_allParamsDict["AssetsConstantLeverage"][0];         // "1,1,1,-1,1.5,2,2"
            string rebalancingFrequencyStr = p_allParamsDict["RebalancingFrequency"][0];             // "Weekly,Fridays";   // "Daily, 2d"(trading days),"Weekly, Fridays", "Monthly, T+1"/"Monthly, T-1" (first/last trading day of the month), 
            string pctChannelLookbackDaysStr = p_allParamsDict["PctChannelLookbackDays"][0];         // "30-60-120-252"
            string pctChannelPctLimitsStr = p_allParamsDict["PctChannelPctLimits"][0];               // "30-70"
            string pctChannelIsConditionalStr = p_allParamsDict["PctChannelIsConditional"][0];       // "Yes"
            string histVolLookbackDaysStr = p_allParamsDict["HistVolLookbackDays"][0];               // "20"
            string dynamicLeverageClmtParamsStr = p_allParamsDict["DynamicLeverageClmtParams"][0];   // "SMA(SPX,50d,200d); PR(XLU,VTI,20d)";   // SPX 50/200 crossover; PR=PriceRatio of XLU/VTI for 20 days
            string uberVxxEventsParamsStr = p_allParamsDict["UberVxxEventsParams"][0];               // "FOMC;Holidays"

            RebalancingPeriodicity rebalancingPeriodicity = RebalancingPeriodicity.Weekly;
            DayOfWeek weeklyRebalancingWeekDay = DayOfWeek.Friday;
            int[] pctChannelLookbackDays = pctChannelLookbackDaysStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Select(r => Int32.Parse(r)).ToArray();
            string[] pctChannelPctLimitsStr2 = pctChannelPctLimitsStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            double pctChannelPctLimitLower = Double.Parse(pctChannelPctLimitsStr2[0]) / 100.0;
            double pctChannelPctLimitUpper = Double.Parse(pctChannelPctLimitsStr2[1]) / 100.0;
            int histVolLookbackDays = Int32.Parse(histVolLookbackDaysStr);
            int clmt1_shorterDays = 50, clmt1_longerDays = 200, clmt2_utilsLookbackDays = 20;

            string[] tickers = assetsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            bool isNonActiveAssetsPlayedByCash = true;
            string cashSubstituteTicker = "SHY";    // ""

            List<string> tickersNeeded = tickers.ToList();
            if (!String.IsNullOrEmpty(cashSubstituteTicker))
                tickersNeeded.Add(cashSubstituteTicker);
            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams, tickersNeeded);
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();
            IList<List<DailyData>> quotes;
            List<DailyData> cashSubstituteQuotes = null;  
            if (String.IsNullOrEmpty(cashSubstituteTicker))
               quotes = getAllQuotesData.Item1;
            else
            {
                quotes = getAllQuotesData.Item1.ToList().GetRange(0, tickers.Length);
                cashSubstituteQuotes = getAllQuotesData.Item1[tickers.Length];
            }

            string warningToUser = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";

            List<DailyData> pv;
            DoBacktestInTheTimeInterval_TAA(quotes, tickers, 
                    rebalancingPeriodicity, weeklyRebalancingWeekDay, 
                    pctChannelLookbackDays, pctChannelPctLimitLower, pctChannelPctLimitUpper, histVolLookbackDays, 
                    clmt1_shorterDays, clmt1_longerDays, clmt2_utilsLookbackDays, 
                    isNonActiveAssetsPlayedByCash, cashSubstituteQuotes, 
                    ref warningToUser, ref noteToUserBacktest, ref errorMessage, ref debugMessage, out pv);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               warningToUser + "***" + noteToUserBacktest, 
               errorMessage,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }

        // Others try to implement Varadi's original strategy
        // https://www.r-bloggers.com/an-attempt-at-replicating-david-varadis-percentile-channels-strategy/ 
        // https://quantstrattrader.wordpress.com/2015/02/20/a-closer-update-to-david-varadis-percentile-channels-strategy/
        private static void DoBacktestInTheTimeInterval_TAA(IList<List<DailyData>> p_quotes, string[] p_tickers,
                RebalancingPeriodicity p_rebalancingPeriodicity, DayOfWeek p_weeklyRebalancingWeekDay,
                int[] p_pctChannelLookbackDays, double p_pctChannelPctLimitLower, double p_pctChannelPctLimitUpper, int p_histVolLookbackDays,
                int clmt1_shorterDays, int clmt1_longerDays, int clmt2_utilsLookbackDays,
                bool p_isNonActiveAssetsPlayedByCash, List<DailyData> p_cashSubstituteQuotes,
                ref string p_noteToUserCheckData, ref string p_noteToUserBacktest, ref string errorMessage, ref string debugMessage, out List<DailyData> pv)
        {
            StringBuilder noteToUser = new StringBuilder("DoBacktestInTheTimeInterval_TAA()");
            pv = null;
            // implement CLMT in a way, that those data days don't restrict Strategy StartDate. If they are not available on a day, simple 100% is used. CLMT: "SMA(SPX,50d,200d); PR(XLU,VTI,20d)"
            // 1. Determine commonAssetStartDate
            DateTime commonAssetStartDate, commonAssetEndDate;
            StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(p_quotes, p_tickers, ref p_noteToUserCheckData, out commonAssetStartDate, out commonAssetEndDate);
            int commonAssetStartDateInd = p_quotes[0].FindIndex(r => r.Date >= commonAssetStartDate);
            int commonAssetEndDateInd = p_quotes[0].FindIndex(commonAssetStartDateInd, r => r.Date >= commonAssetEndDate);

            // 2. Determine firstAllDataAvailableDate: shift ShartDate when we have all the data for "Use 60,120,180, 252-day percentile channels"
            int requiredLookBackDays = Math.Max(p_pctChannelLookbackDays.Max(), p_histVolLookbackDays);
            int firstAllDataAvailableDateInd = commonAssetStartDateInd + (requiredLookBackDays - 1);
            if (firstAllDataAvailableDateInd > commonAssetEndDateInd)
            {
                errorMessage = "firstAllDataAvailableDate cannot be determined";
                return;
            }
            DateTime firstAllDataAvailableDate = p_quotes[0][commonAssetStartDateInd + (requiredLookBackDays - 1)].Date;

            // 3. Determine First Rebalance day. Maybe only Fridays,  or maybe TotM-1. That will be the real pvStartDate
            DateTime firstRebalancingDate;
            int firstRebalancingDateInd = -1;
            if (p_rebalancingPeriodicity == RebalancingPeriodicity.Weekly)
            {
                for (int i = firstAllDataAvailableDateInd; i <= commonAssetEndDateInd; i++)
                {
                    if (p_quotes[0][i].Date.DayOfWeek == p_weeklyRebalancingWeekDay)
                    {
                        firstRebalancingDateInd = i;
                        break;
                    }
                }
            }
            if (firstRebalancingDateInd == -1)
            {
                errorMessage = "StartDate cannot be determined";
                return;
            }
            firstRebalancingDate = p_quotes[0][firstRebalancingDateInd].Date;
            

            // 4. pvStartDate is now final, calculate the index of that startDate for each asset quotes
            DateTime pvStartDate = firstRebalancingDate;
            DateTime pvEndDate = commonAssetEndDate;
            int nDays = commonAssetEndDateInd - firstRebalancingDateInd + 1;        // startDate, endDate is included
            int nAssets = p_quotes.Count;
            int[] iQ = new int[nAssets];
            for (int i = 0; i < nAssets; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }
            int iCashSubst = (p_cashSubstituteQuotes == null) ? -1 : p_cashSubstituteQuotes.FindIndex(r => r.Date >= pvStartDate);


            pv = new List<DailyData>(nDays);

            double pvDaily = 100.0;
            double cash = pvDaily;
            double[] assetPos = new double[nAssets];
            double[] assetScores = new double[nAssets];
            double[] assetHV = new double[nAssets];
            double[] assetWeights = new double[nAssets];
            double[,] assetPctChannelsUpper = new double[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each 
            double[,] assetPctChannelsLower = new double[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each
            sbyte[,] assetPctChannelsSignal = new sbyte[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each 

            for (int iAsset = 0; iAsset < nAssets; iAsset++) {
                assetPos[iAsset] = 0.0;
                for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                {
                    assetPctChannelsSignal[iAsset, iChannel] = 1;       // let all assets be active at the beginning (if they are not under 25% percentile)
                }
            }

            noteToUser.AppendLine("Date, pvDaily, assetWeights... ,cashWeight <br />");
            for (int iDay = 0; iDay < nDays; iDay++)    // march for all days
            {
                // 1. Evaluate the value of the portfolio based on assetPos and this day's %change
                pvDaily = 0;
                if (iDay != 0)    // on first day, don't calculate %change, we may not have previous day
                {
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double assetChg = p_quotes[iAsset][iQ[iAsset] + iDay].ClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].ClosePrice;
                        assetPos[iAsset] *= assetChg;
                        pvDaily += assetPos[iAsset];
                    }

                    if (p_cashSubstituteQuotes != null)
                    {
                        double cashChg = p_cashSubstituteQuotes[iCashSubst + iDay].ClosePrice / p_cashSubstituteQuotes[iCashSubst + iDay - 1].ClosePrice;
                        cash *= cashChg;
                    }
                }
                pvDaily += cash;    // cash has to be added, on first day or on other days
                pv.Add(new DailyData() { Date = p_quotes[0][firstRebalancingDateInd + iDay].Date, ClosePrice = pvDaily });

                // 2. Decide if rebalance is needed at the end of the day, and allocate assetPos[]. This will not change PV.
                //if (iDay % 20 == 0)    // test: every periodic days
                if (iDay % 1 == 0)    // test: every periodic days
                {
                    // https://docs.google.com/document/d/1kx3_UuYy_RApp6s0KmO2b4pbwQdClMuzjf6EyJynghs/edit   Clarification of the rules
                    double totalWeight = 0.0;
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        //if ((p_quotes[0][firstRebalancingDateInd + iDay].Date == new DateTime(2016, 11, 15) && (p_tickers[iAsset] == "TSLA")))
                        //{
                        //    int tempBlaBla = 0;
                        //}
                        double assetPrice = p_quotes[iAsset][iQ[iAsset] + iDay].ClosePrice;
                        sbyte compositeSignal = 0;    // For every stocks, sum up the four signals every day. This sum will be -4, -2, 0, +2 or +4.
                        for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                        {
                            // A long position would be initiated if the price exceeds the 75th percentile of prices over the last “n” days.The position would be closed if the price falls below the 25th percentile of prices over the last “n” days.
                            var usedQuotes = p_quotes[iAsset].GetRange(iQ[iAsset] + iDay - (p_pctChannelLookbackDays[iChannel] - 1), p_pctChannelLookbackDays[iChannel]).Select(r => r.ClosePrice);
                            assetPctChannelsLower[iAsset, iChannel] = Statistics.Quantile(usedQuotes, p_pctChannelPctLimitLower);
                            if (assetPrice < assetPctChannelsLower[iAsset, iChannel])
                                assetPctChannelsSignal[iAsset, iChannel] = -1;
                            else
                            {
                                assetPctChannelsUpper[iAsset, iChannel] = Statistics.Quantile(usedQuotes, p_pctChannelPctLimitUpper);
                                if (assetPrice > assetPctChannelsUpper[iAsset, iChannel])
                                    assetPctChannelsSignal[iAsset, iChannel] = 1;
                            }

                            compositeSignal += assetPctChannelsSignal[iAsset, iChannel];
                        }
                        assetScores[iAsset] = compositeSignal / 4.0;    // Divide it by 4 to get a signal between -1 and +1 (this will be the “score”).

                        double[] hvPctChg = new double[p_histVolLookbackDays];
                        for (int iHv = 0; iHv < p_histVolLookbackDays; iHv++)
                        {
                            hvPctChg[p_histVolLookbackDays - iHv - 1] = p_quotes[iAsset][iQ[iAsset] + iDay - iHv].ClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - iHv - 1].ClosePrice - 1;
                        }
                        // Balazs: uses "corrected sample standard deviation"; corrected: dividing by 19, not 20; He doesn't annualize. He uses daily StDev
                        assetHV[iAsset] = ArrayStatistics.StandardDeviation(hvPctChg);  // Calculate the 20-day historical volatility of daily percentage changes for every stock.
                        assetWeights[iAsset] = assetScores[iAsset] / assetHV[iAsset];   // “Score/Vol” quotients will define the weights of the stocks. They can be negative as well. 
                        if (p_isNonActiveAssetsPlayedByCash)    // in the original Varadi's strategy
                            totalWeight += Math.Abs(assetWeights[iAsset]);      // Sum up the absolute values of the “Score/Vol” quotients. TotalWeight contains even the non-active assets so have have some cash.
                        else if (assetWeights[iAsset] > 0)      // otherwise all the capital is allocated between active assets. No cash is maintained.
                            totalWeight += assetWeights[iAsset];

                    }

                    cash = pvDaily; // at rebalancing, we simulate that we sell assets, so everything is converted to Cash 1 seconds before MarketClose
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double weight = (assetWeights[iAsset] > 0) ? assetWeights[iAsset] / totalWeight : 0.0;  // If the score of a stock is positive, this ratio is the weight of the given stock. Otherwise omit from portfolio.
                        assetPos[iAsset] = pvDaily * weight;        // weight can be 0.5 positive = 50%, or  negative = -0.5, -50%. In that case we short the asset.
                        cash -= assetPos[iAsset];    // if weight is positive, assetPos is positive, so we take it away from cash. Otherwise, we short the Asset, and cash is increased.
                    }
                }   // if rebalancing

                noteToUser.Append($"{pv[iDay].Date.ToString("yyyy-MM-dd")}, {pvDaily:F2},");
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    //noteToUser.Append($"{p_quotes[iAsset][iQ[iAsset] + iDay].ClosePrice:F2},");
                    //double assetChg = p_quotes[iAsset][iQ[iAsset] + iDay].ClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].ClosePrice - 1;
                    //noteToUser.Append($"{assetChg},");

                    //for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                    //{
                    //    noteToUser.Append($"{assetPctChannelsLower[iAsset, iChannel]},");
                    //    noteToUser.Append($"{assetPctChannelsUpper[iAsset, iChannel]},");
                    //}
                    //for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                    //{
                    //    noteToUser.Append($"{assetPctChannelsSignal[iAsset, iChannel]},");
                    //}
                    //noteToUser.Append($"{assetScores[iAsset]},{assetHV[iAsset]}, {assetWeights[iAsset]},");

                    double wAsset = assetPos[iAsset] / pvDaily * 100;
                    noteToUser.Append($"{wAsset:F2}%,,");
                }
                noteToUser.Append($"{cash / pvDaily * 100:F2}%<br />");
            }

            p_noteToUserBacktest = noteToUser.ToString();
            //noteToUserBacktest = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        } // DoBacktestInTheTimeInterval_TAA





    }   // class
}
