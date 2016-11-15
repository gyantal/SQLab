using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    enum RebalancingPeriodicity { Daily, Weekly, Monthly };

    public class DailyTaaData
    {
        public DateTime Date { get; set; }
        public double PvClosePrice { get; set; }

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
            string pctChannelPctLimitStrs = p_allParamsDict["PctChannelPctLimits"][0];               // "30-70"
            string pctChannelIsConditionalStr = p_allParamsDict["PctChannelIsConditional"][0];       // "Yes"
            string histVolLookbackDaysStr = p_allParamsDict["HistVolLookbackDays"][0];               // "20"
            string dynamicLeverageClmtParamsStr = p_allParamsDict["DynamicLeverageClmtParams"][0];   // "SMA(SPX,50d,200d); PR(XLU,VTI,20d)";   // SPX 50/200 crossover; PR=PriceRatio of XLU/VTI for 20 days
            string uberVxxEventsParamsStr = p_allParamsDict["UberVxxEventsParams"][0];               // "FOMC;Holidays"

            RebalancingPeriodicity rebalancingPeriodicity = RebalancingPeriodicity.Weekly;
            DayOfWeek weeklyRebalancingWeekDay = DayOfWeek.Friday;
            int[] pctChannelLookbackDays = pctChannelLookbackDaysStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Select(r => Int32.Parse(r)).ToArray();
            int histVolLookbackDays = Int32.Parse(histVolLookbackDaysStr);
            int clmt1_shorterDays = 50, clmt1_longerDays = 200, clmt2_utilsLookbackDays = 20;

            string[] tickers = assetsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams, tickers.ToList());
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();
            var quotes = getAllQuotesData.Item1;

            string warningToUser = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";

            List<DailyData> pv;
            DoBacktestInTheTimeInterval_TAA(quotes, tickers, 
                    rebalancingPeriodicity, weeklyRebalancingWeekDay, 
                    pctChannelLookbackDays, histVolLookbackDays,
                    clmt1_shorterDays, clmt1_longerDays, clmt2_utilsLookbackDays, ref warningToUser, ref noteToUserBacktest, ref errorMessage, ref debugMessage, out pv);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               warningToUser + "***" + noteToUserBacktest, 
               errorMessage,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }


        private static void DoBacktestInTheTimeInterval_TAA(IList<List<DailyData>> p_quotes, string[] p_tickers,
                RebalancingPeriodicity p_rebalancingPeriodicity, DayOfWeek p_weeklyRebalancingWeekDay,
                int[] p_pctChannelLookbackDays, int p_histVolLookbackDays,
                int clmt1_shorterDays, int clmt1_longerDays, int clmt2_utilsLookbackDays,
                ref string p_noteToUserCheckData, ref string p_noteToUserBacktest, ref string errorMessage, ref string debugMessage, out List<DailyData> pv)
        {
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
                for (int i = firstAllDataAvailableDateInd; i < p_quotes[0].Count; i++)
                {
                    if (p_quotes[0][i].Date.DayOfWeek == p_weeklyRebalancingWeekDay)
                    {
                        firstRebalancingDateInd = i;
                        break;
                    }
                }
            }
            if ((firstRebalancingDateInd == -1) || (firstRebalancingDateInd > commonAssetEndDateInd))
            {
                errorMessage = "StartDate cannot be determined";
                return;
            }
            firstRebalancingDate = p_quotes[0][firstRebalancingDateInd].Date;










            // 4. Having proper firstRebalancingDate, deep copy first quote to PV.
            List<DailyTaaData> pvData = new List<DailyTaaData>(commonAssetEndDateInd - firstRebalancingDateInd);
            for (int i = firstRebalancingDateInd; i <= commonAssetEndDateInd; i++)
            {
                pvData.Add(new DailyTaaData() { Date = p_quotes[0][i].Date, PvClosePrice = p_quotes[0][i].ClosePrice, HistVol = 0.18 });
            }

            // 5. that PV is fine, but we need to calculate Daily Data 252 i





            pv = new List<DailyData>(commonAssetEndDateInd - firstRebalancingDateInd);
            for (int i = firstRebalancingDateInd; i <= commonAssetEndDateInd; i++)
            {
                pv.Add(new DailyData() { Date = p_quotes[0][i].Date, ClosePrice = p_quotes[0][i].ClosePrice});
            }














            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            int[] iQ = new int[p_quotes.Count];
            for (int i = 0; i < p_quotes.Count; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }

            

            double pvDaily = 100.0;
            double cash = pvDaily;
            double equalWeight = 1.0 / (double)p_quotes.Count;
            double[] assetPos = new double[p_quotes.Count];
            for (int i = 0; i < p_quotes.Count; i++)
            {
                assetPos[i] = pvDaily * equalWeight;        // weight can be 0.5 positive = 50%, or  negative = -0.5, -50%. In that case we short the asset.
                cash -= assetPos[i];    // if weight is positive, assetPos is positive, so we take it away from cash. Otherwise, we short the Asset, and cash is increased.
            }
            pv[0].ClosePrice = pvDaily; // on the date when the first quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            for (int iDay = 1; iDay < pv.Count(); iDay++)
            {
                pvDaily = cash;
                for (int i = 0; i < p_quotes.Count; i++)
                {
                    double assetChg = p_quotes[i][iQ[i] + iDay].ClosePrice / p_quotes[i][iQ[i] + iDay - 1].ClosePrice;
                    assetPos[i] *= assetChg;
                    pvDaily += assetPos[i];
                }

                //if (i % p_rebalancingTradingDays == 0)    // every periodic days
                //{


                //}

                pv[iDay].ClosePrice = pvDaily;
            }

            p_noteToUserBacktest = "Nothing.";
            //noteToUserBacktest = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        } // DoBacktestInTheTimeInterval_TAA





    }   // class
}
