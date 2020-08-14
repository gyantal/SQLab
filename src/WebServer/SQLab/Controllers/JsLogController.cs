using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System.IO;
using System.Text;

namespace SQLab.Controllers
{
    // Logger for Javascript code. This can notify Healthmonitor if Crash occurs in HTML JS in the client side.
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

            var clientIP = WsUtils.GetRequestIP(this.HttpContext);
            var clientUserEmail = WsUtils.GetRequestUser(this.HttpContext);
            if (clientUserEmail == null)
                clientUserEmail = "UnknownUser@gmail.com";

            string jsLogMsgWithOrigin = $"Javascript Logger /JsLogController was called by '{clientUserEmail}' from '{clientIP}' and it indicates error. Received JS log: '{jsLogMessage}'";

            m_logger.LogInformation(jsLogMsgWithOrigin);
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
                HealthMonitorMessage.SendAsync(jsLogMsgWithOrigin, HealthMonitorMessageID.ReportErrorFromSQLabWebsite).TurnAsyncToSyncTask();
            }


            return NoContent();
        }
    }
}
