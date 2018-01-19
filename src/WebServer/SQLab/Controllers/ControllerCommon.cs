using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab.Controllers
{
    public class ControllerCommon
    {
        public static ActionResult CheckAuthorizedGoogleEmail(Controller p_controller, ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
#if DEBUG
            return null;
#else
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

            //GetRequestUserAndIP(p_controller, out string email, out string ip);

            var ip = WsUtils.GetRequestIP(p_controller.HttpContext);
            var email = WsUtils.GetRequestUser(p_controller.HttpContext);
            if (email == null)
                email = "UnknownUser@gmail.com";

            p_logger.LogInformation($"!!! User '{email}' from '{ip}' requesting '{((p_controller.HttpContext.Request.Path.HasValue) ? p_controller.HttpContext.Request.Path.Value : String.Empty)}'.");
            if (email == "Unknown")
            {
                return new RedirectResult("/login");
            }

            if (!Utils.IsAuthorizedGoogleUsers(p_config, email))
            {
                return p_controller.Content(@"<HTML><body>Google Authorization Is Required. Your Google account: '<strong>" + email + @"'</strong> is not accepted.<br>" +
                   @"<A href=""/logout"">logout this Google user </a> and login with another one." +
                    "</body></HTML>", "text/html");
            }
            else
                return null;
#endif
        }

    }
}
