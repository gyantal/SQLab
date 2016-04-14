using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SqCommon
{
    // A. Utils_tradingHours.cs is for TradingHours, Open, Close, EarlyClose. It can calculate Open and Close times precisely. It can calculate next trading days in the future (nextTradingDay). 
    //      It has only currently 1 year data from Nasdaq webpage.
    //      It is used by VirtualBrokers for trading scheduling.
    // B. DbUtils_historicalMarketHolidayDays.cs is about all historical days. That is from the SqlDb. It has no idea if there was an EarlyClose or not on that day. 
    //      Currently, it contains 16 years of data, from 2001-01-01.
    //      It is used by QuickTester for historical backtesting.
    public static partial class Utils
    {
        static List<Tuple<DateTime, DateTime?>> g_holidays = null;
        static DateTime g_holidaysDownloadDate = DateTime.MinValue;    // the last time we downloaded info from the internet

        // the advantage of using https://www.nyse.com/markets/hours-calendars is that it not only gives back Early Closes, but the Holiday days too
        public static List<Tuple<DateTime, DateTime?>> GetHolidaysAndHalfHolidaysWithCloseTimesInET(TimeSpan p_maxAllowedStaleness)
        {
            if ((g_holidays != null) && (DateTime.UtcNow - g_holidaysDownloadDate) < p_maxAllowedStaleness)
                return g_holidays;

            string webPage = String.Empty;

            // using http://www.thestreet.com/stock-market-news/11771386/market-holidays-2015.html is not recommended, 
            //because for 20x pages it does an Adver redirection instead of giving back the proper info the returned page 
            // is an advert. So, stick to the official NYSE website.
            bool isDownloaded = Utils.DownloadStringWithRetry(out webPage, "https://www.nyse.com/markets/hours-calendars", 5, TimeSpan.FromSeconds(2), false);
            if (!isDownloaded)
            {
                if ((g_holidays != null))
                {
                    if ((DateTime.UtcNow - g_holidaysDownloadDate) < TimeSpan.FromDays(8))
                        return g_holidays;  // silently use the old data
                    else
                    {
                        // the g_holidays data is considered to be too old, but use it
                        Utils.Logger.Error(@"Failed 5x to townload ""https://www.nyse.com/markets/hours-calendars"". We use older than 8 days g_holidays data. Which is OK, but take note of this. Check that that website works or not.");
                        return g_holidays;
                    }
                }
                else
                {
                    Utils.Logger.Error(@"Failed 5x to townload ""https://www.nyse.com/markets/hours-calendars"". And there is no old g_holidays data to use. Null is returned.");
                    return null;
                }
            }

            var holidays1 = new List<Tuple<DateTime, DateTime?>>();
            var holidays2 = new List<Tuple<DateTime, DateTime?>>();

            try
            {
                // 1. Get section from <thead> to </tbody> for the holidays
                // 2. Get section from ">*", ">**", ">***" to </p> to get the 3 footnotes
                int iTHead = webPage.IndexOf(@"{""head"":");
                int iTBody = webPage.IndexOf(@"""foot"":", iTHead);
                string holidayTable = webPage.Substring(iTHead, iTBody - iTHead);
                int iFootnoteStart = webPage.IndexOf(">*", iTBody);
                int iFootnoteEnd = webPage.IndexOf("The NYSE", iFootnoteStart);
                string footnote = webPage.Substring(iFootnoteStart, iFootnoteEnd - iFootnoteStart);

                int year1 = -1, year2 = -1;
                var trs = holidayTable.Split(new string[] { @"{""cells"":[" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < trs.Length; i++)
                {
                    if (!trs[i].TrimStart().StartsWith(@"{""content"""))
                        continue;

                    var tds = trs[i].Split(new string[] { @"""content"":""", @"""}" }, StringSplitOptions.RemoveEmptyEntries);
                    if (year1 == -1 && year2 == -1)
                    {
                        year1 = Int32.Parse(tds[3]);
                        year2 = Int32.Parse(tds[5]);
                        continue;
                    }

                    //string holidayName = tds[1];
                    ProcessHolidayCellInET(tds[3], year1, footnote, holidays1);
                    ProcessHolidayCellInET(tds[5], year2, footnote, holidays2);
                }

            }
            catch (Exception ex)
            {
                Utils.Logger.Error(ex, "This error is expected once every year. Exception in DetermineUsaMarketOpenOrCloseTimeNYSE() in String operations. Probably the structure of the page changed, re-code is needed every year. Debug it in VS, recode and redeploy. Message:" + ex.Message);
                return null;
            }

            g_holidays = holidays1;
            g_holidays.AddRange(holidays2); // the holidays list is not ordered by date, because sometimes this halfDay comes before/after the holiday day
            g_holidaysDownloadDate = DateTime.UtcNow;
            return g_holidays;
        }

        private static void ProcessHolidayCellInET(string p_td, int p_year, string p_footnote, List<Tuple<DateTime, DateTime?>> p_holidays)
        {
            // "<td>July 4 (Observed July 3)</td>"  , 4th is Saturday, so the market is closed on the "observed" date
            // <td>November 26**</td>
            //< td > December 25(Observed December 26) ***</ td >  this has both Observed and a half-holiday too

            // at first 
            if (p_td.IndexOf('*') != -1)    // read the footnotes; there will be a half-holiday on the next or the previous day
            {
                // "**Each market will close early at 1:00 p.m. on Friday, November 27, 2015 and Friday, November 25, 2016 (the day after Thanksgiving)"
                // "***Each market will close early at 1:00 p.m. on Thursday, December 24, 2015. "
                int nAsterisk = p_td.Count(r => r == '*');
                int indExplanation = -1;
                for (int i = 0; i < p_footnote.Length; i++)
                {
                    if (p_footnote[i] == '>')
                    {
                        for (int j = 0; j < nAsterisk; j++)
                        {
                            if (p_footnote[i + 1 + j] != '*')
                                break;
                            if (j == nAsterisk - 1)
                                indExplanation = i + 1 + j + 1;
                        }
                    }
                    if (indExplanation != -1)
                        break;
                }

                int indExplanationEnd = p_footnote.IndexOf("</p>", indExplanation);    // go to the end of the sentence only.
                string explanation = p_footnote.Substring(indExplanation, indExplanationEnd - indExplanation);

                int indTimeET = explanation.IndexOf("Each market will close early at ");
                int indTimeET1 = indTimeET + "Each market will close early at ".Length;
                int indTimeET2 = explanation.IndexOf(':', indTimeET1);
                int indTimeET3 = explanation.IndexOf("p.m.", indTimeET2);
                string earlyCloseHourStr = explanation.Substring(indTimeET1, indTimeET2 - indTimeET1);
                string earlyCloseMinStr = explanation.Substring(indTimeET2 + 1, indTimeET3 - indTimeET2 - 1);
                int earlyCloseHour = Int32.Parse(earlyCloseHourStr) + 12; //"1 p.m." means you have to add 12 hours to the recognized digit
                int earlyCloseMin = Int32.Parse(earlyCloseMinStr);

                // try to find the Year in the text, then wark backwards for 2 commas
                int indYear = explanation.IndexOf(p_year.ToString(), indTimeET3);
                int indFirstComma = explanation.LastIndexOf(',', indYear);
                int indSecondComma = explanation.LastIndexOf(',', indFirstComma - 1);
                string dateStr = explanation.Substring(indSecondComma + 1, (indFirstComma - 1) - indSecondComma);
                DateTime halfDay = DateTime.Parse(dateStr + ", " + p_year.ToString());
                // the holidays list is not ordered by date, because sometimes this halfDay comes before/after the holiday day
                p_holidays.Add(new Tuple<DateTime, DateTime?>(halfDay, new DateTime(halfDay.Year, halfDay.Month, halfDay.Day, earlyCloseHour, earlyCloseMin, 0)));

                p_td = p_td.Replace('*', ' ');  //remove ** if it is in the string, because Date.Parse() will fail on that
            }

            DateTime dateHoliday;
            int indObserved = p_td.IndexOf("(Observed");
            if (indObserved == -1)
            {
                dateHoliday = DateTime.Parse(p_td + ", " + p_year.ToString());
            }
            else
            {
                int observedDateStartInd = indObserved + "(Observed".Length;
                int indObservedEnd = p_td.IndexOf(')', observedDateStartInd);
                dateHoliday = DateTime.Parse(p_td.Substring(observedDateStartInd, indObservedEnd - observedDateStartInd) + ", " + p_year.ToString());
            }
            p_holidays.Add(new Tuple<DateTime, DateTime?>(dateHoliday, null));

        }

        // it is important that p_timeUtc can be a Time and it is in UTC. Convert it to ET to work with it.
        public static bool DetermineUsaMarketTradingHours(DateTime p_timeUtc, out bool p_isMarketTradingDay, out DateTime p_openTimeUtc, out DateTime p_closeTimeUtc, TimeSpan p_maxAllowedStaleness)
        {
            p_openTimeUtc = p_closeTimeUtc = DateTime.MinValue;
            p_isMarketTradingDay = false;

            TimeZoneInfo utcZone = TimeZoneInfo.Utc;
            TimeZoneInfo estZone = null;
            try
            {
                estZone = Utils.FindSystemTimeZoneById(TimeZoneId.EST);
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Exception because of TimeZone conversion ");
                return false;
            }

            DateTime timeET = TimeZoneInfo.ConvertTime(p_timeUtc, utcZone, estZone);

            if (timeET.DayOfWeek == DayOfWeek.Saturday || timeET.DayOfWeek == DayOfWeek.Sunday)
            {
                p_isMarketTradingDay = false;
                return true;
            }

            List<Tuple<DateTime, DateTime?>> holidaysAndHalfHolidays = GetHolidaysAndHalfHolidaysWithCloseTimesInET(p_maxAllowedStaleness);
            if (holidaysAndHalfHolidays == null || holidaysAndHalfHolidays.Count == 0)
            {
                Logger.Error("holidaysAndHalfHolidays are not recognized");
                return false;
            }

            DateTime openInET = new DateTime(timeET.Year, timeET.Month, timeET.Day, 9, 30, 0);
            DateTime closeInET;
            var todayHoliday = holidaysAndHalfHolidays.FirstOrDefault(r => r.Item1 == timeET.Date);
            if (todayHoliday == null)   // it is a normal day, not holiday: "The NYSE and NYSE MKT are open from Monday through Friday 9:30 a.m. to 4:00 p.m. ET."
            {
                p_isMarketTradingDay = true;
                closeInET = new DateTime(timeET.Year, timeET.Month, timeET.Day, 16, 0, 0);
            }
            else
            { // if it is a holiday or a half-holiday (that there is trading, but early close)
                p_isMarketTradingDay = (todayHoliday.Item2 != null);   // Item2 is the CloseTime (that is for half-holidays)
                if (todayHoliday.Item2 == null)
                {
                    p_isMarketTradingDay = false;
                    return true;
                }
                else
                {
                    p_isMarketTradingDay = true;
                    closeInET = (DateTime)todayHoliday.Item2;  // yes, halfHolidays are in ET 
                }
            }

            if (!p_isMarketTradingDay)
                return true;
            
            p_openTimeUtc = TimeZoneInfo.ConvertTime(openInET, estZone, utcZone);
            p_closeTimeUtc = TimeZoneInfo.ConvertTime(closeInET, estZone, utcZone);
            return true;
        }

        public static bool IsInRegularUsaTradingHoursNow(TimeSpan p_maxAllowedStaleness)
        {
            DateTime utcNow = DateTime.UtcNow;

            bool isMarketTradingDay;
            DateTime openTimeUtc, closeTimeUtc;
            bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(utcNow, out isMarketTradingDay, out openTimeUtc, out closeTimeUtc, p_maxAllowedStaleness);
            if (!isTradingHoursOK)
            {
                Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
                return false;
            }
            else
            {
                if (!isMarketTradingDay)
                    return false;
                if (utcNow < openTimeUtc)
                    return false;
                if (utcNow > closeTimeUtc)
                    return false;

                return true;
            }
        }

        /// <summary> Postcondition: result.TimeOfDay == local 00:00 converted to UTC </summary>
        public static DateTime GetNextUsaMarketOpenDayUtc(this DateTime p_timeUtc, bool p_currentDayTimeAcceptable /* today is Inclusive or not */)
        {
            return GetNextOrPrevUsaMarketOpenDayUtc(p_timeUtc, p_currentDayTimeAcceptable, true, TimeSpan.FromDays(3));
        }

        /// <summary> Postcondition: result.TimeOfDay == local 00:00 converted to UTC </summary>
        public static DateTime GetPreviousUsaMarketOpenDayUtc(this DateTime p_timeUtc, bool p_currentDayTimeAcceptable /* today is Inclusive or not */)
        {
            return GetNextOrPrevUsaMarketOpenDayUtc(p_timeUtc, p_currentDayTimeAcceptable, false, TimeSpan.FromDays(3));
        }

        /// <summary> Postcondition: result.TimeOfDay == local 00:00 converted to UTC </summary>
        public static DateTime GetNextOrPrevUsaMarketOpenDayUtc(this DateTime p_timeUtc, bool p_currentDayTimeAcceptable /* Inclusive */, bool p_isNext, TimeSpan p_maxAllowedStaleness) // today is not allowed
        {
            DateTime timeToTestUtc = (p_currentDayTimeAcceptable) ? p_timeUtc : ((p_isNext) ? p_timeUtc.AddDays(1) : p_timeUtc.AddDays(-1));
            int iLoop = 0;
            while (true)
            {
                if (iLoop++ > 50)
                {
                    Utils.Logger.Error($"Avoiding infinite loop. GetNextUsaMarketOpenDayUtc() iLoop is too high: {iLoop}. There is no marketOpen day in the next/previous 50 days. Impossible.");
                    return DateTime.MaxValue;
                }
                bool isMarketTradingDay;
                DateTime openTimeUtc, closeTimeUtc;
                bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(timeToTestUtc, out isMarketTradingDay, out openTimeUtc, out closeTimeUtc, p_maxAllowedStaleness);
                if (!isTradingHoursOK)
                {
                    Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
                    return DateTime.MaxValue;
                }
                else
                {
                    if (isMarketTradingDay) // Postcondition: result.TimeOfDay == local 00:00 converted to UTC
                    {
                        DateTime resultInET = Utils.ConvertTimeFromUtcToEt(timeToTestUtc).Date; // == local 00:00
                        return Utils.ConvertTimeFromEtToUtc(resultInET);
                    }
                    timeToTestUtc = (p_isNext) ? timeToTestUtc.AddDays(1) : timeToTestUtc.AddDays(-1);
                }
            }   // while


        }


    }
}
