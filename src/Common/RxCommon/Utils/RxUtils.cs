using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using SqCommon;

namespace RxCommon
{

    public static partial class RxUtils
    {
        public static bool InitDefaultLogger(string p_filenameWithoutExt)
        {
            try
            {
                //  where the project.json is
                // in VS 15.5: G:\work\Archi-data\GitHubRepos\SQLab\src\Server\HealthMonitor
                // in VS 15.6.1: G:\work\Archi-data\GitHubRepos\SQLab\src\Server\HealthMonitor\bin\Debug\netcoreapp2.0
                string csprojDir = Directory.GetCurrentDirectory();
                if (String.Equals(Path.GetFileName(csprojDir), "netcoreapp2.0", StringComparison.InvariantCultureIgnoreCase))
                {
                    csprojDir = Path.Combine(csprojDir, "..", "..", "..");
                }

                //var currProject = Path.Combine(currDir, "project.json"); // we can open it and read its contents later
                var currProject = Path.Combine(csprojDir, p_filenameWithoutExt + ".csproj"); // we can open it and read its contents later
                if (!File.Exists(currProject))
                {
                    Console.WriteLine($"!Error. We assume Current Directory is where {p_filenameWithoutExt}.csproj is. Cannot find: {currProject}");
                    return false;
                }
                string logDir = Path.Combine(csprojDir, "..", "..", "..", "logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                string logFilePath = Path.Combine(logDir, p_filenameWithoutExt + "."+ DateTime.UtcNow.ToString("yyyy-MM-dd") + ".sqlog");  // the extension is *.sqlog so easy to find it in the file system

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
