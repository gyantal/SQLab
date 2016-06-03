using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Security.Cryptography.X509Certificates;
using SqCommon;

namespace SQLab
{
    public class Program
    {
        public static void Main(string[] args)
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
                    //SSL certificate was best obtained by Amazon Certification Manager. Currently (2016-06-03) that Cert can only be used with CloudFront. *.pfx cannot be obtained.
                    //Configure SSL is not necessary now as CloudFront will route all HTTPS request to the HTTP port of the EC2 instance. So, EC2 instance only supports HTTP, not HTTPS (right now).
                    //also an advantage is that Cloudfront computation is used, because the Linux EC2 instance doesn't have to use CPU for encryption. It is good for scalability.
                    //var serverCertificate = LoadHttpsCertificate();
                    //options.UseHttps(serverCertificate);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }

        //private static X509Certificate2 LoadHttpsCertificate()
        //{
        //    //var socialSampleAssembly = typeof(Startup).GetTypeInfo().Assembly;
        //    //var embeddedFileProvider = new EmbeddedFileProvider(socialSampleAssembly, "SocialSample");
        //    //var certificateFileInfo = embeddedFileProvider.GetFileInfo("compiler/resources/cert.pfx");
        //    //using (var certificateStream = certificateFileInfo.CreateReadStream())

        //    string fullPath = (Utils.RunningPlatform() == Platform.Linux) ?
        //            "/home/ubuntu/SQ/Client/SQLab/snifferquant.net.pfx" :
        //            @"g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\certification\snifferquant.net.pfx";

        //    using (var certificateStream = System.IO.File.OpenRead(fullPath))
        //    {
        //        byte[] certificatePayload;
        //        using (var memoryStream = new MemoryStream())
        //        {
        //            certificateStream.CopyTo(memoryStream);
        //            certificatePayload = memoryStream.ToArray();
        //        }

        //        return new X509Certificate2(certificatePayload, "<Find correct password in 'snifferquant.net.pfx password.txt' locally>");
        //    }
        //}
    }
}
