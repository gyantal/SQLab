﻿using Microsoft.AspNetCore.Mvc;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using SQCommon.MathNet;
using System.Numerics;

namespace SQLab.Controllers
{
    public class SINAddictionDataController : Controller
    {
#if !DEBUG
        [Authorize]
#endif
        public ActionResult Index(string commo)
        {
           try
           {
                return Content(GetStr(commo), "text/html");
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

        
        //Get event, current position data from Google Spreadsheet - George
        [HttpGet]
        public ActionResult SINGoogleApiGsheet(string p_usedGSheetRef)
        {
            Utils.Logger.Info("SINGoogleApiGsheet() BEGIN");

            string valuesFromGSheetStr = "Error. Make sure GoogleApiKeyKey, GoogleApiKeyKey is in SQLab.WebServer.SQLab.NoGitHub.json !";
            if (!String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyName"]) && !String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyKey"]))
            {
                if (!Utils.DownloadStringWithRetry(out valuesFromGSheetStr, p_usedGSheetRef + Utils.Configuration["GoogleApiKeyKey"], 3, TimeSpan.FromSeconds(2), true))
                    valuesFromGSheetStr = "Error in DownloadStringWithRetry().";
            }
            
            Utils.Logger.Info("SINGoogleApiGsheet() END");
            return Content($"<HTML><body>SINGoogleApiGsheet() finished OK. <br> Received data: '{valuesFromGSheetStr}'</body></HTML>", "text/html");
        }

        //Selecting, splitting data got from GSheet
        public static Tuple< int[], string[], int[], bool[], int[], double[]> GSheetConverter(string p_gSheetString)
        {
            string[] gSheetTableRows = p_gSheetString.Split(new string[] { "[" }, StringSplitOptions.RemoveEmptyEntries);
            int assNum = gSheetTableRows.Length - 9;
            string[] assNameString = new string[assNum];
            string[] currPosAssString = new string[assNum];
            string[] currAssIndString = new string[assNum];
            for (int iRows = 4; iRows < gSheetTableRows.Length-5; iRows++)
            {
                string currPosRaw = gSheetTableRows[iRows];
                currPosRaw = currPosRaw.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
                string[] currPos = currPosRaw.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);
                assNameString[iRows - 4] = currPos[0];
                currPosAssString[iRows-4] = currPos[1];
                currAssIndString[iRows - 4] = currPos[2];
            }

            string currDateRaw = gSheetTableRows[2];
            currDateRaw = currDateRaw.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
            string[] currDateVec = currDateRaw.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);

            string currDateRaw2 = gSheetTableRows[3];
            currDateRaw2 = currDateRaw2.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
            string[] currDateVec2 = currDateRaw2.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);

            string currCashRaw = gSheetTableRows[gSheetTableRows.Length-5];
            currCashRaw = currCashRaw.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
            string[] currCashVec = currCashRaw.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);
                        
            string[] prevPVString = new string[4];
            for (int iRows = 0; iRows < prevPVString.Length; iRows++)
            {
                string currPosRaw = gSheetTableRows[gSheetTableRows.Length-4+iRows];
                currPosRaw = currPosRaw.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
                string[] currPos = currPosRaw.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);
                prevPVString[iRows] = currPos[1];
            }

            int currPosDate = Int32.Parse(currDateVec[1]);
            int currPosCash = Int32.Parse(currCashVec[1]);
            int[] currPosDateCash = new int[] { currPosDate, currPosCash };
            double leverage = Double.Parse(currDateVec[2]);
            double maxBondPerc = Double.Parse(currDateVec2[2]);
            double[] levMaxBondPerc = new double[] {leverage, maxBondPerc};

            int[] currPosAssets = Array.ConvertAll(currPosAssString, int.Parse);
            bool[] currAssInd = currAssIndString.Select(chr => chr == "1").ToArray();
            int[] prevPV = Array.ConvertAll(prevPVString, int.Parse);


            Tuple< int[], string[], int[], bool[], int[], double[]> gSheetResFinal = Tuple.Create(currPosDateCash, assNameString, currPosAssets, currAssInd, prevPV, levMaxBondPerc);

            return gSheetResFinal;
        }

        //Downloading price data from SQL Server
        public static Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, List<SQLab.Controllers.QuickTester.Strategies.DailyData>> DataSQDBG(string[] p_allAssetList)
        {
            List<string> tickersNeeded = p_allAssetList.ToList();
            DateTime endTimeUtc = DateTime.UtcNow.AddDays(10);
            DateTime endTimeUtc2 = endTimeUtc.AddDays(-11);
            DateTime endTimeUtc3 = endTimeUtc.AddDays(-12);
            DateTime startTimeUtc = endTimeUtc.AddDays(-420);

            var getAllQuotesTask = SQLab.Controllers.QuickTester.Strategies.StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(startTimeUtc, endTimeUtc, tickersNeeded);  
            Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = getAllQuotesTask.Result;

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
            List<SQLab.Controllers.QuickTester.Strategies.DailyData> cashEquivalentQuotes = null;

            quotes = getAllQuotesData.Item1.ToList().GetRange(0, p_allAssetList.Length-1);
            cashEquivalentQuotes = getAllQuotesData.Item1[p_allAssetList.Length-1];
            
            Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, List<SQLab.Controllers.QuickTester.Strategies.DailyData>> dataFromSQServer = Tuple.Create(quotes, cashEquivalentQuotes);
            
            return dataFromSQServer;
        }

        //Calculating TAA weights - based on George's TAA code
        public static Tuple<double[], double[,], double[]> TaaWeights(IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> p_taaWeightsData, int[] p_pctChannelLookbackDays, int p_histVolLookbackDays, int p_thresholdLower)
        {
            var dshd = p_taaWeightsData;
            int nAssets = p_taaWeightsData.Count;

            double[] assetScores = new double[nAssets];
            double[] assetHV = new double[nAssets];
            double[] assetWeights = new double[nAssets];
            double[] assetWeights2 = new double[nAssets];
            double[,] assetPctChannelsUpper = new double[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each 
            double[,] assetPctChannelsLower = new double[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each
            sbyte[,] assetPctChannelsSignal = new sbyte[nAssets, p_pctChannelLookbackDays.Length];  // for assets and for each
            int startNumDay = p_pctChannelLookbackDays.Max()-1;
            double thresholdLower = p_thresholdLower / 100.0;
            double thresholdUpper = 1-thresholdLower;

            int nDays = p_taaWeightsData[0].Count - startNumDay;
            double[,] dailyAssetWeights = new double[nDays,nAssets];
            double[,] dailyAssetScores = new double[nDays, nAssets];
            double[,] dailyAssetHv = new double[nDays, nAssets];
            for (int iDay = 0; iDay < nDays; iDay++)
            {
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    double assetPrice = p_taaWeightsData[iAsset][startNumDay + iDay].AdjClosePrice;
                    for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                    {
                        // A long position would be initiated if the price exceeds the 75th percentile of prices over the last “n” days.The position would be closed if the price falls below the 25th percentile of prices over the last “n” days.
                        var usedQuotes = p_taaWeightsData[iAsset].GetRange(startNumDay + iDay - (p_pctChannelLookbackDays[iChannel] - 1), p_pctChannelLookbackDays[iChannel]).Select(r => r.AdjClosePrice);
                        assetPctChannelsLower[iAsset, iChannel] = Statistics.Quantile(usedQuotes, thresholdLower);
                        assetPctChannelsUpper[iAsset, iChannel] = Statistics.Quantile(usedQuotes, thresholdUpper);
                        if (assetPrice < assetPctChannelsLower[iAsset, iChannel])
                        assetPctChannelsSignal[iAsset, iChannel] = -1;
                        else if (assetPrice > assetPctChannelsUpper[iAsset, iChannel])
                        assetPctChannelsSignal[iAsset, iChannel] = 1;
                        else if (iDay==0)
                        assetPctChannelsSignal[iAsset, iChannel] = 1;
                    }
                }

                // Calculate assetWeights
                double totalWeight = 0.0;
                
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    sbyte compositeSignal = 0;    // For every stocks, sum up the four signals every day. This sum will be -4, -2, 0, +2 or +4.
                    for (int iChannel = 0; iChannel < p_pctChannelLookbackDays.Length; iChannel++)
                    {
                        compositeSignal += assetPctChannelsSignal[iAsset, iChannel];
                    }
                    assetScores[iAsset] = compositeSignal / 4.0;    // Divide it by 4 to get a signal between -1 and +1 (this will be the “score”).

                    double[] hvPctChg = new double[p_histVolLookbackDays];
                    for (int iHv = 0; iHv < p_histVolLookbackDays; iHv++)
                    {
                        hvPctChg[p_histVolLookbackDays - iHv - 1] = p_taaWeightsData[iAsset][startNumDay + iDay - iHv].AdjClosePrice / p_taaWeightsData[iAsset][startNumDay + iDay - iHv - 1].AdjClosePrice - 1;
                    }
                    // Balazs: uses "corrected sample standard deviation"; corrected: dividing by 19, not 20; He doesn't annualize. He uses daily StDev
                    assetHV[iAsset] = ArrayStatistics.StandardDeviation(hvPctChg);  // Calculate the 20-day historical volatility of daily percentage changes for every stock.
                    assetWeights[iAsset] = assetScores[iAsset] / assetHV[iAsset];   // “Score/Vol” quotients will define the weights of the stocks. They can be 0 or negative as well. 
                                                                                    // there is an interesting observation here. Actually, it is a good behavour.
                                                                                    // If assetScores[i]=0, assetWeights[i] becomes 0, so we don't use its weight when p_isCashAllocatedForNonActives => TLT will not fill its Cash-place; NO TLT will be invested (if this is the only stock with 0 score), the portfolio will be 100% in other stocks. We are more Brave.
                                                                                    // However, if assetScores[i]<0 (negative), assetWeights[i] becoumes a proper negative number. It will be used in TotalWeight calculation => TLT will fill its's space. (if this is the only stock with negative score), TLT will be invested in its place; consequently the portfolio will NOT be 100% in other stocks. We are more defensive.
                    totalWeight += Math.Abs(assetWeights[iAsset]);      // Sum up the absolute values of the “Score/Vol” quotients. TotalWeight contains even the non-active assets so have have some cash.
                    assetWeights2[iAsset] = (assetWeights[iAsset]>=0) ?assetWeights[iAsset]:0.0;

                }
                for (int iAsset = 0; iAsset < nAssets; iAsset++)
                {
                    dailyAssetWeights[iDay, iAsset] = assetWeights2[iAsset]/totalWeight;
                    dailyAssetScores[iDay, iAsset] = assetScores[iAsset];
                    dailyAssetHv[iDay, iAsset] = assetHV[iAsset];
                }

            }

            double[] lastDayScores = new double[nAssets];
            for (int iAsset = 0; iAsset < nAssets; iAsset++)
            {
                lastDayScores[iAsset] = dailyAssetScores[dailyAssetScores.GetLength(0)-1,iAsset]; ;
            }

            IEnumerable<DateTime> taaWeightDateVec = p_taaWeightsData[0].GetRange(p_taaWeightsData[0].Count-nDays ,nDays).Select(r => r.Date);
            DateTime[] taaWeightDateArray = taaWeightDateVec.ToArray();
            DateTime startMatlabDate = DateTime.ParseExact("1900/01/01", "yyyy/MM/dd", CultureInfo.InvariantCulture);

            double[] taaWeightMatlabDateVec = new double[taaWeightDateVec.Count()];
            for (int i = 0; i < taaWeightMatlabDateVec.Length; i++)
            {
                taaWeightMatlabDateVec[i] = (taaWeightDateArray[i] - startMatlabDate).TotalDays + 693962;
            }

            Tuple<double[], double[,], double[]> taaWeightResults = Tuple.Create(taaWeightMatlabDateVec, dailyAssetWeights, lastDayScores);
            return taaWeightResults;
        }
        
       
        public string GetStr(string p_user)
        {
            //Defining asset lists.
            //string[] allAssetList = new string[]{ "MDY", "ILF", "FEZ", "EEM", "EPP", "VNQ", "TLT" }; //TLT is used as a cashEquivalent
            //string[] allAssetList = new string[] { "ATVI", "BA", "BTI", "BUD", "CARA", "CGC", "DEO", "EA", "GD", "GWPH", "HEI", "HEINY", "HON", "INSYQ", "LHX", "LMT", "LVS", "MO", "MTCH", "NOC", "PDRDY", "PM", "RICK", "RTN", "SCHYY", "SNE", "STZ", "TLRY", "UTX", "WYNN", "TLT" }; //TLT is used as a cashEquivalent

            string titleString = "";
            string usedGSheetRef = "";
            string usedGSheet2Ref = "";
            if (p_user=="AGY")
            {
                titleString = "AGY Version";
                usedGSheetRef = "https://sheets.googleapis.com/v4/spreadsheets/1Rw4xge-dpUwP0l_cYZeQFpOINPST8TQKVbNjySCapfE/values/A1:Z2000?key=";
                usedGSheet2Ref = "https://docs.google.com/spreadsheets/d/1Rw4xge-dpUwP0l_cYZeQFpOINPST8TQKVbNjySCapfE/edit?usp=sharing";
            }
            else
            {
                titleString = "Monthly rebalance, <b>The Charmat Rebalancing Method</b> (Trend following with Percentile Channel weights), Cash to TLT";
                usedGSheetRef = "https://sheets.googleapis.com/v4/spreadsheets/1JXMbEMAP5AOqB1FjdM8jpptXfpuOno2VaFVYK8A1eLo/values/A1:Z2000?key=";
                usedGSheet2Ref = "https://docs.google.com/spreadsheets/d/1JXMbEMAP5AOqB1FjdM8jpptXfpuOno2VaFVYK8A1eLo/edit?usp=sharing";
            }
            string usedGDocRef = "https://docs.google.com/document/d/1dBHg3-McaHeCtxCTZdJhTKF5NPaixXYjEngZ4F2_ZBE/edit?usp=sharing";

            //Get, split and convert GSheet data
            var gSheetReadResult = SINGoogleApiGsheet(usedGSheetRef);
            string gSheetString = ((Microsoft.AspNetCore.Mvc.ContentResult)gSheetReadResult).Content;
            Tuple<int[], string[], int[], bool[], int[], double[]> gSheetResToFinCalc = GSheetConverter(gSheetString);
            string[] allAssetList = gSheetResToFinCalc.Item2;

            //Parameters
            int thresholdLower = 25; //Upper threshold is 100-thresholdLower.
            int[] lookbackDays = new int[] { 30, 60, 120, 252 };
            int volDays = 20;
            int[] pastPerfDays = new int[] { 1, 5, 10, 21, 63, 126, 252};
            string[] pastPerfDaysString = new string[] { "1-Day", "1-Week", "2-Weeks", "1-Month", "3-Months", "6-Months", "1-Year" };
            double leverage = gSheetResToFinCalc.Item6[0];
            double maxBondPerc = gSheetResToFinCalc.Item6[1];

            //Collecting and splitting price data got from SQL Server
            Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, List<SQLab.Controllers.QuickTester.Strategies.DailyData>> dataListTupleFromSQServer = DataSQDBG(allAssetList);

            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> quotesData =dataListTupleFromSQServer.Item1;
            List<SQLab.Controllers.QuickTester.Strategies.DailyData> cashEquivalentQuotesData = dataListTupleFromSQServer.Item2;

                        // below commented code is for validation purpose Sin SqCore vs SqLab
            // // StringBuilder sbDebugData = new();
            // try
            // {
            //     StreamWriter sw = new StreamWriter("C:\\temp\\quotesData_SqLab.csv");
            //     for (int i = 0; i < allAssetList.Length - 1; i++)
            //     {
            //         sw.Write(allAssetList[i] + ": " + Environment.NewLine);
            //         List<SQLab.Controllers.QuickTester.Strategies.DailyData> prices = dataListTupleFromSQServer.Item1[i];
            //         string priceStr = String.Join('\t', prices.Select(r => r.Date.ToString() + ", " + r.AdjClosePrice.ToString() + Environment.NewLine));
            //         sw.Write(priceStr + Environment.NewLine);
            //     }
            //     sw.Close();
            // }
            // catch(Exception e)
            // {
            //     Console.WriteLine("Exception: " + e.Message);
            // }

            // ///....
            // StreamWriter sw1 = new StreamWriter("C:\\temp\\quotescashEquivalentQuotesData_SqLab.csv");
            // sw1.Write(allAssetList[allAssetList.Length - 1] + ": " + Environment.NewLine);
            // List<SQLab.Controllers.QuickTester.Strategies.DailyData> prices1 = dataListTupleFromSQServer.Item2;
            // string priceStr1 = String.Join('\t', prices1.Select(r => r.Date.ToString() + ", " + r.AdjClosePrice.ToString() + Environment.NewLine));
            // sw1.Write(priceStr1 + Environment.NewLine);
            // // Debug.WriteLine(sbDebugData.ToString());

            //Calculating basic weights based on percentile channels - base Varadi TAA
            Tuple<double[], double[,], double[]> taaWeightResultsTuple = TaaWeights(quotesData, lookbackDays, volDays, thresholdLower);
                   
                
                      
            //Request time (UTC)
            DateTime liveDateTime = DateTime.UtcNow;
            string liveDate = System.String.Empty;
            liveDate = liveDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime timeNowET = Utils.ConvertTimeFromUtcToEt(liveDateTime);
            string liveDateString = "Request time (UTC): " + liveDate;

            //Last data time (UTC)
            string lastDataTime = (quotesData[0][quotesData[0].Count - 1].Date.Date == liveDateTime.Date & timeNowET.TimeOfDay<=new DateTime(2000,1,1,16,15,0).TimeOfDay) ? "Live data at " + liveDateTime.ToString("yyyy-MM-dd HH:mm:ss") : "Close price on "+ quotesData[0][quotesData[0].Count - 1].Date.ToString("yyyy-MM-dd");
            string lastDataTimeString = "Last data time (UTC): "+lastDataTime;

            DateTime nextWeekday = (quotesData[0][quotesData[0].Count - 1].Date.Date == liveDateTime.Date & timeNowET.TimeOfDay <= new DateTime(2000, 1, 1, 16, 15, 0).TimeOfDay) ? liveDateTime.Date.AddDays(1) : quotesData[0][quotesData[0].Count - 1].Date.AddDays(1);

            //Current PV, Number of current and required shares
            DateTime startMatlabDate = DateTime.ParseExact("1900/01/01", "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime nextTradingDay = nextWeekday;
            string nextTradingDayString = System.String.Empty;
            nextTradingDayString = nextTradingDay.ToString("yyyy-MM-dd");
            DateTime currPosDate = startMatlabDate.AddDays(gSheetResToFinCalc.Item1[0] - 693962);
            string currPosDateString = System.String.Empty;
            currPosDateString = currPosDate.ToString("yyyy-MM-dd");

            double currPV;
            int[] currPosInt = new int[allAssetList.Length + 1];


            double[] currPosValue = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < currPosValue.Length - 2; jCols++)
            {
                currPosInt[jCols] = gSheetResToFinCalc.Item3[jCols];
                currPosValue[jCols] = quotesData[jCols][quotesData[0].Count - 1].AdjClosePrice * currPosInt[jCols];
            }
            currPosInt[currPosInt.Length - 2] = gSheetResToFinCalc.Item3[gSheetResToFinCalc.Item3.Length - 1];
            currPosInt[currPosInt.Length - 1] = gSheetResToFinCalc.Item1[1];
            currPosValue[currPosValue.Length - 2] = cashEquivalentQuotesData[quotesData[0].Count - 1].AdjClosePrice * gSheetResToFinCalc.Item3[gSheetResToFinCalc.Item3.Length - 1];
            currPosValue[currPosValue.Length - 1] = gSheetResToFinCalc.Item1[1];
            currPV = Math.Round(currPosValue.Sum());

            double[] nextPosValue = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < nextPosValue.Length - 2; jCols++)
            {
                nextPosValue[jCols] = (gSheetResToFinCalc.Item4[jCols])?currPV * taaWeightResultsTuple.Item2[taaWeightResultsTuple.Item2.GetLength(0) - 1, jCols]*leverage:0;
            }
            nextPosValue[nextPosValue.Length - 2] = Math.Min(Math.Max(0, currPV - nextPosValue.Take(nextPosValue.Length - 2).ToArray().Sum()/leverage),currPV*maxBondPerc)*leverage;
            nextPosValue[nextPosValue.Length - 1] = currPV - nextPosValue.Take(nextPosValue.Length - 1).ToArray().Sum();

            double currBondPerc = currPosValue[currPosValue.Length - 2] / currPV;
            double nextBondPerc = nextPosValue[nextPosValue.Length - 2] / (currPV*leverage);

            double[] nextPosInt = new double[nextPosValue.Length];
            for (int jCols = 0; jCols < nextPosInt.Length - 2; jCols++)
            {
                nextPosInt[jCols] = nextPosValue[jCols] / quotesData[jCols][quotesData[0].Count - 1].AdjClosePrice;
            }
            nextPosInt[nextPosInt.Length - 2] = nextPosValue[nextPosInt.Length - 2] / cashEquivalentQuotesData[quotesData[0].Count - 1].AdjClosePrice;
            nextPosInt[nextPosInt.Length - 1] = nextPosValue[nextPosInt.Length - 1];

            double[] posValueDiff = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < posValueDiff.Length; jCols++)
            {
                posValueDiff[jCols] = nextPosValue[jCols] - currPosValue[jCols];
            }

            double[] posIntDiff = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < posIntDiff.Length; jCols++)
            {
                posIntDiff[jCols] = nextPosInt[jCols] - currPosInt[jCols];
            }



            //Profits

            int[] prevPV = gSheetResToFinCalc.Item5;
            int boyPV = prevPV[0] + prevPV[1];
            int bomPV = prevPV[2] + prevPV[3];
            double ytdProfDoll = currPV - boyPV;
            double mtdProfDoll = currPV - bomPV;
            double ytdProfPerc = currPV/boyPV-1;
            double mtdProfPerc = currPV/bomPV-1;

            double[] prevDayPosValue = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < prevDayPosValue.Length - 2; jCols++)
            {
                prevDayPosValue[jCols] = quotesData[jCols][quotesData[0].Count - 2].AdjClosePrice * currPosInt[jCols];
            }
            prevDayPosValue[currPosValue.Length - 2] = cashEquivalentQuotesData[quotesData[0].Count - 2].AdjClosePrice * gSheetResToFinCalc.Item3[gSheetResToFinCalc.Item3.Length - 1];
            prevDayPosValue[currPosValue.Length - 1] = gSheetResToFinCalc.Item1[1];
            double prevDayPV = Math.Round(prevDayPosValue.Sum());

            double dailyProfDoll = currPV - prevDayPV;
            double dailyProfPerc = currPV/prevDayPV - 1;
                                 
            string dailyProfDollString = "";
            string dailyProfPercString = "";

            string dailyProfString = "";
            string dailyProfSign = "";
            if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && liveDateTime.ToString("yyyy-MM-dd") == quotesData[0][quotesData[0].Count - 1].Date.ToString("yyyy-MM-dd") && dailyProfDoll >= 0)
            {
                dailyProfString = "posDaily";
                dailyProfSign = "+$";
                dailyProfDollString = dailyProfDoll.ToString("#,##0");
                dailyProfPercString = Math.Round(dailyProfPerc*100,2).ToString();
            }
            else if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && liveDateTime.ToString("yyyy-MM-dd") == quotesData[0][quotesData[0].Count - 1].Date.ToString("yyyy-MM-dd") && dailyProfDoll < 0)
            {
                dailyProfString = "negDaily";
                dailyProfSign = "-$";
                dailyProfDollString = (-dailyProfDoll).ToString("#,##0");
                dailyProfPercString = Math.Round(dailyProfPerc * 100, 2).ToString();
            }
            else
            {
                dailyProfString = "notDaily";
                dailyProfSign = "N/A";
                dailyProfDollString = "";
                dailyProfPercString = "";
            }


            string monthlyProfDollString = "";
            string monthlyProfPercString = "";

            string monthlyProfString = "";
            string monthlyProfSign = "";
            if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && mtdProfDoll >= 0)
            {
                monthlyProfString = "posMonthly";
                monthlyProfSign = "+$";
                monthlyProfDollString = mtdProfDoll.ToString("#,##0");
                monthlyProfPercString = Math.Round(mtdProfPerc * 100, 2).ToString();
            }
            else if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && mtdProfDoll < 0)
            {
                monthlyProfString = "negMonthly";
                monthlyProfSign = "-$";
                monthlyProfDollString = (-mtdProfDoll).ToString("#,##0");
                monthlyProfPercString = Math.Round(mtdProfPerc * 100, 2).ToString();
            }
            else
            {
                monthlyProfString = "notMonthly";
                monthlyProfSign = "N/A";
                monthlyProfDollString = "";
                monthlyProfPercString = "";
            }


            string yearlyProfDollString = "";
            string yearlyProfPercString = "";

            string yearlyProfString = "";
            string yearlyProfSign = "";
            if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && ytdProfDoll >= 0)
            {
                yearlyProfString = "posYearly";
                yearlyProfSign = "+$";
                yearlyProfDollString = ytdProfDoll.ToString("#,##0");
                yearlyProfPercString = Math.Round(ytdProfPerc * 100, 2).ToString();
            }
            else if ((currPosDateString != liveDateTime.ToString("yyyy-MM-dd")) && ytdProfDoll < 0)
            {
                yearlyProfString = "negYearly";
                yearlyProfSign = "-$";
                yearlyProfDollString = (-ytdProfDoll).ToString("#,##0");
                yearlyProfPercString = Math.Round(ytdProfPerc * 100, 2).ToString();
            }
            else
            {
                yearlyProfString = "notYearly";
                yearlyProfSign = "N/A";
                yearlyProfDollString = "";
                yearlyProfPercString = "";
            }



            //AssetPrice Changes in last x days

            string[,] assetChangesMtx = new string[allAssetList.Length, pastPerfDays.Length];
            for (int iRows = 0; iRows < assetChangesMtx.GetLength(0)-1; iRows++)
            {
                for (int jCols = 0; jCols < assetChangesMtx.GetLength(1); jCols++)
                {
                    assetChangesMtx[iRows, jCols] = Math.Round((quotesData[iRows][quotesData[0].Count-1].AdjClosePrice / quotesData[iRows][quotesData[0].Count - 1-pastPerfDays[jCols]].AdjClosePrice - 1) * 100.0, 2).ToString() + "%";
                }
            }
            for (int jCols = 0; jCols < assetChangesMtx.GetLength(1); jCols++)
            {
                assetChangesMtx[assetChangesMtx.GetLength(0)-1, jCols] = Math.Round((cashEquivalentQuotesData[cashEquivalentQuotesData.Count-1].AdjClosePrice / cashEquivalentQuotesData[cashEquivalentQuotesData.Count - 1 - pastPerfDays[jCols]].AdjClosePrice - 1) * 100.0, 2).ToString() + "%";
            }

            //Asset scores and weights on last day

            string[,] assetScoresMtx = new string[allAssetList.Length, 2];
            for (int iRows = 0; iRows < assetScoresMtx.GetLength(0) - 1; iRows++)
            {
                assetScoresMtx[iRows, 0] = Math.Round(taaWeightResultsTuple.Item3[iRows] * 100.0, 2).ToString() + "%";
                assetScoresMtx[iRows, 1] = Math.Round(taaWeightResultsTuple.Item2[taaWeightResultsTuple.Item2.GetLength(0)-1,iRows] * 100.0, 2).ToString() + "%";
            }
            assetScoresMtx[assetScoresMtx.GetLength(0) - 1, 0] = "---";
            assetScoresMtx[assetScoresMtx.GetLength(0) - 1, 1] = Math.Round(nextBondPerc*100,2).ToString() +"%";


            //Creating input string for JavaScript.
            StringBuilder sb = new StringBuilder("{" + Environment.NewLine);
            sb.Append(@"""titleCont"": """ + titleString);
            sb.Append(@"""," + Environment.NewLine + @"""requestTime"": """ + liveDateString);
            sb.Append(@"""," + Environment.NewLine + @"""lastDataTime"": """ + lastDataTimeString);
            sb.Append(@"""," + Environment.NewLine + @"""currentPV"": """ + currPV.ToString("#,##0"));
            sb.Append(@"""," + Environment.NewLine + @"""currentPVDate"": """ + currPosDateString);
            sb.Append(@"""," + Environment.NewLine + @"""gDocRef"": """ + usedGDocRef);
            sb.Append(@"""," + Environment.NewLine + @"""gSheetRef"": """ + usedGSheet2Ref);

            sb.Append(@"""," + Environment.NewLine + @"""dailyProfSig"": """ + dailyProfSign);
            sb.Append(@"""," + Environment.NewLine + @"""dailyProfAbs"": """ + dailyProfDollString);
            sb.Append(@"""," + Environment.NewLine + @"""dailyProfPerc"": """ + dailyProfPercString);
            sb.Append(@"""," + Environment.NewLine + @"""dailyProfString"": """ + dailyProfString);
            sb.Append(@"""," + Environment.NewLine + @"""monthlyProfSig"": """ + monthlyProfSign);
            sb.Append(@"""," + Environment.NewLine + @"""monthlyProfAbs"": """ + monthlyProfDollString);
            sb.Append(@"""," + Environment.NewLine + @"""monthlyProfPerc"": """ + monthlyProfPercString);
            sb.Append(@"""," + Environment.NewLine + @"""monthlyProfString"": """ + monthlyProfString);
            sb.Append(@"""," + Environment.NewLine + @"""yearlyProfSig"": """ + yearlyProfSign);
            sb.Append(@"""," + Environment.NewLine + @"""yearlyProfAbs"": """ + yearlyProfDollString);
            sb.Append(@"""," + Environment.NewLine + @"""yearlyProfPerc"": """ + yearlyProfPercString);
            sb.Append(@"""," + Environment.NewLine + @"""yearlyProfString"": """ + yearlyProfString);
            sb.Append(@"""," + Environment.NewLine + @"""currBondPerc"": """ + Math.Round(currBondPerc*100,2).ToString()+"%");
            sb.Append(@"""," + Environment.NewLine + @"""nextBondPerc"": """ + Math.Round(nextBondPerc * 100, 2).ToString() + "%");
            sb.Append(@"""," + Environment.NewLine + @"""leverage"": """ + Math.Round(leverage * 100, 2).ToString() + "%");
            sb.Append(@"""," + Environment.NewLine + @"""maxBondPerc"": """ + Math.Round(maxBondPerc * 100, 2).ToString() + "%");



            sb.Append(@"""," + Environment.NewLine + @"""assetNames"": """);
            for (int i = 0; i < allAssetList.Length - 1; i++)
                sb.Append(allAssetList[i] + ", ");
            sb.Append(allAssetList[allAssetList.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""assetNames2"": """);
            for (int i = 0; i < allAssetList.Length; i++)
                sb.Append(allAssetList[i] + ", ");
            sb.Append("Cash");

            sb.Append(@"""," + Environment.NewLine + @"""pastPerfDaysNum"": """);
            for (int i = 0; i < pastPerfDays.Length - 1; i++)
                sb.Append(pastPerfDays[i].ToString() + ", ");
            sb.Append(pastPerfDays[pastPerfDays.Length - 1].ToString());

            sb.Append(@"""," + Environment.NewLine + @"""pastPerfDaysName"": """);
            for (int i = 0; i < pastPerfDaysString.Length - 1; i++)
                sb.Append(pastPerfDaysString[i] + ", ");
            sb.Append(pastPerfDaysString[pastPerfDaysString.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""currPosNum"": """);
            for (int i = 0; i < currPosInt.Length - 1; i++)
                sb.Append(currPosInt[i].ToString() + ", ");
            sb.Append("$" + Math.Round(currPosInt[currPosInt.Length - 1] / 1000.0).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""currPosVal"": """);
            for (int i = 0; i < currPosValue.Length - 1; i++)
                sb.Append("$" + Math.Round(currPosValue[i] / 1000).ToString() + "K, ");
            sb.Append("$" + Math.Round(currPosValue[currPosValue.Length - 1] / 1000).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""nextPosNum"": """);
            for (int i = 0; i < nextPosInt.Length - 1; i++)
                sb.Append(Math.Round(nextPosInt[i]).ToString() + ", ");
            sb.Append("$" + Math.Round(nextPosInt[nextPosInt.Length - 1] / 1000).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""nextPosVal"": """);
            for (int i = 0; i < nextPosValue.Length - 1; i++)
                sb.Append("$" + Math.Round(nextPosValue[i] / 1000).ToString() + "K, ");
            sb.Append("$" + Math.Round(nextPosValue[nextPosValue.Length - 1] / 1000).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""posNumDiff"": """);
            for (int i = 0; i < posIntDiff.Length - 1; i++)
                sb.Append(Math.Round(posIntDiff[i]).ToString() + ", ");
            sb.Append("$" + Math.Round(posIntDiff[posIntDiff.Length - 1] / 1000).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""posValDiff"": """);
            for (int i = 0; i < posValueDiff.Length - 1; i++)
                sb.Append("$" + Math.Round(posValueDiff[i] / 1000).ToString() + "K, ");
            sb.Append("$" + Math.Round(posValueDiff[posValueDiff.Length - 1] / 1000).ToString() + "K");

            sb.Append(@"""," + Environment.NewLine + @"""nextTradingDay"": """ + nextTradingDayString);
            sb.Append(@"""," + Environment.NewLine + @"""currPosDate"": """ + currPosDateString);

            sb.Append(@"""," + Environment.NewLine + @"""assetChangesToChartMtx"": """);
            for (int i = 0; i < assetChangesMtx.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < assetChangesMtx.GetLength(1) - 1; j++)
                {
                    sb.Append(assetChangesMtx[i, j] + ", ");
                }
                sb.Append(assetChangesMtx[i, assetChangesMtx.GetLength(1) - 1]);
                if (i < assetChangesMtx.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""assetScoresMtx"": """);
            for (int i = 0; i < assetScoresMtx.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < assetScoresMtx.GetLength(1) - 1; j++)
                {
                    sb.Append(assetScoresMtx[i, j] + ", ");
                }
                sb.Append(assetScoresMtx[i, assetScoresMtx.GetLength(1) - 1]);
                if (i < assetScoresMtx.GetLength(0) - 1)
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
