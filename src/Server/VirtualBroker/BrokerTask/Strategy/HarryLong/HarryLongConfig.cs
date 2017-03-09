using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class HarryLongConfig
    {
        public string Ticker1 = "TVIX";     // based on $Volume, UVXY is about 3.5x more traded than TVIX. We started with UVXY. But 2017-02-14: switched to TVIX, because of forced "SHORT STOCK POSITION BOUGHT IN" without advance notice. UVXY was not available for shorting for 5 days, and borrowing cost was 20%, while it is 3.4% for TVIX.
        public double Weight1 = -0.35;
        public string Ticker2 = "TMV";      // Direxion Daily 20+ Yr Trsy Bear 3X ETF (TMV)
        public double Weight2 = -0.65;

        //public int RebalancingTradingDays = 1;  // 1 or 5 or 10 days. It is not really used, as we rebalance every day.
    }
}
