using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqCommon;

namespace DbCommon
{
    public static partial class DbUtils
    {
        private static TimeZoneId[] g_stockExchangeToTimeZoneId;
        public static TimeZoneId[] StockExchangeToTimeZoneId
        {
            get
            {
                if (g_stockExchangeToTimeZoneId != null)
                    return g_stockExchangeToTimeZoneId;

                g_stockExchangeToTimeZoneId = new TimeZoneId[] {
                    TimeZoneId.Unknown,
                    TimeZoneId.EST, // StockExchangeID.NASDAQ
                    TimeZoneId.EST, // StockExchangeID.NYSE
                    TimeZoneId.EST, // StockExchangeID.AMEX
                    TimeZoneId.EST, // StockExchangeID.PINK
                    TimeZoneId.EST, // StockExchangeID.CDNX
                    TimeZoneId.London, // StockExchangeID.LSE
                    TimeZoneId.CET, // StockExchangeID.XETRA
                    TimeZoneId.EST, // StockExchangeID.CBOE
                    TimeZoneId.EST, // StockExchangeID.ARCA
                    TimeZoneId.EST, // StockExchangeID.BATS
                    TimeZoneId.EST, // StockExchangeID.OTCBB
               };

                return g_stockExchangeToTimeZoneId;
            }
        }
        


    }
}
