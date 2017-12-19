using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace SQLab.Controllers
{
    //[Route("WebServer")]
    public class WebServerController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public WebServerController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        [HttpGet]   // Ping is accessed by the HealthMonitor every 9 minutes (to keep it alive), no no GoogleAuth there
        public ActionResult Ping()
        {
            // pinging Index.html do IO file operation. Also currently it is a Redirection. There must be a quicker way to ping our Webserver. (for keeping it alive)
            // a ping.html or better a c# code that gives back only some bytes, not reading files. E.G. it gives back UTcTime. It has to be quick.
            return Content(@"<HTML><body>Ping. Webserver UtcNow:" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "</body></HTML>", "text/html");
        }

        [HttpGet]
#if !DEBUG
        [Authorize]     // we can live without it, because ControllerCommon.CheckAuthorizedGoogleEmail() will redirect to /login anyway, but it is quicker that this automatically redirects without clicking another URL link.
#endif
        public ActionResult UserInfo()
        {
            var authorizedEmailResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); if (authorizedEmailResponse != null) return authorizedEmailResponse;

            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("Hello " + (User.Identity.Name ?? "anonymous") + "<br>");
            sb.Append("Request.Path '" + (Request.Path.ToString() ?? "Empty") + "'<br>");
            foreach (var claim in User.Claims)
            {
                sb.Append(claim.Type + ": " + claim.Value + "<br>");
            }

            sb.Append("Tokens:<br>");
            sb.Append("Access Token: " + HttpContext.GetTokenAsync("access_token").Result + "<br>");
            sb.Append("Refresh Token: " + HttpContext.GetTokenAsync("refresh_token").Result + "<br>");
            sb.Append("Token Type: " + HttpContext.GetTokenAsync("token_type").Result + "<br>");
            sb.Append("expires_at: " + HttpContext.GetTokenAsync("expires_at").Result + "<br>");
            sb.Append("<a href=\"/logout\">Logout</a><br>");
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html");
        }

        [HttpGet]
        public ActionResult HttpRequestHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("Request.Headers: <br><br>");
            foreach (var header in Request.Headers)
            {
                sb.Append($"{header.Key} : {header.Value} <br>");
            }
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html");
        }

        [HttpGet]
        public ActionResult TestHealthMonitorEmailByRaisingException()
        {
            string crash = null;
            Console.WriteLine(crash.ToString());

            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("TestHealthMonitorEmailByRaisingException: <br><br>");
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html");
        }

        // in theory we only support HTTP POST, because we need the data in the package. However, AWS CloudFront "Redirect HTTP to HTTPS" turned POST to GET. We removed that CloudFront feature.
        //--- AWS CloudFrontFront settings: 
        //"Redirect HTTP to HTTPS"  https://www.snifferquant.net/HealthMonitor doesn't work.
        //to allow both "HTTP and HTTPS": https://www.snifferquant.net/HealthMonitor  works.
        //That was the solution.
        //"Redirect HTTP to HTTPS" of CloudFront changes HTTPS POST to GET, ruining it, and even removing the data package of the POST.
        //Bad.So, solution is that I have to allow CloudFront both HTTP and HTTPS traffic flowing to OriginServer,
        //and IF I don't want HTTP traffic, I should redirect it locally, on the Kestrel server.
        //However, it is not an important development, so keep both HTTP and HTTPS for now.
        //[HttpPost]
        [HttpPost, HttpGet]     // we only leave HttpGet here so we got a Log message into a log file.
        public ActionResult ReportHealthMonitorCurrentStateToDashboardInJSON()
        {
            long highResWebRequestReceivedTime = System.Diagnostics.Stopwatch.GetTimestamp();
            m_logger.LogInformation("ReportHealthMonitorCurrentStateToDashboardInJSON() is called");
            // TODO: we should check here if it is a HttpGet (or a message without data package) and return gracefully

            try
            {
                if (Request.Body.CanSeek)
                {
                    Request.Body.Position = 0;                 // Reset the position to zero to read from the beginning.
                }
                string jsonToBackEnd = new StreamReader(Request.Body).ReadToEnd();

                try
                {
                    string messageFromWebJob = null;
                    using (var client = new TcpClient())
                    {
                        Task task = client.ConnectAsync(ServerIp.HealthMonitorPublicIp, HealthMonitorMessage.DefaultHealthMonitorServerPort);
                        if (Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))).Result != task)
                        {
                            m_logger.LogError("Error:HealthMonitor server: client.Connect() timeout.");
                            return Content(@"{""ResponseToFrontEnd"" : ""Error: Error:HealthMonitor server: client.Connect() timeout.", "application/json");
                        }

                        BinaryWriter bw = new BinaryWriter(client.GetStream());
                        bw.Write((Int32)HealthMonitorMessageID.GetHealthMonitorCurrentStateToHealthMonitorWebsite);
                        bw.Write(jsonToBackEnd);
                        bw.Write((Int32)HealthMonitorMessageResponseFormat.JSON);

                        BinaryReader br = new BinaryReader(client.GetStream());
                        messageFromWebJob = br.ReadString();
                        m_logger.LogDebug("ReportHealthMonitorCurrentStateToDashboardInJSON() returned answer: " + messageFromWebJob);
                    }

                    m_logger.LogDebug("ReportHealthMonitorCurrentStateToDashboardInJSON() after WaitMessageFromWebJob()");
                    return Content(messageFromWebJob, "application/json");
                }
                catch (Exception e)
                {
                    m_logger.LogError("Error:HealthMonitor SendMessage exception:  " + e);
                    return Content(@"{""ResponseToFrontEnd"" : ""Error:HealthMonitor SendMessage exception. Check log file of the WepApp: " + e.Message, "application/json");
                }
            }
            catch (Exception ex)
            {
                return Content(@"{""ResponseToFrontEnd"" : ""Error: " + ex.Message + @"""}", "application/json");
            }


        }



        [HttpGet]   // Ping is accessed by the HealthMonitor every 9 minutes (to keep it alive), no no GoogleAuth there
        public ActionResult TestUnobservedTaskException20170315()
        {
            Utils.Logger.Info("TestUnobservedTaskException20170315() BEGIN");
            GC.Collect();       // temp: for debugging exceptions
            GC.WaitForPendingFinalizers();
            Utils.Logger.Info("TestUnobservedTaskException20170315() GC.Collected() 1");
            Thread.Sleep(1000);
            Utils.Logger.Info("TestUnobservedTaskException20170315() Sleep(1000) 1");

            string webpageHist;
            bool isOk = Utils.DownloadStringWithRetry(out webpageHist, "http://vixcentral.com/historical/?days=10000", 3, TimeSpan.FromSeconds(2), true);
            if (!isOk)
                return null;

            Utils.Logger.Info("TestUnobservedTaskException20170315() 300K was downloaded");
            Thread.Sleep(1000);
            Utils.Logger.Info("TestUnobservedTaskException20170315() Sleep(1000) 2");

            ////Downloading live data from vixcentral.com.
            //string webpageLive;
            //bool isOkLive = Utils.DownloadStringWithRetry(out webpageLive, "http://vixcentral.com", 3, TimeSpan.FromSeconds(2), true);
            //if (!isOkLive)
            //    return null;

            GC.Collect();       // temp: for debugging exceptions
            GC.WaitForPendingFinalizers();
            Utils.Logger.Info("TestUnobservedTaskException20170315() GC.Collected() 2");

            return Content(@"<HTML><body>TestUnobservedTaskException20170315() finished OK. <br> Webserver UtcNow:" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "</body></HTML>", "text/html");
        }

        [HttpGet]   // Ping is accessed by the HealthMonitor every 9 minutes (to keep it alive), no no GoogleAuth there
        public ActionResult TestGoogleApiGsheet1()
        {
            Utils.Logger.Info("TestGoogleApiGsheet1() BEGIN");

            string valuesFromGSheetStr = "Error. Make sure GoogleApiKeyKey, GoogleApiKeyKey is in SQLab.WebServer.SQLab.NoGitHub.json !";
            if (!String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyName"]) && !String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyKey"]))
            {
                if (!Utils.DownloadStringWithRetry(out valuesFromGSheetStr, "https://sheets.googleapis.com/v4/spreadsheets/1onwqrdxQIIUJytd_PMbdFKUXnBx3YSRYok0EmJF8ppM/values/A1%3AA3?key=" + Utils.Configuration["GoogleApiKeyKey"], 3, TimeSpan.FromSeconds(2), true))
                    valuesFromGSheetStr = "Error in DownloadStringWithRetry().";
            }

            Utils.Logger.Info("TestGoogleApiGsheet1() END");
            return Content($"<HTML><body>TestGoogleApiGsheet1() finished OK. <br> Received data: '{valuesFromGSheetStr}'</body></HTML>", "text/html");
        }
    }
}
