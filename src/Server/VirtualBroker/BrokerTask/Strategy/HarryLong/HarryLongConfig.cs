using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class HarryLongConfig
    {
        public string Ticker1 = "UVXY";     // based on $Volume, UVXY is about 3.5x more traded than TVIX
        public double Weight1 = -0.35;
        public string Ticker2 = "TMV";      // Direxion Daily 20+ Yr Trsy Bear 3X ETF (TMV)
        public double Weight2 = -0.65;

        //public int RebalancingTradingDays = 1;  // 1 or 5 or 10 days. It is not really used, as we rebalance every day.
    }
}
