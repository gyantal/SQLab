using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


// https://www.snifferquant.net/rtp?s=VXXB,SVXY,UWM,TWM,^RUT&f=l  // without JsonP, these tickers are streamed all the time
// https://www.snifferquant.net/rtp?s=VXXB,SVXY,UWM,TWM,^RUT,AAPL,GOOGL&f=l  // without JsonP, AAPL and GOOGL is not streamed
// https://www.snifferquant.net/rtp?s=VXXB,^VIX,^GSPC,SVXY&f=l  // without JsonP, this was the old test 1
// https://www.snifferquant.net/rtp?s=VXXB,^VIX,^GSPC,SVXY,^^^VIX201610,GOOG&f=l&jsonp=myCallbackFunction  // with JsonP, this was the old test 2
namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    [Route("~/rtp", Name = "rtp")]
    public class RealtimePrice : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public RealtimePrice(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

//#if !DEBUG
//        [Authorize]
//#endif
        public ActionResult Index()
        {
            // if the query is from the HealthMonitor.exe as a heartbeat, we allow it without Gmail Authorization
            string callerIP = WsUtils.GetRequestIP(this.HttpContext);
            Utils.Logger.Info($"RealtimePrice is called from IP {callerIP}");
            // Authorized ServerIP whitelist: 
            if (!String.Equals(callerIP, ServerIp.HealthMonitorPublicIp, StringComparison.CurrentCultureIgnoreCase) &&       //  HealthMonitor for checking that this service works
                !String.Equals(callerIP, ServerIp.HQaVM1PublicIp, StringComparison.CurrentCultureIgnoreCase))     // HQaVM1. e.g. website for real time price of "VIX futures" http://www.snifferquant.com/dac/VixTimer
            {
                var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config);
                if (authorizedEmailErrResponse != null)
                    return authorizedEmailErrResponse;
            }

            string content = GenerateRtpResponse(this.HttpContext.Request.QueryString.ToString()).Result;
            return Content(content, "application/json");

        }

        public static async Task<string> GenerateRtpResponse(string p_queryString)
        {
            try
            {
                var jsonDownload = string.Empty;
                //string queryString = @"?s=VXXB,SVXY,UWM,TWM,^RUT&f=l"; // without JsonP, these tickers are streamed all the time
                Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Sending to VBroker: '{p_queryString}'");
                Task<string> vbMessageTask = VirtualBrokerMessage.Send(p_queryString, VirtualBrokerMessageID.GetRealtimePrice);
                string reply = await vbMessageTask;
                if (vbMessageTask.Exception != null || String.IsNullOrEmpty(reply))
                {
                    string errorMsg = $"RealtimePrice.GenerateRtpResponse(). Received Null or Empty from VBroker. Check that the VirtualBroker is listering on IP: {VirtualBrokerMessage.AtsVirtualBrokerServerPublicIpForClients}:{VirtualBrokerMessage.DefaultVirtualBrokerServerPort}";
                    Utils.Logger.Error(errorMsg);
                    return @"{ ""Message"":  """ + errorMsg + @""" }";
                }
                Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Received '{reply}'");
                return reply;
            }
            catch (Exception e)
            {
                return @"{ ""Message"":  ""Exception caught by WebApi Get(): " + e.Message + @""" }";
            }
        }
     
    }
}
