using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Text;
using SqCommon;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

// https://www.snifferquant.net/rtp?s=VXX,SVXY,UWM,TWM,^RUT&f=l  // without JsonP, these tickers are streamed all the time
// https://www.snifferquant.net/rtp?s=VXX,SVXY,UWM,TWM,^RUT,AAPL,GOOGL&f=l  // without JsonP, AAPL and GOOGL is not streamed
// https://www.snifferquant.net/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV&f=l  // without JsonP, this was the old test 1
// https://www.snifferquant.net/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV,^^^VIX201610,GOOG&f=l&jsonp=myCallbackFunction  // with JsonP, this was the old test 2
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
            string callerIP = ControllerCommon.GetRequestIP(this);
            Utils.Logger.Info($"RealtimePrice is called from IP {callerIP}");
            if (!String.Equals(callerIP, "23.20.243.199", StringComparison.CurrentCultureIgnoreCase))       // If not from (public IP for the VBrokerDEV server (HealthMonitor heartbeat check), check GoogleAuth
            {
                var authorizedEmailResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); if (authorizedEmailResponse != null) return authorizedEmailResponse;
            }

            string content = GenerateRtpResponse(this.HttpContext.Request.QueryString.ToString());
            return Content(content, "application/json");

        }

        public static string GenerateRtpResponse(string p_queryString)
        {
            try
            {
                var jsonDownload = string.Empty;
                //string queryString = @"?s=VXX,SVXY,UWM,TWM,^RUT&f=l"; // without JsonP, these tickers are streamed all the time
                Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Sending '{p_queryString}'");
                string reply = VirtualBrokerMessage.Send(p_queryString, VirtualBrokerMessageID.GetRealtimePrice).Result;
                
                if (String.IsNullOrEmpty(reply))
                {
                    string errorMsg = $"RealtimePrice.GenerateRtpResponse(). Received Null or Empty from VBroker. Check that the VirtualBroker is listering on IP: {VirtualBrokerMessage.TcpServerHost}:{VirtualBrokerMessage.TcpServerPort}";
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

        // it is temporary simple redirection (untir VBrokerGateway supports real-time price requests.). 
        //It is needed in SQLab server that HTTPS webpage get code from other HTTPS services. (not HTTP)
        private Tuple<string, string> GenerateRtpResponseBySendingToHqaCompute_Azure_Webservice()
        {
            try
            {
                var jsonDownload = string.Empty;
                string rtpURI = @"http://hqacompute.cloudapp.net/q/rtp" + this.HttpContext.Request.QueryString;
                if (!Utils.DownloadStringWithRetry(out jsonDownload, rtpURI, 5, TimeSpan.FromSeconds(5), false))
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: rtp download was not succesfull: " + rtpURI + @""" }", "application/json");
                }
                else
                    return new Tuple<string, string>(jsonDownload, "application/json");          
            }
            catch (Exception e)
            {
                return new Tuple<string, string>(@"{ ""Message"":  ""Exception caught by WebApi Get(): " + e.Message + @""" }", "application/json");
            }
        }

     
    }
}
