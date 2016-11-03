using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    public partial class LEtfDistcrepancy
    {
        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, string p_params)
        {
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();

            if (p_strategyName != "LETFDiscrepancy1" && p_strategyName != "LETFDiscrepancy2" && p_strategyName != "LETFDiscrepancy3")
                return null;

            string strategyParams = p_params;
            int ind = -1;

            string etfPairs = null;
            if (strategyParams.StartsWith("ETFPairs=", StringComparison.CurrentCultureIgnoreCase))
            {
                strategyParams = strategyParams.Substring("ETFPairs=".Length);
                ind = strategyParams.IndexOf('&');
                if (ind == -1)
                {
                    return @"{ ""errorMessage"":  ""Error: uriQuery.IndexOf('&') 2. Uri: " + strategyParams + @""" }";
                }
                etfPairs = strategyParams.Substring(0, ind);
                strategyParams = strategyParams.Substring(ind + 1);
            }

            string rebalancingFrequency = null;
            if (strategyParams.StartsWith("rebalancingFrequency=", StringComparison.CurrentCultureIgnoreCase))
            {
                strategyParams = strategyParams.Substring("rebalancingFrequency=".Length);
                ind = strategyParams.IndexOf('&');
                if (ind == -1)
                {
                    ind = strategyParams.Length;
                }
                rebalancingFrequency = strategyParams.Substring(0, ind);
                if (ind < strategyParams.Length)
                    strategyParams = strategyParams.Substring(ind + 1);
                else
                    strategyParams = "";
            }

            ind = etfPairs.IndexOf('-');
            if (ind == -1)
            {
                return @"{ ""errorMessage"":  ""Error: cannot find tickers in : " + etfPairs + @""" }";
            }

            string bullishTicker = etfPairs.Substring(0, ind);
            string bearishTicker = etfPairs.Substring(ind + 1);

            // startDates
            // URE: Feb 2, 2007
            // SRS: Feb 1, 2007
            // XIV: Nov 30, 2010
            // VXX: Jan 30, 2009
            // FAS: Nov 19, 2008
            // FAZ: Nov 19, 2008
            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams, (new string[] { bullishTicker, bearishTicker }).ToList());
            var getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();


            string htmlNoteFromStrategy = "", noteToUserCheckData = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";

            List<DailyData> pv = null;
            if (String.Equals(p_strategyName, "LETFDiscrepancy1", StringComparison.CurrentCultureIgnoreCase))
            {
                pv = DoBacktestExample(getAllQuotesData.Item1, bullishTicker, bearishTicker, rebalancingFrequency);
            }
            else
            {
                pv = DoBacktestBasic(getAllQuotesData.Item1, bullishTicker, bearishTicker, p_strategyName, rebalancingFrequency, ref noteToUserCheckData, ref htmlNoteFromStrategy);
            }


            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
                htmlNoteFromStrategy + ". " + noteToUserCheckData + "***" + noteToUserBacktest, errorMessage,
                debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
            //{
            //  "Name": "Apple",
            //  "Expiry": "2008-12-28T00:00:00",
            //  "Sizes": [
            //    "Small"
            //  ]
            //}

            //returnStr = "[" + String.Join(Environment.NewLine,
            //    (await Tools.GetHistoricalQuotesAsync(new[] {
            //        new QuoteRequest { Ticker = "VXX", nQuotes = 2, StartDate = new DateTime(2011,1,1), NonAdjusted = true },
            //        new QuoteRequest { Ticker = "SPY", nQuotes = 3 }
            //    }, HQCommon.AssetType.Stock))
            //    .Select(row => String.Join(",", row))) + "]";

            //returnStr = returnStr.Replace(" 00:00:00", "");
            //returnStr = returnStr.Replace("\n", ",");

            //return @"[{""Symbol"":""VXX""},{""Symbol"":""^VIX"",""LastUtc"":""2015-01-08T19:25:48"",""Last"":17.45,""UtcTimeType"":""LastChangedTime""}]";
        }


        static List<DailyData> DoBacktestExample(IList<List<DailyData>> p_allQuotes, string p_bullishTicker, string p_bearishTicker, string p_rebalancingFrequency)
        {
            var pv = p_allQuotes[0];
            return pv;
        }

        static List<DailyData> DoBacktestBasic(IList<List<DailyData>> p_allQuotes, string p_bullishTicker, string p_bearishTicker, string p_strategyName, string p_rebalancingFrequency, ref string p_noteToUserCheckData, ref string p_htmlNoteFromStrategy)
        {
            var bullishQoutes = p_allQuotes[0];    // URE, XIV, FAS, ZIV
            var bearishQoutes = p_allQuotes[1];    // SRS, VXX, FAZ, VXZ

            List<DailyData> pv = StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(bullishQoutes, bearishQoutes, p_bullishTicker, p_bearishTicker, ref p_noteToUserCheckData);


            if (String.Equals(p_strategyName, "LETFDiscrepancy2", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_RebalanceToNeutral(bullishQoutes, bearishQoutes, p_rebalancingFrequency, pv, ref p_htmlNoteFromStrategy);
            }
            else if (String.Equals(p_strategyName, "LETFDiscrepancy3", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(bullishQoutes, bearishQoutes, p_rebalancingFrequency, pv, ref p_htmlNoteFromStrategy);
            }
            else
            {

            }

            return pv;
        }

        // every 5-20 days, rebalance it to market neutral;
        private static void DoBacktestInTheTimeInterval_RebalanceToNeutral(List<DailyData> bullishQuotes, List<DailyData> bearishQuotes, string rebalancingFrequencyStr, List<DailyData> pv, ref string p_htmlNoteFromStrategy)
        {
            p_htmlNoteFromStrategy = "Rebalances to be market neutral with the specified frequencies.";
            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = bullishQuotes.FindIndex(row => row.Date == pvStartDate);
            int iBearish = bearishQuotes.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            pv[0].ClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            int rebalancingTradingDays;
            if (!Int32.TryParse(rebalancingFrequencyStr.TrimEnd(new char[] { 'd', 'D' }), out rebalancingTradingDays))
                rebalancingTradingDays = Int32.MaxValue;        //So we don't rebalance


            for (int i = 1; i < pv.Count(); i++)
            {
                double buEtfChg = bullishQuotes[iBullish + i].ClosePrice / bullishQuotes[iBullish + i - 1].ClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = bearishQuotes[iBearish + i].ClosePrice / bearishQuotes[iBearish + i - 1].ClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;
                if (i % rebalancingTradingDays == 0)    // every periodic days
                {
                    bullishEtfPosition = pvDaily * -0.5;
                    bearishEtfPosition = pvDaily * -0.5;

                    cash = pvDaily * 2.0;
                }
                pv[i].ClosePrice = pvDaily;
            }
        }


        //- the previous strategy: rebalance every 20 days: 13.18%CAGR. rebalancing 10day: CAGR: 14.79% .not much. And I have to pay the borrowing fee.
        //So I didn't gain money this way:
        //+ I didn't rebalance to market Neutral, but I did a market bet.... shorting more money into the one that went down, and keeping
        //the overleverage of the other side.
        //Test this: every X, 20 days. Keep the other leg. Short more from the etf that is down. 
        //So, I follow the trend: put more money into the right place. That is how I played.
        private static void DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(List<DailyData> bullishQuotes, List<DailyData> bearishQuotes, string rebalancingFrequencyStr, List<DailyData> pv, ref string p_htmlNoteFromStrategy)
        {
            p_htmlNoteFromStrategy = "Rebalances with the specified frequencies. But AddToTheWinningSideWithLeverage.";
            StringBuilder sbDebugToUser = new StringBuilder("Date, PVDaily, BullishLeverage, BearishLeverage, Leverage, RatioBullishPerBearish<br>");
            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = bullishQuotes.FindIndex(row => row.Date == pvStartDate);
            int iBearish = bearishQuotes.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            pv[0].ClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            int rebalancingTradingDays;
            if (!Int32.TryParse(rebalancingFrequencyStr.TrimEnd(new char[] { 'd', 'D' }), out rebalancingTradingDays))
                rebalancingTradingDays = Int32.MaxValue;        //So we don't rebalance

            // usually it is 50%=0.5, when it goes under 47%, short more of this side.
            double tooLowStockLeverage = 0.47;     
            double tooHighPortfolioLeverage = 2.0;      // we can play double leverage, because it is quite balanced LongShort, so not risky
            double ratioTooLow = 0.77;  // 1/1.3=0.77
            double ratioTooHigh = 1.3;

            int nRatioUnbalanceShortMoreWinning = 0;
            int nUnderLeveragedShortMoreWinning = 0;
            int nOkLeveragedDoNothing = 0;
            int nOverLeveragedRebalanceToNavAndNeutral = 0;
            for (int i = 1; i < pv.Count(); i++)
            {
                double buEtfChg = bullishQuotes[iBullish + i].ClosePrice / bullishQuotes[iBullish + i - 1].ClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = bearishQuotes[iBearish + i].ClosePrice / bearishQuotes[iBearish + i - 1].ClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;

                if (i % rebalancingTradingDays == 0)    // every periodic days
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
                pv[i].ClosePrice = pvDaily;
                sbDebugToUser.AppendLine($"{pv[i].Date}, {pvDaily}, {Math.Abs(bullishEtfPosition) / pvDaily}, {Math.Abs(bearishEtfPosition) / pvDaily}, {Math.Abs(bullishEtfPosition + bearishEtfPosition) / pvDaily}, {Math.Abs(bullishEtfPosition / bearishEtfPosition)}<br>");
            }   // for

            p_htmlNoteFromStrategy = p_htmlNoteFromStrategy + ". nRatioUnbalanceShortMoreWinning: " + nRatioUnbalanceShortMoreWinning + ", nUnderLeveragedShortMoreWinning: " + nUnderLeveragedShortMoreWinning + ",nOkLeveragedDoNothing: " + nOkLeveragedDoNothing + ",nOverLeveragedRebalanceToNavAndNeutral: " + nOverLeveragedRebalanceToNavAndNeutral;
            p_htmlNoteFromStrategy = p_htmlNoteFromStrategy + "<br>" + sbDebugToUser.ToString();
        }


    }
}
