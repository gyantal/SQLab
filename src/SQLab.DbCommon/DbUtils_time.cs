using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqCommon;

namespace DbCommon
{
    public static partial class DbUtils
    {
        // Contains 1 item for every possible StockExchangeID
        // Designed to be thread safe!
        //static StockExchangeTimeZoneData[] g_timeZoneInfo;
        public class StockExchangeTimeZoneData
        {
            public StockExchangeID StockExchangeID;
            public CountryID CountryID;
            public TimeZoneId TimeZoneID;
            public TimeZoneInfo TimeZoneInfo;

            internal StockExchangeTimeZoneData(StockExchangeID p_stockExchangeID, CountryID p_countryID, TimeZoneId p_timeZoneID, TimeZoneInfo p_timeZoneInfo)
            {
                StockExchangeID = p_stockExchangeID;
                CountryID = p_countryID;
                TimeZoneID = p_timeZoneID;
                TimeZoneInfo = p_timeZoneInfo;
            }

            public override string ToString() { return StockExchangeID.ToString(); }    // for debugging
            public override int GetHashCode() { return ((int)TimeZoneID << 8) + (int)StockExchangeID; }
            public override bool Equals(object obj)
            {
                return (obj is StockExchangeTimeZoneData) && GetHashCode() == obj.GetHashCode();
            }
        }

        private static StockExchangeTimeZoneData[] g_stockExchangeToTimeZoneData;
        public static StockExchangeTimeZoneData[] StockExchangeToTimeZoneData
        {
            get
            {
                if (g_stockExchangeToTimeZoneData != null)
                    return g_stockExchangeToTimeZoneData;

                var tziEST = SqCommon.Utils.FindSystemTimeZoneById(TimeZoneId.EST);
                var tziLondon = SqCommon.Utils.FindSystemTimeZoneById(TimeZoneId.London);
                var tziCet = SqCommon.Utils.FindSystemTimeZoneById(TimeZoneId.CET);

                g_stockExchangeToTimeZoneData = new StockExchangeTimeZoneData[] {
                    new StockExchangeTimeZoneData(StockExchangeID.Unknown, CountryID.Unknown, TimeZoneId.Unknown, TimeZoneInfo.Utc),    // actually StockExchangeID.Unknown = -1, which would crash, which may be OK, because it is dangerous to continue
                    new StockExchangeTimeZoneData(StockExchangeID.NASDAQ, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.NYSE, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.AMEX, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.PINK, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.CDNX, CountryID.Canada, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.LSE, CountryID.UnitedKingdom, TimeZoneId.London, tziLondon),
                    new StockExchangeTimeZoneData(StockExchangeID.XETRA, CountryID.Germany, TimeZoneId.CET, tziCet),
                    new StockExchangeTimeZoneData(StockExchangeID.CBOE, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.ARCA, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.BATS, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
                    new StockExchangeTimeZoneData(StockExchangeID.OTCBB, CountryID.UnitedStates, TimeZoneId.EST, tziEST),
               };

                return g_stockExchangeToTimeZoneData;
            }
        }


        


    }
}
