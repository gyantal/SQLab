using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

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
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
