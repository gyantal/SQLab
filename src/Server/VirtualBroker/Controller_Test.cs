using IBApi;
using SqCommon;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;     // this is the only timer available under DotNetCore
using System.Threading.Tasks;

namespace VirtualBroker
{
    public partial class Controller
    {

        internal void TestElapseFirstTaskFirstTriggerWithSimulation()
        {
            foreach (var taskSchema in g_taskSchemas)
            {
                foreach (var trigger in taskSchema.Triggers)
                {
                    object isSimulationObj;
                    if (trigger.TriggerSettings.TryGetValue(BrokerTaskSetting.IsSimulatedTrades, out isSimulationObj))
                    {
                        if ((bool)isSimulationObj)
                        {
                            trigger.Timer_Elapsed(null);
                            break;  // just elapse the first one
                        }
                    }
                }
            }
        }


        internal void TestHardCrash()
        {
            //// Don't do un-protected threads, like this. Because Exception will be noticed only at Garbage Collection.
            //Task taskNotCaughtImmediately = Task.Factory.StartNew(x => {  throw new Exception("Test Exception in a Task"); }, TaskCreationOptions.LongRunning);

            //// 1. Do Wait() and TaskScheduler.UnobservedTaskException will be called immediately
            //Task taskGood1 = Task.Factory.StartNew(x => { throw new Exception("Test Exception in a Task"); }, TaskCreationOptions.LongRunning);
            //taskGood1.Wait();

            // 2. Or don't do Wait(), but protect locally   (for ThreadPool.Worker the only way is to protect locally like this, so maybe get used to this approach)
            Task taskGood2 = Task.Factory.StartNew(x=> {
                try { throw new Exception("Test Exception in a Task"); }
                catch (Exception e) { HealthMonitorMessage.SendException("Task1 Thread", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker); }
            }, TaskCreationOptions.LongRunning);
        }


        internal async void TestHealthMonitorListenerBySendingErrorFromVirtualBroker()
        {
            // see HealthMonitorMessage.SendMassage for simpler application that will not read the response
            TcpClient client = new TcpClient();
            Task task = client.ConnectAsync("localhost", 52100);
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))) != task)
            {
                Console.WriteLine("Error: client.Connect() timeout.");
                return;
            }

            HealthMonitorMessage message = new HealthMonitorMessage()
            {
                ID = HealthMonitorMessageID.ReportErrorFromVirtualBroker,
                ParamStr = "Error reason here",
                ResponseFormat = HealthMonitorMessageResponseFormat.String
            };

            BinaryWriter bw = new BinaryWriter(client.GetStream());
            message.SerializeTo(bw);
            //bw.Write("I am VirtualBroker");

            if (message.ResponseFormat != HealthMonitorMessageResponseFormat.None)
            {
                BinaryReader br = new BinaryReader(client.GetStream());
                Console.WriteLine(br.ReadString());
            }
            Utils.TcpClientDispose(client);
        }


        internal void TestVbGatewayConnection()
        {

            Utils.Logger.Debug("TestVbGatewayConnection() BEGIN");
            // start c:\Jts\StartIBGateway.bat 
            // IBGateway this version works: Build 952.1a, Aug 18, 2015 3:38:07 PM  // c:\Jts\StartIBGateway.bat 
            // this works too. Stable: Build 952.2h, Jan 29, 2016 4:40:48 PM        // c:\Jts\952\jars\StartIBGateway.bat , or simple "javaw.exe -cp jts.jar;total.jar ibgateway.GWClient" command line works
            // Latest (not Stable) doesn't work c:\Jts\955\jars\StartIBGateway.bat  or simple "javaw.exe -cp jts4launch.jar;total.jar ibgateway.GWClient" command line doesn't work, although the ibgateway.GWClient is there. Buggy.

            // see for samples:  "g:\temp\_programmingTemp\TWS API_972.12(2016-02-26)\samples\CSharp\IBSamples\Program.cs" 

            BrokerWrapperIb testImpl = new BrokerWrapperIb();
            EClientSocket client = testImpl.ClientSocket;
            client.eConnect("127.0.0.1", 7496, 0, false);

            //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
            var reader = new EReader(client, testImpl.Signal);
            reader.Start();
            //Once the messages are in the queue, an additional thread need to fetch them
            new Thread(() =>
            {
                while (client.IsConnected())
                {
                    testImpl.Signal.waitForSignal();
                    reader.processMsgs();
                }
            })
            { IsBackground = true }.Start();

            /*************************************************************************************************************************************************/
            /* One (although primitive) way of knowing if we can proceed is by monitoring the order's nextValidId reception which comes down automatically after connecting. */
            /*************************************************************************************************************************************************/
            while (testImpl.NextOrderId <= 0) { }



            /*** Requesting managed accounts***/
            client.reqManagedAccts();
            /*** Requesting accounts' summary ***/
            Thread.Sleep(2000);
            client.reqAccountSummary(9001, "All", AccountSummaryTags.GetAllTags());
            /*** Subscribing to an account's information. Only one at a time! ***/

            Thread.Sleep(6000);
            Console.WriteLine("Disconnecting...");
            client.eDisconnect();
        }
    }
}
