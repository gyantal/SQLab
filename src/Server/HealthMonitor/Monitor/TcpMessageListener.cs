using SQCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        TcpListener m_tcpListener;
        //static Semaphore Go = new Semaphore(0, 1);
        //ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>> m_messageQueue = new ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>>();
        BlockingCollection<Tuple<TcpClient, HealthMonitorMessage>> m_messageQueue = new BlockingCollection<Tuple<TcpClient, HealthMonitorMessage>>(new ConcurrentQueue<Tuple<TcpClient, HealthMonitorMessage>>());


        void StartTcpMessageListenerThreads()
        {
            // start 1 thread to listen TCP traffic (that can create many threads for reading message)
            Task tcpListenerTask = Task.Factory.StartNew(TcpMessageListenerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.

            // start 2 threads max to process Messages (limit to not overwhelm the Server CPU)
            Task msgProcessing1 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
            Task msgProcessing2 = Task.Factory.StartNew(MessageProcessorWorkerLoop, TaskCreationOptions.LongRunning);  // it is a Background Thread. Checked. Tasks create Background Threads always.
        }

        void MessageProcessorWorkerLoop()
        {
            try
            {
                while (true)
                {
                    var packet = m_messageQueue.Take(); //this blocks if there are no items in the queue.
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            var data = (Tuple<TcpClient, HealthMonitorMessage>)state;
                            ProcessMessage(data.Item1, data.Item2);
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
                throw;
            }
        }

        // http://stackoverflow.com/questions/7690520/c-sharp-networking-tcpclient
        void TcpMessageListenerLoop()
        {
            try
            {
                m_tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 52100);    // largest port number: 65535 
                m_tcpListener.Start();
                while (true)
                {
                    TcpClient client = m_tcpListener.AcceptTcpClientAsync().Result;        // Task.Result is blocking. OK.
                    new Thread((x) => ReadTcpClientStream(x)).Start(client);    // read the BinaryReader() and deserialize in separate thread, so not block the TcpListener loop
                }
            }
            catch (Exception e)
            {
                if (m_mainThreadExitsResetEvent.IsSet)
                    return; // if App is exiting gracefully, this Exception is not a problem
                Utils.Logger.Error("Exception caught in TcpMessageListenerLoop. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                throw;  // else, rethrow
            }
        }

        void ReadTcpClientStream(object p_tcpClient)      // this in running in a separate thread
        {
            try
            {
                TcpClient client = p_tcpClient as TcpClient;
                BinaryReader br = new BinaryReader(client.GetStream());
                HealthMonitorMessage message = (new HealthMonitorMessage()).DeserializeFrom(br);
                //string strFromClient = br.ReadString();
                string logStr = $"TcpMessageListener: Message ID:'{ message.ID}', ParamStr: '{ message.ParamStr}', ResponseFormat: {message.ResponseFormat}'";
                Utils.Logger.Info(logStr);
#if DEBUG
                Console.WriteLine(logStr);  // temporary only, in Development, not in Release
#endif
                if (message.ResponseFormat == HealthMonitorMessageResponseFormat.None)
                {
                    Utils.TcpClientDispose(client);
                    client = null;
                }

                m_messageQueue.Add(new Tuple<TcpClient, HealthMonitorMessage>(client, message));
            }
            catch (Exception e)
            {
                Utils.Logger.Error("Exception caught in ReadTcpClientStream. " + e.Message + " ,InnerException: " + ((e.InnerException != null) ? e.InnerException.Message : ""));
                throw;
            }
            
        }

        void StopTcpMessageListener()
        {
            // you can finish current TcpConnections properly if it is important
            m_tcpListener.Stop();   // there is no Dispose() method
            // Tasks create Background Threads always, so the TcpMessageListenerLoop() is a Background thread, it will exits when main thread exits, which is OK.
        }

    }
}
