using Microsoft.AspNetCore.Mvc;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using SQCommon.MathNet;
using System.Numerics;

namespace SQLab.Controllers
{
    public static class EM
    {
        public static int[] FindAllIndexofPos<T>(this IEnumerable<double> values, T val)
        {
            return values.Select((b, i) => b > 0 ? i : -1).Where(i => i != -1).ToArray();
        }
        public static int[] FindAllIndexofNeg<T>(this IEnumerable<double> values, T val)
        {
            return values.Select((b, i) => b < 0 ? i : -1).Where(i => i != -1).ToArray();
        }
    }
    public class GChBetaController : Controller
    {
#if !DEBUG
        [Authorize]
#endif
        public ActionResult Index()
        {
            try
            {
                return Content(GetStr(), "text/html");
            }
            catch
            {
                return Content(GetStr2(), "text/html");
            }
        }

        public string GetStr2()
        {
            return "Error";
        }


        //Downloading price data from SQL Server
        public static IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> DataSQDBGmod(string[] p_allAssetList)
        {
            List<string> tickersNeeded = p_allAssetList.ToList();
            DateTime endTimeUtc = DateTime.UtcNow.AddDays(10);
            DateTime endTimeUtc2 = endTimeUtc.AddDays(-11);
            DateTime endTimeUtc3 = endTimeUtc.AddDays(-12);
            DateTime startTimeUtc = DateTime.ParseExact("2004/01/01", "yyyy/MM/dd", CultureInfo.InvariantCulture);

            var getAllQuotesTask = SQLab.Controllers.QuickTester.Strategies.StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(startTimeUtc, endTimeUtc, tickersNeeded);
            Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = getAllQuotesTask.Result;

            if (p_allAssetList.Contains("TCEHY") && getAllQuotesData.Item1[0].Count - getAllQuotesData.Item1[p_allAssetList.Length - 2].Count == 1)
            {
                getAllQuotesData.Item1[p_allAssetList.Length - 2].Add(getAllQuotesData.Item1[p_allAssetList.Length - 2][getAllQuotesData.Item1[p_allAssetList.Length - 2].Count - 1]);
            }

            int[] histLength = new int[p_allAssetList.Length];
            for (int i = 0; i < histLength.Length; i++)
            {
                histLength[i] = getAllQuotesData.Item1[i].Count;
            }

            int allDataLength = (histLength.Min() == histLength.Max()) ? histLength.Min() : 0;

            if (allDataLength == 0)
            {
                getAllQuotesTask = SQLab.Controllers.QuickTester.Strategies.StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(startTimeUtc, endTimeUtc2, tickersNeeded);
                getAllQuotesData = getAllQuotesTask.Result;

                int[] histLength2 = new int[p_allAssetList.Length];
                for (int i = 0; i < histLength2.Length; i++)
                {
                    histLength2[i] = getAllQuotesData.Item1[i].Count;
                }
                allDataLength = (histLength2.Min() == histLength2.Max()) ? histLength2.Min() : 0;
            }

            if (allDataLength == 0)
            {
                getAllQuotesTask = SQLab.Controllers.QuickTester.Strategies.StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(startTimeUtc, endTimeUtc3, tickersNeeded);
                getAllQuotesData = getAllQuotesTask.Result;

                int[] histLength3 = new int[p_allAssetList.Length];
                for (int i = 0; i < histLength3.Length; i++)
                {
                    histLength3[i] = getAllQuotesData.Item1[i].Count;
                }
                allDataLength = (histLength3.Min() == histLength3.Max()) ? histLength3.Min() : 0;
            }

            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> quotes;

            quotes = getAllQuotesData.Item1.ToList();


            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> dataFromSQServermod = quotes;

            return dataFromSQServermod;
        }



        public string GetStr()
        {
            //Defining asset lists.
            string[] allAssetList = new string[] { "AAPL", "AMZN", "BABA", "BIDU", "CRM", "FB", "GOOGL", "JD", "MSFT", "NFLX", "NVDA", "SQ", "TCEHY", "QQQ", "SPY" };
            string[] allAssetList2 = new string[] { "AAPL", "AMZN", "BABA", "BIDU", "CRM", "FB", "GOOGL", "JD", "MSFT", "NFLX", "NVDA", "SQ", "TCEHY", "EqualWeighted GCh" };

            //Defining lookback periods for beta calculation.                   
            int[] betaLB = new int[] { 5, 10, 21, 42, 63, 126, 252, 504, 756, 100000 };
            string[] betaLBStr = new string[] { "1 Week", "2 Weeks", "1 Month", "2 Months", "3 Months", "6 Months", "1 Year", "2 Years", "3 Years", "Max" };

            int[] retLB = new int[] { 1, 3, 5, 10, 20, 63, 126, 252 };
            string[] retLBStr = new string[] { "1 Day", "3 Days", "1 Week", "2 Weeks", "1 Month", "3 Months", "6 Months", "1 Year" };

            //Collecting and splitting price data got from SQL Server
            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> quotesData = DataSQDBGmod(allAssetList);
            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> quotesData1 = new List<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>(quotesData);

            //87
            int noAssets = allAssetList.Length;
            int noGChs = noAssets - 2;
            int noBtDays = quotesData1[0].Count;
            DateTime[] quotesDateVec = new DateTime[noBtDays];

            for (int iRows = 0; iRows < quotesDateVec.Length; iRows++)
            {
                quotesDateVec[iRows] = quotesData1[0][iRows].Date;
            }

            int[] lenGchDat = new int[noGChs + 1];
            int[] startGchDat = new int[noGChs + 1];
            for (int jAssets = 0; jAssets < lenGchDat.Length - 1; jAssets++)
            {
                lenGchDat[jAssets] = quotesData1[jAssets].Count;
                startGchDat[jAssets] = noBtDays - lenGchDat[jAssets];
            }
            lenGchDat[lenGchDat.Length - 1] = noBtDays;
            startGchDat[lenGchDat.Length - 1] = 0;

            int[] noGchsDay = new int[noBtDays];
            for (int jDays = 0; jDays < noBtDays; jDays++)
            {
                int noGchGD = Array.FindAll(startGchDat, item => item <= jDays).Length - 1;
                noGchsDay[jDays] = noGchGD;
            }

            DateTime[] quotesFirstDates = new DateTime[noAssets];

            for (int jAssets = 0; jAssets < quotesFirstDates.Length; jAssets++)
            {
                quotesFirstDates[jAssets] = quotesData1[jAssets][0].Date;
            }

            DateTime[] quotesLastDates = new DateTime[noAssets];

            for (int jAssets = 0; jAssets < quotesLastDates.Length; jAssets++)
            {
                quotesLastDates[jAssets] = quotesData1[jAssets][quotesData1[jAssets].Count - 1].Date;
            }

            double[] quotesFirstPrices = new double[noAssets];

            for (int jAssets = 0; jAssets < quotesFirstPrices.Length; jAssets++)
            {
                quotesFirstPrices[jAssets] = quotesData1[jAssets][0].AdjClosePrice;
            }

            double[] quotesLastPrices = new double[noAssets];

            for (int jAssets = 0; jAssets < quotesLastPrices.Length; jAssets++)
            {
                quotesLastPrices[jAssets] = quotesData1[jAssets][quotesData1[jAssets].Count - 1].AdjClosePrice;
            }

            IList<List<double>> quotesPrices = new List<List<double>>();

            for (int iAsset = 0; iAsset < noAssets; iAsset++)
            {
                int shiftDays = 0;
                List<double> assPriceSubList = new List<double>();
                int jRows = 0;
                while (quotesDateVec[jRows] < quotesFirstDates[iAsset])
                {
                    assPriceSubList.Add(quotesFirstPrices[iAsset]);
                    shiftDays += 1;
                    jRows++;
                    if (jRows >= noBtDays)
                    {
                        break;
                    }
                }
                while (quotesDateVec[jRows] == quotesData1[iAsset][jRows - shiftDays].Date)
                {
                    assPriceSubList.Add(quotesData1[iAsset][jRows - shiftDays].AdjClosePrice);
                    jRows++;
                    if (jRows >= quotesData1[iAsset].Count + shiftDays)
                    {
                        break;
                    }
                }
                if (jRows < noBtDays)
                {
                    while (quotesDateVec[jRows] > quotesLastDates[iAsset])
                    {
                        assPriceSubList.Add(quotesLastPrices[iAsset]);
                        jRows++;
                        if (jRows >= noBtDays)
                        {
                            break;
                        }
                    }
                }

                quotesPrices.Add(assPriceSubList);
            }

            double[,] histRet = new double[retLB.Length, noAssets];

            for (int iAsset = 0; iAsset < noAssets; iAsset++)
            {
                for (int jRows = 0; jRows < retLB.Length; jRows++)
                {
                    histRet[jRows, iAsset] = quotesPrices[iAsset][quotesPrices[0].Count - 1] / quotesPrices[iAsset][quotesPrices[0].Count - 1 - retLB[jRows]] - 1;
                }
            }

            int histRetLengthSum = retLB.Sum();
            double[,] histRet2 = new double[histRetLengthSum, noAssets];

            int kShift = 0;
            for (int kLen = 0; kLen < retLB.Length; kLen++)
            {
                for (int iAsset = 0; iAsset < noAssets; iAsset++)
                {
                    for (int jRows = 0; jRows < retLB[kLen]; jRows++)
                    {
                        histRet2[kShift + jRows, iAsset] = quotesPrices[iAsset][quotesPrices[0].Count - retLB[kLen] + jRows] / quotesPrices[iAsset][quotesPrices[0].Count - 1 - retLB[kLen]] - 1;
                    }
                }
                kShift += retLB[kLen];
            }

            IList<List<double>> quotesRets = new List<List<double>>();

            for (int iAsset = 0; iAsset < noAssets; iAsset++)
            {
                List<double> assSubList = new List<double>();
                assSubList.Add(0);
                for (int jRows = 1; jRows < noBtDays; jRows++)
                {
                    assSubList.Add(quotesPrices[iAsset][jRows] / quotesPrices[iAsset][jRows - 1] - 1);
                }
                quotesRets.Add(assSubList);
            }

            List<double> eqWeGChRets = new List<double>();
            for (int jRows = 0; jRows < noBtDays; jRows++)
            {
                double dailySumRet = 0;
                for (int kAssets = 0; kAssets < noGChs; kAssets++)
                {
                    dailySumRet += quotesRets[kAssets][jRows];
                }
                eqWeGChRets.Add(dailySumRet / noGchsDay[jRows]);
            }

            IList<List<double>> quotesRets2 = new List<List<double>>(quotesRets);
            quotesRets2.RemoveAt(allAssetList.Length - 1);
            quotesRets2.RemoveAt(allAssetList.Length - 2);
            quotesRets2.Add(eqWeGChRets);


            string[] dateYearsVec = new string[noBtDays];
            string[] dateYearsMonthsVec = new string[noBtDays];
            for (int iRows = 0; iRows < dateYearsMonthsVec.Length; iRows++)
            {
                dateYearsVec[iRows] = quotesDateVec[iRows].ToString("yyyy");
                dateYearsMonthsVec[iRows] = quotesDateVec[iRows].ToString("yyyy-MM");
            }

            string[] dateYearsDist = dateYearsVec.Distinct().ToArray();
            string[] dateYearsMonthsDist = dateYearsMonthsVec.Distinct().ToArray();

            int[] dateYearsEndInd = new int[dateYearsDist.Length];
            for (int iRows = 0; iRows < dateYearsEndInd.Length; iRows++)
            {
                dateYearsEndInd[iRows] = Array.FindLastIndex(dateYearsVec, item => item == dateYearsDist[iRows]);
            }

            int[] dateYearsMonthsEndInd = new int[dateYearsMonthsDist.Length];
            for (int iRows = 0; iRows < dateYearsMonthsEndInd.Length; iRows++)
            {
                dateYearsMonthsEndInd[iRows] = Array.FindLastIndex(dateYearsMonthsVec, item => item == dateYearsMonthsDist[iRows]);
            }
            int noMonths = dateYearsMonthsEndInd.Length;

            int[] dateYearsCount = new int[dateYearsDist.Length];
            dateYearsCount[0] = dateYearsEndInd[0];
            for (int iRows = 1; iRows < dateYearsCount.Length; iRows++)
            {
                dateYearsCount[iRows] = dateYearsEndInd[iRows] - dateYearsEndInd[iRows - 1];
            }

            int[] dateYearsMonthsCount = new int[dateYearsMonthsDist.Length];
            dateYearsMonthsCount[0] = dateYearsMonthsEndInd[0];
            for (int iRows = 1; iRows < dateYearsMonthsCount.Length; iRows++)
            {
                dateYearsMonthsCount[iRows] = dateYearsMonthsEndInd[iRows] - dateYearsMonthsEndInd[iRows - 1];
            }

            int noTotalDays = dateYearsCount.Sum();


            DateTime[] quotesDateMEVec = new DateTime[noMonths];

            for (int iRows = 0; iRows < quotesDateMEVec.Length; iRows++)
            {
                quotesDateMEVec[iRows] = quotesData1[0][dateYearsMonthsEndInd[iRows]].Date;
            }


            double[,,] betaCalcQQQ = new double[noMonths, betaLBStr.Length, noGChs + 1];
            double[,,] betaCalcSPY = new double[noMonths, betaLBStr.Length, noGChs + 1];
            
            for (int iDays = 0; iDays < noMonths; iDays++)
            {
                for (int jLB = 0; jLB < betaLBStr.Length - 1; jLB++)
                {
                    if (dateYearsMonthsEndInd[iDays] >= betaLB[jLB] - 1)
                    {

                        double[] qqqRets = quotesRets[noAssets - 2].GetRange(dateYearsMonthsEndInd[iDays] - betaLB[jLB] + 1, betaLB[jLB]).ToArray();
                        double[] spyRets = quotesRets[noAssets - 1].GetRange(dateYearsMonthsEndInd[iDays] - betaLB[jLB] + 1, betaLB[jLB]).ToArray();

                        double varQQQ = ArrayStatistics.Variance(qqqRets);
                        double varSPY = ArrayStatistics.Variance(spyRets);


                        for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                        {
                            if (dateYearsMonthsEndInd[iDays] >= noBtDays - lenGchDat[kAssets] + betaLB[jLB] - 1)
                            {
                                double[] assRets = quotesRets2[kAssets].GetRange(dateYearsMonthsEndInd[iDays] - betaLB[jLB] + 1, betaLB[jLB]).ToArray();
                                double covQQQ = ArrayStatistics.Covariance(qqqRets, assRets);
                                double covSPY = ArrayStatistics.Covariance(spyRets, assRets);
                                betaCalcQQQ[iDays, jLB, kAssets] = covQQQ / varQQQ;
                                betaCalcSPY[iDays, jLB, kAssets] = covSPY / varSPY;
                            }

                        }

                    }
                }
                for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                {
                    if (dateYearsMonthsEndInd[iDays] > startGchDat[kAssets] + 3)
                    {
                        double[] assRetsFull = quotesRets2[kAssets].GetRange(startGchDat[kAssets], dateYearsMonthsEndInd[iDays] - startGchDat[kAssets] + 1).ToArray();
                        double[] qqqRetsFull = quotesRets[noAssets - 2].GetRange(startGchDat[kAssets], dateYearsMonthsEndInd[iDays] - startGchDat[kAssets] + 1).ToArray();
                        double[] spyRetsFull = quotesRets[noAssets - 1].GetRange(startGchDat[kAssets], dateYearsMonthsEndInd[iDays] - startGchDat[kAssets] + 1).ToArray();
                        double varQQQFull = ArrayStatistics.Variance(qqqRetsFull);
                        double varSPYFull = ArrayStatistics.Variance(spyRetsFull);
                        double covQQQFull = ArrayStatistics.Covariance(qqqRetsFull, assRetsFull);
                        double covSPYFull = ArrayStatistics.Covariance(spyRetsFull, assRetsFull);
                        betaCalcQQQ[iDays, betaLBStr.Length - 1, kAssets] = covQQQFull / varQQQFull;
                        betaCalcSPY[iDays, betaLBStr.Length - 1, kAssets] = covSPYFull / varSPYFull;
                        
                    }
                }
            }

            double[,] betaCalcQQQCurr = new double[betaLBStr.Length, noGChs + 1];
            for (int jLB = 0; jLB < betaLBStr.Length; jLB++)
            {
                for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                {
                    betaCalcQQQCurr[jLB, kAssets] = betaCalcQQQ[noMonths - 1, jLB, kAssets];
                }
            }

            double[,] betaCalcSPYCurr = new double[betaLBStr.Length, noGChs + 1];
            for (int jLB = 0; jLB < betaLBStr.Length; jLB++)
            {
                for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                {
                    betaCalcSPYCurr[jLB, kAssets] = betaCalcSPY[noMonths - 1, jLB, kAssets];
                }
            }

            double[] betaQQQTotal = new double[noGChs + 1];
            double[] betaSPYTotal = new double[noGChs + 1];
            for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
            {
                betaQQQTotal[kAssets] = betaCalcQQQ[noMonths - 1, betaLBStr.Length - 1,kAssets];
                betaSPYTotal[kAssets] = betaCalcSPY[noMonths - 1, betaLBStr.Length - 1, kAssets];
            }

            double[] betaCalcQQQPosAll = new double[noGChs + 1];
            double[] betaCalcSPYPosAll = new double[noGChs + 1];
            double[] betaCalcQQQNegAll = new double[noGChs + 1];
            double[] betaCalcSPYNegAll = new double[noGChs + 1];
            for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
            {
                double[] qqqRetsAll = quotesRets[noAssets - 2].GetRange(startGchDat[kAssets], noBtDays-startGchDat[kAssets]).ToArray();
                double[] spyRetsAll = quotesRets[noAssets - 1].GetRange(startGchDat[kAssets], noBtDays - startGchDat[kAssets]).ToArray();
                double[] stockRetsAll = quotesRets2[kAssets].GetRange(startGchDat[kAssets], noBtDays - startGchDat[kAssets]).ToArray();
                int[] indQQQPosAll = qqqRetsAll.FindAllIndexofPos("B");
                int[] indSPYPosAll = spyRetsAll.FindAllIndexofPos("B");
                int[] indQQQNegAll = qqqRetsAll.FindAllIndexofNeg("B");
                int[] indSPYNegAll = spyRetsAll.FindAllIndexofNeg("B");
                double[] qqqRetsPosAll = indQQQPosAll.Select(i => qqqRetsAll[i]).ToArray();
                double[] spyRetsPosAll = indSPYPosAll.Select(i => spyRetsAll[i]).ToArray();
                double[] qqqRetsNegAll = indQQQNegAll.Select(i => qqqRetsAll[i]).ToArray();
                double[] spyRetsNegAll = indSPYNegAll.Select(i => spyRetsAll[i]).ToArray();
                double varQQQPosAll = ArrayStatistics.Variance(qqqRetsPosAll);
                double varSPYPosAll = ArrayStatistics.Variance(spyRetsPosAll);
                double varQQQNegAll = ArrayStatistics.Variance(qqqRetsNegAll);
                double varSPYNegAll = ArrayStatistics.Variance(spyRetsNegAll);
                double[] assRetsPosQQQAll = indQQQPosAll.Select(i => stockRetsAll[i]).ToArray();
                double[] assRetsNegQQQAll = indQQQNegAll.Select(i => stockRetsAll[i]).ToArray();
                double[] assRetsPosSPYAll = indSPYPosAll.Select(i => stockRetsAll[i]).ToArray();
                double[] assRetsNegSPYAll = indSPYNegAll.Select(i => stockRetsAll[i]).ToArray();
                double covQQQPosAll = ArrayStatistics.Covariance(qqqRetsPosAll, assRetsPosQQQAll);
                double covSPYPosAll = ArrayStatistics.Covariance(spyRetsPosAll, assRetsPosSPYAll);
                double covQQQNegAll = ArrayStatistics.Covariance(qqqRetsNegAll, assRetsNegQQQAll);
                double covSPYNegAll = ArrayStatistics.Covariance(spyRetsNegAll, assRetsNegSPYAll);
                betaCalcQQQPosAll[kAssets] = covQQQPosAll / varQQQPosAll;
                betaCalcSPYPosAll[kAssets] = covSPYPosAll / varSPYPosAll;
                betaCalcQQQNegAll[kAssets] = covQQQNegAll / varQQQNegAll;
                betaCalcSPYNegAll[kAssets] = covSPYNegAll / varSPYNegAll;
            }

            double[,] betaCalcQQQYearly = new double[dateYearsDist.Length, noGChs + 1];
            double[,] betaCalcSPYYearly = new double[dateYearsDist.Length, noGChs + 1];
            double[,] betaCalcQQQPosYearly = new double[dateYearsDist.Length, noGChs + 1];
            double[,] betaCalcSPYPosYearly = new double[dateYearsDist.Length, noGChs + 1];
            double[,] betaCalcQQQNegYearly = new double[dateYearsDist.Length, noGChs + 1];
            double[,] betaCalcSPYNegYearly = new double[dateYearsDist.Length, noGChs + 1];
            int[] noDaysQQQPosYearly = new int[dateYearsDist.Length];
            int[] noDaysSPYPosYearly = new int[dateYearsDist.Length];
            int[] noDaysQQQNegYearly = new int[dateYearsDist.Length];
            int[] noDaysSPYNegYearly = new int[dateYearsDist.Length];

            double[] qqqRetsY0 = quotesRets[noAssets - 2].GetRange(0, dateYearsEndInd[0]).ToArray();
            double[] spyRetsY0 = quotesRets[noAssets - 1].GetRange(0, dateYearsEndInd[0]).ToArray();
            int[] indQQQPosY0 = qqqRetsY0.FindAllIndexofPos("B");
            int[] indSPYPosY0 = spyRetsY0.FindAllIndexofPos("B");
            int[] indQQQNegY0 = qqqRetsY0.FindAllIndexofNeg("B");
            int[] indSPYNegY0 = spyRetsY0.FindAllIndexofNeg("B");
            double[] qqqRetsPosY0 = indQQQPosY0.Select(i => qqqRetsY0[i]).ToArray();
            double[] spyRetsPosY0 = indSPYPosY0.Select(i => spyRetsY0[i]).ToArray();
            double[] qqqRetsNegY0 = indQQQNegY0.Select(i => qqqRetsY0[i]).ToArray();
            double[] spyRetsNegY0 = indSPYNegY0.Select(i => spyRetsY0[i]).ToArray();
            noDaysQQQPosYearly[0] = qqqRetsPosY0.Length;
            noDaysSPYPosYearly[0] = spyRetsPosY0.Length;
            noDaysQQQNegYearly[0] = qqqRetsNegY0.Length;
            noDaysSPYNegYearly[0] = spyRetsNegY0.Length;
            double varQQQY0 = ArrayStatistics.Variance(qqqRetsY0);
            double varSPYY0 = ArrayStatistics.Variance(spyRetsY0);
            double varQQQPosY0 = ArrayStatistics.Variance(qqqRetsPosY0);
            double varSPYPosY0 = ArrayStatistics.Variance(spyRetsPosY0);
            double varQQQNegY0 = ArrayStatistics.Variance(qqqRetsNegY0);
            double varSPYNegY0 = ArrayStatistics.Variance(spyRetsNegY0);
            for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
            {
                if (dateYearsEndInd[0] >= startGchDat[kAssets] + 4)
                {
                    double[] assRetsY0 = quotesRets2[kAssets].GetRange(0, dateYearsEndInd[0]).ToArray();
                    double[] assRetsPosQQQY0 = indQQQPosY0.Select(i => assRetsY0[i]).ToArray();
                    double[] assRetsNegQQQY0 = indQQQNegY0.Select(i => assRetsY0[i]).ToArray();
                    double[] assRetsPosSPYY0 = indSPYPosY0.Select(i => assRetsY0[i]).ToArray();
                    double[] assRetsNegSPYY0 = indSPYNegY0.Select(i => assRetsY0[i]).ToArray();
                    double covQQQY0 = ArrayStatistics.Covariance(qqqRetsY0, assRetsY0);
                    double covSPYY0 = ArrayStatistics.Covariance(spyRetsY0, assRetsY0);
                    double covQQQPosY0 = ArrayStatistics.Covariance(qqqRetsPosY0, assRetsPosQQQY0);
                    double covSPYPosY0 = ArrayStatistics.Covariance(spyRetsPosY0, assRetsPosSPYY0);
                    double covQQQNegY0 = ArrayStatistics.Covariance(qqqRetsNegY0, assRetsNegQQQY0);
                    double covSPYNegY0 = ArrayStatistics.Covariance(spyRetsNegY0, assRetsNegSPYY0);
                    betaCalcQQQYearly[0, kAssets] = covQQQY0 / varQQQY0;
                    betaCalcSPYYearly[0, kAssets] = covSPYY0 / varSPYY0;
                    betaCalcQQQPosYearly[0, kAssets] = covQQQPosY0 / varQQQPosY0;
                    betaCalcSPYPosYearly[0, kAssets] = covSPYPosY0 / varSPYPosY0;
                    betaCalcQQQNegYearly[0, kAssets] = covQQQNegY0 / varQQQNegY0;
                    betaCalcSPYNegYearly[0, kAssets] = covSPYNegY0 / varSPYNegY0;
                }

            }
            for (int iYears = 1; iYears < dateYearsDist.Length; iYears++)
            {
                double[] qqqRetsY = quotesRets[noAssets - 2].GetRange(dateYearsEndInd[iYears - 1] + 1, dateYearsEndInd[iYears] - dateYearsEndInd[iYears - 1]).ToArray();
                double[] spyRetsY = quotesRets[noAssets - 1].GetRange(dateYearsEndInd[iYears - 1] + 1, dateYearsEndInd[iYears] - dateYearsEndInd[iYears - 1]).ToArray();
                int[] indQQQPosY = qqqRetsY.FindAllIndexofPos("B");
                int[] indSPYPosY = spyRetsY.FindAllIndexofPos("B");
                int[] indQQQNegY = qqqRetsY.FindAllIndexofNeg("B");
                int[] indSPYNegY = spyRetsY.FindAllIndexofNeg("B");
                double[] qqqRetsPosY = indQQQPosY.Select(i => qqqRetsY[i]).ToArray();
                double[] spyRetsPosY = indSPYPosY.Select(i => spyRetsY[i]).ToArray();
                double[] qqqRetsNegY = indQQQNegY.Select(i => qqqRetsY[i]).ToArray();
                double[] spyRetsNegY = indSPYNegY.Select(i => spyRetsY[i]).ToArray();
                noDaysQQQPosYearly[iYears] = qqqRetsPosY.Length;
                noDaysSPYPosYearly[iYears] = spyRetsPosY.Length;
                noDaysQQQNegYearly[iYears] = qqqRetsNegY.Length;
                noDaysSPYNegYearly[iYears] = spyRetsNegY.Length;
                double varQQQY = ArrayStatistics.Variance(qqqRetsY);
                double varSPYY = ArrayStatistics.Variance(spyRetsY);
                double varQQQPosY = ArrayStatistics.Variance(qqqRetsPosY);
                double varSPYPosY = ArrayStatistics.Variance(spyRetsPosY);
                double varQQQNegY = ArrayStatistics.Variance(qqqRetsNegY);
                double varSPYNegY = ArrayStatistics.Variance(spyRetsNegY);
                for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                {
                    if (dateYearsEndInd[iYears] >= startGchDat[kAssets] + 4)
                    {
                        double[] assRetsY = quotesRets2[kAssets].GetRange(dateYearsEndInd[iYears - 1] + 1, dateYearsEndInd[iYears] - dateYearsEndInd[iYears - 1]).ToArray();
                        double[] assRetsPosQQQY = indQQQPosY.Select(i => assRetsY[i]).ToArray();
                        double[] assRetsNegQQQY = indQQQNegY.Select(i => assRetsY[i]).ToArray();
                        double[] assRetsPosSPYY = indSPYPosY.Select(i => assRetsY[i]).ToArray();
                        double[] assRetsNegSPYY = indSPYNegY.Select(i => assRetsY[i]).ToArray();
                        double covQQQY = ArrayStatistics.Covariance(qqqRetsY, assRetsY);
                        double covSPYY = ArrayStatistics.Covariance(spyRetsY, assRetsY);
                        double covQQQPosY = ArrayStatistics.Covariance(qqqRetsPosY, assRetsPosQQQY);
                        double covSPYPosY = ArrayStatistics.Covariance(spyRetsPosY, assRetsPosSPYY);
                        double covQQQNegY = ArrayStatistics.Covariance(qqqRetsNegY, assRetsNegQQQY);
                        double covSPYNegY = ArrayStatistics.Covariance(spyRetsNegY, assRetsNegSPYY);
                        betaCalcQQQYearly[iYears, kAssets] = covQQQY / varQQQY;
                        betaCalcSPYYearly[iYears, kAssets] = covSPYY / varSPYY;
                        betaCalcQQQPosYearly[iYears, kAssets] = covQQQPosY / varQQQPosY;
                        betaCalcSPYPosYearly[iYears, kAssets] = covSPYPosY / varSPYPosY;
                        betaCalcQQQNegYearly[iYears, kAssets] = covQQQNegY / varQQQNegY;
                        betaCalcSPYNegYearly[iYears, kAssets] = covSPYNegY / varSPYNegY;
                    }

                }
            }


            double[,] betaCalcQQQMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            double[,] betaCalcSPYMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            double[,] betaCalcQQQPosMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            double[,] betaCalcSPYPosMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            double[,] betaCalcQQQNegMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            double[,] betaCalcSPYNegMonthly = new double[dateYearsMonthsDist.Length, noGChs + 1];
            int[] noDaysQQQPosMonthly = new int[dateYearsMonthsDist.Length];
            int[] noDaysSPYPosMonthly = new int[dateYearsMonthsDist.Length];
            int[] noDaysQQQNegMonthly = new int[dateYearsMonthsDist.Length];
            int[] noDaysSPYNegMonthly = new int[dateYearsMonthsDist.Length];

            double[] qqqRetsM0 = quotesRets[noAssets - 2].GetRange(0, dateYearsMonthsEndInd[0]).ToArray();
            double[] spyRetsM0 = quotesRets[noAssets - 1].GetRange(0, dateYearsMonthsEndInd[0]).ToArray();
            int[] indQQQPosM0 = qqqRetsM0.FindAllIndexofPos("B");
            int[] indSPYPosM0 = spyRetsM0.FindAllIndexofPos("B");
            int[] indQQQNegM0 = qqqRetsM0.FindAllIndexofNeg("B");
            int[] indSPYNegM0 = spyRetsM0.FindAllIndexofNeg("B");
            double[] qqqRetsPosM0 = indQQQPosM0.Select(i => qqqRetsM0[i]).ToArray();
            double[] spyRetsPosM0 = indSPYPosM0.Select(i => spyRetsM0[i]).ToArray();
            double[] qqqRetsNegM0 = indQQQNegM0.Select(i => qqqRetsM0[i]).ToArray();
            double[] spyRetsNegM0 = indSPYNegM0.Select(i => spyRetsM0[i]).ToArray();
            noDaysQQQPosMonthly[0] = qqqRetsPosM0.Length;
            noDaysSPYPosMonthly[0] = spyRetsPosM0.Length;
            noDaysQQQNegMonthly[0] = qqqRetsNegM0.Length;
            noDaysSPYNegMonthly[0] = spyRetsNegM0.Length;
            double varQQQM0 = ArrayStatistics.Variance(qqqRetsM0);
            double varSPYM0 = ArrayStatistics.Variance(spyRetsM0);
            double varQQQPosM0 = ArrayStatistics.Variance(qqqRetsPosM0);
            double varSPYPosM0 = ArrayStatistics.Variance(spyRetsPosM0);
            double varQQQNegM0 = ArrayStatistics.Variance(qqqRetsNegM0);
            double varSPYNegM0 = ArrayStatistics.Variance(spyRetsNegM0);
            for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
            {
                if (dateYearsMonthsEndInd[0] >= startGchDat[kAssets] + 4)
                {
                    double[] assRetsM0 = quotesRets2[kAssets].GetRange(0, dateYearsMonthsEndInd[0]).ToArray();
                    double[] assRetsPosQQQM0 = indQQQPosM0.Select(i => assRetsM0[i]).ToArray();
                    double[] assRetsNegQQQM0 = indQQQNegM0.Select(i => assRetsM0[i]).ToArray();
                    double[] assRetsPosSPYM0 = indSPYPosM0.Select(i => assRetsM0[i]).ToArray();
                    double[] assRetsNegSPYM0 = indSPYNegM0.Select(i => assRetsM0[i]).ToArray();
                    double covQQQM0 = ArrayStatistics.Covariance(qqqRetsM0, assRetsM0);
                    double covSPYM0 = ArrayStatistics.Covariance(spyRetsM0, assRetsM0);
                    double covQQQPosM0 = ArrayStatistics.Covariance(qqqRetsPosM0, assRetsPosQQQM0);
                    double covSPYPosM0 = ArrayStatistics.Covariance(spyRetsPosM0, assRetsPosSPYM0);
                    double covQQQNegM0 = ArrayStatistics.Covariance(qqqRetsNegM0, assRetsNegQQQM0);
                    double covSPYNegM0 = ArrayStatistics.Covariance(spyRetsNegM0, assRetsNegSPYM0);
                    betaCalcQQQMonthly[0, kAssets] = covQQQM0 / varQQQM0;
                    betaCalcSPYMonthly[0, kAssets] = covSPYM0 / varSPYM0;
                    betaCalcQQQPosMonthly[0, kAssets] = covQQQPosM0 / varQQQPosM0;
                    betaCalcSPYPosMonthly[0, kAssets] = covSPYPosM0 / varSPYPosM0;
                    betaCalcQQQNegMonthly[0, kAssets] = covQQQNegM0 / varQQQNegM0;
                    betaCalcSPYNegMonthly[0, kAssets] = covSPYNegM0 / varSPYNegM0;
                }

            }
            for (int iMonths = 1; iMonths < dateYearsMonthsDist.Length; iMonths++)
            {
                double[] qqqRetsM = quotesRets[noAssets - 2].GetRange(dateYearsMonthsEndInd[iMonths - 1] + 1, dateYearsMonthsEndInd[iMonths] - dateYearsMonthsEndInd[iMonths - 1]).ToArray();
                double[] spyRetsM = quotesRets[noAssets - 1].GetRange(dateYearsMonthsEndInd[iMonths - 1] + 1, dateYearsMonthsEndInd[iMonths] - dateYearsMonthsEndInd[iMonths - 1]).ToArray();
                int[] indQQQPosM = qqqRetsM.FindAllIndexofPos("B");
                int[] indSPYPosM = spyRetsM.FindAllIndexofPos("B");
                int[] indQQQNegM = qqqRetsM.FindAllIndexofNeg("B");
                int[] indSPYNegM = spyRetsM.FindAllIndexofNeg("B");
                double[] qqqRetsPosM = indQQQPosM.Select(i => qqqRetsM[i]).ToArray();
                double[] spyRetsPosM = indSPYPosM.Select(i => spyRetsM[i]).ToArray();
                double[] qqqRetsNegM = indQQQNegM.Select(i => qqqRetsM[i]).ToArray();
                double[] spyRetsNegM = indSPYNegM.Select(i => spyRetsM[i]).ToArray();
                noDaysQQQPosMonthly[iMonths] = qqqRetsPosM.Length;
                noDaysSPYPosMonthly[iMonths] = spyRetsPosM.Length;
                noDaysQQQNegMonthly[iMonths] = qqqRetsNegM.Length;
                noDaysSPYNegMonthly[iMonths] = spyRetsNegM.Length;
                double varQQQM = ArrayStatistics.Variance(qqqRetsM);
                double varSPYM = ArrayStatistics.Variance(spyRetsM);
                double varQQQPosM = ArrayStatistics.Variance(qqqRetsPosM);
                double varSPYPosM = ArrayStatistics.Variance(spyRetsPosM);
                double varQQQNegM = ArrayStatistics.Variance(qqqRetsNegM);
                double varSPYNegM = ArrayStatistics.Variance(spyRetsNegM);

                for (int kAssets = 0; kAssets < noGChs + 1; kAssets++)
                {
                    if (dateYearsMonthsEndInd[iMonths] >= startGchDat[kAssets] + 4)
                    {
                        double[] assRetsM = quotesRets2[kAssets].GetRange(dateYearsMonthsEndInd[iMonths - 1] + 1, dateYearsMonthsEndInd[iMonths] - dateYearsMonthsEndInd[iMonths - 1]).ToArray();
                        double[] assRetsPosQQQM = indQQQPosM.Select(i => assRetsM[i]).ToArray();
                        double[] assRetsNegQQQM = indQQQNegM.Select(i => assRetsM[i]).ToArray();
                        double[] assRetsPosSPYM = indSPYPosM.Select(i => assRetsM[i]).ToArray();
                        double[] assRetsNegSPYM = indSPYNegM.Select(i => assRetsM[i]).ToArray();
                        double covQQQM = ArrayStatistics.Covariance(qqqRetsM, assRetsM);
                        double covSPYM = ArrayStatistics.Covariance(spyRetsM, assRetsM);
                        double covQQQPosM = ArrayStatistics.Covariance(qqqRetsPosM, assRetsPosQQQM);
                        double covSPYPosM = ArrayStatistics.Covariance(spyRetsPosM, assRetsPosSPYM);
                        double covQQQNegM = ArrayStatistics.Covariance(qqqRetsNegM, assRetsNegQQQM);
                        double covSPYNegM = ArrayStatistics.Covariance(spyRetsNegM, assRetsNegSPYM);
                        betaCalcQQQMonthly[iMonths, kAssets] = covQQQM / varQQQM;
                        betaCalcSPYMonthly[iMonths, kAssets] = covSPYM / varSPYM;
                        betaCalcQQQPosMonthly[iMonths, kAssets] = covQQQPosM / varQQQPosM;
                        betaCalcSPYPosMonthly[iMonths, kAssets] = covSPYPosM / varSPYPosM;
                        betaCalcQQQNegMonthly[iMonths, kAssets] = covQQQNegM / varQQQNegM;
                        betaCalcSPYNegMonthly[iMonths, kAssets] = covSPYNegM / varSPYNegM;
                    }

                }
            }
            int noTotalQQQPosDays = noDaysQQQPosYearly.Sum();
            int noTotalQQQNegDays = noDaysQQQNegYearly.Sum();
            int noTotalSPYPosDays = noDaysSPYPosYearly.Sum();
            int noTotalSPYNegDays = noDaysSPYNegYearly.Sum();

            //Request time (UTC)
            DateTime liveDateTime = DateTime.UtcNow;
            string liveDate = System.String.Empty;
            liveDate = liveDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime timeNowET = Utils.ConvertTimeFromUtcToEt(liveDateTime);
            string liveDateString = "Request time (UTC): " + liveDate;

            //Last data time (UTC)
            string lastDataTime = (quotesData[0][quotesData[0].Count - 1].Date.Date == liveDateTime.Date & timeNowET.TimeOfDay <= new DateTime(2000, 1, 1, 16, 15, 0).TimeOfDay) ? "Live data at " + liveDateTime.ToString("yyyy-MM-dd HH:mm:ss") : "Close price on " + quotesData[0][quotesData[0].Count - 1].Date.ToString("yyyy-MM-dd");
            string lastDataTimeString = "Last data time (UTC): " + lastDataTime;



            ////Creating input string for JavaScript.
            StringBuilder sb = new StringBuilder("{" + Environment.NewLine);
            sb.Append(@"""requestTime"": """ + liveDateString);
            sb.Append(@"""," + Environment.NewLine + @"""lastDataTime"": """ + lastDataTimeString);

            //sb.Append(@"""," + Environment.NewLine + @"""volLBPeri"": """ + volLBPeriod);
            //sb.Append(@"""," + Environment.NewLine + @"""retHistLBPeri"": """ + retHistLB);

            sb.Append(@"""," + Environment.NewLine + @"""betaLBPeris"": """);
            for (int i = 0; i < betaLBStr.Length - 1; i++)
                sb.Append(betaLBStr[i] + ", ");
            sb.Append(betaLBStr[betaLBStr.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""retLBPeris"": """);
            for (int i = 0; i < retLB.Length - 1; i++)
                sb.Append(retLBStr[i] + ", ");
            sb.Append(retLBStr[retLBStr.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""retLBPerisNo"": """);
            for (int i = 0; i < retLB.Length - 1; i++)
                sb.Append(retLB[i] + ", ");
            sb.Append(retLB[retLB.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""assetNames"": """);
            for (int i = 0; i < allAssetList.Length - 1; i++)
                sb.Append(allAssetList[i] + ", ");
            sb.Append(allAssetList[allAssetList.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""assetNames2"": """);
            for (int i = 0; i < allAssetList2.Length - 1; i++)
                sb.Append(allAssetList2[i] + ", ");
            sb.Append(allAssetList2[allAssetList2.Length - 1]);


            sb.Append(@"""," + Environment.NewLine + @"""quotesDateVector"": """);
            for (int i = 0; i < quotesDateVec.Length - 1; i++)
                sb.Append(quotesDateVec[i].ToString("yyyy-MM-dd") + ", ");
            sb.Append(quotesDateVec[quotesDateVec.Length - 1].ToString("yyyy-MM-dd"));

            sb.Append(@"""," + Environment.NewLine + @"""quotesDateMEVector"": """);
            for (int i = 0; i < quotesDateMEVec.Length - 1; i++)
                sb.Append(quotesDateMEVec[i].ToString("yyyy-MM-dd") + ", ");
            sb.Append(quotesDateMEVec[quotesDateMEVec.Length - 1].ToString("yyyy-MM-dd"));

            //sb.Append(@"""," + Environment.NewLine + @"""dailyVolDrags"": """);
            //for (int i = 0; i < assVolDrags[0].Count; i++)
            //{
            //    sb.Append("");
            //    for (int j = 0; j < assVolDrags.Count - 1; j++)
            //    {
            //        sb.Append(Math.Round(assVolDrags[j][i]*100,2).ToString() + "%, ");
            //    }
            //    sb.Append(Math.Round(assVolDrags[assVolDrags.Count - 1][i]*100,2).ToString() + "%");
            //    if (i < assVolDrags[0].Count - 1)
            //    {
            //        sb.Append("ß ");
            //    }
            //}

            //sb.Append(@"""," + Environment.NewLine + @"""dailyVIXMas"": """);
            //for (int i = 0; i < vixLevel.Length - 1; i++)
            //    sb.Append(Math.Round(vixLevel[i],2).ToString() + ", ");
            //sb.Append(Math.Round(vixLevel[vixLevel.Length - 1],2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearList"": """);
            for (int i = 0; i < dateYearsDist.Length - 1; i++)
                sb.Append(dateYearsDist[i] + ", ");
            sb.Append(dateYearsDist[dateYearsDist.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""yearMonthList"": """);
            for (int i = 0; i < dateYearsMonthsDist.Length - 1; i++)
                sb.Append(dateYearsMonthsDist[i] + ", ");
            sb.Append(dateYearsMonthsDist[dateYearsMonthsDist.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""betaCalcQQQ"": """);
            for (int i = 0; i < betaCalcQQQ.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQ.GetLength(1) - 1; j++)
                {
                    for (int k = 0; k < betaCalcQQQ.GetLength(2); k++)
                    {
                        sb.Append(Math.Round(betaCalcQQQ[i, j, k], 2).ToString() + ", ");
                    }
                }
                for (int k = 0; k < betaCalcQQQ.GetLength(2)-1; k++)
                {
                    sb.Append(Math.Round(betaCalcQQQ[i, betaCalcQQQ.GetLength(1) - 1, k], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQ[i, betaCalcQQQ.GetLength(1) - 1, betaCalcQQQ.GetLength(2) - 1], 2).ToString());
                if (i < betaCalcQQQ.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""betaCalcSPY"": """);
            for (int i = 0; i < betaCalcSPY.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPY.GetLength(1) - 1; j++)
                {
                    for (int k = 0; k < betaCalcSPY.GetLength(2); k++)
                    {
                        sb.Append(Math.Round(betaCalcSPY[i, j, k], 2).ToString() + ", ");
                    }
                }
                for (int k = 0; k < betaCalcSPY.GetLength(2) - 1; k++)
                {
                    sb.Append(Math.Round(betaCalcSPY[i, betaCalcSPY.GetLength(1) - 1, k], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPY[i, betaCalcSPY.GetLength(1) - 1, betaCalcSPY.GetLength(2) - 1], 2).ToString());
                if (i < betaCalcSPY.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }


            sb.Append(@"""," + Environment.NewLine + @"""betaCalcQQQCurr"": """);
            for (int i = 0; i < betaCalcQQQCurr.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQCurr.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQCurr[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQCurr[i, betaCalcQQQCurr.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQCurr.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""betaCalcSPYCurr"": """);
            for (int i = 0; i < betaCalcSPYCurr.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYCurr.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYCurr[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYCurr[i, betaCalcSPYCurr.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYCurr.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""yearlyBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQYearly[i, betaCalcQQQYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQMonthly[i, betaCalcQQQMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""yearlyBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYYearly[i, betaCalcSPYYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYMonthly[i, betaCalcSPYMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }


            sb.Append(@"""," + Environment.NewLine + @"""yearlyPosBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQPosYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQPosYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQPosYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQPosYearly[i, betaCalcQQQPosYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQPosYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyPosBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQPosMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQPosMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQPosMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQPosMonthly[i, betaCalcQQQPosMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQPosMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""yearlyNegBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQNegYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQNegYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQNegYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQNegYearly[i, betaCalcQQQNegYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQNegYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyNegBetasQQQ"": """);
            for (int i = 0; i < betaCalcQQQNegMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcQQQNegMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcQQQNegMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcQQQNegMonthly[i, betaCalcQQQNegMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcQQQNegMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""yearlyPosBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYPosYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYPosYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYPosYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYPosYearly[i, betaCalcSPYPosYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYPosYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyPosBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYPosMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYPosMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYPosMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYPosMonthly[i, betaCalcSPYPosMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYPosMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""yearlyNegBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYNegYearly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYNegYearly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYNegYearly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYNegYearly[i, betaCalcSPYNegYearly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYNegYearly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""monthlyNegBetasSPY"": """);
            for (int i = 0; i < betaCalcSPYNegMonthly.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < betaCalcSPYNegMonthly.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(betaCalcSPYNegMonthly[i, j], 2).ToString() + ", ");
                }
                sb.Append(Math.Round(betaCalcSPYNegMonthly[i, betaCalcSPYNegMonthly.GetLength(1) - 1], 2).ToString());
                if (i < betaCalcSPYNegMonthly.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }
            //sb.Append(@"""," + Environment.NewLine + @"""yearlyVIXAvgs"": """);
            //for (int i = 0; i < dateYearsVixAvgs.Length - 1; i++)
            //    sb.Append(Math.Round(dateYearsVixAvgs[i], 2).ToString() + ", ");
            //sb.Append(Math.Round(dateYearsVixAvgs[dateYearsVixAvgs.Length - 1], 2).ToString());

            //sb.Append(@"""," + Environment.NewLine + @"""monthlyVIXAvgs"": """);
            //for (int i = 0; i < dateYearsMonthsVixAvgs.Length - 1; i++)
            //    sb.Append(Math.Round(dateYearsMonthsVixAvgs[i], 2).ToString() + ", ");
            //sb.Append(Math.Round(dateYearsMonthsVixAvgs[dateYearsMonthsVixAvgs.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearlyCounts"": """);
            for (int i = 0; i < dateYearsCount.Length - 1; i++)
                sb.Append(dateYearsCount[i].ToString() + ", ");
            sb.Append(dateYearsCount[dateYearsCount.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""noTotalDays"": """ + noTotalDays);

            sb.Append(@"""," + Environment.NewLine + @"""noTotalQQQPosDays"": """ + noTotalQQQPosDays);
            sb.Append(@"""," + Environment.NewLine + @"""noTotalQQQNegDays"": """ + noTotalQQQNegDays);
            sb.Append(@"""," + Environment.NewLine + @"""noTotalSPYPosDays"": """ + noTotalSPYPosDays);
            sb.Append(@"""," + Environment.NewLine + @"""noTotalSPYNegDays"": """ + noTotalSPYNegDays);

            sb.Append(@"""," + Environment.NewLine + @"""monthlyCounts"": """);
            for (int i = 0; i < dateYearsMonthsCount.Length - 1; i++)
                sb.Append(dateYearsMonthsCount[i].ToString() + ", ");
            sb.Append(dateYearsMonthsCount[dateYearsMonthsCount.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearlyQQQPosCounts"": """);
            for (int i = 0; i < noDaysQQQPosYearly.Length - 1; i++)
                sb.Append(noDaysQQQPosYearly[i].ToString() + ", ");
            sb.Append(noDaysQQQPosYearly[noDaysQQQPosYearly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearlyQQQNegCounts"": """);
            for (int i = 0; i < noDaysQQQNegYearly.Length - 1; i++)
                sb.Append(noDaysQQQNegYearly[i].ToString() + ", ");
            sb.Append(noDaysQQQNegYearly[noDaysQQQNegYearly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearlySPYPosCounts"": """);
            for (int i = 0; i < noDaysSPYPosYearly.Length - 1; i++)
                sb.Append(noDaysSPYPosYearly[i].ToString() + ", ");
            sb.Append(noDaysSPYPosYearly[noDaysSPYPosYearly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""yearlySPYNegCounts"": """);
            for (int i = 0; i < noDaysSPYNegYearly.Length - 1; i++)
                sb.Append(noDaysSPYNegYearly[i].ToString() + ", ");
            sb.Append(noDaysSPYNegYearly[noDaysSPYNegYearly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""monthlyQQQPosCounts"": """);
            for (int i = 0; i < noDaysQQQPosMonthly.Length - 1; i++)
                sb.Append(noDaysQQQPosMonthly[i].ToString() + ", ");
            sb.Append(noDaysQQQPosMonthly[noDaysQQQPosMonthly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""monthlyQQQNegCounts"": """);
            for (int i = 0; i < noDaysQQQNegMonthly.Length - 1; i++)
                sb.Append(noDaysQQQNegMonthly[i].ToString() + ", ");
            sb.Append(noDaysQQQNegMonthly[noDaysQQQNegMonthly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""monthlySPYPosCounts"": """);
            for (int i = 0; i < noDaysSPYPosMonthly.Length - 1; i++)
                sb.Append(noDaysSPYPosMonthly[i].ToString() + ", ");
            sb.Append(noDaysSPYPosMonthly[noDaysSPYPosMonthly.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""monthlySPYNegCounts"": """);
            for (int i = 0; i < noDaysSPYNegMonthly.Length - 1; i++)
                sb.Append(noDaysSPYNegMonthly[i].ToString() + ", ");
            sb.Append(noDaysSPYNegMonthly[noDaysSPYNegMonthly.Length - 1].ToString());

            //sb.Append(@"""," + Environment.NewLine + @"""vixAvgTotal"": """ + Math.Round(vixAvgTotal,2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaQQQTotalVec"": """);
            for (int i = 0; i < betaQQQTotal.Length - 1; i++)
                sb.Append(Math.Round(betaQQQTotal[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaQQQTotal[betaQQQTotal.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaSPYTotalVec"": """);
            for (int i = 0; i < betaSPYTotal.Length - 1; i++)
                sb.Append(Math.Round(betaSPYTotal[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaSPYTotal[betaSPYTotal.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaQQQTotalPosVec"": """);
            for (int i = 0; i < betaCalcQQQPosAll.Length - 1; i++)
                sb.Append(Math.Round(betaCalcQQQPosAll[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaCalcQQQPosAll[betaCalcQQQPosAll.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaQQQTotalNegVec"": """);
            for (int i = 0; i < betaCalcQQQNegAll.Length - 1; i++)
                sb.Append(Math.Round(betaCalcQQQNegAll[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaCalcQQQNegAll[betaCalcQQQNegAll.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaSPYTotalPosVec"": """);
            for (int i = 0; i < betaCalcSPYPosAll.Length - 1; i++)
                sb.Append(Math.Round(betaCalcSPYPosAll[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaCalcSPYPosAll[betaCalcSPYPosAll.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""betaSPYTotalNegVec"": """);
            for (int i = 0; i < betaCalcSPYNegAll.Length - 1; i++)
                sb.Append(Math.Round(betaCalcSPYNegAll[i], 2).ToString() + ", ");
            sb.Append(Math.Round(betaCalcSPYNegAll[betaCalcSPYNegAll.Length - 1], 2).ToString());

            sb.Append(@"""," + Environment.NewLine + @"""histRetMtx"": """);
            for (int i = 0; i < histRet.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < histRet.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(histRet[i, j] * 100, 2).ToString() + "%, ");
                }
                sb.Append(Math.Round(histRet[i, histRet.GetLength(1) - 1] * 100, 2).ToString() + "%");
                if (i < histRet.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""histRet2Chart"": """);
            for (int i = 0; i < histRet2.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < histRet2.GetLength(1) - 1; j++)
                {
                    sb.Append(Math.Round(histRet2[i, j] * 100, 2).ToString() + "%, ");
                }
                sb.Append(Math.Round(histRet2[i, histRet2.GetLength(1) - 1] * 100, 2).ToString() + "%");
                if (i < histRet2.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.AppendLine(@"""" + Environment.NewLine + @"}");

            var asdfa = sb.ToString(); //testing created string to JS

            return sb.ToString();

        }


    }
}
