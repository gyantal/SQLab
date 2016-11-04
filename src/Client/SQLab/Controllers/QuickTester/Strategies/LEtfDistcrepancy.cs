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

            if (p_strategyName != "LETFDiscrepancy1" && p_strategyName != "LETFDiscrepancy2" && p_strategyName != "LETFDiscrepancy3" && p_strategyName != "LETFDiscrepancy4")
                return null;

            string strategyParams = p_params;
            string etfPairs = ExtractParam("ETFPairs", ref strategyParams);
            string rebalancingFrequency = ExtractParam("RebalancingFrequency", ref strategyParams);
            string etf1 = ExtractParam("ETF1", ref strategyParams);
            string weightStr1 = ExtractParam("Weight1", ref strategyParams);
            double weight1 = Double.NaN;
            if (!Double.TryParse(weightStr1, out weight1))
                return @"{ ""errorMessage"":  ""Error: Weight1 cannot be converted to Number : " + weightStr1 + @""" }";
            string etf2 = ExtractParam("ETF2", ref strategyParams);
            string weightStr2 = ExtractParam("Weight2", ref strategyParams);
            double weight2 = Double.NaN;
            if (!Double.TryParse(weightStr2, out weight2))
                return @"{ ""errorMessage"":  ""Error: Weight2 cannot be converted to Number : " + weightStr2 + @""" }";

            int ind = etfPairs.IndexOf('-');
            if (ind == -1)
            {
                return @"{ ""errorMessage"":  ""Error: cannot find tickers in : " + etfPairs + @""" }";
            }
            string etfPairs1 = etfPairs.Substring(0, ind);
            string etfPairs2 = etfPairs.Substring(ind + 1);

            string ticker1 = null, ticker2 = null;
            if (p_strategyName == "LETFDiscrepancy4")       // in HarryLong strategy use these as tickers
            {
                ticker1 = etf1;
                ticker2 = etf2;
            }else
            {
                ticker1 = etfPairs1;
                ticker2 = etfPairs2;
            }

            int rebalancingTradingDays;
            if (!Int32.TryParse(rebalancingFrequency.TrimEnd(new char[] { 'd', 'D' }), out rebalancingTradingDays))
                rebalancingTradingDays = Int32.MaxValue;        //So we don't rebalance

            // startDates
            // URE: Feb 2, 2007
            // SRS: Feb 1, 2007
            // XIV: Nov 30, 2010
            // VXX: Jan 30, 2009
            // FAS: Nov 19, 2008
            // FAZ: Nov 19, 2008
            List<string> tickers = new List<string>();
            if (!ticker1.Equals("Cash"))
                tickers.Add(ticker1);
            if (!ticker2.Equals("Cash"))
                tickers.Add(ticker2);
            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams, tickers);
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();

            IList<List<DailyData>> quotes12 = getAllQuotesData.Item1;
            List<DailyData> quotes1 = null, quotes2 = null;
            if (!ticker1.Equals("Cash"))
            {
                quotes1 = quotes12[0];
                quotes12.RemoveAt(0);
            }
            if (!ticker2.Equals("Cash"))
            {
                quotes2 = quotes12[0];
                quotes12.RemoveAt(0);
            }

            if (quotes1 == null)    // it is Cash, set it according to the other, but use cash
                quotes1 = quotes2.Select(item => new DailyData() { Date = item.Date, ClosePrice = 100.0 }).ToList();
            if (quotes2 == null)    // it is Cash, set it according to the other, but use cash
                quotes2 = quotes1.Select(item => new DailyData() { Date = item.Date, ClosePrice = 100.0 }).ToList();

            string htmlNoteFromStrategy = "", noteToUserCheckData = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";

            List<DailyData> pv = null;
            if (String.Equals(p_strategyName, "LETFDiscrepancy1", StringComparison.CurrentCultureIgnoreCase))
            {
                pv = getAllQuotesData.Item1[0]; // only testing: PV = First of the ETF pair
            }
            else
            {
                pv = DoBacktest(p_strategyName, quotes1, quotes2, ticker1, ticker2, weight1 / 100.0, weight2 / 100.0, rebalancingTradingDays, ref noteToUserCheckData, ref htmlNoteFromStrategy);
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

        private static string ExtractParam(string p_paramName, ref string p_strategyParams)
        {
            int ind = -1;
            string paramStr = null;
            if (p_strategyParams.StartsWith(p_paramName + "=", StringComparison.CurrentCultureIgnoreCase))
            {
                p_strategyParams = p_strategyParams.Substring((p_paramName +"=").Length);
                ind = p_strategyParams.IndexOf('&');
                if (ind == -1)
                {
                    ind = p_strategyParams.Length;
                }
                paramStr = p_strategyParams.Substring(0, ind);
                if (ind < p_strategyParams.Length)
                    p_strategyParams = p_strategyParams.Substring(ind + 1);
                else
                    p_strategyParams = "";
            }

            return paramStr;
        }

        static List<DailyData> DoBacktest(string p_strategyName, List<DailyData> p_quotes1, List<DailyData> p_quotes2, string p_ticker1, string p_ticker2, double p_weight1, double p_weight2, int p_rebalancingTradingDays, ref string p_noteToUserCheckData, ref string p_htmlNoteFromStrategy)
        {
            List<DailyData> pv = StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(p_quotes1, p_quotes2, p_ticker1, p_ticker2, ref p_noteToUserCheckData);


            if (String.Equals(p_strategyName, "LETFDiscrepancy2", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_RebalanceToNeutral(p_quotes1, p_quotes2, p_rebalancingTradingDays, pv, ref p_htmlNoteFromStrategy);
            }
            else if (String.Equals(p_strategyName, "LETFDiscrepancy3", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(p_quotes1, p_quotes2, p_rebalancingTradingDays, pv, ref p_htmlNoteFromStrategy);
            }
            else if (String.Equals(p_strategyName, "LETFDiscrepancy4", StringComparison.CurrentCultureIgnoreCase))
            {
                DoBacktestInTheTimeInterval_HarryLong(p_quotes1, p_quotes2, p_weight1, p_weight2, p_rebalancingTradingDays, pv, ref p_htmlNoteFromStrategy);
            }
            else
            {

            }

            return pv;
        }

        // every 5-20 days, rebalance it to market neutral;
        private static void DoBacktestInTheTimeInterval_RebalanceToNeutral(List<DailyData> quotes1, List<DailyData> quotes2, int p_rebalancingTradingDays, List<DailyData> pv, ref string p_htmlNoteFromStrategy)
        {
            p_htmlNoteFromStrategy = "Rebalances to be market neutral at the specified frequencies.";
            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = quotes1.FindIndex(row => row.Date == pvStartDate);
            int iBearish = quotes2.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            pv[0].ClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            for (int i = 1; i < pv.Count(); i++)
            {
                double buEtfChg = quotes1[iBullish + i].ClosePrice / quotes1[iBullish + i - 1].ClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = quotes2[iBearish + i].ClosePrice / quotes2[iBearish + i - 1].ClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;
                if (i % p_rebalancingTradingDays == 0)    // every periodic days
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
        private static void DoBacktestInTheTimeInterval_AddToTheWinningSideWithLeverage(List<DailyData> quotes1, List<DailyData> quotes2, int p_rebalancingTradingDays, List<DailyData> pv, ref string p_htmlNoteFromStrategy)
        {
            p_htmlNoteFromStrategy = "Rebalances at the specified frequencies. But AddToTheWinningSideWithLeverage.";
            StringBuilder sbDebugToUser = new StringBuilder("Date, PVDaily, BullishLeverage, BearishLeverage, Leverage, RatioBullishPerBearish<br>");
            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iBullish = quotes1.FindIndex(row => row.Date == pvStartDate);
            int iBearish = quotes2.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double bullishEtfPosition = pvDaily * -0.5;  // on day0, we short 50% URE, 50% SRS
            double bearishEtfPosition = pvDaily * -0.5;
            double cash = pvDaily * 2.0;

            pv[0].ClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close
        
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
                double buEtfChg = quotes1[iBullish + i].ClosePrice / quotes1[iBullish + i - 1].ClosePrice;
                bullishEtfPosition = bullishEtfPosition * buEtfChg;

                double beEtfChg = quotes2[iBearish + i].ClosePrice / quotes2[iBearish + i - 1].ClosePrice;
                bearishEtfPosition = bearishEtfPosition * beEtfChg;

                pvDaily = cash + bullishEtfPosition + bearishEtfPosition;

                if (i % p_rebalancingTradingDays == 0)    // every periodic days
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


        private static void DoBacktestInTheTimeInterval_HarryLong(List<DailyData> quotes1, List<DailyData> quotes2, double p_weight1, double p_weight2, int p_rebalancingTradingDays, List<DailyData> pv, ref string p_htmlNoteFromStrategy)
        {
            p_htmlNoteFromStrategy = "Rebalances at the specified frequencies. But AddToTheWinningSideWithLeverage.";
            StringBuilder sbDebugToUser = new StringBuilder("Date, PVDaily, Etf1Weight, Etf2Weight, Leverage, RatioEtf1PerEtf2<br>");
            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            // note: bullishQuotes[0].Date, bearishQuotes[0].Date refers to different date. We have to find the StartDate in both.
            int iEtf1 = quotes1.FindIndex(row => row.Date == pvStartDate);
            int iEtf2 = quotes2.FindIndex(row => row.Date == pvStartDate);

            double pvDaily = 100.0;
            double etf1Position = pvDaily * p_weight1;  // on day0, we short -25% TVIX, -75% TMV, p_weight1 is negative if we short, positive if we long.
            double etf2Position = pvDaily * p_weight2;
            //double cash = pvDaily * 2.0;
            double cash = pvDaily - etf1Position - etf2Position;

            pv[0].ClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            //// usually it is 50%=0.5, when it goes under 47%, short more of this side.
            //double tooLowStockLeverage = 0.47;
            //double tooHighPortfolioLeverage = 2.0;      // we can play double leverage, because it is quite balanced LongShort, so not risky
            //double ratioTooLow = 0.77;  // 1/1.3=0.77
            //double ratioTooHigh = 1.3;

            //int nRatioUnbalanceShortMoreWinning = 0;
            //int nUnderLeveragedShortMoreWinning = 0;
            //int nOkLeveragedDoNothing = 0;
            //int nOverLeveragedRebalanceToNavAndNeutral = 0;
            for (int i = 1; i < pv.Count(); i++)
            {
                double etf1Chg = quotes1[iEtf1 + i].ClosePrice / quotes1[iEtf1 + i - 1].ClosePrice;
                etf1Position = etf1Position * etf1Chg;

                double etf2Chg = quotes2[iEtf2 + i].ClosePrice / quotes2[iEtf2 + i - 1].ClosePrice;
                etf2Position = etf2Position * etf2Chg;

                pvDaily = cash + etf1Position + etf2Position;

                if (i % p_rebalancingTradingDays == 0)    // every periodic days
                {
                    etf1Position = pvDaily * p_weight1; //Rebalance to NAV and to proposed weights.
                    etf2Position = pvDaily * p_weight2;

                    cash = pvDaily - etf1Position - etf2Position;

                    //double leverage = Math.Abs(etf1Position + etf2Position) / pvDaily;
                    //if (leverage >= tooHighPortfolioLeverage)    // if we are over-leveraged -> bad. Margin call risk. Rebalance to NAV and market neutral.
                    //{
                    //    nOverLeveragedRebalanceToNavAndNeutral++;
                    //    etf1Position = pvDaily * -0.5;
                    //    etf2Position = pvDaily * -0.5;

                    //    cash = pvDaily * 2.0;
                    //}
                    //else
                    //{
                    //    // bullishEtfLeverage = 44%, while bearishLeverage = 96%. Altogether = 140% leverage.
                    //    // it was allowed and BullishLeverage was not increased, because it was over 47%. Bad.
                    //    // We should watch the Ratio of Bullish / Bearish.So, this thing wouldn't happen. In real life, I have already rebalanced it much earlier.
                    //    // However, the current solution works that it adds only to the Winning side. It adds only to the smaller position.
                    //    // This is kind of OK. But it means everytime we do this RatioBuPerBe rebalancing, we increase by 10-20% (never decrease) the total leverage,.
                    //    // So, in about 5 RatioRebalancing, we reach Leverage of 2.0 from 1.0, which means we will do the tooHighPortfolioLeverage rebalancing.
                    //    // Therefore, it would be nice to not do this ratioRebalancing too frequently => instead of 20% discrepancy, try 30% ratio difference.
                    //    double ratioBullishPerBearish = Math.Abs(etf1Position / etf2Position);
                    //    if (ratioBullishPerBearish < ratioTooLow)       // bullishEtfPosition is too small, increase it
                    //    {
                    //        nRatioUnbalanceShortMoreWinning++;
                    //        // try to increase buEtfLeverage
                    //        double positionIncrement = -1 * (Math.Min(etf2Position, -0.5 * pvDaily) - etf1Position); // bearishEtfPosition, bullishEtfPosition are negative
                    //        etf1Position -= positionIncrement;
                    //        cash += positionIncrement;
                    //    }
                    //    else if (ratioBullishPerBearish > ratioTooHigh)  // bearishEtfPosition is too small, increase it
                    //    {
                    //        nRatioUnbalanceShortMoreWinning++;
                    //        // try to increase buEtfLeverage
                    //        double positionIncrement = -1 * (Math.Min(etf1Position, -0.5 * pvDaily) - etf2Position); // bearishEtfPosition, bullishEtfPosition are negative
                    //        etf2Position -= positionIncrement;
                    //        cash += positionIncrement;
                    //    }


                    //    double buEtfLeverage = Math.Abs(etf1Position) / pvDaily;
                    //    if (buEtfLeverage <= tooLowStockLeverage)    // if we are under-leveraged -> short more of the winning side (Trend Following)
                    //    {
                    //        nUnderLeveragedShortMoreWinning++;
                    //        // try to increase buEtfLeverage
                    //        double positionIncrement = -1 * (Math.Min(etf2Position, -0.5 * pvDaily) - etf1Position); // bearishEtfPosition, bullishEtfPosition are negative
                    //        etf1Position -= positionIncrement;
                    //        cash += positionIncrement;
                    //    }
                    //    else
                    //    {
                    //        double beEtfLeverage = Math.Abs(etf2Position) / pvDaily;
                    //        if (beEtfLeverage <= tooLowStockLeverage)    // if we are under-leveraged -> short more of the winning side (Trend Following)
                    //        {
                    //            nUnderLeveragedShortMoreWinning++;
                    //            // try to increase buEtfLeverage
                    //            double positionIncrement = -1 * (Math.Min(etf1Position, -0.5 * pvDaily) - etf2Position); // bearishEtfPosition, bullishEtfPosition are negative
                    //            etf2Position -= positionIncrement;
                    //            cash += positionIncrement;
                    //        }
                    //        else
                    //            nOkLeveragedDoNothing++;
                    //    }
                    //}
                }
                pv[i].ClosePrice = pvDaily;
                sbDebugToUser.AppendLine($"{pv[i].Date}, {pvDaily}, {Math.Abs(etf1Position) / pvDaily}, {Math.Abs(etf2Position) / pvDaily}, {Math.Abs(etf1Position + etf2Position) / pvDaily}, {Math.Abs(etf1Position / etf2Position)}<br>");
            }   // for

            //p_htmlNoteFromStrategy = p_htmlNoteFromStrategy + ". nRatioUnbalanceShortMoreWinning: " + nRatioUnbalanceShortMoreWinning + ", nUnderLeveragedShortMoreWinning: " + nUnderLeveragedShortMoreWinning + ",nOkLeveragedDoNothing: " + nOkLeveragedDoNothing + ",nOverLeveragedRebalanceToNavAndNeutral: " + nOverLeveragedRebalanceToNavAndNeutral;
            p_htmlNoteFromStrategy = p_htmlNoteFromStrategy + "<br>" + sbDebugToUser.ToString();
        }


    }
}
