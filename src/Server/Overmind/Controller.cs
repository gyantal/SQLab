using SqCommon;
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

        readonly DateTime g_DailyMorningTimerTime = new DateTime(2000, 1, 1, 9, 05, 0);      // the date part is not used only the time part. Activate every day 9:05, London Time Zone
        readonly DateTime g_DailyMiddayTimerTime = new DateTime(2000, 1, 1, 16, 45, 0);      // the date part is not used only the time part, Activate every day: 16:45, London Time Zone
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


        static TimeSpan GetNextDailyTimerIntervalMsec(DateTime p_targetDateTimeLT)  // LT: LondonTime
        {
            try
            {
                TimeZoneInfo utcZone = TimeZoneInfo.Utc;
                TimeZoneInfo londonZone = Utils.FindSystemTimeZoneById(TimeZoneId.London);
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
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Error in GetNextDailyTimerIntervalMsec().");
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
                string todayDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string todayMonthAndDayStr = DateTime.UtcNow.ToString("MM-dd", CultureInfo.InvariantCulture);

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

                double? price = GetAmazonProductPrice("https://www.amazon.co.uk/Electronics-Sennheiser-Professional-blocking-gaming-headset-Black/dp/B00JQDOANK/");
                if (price == null || price <= 150.0)
                {
                    new Email
                    {
                        ToAddresses = Utils.Configuration["EmailGyantal"],
                        Subject = "OvermindServer: Amazon price warning.",
                        Body = (price == null) ?
                            $"GetAmazonProductPrice() couldn't obtain current price. Check log file.":
                            $"Time to buy Sennheiser GAME ZERO now. Amazon price dropped from 199.99 to {price}. Go to https://www.amazon.co.uk/Electronics-Sennheiser-Professional-blocking-gaming-headset-Black/dp/B00JQDOANK/ and buy headset now. See '2016-05, Sennheiser buying.txt'.",
                        IsBodyHtml = false
                    }.Send();
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

                string gyantalEmailInnerlStr = String.Empty;
                string gyantalPhoneCallInnerStr = String.Empty;
                string charmatEmailInnerlStr = String.Empty;
                string charmatPhoneCallInnerStr = String.Empty;

                double biduTodayPctChange = GetTodayPctChange("BIDU");
                if (Math.Abs(biduTodayPctChange) >= 0.04)
                {
                    gyantalEmailInnerlStr += "BIDU price warning: bigger than usual move. In percentage: " + (biduTodayPctChange * 100).ToString("0.00") + @"%." + Environment.NewLine;
                    gyantalPhoneCallInnerStr += "the ticker B I D U, ";
                }
                double vxxTodayPctChange = GetTodayPctChange("VXX");
                if (Math.Abs(vxxTodayPctChange) >= 0.06)
                {
                    gyantalEmailInnerlStr += "VXX price warning: bigger than usual move. In percentage: " + (vxxTodayPctChange * 100).ToString("0.00") + @"%";
                    gyantalPhoneCallInnerStr += "the ticker V X X ";
                    charmatEmailInnerlStr += "VXX price warning: bigger than usual move. In percentage: " + (vxxTodayPctChange * 100).ToString("0.00") + @"%";
                    charmatPhoneCallInnerStr += "the ticker V X X ";
                }

                double fbTodayPctChange = GetTodayPctChange("FB");
                if (Math.Abs(fbTodayPctChange) >= 0.04)
                {
                    charmatEmailInnerlStr += "Facebook price warning: bigger than usual move. In percentage: " + (fbTodayPctChange * 100).ToString("0.00") + @"%." + Environment.NewLine;
                    charmatPhoneCallInnerStr += "the ticker Facebook, ";
                }

                double amznTodayPctChange = GetTodayPctChange("AMZN");
                if (Math.Abs(amznTodayPctChange) >= 0.04)
                {
                    charmatEmailInnerlStr += "Amazon price warning: bigger than usual move. In percentage: " + (amznTodayPctChange * 100).ToString("0.00") + @"%." + Environment.NewLine;
                    charmatPhoneCallInnerStr += "the ticker Amazon, ";
                }

                double googleTodayPctChange = GetTodayPctChange("FB");
                if (Math.Abs(googleTodayPctChange) >= 0.04)
                {
                    charmatEmailInnerlStr += "Google price warning: bigger than usual move. In percentage: " + (googleTodayPctChange * 100).ToString("0.00") + @"%." + Environment.NewLine;
                    charmatPhoneCallInnerStr += "the ticker Google, ";
                }

                if (!String.IsNullOrEmpty(gyantalEmailInnerlStr))
                {
                    new Email { ToAddresses = Utils.Configuration["EmailGyantal"], Subject = "SnifferQuant Price Warning", Body = gyantalEmailInnerlStr, IsBodyHtml = false }.Send();
                    var call = new PhoneCall
                    {
                        FromNumber = Caller.Gyantal,
                        ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                        Message = "This is a warning notification from SnifferQuant. There's a large up or down movement in " + gyantalPhoneCallInnerStr +  " ... I repeat " + gyantalPhoneCallInnerStr,
                        NRepeatAll = 2
                    };
                    Console.WriteLine(call.MakeTheCall());
                }

                if (!String.IsNullOrEmpty(charmatEmailInnerlStr))
                {
                    new Email { ToAddresses = Utils.Configuration["EmailCharmat0"], Subject = "SnifferQuant Price Warning", Body = charmatEmailInnerlStr, IsBodyHtml = false }.Send();
                    var call = new PhoneCall
                    {
                        FromNumber = Caller.Gyantal,
                        ToNumber = PhoneCall.PhoneNumbers[Caller.Charmat0],
                        Message = "This is a warning notification from SnifferQuant. There's a large up or down movement in " + charmatPhoneCallInnerStr + " ... I repeat " + charmatPhoneCallInnerStr,
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

        // Amazon UK price history can be checked in uk.camelcamelcamel.com, for example: http://uk.camelcamelcamel.com/Sennheiser-Professional-blocking-gaming-headset-Black/product/B00JQDOANK
        private static double? GetAmazonProductPrice(string p_amazonProductUrl)
        {
            string errorMessage = String.Empty;
            double price = 0.0;
            var webpage = new HttpClient().GetStringAsync(p_amazonProductUrl).Result;
            Utils.Logger.Info("HttpClient().GetStringAsync returned: " + ((webpage.Length > 100) ? webpage.Substring(0, 100) : webpage));

            // <span id="priceblock_ourprice" class="a-size-medium a-color-price">£199.95</span>
            string searchStr = @"id=""priceblock_ourprice"" class=""a-size-medium a-color-price"">";
            int startInd = webpage.IndexOf(searchStr);
            if (startInd == -1)
            {   // it is expected (not an exception), that sometimes Amazon changes its website, so we will fail. User will be notified.
                Utils.Logger.Info($"searchString '{searchStr}' was not found.");
                return null;
            }
            int endInd = webpage.IndexOf('<', startInd + searchStr.Length);
            if (endInd == -1)
            {   // it is expected (not an exception), that sometimes Amazon changes its website, so we will fail. User will be notified.
                Utils.Logger.Info($"'<' after searchString '{searchStr}' was not found.");
                return null;
            }
            string priceStr = webpage.Substring(startInd + searchStr.Length + 1, endInd - (startInd + searchStr.Length + 1));
            if (!Double.TryParse(priceStr, out price))
            {
                Utils.Logger.Info($"{priceStr} cannot be parsed to Double.");
                return null;
            }
            return price;
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

        internal void TestCheckingAmazonPrice()
        {
            Utils.Logger.Info("TestCheckingAmazonPrice() START");
            double? price = GetAmazonProductPrice("https://www.amazon.co.uk/Electronics-Sennheiser-Professional-blocking-gaming-headset-Black/dp/B00JQDOANK/");
            string priceStr = (price == null) ? "null" : price.ToString();
            Console.WriteLine($"Amazon price is: {priceStr}. Sending email.");

            new Email
            {
                ToAddresses = Utils.Configuration["EmailGyantal"],
                Subject = "OvermindServer: Amazon price warning: Time to buy Sennheiser GAME ZERO now",
                Body = $"Time to buy Sennheiser GAME ZERO now. Amazon price dropped from 199.99 to {priceStr}. Go to https://www.amazon.co.uk/Electronics-Sennheiser-Professional-blocking-gaming-headset-Black/dp/B00JQDOANK/ and buy headset now. See '2016-05, Sennheiser buying.txt'. ",
                IsBodyHtml = false
            }.Send();

        }
    }
}
