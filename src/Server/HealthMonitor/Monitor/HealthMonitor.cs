using SqCommon;
using System;
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

        public bool IsProcessingVbGatewaysManagerMessagesEnabled { get; set; } = true;

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
        Timer m_heartbeatTimer = null;
        Timer m_checkWebsitesAndKeepAliveTimer = null;
        Timer m_checkAmazonAwsInstancesTimer = null;

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
            

            //            this is from old Azure Based HealthMonitor
            //            m_dailyMarketOpenTimer = new System.Threading.Timer(OnDailyMarketOpenTimerCallBack);
            //            SetupNotRepeatingDailyMarketOpenTimer();

            //            m_dailyReportTimer = new System.Threading.Timer(SendDailySummaryReportEmail);
            //            SetupNotRepeatingDailyReportTimer();    // usually MarketOpenTimer re-set it, but if App is started intraday, this will set the timer

            //            // 2. Start Watcher Timers
            //            m_rtpsTimer = new System.Threading.Timer(OnRtpsTimerCallBack, null, TimeSpan.FromMinutes(0.6), TimeSpan.FromMinutes(cRtpsTimerFrequencyMinutes));
            //#if DEBUG_LOCAL_DEVELOPMENT
            //            //OnRtpsTimerCallBack(null);
            //            //OnDailyMarketOpenTimerCallBack(null);
            //#endif
        }

        // at graceful shutdown, it is called
        public void Exit()
        {
            m_mainThreadExitsResetEvent.Set();
            //PersistedState.Save();
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
    }
}
