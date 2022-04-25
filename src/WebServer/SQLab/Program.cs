using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using SqCommon;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace SQLab
{
    public enum RunningEnvStrType
    {
        Unknown,
        NonCommitedSensitiveDataFullPath,
        HttpsCertificateFullPath,
        DontPublishToPublicWwwroot,
        SQLabFolder
    }

    public interface IWebAppGlobals
    {
        DateTime WebAppStartTime { get; set; }

        Queue<HttpRequestLog> HttpRequestLogs { get; set; }     // Fast Insert, limited size. Better that List
    }

    public class WebAppGlobals : IWebAppGlobals
    {
        DateTime m_webAppStartTime = DateTime.UtcNow;
        DateTime IWebAppGlobals.WebAppStartTime { get => m_webAppStartTime; set => m_webAppStartTime = value; }

        Queue<HttpRequestLog> m_httpRequestLogs = new Queue<HttpRequestLog>();
        Queue<HttpRequestLog> IWebAppGlobals.HttpRequestLogs { get => m_httpRequestLogs; set => m_httpRequestLogs = value; }
    }


    public class Program
    {
        public static IWebAppGlobals g_webAppGlobals { get; set; }

        public static void Main(string[] args)
        {
            string runtimeConfig = "Unknown";
#if RELEASE
            runtimeConfig = "RELEASE";
# elif DEBUG
            runtimeConfig = "DEBUG";
#endif
            Console.WriteLine($"Hello SQLab WebServer, v1.0.13 ({runtimeConfig}, dotnet {Utils.GetNetCoreVersion()},  ThId-{Thread.CurrentThread.ManagedThreadId})");
            Console.Title = "SQLab WebServer v1.0.13";
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))    // will be "Client.SQLab.log"
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            // After Configuring logging, set-up other things
            Utils.Configuration = Utils.InitConfigurationAndInitUtils(RunningEnvStr(RunningEnvStrType.NonCommitedSensitiveDataFullPath));
            Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
            HealthMonitorMessage.InitGlobals(ServerIp.HealthMonitorPublicIp, HealthMonitorMessage.DefaultHealthMonitorServerPort);       // until HealthMonitor runs on the same Server, "localhost" is OK
            StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.

            g_webAppGlobals = new WebAppGlobals();
            try
            {
                BuildWebHost(args).Run();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendAsync($"Exception in Website.C#.MainThread. Exception: '{ e.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromSQLabWebsite).TurnAsyncToSyncTask();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                // for changing the default port 5000 ("UPDATED 2: ASP.NET Core RC2 changes"): http://stackoverflow.com/questions/34212765/how-do-i-get-the-kestrel-web-server-to-listen-to-non-localhost-requests
                // effect of adding .UseUrls("http://*:80",)
                // Windows: Firewall will pop up. Allow rule is required.
                // Linux: exception is thrown at Kestrel start
                //      Unhandled Exception: System.AggregateException: One or more errors occurred. (Error - 13 EACCES permission denied)--->Microsoft.AspNetCore.Server.Kestrel.Networking.UvException: Error - 13 EACCES permission denied
                //      at Microsoft.AspNetCore.Server.Kestrel.Networking.Libuv.Check(Int32 statusCode)
                //      at Microsoft.AspNetCore.Server.Kestrel.Networking.Libuv.tcp_bind(UvTcpHandle handle, SockAddr & addr, Int32 flags)
                // https://github.com/aspnet/Home/issues/311
                // A normal user is not allowed to bind sockets to TCP ports < 1024, you need more permissions than the normal user has (read: 'root'-access). The easiest and quickest solution probably is using sudo to start the process, but that's also the most frowned upon solution. The reason is that the process then has every access right on the system.
                // The cleanest solution and the one that for which Kestrel is developed is using a front-end server / load balancer on port 80(for example nginx, or endpoints in Azure) that forwards the requests to an unprivileged Kestrel on a private port.
                // Because of Google Auth cookie usage and Chrome SameSite policy, use only the HTTPS protocol (no HTTP), even in local development. See explanation at Google Auth code.
                // However, because of AWS CloudFront, we cannot do that. CloudFront cannot do HTTPS to HTTPS translation, because it doesn't accept our Dev-Certificate (it would work with a proper SSL certificate)
                // So, we have to use HTTP behind CloudFront. We did set CookiePolicy to Lax (instead of None). If it is None, HTTP server doesn't send Secure attribute.
                // But because it is Lax, HTTP server doesn't send Secure attribute, but - good news - it is not required. Lax doens't require Secure in Chrome.
                // Ultimately we merge these tools from SqLab to SqCore, where we own the SSL cert and CloudFront is not required.
                // Until that, we have to use HTTP.
                .UseUrls("http://*:80", "https://*:443", "https://localhost:5000")        // default only: "http://localhost:5000",  adding "http://*:80" will listen to clients from any IP4 or IP6 address, so Windows Firewall will prompt                 
                .UseKestrel(options =>
                {
                    // 0. HTTPS preferable over HTTP on main webApps.
                    //But there are valid cases when HTTP should be allowed, and faster. When a trusted, local other server (HealthMonitor on a specific IP) asks something, there is no point of encrypting it.
                    //Or for specific Data-urls could be faster HTTP, not HTTPS. The main pages (like https://localhost/HealthMonitor), which only asked once should be HTTPS.

                    //1. SSL certificate was best obtained by Amazon Certification Manager. Currently (2016-06-03, 2018-01) that Cert can only be used with CloudFront. *.pfx cannot be obtained.
                    //Configure SSL is not necessary now as CloudFront will route all HTTPS request to the HTTP port of the EC2 instance. So, EC2 instance only supports HTTP, not HTTPS (right now).
                    //also an advantage is that Cloudfront computation is used, because the Linux EC2 instance doesn't have to use CPU for encryption. It is good for scalability.
                    //However, there are problems with AWS CloudFrount. See later. (Converts Post message to Get, and losing Post data + after GoogleAuth it loses HTTPS and reverts back to HTTP). See later in these notes.

                    //2. I had to support HTTPS on the Kestrel server.
                    //Because if only HTTP is allowed, Cloudfront will transfer HTTPS to HTTP to the server.At that moment,
                    //Kestrel server thinks it is only HTTP. (not HTTPS), so when the query of
                    //https://www.snifferquant.net will be not received by Kestrel server, only http://www.snifferquant.net is received,
                    //the Kestrel servel will redirect to the HTTP one http://www.snifferquant.net/login?ReturnUrl=%2F,
                    //not the HTTPS one. After that Chrome browser loses the HTTPS one, and Cloudfront will Error, (BAD request), beacuse
                    //it receives a HTTP.../ login ? request, when HTTP is not allowed.
                    // > Solution: Kestrel supports HTTPS with a test certification.More CPU work, but whatever.
                    //Good side of it: the direct version direct.snifferquant.net can test both http and https. Good for debugging.
                    //>as a side effect Cloudfront now puts a Request header: CloudFrontSQNet = True into the request.So, later I can recognize it.

                    // 3. We had to go back to HTTP Only. Conclusion Origin Protocol Policy: HTTP Only  (otherwise, CloudFront will drop the connection if HTTPS certification of Origin is not proper)
                    //"If you're using an HTTP server as your origin, and if you want to use HTTPS both between viewers and CloudFront and between CloudFront and your origin, you must install an SSL/TLS certificate on the HTTP server that is signed by a trusted certificate authority, for example, Comodo, DigiCert, or Symantec. If your origin is an Elastic Load Balancing load balancer, you can use an SSL/TLS certificate from the Amazon Trust Services certificate authority (via AWS Certificate Manager).
                    //Caution:  If the origin server returns an expired certificate, an invalid certificate or a self-signed certificate, or if the origin server returns the certificate chain in the wrong order, CloudFront drops the TCP connection, returns HTTP error code 502, and sets the X-Cache header to Error from cloudfront."
                    //However, I keep the HTTPS code here, because //Good side of it: the direct version direct.snifferquant.net can test both http and https. Good for debugging.
                    // Anyway. Later Amazon will support its own Certificate here (not only Comodo, DigiCert, etc.), so a temporary fix now is OK.

                    // 4. trying CloudFront "Redirect HTTP to HTTPS" instead of allowing both: /login redirection works as it is intended.
                    //Kestrel redirects browser to HTTP / login, but CloudFront when receives http://*login, its response says it was https://login,
                    //so it properly Redirects. 
                    // So, final good solution: communication to Origin: HTTP Only ; Communication with Browsers: Redirect HTTP to HTTPS

                    // 5. (2017-10) "Redirect HTTP to HTTPS" of CloudFront changes HTTPS POST to GET, ruining it, and even removing the data package of the POST.
                    // see ReportHealthMonitorCurrentStateToDashboardInJSON()
                    //Bad.So, solution is that I have to disable HTTP to HTTPS redirection and allow CloudFront both HTTP and HTTPS traffic flowing to OriginServer,
                    //and IF I don't want HTTP traffic (e.g. HealthMonitor main page), I should redirect it locally, on the Kestrel server.
                    //However, it is not an important development, so keep both HTTP and HTTPS for now.

                    // 6. (2018-01-09): Amazon issued SSL certificates cannot be downloaded yet.
                    // Problem: Amazon CloudFront has the trusted Amazon issued 'SSL Certificate' for https, so the Kestrel webserver doesn't have that SSL certificate. It only has its own local untrusted cert.
                    // "I want to download SSL that means I want to download its private key so that I can deploy aws acm ssl to any server I want. I think that it's not possible to do it in anyway."
                    // Q: Can I use certificates on Amazon EC2 instances or on my own servers?
                    // A: No.At this time, certificates provided by ACM can only be used with specific AWS services" on Aug 28 2017

                    // 7. (2018-01-09): There are 2 ways the user can login with Google Auth.
                    //1.
                    //AccountController.cs   Login() method as forced login/logout
                    //http://localhost/login    this logs in Google and go to HTTP://localhost/# 
                    //https://localhost/login  this logs in Google and go to HTTPS://localhost/#  (so, the HTTPS kind of login goes nicely to HTTPS) fine.
                    //2.A.
                    //RootHomepageRedirectController.cs  [Authorize] attribute will force it. However, it will not visit AccountController.cs code, but it will run Startup.cs/OnCreatingTicket code 
                    //https://direct.snifferquant.net  // Works, Redirects back to HTTPS, after login with Google.
                    //2.B
                    //https://www.snifferquant.net/	//This doesn't work. After logging in by Google it goes to HTTP, not HTTPS. It may be because Amazon Cloudfront, because
                    //So, without Cloudfront, the https://direct.snifferquant.net  HTTPS to HTTPS redirection works fine, but with Amazon CloudFront, it is not.
                    //It will be solved in the future when we can download Amazon issued HTTPS certificate locally and use in in Kestrel. On 2018-01, it is still not available.

                    // 8. [RequireHttps] attribute. After setting it up,
                    // https://localhost/HealthMonitor works, but http://localhost/HealthMonitor redirects 'automatically' (no error page) to the https://localhost/HealthMonitor . It is perfect locally.
                    // However, again, Aws CloudFront ruins it. It works with with Localhost and with https://direct.snifferquant.net/HealthMonitor , 
                    // but with https://www.snifferquant.net/HealthMonitor which goes through AWS Cloudfront, it is 'redirected you too many times.'
                    // So, temporary, until We can use our own HTTPS SSL certificate, we are switching off this feature.

                    options.Listen(IPAddress.Any, 80);
                    options.Listen(IPAddress.Any, 443, listenOptions =>
                    {
                        var serverCertificate = LoadHttpsCertificate();
                        listenOptions.UseHttps(serverCertificate);
                    });
                    options.Listen(IPAddress.Any, 5000, listenOptions =>
                    {
                        var serverCertificate = LoadHttpsCertificate();
                        listenOptions.UseHttps(serverCertificate);
                    });
                })
#if RELEASE
                .UseEnvironment("Production")  // This is better way then setting up the machine wide global ASPNETCORE_ENVIRONMENT env.Variable. User env. variable doesn't work. https://dotnetcoretutorials.com/2017/05/03/environments-asp-net-core/
#elif DEBUG
                .UseEnvironment("Development")  // This is better way then setting up the machine wide global ASPNETCORE_ENVIRONMENT env.Variable. User env. variable doesn't work. https://dotnetcoretutorials.com/2017/05/03/environments-asp-net-core/
#endif
                .UseContentRoot(Directory.GetCurrentDirectory())
                //.UseIISIntegration() onfigures the port and base path the server should listen on when running behind AspNetCoreModule. The app will also be configured to capture startup errors.
                .UseStartup<Startup>()
                .Build();

        // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            //1. Any wrong http request that is quickly aborted later will raise UnobservedTaskException() at the next GC.Collect():
            //"TaskScheduler_UnobservedTaskException. Exception: 'System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (The request was aborted) ---> System.Threading.Tasks.TaskCanceledException: The request was aborted
            //   --- End of inner exception stack trace ---
            //---> (Inner Exception #0) System.Threading.Tasks.TaskCanceledException: The request was aborted<---
            //"
            //For example: http://www.snifferquant.net/aaaa  (however, I cannot reproduce with this, because I don't abort the request in my browser. However, these guys ask a fake query, and they quickly abort the request, so they can do 50 queries per second very quickly.)
            //0315T21:27:14.379#3#Info: Request starting HTTP/1.1 GET http://www.snifferquant.net/aaaa    
            //0315T21:27:14.379#3#Info: Request finished in 0.2086ms 404 
            //And there are many naughty guys, who just simply try random queries for vulnerability seek. 
            //Is 3 seconds I quickly had 50 of these (that were aborted quickly too) like:
            //0315T21:27:14.119#9#Info: Request starting HTTP/1.1 GET http://23.20.243.199/w00tw00t.at.blackhats.romanian.anti-sec:)  
            //0315T21:27:14.126#9#Info: Request finished in 6.942ms 404 
            //0315T21:27:14.379#3#Info: Request starting HTTP/1.1 GET http://23.20.243.199/scripts/setup.php  
            //0315T21:27:14.379#3#Info: Request finished in 0.2086ms 404 
            //>see: my custom code hasn't been even invoked. Kestrel returns 404 not found, and my code has no chance to do anything at all.
            //>Solution: It is inevitable that crooks tries this query-and-abort and Kestrel is written badly that it doesn't handle Abortion properly.
            //So, in UnobservedTaskException() filters these aborts and don't send it to HealthMonitor.
            Utils.Logger.Info($"TaskScheduler_UnobservedTaskException() START");
            if (SqFirewallMiddleware.IsSendableToHealthMonitorForEmailing(e.Exception))
                HealthMonitorMessage.SendAsync($"Exception in Website.C# in TaskScheduler_UnobservedTaskException(). Exception: '{ e.Exception.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromSQLabWebsite).TurnAsyncToSyncTask();

            Utils.Logger.Info($"TaskScheduler_UnobservedTaskException() Calling e.SetObserved()");
            e.SetObserved();        //  preventing it from triggering exception escalation policy which, by default, terminates the process.
            Utils.Logger.Info($"TaskScheduler_UnobservedTaskException() Called e.SetObserved()");

            Task senderTask = (Task)sender;
            if (senderTask != null)
            {
                Utils.Logger.Info($"TaskScheduler_UnobservedTaskException(): sender is a task. TaskId: {senderTask.Id}, IsCompleted: {senderTask.IsCompleted}, IsCanceled: {senderTask.IsCanceled}, IsFaulted: {senderTask.IsFaulted}, TaskToString(): {senderTask.ToString()}");
                if (senderTask.Exception == null)
                    Utils.Logger.Info("SenderTask.Exception is null");
                else
                    Utils.Logger.Info($"SenderTask.Exception {senderTask.Exception.ToString()}");
            }
            else
                Utils.Logger.Info("TaskScheduler_UnobservedTaskException(): sender is not a task.");

            Utils.Logger.Info($"TaskScheduler_UnobservedTaskException() END");
        }

        internal static void StrongAssertMessageSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            HealthMonitorMessage.SendAsync($"Msg from Website.C#.StrongAssert. StrongAssert Warning (if Severity is NoException, it is just a mild Warning. If Severity is ThrowException, that exception triggers a separate message to HealthMonitor as an Error). Severity: {p_msg.Severity}, Message: { p_msg.Message}, StackTrace: { p_msg.StackTrace}", HealthMonitorMessageID.ReportErrorFromSQLabWebsite).FireParallelAndForgetAndLogErrorTask();
        }


        static Dictionary<RunningEnvStrType, Dictionary<RunningEnvironment, string>> RunningEnvStrDict = new Dictionary<RunningEnvStrType, Dictionary<RunningEnvironment, string>>()
        {
             { RunningEnvStrType.NonCommitedSensitiveDataFullPath,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, "/home/ubuntu/SQ/WebServer/SQLab/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsAGy, "c:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsDaya_laptop, "c:/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_desktop, "d:/GDrive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_laptop, "d:/GDrive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" }
                }
            },
            { RunningEnvStrType.HttpsCertificateFullPath,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, "/home/ubuntu/SQ/WebServer/SQLab/snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsAGy, @"c:\agy\GitHub\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsDaya_laptop, @"c:\Google Drive\GDriveHedgeQuant\shared\GitHubRepos\NonCommitedSensitiveData\cert\AwsVbDev\snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsBL_desktop, @"d:\SVN\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\SVN\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" }
                }
            },
            { RunningEnvStrType.DontPublishToPublicWwwroot,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, $"/home/ubuntu/SQ/WebServer/SQLab/src/WebServer/SQLab/noPublishTo_wwwroot/" },
                    { RunningEnvironment.WindowsAGy, @"c:\agy\GitHub\SQLab\src\WebServer\SQLab\noPublishTo_wwwroot\" },   // TEMPORARY
                    { RunningEnvironment.WindowsDaya_laptop, @"c:\GitHubRepos\SQLab\src\WebServer\SQLab\noPublishTo_wwwroot\" },   // TEMPORARY
                    { RunningEnvironment.WindowsBL_desktop, @"d:\GitHub\SQLab\src\WebServer\SQLab\noPublishTo_wwwroot\" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\GitHub\SQLab\src\WebServer\SQLab\noPublishTo_wwwroot\" }
                }
            },
            { RunningEnvStrType.SQLabFolder,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, $"/home/ubuntu/SQ/WebServer/SQLab/src/WebServer/SQLab/" },
                    { RunningEnvironment.WindowsAGy, @"c:\agy\GitHub\SQLab\src\WebServer\SQLab\" },
                    { RunningEnvironment.WindowsDaya_laptop, @"c:\GitHubRepos\SQLab\src\WebServer\SQLab\" },
                    //{ RunningEnvironment.WindowsAGy, @"g:\work\Archi-data\GitHubRepos\SQLab\src\WebServer\SQLab\" },  // this will be the new after migration to NetCore2
                    { RunningEnvironment.WindowsBL_desktop, @"d:\GitHub\SQLab\src\WebServer\SQLab\" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\GitHub\SQLab\src\WebServer\SQLab\" }
                }
            }

        };
        public static string RunningEnvStr(RunningEnvStrType p_runningEnvStrType)
        {
            if (RunningEnvStrDict.TryGetValue(p_runningEnvStrType, out Dictionary<RunningEnvironment, string> dictRe))
            {
                if (dictRe.TryGetValue(Utils.RunningEnv(), out string str))
                {
                    return str;
                }
            }
            Utils.Logger.Error("Error in RunningEnvStr(). Couldn't find: " + p_runningEnvStrType + ". Returning null.");
            return null;
        }

        private static X509Certificate2 LoadHttpsCertificate()
        {
            //var socialSampleAssembly = typeof(Startup).GetTypeInfo().Assembly;
            //var embeddedFileProvider = new EmbeddedFileProvider(socialSampleAssembly, "SocialSample");
            //var certificateFileInfo = embeddedFileProvider.GetFileInfo("compiler/resources/cert.pfx");
            //using (var certificateStream = certificateFileInfo.CreateReadStream())

            string fullPath = RunningEnvStr(RunningEnvStrType.HttpsCertificateFullPath);
            using (var certificateStream = System.IO.File.OpenRead(fullPath))
            {
                byte[] certificatePayload;
                using (var memoryStream = new MemoryStream())
                {
                    certificateStream.CopyTo(memoryStream);
                    certificatePayload = memoryStream.ToArray();
                }

                string pfxPwd = Utils.Configuration["SSLTestCertificatePfxPwd"];
                return new X509Certificate2(certificatePayload, pfxPwd);
                //return new X509Certificate2(certificatePayload, "<Find correct password in 'snifferquant.net.pfx password.txt' locally>");
            }
        }
    }
}
