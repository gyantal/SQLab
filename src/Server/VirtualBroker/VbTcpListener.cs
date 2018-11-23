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
                //Console.WriteLine("<Tcp:>" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg.ID:{message.ID}, Param:{message.ParamStr}");  // user can quickly check from Console the messages. It is good in HealthMonitor, but in VBroker we don't want to clutter the Console.
                Utils.Logger.Info($"Controller.ProcessTcpClient(): Message ID:'{ message.ID}', ParamStr: '{ message.ParamStr}', ResponseFormat: '{message.ResponseFormat}'");
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

            string reply = null;
            switch (message.ID)
            {
                case VirtualBrokerMessageID.GetRealtimePrice:
                    reply = Controller.g_gatewaysWatcher.GetRealtimePriceService(message.ParamStr);
                    break;
                //case VirtualBrokerMessageID.GetAccountsSummary:
                //case VirtualBrokerMessageID.GetAccountsPositions:
                case VirtualBrokerMessageID.GetAccountsInfo:
                    reply = Controller.g_gatewaysWatcher.GetAccountsInfo(message.ParamStr);
                    break;
                default:
                    StrongAssert.Fail(Severity.NoException, $"<Tcp:> ProcessTcpClient: Message ID:'{ message.ID}' is unexpected, unhandled. This probably means a serious error.");
                    break;

            }

            if (message.ResponseFormat != VirtualBrokerMessageResponseFormat.None)
            {
                if (String.IsNullOrEmpty(reply))
                {
                    Utils.Logger.Warn("<Tcp:> Warning. Controller.g_gatewaysWatcher.SomeService() returned IsNullOrEmpty. We return empty string to the caller. Better to send this error instantly than letting the caller timeout.");
                    if (reply == null)
                        reply = String.Empty;
                }
                BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                bw.Write(reply);
                Utils.Logger.Trace($"<Tcp:> TcpListener.SomeService(). Message ID:'{ message.ID}', Query:'{message.ParamStr}', Reply:'{reply}'.");

                Utils.TcpClientDispose(p_tcpClient); // if Processing needed Response to Client, we dispose here. otherwise, it was disposed before putting into processing queue
            }
        }
    }
}
