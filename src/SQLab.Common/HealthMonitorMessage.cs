﻿using System;
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

        // DEV server: private IP: 172.31.60.145, public static IP (Elastic): 23.20.243.199 == currently http://snifferquant.net/ but what if in the future, the Website and HealthMonitor will be on separate servers. So, use IP, instead of DNS name *.net.
        public static string HealthMonitorServerPrivateIpForListener
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "127.0.0.1";
                else
                    return "172.31.60.145";     // private IP of the VBrokerDEV server (where the HealthMonitor App runs)
            }
        }

        public static string HealthMonitorServerPublicIpForClients
        {
            get {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new HealthMonitor features
                    //return "23.20.243.199";      // public IP for the VBrokerDEV server, sometimes for clients running on Windows (in development), we want the proper Healthmonitor if Testing runnig VBroker locally
                else
                    return "23.20.243.199";     // public IP for the VBrokerDEV server
            }
        }

      

        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }

        // all the exceptions are sent, because they are important Even if many happens in a 30 minutes period
        public static void SendException(string p_locationMsg, Exception e, HealthMonitorMessageID p_healthMonId)
        {
            //Utils.Logger.Warn($"HealthMonitorMessage.SendException(). Crash in { p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}");
            Utils.Logger.Warn($"HealthMonitorMessage.SendException(): Exception occured in {p_locationMsg}. Exception: '{ e.ToString()}'");
            if (!(new HealthMonitorMessage()
            {
                ID = p_healthMonId,
                ParamStr = $"Exception in {p_locationMsg}. Exception: '{ e.ToStringWithShortenedStackTrace(400)}'",
                ResponseFormat = HealthMonitorMessageResponseFormat.None
            }.SendMessage().Result))
            {
                Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
            }
        }

        public static void SendStrongAssert(string p_locationMsg, StrongAssertMessage p_msg, HealthMonitorMessageID p_healthMonId)
        {
            Send(p_locationMsg, $"StrongAssert. Severity: {p_msg.Severity}, Message { p_msg.Message}, StackTrace: { p_msg.StackTrace}", p_healthMonId);
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
