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

namespace SqCommon
{
    public partial class ParallelTcpListener     // listening happens in 1 thread only, ProcessMessageFunc(TcpClient) runs on multiple threads
    {
        public delegate void ProcessTcpClientFunc(TcpClient p_tcpClient);

        int m_port = -1;
        string m_privateIP = null;
        ProcessTcpClientFunc m_processTcpClient;

        System.Net.Sockets.TcpListener m_tcpListener;
        //Task<TcpClient> m_tcpListenerCurrentClientTask;
        //TcpClient m_tcpListenerCurrentClient;
        //static Semaphore Go = new Semaphore(0, 1);
        //ConcurrentQueue<TcpClient> m_messageQueue = new ConcurrentQueue<TcpClient>();
        BlockingCollection<TcpClient> m_tcpClientQueue = new BlockingCollection<TcpClient>(new ConcurrentQueue<TcpClient>());

        public ParallelTcpListener(string p_privateIP, int p_port, ProcessTcpClientFunc p_processTcpClient)
        {
            m_privateIP = p_privateIP;
            m_port = p_port;
            m_processTcpClient = p_processTcpClient;
        }

        public void StartTcpMessageListenerThreads()
        {
            // start 1 thread to listen TCP traffic (that can create many threads for reading message)
            Task tcpListenerTask = Task.Factory.StartNew(TcpListenerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.

            // start 2 threads max to process Messages (limit to not overwhelm the Server CPU)
            Task msgProcessing1 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
            Task msgProcessing2 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
        }

        

        // http://stackoverflow.com/questions/7690520/c-sharp-networking-tcpclient
        void TcpListenerLoop()
        {
            try
            {
                m_tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Parse(m_privateIP), m_port);  
                m_tcpListener.Start();
                Console.WriteLine($"*TcpListener is listening on port {m_privateIP}:{m_port}.");
                while (true)
                {
                    var tcpListenerCurrentClientTask = m_tcpListener.AcceptTcpClientAsync();
                    var tcpClient = tcpListenerCurrentClientTask.Result;        // Task.Result is blocking. OK.
                    Console.WriteLine($"TcpListenerLoop.NextClientAccepted.");
                    Utils.Logger.Info($"TcpListenerLoop.NextClientAccepted.");
                    if (Utils.MainThreadIsExiting.IsSet)
                        return; // if App is exiting gracefully, don't start new thread

                    m_tcpClientQueue.Add(tcpClient);     // If it is a long processing, e.g. reading the TcpClient, do it in a separate thread. If it is just added to the queue, don't start a new thread
                    //(new Thread((x) => ReadTcpClientStream(x)) { IsBackground = true }).Start(tcpClient);    // read the BinaryReader() and deserialize in separate thread, so not block the TcpListener loop
                }
            }
            catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
            {
                if (Utils.MainThreadIsExiting.IsSet)
                    return; // if App is exiting gracefully, this Exception is not a problem
                Utils.Logger.Error("Not expected Exception. We send email by StrongAssert and rethrow exception, which will crash App. TcpListenerLoop. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                StrongAssert.Fail(Severity.ThrowException, "Not expected Exception. We send email by StrongAssert and rethrow exception, which will crash App. TcpListenerLoop. VirtualBroker: manual restart is needed.");
                throw;  // if we don't listen to TcpListener any more, there is no point to continue. Crash the App.
            }
        }

        void MessageProcessorWorkerLoop()
        {
            try
            {
                while (true)
                {
                    var packet = m_tcpClientQueue.Take(); //this blocks if there are no items in the queue.
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            var data = (TcpClient)state;
                            m_processTcpClient(data);
                            //ProcessMessage(data.Item1, data.Item2);
                        }
                        catch (Exception e)
                        {
                            Utils.Logger.Error("Exception caught in MessageProcessorWorkerLoop.QueueUserWorkItem. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                            throw;
                        }

                        //do whatever you have to do
                    }, packet);
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Error("Exception caught in MessageProcessorWorkerLoop. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                //throw; Don't allow even Bacgkround threads to crash the App.
            }
        }

        //void ReadTcpClientStream(object p_tcpClient)      // this in running in a separate thread
        //{
        //    try
        //    {
        //        TcpClient client = p_tcpClient as TcpClient;
        //       // BinaryReader br = new BinaryReader(client.GetStream());

        //        //T message = m_createMessageInstance(br);

        //        //T message = (new T()).DeserializeFrom(br);
        //        ////string strFromClient = br.ReadString();
        //        //Console.WriteLine(">" + DateTime.UtcNow.ToString("MM-dd HH:mm:ss") + $" Msg:{message.ID}, Param:{message.ParamStr}");  // user can quickly check from Console the messages
        //        //Utils.Logger.Info($"TcpMessageListener: Message ID:\"{ message.ID}\", ParamStr: \"{ message.ParamStr}\", ResponseFormat: \"{message.ResponseFormat}\"");
        //        //if (message.ResponseFormat == VirtualBrokerMessageResponseFormat.None)
        //        //{
        //        //    Utils.TcpClientDispose(client);
        //        //    client = null;
        //        //}

        //        m_messageQueue.Add(client);
        //    }
        //    catch (Exception e) // Background thread can crash application. A background thread does not keep the managed execution environment running.
        //    {
        //        Console.WriteLine($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
        //        Utils.Logger.Info($"Expected Exception. We don't rethrow it. Occurs daily when client VBroker VM server reboots. ReadTcpClientStream(BckgTh:{Thread.CurrentThread.IsBackground}). {e.Message}, InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : "null"));
        //    }

        //}

        public void StopTcpMessageListener()
        {
            Console.WriteLine("StopTcpMessageListener() exiting...");
            // you can finish current TcpConnections properly if it is important
            // to Dispose the TcpListener, this hack has to be used to do a Last, Final Connection: http://stackoverflow.com/questions/19220957/tcplistener-how-to-stop-listening-while-awainting-accepttcpclientasync
            TcpClient dummyClient = new TcpClient();
            dummyClient.ConnectAsync(m_privateIP, m_port).Wait();
            Console.WriteLine($"StopTcpMessageListener(). Is DummyClient connected: {dummyClient.Connected}");
            Utils.TcpClientDispose(dummyClient);

            Console.WriteLine("StopTcpMessageListener() exiting..");
            m_tcpListener.Stop();   // there is no Dispose() method
            Console.WriteLine("StopTcpMessageListener() exiting.");

            // Tasks create Background Threads always, so the TcpListenerLoop() is a Background thread, it will exits when main thread exits, which is OK.
        }


        //void ProcessMessage(TcpClient p_tcpClient, T p_message)
        //{
        //    switch (p_message.ID)
        //    {
        //        case VirtualBrokerMessageID.GetRealtimePrice:
        //            GetRealtimePrice(p_tcpClient, p_message);
        //            break;
        //    }

        //    if (p_message.ResponseFormat != VirtualBrokerMessageResponseFormat.None)    // if Processing needed Response to Client, we dispose here. otherwise, it was disposed before putting into processing queue
        //    {
        //        Utils.TcpClientDispose(p_tcpClient);
        //    }
        //}

        //private void GetRealtimePrice(TcpClient p_tcpClient, VirtualBrokerMessage p_message)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
