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

            bool serveDeveloperDashboard = false;   // for Index.html, the DEBUG def gives a direction
#if DEBUG
            serveDeveloperDashboard = true;
#endif
            var urlPath = (HttpContext.Request.Path.HasValue) ? HttpContext.Request.Path.Value : String.Empty;
            if (urlPath.ToLower() == "/developerdashboard") // a specific path can always force to get the proper page. Irrespective of DEBUG or RELEASE
                serveDeveloperDashboard = true;
            if (urlPath.ToLower() == "/userdashboard") // a specific path can always force to get the proper page. Irrespective of DEBUG or RELEASE
                serveDeveloperDashboard = false;  

            if (serveDeveloperDashboard)
            {
                // this should work only in local development, not when it is published on the Server as Release. On the server, it can be accessed temporarily with "/DeveloperDashboard.html"
                // loading files from HDD is 370msec at the first time, later it comes from file cache, so only 10msec. Still, in the future, it is better to serve small Index.html from RAM
                string fileStr = System.IO.File.ReadAllText(((Utils.RunningPlatform() == Platform.Linux) ?
                    "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/DeveloperDashboard.html" :
                    @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\DeveloperDashboard.html"));
                return Content(fileStr, "text/html");
            } else
            {
                string fileStr = System.IO.File.ReadAllText(((Utils.RunningPlatform() == Platform.Linux) ?
                    "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/UserDashboard.html" :
                    @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\UserDashboard.html"));
                return Content(fileStr, "text/html");
            }
            
        }

        
    }
}
