using DbCommon;
using IBApi;
using SqCommon;
using SQCommon.MathNet;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    public enum RebalancingPeriodicity { Daily, Weekly, Monthly };

    enum DebugDetailToHtml { Date, PV, AssetFinalWeights, CashWeight, AssetData, PctChannels };

    // Portfolio specific parameters are here. User1's portfolio1 may use double leverage than User2's portfolio2. The Common Strategy params should go to StrategyConfig.cs
    public class PortfolioParamTAA : IPortfolioParam
    {
        //public double PlayingInstrumentTicker1Leverage { get; set; }
        //public double PlayingInstrumentTicker2Leverage { get; set; }
    }

    public partial class TAAStrategy : IBrokerStrategy
    {
        StringBuilder m_detailedReportSb;

        Task<Dictionary<string, Tuple<IAssetID, string>>> m_loadAssetIdTask = null;
        Dictionary<string, Tuple<IAssetID, string>> m_tickerToAssetId = null;   // ticker => AssetID : FullTicker mapping

        public bool IsSameStrategyForAllUsers { get; set; } = true;

        TAAConfig taaConfig = new TAAConfig();
        List<List<DailyData>> m_quotes = null;
        List<DailyData> m_cashEquivalentQuotes = null;
        double[] m_lastWeights = null;

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new TAAStrategy();
        }

        public void Init(StringBuilder p_detailedReportSb)
        {
            m_detailedReportSb = p_detailedReportSb;
            List<string> allTickers = taaConfig.Tickers.ToList();
            allTickers.Add(taaConfig.CashEquivalentTicker);
            allTickers.AddRange(taaConfig.Traded2xTickers);
            allTickers.AddRange(taaConfig.Traded3xTickers);
            var distinctTickers = allTickers.GroupBy(r => r).Select(r => r.First()).ToList();  // select distinct, because they are tickers represented many times.
            m_loadAssetIdTask = Task.Run(() => DbCommon.SqlTools.LoadAssetIdsForTickers(distinctTickers));   // task will start to run on another thread (in the threadpool)
        }

        public string StockIdToTicker(int p_stockID)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId.First(r => r.Value.Item1.ID == p_stockID).Key;
        }

        public IAssetID TickerToAssetID(string p_ticker)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId[p_ticker].Item1;
        }

        public double GetPortfolioLeverage(List<PortfolioPositionSpec> p_suggestedPortfItems, IPortfolioParam p_param)
        {
            return 1.0;
        }

        public List<PortfolioPositionSpec> GeneratePositionSpecs()
        {
            Utils.Logger.Info("TAAStrategy.GeneratePositionSpecs() Begin.");
            // the asset.IsActive depends not only of the recent 150 days, but maybe all about its previous history.
            // imagine an asset that was active at its start 10 years ago. Then in year 9, it dipped under Lower %Channel, so it become inactive. 
            // Imagine that the price is constant since (last 8 years), or the price is wiggling in its channel, but never breaking the UpperChannel. 
            // In that case, the asset is still Inactive. But if we don't consider that event 9 years ago, we will never know it is inactive.
            // Conclusion: get to know that an asset is active or not without doubt, we have to simulate a lot of history. Actually, all of it.



            if(!GetHistoricalAndRealTimeDataForAllParts())
            {
                throw new Exception("TAAStrategy.GeneratePositionSpecs() GetHistoricalAndRealTimeDataForAllParts() Error. We don't continue, because we don't want to liquidate the portofolio by returning an empty List<PortfolioPositionSpec>. We crash here.");
            }

            if (!CalculateAssetAndCashWeights())
            {
                throw new Exception("TAAStrategy.GeneratePositionSpecs() CalculateAssetAndCashWeights() Error. We don't continue, because we don't want to liquidate the portofolio by returning an empty List<PortfolioPositionSpec>. We crash here.");
            }


            StringBuilder consoleMsgSb = new StringBuilder("Target: ");
            List<PortfolioPositionSpec> specs = new List<PortfolioPositionSpec>();
            for (int i = 0; i < taaConfig.Tickers.Length; i++)
            {
                string ticker = taaConfig.Tickers[i];
                string tradedTicker = taaConfig.Traded2xTickers[i];
                double weight = m_lastWeights[i];
                double tradedWeight = weight * taaConfig.TradedLeverages[i];

                if (Math.Abs(weight) > 0.0000001)
                    consoleMsgSb.Append($"{ticker}({tradedTicker}):{ weight * 100:F2}%({ tradedWeight * 100:F2}%), ");

                if (weight > 0.0000001)  // if Weight is 0,  the target position is 0.
                    specs.Add(new PortfolioPositionSpec() { Ticker = tradedTicker, PositionType = PositionType.Long, Size = WeightedSize.Create(Math.Abs(weight)) });
                else if (weight < -0.0000001)
                    specs.Add(new PortfolioPositionSpec() { Ticker = tradedTicker, PositionType = PositionType.Short, Size = WeightedSize.Create(Math.Abs(weight)) });
            }

            double cashWeight = m_lastWeights.Last();
            consoleMsgSb.Append($"Cash({taaConfig.CashEquivalentTicker}):{ cashWeight * 100:F2}%,");
            if (cashWeight > 0.0000001)  // if Weight is 0,  the target position is 0.
                specs.Add(new PortfolioPositionSpec() { Ticker = taaConfig.CashEquivalentTicker, PositionType = PositionType.Long, Size = WeightedSize.Create(Math.Abs(cashWeight)) });
            else if (cashWeight < -0.0000001)
                specs.Add(new PortfolioPositionSpec() { Ticker = taaConfig.CashEquivalentTicker, PositionType = PositionType.Short, Size = WeightedSize.Create(Math.Abs(cashWeight)) });

            string consoleMsg = consoleMsgSb.ToString();
            Utils.ConsoleWriteLine(ConsoleColor.Green, false, consoleMsg);
            Utils.Logger.Info(consoleMsg);
            m_detailedReportSb.AppendLine($"<font color=\"#10ff10\">{consoleMsg}</font>");

            return specs;
        }

        

        private bool GetHistoricalAndRealTimeDataForAllParts()
        {
            // IB.ReqHistoricalData() only gives back data for the last year. We need more than that, so we have to use SQL DB
            // CommonAssetStartDate = VNQ startdate: 2004-09-29, others started earlier. (TLT: 2002-07-30). Helpful so we don't download unnecessary data from SQLdb
            List<string> allTickers = taaConfig.Tickers.ToList();
            allTickers.Add(taaConfig.CashEquivalentTicker);

            List<List<DailyData>> allQuotes = StrategiesCommon.GetHistoricalAndRealTimeDataForAllParts(allTickers, taaConfig.CommonAssetStartDate, Int32.MaxValue);
            if (allQuotes == null)
            {
                m_quotes = null;
                m_cashEquivalentQuotes = null;
                return false;
            }

            m_quotes = allQuotes.GetRange(0, taaConfig.Tickers.Length);
            m_cashEquivalentQuotes = allQuotes[taaConfig.Tickers.Length];
            return true;
        }

        private bool CalculateAssetAndCashWeights()
        {
            m_lastWeights = new double[taaConfig.Tickers.Length + 1];
            List<DailyData> pv = null;
            string warningToUser = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";
            DoBacktestInTheTimeInterval_TAA(m_quotes, taaConfig.Tickers, m_quotes[0][0].Date, m_quotes[0].Last().Date, taaConfig.AssetsConstantLeverages,
                taaConfig.RebalancingPeriodicity, taaConfig.DailyRebalancingDays, taaConfig.WeeklyRebalancingWeekDay, taaConfig.MonthlyRebalancingOffset,
                taaConfig.PctChannelLookbackDays, taaConfig.PctChannelPctLimitLower, taaConfig.PctChannelPctLimitUpper,
                taaConfig.IsPctChannelActiveEveryDay, false, taaConfig.HistVolLookbackDays, taaConfig.IsCashAllocatedForNonActives,
                m_cashEquivalentQuotes, 
                new Dictionary<DebugDetailToHtml, bool> {   { DebugDetailToHtml.Date, true }, { DebugDetailToHtml.PV, true }, { DebugDetailToHtml.AssetFinalWeights, true },
                                                            { DebugDetailToHtml.CashWeight, true }, { DebugDetailToHtml.AssetData, true }, { DebugDetailToHtml.PctChannels, true } }, 
                6 /* calendar days, so consider long weekends as extra 2-3 days */, Environment.NewLine,
                ref warningToUser, ref noteToUserBacktest, ref errorMessage, ref debugMessage, ref pv, m_lastWeights);

            Utils.Logger.Debug(noteToUserBacktest);
            return true;
        }


        // copied from BackTester. Try to keep the code absolutely the same. Later, we may put them into a common DLL, but it is very rare that VirtualBroker has to do a whole historicalBacktesting run.
        private static void DoBacktestInTheTimeInterval_TAA(IList<List<DailyData>> p_quotes, string[] p_tickers, DateTime p_commonAssetStartDate, DateTime p_commonAssetEndDate, double[] p_assetsConstantLeverages,
              RebalancingPeriodicity p_rebalancingPeriodicity, int p_dailyRebalancingDays, DayOfWeek p_weeklyRebalancingWeekDay, int p_monthlyRebalancingOffset,
              int[] p_pctChannelLookbackDays, double p_pctChannelPctLimitLower, double p_pctChannelPctLimitUpper, bool p_isPctChannelActiveEveryDay, bool p_isPctChannelConditional,
              int p_histVolLookbackDays,
              bool p_isCashAllocatedForNonActives, List<DailyData> p_cashEquivalentQuotes,
              Dictionary<DebugDetailToHtml, bool> p_debugDetailToHtml, int p_nCalendarDaysToDebugDetailToHtml, string p_noteToUserNewLine,
              ref string p_noteToUserCheckData, ref string p_noteToUserBacktest, ref string errorMessage, ref string debugMessage, ref List<DailyData> p_pv, double[] p_lastWeights)
        {
            StringBuilder noteToUser = new StringBuilder("DoBacktestInTheTimeInterval_TAA()");
            DateTime nowDate = DateTime.UtcNow.Date;
            DateTime debugDetailToHtmlMinDate = DateTime.MinValue;
            if (p_nCalendarDaysToDebugDetailToHtml < (nowDate - DateTime.MinValue).TotalDays)
                debugDetailToHtmlMinDate = nowDate.AddDays(-1 * p_nCalendarDaysToDebugDetailToHtml);
            List<DailyData> pv = null;
            // implement CLMT in a way, that those data days don't restrict Strategy StartDate. If they are not available on a day, simple 100% is used. CLMT: "SMA(SPX,50d,200d); PR(XLU,VTI,20d)"
            // 1. Determine commonAssetStartDate
            int commonAssetStartDateInd = p_quotes[0].FindIndex(r => r.Date >= p_commonAssetStartDate);
            int commonAssetEndDateInd = p_quotes[0].FindIndex(commonAssetStartDateInd, r => r.Date >= p_commonAssetEndDate);

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
                errorMessage = "StartDate cannot be determined";
                return;
            }
            firstRebalancingDate = p_quotes[0][firstRebalancingDateInd].Date;


            // 4. pvStartDate is now final, calculate the index of that startDate for each asset quotes
            DateTime pvStartDate = firstRebalancingDate;
            DateTime pvEndDate = p_commonAssetEndDate;
            int nDays = commonAssetEndDateInd - firstRebalancingDateInd + 1;        // startDate, endDate is included
            int nAssets = p_quotes.Count;
            int[] iQ = new int[nAssets];
            for (int i = 0; i < nAssets; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }
            int iCashSubst = (p_cashEquivalentQuotes == null) ? -1 : p_cashEquivalentQuotes.FindIndex(r => r.Date >= pvStartDate);


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

            for (int iAsset = 0; iAsset < nAssets; iAsset++)
            {
                assetPos[iAsset] = 0.0;
                for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                {
                    assetPctChannelsSignal[iAsset, iChannel] = 1;       // let all assets be active at the beginning (if they are not under 25% percentile)
                }
            }

            noteToUser.AppendLine("Date, pvDaily, {assetPrice, assetPctChange, {PctChannels(Lower,Upper)}, {PctChannels(Signal)}, assetScore, assetHV, assetWeights, assetWeightsBasedOnPV}... ,cashWeight <br />");
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

                    if (p_cashEquivalentQuotes != null)
                    {
                        double cashChg = p_cashEquivalentQuotes[iCashSubst + iDay].AdjClosePrice / p_cashEquivalentQuotes[iCashSubst + iDay - 1].AdjClosePrice;
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
                if (p_isPctChannelActiveEveryDay || isRebalanceDay)
                {
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        //if ((p_quotes[0][firstRebalancingDateInd + iDay].Date == new DateTime(2016, 11, 15) && (p_tickers[iAsset] == "TSLA")))
                        //{
                        //    int tempBlaBla = 0;
                        //}
                        double assetPrice = p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice;
                        for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                        {
                            // A long position would be initiated if the price exceeds the 75th percentile of prices over the last “n” days.The position would be closed if the price falls below the 25th percentile of prices over the last “n” days.
                            var usedQuotes = p_quotes[iAsset].GetRange(iQ[iAsset] + iDay - (p_pctChannelLookbackDays[iChannel] - 1), p_pctChannelLookbackDays[iChannel]).Select(r => r.AdjClosePrice);
                            assetPctChannelsLower[iAsset, iChannel] = Statistics.Quantile(usedQuotes, p_pctChannelPctLimitLower);
                            assetPctChannelsUpper[iAsset, iChannel] = Statistics.Quantile(usedQuotes, p_pctChannelPctLimitUpper);
                            if (assetPrice < assetPctChannelsLower[iAsset, iChannel])
                                assetPctChannelsSignal[iAsset, iChannel] = -1;
                            else if (assetPrice > assetPctChannelsUpper[iAsset, iChannel])
                                assetPctChannelsSignal[iAsset, iChannel] = 1;
                        }
                    }
                }

                // 3. At the end of the day, allocate assetPos[]. This will not change PV.
                if (isRebalanceDay)
                {
                    // https://docs.google.com/document/d/1kx3_UuYy_RApp6s0KmO2b4pbwQdClMuzjf6EyJynghs/edit   Clarification of the rules
                    // 3.1 Calculate assetWeights
                    double totalWeight = 0.0;
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        //if ((p_quotes[0][firstRebalancingDateInd + iDay].Date == new DateTime(2016, 11, 15) && (p_tickers[iAsset] == "TSLA")))
                        //{
                        //    int tempBlaBla = 0;
                        //}
                        sbyte compositeSignal = 0;    // For every stocks, sum up the four signals every day. This sum will be -4, -2, 0, +2 or +4.
                        for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                        {
                            compositeSignal += assetPctChannelsSignal[iAsset, iChannel];
                        }
                        assetScores[iAsset] = compositeSignal / 4.0;    // Divide it by 4 to get a signal between -1 and +1 (this will be the “score”).

                        double[] hvPctChg = new double[p_histVolLookbackDays];
                        for (int iHv = 0; iHv < p_histVolLookbackDays; iHv++)
                        {
                            hvPctChg[p_histVolLookbackDays - iHv - 1] = p_quotes[iAsset][iQ[iAsset] + iDay - iHv].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - iHv - 1].AdjClosePrice - 1;
                        }
                        // Balazs: uses "corrected sample standard deviation"; corrected: dividing by 19, not 20; He doesn't annualize. He uses daily StDev
                        assetHV[iAsset] = ArrayStatistics.StandardDeviation(hvPctChg);  // Calculate the 20-day historical volatility of daily percentage changes for every stock.
                        assetWeights[iAsset] = assetScores[iAsset] / assetHV[iAsset];   // “Score/Vol” quotients will define the weights of the stocks. They can be negative as well. 
                        if (p_isCashAllocatedForNonActives)    // in the original Varadi's strategy
                            totalWeight += Math.Abs(assetWeights[iAsset]);      // Sum up the absolute values of the “Score/Vol” quotients. TotalWeight contains even the non-active assets so have have some cash.
                        else if (assetWeights[iAsset] > 0)      // otherwise all the capital is allocated between active assets. No cash is maintained.
                            totalWeight += assetWeights[iAsset];

                    }
                    // 3.2 With assetWeights calculated, do the rebalancing of assetPos[]
                    cash = pvDaily; // at rebalancing, we simulate that we sell assets, so everything is converted to Cash 1 seconds before MarketClose
                    for (int iAsset = 0; iAsset < nAssets; iAsset++)
                    {
                        double weight = (assetWeights[iAsset] > 0) ? assetWeights[iAsset] / totalWeight : 0.0;  // If the score of a stock is positive, this ratio is the weight of the given stock. Otherwise omit from portfolio.
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
                            double assetChg = p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice / p_quotes[iAsset][iQ[iAsset] + iDay - 1].AdjClosePrice - 1;
                            noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"${p_quotes[iAsset][iQ[iAsset] + iDay].AdjClosePrice:F2}, {assetChg * 100.0:F2}%";
                            wasAnyNoteToUser = true;
                        }

                        if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.PctChannels))
                        {
                            for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                            {
                                noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{assetPctChannelsLower[iAsset, iChannel]:F3}";
                                noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{assetPctChannelsUpper[iAsset, iChannel]:F3}";
                                noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{assetPctChannelsSignal[iAsset, iChannel]}";
                            }
                            wasAnyNoteToUser = true;
                        }

                        if (p_debugDetailToHtml.ContainsKey(DebugDetailToHtml.AssetData))
                        {
                            noteToUserRow += ((wasAnyNoteToUser) ? ", " : String.Empty) + $"{assetScores[iAsset]},{assetHV[iAsset]:F6}, {assetWeights[iAsset]:F3}";
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
                        noteToUser.Append(noteToUserRow + p_noteToUserNewLine);
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

            p_noteToUserBacktest = noteToUser.ToString();
            //noteToUserBacktest = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        } // DoBacktestInTheTimeInterval_TAA


    }
}
