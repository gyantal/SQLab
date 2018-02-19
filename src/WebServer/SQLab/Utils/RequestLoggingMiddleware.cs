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


    internal class RequestLoggingMiddleware
    {
        readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            Exception exception = null;
            DateTime startTime = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                // when NullReference exception was raised in TestHealthMonitorEmailByRaisingException(), The excption didn't fall to here. if 
                // It was handled already and I got a nice Error page to the client. So, here, we don't have the exceptions and exception messages and the stack trace.
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
                    StringBuilder sb = new StringBuilder("Exception in Website.C#.RequestLoggingMiddleware. \r\n");
                    var requestLogStr = String.Format("{0}#{1}{2} {3} '{4}' from {5} (u: {6}) ret: {7} in {8:0.00}ms", requestLog.StartTime.ToString("HH':'mm':'ss.f"), requestLog.IsError ? "ERROR in " : String.Empty, requestLog.IsHttps ? "HTTPS" : "HTTP", requestLog.Method, requestLog.Path + (String.IsNullOrEmpty(requestLog.QueryString) ? "" : requestLog.QueryString), requestLog.ClientIP, requestLog.ClientUserEmail, requestLog.StatusCode, requestLog.TotalMilliseconds);
                    sb.Append("Request: " + requestLogStr + "\r\n");
                    sb.Append("Exception: '" + requestLog.Exception.ToStringWithShortenedStackTrace(400) + "'\r\n");
                    HealthMonitorMessage.Send(sb.ToString(), HealthMonitorMessageID.ReportErrorFromSQLabWebsite);
                }
                    
            }
            
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


            Utils.Logger.Debug($"IsSendableToHealthMonitorForEmailing().IsSendable:{isSendable}, FullExceptionStr:'{fullExceptionStr}'");
            return isSendable;
        }

    }
}
