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

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

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

        [Authorize]
        // http://hqacompute.cloudapp.net/q/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV&f=l it is only 100ms. 
        // http://localhost/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV&f=l  (this is 700ms, which is weird, while from Authorize or not Authorize doesn't help.)anyway, it is a temporary solution, so it doesn't worth debugging it now.
        // https://www.snifferquant.net/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV&f=l  (200ms, which is perfect: 100ms to AWS server, and 100ms from AWS server to Azure server.)
        // http://localhost/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV,^^^VIX201410,GOOG&f=l&jsonp=myCallbackFunction
        // https://www.snifferquant.net/rtp?s=VXX,^VIX,^VXV,^GSPC,XIV,^^^VIX201410,GOOG&f=l&jsonp=myCallbackFunction
        public ActionResult Index()
        {
            var authorizedEmailResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); if (authorizedEmailResponse != null) return authorizedEmailResponse;

            Tuple<string, string> contentAndType = GenerateRtpResponse();
            return Content(contentAndType.Item1, contentAndType.Item2);

        }

        // it is temporary simple redirection (untir VBrokerGateway supports real-time price requests.). 
        //It is needed in SQLab server that HTTPS webpage get code from other HTTPS services. (not HTTP)
        private Tuple<string, string> GenerateRtpResponse()
        {
            try
            {
                string rtpURI = @"http://hqacompute.cloudapp.net/q/rtp" + this.HttpContext.Request.QueryString;
                var jsonDownload = string.Empty;
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
