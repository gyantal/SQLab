using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;


namespace SQLab.Controllers
{
    
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
        static Dictionary<string, Tuple<DateTime, string>> g_dataCache = new Dictionary<string, Tuple<DateTime, string>>()
        {
        };

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
            //if (cacheFlagsStr != "ClearWebsiteCache")     // it is not necessary. If there is no "&cache=" part that means, we don't want to allow cache usage.
            //{
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
            //}

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
                string reply = await vbMessageTask;
                if (vbMessageTask.Exception != null || String.IsNullOrEmpty(reply))
                {
                    string errorMsg = $"Error.<BR> Check that both the IB's TWS and the VirtualBroker are running on Manual Trading Server! Start them manually if needed!";
                    Utils.Logger.Error(errorMsg);
                    return @"{ ""Message"": """ + errorMsg + @""" }";
                }
                Utils.Logger.Info($"GetAccountsInfo.GenerateGaiResponse(). Received '{reply}'");
                return reply;
            }
            catch (Exception e)
            {
                return @"{ ""Message"":  ""Exception caught by WebApi Get(): " + e.Message + @""" }";
            }
        }
     
    }
}
