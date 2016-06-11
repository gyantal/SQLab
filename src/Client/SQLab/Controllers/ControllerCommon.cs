using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
            p_logger.LogInformation($"!!! User '{email}' requesting '{((p_controller.HttpContext.Request.Path.HasValue) ? p_controller.HttpContext.Request.Path.Value : String.Empty)}' from {GetRequestIP(p_controller)}.");
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

        // Some fallback logic can be added to handle the presence of a Load Balancer.  or CloudFront. Checked: CloudFront uses X-Forwarded-For : "82.44.159.196"
        // http://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
        public static string GetRequestIP(Controller p_controller, bool p_tryUseXForwardHeader = true)
        {
            string remoteIP = null;
            if (p_tryUseXForwardHeader)
            {
                remoteIP = GetHeaderValueAs<string>(p_controller, "X-Forwarded-For");       // Old standard, but used by AWS CloudFront
                // todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For
                if (String.IsNullOrWhiteSpace(remoteIP))
                    remoteIP = GetHeaderValueAs<string>(p_controller, "Forwarded");     // new standard  (2014 RFC 7239)
                //if (String.IsNullOrWhiteSpace(remoteIP))
                //     remoteIP = GetHeaderValueAs<string>(p_controller, "REMOTE_ADDR");     // there are 10 more, but we have to support only CloudFront for CPU saving. We don't need others. http://stackoverflow.com/questions/527638/getting-the-client-ip-address-remote-addr-http-x-forwarded-for-what-else-coul

            }

            if (String.IsNullOrWhiteSpace(remoteIP) && p_controller.HttpContext.Connection.RemoteIpAddress != null)
                remoteIP = p_controller.HttpContext.Connection.RemoteIpAddress.ToString();

            return String.IsNullOrWhiteSpace(remoteIP) ? "<Unknown IP>": remoteIP;
        }

        public static T GetHeaderValueAs<T>(Controller p_controller, string p_headerName)
        {
            StringValues values;

            if (p_controller.HttpContext?.Request?.Headers?.TryGetValue(p_headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!String.IsNullOrEmpty(rawValues))
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default(T);
        }
    }
}
