using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SQLab.Controllers
{
    public class ControllerCommon
    {
        public static ActionResult CheckAuthorizedGoogleEmail(Controller p_controller, ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
            //var user = HttpContext.User;
            //// Deny anonymous request beyond this point.
            //if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
            //{
            //    //return new ChallengeResult("Google", new AuthenticationProperties { RedirectUri = "/" });
            //    return new RedirectResult("/login");
            //}

            //StringBuilder sbUser = new StringBuilder();
            //sbUser.AppendLine("Hello " + (HttpContext.User.Identity.Name ?? "anonymous"));
            //foreach (var claim in HttpContext.User.Claims)
            //{
            //    sbUser.AppendLine(claim.Type + ": " + claim.Value);
            //}
            
            string email = "Unknown";
            foreach (var claim in p_controller.User.Claims)
            {
                if (claim.Type == @"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                    email = claim.Value;
            }
            p_logger.LogInformation($"{((p_controller.HttpContext.Request.Path.HasValue) ? p_controller.HttpContext.Request.Path.Value : String.Empty)} is called with email: '" + email + "'");
            if (email == "Unknown")
            {
                return new RedirectResult("/login");
            }

            if (!Utils.IsAuthorizedGoogleUsers(p_config, email))
            {
                return p_controller.Content(@"<HTML><body>Google Authorization Is Required. Your Google account: '<strong>" + email + @"'</strong> is not accepted.<br/>" +
                   @"<A href=""/logout"">logout this Google user </a> and login with another one." +
                    "</body></HTML>", "text/html");
            }
            else
                return null;
        }
    }
}
