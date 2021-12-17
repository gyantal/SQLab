using DbCommon;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public partial class UberVxxStrategy : IBrokerStrategy
    {
        // Fomc is just like any other regular holiday. It recurs. It should be forecasted with adaptive learning.
        // 
        // Holiday substrategy is too overoptimized, because the number of samples are low. For 20 years data, the number of samples is 20. Not much.
        // still, it is better to play it adaptively with learning, because we don't have to manually fine-tune the rules later.
        public double? GetUberVxx_FomcAndHolidays_ForecastVxx()
        {
            List<DateProperty> specialDates = DbCommon.SqlTools.LoadRegularEventDatesHolidays().Result[CountryID.UnitedStates]; // it is ordered by Date

            DateTime nextTradingDayUtc = Utils.GetNextUsaMarketOpenDayUtc(DateTime.UtcNow, false);
            DateTime nextTradingDayET_Date = Utils.ConvertTimeFromUtcToEt(nextTradingDayUtc).Date;
            //SampleStatsPerRegime regimeToUse = IsBullishWinterDay(nextTradingDayET_Date) ? m_winterStats : m_summerStats;

            // 1. FOMC section (separation is not necessary). With this implementation that FOMC is ahead of Holidays, FOMC has higher priority, which is OK for now.
            // for FOMC T-3, T-2, T-1, T+0, T+1, and because there are 8 FOMC days in a year, find the first Fomc date backwards which is less than NextTradingDay + 10 calendar days
            DateTime fomcDateUpperThresholdET = nextTradingDayET_Date.AddDays(10);  // for debug purposes, you can change this to 30, but in the final version this shouldb be 10, so that FindList() only has maximum one candidate
            DateTime fomcDateLowerThresholdET = nextTradingDayET_Date.AddDays(-10);
            var closestFomcET = specialDates.FindLast(r => ((r.Flags & DatePropertiesFlags.FomcMeetingLastDay) != 0) && (r.DateLoc < fomcDateUpperThresholdET) && (r.DateLoc > fomcDateLowerThresholdET)); // FindLast(), ordered list. Search backward as there are less items in the future than in the past
            if (closestFomcET != null)
            {
                int offsetInd = CalculateOffsetIndOfTradingDateFromEvent(nextTradingDayET_Date, closestFomcET.DateLoc);       // VXX forecast: Long,Long,Short,Short,Long for days T-3, T-2, T-1, T+0, T+1
                Utils.ConsoleWriteLine(null, false, $"Closest FOMC date: {closestFomcET.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd >= 0) ? "+" : "-")}{Math.Abs(offsetInd)}");
                Utils.Logger.Debug($"Closest FOMC date found: {closestFomcET.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd>=0)? "+": "-")}{Math.Abs(offsetInd)}");
                m_detailedReportSb.AppendLine($"Closest FOMC date: {closestFomcET.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd >= 0) ? "+" : "-")}{Math.Abs(offsetInd)}");
                if (offsetInd == -1 || offsetInd == 0)
                    return -1.0;    // VXX negative short forecast, which is bullish for the market
                if (offsetInd == -3 || offsetInd == -2 || offsetInd == +1)
                    return 1.0;     // VXX positive long forecast, which is bearish for the market
            }


            //2. Holidays section (separation is not necessary)
            DateTime holidayDateUpperThresholdET = nextTradingDayET_Date.AddDays(15);  // 15 is optimal as: T-5 is played for Xmas, and T+5 is played for NewYear, and they can overlap. It can be any other number, not only 15, the code will find the closest one.
            DateTime holidayDateLowerThresholdET = nextTradingDayET_Date.AddDays(-15);
            // NewYear and Xmas is close to each other, so multiple holidays can be collected. Finding the closest one is not enough. 
            // 2016 -12-28 was Xmas(T+2) was closest (no signal, so we followed Connor daily FollowThrough), but NewYear(T-3) as second closest (had bearish signal.). We should have followed NewYear(T-3), as that was profitable.
            // So we have to collect all holidays within the -15..+15 day's range, and go to the second or third too if first closest doesn't have a signal
            List<DateProperty> holidaysInRange = new List<DateProperty>();
            for (int i = specialDates.Count - 1; i >= 0; i--)
            {
                if (specialDates[i].DateLoc > holidayDateUpperThresholdET)
                    continue;
                if (specialDates[i].DateLoc < holidayDateLowerThresholdET)
                    break;  // List is ordered by Date. If DateLoc is too low, don't search lower dates

                if (((specialDates[i].Flags & DatePropertiesFlags._KindOfUsaHoliday) == 0))
                    continue;

                Utils.Logger.Debug($"Potential holiday date found: {specialDates[i].DateLoc}");
                holidaysInRange.Add(specialDates[i]);
            }

            // order holidaysInRange by closeness
            var holidaysInRangeOrdered = holidaysInRange.OrderBy(r =>
            {
                int offsetInd = CalculateOffsetIndOfTradingDateFromEvent(nextTradingDayET_Date, r.DateLoc);
                return Math.Abs(offsetInd);
            });

            foreach (var holiday in holidaysInRangeOrdered)
            {
                int offsetInd = CalculateOffsetIndOfTradingDateFromEvent(nextTradingDayET_Date, holiday.DateLoc);
                double? holidaySignal = GetHolidaySignal(holiday, offsetInd);
                if (holidaySignal != null)
                {
                    DatePropertiesFlags holidayFlagOnly = holiday.Flags & DatePropertiesFlags._KindOfUsaHoliday;
                    Utils.Logger.Debug($"Closest holiday with signal: '{holidayFlagOnly}' on {holiday.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd >= 0) ? "+" : "-")}{Math.Abs(offsetInd)}");
                    Utils.ConsoleWriteLine(null, false, $"Closest holiday with signal: '{holidayFlagOnly}' on {holiday.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd >= 0) ? "+" : "-")}{Math.Abs(offsetInd)}");
                    m_detailedReportSb.AppendLine($"Closest holiday with signal: '{holidayFlagOnly}' on {holiday.DateLoc.ToString("yyyy-MM-dd")}, day T{((offsetInd >= 0) ? "+" : "-")}{Math.Abs(offsetInd)}");
                    return holidaySignal;
                }
            }

            return null;
        }

        private double? GetHolidaySignal(DateProperty p_holiday, int p_offsetInd)
        {
            DatePropertiesFlags holidayFlagOnly = p_holiday.Flags & DatePropertiesFlags._KindOfUsaHoliday;
            // Holiday days was revised in 2015-11, based on that here is the latest in 2016-04: https://docs.google.com/document/d/1Kaazv6gjDfffHG3cjNgSMuseoe45UftMKiZP8XPO2pA/edit
            // Holiday days was revised in 2017-02: https://docs.google.com/document/d/1OAMwErTzAxezrqcgyan4kgapF5OcG3VFF6Y5zpm5Xkk
            
            // If NewYearEve is Saturday, then officially there is no USA holiday, and there is no [observed] holiday either on Monday or Friday.
            // According to this federal law, we have the following in SQL:
            // SELECT *  FROM [dbo].[DateProperties]  WHERE [Flags]&(15) = 1
            // 1990-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1991-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1992-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1993-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // // 1994 - missing: Saturday.
            // 1995-01-02	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1996-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1997-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1998-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 1999-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // //2000  - missing: Saturday.
            // 2001-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2002-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2003-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2004-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // //2005  - missing: Saturday.
            // 2006-01-02	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2007-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2008-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2009-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2010-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // // 2011  - missing: Saturday.
            // 2012-01-02	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2013-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2014-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2015-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2016-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2017-01-02	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2018-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2019-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2020-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // 2021-01-01	1	16385	NULL	|StockMarketClosed|NewYear|
            // // 2022 - missing: Saturday.
            // SqLab:UberVxx strategy uses: SQL: "SELECT * FROM [dbo].[DateProperties]" as a base for holiday.
            // So, in any backtest (or VirtualBroker real-time strategy trade), those quasy-holiday days are missing.
            // Right or wrong? Interesting.
            // In the future, in the new RedisDb, we might design that we have these exceptional dates too, with "NewYear|" flag.
            // Otherwise, every C# code has to be ready to consider these exceptional cases. (which will be forgotten for sure)
            // But at the moment, I don't want to poke with (and potentially ruin) the SqFramework SQL.

            switch (holidayFlagOnly)
            {
                case DatePropertiesFlags.NewYear:
                    if (p_offsetInd == +1 || p_offsetInd == +2 || p_offsetInd == +3 || p_offsetInd == +4 || p_offsetInd == +5)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    if (p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3)
                        return 1.0;    // VXX positive long forecast, which is bearish for the market
                    break;
                case DatePropertiesFlags.MLutherKing:
                    if (p_offsetInd == -1 || p_offsetInd == -2)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    break;
                // as of 2016, SuperBowl is not a stock market holiday, only civil holiday, 
                // 2017-02-03: it is not in our database, but I am happy to skip this, it shouldn't be significant
                // 2018-02-05: it was not in our database, so we didn't play it. However the forecast was very good. VXX went up + 30% on that day. A pity we didn't have 
                // Balazs's HolidayResearch Excel table should contain old dates for SuperBowl and other holidays, Future dates should be collected by a Crawler, or by a Warning email to Supervisors.
                case DatePropertiesFlags.SuperBowl:     // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                    if (p_offsetInd == 0)
                        return 1.0;    // VXX positive long forecast, which is bearish for the market
                    break;
                case DatePropertiesFlags.Presidents:
                    if (p_offsetInd == -1 || p_offsetInd == -2)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    break;
                case DatePropertiesFlags.GoodFriday:
                    if (p_offsetInd == +1 || p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3 || p_offsetInd == -4)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    break;
                case DatePropertiesFlags.Memorial:
                    break;
                case DatePropertiesFlags.Juneteenth:
                    break;
                case DatePropertiesFlags.Independence:
                    if (p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3 || p_offsetInd == -4 || p_offsetInd == -5)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    if (p_offsetInd == +1)
                        return 1.0;    // VXX positive long forecast, which is bearish for the market
                    break;
                case DatePropertiesFlags.Labor:
                    if (p_offsetInd == -4 || p_offsetInd == -5)
                        return 1.0;    // VXX positive long forecast, which is bearish for the market
                    if (p_offsetInd == +3 || p_offsetInd == +2 || p_offsetInd == +1 || p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    break;
                case DatePropertiesFlags.Columbus: // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                    break;
                case DatePropertiesFlags.Veterans: // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                    break;
                case DatePropertiesFlags.Thanksgiving:
                    if (p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3 || p_offsetInd == -4 || p_offsetInd == +3 || p_offsetInd == +4)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    if (p_offsetInd == +1 || p_offsetInd == +2)
                        return 1.0;    // VXX positive long forecast, which is bearish for the market
                    break;
                case DatePropertiesFlags.Xmas:
                    if (p_offsetInd == -1 || p_offsetInd == -2 || p_offsetInd == -3 || p_offsetInd == -4 || p_offsetInd == -5)
                        return -1.0;    // VXX negative short forecast, which is bullish for the market
                    break;
                default:
                    break;
            }
            return null;
        }

        private int CalculateOffsetIndOfTradingDateFromEvent(DateTime p_tradingDate, DateTime p_eventDate)
        {
            DateTime nextTradingDate = p_tradingDate.Date;  // don't trust caller that it has only a Date part. It should, but it is safer this way.
            DateTime eventDate = p_eventDate.Date;
            if (nextTradingDate == eventDate)
                return 0;       // it is T+0

            DateTime iDate = nextTradingDate;
            if (nextTradingDate < eventDate)
            {
                iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false); // this is day T+1
                int iDateOffset = 1;    // T+1
                while (iDate < eventDate) // marching forward until iDate = startDate
                {
                    iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false);
                    iDateOffset++;
                }
                return -1* iDateOffset; // inversion is needed. We walked from nextTradingDate to the Event. However, strategies use T-values from Event to the TradingDay, which is opposite.
            }
            else
            {
                iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false); // this is day T+1
                int iDateOffset = -1;    // T-1
                while (iDate > eventDate) // marching backward until iDate = startDate
                {
                    iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);
                    iDateOffset--;
                }
                return -1 * iDateOffset; // inversion is needed. We walked from nextTradingDate to the Event. However, strategies use T-values from Event to the TradingDay, which is opposite.
            }
        }
    }




}
