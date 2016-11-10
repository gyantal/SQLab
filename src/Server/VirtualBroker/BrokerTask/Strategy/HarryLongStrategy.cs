using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{

    // Portfolio specific parameters are here. User1's portfolio1 may use double leverage than User2's portfolio2. The Common Strategy params should go to StrategyConfig.cs
    public class PortfolioParamHarryLong : IPortfolioParam 
    {
        //public double PlayingInstrumentTicker1Leverage { get; set; }
        //public double PlayingInstrumentTicker2Leverage { get; set; }
    }

    public partial class HarryLongStrategy : IBrokerStrategy
    {
        StringBuilder m_detailedReportSb;

        Task<Dictionary<string, Tuple<IAssetID, string>>> m_loadAssetIdTask = null;
        Dictionary<string, Tuple<IAssetID, string>> m_tickerToAssetId = null;   // ticker => AssetID : FullTicker mapping

        public bool IsSameStrategyForAllUsers { get; set; } = true;

        HarryLongConfig harryLongConfig = new HarryLongConfig();

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new HarryLongStrategy();
        }

        public void Init(StringBuilder p_detailedReportSb)
        {
            m_detailedReportSb = p_detailedReportSb;
            m_loadAssetIdTask = Task.Run(() => DbCommon.SqlTools.LoadAssetIdsForTickers(new List<string>() { harryLongConfig.Ticker1, harryLongConfig.Ticker2 }));   // task will start to run on another thread (in the threadpool)
        }

        public string StockIdToTicker(int p_stockID)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId.First(r=> r.Value.Item1.ID == p_stockID).Key;
        }

        public IAssetID TickerToAssetID(string p_ticker)
        {
            if (m_tickerToAssetId == null)
                m_tickerToAssetId = m_loadAssetIdTask.Result;   // wait until the parrallel task arrives
            return m_tickerToAssetId[p_ticker].Item1;
        }

        public double GetPortfolioLeverage(List<PortfolioPositionSpec> p_suggestedPortfItems, IPortfolioParam p_param)
        {
            return 1.0;
        }

        public List<PortfolioPositionSpec> GeneratePositionSpecs()
        {
            Utils.Logger.Info("HarryLongStrategy.GeneratePositionSpecs() Begin.");

            string consoleMsg = $"Target: { harryLongConfig.Ticker1}:{harryLongConfig.Weight1 * 100}%, { harryLongConfig.Ticker2}:{harryLongConfig.Weight2 * 100}%";
            Utils.ConsoleWriteLine(ConsoleColor.Green, false, consoleMsg);
            Utils.Logger.Info(consoleMsg);
            m_detailedReportSb.AppendLine($"<font color=\"#10ff10\">{consoleMsg}</font>");

            List <PortfolioPositionSpec> specs = new List<PortfolioPositionSpec>();
            // if Weight is 0,  the target position is 0.
            if (harryLongConfig.Weight1 > 0.0000001)
                specs.Add(new PortfolioPositionSpec() { Ticker = harryLongConfig.Ticker1, PositionType = PositionType.Long, Size = WeightedSize.Create(Math.Abs(harryLongConfig.Weight1)) });
            else if (harryLongConfig.Weight1 < -0.0000001)
                specs.Add(new PortfolioPositionSpec() { Ticker = harryLongConfig.Ticker1, PositionType = PositionType.Short, Size = WeightedSize.Create(Math.Abs(harryLongConfig.Weight1)) });

            if (harryLongConfig.Weight2 > 0.0000001)
                specs.Add(new PortfolioPositionSpec() { Ticker = harryLongConfig.Ticker2, PositionType = PositionType.Long, Size = WeightedSize.Create(Math.Abs(harryLongConfig.Weight2)) });
            else if (harryLongConfig.Weight2 < -0.0000001)
                specs.Add(new PortfolioPositionSpec() { Ticker = harryLongConfig.Ticker2, PositionType = PositionType.Short, Size = WeightedSize.Create(Math.Abs(harryLongConfig.Weight2)) });

            return specs;
        }
    }
}
