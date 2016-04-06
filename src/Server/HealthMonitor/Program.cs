using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqCommon;
using System.Reflection;
using System.IO;
using System.Threading;

namespace HealthMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var y = typeof(Program).GetTypeInfo().Assembly;
            // 		(only in Dnx451, not DotNetCore) CodeBase	"file:///C:/Users/gyantal/.dnx/runtimes/dnx-clr-win-x64.1.0.0-rc2-16357/bin/Microsoft.Dnx.Loader.dll"	string
            //AssemblyLoadContext.GetAssemblyName.GetLoadContext.args;
            //var x = typeof(Program).GetType(); //.GetTypeInfo().Assembly;  // Using type.GetTypeInfo() helps with this issue for obtaining an Assembly
            //Console.WriteLine(y.GetManifestResourceInfo.);
            // or thin under Webserver: var libs = PlatformServices.Default.LibraryManager.GetReferencingLibraries("myLib")
            //.SelectMany(info => info.Assemblies)
            //.Select(info => Assembly.Load(new AssemblyName(info.Name)));

            string runtimeConfig = "Unknown";
#if RELEASE
            runtimeConfig = "RELEASE";
# elif DEBUG
            runtimeConfig = "DEBUG";
#endif
            Console.WriteLine($"Hello HealthMonitor, v1.0.13 ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");
#if DNX451 || NET451
            Console.Title = "HealthMonitor v1.0.13";   // Exception in DotNetCore in Win (but it runs on Linux, but it doesn't do anything): Unhandled Exception: System.MissingMethodException: Method not found: 'Void System.Console.set_Title(System.String)'.
#endif
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            Utils.Configuration = Utils.InitConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.HealthMonitor.NoGitHub.json", "/home/ubuntu/SQ/Server/HealthMonitor/SQLab.HealthMonitor.NoGitHub.json");
            StrongAssert.g_strongAssertEvent += StrongAssertEmailSendingEventHandler;

            HealthMonitor.g_healthMonitor.Init();

            string userInput = String.Empty;
            do
            {

                userInput = DisplayMenu();
                switch (userInput)
                {
                    case "1":
                        Console.WriteLine("Hello. I am not crashed yet! :)");
                        Utils.Logger.Info("Hello. I am not crashed yet! :)");
                        break;
                    case "2":
                        HealthMonitor.g_healthMonitor.CheckAmazonAwsInstances_Elapsed(null);
                        break;
                    case "3":
                        Console.WriteLine(HealthMonitor.g_healthMonitor.DailySummaryReport(false).ToString());
                        break;
                    case "4":
                        HealthMonitor.g_healthMonitor.DailyReportTimer_Elapsed(null);
                        Console.WriteLine("DailyReport email was sent.");
                        break;
                }

            } while (userInput != "5" && userInput != "ConsoleIsForcedToShutDown");

            Utils.Logger.Info("****** Main() END");
            HealthMonitor.g_healthMonitor.Exit();
            Utils.Logger.Exit();
        }


        static DateTime gLastStrongAssertEmailTime = DateTime.MinValue;
        internal static void StrongAssertEmailSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            if ((DateTime.UtcNow - gLastStrongAssertEmailTime).TotalMinutes > 30)   // don't send it in every minute, just after 30 minutes
            {
                new Email
                {
                    ToAddresses = Utils.Configuration["EmailGyantal"],
                    Subject = "SQ HealthMonitor: StrongAssert failed.",
                    Body = "SQ HealthMonitor: StrongAssert failed. " + p_msg.Message + "/" + p_msg.StackTrace,
                    IsBodyHtml = false
                }.Send();
                gLastStrongAssertEmailTime = DateTime.UtcNow;
            }
        }




        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;

            ConsoleColor previousForeColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----HealthMonitor Server    (type and press Enter)----");
            Console.ForegroundColor = previousForeColour;
            Console.WriteLine("1. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("2. Test AmazonAWS API:DescribeInstances()");
            Console.WriteLine("3. VirtualBroker Report: show on Console.");
            Console.WriteLine("4. VirtualBroker Report: send Html email.");
            Console.WriteLine("5. Exit gracefully (Avoid Ctrl-^C).");
            string result = null;
            try
            {
                result = Console.ReadLine();
            }
            catch (System.IO.IOException e) // on Linux, of somebody closes the Terminal Window, Console.Readline() will throw an Exception with Message "Input/output error"
            {
                Utils.Logger.Info($"Console.ReadLine() exception. Somebody closes the Terminal Window: {e.Message}");
                return "ConsoleIsForcedToShutDown";
            }
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
