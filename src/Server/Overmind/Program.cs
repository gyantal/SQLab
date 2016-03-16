using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Xml;
using SqCommon;

namespace Overmind
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

            Console.WriteLine("Hello Overmind, v1.0.7");
            Console.Title = "Overmind v1.0.7";
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info("****** Main() START");
            Utils.Configuration = Utils.InitConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.Overmind.NoGitHub.json", "/home/ubuntu/SQ/Server/Overmind/SQLab.Overmind.NoGitHub.json");
            //StrongAssert.g_strongAssertEvent += StrongAssertEmailSendingEventHandler;     // HealthMonitor StrongAssert should send an email, but all other programs should inform HealthMonitor instead. HealthMonitor is the gatekeeper that assures that users don't receive too many emails.

            //    // on Windows the NLog.Config near the project.json is found by Nlog, but on Linux, there is problem
            //    //var nLogConfigPath = NLog.LogFactory.CurrentAppDomain.BaseDirectory + "NLog.config";
            //    //Console.WriteLine("NLog file: " + nLogConfigPath);

            /* NLog. Solution 1:  (doesn't yet work on DotNetCore runtime, only DNX451/maybe Mono runtime) */
            //Utils.Logger = LogManager.GetCurrentClassLogger();   //https://github.com/nlog/NLog/wiki/Configuration-file

            /* NLog. Solution 2: (doesn't yet work on DotNetCore runtime, only DNX451/maybe Mono runtime)
           // 2016-02-09: even on Windows, NLog doesn't doesn't do log file with the DotNetCore runtime, only with the DNX451 runtime
           // it is probably best to wait until Nlog team fixes the problems and release new nuget package
           // https://github.com/NLog/NLog/issues/641
           //FileStream file = File.OpenRead("NLog.config");
           //var reader = XmlReader.Create(file); //stream preferred above byte[] / string.
           //var configNlog = new XmlLoggingConfiguration(reader, null); //filename is not required.
           //LogManager.Configuration = configNlog;
           //Utils.Logger = LogManager.GetCurrentClassLogger();   //https://github.com/nlog/NLog/wiki/Configuration-file
           */

            /* NLog. Solution 3: (doesn't yet work on DotNetCore runtime, only DNX451/maybe Mono runtime)
            // 2016-02-09: even on Windows, NLog doesn't doesn't do log file with the DotNetCore runtime, only with the DNX451 runtime
            // it is probably best to wait until Nlog team fixes the problems and release new nuget package
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);
            // Step 3. Set target properties 
            //fileTarget.FileName = "${basedir}/nLogOvermind${shortdate}.log";
            fileTarget.FileName = "g:/temp/nLogOvermind.log";
            //fileTarget.FileName = "logs/nLogOvermind${shortdate}.log";
            fileTarget.Layout = "${longdate}|${level:uppercase=true}|${logger}|${event-context:item=EventId}|${message}|${ndc}";
            // Step 4. Define rules
            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);
            // Step 5. Activate the configuration
            LogManager.Configuration = config;
            // Example usage
            Utils.Logger = LogManager.GetLogger("Program");
            */


            Controller.g_controller.Start();

            string userInput = String.Empty;
            do
            {

                userInput = DisplayMenu();
                switch (userInput)
                {
                    case "1":
                        Controller.g_controller.TestSendingEmailAndPhoneCall();
                        break;
                    case "2":
                        //new HQEmail().SendOnMono(true);
                        Console.WriteLine("Hello. I am not crashed yet! :)");
                        Utils.Logger.Info("Hello. I am not crashed yet! :)");
                        break;
                }

            } while (userInput != "3");

            Utils.Logger.Info("****** Main() END");
            Controller.g_controller.Exit();
            Utils.Logger.Exit();

            //nlogLogFactory.Flush();
            //var resetEvent = new ManualResetEventSlim(false);
            //LogManager.Flush(ex => resetEvent.Set(), TimeSpan.FromSeconds(15));
            //resetEvent.Wait(TimeSpan.FromSeconds(15));
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
            Console.WriteLine("----Overmind Server    (type and press Enter)----");
            Console.ForegroundColor = previousForeColour;
            Console.WriteLine("1. Test Server (Sending Email)");
            Console.WriteLine("2. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("3. Exit gracefully (Avoid Ctrl-^C).");
            var result = Console.ReadLine();
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
