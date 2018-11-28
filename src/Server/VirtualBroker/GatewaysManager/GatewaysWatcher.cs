using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    public class SavedState : PersistedState   // data to persist between restarts of the crawler process
    {
        public bool IsSendErrorEmailAtGracefulShutdown { get; set; } = true;   // switch this off before deployment, and switch it on after deployment; make functionality on the WebSite
    }

    
    
    public partial class GatewaysWatcher
    {
        SavedState m_persistedState = null;
        List<Gateway> m_gateways = new List<Gateway>();

        GatewayUser m_mainGatewayUser;
        Gateway m_mainGateway = null;

        bool m_isReady = false;
        
        public SavedState PersistedState
        {
            get
            {
                return m_persistedState;
            }

            set
            {
                m_persistedState = value;
            }
        }

        public void Init()
        {
            Utils.Logger.Info("****GatewaysWatcher:Init()");
            PersistedState = new SavedState();
            
            Task reconnectToGatewaysTask = Task.Factory.StartNew(ReconnectToGateways).LogUnobservedTaskExceptions("GatewaysWatcher.ReconnectToGateways()");  // short running thread on ThreadPool
        }

        async void ReconnectToGateways()
        {
            Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways() BEGIN");
            m_mainGateway = null;
            try
            {
                Gateway gateway2 = null, gateway3 = null;
                Gateway gateway1 = new Gateway(GatewayUser.GyantalMain, p_accountMaxTradeValueInCurrency: 100000 /* UberVXX is 12K, 2xleveraged=24K, double=48K*/, p_accountMaxEstimatedValueSumRecentlyAllowed: 75000) { VbAccountsList = "U407941", SocketPort = (int)GatewayUserPort.GyantalMain, BrokerConnectionClientID = 41 };
                if (!Controller.IsRunningAsLocalDevelopment())
                {
                    gateway2 = new Gateway(GatewayUser.CharmatSecondary, p_accountMaxTradeValueInCurrency: 600000.0 /* HarryLong is played 400K*70%=300K, double it */, p_accountMaxEstimatedValueSumRecentlyAllowed: 1000000  /* 1M */ ) { VbAccountsList = "U988767", SocketPort = (int)GatewayUserPort.CharmatSecondary, BrokerConnectionClientID = 42};
                    gateway3 = new Gateway(GatewayUser.TuSecondary, p_accountMaxTradeValueInCurrency: 15000.0 /* HarryLong is played 10K*70%=7K, double it */, p_accountMaxEstimatedValueSumRecentlyAllowed: 20000  /* 20K */ ) { VbAccountsList = "U1156489", SocketPort = (int)GatewayUserPort.TuSecondary, BrokerConnectionClientID = 43 };
                    //Gateway gateway2 = new Gateway() { GatewayUser = GatewayUser.CharmatWifeMain, VbAccountsList = "U1034066", SocketPort = 7302 };
                    m_mainGatewayUser = GatewayUser.CharmatSecondary;
                }
                else
                {
                    m_mainGatewayUser = GatewayUser.GyantalMain;
                }

                m_gateways = new List<Gateway>() { gateway1 };   // delete previous Gateway connections
                if (gateway2 != null)   // sometimes (for development), 1 gateway is used only
                    m_gateways.Add(gateway2);
                if (gateway3 != null)   // sometimes (for development), 1 gateway is used only
                    m_gateways.Add(gateway3);

                //Task connectTask1 = Task.Factory.StartNew(ReconnectToGateway, gateway1, TaskCreationOptions.LongRunning);
                //Task connectTask2 = Task.Factory.StartNew(ReconnectToGateway, gateway2, TaskCreationOptions.LongRunning);
                var reconnectTasks = m_gateways.Select(r => Task.Factory.StartNew(ReconnectToGateway, r, TaskCreationOptions.LongRunning).LogUnobservedTaskExceptions("GatewaysWatcher.reconnectTasks"));

                Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways()  reconnectTasks BEGIN");
                // At the beginning: Linux had a problem to Connect sequentially on 2 separate threads. Maybe Linux DotNetCore 'Beta' implementation synchronization problem. Temporary connect sequentally. maybe doing Connection sequentially, not parallel would help
                foreach (var task in reconnectTasks)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));  // just for synch safety. However, if Linux has synch problem, we can have problems at order execution...!!
                    //task.Wait();  // this gives Compiler Warning that the method is async, but there is no await in it
                    await task; // this doesn't give Compiler Warning.
                }
                //await Task.WhenAll(reconnectTasks); // async. This threadpool thread will return to the threadpool for temporary reuse, and when tasks are ready, it will be recallade
                ////Task.WaitAll(connectTask1, connectTask2);     // blocking wait. This thread will wait forever if needed, but we don't want to starve the threadpool
                Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways()  reconnectTasks END");

                bool isAllConnected = true;
                foreach (var gateway in m_gateways)
                {
                    if (!gateway.IsConnected)
                        isAllConnected = false;
                    if (gateway.GatewayUser == m_mainGatewayUser)
                    {
                        m_mainGateway = gateway;
                    }

                }
                StrongAssert.True(isAllConnected, Severity.ThrowException, $"Some Gateways are not connected.");
                StrongAssert.True(m_mainGateway != null, Severity.ThrowException, $"Gateway for main user { m_mainGatewayUser} is not found.");

                Utils.Logger.Info("GatewaysWatcher is ready. Connections were successful.");
                m_isReady = true;   // GatewaysWatcher is ready

                // getting prices of SPY (has dividend, but liquid) or VXX (no dividend, but less liquids) is always a must. An Agent would always look that price. So, subscribe to that on the MainGateway

                // see what is possible to call: 
                // "g:\temp\_programmingTemp\TWS API_972.12(2016-02-26)\samples\CSharp\IBSamples\IBSamples.sln" 
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "VXX", SecType = "STK", Currency = "USD", Exchange = "SMART" });
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "SVXY", SecType = "STK", Currency = "USD", Exchange = "SMART" });
                ////m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "SPY", SecType = "STK", Currency = "USD", Exchange = "SMART" }); // for TotM forecast, but it is not needed just yet
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "RUT", SecType = "IND", Currency = "USD", Exchange = "RUSSELL", LocalSymbol="RUT" });
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "UWM", SecType = "STK", Currency = "USD", Exchange = "SMART" });
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "TWM", SecType = "STK", Currency = "USD", Exchange = "SMART" });

                // for NeuralSniffer
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("^RUT"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("UWM"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TWM"));

                // for UberVXX
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("VXX"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("SVXY"));
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "SPY", SecType = "STK", Currency = "USD", Exchange = "SMART" }); // for TotM forecast, but it is not needed just yet

                // for HarryLong
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TQQQ"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("ZIV"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("VXZ"));  // needed until 2018-05-01, when this will be change back to ZIV for Agy

                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TMV"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TMF")); // needed until 2018-05-01, when this will be change back to TMV for Agy
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("UGAZ")); // instead of UNG, BOIL(2x)
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("UWT"));  // insteod of USO
                //m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("DWT"));  // temporary. Until short UWT is not available

                // for TAA, but it is only temporary. We will not stream this unnecessary data all day long, as TAA can take its time. It only trades MOC. Extra 2-3 seconds doesn't matter.
                // "TLT"+ "MDY","ILF","FEZ","EEM","EPP","VNQ","IBB"  +  "MVV", "URE", "BIB"
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TLT"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("MDY"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("ILF"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("FEZ"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("EEM"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("EPP"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("VNQ"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("IBB"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("MVV"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("URE"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("BIB"));
            }
            catch (Exception e)
            {
                Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways() in catching exception.");
                // .RunSynchronously(); only works if it is a delegate (or event). In an async task this is raised: "System.InvalidOperationException: RunSynchronously may not be called on a task not bound to a delegate"
                // https://medium.com/bynder-tech/c-why-you-should-use-configureawait-false-in-your-library-code-d7837dce3d7f
                // https://stackoverflow.com/questions/14485115/synchronously-waiting-for-an-async-operation-and-why-does-wait-freeze-the-pro
                // "Use ConfigureAwait(continueOnCapturedContext: false) as much as possible. This enables your async methods to continue executing without having to re-enter the context.
                // Use async all the way.Use await instead of Result or Wait"
                await HealthMonitorMessage.SendAsync($"Exception in ReConnectToGatewaysThread(), but we try to not intentionally crash the whole VBroker app. Exception: '{ e.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromVirtualBroker).ConfigureAwait(continueOnCapturedContext: false);
                //HealthMonitorMessage.SendAsync($"Exception in ReConnectToGatewaysThread(), but we try to not intentionally crash the whole VBroker app. Exception: '{ e.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromVirtualBroker).RunSynchronously();
                Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways() in catching exception and HealthMonitorMessage was sent.");
                Thread.Sleep(2000);  // TEMP: just wait 1 sec to leave time for loggers to log everything.
            }

            if (!m_isReady)
            {
                // Without all the IB connections (isAllConnected), we can choose to crash the App, but we do NOT do that, because we may be able to recover them later. 
                // It is a strategic (safety vs. conveniency) decision: in that case if not all IBGW is connected, (it can be an 'expected error'), VBroker runs further and try connecting every 10 min.

                // 2018-11-12: current state is: after failed IBGW connections, VBroker sends 1 HealthMonitor message and runs further (m_isReady = false), and it never tries to reconnect IBs. Reprogram that functionality later.
                // TODO: start a timer here to call ReconnectToGateways() 10 minutes later.   
                Utils.Logger.Warn("GatewaysWatcher:ReconnectToGateways(): m_isReady is FALSE. 'TODO: start a timer here to call ReconnectToGateways() 10 minutes later.'");
            }

            Utils.Logger.Info("GatewaysWatcher:ReconnectToGateways() END");
        }

        void ReconnectToGateway(object p_object)
        {
            Gateway gateway = (Gateway)p_object;
            int nMaxRetry = 3;
            int nConnectionRetry = 0;
            do
            {
                try
                {
                    nConnectionRetry++;
                    IBrokerWrapper ibWrapper = null;
                    if (!Controller.IsRunningAsLocalDevelopment())
                    {
                        ibWrapper = new BrokerWrapperIb(gateway.AccSumArrived, gateway.AccSumEnd, gateway.AccPosArrived, gateway.AccPosEnd);      // recreate IB wrapper at every Connection try. It is good this way.
                    }
                    else
                    {
                        bool isPreferIbAlltime = true;  // isPreferIbAlltime is used in general for functionality (RT price) development, but !isPreferIbAlltime is better for strategy developing (maybe)
                        if (isPreferIbAlltime || Utils.IsInRegularUsaTradingHoursNow(TimeSpan.FromDays(3)))
                            ibWrapper = new BrokerWrapperIb(gateway.AccSumArrived, gateway.AccSumEnd, gateway.AccPosArrived, gateway.AccPosEnd);    // when isPreferIbAlltime or when !isPreferIbAlltime, but USA market is open
                        else
                            ibWrapper = new BrokerWrapperYF();     // Before market open, or After market close. Simulated real time price is needed to determine current portfolio $size.
                    }
                    if (!ibWrapper.Connect(gateway.GatewayUser, gateway.SocketPort, gateway.BrokerConnectionClientID))
                    {
                        Utils.Logger.Error($"Timeout or other Error (like serverVersion=14). Cannot connect to IbGateway {gateway.GatewayUser} on port { gateway.SocketPort}. Trials: {nConnectionRetry}/{nMaxRetry}");
                        continue;
                    }

                    StrongAssert.Equal(ibWrapper.IbAccountsList, gateway.VbAccountsList, Severity.ThrowException, $"Expected IbAccount {gateway.VbAccountsList} is not found: { ibWrapper.IbAccountsList}.");

                    // after this line, we are really connected
                    gateway.BrokerWrapper = ibWrapper;
                    gateway.IsConnected = true;

                    string warnMessage = (ibWrapper is BrokerWrapperIb) ? "" : "!!!WARNING. Fake Broker (YF!). ";
                    Utils.Logger.Info($"{warnMessage}Gateway {ibWrapper} is connected. User {gateway.GatewayUser} acc {gateway.VbAccountsList}.");
                    Console.WriteLine($"*{warnMessage}Gateway user {gateway.GatewayUser} acc {gateway.VbAccountsList} connected.");
                    return;

                    //client.reqAccountSummary(9001, "All", AccountSummaryTags.GetAllTags());
                    /*** Subscribing to an account's information. Only one at a time! ***/
                    //Thread.Sleep(6000);

                }
                catch (Exception e)
                {
                    //If IBGateways doesn't connect: Retry the connection about 3 times, before Exception. So, so this problem is an Expected problem if another try to reconnect solves it.
                    Utils.Logger.Info(e, $"Exception in ReconnectToGateway()-user:{gateway.GatewayUser}: nRetry:{nConnectionRetry} : Msg:{e.Message}");
                    if (nConnectionRetry >= nMaxRetry)
                    {
                        Utils.Logger.Info("GatewaysWatcher:ReconnectToGateway(). This gateway failed after many retries. We could send HealthMonitor message here, but better at a higher level if the second Gateway fails too.");
                        //HealthMonitorMessage.SendException($"ReConnectToGateway Thread: nMaxRetry: {nMaxRetry}", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);  // the higher level ReconnectToGateways() will send the Error to HealthMonitor
                        throw;
                    }
                    else
                    {
                        // if we do retry, wait 10 seconds here. Maybe IB Gateway is will reconnect later
                        Thread.Sleep(10000);
                    }
                }
            } while (nConnectionRetry < nMaxRetry);
        }

        // at graceful shutdown, it is called
        public void Exit()
        {
            m_isReady = false;
            foreach (var gateway in m_gateways)
            {
                gateway.BrokerWrapper.Disconnect();
            }

            //PersistedState.Save();
            //StopTcpMessageListener();
        }

        internal bool IsGatewayConnected(GatewayUser p_ibGatewayUserToTrade)
        {
            var gateway = m_gateways.FirstOrDefault(r => r.GatewayUser == p_ibGatewayUserToTrade);
            if (gateway == null)
                return false;
            return (gateway.IsConnected);
        }


        internal bool GetMktDataSnapshot(Contract p_contract, ref Dictionary<int, PriceAndTime> p_quotes)
        {
            if (!m_isReady)
                return false;
            return m_mainGateway.BrokerWrapper.GetMktDataSnapshot(p_contract, ref p_quotes);
        }

        internal bool ReqHistoricalData(DateTime p_endDateTime, int p_lookbackWindowSize, string p_whatToShow, Contract p_contract, out List<QuoteData> p_quotes)
        {
            p_quotes = null;
            if (!m_isReady)
                return false;
            return m_mainGateway.BrokerWrapper.ReqHistoricalData(p_endDateTime, p_lookbackWindowSize, p_whatToShow, p_contract, out p_quotes);
        }

        internal int PlaceOrder(GatewayUser p_gatewayUserToTrade, double p_portfolioMaxTradeValueInCurrency, double p_portfolioMinTradeValueInCurrency, 
            Contract p_contract, TransactionType p_transactionType, double p_volume, OrderExecution p_orderExecution, OrderTimeInForce p_orderTif, double? p_limitPrice, double? p_stopPrice, bool p_isSimulatedTrades, StringBuilder p_detailedReportSb)
        {
            if (!m_isReady)
                return -1;

            Gateway userGateway = m_gateways.FirstOrDefault(r => r.GatewayUser == p_gatewayUserToTrade);
            if (userGateway == null)
            {
                Utils.Logger.Error($"ERROR. PlacingOrder(). GatewayUserToTrade {p_gatewayUserToTrade} is not found among connected Gateways.");
                return -1;
            }

            var rtPrices = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() } };   // MID is the most honest price. LAST may happened 1 hours ago
            m_mainGateway.BrokerWrapper.GetMktDataSnapshot(p_contract, ref rtPrices);
            int virtualOrderId = userGateway.PlaceOrder(p_portfolioMaxTradeValueInCurrency, p_portfolioMinTradeValueInCurrency, p_contract, p_transactionType, p_volume, p_orderExecution, p_orderTif, p_limitPrice, p_stopPrice, rtPrices[TickType.MID].Price, p_isSimulatedTrades, p_detailedReportSb);
            return virtualOrderId;
        }

        internal bool WaitOrder(GatewayUser p_gatewayUserToTrade, int p_virtualOrderId, bool p_isSimulatedTrades)
        {
            if (!m_isReady)
                return false;

            Gateway userGateway = m_gateways.FirstOrDefault(r => r.GatewayUser == p_gatewayUserToTrade);
            if (userGateway == null)
                return false;

            return userGateway.WaitOrder(p_virtualOrderId, p_isSimulatedTrades);
        }

        internal bool GetVirtualOrderExecutionInfo(GatewayUser p_gatewayUserToTrade, int p_virtualOrderId, ref OrderStatus orderStatus, ref double executedVolume, ref double executedAvgPrice, ref DateTime executionTime, bool p_isSimulatedTrades)
        {
            if (!m_isReady)
                return false;

            Gateway userGateway = m_gateways.FirstOrDefault(r => r.GatewayUser == p_gatewayUserToTrade);
            if (userGateway == null)
                return false;

            return userGateway.GetVirtualOrderExecutionInfo(p_virtualOrderId, ref orderStatus, ref executedVolume, ref executedAvgPrice, ref executionTime, p_isSimulatedTrades);
        }

        public string GetRealtimePriceService(string p_query)
        {
            if (!m_isReady || m_mainGateway == null)
                return null;
            return m_mainGateway.GetRealtimePriceService(p_query);
        }

        
    }
}
