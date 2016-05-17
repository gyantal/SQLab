using DbCommon;
using IBApi;
using SqCommon;
using SQCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;     // this is the only timer available under DotNetCore
using System.Threading.Tasks;

namespace VirtualBroker
{
    public partial class Controller
    {
        // it is better to not store Global State, for example TickerProvider, because it can go stale (the cached copy is not up-to-date), and reqular updating is a lot of problem: when to do it, every 10 minutes or every day?
        // so, don't store State, and cache. When required, fetch it from the SQL server 5 seconds before its is needed
        //internal static DBManager g_dbManager = new DBManager();  // don't cache dbManager state. Max. cache the SqlConnections

        public static Controller g_controller = new Controller();
        public static GatewaysWatcher g_gatewaysWatcher = new GatewaysWatcher();    // this is the Trading Risk Manager Agent. The gateway for trading.
        public static BrokerScheduler g_brokerScheduler = new BrokerScheduler();    // this is the Boss of the Virtual Broker bees. It schedules them.
        public static List<BrokerTaskSchema> g_taskSchemas = new List<BrokerTaskSchema>();  // the worker bees, the Trading Agents
        

        internal void Start()
        {
            g_gatewaysWatcher.Init();
            BuildTasks();
            g_brokerScheduler.Init();
        }

        public static bool IsRunningAsLocalDevelopment()
        {
            if (Utils.RunningPlatform() == Platform.Linux)    // assuming production environment on Linux, Other ways to customize: ifdef DEBUG/RELEASE  ifdef PRODUCTION/DEVELOPMENT, etc. this Linux/Windows is fine for now
            {
                return false;
            }
            else
            {
                // Windows: however, sometimes, when Running on Windows, we want to Run it as a proper Production environment. E.g.
                //      + Sometimes, for Debugging reasons, 
                //      + sometimes, because Linux server is down and running the Production locally on Windows
                return true;
            }
        }

        public void BuildTasks()
        {
            Func<IBrokerStrategy> uberVxxStrategyCreateFunc = UberVxxStrategy.StrategyFactoryCreate;
            var uberVxxTaskSchema = new BrokerTaskSchema()
            {
                Name = "UberVXX",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, uberVxxStrategyCreateFunc },
                    { BrokerTaskSetting.OrderExecution, OrderExecution.Market },
                    { BrokerTaskSetting.Portfolios, new List<BrokerTaskPortfolio>()
                        {
                        new BrokerTaskPortfolio() { Name = "! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.GyantalMain,
                            Param = new PortfolioParamUberVXX() { PlayingInstrumentVixLongLeverage = 1.0, PlayingInstrumentVixShortLeverage = 2.0 } },
                        new BrokerTaskPortfolio() { Name = "! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live", HQUserID = HQUserID.drcharmat, IbGatewayUserToTrade = GatewayUser.CharmatSecondary,
                            Param = new PortfolioParamUberVXX() { PlayingInstrumentVixLongLeverage = 1.0, PlayingInstrumentVixShortLeverage = 1.0 } }
                        }
                    }
                }
            };
            uberVxxTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = uberVxxTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketOpen,
                StartTimeOffset = TimeSpan.FromMinutes(20),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            uberVxxTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = uberVxxTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-31),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            uberVxxTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = uberVxxTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromSeconds(-15),    // from -20sec to -15sec. From start, the trade executes in 2seconds
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, false } }
            });
            g_taskSchemas.Add(uberVxxTaskSchema);


            Func<IBrokerStrategy> neuralSniffer1StrategyCreateFunc = NeuralSniffer1Strategy.StrategyFactoryCreate;
            var neuralSniffer1TaskSchema = new BrokerTaskSchema()
            {
                Name = "NeuralSniffer1",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, neuralSniffer1StrategyCreateFunc },
                    { BrokerTaskSetting.OrderExecution, OrderExecution.MarketOnClose },
                    { BrokerTaskSetting.Portfolios, new List<BrokerTaskPortfolio>()
                        {
                        new BrokerTaskPortfolio() { Name = "! NeuralSniffer Aggressive Agy Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.GyantalMain,
                            Param = new PortfolioParamNeuralSniffer1() { PlayingInstrumentUpsideLeverage = -2.0, PlayingInstrumentDownsideLeverage = -2.0 } }
                        }
                    }
                }
            };
            neuralSniffer1TaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = neuralSniffer1TaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketOpen,
                StartTimeOffset = TimeSpan.FromMinutes(15),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            neuralSniffer1TaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = neuralSniffer1TaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-35),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            neuralSniffer1TaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = neuralSniffer1TaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-16), 
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, false } }
            });
            g_taskSchemas.Add(neuralSniffer1TaskSchema);


        }

        internal void Exit()
        {
            g_brokerScheduler.Exit();
            g_gatewaysWatcher.Exit();
        }
        
    }
}
