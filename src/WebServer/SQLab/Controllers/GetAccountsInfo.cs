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
        // https://www.snifferquant.net/gai?d=ATS&bAcc=Gyantal,Charmat&data=AccSum,Pos,EstPr&flags=CacheMaxStaleMin5    // Destination=AutoTradeServer
        // https://www.snifferquant.net/gai?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr&flags=CacheMaxStaleMin5    // Destination=ManualTradeServer
        [Route("~/gai", Name = "gai")]
        public ActionResult Index()     // returns only data in JSON form
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

            string content = GenerateGaiResponse(this.HttpContext.Request.QueryString.ToString()).Result;
            return Content(content, "application/json");

        }

        // https://www.snifferquant.net/gaiV1?d=ATS&bAcc=Gyantal&data=AccSum,Pos,EstPr&flags=CacheMaxStaleMin5      // Destination=AutoTradeServer
        // https://www.snifferquant.net/gaiV1?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr&flags=CacheMaxStaleMin5   // Destination=ManualTradeServer
        [Route("~/gaiV1", Name = "gaiV1")]
        public ActionResult Index2()    // returns UI in HTML form
        {
            var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config);
            if (authorizedEmailErrResponse != null)
                return authorizedEmailErrResponse;

            var queryStr = HttpContext.Request.QueryString.ToString();
            int iFlags = queryStr.IndexOf("&flags=");
            if (iFlags != -1)    // remove everything over "&flags="
                queryStr = queryStr.Substring(0, iFlags);

            // TODO: process flags=CacheMaxStaleMin5 and act accordingly, not the fix 1 hour stale
            if (g_dataCache.TryGetValue(queryStr, out Tuple<DateTime, string> data))
            {
                if ((DateTime.UtcNow - data.Item1).TotalHours > 1.0) // if data is too stale, we have to query it again.
                    data = null;
            }

            if (data == null)
            {
                // get JSON data and create item in the m_dataCache
                string content = GenerateGaiResponse(queryStr + "&flags=None").Result;
                data = new Tuple<DateTime, string>(DateTime.UtcNow, content);
                g_dataCache[queryStr] = data;
            }


            string wwwRootPath = Program.RunningEnvStr(RunningEnvStrType.DontPublishToPublicWwwroot);
            string fileStr = System.IO.File.ReadAllText(wwwRootPath + "GetAccountsInfoVer1.html");

            var result = fileStr.Replace("[{\"ToBeReplaced\":\"ByWebserver\"}]", data.Item2);
            return Content(result, "text/html");
        }

        public static async Task<string> GenerateGaiResponse(string p_queryString)  // ?d=MTS&bAcc=Charmat,DeBlanzac&data=AccSum,Pos,EstPr&flags=CacheMaxStaleMin5
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
                    string errorMsg = $"RealtimePrice.GenerateRtpResponse(). Received Null or Empty from VBroker. Check that the VirtualBroker is listering on IP: {vbServerIp}:{VirtualBrokerMessage.DefaultVirtualBrokerServerPort}";
                    Utils.Logger.Error(errorMsg);
                    return @"{ ""Message"":  """ + errorMsg + @""" }";
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
