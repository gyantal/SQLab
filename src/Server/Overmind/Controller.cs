using SQCommon;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;     // this is the only timer available under DotNetCore

namespace Overmind
{
    // send Calendar emails (about Birthday (orsi), Pay-Rent, Accountant), 
    // it can do other things like: watch BIDU price and send SMS if it fell more than 5%;
    class Controller
    {
        static public Controller g_controller = new Controller();

        ManualResetEventSlim gMainThreadExitsResetEvent = null;

        //Your timer object goes out of scope and gets erased by Garbage Collector after some time, which stops callbacks from firing. Save reference to it in a member of class.
        long m_nHeartbeat = 0;
        Timer m_heartbeatTimer = null;
        Timer m_dailyMorningTimer = null;
        Timer m_dailyMiddayTimer = null;

        readonly DateTime g_DailyMorningTimerTime = new DateTime(2000, 1, 1, 9, 05, 0);      // the date part is not used only the time part. Activate every day 9:05
        readonly DateTime g_DailyMiddayTimerTime = new DateTime(2000, 1, 1, 18, 0, 0);      // the date part is not used only the time part, Activate every day: 18:00
        const int cHeartbeatTimerFrequencyMinutes = 5;

        internal void Start()
        {
            gMainThreadExitsResetEvent = new ManualResetEventSlim(false);
            ScheduleDailyTimers();
        }

        internal void Exit()
        {
            gMainThreadExitsResetEvent.Set();
        }

        internal void ScheduleDailyTimers()
        {
            try
            {
                Utils.Logger.Info("ScheduleDailyTimers() BEGIN");

                TimeSpan untilDailyTimer = GetNextDailyTimerIntervalMsec(g_DailyMorningTimerTime);
                m_dailyMorningTimer = new System.Threading.Timer(new TimerCallback(DailyMorningTimer_Elapsed), null, untilDailyTimer, TimeSpan.FromMilliseconds(-1.0));
                Utils.Logger.Info("m_dailyTimer is scheduled at " + (DateTime.UtcNow + untilDailyTimer).ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture));

                TimeSpan untilDailyMarketWatcherTimer = GetNextDailyTimerIntervalMsec(g_DailyMiddayTimerTime);
                m_dailyMiddayTimer = new System.Threading.Timer(new TimerCallback(DailyMiddayTimer_Elapsed), null, untilDailyMarketWatcherTimer, TimeSpan.FromMilliseconds(-1.0));
                Utils.Logger.Info("m_dailyMarketWatcherTimer is scheduled at " + (DateTime.UtcNow + untilDailyMarketWatcherTimer).ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture));

                m_heartbeatTimer = new System.Threading.Timer((e) =>    // Heartbeat log is useful to find out when VM was shut down, or when the App crashed
                {
                    Utils.Logger.Info(String.Format("**m_nHeartbeat: {0} (at every {1} minutes)", m_nHeartbeat, cHeartbeatTimerFrequencyMinutes));
                    m_nHeartbeat++;
                }, null, TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(cHeartbeatTimerFrequencyMinutes));

            }
            catch (Exception e)
            {
                Utils.Logger.Info(e, "ScheduleDailyTimers() Exception.");
            }
            Utils.Logger.Info("ScheduleDailyTimers() END");
        }



        // http://mono.1490590.n4.nabble.com/Cross-platform-time-zones-td1507630.html
        //In windows the timezones have a descriptive name such as "Eastern 
        //Standard Time" but in linux the same timezone has the name 
        //"US/Eastern". 
        //Is there a cross platform way of running
        //TimeZoneInfo.FindSystemTimeZoneById that can be used both in linux and
        //windows, or would i have to add additional code to check what platform
        //i am running before getting the time zone.
        //WINDOWS TIMEZONE ID DESCRIPTION                UNIX TIMEZONE ID
        //Eastern Standard Time => GMT - 5 w/DST             => US/Eastern
        //Central Standard Time => GMT - 6 w/DST             => US/Central
        //US Central Standard Time  => GMT-6 w/o DST(Indiana) => US / Indiana - Stark
        //Mountain Standard Time    => GMT-7 w/DST             => US/Mountain
        //US Mountain Standard Time => GMT-7 w/o DST(Arizona) => US / Arizona
        //Pacific Standard Time     => GMT-8 w/DST             => US/Pacific
        //Alaskan Standard Time => GMT - 9 w/DST             => US/Alaska
        //Hawaiian Standard Time => GMT - 10 w/DST            => US/Hawaii


        static TimeSpan GetNextDailyTimerIntervalMsec(DateTime p_targetDateTimeLT)  // LT: LondonTime
        {
            //http://www.mcnearney.net/blog/windows-timezoneinfo-olson-mapping/
            //http://unicode.org/repos/cldr/trunk/common/supplemental/windowsZones.xml
            //string londonZoneId = "GMT Standard Time";      // Linux: "Europe/London"

            string londonZoneId = String.Empty;
            if (Utils.RunningPlatform() == Platform.Windows)
                londonZoneId = "GMT Standard Time";
            else
                londonZoneId = "Europe/London";
            //if (Common.Utils.RunningPlatform() == Common.Platform.Windows)
            //    utcZoneId = "UTC";
            //else
            //    utcZoneId = "Etc/GMT";

            try
            {
                TimeZoneInfo utcZone = TimeZoneInfo.Utc;

                TimeZoneInfo londonZone = null;
                try
                {
                    londonZone = TimeZoneInfo.FindSystemTimeZoneById(londonZoneId);  // that is London time. != UTC
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Unable to find the {0} zone in the registry. {1}", londonZoneId, e.Message);
                }

                DateTime nowLT = TimeZoneInfo.ConvertTime(DateTime.UtcNow, utcZone, londonZone);  // LT: London time
                //DateTime nowLT = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, londonZone);  // LT: London time
                DateTime proposedTimerStartLT = new DateTime(nowLT.Year, nowLT.Month, nowLT.Day, p_targetDateTimeLT.Hour, p_targetDateTimeLT.Minute, p_targetDateTimeLT.Second);
                double scheduleStartTimerInMsec = (proposedTimerStartLT - nowLT).TotalMilliseconds;
                if (scheduleStartTimerInMsec <= 1000)  // it can be negative if we are after 9:00
                {
                    DateTime tomorrowLT = nowLT.AddDays(1); // if today is the 31st of the month, the next day is not a simple addition
                    proposedTimerStartLT = new DateTime(tomorrowLT.Year, tomorrowLT.Month, tomorrowLT.Day, p_targetDateTimeLT.Hour, p_targetDateTimeLT.Minute, p_targetDateTimeLT.Second); // next day
                    scheduleStartTimerInMsec = (proposedTimerStartLT - nowLT).TotalMilliseconds;
                }
                return TimeSpan.FromMilliseconds(scheduleStartTimerInMsec);
            }
            
            catch (InvalidTimeZoneException)
            {
                Console.WriteLine("Registry data on the {0} zone has been corrupted.", londonZoneId);
            }

            return TimeSpan.FromHours(24);  // let it be the next 24 hour
        }

     


        public void DailyMorningTimer_Elapsed(object p_sender)
        {
            Utils.Logger.Info("DailyMorningTimer_Elapsed() BEGIN");
            Console.WriteLine(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + " : DailyMorningTimer_Elapsed() BEGIN");
            try
            {
                if (m_dailyMorningTimer != null)
                {
                    TimeSpan ts = GetNextDailyTimerIntervalMsec(g_DailyMorningTimerTime);
                    m_dailyMorningTimer.Change(ts, TimeSpan.FromMilliseconds(-1.0));
                    Utils.Logger.Info("m_dailyMorningTimer is scheduled at " + (DateTime.UtcNow + ts).ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture));
                }
                    

                DateTime utcToday = DateTime.UtcNow.Date;
                string todayDateStr = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
                string todayMonthAndDayStr = DateTime.UtcNow.ToString("MM'-'dd", CultureInfo.InvariantCulture);

                if (todayMonthAndDayStr == "10-05")        // Orsi's birthday
                {
                    new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer: Orsi's birthday", Body = "Orsi's birthday is on 1976-10-09.", IsBodyHtml = false }.Send();
                }

                Utils.Logger.Info("DailyMorningTimer_Elapsed(): Checking first day of the month");
                if (DateTime.UtcNow.AddDays(0).Day == 1)
                {
                    // Balazs Lukucz asked me that never send salaries on 30th or 31st of previous month. 
                    // So I will report to Barbara only on 1st day of every month, and maybe they got salaries later. 
                    // And this has an advantage that as I don't send the holidays report earlier, if they forget to tell me their 'last minute' day-offs, it is not reported to Barbara too early.
                    // So less headache overall.
                    new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer: send holidays, bank report to accountant", Body = "Send holidays, bank report to accountant. In 3 days, it is the 1st day of the month. ", IsBodyHtml = false }.Send();
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e.Message);
                new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer: Crash", Body = "Crash. Exception: " + e.Message + ", StackTrace " + e.StackTrace + ", ToString(): " + e.ToString(), IsBodyHtml = false }.Send();
            }

            Utils.Logger.Info("DailyMorningTimer_Elapsed() END");
        }

        public void DailyMiddayTimer_Elapsed(object p_sender)
        {
            Utils.Logger.Info("DailyMiddayTimer_Elapsed() BEGIN");
            Console.WriteLine(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + " : DailyMiddayTimer_Elapsed() BEGIN");
            try
            {
                if (m_dailyMiddayTimer != null)
                {
                    TimeSpan ts = GetNextDailyTimerIntervalMsec(g_DailyMiddayTimerTime);
                    m_dailyMiddayTimer.Change(ts, TimeSpan.FromMilliseconds(-1.0));
                    Utils.Logger.Info("m_dailyMarketWatcherTimer is scheduled at " + (DateTime.UtcNow + ts).ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture));
                }

                // TODO: if market holiday: it shouldn't process anything either
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                {
                    Utils.Logger.Debug("DailyMiddayTimer_Elapsed(). Weekend is detected. Don't do a thing.");
                    return;
                }

                string emailInnerlStr = String.Empty;
                string phoneCallInnerStr = String.Empty;

                double biduTodayPctChange = GetTodayPctChange("BIDU");
                if (Math.Abs(biduTodayPctChange) >= 0.04)
                {
                    emailInnerlStr += "BIDU price warning: bigger than 4% move. In percentage: " + (biduTodayPctChange * 100).ToString("0.00") + @"%." + Environment.NewLine;
                    phoneCallInnerStr += "the ticker B I D U, ";
                }
                double vxxTodayPctChange = GetTodayPctChange("VXX");
                if (Math.Abs(vxxTodayPctChange) >= 0.06)
                {
                    emailInnerlStr += "VXX price warning: bigger than 6% move. In percentage: " + (vxxTodayPctChange * 100).ToString("0.00") + @"%";
                    phoneCallInnerStr += "the ticker V X X ";
                }



                if (!String.IsNullOrEmpty(emailInnerlStr))
                {
                    new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer Price Warning", Body = emailInnerlStr, IsBodyHtml = false }.Send();
                    var call = new PhoneCall
                    {
                        FromNumber = Caller.Gyantal,
                        ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                        Message = "This is a warning notification from SnifferQuant. There's a large up or down movement in " + phoneCallInnerStr +  " ... I repeat " + phoneCallInnerStr,
                        NRepeatAll = 2
                    };
                    Console.WriteLine(call.MakeTheCall());
                }

                Utils.Logger.Info("DailyMiddayTimer_Elapsed()-4");
            }
            catch (Exception e)
            {
                Utils.Logger.Info("DailyMiddayTimer_Elapsed() in Exception");
                Console.WriteLine("DailyMiddayTimer_Elapsed() in Exception");
                Utils.Logger.Error(e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer: Crash", Body = "Crash. Exception: " + e.Message + ", StackTrace " + e.StackTrace + ", ToString(): " + e.ToString(), IsBodyHtml = false }.Send();
            }
            Utils.Logger.Info("DailyMiddayTimer_Elapsed() END");
        }

        private static double GetTodayPctChange(string p_ticker)
        {
            var biduDelayedPriceCSV = new HttpClient().GetStringAsync("http://download.finance.yahoo.com/d/quotes.csv?s=" + p_ticker + "&f=sl1d1t1c1ohgv&e=.csv").Result;
            Utils.Logger.Info("HttpClient().GetStringAsync returned: " + biduDelayedPriceCSV);
            string[] biduDelayedPriceSplit = biduDelayedPriceCSV.Split(new char[] { ',', ' ' });
            double realTimePrice = Double.Parse(biduDelayedPriceSplit[1]);
            double dailyChange = Double.Parse(biduDelayedPriceSplit[4]);
            double yesterdayClose = realTimePrice - dailyChange;
            double todayPercentChange = realTimePrice / yesterdayClose - 1;
            return todayPercentChange;
        }

        internal void TestSendingEmailAndPhoneCall()
        {
            Console.WriteLine("TestSendingEmail started.");
            Utils.Logger.Info("TestSendingEmail() START");
            DailyMorningTimer_Elapsed(null);

            //string biduDelayedPriceCSV = new WebClient().DownloadString("http://download.finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv");
            //string biduDelayedPriceCSV = new WebClient().DownloadString("http://finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv");
            var biduDelayedPriceCSV = new HttpClient().GetStringAsync("http://finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv").Result;
            string[] biduDelayedPriceSplit = biduDelayedPriceCSV.Split(new char[] { ',', ' ' });
            double realTimePrice = Double.Parse(biduDelayedPriceSplit[1]);

            double dailyChange = Double.Parse(biduDelayedPriceSplit[4]);
            double yesterdayClose = realTimePrice - dailyChange;
            double todayPercentChange = realTimePrice / yesterdayClose - 1;
            Console.WriteLine("BIDU %change: " + (todayPercentChange * 100).ToString("0.00") + @"%");
            Utils.Logger.Info("BIDU %change: " + (todayPercentChange * 100).ToString("0.00") + @"%");
            //if (Math.Abs(todayPercentChange) >= 0.04)
            if (Math.Abs(todayPercentChange) >= 0.00)
            {
                new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "OvermindServer: BIDU price % move", Body = "BIDU price % move. In percentage: " + (todayPercentChange * 100).ToString("0.00") + @"%", IsBodyHtml = false }.Send();

                Console.WriteLine("Email was sent.");
                Utils.Logger.Info("Email was sent.");

                var call = new PhoneCall
                {
                    FromNumber = Caller.Gyantal,
                    ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                    Message = "This is a warning notification from SnifferQuant. There's a large up or down movement in the ticker B I D U. ... I repeat the ticker: B I D U.",
                    NRepeatAll = 2
                };
                // skipped temporarily
                bool phoneCallSuccess = call.MakeTheCall();
                Console.WriteLine("Phonecall success: " + phoneCallSuccess);
                Utils.Logger.Info("Phonecall success: " + phoneCallSuccess);
            }

            Console.WriteLine("TestSendingEmail Finished.");
            Utils.Logger.Info("TestSendingEmail() END");
        }
    }
}
