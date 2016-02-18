using SQCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public partial class Controller
    {
        // You can't create a 'const' array because arrays are objects and can only be created at runtime and const entities are resolved at compile time.
        public readonly string[] cWebsitesToCheck = {
            "http://sqhealthmonitor.azurewebsites.net/WebServer/Ping",
            "http://www.snifferquant.com/dac/",
            "http://strategysniffer.azurewebsites.net" };

        bool m_isCheckWebsitesServiceOutageEmailWasSent = false;  // to avoid sending the same warning email every 9 minutes; send only once
        
        public void CheckWebsitesAndKeepAliveTimer_Elapsed(object p_sender)
        {
            Utils.Logger.Info("CheckWebsitesAndKeepAliveTimer_Elapsed() BEGIN");
            try
            {
                List<string> failedWebsites = new List<string>();

                foreach (var website in cWebsitesToCheck)
                {

                    string hmWebsiteStr = String.Empty;
                    if (Utils.DownloadStringWithRetry(out hmWebsiteStr, website, 5, TimeSpan.FromSeconds(5), false))
                        Utils.Logger.Info(website + " returned: " + hmWebsiteStr.Substring(0, (hmWebsiteStr.Length > 45) ? 45 : hmWebsiteStr.Length));
                    else
                    {
                        Utils.Logger.Info("Failed download multiple (5x) times :" + website);
                        failedWebsites.Add(website);
                    }
                }

                bool isOK = (failedWebsites.Count == 0);
                if (!isOK)
                {
                    Utils.Logger.Info("CheckWebsitesAndKeepAliveTimer(): !isOK.");
                    if (!m_isCheckWebsitesServiceOutageEmailWasSent)
                    {
                        Utils.Logger.Info("CheckWebsitesAndKeepAliveTimer(). Sending Warning email.");
                        new SQEmail
                        {
                            ToAddresses = Utils.Configuration["EmailGyantal"],
                            Subject = "SQ HealthMonitor: WARNING! CheckWebsites was NOT successfull.",
                            Body = "SQ HealthMonitor: WARNING! The following downloads are failed multiple (5x) times: " + String.Join(",", failedWebsites.ToArray()),
                            IsBodyHtml = false
                        }.Send();
                        m_isCheckWebsitesServiceOutageEmailWasSent = true;
                    }
                }
                else
                {
                    Utils.Logger.Info("CheckWebsitesAndKeepAliveTimer(): isOK.");
                    if (m_isCheckWebsitesServiceOutageEmailWasSent)
                    {  // it was bad, but now it is correct somehow
                        new SQEmail
                        {
                            ToAddresses = Utils.Configuration["EmailGyantal"],
                            Subject = "SQ HealthMonitor: OK! CheckWebsites was successfull again.",
                            Body = "SQ HealthMonitor: OK! CheckWebsites was successfull again.",
                            IsBodyHtml = false
                        }.Send();
                        m_isCheckWebsitesServiceOutageEmailWasSent = false;
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Error("Exception caught in CheckWebsites Timer. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
            }
        }

    }
}
