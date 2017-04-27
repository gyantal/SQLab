using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ReproSqlConnectionUnobservedException
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;     // You have to attach this ! Without it you will not see the Error.
            string connStringGood = "Server=tcp:ZZZ.database.windows.net,1433;Database=ZZZ;User ID=ZZZ@ZZZ;Password=ZZZ;Connection Timeout=600;";
            string connStringBad = "Server=tcp:ZZZ.database.windows.net;Database=ZZZ;User ID=ZZZ@ZZZ;Password=ZZZ;Connection Timeout=600;";

            string userInput = String.Empty;
            do
            {
                Console.WriteLine("--------- Select number and press ENTER ----------");
                Console.WriteLine("1. Try connection with good ConnStr");
                Console.WriteLine("2. Try connection with bad ConnStr");
                Console.WriteLine("3. Exit");
                userInput = Console.ReadLine();
                if (userInput == "3")
                    break;

                var connString = (userInput == "1") ? connStringGood : connStringBad; 
                var conn = new SqlConnection(connString);
                conn.Open();
                conn.Dispose();
                Console.WriteLine("Connection was openned and closed.");

                GC.Collect();
                GC.WaitForPendingFinalizers();
            } while (true);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("Error. TaskScheduler_UnobservedTaskException was called !!! (This only happens on Linux (not Windows) and only when default SQL port number is not given.");
        }
    }
}