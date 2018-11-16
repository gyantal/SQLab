using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// A common base used both in VBroker and SQLab website. (E.g. for realtime price communication)
namespace SqCommon
{
    public enum VirtualBrokerMessageID      // messages To VirtualBroker (from other programs: e.g. website for realtime price)
    {
        Undefined = 0,      // for security reasons, better to give random numbers in the Tcp Communication. Fake clienst generally try to send: '0', or '1', '2', treat those as Unexpected
        GetVirtualBrokerCurrentState = 1640,   // not used at the moment. HealthMonitor may do active polling to query if VBroker is alive or not.
        GetRealtimePrice = 1641,
        GetAccountsSummaryOrPositions = 1642, // the previous two in one query (for speed). The client might start with this, but later only asks for positions. Handle this with an inside param.
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
                    //return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new VirtualBroker features
                    return "52.203.240.30";      // sometimes for clients running on Windows (in development), we want the proper VirtualBroker if Testing runnig VBroker locally
                else
                    return "52.203.240.30";
            }
        }

      

        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }


        public static async Task<string> Send(string p_msg, VirtualBrokerMessageID p_vbMessageId)
        {
            Utils.Logger.Info($"VirtualBrokerMessage.Send(): Message: '{ p_msg}'");

            var t = (new VirtualBrokerMessage()
            {
                ID = p_vbMessageId,
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
            string reply = null;

            try {
                TcpClient client = new TcpClient();
                Task connectTask = client.ConnectAsync(TcpServerHost, TcpServerPort);      // usually, we create a task with a CancellationToken. However, this task is not cancellable. I cannot cancel it. I have to wait for its finish.

                //https://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout
                using (var delayTaskCancellationTokenSource = new CancellationTokenSource())
                {
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(30), delayTaskCancellationTokenSource.Token));
                    if (completedTask == connectTask)
                    {
                        // Task completed within timeout.
                        // Consider that the task may have faulted or been canceled.
                        // We re-await the task so that any exceptions/cancellation is rethrown.
                        Utils.Logger.Debug("VirtualBrokerMessage.SendMessage(). client.ConnectAsync() completed without timeout.");
                        delayTaskCancellationTokenSource.Cancel();  // Task.Delay task is backed by a system timer. Release those resources instead of waiting for 30sec

                        await connectTask;  // Very important in order to propagate exceptions
                        // sometimes task ConnectAsync() returns instantly (no timeout), but there is an error in it. Which results an hour later: "TaskScheduler_UnobservedTaskException. Exception. A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. "
                        if (connectTask.Exception != null)
                        {
                            Utils.Logger.Error(connectTask.Exception, "Error:VirtualBrokerMessage.SendMessage(). Exception in ConnectAsync() task.");
                        }
                        else
                        {
                            BinaryWriter bw = new BinaryWriter(client.GetStream()); // sometimes "System.InvalidOperationException: The operation is not allowed on non-connected sockets." at TcpClient.GetStream()
                            SerializeTo(bw);

                            BinaryReader br = new BinaryReader(client.GetStream());
                            reply = br.ReadString(); // sometimes "System.IO.EndOfStreamException: Unable to read beyond the end of the stream." at ReadString()
                        }
                    }
                    else  // timeout/cancellation logic
                    {
                        //throw new TimeoutException("The operation has timed out.");
                        Utils.Logger.Error("Error:VirtualBrokerMessage.SendMessage(). client.ConnectAsync() timeout.");
                        connectTask.Dispose();  // try to Cancel the long running ConnectAsync() task, so it does'nt raise exception 2 days later.
                    }
                }

                Utils.TcpClientDispose(client);
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Error:VirtualBrokerMessage.SendMessage exception.");
            }
            return reply; // in case of timeout, return null string to the caller.
        }
    }


}
