﻿using IBApi;
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
        // // VXX,^VIX,^GSPC,XIV,#^VIX201610,GOOG
        public static Contract ParseSqTickerToContract(string p_sqTicker)    
        {
            Contract contract;
            if (p_sqTicker[0] == '^') // if Index, not stock. Index has only LastPrice and TickType.ClosePrice
            {
                string symbol = p_sqTicker.Substring(1); // skip the "^"
                if (symbol == "GSPC")       //
                    symbol = "SPX";
                else if (symbol == "VXV")   // On September 18, 2017 the ticker symbol for the Cboe 3-Month Volatility Index was changed from “VXV” to “VIX3M”; So IB returns 'No security definition has been found' for VXV, but accepts VIX3M.
                    symbol = "VIX3M";
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

                string expiry = p_sqTicker.Substring(p_sqTicker.Length - 6);        // expiry = "201610", however in real life expire = "20161019" as last day can be also specified for LastTradeDateOrContractMonth
                string exchange = "CFE";    // works for VIX futures
                // from 2016: they introduced weekly VIX futures, not only monthly. Those have same Multiplier = 1000, but different TradingClass.
                // Ib error: "The contract description specified for VIX is ambiguous; you must specify the multiplier or trading class."
                // Trading Class: for monthly VIX futures: "VX", for weekly, differs, eg. "VX40V6", and different for each weekly.
                // Conlusion: we should add the multiplier. Easier than handle the messy TradingClass. But that didn't solve the problem.
                // After adding only multiplier, Ib error: "The contract description specified for VIX is ambiguous."
                // After adding only tradingClass, Ib error: ErrCode: 354, Msg: "Requested market data is not subscribed.Error&CFE/FUT/Top&CFE/FUT/Top.". But this was, because I didn't have realtime data, only delayed. Paid $2.5 per month, and it works.
                string multiplier = null, tradingClass = null;   // the default value is null
                if (symbol.Equals("VIX", StringComparison.CurrentCultureIgnoreCase))
                {
                    multiplier = "1000";    // works for VIX futures and in general for futures
                    tradingClass = "VX";
                    //expiry = "20161019";  // not needed. "201610" is fine as ContractMonth in LastTradeDateOrContractMonth 
                }

                contract = new Contract() { Symbol = symbol, SecType = "FUT", Currency = "USD", Exchange = exchange, LastTradeDateOrContractMonth = expiry, Multiplier = multiplier, TradingClass = tradingClass };  // this works for Futures, but we don't want to Fix it as they will expire 
                //contract = new Contract("VIX", "CFE", SecurityType.Future, "USD", "201404"); // this works for VIX Futures
            }
            else
            {
                contract = new Contract() { Symbol = p_sqTicker, SecType = "STK", Currency = "USD", Exchange = "SMART" };

                switch (p_sqTicker.ToUpper())
                {
                    case "GLD":     // ErrCode: 200, Msg: The contract description specified for GLD is ambiguous.  Because there is a UK stock with the same name, Symbol:GLD,Underlying:GLD,Currency:USD,Exchange:SMART,PrimaryExchange:LSE,IssuerCountry:IE. 
                        // So, in this case we have to specify ARCA, but we don't want to specify for All USA stocks, because other stocks are on NYSE.
                        contract.PrimaryExch = "ARCA";
                        break;
                }
            }

            return contract;
        }

        public static bool IsContractEqual(Contract p_contract1, Contract p_contract2)  // a deep
        {
            if (p_contract1.Symbol != p_contract2.Symbol)
                return false;
            if (p_contract1.SecType != p_contract2.SecType)
                return false;
            if (p_contract1.Currency != p_contract2.Currency)
                return false;

            if (p_contract1.LastTradeDateOrContractMonth != p_contract2.LastTradeDateOrContractMonth)
                return false;
            if (p_contract1.Multiplier != p_contract2.Multiplier)
                return false;
            if (p_contract1.TradingClass != p_contract2.TradingClass)
                return false;

            return true;
        }
    }
}
