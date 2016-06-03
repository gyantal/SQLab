using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SqCommon;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    [Route("~/", Name = "default")]
    [Route("~/login", Name = "login")]
    [Route("~/access_denied", Name = "access_denied")]
    [Route("~/app/{*url}", Name = "app")]
    [Route("~/DeveloperDashboard", Name = "DeveloperDashboard")]
    public class RootHomepageRedirectController : Controller
    {
        public ActionResult Index()

        {
            bool serveDeveloperDashboard = false;
#if DEBUG
            serveDeveloperDashboard = true;
#else
            var urlPath = "";
            if (HttpContext.Request.Path.HasValue)
                urlPath = HttpContext.Request.Path.Value;
            if (urlPath.ToLower() == "/developerdashboard")
                serveDeveloperDashboard = true;     // in RELEASE: only temporary, until it is under heavy development. Later, turn this to False.
#endif
            if (serveDeveloperDashboard)
            {
                // this should work only in local development, not when it is published on the Server as Release. On the server, it can be accessed temporarily with "/DeveloperDashboard.html"
                // loading files from HDD is 370msec at the first time, later it comes from file cache, so only 10msec. Still it is better to serve small Index.html from RAM
                string fullPath = (Utils.RunningPlatform() == Platform.Linux) ?
                    "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/DeveloperDashboard.html" :
                    @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\DeveloperDashboard.html";
             
                string fileStr = System.IO.File.ReadAllText(fullPath);
                return Content(fileStr, "text/html");
            }

            string email = "Unknown";
            foreach (var claim in User.Claims)
            {
                if (claim.Type == @"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                    email = claim.Value;
            }
            //m_logger.LogInformation("RootHomepageRedirect.Index() is called with email: '" + email + "'");
            //if (email == "Unknown")
            //{
            //    return new ChallengeResult("Google", new AuthenticationProperties { RedirectUri = "/" });
            //}

            //if (Utils.IsAuthorizedGoogleUsers(m_config, email))
            //{
            //    StringBuilder sb = new StringBuilder(m_dashboardStr1);
            //    sb.Append(email).Append(m_dashboardStr2);
            //    return Content(sb.ToString(), "text/html");   // Fast and protected and cannot be accessed as a File without Google authentication
            //}
            ////return View("~/Views/Index.html"); 
            ////return Redirect("Index.html");
            //else
            //{
            return Content(@"<HTML><body>Google Authorization Is Required. Your Google account: '<strong>" + email + @"'</strong> is not accepted.<br/>" +
                    @"<A href=""TestAuth/ExternalLogin"">Login here</a> or " +
                    @"<A href=""TestAuth/ExternalLogout"">logout this Google user </a> and login with another one." +
                    "</body></HTML>", "text/html");
            //}
        }
    }
}
