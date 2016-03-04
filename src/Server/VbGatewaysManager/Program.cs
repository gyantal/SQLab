using System;
using RxCommon;
using System.Threading;
using SQCommon;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace VbGatewaysManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"ThId-{Thread.CurrentThread.ManagedThreadId}: VbGatewaysManager, v1.0.7");
            if (!RxUtils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info("****** Main() START");
            Utils.Configuration = Utils.InitConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.VbGatewaysManager.NoGitHub.json", "/home/ubuntu/SQ/Server/VbGatewaysManager/SQLab.VbGatewaysManager.NoGitHub.json");
            HealthMonitorMessage.InitGlobals("localhost", 52100);       // until HealthMonitor runs on the same Server, "localhost" is OK
            StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.

            try
            {
                Controller.g_controller.Start();

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
                            Controller.g_controller.TestHealthMonitorListenerBySendingErrorFromGatewaysManager();
                            break;
                        case "4":
                            Controller.g_controller.TestHardCrash();
                            break;
                    }

                } while (userInput != "5");

                Utils.Logger.Info("****** Main() END");
                Controller.g_controller.Exit();
                Utils.Logger.Exit();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("Main Thread", e);
            }
            
        }

        // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HealthMonitorMessage.SendException("TaskScheduler_UnobservedTaskException", e.Exception);
        }

        //If there is a Crash or Error, Catch and hadle it.AggregateGateway should Report it.HealthMonitor should know about it,
        //so send a message to HealthMonitor2, that calls my Phone and sends email.
        // "I shouldn't receive 20 email per day about 'Vbroker X was OK'; but I should receive 1 email only if there is a problem."
        static DateTime gLastStrongAssertMessageTime = DateTime.MinValue;
        internal static void StrongAssertMessageSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            if ((DateTime.UtcNow - gLastStrongAssertMessageTime).TotalMinutes > 30)   // don't send it in every minute, just after 30 minutes
            {
                if (!(new HealthMonitorMessage() { ID = HealthMonitorMessageID.ReportErrorFromVbGatewaysManager,
                    ParamStr = $"StrongAssert occured in VbGatewaysManager. Severity: {p_msg.Severity}, Message { p_msg.Message}, StackTrace: { p_msg.StackTrace}",
                    ResponseFormat = HealthMonitorMessageResponseFormat.None
                }.SendMessage()))
                {
                    Utils.Logger.Error("Error in sending HealthMonitorMessage to Server.");
                }

                gLastStrongAssertMessageTime = DateTime.UtcNow;
            }
        }

        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;

            ConsoleColor previousForeColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("----VbGatewaysManager (type and press Enter)----");
            Console.ForegroundColor = previousForeColour;
            Console.WriteLine("1. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("2. Test IbGateway Connection");
            Console.WriteLine("3. Test HealthMonitor TcpListener Communication by sending ErrorFromVbGatewaysManager");
            Console.WriteLine("4. Crash in a Task thread (always Background thread) and let's see if HealthMonitor calls the phone");
            Console.WriteLine("5. Exit gracefully (Avoid Ctrl-^C).");
            var result = Console.ReadLine();
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
