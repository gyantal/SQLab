using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    public partial class LEtf
    {

        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, Dictionary<string, StringValues> p_allParamsDict)
        {
            string errorToUser = "", warningToUser = "", noteToUser = "", debugMessage = "";

            if (p_strategyName != "LETFDiscrRebToNeutral" && p_strategyName != "LETFDiscrAddToWinner" && p_strategyName != "LETFHarryLong")
                return null;
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();

            // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
            // 1. read parameter strings
            string assetsStr = p_allParamsDict["Assets"][0];                                         // "TVIX,TMV"
            string assetsWeightPctStr = p_allParamsDict["AssetsConstantWeightPct"][0];              // "-35,-65"
            string rebalancingFrequencyStr = p_allParamsDict["RebalancingFrequency"][0];             // "Daily,1d";   // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)


            // 2. Process parameter strings to numbers, enums; do parameter checking
            string[] tickers = assetsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            double[] assetsWeightPctInput = assetsWeightPctStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => Double.Parse(r)).ToArray();
            double[] assetsWeights = new double[tickers.Length];
            for (int i = 0; i < tickers.Length; i++)
            {
                tickers[i] = tickers[i].Trim(); // remove extra whitespace from end
                if (i < assetsWeightPctInput.Length)
                    assetsWeights[i] = assetsWeightPctInput[i] / 100.0;   // weights were given in percentange, "-35,-65". Convert it to -0.35 as weights
                else
                    assetsWeights[i] = 1.0;       // fill up with default 1.0=100%, if it is not given in the input
            }

            // if assetWeight[i] = 0, we don't need the ticker to be honest. In theory, we could drop those tickers totally. We would save to query Historical SQL data or realtime data for those zero weight stocks.
            // However, that would change the commonAssetStartDate if we skip zero-weight stocks, and we may not want that. We liked that firstRebalancingDate = commonAssetStartDate, because we can test multiple ETFs together.
            // If there is no RT price for a zero-weight ticker (like GLD in 2017-06, but later we fixed that error, 'contract was ambiguous') a workaround is to remove GLD from the list of ETFs textbox.
            // However, note that VBroker 'should' give "SnapshotTime" RT price for all stocks, not only for the ones that are registered with ReqMktDataStream(); 

            string[] rebalancingFrequencyStrSplits = rebalancingFrequencyStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            RebalancingPeriodicity rebalancingPeriodicity = RebalancingPeriodicity.Daily;
            int dailyRebalancingDays = 1;   // 1d means every day, 2d means every 2nd days, 20d means, every 20th days
            DayOfWeek weeklyRebalancingWeekDay = DayOfWeek.Friday;
            int monthlyRebalancingOffset = -1;       // +1 means T+1, -1 means T-1
            switch (rebalancingFrequencyStrSplits[0])
            {
                case "Monthly":
                case "Weekly":
                    throw new NotImplementedException("Only Daily rebalancing frequency is implemented. Use 5d for weekly and 20d for monhly if necessary.");
                default:    // "Daily"
                    dailyRebalancingDays = Int32.Parse(rebalancingFrequencyStrSplits[1].Replace("d", ""));  // "Daily,2d"
                    break;
            }

            // 3. After Parameters are processed, load ticker price histories from DB and real time
            List<string> tickersNeeded = new List<string>();
            for (int i = 0; i < tickers.Length; i++)
            {
                if (!tickers[i].Equals("Cash"))
                    tickersNeeded.Add(tickers[i]);
            }
            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams.startDateUtc, p_generalParams.endDateUtc, tickersNeeded);  // only the necessary quotes are asked from SQL. Good.
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            IList<List<DailyData>> quotesNeeded = getAllQuotesData.Item1;
            stopWatch.Stop();
            IList<List<DailyData>> quotes = new List<List<DailyData>>();
            for (int i = 0; i < tickers.Length; i++)
            {
                if (tickers[i].Equals("Cash"))  // only 1 Cash ticker likely to exist; create its quote with the longest history quote
                {
                    int longestHistoryQuoteInd = -1;
                    int longestHistoryQuoteLength = Int32.MinValue;
                    for (int j = 0; j < quotesNeeded.Count; j++)
                    {
                        if (quotesNeeded[j].Count > longestHistoryQuoteLength)
                        {
                            longestHistoryQuoteLength = quotesNeeded[j].Count;
                            longestHistoryQuoteInd = j;
                        }
                    }
                    quotes.Add(quotesNeeded[longestHistoryQuoteInd].Select(item => new DailyData() { Date = item.Date, AdjClosePrice = 100.0 }).ToList());
                }
                else
                {

                    int indNeeded = tickersNeeded.IndexOf(tickers[i]);
                    if (quotesNeeded[indNeeded].Count == 0)
                    {
                        errorToUser += $"No price quotes found for ticker '{tickers[i]}'. Consider UPPERCASE."; // Don't want to auto-convert tickers to Uppercase, because 'SVXY!Light0.5x.SQ' is a perfectly good ticker with lowercase letter. User should learn that SPY is different to 'spy'.
                    }
                    quotes.Add(quotesNeeded[indNeeded]);

                }
            }

          if (!String.IsNullOrEmpty(errorToUser))
            {
                StrategyResult stratResult1 = new StrategyResult() { errorMessage = errorToUser };
                return JsonConvert.SerializeObject(stratResult1);
            }

  

            StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(quotes, tickers, ref warningToUser, out DateTime commonAssetStartDate, out DateTime commonAssetEndDate);

            List<DailyData> pv = new List<DailyData>();

            if (String.Equals(p_strategyName, "LETFDiscrRebToNeutral", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_RebalanceToNeutral(p_generalParams, quotes, tickers, commonAssetStartDate, commonAssetEndDate, assetsWeights,
                    rebalancingPeriodicity, dailyRebalancingDays, weeklyRebalancingWeekDay, monthlyRebalancingOffset,
                    "<br>", ref warningToUser, ref noteToUser, ref errorToUser, ref debugMessage, ref pv);
            }
            else if (String.Equals(p_strategyName, "LETFDiscrAddToWinner", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(p_generalParams, quotes, tickers, commonAssetStartDate, commonAssetEndDate, assetsWeights,
                    rebalancingPeriodicity, dailyRebalancingDays, weeklyRebalancingWeekDay, monthlyRebalancingOffset,
                    "<br>", ref warningToUser, ref noteToUser, ref errorToUser, ref debugMessage, ref pv);
            }
            else
                DoBacktestInTheTimeInterval_HarryLong(p_generalParams, quotes, tickers, commonAssetStartDate, commonAssetEndDate, assetsWeights,
                    rebalancingPeriodicity, dailyRebalancingDays, weeklyRebalancingWeekDay, monthlyRebalancingOffset,
                    "<br>", ref warningToUser, ref noteToUser, ref errorToUser, ref debugMessage, ref pv);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               warningToUser + "***" + noteToUser,
               errorToUser,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }

        private static void DoBacktestInTheTimeInterval_HarryLong(GeneralStrategyParameters p_generalParams, IList<List<DailyData>> p_quotes, string[] p_tickers, DateTime p_commonAssetStartDate, DateTime p_commonAssetEndDate, double[] p_assetsWeights,
               RebalancingPeriodicity p_rebalancingPeriodicity, int p_dailyRebalancingDays, DayOfWeek p_weeklyRebalancingWeekDay, int p_monthlyRebalancingOffset,
               string p_noteToUserNewLine,
               ref string p_noteToUserCheckData, ref string p_noteToUser, ref string p_errorToUser, ref string p_debugMessage, ref List<DailyData> p_pv)
        {
            StringBuilder sbNoteToUser = new StringBuilder("Rebalances at the specified frequencies. HarryLong style.<br>");
            StringBuilder sbDebugToUser = new StringBuilder("Date, PVDaily, PvLeverage, Weight_1 ... Weight_N<br>");

            // implement CLMT in a way, that those data days don't restrict Strategy StartDate. If they are not available on a day, simple 100% is used. CLMT: "SMA(SPX,50d,200d); PR(XLU,VTI,20d)"
            // 1. CommonAssetStartDate is already correct, because only the necessary quotes are asked from SQL. (After backtest StartDate). Good.
            int commonAssetStartDateInd = p_quotes[0].FindIndex(r => r.Date >= p_commonAssetStartDate);
            int commonAssetEndDateInd = p_quotes[0].FindIndex(commonAssetStartDateInd, r => r.Date >= p_commonAssetEndDate);
            int firstRebalancingDateInd = commonAssetStartDateInd;

            // 4. pvStartDate is now final, calculate the index of that startDate for each asset quotes
            DateTime pvStartDate = p_commonAssetStartDate;
            DateTime pvEndDate = p_commonAssetEndDate;
            int nDays = commonAssetEndDateInd - firstRebalancingDateInd + 1;        // startDate, endDate is included
            int nAssets = p_quotes.Count;
            int[] iQ = new int[nAssets];
            for (int i = 0; i < nAssets; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }

            List<DailyData> pv = new List<DailyData>(nDays);

            double pvDaily = 100.0;
            double cash = pvDaily;
            double[] assetPos = new double[nAssets];        // default values are 0.0
            double[] assetWeights = new double[nAssets];    // default values are 0.0

            for (int iDay = 0; iDay < nDays; iDay++)    // march for all days
            {
                // 1. Evaluate the value of the portfolio based on assetPos and this day's %change
                
                pvDaily = 0;
                if (iDay != 0)    // on first day, don't calculate %change, we may not have previous day
                {
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double assetChg = (p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].AdjClosePrice - 1);
                        assetPos[iAsset] *= (1.0 + assetChg);
                        pvDaily += assetPos[iAsset];
                        
                    }
                }
                pvDaily += cash;    // cash has to be added, on first day or on other days
                pv.Add(new DailyData() { Date = p_quotes[0][firstRebalancingDateInd + iDay].Date, AdjClosePrice = pvDaily });

                // 2. Debug info to UI
                sbDebugToUser.Append($"{pv[iDay].Date},{pvDaily},");
                double totalAssetPosExposure = 0.0;
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    totalAssetPosExposure += Math.Abs(assetPos[iAsset]);
                    sbDebugToUser.Append($"{Math.Abs(assetPos[iAsset]) / pvDaily},");
                }
                sbDebugToUser.AppendLine($"{Math.Abs(totalAssetPosExposure) / pvDaily}<br>");

                // 3. On rebalancing days allocate assetPos[]. This will not change PV.
                bool isRebalanceDay = false;
                if (p_rebalancingPeriodicity == RebalancingPeriodicity.Daily)
                    isRebalanceDay = (iDay % p_dailyRebalancingDays == 0); // test: every periodic days
                if (isRebalanceDay)
                {
                    // With assetWeights known, do the rebalancing of assetPos[]
                    cash = pvDaily; // at rebalancing, we simulate that we sell assets, so everything is converted to Cash 1 seconds before MarketClose
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        assetPos[iAsset] = pvDaily * p_assetsWeights[iAsset];        // weight can be 0.5 positive = 50%, or  negative = -0.5, -50%. In that case we short the asset.
                        cash -= assetPos[iAsset];    // if weight is positive, assetPos is positive, so we take it away from cash. Otherwise, we short the Asset, and cash is increased.
                    }
                }   // if rebalancing
                
            }  // march for all days

            if (p_pv != null)
                p_pv = pv;

            p_noteToUser = sbNoteToUser.ToString();
            p_debugMessage = sbDebugToUser.ToString();
        }





        // Idea: every 5-20 days, rebalance it to market neutral;
        //This is equivalent of the HarryLong with 2 tickers, "URE, SRS", with -50%,-50% weight and daily rebalancing.Because HarryLong always Rebalances to neutral (=fixed) weights.
        //So, this code is not necessary here.However, the AddToTheWinningSideWithLeverage() cannot be simulated with HarryLong, so keep this couple of lines of code for simplicity 
        //and easy comparison to the AddToTheWinningSideWithLeverage() version.
        private static void DoBacktestInTheTimeInterval_RebalanceToNeutral(GeneralStrategyParameters p_generalParams, IList<List<DailyData>> p_quotes, string[] p_tickers, DateTime p_commonAssetStartDate, DateTime p_commonAssetEndDate, double[] p_assetsWeights,
               RebalancingPeriodicity p_rebalancingPeriodicity, int p_dailyRebalancingDays, DayOfWeek p_weeklyRebalancingWeekDay, int p_monthlyRebalancingOffset,
               string p_noteToUserNewLine,
               ref string p_noteToUserCheckData, ref string p_noteToUser, ref string p_errorToUser, ref string p_debugMessage, ref List<DailyData> p_pv)
        {
            p_noteToUser = "Rebalances to be market neutral at the specified frequencies. Using only the first 2 tickers in the list.";

            DateTime pvStartDate = p_commonAssetStartDate;

            List<DailyData> quotes1 = p_quotes[0];
            List<DailyData> quotes2 = p_quotes[1];
            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = quotes1.FindIndex(row => row.Date == pvStartDate);
            int iBearish = quotes2.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            int iBullishEndDateInd = quotes1.FindIndex(iBullish, r => r.Date >= p_commonAssetEndDate);
            int nDays = iBullishEndDateInd - iBullish + 1;        // startDate, endDate is included
            List<DailyData> pv = new List<DailyData>(nDays) {
                new DailyData() { Date = quotes1[iBullish].Date, AdjClosePrice = pvDaily }};  // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            for (int i = 1; i < nDays; i++) // march for all days
            {
                double buEtfChg = quotes1[iBullish + i].AdjClosePrice / quotes1[iBullish + i - 1].AdjClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = quotes2[iBearish + i].AdjClosePrice / quotes2[iBearish + i - 1].AdjClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;
                if (i % p_dailyRebalancingDays == 0)    // every periodic days
                {
                    bullishEtfPosition = pvDaily * -0.5;
                    bearishEtfPosition = pvDaily * -0.5;

                    cash = pvDaily * 2.0;
                }
                pv.Add(new DailyData() { Date = quotes1[iBullish + i].Date, AdjClosePrice = pvDaily });
            } // march for all days

            if (p_pv != null)
                p_pv = pv;
        }


        //- the previous strategy: rebalance every 20 days: 13.18%CAGR. rebalancing 10day: CAGR: 14.79% .not much. And I have to pay the borrowing fee.
        //So I didn't gain money this way:
        //+ I didn't rebalance to market Neutral in real life, but I did a market bet.... shorting more money into the one that went down, and keeping
        //the overleverage of the other side.
        //Test this: every X, 20 days. Keep the other leg. Short more from the etf that is down. 
        //So, I follow the trend: put more money into the right place. That is how I played.
        private static void DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(GeneralStrategyParameters p_generalParams, IList<List<DailyData>> p_quotes, string[] p_tickers, DateTime p_commonAssetStartDate, DateTime p_commonAssetEndDate, double[] p_assetsWeights,
               RebalancingPeriodicity p_rebalancingPeriodicity, int p_dailyRebalancingDays, DayOfWeek p_weeklyRebalancingWeekDay, int p_monthlyRebalancingOffset,
               string p_noteToUserNewLine,
               ref string p_noteToUserCheckData, ref string p_noteToUser, ref string p_errorToUser, ref string p_debugMessage, ref List<DailyData> p_pv)
        {
            p_noteToUser = "Rebalances at the specified frequencies. But AddToTheWinningSideWithLeverage. Using only the first 2 tickers in the list.";
            StringBuilder sbDebugToUser = new StringBuilder("Date, PVDaily, BullishLeverage, BearishLeverage, Leverage, RatioBullishPerBearish<br>");
            DateTime pvStartDate = p_commonAssetStartDate;

            List<DailyData> quotes1 = p_quotes[0];
            List<DailyData> quotes2 = p_quotes[1];
            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = quotes1.FindIndex(row => row.Date == pvStartDate);
            int iBearish = quotes2.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            int iBullishEndDateInd = quotes1.FindIndex(iBullish, r => r.Date >= p_commonAssetEndDate);
            int nDays = iBullishEndDateInd - iBullish + 1;        // startDate, endDate is included
            List<DailyData> pv = new List<DailyData>(nDays) {
                new DailyData() { Date = quotes1[iBullish].Date, AdjClosePrice = pvDaily } }; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            // usually it is 50%=0.5, when it goes under 47%, short more of this side.
            double tooLowStockLeverage = 0.47;     
            double tooHighPortfolioLeverage = 2.0;      // we can play double leverage, because it is quite balanced LongShort, so not risky
            double ratioTooLow = 0.77;  // 1/1.3=0.77
            double ratioTooHigh = 1.3;

            int nRatioUnbalanceShortMoreWinning = 0;
            int nUnderLeveragedShortMoreWinning = 0;
            int nOkLeveragedDoNothing = 0;
            int nOverLeveragedRebalanceToNavAndNeutral = 0;
            for (int i = 1; i < nDays; i++)  // march for all days
            {
                double buEtfChg = quotes1[iBullish + i].AdjClosePrice / quotes1[iBullish + i - 1].AdjClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = quotes2[iBearish + i].AdjClosePrice / quotes2[iBearish + i - 1].AdjClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;

                if (i % p_dailyRebalancingDays == 0)    // every periodic days
                {
                    double leverage = Math.Abs(bullishEtfPosition + bearishEtfPosition) / pvDaily;
                    if (leverage >= tooHighPortfolioLeverage)    // if we are over-leveraged -> bad. Margin call risk. Rebalance to NAV and market neutral.
                    {
                        nOverLeveragedRebalanceToNavAndNeutral++;
                        bullishEtfPosition = pvDaily * -0.5;
                        bearishEtfPosition = pvDaily * -0.5;

                        cash = pvDaily * 2.0;
                    }
                    else
                    {
                        // bullishEtfLeverage = 44%, while bearishLeverage = 96%. Altogether = 140% leverage.
                        // it was allowed and BullishLeverage was not increased, because it was over 47%. Bad.
                        // We should watch the Ratio of Bullish / Bearish.So, this thing wouldn't happen. In real life, I have already rebalanced it much earlier.
                        // However, the current solution works that it adds only to the Winning side. It adds only to the smaller position.
                        // This is kind of OK. But it means everytime we do this RatioBuPerBe rebalancing, we increase by 10-20% (never decrease) the total leverage,.
                        // So, in about 5 RatioRebalancing, we reach Leverage of 2.0 from 1.0, which means we will do the tooHighPortfolioLeverage rebalancing.
                        // Therefore, it would be nice to not do this ratioRebalancing too frequently => instead of 20% discrepancy, try 30% ratio difference.
                        double ratioBullishPerBearish = Math.Abs(bullishEtfPosition / bearishEtfPosition);
                        if (ratioBullishPerBearish < ratioTooLow)       // bullishEtfPosition is too small, increase it
                        {
                            nRatioUnbalanceShortMoreWinning++;
                            // try to increase buEtfLeverage
                            double positionIncrement = -1 * (Math.Min(bearishEtfPosition, -0.5 * pvDaily) - bullishEtfPosition); // bearishEtfPosition, bullishEtfPosition are negative
                            bullishEtfPosition -= positionIncrement;
                            cash += positionIncrement;
                        } else if (ratioBullishPerBearish > ratioTooHigh)  // bearishEtfPosition is too small, increase it
                        {
                            nRatioUnbalanceShortMoreWinning++;
                            // try to increase buEtfLeverage
                            double positionIncrement = -1 * (Math.Min(bullishEtfPosition, -0.5 * pvDaily) - bearishEtfPosition); // bearishEtfPosition, bullishEtfPosition are negative
                            bearishEtfPosition -= positionIncrement;
                            cash += positionIncrement;
                        }


                        double buEtfLeverage = Math.Abs(bullishEtfPosition) / pvDaily;
                        if (buEtfLeverage <= tooLowStockLeverage)    // if we are under-leveraged -> short more of the winning side (Trend Following)
                        {
                            nUnderLeveragedShortMoreWinning++;
                            // try to increase buEtfLeverage
                            double positionIncrement = -1 * (Math.Min(bearishEtfPosition, -0.5* pvDaily) - bullishEtfPosition); // bearishEtfPosition, bullishEtfPosition are negative
                            bullishEtfPosition -= positionIncrement;
                            cash += positionIncrement;
                        }
                        else
                        {
                            double beEtfLeverage = Math.Abs(bearishEtfPosition) / pvDaily;
                            if (beEtfLeverage <= tooLowStockLeverage)    // if we are under-leveraged -> short more of the winning side (Trend Following)
                            {
                                nUnderLeveragedShortMoreWinning++;
                                // try to increase buEtfLeverage
                                double positionIncrement = -1 * (Math.Min(bullishEtfPosition, -0.5* pvDaily) - bearishEtfPosition); // bearishEtfPosition, bullishEtfPosition are negative
                                bearishEtfPosition -= positionIncrement;
                                cash += positionIncrement;
                            }
                            else
                                nOkLeveragedDoNothing++;
                        }
                    }
                }
                pv.Add(new DailyData() { Date = quotes1[iBullish + i].Date, AdjClosePrice = pvDaily });
                sbDebugToUser.AppendLine($"{pv[i].Date}, {pvDaily}, {Math.Abs(bullishEtfPosition) / pvDaily}, {Math.Abs(bearishEtfPosition) / pvDaily}, {Math.Abs(bullishEtfPosition + bearishEtfPosition) / pvDaily}, {Math.Abs(bullishEtfPosition / bearishEtfPosition)}<br>");
            }   // march for all days

            if (p_pv != null)
                p_pv = pv;

            p_noteToUser = p_noteToUser + ". nRatioUnbalanceShortMoreWinning: " + nRatioUnbalanceShortMoreWinning + ", nUnderLeveragedShortMoreWinning: " + nUnderLeveragedShortMoreWinning + ",nOkLeveragedDoNothing: " + nOkLeveragedDoNothing + ",nOverLeveragedRebalanceToNavAndNeutral: " + nOverLeveragedRebalanceToNavAndNeutral;
            p_noteToUser = p_noteToUser + "<br>" + sbDebugToUser.ToString();
        }


    }
}
