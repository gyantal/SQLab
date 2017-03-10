using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using SqCommon;
using System.Threading;

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

    public class Program
    {
        public static void Main(string[] args)
        {
            string runtimeConfig = "Unknown";
#if RELEASE
            runtimeConfig = "RELEASE";
# elif DEBUG
            runtimeConfig = "DEBUG";
#endif
            Console.WriteLine($"Hello SQLab WebServer, v1.0.12 ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");
            Console.Title = "SQLab WebServer v1.0.12";
            //if (!Utils.InitDefaultLogger("Client." + typeof(Program).Namespace))    // will be "Client.SQLab.log"
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))    // will be "Client.SQLab.log"
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            // After Configuring logging, set-up other things
            Utils.Configuration = Utils.InitConfigurationAndInitUtils(RunningEnvStr(RunningEnvStrType.NonCommitedSensitiveDataFullPath));
            Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
            HealthMonitorMessage.InitGlobals(ServerIp.HealthMonitorPublicIp, HealthMonitorMessage.DefaultHealthMonitorServerPort);       // until HealthMonitor runs on the same Server, "localhost" is OK
            StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.

            try
            {
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
                var host = new WebHostBuilder()
                .UseUrls("http://*:80", "https://*:443", "http://localhost:5000")        // default only: "http://localhost:5000",  adding "http://*:80" will listen to clients from any IP4 or IP6 address, so Windows Firewall will prompt                 
                .UseKestrel(options =>
                {
                    //1. SSL certificate was best obtained by Amazon Certification Manager. Currently (2016-06-03) that Cert can only be used with CloudFront. *.pfx cannot be obtained.
                    //Configure SSL is not necessary now as CloudFront will route all HTTPS request to the HTTP port of the EC2 instance. So, EC2 instance only supports HTTP, not HTTPS (right now).
                    //also an advantage is that Cloudfront computation is used, because the Linux EC2 instance doesn't have to use CPU for encryption. It is good for scalability.

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

                    // 4. trying Redirect HTTP to HTTPS instead of allowing both: /login redirection works as it is intended.
                    //Kestrel redirects browser to HTTP / login, but CloudFront when receives http://*login, its response says it was https://login,
                    //so it properly Redirects. 
                    // So, final good solution: communication to Origin: HTTP Only ; Communication with Browsers: Redirect HTTP to HTTPS

                    var serverCertificate = LoadHttpsCertificate();
                    options.UseHttps(serverCertificate);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

                host.Run();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("Website.C#.MainThread", e, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);
            }
        }

        // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HealthMonitorMessage.SendException("Website.C#.TaskScheduler_UnobservedTaskException", e.Exception, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);
        }

        internal static void StrongAssertMessageSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            HealthMonitorMessage.SendStrongAssert("Website.C#.StrongAssert", p_msg, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);
        }


        static Dictionary<RunningEnvStrType, Dictionary<RunningEnvironment, string>> RunningEnvStrDict = new Dictionary<RunningEnvStrType, Dictionary<RunningEnvironment, string>>()
        {
             { RunningEnvStrType.NonCommitedSensitiveDataFullPath,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, "/home/ubuntu/SQ/Client/SQLab/SQLab.Client.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsAGy, "g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.Client.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_desktop, "h:/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.Client.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_laptop, "h:/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.Client.SQLab.NoGitHub.json" }
                }
            },
            { RunningEnvStrType.HttpsCertificateFullPath,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, "/home/ubuntu/SQ/Client/SQLab/snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsAGy, @"g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsBL_desktop, @"d:\SVN\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\SVN\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx" }
                }
            },
            { RunningEnvStrType.DontPublishToPublicWwwroot,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, $"/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/" },
                    { RunningEnvironment.WindowsAGy, @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\" },
                    { RunningEnvironment.WindowsBL_desktop, @"d:\GitHub\SQLab\src\Client\SQLab\noPublishTo_wwwroot\" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\GitHub\SQLab\src\Client\SQLab\noPublishTo_wwwroot\" }
                }
            },
            { RunningEnvStrType.SQLabFolder,
                new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, $"/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/" },
                    { RunningEnvironment.WindowsAGy, @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\" },
                    { RunningEnvironment.WindowsBL_desktop, @"d:\GitHub\SQLab\src\Client\SQLab\" },
                    { RunningEnvironment.WindowsBL_laptop, @"d:\GitHub\SQLab\src\Client\SQLab\" }
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
