using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    public class WebServerController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public WebServerController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        [HttpGet]   // Ping is accessed by the WebJob every 5 minutes (to keep it alive), no no GoogleAuth there
        public ActionResult Ping()
        {
            // pinging Index.html do IO file operation. Also currently it is a Redirection. There must be a quicker way to ping our Webserver. (for keeping it alive)
            // a ping.html or better a c# code that gives back only some bytes, not reading files. E.G. it gives back UTcTime. It has to be quick.
            return Content(@"<HTML><body>Ping. Webserver UtcNow:" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "</body></HTML>", "text/html");
        }

        [HttpGet]   // Ping is accessed by the WebJob every 5 minutes (to keep it alive), no no GoogleAuth there
        [Authorize]     // we can live without it, because ControllerCommon.CheckAuthorizedGoogleEmail() will redirect to /login anyway, but it is quicker that this automatically redirects without clicking another URL link.
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
            sb.Append("Access Token: " + HttpContext.Authentication.GetTokenAsync("access_token").Result + "<br>");
            sb.Append("Refresh Token: " + HttpContext.Authentication.GetTokenAsync("refresh_token").Result + "<br>");
            sb.Append("Token Type: " + HttpContext.Authentication.GetTokenAsync("token_type").Result + "<br>");
            sb.Append("expires_at: " + HttpContext.Authentication.GetTokenAsync("expires_at").Result + "<br>");
            sb.Append("<a href=\"/logout\">Logout</a><br>");
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html");
        }

        [HttpGet]   // Ping is accessed by the WebJob every 5 minutes (to keep it alive), no no GoogleAuth there
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

        [HttpGet]   // Ping is accessed by the WebJob every 5 minutes (to keep it alive), no no GoogleAuth there
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
    }
}
