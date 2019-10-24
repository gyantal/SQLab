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
{    public class MomTF
    {
        enum SubStrategy { Momentum, TF };
        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, Dictionary<string, StringValues> p_allParamsDict)
        {
            if (p_strategyName != "MomTF")
                return null;
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();


            // 1. read parameter strings
            // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
            string assetsStr = p_allParamsDict["Assets"][0];                                         // "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT"
            string assetsConstantLeverageStr = p_allParamsDict["AssetsConstantLeverage"][0];         // "1,1,1,-1,1.5,2,2"
            string rebalancingFrequencyStr = p_allParamsDict["RebalancingFrequency"][0];             // "Weekly,Fridays";   // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
            string momOrTFStr = p_allParamsDict["MomOrTF"][0];                                       // "Mom" // Mom is usually simpler and has a bit better performance.
            string lookbackDaysStr = p_allParamsDict["LookbackDays"][0];                             // "360", EMA (12)
            string excludeLastDaysStr = p_allParamsDict["ExcludeLastDays"][0];                 // "30" , skip last month
            string isCashAllocatedForNonActivesStr = p_allParamsDict["IsCashAllocatedForNonActives"][0];  // "Yes"  
            string cashEquivalentTickerStr = p_allParamsDict["CashEquivalentTicker"][0];             // "" (default) / "SHY" / "TLT"
            string isShortInsteadOfCashStr = p_allParamsDict["IsShortInsteadOfCash"][0];             // "No"
            string isVolScaledPosStr = p_allParamsDict["IsVolScaledPos"][0];                         // "No"
            string histVolLookbackDaysStr = p_allParamsDict["HistVolLookbackDays"][0];               // "20"
            string debugDetailToHtmlStr = p_allParamsDict["DebugDetailToHtml"][0];                   // "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels"

           
            // 2. Process parameter strings to numbers, enums; do parameter checking
            string[] tickers = assetsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            double[] assetsConstantLeveragesInput = assetsConstantLeverageStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => Double.Parse(r)).ToArray();
            double[] assetsConstantLeverages = new double[tickers.Length];
            for (int i = 0; i < assetsConstantLeverages.Length; i++)
            {
                if (i < assetsConstantLeveragesInput.Length)
                    assetsConstantLeverages[i] = assetsConstantLeveragesInput[i];
                else
                    assetsConstantLeverages[i] = 1.0;       // fill up with default 1.0, if it is not given in the input
            }

            string[] rebalancingFrequencyStrSplits = rebalancingFrequencyStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            RebalancingPeriodicity rebalancingPeriodicity = RebalancingPeriodicity.Daily;
            int dailyRebalancingDays = 1;   // 1d means every day, 2d means every 2nd days, 20d means, every 20th days
            DayOfWeek weeklyRebalancingWeekDay = DayOfWeek.Friday;
            int monthlyRebalancingOffset = -1;       // +1 means T+1, -1 means T-1
            switch (rebalancingFrequencyStrSplits[0])
            {
                case "Monthly":
                    rebalancingPeriodicity = RebalancingPeriodicity.Monthly;
                    monthlyRebalancingOffset = Int32.Parse(rebalancingFrequencyStrSplits[1].Replace("T", ""));  //"Monthly,T-1" / "Monthly,T+0"
                    break;
                case "Weekly":
                    rebalancingPeriodicity = RebalancingPeriodicity.Weekly;
                    string dayOfWeekStr = rebalancingFrequencyStrSplits[1].Substring(0, rebalancingFrequencyStrSplits[1].Length - 1);   // remove last 's's as plural. Fridays -> Friday
                    if (Enum.IsDefined(typeof(DayOfWeek), dayOfWeekStr))
                        weeklyRebalancingWeekDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeekStr, true);
                    break;
                default:    // "Daily"
                    dailyRebalancingDays = Int32.Parse(rebalancingFrequencyStrSplits[1].Replace("d",""));  // "Daily,2d"
                    break;
            }

            SubStrategy subStrategy = String.Equals(momOrTFStr, "Mom", StringComparison.OrdinalIgnoreCase) ? SubStrategy.Momentum : SubStrategy.TF;
            Int32.TryParse(lookbackDaysStr, out int lookbackDays);    // returns 0 if the conversion failed, which is OK. That is Buy&Hold.
            Int32.TryParse(excludeLastDaysStr, out int excludeLastDays);    // returns 0 if the conversion failed, which is OK.
            
            Int32.TryParse(histVolLookbackDaysStr, out int histVolLookbackDays);    // returns 0 if the conversion failed, which is OK.
            bool isCashAllocatedForNonActives = String.Equals(isCashAllocatedForNonActivesStr, "Yes", StringComparison.OrdinalIgnoreCase);
            string cashEquivalentTicker = cashEquivalentTickerStr.Trim();
            bool isShortInsteadOfCash = String.Equals(isShortInsteadOfCashStr, "Yes", StringComparison.OrdinalIgnoreCase);
            bool isVolScaledPos = String.Equals(isVolScaledPosStr, "Yes", StringComparison.OrdinalIgnoreCase);
            Dictionary<DebugDetailToHtml, bool> debugDetailToHtml = debugDetailToHtmlStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => (DebugDetailToHtml)Enum.Parse(typeof(DebugDetailToHtml), r, true)).ToDictionary(r => r, r => true);


            // 3. After Parameters are processed, load ticker price histories from DB and real time
            List<string> tickersNeeded = tickers.ToList();
            if (!String.IsNullOrEmpty(cashEquivalentTicker))
                tickersNeeded.Add(cashEquivalentTicker);
            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams.startDateUtc, p_generalParams.endDateUtc, tickersNeeded);   // Not good; for TAA, we need more quotes, earlier than p_generalParams.StartDate
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();
            IList<List<DailyData>> quotes;
            List<DailyData> cashEquivalentQuotes = null;
            if (String.IsNullOrEmpty(cashEquivalentTicker))
                quotes = getAllQuotesData.Item1;
            else
            {
                quotes = getAllQuotesData.Item1.ToList().GetRange(0, tickers.Length);
                cashEquivalentQuotes = getAllQuotesData.Item1[tickers.Length];
            }

            
            string errorToUser = "", warningToUser = "", noteToUser = "", debugMessage = "";
            DateTime commonAssetStartDate, commonAssetEndDate;
            StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(quotes, tickers, ref warningToUser, out commonAssetStartDate, out commonAssetEndDate);

            List<DailyData> pv = new List<DailyData>();
            DoBacktestInTheTimeInterval_MomTF(p_generalParams, quotes, tickers, commonAssetStartDate, commonAssetEndDate, assetsConstantLeverages,
                    rebalancingPeriodicity, dailyRebalancingDays, weeklyRebalancingWeekDay, monthlyRebalancingOffset,
                    subStrategy, lookbackDays, excludeLastDays, histVolLookbackDays, isCashAllocatedForNonActives, cashEquivalentQuotes, !isShortInsteadOfCash, isVolScaledPos,
                    debugDetailToHtml, Int32.MaxValue, "<br>", ref warningToUser, ref noteToUser, ref errorToUser, ref debugMessage, ref pv, null);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               warningToUser + "***" + noteToUser,
               errorToUser,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }

        // Implement Michael Harris's Momentum and TF. https://www.priceactionlab.com/Blog/2019/10/momentum-trend-following/
        // The performance can be bad, based on the tested period: https://www.priceactionlab.com/Blog/2019/10/risk-market-timing/ 
        // Volatility (risk) scaled momentum is good to avoid too many whipsaws: http://docentes.fe.unl.pt/~psc/MomentumMoments.pdf 
        // in the current implementation p_isVolScaledPos works like in Varadi's TAA. When used in single asset, it does nothing. It will fully allocate 100% of the portfolio to that single asset.
        // However, if two assets are used, instead of 50%-50%, the allocation varies based on their StDev(20). A bit less is given to a volatile stock. weights ratio: 1/StDev1 : 1/StDev2.
        private static void DoBacktestInTheTimeInterval_MomTF(GeneralStrategyParameters p_generalParams, IList<List<DailyData>> p_quotes, string[] p_tickers, DateTime p_commonAssetStartDate, DateTime p_commonAssetEndDate, double[] p_assetsConstantLeverages,
               RebalancingPeriodicity p_rebalancingPeriodicity, int p_dailyRebalancingDays, DayOfWeek p_weeklyRebalancingWeekDay, int p_monthlyRebalancingOffset,
               SubStrategy p_subStrategy, int p_lookbackDays, int p_excludeLastDays, int p_histVolLookbackDays, bool p_isCashAllocatedForNonActives, List<DailyData> p_cashEquivalentQuotes, bool p_isCashInsteadOfShort, bool p_isVolScaledPos,
               Dictionary<DebugDetailToHtml, bool> p_debugDetailToHtml, int p_nTradingDaysToDebugDetailToHtml, string p_noteToUserNewLine,
               ref string p_noteToUserCheckData, ref string p_noteToUser, ref string p_errorToUser, ref string p_debugMessage, ref List<DailyData> p_pv, double[] p_lastWeights)
        {
            // Multiple tickers: if we have 10 tickers and only 1 passes the Filter, then what should be its size? 100% weight is not OK, as PV will be volatile, and we will only use cache when All 10 tickers are bearish mode. 
            // However, that will never happen. If there is many tickers, the user wants to allocate only 1/n-th position to one ticker. Whether it is cash or short or long. So, the many tickers will be averaged. 
            // Imagine Nasdaq100, each played by 1/100 of the PV.

            bool p_isMomTfSignalActiveEveryDay = true;     // it is only useful for calculating signals between rebalancing days. For logging purposes only. It is not used for stop-lossing position intra-month. Stop-loss occurs only on rebalacing day.

            StringBuilder sbNoteToUser = new StringBuilder("DoBacktestInTheTimeInterval_MomTF()");
            
            // 1. Determine commonAssetStartDate
            int commonAssetStartDateInd = p_quotes[0].FindIndex(r => r.Date >= p_commonAssetStartDate);
            int commonAssetEndDateInd = p_quotes[0].FindIndex(commonAssetStartDateInd, r => r.Date >= p_commonAssetEndDate);

            // 2. Determine firstAllDataAvailableDate: shift StartDate when we have all the data
            int requiredNDays = Math.Max(p_lookbackDays, p_histVolLookbackDays); 
            int firstAllDataAvailableDateInd = commonAssetStartDateInd + requiredNDays;
            if (firstAllDataAvailableDateInd > commonAssetEndDateInd)
            {
                p_errorToUser = "firstAllDataAvailableDate cannot be determined";
                return;
            }
            DateTime firstAllDataAvailableDate = p_quotes[0][firstAllDataAvailableDateInd].Date;

            // 3. Determine First Rebalance day. Maybe only Fridays,  or maybe TotM-1. That will be the real pvStartDate
            DateTime firstRebalancingDate;
            int firstRebalancingDateInd = -1;
            if (p_rebalancingPeriodicity == RebalancingPeriodicity.Daily)
            {
                firstRebalancingDateInd = firstAllDataAvailableDateInd;
            }
            else if (p_rebalancingPeriodicity == RebalancingPeriodicity.Weekly)
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
            else if (p_rebalancingPeriodicity == RebalancingPeriodicity.Monthly)
            {
                for (int i = firstAllDataAvailableDateInd; i <= commonAssetEndDateInd; i++)
                {
                    int inspectedDayOffset = -1 * p_monthlyRebalancingOffset;        // -1, 0,... "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
                    int inspectedIdx = i + inspectedDayOffset;
                    if (p_quotes[0][inspectedIdx - 1].Date.Month != p_quotes[0][inspectedIdx].Date.Month)   // the inspected day and previous day should have different months
                    {
                        firstRebalancingDateInd = i;
                        break;
                    }
                }
            }

            if (firstRebalancingDateInd == -1)
            {
                p_errorToUser = "StartDate cannot be determined";
                return;
            }
            firstRebalancingDate = p_quotes[0][firstRebalancingDateInd].Date;


            // 4. pvStartDate is now final, calculate the index of that startDate for each asset quotes
            DateTime pvStartDate = firstRebalancingDate;
            DateTime pvEndDate = p_commonAssetEndDate;
            int nDays = commonAssetEndDateInd - firstRebalancingDateInd + 1;        // startDate, endDate is included
            int nAssets = p_quotes.Count;
            int[] iQ = new int[nAssets];    // iQ is the index for startdates in each asset-quote
            for (int i = 0; i < nAssets; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }
            DateTime debugDetailToHtmlMinDate = (p_nTradingDaysToDebugDetailToHtml >= nDays) ? pvStartDate : p_quotes[0][firstRebalancingDateInd + nDays - p_nTradingDaysToDebugDetailToHtml].Date;

            // QQQ starts from 1999, TLT starts from only 2002; in that case we want the backtest to start from 1999 (more useful), but for the 1999-2002 period, it will use Cash, instead of TLT. Still, it is a more meaningful, longer backtest.
            DateTime cashEqStartDate = DateTime.MinValue;
            int cashEqRunDateInd = -1;
            if  (p_cashEquivalentQuotes != null) 
            {
                cashEqRunDateInd = p_cashEquivalentQuotes.FindIndex(r => r.Date == pvStartDate); // on first day, don't calculate %change, so address the next item (++) in the price, as that will be the first index to be used.
                if (cashEqRunDateInd != -1) // if pvStartDate is found in the cash-eq-quotes, then cash-eq-quotes has a very long history. Good.
                {
                    cashEqRunDateInd++;
                    cashEqStartDate = p_cashEquivalentQuotes[cashEqRunDateInd].Date;
                } else
                {   // if pvStartDate (1999 for QQQ) is not found in the cash-eq-quotes (TLT starts in 2020), then cash-eq-quotes only starts much later. We will start to use the first date when cash-eq %change is available, which is index 1. (the second item)
                    cashEqRunDateInd = 1;
                    cashEqStartDate = p_cashEquivalentQuotes[1].Date;
                }                
            }


            List<DailyData> pv = new List<DailyData>(nDays);
            double pvDaily = 100.0;
            double cash = pvDaily;
            sbyte[] assetMomTfSignal = new sbyte[nAssets];  // for assets
            double[] assetScores = new double[nAssets]; // score can be 1.0 for bullish, -1.0 bearish. 0.0. neutral.
            double[] assetHV = new double[nAssets];
            double[] assetWeights = new double[nAssets];    // in TAA it was: assetScores[iAsset] / assetHV[iAsset];    // in MomTF it can also be the VolWeights: 100%, 0%, -100%, weighted by the HV
            double[] assetPos = new double[nAssets];    // the allocated $cash of the $PV to this asset.

            for (int iAsset = 0; iAsset < nAssets; iAsset++)
            {
                assetPos[iAsset] = 0.0;
            }

            sbNoteToUser.AppendLine("Date, pvDaily, {assetPrice, assetPctChange, assetScore, assetHV, assetWeights, assetWeightsBasedOnPV}... ,cashWeight <br>");
            for (int iDay = 0; iDay < nDays; iDay++)    // march for all days
            {
                // 1. Evaluate the value of the portfolio based on assetPos and this day's %change
                pvDaily = 0;
                if (iDay != 0)    // on first day, don't calculate %change, we may not have previous day
                {
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double assetChg = (p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].AdjClosePrice - 1) * p_assetsConstantLeverages[iAsset];
                        assetPos[iAsset] *= (1.0 + assetChg);
                        pvDaily += assetPos[iAsset];
                    }
                 
                    if (p_cashEquivalentQuotes != null && p_quotes[0][firstRebalancingDateInd + iDay].Date >= cashEqStartDate)
                    {
                        //int neededCashEqStartDateInd = p_cashEquivalentQuotes.FindIndex(r => r.Date == p_quotes[0][iQ[0] + iDay].Date);
                        double cashChg = 1.0;
                        if (cashEqRunDateInd > 0) // first day of Cash substitute has no %Chg, because there is no previous day.
                            cashChg = p_cashEquivalentQuotes[cashEqRunDateInd].AdjClosePrice / p_cashEquivalentQuotes[cashEqRunDateInd - 1].AdjClosePrice;
                        cashEqRunDateInd++;
                        cash *= cashChg;
                    }
                }
                pvDaily += cash;    // cash has to be added, on first day or on other days
                pv.Add(new DailyData() { Date = p_quotes[0][firstRebalancingDateInd + iDay].Date, AdjClosePrice = pvDaily });

                bool isRebalanceDay = false;
                if (p_rebalancingPeriodicity == RebalancingPeriodicity.Daily)
                    isRebalanceDay = (iDay % p_dailyRebalancingDays == 0); // test: every periodic days
                else if (p_rebalancingPeriodicity == RebalancingPeriodicity.Weekly)
                    isRebalanceDay = p_quotes[0][firstRebalancingDateInd + iDay].Date.DayOfWeek == p_weeklyRebalancingWeekDay;
                else if (p_rebalancingPeriodicity == RebalancingPeriodicity.Monthly)
                {
                    int inspectedDayOffset = -1 * p_monthlyRebalancingOffset;        // -1, 0,... "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
                    int inspectedIdx = firstRebalancingDateInd + iDay + inspectedDayOffset;
                    if (inspectedIdx < p_quotes[0].Count)
                        isRebalanceDay = (p_quotes[0][inspectedIdx - 1].Date.Month != p_quotes[0][inspectedIdx].Date.Month);   // the inspected day and previous day should have different months
                }

                // 2. adjust assetPctChannelsSignal[]. Most of the time it is needed on every day, even if there is no rebalancing.
                // they can signal exit of asset intramonth, even if rebalance happens only at end of the month. (indication in the Varadi replication people that Varadi does this. Also it play short term MR, which is good.)
                if (p_isMomTfSignalActiveEveryDay || isRebalanceDay)
                {
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        //if ((p_quotes[0][firstRebalancingDateInd + iDay].Date == new DateTime(2016, 11, 15) && (p_tickers[iAsset] == "TSLA")))
                        //{
                        //    int tempBlaBla = 0;
                        //}         
                        assetMomTfSignal[iAsset] = -1;  // bearish signal
                        if (p_subStrategy == SubStrategy.Momentum)  // it uses today's one and the one 240 days ago. Altogether a span of 241 days are needed.
                        {
                            int assetPriceCheckFromInd = iQ[iAsset] + iDay - p_lookbackDays;    // p_lookbackDays = 240
                            int assetPriceCheckToInd = iQ[iAsset] + iDay - p_excludeLastDays;
                            double assetPriceCheckFrom = p_quotes[iAsset][assetPriceCheckFromInd].AdjClosePrice;
                            double assetPriceCheckTo = p_quotes[iAsset][assetPriceCheckToInd].AdjClosePrice;
                            if (assetPriceCheckTo >= assetPriceCheckFrom)
                                assetMomTfSignal[iAsset] = 1;
                        } else  // it uses today's one and the one 240 days ago. Altogether a span of 241 days are needed. For SMA, we use 240 items.
                        {
                            //int assetTodayInd = iQ[iAsset] + iDay;
                            int assetPriceCheckInd = iQ[iAsset] + iDay - p_excludeLastDays;
                            int lookbackDaysAdj = p_lookbackDays - p_excludeLastDays;    // 240-20=220,  we want TF to use about the same days as Momentum. If SMA(240) is given, but we ignore the last 20 days, when SMA is calculated 20 days ago, it shouldn't use earlier data than Momentum, so, we should use SMA(220) as override
                            double assetPrice = p_quotes[iAsset][assetPriceCheckInd].AdjClosePrice; 
                            double movingAvg = 0.0; // SMA is used now, EMA can be used later.
                            int nAvg = 0;
                            for (int iMaD = assetPriceCheckInd - lookbackDaysAdj; iMaD < assetPriceCheckInd; iMaD++)    // this SMA will contain the 240 previous days, but not today
                            {
                                movingAvg += p_quotes[iAsset][iMaD].AdjClosePrice;
                                nAvg++;
                            }
                            movingAvg /= (double)lookbackDaysAdj;
                            if (assetPrice >= movingAvg)
                                assetMomTfSignal[iAsset] = 1;
                        }
                            
                    }
                }

                // 3. On rebalancing days allocate assetPos[]. This will not change PV.
                if (isRebalanceDay)
                {
                     // 3.1 Calculate assetWeights
                    double totalWeight = 0.0;
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        assetScores[iAsset] = assetMomTfSignal[iAsset]; // in TAA, score was signal/4.0;

                        double[] hvPctChg = new double[p_histVolLookbackDays];
                        for (int iHv = 0; iHv < p_histVolLookbackDays; iHv++)
                        {
                            hvPctChg[p_histVolLookbackDays - iHv - 1] = p_quotes[iAsset][iQ[iAsset] + iDay - iHv].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - iHv - 1].AdjClosePrice - 1;
                        }
                        // Balazs: uses "corrected sample standard deviation"; corrected: dividing by 19, not 20; He doesn't annualize. He uses daily StDev
                        // if p_histVolLookbackDays == 0, it means we don't do HV weighting, we use HV = 1.0
                        assetHV[iAsset] = (p_histVolLookbackDays == 0) ? 1.0 : ArrayStatistics.StandardDeviation(hvPctChg);  // Calculate the 20-day historical volatility of daily percentage changes for every stock.
                        assetWeights[iAsset] = (p_isVolScaledPos) ? assetScores[iAsset] / assetHV[iAsset] : assetScores[iAsset];   // “Score/Vol” quotients will define the weights of the stocks. They can be 0 or negative as well. 
                        // there is an interesting observation here. Actually, it is a good behavour.
                        // If assetScores[i]=0, assetWeights[i] becomes 0, so we don't use its weight when p_isCashAllocatedForNonActives => TLT will not fill its Cash-place; NO TLT will be invested (if this is the only stock with 0 score), the portfolio will be 100% in other stocks. We are more Brave.
                        // However, if assetScores[i]<0 (negative), assetWeights[i] becomes a proper negative number. It will be used in TotalWeight calculation => TLT will fill its's space. (if this is the only stock with negative score), TLT will be invested in its place; consequently the portfolio will NOT be 100% in other stocks. We are more defensive.
                        if (p_isCashAllocatedForNonActives)    // == p_is Place AllocatedForNonActives. If yes, it means we will do an average strategy over the tickers. Make sense. If false, then one ticker can consume the space left by the other ticker
                            totalWeight += Math.Abs(assetWeights[iAsset]);      // Sum up the absolute values of the “Score/Vol” quotients. TotalWeight contains even the non-active assets so have have some cash.
                        else
                        {
                            if (assetWeights[iAsset] <= 0 && p_isCashInsteadOfShort)
                            {
                                // if bearish bet and it bearish is played as cash, weight will be 0.0. So, don't add to total.
                            }
                            else
                                totalWeight += Math.Abs(assetWeights[iAsset]);  // if bullish bet, or if bearish bet and played by Shorting
                        }
                    }
                    // 3.2 With assetWeights calculated, do the rebalancing of assetPos[]
                    cash = pvDaily; // at rebalancing, we simulate that we sell assets, so everything is converted to Cash 1 seconds before MarketClose
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double weight = (assetWeights[iAsset] <= 0 && p_isCashInsteadOfShort) ?  0.0 : assetWeights[iAsset] / totalWeight;  // If the score of a stock is positive, this ratio is the weight of the given stock. Otherwise omit from portfolio.
                        assetPos[iAsset] = pvDaily * weight;        // weight can be 0.5 positive = 50%, or  negative = -0.5, -50%. In that case we short the asset.
                        cash -= assetPos[iAsset];    // if weight is positive, assetPos is positive, so we take it away from cash. Otherwise, we short the Asset, and cash is increased.
                    }
                }   // if rebalancing


                if (pv[iDay].Date >= debugDetailToHtmlMinDate)
                {
                    bool wasAnyNoteToUser = false;
                    string noteToUserRow = String.Empty;
                    if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.Date))
                    {
                        noteToUserRow += $"{pv[iDay].Date.ToString("yyyy-MM-dd")}";
                        wasAnyNoteToUser = true;
                    }
                    if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.PV))
                    {
                        noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{pv[iDay].AdjClosePrice:F2}";
                        wasAnyNoteToUser = true;
                    }
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.AssetData))
                        {
                            double assetChg = (iQ[iAsset] + iDay - 1 >= 0) ? p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].AdjClosePrice - 1 : 0.0;
                            noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"${p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice:F2}, {assetChg * 100.0:F2}%";
                            wasAnyNoteToUser = true;
                        }

                        if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.AssetData))
                        {
                            noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{assetMomTfSignal[iAsset]},{assetScores[iAsset]},{assetHV[iAsset]:F6}, {assetWeights[iAsset]:F3}";
                            wasAnyNoteToUser = true;
                        }

                        if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.AssetFinalWeights))
                        {
                            double wAsset = assetPos[iAsset] / pvDaily * 100;
                            noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{wAsset:F2}%,,";
                            wasAnyNoteToUser = true;
                        }
                    }
                    if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.CashWeight))
                    {
                        noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{cash / pvDaily * 100:F2}%";
                        wasAnyNoteToUser = true;
                    }
                    if (!String.IsNullOrEmpty(noteToUserRow))
                        sbNoteToUser.Append(noteToUserRow + p_noteToUserNewLine);
                }
            } // march for all days

            if (p_lastWeights != null)
            {
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    p_lastWeights[iAsset] = assetPos[iAsset] / pvDaily;
                }
                p_lastWeights[nAssets] = cash / pvDaily;
            }
            if (p_pv != null)
                p_pv = pv;

            p_noteToUser = sbNoteToUser.ToString();
            //noteToUser = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        } // DoBacktestInTheTimeInterval_MomTF

    }   // class
}
