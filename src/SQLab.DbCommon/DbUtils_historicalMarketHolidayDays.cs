using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbCommon
{
    // A. Utils_tradingHours.cs is for TradingHours, Open, Close, EarlyClose. It can calculate Open and Close times precisely. It can calculate next trading days in the future (nextTradingDay). 
    //      It has only currently 1 year data from Nasdaq webpage.
    //      It is used by VirtualBrokers for trading scheduling.
    // B. DbUtils_historicalMarketHolidayDays.cs is about all historical days. That is from the SqlDb. It has no idea if there was an EarlyClose or not on that day. 
    //      Currently, it contains 16 years of data, from 2001-01-01.
    //      It is used by QuickTester for historical backtesting.
    public static partial class DbUtils
    {
        static Dictionary<CountryID, List<DateTime>> g_marketHolidays = null;
        static DateTime g_marketHolidaysDownloadDate = DateTime.MinValue;    // the last time we downloaded info from the internet
        static TimeSpan g_maxAllowedStalenessDefault = TimeSpan.FromDays(3);

        public static Dictionary<CountryID, List<DateTime>> GetMarketHolidays()
        {
            return GetMarketHolidays(g_maxAllowedStalenessDefault);
        }

        public static Dictionary<CountryID, List<DateTime>> GetMarketHolidays(TimeSpan p_maxAllowedStaleness)
        {
            if ((g_marketHolidays != null) && (DateTime.UtcNow - g_marketHolidaysDownloadDate) < p_maxAllowedStaleness)
                return g_marketHolidays;

            g_marketHolidays = SqlTools.LoadMarketHolidays().Result;

            g_marketHolidaysDownloadDate = DateTime.UtcNow;
            return g_marketHolidays;
        }

        public static bool IsMarketOpenDayUtc(this DateTime p_timeUtc, StockExchangeID p_xchg)
        {
            var tzData = DbUtils.StockExchangeToTimeZoneData[(int)p_xchg];
            DateTime timeLoc = TimeZoneInfo.ConvertTime(p_timeUtc, TimeZoneInfo.Utc, tzData.TimeZoneInfo);

            return IsMarketOpenDayLoc(timeLoc.Date, tzData.CountryID);
        }

        public static bool IsUsaMarketOpenDayUtc(this DateTime p_timeUtc)
        {
            var tzData = DbUtils.StockExchangeToTimeZoneData[(int)StockExchangeID.NASDAQ];
            DateTime timeLoc = TimeZoneInfo.ConvertTime(p_timeUtc, TimeZoneInfo.Utc, tzData.TimeZoneInfo);

            return IsMarketOpenDayLoc(timeLoc.Date, tzData.CountryID);
        }




        public static bool IsUsaMarketOpenDayLoc(this DateTime p_timeLoc)
        {
            return IsMarketOpenDayLoc(p_timeLoc.Date, CountryID.UnitedStates);
        }


        /// <summary> Precondition: p_dateLocal.TimeOfDay == 0 </summary>
        // See also http://www.isthemarketopen.com/,
        // http://www.chronos-st.org/NYSE_Observed_Holidays-1885-Present.html
        public static bool IsMarketOpenDayLoc(DateTime p_dateLocal, CountryID p_countryID)
        {
            DayOfWeek day = p_dateLocal.DayOfWeek;
            if (day == DayOfWeek.Sunday || day == DayOfWeek.Saturday)
                return false;
            var marketHolidays = GetMarketHolidays()[p_countryID];
            DateTime dateLocalDate = p_dateLocal.Date;
            var foundInd = marketHolidays.BinarySearch(dateLocalDate);
            bool isHoliday = foundInd >= 0;
            return !isHoliday;
        }

        public static DateTime GetNextUsaMarketOpenDayLoc(this DateTime p_timeLoc, bool p_currentDayAcceptable /* today is Inclusive or not */)
        {
            return GetNextOrPreviousMarketOpenDayLoc(p_timeLoc, CountryID.UnitedStates, p_currentDayAcceptable, true);
        }

        public static DateTime GetPreviousUsaMarketOpenDayLoc(this DateTime p_timeLoc, bool p_currentDayAcceptable /* today is Inclusive or not */)
        {
            return GetNextOrPreviousMarketOpenDayLoc(p_timeLoc, CountryID.UnitedStates, p_currentDayAcceptable, false);
        }

        public static DateTime GetNextOrPreviousMarketOpenDayLoc(this DateTime p_timeLoc, CountryID p_countryID, bool p_currentDayAcceptable /* Inclusive */, bool p_isNext)
        {
            DateTime timeToTestLoc = (p_currentDayAcceptable) ? p_timeLoc.Date : ((p_isNext) ? p_timeLoc.Date.AddDays(1) : p_timeLoc.Date.AddDays(-1));
            int iLoop = 0;
            while (true)
            {
                if (iLoop++ > 50)
                {
                    Utils.Logger.Error($"Avoiding infinite loop. GetNextOrPreviousUsaMarketOpenDayLoc() iLoop is too high: {iLoop}. There is no marketOpen day in the next/previous 50 days. Impossible.");
                    return DateTime.MaxValue;
                }
                bool isMarketTradingDay = IsMarketOpenDayLoc(timeToTestLoc, p_countryID);
                if (isMarketTradingDay) // Postcondition: result.TimeOfDay == local 00:00 converted to UTC
                {
                    return timeToTestLoc;
                }
                timeToTestLoc = (p_isNext) ? timeToTestLoc.AddDays(1) : timeToTestLoc.AddDays(-1);
            }   // while
        }
    }
}
