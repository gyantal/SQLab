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
            Console.Title = "HealthMonitor v1.0.13";
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            Utils.Configuration = Utils.InitConfigurationAndInitUtils((Utils.RunningPlatform() == Platform.Windows) ? "g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.HealthMonitor.NoGitHub.json" : "/home/ubuntu/SQ/Server/HealthMonitor/SQLab.HealthMonitor.NoGitHub.json");
            Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
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
                        TestPhoneCall();
                        break;
                    case "3":
                        HealthMonitor.g_healthMonitor.CheckAmazonAwsInstances_Elapsed("ConsoleMenu");
                        break;
                    case "4":
                        Console.WriteLine(HealthMonitor.g_healthMonitor.DailySummaryReport(false).ToString());
                        break;
                    case "5":
                        HealthMonitor.g_healthMonitor.DailyReportTimer_Elapsed(null);
                        Console.WriteLine("DailyReport email was sent.");
                        break;
                }

            } while (userInput != "6" && userInput != "ConsoleIsForcedToShutDown");

            Utils.Logger.Info("****** Main() END");
            Utils.MainThreadIsExiting.Set();
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

            Utils.ConsoleWriteLine(ConsoleColor.Magenta, "----HealthMonitor Server    (type and press Enter)----");      
            Console.WriteLine("1. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("2. Test Twilio phone call service.");
            Console.WriteLine("3. Test AmazonAWS API:DescribeInstances()");
            Console.WriteLine("4. VirtualBroker Report: show on Console.");
            Console.WriteLine("5. VirtualBroker Report: send Html email.");
            Console.WriteLine("6. Exit gracefully (Avoid Ctrl-^C).");
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

        static public void TestPhoneCall()
        {
            Console.WriteLine("Calling phone number via Twilio. It should ring out.");
            Utils.Logger.Info("Calling phone number via Twilio. It should ring out.");

            try
            {
                var call = new PhoneCall
                {
                    FromNumber = Caller.Gyantal,
                    ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                    Message = "This is a test phone call from Health Monitor.",
                    NRepeatAll = 2
                };
                // skipped temporarily
                bool didTwilioAcceptedTheCommand = call.MakeTheCall();
                if (didTwilioAcceptedTheCommand)
                {
                    Utils.Logger.Debug("PhoneCall instruction was sent to Twilio.");
                }
                else
                    Utils.Logger.Error("PhoneCall instruction was NOT accepted by Twilio.");
            } catch (Exception e)
            {
                Utils.Logger.Error(e, "Exception in TestPhoneCall().");
            }
        }
    }
}
