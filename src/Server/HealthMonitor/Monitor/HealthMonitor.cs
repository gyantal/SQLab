using SqCommon;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public class SavedState : PersistedState   // data to persist between restarts of the crawler process
    {
        public bool IsDailyEmailReportEnabled { get; set; } = true;
        public bool IsRealtimePriceServiceTimerEnabled { get; set; } = true;
        public bool IsVBrokerTimerEnabled { get; set; } = true;
        public bool IsVBrokerOldTimerEnabled { get; set; } = true;

        public bool IsProcessingVirtualBrokerMessagesEnabled { get; set; } = true;

        public bool IsProcessingVBrokerMessagesEnabled { get; set; } = true;

        public bool IsSendErrorEmailAtGracefulShutdown { get; set; } = true;   // switch this off before deployment, and switch it on after deployment; make functionality on the WebSite

        public int nFailedDownload_YahooFinanceMain { get; set; } = 0;
    }

    public partial class HealthMonitor
    {
        static public HealthMonitor g_healthMonitor = new HealthMonitor();

        SavedState m_persistedState = null;
        ManualResetEventSlim m_mainThreadExitsResetEvent = null;

        //Your timer object goes out of scope and gets erased by Garbage Collector after some time, which stops callbacks from firing. Save reference to it in a member of class.
        long m_nHeartbeat = 0;
        long m_nRtpsTimerCalled = 0;     // Real Time Price Service

        Timer m_heartbeatTimer = null;
        Timer m_checkWebsitesAndKeepAliveTimer = null;
        Timer m_checkAmazonAwsInstancesTimer = null;
        Timer m_rtpsTimer = null;

        Timer m_dailyMarketOpenTimer = null;   // called at 9:30 ET, may be late or early by 1 hour, if there is a DayLightSaving day
        Timer m_dailyReportTimer = null;   // called at the market close, because this is set by the MarketOpen Timer, it always use the current day proper DayLightSaving settings. Will be correct.

        const int cRtpsTimerFrequencyMinutes = 10;
        const int cHeartbeatTimerFrequencyMinutes = 5;

        public SavedState PersistedState
        {
            get
            {
                return m_persistedState;
            }

            set
            {
                m_persistedState = value;
            }
        }

        public void Init()
        {
            Utils.Logger.Info("****HealthMonitor:Init()");

            // 1. Get the Current Parameter state from the AzureTable (in case this WebJob was unloaded and restarted)
            //PersistedState = new SavedState().CreateOrOpenEx();
            PersistedState = new SavedState();
            m_mainThreadExitsResetEvent = new ManualResetEventSlim(false);
            ScheduleTimers();

            StartTcpMessageListenerThreads();

            InitVbScheduler();
        }

        // at graceful shutdown, it is called
        public void Exit()
        {
            m_mainThreadExitsResetEvent.Set();
            //PersistedState.Save();

            ExitVbScheduler();
            StopTcpMessageListener();
        }

        private void ScheduleTimers()
        {
            try
            {
                Utils.Logger.Info("ScheduleDailyTimers() BEGIN");
                // "if I don't hit the site for 10-15 minutes, it goes to sleep"; "default configuration of an IIS Application pool that is set to have an idle-timeout of 20 minutes"
                m_checkWebsitesAndKeepAliveTimer = new System.Threading.Timer(new TimerCallback(CheckWebsitesAndKeepAliveTimer_Elapsed), null, TimeSpan.Zero, TimeSpan.FromMinutes(9.0));

                m_checkAmazonAwsInstancesTimer = new System.Threading.Timer(new TimerCallback(CheckAmazonAwsInstances_Elapsed), null, TimeSpan.Zero, TimeSpan.FromMinutes(60.0));

                m_rtpsTimer = new System.Threading.Timer(new TimerCallback(RtpsTimer_Elapsed), null, TimeSpan.FromSeconds(40), TimeSpan.FromMinutes(cRtpsTimerFrequencyMinutes));

                m_heartbeatTimer = new System.Threading.Timer((e) =>    // Heartbeat log is useful to find out when VM was shut down, or when the App crashed
                {
                    Utils.Logger.Info(String.Format("**m_nHeartbeat: {0} (at every {1} minutes)", m_nHeartbeat, cHeartbeatTimerFrequencyMinutes));
                    m_nHeartbeat++;
                }, null, TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(cHeartbeatTimerFrequencyMinutes));

                m_dailyMarketOpenTimer = new System.Threading.Timer(new TimerCallback(DailyMarketOpenTimer_Elapsed), null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
                m_dailyReportTimer = new System.Threading.Timer(new TimerCallback(DailyReportTimer_Elapsed), null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
                DailyMarketOpenTimer_Elapsed(null);    // usually MarketOpenTimer re-set m_dailyReportTimer at market open, but if App is started intraday, this will set both timer
            }
            catch (Exception e)
            {
                Utils.Logger.Info(e, "ScheduleDailyTimers() Exception.");
            }
            Utils.Logger.Info("ScheduleDailyTimers() END");
        }

        // Any solution that depends on System.Timers.Timer, System.Threading.Timer, or any of the other timers 
        // that currently exist in the.NET Framework will fail in the face of Daylight Saving time changes.  (if you want to time for Local time, but I want to time it for UtcTime)
        // If you use any of those timers, you will have to do some polling.
        // That will fail in the face of Daylight Saving time changes. It could execute an hour late or an hour early. 
        // but as we want to set up a Timer using UtcTime, it will not fail, because we exactly want utcTime timing. 
        // And the problem that time in USA local time, sometimes it is one hour early, sometimes one hour late, we don't care.
        private void SetupNotRepeatingDailyMarketOpenTimer()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime openInET = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 9, 30, 0);  // USA market always open at 9:30 ET
            DateTime requiredUtcTime = TimeZoneInfo.ConvertTime(openInET, SqCommon.Utils.FindSystemTimeZoneById(TimeZoneId.EST), TimeZoneInfo.Utc);

            // trigger the event at 15:00 in UTC; USA market opens at 14:30 in general or sometimes at 13:30 (only in 2 weeks)
            //DateTime requiredUtcTime = DateTime.UtcNow.Date.AddHours(15).AddMinutes(00); // that is today at 15:00
            if (utcNow > requiredUtcTime)
            {
                requiredUtcTime = requiredUtcTime.AddDays(1);
            }
            Utils.Logger.Info("SetupNotRepeatingDailyMarketOpenTimer for UTC time as: " + requiredUtcTime);

            // first parameter is the start time Interval and the second parameter is the interval
            // Timeout.Infinite means do not repeat the interval, only start the timer
            m_dailyMarketOpenTimer.Change(requiredUtcTime - utcNow, Timeout.InfiniteTimeSpan);
        }

        private void SetupNotRepeatingDailyReportTimer()
        {
            bool isMarketTradingDay;
            DateTime openTimeUtc, closeTimeUtc;
            bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(DateTime.UtcNow, out isMarketTradingDay, out openTimeUtc, out closeTimeUtc, TimeSpan.FromDays(3));
            if (!isTradingHoursOK)
            {
                Utils.Logger.Warn("DetermineUsaMarketTradingHours() was not ok.");
            }
            else
            {
                DateTime dailyReportStartTime = closeTimeUtc.AddMinutes(1.0);   // run it 1 minute after close, when VBrokers finished
                if (isMarketTradingDay && (DateTime.UtcNow.AddSeconds(10) < dailyReportStartTime)) // if it is only 10 seconds or less until close, don't start it.
                {
                    Utils.Logger.Info("SetupNotRepeatingDailyReportTimer for UTC time as: " + (DateTime)dailyReportStartTime);
                    m_dailyReportTimer.Change((DateTime)dailyReportStartTime - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    Utils.Logger.Info("SetupNotRepeatingDailyReportTimer not set. It is not a market day or market is closed already. ReportTimer will be set by MarketOpenTimer on the next trading day.");
                }
            }
        }

        private void DailyMarketOpenTimer_Elapsed(object p_stateObj)   // this is called at 14:30 UTC every day; during that day DayLightSaving setting will not change
        {
            Utils.Logger.Info("DailyMarketOpenTimer_Elapsed()");

            SetupNotRepeatingDailyReportTimer();
            SetupNotRepeatingDailyMarketOpenTimer();
        }

        // called at the market close, because this is set by the MarketOpen Timer, it always use the current day proper DayLightSaving settings. Will be correct.
        public void DailyReportTimer_Elapsed(object p_stateObj)
        {
            Utils.Logger.Info("DailyReportTimer_Elapsed()");
            StringBuilder sb = DailySummaryReport(true);

            new Email
            {
                ToAddresses = Utils.Configuration["EmailGyantal"],
                Subject = "HealthMonitor Daily Report",
                Body = sb.ToString(),
                IsBodyHtml = true
            }.Send();

        }

        string m_dailyReportEmailStr1 =
@"<!DOCTYPE html>
<html>
<head>
    <style>
.sqNormalText {
    font-size: 125%;
}
.sqImportantOK {
    font-size: 140%;
    color: #10ff10;
    font-weight: bold;
}
.sqImportantError {
    font-size: 140%;
    color: #FF2020;
    font-weight: bold;
}
    </style>
</head>
<body class=""sqNormalText"">
    <strong>Realtime Price</strong> Service: ";

        string m_dailyReportEmailStr2 =
@"</body>
</html>";

        // just summarize the whole today; don't go into too much detail. Result is sent by email
        public StringBuilder DailySummaryReport(bool p_isHtml)
        {
            Utils.Logger.Info("DailySummaryReport()");
            if (!PersistedState.IsDailyEmailReportEnabled)
                return new StringBuilder();

            StringBuilder sb = new StringBuilder((p_isHtml) ? m_dailyReportEmailStr1 : "Realtime Price:");

            bool? rtpsLastDownloadAccepted = null;
            var rtpsLastDownloadsSnapshot = m_rtpsLastDownloads.ToArray(); // we have to make a snapshot anyway, so the Timer thread can write it while we itarate
            if (rtpsLastDownloadsSnapshot.Length > 0)  // we need the last element
            {
                rtpsLastDownloadAccepted = rtpsLastDownloadsSnapshot[rtpsLastDownloadsSnapshot.Length - 1].Item2;
            }

            if (rtpsLastDownloadAccepted == null)
                sb.Append((p_isHtml) ? @"<span class=""sqImportantError""> ?</span>" : $" ?{Environment.NewLine}");
            else if ((bool)rtpsLastDownloadAccepted)
                sb.Append((p_isHtml) ? @"<span class=""sqImportantOK""> OK</span>" : $" OK{Environment.NewLine}");
            else
                sb.Append((p_isHtml) ? @"<span class=""sqImportantError""> ERROR</span>" : $" ERROR{Environment.NewLine}");


            sb.Append((p_isHtml) ? @"<br/><strong>VBroker</strong>:" : "VBroker:"); // try to be concise in the email, so user has to spend only 1 second to evaluate: OK or ERROR. (don't put extra info, because it takes too long to evaluate)
            DateTime utcStartOfToday = DateTime.UtcNow.Date;
            bool wasAllOkToday = true;
            int nReportsToday = 0;
            StringBuilder sb2 = new StringBuilder();
            lock (m_VbReport)
            {
                for (int i = 0; i < m_VbReport.Count; i++)
                {
                    if (m_VbReport[i].Item1 > utcStartOfToday)
                    {
                        wasAllOkToday &= m_VbReport[i].Item2;
                        string strategyName = String.Empty;
                        int strategyNameInd1 = m_VbReport[i].Item3.ParamStr.IndexOf("BrokerTask ");  // "BrokerTask UberVXX was OK" or "had ERROR"
                        if (strategyNameInd1 != -1)
                        {
                            int strategyNameInd2 = strategyNameInd1 + "BrokerTask ".Length;
                            int strategyNameInd3 = m_VbReport[i].Item3.ParamStr.IndexOf(" ", strategyNameInd2);
                            if (strategyNameInd3 != -1)
                            {
                                strategyName = m_VbReport[i].Item3.ParamStr.Substring(strategyNameInd2, strategyNameInd3 - strategyNameInd2);
                            }
                        }

                        nReportsToday++;
                        sb2.Append("    - " + m_VbReport[i].Item1.ToString("HH:mm:ss")
                            + (String.IsNullOrEmpty(strategyName) ? "" : " (" + strategyName + ")") +
                            ": " + ((m_VbReport[i].Item2) ? "OK" : "ERROR") +
                            ((p_isHtml) ? "<br/>" : Environment.NewLine));
                        

                    }
                }

                // we don't want to leak memory, and accumulate 200 daily records, so if the messages are 30 day or older remove them from the List
                DateTime day30DaysEarlier = utcStartOfToday.AddDays(-30);
                var v2 = m_VbReport.Where(r => r.Item1 > day30DaysEarlier).OrderBy(r => r.Item1).ToList();
                m_VbReport = v2;
            }

            if (nReportsToday == 0)
                sb.Append((p_isHtml) ? @"<span class=""sqImportantError""> ERROR</span>: No report today from VBroker. Maybe it has crashed or VM is down." : "No report today from VBroker. Maybe it has crashed or VM is down.");
            else if (wasAllOkToday) // there was a report today from VBroker AND it was allOk
                sb.Append((p_isHtml) ? @"<span class=""sqImportantOK""> OK</span>" : " OK");
            else
                sb.Append((p_isHtml) ? @"<span class=""sqImportantError""> ERROR</span>: Errors occured today. See logs." : " ERROR");

            if (sb2.Length > 0)
            {
                sb.Append((p_isHtml) ? "<br/>" : Environment.NewLine);
                sb.Append(sb2);
            }

            if (p_isHtml)
                sb.Append(m_dailyReportEmailStr2);

            return sb;
        }
    }
}
