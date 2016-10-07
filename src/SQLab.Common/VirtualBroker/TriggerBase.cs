using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public class TriggeredTaskSchema
    {
        public string Name { get; set; }
        public List<TriggerBase> Triggers { get; set; } = new List<TriggerBase>();
    }

    public class TriggerBase
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

        public TriggeredTaskSchema TriggeredTaskSchema { get; set; }

        public TriggerBase()
        {
            Timer = new System.Threading.Timer(new TimerCallback(Timer_Elapsed), null, TimeSpan.FromMilliseconds(-1.0), TimeSpan.FromMilliseconds(-1.0));
        }

        public virtual void Timer_Elapsed(object state)    // Timer is coming on a ThreadPool thread
        {
            try
            {
                Utils.Logger.Warn("TriggerBase.Timer_Elapsed(). You shouldn't be here. Timer_Elapsed() virtual method should be called in the derived class.");
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "TriggerBase.Timer_Elapsed() exception.");
                throw;
            }
        }
    }
}
