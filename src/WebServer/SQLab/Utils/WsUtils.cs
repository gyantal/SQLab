using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab
{
    public static partial class WsUtils
    {
        public static string GetRequestUser(HttpContext p_httpContext)
        {
            var userEmailClaim = p_httpContext?.User?.Claims?.FirstOrDefault(p => p.Type == @"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            return userEmailClaim?.Value;
        }

        // Some fallback logic can be added to handle the presence of a Load Balancer.  or CloudFront. Checked: CloudFront uses X-Forwarded-For : "82.44.159.196"
        // http://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
        public static string GetRequestIP(HttpContext p_httpContext, bool p_tryUseXForwardHeader = true)
        {
            string remoteIP = null;
            if (p_tryUseXForwardHeader)
            {
                remoteIP = GetHeaderValueAs<string>(p_httpContext, "X-Forwarded-For");       // Old standard, but used by AWS CloudFront
                // todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For
                if (String.IsNullOrWhiteSpace(remoteIP))
                    remoteIP = GetHeaderValueAs<string>(p_httpContext, "Forwarded");     // new standard  (2014 RFC 7239)
                //if (String.IsNullOrWhiteSpace(remoteIP))
                //     remoteIP = GetHeaderValueAs<string>(p_controller, "REMOTE_ADDR");     // there are 10 more, but we have to support only CloudFront for CPU saving. We don't need others. http://stackoverflow.com/questions/527638/getting-the-client-ip-address-remote-addr-http-x-forwarded-for-what-else-coul

            }

            // one way to get IP
            //var connection = p_httpContext.Features.Get<IHttpConnectionFeature>();
            //var clientIP = connection?.RemoteIpAddress?.ToString();

            // another way to get it
            if (String.IsNullOrWhiteSpace(remoteIP) && p_httpContext?.Connection?.RemoteIpAddress != null)
                remoteIP = p_httpContext?.Connection?.RemoteIpAddress?.ToString();

            return String.IsNullOrWhiteSpace(remoteIP) ? "<Unknown IP>" : remoteIP;
        }

        public static T GetHeaderValueAs<T>(HttpContext p_httpContext, string p_headerName)
        {
            if (p_httpContext?.Request?.Headers?.TryGetValue(p_headerName, out StringValues values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!String.IsNullOrEmpty(rawValues))
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default(T);
        }

    }
}
