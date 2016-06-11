using SqCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public partial class HealthMonitor
    {
        Object m_lastSqWebsiteInformSupervisorLock = new Object();   // null value cannot be locked, so we have to create an object
        DateTime m_lastSqWebsiteErrorEmailTime = DateTime.MinValue;    // don't email if it was made in the last 10 minutes
        DateTime m_lastSqWebsiteErrorPhoneCallTime = DateTime.MinValue;    // don't call if it was made in the last 30 minutes
      
        private void ErrorFromSqLabWebsite(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (!m_persistedState.IsProcessingSQLabWebsiteMessagesEnabled)
                return;

            if (p_message.ResponseFormat == HealthMonitorMessageResponseFormat.String)
            {
                BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                bw.Write("FromServer: Message received, saved and starting processing: " + p_message.ParamStr);
            }

            Utils.Logger.Info("ErrorFromSqLabWebsite().");
            InformSupervisors("SQ HealthMonitor: ERROR from SQLab Website.", $"SQ HealthMonitor: ERROR from SQLab Website. MessageParamStr: { p_message.ParamStr}", null, ref m_lastSqWebsiteInformSupervisorLock, ref m_lastSqWebsiteErrorEmailTime, ref m_lastSqWebsiteErrorPhoneCallTime);
        }

    
        
    }
}
