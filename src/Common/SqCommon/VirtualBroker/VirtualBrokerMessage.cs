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
        GetAccountsInfo = 1642, // AccountSummary and Positions and MarketValues info
    };

    public enum VirtualBrokerMessageResponseFormat { None = 0, String, JSON };


    public class VirtualBrokerMessage
    {
        public string TcpServerHost { get; set; }   // = There are at least 2 Vbrokers: ManualTradingServer (for GetAccountInfo()), AutoTradingServer
        public int TcpServerPort { get; set; }      // = DefaultVirtualBrokerServerPort;

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
                {
                    var vbServerEnvironment = Utils.Configuration["VbServerEnvironment"];
                    if (vbServerEnvironment.ToLower() == "AutoTradingServer".ToLower())
                        return "172.31.56.196";     // private IP of the VBrokerAgent Linux (where VBrokerAgen app runs)
                    else if (vbServerEnvironment.ToLower() == "ManualTradingServer".ToLower())
                        return "172.31.43.137";     // private IP of the VBrokerAgent Linux (where VBrokerAgen app runs)
                    else
                        return "127.0.0.1";
                }
            }
        }

        public static string AtsVirtualBrokerServerPublicIpForClients   // AutoTraderServer
        {
            get {
                if (Utils.RunningPlatform() == Platform.Windows)
                    //return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new VirtualBroker features
                    return "52.203.240.30";      // sometimes for clients running on Windows (in development), we want the proper VirtualBroker if Testing runnig VBroker locally
                else
                    return "52.203.240.30";
            }
        }

        public static string MtsVirtualBrokerServerPublicIpForClients   //ManualTraderServer
        {
            get
            {
                if (Utils.RunningPlatform() == Platform.Windows)
                    //return "localhost";       // sometimes for clients running on Windows (in development), we want localHost if Testing new VirtualBroker features
                    return "34.251.1.119";      // sometimes for clients running on Windows (in development), we want the proper VirtualBroker if Testing runnig VBroker locally
                else
                    return "34.251.1.119";
            }
        }


        //public static void InitGlobals(string p_host, int p_port)
        //{
        //    TcpServerHost = p_host;
        //    TcpServerPort = p_port;
        //}

        public static async Task<string> Send(string p_msg, VirtualBrokerMessageID p_vbMessageId)
        {
            return await Send(p_msg, p_vbMessageId, AtsVirtualBrokerServerPublicIpForClients, DefaultVirtualBrokerServerPort);
        }

        public static async Task<string> Send(string p_msg, VirtualBrokerMessageID p_vbMessageId, string p_tcpServerHost, int p_tcpServerPort)
        {
            Utils.Logger.Info($"VirtualBrokerMessage.Send(): Message: '{ p_msg}'");

            var t = (new VirtualBrokerMessage()
            {
                TcpServerHost = p_tcpServerHost,
                TcpServerPort = p_tcpServerPort,
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

        // 2020-06-07: After HTTP GET '/rtp' as real-time price query message sent to Vbroker, the server slowed down with 100% CPU. 
        // 1 minute later Kestrel logged: #Warn: Heartbeat took longer than "00:00:01"
        // https://github.com/dotnet/aspnetcore/issues/17321    https://github.com/dotnet/aspnetcore/issues/4760
        // this is usually happens if thread pool is starved, because it cannot find any free threads in threadpool, because each of them is blocked and waiting.
        // It is very suspicios that somehow this Tcp connection/write/read left or the DelayTask was left on, and didn't finish properly.
        // next time it happens check Top again, showing nTH (Number of Threads). In normal cases, there are 22-23 threads in the Threadpool waiting for connections from clients.
        // https://www.golinuxcloud.com/check-threads-per-process-count-processes/
        // If it goes to 50, then it shows it is really the problem that there are no free threads. Then we have to rewrite this Tcp communication code.
        // Alternatively, it might be possible ASP.Net Core 2.0 has some bug, which was later corrected in ASP.NET core 3.0, and as we migrate from SqLab to SqCore, we don't really have to worry about this.
        // 2020-06-09: same thing. Number of threads: 25-28, it is not excessive.
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
                        // !!! IMPORTANT !!! If there is Exception here
                        // Check that Amazon AWS Firewall let the port through
                        // Check that Linux firewall is inactive "sudo ufw status verbose"

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
                Utils.Logger.Error(e, "Error:VirtualBrokerMessage.SendMessage exception. Check both AWS and Linux firewalls! ");
            }
            return reply; // in case of timeout, return null string to the caller.
        }
    }


}
