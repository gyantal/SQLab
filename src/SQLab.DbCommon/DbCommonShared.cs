// Miscellaneous code fragments for HQCommonLite (and HQCommon)

// HQCommonLite duplicates source code from HQCommon by including the same
// source code files into both dlls. When such a source code fragment is
// large enough, it is stored in a separate source code file, but when it
// is too small to occupy a separate file, we move it into this file.

// HQCommonLite and HQCommon are not meant to be referenced at the same time,
// because they contain identically named namespaces and types,
// to facilitate porting source code between HQCommon and HQCommonLite (in both
// directions).

// The HQCommonLite namespace is dedicated to things that notably pertain to
// HQCommonLite (although parts of this namespace are in the shared source code
// files which are compiled into HQCommon, too)

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace DbCommon
{
    
    public static partial class DbUtils
    {
        /// <summary> Returns a date-only string (without quotes) in a format which is suitable 
        /// in SQL commands.
        /// IMPORTANT: the current format of the string ("05/24/2008") should be changeable
        /// when the SQL server changes, so don't use this method for other purposes
        /// than SQL communication!
        /// </summary>
        // TODO: a szerver megerti a YYYYMMDD formatumot is! Ez annyibol jobb h. rovidebb
        // A help-ben igy talalod meg: 'Unseparated String Format'.  {0:yyyyMMdd}
        // http://msdn.microsoft.com/en-us/library/ms180878.aspx#UnseparatedStringFormat
        public static string Date2Str(DateTime p_date)
        {
            return p_date.ToString("d", System.Globalization.CultureInfo.InvariantCulture);
        }
        public static string Date2Str(DateTime? p_date)
        {
            return p_date.HasValue ? Date2Str(p_date.Value) : null;
        }

        /// <summary> Returns true for exceptions that occur normally because how the SQL server works.
        /// If such exception occurs, we should Retry the tasks. </summary>
        /// <see cref="http://j.mp/gdw4XL">Sporadic transport-level connection errors from SQL Azure</see>
        /// <param name="p_tryIdx">0 is the first try. Certain errors (like 'Invalid object name...'
        /// that indicate bug in the query) must occur in every try, therefore should NOT be retried
        /// if occur during the first try, but if we passed the first try without that error, then
        /// in subsequent tries the same error indicates bug in the communication layer, and should be
        /// retried. </param>
        public static bool IsSqlExceptionToRetry(SqlException p_e, int p_tryIdx = 0)
        {
            if (p_e == null)
                return false;
            int @class = p_e.Class;
            switch (p_e.Number)
            {
                case 0:
                    //"The connection is broken and recovery is not possible. The client driver attempted to recover the connection one or more times and all attempts failed. Increase the value of ConnectRetryCount to increase the number of recovery attempts."
                    if (IsSqlExceptionToRetry(p_e.InnerException as SqlException, p_tryIdx))
                        return true;
                    break;

                //"Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding." (Class=11, Number=-2, State=0, LineNumber=0)
                //http://msdn.microsoft.com/en-us/library/cc645611.aspx
                case -2: return true;

                //This should be retried for SqlAzure only! (which cannot be examined here)
                //"The instance of SQL Server you attempted to connect to does not support encryption." (Class=20, Number=20, State=0, LineNumber=0)
                case 20: return true;

                //"A transport-level error has occurred when receiving results from the server. (provider: TCP Provider, error: 0 - The specified network name is no longer available.) (Class=20, Number=64, State=0, LineNumber=0)" (Win32 error)
                // This can occur with Class=16, too, see notes_done.txt#110620.1
                case 64: return (@class >= 16);

                //"A transport-level error has occurred when receiving results from the server. (provider: TCP Provider, error: 0 - The semaphore timeout period has expired.) (Class=20, Number=121, State=0, LineNumber=0)"
                //http://msdn.microsoft.com/en-us/library/ms681382.aspx   (Win32 error)
                case 121:
                    return (@class >= 16);      // class==16 was observed with OPENROWSET(), see note#110620.1/2012-01-23

                //"Invalid object name '...'." Experienced with OPENROWSET(), see note#110620.1/2012-01-30
                case 208:
                    return (@class == 16 && 0 < p_tryIdx);

                // "TCP Provider: Timeout error [258]" (Class=16, Number=258, State=1, LineNumber=0) (Win32 error: WAIT_TIMEOUT)
                // Experienced with OPENROWSET(), see note#110620.1/2012-12-31, http://msdn.microsoft.com/en-us/library/ms681382.aspx
                case 258:
                    return (@class == 16);

                //"Transaction (Process ID %d) was deadlocked on %.*ls resources with another process and has been chosen as the deadlock victim. Rerun the transaction."
                //http://msdn.microsoft.com/en-us/library/aa337376.aspx
                case 1205: return true;

                //"A timeout occurred while waiting for memory resources to execute the query in resource pool '%ls' (%ld). Rerun the query."
                //http://msdn.microsoft.com/en-us/library/cc280471.aspx  http://goo.gl/ennOZV
                case 8645: return true;

                //"A network-related or instance-specific error occurred while establishing a connection to SQL Server.
                // The server was not found or was not accessible. Verify that the instance name is correct and that
                // SQL Server is configured to allow remote connections. (provider: TCP Provider, error: 0 - No such host is known.)"
                // (Class=20, Number=11001, State=0, LineNumber=0)  HQEmailDrWatson/done/2012-10-16T014344.4378750Z_YahooDataCrawler.xml
                // http://msdn.microsoft.com/en-us/library/bb326379%28v=sql.100%29.aspx
                case 11001: return true;

                //"Login failed for user '%.*ls'. Reason: Server is in script upgrade mode. Only administrator can connect at this time."
                //why to retry this: http://j.mp/oPY23h
                case 18401: return (@class < 20);

                // See 'docs\robin\notes_done.txt' for more info (look for the following substrings)
                // Experienced with OPENROWSET() commands
                case 65535:
                    return (@class == 16 && (
               p_e.Message.Contains("Session Provider: Physical connection is not usable [xFFFFFFFF]")
            || p_e.Message.Contains("TCP Provider: The specified network name is no longer available")
            || p_e.Message.Contains("TCP Provider: An existing connection was forcibly closed by the remote host")));

                //
                // SQL Azure Connection-loss Errors
                //      https://azure.microsoft.com/en-us/documentation/articles/sql-database-develop-error-messages/
                //      old links: j.mp/edWus5  j.mp/hGzkaI
                //
                //"The client was unable to establish a connection because of an error during connection initialization process before login. Possible causes include the following: ... the server was too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)"
                case 233: return true;

                // msdn.microsoft.com/en-us/library/ff394106.aspx :
                case 4060:     // "Cannot open database "%.*ls" requested by the login. The login failed."
                case 9001:     // "The log for database '..guid..' is not available" occurs during S1<->S2 scaling operations. erros/email#561441e1 Archidata/email#5613bc16
                case 10928:     // "Resource ID: %d. The %s limit for the database is %d and has been reached."
                case 10929:     // "...However, the server is currently too busy to support requests greater than %d for this database..."
                    return true;

                //"A transport-level error has occurred when sending the request to the server. (provider: TCP Provider, error: 0 - An established connection was aborted by the software in your host machine.) (Class=20, Number=10053, State=0, LineNumber=0)"
                case 10053: return true;

                //"A transport-level error has occurred when receiving results from the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.) (Class=20, Number=10054, State=0, LineNumber=0)"
                case 10054:
                    return (@class >= 16);      // class==16 was observed with OPENROWSET(), see note#110620.1/2012-01-23

                case 10060: //"A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.) (Class=20, Number=10060, State=0, LineNumber=0)"
                            // this usually occurs when the SQL server is Disabled
                case 40197: //"The service has encountered an error processing your request. Please try again. Error code %d."  (Azure failover)
                case 40501: //"The service is currently busy. Retry the request after 10 seconds. Code: %d."
                case 40532: //"Cannot open server "%.*ls" requested by the login. The login failed."
                case 40549: //"Session is terminated because you have a long-running transaction. Try shortening your transaction."
                case 40613: //"Database '%.*ls' on server '%.*ls' is not currently available. Please retry the connection later."
                case 40615: //"Client with IP address '{1}' is not allowed to access the server... It may take up to five minutes for this change to take effect."
                case 40627: //"Operation on server '{0}' and database '{1}' is in progress. Please wait a few minutes before trying again."
                case 40640: //"The server encountered an unexpected exception."
                case 40642: //"The server is currently too busy. Please try again later."
                case 40648: //"Too many requests have been performed. Please retry later."
                case 40650: //"Subscription does not exist or is not ready for the operation."
                case 40671: //"Communication failure between the gateway and the management service. Please retry later."
                case 45168: //"The SQL Azure system is under load, ... The server ... has exceeded the maximum number of concurrent connections. Try again later."
                case 45169: //"The SQL azure system is under load, ... The subscription ... has exceeded the maximum number of concurrent connections ... Try again later."
                case 49918: //"Cannot process request. Not enough resources to process request. The service is currently busy. Please retry the request later."
                case 49919: //"Cannot process create or update request. Too many create or update operations in progress for subscription "%ld"."
                case 49920: //"Cannot process request. Too many operations in progress for subscription "%ld"."
                    return true;
            }

            // The following was observed in errors/email#55e7dc27 with Class=11, Number=0, State=0, LineNumber=0
            //                     and in Archidata/email#5613bc16 with Class=21, Number=9001, State=3, LineNumber=16
            return System.Text.RegularExpressions.Regex.IsMatch(p_e.Message ?? "",
                @"A severe error occurred on the current command.*The results, if any, should be discarded");
        }

        /// <summary> p_tryIdx: 0 is the first try </summary>
        public static bool IsNonSqlExceptionToRetry(Exception p_e, int p_tryIdx)
        {
            if (p_e == null)
                return false;

            // Sometimes we parallelize queries using System.Threading.Tasks
            // (e.g. to overcome the per-connection bandwidth limitation of SqlAzure)
            // In this case retriable SqlExceptions get wrapped into AggregateException
            var a = p_e as AggregateException;
            if (a != null && a.InnerExceptions.Count ==
                a.InnerExceptions.OfType<SqlException>().Count(sqlE => IsSqlExceptionToRetry(sqlE, p_tryIdx)))
                return true;

            //InvalidOperationException: "Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached."
            //InvalidOperationException: "ExecuteReader requires an open and available Connection. The connection's current state is closed."  errors/email#562fee20
            if (p_e is InvalidOperationException)
            {
#if DNX451 || NET451
                string s = p_e.TargetSite.DeclaringType.Name + "." + p_e.TargetSite.Name;
                if (s == "DbConnectionFactory.TryGetConnection" //|| s == "SqlCommand.ValidateCommand"
                    || 0 <= p_e.Message.IndexOf("The connection's current state is ", StringComparison.OrdinalIgnoreCase))
                    return true;
#else
                return true;    // decide that we do the Retry()
#endif     
            }

            //Microsoft.SqlServer.Management.Common.ConnectionFailureException with InnerException=SqlException. See note#110620.1/2013-03-11
            if (p_e.GetType().Name == "ConnectionFailureException"      // comes from Microsoft.SqlServer.ConnectionInfo.dll
                && IsSqlExceptionToRetry(p_e.InnerException as SqlException, p_tryIdx))
                return true;

            //Custom case: we caught an exception that shouldn't be retried in general, but now it should
            if (p_e is HQRetryEx && p_tryIdx < ((HQRetryEx)p_e).NTimes)
                return true;

            return false;
        }
    }

    public class HQRetryEx : Exception
    {
        public int NTimes;

        public HQRetryEx(string p_message, Exception p_innerException = null)
            : this(p_innerException is HQRetryEx ? ((HQRetryEx)p_innerException).NTimes : int.MaxValue, p_innerException ?? new Exception(p_message))
        { }
        public HQRetryEx(int p_nTimes = int.MaxValue, Exception p_innerException = null)
            : base("This situation should be retried " + (p_nTimes == int.MaxValue ? null : p_nTimes + " times"), p_innerException)
        {
            NTimes = p_nTimes;
        }
    }


#region Accessing configuration
    /// <summary> Defines .exe.config setting names for HQCommonLite in accordance with HQCommon
    /// to minimize configuration differences between HQCommonLite- and HQCommon-based programs.
    /// HQCommon-based programs should use this enum for these names to ensure that these are up-to-date
    /// (central definition, avoid further duplication).
    ///
    /// Only those names have to be included here that are/may be used in HQCommonLite
    /// (but it may be comfortable to include all settings for which HQCommon provides
    /// a factory default value with sensitive data).
    ///
    /// Most of these settings have no "factory default" value in HQCommonLite, but have one in HQCommon.
    /// These default values are served when the .exe.config-accessing API of HQCommon is used: Utils.ExeConfig[].
    /// This works in HQCommonLite, too, and is therefore recommended, in order to foster interchangeability of
    /// source code between the two projects.
    /// The ExeCfgSettings.Read() extension method is a syntactic sugar to Utils.ExeConfig[enumMember.ToString()].
    /// </summary>
    public enum ExeCfgSettings
    {
        EmailAddressCharmat,
        EmailAddressJeanCharmat,
        EmailAddressCharmat2,
        EmailAddressGyantal,
        EmailAddressRobin,
        EmailAddressLNemeth,
        EmailAddressBLukucz,
        EmailSenderForUsers,
        SmsNumberGyantalHU,
        SmsNumberGyantalUK,
        SmsNumberCharmat,
        SmsNumberLNemeth,
        SmsNumberBLukucz,
        SmsNumberRobin,
        SmsAuthUname,
        SmsAuthPwd,
        TwilioSid,
        TwilioToken,
        ServerHedgeQuantConnectionString,
        /// <summary> In case of HQCommon it can also be set as ‹add key="HQEmailSettings" value="UserName=..."/› </summary>
        SmtpUsername,
        /// <summary> In case of HQCommon it can also be set as ‹add key="HQEmailSettings" value="Password=..."/› </summary>
        SmtpPassword,
        SqlNTryDefault,
        SqlTimeoutShort,
        SqlTimeoutLong
    }
    

    public static partial class DbUtils
    {
        /// <summary> Equivalent to Utils.ExeConfig[p_enumMember.ToString()].ToString() + handling of nulls. Numeric values produce null result. </summary>
        public static string Read2(this DbCommon.ExeCfgSettings p_enumMember)
        {
            string key = p_enumMember.ToString(); object val = null;
            if (!String.IsNullOrEmpty(key) && 'A' <= key[0])    // valid enum value
                val = DbUtils.ExeConfig[key];
            return (val != null) ? val.ToString() : null;
        }
    }

#endregion

#region Miscellaneous utility methods

    public static class UtilsL
    {
        public static void LogError(string p_msg) {
            //g_LogFn(TraceLevel.Error, p_msg);
        }
        public static void LogWarning(string p_msg) {
            //g_LogFn(TraceLevel.Warning, p_msg);
        }
        //public static void DefaultLogger(TraceLevel p_level, string p_msg) {
            //Trace.WriteLine(p_msg);
        //}
        //public static Action<TraceLevel, string> g_LogFn = DbCommon.Utils.HQCommonSpecific<Action<TraceLevel, string>>("GetLogger4HQCommonLite") ?? DefaultLogger;

        // syntactic sugar for source codes that "using HQCommonLite" only
        /// <summary> Equivalent to Utils.ExeConfig[p_enumMember.ToString()].ToString() + handling of nulls. Numeric values produce null result. </summary>
        public static string Read(this ExeCfgSettings p_enumMember) { return DbCommon.DbUtils.Read2(p_enumMember); }
    }


    public static partial class DbUtils
    {
        public static string ToStringOrNull(this object o)
        {
            return o == null ? null : o.ToString();
        }
        public static string ToStringWithoutStackTrace(this Exception e)
        {
            string s = (e == null ? null : e.ToString()) ?? String.Empty;
            return s.Substring(0, Math.Min(s.Length, s.IndexOf("\n   at ") & int.MaxValue));
        }
        /// <summary> p_whatIsOceLike bits: 1=OperationCanceledException, 2=TaskCanceledException, 4=ThreadAbortException.<para>
        /// Dives into AggregateException. For any e Exception: IsOceLike(e.InnerException) -> IsOceLike(e)</para></summary>
        public static bool IsOceLike(Exception e, int p_whatIsOceLike = 3)
        {
            if (e == null) return false;
            if ((p_whatIsOceLike & 1) != 0 && (e is OperationCanceledException)) return true;
            if ((p_whatIsOceLike & 2) != 0 && (e is System.Threading.Tasks.TaskCanceledException)) return true;
#if DNX451 || NET451
            if ((p_whatIsOceLike & 4) != 0 && (e is System.Threading.ThreadAbortException)) return true;
#else
#endif
            var ag = (e as AggregateException); int n = 0;
            var ies = (ag != null) ? ag.InnerExceptions
                : new Exception[] { e.InnerException } as System.Collections.Generic.IEnumerable<Exception>;
            foreach (Exception ie in ies)
                if (ie == null) continue;
                else if (IsOceLike(ie)) ++n;
                else return false;
            return 0 < n;   // n==0: itself is not oce-like, and has no innerException
        }
    }

#endregion
}