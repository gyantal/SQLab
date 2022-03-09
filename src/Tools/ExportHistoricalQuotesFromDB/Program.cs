using DbCommon;
using SqCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExportHistoricalQuotesFromDB
{
    public class DailyData
    {
        public DateTime Date { get; set; }
        public double AdjClosePrice { get; set; }
    }

    class Program
    {
        static Dictionary<RunningEnvironment, string> gSensitiveDataFullPath  = new Dictionary<RunningEnvironment, string>()
                {
                    { RunningEnvironment.LinuxServer, "/home/ubuntu/SQ/WebServer/SQLab/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsAGy, "c:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_desktop, "d:/GDrive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" },
                    { RunningEnvironment.WindowsBL_laptop, "d:/GDrive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.WebServer.SQLab.NoGitHub.json" }
                };

        static void Main(string[] args)
        {
            Console.WriteLine("Example usage:  <no space between tickers in tickerlist> <output folder without ending backslash (\\)>");
            Console.WriteLine("ExportHistoricalQuotesFromDB ticker1,ticker2 G:\\temp");
            Console.WriteLine("If no tickers are given, SVXY.SQ is assumed. If no folder is given, files are created into the 'current' folder. ");
            if (!Utils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START");
            Utils.Configuration = Utils.InitConfigurationAndInitUtils(gSensitiveDataFullPath[Utils.RunningEnv()]);
          
            var tickersArr = (args[0] ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var tickers = tickersArr.ToList();
            if (tickers.Count == 0 || args[0]== "-")
            {
                //tickers = new List<string> { "SVXY" };
                //tickers = new List<string> { "SVXY.SQ", "SVXY" };
                tickers = new List<string> { "SVXY!Light0.5x.SQ", "SVXY.SQ", "SVXY"};
                //tickers = new List<string> {"SVXY!Light0.5x.SQ",  "SVXY.SQ",  "SVXY",  };
            }

            string folderFullPath = (args.Length <= 1) ? null : args[1];
            if (String.IsNullOrEmpty(folderFullPath))
                folderFullPath = Directory.GetCurrentDirectory();  // does not end with a backslash (\).

            Console.WriteLine($"Exporting tickers '{String.Join(", ", tickers.ToArray())}' into folder '{folderFullPath}'...");
            Console.WriteLine("Waiting for database...");
            ExportTicker(tickers, folderFullPath);
            Console.WriteLine("Export finished. Press any key...");
            Console.ReadKey();
        }

        private static void ExportTicker(List<string> p_tickers, string p_folderFullPath)
        {
            DateTime startDateUtc = DateTime.MinValue;
            DateTime endDateUtc = DateTime.MaxValue;
           
            ushort sqlReturnedColumns = QuoteRequest.TDOHLCV;       // QuoteRequest.All or QuoteRequest.TDOHLCVS
            var sqlReturnTask = SqlTools.GetHistQuotesAsync(startDateUtc, endDateUtc, p_tickers, sqlReturnedColumns);
            var sqlReturnData = sqlReturnTask.Result;
            var sqlReturn = sqlReturnData.Item1;

            foreach (var ticker in p_tickers)
            {
                string csvFilePath = Path.Combine(p_folderFullPath, ticker + "_DOHLCV.csv");
                Console.WriteLine($"Writing '{ticker}_DOHLCV.csv'...");
                FileStream fs = new FileStream(csvFilePath, FileMode.Create);
                var m_file = new StreamWriter(fs);
                m_file.AutoFlush = true;    // no manual flush is required

                IEnumerable<object[]> mergedRows = SqlTools.GetTickerAndBaseTickerRows(sqlReturn, ticker);
                foreach (var row in mergedRows)
                {
                    // row[2] is object(double) if it is a stock (because Adjustment multiplier), and object(float) if it is Indices. However Convert.ToDouble(row[2]) would convert 16.66 to 16.6599999
                    string volume = (row.Length < 7 || row[6] == null) ? "NULL" : row[6].ToString();
                    m_file.WriteLine($"{row[1]},{(double)Convert.ToDecimal(row[2])},{(double)Convert.ToDecimal(row[3])},{(double)Convert.ToDecimal(row[4])},{(double)Convert.ToDecimal(row[5])},{volume}");
                }


                m_file.Flush();
                m_file.Dispose();
            }
        }

     

    }
}
