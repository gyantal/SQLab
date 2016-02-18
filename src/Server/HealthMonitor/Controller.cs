using SQCommon;
using System;
using System.Threading;     // this is the only timer available under DotNetCore

namespace HealthMonitor
{
    public partial class Controller
    {
        static public Controller g_controller = new Controller();

        ManualResetEventSlim m_mainThreadExitsResetEvent = null;

        //Your timer object goes out of scope and gets erased by Garbage Collector after some time, which stops callbacks from firing. Save reference to it in a member of class.
        long m_nHeartbeat = 0;
        Timer m_heartbeatTimer = null;
        Timer m_checkWebsitesAndKeepAliveTimer = null;
        Timer m_checkAmazonAwsInstancesTimer = null;

        const int cHeartbeatTimerFrequencyMinutes = 5;

        internal void Start()
        {
            m_mainThreadExitsResetEvent = new ManualResetEventSlim(false);
            ScheduleTimers();
        }

        internal void Exit()
        {
            m_mainThreadExitsResetEvent.Set();
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
