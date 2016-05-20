﻿using AIFH_Vol3.Core.General.Data;
using AIFH_Vol3_Core.Core.ANN;
using AIFH_Vol3_Core.Core.ANN.Activation;
using AIFH_Vol3_Core.Core.ANN.Train;
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
        /// <summary>
        ///     The input necessary for XOR.
        /// </summary>
        public static double[][] XOR_INPUT =
        {
            new[] {0.0, 0.0},
            new[] {1.0, 0.0},
            new[] {0.0, 1.0},
            new[] {1.0, 1.0}
        };

        /// <summary>
        ///     The ideal data necessary for XOR.
        /// </summary>
        public static double[][] XOR_IDEAL =
        {
            new[] {0.0},
            new[] {1.0},
            new[] {1.0},
            new[] {0.0}
        };

        public void EncogXORHelloWorld()
        {
            // 3. Source code from the new book "Artificial Intelligence for Humans",  https://github.com/jeffheaton/aifh/tree/26b07c42a3870277fe201356467bce7b4213b604

            var network = new BasicNetwork();
            network.AddLayer(new BasicLayer(null, true, 2));
            network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 5));
            network.AddLayer(new BasicLayer(new ActivationSigmoid(), false, 1));
            network.FinalizeStructure();
            network.Reset();

            var trainingData = BasicData.ConvertArrays(XOR_INPUT, XOR_IDEAL);

            // train the neural network
            var train = new ResilientPropagation(network, trainingData);

            var epoch = 1;

            do
            {
                train.Iteration();
                Console.WriteLine("Epoch #" + epoch + " Error:" + train.LastError);
                epoch++;
            } while (train.LastError > 0.01);

            // test the neural network
            Console.WriteLine("Neural Network Results:");
            for (var i = 0; i < XOR_INPUT.Length; i++)
            {
                var output = network.ComputeRegression(XOR_INPUT[i]);
                Console.WriteLine(string.Join(",", XOR_INPUT[i])
                                  + ", actual=" + string.Join(",", output)
                                  + ",ideal=" + string.Join(",", XOR_IDEAL[i]));
            }
        }


        internal void TestEncogSimilarThatIsUsedByVirtualBroker()
        {
            int p_nNeurons = 5;
            int p_lookbackWindowSize = 200;
            int p_maxEpoch = 5000;
            int inputDim = 0; // can be 3 or 5+1+1
            double[] testInput = new double[inputDim];

            // extract original data. We have to exclude outliers and normalize later
            double[][] nnInput = new double[p_lookbackWindowSize][];
            double[][] nnOutput = new double[p_lookbackWindowSize][];

            // Matlab emulation network
            // consider the newFF() in Matlab:
            // 1. The input is not a layer; no activation function, no bias
            // 2. The middle layer has a bias, and tansig transfer function
            // 3. The output is a layer; having a bias (I checked); but has Linear activation  (in the default case); in the Matlab book, there are examples with tansig ouptput layers too
            // Jeff use a similar one
            //  I've been using a linear activation function on the output layer, and sigmoid or htan on the input and hidden lately for my prediction nets, 
            // and getting lower error rates than a uniform activation function. (uniform: using the same on every layer)
            BasicNetwork network = new BasicNetwork();
            //var ffPattern = new FeedForwardPattern();  maybe I don't need that Logic settings
            //network.Logic = new Encog.Neural.Networks.Logic.FeedforwardLogic(); // the default is SimpleRecurrentLogic; but FeedforwardLogic() is faster, simpler
            network.AddLayer(new BasicLayer(new ActivationLinear(), false, inputDim)); // doesn't matter what is here. nor the act.function, neither the bias are used
            network.AddLayer(new BasicLayer(new ActivationTANH(), true, p_nNeurons));
            network.AddLayer(new BasicLayer(new ActivationLinear(), true, 1));
            //network.Structure.FinalizeStructure();  // after this, the Layers.BiasWeight and Synapses.Weights are zero
            network.FinalizeStructure();

            //Utils.Input1DVisualizer(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { 0.0, Double.MaxValue }, new double[] { });
            //Utils.Input1DVisualizer(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { 0.5, 1.5, 2.5, 3.5, 4.5}, new double[] { }); 

            double[] agyTestForecast = new double[1];
            double[] agyTestTrainError = new double[1];
            ResilientPropagation train = null;
            for (int i = 0; i < agyTestForecast.Length; i++)
            {
                network.Reset();
                //network.Reset(new Random((int)DateTime.Now.Ticks).Next());
                //network.Reset(new Random(123));    // randomizes  Layers.BiasWeight and Synapses.Weights; if initialweights are left zero; they will be zeroWeights after Resilient and Backprog training. Only the last biasWeight will be non-zero. means: the output will be the same regardless of the input; Bad.

                // train the neural network
                var trainingData = BasicData.ConvertArrays(nnInput, nnOutput);
                train = new ResilientPropagation(network, trainingData);

                //INeuralDataSet trainingSet = new BasicNeuralDataSet(nnInput, nnOutput);
                //train = new ResilientPropagation(network, trainingSet); // err remained=0.99; with 100 random samples: accumulatedValue=0.69, 0.09, 0.57; up/down ratio stayed at 0.52 in all cases (ratio is stable)
                //train.NumThreads = 1; // default is 0; that means Encog can determine how many; (but I specificaly want 1 threads train, because I run 4 backtests in parallel; so I use the CPUs anyway)

                int epoch = 1;
                do
                {
                    train.Iteration();
                    //Utils.Logger.Info("Epoch #" + epoch + " Error:" + train.Error);
                    epoch++;
                } while ((epoch <= p_maxEpoch) && (train.LastError > 0.001));      // epoch = 5000?



                //INeuralData inputData = new BasicNeuralData(testInput);
                //INeuralData outputData = network.Compute(inputData);
                //double[] ouput = outputData.Data;
                //NeuralSnifferUtil.DenormalizeWithoutMovingCenterStd(ouput, 0, outputMultiplier);
                //double forecast = outputData[0];
                ////Utils.Logger.Info(@"Real%change: " + p_barChanges[p_iRebalance] * 100 + "%, Network forecast: " + outputData.ToString() + "%");
                //agyTestForecast[i] = forecast;
                //agyTestTrainError[i] = train.Error;
            }

        }

        internal void TestElapseFirstTriggerWithSimulation(int p_taskSchemaInd)
        {
            if (g_taskSchemas.Count <= p_taskSchemaInd)
            {
                Console.WriteLine("No such taskschema.");
                return;
            }

            foreach (var trigger in g_taskSchemas[p_taskSchemaInd].Triggers)
            {
                object isSimulationObj;
                if (trigger.TriggerSettings.TryGetValue(BrokerTaskSetting.IsSimulatedTrades, out isSimulationObj))
                {
                    if ((bool)isSimulationObj)
                    {
                        ((VbTrigger)trigger).Timer_Elapsed(null);
                        break;  // just elapse the first one
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
            Task task = client.ConnectAsync(HealthMonitorMessage.HealthMonitorServerPublicIpForClients, HealthMonitorMessage.DefaultHealthMonitorServerPort);
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