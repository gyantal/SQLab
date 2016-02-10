using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
//using System.Timers;
//using Timer = System.Timers.Timer;
using Timer = System.Threading.Timer;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using NLog;
using Microsoft.Extensions.Configuration;

// System.Timers.Timer is geared towards multithreaded applications and is therefore thread-safe via its SynchronizationObject property, whereas System.Threading.Timer is ironically not thread-safe out-of-the-box.
// but we dont' have it in coreClr, so use System.Threading.Timers
namespace Overmind
{
    public enum Platform
    {
        Windows,
        Linux,
        Mac
    }

    public class PeriodicTask
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    action();
                    //  Agy: only do it once now
                    break;
                }
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }

    // send Calendar emails (about Birthday (orsi), Pay-Rent, Accountant), 
    // it can do other things like: watch BIDU price and send SMS if it fell more than 5%;
    class Controller
    {
        static public Controller g_controller = new Controller();
        public IConfigurationRoot g_configuration = null;
        Timer m_dailyTimer = null;
        Timer m_dailyMarketWatcherTimer = null;

        readonly DateTime g_DailyTimerTime = new DateTime(2000, 1, 1, 9, 05, 0);      // the date part is not used only the time part. Activate every day 9:05
        readonly DateTime g_DailyMarketWatcherTimerTime = new DateTime(2000, 1, 1, 18, 0, 0);      // the date part is not used only the time part, Activate every day: 18:00

        Thread gThreadDailyTimer = null;
        Thread gThreadDailyMarketWatchTimer = null;
        ManualResetEventSlim gMainThreadExitsResetEvent = null;

        internal void Start(IConfigurationRoot p_configuration)
        {
            g_configuration = p_configuration;
            gMainThreadExitsResetEvent = new ManualResetEventSlim(false);
            ScheduleDailyTimers();
        }

        internal void Exit()
        {
            gMainThreadExitsResetEvent.Set();
        }


        // see discussion here in CoreCLR (they are not ready) https://github.com/dotnet/corefx/issues/1017
        public static Platform RunningPlatform()
        {
            switch (Environment.NewLine)
            {
                case "\n":
                        return Platform.Linux;

                case "\r\n":
                    return Platform.Windows;

                default:
                    throw new Exception("RunningPlatform() not recognized");
            }
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


        static double GetNextDailyTimerIntervalMsec(DateTime p_targetDateTimeLT)  // LT: LondonTime
        {
            //http://www.mcnearney.net/blog/windows-timezoneinfo-olson-mapping/
            //http://unicode.org/repos/cldr/trunk/common/supplemental/windowsZones.xml
            //string londonZoneId = "GMT Standard Time";      // Linux: "Europe/London"

            string londonZoneId = String.Empty;
            if (RunningPlatform() == Platform.Windows)
                londonZoneId = "GMT Standard Time";
            else
                londonZoneId = "Europe/London";

            //string utcZoneId = String.Empty;
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
                return scheduleStartTimerInMsec;
            }
            
            catch (InvalidTimeZoneException)
            {
                Console.WriteLine("Registry data on the {0} zone has been corrupted.", londonZoneId);
            }

            return 24 * 60 * 60 * 1000;  // let it be the next 24 hour
        }

        //// This method that will be called when the thread is started
        public void NewThreadDailyTimer()
        {
            Console.WriteLine("NewThreadDailyTimer()");
            Program.gLogger.Info("NewThreadDailyTimer()");
            while (!gMainThreadExitsResetEvent.IsSet)
            {
                //DateTime m_DailyTimerTimeTestLT = DateTime.UtcNow.AddSeconds(20);     // Date part is not used, only the time part
                //double mSecUntilDailyTimer = GetNextDailyTimerIntervalMsec(m_DailyTimerTimeTestLT);
                double mSecUntilDailyTimer = GetNextDailyTimerIntervalMsec(g_DailyTimerTime);
                Program.gLogger.Info("wait for secUntilDailyTimer: " + mSecUntilDailyTimer / 1000.0);

                gMainThreadExitsResetEvent.Wait(TimeSpan.FromMilliseconds(mSecUntilDailyTimer));
                //Thread.Sleep(TimeSpan.FromMilliseconds(mSecUntilDailyTimer));
                Program.gLogger.Info("NewThreadDailyTimer.Sleep() END");
                if (!gMainThreadExitsResetEvent.IsSet)
                    DailyTimer_Elapsed(null);
            }

            //while (!cancellationToken.IsCancellationRequested)
            //while (!cancellationToken.IsCancellationRequested)
            //{
            //    await Task.Delay(period, cancellationToken);

            //    if (!cancellationToken.IsCancellationRequested)
            //    {
            //        action();
            //        //  Agy: only do it once now
            //        break;
            //    }
            //}
        }

        public void NewThreadDailyMarketWatchTimer()
        {
            Console.WriteLine("NewThreadDailyMarketWatchTimer()");
            Program.gLogger.Info("NewThreadDailyMarketWatchTimer()");
            while (!gMainThreadExitsResetEvent.IsSet)
            {
                double mSecUntilDailyMarketWatcherTimer = GetNextDailyTimerIntervalMsec(g_DailyMarketWatcherTimerTime);
                Program.gLogger.Info("wait for secUntilDailyMarketWatchTimer: " + mSecUntilDailyMarketWatcherTimer / 1000.0);
                //Thread.Sleep(TimeSpan.FromMilliseconds(mSecUntilDailyMarketWatcherTimer));
                gMainThreadExitsResetEvent.Wait(TimeSpan.FromMilliseconds(mSecUntilDailyMarketWatcherTimer));
                Program.gLogger.Info("NewThreadDailyMarketWatchTimer.Sleep() END");
                if (!gMainThreadExitsResetEvent.IsSet)
                    DailyMarketWatchTimer_Elapsed(null);
            }
        }

        


        // Linux Timer problem (2016-01-22, CoreCLR): it seems: Timer is called only after Main Exits. Until the main thread is not finished the other threads don't run or what.
        // even the Console message is only written after we exit the program.
        // 1. new Thread().Start() :  only this works on Linux properly
        // 2. new System.Threading.Timer(): this is only called when program exits
        // 3. PeriodicTask.Run((): only called when program exits
        private void ScheduleDailyTimers()
        {
            // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Threading/Timer.cs/
            try
            {
                Program.gLogger.Info("ScheduleDailyTimers() BEGIN");

                gThreadDailyTimer = new Thread(new ThreadStart(this.NewThreadDailyTimer));
                gThreadDailyTimer.Start();

                gThreadDailyMarketWatchTimer = new Thread(new ThreadStart(this.NewThreadDailyMarketWatchTimer));
                gThreadDailyMarketWatchTimer.Start();



                //DateTime m_DailyTimerTimeTestLT = DateTime.UtcNow.AddSeconds(20);     // Date part is not used, only the time part
                //// dueTime: Specify negative one (-1) milliseconds to prevent the timer from starting
                ////double mSecUntilDailyTimer = GetNextDailyTimerIntervalMsec(g_DailyTimerTime);
                //double mSecUntilDailyTimer = GetNextDailyTimerIntervalMsec(m_DailyTimerTimeTestLT);
                //Program.gLogger.Info("secUntilDailyTimer: " + mSecUntilDailyTimer / 1000.0);
                ////m_dailyTimer = new System.Threading.Timer(new TimerCallback(DailyTimer_Elapsed), null, TimeSpan.FromMilliseconds(mSecUntilDailyTimer), TimeSpan.FromMilliseconds(-1.0));
                //m_dailyTimer = new System.Threading.Timer(new TimerCallback(DailyTimer_Elapsed), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1.0));

                //PeriodicTask.Run(() =>
                //{
                //    Program.gLogger.Info("PeriodicTask.Run(()");
                //    Console.WriteLine("PeriodicTask.Run(()");
                //}, TimeSpan.FromMilliseconds(mSecUntilDailyTimer));




                //m_dailyTimer = new Timer();
                //m_dailyTimer.Elapsed += new ElapsedEventHandler(DailyTimer_Elapsed);
                //m_dailyTimer.AutoReset = true;      // so it will repeat
                //m_dailyTimer.Interval = GetNextDailyTimerIntervalMsec(g_DailyTimerTime);
                //m_dailyTimer.Enabled = true;

                //double mSecUntilDailyMarketWatcherTimer = GetNextDailyTimerIntervalMsec(g_DailyMarketWatcherTimerTime);
                //Program.gLogger.Info("secUntilDailyMarketWatcherTimer: " + mSecUntilDailyMarketWatcherTimer / 1000.0);
                //m_dailyMarketWatcherTimer = new System.Threading.Timer(new TimerCallback(DailyMarketWatchTimer_Elapsed), null, TimeSpan.FromMilliseconds(mSecUntilDailyMarketWatcherTimer), TimeSpan.FromMilliseconds(-1.0));

                //m_dailyMarketWatcherTimer = new Timer();
                //m_dailyMarketWatcherTimer.Elapsed += new ElapsedEventHandler(DailyMarketWatchTimer_Elapsed);
                //m_dailyMarketWatcherTimer.AutoReset = true;      // so it will repeat
                //m_dailyMarketWatcherTimer.Interval = GetNextDailyTimerIntervalMsec(g_DailyMarketWatcherTimerTime);
                //m_dailyMarketWatcherTimer.Enabled = true;
                //                Console.WriteLine("The date and time are {0} UTC.", TimeZoneInfo.ConvertTimeToUtc(easternTime, easternZone));
            }
            catch (Exception e)
            {
                Program.gLogger.Info("ScheduleDailyTimers() Exception: " + e.Message);
                Console.WriteLine(e.Message, "Exception!");
            }
            Program.gLogger.Info("ScheduleDailyTimers() END");
        }



        //public void DailyTimer_Elapsed(object p_sender, ElapsedEventArgs p_e)
        public void DailyTimer_Elapsed(object p_sender)
        {
            Program.gLogger.Info("DailyTimer_Elapsed() BEGIN");
            Console.WriteLine("DailyTimer_Elapsed() BEGIN");
            try
            {
                if (m_dailyTimer != null)
                    m_dailyTimer.Change(TimeSpan.FromMilliseconds(GetNextDailyTimerIntervalMsec(g_DailyTimerTime)), TimeSpan.FromMilliseconds(-1.0));

                //m_dailyTimer.Interval = GetNextDailyTimerIntervalMsec(g_DailyTimerTime);

                DateTime utcToday = DateTime.UtcNow.Date;

                string todayDateStr = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
                string todayMonthAndDayStr = DateTime.UtcNow.ToString("MM'-'dd", CultureInfo.InvariantCulture);

                Trace.WriteLine(@"DailyTimer_Elapsed(), UtcTime: " + DateTime.UtcNow.ToString("yyyy'-'MM'-'dd H:mm:ss", CultureInfo.InvariantCulture));

                if (todayMonthAndDayStr == "10-05")        // Orsi's birthday
                {
                    new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: Orsi's birthday", Body = "Orsi's birthday is on 1976-10-09.", IsBodyHtml = false }.Send(true);
                }

                Program.gLogger.Info("DailyTimer_Elapsed(): Checking first day of the month");
                if (DateTime.UtcNow.AddDays(0).Day == 1)
                {
                    // Balazs Lukucz asked me that never send salaries on 30th or 31st of previous month. 
                    // So I will report to Barbara only on 1st day of every month, and maybe they got salaries later. 
                    // And this has an advantage that as I don't send the holidays report earlier, if they forget to tell me their 'last minute' day-offs, it is not reported to Barbara too early.
                    // So less headache overall.
                    new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: send holidays, bank report to accountant", Body = "Send holidays, bank report to accountant. In 3 days, it is the 1st day of the month. ", IsBodyHtml = false }.Send(true);
                }
            }
            catch (Exception e)
            {
                Program.gLogger.Error(e.Message);
                new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: Crash", Body = "Crash. Exception: " + e.Message + ", StackTrace " + e.StackTrace + ", ToString(): " + e.ToString(), IsBodyHtml = false }.Send(true);
            }

            Program.gLogger.Info("DailyTimer_Elapsed() END");
        }

        //public void DailyMarketWatchTimer_Elapsed(object p_sender, ElapsedEventArgs p_e)
        public void DailyMarketWatchTimer_Elapsed(object p_sender)
        {
            Program.gLogger.Info("DailyMarketWatchTimer_Elapsed() BEGIN");
            Console.WriteLine("DailyMarketWatchTimer_Elapsed() BEGIN");
            try
            {
                if (m_dailyMarketWatcherTimer != null)
                    m_dailyMarketWatcherTimer.Change(TimeSpan.FromMilliseconds(GetNextDailyTimerIntervalMsec(g_DailyMarketWatcherTimerTime)), TimeSpan.FromMilliseconds(-1.0));
                //m_dailyMarketWatcherTimer.Interval = GetNextDailyTimerIntervalMsec(g_DailyMarketWatcherTimerTime);

                var biduDelayedPriceCSV = new HttpClient().GetStringAsync("http://download.finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv").Result;


                Console.WriteLine("HttpClient().GetStringAsync returned: " + biduDelayedPriceCSV.Length);
                Console.WriteLine("Downloaded string: " + biduDelayedPriceCSV);

                Program.gLogger.Info("HttpClient().GetStringAsync returned: " + biduDelayedPriceCSV.Length);
                Program.gLogger.Info("Downloaded string: " + biduDelayedPriceCSV);

                //string biduDelayedPriceCSV = new WebClient().DownloadString("http://download.finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv");
                string[] biduDelayedPriceSplit = biduDelayedPriceCSV.Split(new char[] { ',', ' ' });
                double realTimePrice = Double.Parse(biduDelayedPriceSplit[1]);

                Program.gLogger.Info("DailyMarketWatchTimer_Elapsed()-2");
                double dailyChange = Double.Parse(biduDelayedPriceSplit[4]);
                double yesterdayClose = realTimePrice - dailyChange;
                double todayPercentChange = realTimePrice / yesterdayClose - 1;
                Program.gLogger.Info("DailyMarketWatchTimer_Elapsed()-3. TodayPctChange: " + todayPercentChange);
                if (Math.Abs(todayPercentChange) >= 0.04)
                {
                    new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: BIDU price warning: bigger than 5% move", Body = "BIDU price warning: bigger than 5% move. In percentage: " + (todayPercentChange * 100).ToString("0.00") + @"%", IsBodyHtml = false }.Send(true);

                    var call = new PhoneCall
                    {
                        FromNumber = Caller.Gyantal,
                        ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                        Message = "This is a warning notification from SnifferQuant. There's a large up or down movement in the ticker B I D U. ... I repeat the ticker: B I D U.",
                        NRepeatAll = 2
                    };
                    Console.WriteLine(call.MakeTheCall());
                }

                Program.gLogger.Info("DailyMarketWatchTimer_Elapsed()-4");
            }
            catch (Exception e)
            {
                Program.gLogger.Info("DailyMarketWatchTimer_Elapsed() in Exception");
                Console.WriteLine("DailyMarketWatchTimer_Elapsed() in Exception");
                Program.gLogger.Error(e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: Crash", Body = "Crash. Exception: " + e.Message + ", StackTrace " + e.StackTrace + ", ToString(): " + e.ToString(), IsBodyHtml = false }.Send(true);
            }
            Program.gLogger.Info("DailyMarketWatchTimer_Elapsed() END");
        }





        internal void TestSendingEmailAndPhoneCall()
        {
            Console.WriteLine("TestSendingEmail started.");
            Program.gLogger.Info("TestSendingEmail() START");
            DailyTimer_Elapsed(null);

            //string biduDelayedPriceCSV = new WebClient().DownloadString("http://download.finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv");
            //string biduDelayedPriceCSV = new WebClient().DownloadString("http://finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv");
            var biduDelayedPriceCSV = new HttpClient().GetStringAsync("http://finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv").Result;
            string[] biduDelayedPriceSplit = biduDelayedPriceCSV.Split(new char[] { ',', ' ' });
            double realTimePrice = Double.Parse(biduDelayedPriceSplit[1]);

            double dailyChange = Double.Parse(biduDelayedPriceSplit[4]);
            double yesterdayClose = realTimePrice - dailyChange;
            double todayPercentChange = realTimePrice / yesterdayClose - 1;
            Console.WriteLine("BIDU %change: " + (todayPercentChange * 100).ToString("0.00") + @"%");
            Program.gLogger.Info("BIDU %change: " + (todayPercentChange * 100).ToString("0.00") + @"%");
            //if (Math.Abs(todayPercentChange) >= 0.04)
            if (Math.Abs(todayPercentChange) >= 0.00)
            {
                new HQEmail { ToAddresses = g_configuration.GetSection("EmailGyantal").Value, Subject = "OvermindServer: BIDU price % move", Body = "BIDU price % move. In percentage: " + (todayPercentChange * 100).ToString("0.00") + @"%", IsBodyHtml = false }.Send(true);

                Console.WriteLine("Email was sent.");
                Program.gLogger.Info("Email was sent.");

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
                Program.gLogger.Info("Phonecall success: " + phoneCallSuccess);
            }

            Console.WriteLine("TestSendingEmail Finished.");
            Program.gLogger.Info("TestSendingEmail() END");
        }
    }
}
