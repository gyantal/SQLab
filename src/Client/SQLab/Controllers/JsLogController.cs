using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using SqCommon;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    public class JsLogController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public JsLogController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        // http://stackoverflow.com/questions/16996713/receiving-a-http-post-in-http-handler
        [HttpGet, HttpPost]
        public ActionResult Index()
        {
            string jsLogMessage = null;
            // if http.setRequestHeader("Content-type", "application/x-www-form-urlencoded");   is given, Request.Form has the message as Dictionary
            // if http.setRequestHeader("Content-type", "application/x-www-form-urlencoded");    is not given, Request.Body stream has a message as Stream
            // use the general Stream version, because we don't want that  the Framework processes in unnecessarily into properties. It is a one big string for us.
            using (var reader = new StreamReader(Request.Body))
            {
                // This will equal to "charset = UTF-8 & param1 = val1 & param2 = val2 & param3 = val3 & param4 = val4"
                jsLogMessage = reader.ReadToEnd();
            }

            string email, ip;
            ControllerCommon.GetRequestUserAndIP(this, out email, out ip);
            string jsLogMsgWithOrigin = $"User '{email}' from '{ip}': {jsLogMessage}";

            m_logger.LogInformation("JsLog arrived: " + jsLogMsgWithOrigin);
            // get the control string, which is until the first ":".  e.g. "JsLog.Err:Error: Uncaught ReferenceError:..."
            int logLevelInd = jsLogMessage.IndexOf(':');
            if (logLevelInd == -1)
            {
                m_logger.LogError("Unrecognized jsLog message.");
                return NoContent();
            }
            string logLevel = jsLogMessage.Substring(0, logLevelInd);
            if (logLevel == "JsLog.Err")
            {   // notify HealthMonitor to send an email
                HealthMonitorMessage.Send("Website.JS", jsLogMsgWithOrigin, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);

            }


            return NoContent();
        }
    }
}
