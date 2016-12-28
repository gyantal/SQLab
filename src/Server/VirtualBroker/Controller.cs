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
        DateTime m_startTime;

        // it is better to not store Global State, for example TickerProvider, because it can go stale (the cached copy is not up-to-date), and reqular updating is a lot of problem: when to do it, every 10 minutes or every day?
        // so, don't store State, and cache. When required, fetch it from the SQL server 5 seconds before its is needed
        //internal static DBManager g_dbManager = new DBManager();  // don't cache dbManager state. Max. cache the SqlConnections

        public static Controller g_controller = new Controller();
        public static GatewaysWatcher g_gatewaysWatcher = new GatewaysWatcher();    // this is the Trading Risk Manager Agent. The gateway for trading.
        public static BrokerScheduler g_brokerScheduler = new BrokerScheduler();    // this is the Boss of the Virtual Broker bees. It schedules them.
        public static List<BrokerTaskSchema> g_taskSchemas = new List<BrokerTaskSchema>();  // the worker bees, the Trading Agents
        

        internal void Init()
        {
            Utils.Logger.Info("****VBroker:Init()");
            m_startTime = DateTime.UtcNow;

            g_gatewaysWatcher.Init();
            BuildTasks();
            g_brokerScheduler.Init();

            m_tcpListener = new ParallelTcpListener(VirtualBrokerMessage.VirtualBrokerServerPrivateIpForListener, VirtualBrokerMessage.DefaultVirtualBrokerServerPort, ProcessTcpClient);
            m_tcpListener.StartTcpMessageListenerThreads();
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
            // IB MOC orders: https://www.interactivebrokers.com/en/index.php?f=599  (NYSE, NYSE MKT, NYSE Arca: 15:45 ET, Nasdaq: 15:50 ET)
            var neuralSniffer1TaskSchema = new BrokerTaskSchema()
            {
                Name = "NeuralSniffer1",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, new Func<IBrokerStrategy>(NeuralSniffer1Strategy.StrategyFactoryCreate) },
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
                StartTimeOffset = TimeSpan.FromMinutes(-43),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            neuralSniffer1TaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = neuralSniffer1TaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-17),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, false } }
            });
            g_taskSchemas.Add(neuralSniffer1TaskSchema);


            var taaTaskSchema = new BrokerTaskSchema()
            {
                Name = "TAA",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, new Func<IBrokerStrategy>(TAAStrategy.StrategyFactoryCreate) },
                    { BrokerTaskSetting.OrderExecution, OrderExecution.MarketOnClose },
                    { BrokerTaskSetting.Portfolios, new List<BrokerTaskPortfolio>()
                        {
                        new BrokerTaskPortfolio() { Name = "! Advanced UberTAA with Global Assets Agy Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.GyantalMain,
                            MaxTradeValueInCurrency = 15000, // portfolio is 10K original, but it has 7assets+TLT in it.  If everything is Cash (not likely), TLT is used as 10K. So, set it up as 15K just to think about the future.
                            MinTradeValueInCurrency = 100,
                            Param = new PortfolioParamTAA() { } }
                        }
                    }
                }
            };
            taaTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = taaTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketOpen,
                StartTimeOffset = TimeSpan.FromMinutes(20),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            taaTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = taaTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-39),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            taaTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = taaTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-16),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, false } }
            });
            g_taskSchemas.Add(taaTaskSchema);


            var uberVxxTaskSchema = new BrokerTaskSchema()
            {
                Name = "UberVXX",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, new Func<IBrokerStrategy>(UberVxxStrategy.StrategyFactoryCreate)  },
                    { BrokerTaskSetting.OrderExecution, OrderExecution.Market },
                    { BrokerTaskSetting.Portfolios, new List<BrokerTaskPortfolio>()
                        {
                        new BrokerTaskPortfolio() { Name = "! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.GyantalMain,
                            MaxTradeValueInCurrency = 20000, // portfolio is 5K original, 4K is 2016-11, but it plays double leverage: 8K-10K. 10K is possible. So, double the range to 20K too.
                            MinTradeValueInCurrency = 100,
                            Param = new PortfolioParamUberVXX() { PlayingInstrumentVixLongLeverage = 1.0, PlayingInstrumentVixShortLeverage = 2.0 } },
                        new BrokerTaskPortfolio() { Name = "! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live", HQUserID = HQUserID.drcharmat, IbGatewayUserToTrade = GatewayUser.CharmatSecondary,
                            MaxTradeValueInCurrency = 20000, // >For Mr.C. VXX (10K original, 5K now, I would set MaxValue=20K (assuming portfolio double in a year)
                            MinTradeValueInCurrency = 100,
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
                StartTimeOffset = TimeSpan.FromMinutes(25),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            uberVxxTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = uberVxxTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-35),
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


            


            var harryLongTaskSchema = new BrokerTaskSchema()
            {
                Name = "HarryLong",
                BrokerTaskFactory = BrokerTaskTradeStrategy.BrokerTaskFactoryCreate,
                Settings = new Dictionary<object, object>() {  //not necessary, because VBrokerTask can have local parameters inside itself
                    { BrokerTaskSetting.StrategyFactory, new Func<IBrokerStrategy>(HarryLongStrategy.StrategyFactoryCreate) },
                    { BrokerTaskSetting.OrderExecution, OrderExecution.Market },
                    { BrokerTaskSetting.Portfolios, new List<BrokerTaskPortfolio>()
                        {
                        new BrokerTaskPortfolio() { Name = "! Harry Long (Contango-Bond) harvester Agy Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.GyantalMain,
                            MaxTradeValueInCurrency = 10000, // For Agy: portfolio is 5K original. Set MaxValue=10K  (assuming portfolio double in a year)
                            MinTradeValueInCurrency = 100,
                            Param = new PortfolioParamHarryLong() { } },
                        new BrokerTaskPortfolio() { Name = "! Harry Long (Contango-Bond) harvester Live", HQUserID = HQUserID.drcharmat, IbGatewayUserToTrade = GatewayUser.CharmatSecondary,
                            MaxTradeValueInCurrency = 400000, // For Mr.C.: portfolio is 200K original. Set MaxValue=400K  (assuming portfolio double in a year)
                            MinTradeValueInCurrency = 200,
                            Param = new PortfolioParamHarryLong() {  } },
                        new BrokerTaskPortfolio() { Name = "! IB T. Risky 2 Live", HQUserID = HQUserID.gyantal, IbGatewayUserToTrade = GatewayUser.TuSecondary,
                            MaxTradeValueInCurrency = 10000, // For Tu: portfolio is 5K original. Set MaxValue=10K  (assuming portfolio double in a year)
                            MinTradeValueInCurrency = 200,
                            Param = new PortfolioParamHarryLong() {  } }
                        }
                    }
                }
            };
            harryLongTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = harryLongTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketOpen,
                StartTimeOffset = TimeSpan.FromMinutes(30),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            harryLongTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = harryLongTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromMinutes(-31),
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, true } }
            });
            harryLongTaskSchema.Triggers.Add(new VbTrigger()
            {
                TriggeredTaskSchema = harryLongTaskSchema,
                TriggerType = TriggerType.DailyOnUsaMarketDay,
                StartTimeBase = StartTimeBase.BaseOnUsaMarketClose,
                StartTimeOffset = TimeSpan.FromSeconds(-11),    // Give UberVXX priority (executing at -15sec). That is more important because that can change from full 100% long to -200% short. This Harry Long strategy just slowly modifies weights, so if one trade is missed, it is not a problem.
                TriggerSettings = new Dictionary<object, object>() { { BrokerTaskSetting.IsSimulatedTrades, false } }
            });
            g_taskSchemas.Add(harryLongTaskSchema);

        }

       

        internal void Exit() // in general exit should happen in the opposite order as Init()
        {
            m_tcpListener.StopTcpMessageListener();
            g_brokerScheduler.Exit();
            g_gatewaysWatcher.Exit();
        }
        
    }
}
