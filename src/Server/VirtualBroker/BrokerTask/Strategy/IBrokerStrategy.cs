using DbCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public enum PositionType
    {
        Unspecified,
        Unused,
        Long,
        Short
    }

    public interface ISizeSpec
    {
        string ToString();
    }

    public class FixedSize : ISizeSpec
    {
        public virtual double Size { get; protected set; }

        public override string ToString() { return $"FixedSize: {Size:F4}"; }
    }

    public class ProportionSize : ISizeSpec
    {
        public double Min { get; protected set; }
        public double PreferredMin { get; protected set; }
        public double Wanted { get; protected set; }
        public double PreferredMax { get; protected set; }
        public double Max { get; protected set; }

        public override string ToString() { return $"ProportionSize; Wanted: {Wanted:F4}"; }
    }

    public class WeightedSize : ISizeSpec
    {
        public double Weight { get; set; }

        public static readonly ISizeSpec Default = Create(1);
        public static readonly ISizeSpec One = Create(1);

        public static WeightedSize Create(double p_weight)
        {
            if (p_weight < 0)
                throw new ArgumentOutOfRangeException();
            return new WeightedSize { Weight = p_weight };
        }

        public override string ToString() { return $"WeightSize: {Weight:F4}"; }
    }


    [DebuggerDisplay("{DebugString()}")]
    public class PortfolioPositionSpec
    {
        public string Ticker { get; set; }
        public PositionType PositionType { get; set; }
        public ISizeSpec Size { get; set; }

        string DebugString()
        {
            //return ToString(TickerProvider.Singleton);
            return $"{PositionType} {Ticker} with {Size}";
        }

        public bool IsShort { get { return PositionType == PositionType.Short; } }
    }

  
    public interface IBrokerStrategy
    {
        bool IsSameStrategyForAllUsers { get; set; }        // if it is true it is enough to run only once, and trade on many portfolios

        void Init(StringBuilder p_detailedReportSb);
        List<PortfolioPositionSpec> GeneratePositionSpecs();
        string StockIdToTicker(int p_stockID);
        IAssetID TickerToAssetID(string p_ticker);

        double GetPortfolioLeverage(List<PortfolioPositionSpec> p_suggestedPortfItems, IPortfolioParam p_param);

        //IRealBrokerAPI BrokerAPI { get; set; }
        //List<PortfolioItemPlus> CurrentPortfolioSnapshot { get; set; }

        //void InitAfterBrokerApiOpened();
        //void Init(IStrategyContext p_context);

        ///// <summary> Returns zero or more rebalancing specification sets 
        ///// in strictly increasing order of PortfolioSpecs.TradingTimeUtc
        ///// (preferably at the times specified by IStrategyContext.TradingTimes[],
        ///// but this is not obligatory). The first specification set must be
        ///// complete (full rebalancing), further ones may be partial.
        ///// </summary>
        //IEnumerable<PortfolioSpecs> GenerateSpecs();
    }
}
