using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class HarryLongConfig
    {
        // based on $Volume, UVXY is about 3.5x more traded than TVIX. We started with UVXY. But 2017-02-14: switched to TVIX, because of forced "SHORT STOCK POSITION BOUGHT IN" without advance notice. UVXY was not available for shorting for 5 days, and borrowing cost was 20%, while it is 3.4% for TVIX.
        // TMV is 3x TLT: Direxion Daily 20+ Yr Trsy Bear 3X ETF (TMV)
        public string[] Tickers = new string[] { "TVIX", "TMV", "USO", "UNG", "JJC" };
        public double[] AssetsWeights = new double[] { -0.35, -0.25, -0.08, -0.28, -0.04 };

        //public int RebalancingTradingDays = 1;  // 1 or 5 or 10 days. It is not really used, as we rebalance every day.
    }
}
