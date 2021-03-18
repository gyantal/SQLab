using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// A common base used both in VBroker and HealthMonitor (E.g. HealthMonitor checks that VBroker OK message arrived properly from the Expected Strategy at the expected time. If not, it sends warning email.)
namespace SqCommon
{
    
    public enum TriggerType : byte
    {   // similar to Windows TaskScheduler
        // On a schedule: 
        OneTime, Daily, DailyOnUsaMarketDay, DailyOnLondonMarketDay, Weekly, Monthly,
        // On an event:
        AtApplicationStartup, AtApplicationExit, OnGatewayDisconnectionEvent, OnError, Unknown
    };
    public enum StartTimeBase : byte { BaseOnAbsoluteTime, BaseOnUsaMarketOpen, BaseOnUsaMarketClose, Unknown };

    public class SqTrigger
    {
        public bool Enabled { get; set; }
        public TriggerType TriggerType { get; set; }   // currently only Daily supported
        public int RepeatEveryXSeconds { get; set; } // -1, if not Recur
        public StartTimeBase StartTimeBase { get; set; }
        public TimeSpan StartTimeOffset { get; set; }
        public DateTime StartTimeExplicitUtc { get; set; }
        public Dictionary<object, object> TriggerSettings { get; set; } = new Dictionary<object, object>();

        public DateTime? NextScheduleTimeUtc { get; set; }
        public Timer Timer { get; set; }

        public SqTask SqTask { get; set; }

        public SqTrigger()
        {
            Timer = new System.Threading.Timer(new TimerCallback(Timer_Elapsed), null, TimeSpan.FromMilliseconds(-1.0), TimeSpan.FromMilliseconds(-1.0));
        }

        public virtual void Timer_Elapsed(object state)    // Timer is coming on a ThreadPool thread
        {
            Utils.Logger.Info("Trigger.Timer_Elapsed() ");
            NextScheduleTimeUtc = null;

            bool isMarketTradingDay;
            DateTime marketOpenTimeUtc, marketCloseTimeUtc;
            bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(DateTime.UtcNow, out isMarketTradingDay, out marketOpenTimeUtc, out marketCloseTimeUtc, TimeSpan.FromDays(3));
            if (!isTradingHoursOK)
                Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
            else
                SqTaskScheduler.gTaskScheduler.ScheduleTrigger(this, isMarketTradingDay, marketOpenTimeUtc, marketCloseTimeUtc);

            try
            {
                if (SqTask != null)
                {
                    SqExecution sqExecution = ((SqTask)SqTask).ExecutionFactory();
                    sqExecution.SqTask = (SqTask)SqTask;
                    sqExecution.Trigger = this;
                    sqExecution.Run();
                }
            }
            catch (Exception e)
            {                
                HealthMonitorMessage.SendAsync($"Exception in BrokerTaskExecutionThreadRun(). Exception: '{ e.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromVirtualBroker).TurnAsyncToSyncTask();
            }
        }
    }
}
