using DbCommon;
using IBApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{
    
    public class PriceAndTime      // it makes sense to store Time, because values older than X hours cannot be used, even if they are stored in the RAM
    {
        public double Price = Double.NaN;
        public DateTime Time = new DateTime(1950, 1, 1);    // a very old date

        public bool IsTimeUnchanged()
        {
            return Time == new DateTime(1950, 1, 1);
        }
    }

    public class MktDataSubscription
    {
        public delegate void MktDataArrivedFunc(int p_mktDataId, MktDataSubscription p_mktDataSubscription, int p_tickType, double p_price);
        public MktDataArrivedFunc MarketDataArrived;

        public Contract Contract { get; set; }
        public int MarketDataId { get; set; }
        public bool IsAnyPriceArrived { get; set; }
        public Timer CheckDataIsAliveTimer { get; set; }

        //public AutoResetEvent m_priceTickARE = new AutoResetEvent(false);

        //public int MktDataTickerID { get; set; }

        public ConcurrentDictionary<int, PriceAndTime> Prices { get; set; }
        // Do we get this value every second really?, even if no new Ask or Bid Price has changed? If that is true, it is a good validator that we have the current price data or not.
        // If we don't get TimeStamp every second, we can use the Time value in the PriceAndTime() to know when did we received the data.
        // This tick represents "timestamp of the last Last tick" value in seconds (counted from 00:00:00 1 Jan 1970 UTC).  Value: 1457126686, which is a UNIX timestamp epoch: https://www.epochconverter.com/  seconds since Jan 01 1970. (UTC)
        public string LastTimestampStr { get; set; } = "0";   // store it quickly in i String, it arrives in a string. Do not process it unnecessarily.  

        public MktDataSubscription()
        {
            Prices = new ConcurrentDictionary<int, PriceAndTime>();
            Prices.TryAdd(TickType.BID, new PriceAndTime());        // we are interested in the following Prices
            Prices.TryAdd(TickType.ASK, new PriceAndTime());
            Prices.TryAdd(TickType.LAST, new PriceAndTime());

            Prices.TryAdd(TickType.CLOSE, new PriceAndTime());      // previous day close
            Prices.TryAdd(TickType.OPEN, new PriceAndTime());
            Prices.TryAdd(TickType.HIGH, new PriceAndTime());
            Prices.TryAdd(TickType.LOW, new PriceAndTime());
        }
    }

    public class HistDataSubscription
    {
        public Contract Contract { get; set; }
        public AutoResetEvent AutoResetEvent { get; set; } = new AutoResetEvent(false);
        public List<QuoteData> QuoteData { get; set; } = new List<VirtualBroker.QuoteData>();
    }

    public class OrderSubscription
    {
        public Contract Contract { get; set; }
        public Order Order { get; set; }
        public AutoResetEvent AutoResetEvent { get; set; } = new AutoResetEvent(false);

        public DateTime DateTime { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public double Filled { get; set; }
        public double Remaining { get; set; }
        public double AvgFillPrice { get; set; }
        public int PermId { get; set; }
        public int ParentId { get; set; }
        public double LastFillPrice { get; set; }
        public int ClientId { get; set; }
        public string WhyHeld { get; set; }
    }


    public interface IBrokerWrapper : EWrapper
    {
        string IbAccountsList { get; set; }

        bool Connect(GatewayUser p_gatewayUser, int p_socketPort, int p_brokerConnectionClientID);
        void Disconnect();
        bool IsConnected();

        int ReqMktDataStream(Contract p_contract, bool p_snapshot = false, MktDataSubscription.MktDataArrivedFunc p_mktDataArrivedFunc = null);
        void CancelMktData(int p_marketDataId);
        bool GetMktDataSnapshot(Contract p_contract, ref Dictionary<int, PriceAndTime> p_quotes);
        bool ReqHistoricalData(DateTime p_endDateTime, int p_lookbackWindowSize, string p_whatToShow, Contract p_contract, out List<QuoteData> p_quotes);

        int PlaceOrder(Contract p_contract, TransactionType p_transactionType, double p_volume, OrderExecution p_orderExecution, OrderTimeInForce p_orderTif, double? p_limitPrice, double? p_stopPrice, double p_estimatedPrice, bool p_isSimulatedTrades);
        bool WaitOrder(int p_realOrderId, bool p_isSimulatedTrades);
        bool GetRealOrderExecutionInfo(int p_realOrderId, ref OrderStatus p_realOrderStatus, ref double p_realExecutedVolume, ref double p_realExecutedAvgPrice, ref DateTime p_execptionTime, bool p_isSimulatedTrades);
    }
}
