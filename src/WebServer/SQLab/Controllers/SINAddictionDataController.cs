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
    public class SINAddictionDataController : Controller
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

        //Get event, current position data from Google Spreadsheet - George
        [HttpGet]
        public ActionResult UberTAAGChGoogleApiGsheet1(string p_usedGSheetRef)
        {
            Utils.Logger.Info("UberTAAGChGoogleApiGsheet1() BEGIN");

            string valuesFromGSheetStr = "Error. Make sure GoogleApiKeyKey, GoogleApiKeyKey is in SQLab.WebServer.SQLab.NoGitHub.json !";
            if (!String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyName"]) && !String.IsNullOrEmpty(Utils.Configuration["GoogleApiKeyKey"]))
            {
                if (!Utils.DownloadStringWithRetry(out valuesFromGSheetStr, p_usedGSheetRef + Utils.Configuration["GoogleApiKeyKey"], 3, TimeSpan.FromSeconds(2), true))
                    valuesFromGSheetStr = "Error in DownloadStringWithRetry().";
            }
            
            Utils.Logger.Info("UberTAAGChGoogleApiGsheet1() END");
            return Content($"<HTML><body>UberTAAGChGoogleApiGsheet1() finished OK. <br> Received data: '{valuesFromGSheetStr}'</body></HTML>", "text/html");
        }

        //Selecting, splitting data got from GSheet
        public static Tuple< int[], int[]> GSheetConverter(string p_gSheetString, string[] p_allAssetList)
        {
            string[] gSheetTableRows = p_gSheetString.Split(new string[] { "[" }, StringSplitOptions.RemoveEmptyEntries);
            string currPosRaw = gSheetTableRows[3];
            currPosRaw = currPosRaw.Replace("\n", "").Replace("]", "").Replace("\",", "BRB").Replace("\"", "").Replace(" ", "").Replace(",", "");
            string[] currPos = currPosRaw.Split(new string[] { "BRB" }, StringSplitOptions.RemoveEmptyEntries);
            string[] currPosAP = new string[p_allAssetList.Length];
            Array.Copy(currPos, 2, currPosAP, 0, p_allAssetList.Length);
            int currPosDate = Int32.Parse(currPos[0]);
            int currPosCash = Int32.Parse(currPos[currPos.Length - 3]);
            int[] currPosDateCash = new int[] {currPosDate,currPosCash };
            int[] currPosAssets = Array.ConvertAll(currPosAP, int.Parse);
                        
            Tuple< int[], int[]> gSheetResFinal = Tuple.Create(currPosDateCash, currPosAssets);

            return gSheetResFinal;
        }

        //Downloading price data from SQL Server
        public static Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, List<SQLab.Controllers.QuickTester.Strategies.DailyData>> DataSQDBG(string[] p_allAssetList)
        {
            List<string> tickersNeeded = p_allAssetList.ToList();
            DateTime endTimeUtc = DateTime.UtcNow.AddDays(10);
            DateTime endTimeUtc2 = endTimeUtc.AddDays(-11);
            DateTime endTimeUtc3 = endTimeUtc.AddDays(-12);
            DateTime startTimeUtc = endTimeUtc.AddDays(-500);

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
        public static Tuple<double[], double[,]> TaaWeights(IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> p_taaWeightsData, int[] p_pctChannelLookbackDays, int p_histVolLookbackDays, int p_thresholdLower)
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

            IEnumerable<DateTime> taaWeightDateVec = p_taaWeightsData[0].GetRange(p_taaWeightsData[0].Count-nDays ,nDays).Select(r => r.Date);
            DateTime[] taaWeightDateArray = taaWeightDateVec.ToArray();
            DateTime startMatlabDate = DateTime.ParseExact("1900/01/01", "yyyy/MM/dd", CultureInfo.InvariantCulture);

            double[] taaWeightMatlabDateVec = new double[taaWeightDateVec.Count()];
            for (int i = 0; i < taaWeightMatlabDateVec.Length; i++)
            {
                taaWeightMatlabDateVec[i] = (taaWeightDateArray[i] - startMatlabDate).TotalDays + 693962;
            }

            Tuple<double[],double[,]> taaWeightResults = Tuple.Create(taaWeightMatlabDateVec, dailyAssetWeights);
            return taaWeightResults;
        }
        
       
        public string GetStr()
        {
            //Defining asset lists.
            //string[] allAssetList = new string[]{ "MDY", "ILF", "FEZ", "EEM", "EPP", "VNQ", "TLT" }; //TLT is used as a cashEquivalent
            string[] allAssetList = new string[] { "ABEV", "ATVI", "EA", "GWPH", "KSHB", "LMT", "MJ", "MO", "MTCH", "PDRDY", "PM", "SPRWF", "STZ", "TLT" }; //TLT is used as a cashEquivalent

            string titleString ="SIN, Addiction";
            
            string usedGSheetRef = "https://sheets.googleapis.com/v4/spreadsheets/1ugql_-IXXVrU7M2TtU4wPaDELH5M6NQXy82fwZgY2yU/values/A1:Z2000?key=";
            string usedGSheet2Ref = "https://docs.google.com/spreadsheets/d/1ugql_-IXXVrU7M2TtU4wPaDELH5M6NQXy82fwZgY2yU/edit?usp=sharing";
            string usedGDocRef = "https://docs.google.com/document/d/1dBHg3-McaHeCtxCTZdJhTKF5NPaixXYjEngZ4F2_ZBE/edit?usp=sharing";
            

            int thresholdLower = 25; //Upper threshold is 100-thresholdLower.
            int[] lookbackDays = new int[] { 60, 120, 180, 252 };
            int volDays = 40;

           //Collecting and splitting price data got from SQL Server
            Tuple<IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>>, List<SQLab.Controllers.QuickTester.Strategies.DailyData>> dataListTupleFromSQServer = DataSQDBG(allAssetList);

            IList<List<SQLab.Controllers.QuickTester.Strategies.DailyData>> quotesData =dataListTupleFromSQServer.Item1;
            List<SQLab.Controllers.QuickTester.Strategies.DailyData> cashEquivalentQuotesData = dataListTupleFromSQServer.Item2;

            //Calculating basic weights based on percentile channels - base Varadi TAA
            Tuple<double[], double[,]> taaWeightResultsTuple = TaaWeights(quotesData, lookbackDays, volDays, thresholdLower);
                        
            ////Setting last data date
                //double lastDataDate = (clmtRes[0][clmtRes[0].Length-1] == taaWeightResultsTuple.Item1[taaWeightResultsTuple.Item1.Length-1]) ? clmtRes[0][clmtRes[0].Length-1] : 0;

            //Get, split and convert GSheet data
            var gSheetReadResult = UberTAAGChGoogleApiGsheet1(usedGSheetRef);
            string gSheetString=((Microsoft.AspNetCore.Mvc.ContentResult)gSheetReadResult).Content;

            Tuple< int[], int[]> gSheetResToFinCalc = GSheetConverter(gSheetString, allAssetList);

            
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
                currPosInt[jCols] = gSheetResToFinCalc.Item2[jCols];
                currPosValue[jCols] = quotesData[jCols][quotesData[0].Count - 1].AdjClosePrice * currPosInt[jCols];
            }
            currPosInt[currPosInt.Length - 2] = gSheetResToFinCalc.Item2[gSheetResToFinCalc.Item2.Length - 1];
            currPosInt[currPosInt.Length - 1] = gSheetResToFinCalc.Item1[1];
            currPosValue[currPosValue.Length - 2] = cashEquivalentQuotesData[quotesData[0].Count - 1].AdjClosePrice * gSheetResToFinCalc.Item2[gSheetResToFinCalc.Item2.Length - 1];
            currPosValue[currPosValue.Length - 1] = gSheetResToFinCalc.Item1[1];
            currPV = Math.Round(currPosValue.Sum());

            double[] nextPosValue = new double[allAssetList.Length + 1];
            for (int jCols = 0; jCols < nextPosValue.Length - 2; jCols++)
            {
                nextPosValue[jCols] = currPV * taaWeightResultsTuple.Item2[taaWeightResultsTuple.Item2.GetLength(0) - 1, jCols];
            }
            nextPosValue[nextPosValue.Length - 2] = Math.Max(0, currPV - nextPosValue.Take(nextPosValue.Length - 2).ToArray().Sum());
            nextPosValue[nextPosValue.Length - 1] = currPV - nextPosValue.Take(nextPosValue.Length - 1).ToArray().Sum();

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


            //Position weights in the last 20 days
            string[,] prevPosMtx = new string[taaWeightResultsTuple.Item2.GetLength(0) + 1, allAssetList.Length + 2];
            for (int iRows = 0; iRows < prevPosMtx.GetLength(0) - 1; iRows++)
            {
                DateTime assDate = startMatlabDate.AddDays(taaWeightResultsTuple.Item1[iRows] - 693962);
                string assDateString = System.String.Empty;
                assDateString = assDate.ToString("yyyy-MM-dd");
                prevPosMtx[iRows, 0] = assDateString;

                double assetWeightSum = 0;
                for (int jCols = 0; jCols < prevPosMtx.GetLength(1) - 3; jCols++)
                {
                    assetWeightSum += taaWeightResultsTuple.Item2[iRows, jCols];
                    prevPosMtx[iRows, jCols + 1] = Math.Round(taaWeightResultsTuple.Item2[iRows, jCols] * 100.0, 2).ToString() + "%";
                }
                prevPosMtx[iRows, prevPosMtx.GetLength(1) - 1] = Math.Round(Math.Max((1.0 - assetWeightSum), 0) * 100.0, 2).ToString() + "%";
                prevPosMtx[iRows, prevPosMtx.GetLength(1) - 1] = Math.Round((1.0 - assetWeightSum - Math.Max((1.0 - assetWeightSum), 0)) * 100.0, 2).ToString() + "%";
            }
            prevPosMtx[prevPosMtx.GetLength(0) - 1, 0] = "";
            for (int jCols = 0; jCols < prevPosMtx.GetLength(1) - 2; jCols++)
            {
                prevPosMtx[prevPosMtx.GetLength(0) - 1, jCols + 1] = allAssetList[jCols];
            }
            prevPosMtx[prevPosMtx.GetLength(0) - 1, prevPosMtx.GetLength(1) - 1] = "Cash";
            

            for (int iRows = 0; iRows < prevPosMtx.GetLength(0) / 2; iRows++)
            {
                for (int jCols = 0; jCols < prevPosMtx.GetLength(1); jCols++)
                {
                    string tmp = prevPosMtx[iRows, jCols];
                    prevPosMtx[iRows, jCols] = prevPosMtx[prevPosMtx.GetLength(0) - iRows - 1, jCols];
                    prevPosMtx[prevPosMtx.GetLength(0) - iRows - 1, jCols] = tmp;
                }
            }


            //AssetPrice Changes in last 20 days to chart
            int assetChartLength = 20;
            string[,] assetChangesMtx = new string[assetChartLength + 1, allAssetList.Length];
            for (int iRows = 0; iRows < assetChangesMtx.GetLength(0); iRows++)
            {
                assetChangesMtx[iRows, 0] = quotesData[0][quotesData[0].Count - 1 - assetChartLength + iRows].Date.ToString("yyyy-MM-dd");
                for (int jCols = 0; jCols < assetChangesMtx.GetLength(1) - 1; jCols++)
                {
                    assetChangesMtx[iRows, jCols + 1] = Math.Round((quotesData[jCols][quotesData[jCols].Count - 1 - assetChartLength + iRows].AdjClosePrice / quotesData[jCols][quotesData[jCols].Count - 1 - assetChartLength].AdjClosePrice - 1) * 100.0, 2).ToString() + "%";
                }
            }

            //Daily changes, currently does not used.
            string[,] assetDailyChangesMtx = new string[assetChartLength + 1, allAssetList.Length];
            for (int iRows = 0; iRows < assetDailyChangesMtx.GetLength(0); iRows++)
            {
                assetDailyChangesMtx[iRows, 0] = quotesData[0][quotesData[0].Count - 1 - assetChartLength + iRows].Date.ToString("yyyy-MM-dd");
                for (int jCols = 0; jCols < assetDailyChangesMtx.GetLength(1) - 1; jCols++)
                {
                    assetDailyChangesMtx[iRows, jCols + 1] = Math.Round((quotesData[jCols][quotesData[jCols].Count - 1 - assetChartLength + iRows].AdjClosePrice / quotesData[jCols][quotesData[jCols].Count - 1 - assetChartLength + iRows - 1].AdjClosePrice - 1) * 100.0, 2).ToString() + "%";
                }
            }

            //Creating input string for JavaScript.
            StringBuilder sb = new StringBuilder("{" + Environment.NewLine);
            sb.Append(@"""titleCont"": """ + titleString);
            sb.Append(@"""," + Environment.NewLine + @"""requestTime"": """ + liveDateString);
            sb.Append(@"""," + Environment.NewLine + @"""lastDataTime"": """ + lastDataTimeString);
            sb.Append(@"""," + Environment.NewLine + @"""currentPV"": """ + currPV.ToString("#,##0"));
            sb.Append(@"""," + Environment.NewLine + @"""currentPVDate"": """ + currPosDateString);
            sb.Append(@"""," + Environment.NewLine + @"""gDocRef"": """ + usedGDocRef);
            sb.Append(@"""," + Environment.NewLine + @"""gSheetRef"": """ + usedGSheet2Ref);

            sb.Append(@"""," + Environment.NewLine + @"""assetNames"": """);
            for (int i = 0; i < allAssetList.Length - 1; i++)
                sb.Append(allAssetList[i] + ", ");
            sb.Append(allAssetList[allAssetList.Length - 1]);

            sb.Append(@"""," + Environment.NewLine + @"""assetNames2"": """);
            for (int i = 0; i < allAssetList.Length; i++)
                sb.Append(allAssetList[i] + ", ");
            sb.Append("Cash");

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

            sb.Append(@"""," + Environment.NewLine + @"""prevPositionsMtx"": """);
            for (int i = 0; i < prevPosMtx.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < prevPosMtx.GetLength(1) - 1; j++)
                {
                    sb.Append(prevPosMtx[i, j] + ", ");
                }
                sb.Append(prevPosMtx[i, prevPosMtx.GetLength(1) - 1]);
                if (i < prevPosMtx.GetLength(0) - 1)
                {
                    sb.Append("ß ");
                }
            }

            sb.Append(@"""," + Environment.NewLine + @"""chartLength"": """ + assetChartLength);
            
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

            sb.Append(@"""," + Environment.NewLine + @"""assetDailyChangesToChartMtx"": """);
            for (int i = 0; i < assetDailyChangesMtx.GetLength(0); i++)
            {
                sb.Append("");
                for (int j = 0; j < assetDailyChangesMtx.GetLength(1) - 1; j++)
                {
                    sb.Append(assetDailyChangesMtx[i, j] + ", ");
                }
                sb.Append(assetDailyChangesMtx[i, assetDailyChangesMtx.GetLength(1) - 1]);
                if (i < assetDailyChangesMtx.GetLength(0) - 1)
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
