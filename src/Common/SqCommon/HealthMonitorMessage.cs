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
        ReportWarningFromVirtualBroker,
        SendDailySummaryReportEmail,
        GetHealthMonitorCurrentState,   // not used at the moment
        GetHealthMonitorCurrentStateToHealthMonitorWebsite,
        ReportErrorFromSQLabWebsite,
    };

    public enum HealthMonitorMessageResponseFormat { None = 0, String, JSON };


    public class HealthMonitorMessage
    {
        public static string TcpServerHost { get; set; }
        public static int TcpServerPort { get; set; }

        public HealthMonitorMessageID ID { get; set; }
        public string ParamStr { get; set; } = String.Empty;
        public HealthMonitorMessageResponseFormat ResponseFormat { get; set; }

        public const int DefaultHealthMonitorServerPort = 52100;    // largest port number: 65535, HealthMonitor listens on 52100, VBroker on 52101

        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }

        // all the exceptions are sent, because they are important Even if many happens in a 30 minutes period
        public static void SendException(string p_locationMsg, Exception p_e, HealthMonitorMessageID p_healthMonId)
        {
            //Utils.Logger.Warn($"HealthMonitorMessage.SendException(). Crash in { p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}");
            Utils.Logger.Warn($"HealthMonitorMessage.SendException(): Exception occured in {p_locationMsg}. Exception: '{ p_e.ToString()}'");
            if (!(new HealthMonitorMessage()
            {
                ID = p_healthMonId,
                ParamStr = $"Exception in {p_locationMsg}. Exception: '{ p_e.ToStringWithShortenedStackTrace(400)}'",
                ResponseFormat = HealthMonitorMessageResponseFormat.None
            }.SendMessage().Result))
            {
                Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
            }
        }

        // all the exceptions are sent, because they are important Even if many happens in a 30 minutes period
        public static void Send(string p_fullMsg, HealthMonitorMessageID p_healthMonId)
        {
            Utils.Logger.Info($"HealthMonitorMessage.Send(): '{p_fullMsg }' ");
            if (!(new HealthMonitorMessage()
            {
                ID = p_healthMonId,
                ParamStr = p_fullMsg,
                ResponseFormat = HealthMonitorMessageResponseFormat.None
            }.SendMessage().Result))
            {
                Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
            }
        }


        public static void SendStrongAssert(string p_locationMsg, StrongAssertMessage p_msg, HealthMonitorMessageID p_healthMonId)
        {
            Send(p_locationMsg, $"StrongAssert Warning (if Severity is NoException, it is just a mild Warning. If Severity is ThrowException, that exception triggers a separate message to HealthMonitor as an Error). Severity: {p_msg.Severity}, Message: { p_msg.Message}, StackTrace: { p_msg.StackTrace}", p_healthMonId);
        }

        static DateTime gLastMessageTime = DateTime.MinValue;

        public static async void Send(string p_locationMsg, string p_msg, HealthMonitorMessageID p_healthMonId)
        {
            //Utils.Logger.Warn($"HealthMonitorMessage.SendException(). Crash in { p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}");
            Utils.Logger.Warn($"HealthMonitorMessage.Send(): Msg from {p_locationMsg}. Message: '{ p_msg}'");
            if ((DateTime.UtcNow - gLastMessageTime).TotalMinutes > 30)   // don't send it in every minute, just after 30 minutes
            {
                var t = (new HealthMonitorMessage()
                {
                    ID = p_healthMonId,
                    ParamStr = $"Msg from {p_locationMsg}. {p_msg}",
                    ResponseFormat = HealthMonitorMessageResponseFormat.None
                }.SendMessage());

                if (!(await t))
                {
                    Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
                }
                gLastMessageTime = DateTime.UtcNow;
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
