using Microsoft.AspNetCore.Mvc;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SQLab.Controllers
{
    struct VixCentralRec
    {
        public DateTime Date;
        public int FirstMonth;
        public double F1;
        public double F2;
        public double F3;
        public double F4;
        public double F5;
        public double F6;
        public double F7;
        public double F8;
        public double STCont;
        public double LTCont;

        public DateTime NextExpiryDate;
        public int F1expDays;
        public int F2expDays;
        public int F3expDays;
        public int F4expDays;
        public int F5expDays;
        public int F6expDays;
        public int F7expDays;
        public int F8expDays;

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd")},{FirstMonth}, {F1:F3}, {F2:F3}, {F3:F3}, {F4:F3}, {F5:F3}, {F6:F3}, {F7:F3}, {F8:F3}, {STCont:P2}, {LTCont:P2}, {NextExpiryDate.ToString("yyyy-MM-dd")}, {F1expDays}, {F2expDays}, {F3expDays}, {F4expDays}, {F5expDays}, {F6expDays}, {F7expDays}, {F8expDays} ";
        }
    }

    struct Data2Charts
    {
        public int ExpDays;
        public double FutPrices;
        public double FutSpreads;
        public double STCont;
    }

    //--[Route("[controller]")]
    public class VixFuturesAnalyserDataController : Controller
    {
#if !DEBUG
        [Authorize]
#endif
        public ActionResult Index()
        {
            return Content(GetStr(), "text/html");
        }

        
        public string GetStr()
        {
            
            Stopwatch stopWatchTotal = Stopwatch.StartNew();

            Stopwatch stopWatch1 = Stopwatch.StartNew();

            //Downloading historical data from vixcentral.com.
            string webpageHist;
            bool isOk = Utils.DownloadStringWithRetry(out webpageHist, "http://vixcentral.com/historical/?days=10000", 3, TimeSpan.FromSeconds(2), true);
            if (!isOk)
                return "Error in historical data";

            //Downloading live data from vixcentral.com.
            string webpageLive;
            bool isOkLive = Utils.DownloadStringWithRetry(out webpageLive, "http://vixcentral.com", 3, TimeSpan.FromSeconds(2), true);
            if (!isOkLive)
                return "Error in live data";
            stopWatch1.Stop();

            //Selecting data from live data string.
            Stopwatch stopWatch2 = Stopwatch.StartNew();
            string[] tableRows = webpageHist.Split(new string[] { "<tr>", "</tr>" }, StringSplitOptions.RemoveEmptyEntries);
            int nHistoricalRec = tableRows.Length - 2;

            string liveFuturesDataDT = System.String.Empty;
            string liveFuturesDataDate = System.String.Empty;
            string liveFuturesDataTime = System.String.Empty;
            string liveFuturesData = System.String.Empty;
            string liveFuturesNextExp = System.String.Empty;

            int startPosLiveDate = webpageLive.IndexOf("var time_data_var=['") + "var time_data_var=['".Length;
            int startPosLive = webpageLive.IndexOf("var last_data_var=[",startPosLiveDate) + "var last_data_var=[".Length;
            int endPosLive = webpageLive.IndexOf("];for(var i=0;i<last_data_var.length;i++){if(last_data_var[i]<1)last_data_var[i]=null;}", startPosLive);
            int nextExpLiveMonth = webpageLive.IndexOf("var mx=['", endPosLive) + "var mx=['".Length;
            liveFuturesDataDT = webpageLive.Substring(startPosLiveDate, 16);
            liveFuturesNextExp = webpageLive.Substring(nextExpLiveMonth, 3);
            liveFuturesData = webpageLive.Substring(startPosLive, endPosLive - startPosLive);
            
            stopWatch2.Stop();

            liveFuturesDataDate = liveFuturesDataDT.Substring(0,10);
            liveFuturesDataTime = liveFuturesDataDT.Substring(11, 5) + " EST";

            string[] liveFuturesPrices = liveFuturesData.Split(new string[] { ","}, StringSplitOptions.RemoveEmptyEntries);
            int lengthLiveFuturesPrices = liveFuturesPrices.Length;

            string[] monthsNumList = {"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"};
            int monthsNum = Array.IndexOf(monthsNumList,liveFuturesNextExp)+1;
          

            DateTime liveDateTime;
            string liveDate = System.String.Empty;
            liveDateTime = DateTime.Parse(liveFuturesDataDate);
            liveDate = liveDateTime.ToString("yyyy-MM-dd");


            string[] firstTableCells = tableRows[2].Split(new string[] { "<td>", "</td>" }, StringSplitOptions.RemoveEmptyEntries);
            DateTime histStartDay;
            string histStartDate = System.String.Empty;
            histStartDay = DateTime.Parse(firstTableCells[0]);
            histStartDate = histStartDay.ToString("yyyy-MM-dd");
            bool isExtraDay = !string.Equals(liveDate, histStartDate);

            //Sorting historical data.
            int nRec = (isExtraDay) ? nHistoricalRec + 1 :nHistoricalRec;
            VixCentralRec[] vixCentralRec = new VixCentralRec[nRec-2];
            //--List<VixCentralRec> vixRec2 = new List<VixCentralRec>();
            //--vixRec2.Add(new VixCentralRec());

            for (int iRows = 2; iRows < tableRows.Length - 2; iRows++)
            {
                string[] tableCells = tableRows[iRows].Split(new string[] { "<td>", "</td>" }, StringSplitOptions.RemoveEmptyEntries);
                int iRec = (isExtraDay) ? iRows - 1 : iRows - 2;
                vixCentralRec[iRec].Date = DateTime.Parse(tableCells[0]);
                vixCentralRec[iRec].FirstMonth = Int32.Parse(tableCells[1]);
                vixCentralRec[iRec].F1 = Double.Parse(tableCells[2]);
                vixCentralRec[iRec].F2 = Double.Parse(tableCells[3]);
                vixCentralRec[iRec].F3 = Double.Parse(tableCells[4]);
                vixCentralRec[iRec].F4 = Double.Parse(tableCells[5]);
                vixCentralRec[iRec].F5 = Double.Parse(tableCells[6]);
                vixCentralRec[iRec].F6 = Double.Parse(tableCells[7]);
                vixCentralRec[iRec].F7 = Double.Parse(tableCells[8]);
                vixCentralRec[iRec].F8 = (tableCells[9]=="0")? vixCentralRec[iRec].F7 : Double.Parse(tableCells[9]);
                vixCentralRec[iRec].STCont = vixCentralRec[iRec].F2/ vixCentralRec[iRec].F1 -1;
                vixCentralRec[iRec].LTCont = vixCentralRec[iRec].F7 / vixCentralRec[iRec].F4 - 1;
            }
            
            if (isExtraDay)
            {
                vixCentralRec[0].Date = DateTime.Parse(liveDate);
                vixCentralRec[0].FirstMonth = monthsNum;
                vixCentralRec[0].F1 = Double.Parse(liveFuturesPrices[0]);
                vixCentralRec[0].F2 = Double.Parse(liveFuturesPrices[1]);
                vixCentralRec[0].F3 = Double.Parse(liveFuturesPrices[2]);
                vixCentralRec[0].F4 = Double.Parse(liveFuturesPrices[3]);
                vixCentralRec[0].F5 = Double.Parse(liveFuturesPrices[4]);
                vixCentralRec[0].F6 = Double.Parse(liveFuturesPrices[5]);
                vixCentralRec[0].F7 = Double.Parse(liveFuturesPrices[6]);
                vixCentralRec[0].F8 = (lengthLiveFuturesPrices == 8 ) ? Double.Parse(liveFuturesPrices[7]) : 0;
                vixCentralRec[0].STCont = vixCentralRec[0].F2 / vixCentralRec[0].F1 - 1;
                vixCentralRec[0].LTCont = vixCentralRec[0].F7 / vixCentralRec[0].F4 - 1;

            }
                    
            //Calculating futures expiration dates.
            var firstDataDay = vixCentralRec[nRec - 3].Date;
            int firstDataYear = firstDataDay.Year;
            string firstData = firstDataDay.ToString("yyyy-MM-dd");

            var lastDataDay = vixCentralRec[0].Date;
            int lastDataYear = lastDataDay.Year;
            string lastData = lastDataDay.ToString("yyyy-MM-dd");

            var lengthExps = (lastDataYear - firstDataYear + 2) * 12;
            int[,] expDatesDat = new int[lengthExps,2];
            //--int[][] expDatesDat = new int[lengthExps,2];

            expDatesDat[0,0] = lastDataYear + 1;
            expDatesDat[0,1] = 12;

            for (int iRows = 1; iRows < expDatesDat.GetLength(0); iRows++)
            {
                decimal f = iRows / 12;
             expDatesDat[iRows,0] = lastDataYear - Decimal.ToInt32(Math.Floor(f))+1;
             expDatesDat[iRows,1] = 12 - iRows % 12;
            }

            DateTime[] expDates = new DateTime[expDatesDat.GetLength(0)];
            for (int iRows = 0; iRows < expDates.Length; iRows++)
            {
                DateTime thirdFriday = new DateTime(expDatesDat[iRows,0], expDatesDat[iRows,1], 15);
                while (thirdFriday.DayOfWeek != DayOfWeek.Friday)
                {
                    thirdFriday = thirdFriday.AddDays(1);
                }
                expDates[iRows] = thirdFriday.AddDays(-30);
                if (expDates[iRows]==DateTime.Parse("2014-03-19"))
                {
                    expDates[iRows] = DateTime.Parse("2014-03-18");
                }
            }

            //Calculating number of calendar days until expirations.
            for (int iRec = 0; iRec < vixCentralRec.Length; iRec++)
            {
                int index1 = Array.FindIndex(expDates, item => item <= vixCentralRec[iRec].Date);
                vixCentralRec[iRec].NextExpiryDate = expDates[index1-1];
                vixCentralRec[iRec].F1expDays = (expDates[index1 - 1] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F2expDays = (expDates[index1 - 2] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F3expDays = (expDates[index1 - 3] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F4expDays = (expDates[index1 - 4] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F5expDays = (expDates[index1 - 5] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F6expDays = (expDates[index1 - 6] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F7expDays = (expDates[index1 - 7] - vixCentralRec[iRec].Date).Days;
                vixCentralRec[iRec].F8expDays = (vixCentralRec[0].F8 > 0) ? (expDates[index1 - 8] - vixCentralRec[iRec].Date).Days:0;
            }
            
            string ret = Processing(vixCentralRec, expDates, liveDate, liveFuturesDataTime);

            stopWatchTotal.Stop();

            Utils.Logger.Info($"VixFuturesAnalyserDataController(): Benchmark times. VixCentral: {stopWatch1.Elapsed.TotalMilliseconds}ms, Step2: {stopWatch2.Elapsed.TotalMilliseconds}ms, Total: {stopWatchTotal.Elapsed.TotalMilliseconds}ms");

            return ret;

            //--return new string[] { tCells[0][0], "value2" };
        }

        private string Processing(VixCentralRec[] p_vixCentralRec, DateTime[] p_expDates, string p_liveDate, string p_liveFuturesDataTime)
        {
            //Calculating dates to html.           
            DateTime timeNowET = Utils.ConvertTimeFromUtcToEt(DateTime.UtcNow);
            
            DateTime firstDataDay = p_vixCentralRec[p_vixCentralRec.Length-1].Date;
            string firstDataDate = firstDataDay.ToString("yyyy-MM-dd");
            DateTime lastDataDay = p_vixCentralRec[0].Date;
            string lastDataDate = lastDataDay.ToString("yyyy-MM-dd");
            DateTime prevDataDay = p_vixCentralRec[1].Date;
            string prevDataDate = prevDataDay.ToString("yyyy-MM-dd");

            //Creating the current data array (prices and spreads).
            double[] currData = new double[17];
            currData[0] = p_vixCentralRec[0].F1;
            currData[1] = p_vixCentralRec[0].F2;
            currData[2] = p_vixCentralRec[0].F3;
            currData[3] = p_vixCentralRec[0].F4;
            currData[4] = p_vixCentralRec[0].F5;
            currData[5] = p_vixCentralRec[0].F6;
            currData[6] = p_vixCentralRec[0].F7;
            currData[7] = p_vixCentralRec[0].F8;
            currData[8] = p_vixCentralRec[0].STCont;
            currData[9] = p_vixCentralRec[0].LTCont;
            currData[10] = p_vixCentralRec[0].F2 - p_vixCentralRec[0].F1;
            currData[11] = p_vixCentralRec[0].F3 - p_vixCentralRec[0].F2;
            currData[12] = p_vixCentralRec[0].F4 - p_vixCentralRec[0].F3;
            currData[13] = p_vixCentralRec[0].F5 - p_vixCentralRec[0].F4;
            currData[14] = p_vixCentralRec[0].F6 - p_vixCentralRec[0].F5;
            currData[15] = p_vixCentralRec[0].F7 - p_vixCentralRec[0].F6;
            currData[16] = (p_vixCentralRec[0].F8 > 0) ? p_vixCentralRec[0].F8 - p_vixCentralRec[0].F7:0;

            //Creating the current days to expirations array.
            double[] currDataDays = new double[17];
            currDataDays[0] = p_vixCentralRec[0].F1expDays;
            currDataDays[1] = p_vixCentralRec[0].F2expDays;
            currDataDays[2] = p_vixCentralRec[0].F3expDays;
            currDataDays[3] = p_vixCentralRec[0].F4expDays;
            currDataDays[4] = p_vixCentralRec[0].F5expDays;
            currDataDays[5] = p_vixCentralRec[0].F6expDays;
            currDataDays[6] = p_vixCentralRec[0].F7expDays;
            currDataDays[7] = (p_vixCentralRec[0].F8>0)? p_vixCentralRec[0].F8expDays:0;
            currDataDays[8] = p_vixCentralRec[0].F1expDays;
            currDataDays[9] = p_vixCentralRec[0].F4expDays;
            currDataDays[10] = p_vixCentralRec[0].F1expDays;
            currDataDays[11] = p_vixCentralRec[0].F2expDays;
            currDataDays[12] = p_vixCentralRec[0].F3expDays;
            currDataDays[13] = p_vixCentralRec[0].F4expDays;
            currDataDays[14] = p_vixCentralRec[0].F5expDays;
            currDataDays[15] = p_vixCentralRec[0].F6expDays;
            currDataDays[16] = (p_vixCentralRec[0].F8 > 0) ? p_vixCentralRec[0].F7expDays:0;

            //Creating the data array of previous day (prices and spreads).
            double[] prevData = new double[17];
            prevData[0] = (p_vixCentralRec[0].F1expDays- p_vixCentralRec[1].F1expDays <0) ?p_vixCentralRec[1].F1: p_vixCentralRec[1].F2;
            prevData[1] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F2 : p_vixCentralRec[1].F3;
            prevData[2] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F3 : p_vixCentralRec[1].F4;
            prevData[3] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F4 : p_vixCentralRec[1].F5;
            prevData[4] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F5 : p_vixCentralRec[1].F6;
            prevData[5] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F6 : p_vixCentralRec[1].F7;
            prevData[6] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F7 : p_vixCentralRec[1].F8;
            prevData[7] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? ((p_vixCentralRec[0].F8 > 0)? p_vixCentralRec[1].F8 :0 ): 0;
            prevData[8] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].STCont : p_vixCentralRec[1].F3/p_vixCentralRec[1].F2-1; 
            prevData[9] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].LTCont : p_vixCentralRec[1].F8 / p_vixCentralRec[1].F5 - 1; 
            prevData[10] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F2 - p_vixCentralRec[1].F1 : p_vixCentralRec[1].F3 - p_vixCentralRec[1].F2; 
            prevData[11] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F3 - p_vixCentralRec[1].F2 : p_vixCentralRec[1].F4 - p_vixCentralRec[1].F3;
            prevData[12] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F4 - p_vixCentralRec[1].F3 : p_vixCentralRec[1].F5 - p_vixCentralRec[1].F4;
            prevData[13] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F5 - p_vixCentralRec[1].F4 : p_vixCentralRec[1].F6 - p_vixCentralRec[1].F5;
            prevData[14] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F6 - p_vixCentralRec[1].F5 : p_vixCentralRec[1].F7 - p_vixCentralRec[1].F6;
            prevData[15] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? p_vixCentralRec[1].F7 - p_vixCentralRec[1].F6 : p_vixCentralRec[1].F8 - p_vixCentralRec[1].F7;
            prevData[16] = (p_vixCentralRec[0].F1expDays - p_vixCentralRec[1].F1expDays < 0) ? ((p_vixCentralRec[0].F8 > 0) ? p_vixCentralRec[1].F8 - p_vixCentralRec[1].F7 :0) : 0;

            //Creating the difference of current and previous data array (prices and spreads).
            double[] currDataDiff = new double[17];
            for (int iRow = 0; iRow < currDataDiff.Length; iRow++)
            {
                currDataDiff[iRow] = currData[iRow] - prevData[iRow];
            }
            
            //Calculating the number of days (total, contango, backwardation).
            int dayTot = p_vixCentralRec.Length;
            int dayCont = p_vixCentralRec.Where(x => x.STCont >= 0).Count();
            double dayContPerc = (double)dayCont/dayTot;
            int dayBackw = p_vixCentralRec.Where(x => x.STCont < 0).Count();
            double dayBackwPerc = (double)dayBackw / dayTot;

            //Calculating the arithmetic mean of VIX futures prices, spreads using all of the days.
            double[] futsMeanTotal = new double[17];
            futsMeanTotal[0] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F1);
            futsMeanTotal[1] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F2);
            futsMeanTotal[2] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F3);
            futsMeanTotal[3] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F4);
            futsMeanTotal[4] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F5);
            futsMeanTotal[5] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F6);
            futsMeanTotal[6] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F7);
            futsMeanTotal[7] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F8);
            futsMeanTotal[8] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.STCont);
            futsMeanTotal[9] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.LTCont);
            futsMeanTotal[10] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F2 - x.F1);
            futsMeanTotal[11] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F3 - x.F2);
            futsMeanTotal[12] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F4 - x.F3);
            futsMeanTotal[13] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F5 - x.F4);
            futsMeanTotal[14] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F6 - x.F5);
            futsMeanTotal[15] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F7 - x.F6);
            futsMeanTotal[16] = p_vixCentralRec.DefaultIfEmpty().Average(x => x.F8 - x.F7);

            //Calculating the median of VIX futures prices, spreads using all of the days.
            double[] futsMedianTotal = new double[17];
            futsMedianTotal[0] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F1);
            futsMedianTotal[1] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F2);
            futsMedianTotal[2] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F3);
            futsMedianTotal[3] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F4);
            futsMedianTotal[4] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F5);
            futsMedianTotal[5] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F6);
            futsMedianTotal[6] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F7);
            futsMedianTotal[7] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F8);
            futsMedianTotal[8] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.STCont);
            futsMedianTotal[9] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.LTCont);
            futsMedianTotal[10] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
            futsMedianTotal[11] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
            futsMedianTotal[12] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
            futsMedianTotal[13] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
            futsMedianTotal[14] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
            futsMedianTotal[15] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
            futsMedianTotal[16] = p_vixCentralRec.DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);

            //Calculating the arithmetic mean of VIX futures prices, spreads using contango days.
            double[] futsMeanContTotal = new double[17];
            futsMeanContTotal[0] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F1);
            futsMeanContTotal[1] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F2);
            futsMeanContTotal[2] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F3);
            futsMeanContTotal[3] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F4);
            futsMeanContTotal[4] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F5);
            futsMeanContTotal[5] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F6);
            futsMeanContTotal[6] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F7);
            futsMeanContTotal[7] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F8);
            futsMeanContTotal[8] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.STCont);
            futsMeanContTotal[9] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.LTCont);
            futsMeanContTotal[10] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F2 - x.F1);
            futsMeanContTotal[11] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F3 - x.F2);
            futsMeanContTotal[12] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F4 - x.F3);
            futsMeanContTotal[13] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F5 - x.F4);
            futsMeanContTotal[14] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F6 - x.F5);
            futsMeanContTotal[15] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F7 - x.F6);
            futsMeanContTotal[16] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().Average(x => x.F8 - x.F7);

            //Calculating the median of VIX futures prices, spreads using contango days.
            double[] futsMedianContTotal = new double[17];
            futsMedianContTotal[0] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F1);
            futsMedianContTotal[1] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F2);
            futsMedianContTotal[2] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F3);
            futsMedianContTotal[3] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F4);
            futsMedianContTotal[4] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F5);
            futsMedianContTotal[5] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F6);
            futsMedianContTotal[6] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F7);
            futsMedianContTotal[7] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F8);
            futsMedianContTotal[8] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.STCont);
            futsMedianContTotal[9] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.LTCont);
            futsMedianContTotal[10] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
            futsMedianContTotal[11] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
            futsMedianContTotal[12] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
            futsMedianContTotal[13] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
            futsMedianContTotal[14] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
            futsMedianContTotal[15] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
            futsMedianContTotal[16] = p_vixCentralRec.Where(x => x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);

            //Calculating the arithmetic mean of VIX futures prices, spreads using backwardation days.
            double[] futsMeanBackwTotal = new double[17];
            futsMeanBackwTotal[0] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F1);
            futsMeanBackwTotal[1] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F2);
            futsMeanBackwTotal[2] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F3);
            futsMeanBackwTotal[3] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F4);
            futsMeanBackwTotal[4] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F5);
            futsMeanBackwTotal[5] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F6);
            futsMeanBackwTotal[6] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F7);
            futsMeanBackwTotal[7] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F8);
            futsMeanBackwTotal[8] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.STCont);
            futsMeanBackwTotal[9] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.LTCont);
            futsMeanBackwTotal[10] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F2 - x.F1);
            futsMeanBackwTotal[11] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F3 - x.F2);
            futsMeanBackwTotal[12] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F4 - x.F3);
            futsMeanBackwTotal[13] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F5 - x.F4);
            futsMeanBackwTotal[14] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F6 - x.F5);
            futsMeanBackwTotal[15] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F7 - x.F6);
            futsMeanBackwTotal[16] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().Average(x => x.F8 - x.F7);

            //Calculating the median of VIX futures prices, spreads using backwardation days.
            double[] futsMedianBackwTotal = new double[17];
            futsMedianBackwTotal[0] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F1);
            futsMedianBackwTotal[1] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F2);
            futsMedianBackwTotal[2] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F3);
            futsMedianBackwTotal[3] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F4);
            futsMedianBackwTotal[4] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F5);
            futsMedianBackwTotal[5] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F6);
            futsMedianBackwTotal[6] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F7);
            futsMedianBackwTotal[7] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F8);
            futsMedianBackwTotal[8] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.STCont);
            futsMedianBackwTotal[9] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.LTCont);
            futsMedianBackwTotal[10] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
            futsMedianBackwTotal[11] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
            futsMedianBackwTotal[12] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
            futsMedianBackwTotal[13] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
            futsMedianBackwTotal[14] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
            futsMedianBackwTotal[15] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
            futsMedianBackwTotal[16] = p_vixCentralRec.Where(x => x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);

            //Calculating number of days and percentage of days by months: total, contango, backwardation.
            double[] futsCount = new double[12];
            double[] futsContCount = new double[12];
            double[] futsContCountPerc = new double[12];
            double[] futsBackwCount = new double[12];
            double[] futsBackwCountPerc = new double[12];
            for (int iRows = 0; iRows < 12; iRows++)
            {
                futsCount[iRows] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).Count();
                futsContCount[iRows] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).Count();
                futsContCountPerc[iRows] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).Count()/futsCount[iRows];
                futsBackwCount[iRows] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).Count();
                futsBackwCountPerc[iRows] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).Count()/futsCount[iRows];
            }

            //Calculating the arithmetic means and medians of VIX futures prices, spreads by months: total, contango and backwardation days.
            double[,] futsMeanMonth = new double[12,17];
            double[,] futsMedianMonth = new double[12, 17];
            double[,] futsMeanMonthCont = new double[12, 17];
            double[,] futsMeanMonthBackw = new double[12, 17];
            double[,] futsMedianMonthCont = new double[12, 17];
            double[,] futsMedianMonthBackw = new double[12, 17];
            for (int iRows = 0; iRows < 12; iRows++)
            {
                futsMeanMonth[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F1);
                futsMeanMonth[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F2);
                futsMeanMonth[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F3);
                futsMeanMonth[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F4);
                futsMeanMonth[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F5);
                futsMeanMonth[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F6);
                futsMeanMonth[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F7);
                futsMeanMonth[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F8);
                futsMeanMonth[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.STCont);
                futsMeanMonth[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.LTCont);
                futsMeanMonth[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F2 - x.F1);
                futsMeanMonth[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F3 - x.F2);
                futsMeanMonth[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F4 - x.F3);
                futsMeanMonth[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F5 - x.F4);
                futsMeanMonth[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F6 - x.F5);
                futsMeanMonth[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F7 - x.F6);
                futsMeanMonth[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().Average(x => x.F8 - x.F7);
           
                futsMedianMonth[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F1);
                futsMedianMonth[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F2);
                futsMedianMonth[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F3);
                futsMedianMonth[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F4);
                futsMedianMonth[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F5);
                futsMedianMonth[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F6);
                futsMedianMonth[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F7);
                futsMedianMonth[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F8);
                futsMedianMonth[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.STCont);
                futsMedianMonth[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.LTCont);
                futsMedianMonth[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
                futsMedianMonth[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
                futsMedianMonth[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
                futsMedianMonth[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
                futsMedianMonth[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
                futsMedianMonth[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
                futsMedianMonth[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);
           
                futsMeanMonthCont[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F1);
                futsMeanMonthCont[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F2);
                futsMeanMonthCont[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F3);
                futsMeanMonthCont[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F4);
                futsMeanMonthCont[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F5);
                futsMeanMonthCont[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F6);
                futsMeanMonthCont[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F7);
                futsMeanMonthCont[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F8);
                futsMeanMonthCont[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.STCont);
                futsMeanMonthCont[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.LTCont);
                futsMeanMonthCont[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F2 - x.F1);
                futsMeanMonthCont[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F3 - x.F2);
                futsMeanMonthCont[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F4 - x.F3);
                futsMeanMonthCont[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F5 - x.F4);
                futsMeanMonthCont[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F6 - x.F5);
                futsMeanMonthCont[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F7 - x.F6);
                futsMeanMonthCont[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.F8 - x.F7);
            
                futsMeanMonthBackw[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F1);
                futsMeanMonthBackw[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F2);
                futsMeanMonthBackw[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F3);
                futsMeanMonthBackw[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F4);
                futsMeanMonthBackw[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F5);
                futsMeanMonthBackw[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F6);
                futsMeanMonthBackw[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F7);
                futsMeanMonthBackw[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F8);
                futsMeanMonthBackw[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.STCont);
                futsMeanMonthBackw[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.LTCont);
                futsMeanMonthBackw[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F2 - x.F1);
                futsMeanMonthBackw[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F3 - x.F2);
                futsMeanMonthBackw[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F4 - x.F3);
                futsMeanMonthBackw[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F5 - x.F4);
                futsMeanMonthBackw[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F6 - x.F5);
                futsMeanMonthBackw[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F7 - x.F6);
                futsMeanMonthBackw[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.F8 - x.F7);
            
                futsMedianMonthCont[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F1);
                futsMedianMonthCont[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F2);
                futsMedianMonthCont[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F3);
                futsMedianMonthCont[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F4);
                futsMedianMonthCont[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F5);
                futsMedianMonthCont[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F6);
                futsMedianMonthCont[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F7);
                futsMedianMonthCont[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F8);
                futsMedianMonthCont[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.STCont);
                futsMedianMonthCont[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.LTCont);
                futsMedianMonthCont[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
                futsMedianMonthCont[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
                futsMedianMonthCont[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
                futsMedianMonthCont[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
                futsMedianMonthCont[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
                futsMedianMonthCont[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
                futsMedianMonthCont[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);
            
                futsMedianMonthBackw[iRows, 0] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F1);
                futsMedianMonthBackw[iRows, 1] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F2);
                futsMedianMonthBackw[iRows, 2] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F3);
                futsMedianMonthBackw[iRows, 3] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F4);
                futsMedianMonthBackw[iRows, 4] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F5);
                futsMedianMonthBackw[iRows, 5] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F6);
                futsMedianMonthBackw[iRows, 6] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F7);
                futsMedianMonthBackw[iRows, 7] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F8);
                futsMedianMonthBackw[iRows, 8] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.STCont);
                futsMedianMonthBackw[iRows, 9] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.LTCont);
                futsMedianMonthBackw[iRows, 10] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F2 - x.F1);
                futsMedianMonthBackw[iRows, 11] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F3 - x.F2);
                futsMedianMonthBackw[iRows, 12] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F4 - x.F3);
                futsMedianMonthBackw[iRows, 13] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F5 - x.F4);
                futsMedianMonthBackw[iRows, 14] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F6 - x.F5);
                futsMedianMonthBackw[iRows, 15] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F7 - x.F6);
                futsMedianMonthBackw[iRows, 16] = p_vixCentralRec.Where(x => x.FirstMonth == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.F8 - x.F7);
            }

            //Calculating data for charts: prices and spreads.           
            double[] data2ChartsF = new double[p_vixCentralRec.Length*8];
            Array.Copy(p_vixCentralRec.Select(r => r.F1).ToArray(), 0, data2ChartsF, 0, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F2).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F3).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*2, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F4).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*3, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F5).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*4, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F6).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*5, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F7).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*6, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F8).ToArray(), 0, data2ChartsF, p_vixCentralRec.Length*7, p_vixCentralRec.Length);

            double[] data2ChartsSpr = new double[p_vixCentralRec.Length * 8];
            Array.Copy(p_vixCentralRec.Select(r => r.F2 - r.F1).ToArray(), 0, data2ChartsSpr, 0, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F3 - r.F2).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F4 - r.F3).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 2, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F5 - r.F4).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 3, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F6 - r.F5).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 4, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F7 - r.F6).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 5, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F8 - r.F7).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 6, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F8 - r.F8).ToArray(), 0, data2ChartsSpr, p_vixCentralRec.Length * 7, p_vixCentralRec.Length);

            int[] data2ChartsDays = new int[p_vixCentralRec.Length * 8];
            Array.Copy(p_vixCentralRec.Select(r => r.F1expDays).ToArray(), 0, data2ChartsDays, 0, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F2expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F3expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 2, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F4expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 3, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F5expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 4, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F6expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 5, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F7expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 6, p_vixCentralRec.Length);
            Array.Copy(p_vixCentralRec.Select(r => r.F8expDays).ToArray(), 0, data2ChartsDays, p_vixCentralRec.Length * 7, p_vixCentralRec.Length);

            double[] data2ChartsSTC = new double[p_vixCentralRec.Length * 8];
            for (int iRows = 0; iRows < 8; iRows++)
            {
                Array.Copy(p_vixCentralRec.Select(r => r.STCont).ToArray(), 0, data2ChartsSTC, p_vixCentralRec.Length*iRows, p_vixCentralRec.Length);
            }

            
            
            Data2Charts[] data2Charts = new Data2Charts[p_vixCentralRec.Length*8];
            for (int iRows = 0; iRows < data2Charts.Length; iRows++)
            {
                data2Charts[iRows].ExpDays = data2ChartsDays[iRows];
                data2Charts[iRows].FutPrices = data2ChartsF[iRows];
                data2Charts[iRows].FutSpreads = data2ChartsSpr[iRows];
                data2Charts[iRows].STCont = data2ChartsSTC[iRows];

            }

            double[,] res2ChartsFPs = new double[200, 10];
            for (int iRows = 0; iRows < res2ChartsFPs.GetLength(0); iRows++)
            {
                res2ChartsFPs[iRows, 0] = iRows + 1;
                res2ChartsFPs[iRows, 1] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().Average(x => x.FutPrices);
                res2ChartsFPs[iRows, 2] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.FutPrices);
                res2ChartsFPs[iRows, 3] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().Count();
                res2ChartsFPs[iRows, 4] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.FutPrices);
                res2ChartsFPs[iRows, 5] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.FutPrices);
                res2ChartsFPs[iRows, 6] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Count();
                res2ChartsFPs[iRows, 7] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.FutPrices);
                res2ChartsFPs[iRows, 8] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.FutPrices);
                res2ChartsFPs[iRows, 9] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Count();
            }
            int res2Length = 0;
            for (int i = 0; i < 200 ; i++)
            {
                if (res2ChartsFPs[i,3]>9)
                {
                    res2Length++;
                }
            }

            double[,] res2ChartsFPsmod = new double[res2Length, 10];
            int lineNum = 0;
            for (int iRows = 0; iRows < res2ChartsFPs.GetLength(0); iRows++)
            {
                if (res2ChartsFPs[iRows, 3] > 9)
                {
                    for (int iCols = 0; iCols < 10; iCols++)
                    {
                        res2ChartsFPsmod[lineNum, iCols] = res2ChartsFPs[iRows, iCols];
                        
                    }
                    lineNum++;
                }
            }

            
            double[,] res2ChartsSprs = new double[200, 10];
            for (int iRows = 0; iRows < res2ChartsFPs.GetLength(0); iRows++)
            {
                res2ChartsSprs[iRows, 0] = iRows + 1;
                res2ChartsSprs[iRows, 1] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().Average(x => x.FutSpreads);
                res2ChartsSprs[iRows, 2] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().LowerMedian(x => x.FutSpreads);
                res2ChartsSprs[iRows, 3] = data2Charts.Where(x => x.ExpDays == iRows + 1).DefaultIfEmpty().Count();
                res2ChartsSprs[iRows, 4] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Average(x => x.FutSpreads);
                res2ChartsSprs[iRows, 5] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().LowerMedian(x => x.FutSpreads);
                res2ChartsSprs[iRows, 6] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont >= 0).DefaultIfEmpty().Count();
                res2ChartsSprs[iRows, 7] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Average(x => x.FutSpreads);
                res2ChartsSprs[iRows, 8] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().LowerMedian(x => x.FutSpreads);
                res2ChartsSprs[iRows, 9] = data2Charts.Where(x => x.ExpDays == iRows + 1 && x.STCont < 0).DefaultIfEmpty().Count();
            }

            double[,] res2ChartsSprsmod = new double[res2Length, 10];
            int lineNum2 = 0;
            for (int iRows = 0; iRows < res2ChartsSprs.GetLength(0); iRows++)
            {
                if (res2ChartsFPs[iRows, 3] > 9)
                {
                    for (int iCols = 0; iCols < 10; iCols++)
                    {
                        res2ChartsSprsmod[lineNum2, iCols] = res2ChartsSprs[iRows, iCols];
                        
                    }
                    lineNum2++;
                }
            }

            //Creating input string for JavaScript.
            StringBuilder sb = new StringBuilder("{" + Environment.NewLine);
            //--sb.Append(@"""vixCentralRec"": """);
            //--for (int i = 0; i < p_vixCentralRec.Length; i++)
            //--sb.AppendLine(p_vixCentralRec[i].ToString());
            //--sb.Append(@"""," + Environment.NewLine + @"""timeNow"": """ + timeNow.ToString() + " EST");
            sb.Append(@"""timeNow"": """ + timeNowET.ToString("yyyy-MM-dd HH:mm") + " EST");

            sb.Append(@"""," + Environment.NewLine + @"""liveDataDate"": """ + p_liveDate);

            sb.Append(@"""," + Environment.NewLine + @"""liveDataTime"": """ + p_liveFuturesDataTime);

            sb.Append(@"""," + Environment.NewLine + @"""firstDataDate"": """ + firstDataDate);

            sb.Append(@"""," + Environment.NewLine + @"""lastDataDate"": """ + lastDataDate);

            sb.Append(@"""," + Environment.NewLine + @"""prevDataDate"": """ + prevDataDate);

            sb.Append(@"""," + Environment.NewLine + @"""currDataVec"": """);
            for (int i = 0; i < currData.Length - 1; i++)
                sb.Append(Math.Round(currData[i], 4).ToString() + ", ");
            sb.Append(Math.Round(currData[currData.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""currDataDaysVec"": """);
            for (int i = 0; i < currDataDays.Length - 1; i++)
                sb.Append(currDataDays[i].ToString() + ", ");
            sb.Append(currDataDays[currDataDays.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""prevDataVec"": """);
            for (int i = 0; i < prevData.Length - 1; i++)
                sb.Append(Math.Round(prevData[i], 4).ToString() + ", ");
            sb.Append(Math.Round(prevData[prevData.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""currDataDiffVec"": """);
            for (int i = 0; i < currDataDiff.Length - 1; i++)
                sb.Append(Math.Round(currDataDiff[i], 4).ToString() + ", ");
            sb.Append(Math.Round(currDataDiff[currDataDiff.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfTotalDays"": """ + dayTot.ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfContangoDays"": """ + dayCont.ToString());

            sb.Append(@"""," + Environment.NewLine + @"""percOfContangoDays"": """ + Math.Round(dayContPerc, 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfBackwardDays"": """ + dayBackw.ToString());

            sb.Append(@"""," + Environment.NewLine + @"""percOfBackwardDays"": """ + Math.Round(dayBackwPerc, 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfTotalDaysByMonthsVec"": """);
            for (int i = 0; i < futsCount.Length - 1; i++)
                sb.Append(Math.Round(futsCount[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsCount[futsCount.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfContangoDaysByMonthsVec"": """);
            for (int i = 0; i < futsContCount.Length - 1; i++)
                sb.Append(Math.Round(futsContCount[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsContCount[futsContCount.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""numberOfBackwardDaysByMonthsVec"": """);
            for (int i = 0; i < futsBackwCount.Length - 1; i++)
                sb.Append(Math.Round(futsBackwCount[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsBackwCount[futsBackwCount.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""percOfContangoDaysByMonthsVec"": """);
            for (int i = 0; i < futsContCountPerc.Length - 1; i++)
                sb.Append(Math.Round(futsContCountPerc[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsContCountPerc[futsContCountPerc.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""percOfBackwardDaysByMonthsVec"": """);
            for (int i = 0; i < futsBackwCountPerc.Length - 1; i++)
                sb.Append(Math.Round(futsBackwCountPerc[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsBackwCountPerc[futsBackwCountPerc.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""meanOfTotalDaysTotalVec"": """);
            for (int i = 0; i < futsMeanTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMeanTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMeanTotal[futsMeanTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""medianOfTotalDaysTotalVec"": """);
            for (int i = 0; i < futsMedianTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMedianTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMedianTotal[futsMedianTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""meanOfContangoDaysTotalVec"": """);
            for (int i = 0; i < futsMeanContTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMeanContTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMeanContTotal[futsMeanContTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""medianOfContangoDaysTotalVec"": """);
            for (int i = 0; i < futsMedianContTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMedianContTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMedianContTotal[futsMedianContTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""meanOfBackwardDaysTotalVec"": """);
            for (int i = 0; i < futsMeanBackwTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMeanBackwTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMeanBackwTotal[futsMeanBackwTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""medianOfBackwardDaysTotalVec"": """);
            for (int i = 0; i < futsMedianBackwTotal.Length - 1; i++)
                sb.Append(Math.Round(futsMedianBackwTotal[i], 4).ToString() + ", ");
            sb.Append(Math.Round(futsMedianBackwTotal[futsMedianBackwTotal.Length - 1], 4).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""meanOfTotalDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMeanMonth[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMeanMonth[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""medianOfTotalDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMedianMonth[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMedianMonth[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""meanOfContangoDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMeanMonthCont[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMeanMonthCont[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""medianOfContangoDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMedianMonthCont[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMedianMonthCont[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""meanOfBackwardDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMeanMonthBackw[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMeanMonthBackw[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""medianOfBackwardDaysByMonthsMtx"": """);
            for (int i = 0; i < 12; i++)
            {
                sb.Append("");
                for (int j = 0; j < 16; j++)
                {
                    sb.Append(Math.Round(futsMedianMonthBackw[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(futsMedianMonthBackw[i, 16], 4).ToString());
                if (i < 11)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""resultsToChartFutPricesMtx"": """);
            for (int i = 0; i < lineNum; i++)
            {
                sb.Append("");
                for (int j = 0; j < 9; j++)
                {
                    sb.Append(Math.Round(res2ChartsFPsmod[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(res2ChartsFPsmod[i, 9], 4).ToString());
                if (i < lineNum - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""resultsToChartFutSpreadsMtx"": """);
            for (int i = 0; i < lineNum2; i++)
            {
                sb.Append("");
                for (int j = 0; j < 9; j++)
                {
                    sb.Append(Math.Round(res2ChartsSprsmod[i, j], 4).ToString() + ", ");
                }
                sb.Append(Math.Round(res2ChartsSprsmod[i, 9], 4).ToString());
                if (i < lineNum2 - 1)
                {
                    sb.Append("ß ");
                }
            }


            sb.AppendLine(@"]"""+ Environment.NewLine + @"}");
           
            return sb.ToString();
                   
        }
    }
}
