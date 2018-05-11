using System;
using RxCommon;
using System.Threading;
using SqCommon;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string runtimeConfig = "Unknown";
#if RELEASE
            runtimeConfig = "RELEASE";
# elif DEBUG
            runtimeConfig = "DEBUG";
#endif
            Console.WriteLine($"Hello VirtualBroker, v1.0.14 ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");
            Console.Title = "VirtualBroker v1.0.14";
            if (!RxUtils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            if (!Controller.IsRunningAsLocalDevelopment())
            {
                // Should we start VBroker at the weekend? Decision: Yes.  (because we need VBroker Realtime data on the weekend or we may want to trade Futures that are traded on the weekend)
                // Against it:                 // if it is Saturday, Sunday, we can think about not starting VBroker, because often IB makes maintenance on the weekends and IBGateway cannot connect, therefore VBroker will raise an error anyway.
                // However, if it is true that IB gives many false maintenance errors, we should find a solution there. Maybe report error only if it is weekdays, not weekends
                // however, don't consider USA holidays. It is better to start VBroker, because it may not be a UK, German, French holiday, so maybe a DAX trader would like to trade on that day   
                //DateTime dateNowInET = Utils.ConvertTimeFromUtcToEt(DateTime.UtcNow);
                //if (dateNowInET.DayOfWeek == DayOfWeek.Saturday || dateNowInET.DayOfWeek == DayOfWeek.Sunday)
                //{
                //    Utils.Logger.Info($"Assuming morning restart of VBroker every day!!!, if today is the weekend, there is no reason to start. Ending execution now.");
                //    return;
                //}
            }

            Utils.Configuration = Utils.InitConfigurationAndInitUtils((Utils.RunningPlatform() == Platform.Windows) ? "g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.VirtualBroker.NoGitHub.json" : "/home/ubuntu/SQ/Server/VirtualBroker/SQLab.VirtualBroker.NoGitHub.json");
            Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
            HealthMonitorMessage.InitGlobals(ServerIp.HealthMonitorPublicIp, HealthMonitorMessage.DefaultHealthMonitorServerPort);       // until HealthMonitor runs on the same Server, "localhost" is OK
            StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.

            try
            {
                Controller.g_controller.Init();

                string userInput = String.Empty;
                do
                {

                    userInput = DisplayMenu();
                    switch (userInput)
                    {
                        case "1":
                            Console.WriteLine("Hello. I am not crashed yet! :)");
                            Utils.Logger.Info("Hello. I am not crashed yet! :)");
                            break;
                        case "2":
                            Controller.g_controller.TestVbGatewayConnection();
                            break;
                        case "3":
                            Controller.g_controller.TestHealthMonitorListenerBySendingErrorFromVirtualBroker();
                            break;
                        case "4":
                            //Controller.g_controller.TestHardCrash();
                            Controller.g_controller.TestRealtimePriceService();
                            break;
                        case "5":
                            //Controller.g_controller.TestHardCrash();
                            //Controller.g_controller.EncogXORHelloWorld();
                            Controller.g_controller.TestSqlDb();
                            break;
                        case "6":
                            Console.WriteLine(Controller.g_controller.GetNextScheduleTimes(false).ToString());
                            break;
                        case "7":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(0);
                            break;
                        case "8":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(1);
                            break;
                        case "9":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(2);
                            break;
                        case "10":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(3);
                            break;
                    }

                } while (userInput != "11" && userInput != "ConsoleIsForcedToShutDown");

                Utils.Logger.Info("****** Main() END");
                Utils.MainThreadIsExiting.Set();
                Controller.g_controller.Exit();
                Utils.Logger.Exit();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendAsync($"Exception in VBroker Main Thread. Exception: '{ e.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromVirtualBroker).RunSynchronously();
            }

        }

        // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HealthMonitorMessage.SendAsync($"Exception in VBroker TaskScheduler_UnobservedTaskException. Exception: '{ e.Exception.ToStringWithShortenedStackTrace(400)}'", HealthMonitorMessageID.ReportErrorFromVirtualBroker).RunSynchronously();
            e.SetObserved();        //  preventing it from triggering exception escalation policy which, by default, terminates the process.

            Task senderTask = (Task)sender;
            if (senderTask != null)
            {
                Utils.Logger.Info($"TaskScheduler_UnobservedTaskException(): sender is a task. TaskId: {senderTask.Id}, IsCompleted: {senderTask.IsCompleted}, IsCanceled: {senderTask.IsCanceled}, IsFaulted: {senderTask.IsFaulted}, TaskToString(): {senderTask.ToString()}");
                if (senderTask.Exception == null)
                    Utils.Logger.Info("SenderTask.Exception is null");
                else
                    Utils.Logger.Info($"SenderTask.Exception {senderTask.Exception.ToString()}");
            }
            else
                Utils.Logger.Info("TaskScheduler_UnobservedTaskException(): sender is not a task.");
        }

        //If there is a Crash or Error, Catch and hadle it. AggregateIBGateway should Report it. HealthMonitor should know about it,
        //so send a message to HealthMonitor2, that calls my Phone and sends email.
        // "I shouldn't receive 20 email per day about 'Vbroker X was OK'; but I should receive 1 email only if there is a problem."
        internal static void StrongAssertMessageSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            // HealthMonitorMessage.SendAsync()..FireParallelAndForgetAndLogErrorTask() is not safe. Don't do it. If mean thread asserts and crashes the whole app instantenously, the background thread may not be able to finish sending the message to HealthMonitor. Use. *.RunSynchronously(); instead.
            HealthMonitorMessage.SendAsync($"Msg from VirtualBroker. StrongAssert Warning (if Severity is NoException, it is just a mild Warning. If Severity is ThrowException, that exception triggers a separate message to HealthMonitor as an Error). Severity: {p_msg.Severity}, Message: { p_msg.Message}, StackTrace: { p_msg.StackTrace}",
                (p_msg.Severity == Severity.NoException) ? HealthMonitorMessageID.ReportWarningFromVirtualBroker : HealthMonitorMessageID.ReportErrorFromVirtualBroker).RunSynchronously();
        }


        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;
            
            Utils.ConsoleWriteLine(ConsoleColor.Magenta, "------- VirtualBroker (type and press Enter) ------- ");
            Console.WriteLine("1.  Say Hello. Don't do anything. Check responsivenes");
            Console.WriteLine($"2.  Test IbGateway Connection on port={(int)GatewayUserPort.GyantalMain} (Gyantal user) with clientID=0");
            Console.WriteLine("3.  Test HealthMonitor by sending ErrorFromVirtualBroker");
            Console.WriteLine("4.  Test Realtime price service");
            Console.WriteLine("5.  Test SQL DB or Encog");
            //Console.WriteLine("5.  Test Encog");
            Console.WriteLine("6.  Show next schedule times");
            Console.WriteLine("7.  Elapse TaskShema (NeuralSniffer1) First Simulation Trigger");
            Console.WriteLine("8.  Elapse TaskShema (TAA) First Simulation Trigger");
            Console.WriteLine("9.  Elapse TaskShema (UberVxx) First Simulation Trigger");
            Console.WriteLine("10. Elapse TaskShema (HarryLong) First Simulation Trigger");
            Console.WriteLine("11. Exit gracefully (Avoid Ctrl-^C).");
            string result = null;
            try
            {
                result = Console.ReadLine();
            }
            catch (Exception e) // on Linux, if somebody closed the Terminal Window, Console.Readline() will throw an Exception with Message "Input/output error"
            {
                Utils.Logger.Info($"Console.ReadLine() Exception. Somebody closed the Terminal Window. Exception message: {e.Message}");
                return "ConsoleIsForcedToShutDown";
            }
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
