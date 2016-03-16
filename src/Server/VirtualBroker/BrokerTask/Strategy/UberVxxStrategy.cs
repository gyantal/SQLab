using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    public class PortfolioParamUberVXX : IPortfolioParam     // Strategy specific parameters are here
    {
        public double PlayingInstrumentVixLongLeverage { get; set; }
        public double PlayingInstrumentVixShortLeverage { get; set; }
    }

    public class UberVxxStrategy : IBrokerStrategy
    {
        Task<Dictionary<string, Tuple<IAssetID, string>>> m_loadAssetIdTask = null;
        Dictionary<string, Tuple<IAssetID, string>> m_tickerToAssetId = null;   // ticker => AssetID : FullTicker mapping

        public bool IsSameStrategyForAllUsers { get; set; } = true;

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new UberVxxStrategy();
        }

        public void Init()
        {
            m_loadAssetIdTask = Task.Run(() => DbCommon.SqlTools.LoadAssetIdsForTickers(new List<string>() { "VXX", "SVXY" }));   // task will start to run on another thread (in the threadpool)
        }

        public string StockIdToTicker(int p_stockID)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId.First(r=> r.Value.Item1.ID == p_stockID).Key;
        }

        public IAssetID TickerToAssetID(string p_ticker)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId[p_ticker].Item1;
        }

        public List<PortfolioPositionSpec> GeneratePositionSpecs()
        {
            Utils.Logger.Info("UberVxxStrategy.GenerateSpecs() Begin.");
            int lookbackWindowSize = 102;

            List<QuoteData> m_vxx;      // contains today real-time price too
            double m_probDailyFT = 0.0;
            double m_probDailyFTGoodFtRegimeThreshold = 0.480001;

            // 1. Get VXX price data

            // we can get histical data from these sources and compare them
            // 1. IB: quickest, but 'Historical data request for greater than 365 days rejected.' and it is split adjusted, but if dividend is less than 10%, it is not adjusted.
            // 2. YahooFinance as CSV, but twice every year YahooFinance doesn't have the data, or one date is missing from the 200 values
            // 3. GoogleFinance as HttpDownload
            // 4. Our SQL server.
            //// we assume our SQL DB is correct, it is always alive; and has the correct Split info in it, so AdjustedPrice is calculated correctly
            // YF sometimes is not correct. And if it is not correct, we cannot reapair it.
            // IB sometimes doesn't give back the LastClosePrice; it is bug in their system. We cannot fix it.
            // At least, if our SQL DB doesn't give back the info, we can correct it during the day. So, we should rely on our SQL DB more than YF.

            //var p_sqlConn = new SqlConnection("ConnectionString");

            List<QuoteData> vxxQuotesFromSqlDB = SqlTools.LoadHistoricalQuotesAsync(new[] {
                    new QuoteRequest { Ticker = "VXX", nQuotes = lookbackWindowSize }}, DbCommon.AssetType.Stock).Result.
                    Select(row => new QuoteData { Date = (DateTime)row[1], AdjClosePrice = (double)row[2] }).OrderBy(row => row.Date).ToList(); // stocks come as double objects: (double)row[2], indexes as floats  (double)(float)row[2]

            // check that the last date in the CSV is what we expect: the previous market Open day
            //DateTime expectedUtcLastDateTime = DBUtils.GetPreviousMarketOpenDay(utcNow, StockExchangeID.NASDAQ, VBroker.g_dbManager); // it needs UTC input, result.TimeOfDay == local 00:00 converted to UTC; so it is <Date>:4:00 (UTC), so we should convert it back to Local
            //DateTime expectedETLastDateTime = TimeZoneInfo.ConvertTimeFromUtc(expectedUtcLastDateTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            //DateTime expectedETLastDate = expectedETLastDateTime.Date;
            //VBrokerLogger.StrongAssert(taskLogFile, vxxQuotesFromSqlDB.Last().Date == expectedETLastDate, Severity.Exception, "m_vxxQuotesFromSqlDB.Last().Date == m_dateNowInET");             // assert that its last Date is today, VBroker is running all day

            //m_vxx = vxxQuotesFromSqlDB.Select(item => new QuoteData() { Date = item.Date, AdjClosePrice = item.AdjClosePrice }).ToList(); // Clone the SQL version, not YF

            // so, for VXX, for which there is no dividend, I can use the split-adjusted IB prices, but for other cases, I will have to use our SQL database (or YF or GF or all)
            Contract contract = new Contract() { Symbol = "VXX", SecType = "STK", Currency = "USD", Exchange = "SMART" };
            if (!Controller.g_gatewaysWatcher.ReqHistoricalData(DateTime.UtcNow, lookbackWindowSize, "TRADES", contract, out m_vxx))   // real trades, not the MidPoint = AskBidSpread
                return null;

            // Check danger after stock split correctness: adjusted price from IB should match to the adjusted price of our SQL DB. Although it can happen that both data source is faulty.
            if (Utils.IsInRegularTradingHoursNow(TimeSpan.FromDays(3))) // in development, we often program code after IB market closed. Ignore this warning after market, but check it during market.
                StrongAssert.True(Math.Abs(vxxQuotesFromSqlDB[vxxQuotesFromSqlDB.Count - 1].AdjClosePrice - m_vxx[m_vxx.Count - 2].AdjClosePrice) < 0.02, Severity.NoException, "We continue but yesterday price data doesn't match from IB and SQL DB");

            // log the last 3 values (for later debugging)
            Utils.Logger.Warn($"{m_vxx[m_vxx.Count - 3].Date.ToString("yyyy-MM-dd")}: {m_vxx[m_vxx.Count - 3].AdjClosePrice}");
            Utils.Logger.Warn($"{m_vxx[m_vxx.Count - 2].Date.ToString("yyyy-MM-dd")}: {m_vxx[m_vxx.Count - 2].AdjClosePrice}");
            Utils.Logger.Warn($"{m_vxx[m_vxx.Count - 1].Date.ToString("yyyy-MM-dd")}: {m_vxx[m_vxx.Count -1].AdjClosePrice} (last trade, not the last midPoint. MidPoint would be better)");

            // 2. Do some preprocess
            //Implement that connor vbroker calculates sma100 of prob ft dir, and act according to a threshold
            //FTDirProb Regime Threshold:	0.48, >48% is good regime (for example 49% is still a good regime); but, 48.00 is a bed regime. In bad regime, we do MR, otherwise FT.
            //"g:\work\Archi-data\HedgeQuant\docs\gyantal\Studies\VIX\Autocorrelation\VXX auto-correlation with timers\probFTDirSMA100Timer\VXX auto-correlation-from2004_probFTDirSMA100Timer_doInverse 2.xlsx" 
            double[] vxxDailyPChg = new double[101];
            for (int i = 0; i < 101; i++)
            {
                vxxDailyPChg[i] = m_vxx[m_vxx.Count - (101 - i)].AdjClosePrice / m_vxx[m_vxx.Count - (101 - i) - 1].AdjClosePrice - 1;
            }
            double[] vxxDailyFT = new double[100];
            for (int i = 0; i < 100; i++)
            {
                vxxDailyFT[i] = (vxxDailyPChg[i] >= 0 && vxxDailyPChg[i + 1] >= 0 || vxxDailyPChg[i] < 0 && vxxDailyPChg[i + 1] < 0) ? 1 : 0;
            }
            m_probDailyFT = vxxDailyFT.Average();

            // 3. Process it
            double dailyPercentChange = vxxDailyPChg[vxxDailyPChg.Length - 1];
            //FTDirProb Regime Threshold:	0.48, >48% is good regime (for example 49% is still a good regime); but, 48.00 is a bed regime. In bad regime, we do MR, otherwise FT.
            // if today %change = 0, try to short VXX, because in general, 80% of the time, it is worth shorting VXX than going long
            double forecast = 0;
            bool isFTRegime = m_probDailyFT > m_probDailyFTGoodFtRegimeThreshold;
            if (isFTRegime)   // FT regime
                forecast = (dailyPercentChange > 0) ? 1 : -1;        // FT regime: if %change = 0, we bet VXX will go down (-1)
            else
                forecast = (dailyPercentChange >= 0) ? -1 : 1;// MR regime

            Utils.Logger.Warn($"dailyPercentChange: {dailyPercentChange * 100.0:F2}%, m_probDailyFT: {m_probDailyFT * 100.0}%, isFTRegime: {((isFTRegime) ? "FT" : "MR")}, forecast: {forecast*100}%");

            List<PortfolioPositionSpec> specs = new List<PortfolioPositionSpec>();
            if (forecast > 0)   // bullish on VXX: buy VXX or UVXY/TVIX
            {
                // IBrokerStrategy should not really know about the Trading Execution, or the proper trading instrument of the user or leverage of the user. 
                // The BrokerTask should overwrite the trading instrument if it wants that suits to the different portfolios
                // but we want to Calculate this complex Strategy only once, just giving a guideline for the BrokerTask
                specs.Add(new PortfolioPositionSpec() { Ticker = "VXX", PositionType = PositionType.Long, Size = WeightedSize.Create(1.0) });
            }
            else if (forecast < 0)  // bearish on VXX: short VXX or UVXY/TVIX
            {
                specs.Add(new PortfolioPositionSpec() { Ticker = "SVXY", PositionType = PositionType.Long, Size = WeightedSize.Create(1.0) });
            }
            else
                Utils.Logger.Warn("Stay in cash");

            return specs;
        }


        public double GetPortfolioLeverage(List<PortfolioPositionSpec> p_suggestedPortfItems, IPortfolioParam p_param)
        {
            PortfolioParamUberVXX param = (PortfolioParamUberVXX)p_param;
            // the strategy knows that p_suggestedPortfItems only has 1 row, either VXX (for long VXX) or SVXY (for short VXX)
            if (p_suggestedPortfItems[0].Ticker == "VXX")
                return param.PlayingInstrumentVixLongLeverage;
            else
                return param.PlayingInstrumentVixShortLeverage;
        }
    }
}
