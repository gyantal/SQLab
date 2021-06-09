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

// We have to name something in the code "Sobek, the Almighty" (the Egyptian crocodile god), to remember him, as almighty, all powerful god. 
// See Harari's book Homo Deus. Sobek the Almighty is so powerful he can predict the future. Also he was like Google or big firms now. 
// Servants collected money in his name. He become very rich. As rich and as powerful as the pharao.
// >https://simple.wikipedia.org/wiki/Sobek
// >Maybe the TaskScheduler, or the
// >TransactionAccumulator, or 
// >just something on the UI. On UI it can have a crocodile icon. Or the portfolio Connor can be transformed to almighty Sobek. That is a good idea. Rename it in SqDesktop portfolio as "Sobek (old Connor)". I don't have a Connor strategy in code, because we call it UberVXX.
// >HarryLong can be renamed to Sobek. Because in 20 years, when I develop my own strategy I don't want to call it "HarryLong", the guy. As it will be completely different from his old idea. It is also better to call it with a cryptic name that nobady can search on the internet.
    public partial class SobekStrategy : IBrokerStrategy
    {
        StringBuilder m_detailedReportSb;

        Task<Dictionary<string, Tuple<IAssetID, string>>> m_loadAssetIdTask = null;
        Dictionary<string, Tuple<IAssetID, string>> m_tickerToAssetId = null;   // ticker => AssetID : FullTicker mapping

        public bool IsSameStrategyForAllUsers { get; set; } = false;

        HarryLongConfig harryLongConfig = new HarryLongConfig();

        public static IBrokerStrategy StrategyFactoryCreate()
        {
            return new SobekStrategy();
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
