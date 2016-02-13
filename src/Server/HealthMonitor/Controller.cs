using SQCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public partial class Controller
    {
        static public Controller g_controller = new Controller();

        ManualResetEventSlim m_mainThreadExitsResetEvent = null;
        Timer m_checkWebsitesAndKeepAliveTimer = null;



        internal void Start(/*IConfigurationRoot p_configuration*/)
        {
            //g_configuration = p_configuration;
            m_mainThreadExitsResetEvent = new ManualResetEventSlim(false);
            ScheduleTimers();
        }

        private void ScheduleTimers()
        {
            // "if I don't hit the site for 10-15 minutes, it goes to sleep"; "default configuration of an IIS Application pool that is set to have an idle-timeout of 20 minutes"
            m_checkWebsitesAndKeepAliveTimer = new System.Threading.Timer(new TimerCallback(CheckWebsitesAndKeepAliveTimer_Elapsed), null, TimeSpan.Zero, TimeSpan.FromMinutes(9.0));
        }


        internal void Exit()
        {
            m_mainThreadExitsResetEvent.Set();
        }

    }
}
