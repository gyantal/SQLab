using SqCommon;
using SQCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{
    
    public class VbTrigger : TriggerBase
    {
        public VbTrigger() : base()
        {
        }

        public override void Timer_Elapsed(object state)    // Timer is coming on a ThreadPool thread
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

            Task.Factory.StartNew(BrokerTaskExecutionThreadRun, TaskCreationOptions.LongRunning).LogUnobservedTaskExceptions("VbTrigger.Timer_Elapsed()");  // a separate thread. Not on ThreadPool, because it may take 30+ seconds
        }

        private void BrokerTaskExecutionThreadRun()
        {
            try
            {
                BrokerTask brokerTask = ((BrokerTaskSchema)TriggeredTaskSchema).BrokerTaskFactory();
                brokerTask.BrokerTaskSchema = (BrokerTaskSchema)TriggeredTaskSchema;
                brokerTask.Trigger = this;
                brokerTask.Run();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("BrokerTaskExecutionThreadRun Exception: ", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker); // don't send Email here. HealthMonitor will decide what to do
            }
        }
    }

}
