using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SqCommon;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    [Route("~/", Name = "default")]
    // [Route("~/login", Name = "login")]
    //[Route("~/access_denied", Name = "access_denied")]
    [Route("~/app/{*url}", Name = "app")]
    [Route("~/DeveloperDashboard", Name = "DeveloperDashboard")]
    [Route("~/UserDashboard", Name = "UserDashboard")]
    [Route("~/VolatilityIndicesInDifferentMonths", Name = "VolatilityIndicesInDifferentMonths")]
    [Route("~/VXXAdaptiveConnorLiveBacktest", Name = "VXXAdaptiveConnorLiveBacktest")]
    [Authorize]     // we can live without it, because ControllerCommon.CheckAuthorizedGoogleEmail() will redirect to /login anyway, but it is quicker that this automatically redirects without clicking another URL link.
    public class RootHomepageRedirectController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public RootHomepageRedirectController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        public ActionResult Index()
        {
            var authorizedEmailResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); if (authorizedEmailResponse != null) return authorizedEmailResponse;

            var urlPath = (HttpContext.Request.Path.HasValue) ? HttpContext.Request.Path.Value.ToLower() : String.Empty;
#if DEBUG   // for the Index page, give Dashboard according to DEBUG or RELEASE
            if (String.IsNullOrWhiteSpace(urlPath) || urlPath == "/")
                urlPath = "/developerdashboard";
#else
            if (String.IsNullOrWhiteSpace(urlPath) || urlPath == "/")
                urlPath = "/userdashboard";
#endif
            string fileName = String.Empty;
            switch (urlPath)
            {
                case "/userdashboard":
                    fileName = "UserDashboard.html";
                    break;
                case "/developerdashboard":
                    fileName = "DeveloperDashboard.html";
                    break;
                case "/volatilityindicesindifferentmonths":
                    fileName = "VolatilityIndicesInDifferentMonths.html";
                    break;
                case "/vxxadaptiveconnorlivebacktest":
                    fileName = "VXXAdaptiveConnorLiveBacktest.html";
                    break;
                default:
                    fileName = "UserDashboard.html";
                    break;
            }

            string fileStr = System.IO.File.ReadAllText(((Utils.RunningPlatform() == Platform.Linux) ?
                    $"/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/{fileName}" :
                    @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\" + fileName));
            return Content(fileStr, "text/html");
        }

        
    }
}
