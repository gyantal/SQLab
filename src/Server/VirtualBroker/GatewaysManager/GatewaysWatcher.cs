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

    
    

    public class GatewaysWatcher
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
                Gateway gateway1 = new Gateway(GatewayUser.GyantalMain, p_accountMaxTradeValueInCurrency: 48000 /* UberVXX is 12K, 2xleveraged=24K, double=48K*/, p_accountMaxEstimatedValueSumRecentlyAllowed: 75000) { VbAccountsList = "U407941", SocketPort = (int)GatewayUserPort.GyantalMain, BrokerConnectionClientID = 41 };
                if (!Controller.IsRunningAsLocalDevelopment())
                {
                    gateway2 = new Gateway(GatewayUser.CharmatSecondary, p_accountMaxTradeValueInCurrency: 300000.0 /* HarryLong is played 200K*65%=150K, double it */, p_accountMaxEstimatedValueSumRecentlyAllowed: 1000000  /* 1M */ ) { VbAccountsList = "U988767", SocketPort = (int)GatewayUserPort.CharmatSecondary, BrokerConnectionClientID = 42};
                    gateway3 = new Gateway(GatewayUser.TuSecondary, p_accountMaxTradeValueInCurrency: 7000.0 /* HarryLong is played 5K*65%=3.5K, double it */, p_accountMaxEstimatedValueSumRecentlyAllowed: 20000  /* 20K */ ) { VbAccountsList = "U1156489", SocketPort = (int)GatewayUserPort.TuSecondary, BrokerConnectionClientID = 43 };
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
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TVIX"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("TMV"));
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("UGAZ")); // instead of UNG, BOIL(2x)
                m_mainGateway.BrokerWrapper.ReqMktDataStream(VBrokerUtils.ParseSqTickerToContract("UWT"));  // insteod of USO

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
                HealthMonitorMessage.SendException("ReConnectToGateways Thread", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
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
                        ibWrapper = new BrokerWrapperIb();      // recreate IB wrapper at every Connection try. It is good this way.
                    }
                    else
                    {
                        if (Utils.IsInRegularUsaTradingHoursNow(TimeSpan.FromDays(3)))
                            ibWrapper = new BrokerWrapperIb();    // when USA market is open
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
                        Utils.Logger.Info("GatewaysWatcher:ReconnectToGateway(). This gateway failed after many retries. We can send HealthMonitor message here, but better at a higher level if the second Gateway fails too.");
                        //HealthMonitorMessage.SendException($"ReConnectToGateway Thread: nMaxRetry: {nMaxRetry}", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);  // the higher level ReconnectToGateways() will send the Error to HealthMonitor
                        throw; // without IB connection, we can crash the App. No point to continue. we cannot recover.
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
