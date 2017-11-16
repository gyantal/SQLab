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
    // implement prices by asking Yahoo Finance. Good for realtime, and for many stocks.
    public class BrokerWrapperYF : IBrokerWrapper
    {
        int m_socketPort;

        private string m_IbAccountsList;

        public string IbAccountsList
        {
            get {
                switch (m_socketPort)
                {
                    case 7301:
                        return "U407941";   // Gyantal
                    case 7302:
                        return "U1034066";  // Wife
                    case 7303:
                        return "U988767";  // Charmat
                    case 7304:
                        return "U1156489";  // Tu
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

        // 2017-11-03: YF discontinued the service. It only works with V7 API with crumbs (in YFForwarder of the website), however, we want a simpler solution here, which may be not accurate
        // copy the code from Overmind. GetTodayPctChange(), where CNBC.com query is implemented
        public bool GetMktDataSnapshot(Contract p_contract, ref Dictionary<int, PriceAndTime> p_quotes)
        {
            if (p_contract.SecType == "FUT")
                return false;       // there is no way YF can give Futures prices
            string ticker = (p_contract.SecType != "IND") ? p_contract.Symbol : "^" + p_contract.Symbol;

            // http://www.canbike.org/information-technology/yahoo-finance-url-download-to-a-csv-file.html
            // http://download.finance.yahoo.com/d/quotes.csv?s=AAPL&f=sl1d1t1c1ohgv&e=.csv     where s = symbol, l1 – Last Trade Price, b2	Ask (Real-time), b3	Bid (Real-time)
            // sometimes, when there is an 1 hour SummerTime setting difference, we have proper real-time price not 20min later, but 1h20min later.
            //"VXX",20.87,"3/15/2016","4:00pm",+0.29,21.19,21.27,20.83,541558
            // but using k1,b2,b3 is no better: http://download.finance.yahoo.com/d/quotes.csv?s=VXX&f=sl1d1t1k1b2b3c1ohgv&e=.csv
            // "VXX",20.87,"3/15/2016","4:00pm",N/A,N/A,N/A,+0.29,21.19,21.27,20.83,541558
            //string uri = $"http://download.finance.yahoo.com/d/quotes.csv?s={ticker}&f=sl1d1t1k1b2b3c1ohgv&e=.csv";
            string uri = $"http://download.finance.yahoo.com/d/quotes.csv?s={ticker}&f=sl1d1t1k1abc1ohgv&e=.csv";
            string csvDownload;
            if (!Utils.DownloadStringWithRetry(out csvDownload, uri, 5, TimeSpan.FromSeconds(5), false))
                return false;

            Utils.Logger.Warn("!YF RT: " + csvDownload.Trim());
            string[] cells = csvDownload.Split(',');

            double lastPrice = Double.NaN;
            Double.TryParse(cells[1], out lastPrice);
            double askPriceRT = Double.NaN;
            Double.TryParse(cells[5], out askPriceRT);
            double bidPriceRT = Double.NaN;
            Double.TryParse(cells[6], out bidPriceRT);

            foreach (var item in p_quotes)
            {
                if (item.Key == TickType.MID)
                {
                    item.Value.Time = DateTime.UtcNow;
                    item.Value.Price = (askPriceRT + bidPriceRT) / 2.0;
                }
                else if (item.Key == TickType.LAST)
                {
                    item.Value.Time = DateTime.UtcNow;
                    item.Value.Price = lastPrice;
                }
                else if (item.Key == TickType.ASK)
                {
                    item.Value.Time = DateTime.UtcNow;
                    item.Value.Price = askPriceRT;
                }
                else if (item.Key == TickType.BID)
                {
                    item.Value.Time = DateTime.UtcNow;
                    item.Value.Price = bidPriceRT;
                }
            }

            foreach (var item in p_quotes)
            {
                if (item.Value.Price < 0.0)
                {
                    Utils.Logger.Warn($"Warning. Something is wrong. Price is negative. Returning False for price.");   // however, VBroker may want to continue, so don't throw Exception or do StrongAssert()
                    return false;
                }
                //// for daily High, Daily Low, Previous Close, etc. don't check this staleness
                //bool doCheckDataStaleness = item.Key != TickType.LOW && item.Key != TickType.HIGH && item.Key != TickType.CLOSE;
                //if (doCheckDataStaleness && (DateTime.UtcNow - item.Value.Time).TotalMinutes > 5.0)
                //{
                //    Utils.Logger.Warn($"Warning. Something may be wrong. We have the RT price of {item.Key} for '{p_contract.Symbol}' , but it is older than 5 minutes. Maybe Gateway was disconnected. Returning False for price.");
                //    return false;
                //}
            }

            //p_quotes = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() { Price = Double.Parse(cells[1]), Time = DateTime.UtcNow } } };
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
            p_quotes = null;
            string ticker = (p_contract.SecType != "IND") ? p_contract.Symbol : "^" + p_contract.Symbol;

            DateTime startDateTime = p_endDateTime.AddDays(-1.0 * p_lookbackWindowSize / 5.0 * 7.0 * 1.15);  // convert trading days to calendar days, and add extra 10%
            //string uri = "http://ichart.finance.yahoo.com/table.csv?s=VXX&d=1&e=21&f=2014&g=d&a=0&b=30&c=2009&ignore=.csv";
            string uri = $"http://ichart.finance.yahoo.com/table.csv?s={ticker}&d={p_endDateTime.Month - 1}&e={p_endDateTime.Day}&f={p_endDateTime.Year}&g=d&a={startDateTime.Month - 1}&b={startDateTime.Day}&c={startDateTime.Year}&ignore=.csv";
            string csvDownload;
            if (!Utils.DownloadStringWithRetry(out csvDownload, uri, 5, TimeSpan.FromSeconds(5), false))
                return false;

            p_quotes = VBrokerUtils.ParseCSVToQuotes(csvDownload, true).ToList();

            // during regular trading hours, IB gateway adds today lastPrice as a ClosePrice of this historical. Attach to it.
            if (p_quotes[p_quotes.Count - 1].Date.Date != DateTime.UtcNow.Date)
            {
                // during regular trading hours OR after the market closed, but If there is a trading day today. (if today is Sunday, then what?)
                //DateTime utcNow = DateTime.UtcNow;
                //bool isMarketTradingDay;
                //DateTime openTimeUtc, closeTimeUtc;
                //bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(utcNow, out isMarketTradingDay, out openTimeUtc, out closeTimeUtc, p_maxAllowedStaleness);
                //if (!isTradingHoursOK)
                //{
                //    Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
                //    return false;
                //}
                //else
                //{
                //    if (!isMarketTradingDay)
                //        return false;
                //    if (utcNow < openTimeUtc)
                //        return false;
                //    if (utcNow > closeTimeUtc)
                //        return false;

                //    return true;
                //}

                //if (Utils.IsInRegularTradingHoursNow(TimeSpan.FromDays(3))) // in regular trading hours, IB gateway adds today lastPrice as a ClosePrice of this historical. Attach to it.
                //{
                    var rtPrices = new Dictionary<int, PriceAndTime>() { { TickType.MID, new PriceAndTime() } };    // MID is the most honest price. LAST may happened 1 hours ago
                if (!GetMktDataSnapshot(p_contract, ref rtPrices))
                        return false;
                    p_quotes.Add(new QuoteData() { Date = rtPrices[TickType.MID].Time, AdjClosePrice = rtPrices[TickType.MID].Price });
                //}
            }

            p_quotes.RemoveRange(0, p_quotes.Count - p_lookbackWindowSize);
            return true;
        }

        public int ReqMktDataStream(Contract p_contract, bool p_snapshot = false, MktDataSubscription.MktDataArrivedFunc p_mktDataArrivedFunc = null)
        {
            switch (p_contract.Symbol)
            {
                case "VXX":
                    return 4001;
                case "SVXY":
                    return 4002;
                case "RUT":
                    return 4003;
                case "UWM":
                    return 4004;
                case "TWM":
                    return 4005;
                default:
                    return 3999;
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
            return new Random().Next(10000);    // time dependent seed. Good.
        }

        public bool WaitOrder(int p_realOrderId, bool p_isSimulatedTrades)
        {
            return true;
        }

        public bool GetRealOrderExecutionInfo(int p_realOrderId, ref OrderStatus p_realOrderStatus, ref double p_realExecutedVolume, ref double p_realExecutedAvgPrice, ref DateTime p_execptionTime, bool p_isSimulatedTrades)
        {
            //if (p_isSimulatedTrades)    // there was no orderStatus(), so just fake one
            //{
                p_realOrderStatus = OrderStatus.Filled;
                p_realExecutedVolume = 0;    // maybe less is filled than it was required...
                p_realExecutedAvgPrice = 1.0;   // assume we bought it for $1.0 each // we can do RealTime price or YahooEstimated price, or lastDay Closeprice later if it is required
                p_execptionTime = DateTime.UtcNow;
                return true;
            //}
        }
    }
}
