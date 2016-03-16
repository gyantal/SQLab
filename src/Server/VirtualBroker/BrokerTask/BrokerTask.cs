using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class BrokerTask
    {
        public BrokerTaskSchema BrokerTaskSchema { get; set; }
        public Trigger Trigger { get; set; }
        public BrokerTaskState BrokerTaskState { get; set; } = BrokerTaskState.NeverStarted; 

        internal virtual void Run()
        {
            
        }
    }
}
