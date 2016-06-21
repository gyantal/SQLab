using IBApi;
using SqCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public struct QuoteData
    {
        public DateTime Date;
        public double AdjClosePrice;
    }

    public partial class VBrokerUtils
    {
        internal static List<QuoteData> ParseCSVToQuotes(string p_csvStr, bool p_oldestFirst)
        {
            List<QuoteData> parsedData = new List<QuoteData>();

            try
            {
                var rows = p_csvStr.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                int i = 0;
                foreach (var line in rows)
                {
                    string[] cells = line.Split(',');
                    if (i != 0)     // skip the first line: that is the header
                        parsedData.Add(new QuoteData() { Date = DateTime.Parse(cells[0]), AdjClosePrice = Double.Parse(cells[6]) });
                    i++;
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Info(e.Message);
            }

            if (p_oldestFirst)  // if we want chronological order (the index = 0 is the latest), we have to reverse the data
                parsedData.Reverse();

            return parsedData;
        }


        public static bool IsCSVAcceptable(List<QuoteData> p_quoteCSV, bool p_oldestFirst, DateTime p_startDateInclusive, DateTime p_endDateInclusive)  //p_vxxCSV : oldest data is first in the list
        {
            // Robi says don't use Linq2SQL; use SQL query or MemTables. I want lightweight without Cache-ing, so I query it directly. ExecuteSqlCommand() retries 4 times.
            //DataTable marketHolidays = (DataTable)VBroker.g_dbManager.ExecuteSqlCommand(DBType.Remote,
            //    String.Format("SELECT Date from MarketHoliday WHERE CountryID = 1 AND (Date >= '{0:yyyy'-'MM'-'dd}' AND Date <= '{1:yyyy'-'MM'-'dd}') ORDER By Date", p_startDateInclusive, p_endDateInclusive),
            //    CommandType.Text, null, SqlCommandReturn.Table, 10);  // it retries 4 times, which is OK.
            //List<DateTime> marketHolidayDates = marketHolidays.Rows.OfType<DataRow>().Select(r => (DateTime)r.ItemArray[0]).ToList();

            //int indCsv = p_oldestFirst ? 0 : p_quoteCSV.Count - 1;
            //DateTime date = p_startDateInclusive;
            //while (date <= p_endDateInclusive)
            //{
            //    bool isDateMarketOffday = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            //    if (!isDateMarketOffday)
            //    {
            //        isDateMarketOffday = marketHolidayDates.Contains(date);
            //    }

            //    if (isDateMarketOffday)
            //    {
            //        date = date.AddDays(1);
            //        continue;
            //    }

            //    // if it is not an off day, we expect to find it in the CSV file
            //    if (p_quoteCSV[indCsv].Date != date)
            //    {
            //        return false;       // so it is not acceptable
            //    }

            //    if (p_oldestFirst)
            //        indCsv++;
            //    else
            //        indCsv--;
            //    date = date.AddDays(1);
            //}

            return true;    // so it is acceptable
        }

        // use YahooFinance ticker terminology (^GSPC instead of SPX); uppercase is a must. Hide it here, so it is not global. threadLock is not required
        // // VXX,^VIX,^VXV,^GSPC,XIV,#^VIX201610,GOOG
        public static Contract ParseSqTickerToContract(string p_sqTicker)    
        {
            Contract contract;
            if (p_sqTicker[0] == '^') // if Index, not stock. Index has only LastPrice and TickType.ClosePrice
            {
                string symbol = p_sqTicker.Substring(1); // skip the "^"
                if (symbol == "GSPC")       //
                    symbol = "SPX";
                string exchange = (p_sqTicker == "^RUT") ? "RUSSELL" : "CBOE";
                string localSymbol = symbol;        // maybe it is not necessary. However for RUT, it worked
                contract = new Contract() { Symbol = symbol, SecType = "IND", Currency = "USD", Exchange = exchange, LocalSymbol = localSymbol };  // remove the ^ when you send to IB
            }
            else if (p_sqTicker[0] == '#')    // ?s=^^VIX201404 was converted to ?s=#^VIX201404
            {
                // assume last YYYYMM 6 characters is the expiry
                string symbol = p_sqTicker.Substring(1, p_sqTicker.Length - 6 - 1);
                if (symbol[0] == '^')
                    symbol = symbol.Substring(1, symbol.Length - 1);
                string exchange = "CFE";    // wokrs for VIX futures
                string expiry = p_sqTicker.Substring(p_sqTicker.Length - 6);

                contract = new Contract() { Symbol = symbol, SecType = "FUT", Currency = "USD", Exchange = exchange, LastTradeDateOrContractMonth = expiry };  // this works for Futures, but we don't want to Fix it as they will expire 
                //contract = new Contract("VIX", "CFE", SecurityType.Future, "USD", "201404"); // this works for VIX Futures
            }
            else
                contract = new Contract() { Symbol = p_sqTicker, SecType = "STK", Currency = "USD", Exchange = "SMART" };

            return contract;
        }
    }
}
