using System;
using Microsoft.Extensions.Logging;
using SqCommon;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SQLab
{
    internal class SQLabAspLoggerProvider : ILoggerProvider
    {
        private bool m_disposed = false;

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string p_categoryName)
        {
            Microsoft.Extensions.Logging.ILogger aspLogger = new SQLabCommonAspLogger(p_categoryName);
            return aspLogger;
        }

        //official Disposable pattern. The finalizer should call your dispose method explicitly.
        //Note:!! The finalizer isn't guaranteed to be called if your application hard crashes.
        //Checked: when I shutdown the Webserver, by typing Ctrl-C, saying Initiate Webserver shutdown, this Dispose was called by an External code; maybe Webserver, maybe the GC. 
        // So, it means that at official Webserver shutdown (change of web.config, shutting down AzureVM, etc.), this Dispose is called.
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool p_disposing)
        {
            if (p_disposing)
            {
                // dispose managed resources
                if (!m_disposed)
                {
                    m_disposed = true;
                    //m_nLogFactory.Flush();  // for sending all the Logs to TableStorage or to the logger EmailBufferingWrapper
                    //m_nLogFactory.Dispose();
                }
            }
            // dispose unmanaged resources
        }

        ~SQLabAspLoggerProvider()
        {
            this.Dispose(false);
        }
    }

    internal class SQLabCommonAspLogger : Microsoft.Extensions.Logging.ILogger, IDisposable
    {
        private string p_categoryName;
        object m_scope;

        public SQLabCommonAspLogger(string p_categoryName)
        {
            this.p_categoryName = p_categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            m_scope = state;
            return this;
        }

        // Gets a value indicating whether logging is enabled for the specified level.
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical: return true;
                case LogLevel.Error: return true;
                case LogLevel.Warning: return true;
                case LogLevel.Information: return true;
                case LogLevel.Trace: return false;
                default: return false;
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;
            if (formatter != null)
            {
                try
                {
                    message = formatter(state, exception);  // this is Microsoft.AspNetCore.Hosting.Internal.HostingLoggerExtensions.HostingRequestStarting.ToString(). it can give exception: "System.ArgumentException: Decoded string is not a valid IDN name."
                }
                catch (Exception e) // "System.ArgumentException: Decoded string is not a valid IDN name." at Microsoft.AspNetCore.Http.HostString.ToUriComponent()
                {
                    // swallow the exception and substitute Message with the Exception data.
                    message = "SQLabAspLoggerProvider.Log(): Log Message could'n be formatted to ASCII by the Formatter. Probably bad URL input query. Investigate log files about the URL query. Exception is this: " + e.ToString();
                }
                //if (exception != null)  // formatter function doesn't put the Exception into the message, so add it.
                //{
                //    message += Environment.NewLine + exception;
                //}
            }
            else
            {
                if (state != null)
                {
                    message += state;
                }
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            if (!string.IsNullOrEmpty(message))
            {
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        if (exception == null)
                            Utils.Logger.Fatal(message);
                        else
                            Utils.Logger.Fatal(exception, message);
                        break;
                    case LogLevel.Error:
                        if (exception == null)
                            Utils.Logger.Error(message);
                        else
                            Utils.Logger.Error(exception, message);
                        break;
                    case LogLevel.Warning:
                        if (exception == null)
                            Utils.Logger.Warn(message);
                        else
                            Utils.Logger.Warn(exception, message);
                        break;
                    case LogLevel.Information:
                        Utils.Logger.Info(message);
                        break;
                    case LogLevel.Trace:
                        Utils.Logger.Debug(message);
                        break;
                    default:
                        Utils.Logger.Debug(message);
                        break;
                }


                //_traceSource.TraceEvent(GetEventType(logLevel), eventId.Id, message);
            }

            if (exception != null && IsSendableToHealthMonitorForEmailing(exception))
                HealthMonitorMessage.SendException("Website.C#.AspLogger", exception, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);

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
            if (fullExceptionStr.IndexOf(@"Microsoft.AspNetCore.Server.Kestrel.BadHttpRequestException: Invalid method") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Microsoft.AspNetCore.Server.Kestrel.BadHttpRequestException: Missing method") != -1)
                isSendable = false;
            if (fullExceptionStr.IndexOf(@"Microsoft.AspNetCore.Server.Kestrel.BadHttpRequestException: Invalid request line") != -1)
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




            Utils.Logger.Debug($"IsSendableToHealthMonitorForEmailing().IsSendable:{isSendable}, FullExceptionStr:'{fullExceptionStr}'");
            return isSendable;
        }

        public void Dispose()
        {
        }
    }

}