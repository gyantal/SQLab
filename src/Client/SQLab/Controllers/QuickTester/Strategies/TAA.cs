using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    public class TAA
    {
        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, Dictionary<string, StringValues> p_allParamsDict)
        {
            if (p_strategyName != "TAA")
                return null;
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();

            // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
            string assetsStr = p_allParamsDict["Assets"][0];                                         // "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT"
            string assetsConstantLeverageStr = p_allParamsDict["AssetsConstantLeverage"][0];         // "1,1,1,-1,1.5,2,2"
            string rebalancingFrequencyStr = p_allParamsDict["RebalancingFrequency"][0];             // "Weekly,Fridays";   // "Daily, 2d"(trading days),"Weekly, Fridays", "Monthly, T+1"/"Monthly, T-1" (first/last trading day of the month), 
            string pctChannelLookbackDaysStr = p_allParamsDict["PctChannelLookbackDays"][0];         // "30-60-120-252"
            string pctChannelPctLimitStrs = p_allParamsDict["PctChannelPctLimits"][0];               // "30-70"
            string pctChannelIsConditionalStr = p_allParamsDict["PctChannelIsConditional"][0];       // "Yes"
            string histVolLookbackDaysStr = p_allParamsDict["HistVolLookbackDays"][0];               // "20d"
            string dynamicLeverageClmtParamsStr = p_allParamsDict["DynamicLeverageClmtParams"][0];   // "SPX 50/200-day MA; XLU/VTI"
            string uberVxxEventsParamsStr = p_allParamsDict["UberVxxEventsParams"][0];               // "FOMC;Holidays"

            string[] tickers = assetsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams, tickers.ToList());
            Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan> getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();
            var quotes = getAllQuotesData.Item1;

            string noteToUserCheckData = "", noteToUserBacktest = "", debugMessage = "", errorMessage = "";
            List<DailyData> pv = StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(quotes, tickers, ref noteToUserCheckData);

            DoBacktestInTheTimeInterval_TAA(quotes, tickers, pv, ref noteToUserBacktest);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               noteToUserCheckData + "***" + noteToUserBacktest, errorMessage,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }


        private static void DoBacktestInTheTimeInterval_TAA(IList<List<DailyData>> p_quotes, string[] p_tickers, List<DailyData> pv, ref string noteToUserBacktest)
        {
            // shift ShartDate when we have all the data for "Use 60,120,180, 252-day percentile channels"

            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            int[] iQ = new int[p_quotes.Count];
            for (int i = 0; i < p_quotes.Count; i++)
            {
                iQ[i] = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
            }

            double pvDaily = 100.0;
            double cash = pvDaily;
            double equalWeight = 1.0 / (double)p_quotes.Count;
            double[] assetPos = new double[p_quotes.Count];
            for (int i = 0; i < p_quotes.Count; i++)
            {
                assetPos[i] = pvDaily * equalWeight;        // weight can be 0.5 positive = 50%, or  negative = -0.5, -50%. In that case we short the asset.
                cash -= assetPos[i];    // if weight is positive, assetPos is positive, so we take it away from cash. Otherwise, we short the Asset, and cash is increased.
            }
            pv[0].ClosePrice = pvDaily; // on the date when the first quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            for (int iDay = 1; iDay < pv.Count(); iDay++)
            {
                pvDaily = cash;
                for (int i = 0; i < p_quotes.Count; i++)
                {
                    double assetChg = p_quotes[i][iQ[i] + iDay].ClosePrice / p_quotes[i][iQ[i] + iDay - 1].ClosePrice;
                    assetPos[i] *= assetChg;
                    pvDaily += assetPos[i];
                }

                //if (i % p_rebalancingTradingDays == 0)    // every periodic days
                //{


                //}

                pv[iDay].ClosePrice = pvDaily;
            }

            noteToUserBacktest = "Nothing.";
            //noteToUserBacktest = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        } // DoBacktestInTheTimeInterval_TAA





    }   // class
}
