using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello. DotNetCoreTest BEGIN");

            string userInput = String.Empty;
            do
            {

                userInput = DisplayMenu();
                switch (userInput)
                {
                    case "1":
                        Controller.g_controller.TestHttpDownload();
                        break;
                    case "2":
                        //new HQEmail().SendOnMono(true);
                        Console.WriteLine("Hello. I am not crashed yet! :)");
                        //gLogger.Info("Hello. I am not crashed yet! :)");
                        break;
                }

            } while (userInput != "3" && userInput != "ConsoleIsForcedToShutDown");
        }

        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;

            Console.WriteLine("----DotNetCore Test    (type and press Enter)----");
            //Console.WriteLine();
            Console.WriteLine("1. Test HttpDownload()");
            Console.WriteLine("2. Say Hello. Don't do anything. Check responsivenes.");
            Console.WriteLine("3. Exit gracefully (Avoid Ctrl-^C).");
            string result = null;
            try
            {
                result = Console.ReadLine();
            }
            catch  // on Linux, of somebody closes the Terminal Window, Console.Readline() will throw an Exception with Message "Input/output error"
            {
                return "ConsoleIsForcedToShutDown";
            }
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
