using DbCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class NeuralSniffer1Strategy : IBrokerStrategy
    {
        public bool IsSameStrategyForAllUsers { get; set; } = true;

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new NeuralSniffer1Strategy();
        }

        public void Init()
        {
            throw new NotImplementedException();
        }

        public List<PortfolioPositionSpec> GeneratePositionSpecs()
        {
            throw new NotImplementedException();
        }

        public string StockIdToTicker(int p_stockID)
        {
            return null;
        }

        public IAssetID TickerToAssetID(string p_ticker)
        {
            return null;
        }

        public double GetPortfolioLeverage(List<PortfolioPositionSpec> p_suggestedPortfItems, IPortfolioParam p_param)
        {
            throw new NotImplementedException();
        }
    }
}
