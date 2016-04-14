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
        public static async Task<Dictionary<CountryID, List<DateTime>>> LoadMarketHolidays()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.Append("SELECT [CountryID],[Date] FROM [dbo].[MarketHoliday] ORDER BY Date");
            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);


            Dictionary<CountryID, List<DateTime>> marketHolidays = new Dictionary<CountryID, List<DateTime>>();
            int nAllHolidaysInTheFuture = 0;
            DateTime utcNow = DateTime.UtcNow;
            foreach (var dbRow in sqlResult[0])
            {
                CountryID countryID = (CountryID)dbRow[0];
                List<DateTime> holidayDays;
                if (!marketHolidays.TryGetValue(countryID, out holidayDays))
                {
                    holidayDays = new List<DateTime>();
                    marketHolidays.Add(countryID, holidayDays);
                }

                DateTime holiday = (DateTime)dbRow[1];
                if (holiday > utcNow)
                    nAllHolidaysInTheFuture++;
                holidayDays.Add(holiday);
            }

            // there is usually 9 marketHolidays in a year. If there is only 3 in the future, inform supervisors, 
            // check whether the last item is suspiciosly problematic because it is too close 
            int nHolidaysInTheFuturePerCountry = (int)(nAllHolidaysInTheFuture / ((double)marketHolidays.Count));
            if (nHolidaysInTheFuturePerCountry <= 2)
            {
                // write to Log file
                Utils.Logger.Warn("MarketHolidays table in SqlDB needs update. Only 2 holidays in the future.");    
                // VBroker App Handler will Send Tcp Error message to HealthMonitor, but this execution continues, no Exception is thrown
                StrongAssert.Fail(Severity.NoException, "MarketHolidays table in SqlDB needs update. Only 2 holidays in the future.");  
            }

            return marketHolidays;
        }

    }
}