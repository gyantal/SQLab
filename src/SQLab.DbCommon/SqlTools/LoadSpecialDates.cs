using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbCommon
{
    public static partial class SqlTools
    {
        public static async Task<Dictionary<CountryID, List<DateTime>>> LoadStockMarketClosedDates()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            //  DATEPART(dw,Sunday)=1, Saturday=7, we don't want Sundays (1), but Saturday could be a trading day in Hungary sometimes, so get Saturday dates. Just exclude Sundays.
            // this code is from the MarketHoliday view in SQlDb
            sqlBuilder.Append($"SELECT [CountryID],[Date] FROM [dbo].[DateProperties]  WHERE ([Flags] & {(short)DatePropertiesFlags.StockMarketClosed})<>0 AND (DATEPART(dw,[Date])%6)<>1 ORDER BY Date");
            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);

            Dictionary<CountryID, List<DateTime>> marketClosedDates = new Dictionary<CountryID, List<DateTime>>();
            int nFutureUsaMarketClosedDays = 0;
            DateTime utcNow = DateTime.UtcNow;
            foreach (var dbRow in sqlResult[0])
            {
                CountryID countryID = (CountryID)dbRow[0];
                List<DateTime> countryMarketClosedDates;
                if (!marketClosedDates.TryGetValue(countryID, out countryMarketClosedDates))
                {
                    countryMarketClosedDates = new List<DateTime>();
                    marketClosedDates.Add(countryID, countryMarketClosedDates);
                }

                DateTime marketClosedDateLoc = (DateTime)dbRow[1];
                if (countryID == CountryID.UnitedStates && marketClosedDateLoc > utcNow)
                {
                    nFutureUsaMarketClosedDays++;
                }
                countryMarketClosedDates.Add(marketClosedDateLoc);
            }

            // there is usually 9 marketHolidays in a year. If there is only 3 in the future, inform supervisors, 
            // check whether the last item is suspiciosly problematic because it is too close 
            if (nFutureUsaMarketClosedDays <= 3)
            {
                Utils.Logger.Warn("DateProperties table in SqlDB needs update. Only average 3 nFutureUsaMarketClosedDays.");
                // VBroker App Handler will Send Tcp Error message to HealthMonitor, but this execution continues, no Exception is thrown
                StrongAssert.Fail(Severity.NoException, "DateProperties table in SqlDB needs update. Only average 3 nFutureUsaMarketClosedDays.");
            }

            return marketClosedDates;
        }


        public static async Task<Dictionary<CountryID, List<DateProperty>>> LoadRegularEventDatesHolidays()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            // 16384: StockMarketClosed, about 159 rows, "911Attacks", "HurricaneSandy", "FuneralReagen" included in Comment. 
            //      But we don't need those. And if a CivilHoliday day has a stock market open, that day is not included here.
            // 15 <binary 1111, bottom 4 bits>: USA Civil Holidays, all civil holidays irrespective of that stock market was open or not. 151 rows. (911Attacks excluded)
            //      However, on 2016-05-20, the database is not yet filled with examples of SuperBowl or other lesser holidays when the stock market was open. But we don't need that data yet. 
            // Balazs's HolidayResearch Excel table should contain old dates, Future dates should be collected by a Crawler, or by a Warning email to Supervisors.
            // 8192: FOMC-MeetingLastDay, about 136 rows from 2000-02-02
            // this SQL query will get all dates, except one time-stock market close days like "911Attacks", "HurricaneSandy", "FuneralReagen" (not regular), 
            // exclude "|EarlyCloseTimeLoc=13:00 EST|Christmas Eve Day|" days, because we don't need open market EarlyCloseTimeLoc days.
            // Note that these "|EarlyCloseTimeLoc=13:00 EST|Christmas Eve Day|" days didn't occur every year, but be sure that Xmas days indicated with a Flag is in the table for every year.
            sqlBuilder.Append($"SELECT [Date],[CountryID],[Flags],[Comment] FROM [dbo].[DateProperties]  WHERE ([Flags] & ({(short)DatePropertiesFlags._KindOfUsaHolidayAndAllRegularEvents}))<>0 ORDER BY Date");
            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);

            Dictionary<CountryID, List<DateProperty>> eventDates = new Dictionary<CountryID, List<DateProperty>>();
            int nFutureUsaHolidays = 0;
            int nFutureFomcMeetingDates = 0;
            DateTime utcNow = DateTime.UtcNow;
            foreach (var dbRow in sqlResult[0])
            {
                CountryID countryID = (CountryID)dbRow[1];
                List<DateProperty> countryEventDates;
                if (!eventDates.TryGetValue(countryID, out countryEventDates))
                {
                    countryEventDates = new List<DateProperty>();
                    eventDates.Add(countryID, countryEventDates);
                }

                DateTime eventDateLoc = (DateTime)dbRow[0];
                DatePropertiesFlags flags = (DatePropertiesFlags)(short)dbRow[2];
                string comment = (string)dbRow[3];
                if (countryID == CountryID.UnitedStates && eventDateLoc > utcNow)
                {
                    if ((flags & DatePropertiesFlags.FomcMeetingLastDay) != 0)
                        nFutureFomcMeetingDates++;
                    if ((flags & DatePropertiesFlags._KindOfUsaHoliday) != 0)
                        nFutureUsaHolidays++;

                }
                countryEventDates.Add(new DateProperty() { CountryID = countryID, DateLoc = eventDateLoc, Flags = flags, Comment = comment  });
            }

            // there is usually 9 marketHolidays in a year. If there is only 3 in the future, inform supervisors, 
            // check whether the last item is suspiciosly problematic because it is too close 
            if (nFutureUsaHolidays <= 3)
            {
                Utils.Logger.Warn("DateProperties table in SqlDB needs update. Only average 3 nFutureUsaHolidays.");
                // VBroker App Handler will Send Tcp Error message to HealthMonitor, but this execution continues, no Exception is thrown
                StrongAssert.Fail(Severity.NoException, "DateProperties table in SqlDB needs update. Only average 3 nFutureUsaHolidays.");
            }
            if (nFutureFomcMeetingDates <= 2)   // allow 3 Fomc meetings, because they don't update dates very often: https://www.federalreserve.gov/monetarypolicy/fomccalendars.htm
            {
                Utils.Logger.Warn("DateProperties table in SqlDB needs update. Only average 2 nFutureFomcMeetingDates.");
                // VBroker App Handler will Send Tcp Error message to HealthMonitor, but this execution continues, no Exception is thrown
                StrongAssert.Fail(Severity.NoException, "DateProperties table in SqlDB needs update. Only average 2 nFutureFomcMeetingDates.");
            }

            return eventDates;
        }

    }
}