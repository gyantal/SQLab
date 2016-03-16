using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public enum TriggerType : byte
    {   // similar to Windows TaskScheduler
        // On a schedule: 
        OneTime, Daily, DailyOnUsaMarketDay, DailyOnLondonMarketDay, Weekly, Monthly,
        // On an event:
        AtApplicationStartup, AtApplicationExit, OnGatewayDisconnectionEvent, OnError, Unknown
    };
    public enum StartTimeBase : byte { BaseOnAbsoluteTime, BaseOnUsaMarketOpen, BaseOnUsaMarketClose, Unknown };

    public class Trigger
    {
        public BrokerTaskSchema BrokerTaskSchema { get; set; }
        public bool Enabled { get; set; }
        public TriggerType TriggerType { get; set; }   // currently only Daily supported
        public int RepeatEveryXSeconds { get; set; } // -1, if not Recur
        public StartTimeBase StartTimeBase { get; set; }
        public TimeSpan StartTimeOffset { get; set; }
        public DateTime StartTimeExplicitUtc { get; set; }
        public Dictionary<object, object> TriggerSettings { get; set; } = new Dictionary<object, object>();
        

        public DateTime? NextScheduleTimeUtc { get; set; }
        public Timer Timer { get; set; }

        public Trigger()
        {
            Timer = new System.Threading.Timer(new TimerCallback(Timer_Elapsed), null, TimeSpan.FromMilliseconds(-1.0), TimeSpan.FromMilliseconds(-1.0));
        }

        public void Timer_Elapsed(object state)    // Timer is coming on o ThreadPool thread
        {
            Utils.Logger.Info("Trigger.Timer_Elapsed() ");
            NextScheduleTimeUtc = null;

            bool isMarketTradingDay;
            DateTime marketOpenTimeUtc, marketCloseTimeUtc;
            bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(DateTime.UtcNow, out isMarketTradingDay, out marketOpenTimeUtc, out marketCloseTimeUtc, TimeSpan.FromDays(3));
            if (!isTradingHoursOK)
            {
                Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
            }
            else
            {
                Controller.g_brokerScheduler.ScheduleTrigger(this, isMarketTradingDay, marketOpenTimeUtc, marketCloseTimeUtc);
            }

            Task.Factory.StartNew(BrokerTaskExecutionThreadRun, TaskCreationOptions.LongRunning);  // a separate thread. Not on ThreadPool, because it may take 30 seconds
        }

        private void BrokerTaskExecutionThreadRun()
        {
            try
            {
                BrokerTask brokerTask = BrokerTaskSchema.BrokerTaskFactory();
                brokerTask.BrokerTaskSchema = BrokerTaskSchema;
                brokerTask.Trigger = this;
                brokerTask.Run();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("BrokerTaskExecutionThreadRun Exception: ", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
            }
        }
    }

}
