using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IO;
using System.Xml;

namespace Overmind
{
    public class Program
    {
        public static Logger gLogger = null;
        public static void Main(string[] args)
        {
            //    // on Windows the NLog.Config near the project.json is found by Nlog, but on Linux, there is problem
            //    //var nLogConfigPath = NLog.LogFactory.CurrentAppDomain.BaseDirectory + "NLog.config";
            //    //Console.WriteLine("NLog file: " + nLogConfigPath);

            /* NLog. Solution 1:  (doesn't yet work on DotNetCore runtime, only DNX451/maybe Mono runtime) */
            gLogger = LogManager.GetCurrentClassLogger();   //https://github.com/nlog/NLog/wiki/Configuration-file

            /* NLog. Solution 2: (doesn't yet work on DotNetCore runtime, only DNX451/maybe Mono runtime)
           // 2016-02-09: even on Windows, NLog doesn't doesn't do log file with the DotNetCore runtime, only with the DNX451 runtime
           // it is probably best to wait until Nlog team fixes the problems and release new nuget package
           // https://github.com/NLog/NLog/issues/641
           //FileStream file = File.OpenRead("NLog.config");
           //var reader = XmlReader.Create(file); //stream preferred above byte[] / string.
           //var configNlog = new XmlLoggingConfiguration(reader, null); //filename is not required.
           //LogManager.Configuration = configNlog;
           //gLogger = LogManager.GetCurrentClassLogger();   //https://github.com/nlog/NLog/wiki/Configuration-file
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
            gLogger = LogManager.GetLogger("Program");
            */


            gLogger.Info("****** Main() START");
            //NLog.LogFactory nlogLogFactory = new global::NLog.LogFactory();

            // Microsoft.Framework.* has been renamed to Microsoft.Extensions.*
            var builder = new ConfigurationBuilder()
               //.AddJsonFile("appsettings.json")
               //.AddJsonFile("../../../SQHealthMonitorNoGitHubConfig.json", optional: true)       // that file will not go to GIT source control
               //.AddJsonFile("../SQOvermindNoGitHubConfig.json", optional: true)    // for the Production server
               .AddJsonFile("/home/ubuntu/SQ/Server/Overmind/SQOvermindNoGitHubConfig.json", optional: true)    // for the Production server
               .AddJsonFile("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQOvermindNoGitHubConfig.json", optional: true); // for the development PC
                                                                                                                                                               // that file will not go to GIT source control
                                                                                                                                                               // AddEnvironmentVariables();           // adds settings from the Azure WebSite config, needed package : Microsoft.Extensions.Configuration.EnvironmentVariables
            IConfigurationRoot configuration = builder.Build();
            var configItemTest = configuration.GetSection("EmailGyantal").Value;
            gLogger.Info("Test item from config.json: " + configItemTest);
            Console.WriteLine("Test item from config.json: " + configItemTest);

            // Decode all from Base64 encoding, so later the code don't have to do it all the times
            foreach (var item in configuration.GetChildren())
            {
                if (item.Value != null)
                    item.Value = Encoding.UTF8.GetString(Convert.FromBase64String(item.Value));
            }
            gLogger.Info("Test from config.json after decoding: " + configuration.GetSection("EmailGyantal").Value);
            Console.WriteLine("Test from config.json after decoding: " + configuration.GetSection("EmailGyantal").Value);
            if (String.IsNullOrEmpty(configuration.GetSection("EmailGyantal").Value))
            {
                gLogger.Info("ERROR!!!: test item from SQOvermindNoGitHubConfig.json was not found.");
                Console.WriteLine("ERROR!!!: test item from SQOvermindNoGitHubConfig.json was not found.");
            }

            HQEmail.SenderName = configuration.GetSection("EmailHQServer").Value;
            HQEmail.SenderPwd = configuration.GetSection("EmailHQServerPwd").Value;


            Controller.g_controller.Start(configuration);

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
                        gLogger.Info("Hello. I am not crashed yet! :)");
                        break;
                }

            } while (userInput != "3");

            gLogger.Info("****** Main() END");
            Controller.g_controller.Exit();
            //nlogLogFactory.Flush();
            var resetEvent = new ManualResetEventSlim(false);
            LogManager.Flush(ex => resetEvent.Set(), TimeSpan.FromSeconds(15));
            resetEvent.Wait(TimeSpan.FromSeconds(15));
        }

        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;

            Console.WriteLine("----Overmind Server    (type and press Enter)----");
            //Console.WriteLine();
            Console.WriteLine("1. Test Server (Sending Email)");
            Console.WriteLine("2. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("3. Exit gracefully (Avoid Ctrl-^C).");
            var result = Console.ReadLine();
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
