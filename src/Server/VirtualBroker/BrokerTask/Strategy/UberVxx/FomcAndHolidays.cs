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
        public double? GetUberVxx_FomcAndHolidays_ForecastVxx()    // forecast VXX, not SPY
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
            // NewYear and Xmas is close to each other, so multiple holidays can be collected. We have to find the closest one.
            DateProperty closestHolidayET = null;
            int closestHolidayOffsetInd = Int32.MaxValue;
            for (int i = specialDates.Count - 1; i >= 0; i--)
            {
                if (specialDates[i].DateLoc > holidayDateUpperThresholdET)
                    continue;
                if (specialDates[i].DateLoc < holidayDateLowerThresholdET)
                    break;  // List is ordered by Date. If DateLoc is too low, don't search lower dates

                if (((specialDates[i].Flags & DatePropertiesFlags._KindOfUsaHoliday) == 0))
                    continue;

                Utils.Logger.Debug($"Potential holiday date found: {specialDates[i].DateLoc}");
                int offsetInd = CalculateOffsetIndOfTradingDateFromEvent(nextTradingDayET_Date, specialDates[i].DateLoc);
                if (Math.Abs(offsetInd) < Math.Abs(closestHolidayOffsetInd))
                {
                    closestHolidayOffsetInd = offsetInd;
                    closestHolidayET = specialDates[i];
                }
            }
            if (closestHolidayET != null)
            {
                DatePropertiesFlags holidayFlagOnly = closestHolidayET.Flags & DatePropertiesFlags._KindOfUsaHoliday;
                Utils.Logger.Debug($"Closest holiday '{holidayFlagOnly}' on {closestHolidayET.DateLoc.ToString("yyyy-MM-dd")}, day T{((closestHolidayOffsetInd >= 0) ? "+" : "-")}{Math.Abs(closestHolidayOffsetInd)}");
                Utils.ConsoleWriteLine(null, false, $"Closest holiday '{holidayFlagOnly}' on {closestHolidayET.DateLoc.ToString("yyyy-MM-dd")}, day T{((closestHolidayOffsetInd >= 0) ? "+" : "-")}{Math.Abs(closestHolidayOffsetInd)}");
                m_detailedReportSb.AppendLine($"Closest holiday '{holidayFlagOnly}' on {closestHolidayET.DateLoc.ToString("yyyy-MM-dd")}, day T{((closestHolidayOffsetInd >= 0) ? "+" : "-")}{Math.Abs(closestHolidayOffsetInd)}");

                // Holiday days was revised in 2015-11, based on that here is the latest in 2016-04: https://docs.google.com/document/d/1Kaazv6gjDfffHG3cjNgSMuseoe45UftMKiZP8XPO2pA/edit
                switch (holidayFlagOnly)
                {
                    case DatePropertiesFlags.NewYear:
                        if (closestHolidayOffsetInd == +1 || closestHolidayOffsetInd == +2 || closestHolidayOffsetInd == +3 || closestHolidayOffsetInd == +4 || closestHolidayOffsetInd == +5)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        if (closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3)
                            return 1.0;    // VXX positive long forecast, which is bearish for the market
                        break;
                    case DatePropertiesFlags.MLutherKing:
                        if (closestHolidayOffsetInd == -1)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        break;
                    case DatePropertiesFlags.SuperBowl:     // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                        if (closestHolidayOffsetInd == 0)
                            return 1.0;    // VXX positive long forecast, which is bearish for the market
                        break;
                    case DatePropertiesFlags.Presidents:
                        break;
                    case DatePropertiesFlags.GoodFriday:
                        if (closestHolidayOffsetInd == +1 || closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        break;
                    case DatePropertiesFlags.Memorial:      // it was put here just for testing the implementation. In 2016-05, this was the next potential holiday
                        if (closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == +1)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        break;
                    case DatePropertiesFlags.Independence:
                        if (closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3 || closestHolidayOffsetInd == -4 || closestHolidayOffsetInd == -5)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        break;
                    case DatePropertiesFlags.Labor:
                        if (closestHolidayOffsetInd == +1 || closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        if (closestHolidayOffsetInd == -4 || closestHolidayOffsetInd == -5)
                            return 1.0;    // VXX positive long forecast, which is bearish for the market
                        break;
                    case DatePropertiesFlags.Columbus: // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                        break;
                    case DatePropertiesFlags.Veterans: // T+1 in the Sub-strategy table is actually Day T+0, because after 1998, MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
                        break;
                    case DatePropertiesFlags.Thanksgiving:
                        if (closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3 || closestHolidayOffsetInd == -4)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        if (closestHolidayOffsetInd == +1 || closestHolidayOffsetInd == +2)
                            return 1.0;    // VXX positive long forecast, which is bearish for the market
                        break;
                    case DatePropertiesFlags.Xmas:
                        if (closestHolidayOffsetInd == -1 || closestHolidayOffsetInd == -2 || closestHolidayOffsetInd == -3 || closestHolidayOffsetInd == -4 || closestHolidayOffsetInd == -5)
                            return -1.0;    // VXX negative short forecast, which is bullish for the market
                        break;  
                    default:
                        break;
                }            
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
