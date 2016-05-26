using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    [Route("~/", Name = "default")]
    [Route("~/login", Name = "login")]
    [Route("~/access_denied", Name = "access_denied")]
    [Route("~/app/{*url}", Name = "app")]
    public class RootHomepageRedirectController : Controller
    {
        public ActionResult Index()
        {
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
