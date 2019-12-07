using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public partial class HealthMonitor
    {
        ParallelTcpListener m_tcpListener;

        void ProcessTcpClient(TcpClient p_tcpClient)
        {
            Utils.Logger.Info($"ProcessTcpClient() START");
            HealthMonitorMessage message = null;
            try
            {
                BinaryReader br = new BinaryReader(p_tcpClient.GetStream());
                message = (new HealthMonitorMessage()).DeserializeFrom(br);
                if (message == null)
                {
                    Console.WriteLine("<Tcp:>" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg: NULL");  // user can quickly check from Console the messages
                    Utils.Logger.Info($"<Tcp:>ProcessTcpClient: Message: NULL");
                    return;
                }
                Console.WriteLine("<Tcp:>" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg.ID:{message.ID}, Param:{(String.IsNullOrEmpty(message.ParamStr)?"NULL": message.ParamStr)}");  // user can quickly check from Console the messages
                Utils.Logger.Info($"<Tcp:>ProcessTcpClient: Message ID:\"{ message.ID}\", ParamStr: \"{(String.IsNullOrEmpty(message.ParamStr)?"NULL": message.ParamStr)}\", ResponseFormat: \"{message.ResponseFormat}\"");
            }
            catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
            {
                Console.WriteLine($"<Tcp:>Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots or it is a second message when VBroker crashes dead. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
                Utils.Logger.Info($"<Tcp:>Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots or it is a second message when VBroker crashes dead. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));

                if (e is System.IO.EndOfStreamException)        // in this case, there is no message data.
                {
                    // If VBroker crashes totally, it sends a proper Message ID:"ReportErrorFromVirtualBroker" msg. once, 
                    // but next it may initiate a second message, but it cannot pump the data through, because it is already crashed and all its threads are stopped.
                    // However, don't worry, because the first VBroker message is already under processing. So, second message can be ignored.
                    // the BinaryReader couldn't read the stream, so there is no message, so we dont'n know whether message.ID = VBroker or not. It is unknown. In that case, swallow the error and return, but don't crash HealthMonitor.
                    Utils.Logger.Info($"ProcessTcpClient: System.IO.EndOfStreamException was detected. Return without crashing HealthMonitor thread.");
                    return; // there is no point processing as we don't know the data. However, we still don't want to Crash Healthmonitor. So, just swallow the error.
                }
                else
                {
                    // we may have message.ID and data and we may process it.
                }
            }

            if (message.ResponseFormat == HealthMonitorMessageResponseFormat.None)
            {
                Utils.Logger.Info($"ProcessTcpClient: TcpClientDispose() START");
                Utils.TcpClientDispose(p_tcpClient);
                Utils.Logger.Info($"ProcessTcpClient: TcpClientDispose() END");
            }

            Utils.Logger.Info($"ProcessTcpClient, Step 2");
            switch (message.ID)
            {
                case HealthMonitorMessageID.Ping:
                    ServePingRequest(p_tcpClient, message);
                    break;
                case HealthMonitorMessageID.TestHardCash:
                    throw new Exception("Testing Hard Crash by Throwing this Exception");
                case HealthMonitorMessageID.ReportErrorFromVirtualBroker:
                case HealthMonitorMessageID.ReportWarningFromVirtualBroker:
                case HealthMonitorMessageID.ReportOkFromVirtualBroker:
                    Utils.Logger.Info($"ProcessTcpClient, Step 3");
                    MessageFromVirtualBroker(p_tcpClient, message);
                    Utils.Logger.Info($"ProcessTcpClient, Step 4");
                    break;
                case HealthMonitorMessageID.GetHealthMonitorCurrentStateToHealthMonitorWebsite:
                    CurrentStateToHealthMonitorWebsite(p_tcpClient, message);
                    break;
                case HealthMonitorMessageID.ReportErrorFromSQLabWebsite:
                case HealthMonitorMessageID.SqCoreWebError:
                case HealthMonitorMessageID.SqCoreWebJsError:
                    ErrorFromWebsite(p_tcpClient, message);
                    break;
                default:
                    StrongAssert.Fail(Severity.NoException, $"<Tcp:>ProcessTcpClient: Message ID:'{ message.ID}' is unexpected, unhandled. This probably means a serious error.");
                    break;
            }

            if (message.ResponseFormat != HealthMonitorMessageResponseFormat.None)    // if Processing needed Response to Client, we dispose here. otherwise, it was disposed before putting into processing queue
            {
                Utils.TcpClientDispose(p_tcpClient);
            }

            Utils.Logger.Info($"ProcessTcpClient() END");
        }


        internal void ServePingRequest(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (p_message.ResponseFormat == HealthMonitorMessageResponseFormat.String)
            {
                string responseStr = "Ping. Healthmonitor UtcNow: " + DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
                BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                bw.Write(responseStr);                
            }
        }


        //TcpListener m_tcpListener;
        //Task<TcpClient> m_tcpListenerCurrentClientTask;
        //TcpClient m_tcpListenerCurrentClient;

        ////static Semaphore Go = new Semaphore(0, 1);
        ////ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>> m_messageQueue = new ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>>();
        //BlockingCollection<Tuple<TcpClient, HealthMonitorMessage>> m_messageQueue = new BlockingCollection<Tuple<TcpClient, HealthMonitorMessage>>(new ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>>());


        //void StartTcpMessageListenerThreads()
        //{
        //    // start 1 thread to listen TCP traffic (that can create many threads for reading message)
        //    Task tcpListenerTask = Task.Factory.StartNew(TcpListenerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.

        //    // start 2 threads max to process Messages (limit to not overwhelm the Server CPU)
        //    Task msgProcessing1 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
        //    Task msgProcessing2 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
        //}

        //void MessageProcessorWorkerLoop()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            var packet = m_messageQueue.Take(); //this blocks if there are no items in the queue.
        //            ThreadPool.QueueUserWorkItem(state =>
        //            {
        //                try
        //                {
        //                    var data = (Tuple<TcpClient, HealthMonitorMessage>)state;
        //                    ProcessMessage(data.Item1, data.Item2);
        //                }
        //                catch (Exception e)
        //                {
        //                    Utils.Logger.Error("Exception caught in MessageProcessorWorkerLoop.QueueUserWorkItem. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
        //                    throw;
        //                }

        //                //do whatever you have to do
        //            }, packet);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Utils.Logger.Error("Exception caught in MessageProcessorWorkerLoop. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
        //        //throw; Don't allow even Bacgkround threads to crash the App.
        //    }
        //}

        //// http://stackoverflow.com/questions/7690520/c-sharp-networking-tcpclient
        //void TcpListenerLoop()
        //{
        //    try
        //    {
        //        int port = HealthMonitorMessage.DefaultHealthMonitorServerPort;
        //        string privateIP = ServerIp.HealthMonitorServerPrivateIpForListener;
        //        m_tcpListener = new TcpListener(IPAddress.Parse(privateIP), port);  
        //        m_tcpListener.Start();
        //        Console.WriteLine($"*TcpListener is listening on port {privateIP}:{port}.");
        //        while (true)
        //        {
        //            m_tcpListenerCurrentClientTask = m_tcpListener.AcceptTcpClientAsync();
        //            m_tcpListenerCurrentClient = m_tcpListenerCurrentClientTask.Result;        // Task.Result is blocking. OK.
        //            Console.WriteLine($"TcpListenerLoop.NextClientAccepted.");
        //            Utils.Logger.Info($"TcpListenerLoop.NextClientAccepted.");
        //            if (Utils.MainThreadIsExiting.IsSet)
        //                return; // if App is exiting gracefully, don't start new thread

        //            (new Thread((x) => ReadTcpClientStream(x)) { IsBackground = true }).Start(m_tcpListenerCurrentClient);    // read the BinaryReader() and deserialize in separate thread, so not block the TcpListener loop
        //        }
        //    }
        //    catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
        //    {
        //        if (Utils.MainThreadIsExiting.IsSet)
        //            return; // if App is exiting gracefully, this Exception is not a problem
        //        Utils.Logger.Error("Not expected Exception. We send email by StrongAssert and rethrow exception, which will crash App. TcpListenerLoop. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
        //        StrongAssert.Fail(Severity.ThrowException, "Not expected Exception. We send email by StrongAssert and rethrow exception, which will crash App. TcpListenerLoop. HealthMonitor: manual restart is needed.");
        //        throw;  // if we don't listen to TcpListener any more, there is no point to continue. Crash the App.
        //    }
        //}

        //void ReadTcpClientStream(object p_tcpClient)      // this in running in a separate thread
        //{
        //    try
        //    {
        //        TcpClient client = p_tcpClient as TcpClient;
        //        BinaryReader br = new BinaryReader(client.GetStream());
        //        HealthMonitorMessage message = (new HealthMonitorMessage()).DeserializeFrom(br);
        //        //string strFromClient = br.ReadString();
        //        Console.WriteLine(">" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg:{message.ID}, Param:{message.ParamStr}");  // user can quickly check from Console the messages
        //        Utils.Logger.Info($"TcpMessageListener: Message ID:\"{ message.ID}\", ParamStr: \"{ message.ParamStr}\", ResponseFormat: \"{message.ResponseFormat}\"");
        //        if (message.ResponseFormat == HealthMonitorMessageResponseFormat.None)
        //        {
        //            Utils.TcpClientDispose(client);
        //            client = null;
        //        }

        //        m_messageQueue.Add(new Tuple<TcpClient, HealthMonitorMessage>(client, message));
        //    }
        //    catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
        //    {
        //        Console.WriteLine($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
        //        Utils.Logger.Info($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
        //    }

        //}

        //void StopTcpMessageListener()
        //{
        //    Console.WriteLine("StopTcpMessageListener() exiting...");
        //    // you can finish current TcpConnections properly if it is important
        //    // to Dispose the TcpListener, this hack has to be used to do a Last, Final Connection: http://stackoverflow.com/questions/19220957/tcplistener-how-to-stop-listening-while-awainting-accepttcpclientasync
        //    TcpClient dummyClient = new TcpClient();
        //    dummyClient.ConnectAsync(ServerIp.HealthMonitorServerPrivateIpForListener, HealthMonitorMessage.DefaultHealthMonitorServerPort).Wait();
        //    Console.WriteLine($"StopTcpMessageListener(). Is DummyClient connected: {dummyClient.Connected}");
        //    Utils.TcpClientDispose(dummyClient);

        //    Console.WriteLine("StopTcpMessageListener() exiting..");
        //    m_tcpListener.Stop();   // there is no Dispose() method
        //    Console.WriteLine("StopTcpMessageListener() exiting.");

        //    // Tasks create Background Threads always, so the TcpListenerLoop() is a Background thread, it will exits when main thread exits, which is OK.
        //}


        //void ProcessMessage(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        //{
        //    switch (p_message.ID)
        //    {
        //        case HealthMonitorMessageID.TestHardCash:
        //            throw new Exception("Testing Hard Crash by Throwing this Exception");
        //        case HealthMonitorMessageID.ReportErrorFromVirtualBroker:
        //        case HealthMonitorMessageID.ReportOkFromVirtualBroker:
        //            MessageFromVirtualBroker(p_tcpClient, p_message);
        //            break;
        //        case HealthMonitorMessageID.GetHealthMonitorCurrentStateToHealthMonitorWebsite:
        //            CurrentStateToHealthMonitorWebsite(p_tcpClient, p_message);
        //            break;
        //        case HealthMonitorMessageID.ReportErrorFromSQLabWebsite:
        //            ErrorFromSqLabWebsite(p_tcpClient, p_message);
        //            break;

        //    }

        //    if (p_message.ResponseFormat != HealthMonitorMessageResponseFormat.None)    // if Processing needed Response to Client, we dispose here. otherwise, it was disposed before putting into processing queue
        //    {
        //        Utils.TcpClientDispose(p_tcpClient);
        //    }
        //}

    }
}
