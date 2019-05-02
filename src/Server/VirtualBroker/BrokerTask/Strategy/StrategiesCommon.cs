using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualBroker
{
   
    public class DailyData
    {
        public DateTime Date { get; set; }
        public double AdjClosePrice { get; set; }
    }

    public class StrategiesCommon
    {

        public static List<List<DailyData>> GetHistoricalAndRealTimeDataForAllParts(List<string> p_tickers, DateTime? p_commonStartDate, int p_nQuotes)
        {
            ushort sqlReturnedColumns = QuoteRequest.TDC;       // QuoteRequest.All or QuoteRequest.TDOHLCVS

            Utils.Logger.Trace("LoadHistoricalQuotesAsync() begin");
            var sqlReturn = SqlTools.LoadHistoricalQuotesAsync(p_tickers.Select(r => new QuoteRequest { Ticker = r, StartDate = p_commonStartDate, nQuotes = p_nQuotes, NonAdjusted = false, ReturnedColumns = sqlReturnedColumns }), DbCommon.AssetType.Stock, true).Result;  // Ascending date order: TRUE, better to order it at the SQL server than locally. SQL has indexers
            Utils.Logger.Trace("LoadHistoricalQuotesAsync() end");

            List<List<DailyData>> allQuotes = null;
            // sql query of "VXX.SQ" gives back tickers of VXX and also tickers of "VXX.SQ"
            int closePriceIndex = -1;
            if (sqlReturnedColumns == QuoteRequest.TDOHLCVS)
                closePriceIndex = 5;
            else if (sqlReturnedColumns == QuoteRequest.TDC)
                closePriceIndex = 2;
            else
                throw new NotImplementedException();

            // this sub-optimal data sorting takes 10msec for the 8 assets of TAA Global, with 12 years of data. So, don't optimize this. SQL query took 1200msec=1.2 sec or more.
            allQuotes = p_tickers.Select(ticker =>
            {
                IEnumerable<object[]> mergedRows = SqlTools.GetTickerAndBaseTickerRows(sqlReturn, ticker);
                return mergedRows.Select(
                    row => new DailyData()
                    {
                        Date = ((DateTime)row[1]),
                        AdjClosePrice = (double)Convert.ToDecimal(row[closePriceIndex])  // row[2] is object(double) if it is a stock (because Adjustment multiplier), and object(float) if it is Indices. However Convert.ToDouble(row[2]) would convert 16.66 to 16.6599999
                    }).ToList();
            }).ToList();
            Utils.Logger.Trace("LoadHistoricalQuotesAsync() data crunching ends.");

            var rtPrices = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() } };    // MID is the most honest price. LAST may happened 1 hours ago
            for (int iTicker = 0; iTicker < p_tickers.Count; iTicker++)
            {
                Contract contract = VBrokerUtils.ParseSqTickerToContract(p_tickers[iTicker]);
                StrongAssert.True(Controller.g_gatewaysWatcher.GetAlreadyStreamedPrice(contract, ref rtPrices), Severity.ThrowException, $"There is no point continuing if rtPrice cannot be obtained for ticker '{p_tickers[iTicker]}'.");
                double rtPrice = rtPrices[TickType.MID].Price;

                var quotes = allQuotes[iTicker];
                quotes.Add(new DailyData() { Date = DateTime.UtcNow, AdjClosePrice = rtPrice });

                // log the last 3 values (for later debugging)
                int quoteLength = quotes.Count;
                Utils.Logger.Debug($"{p_tickers[iTicker]},{quotes[quoteLength - 3].Date.ToString("yyyy-MM-dd")}: {quotes[quoteLength - 3].AdjClosePrice}");
                Utils.Logger.Debug($"{p_tickers[iTicker]},{quotes[quoteLength - 2].Date.ToString("yyyy-MM-dd")}: {quotes[quoteLength - 2].AdjClosePrice}");
                Utils.Logger.Debug($"{p_tickers[iTicker]},{quotes[quoteLength - 1].Date.ToString("yyyy-MM-dd")}: {quotes[quoteLength - 1].AdjClosePrice}");

                // 2017-01-19: sbTracer is for debugging porpuses. To check that SQ DB is consistent with AdjustedClosePrices
                StringBuilder sbTracer = new StringBuilder($"{p_tickers[iTicker]}," + Environment.NewLine);
                foreach (var q in quotes)
                {
                    sbTracer.AppendLine($"{q.Date.ToString("yyyy-MM-dd")},{q.AdjClosePrice:F3}");
                }
                Utils.Logger.Trace(sbTracer.ToString());
            }

            return allQuotes;
        }

    }
}
