using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqCommon
{
    // gather IPs here so if it changes, it has to be changed only here
    public static class ServerIp
    {
        // DEV server: private IP: 172.31.60.145, public static IP (Elastic): 23.20.243.199 == currently http://snifferquant.net/ but what if in the future, the Website and HealthMonitor will be on separate servers. So, use IP, instead of DNS name *.net.
        public static string HealthMonitorListenerPrivateIp // HealthMonitor.exe thinks that is its IP
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "127.0.0.1";
                else
                    return "172.31.60.145";     // private IP of the VBrokerDEV server (where the HealthMonitor App runs)
            }
        }

        public static string HealthMonitorPublicIp      // for Clients. Clients of HealthMonitor sees this
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new HealthMonitor features
                                              //return "23.20.243.199";      // public IP for the VBrokerDEV server, sometimes for clients running on Windows (in development), we want the proper Healthmonitor if Testing runnig VBroker locally
                else
                    return "23.20.243.199";
            }
        }

        public static string HQaVM1PublicIp
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new HealthMonitor features
                                              //return "191.237.218.153";      // public IP for the VBrokerDEV server, sometimes for clients running on Windows (in development), we want the proper Healthmonitor if Testing runnig VBroker locally
                else
                    return "191.237.218.153";
            }
        }
    }
}
