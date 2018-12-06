using IBApi;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DbCommon;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    // implement fix prices. Good for debugging only for 1-2 stocks.
    public class BrokerWrapperTest : IBrokerWrapper
    {
        int m_socketPort;

        private string m_IbAccountsList;

        public string IbAccountsList
        {
            get {
                switch (m_socketPort)
                {
                    case 7301:
                        return "U407941";
                    case 7302:
                        return "U1034066";
                    default:
                        return null;
                }
            }
            set { m_IbAccountsList = value; }
        }

        public void accountDownloadEnd(string account)
        {
            throw new NotImplementedException();
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            throw new NotImplementedException();
        }

        public void accountSummaryEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            throw new NotImplementedException();
        }

        public void accountUpdateMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            throw new NotImplementedException();
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            throw new NotImplementedException();
        }

        public bool Connect(GatewayUser p_gatewayUser, int p_socketPort, int p_brokerConnectionClientID)
        {
            //Utils.Logger.Warn($"WARNING!!! This fake BrokerWrapper is only for DEV. Use the IB BrokerWrapper in production.");
            m_socketPort = p_socketPort;
            return true;
        }

        public bool IsConnected()
        {
            return true;
        }

        public void connectAck()
        {
            throw new NotImplementedException();
        }

        public void connectionClosed()
        {
            return;
        }

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            throw new NotImplementedException();
        }

        public void contractDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void currentTime(long time)
        {
            throw new NotImplementedException();
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
        }

        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }

        public void error(string str)
        {
            throw new NotImplementedException();
        }

        public void error(Exception e)
        {
            throw new NotImplementedException();
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            throw new NotImplementedException();
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            throw new NotImplementedException();
        }

        public void execDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void fundamentalData(int reqId, string data)
        {
            throw new NotImplementedException();
        }

        public virtual int ReqAccountSummary()
        {
            throw new NotImplementedException();
        }

        public virtual void CancelAccountSummary(int p_reqId)
        {
            throw new NotImplementedException();
        }

        public virtual void ReqPositions()
        {
            throw new NotImplementedException();
        }

        public bool GetAlreadyStreamedPrice(Contract p_contract, ref Dictionary<int, PriceAndTime> p_quotes)
        {
            switch (p_contract.Symbol)
            {
                case "VXX":
                    p_quotes = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() { Price = 20.58, Time = DateTime.UtcNow } } };   // MID is the most honest price. LAST may happened 1 hours ago
                    break;
                case "SVXY":
                    p_quotes = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() { Price = 43.83, Time = DateTime.UtcNow } } };    // MID is the most honest price. LAST may happened 1 hours ago
                    break;
                default:
                    break;
            }
            return true;
        }

        public void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            throw new NotImplementedException();
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            throw new NotImplementedException();
        }

        public void managedAccounts(string accountsList)
        {
            throw new NotImplementedException();
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            throw new NotImplementedException();
        }

        public void nextValidId(int orderId)
        {
            throw new NotImplementedException();
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }

        public void openOrderEnd()
        {
            throw new NotImplementedException();
        }

        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            throw new NotImplementedException();
        }

        public void position(string account, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionEnd()
        {
            throw new NotImplementedException();
        }

        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }

        public bool ReqHistoricalData(DateTime p_endDateTime, int p_lookbackWindowSize, string p_whatToShow, Contract p_contract, out List<QuoteData> p_quotes)
        {
            p_quotes = new List<QuoteData>() {
                new QuoteData { Date = DateTime.Parse("	2015-10-16	"), AdjClosePrice =     19.690001    },
                new QuoteData { Date = DateTime.Parse("	2015-10-19	"), AdjClosePrice =     18.32    },
                new QuoteData { Date = DateTime.Parse("	2015-10-20	"), AdjClosePrice =     18.950001    },
                new QuoteData { Date = DateTime.Parse("	2015-10-21	"), AdjClosePrice =     20.290001    },
                new QuoteData { Date = DateTime.Parse("	2015-10-22	"), AdjClosePrice =     18.440001    },
                new QuoteData { Date = DateTime.Parse("	2015-10-23	"), AdjClosePrice =     18.629999    },
                new QuoteData { Date = DateTime.Parse("	2015-10-26	"), AdjClosePrice =     19.290001    },
                new QuoteData { Date = DateTime.Parse("	2015-10-27	"), AdjClosePrice =     18.879999    },
                new QuoteData { Date = DateTime.Parse("	2015-10-28	"), AdjClosePrice =     18.34    },
                new QuoteData { Date = DateTime.Parse("	2015-10-29	"), AdjClosePrice =     18.58    },
                new QuoteData { Date = DateTime.Parse("	2015-10-30	"), AdjClosePrice =     18.83    },
                new QuoteData { Date = DateTime.Parse("	2015-11-02	"), AdjClosePrice =     17.879999    },
                new QuoteData { Date = DateTime.Parse("	2015-11-03	"), AdjClosePrice =     18.24    },
                new QuoteData { Date = DateTime.Parse("	2015-11-04	"), AdjClosePrice =     18.82    },
                new QuoteData { Date = DateTime.Parse("	2015-11-05	"), AdjClosePrice =     18.440001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-06	"), AdjClosePrice =     18.040001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-09	"), AdjClosePrice =     19.030001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-10	"), AdjClosePrice =     18.49    },
                new QuoteData { Date = DateTime.Parse("	2015-11-11	"), AdjClosePrice =     18.9     },
                new QuoteData { Date = DateTime.Parse("	2015-11-12	"), AdjClosePrice =     20.559999    },
                new QuoteData { Date = DateTime.Parse("	2015-11-13	"), AdjClosePrice =     21.959999    },
                new QuoteData { Date = DateTime.Parse("	2015-11-16	"), AdjClosePrice =     19.75    },
                new QuoteData { Date = DateTime.Parse("	2015-11-17	"), AdjClosePrice =     20.83    },
                new QuoteData { Date = DateTime.Parse("	2015-11-18	"), AdjClosePrice =     19.450001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-19	"), AdjClosePrice =     20.110001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-20	"), AdjClosePrice =     19.43    },
                new QuoteData { Date = DateTime.Parse("	2015-11-23	"), AdjClosePrice =     18.9     },
                new QuoteData { Date = DateTime.Parse("	2015-11-24	"), AdjClosePrice =     19.030001    },
                new QuoteData { Date = DateTime.Parse("	2015-11-25	"), AdjClosePrice =     18.74    },
                new QuoteData { Date = DateTime.Parse("	2015-11-27	"), AdjClosePrice =     18.91    },
                new QuoteData { Date = DateTime.Parse("	2015-11-30	"), AdjClosePrice =     18.799999    },
                new QuoteData { Date = DateTime.Parse("	2015-12-01	"), AdjClosePrice =     17.98    },
                new QuoteData { Date = DateTime.Parse("	2015-12-02	"), AdjClosePrice =     18.68    },
                new QuoteData { Date = DateTime.Parse("	2015-12-03	"), AdjClosePrice =     20.02    },
                new QuoteData { Date = DateTime.Parse("	2015-12-04	"), AdjClosePrice =     18.219999    },
                new QuoteData { Date = DateTime.Parse("	2015-12-07	"), AdjClosePrice =     18.65    },
                new QuoteData { Date = DateTime.Parse("	2015-12-08	"), AdjClosePrice =     19.23    },
                new QuoteData { Date = DateTime.Parse("	2015-12-09	"), AdjClosePrice =     19.99    },
                new QuoteData { Date = DateTime.Parse("	2015-12-10	"), AdjClosePrice =     20.280001    },
                new QuoteData { Date = DateTime.Parse("	2015-12-11	"), AdjClosePrice =     23.33    },
                new QuoteData { Date = DateTime.Parse("	2015-12-14	"), AdjClosePrice =     21.67    },
                new QuoteData { Date = DateTime.Parse("	2015-12-15	"), AdjClosePrice =     20.76    },
                new QuoteData { Date = DateTime.Parse("	2015-12-16	"), AdjClosePrice =     19.34    },
                new QuoteData { Date = DateTime.Parse("	2015-12-17	"), AdjClosePrice =     20.16    },
                new QuoteData { Date = DateTime.Parse("	2015-12-18	"), AdjClosePrice =     21.77    },
                new QuoteData { Date = DateTime.Parse("	2015-12-21	"), AdjClosePrice =     20.76    },
                new QuoteData { Date = DateTime.Parse("	2015-12-22	"), AdjClosePrice =     19.74    },
                new QuoteData { Date = DateTime.Parse("	2015-12-23	"), AdjClosePrice =     19.27    },
                new QuoteData { Date = DateTime.Parse("	2015-12-24	"), AdjClosePrice =     19.620001    },
                new QuoteData { Date = DateTime.Parse("	2015-12-28	"), AdjClosePrice =     19.389999    },
                new QuoteData { Date = DateTime.Parse("	2015-12-29	"), AdjClosePrice =     19.07    },
                new QuoteData { Date = DateTime.Parse("	2015-12-30	"), AdjClosePrice =     19.620001    },
                new QuoteData { Date = DateTime.Parse("	2015-12-31	"), AdjClosePrice =     20.1     },
                new QuoteData { Date = DateTime.Parse("	2016-01-04	"), AdjClosePrice =     21.34    },
                new QuoteData { Date = DateTime.Parse("	2016-01-05	"), AdjClosePrice =     20.67    },
                new QuoteData { Date = DateTime.Parse("	2016-01-06	"), AdjClosePrice =     21.290001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-07	"), AdjClosePrice =     23.610001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-08	"), AdjClosePrice =     24.83    },
                new QuoteData { Date = DateTime.Parse("	2016-01-11	"), AdjClosePrice =     24.02    },
                new QuoteData { Date = DateTime.Parse("	2016-01-12	"), AdjClosePrice =     22.91    },
                new QuoteData { Date = DateTime.Parse("	2016-01-13	"), AdjClosePrice =     25.200001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-14	"), AdjClosePrice =     24.290001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-15	"), AdjClosePrice =     26.700001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-19	"), AdjClosePrice =     26.709999    },
                new QuoteData { Date = DateTime.Parse("	2016-01-20	"), AdjClosePrice =     27.41    },
                new QuoteData { Date = DateTime.Parse("	2016-01-21	"), AdjClosePrice =     27.290001    },
                new QuoteData { Date = DateTime.Parse("	2016-01-22	"), AdjClosePrice =     25   },
                new QuoteData { Date = DateTime.Parse("	2016-01-25	"), AdjClosePrice =     26.24    },
                new QuoteData { Date = DateTime.Parse("	2016-01-26	"), AdjClosePrice =     24.99    },
                new QuoteData { Date = DateTime.Parse("	2016-01-27	"), AdjClosePrice =     26.02    },
                new QuoteData { Date = DateTime.Parse("	2016-01-28	"), AdjClosePrice =     25.15    },
                new QuoteData { Date = DateTime.Parse("	2016-01-29	"), AdjClosePrice =     24.120001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-01	"), AdjClosePrice =     23.84    },
                new QuoteData { Date = DateTime.Parse("	2016-02-02	"), AdjClosePrice =     25.4     },
                new QuoteData { Date = DateTime.Parse("	2016-02-03	"), AdjClosePrice =     25.120001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-04	"), AdjClosePrice =     25.360001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-05	"), AdjClosePrice =     26.360001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-08	"), AdjClosePrice =     27.620001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-09	"), AdjClosePrice =     27.879999    },
                new QuoteData { Date = DateTime.Parse("	2016-02-10	"), AdjClosePrice =     28.07    },
                new QuoteData { Date = DateTime.Parse("	2016-02-11	"), AdjClosePrice =     29.780001    },
                new QuoteData { Date = DateTime.Parse("	2016-02-12	"), AdjClosePrice =     28.559999    },
                new QuoteData { Date = DateTime.Parse("	2016-02-16	"), AdjClosePrice =     27.25    },
                new QuoteData { Date = DateTime.Parse("	2016-02-17	"), AdjClosePrice =     26.18    },
                new QuoteData { Date = DateTime.Parse("	2016-02-18	"), AdjClosePrice =     26.08    },
                new QuoteData { Date = DateTime.Parse("	2016-02-19	"), AdjClosePrice =     25.42    },
                new QuoteData { Date = DateTime.Parse("	2016-02-22	"), AdjClosePrice =     23.969999    },
                new QuoteData { Date = DateTime.Parse("	2016-02-23	"), AdjClosePrice =     25.18    },
                new QuoteData { Date = DateTime.Parse("	2016-02-24	"), AdjClosePrice =     25.02    },
                new QuoteData { Date = DateTime.Parse("	2016-02-25	"), AdjClosePrice =     24.16    },
                new QuoteData { Date = DateTime.Parse("	2016-02-26	"), AdjClosePrice =     24.5     },
                new QuoteData { Date = DateTime.Parse("	2016-02-29	"), AdjClosePrice =     24.879999    },
                new QuoteData { Date = DateTime.Parse("	2016-03-01	"), AdjClosePrice =     22.719999    },
                new QuoteData { Date = DateTime.Parse("	2016-03-02	"), AdjClosePrice =     22.32    },
                new QuoteData { Date = DateTime.Parse("	2016-03-03	"), AdjClosePrice =     21.59    },
                new QuoteData { Date = DateTime.Parse("	2016-03-04	"), AdjClosePrice =     21.860001    },
                new QuoteData { Date = DateTime.Parse("	2016-03-07	"), AdjClosePrice =     21.889999    },
                new QuoteData { Date = DateTime.Parse("	2016-03-08	"), AdjClosePrice =     22.73    },
                new QuoteData { Date = DateTime.Parse("	2016-03-09	"), AdjClosePrice =     22.43    },
                new QuoteData { Date = DateTime.Parse("	2016-03-10	"), AdjClosePrice =     22.02    },
                new QuoteData { Date = DateTime.Parse("	2016-03-11	"), AdjClosePrice =     20.9     },
                new QuoteData { Date = DateTime.Parse("	2016-03-14	"), AdjClosePrice =     20.58    }
            };

            return true;
        }

        public int ReqMktDataStream(Contract p_contract, string p_genericTickList = null, bool p_snapshot = false, MktDataSubscription.MktDataArrivedFunc p_mktDataArrivedFunc = null, MktDataSubscription.MktDataErrorFunc p_mktDataErrorFunc = null, MktDataSubscription.MktDataTickGenericFunc p_mktDataTickGenericFunc = null, MktDataSubscription.MktDataTypeFunc p_mktDataTypeFunc = null)
        {
            switch (p_contract.Symbol)
            {
                case "VXX":
                    return 4001;
                default:
                    return 4002;
            }
        }

        public virtual void CancelMktData(int p_marketDataId)
        {
            
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            throw new NotImplementedException();
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            throw new NotImplementedException();
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            throw new NotImplementedException();
        }

        public void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            throw new NotImplementedException();
        }

        public void tickSize(int tickerId, int field, int size)
        {
            throw new NotImplementedException();
        }

        public void tickSnapshotEnd(int tickerId)
        {
            throw new NotImplementedException();
        }

        public void tickString(int tickerId, int field, string value)
        {
            throw new NotImplementedException();
        }

        public void updateAccountTime(string timestamp)
        {
            throw new NotImplementedException();
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            throw new NotImplementedException();
        }

        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }

        public int PlaceOrder(Contract p_contract, TransactionType p_transactionType, double p_volume, OrderExecution p_orderExecution, OrderTimeInForce p_orderTif, double? p_limitPrice, double? p_stopPrice, double p_estimatedPrice, bool p_isSimulatedTrades)
        {
            throw new NotImplementedException();
        }

        public bool WaitOrder(int p_realOrderId, bool p_isSimulatedTrades)
        {
            throw new NotImplementedException();
        }

        public bool GetRealOrderExecutionInfo(int p_realOrderId, ref OrderStatus p_realOrderStatus, ref double p_realExecutedVolume, ref double p_realExecutedAvgPrice, ref DateTime p_execptionTime, bool p_isSimulatedTrades)
        {
            throw new NotImplementedException();
        }
    }
}
