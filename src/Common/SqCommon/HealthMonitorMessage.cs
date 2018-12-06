using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SqCommon
{
    public enum HealthMonitorMessageID  // ! if this enum is changed by inserting a new value in the middle, redeploy all apps that uses it, otherwise they interpret the number differently
    {
        Undefined = 0,
        Ping,
        TestHardCash,
        TestSendingEmail,
        TestMakingPhoneCall,
        ReportErrorFromVirtualBroker,       // later we need ReportWarningFromVirtualBroker too which will send only emails, but not Phonecalls
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

        static DateTime gLastMessageTime = DateTime.MinValue;   // be warned, this is global for the whole App; better to not use it, because messages can be swallowed silently. HealthMonitor itself should decide if it swallows it or not, and not the SenderApp.


        public static void InitGlobals(string p_host, int p_port)
        {
            TcpServerHost = p_host;
            TcpServerPort = p_port;
        }

        // In general try to send All the exceptions and messages to HealthMonitor, even though it is CPU busy. It will be network busy anyway. It is the responsibility of HealthMonitor to decide their fate.
        // VBroker or SQLab website don't use p_globalMinTimeBetweenMessages, but maybe other crawler Apps will use it in the future. So keep the functionality, but without strong reason don't use it.
        public static async Task SendAsync(string p_fullMsg, HealthMonitorMessageID p_healthMonId, TimeSpan? p_globalMinTimeBetweenMessages = null)
        {
            Utils.Logger.Warn($"HealthMonitorMessage.SendAsync(): Message: '{ p_fullMsg}'");
            TimeSpan globalMinTimeBetweenMessages = p_globalMinTimeBetweenMessages ?? TimeSpan.MinValue;
            if ((DateTime.UtcNow - gLastMessageTime) > globalMinTimeBetweenMessages)   // don't send it in every minute, just after e.g. 30 minutes
            {
                Utils.Logger.Info($"HealthMonitorMessage.SendAsync(), step 1.");
                var t = (new HealthMonitorMessage()
                {
                    ID = p_healthMonId,
                    ParamStr = p_fullMsg,
                    ResponseFormat = HealthMonitorMessageResponseFormat.None
                }.SendMessage());
                Utils.Logger.Info($"HealthMonitorMessage.SendAsync(), step 2.");

                if (!(await t))
                {
                    Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
                }
                gLastMessageTime = DateTime.UtcNow;
            }
            Utils.Logger.Info($"HealthMonitorMessage.SendAsync() END");
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
            bool reply = false;
            try {
                TcpClient client = new TcpClient();
                Task connectTask = client.ConnectAsync(TcpServerHost, TcpServerPort);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(10)));
                if (completedTask == connectTask)
                {
                    // Task completed within timeout.
                    // Consider that the task may have faulted or been canceled.
                    // We re-await the task so that any exceptions/cancellation is rethrown.
                    Utils.Logger.Debug("HealthMonitorMessage.SendMessage(). client.ConnectAsync() completed without timeout.");
                    //delayTaskCancellationTokenSource.Cancel();  // Task.Delay task is backed by a system timer. Release those resources instead of waiting for 30sec; Was done in VirtualBrokerMessag.cs, but it is not important here.
                    await connectTask;  // Very important in order to propagate exceptions
                                        // sometimes task ConnectAsync() returns instantly (no timeout), but there is an error in it. Which results an hour later: "TaskScheduler_UnobservedTaskException. Exception. A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. "
                    if (connectTask.Exception != null)
                    {
                        Utils.Logger.Error(connectTask.Exception, "Error:HealthMonitorMessage.SendMessage(). Exception in ConnectAsync() task.");
                    }
                    else
                    {
                        BinaryWriter bw = new BinaryWriter(client.GetStream());
                        SerializeTo(bw);
                        reply = true;
                    }
                }
                else  // timeout/cancellation logic
                {
                    //throw new TimeoutException("The operation has timed out.");
                    Utils.Logger.Error("Error:HealthMonitorMessage.SendMessage(). client.ConnectAsync() timeout.");
                    connectTask.Dispose();  // try to Cancel the long running ConnectAsync() task, so it does'nt raise exception 2 days later.
                }
                Utils.TcpClientDispose(client);
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "Error:HealthMonitorMessage.SendMessage exception.");
            }
            return reply;
        }
    }


}
