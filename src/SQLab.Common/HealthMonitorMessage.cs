using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SqCommon
{
    public enum HealthMonitorMessageID
    {
        Undefined = 0,
        TestHardCash,
        TestSendingEmail,
        TestMakingPhoneCall,
        ReportErrorFromVirtualBroker,
        ReportOkFromVirtualBroker,
        SendDailySummaryReportEmail,
        GetHealthMonitorCurrentState
    };

    public enum HealthMonitorMessageResponseFormat { None = 0, String, JSON };


    public class HealthMonitorMessage
    {
        public static string TcpServerHost { get; set; }
        public static int TcpServerPort { get; set; }

        public HealthMonitorMessageID ID { get; set; }
        public string ParamStr { get; set; } = String.Empty;
        public HealthMonitorMessageResponseFormat ResponseFormat { get; set; }

        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }

        public static void SendException(string p_locationMsg, Exception e, HealthMonitorMessageID p_healthMonId)
        {
            if (!(new HealthMonitorMessage()
            {
                ID = p_healthMonId,
                ParamStr = $"Crash in {p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}",
                ResponseFormat = HealthMonitorMessageResponseFormat.None
            }.SendMessage().Result))
            {
                Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
            }
        }


        public void SerializeTo(BinaryWriter p_binaryWriter)
        {
            p_binaryWriter.Write((Int32)ID);
            p_binaryWriter.Write(ParamStr);
            p_binaryWriter.Write((Int32)ResponseFormat);
        }

        public HealthMonitorMessage DeserializeFrom(BinaryReader p_binaryReader)
        {
            ID = (HealthMonitorMessageID)p_binaryReader.ReadInt32();
            ParamStr = p_binaryReader.ReadString();
            ResponseFormat = (HealthMonitorMessageResponseFormat)p_binaryReader.ReadInt32();
            return this;
        }

        public async Task<bool> SendMessage()
        {
            try {
                TcpClient client = new TcpClient();
                Task task = client.ConnectAsync(TcpServerHost, TcpServerPort);
                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))) != task)
                {
                    Utils.Logger.Error("Error:HealthMonitor server: client.Connect() timeout.");
                    return false;
                }

                BinaryWriter bw = new BinaryWriter(client.GetStream());
                SerializeTo(bw);
                Utils.TcpClientDispose(client);
                return true;
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Error:HealthMonitorMessage.SendMessage exception.");
                return false;
            }
        }
    }


}
