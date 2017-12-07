using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
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
            //Utils.Logger.Warn($"VirtualBrokerMessage.SendException(). Crash in { p_locationMsg}. Exception Message: '{ e.Message}', StackTrace: { e.StackTrace}");
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
            try {
                TcpClient client = new TcpClient();

                // http://stackoverflow.com/questions/4036198/does-task-waitint-stop-the-task-if-the-timeout-elapses-without-the-task-finish
                bool wasTimeOut = false;
                Task connectTask = client.ConnectAsync(TcpServerHost, TcpServerPort);      // usually, we create a task with a CancellationToken. However, this task is not cancellable. I cannot cancel it. I have to wait for its finish.
                Task continuationTask = connectTask.ContinueWith((antecedentTask) =>
                {      // this is one way to handle it.
                    if (!wasTimeOut)    // if it was no timeout, don't do anything
                        return;

                    // we cannot let task to sleep, because 2 days later, at GC, it can reaise ''System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread.'
                    Utils.Logger.Debug("VirtualBroker server: client.Connect() timeout happened earlier. ConnectAsync() finished right now.");
                    if (antecedentTask.Exception != null)
                    {
                        Utils.Logger.Debug("VirtualBroker server: client.Connect() timeout happened earlier. ConnectAsync() finished right now. Exception: " + antecedentTask.Exception);
                    }

                });
                if (!continuationTask.Wait(TimeSpan.FromSeconds(30))) // wait for 30 seconds, after timeout, the task is not cancelled, and its async thread is still runnning. Bad. Suggestion: use the Cancellation token.
                {
                    wasTimeOut = true;
                    Utils.Logger.Error("Error:VirtualBroker server: client.Connect() timeout.");

                    return null; // in case of timeout, return instantly to the caller.
                }


                // Remove from Source code later, if this exception doesn't come any more: "System.AggregateException: A Task's exception(s) were not observed either by Waiting"
                //if (!task.Wait(TimeSpan.FromSeconds(30))) // wait for 30 seconds, after timeout, the task is not cancelled, and its async thread is still runnning. Bad. Suggestion: use the Cancellation token.
                //{
                //    Utils.Logger.Error("Error:VirtualBroker server: client.Connect() timeout.");
                //    // we cannot let task to sleep, because 2 days later, at GC, it can reaise ''System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread.'
                //    // if we return with an Timeout Error, we have to kill the task before.
                //    Task.Factory.StartNew(() =>     // The action delegate to execute asynchronously.  "This call is not awaited." Good.
                //    {
                //        try
                //        {
                //            task.Wait();    // the ConnectAsync() task is not Cancellable. It will finish soon. We have to wait ConnectAsync() forever on a separate thread.

                //            if (task.Exception != null)
                //            {
                //                Utils.Logger.Error(task.Exception, "Error:VirtualBroker server: client.Connect() timeout. and later Exception raised.");
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            //WasAborted = true;
                //            //return false;
                //        }
                //    });

                //return null;    // in case of timeout, return instantly to the caller.
                //}



                //if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30))) != task)
                //{
                //    Utils.Logger.Error("Error:VirtualBroker server: client.Connect() timeout.");
                //    // we cannot let task to sleep, because 2 days later, at GC, it can reaise ''System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread.'
                //    // if we return with an Timeout Error, we have to kill the task before.
                //    //task.
                //    return null;
                //}







                // sometimes task ConnectAsync() returns instantly (no timeout), but there is an error in it. Which results an hour later: "TaskScheduler_UnobservedTaskException. Exception. A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. "
                if (connectTask.Exception != null)
                {
                    Utils.Logger.Error(connectTask.Exception, "Error:VirtualBrokerMessage.SendMessage(). Exception in ConnectAsync() task.");
                    return null;
                }

                BinaryWriter bw = new BinaryWriter(client.GetStream()); // sometimes "System.InvalidOperationException: The operation is not allowed on non-connected sockets." at TcpClient.GetStream()
                SerializeTo(bw);

                BinaryReader br = new BinaryReader(client.GetStream());
                string reply = br.ReadString(); // sometimes "System.IO.EndOfStreamException: Unable to read beyond the end of the stream." at ReadString()

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
