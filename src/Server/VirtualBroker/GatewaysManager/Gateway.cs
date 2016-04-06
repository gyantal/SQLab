﻿using DbCommon;
using SqCommon;
using IBApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    public enum GatewayUser { None, Demo, GyantalMain, GyantalSecondary, GyantalPaper, CharmatMain, CharmatSecondary, CharmatPaper, CharmatWifeMain, CharmatWifeSecondary, CharmatWifePaper }

    public class VirtualOrder
    {
        public int OrderId { get; set; }
        public DateTime SubmitTime { get; set; }
        public double EstimatedUsdSize { get; set; }
        public List<int> RealOrderIds { get; set; }     // sometimes, one virtual order is executed with many real orders. Like gradually decreasing the Limit price because we were only half-filled
        public OrderExecution OrderExecution { get; set; }      // MKT, MOC
        public OrderStatus OrderStatus { get; set; }
    }

    public class RealOrder
    {
        public int OrderId { get; set; }
        public List<int> VirtualOrderIds { get; set; }  // sometimes one real order has many virtual orders. If one order is Short 10 VXX, other order is long 7 VXX, than the one real order will be short 3 VXX
    }

    public class Gateway
    {
        public GatewayUser GatewayUser { get;  }
        public string VbAccountsList { get; set; }
        public int SocketPort { get; set; }
        public int BrokerConnectionClientID { get; set; }
        public bool IsConnected { get; set; }

        public IBrokerWrapper BrokerWrapper { get; set; }

        int m_virtualOrderIDseed = 1000;
        protected int GetUniqueVirtualOrderID
        {
            get { return Interlocked.Increment(ref m_virtualOrderIDseed); }  // Increment gives back the incremented value, not the old value
        }
        public List<VirtualOrder> VirtualOrders { get; set; } = new List<VirtualOrder>();
        public List<RealOrder> RealOrders { get; set; } = new List<RealOrder>();


        double m_allowedMinUsdTransactionValue = 200.0;
        public double AllowedMinUsdTransactionValue
        {
            get { return m_allowedMinUsdTransactionValue; }
            set { m_allowedMinUsdTransactionValue = value; }
        }

        double m_allowedMaxUsdTransactionValue = 10000.0;   // $10K (because sometimes we play double leverage
        public double AllowedMaxUsdTransactionValue
        {
            get { return m_allowedMaxUsdTransactionValue; }
            set { m_allowedMaxUsdTransactionValue = value; }
        }

        double m_maxEstimatedUsdSumRecentlyAllowed = 50000.0;
        public double MaxEstimatedUsdSumRecentlyAllowed
        {
            get { return m_maxEstimatedUsdSumRecentlyAllowed; }
            set { m_maxEstimatedUsdSumRecentlyAllowed = value; }
        }

        int m_maxNOrdersRecentlyAllowed = 20;
        public int MaxNOrdersRecentlyAllowed
        {
            get { return m_maxNOrdersRecentlyAllowed; }
            set { m_maxNOrdersRecentlyAllowed = value; }
        }

        public Gateway(GatewayUser p_gatewayUser)   // gateWayUser will be fixed. We don't allow to change it later.
        {
            IsConnected = false;
            GatewayUser = p_gatewayUser;
            if (GatewayUser == GatewayUser.CharmatMain || GatewayUser == GatewayUser.CharmatSecondary || GatewayUser == GatewayUser.CharmatPaper)
            {
                m_allowedMaxUsdTransactionValue = 50000.0;          // $50K
                m_maxEstimatedUsdSumRecentlyAllowed = 1000000.0;    // $1M
            }
        }

        // PlayTransactionsViaBroker(). Consider these use-cases:
        // ******* 1. Problem: if VBroker1 is LONG IWM, VBroker2 is Short IWM at MOC, it cancels out each other(with Minfilter), there is no real life trade, which is OK.
        // However, there were 2 virtual transactions, that Gateway should give back, because VBroker1 and VBroker2 wants to write something to the Portfolio in the DB.
        //+Solution:
        //    - PlayTransactionsViaBroker() always gives back the prices (except in simulation mode); (even if minFilter was applied), Because we need it for the DB. If minfilter was applied, it gives back Volume = 0; showing it was not executed
        //    - PlayTransactionsViaBroker() signals whether an portfolioItem was executed or not (small are not executed) (in a flag or somewhere); it signals by changing the Volume to 0; (minFilter or maxFilter was applied)
        //    - even if an item is not executed, the BrokerTask can write them into the Portfolios (it is his own decision)
        //        + IF	there is at last 1 sub-strategy, in which this is significant amount enough (non-negligible) (we need per strategy minFilter values too; call it significancy; so don't confuse with TradeMin)
        //        + IF it is written into at least 1 sub-strategy portfolio, then All other sub-strategy portfolios are updated with the 'minor/manor' trades
        //        + invariant: keep real/simulated trade values equal; means: if +1 was traded, the aggregate written into DB should be 1 as well
        //        for example; there is 51 substrategy. One shorts 101 IWM, other 50 buys 2 IWM each. The aggregateIntent is short 1. The aggregateReal is 0 (minFilter skip).
        //        We have to update the virtual portfolios: but the aggregateSimulated should be 0 too. (that is the invariant)


        // ******* 2. LimitOrder execution policy:
        //- Determine targetPrice = MidPrice = (Ask+Bid)/2 
        //- convert it to 2 decimals (the smallest unit is 1 penny in the Exchange)
        //- send the order to the Broker with this targetPrice;
        //> Repeat from here
        //    - wait 10 seconds
        //    - cancel the order
        //    - wait some seconds until the Cancelation is confirmed
        //        - sometimes the cancellation fails. In that case, probably the original order were executed; handle it as completed;
        //        after calling cancelOrder(...) you must wait for it to be acknowledged i.e. orderStatus(...) returns with status=="Cancelled" or perhaps status=="Filled" and go from there. 

        //    if Repeat = 5, don't chase it further
        //        break;

        //    Method 1: (implemented)
        //    If the TargetPrice is < 100
        //        - add an extra penny to the target price (if it is a Buy order or Cover order)
        //        - OR subtract an extra penny to the target price (if it is a Sell or Short order)

        //    If the targetPrice is in [100.. 200]
        //        - add an extra 0.02 to the target price (if it is a Buy order or Cover order)
        //        - OR subtract an extra 0.02 to the target price (if it is a Sell or Short order)

        //    If the targetPrice is in [200.. ]
        //        - add an extra 0.05 to the target price (if it is a Buy order or Cover order)
        //        - OR subtract an extra 0.05 to the target price (if it is a Sell or Short order)

        //    Method 2: (Laszlo's idea: not yet implemented)
        //    - query the CurrentMidPrice (askBidprice) again (not based on previus) and set the targetPrice = BidPrice 
        //    this one is good in case of the Exploding stocks; that moves up 1% every 10 seconds (adding 1 penny cannot catch them)

        //    - insert the new order with the new targetPrice
        //> Repeat unti here


        // 3. ***** MOC orders: before Placing them, check current Open MOC orders
        // We can subscribe to all the current OrdersInfo. For MOC orders for example, before PlaceOrder() we should check that if there is already an MOC order then we Modify that (or Cancel&Recreate)

        // 4. *****  Glitch protections:
        //+ check the MaxTradeSize of the bxml file. : MaxTradeUsdValue
        //+ check the number of Trades in the last 10 minutes < 20; After that, stop everything.
        //+ check the $volume of those trades based on last day price (SQ DB SQL usage).; it should be < 1$ Mil in the last 10 minutes

        internal int PlaceOrder(Contract p_contract, TransactionType p_transactionType, double p_volume, OrderExecution p_orderExecution, OrderTimeInForce p_orderTif, double? p_limitPrice, double? p_stopPrice, double p_estimatedPrice, bool p_isSimulatedTrades)
        {
            // 1. Glitch protections
            int virtualOrderID = GetUniqueVirtualOrderID;
            if (double.IsNaN(p_estimatedPrice) || Math.Abs(p_estimatedPrice) < 0.000)
            {   // we want HealthMonitor to be notified with StrongAssert, but continue with other orders
                Utils.Logger.Error("PlaceOrder(): 'double.IsNaN(p_estimatedPrice) || Math.Abs(p_estimatedPrice) < 0.001' failed. Not Safe to do the trade without estimating its value. Crash here.");
                return virtualOrderID;      // try to continue with other PlaceOrders()
            }
            double estimatedTransactionValue = p_volume * p_estimatedPrice;
            if (estimatedTransactionValue > AllowedMaxUsdTransactionValue)  // if maxFilter is breached, safer to not trade at all. Even if it is an MOC order and can be grouped with other orders. Safer this way.
            {
                Utils.Logger.Warn($"Warning. MaxFilter is breached. Transaction is MaxFilter (${AllowedMaxUsdTransactionValue:F0}) skipped: {p_contract.Symbol} {p_volume:F0}");
                return virtualOrderID;
            }

            DateTime utcNow = DateTime.UtcNow;
            int nLatestOrders = 0;
            double estimatedUsdSizeSumRecently = 0.0;
            lock (VirtualOrders)   // safety checks
            {
                foreach (var latestOrder in VirtualOrders.Where(r => (utcNow - r.SubmitTime).TotalMinutes < 10.0))
                {
                    nLatestOrders++;
                    estimatedUsdSizeSumRecently += latestOrder.EstimatedUsdSize;
                }
            }
            if (nLatestOrders >= m_maxNOrdersRecentlyAllowed)
            {
                Utils.Logger.Error($"nLatestOrders >= m_maxNOrdersRecentlyAllowed. This is for your protection. Transaction {p_contract.Symbol} is skipped. Set Settings to allow more.");
                return virtualOrderID;      // try to continue with other PlaceOrders()
            }
            if (estimatedUsdSizeSumRecently >= m_maxEstimatedUsdSumRecentlyAllowed)
            {
                Utils.Logger.Error($"estimatedUsdSizeSum >= m_maxEstimatedUsdSumRecentlyAllowed. This is for your protection. Transaction {p_contract.Symbol} is skipped. Set Settings to allow more.");
                return virtualOrderID;      // try to continue with other PlaceOrders()
            }

            // 2. Execute different orderTypes MKT, MOC
            // vbOrderId and ibOrderID is different, because some Limit orders, or other tricky orders are executed in real life by many concrete IB orders
            // or there are two opposite vbOrder that cancels each other out at MOC, therefore there is no ibOrder at all
            Console.WriteLine($"Place {(p_isSimulatedTrades ? "Simulated" : "Real")} Order {p_transactionType}  {p_volume} {p_contract.Symbol} (${estimatedTransactionValue:F0})");
            Utils.Logger.Info($"Place {(p_isSimulatedTrades?"Simulated":"Real")} Order {p_transactionType} {p_volume} {p_contract.Symbol} (${estimatedTransactionValue:F0})");
            if (p_orderExecution == OrderExecution.Market)
            {
                // Perform minFilter skipped here. But use estimatedTransactionValue. We get the estimatedPrice from the Main Gateway
                if (estimatedTransactionValue < AllowedMinUsdTransactionValue)
                {
                    Utils.Logger.Warn($"Transaction is MinFilter (${AllowedMinUsdTransactionValue:F0}) skipped: {p_contract.Symbol} {p_volume:F0}");
                    lock (VirtualOrders)
                        VirtualOrders.Add(new VirtualOrder() { OrderId = virtualOrderID, SubmitTime = DateTime.UtcNow, EstimatedUsdSize = 0.0, OrderExecution = p_orderExecution, OrderStatus = OrderStatus.MinFilterSkipped });
                    return virtualOrderID;
                }

                int ibRealOrderID = BrokerWrapper.PlaceOrder(p_contract, p_transactionType, p_volume, p_orderExecution, p_orderTif, p_limitPrice, p_stopPrice, p_estimatedPrice, p_isSimulatedTrades);
                lock (VirtualOrders)
                    VirtualOrders.Add(new VirtualOrder() { OrderId = virtualOrderID, SubmitTime = DateTime.UtcNow, EstimatedUsdSize = estimatedTransactionValue, RealOrderIds = new List<int>() { ibRealOrderID }, OrderExecution = p_orderExecution, OrderStatus = OrderStatus.Submitted, });
                lock (RealOrders)
                    RealOrders.Add(new RealOrder() { OrderId = ibRealOrderID, VirtualOrderIds = new List<int>() { virtualOrderID } });
                
            } else if (p_orderExecution == OrderExecution.MarketOnClose)
            {
                // MOC orders need more consideration. Checking current MOC orders of the gateway, modifying it, etc. and 1 realOrders can be many VirtualOrders
                // (MinFilter orders may be played here if there is already a bigger order under Excetion. However, this 'nice' feature is not necessary.)

                // 1. Get a list of current orders from BrokerGateway
                // 2. If this ticker that I want to trade is in the list, don't create a new order, but modify that one.
                // 3. Otherwise, create a new Order

                // !!! Now, we only play UWM, TWM at MOC orders, so in real life, there will be no order Clash. Later, implement it properly. Now, assume that there is no other MOC order for this stock.
                // in the future implement MOC order in a general way. One RealOrder can mean many Virtual MOC orders.

                // Perform minFilter skipped here. But use estimatedTransactionValue. We get the estimatedPrice from the Main Gateway
                if (estimatedTransactionValue < AllowedMinUsdTransactionValue)
                {
                    Utils.Logger.Warn($"Transaction is MinFilter (${AllowedMinUsdTransactionValue:F0}) skipped: {p_contract.Symbol} {p_volume:F0}");
                    lock (VirtualOrders)
                        VirtualOrders.Add(new VirtualOrder() { OrderId = virtualOrderID, SubmitTime = DateTime.UtcNow, EstimatedUsdSize = 0.0, OrderExecution = p_orderExecution, OrderStatus = OrderStatus.MinFilterSkipped });
                    return virtualOrderID;
                }

                int ibRealOrderID = BrokerWrapper.PlaceOrder(p_contract, p_transactionType, p_volume, p_orderExecution, p_orderTif, p_limitPrice, p_stopPrice, p_estimatedPrice, p_isSimulatedTrades);
                lock (VirtualOrders)
                    VirtualOrders.Add(new VirtualOrder() { OrderId = virtualOrderID, SubmitTime = DateTime.UtcNow, EstimatedUsdSize = estimatedTransactionValue, RealOrderIds = new List<int>() { ibRealOrderID }, OrderExecution = p_orderExecution, OrderStatus = OrderStatus.Submitted, });
                lock (RealOrders)
                    RealOrders.Add(new RealOrder() { OrderId = ibRealOrderID, VirtualOrderIds = new List<int>() { virtualOrderID } });

            }
            else
            {
                Utils.Logger.Error("Only the Simple Market order is implemented. MOC orders need more consideration. Checking current MOC orders of the gateway, modifying it, etc.");
            }

            return virtualOrderID;
        }

        internal bool WaitOrder(int p_virtualOrderId, bool p_isSimulatedTrades)
        {
            VirtualOrder virtualOrder = null;
            lock (VirtualOrders)
            {
                virtualOrder = VirtualOrders.FirstOrDefault(r => r.OrderId == p_virtualOrderId);
            }
            if (virtualOrder == null)
            {
                Utils.Logger.Error($"Virtual orderId {p_virtualOrderId} is not found.");
                return false;
            }

            if (virtualOrder.OrderStatus == OrderStatus.MinFilterSkipped || virtualOrder.OrderStatus == OrderStatus.MaxFilterSkipped)
                return true;    // no error. Executed succesfully. Market or MOC orders

            if (virtualOrder.OrderExecution == OrderExecution.Market)       // MinFilter and MaxFilter is only properly handled with MKT order not with MOC order
            {
                int realOrderId = virtualOrder.RealOrderIds[0];
                BrokerWrapper.WaitOrder(realOrderId, p_isSimulatedTrades);
            }
            else if (virtualOrder.OrderExecution == OrderExecution.MarketOnClose)
            {
                // in the future implement MOC order in a general way. One RealOrder can mean many Virtual MOC orders.
                int realOrderId = virtualOrder.RealOrderIds[0];
                BrokerWrapper.WaitOrder(realOrderId, p_isSimulatedTrades);
            }

            return false;
        }

        internal bool GetVirtualOrderExecutionInfo(int p_virtualOrderId, ref OrderStatus orderStatus, ref double executedVolume, ref double executedAvgPrice, ref DateTime p_executionTime, bool p_isSimulatedTrades)
        {
            VirtualOrder virtualOrder = null;
            lock (VirtualOrders)
            {
                virtualOrder = VirtualOrders.FirstOrDefault(r => r.OrderId == p_virtualOrderId);
            }
            if (virtualOrder == null)
            {
                Utils.Logger.Error($"Virtual orderId {p_virtualOrderId} is not found.");
                return false;
            }

            if (virtualOrder.OrderExecution == OrderExecution.Market)
            {
                if (virtualOrder.OrderStatus == OrderStatus.MinFilterSkipped || virtualOrder.OrderStatus == OrderStatus.MaxFilterSkipped)
                {
                    orderStatus = virtualOrder.OrderStatus;
                    executedVolume = 0.0;
                    executedAvgPrice = Double.NaN;
                    p_executionTime = DateTime.UtcNow;
                    return true;    // correct, expected MinFilter Skip
                }

                int realOrderId = virtualOrder.RealOrderIds[0];

                // the real order may or maybe Not finished yet. If it is not filled, we will set it as Error order
                OrderStatus realOrderStatus = OrderStatus.None;
                double realExecutedVolume = Double.NaN;
                double realAvgPrice = Double.NaN;
                DateTime realExecutionTime = DateTime.MinValue;
                if (!BrokerWrapper.GetRealOrderExecutionInfo(realOrderId, ref realOrderStatus, ref realExecutedVolume, ref realAvgPrice, ref realExecutionTime, p_isSimulatedTrades))
                    return false;

                if (realOrderStatus == OrderStatus.Filled)
                {
                    orderStatus = realOrderStatus;
                    executedVolume = realExecutedVolume;
                    executedAvgPrice = realAvgPrice;
                }
                else
                {
                    orderStatus = realOrderStatus;  // return the real order status. it doesn't hurt
                    executedVolume = Double.NaN;    // better not using partially filled info, or 0.0
                    executedAvgPrice = Double.NaN;
                }

                return true;
            }
            else if (virtualOrder.OrderExecution == OrderExecution.MarketOnClose)
            {
                // in the future implement MOC order in a general way. One RealOrder can mean many Virtual MOC orders.

                if (virtualOrder.OrderStatus == OrderStatus.MinFilterSkipped || virtualOrder.OrderStatus == OrderStatus.MaxFilterSkipped)
                {
                    orderStatus = virtualOrder.OrderStatus;
                    executedVolume = 0.0;
                    executedAvgPrice = Double.NaN;
                    p_executionTime = DateTime.UtcNow;
                    return true;    // correct, expected MinFilter Skip
                }

                int realOrderId = virtualOrder.RealOrderIds[0];

                // the real order may or maybe Not finished yet. If it is not filled, we will set it as Error order
                OrderStatus realOrderStatus = OrderStatus.None;
                double realExecutedVolume = Double.NaN;
                double realAvgPrice = Double.NaN;
                DateTime realExecutionTime = DateTime.MinValue;
                if (!BrokerWrapper.GetRealOrderExecutionInfo(realOrderId, ref realOrderStatus, ref realExecutedVolume, ref realAvgPrice, ref realExecutionTime, p_isSimulatedTrades))
                    return false;

                if (realOrderStatus == OrderStatus.Filled)
                {
                    orderStatus = realOrderStatus;
                    executedVolume = realExecutedVolume;
                    executedAvgPrice = realAvgPrice;
                }
                else
                {
                    orderStatus = realOrderStatus;  // return the real order status. it doesn't hurt
                    executedVolume = Double.NaN;    // better not using partially filled info, or 0.0
                    executedAvgPrice = Double.NaN;
                }

                return true;

            }
            return false;
        }
    }

}
