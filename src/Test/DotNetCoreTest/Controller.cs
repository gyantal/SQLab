using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCoreTest
{
    public class Controller
    {
        static public Controller g_controller = new Controller();

        internal void TestHttpDownload()
        {
            Console.WriteLine("TestHttpDownload() BEGIN");

            try
            {
                var biduDelayedPriceCSV = new HttpClient().GetStringAsync("http://download.finance.yahoo.com/d/quotes.csv?s=BIDU&f=sl1d1t1c1ohgv&e=.csv").Result;

                Console.WriteLine("HttpClient().GetStringAsync returned: " + biduDelayedPriceCSV.Length);
                Console.WriteLine("Downloaded string: " + biduDelayedPriceCSV);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
              

            Console.WriteLine("TestHttpDownload() END");
        }
    }
}
