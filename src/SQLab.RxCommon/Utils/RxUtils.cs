using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using SQCommon;

namespace RxCommon
{

    public static partial class RxUtils
    {
        public static bool InitDefaultLogger(string p_filenameWithoutExt)
        {
            try
            {
                var currDir = Directory.GetCurrentDirectory();  // where the project.json is G:\work\Archi-data\GitHubRepos\SQLab\src\Server\HealthMonitor
                var currProject = Path.Combine(currDir, "project.json"); // we can open it and read its contents later
                if (!File.Exists(currProject))
                {
                    Console.WriteLine($"!Error. We assume Current Directory is where project.json is. Cannot find: {currProject}");
                    return false;
                }
                string logFilePath = Path.Combine(currDir, p_filenameWithoutExt + ".sq.log");

                RxLogger.Instance.SubscribeObserver(new ConsoleLogObserver());
                RxLogger.Instance.SubscribeObserver(new FileLogObserver(logFilePath));
                Utils.Logger = RxLogger.Instance;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"!Error. Exception: {e.Message}");
                return false;
            }
            
        }
    }
}
