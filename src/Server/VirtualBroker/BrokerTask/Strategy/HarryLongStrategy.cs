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

    public partial class HarryLongStrategy : IBrokerStrategy
    {
        StringBuilder m_detailedReportSb;

        Task<Dictionary<string, Tuple<IAssetID, string>>> m_loadAssetIdTask = null;
        Dictionary<string, Tuple<IAssetID, string>> m_tickerToAssetId = null;   // ticker => AssetID : FullTicker mapping

        public bool IsSameStrategyForAllUsers { get; set; } = false;

        HarryLongConfig harryLongConfig = new HarryLongConfig();

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new HarryLongStrategy();
        }

        public void Init(StringBuilder p_detailedReportSb)
        {
            m_detailedReportSb = p_detailedReportSb;
            m_loadAssetIdTask = Task.Run(() => DbCommon.SqlTools.LoadAssetIdsForTickers(harryLongConfig.AllPotentialTickersOfPortfolios.ToList()));   // task will start to run on another thread (in the threadpool)
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

        public List<PortfolioPositionSpec> GeneratePositionSpecs(IPortfolioParam p_portfolioParam)
        {
            Utils.Logger.Info("HarryLongStrategy.GeneratePositionSpecs() Begin.");
            PortfolioParamHarryLong portfolioParam = (PortfolioParamHarryLong)p_portfolioParam;

            string[] tickers;
            double[] assetsWeights;
            if (portfolioParam != null)
            {
                tickers = portfolioParam.Tickers;
                assetsWeights = portfolioParam.AssetsWeights;
            } else
            {
                tickers = harryLongConfig.DefaultTradedTickers;
                assetsWeights = harryLongConfig.DefaultTradedAssetsWeights;
            }

            StringBuilder consoleMsgSb = new StringBuilder($"Target: ");
            for (int i = 0; i < tickers.Length; i++)
            {
                consoleMsgSb.Append($"{ tickers[i]}:{assetsWeights[i] * 100}%");
                if (i != tickers.Length - 1)
                    consoleMsgSb.Append(", ");
            }
            string consoleMsg = consoleMsgSb.ToString();
            Utils.ConsoleWriteLine(ConsoleColor.Green, false, consoleMsg);
            Utils.Logger.Info(consoleMsg);
            m_detailedReportSb.AppendLine($"<font color=\"#105A10\">{consoleMsg}</font>");

            List <PortfolioPositionSpec> specs = new List<PortfolioPositionSpec>();
            for (int i = 0; i < tickers.Length; i++)
            {
                double weight = assetsWeights[i];
                // if weight is 0,  the target position is 0.
                if (Utils.IsMore(weight, 0))        // using REAL_EPS
                    specs.Add(new PortfolioPositionSpec() { Ticker = tickers[i], PositionType = PositionType.Long, Size = WeightedSize.Create(Math.Abs(weight)) });
                else if (Utils.IsLess(weight, 0))
                    specs.Add(new PortfolioPositionSpec() { Ticker = tickers[i], PositionType = PositionType.Short, Size = WeightedSize.Create(Math.Abs(weight)) });
            }
            return specs;
        }
    }
}
