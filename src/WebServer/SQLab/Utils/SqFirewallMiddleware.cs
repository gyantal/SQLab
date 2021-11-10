using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLab
{
    public class HttpRequestLog
    {
        public DateTime StartTime;
        public bool IsHttps;  // HTTP or HTTPS
        public string Method; // GET, PUT
        public string Path;
        public string QueryString;  // it is not part of the path
        public string ClientIP;
        public string ClientUserEmail;
        public int? StatusCode;
        public double TotalMilliseconds;
        public bool IsError;
        public Exception Exception;
    }


    // we can call it SqFirewallMiddleware because it is used as a firewall too, not only logging request
    internal class SqFirewallMiddleware
    {
        readonly RequestDelegate _next;

        public SqFirewallMiddleware(RequestDelegate next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            // TEMP for Debugging. when is the request HTTP or HTTPS. 
            // decomment the forced "context.Request.Scheme = "https";" in Startup.cs if you want to test it.
            // Utils.Logger.Info($"SqFirewallMiddleware hook: Scheme: {httpContext.Request.Scheme}:// Host: {httpContext.Request.Host}, RequestProtocol:{httpContext.Request.Protocol}, Path:{httpContext.Request.Path.ToString()}");


            // Don't push it to the next Middleware if the path or IP is on the blacklist. In the future, implement a whitelist too, and only allow  requests explicitely on the whitelist.
            if (IsHttpRequestOnBlacklist(httpContext))
            {
                // silently log it and stop processing
                string isHttpsStr = httpContext.Request.IsHttps ? "HTTPS" : "HTTP";
                var clientIP = WsUtils.GetRequestIP(httpContext);
                string msg = String.Format($"{DateTime.UtcNow.ToString("HH':'mm':'ss.f")}#Blacklisted request is terminated: {isHttpsStr} {httpContext.Request.Method} '{httpContext.Request.Path}' from {clientIP}");
                Console.WriteLine(msg);
                Utils.Logger.Info(msg);
                return;
            }

            if (!IsHttpRequestOnWhitelist(httpContext))
            {
                // inform the user in a nice HTML page that 'for security' ask the superwisor to whitelist path ''
                return;
            }

            Exception exception = null;
            DateTime startTime = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                // when NullReference exception was raised in TestHealthMonitorEmailByRaisingException(), The exception didn't fall to here.
                // If it was handled already and I got a nice Error page to the client. So, here, we don't have the exceptions and exception messages and the stack trace.
                exception = e;
                throw;
            }
            finally
            {
                sw.Stop();  // Kestrel measures about 50ms more overhead than this measurement. Add 50ms more to estimate reaction time.

                var statusCode = httpContext.Response?.StatusCode;      // it may be null if there was an Exception
                var level = statusCode > 499 ? Microsoft.Extensions.Logging.LogLevel.Error : Microsoft.Extensions.Logging.LogLevel.Information;
                var clientIP = WsUtils.GetRequestIP(httpContext);
                var clientUserEmail = WsUtils.GetRequestUser(httpContext);

                var requestLog = new HttpRequestLog() { StartTime = DateTime.UtcNow, IsHttps = httpContext.Request.IsHttps, Method = httpContext.Request.Method, Path = httpContext.Request.Path, QueryString = httpContext.Request.QueryString.ToString(), ClientIP = clientIP, ClientUserEmail = clientUserEmail, StatusCode = statusCode, TotalMilliseconds = sw.Elapsed.TotalMilliseconds, IsError = exception != null || (level == Microsoft.Extensions.Logging.LogLevel.Error), Exception = exception };
                lock (Program.g_webAppGlobals.HttpRequestLogs)  // prepare for multiple threads
                {
                    Program.g_webAppGlobals.HttpRequestLogs.Enqueue(requestLog);
                    while (Program.g_webAppGlobals.HttpRequestLogs.Count > 50*10)  // 2018-02-19: MaxHttpRequestLogs was 50, but changed to 500, because RTP (RealTimePrice) rolls 50 items out after 2 hours otherwise. 500 items will last for 20 hours.
                        Program.g_webAppGlobals.HttpRequestLogs.Dequeue();
                }

                // $"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#

                // string.Format("Value is {0}", someValue) which will check for a null reference and replace it with an empty string. It will however throw an exception if you actually pass  null like this string.Format("Value is {0}", null)
                string msg = String.Format("{0}#{1}{2} {3} '{4}' from {5} (u: {6}) ret: {7} in {8:0.00}ms", requestLog.StartTime.ToString("HH':'mm':'ss.f"), requestLog.IsError ? "ERROR in " : String.Empty, requestLog.IsHttps ? "HTTPS" : "HTTP", requestLog.Method, requestLog.Path, requestLog.ClientIP, requestLog.ClientUserEmail, requestLog.StatusCode, requestLog.TotalMilliseconds);
                Console.WriteLine(msg);
                Utils.Logger.Info(msg);

                if (requestLog.IsError)
                    LogDetailedContextForError(httpContext, requestLog);

                // at the moment, send only raised Exceptions to HealthMonitor, not general IsErrors, like wrong statusCodes
                if (requestLog.Exception != null && IsSendableToHealthMonitorForEmailing(requestLog.Exception))
                {
                    StringBuilder sb = new StringBuilder("Exception in Website.C#.SqFirewallMiddleware. \r\n");
                    var requestLogStr = String.Format("{0}#{1}{2} {3} '{4}' from {5} (u: {6}) ret: {7} in {8:0.00}ms", requestLog.StartTime.ToString("HH':'mm':'ss.f"), requestLog.IsError ? "ERROR in " : String.Empty, requestLog.IsHttps ? "HTTPS" : "HTTP", requestLog.Method, requestLog.Path + (String.IsNullOrEmpty(requestLog.QueryString) ? "" : requestLog.QueryString), requestLog.ClientIP, requestLog.ClientUserEmail, requestLog.StatusCode, requestLog.TotalMilliseconds);
                    sb.Append("Request: " + requestLogStr + "\r\n");
                    sb.Append("Exception: '" + requestLog.Exception.ToStringWithShortenedStackTrace(400) + "'\r\n");
                    HealthMonitorMessage.SendAsync(sb.ToString(), HealthMonitorMessageID.ReportErrorFromSQLabWebsite).TurnAsyncToSyncTask();
                }
                    
            }
            
        }

        // "/robots.txt", "/ads.txt": just don't want to handle search engines. Consume resources.
        static string[] m_blacklistStarts = { "/robots.txt", "/ads.txt", "//", "/index.php", "/user/register", "/latest/dynamic", "/ws/stats", "/corporate/", "/imeges", "/remote"};
        // hackers always try to break the server by typical vulnerability queries. It is pointless to process them. Most of the time it raises an exception.
        static bool IsHttpRequestOnBlacklist(HttpContext p_httpContext)
        {
            // 1. check request path is allowed
            foreach (var blacklistStr in m_blacklistStarts)
            {
                if (p_httpContext.Request.Path.StartsWithSegments(blacklistStr, StringComparison.OrdinalIgnoreCase))   
                    return true;
            }

            // 2. check client IP is banned or not
            return false;
        }

        static bool IsHttpRequestOnWhitelist(HttpContext p_httpContext)
        {
            return true;
        }

        static void LogDetailedContextForError(HttpContext httpContext, HttpRequestLog requestLog)
        {
            var request = httpContext.Request;
            string headers = String.Empty;
            foreach (var key in request.Headers.Keys)
                headers += key + "=" + request.Headers[key] + Environment.NewLine;

            string msg = String.Format("{0}{1} {2} '{3}' from {4} (user: {5}) responded {6} in {7:0.00} ms. RequestHeaders: {8}", requestLog.IsError ? "ERROR in " : String.Empty, requestLog.IsHttps ? "HTTPS" : "HTTP", requestLog.Method, requestLog.Path + (String.IsNullOrEmpty(requestLog.QueryString) ? "" : requestLog.QueryString), requestLog.ClientIP, requestLog.ClientUserEmail, requestLog.StatusCode, requestLog.TotalMilliseconds, headers);
            Console.WriteLine(msg);
            Utils.Logger.Error(msg);    // all the details (IP, Path) go the the Error output, because if the Info level messages are ignored by the Logger totally, this will inform the user. We need all the info in the Error Log. Even though, if Info and Error levels both logged, it results duplicates
        }


        public static bool IsSendableToHealthMonitorForEmailing(Exception p_exception)
        {
            // anonymous people sometimes connect and we have SSL or authentication errors
            // also we are not interested in Kestrel Exception. Some of these exceptions are not bugs, but correctSSL or Authentication fails.
            // we only interested in our bugs our Controller C# code
            string fullExceptionStr = p_exception.ToString();   // You can simply print exception.ToString() -- that will also include the full text for all the nested InnerExceptions.
            bool isSendable = true;
            if (fullExceptionStr.IndexOf("SSL Handshake failed with OpenSSL error") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf("ECONNRESET connection reset by peer") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf("The handshake failed due to an unexpected packet format") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf("ENOTCONN socket is not connected") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf("Authentication failed because the remote party has closed the transport stream") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"The path in 'value' must start with '/'") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"System.Threading.Tasks.TaskCanceledException: The request was aborted") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"The decryption operation failed, see inner exception") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Unrecognized HTTP version") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Malformed request: invalid headers") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Malformed request: MethodIncomplete") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Malformed request: TargetIncomplete") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Malformed request: VersionIncomplete") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"SSL Read BIO failed with OpenSSL error") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"The input string contains non-ASCII or null characters") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Error -110 ETIMEDOUT connection timed out") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"UvException: Error -32 EPIPE broken pipe") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Received an unexpected EOF or 0 bytes from the transport stream") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Connection processing ended abnormally") != -1)  // System.NullReferenceException at  at Microsoft.AspNetCore.Server.Kestrel.Internal.Http.PathNormalizer.ContainsDotSegments(String path)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Missing request target") != -1)      // 'Microsoft.AspNetCore.Server.Kestrel.BadHttpRequestException: Missing request target.'
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"System.IndexOutOfRangeException: Index was outside the bounds of the array." + Environment.NewLine + "   at Microsoft.AspNetCore.Routing.Template.TemplateMatcher.TryMatch") != -1)      // A Kestrel error when this query arrived: "Request starting HTTP/1.1 GET https://23.20.243.199//recordings//theme/main.css". Probably they will fix it, but I don't want to receive errors about it.
                isSendable = false;
            if ((fullExceptionStr.IndexOf(@"System.ArgumentException: Decoded string is not a valid IDN name.") != -1) &&      // If this Exception occurs in Kestrel, swallow it. If it occurs in our code, we send the error.
                (fullExceptionStr.IndexOf(@"at Microsoft.AspNetCore.Hosting.Internal.HostingLoggerExtensions.RequestStarting(") != -1))
                isSendable = false;
            if (fullExceptionStr.IndexOf("A call to SSPI failed") != -1)
                isSendable = false;
            // 2017-12: after migrating to DotNetCore2 
            if (p_exception is Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException)
            {
                // bad request data: "Request is missing Host header."
                // bad request data: "Invalid request line: ..."
                isSendable = false;
            }

            if (p_exception is System.Net.Http.HttpRequestException 
                && p_exception.InnerException != null && p_exception.InnerException is System.IO.IOException
                && p_exception.InnerException.InnerException != null && p_exception.InnerException.InnerException is System.Net.Sockets.SocketException)
            {
                // "FullExceptionStr:'System.Net.Http.HttpRequestException: An error occurred while sending the request. 
                // ---> System.IO.IOException: Unable to read data from the transport connection: Connection reset by peer. 
                // ---> System.Net.Sockets.SocketException: Connection reset by peer"
                // HTTP GET '/dist/__webpack_hmr' from 52.211.231.5 (u: DC)"
                // this happens overnight, or at 5am in the morning. Probably when he left a Chrome tabpage open. The laptop went to sleep mode overnight. Webpack connection was disconnected, but the client still sometimes ask for the data.
                // the client waits 11sec for the data, and the server usually serves this in 1.5sec. 
                // but when the server doesn't serve it for 11sec, then client break the connection, thinking it is fail. And the client is right.
                // actually the problem is that why sometimes GET '/dist/__webpack_hmr' takes 15 sec.
                // at this times, server probably needs a restart. So, we are better if we leave this message to be sent to the admin.
                // Yes. Before restart GET '/dist/__webpack_hmr' was served in 1200ms, (and 15sec rarely), after server restart it is served in 500ms. So, restart helped.
                // What is annoying the client leaves the 'dead' tabpage open in Chrome, which consumes resources all the time. 
                // However, with DotNet 3, and published static file Angular release, it won't be that big problem, because there is no Debug style _webpack under the published one.
                Utils.Logger.Debug($"Warning. Connection reset by peer. Probably because of 15sec timeout on the client while serving /dist/__webpack_hmr. This is the problem, slowness of the server. Consider restarting server. ");
                isSendable = true;
            }


            Utils.Logger.Debug($"IsSendableToHealthMonitorForEmailing().IsSendable:{isSendable}, FullExceptionStr:'{fullExceptionStr}'");
            return isSendable;
        }

    }
}
