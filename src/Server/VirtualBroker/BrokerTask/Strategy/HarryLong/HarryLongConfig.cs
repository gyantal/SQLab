using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class HarryLongConfig
    {
        public string[] AllPotentialTickersOfPortfolios = new string[] { "TVIX", "TMV", "USO", "BOIL", "JJC" };

        public string[] DefaultTradedTickers = new string[] { "TVIX", "TMV" };            // strategy default is used only if PortfolioParamHarryLong is not given
        public double[] DefaultTradedAssetsWeights = new double[] { -0.35, -0.65 };       // strategy default is used only if PortfolioParamHarryLong is not given

        //public int RebalancingTradingDays = 1;  // 1 or 5 or 10 days. It is not really used, as we rebalance every day.
    }

    // Portfolio specific parameters are here. User1's portfolio1 may use double leverage than User2's portfolio2. The Common Strategy params should go to StrategyConfig.cs
    public class PortfolioParamHarryLong : IPortfolioParam // default values here for default users
    {
        // based on $Volume, UVXY is about 3.5x more traded than TVIX. We started with UVXY. But 2017-02-14: switched to TVIX, because of forced "SHORT STOCK POSITION BOUGHT IN" without advance notice. UVXY was not available for shorting for 5 days, and borrowing cost was 20%, while it is 3.4% for TVIX.
        // TMV is 3x TLT: Direxion Daily 20+ Yr Trsy Bear 3X ETF (TMV)
        public string[] Tickers = new string[] { "TVIX", "TMV", "USO", "BOIL" };      // portfolio defaults
        public double[] AssetsWeights = new double[] { -0.35, -0.57, -0.17, -0.26 }; // portfolio defaults, Markowitz MPT optimal weight using 135% allocation. With daily rebalancing it is safe.
    }
}
