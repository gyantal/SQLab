using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public partial class Controller
    {
        ParallelTcpListener m_tcpListener;

        void ProcessTcpClient(TcpClient p_tcpClient)
        {
            VirtualBrokerMessage message = null;
            try
            {
                BinaryReader br = new BinaryReader(p_tcpClient.GetStream());
                message = (new VirtualBrokerMessage()).DeserializeFrom(br);
                Console.WriteLine("<Tcp:>" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg.ID:{message.ID}, Param:{message.ParamStr}");  // user can quickly check from Console the messages
                Utils.Logger.Info($"ProcessTcpClient: Message ID:\"{ message.ID}\", ParamStr: \"{ message.ParamStr}\", ResponseFormat: \"{message.ResponseFormat}\"");
                if (message.ResponseFormat == VirtualBrokerMessageResponseFormat.None)
                {
                    Utils.TcpClientDispose(p_tcpClient);
                }
            }
            catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
            {
                Console.WriteLine($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
                Utils.Logger.Info($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
            }

            switch (message.ID)
            {
                case VirtualBrokerMessageID.GetRealtimePrice:
                    GetRealtimePrice(p_tcpClient, message);
                    break;

            }

            if (message.ResponseFormat != VirtualBrokerMessageResponseFormat.None)    // if Processing needed Response to Client, we dispose here. otherwise, it was disposed before putting into processing queue
            {
                Utils.TcpClientDispose(p_tcpClient);
            }
        }

        private void GetRealtimePrice(TcpClient p_tcpClient, VirtualBrokerMessage p_message)
        {
            string reply = Controller.g_gatewaysWatcher.GetRealtimePriceService(p_message.ParamStr);
            BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
            bw.Write($"Real time price is 0.  :)");

            Console.WriteLine($"<TEMP Until DEV>GetRealtimePrice(). Query:'{p_message.ParamStr}', Reply:'{reply}'.");
        }
    }
}
