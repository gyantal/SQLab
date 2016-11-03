using DbCommon;
using SqCommon;
using SQCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{
    // TODO: read this and throw out that we don't use
    public enum BrokerTaskSetting
    {
        Name,
        ActionType,     // LeverageCheckerAlert, MarketFallMoreThan2PercentAlert, etc.
        ActionParams,   // global for all portfolios
        Portfolios,
        LivePortfolioNames,  // can be null: MarketFallMoreThan2PercentAlert doesn't have portfolio attached, or can be more than 1 portfolio name
        PortfolioUserIDs,   // DbCommon.HQUserID
        PortfoliosParams, //BrokerTask prepares for many portfolios with different Param; play them separately, integrate them; and only play at the borker once   
        PortfolioParams, // when calling a single portfolio (strategy.GenerateSpecs()); PortfolioParams is split to parts, and it is fed to the Strategy.GenerateSpecs()
        SignificancyThresholdValueInCurrency, // used when MinTradeValue filtered a real trade out
        HQUserID,
        SmsNumbersBrokerShouldReport,
        EmailAddressesForSimulationReport,
        EmailAddressesForRealTradeReport,
        EmailAddressesForEmergency,
        TriggerType,
        StartTimeBase,
        StartTimeOffset,

        //#region General back-testing settings
        StartTime, EndTime,

        //
        IsSimulatedTrades,    // Simulate Trade execution, like virtual trades too, because it can be tricky and bug can be found if trades are simulated properly itraday (not only the strategy)
        ReportEmailStringBuilder,
        ReportSmsStringBuilder,
        MaxTradeValueInCurrency,    // in local currency is USD or EUR
        MinTradeValueInCurrency,     // in local currency is USD or EUR
        OrderExecution, // Market or Limit
        OrderTimeInForce, // DAY, GTC, OPG, IOC

        RealBroker,      // InteractiveBrokersAPI or AmeriTradeBrokerAPI
        BrokerAccountID,     // string name of the account "U***" for IB  // for double checking it that we play the correct account
        InteractiveBrokersConnectionPort,     // for InteractiveBrokers
        InteractiveBrokersConnectionClientID,     // for InteractiveBrokers
        MarketOpenTimeUtc, MarketCloseTimeUtc,
        TaskLogFile,
        StrategyFactory
        //BrokerTaskExecution
    }

    public enum OrderExecution { Unknown = 0, Market = 1, Limit = 2, MarketOnOpen = 3, LimitOnOpen = 4, MarketOnClose = 5, LimitOnClose = 6 };

    public enum OrderTimeInForce     // taken from IB API from a different namespace. Default: TimeInForce.Day
    {
        /// <summary>
        /// Day
        /// </summary>
        Day,
        /// <summary>
        /// Good Till Cancel
        /// </summary>
        GoodTillCancel,
        /// <summary>
        /// You can set the time in force for MARKET or LIMIT orders as IOC. This dictates that any portion of the order not executed immediately after it becomes available on the market will be cancelled.
        /// </summary>
        ImmediateOrCancel,
        /// <summary>
        /// Setting FOK as the time in force dictates that the entire order must execute immediately or be canceled.
        /// </summary>
        FillOrKill,
        /// <summary>
        /// Good Till Date
        /// </summary>
        GoodTillDate,
        /// <summary>
        /// Market On Open
        /// </summary>
        MarketOnOpen,
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined
    }

    public class BrokerTaskPortfolio : DbPortfolio
    {
        // General parameters. Not specific for Strategies
        public GatewayUser IbGatewayUserToTrade { get; set; }
        public double MaxTradeValueInCurrency { get; set; } = 5000;
        public double MinTradeValueInCurrency { get; set; } = 0.0;

        // Specific parameters for Strategies
        public IPortfolioParam Param { get; set; }      // e.g. new PortfolioParamUberVXX()


        // Non-parameters. Holding realtime (calculated) values
        public bool IsErrorOccured { get; set; }    // if IBGatewayUser is not connected, or there is an error in processing, then stop further processing, and raise Error message, and Supervisor handles manually

        public double PortfolioUsdSize { get; set; }
        public List<PortfolioPosition> ProposedPositions { get; set; }
        public List<Transaction> ProposedTransactions { get; set; }
    }

    public class IPortfolioParam
    {

    }

  
    public enum BrokerTaskState : byte { NeverStarted, StartedButNotYetInitialized, StartedAndInitialized, Working, Finished, Crashed, Unknown };

    // Schema is like a Class for BrokerTasks. The instances of this class is the BrokerTasks or we called it before: BrokerTaskExecutions
    // If a BrokerTaskSchema specifies a Task that runs every 5 minutes, many parallel BrokerTasks of the same Type/Schema can exist at the same time
    public class BrokerTaskSchema : TriggeredTaskSchema
    {
        public Dictionary<object, object> Settings { get; set; } = new Dictionary<object, object>();

        public List<BrokerTask> BrokerTasks { get; set; } = new List<BrokerTask>();      // if we Execute the task every 5 minutes than these 2 executions can live in parallel

        public Func<BrokerTask> BrokerTaskFactory { get; set; }

        public BrokerTaskSchema()
        {
        }
    }

    
}
