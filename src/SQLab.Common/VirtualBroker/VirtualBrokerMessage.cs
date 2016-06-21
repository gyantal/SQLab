using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SqCommon
{
    public enum VirtualBrokerMessageID      // messages To VirtualBroker (from other programs: e.g. website for realtime price)
    {
        Undefined = 0,      // for security reasons, better to give random numbers in the Tcp Communication. Fake clienst generally try to send: '0', or '1', '2', treat those as Unexpected
        GetVirtualBrokerCurrentState = 1640,   // not used at the moment. HealthMonitor may do active polling to query if VBroker is alive or not.
        GetRealtimePrice = 1641,
        GetIbUserAccountInfo = 1642,     // for example if a website want to get the NAV or stock positions or leverage of an IB user accont
    };

    public enum VirtualBrokerMessageResponseFormat { None = 0, String, JSON };


    public class VirtualBrokerMessage
    {
        public static string TcpServerHost { get; set; } = VirtualBrokerServerPublicIpForClients;
        public static int TcpServerPort { get; set; } = DefaultVirtualBrokerServerPort;

        public VirtualBrokerMessageID ID { get; set; }
        public string ParamStr { get; set; } = String.Empty;
        public VirtualBrokerMessageResponseFormat ResponseFormat { get; set; }

        public const int DefaultVirtualBrokerServerPort = 52101;    // largest port number: 65535, HealthMonitor listens on 52100, VBroker on 52101

        // VBroker server: private IP: 172.31.56.196, public IP (Elastic): 52.203.240.30
        public static string VirtualBrokerServerPrivateIpForListener
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "127.0.0.1";
                else
                    return "172.31.56.196";     // private IP of the VBrokerAgent Linux (where VBrokerAgen app runs)
            }
        }

        public static string VirtualBrokerServerPublicIpForClients
        {
            get {
                if (Utils.RunningPlatform() == Platform.Windows)
                    return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new VirtualBroker features
                                              //return "23.20.243.199";      // sometimes for clients running on Windows (in development), we want the proper VirtualBroker if Testing runnig VBroker locally
                else
                    return "52.203.240.30";
            }
        }

      

        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }


        public static async Task<string> Send(string p_msg, VirtualBrokerMessageID p_healthMonId)
        {
            //Utils.Logger.Warn($"VirtualBrokerMessage.SendException(). Crash in { p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}");
            Utils.Logger.Info($"VirtualBrokerMessage.Send(): Message: '{ p_msg}'");

            var t = (new VirtualBrokerMessage()
            {
                ID = p_healthMonId,
                ParamStr = $"{p_msg}",
                ResponseFormat = VirtualBrokerMessageResponseFormat.String
            }.SendMessage());

            string reply = (await t);
            return reply;
        }



        public void SerializeTo(BinaryWriter p_binaryWriter)
        {
            p_binaryWriter.Write((Int32)ID);
            p_binaryWriter.Write(ParamStr);
            p_binaryWriter.Write((Int32)ResponseFormat);
        }

        public VirtualBrokerMessage DeserializeFrom(BinaryReader p_binaryReader)
        {
            ID = (VirtualBrokerMessageID)p_binaryReader.ReadInt32();
            ParamStr = p_binaryReader.ReadString();
            ResponseFormat = (VirtualBrokerMessageResponseFormat)p_binaryReader.ReadInt32();
            return this;
        }

        public async Task<string> SendMessage()
        {
            try {
                TcpClient client = new TcpClient();
                Task task = client.ConnectAsync(TcpServerHost, TcpServerPort);
                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30))) != task)
                {
                    Utils.Logger.Error("Error:VirtualBroker server: client.Connect() timeout.");
                    return null;
                }

                BinaryWriter bw = new BinaryWriter(client.GetStream());
                SerializeTo(bw);

                BinaryReader br = new BinaryReader(client.GetStream());
                string reply = br.ReadString();

                Utils.TcpClientDispose(client);
                return reply;
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Error:VirtualBrokerMessage.SendMessage exception.");
                return null;
            }
        }
    }


}
