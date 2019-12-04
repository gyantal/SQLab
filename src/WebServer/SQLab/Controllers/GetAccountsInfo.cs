using DbCommon;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using SQLab.Controllers.QuickTester.Strategies;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace SQLab.Controllers
{
    
    public class BrAccJsonHelper
    {
        public string BrAcc;
        public string Timestamp;
        public List<Dictionary<string,string>> AccSums;
        public List<Dictionary<string,string>> AccPoss;

    }
    public class GetAccountsInfo : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        //Note: The caching is done in the Webserver, not in the VBroker.The Webserver can decide to use the cached or approximated values. 
        //Webserver can query VBroker every 30 minutes (like Robert), or like once every day (preferred),
        //or like once 30 minutes after market open, and every hour until market close. Once around market close, and no query until next day. 
        //Or until next user-query.Yes.that can be done. But do it on the Webserver cache, not in the VBroker cache.Do no caching in VBroker for less complication.
        //So, the VBroker function can be lengthy, if user asked that Delta is necessary.
        //If we want faster user feedback, Webserver should cache, approximate it, and later update the user-website.
        static Dictionary<string, Tuple<DateTime, string>> g_dataCache = new Dictionary<string, Tuple<DateTime, string>>() {     };


        private static readonly SemaphoreSlim g_generateGaiResponseSemaphore = new SemaphoreSlim(1); 
        
        static Dictionary<string, DailyData> g_LastClosePrices = new Dictionary<string, DailyData>() {  };
        

        public GetAccountsInfo(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        //#if !DEBUG
        //        [Authorize]
        //#endif
        // https://www.snifferquant.net/gai?d=ATS&bAcc=Gyantal,Charmat&data=AccSum,Pos,EstPr,OptDelta&posExclSymbols=VIX,BLKCF,AXXDF    // Destination=AutoTradeServer
        // https://www.snifferquant.net/gai?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr,OptDelta&posExclSymbols=VIX,BLKCF,AXXDF    // Destination=ManualTradeServer
        [Route("~/gai", Name = "gai")]
        public ActionResult IndexJson()     // returns only data in JSON form
        {
            string callerIP = WsUtils.GetRequestIP(this.HttpContext);
            Utils.Logger.Info($"GetAccountsInfo is called from IP {callerIP}");
            // Authorized ServerIP whitelist: 
            if (!String.Equals(callerIP, ServerIp.HealthMonitorPublicIp, StringComparison.CurrentCultureIgnoreCase) &&       //  HealthMonitor for checking that this service works
                !String.Equals(callerIP, ServerIp.HQaVM1PublicIp, StringComparison.CurrentCultureIgnoreCase))     // HQaVM1. e.g. website if it needs this service
            {
                var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config);
                if (authorizedEmailErrResponse != null)
                    return authorizedEmailErrResponse;
            }

            // now g_dataCache is not used for JSON. Assuming JSON is for developers or programs (e.g. SQDesktop) and those should do their own caching
            string content = GenerateGaiResponse(this.HttpContext.Request.QueryString.ToString()).Result;
            return Content(content, "application/json");

        }

        // https://www.snifferquant.net/gaiV1?d=ATS&bAcc=Gyantal&data=AccSum,Pos,EstPr,OptDelta&posExclSymbols=VIX,BLKCF,AXXDF&cache=MaxStaleMin5      // Destination=AutoTradeServer
        // https://www.snifferquant.net/gaiV1?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr,OptDelta&posExclSymbols=VIX,BLKCF,AXXDF&cache=MaxStaleMin5   // Destination=ManualTradeServer
        [Route("~/gaiV1", Name = "gaiV1")]
        public ActionResult IndexHtml()    // returns UI in HTML form
        {
            var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config);
            if (authorizedEmailErrResponse != null)
                return authorizedEmailErrResponse;

            var queryStr = HttpContext.Request.QueryString.ToString();
            string queryStrWithoutCacheFlags = queryStr;
            string cacheFlagsStr = String.Empty;
            int iCacheStart = queryStr.IndexOf("&cache=");
            if (iCacheStart != -1)
            {  // remove "&cache=" part
                int iCacheEnd = queryStr.IndexOf("&", iCacheStart + "&cache=".Length);
                if (iCacheEnd == -1)
                    iCacheEnd = queryStr.Length;

                cacheFlagsStr = queryStr.Substring(iCacheStart + "&cache=".Length, iCacheEnd - (iCacheStart + "&cache=".Length));
                queryStrWithoutCacheFlags = queryStr.Substring(0, iCacheStart) + queryStr.Substring(iCacheEnd);
            }


            Tuple<DateTime, string> cacheData = null;
            if (g_dataCache.TryGetValue(queryStrWithoutCacheFlags, out cacheData))
                {
                    bool isCacheDataAllowed = false;
                    TimeSpan timespanFromLastQuery = (DateTime.UtcNow - cacheData.Item1);

                    if (cacheFlagsStr.StartsWith("MaxStale"))
                    {
                        string allowedTimeStr = cacheFlagsStr.Substring("MaxStale".Length);
                        if (allowedTimeStr.EndsWith("min"))
                        {
                            string allowedMinStr = allowedTimeStr.Substring(0, allowedTimeStr.Length - "min".Length);
                            if (Double.TryParse(allowedMinStr, out double allowedMin))
                            {
                                if (timespanFromLastQuery.TotalMinutes < allowedMin)
                                    isCacheDataAllowed = true;      // only allow if all cache parameters were correctly recognized
                            }
                        }
                    }

                    if (!isCacheDataAllowed)
                        cacheData = null;
                }

            if (cacheData == null)
            {
                // get JSON data and create item in the m_dataCache
                string content = GenerateGaiResponse(queryStrWithoutCacheFlags).Result;
                cacheData = new Tuple<DateTime, string>(DateTime.UtcNow, content);
                if (!cacheData.Item2.StartsWith("{ \"Message\": \"Error"))   // only store it in cache if it is not an error
                    g_dataCache[queryStrWithoutCacheFlags] = cacheData;
            }


            string wwwRootPath = Program.RunningEnvStr(RunningEnvStrType.DontPublishToPublicWwwroot);
            string fileStr = System.IO.File.ReadAllText(wwwRootPath + "GetAccountsInfoVer1.html");

            var result = fileStr.Replace("[{\"AccInfosToBeReplaced\":\"ByWebserver\"}]", cacheData.Item2);
            result = result.Replace("ForceReloadUrlToBeReplaced", HttpContext.Request.Path + queryStrWithoutCacheFlags);
            return Content(result, "text/html");
        }


        public static async Task<string> GenerateGaiResponse(string p_queryString)  // ?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr,OptDelta&posExclSymbols=VIX,BLKCF,AXXDF
        {
            //Monitor.Enter(g_generateGaiResponseSyncObj);       // it can be fine tuned later, but for a while, this is what lock() uses. Better than the Mutex. http://www.albahari.com/threading/part2.aspx
            // You can't await a task inside a lock scope (which is syntactic sugar for Monitor.Enter and Monitor.Exit). Using a Monitor directly will fool the compiler but not the framework.
            //async-await has no thread-affinity like a Monitor does. The code after the await will probably run in a different thread than the code before it. Which means that the thread that releases the Monitor isn't necessarily the one that acquired it.
            // Either don't use async-await in this case, or use a different synchronization construct like SemaphoreSlim or an AsyncLock you can build yourself.
            await g_generateGaiResponseSemaphore.WaitAsync();
            try
            {

                var queryDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(p_queryString);     // in .NET core, this is the standard for manipulating queries
                string destinationServ = queryDict["d"];
                queryDict.Remove("d");

                DateTime secTokenTimeBegin = new DateTime(2010, 1, 1);
                string securityTokenVer1 = ((long)(DateTime.UtcNow - secTokenTimeBegin).TotalSeconds).ToString();
                char[] charArray = securityTokenVer1.ToCharArray();     // reverse it, so it is not that obvious that it is the seconds
                Array.Reverse(charArray);
                string securityTokenVer2 = new string(charArray);

                List<KeyValuePair<string, string>> queryItems = new List<KeyValuePair<string, string>>  // Convert the StringValues into a list of KeyValue Pairs to make it easier to manipulate
                {
                    new KeyValuePair<string, string>("v", "1"),
                    new KeyValuePair<string, string>("secTok", securityTokenVer2)
                };
                queryItems.AddRange(queryDict.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)));
                var qb = new QueryBuilder(queryItems);  // Use the QueryBuilder to add in new items in a safe way (handles multiples and empty values)
                var queryStr = qb.ToQueryString();  // it contains the prefix '?'


                Utils.Logger.Info($"GetAccountsInfo.GenerateGaiResponse(). Sending to VBroker: '?{queryStr}'");

                string vbServerIp = String.Equals(destinationServ, "MTS", StringComparison.InvariantCultureIgnoreCase) ? VirtualBrokerMessage.MtsVirtualBrokerServerPublicIpForClients : VirtualBrokerMessage.AtsVirtualBrokerServerPublicIpForClients;
                Task<string> vbMessageTask = VirtualBrokerMessage.Send(queryStr.ToString(), VirtualBrokerMessageID.GetAccountsInfo, vbServerIp, VirtualBrokerMessage.DefaultVirtualBrokerServerPort);
                string vbReplyStr = (await vbMessageTask);
                if (vbMessageTask.Exception != null || String.IsNullOrEmpty(vbReplyStr))
                {
                    string errorMsg = $"Error.<BR> Check that both the IB's TWS and the VirtualBroker are running on Manual Trading Server! Start them manually if needed!";
                    Utils.Logger.Error(errorMsg);
                    return @"{ ""Message"": """ + errorMsg + @""" }";
                }
                Utils.Logger.Info($"GetAccountsInfo.GenerateGaiResponse(). Received '{vbReplyStr}'");

                vbReplyStr = vbReplyStr.Replace("\\\"", "\"");

                // 2019-03: After Market close: IB doesn't  give price for some 2-3 stocks (VXZ (only gives ask = -1, bid = -1), URE (only gives ask = 61, bid = -1), no open,low/high/last, not even previous Close price, nothing), these are the ideas to consider: We need some kind of estimation, even if it is not accurate.
                //     >One idea: ask IB's historical data for those missing prices. Then price query is in one place, but we have to wait more for VBroker, and IB throttle (max n. number of queries) may cause problem, so we get data slowly.
                //     >Betteridea: in Website, where the caching happens: ask our SQL database for those missing prices. We can ask our SQL parallel to the Vb query. No throttle is necessary. It can be very fast for the user. Prefer this now. More error proof this solution.
                //     >add lastClosePrice anyway. So, 2 new HTML columns: LastClose, $TodayProfit could be useful, because we don't have to login to TWS every day, and can be checked on smartphone too
                var vbReply = Utils.LoadFromJSON<List<BrAccJsonHelper>>(vbReplyStr);
                bool isNeedSqlDownload = false;
                List<string> symbolsNeedLastClosePrice = new List<string>() { };
                foreach (var brAccInfo in vbReply)
                {
                    foreach (var accPos in brAccInfo.AccPoss)
                    {
                        if (accPos["SecType"] == "STK")
                        {
                            // UNG ClosePrice was sometimes wrong in the morning. Not IB problem. Our proper price crawler arrives to UNG usually around 10:00 next day. 
                            // That can cause wrong UNG prices until 10:00 or until noon (especially that closePrices were cached for 12h, now I changed it to caching for 4h only).
                            // So, if proper prices are in SQL until 10:00, even with 4h caching, we will get proper prices at 14:00
                            if (!g_LastClosePrices.TryGetValue(accPos["Symbol"], out DailyData dailyData) || ((DateTime.UtcNow - dailyData.Date).TotalHours > 4.0))
                            {
                                isNeedSqlDownload = true;                                
                                symbolsNeedLastClosePrice.Add(accPos["Symbol"]);
                            }
                        }
                    }
                }

                Utils.Logger.Info($"isNeedSqlDownload: {isNeedSqlDownload}");
                if (isNeedSqlDownload)
                {
                    // we download data now and wait here, because in that case user always get a proper data (just at first time it is slower)
                    // 1. download data 
                    // 1 hour after close, Closeprice updated in SQL database. At the weekend, we want to see the last traded day (Friday) profit, (and surely 1 hour after close during weekdays).
                    // in London time: after midnight we still want to see the previous day profit. However, at 7am, it is OK that it is reset. So, the cutoff time is actually midnight at ET time zone, which is about 5am in London
                    DateTime nowET = Utils.ConvertTimeFromUtcToEt(DateTime.UtcNow);
                    DateTime maxAcceptedDateLoc;
                    if (nowET.DayOfWeek == DayOfWeek.Saturday)  // on Saturday, accept the previous Thursday as valid price
                        maxAcceptedDateLoc = nowET.Date.AddDays(-2);
                    else if (nowET.DayOfWeek == DayOfWeek.Sunday) // on Sunday, accept the previous Thursday as valid price
                        maxAcceptedDateLoc = nowET.Date.AddDays(-3);
                    else 
                        maxAcceptedDateLoc = nowET.Date.AddDays(-1);   // on normal days, maxAcceptedDate is the previous date
                    var sqlReturnTask = SqlTools.GetLastQuotesAsync(symbolsNeedLastClosePrice, maxAcceptedDateLoc, QuoteRequest.TDC);
                    var sqlReturnData = await sqlReturnTask;
                    var sqlReturn = sqlReturnData.Item1;

                    foreach (var ticker in symbolsNeedLastClosePrice)
                    {
                        Utils.Logger.Info($"symbolsNeedLastClosePrice processing: {ticker}");

                        var tickerRow = sqlReturn.FirstOrDefault(r => (string)r[1] == ticker);
                        if (tickerRow != null) // it happens if "BRK B" ticker is not found in the database.
                        {
                            // int stockID = Convert.ToInt32(tickerRow[0]);
                            // DateTime lastClosePriceDate = Convert.ToDateTime(tickerRow[2]);
                            double lastClosePrice = (double)Convert.ToDecimal(tickerRow[3]);
                            g_LastClosePrices[ticker] = new DailyData() { Date = DateTime.UtcNow, AdjClosePrice = lastClosePrice };
                        }
                    }

                    foreach (var item in g_LastClosePrices)
                    {
                        Utils.Logger.Info($"g_LastClosePrices: {item.Key}, Update time: {item.Value.Date}, {item.Value.AdjClosePrice}");
                    }

                }
                

                // 2. fill LastClosePrices and missing(0.00) RT EstPrice
                foreach (var brAccInfo in vbReply)
                {
                    foreach (var accPos in brAccInfo.AccPoss)
                    {
                        if ((accPos["SecType"] == "STK") && (g_LastClosePrices.TryGetValue(accPos["Symbol"], out DailyData dailyData)))
                        {
                            accPos["LastClose"] = dailyData.AdjClosePrice.ToString("0.00");
                            if (Double.TryParse(accPos["EstPrice"], out double estPrice) && estPrice == 0.0)  // so a price is missing
                            {
                                accPos["EstPrice"] = dailyData.AdjClosePrice.ToString("0.00");
                            }
                        }
                        else
                        {
                            accPos["LastClose"] = "NaN";    // for options LastClose, 'NaN' is better than 'undefined'
                        }
                    }
                }

                vbReplyStr = Utils.SaveToJSON<List<BrAccJsonHelper>>(vbReply);
                return vbReplyStr;
            }
            catch (Exception e)
            {
                string errorMsg = $"Error.<BR> Exception caught by Webserver GenerateGaiResponse()," + e.Message.Replace(Environment.NewLine, "<BR>");
                Utils.Logger.Error(e, "Exception caught by Webserver GenerateGaiResponse()");
                return @"{ ""Message"": """ + errorMsg + @""" }";
            }
            finally
            {
                g_generateGaiResponseSemaphore.Release();
            }
        }
     
    }
}
