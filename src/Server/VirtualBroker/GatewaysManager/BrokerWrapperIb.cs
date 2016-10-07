/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBApi;
using SqCommon;
using System.Collections.Concurrent;
using System.Threading;
using System.Globalization;
using DbCommon;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{

    public class BrokerWrapperIb : IBrokerWrapper
    {
        EClientSocket clientSocket;
        public readonly EReaderSignal Signal = new EReaderMonitorSignal();
        EReader m_eReader;
        private int nextOrderId;

        int m_reqMktDataIDseed = 1000;
        protected int GetUniqueReqMktDataID
        {
            get { return Interlocked.Increment(ref m_reqMktDataIDseed); }  // Increment gives back the incremented value, not the old value
        }

        int m_reqHistoricalDataIDseed = 1000;
        protected int GetUniqueReqHistoricalDataID
        {
            get { return Interlocked.Increment(ref m_reqHistoricalDataIDseed); }  // Increment gives back the incremented value, not the old value
        }

        public string IbAccountsList { get; set; }
        public ConcurrentDictionary<int, MktDataSubscription> MktDataSubscriptions { get; set; } = new ConcurrentDictionary<int, MktDataSubscription>();
        public ConcurrentDictionary<int, HistDataSubscription> HistDataSubscriptions { get; set; } = new ConcurrentDictionary<int, HistDataSubscription>();
        public ConcurrentDictionary<int, OrderSubscription> OrderSubscriptions { get; set; } = new ConcurrentDictionary<int, OrderSubscription>();

        public EClientSocket ClientSocket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }

        public int NextOrderId
        {
            get { return nextOrderId; }
            set { nextOrderId = value; }
        }


        public BrokerWrapperIb()
        {
            clientSocket = new EClientSocket(this, Signal);
        }


        public virtual bool Connect(int p_socketPort, int p_brokerConnectionClientID)
        {
            Utils.Logger.Info($"ClientSocket.eConnect(127.0.0.1, {p_socketPort}, {p_brokerConnectionClientID}, false)");
            ClientSocket.eConnect("127.0.0.1", p_socketPort, p_brokerConnectionClientID, false);
            //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
            m_eReader = new EReader(ClientSocket, Signal);
            m_eReader.Start();
            //Once the messages are in the queue, an additional thread need to fetch them. This is a very long running Thread, always waiting for all messages (Price, historicalData, etc.). This Thread calls the IbWrapper Callbacks.
            new Thread(() =>
            {
                try
                {
                    while (ClientSocket.IsConnected())
                    {
                        Signal.waitForSignal(); // the reader thread will sign the Signal
                        m_eReader.processMsgs();
                    }
                }
                catch (Exception e)
                {
                    if (Utils.MainThreadIsExiting.IsSet)
                        return; // if App is exiting gracefully, this Exception is not a problem
                    Utils.Logger.Error("Exception caught in Gateway Thread that is fetching messages. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                    throw;  // else, rethrow. This will Crash the App, which is OK. Without IB connection, there is no point to continue the VBroker App.
                }
            })
            { IsBackground = true }.Start();

            /*************************************************************************************************************************************************/
            /* One (although primitive) way of knowing if we can proceed is by monitoring the order's nextValidId reception which comes down automatically after connecting. */
            /*************************************************************************************************************************************************/
            //This is returned at Connection:
            //Account list: U1****6
            //Next Valid Id: 1
            DateTime startWaitConnection = DateTime.UtcNow;
            while (NextOrderId <= 0)
            {
                Thread.Sleep(100);
                if ((DateTime.UtcNow - startWaitConnection).TotalSeconds > 5.0)
                {
                    return false;
                }
            }
            return true;
        }



        public bool IsConnected()
        {
            return ClientSocket.IsConnected();
        }

        public virtual void Disconnect()
        {
            foreach (var item in MktDataSubscriptions)
            {
                ClientSocket.cancelMktData(item.Key);
            }
            m_eReader.Stop();
            ClientSocket.eDisconnect();
        }


        // Exception thrown: System.IO.EndOfStreamException: Unable to read beyond the end of the stream.     (if IBGateways are crashing down.)
        public virtual void error(Exception e)
        {
            Console.WriteLine("BrokerWrapperIb.error(). Exception: " + e);
            Utils.Logger.Info("BrokerWrapperIb.error(). Exception: " + e);  // exception.ToString() writes the Stacktrace, not only the Message, which is OK.

            // when VBroker server restarts every morning, the IBGateways are closed too, and as we were keeping a live TcpConnection, we will get an Exception here.
            // We cannot properly Close our connection in this case, because IBGateways are already shutting down.
            // Actually, This expected exception in the Background thread comes 5 mseconds before the ConsoleApp gets the Console.Readline() exception.
            // so in case of EndOfStreamException, let's sleep 100-500msec, and check that MainThread is exiting or not then.
            // 0405T06:15:04.481#14#5#Error: Unexpected BrokerWrapperIb.error(). Exception thrown: System.IO.EndOfStreamException: Unable to read beyond the end of the stream. at System.IO.BinaryReader.FillBuffer(Int32 numBytes)
            // 0405T06:15:04.485#1#5#Info: Console.ReadLine() Exception. Somebody closed the Terminal Window. Exception message: Input/output error
            // 0405T06: 15:04.498#1#5#Info: ****** Main() END
            // 0405T06: 15:04.534#1#5#Info: Connection closed.
            if (e is System.IO.EndOfStreamException)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(300));  // if it is a server reboot, probably during this time, the Main thread will exit anyway, which is OK, because we don't want to send Error report to HealthMonitor in that case.
                if (Utils.MainThreadIsExiting.IsSet)
                {
                    // an expected exception. Don't send Error message to HealthMonitor. This expected event happens every day.
                    Utils.Logger.Info("BrokerWrapperIb.error(). Expected exception, because 'Utils.MainThreadIsExiting.IsSet && e is System.IO.EndOfStreamException'" + e);
                    return;
                }
            }

            // if there is a Connection Error at the begginning, GatewaysWatcher will try to Reconnect 3 times. 
            // There is no point sending HealthManager.ErrorMessages and Phonecalls when the first Connection error occurs. GatewaysWatcher() will send it after the 3rd connection fails.
            if (e is System.AggregateException)
            {
                Utils.Logger.Info("BrokerWrapperIb.error(). AggregateException. Inner: " + e.InnerException.Message);
                if (e.InnerException.Message.IndexOf("Connection refused") != -1)
                {
                    Utils.Logger.Info("BrokerWrapperIb.error().  AggregateException. Inner exception is expected. Don't raise HealthMonitor alert phonecalls (yet)");
                    return;
                }
            }

            // Otherwise, maybe a trading Error, exception.
            // Maybe IBGateways were shut down. In which case, we cannot continue this VBroker App, because we should restart IBGateways, and reconnect to them by restarting VBroker.
            // Try to send HealthMonitor message and shut down the VBroker.
            Utils.Logger.Error("Unexpected BrokerWrapperIb.error(). Exception thrown: " + e);
            HealthMonitorMessage.SendException($"Unexpected  BrokerWrapperIb.error()", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
            throw e;    // this thread will terminate. Because we don't expect this exception. Safer to terminate thread, which will terminate App. The user probably has to restart IBGateways manually anyway.
        }

        public virtual void error(string p_str)
        {
            string errMsg = "BrokerWrapper.error(). " + p_str;
            Console.WriteLine(errMsg);
            Utils.Logger.Error(errMsg);
            HealthMonitorMessage.Send($"BrokerWrapperIb.error()", errMsg, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
            //If there is a single trading error, we may want to continue, so don't terminate the thread or the App, just inform HealthMonitor.
            //throw e;    // this thread will terminate. Because we don't expect this exception. Safer to terminate thread, which will terminate App. The user probably has to restart IBGateways manually anyway.
        }

        public virtual void error(int id, int errorCode, string errorMsg)
        {
            string errMsg = "ErrId: " + id + ", ErrCode: " + errorCode + ", Msg: " + errorMsg;
            Utils.Logger.Debug("BrokerWrapper.error(). " + errMsg); // even if we return and continue, Log it, so it is conserved in the log file.

            if (id == -1)       // -1 probably means there is no ID of the error. It is a special notation.
            {
                if (errorCode == 2104 || errorCode == 2106 || errorCode == 2107 || errorCode == 2108 || errorCode == 2119)
                {
                    // This is not an error. It is the messages at Connection: 
                    //IB Error. Id: -1, Code: 2104, Msg: Market data farm connection is OK:hfarm
                    //IB Error. Id: -1, Code: 2106, Msg: HMDS data farm connection is OK:ushmds.us
                    //IB Error. Id: -1, Code: 2107, Msg: HMDS data farm connection is inactive but should be available upon demand.ushmds
                    //IB Error. Id: -1, Code: 2108, Msg: HMDS data farm connection is inactive but should be available upon demand.ushmds
                    //IB Error. Id: -1, Code: 2119, Msg: Market data farm is connecting:usfarm
                    return; // skip processing the error further. Don't send it to HealthMonitor.
                }
                if (errorCode == 2103 || errorCode == 1100 || errorCode == 1102)
                {
                    // This is Usually not an error if it is received pre-market or after market. IBGateway will try reconnecting, so this is usually temporary. However, log it.
                    //IB Error. ErrId: -1, ErrCode: 2103, Msg: Market data farm connection is broken:usfarm
                    //IB Error. ErrId: -1, ErrCode: 1100, Msg: Connectivity between IB and Trader Workstation has been lost.
                    //IB Error. ErrId: -1, ErrCode: 1102, Msg: Connectivity between IB and Trader Workstation has been restored - data maintained.
                    if (!IsApproximatelyMarketTradingTime())
                        return; // skip processing the error further. Don't send it to HealthMonitor.


                    // otherwise, during market hours, consider this as an error, => so HealthMonitor will be notified
                }
            }

            


            if (errorCode == 200)
            {
                // sometimes it happens. When IB server is down. 99% of the time it is at the weekend
                // ErrId: 2165, ErrCode: 200, Msg: No security definition has been found for the request
                // ErrId: 2116, ErrCode: 200, Msg: No security definition has been found for the request
                // ErrId: 2144, ErrCode: 200, Msg: No security definition has been found for the request
                if (!IsApproximatelyMarketTradingTime())
                    return; // skip processing the error further. Don't send it to HealthMonitor.
            }

            // after subscribing to Market Snapshot data for a ticker, and we call ClientSocket.cancelMktData(p_marketDataId); that is executed properly
            // however IBGateway receives prices for the same ticker, and gives back the Error message here.
            // It occurs after alwayl all CannceMktData(). We should ignore it.
            // BrokerWrapper.error(). Id: 1010, Code: 300, Msg: Can't find EId with tickerId:1010
            if (errorCode == 300)
                return; // skip processing the error further. Don't send it to HealthMonitor.

            if (errorCode == 354)
            {
                // real-time price is queried. And Market data was subscribed, but at the weekend, it returns an error. Swallow it at the weekends.
                // ErrId: 1049, ErrCode: 354, Msg: Requested market data is not subscribed.
                if (!IsApproximatelyMarketTradingTime())
                    return; // skip processing the error further. Don't send it to HealthMonitor.
            }


            if (errorCode == 506)
            {
                if (id == 42)
                {
                    // sometimes it happens at connection. skip this error. IF Connection doesn't happen after trying it 3 times. VBGateway will notify HealthMonitor anyway.
                    // Once per month, this error happens, so the first connection fails, but the next connection goes perfectly through.
                    // ErrId: 42, ErrCode: 506, Msg: Unsupported version
                    return; // skip processing the error further. Don't send it to HealthMonitor.
                }
            }


            // SERIOUS ERRORS AFTER THIS LINE. Notify HealthMonitor.
            // after asking realtime price as "s=^VIX,^^^VIX201610,^^^VIX201611,^VXV,^^^VIX201701,VXX,^^^VIX201704&f=l"
            // Code: 200, Msg: The contract description specified for VIX is ambiguous; you must specify the multiplier or trading class.
            error(errMsg);
        }

        public bool IsApproximatelyMarketTradingTime()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime etNow = Utils.ConvertTimeFromUtcToEt(utcNow);
            if (etNow.DayOfWeek == DayOfWeek.Saturday || etNow.DayOfWeek == DayOfWeek.Sunday)   // if it is the weekend => no Error
                return false;

            // The NYSE and NYSE MKT are open from Monday through Friday 9:30 a.m. to 4:00 p.m. ET.
            if (etNow.Hour <= 8 || etNow.Hour >= 5)   // if it is not Approximately around market hours => no Error
                return false;

            // you can skip holiday days too later

            return true;
        }

        public virtual void connectionClosed()
        {
            Utils.Logger.Info("Connection closed.");
        }

        // https://www.interactivebrokers.co.uk/en/software/tws/usersguidebook/thetradingwindow/price-based.htm
        // MARK_PRICE: can be calculated. So, don't store it.
        //The mark price is equal to the LAST price unless:
        //Ask<Last - the mark price is equal to the ASK price.
        //Bid> Last - the mark price is equal to the BID price.
        //Mid price: can be calculated, don't store it. The midpoint between the current bid and ask.
        public bool GetPrice(Contract p_contract, int p_tickType, out double p_value)
        {
            p_value = Double.NaN;
            return false;
        }

        public virtual void currentTime(long time)
        {
            Console.WriteLine("Current Time: " + time);
        }

        public virtual int ReqMktDataStream(Contract p_contract, bool p_snapshot = false, MktDataSubscription.MktDataArrivedFunc p_mktDataArrivedFunc = null)
        {
            int marketDataId = GetUniqueReqMktDataID;
            Utils.Logger.Debug($"ReqMktDataStream() { marketDataId} START");
            ClientSocket.reqMarketDataType(2);    // 2: streaming data (for realtime), 1: frozen (for historical prices)
            //mainClient.reqMktData(marketDataId, contractSPY, "221", false, null);
            ClientSocket.reqMktData(marketDataId, p_contract, null, p_snapshot, null);

            var mktDataSubscr = new MktDataSubscription()
            {
                Contract = p_contract,
                MarketDataId = marketDataId,
                MarketDataArrived = p_mktDataArrivedFunc
            };
            MktDataSubscriptions.TryAdd(marketDataId, mktDataSubscr);

            // RUT index data comes once ever 5 seconds
            if (!p_snapshot)    // only if it is a continous streaming
                mktDataSubscr.CheckDataIsAliveTimer = new System.Threading.Timer(new TimerCallback(MktDataIsAliveTimer_Elapsed), mktDataSubscr, TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(-1.0));
            return marketDataId;
        }

        public virtual void CancelMktData(int p_marketDataId)
        {
            Utils.Logger.Debug($"CancelMktData() { p_marketDataId} START");
            // 1. at first, inform IBGateway to not send data
            ClientSocket.cancelMktData(p_marketDataId); // if p_snapshot = true, it is not necessarily to Cancel. However, it doesn't hurt.

            // 2. Only after informing IBGateway delete the record from our memory DB
            MktDataSubscription mktDataSubscription;
            MktDataSubscriptions.TryRemove(p_marketDataId, out mktDataSubscription);
            Utils.Logger.Debug($"CancelMktData() { p_marketDataId} END");
        }

        //- When streaming realtime price of Data for RUT, the very first time of the day, TWS gives price, but IBGateway does'nt give any price. 
        // I have to restart VBroker, so second VBroker run, there is a RUT price.
        //>Solution1: If real time price is not given in 5 minutes, cancel and ask realtime mktData again in the morning
        //	>this would work intraday too.After 20 seconds, we Subscribe to market data.
        //>Solution2: or if previous doesn't work, at least, ask mktData again 1 minutes after market Opened.
        //	>this wouldn't work if VBroker started after market Open, because ReSubscribe wouldn't be called.
        //>Solution3: or maybe do both previous ideas.
        // Good news: Solution1 worked perfectly. After cancelMktData() and reqMktData() again, RUT index data started to come instantly, 
        // but at 8.a.m CET, only last,lastClose,High/Low prices were given (there was no USA market). So, it was really connected the second time.
        // However, when USA market opened, at 14:30, RUT lastPrice data poured in at every 5 seconds.
        public void MktDataIsAliveTimer_Elapsed(object p_state)    // Timer is coming on a ThreadPool thread
        {
            try
            {
                MktDataSubscription mktDataSubscr = (MktDataSubscription)p_state;
                if (mktDataSubscr.IsAnyPriceArrived)  // we had at least 1 price, so everything seems ok.
                    return;

                Console.WriteLine($"DataIsAliveTimer_Elapsed(): No price found for {mktDataSubscr.Contract.Symbol}. Cancel and re-subscribe with the same marketDataId.");
                Utils.Logger.Info($"DataIsAliveTimer_Elapsed(): No price found for {mktDataSubscr.Contract.Symbol}. Cancel and re-subscribe with the same marketDataId.");
                ClientSocket.cancelMktData(mktDataSubscr.MarketDataId);
                ClientSocket.reqMarketDataType(2);    // 2: streaming data (for realtime), 1: frozen (for historical prices)
                ClientSocket.reqMktData(mktDataSubscr.MarketDataId, mktDataSubscr.Contract, null, false, null);     // use the same MarketDataId, so we don't have to update the MktDataSubscriptions dictionary.
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "MktDataIsAliveTimer_Elapsed() exception.");
                throw;
            }
        }

        public virtual bool GetMktDataSnapshot(Contract p_contract, ref Dictionary<int, PriceAndTime> p_quotes)
        {
            // Contract contract = new Contract() { Symbol = "VXX", SecType = "STK", Currency = "USD", Exchange = "SMART" };
            var mktDataSubscr = MktDataSubscriptions.Values.FirstOrDefault(r => VBrokerUtils.IsContractEqual(r.Contract, p_contract));
            if (mktDataSubscr == null)
            {
                Utils.Logger.Debug($"Market data for Contract {p_contract.Symbol} was not requested as Stream. Do make that request earlier.");
                return false;
            }

            ConcurrentDictionary<int, PriceAndTime> tickData = mktDataSubscr.Prices;
            lock (tickData) // don't lock for too long
            {
                foreach (var item in p_quotes)
                {
                    if (item.Key == TickType.MID)
                    {
                        PriceAndTime priceAndTimeAsk = null;
                        if (tickData.TryGetValue(TickType.ASK, out priceAndTimeAsk))
                        {
                            PriceAndTime priceAndTimeBid = null;
                            if (tickData.TryGetValue(TickType.BID, out priceAndTimeBid))
                            {
                                item.Value.Time = (priceAndTimeAsk.Time < priceAndTimeBid.Time) ? priceAndTimeAsk.Time : priceAndTimeBid.Time;  // use the older, smaller Time
                                item.Value.Price = (priceAndTimeAsk.Price + priceAndTimeBid.Price) / 2.0;
                            }
                        }
                    }
                    else
                    {
                        PriceAndTime priceAndTime = null;
                        if (tickData.TryGetValue(item.Key, out priceAndTime))
                        {
                            item.Value.Time = priceAndTime.Time;
                            item.Value.Price = priceAndTime.Price;
                        }
                    }
                }
            }

            bool isOk = true;
            foreach (var item in p_quotes)
            {
                if (Double.IsNaN(item.Value.Price))
                    isOk = false;   // expected behaviour. Imagine client asked for ASK, BID, LAST, but we only have LAST. In that case Price=NaN for ASK,BID, but we should return the LAST price

                if (item.Value.Price < 0.0)
                {
                    Utils.Logger.Warn($"Warning. Something is wrong. Price is negative. Returning False for GetMktDataSnapshot().");   // however, VBroker may want to continue, so don't throw Exception or do StrongAssert()
                    isOk = false;
                }
                // for daily High, Daily Low, Previous Close, etc. don't check this staleness
                bool doCheckDataStaleness = !Double.IsNaN(item.Value.Price) &&
                    (item.Key != TickType.LOW && item.Key != TickType.HIGH && item.Key != TickType.CLOSE);
                if (doCheckDataStaleness && (DateTime.UtcNow - item.Value.Time).TotalMinutes > 5.0)
                {
                    Utils.Logger.Warn($"Warning. Something may be wrong. We have the RT price of {item.Key} for '{p_contract.Symbol}' , but it is older than 5 minutes. Maybe Gateway was disconnected. Returning False for price.");
                    isOk = false;
                }
            }

            return isOk;
        }

        // After subscribing by reqMktData...
        //- mainClient.reqMktData(marketDataId, contractSPY, null, false, null);
        //- using "221" messages: mainClient.reqMktData(marketDataId, contractSPY, "221", false, null); results the same
        //- null      // give price+size
        //- 221 	Mark Price(used in TWS P&L computations) 	// give price+size+markPrice (even if there is no LastTrade, MarkPrice can change based on AskPrice, BidPrice)
        //- 225 	Auction values(volume, price and imbalance) // give price+size+AuctionValues
        //- 233 	RTVolume - contains the last trade price, last trade size, last trade time, total volume, VWAP, and single trade flag. // give price+size too
        // Conclusion: cannot get only AskPrice, BidPrice real-time data without the Size. AskSize, BidSize always comes with it. Fine.

        ////1. These are the received ticks after subscription to realtime data: (high/low/previousClose/Open)
        //Tick Price.Ticker Id:1001, Field: bidPrice, Price: 22.01, CanAutoExecute: 1
        //Tick Size. Ticker Id:1001, Field: bidSize, Size: 3

        //Tick Price. Ticker Id:1001, Field: askPrice, Price: 22.02, CanAutoExecute: 1
        //Tick Size. Ticker Id:1001, Field: askSize, Size: 217

        //Tick Price. Ticker Id:1001, Field: lastPrice, Price: 22.01, CanAutoExecute: 0
        //Tick Size. Ticker Id:1001, Field: lastSize, Size: 2

        //Tick Size. Ticker Id:1001, Field: bidSize, Size: 3
        //Tick Size. Ticker Id:1001, Field: askSize, Size: 217
        //Tick Size. Ticker Id:1001, Field: lastSize, Size: 2

        //Tick Size. Ticker Id:1001, Field: volume, Size: 724693
        //Tick Price. Ticker Id:1001, Field: high, Price: 22.08, CanAutoExecute: 0
        //Tick Price. Ticker Id:1001, Field: low, Price: 20.95, CanAutoExecute: 0
        //Tick Price. Ticker Id:1001, Field: close, Price: 21.59, CanAutoExecute: 0
        //Tick Price. Ticker Id:1001, Field: open, Price: 21.31, CanAutoExecute: 0
        //Tick string. Ticker Id:1001, Type: lastTimestamp, Value: 1457126686

        ////2. These were the initial values. Later, this is the regular changes that comes 
        //Tick Generic. Ticker Id:1001, Field: halted, Value: 0
        //Tick Size. Ticker Id:1001, Field: askSize, Size: 206
        //Tick Generic. Ticker Id:1001, Field: halted, Value: 0
        //Tick Size. Ticker Id:1001, Field: volume, Size: 724722
        //Tick Size. Ticker Id:1001, Field: askSize, Size: 204
        //Tick Size. Ticker Id:1001, Field: bidSize, Size: 8
        public virtual void tickPrice(int tickId, int field, double price, int canAutoExecute)
        {
            // This is from old VBrokers code
            //  System.Diagnostics.Trace.WriteLine(DateTime.UtcNow.ToString("MM-dd HH:mm:ss.fff") + ": " + String.Format("TickPriceCB(): {0}/{1}/{2}/{3} ", e.TickerId, e.TickType, e.Price, e.CanAutoExecute));
            //if ((double)e.Price <= 0)        // drop 0 or -1 prices; they are meaningless
            //    return;

            //lock (tickData)
            //{
            //    if (tickData.ContainsKey(e.TickType))     // the Decimal is 20x slower than float, we don't use it: http://gregs-blog.com/2007/12/10/dot-net-decimal-type-vs-float-type/
            //    {
            //        if (m_priceTickARE != null)
            //            m_priceTickARE.Set();     // AutoResetEvent to notify Sleeping Listeners, but I will do it with Reactive Programming later.

            //        tickData[e.TickType].Price = (double)e.Price;
            //        tickData[e.TickType].Time = DateTime.UtcNow;
            //    }
            //}
            // instead of Thread.Sleep(2000);  // wait until data is here; TODO: make it sophisticated later
            //RtpAppController.gBrokerAPI.m_priceTickARE.Reset();
            //Console.WriteLine("Tick Price. Ticker Id:"+tickerId+", Field: "+ TickType.getField(field) + ", Price: " +price+", CanAutoExecute: "+canAutoExecute);
            // Logger.Warn will put it to the Console too. Temporary
            //Utils.Logger.Warn("Tick Price. Ticker Id:" + tickId + ", Field: " + TickType.getField(field) + ", Price: " + price + ", CanAutoExecute: " + canAutoExecute);
            Utils.Logger.Info("Tick Price. Tick Id:" + tickId + ", Field: " + TickType.getField(field) + ", Price: " + price + ", CanAutoExecute: " + canAutoExecute);

            MktDataSubscription mktDataSubscription = null;
            if (!MktDataSubscriptions.TryGetValue(tickId, out mktDataSubscription))
            {
                Utils.Logger.Debug($"tickPrice(). MktDataSubscription tickerID { tickId} is not expected. Although IBGateway can send some prices even after CancelMktData was sent to IBGateway.");
                return;
            }

            if (!mktDataSubscription.IsAnyPriceArrived)
            {
                Console.WriteLine($"Firstprice: {mktDataSubscription.Contract.Symbol}, {TickType.getField(field)}, {price}");
                mktDataSubscription.IsAnyPriceArrived = true;
            }

            //if (mktDataSubscription.Contract.Symbol == "RUT")   // temporary: for debugging purposes
            //{
            //    Console.WriteLine($"RUT: {mktDataSubscription.Contract.Symbol}, {TickType.getField(field)}, {price}");
            //}

            ConcurrentDictionary<int, PriceAndTime> tickData = mktDataSubscription.Prices;
            lock (tickData)
            {
                if (tickData.ContainsKey(field))     // the Decimal is 20x slower than float, we don't use it: http://gregs-blog.com/2007/12/10/dot-net-decimal-type-vs-float-type/
                {
                    tickData[field].Price = price;
                    tickData[field].Time = DateTime.UtcNow;
                }
                else
                    Console.WriteLine("Tick Price. Ticker Id:" + tickId + ", Field: " + TickType.getField(field) + ", Price: " + price + ", CanAutoExecute: " + canAutoExecute);
            }

            if (mktDataSubscription.MarketDataArrived != null)
                mktDataSubscription.MarketDataArrived(tickId, mktDataSubscription, field, price);
        }


        public virtual void tickSize(int tickerId, int field, int size)
        {
            // we don't need the AskSize, BidSize, LastSize values, so we don't process them unnecessarily.
            //Console.WriteLine("Tick Size. Ticker Id:" + tickerId + ", Field: " + TickType.getField(field)  + ", Size: " + size);
        }

        public virtual void tickString(int tickerId, int tickType, string p_value)
        {
            Utils.Logger.Info("Tick string. Ticker Id:" + tickerId + ", Type: " + TickType.getField(tickType) + ", Value: " + p_value);
            // lastTimestamp example: "1303329585"
            if (tickType == TickType.LAST_TIMESTAMP)
            {
                MktDataSubscription mktDataSubscription = null;
                if (!MktDataSubscriptions.TryGetValue(tickerId, out mktDataSubscription))
                {
                    Utils.Logger.Debug($"tickString(). MktDataSubscription tickerID { tickerId} is not expected. Although IBGateway can send some prices even after CancelMktData was sent to IBGateway.");
                    return;
                }
                mktDataSubscription.LastTimestampStr = p_value;
            }
            else
                Console.WriteLine("Tick string. Ticker Id:" + tickerId + ", Type: " + TickType.getField(tickType) + ", Value: " + p_value);
        }

        public virtual void tickGeneric(int tickerId, int field, double value)
        {
            Utils.Logger.Info("Tick Generic. Ticker Id:" + tickerId + ", Field: " + TickType.getField(field) + ", Value: " + value);
            if (field == TickType.HALTED)
            {
                //https://www.interactivebrokers.co.uk/en/software/api/apiguide/tables/tick_types.htm
                //0 = Not halted
                //1 = General halt(trading halt is imposed for purely regulatory reasons) with / without volatility halt.
                //2 = Volatility only halt (trading halt is imposed by the exchange to protect against extreme volatility).
                if (value > 0.0)
                {
                    Utils.Logger.Warn("Trading is halted. Tick Generic. Ticker Id:" + tickerId + ", Field: " + TickType.getField(field) + ", Value: " + value);
                }
                return;
            } else
                Console.WriteLine("Tick Generic. Ticker Id:" + tickerId + ", Field: " + TickType.getField(field) + ", Value: " + value);
        }

        public virtual void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            Console.WriteLine("TickEFP. " + tickerId + ", Type: " + tickType + ", BasisPoints: " + basisPoints + ", FormattedBasisPoints: " + formattedBasisPoints + ", ImpliedFuture: " + impliedFuture + ", HoldDays: " + holdDays + ", FutureLastTradeDate: " + futureLastTradeDate + ", DividendImpact: " + dividendImpact + ", DividendsToLastTradeDate: " + dividendsToLastTradeDate);
        }

        public virtual void tickSnapshotEnd(int tickerId)
        {
            Console.WriteLine("TickSnapshotEnd: " + tickerId);
        }

        public virtual void nextValidId(int orderId)
        {
            //Console.WriteLine("Next Valid Id: "+orderId);
            NextOrderId = orderId;
        }

        public virtual void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            Console.WriteLine("DeltaNeutralValidation. " + reqId + ", ConId: " + underComp.ConId + ", Delta: " + underComp.Delta + ", Price: " + underComp.Price);
        }

        public virtual void managedAccounts(string accountsList)
        {
            IbAccountsList = accountsList;
            //Console.WriteLine("Account list: "+accountsList);
        }

        public virtual void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            Console.WriteLine("TickOptionComputation. TickerId: " + tickerId + ", field: " + field + ", ImpliedVolatility: " + impliedVolatility + ", Delta: " + delta
                + ", OptionPrice: " + optPrice + ", pvDividend: " + pvDividend + ", Gamma: " + gamma + ", Vega: " + vega + ", Theta: " + theta + ", UnderlyingPrice: " + undPrice);
        }

        public virtual void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            Console.WriteLine("Acct Summary. ReqId: " + reqId + ", Acct: " + account + ", Tag: " + tag + ", Value: " + value + ", Currency: " + currency);
        }

        public virtual void accountSummaryEnd(int reqId)
        {
            Console.WriteLine("AccountSummaryEnd. Req Id: " + reqId);
        }

        public virtual void updateAccountValue(string key, string value, string currency, string accountName)
        {
            Console.WriteLine("UpdateAccountValue. Key: " + key + ", Value: " + value + ", Currency: " + currency + ", AccountName: " + accountName);
        }

        public virtual void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {
            Console.WriteLine("UpdatePortfolio. " + contract.Symbol + ", " + contract.SecType + " @ " + contract.Exchange
                + ": Position: " + position + ", MarketPrice: " + marketPrice + ", MarketValue: " + marketValue + ", AverageCost: " + averageCost
                + ", UnrealisedPNL: " + unrealisedPNL + ", RealisedPNL: " + realisedPNL + ", AccountName: " + accountName);
        }

        public virtual void updateAccountTime(string timestamp)
        {
            Console.WriteLine("UpdateAccountTime. Time: " + timestamp);
        }

        public virtual void accountDownloadEnd(string account)
        {
            Console.WriteLine("Account download finished: " + account);
        }

        public virtual void orderStatus(int p_realOrderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            Utils.Logger.Info("OrderStatus. Id: " + p_realOrderId + ", Status: " + status + ", Filled" + filled + ", Remaining: " + remaining
                + ", AvgFillPrice: " + avgFillPrice + ", PermId: " + permId + ", ParentId: " + parentId + ", LastFillPrice: " + lastFillPrice + ", ClientId: " + clientId + ", WhyHeld: " + whyHeld);

            OrderSubscription orderSubscription = null;
            if (!OrderSubscriptions.TryGetValue(p_realOrderId, out orderSubscription))
            {
                Utils.Logger.Error($"OrderSubscription orderId {p_realOrderId} is not expected");
                return;
            }

            OrderStatus orderStatus;
            if (!Enum.TryParse<OrderStatus>(status, true, out orderStatus))
            {
                orderStatus = OrderStatus.Unrecognized;
                Utils.Logger.Error($"Order status string {status} was not recognised to Enum. We still continue.");
            }
            orderSubscription.DateTime = DateTime.UtcNow;
            orderSubscription.OrderStatus = orderStatus;
            orderSubscription.Filled = filled;
            orderSubscription.Remaining = remaining;
            orderSubscription.AvgFillPrice = avgFillPrice;
            orderSubscription.PermId = permId;
            orderSubscription.ParentId = parentId;
            orderSubscription.LastFillPrice = lastFillPrice;
            orderSubscription.ClientId = clientId;
            orderSubscription.WhyHeld = whyHeld;

            if (String.Equals(status, "Filled", StringComparison.CurrentCultureIgnoreCase))
                orderSubscription.AutoResetEvent.Set();  // signal to other thread
        }

        // "Feeds in currently open orders." We can subscribe to all the current OrdersInfo. For MOC orders for example, before PlaceOrder() we should check that if there is already an MOC order then we Modify that (or Cancel&Recreate)
        public virtual void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            Utils.Logger.Info("OpenOrder. ID: " + orderId + ", " + contract.Symbol + ", " + contract.SecType + " @ " + contract.Exchange + ": " + order.Action + ", " + order.OrderType + " " + order.TotalQuantity + ", " + orderState.Status);
        }

        public virtual void openOrderEnd()
        {
            Utils.Logger.Info("OpenOrderEnd");
        }

        public int PlaceOrder(Contract p_contract, TransactionType p_transactionType, double p_volume, OrderExecution p_orderExecution, OrderTimeInForce p_orderTif, double? p_limitPrice, double? p_stopPrice, double p_estimatedPrice, bool p_isSimulatedTrades)
        {

            Order order = new Order();
            switch (p_transactionType)
            {
                case TransactionType.BuyAsset:
                    order.Action = "BUY";
                    break;
                case TransactionType.SellAsset:
                    order.Action = "SELL";
                    break;
                default:
                    throw new Exception($"Unexpected transactionType: {p_transactionType}");
            }

            order.TotalQuantity = p_volume;
            if (p_limitPrice != null)
                order.LmtPrice = (double)p_limitPrice;

            switch (p_orderExecution)
            {
                case OrderExecution.Market:
                    order.OrderType = "MKT";
                    break;
                case OrderExecution.MarketOnClose:
                    order.OrderType = "MOC";
                    break;
                default:
                    throw new Exception($"Unexpected OrderExecution: {p_orderExecution}");
            }

            switch (p_orderTif)
            {
                case OrderTimeInForce.Day:  // Day is the default, so don't do anything
                    order.Tif = "DAY";
                    break;
                default:
                    throw new Exception($"Unexpected OrderTimeInForce: {p_orderTif}");
            }

            int p_realOrderId = NextOrderId++;
            OrderSubscriptions.TryAdd(p_realOrderId, new OrderSubscription() { Contract = p_contract, Order = order });
            if (!p_isSimulatedTrades)
                ClientSocket.placeOrder(p_realOrderId, p_contract, order);
            return p_realOrderId;
        }

        public bool WaitOrder(int p_realOrderId, bool p_isSimulatedTrades)
        {
            // wait here
            OrderSubscription orderSubscription = null;
            if (!OrderSubscriptions.TryGetValue(p_realOrderId, out orderSubscription))
            {
                Utils.Logger.Error($"OrderSubscription orderId {p_realOrderId} is not expected");
                return false;
            }

            string orderType = orderSubscription.Order.OrderType;

            if (p_isSimulatedTrades)    // for simulated orders, pretend its is executed already, even for MOC orders, because the BrokerTask that Simulates intraday doesn't want to wait until it is finished at MarketClose
                return true;

            if (orderType == "MKT")
            {
                bool signalReceived = orderSubscription.AutoResetEvent.WaitOne(TimeSpan.FromMinutes(2)); // timeout of 2 minutes. Don't wait forever, because that will consume this thread forever
                if (!signalReceived)
                    return false;   // if it was a timeout
            } else if (orderType == "MOC")
            {
                // calculate times until MarketClose and wait max 2 minutes after that
                bool isMarketTradingDay;
                DateTime marketOpenTimeUtc, marketCloseTimeUtc;
                bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(DateTime.UtcNow, out isMarketTradingDay, out marketOpenTimeUtc, out marketCloseTimeUtc, TimeSpan.FromDays(3));
                if (!isTradingHoursOK)
                {
                    Utils.Logger.Error("WaitOrder().DetermineUsaMarketTradingHours() was not ok.");
                    return false;
                }
                if (!isMarketTradingDay)
                {
                    Utils.Logger.Error("WaitOrder().isMarketTradingDay is false. That is impossible. Order shouldn't have been placed.");
                    return false;
                }
                DateTime marketClosePlusExtra = marketCloseTimeUtc.AddMinutes(2);
                Utils.Logger.Info($"WaitOrder() waits until {marketClosePlusExtra.ToString("HH:mm:ss")}");
                TimeSpan timeToWait = marketClosePlusExtra - DateTime.UtcNow;
                if (timeToWait < TimeSpan.Zero)
                    return true;

                bool signalReceived = orderSubscription.AutoResetEvent.WaitOne(timeToWait); // timeout of 2 minutes. Don't wait forever, because that will consume this thread forever
                if (!signalReceived)
                    return false;   // if it was a timeout
            }

            return true;
        }

        public bool GetRealOrderExecutionInfo(int p_realOrderId, ref OrderStatus p_realOrderStatus, ref double p_realExecutedVolume, ref double p_realExecutedAvgPrice, ref DateTime p_execptionTime, bool p_isSimulatedTrades)
        {
            OrderSubscription orderSubscription = null;
            if (!OrderSubscriptions.TryGetValue(p_realOrderId, out orderSubscription))
            {
                Utils.Logger.Error($"OrderSubscription orderId {p_realOrderId} is not expected");
                return false;
            }

            if (p_isSimulatedTrades)    // there was no orderStatus(), so just fake one
            {
                p_realOrderStatus = OrderStatus.Filled;
                p_realExecutedVolume = orderSubscription.Order.TotalQuantity;    // maybe less is filled than it was required...
                p_realExecutedAvgPrice = 1.0;   // assume we bought it for $1.0 each // we can do RealTime price or YahooEstimated price, or lastDay Closeprice later if it is required
                p_execptionTime = DateTime.UtcNow;
                return true;
            }

            p_realOrderStatus = orderSubscription.OrderStatus;
            p_realExecutedVolume = orderSubscription.Filled;    // maybe less is filled than it was required...
            p_realExecutedAvgPrice = orderSubscription.AvgFillPrice;
            p_execptionTime = orderSubscription.DateTime;
            return true;
        }


        public virtual void contractDetails(int reqId, ContractDetails contractDetails)
        {
            Console.WriteLine("ContractDetails. ReqId: " + reqId + " - " + contractDetails.Summary.Symbol + ", " + contractDetails.Summary.SecType + ", ConId: " + contractDetails.Summary.ConId + " @ " + contractDetails.Summary.Exchange);
        }

        public virtual void contractDetailsEnd(int reqId)
        {
            Console.WriteLine("ContractDetailsEnd. " + reqId);
        }

        public virtual void execDetails(int reqId, Contract contract, Execution execution)
        {
            //Console.WriteLine("ExecutionDetails. " + reqId + " - " + contract.Symbol + ", " + contract.SecType + ", " + contract.Currency + " - " + execution.ExecId + ", " + execution.OrderId + ", " + execution.Shares);
            Utils.Logger.Info("ExecutionDetails. ReqId:" + reqId + " - " + contract.Symbol + ", " + contract.SecType + ", " + contract.Currency + " ,executionId: " + execution.ExecId + ", orderID:" + execution.OrderId + ", nShares:" + execution.Shares);
        }

        public virtual void execDetailsEnd(int reqId)
        {
            Console.WriteLine("ExecDetailsEnd. " + reqId);
        }

        public virtual void commissionReport(CommissionReport commissionReport)
        {
            //Console.WriteLine("CommissionReport. " + commissionReport.ExecId + " - " + commissionReport.Commission + " " + commissionReport.Currency + " RPNL " + commissionReport.RealizedPNL);
            Utils.Logger.Info("CommissionReport. " + commissionReport.ExecId + " - " + commissionReport.Commission + " " + commissionReport.Currency + " RPNL " + commissionReport.RealizedPNL);
        }

        public virtual void fundamentalData(int reqId, string data)
        {
            Console.WriteLine("FundamentalData. " + reqId + "" + data);
        }

        public virtual void marketDataType(int reqId, int marketDataType)
        {
            // marketDataType 1 for real time, 2 for frozen
            // if we ask m_mainGateway.BrokerWrapper.ReqMktDataStream(new Contract() { Symbol = "RUT", SecType = "IND", Currency = "USD", Exchange = "RUSSELL" });,
            // then After market Close, there is no more realtime price, and this call back tells us that it has a marketDataType=2, which is an Index
            Utils.Logger.Info("MarketDataType. " + reqId + ", Type: (1 for real time, 2 for frozen (Index after MarketClose)) " + marketDataType);
        }

        public virtual void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            Console.WriteLine("UpdateMarketDepth. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + price + ", Size" + size);
        }

        public virtual void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            Console.WriteLine("UpdateMarketDepthL2. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + price + ", Size" + size);
        }


        public virtual void updateNewsBulletin(int msgId, int msgType, String message, String origExchange)
        {
            Console.WriteLine("News Bulletins. " + msgId + " - Type: " + msgType + ", Message: " + message + ", Exchange of Origin: " + origExchange);
        }

        public virtual void position(string account, Contract contract, double pos, double avgCost)
        {
            Console.WriteLine("Position. " + account + " - Symbol: " + contract.Symbol + ", SecType: " + contract.SecType + ", Currency: " + contract.Currency + ", Position: " + pos + ", Avg cost: " + avgCost);
        }

        public virtual void positionEnd()
        {
            Console.WriteLine("PositionEnd \n");
        }

        public virtual void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            Console.WriteLine("RealTimeBars. " + reqId + " - Time: " + time + ", Open: " + open + ", High: " + high + ", Low: " + low + ", Close: " + close + ", Volume: " + volume + ", Count: " + count + ", WAP: " + WAP);
        }

        public virtual void scannerParameters(string xml)
        {
            Console.WriteLine("ScannerParameters. " + xml);
        }

        public virtual void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            Console.WriteLine("ScannerData. " + reqId + " - Rank: " + rank + ", Symbol: " + contractDetails.Summary.Symbol + ", SecType: " + contractDetails.Summary.SecType + ", Currency: " + contractDetails.Summary.Currency
                + ", Distance: " + distance + ", Benchmark: " + benchmark + ", Projection: " + projection + ", Legs String: " + legsStr);
        }

        public virtual void scannerDataEnd(int reqId)
        {
            Console.WriteLine("ScannerDataEnd. " + reqId);
        }

        public virtual void receiveFA(int faDataType, string faXmlData)
        {
            Console.WriteLine("Receing FA: " + faDataType + " - " + faXmlData);
        }

        public virtual void bondContractDetails(int requestId, ContractDetails contractDetails)
        {
            Console.WriteLine("Bond. Symbol " + contractDetails.Summary.Symbol + ", " + contractDetails.Summary);
        }


        // Restrictions:
        // https://www.interactivebrokers.co.uk/en/software/api/apiguide/tables/historical_data_limitations.htm
        // 1. IB Error. Id: 4001, Code: 321, Msg: Error validating request:-'yd' : cause - Historical data request for greater than 365 days rejected.
        // http://www.elitetrader.com/et/index.php?threads/interactive-brokers-maximum-60-historical-data-requests-in-10-minutes.275746/
        // 2. interactive brokers maximum 60 historical data requests in 10 minutes
        // http://www.elitetrader.com/et/index.php?threads/interactive-broker-historical-prices-dividend-adjustment.280815/
        // 3. it is split adjusted (I have checked with FRO for 1:5 split on 2016-02-03), but if dividend is less than 10%, it is not adjusted.
        //"for stock split (and dividend shares of more than 10%), the stock price will be adjusted by the "PAR" value denominator, market cap is the same (shares floating x price) but the price and number of shares outstanding will be adjusted."
        // the returned p_quotes in the last value contains the last realTime price. It comes as CLOSE price for today, but during intraday, this is the realTime lastprice.
        // 4. https://www.interactivebrokers.co.uk/en/software/api/apiguide/tables/historical_data_limitations.htm
        // The following table lists the valid whatToShow values based on the corresponding products. for Index, only TRADES is allowed
        // 5. One of the most important problem: when it is used from one IBGateway on Linux/Windows, later it doesn't work from the other server. Usually works for Stocks, 
        // but for RUT Index, I had a hard time. very unreliable. It is better to get historical data from YF or from our DB. (later I need HistData from many stocks or more than 1 year)
        // 6. read the IBGatewayHistoricalData.txt, but as an essence:
        //maybe, because it is Friday, midnight, that is why RUT historical in unreliable, but the conclusion is:
        //You can use IB historical data: 	>for stocks 	>popular indices, like SPX, but not the RUT.
        //>So, for RUT, implement getting historical from our SQL DB.
        // 7. checked that: the today (last day) of IB.ReqHistoricalData() is not always correct. And it is not always the last real time price. It only works 90% of the time.
        //      2016-07-05: after a 3 days weekend: "Historical data end - 1001 from 20160105  13:50:01 to 20160705  13:50:01 ", and that time (before Market open), realtime price was 13.39.
        //         later on that day, it always give 13.39 for today's last price in ClientSocket.reqHistoricalData. So, don't trust the last day. Asks for a real time price separately from stream.
        //      2016-09-06: the same happend after the 2 days weekend of Labour day. "Historical data end: Date: 20160906, Close: 34.8"
        public virtual bool ReqHistoricalData(DateTime p_endDateTime, int p_lookbackWindowSize, string p_whatToShow, Contract p_contract, out List<QuoteData> p_quotes)
        {
            p_quotes = null;
            int histDataId = GetUniqueReqHistoricalDataID;

            //Console.WriteLine($"ReqHistoricalData() for {p_contract.Symbol}, reqId: {histDataId}");
            Utils.Logger.Info($"ReqHistoricalData() for {p_contract.Symbol}, reqId: {histDataId}");

            // durationString = "60 D" is fine, but "61 D" gives the following error "Historical Market Data Service error message:Time length exceed max.", so after 60, change to Months "3 M" or "11 M"
            string durationString = (p_lookbackWindowSize <= 60) ? $"{p_lookbackWindowSize} D" : $"{p_lookbackWindowSize / 20 + 1} M"; // dividing by int rounds it down. But we want to round it up, so add 1.
            var histDataSubsc = new HistDataSubscription() { Contract = p_contract, QuoteData = new List<QuoteData>(p_lookbackWindowSize) };
            HistDataSubscriptions.TryAdd(histDataId, histDataSubsc);

            //durationString = "5 D";

            ClientSocket.reqHistoricalData(histDataId, p_contract, p_endDateTime.ToString("yyyyMMdd HH:mm:ss"), durationString, "1 day", p_whatToShow, 1, 1, null);    // with daily data formatDate is always "yyyyMMdd", no seconds, and param=2 doesn't give seconds

            // wait here
            bool signalReceived = histDataSubsc.AutoResetEvent.WaitOne(TimeSpan.FromSeconds(10)); // timeout of 10 seconds

            // clean up resources after data arrived
            ClientSocket.cancelHistoricalData(histDataId);

            HistDataSubscription histDataToRemove = null;
            HistDataSubscriptions.TryRemove(histDataId, out histDataToRemove);
            histDataSubsc.AutoResetEvent.Dispose();     // ! AutoResetEvent has a Dispose

            if (!signalReceived)
            {
                Utils.Logger.Error($"ReqHistoricalData() timeout for {p_contract.Symbol}");
                return false;   // if it was a timeout
            }

            if (histDataSubsc.QuoteData.Count > p_lookbackWindowSize)   // if we got too much data, remove the old ones. Very likely it only do shallow copy of values, but no extra memory allocation is required
                histDataSubsc.QuoteData.RemoveRange(0, histDataSubsc.QuoteData.Count - p_lookbackWindowSize);

            p_quotes = histDataSubsc.QuoteData;
            return true;
        }

        public virtual void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            //Console.WriteLine("HistoricalData. " + reqId + " - Date: " + date + ", Open: " + open + ", High: " + high + ", Low: " + low + ", Close: " + close + ", Volume: " + volume + ", Count: " + count + ", WAP: " + WAP + ", HasGaps: " + hasGaps);
            Utils.Logger.Trace("HistoricalData. " + reqId + " - Date: " + date + ", Open: " + open + ", High: " + high + ", Low: " + low + ", Close: " + close + ", Volume: " + volume + ", Count: " + count + ", WAP: " + WAP + ", HasGaps: " + hasGaps);

            HistDataSubscription histDataSubscription = null;
            if (!HistDataSubscriptions.TryGetValue(reqId, out histDataSubscription))
            {
                Utils.Logger.Error($"HistDataSubscriptions reqId { reqId} is not expected");
                return;
            }
            histDataSubscription.QuoteData.Add(new QuoteData() { Date = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture), AdjClosePrice = close });
        }

        public virtual void historicalDataEnd(int reqId, string startDate, string endDate)
        {
            //Console.WriteLine("Historical data end - " + reqId + " from " + startDate + " to " + endDate);
            Utils.Logger.Trace("Historical data end - " + reqId + " from " + startDate + " to " + endDate);

            HistDataSubscription histDataSubscription = null;
            if (!HistDataSubscriptions.TryGetValue(reqId, out histDataSubscription))
            {
                Utils.Logger.Error($"HistDataSubscriptions reqId { reqId} is not expected");
                return;
            }
            histDataSubscription.AutoResetEvent.Set();  // signal to other thread
        }


        public virtual void verifyMessageAPI(string apiData)
        {
            Console.WriteLine("verifyMessageAPI: " + apiData);
        }
        public virtual void verifyCompleted(bool isSuccessful, string errorText)
        {
            Console.WriteLine("verifyCompleted. IsSuccessfule: " + isSuccessful + " - Error: " + errorText);
        }
        public virtual void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            Console.WriteLine("verifyAndAuthMessageAPI: " + apiData + " " + xyzChallenge);
        }
        public virtual void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            Console.WriteLine("verifyAndAuthCompleted. IsSuccessful: " + isSuccessful + " - Error: " + errorText);
        }
        public virtual void displayGroupList(int reqId, string groups)
        {
            Console.WriteLine("DisplayGroupList. Request: " + reqId + ", Groups" + groups);
        }
        public virtual void displayGroupUpdated(int reqId, string contractInfo)
        {
            Console.WriteLine("displayGroupUpdated. Request: " + reqId + ", ContractInfo: " + contractInfo);
        }
        public virtual void positionMulti(int reqId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            Console.WriteLine("Position Multi. Request: " + reqId + ", Account: " + account + ", ModelCode: " + modelCode + ", Symbol: " + contract.Symbol + ", SecType: " + contract.SecType + ", Currency: " + contract.Currency + ", Position: " + pos + ", Avg cost: " + avgCost + "\n");
        }
        public virtual void positionMultiEnd(int reqId)
        {
            Console.WriteLine("Position Multi End. Request: " + reqId + "\n");
        }
        public virtual void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency)
        {
            Console.WriteLine("Account Update Multi. Request: " + reqId + ", Account: " + account + ", ModelCode: " + modelCode + ", Key: " + key + ", Value: " + value + ", Currency: " + currency + "\n");
        }
        public virtual void accountUpdateMultiEnd(int reqId)
        {
            Console.WriteLine("Account Update Multi End. Request: " + reqId + "\n");
        }

        public void connectAck()
        {
            //Console.WriteLine($"connectAck()");
            if (ClientSocket.AsyncEConnect)
                ClientSocket.startApi();
        }

    }
}
