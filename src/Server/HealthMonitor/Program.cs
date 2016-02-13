using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQCommon;
using System.Reflection;
using System.IO;
//using Microsoft.Extensions.Configuration;

namespace HealthMonitor
{
    public class Program
    {
        public static IConfigurationRoot Configuration = null;

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

            Console.WriteLine("Hello HealthMonitor, v1.0.6");
            var currDir = Directory.GetCurrentDirectory();  // where the project.json is G:\work\Archi-data\GitHubRepos\SQLab\src\Server\HealthMonitor
            var currProject = Path.Combine(currDir, "project.json"); // we can open it and read its contents later
            if (!File.Exists(currProject))
            {
                Console.WriteLine("!Error. We assume Current Directory is where project.json is. Cannot find: " + currProject);
                return;
            }
            string logFilePath = Path.Combine(currDir, typeof(Program).Namespace + ".sq.log");
            Console.WriteLine("Log file: " + logFilePath);
            Utils.Logger = new SQLogger(logFilePath);
            Utils.Logger.Info("****** Main() START");

            Utils.Configuration = Utils.BuildConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.HealthMonitor.NoGitHub.json", "/home/ubuntu/SQ/Server/HealthMonitor/SQLab.HealthMonitor.NoGitHub.json");

            Controller.g_controller.Start();

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
                        //Controller.g_controller.TestSendingEmailAndPhoneCall();
                        break;
                }

            } while (userInput != "3");

            Utils.Logger.Info("****** Main() END");
            Controller.g_controller.Exit();
            Utils.Logger.Exit();
        }



        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;

            Console.WriteLine("----HealthMonitor Server    (type and press Enter)----");
            Console.WriteLine("1. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("2. Foo.");
            Console.WriteLine("3. Exit gracefully (Avoid Ctrl-^C).");
            var result = Console.ReadLine();
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
