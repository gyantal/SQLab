using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    public class AdaptiveUberVxx
    {
        public static async Task<string> GenerateQuickTesterResponse(GeneralStrategyParameters p_generalParams, string p_strategyName, Dictionary<string, StringValues> p_allParamsDict)
        {
            if (p_strategyName != "AdaptiveUberVxx")
                return null;
            Stopwatch stopWatchTotalResponse = Stopwatch.StartNew();

            // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
            //string assetsStr = p_allParamsDict["Assets"][0];                                         // "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT"

            Stopwatch stopWatch = Stopwatch.StartNew();
            var getAllQuotesTask = StrategiesCommon.GetHistoricalAndRealtimesQuotesAsync(p_generalParams.startDateUtc, p_generalParams.endDateUtc, (new string[] { "VXXB", "SPY" }).ToList());
            var getAllQuotesData = await getAllQuotesTask;
            stopWatch.Stop();

            string errorToUser = "", warningToUser = "", noteToUser = "", debugMessage = "";
            DateTime startDate, endDate;
            StrategiesCommon.DetermineBacktestPeriodCheckDataCorrectness(getAllQuotesData.Item1, new string[] { "VXXB", "SPY" }, ref warningToUser, out startDate, out endDate);
            List<DailyData> pv = StrategiesCommon.DeepCopyQuoteRange(getAllQuotesData.Item1[0], startDate, endDate);

            var vxxQoutes = getAllQuotesData.Item1[0];
            var spyQoutes = getAllQuotesData.Item1[1];
            DoBacktestInTheTimeInterval_AdaptiveUberVxx(vxxQoutes, spyQoutes, 0.001, 0.001, "Long", pv, ref noteToUser);

            stopWatchTotalResponse.Stop();
            StrategyResult strategyResult = StrategiesCommon.CreateStrategyResultFromPV(pv,
               warningToUser + "***" + noteToUser, errorToUser,
               debugMessage + String.Format("SQL query time: {0:000}ms", getAllQuotesData.Item2.TotalMilliseconds) + String.Format(", RT query time: {0:000}ms", getAllQuotesData.Item3.TotalMilliseconds) + String.Format(", All query time: {0:000}ms", stopWatch.Elapsed.TotalMilliseconds) + String.Format(", TotalC#Response: {0:000}ms", stopWatchTotalResponse.Elapsed.TotalMilliseconds));
            string jsonReturn = JsonConvert.SerializeObject(strategyResult);
            return jsonReturn;
        }


        private static void DoBacktestInTheTimeInterval_AdaptiveUberVxx(List<DailyData> vxxQoutes, List<DailyData> spyQoutes, double spyMinPctMove, double vxxMinPctMove, string longOrShortTrade, List<DailyData> pv, ref string noteToUser)
        {
            // temporary copy from private static void DoBacktestInTheTimeInterval_VXX_SPY_Controversial()

            bool? isTradeLongVXX = null;        // it means Cash
            if (String.Equals(longOrShortTrade, "Long"))
                isTradeLongVXX = true;
            else if (String.Equals(longOrShortTrade, "Short"))
                isTradeLongVXX = false;

            DateTime pvStartDate = pv[0].Date;
            DateTime pvEndDate = pv[pv.Count() - 1].Date;

            int iSpy = spyQoutes.FindIndex(row => row.Date == pvStartDate);
            int iVXX = vxxQoutes.FindIndex(row => row.Date == pvStartDate);


            double pvDaily = 100.0;
            pv[0].AdjClosePrice = pvDaily; // on the date when the quotes available: At the end of the first day, PV will be 1.0, because we trade at Market Close

            // on first day: short stock: we cannot check what was 'yesterday' %change
            //pv[1].ClosePrice = pvDaily;
            double vxxChgOnFirstDay = vxxQoutes[iVXX + 1].AdjClosePrice / vxxQoutes[iVXX].AdjClosePrice - 1.0;
            double newNAVOnFirstDay = 2 * pvDaily - (vxxChgOnFirstDay + 1.0) * pvDaily;     // 2 * pvDaily is the cash
            pvDaily = newNAVOnFirstDay;
            pv[1].AdjClosePrice = pvDaily;

            int nControversialDays = 0;

            for (int i = 2; i < pv.Count(); i++)
            {
                double vxxChgYesterday = vxxQoutes[iVXX + i - 1].AdjClosePrice / vxxQoutes[iVXX + i - 2].AdjClosePrice - 1.0;
                double spyChgYesterday = spyQoutes[iSpy + i - 1].AdjClosePrice / spyQoutes[iSpy + i - 2].AdjClosePrice - 1.0;

                double vxxChg = vxxQoutes[iVXX + i].AdjClosePrice / vxxQoutes[iVXX + i - 1].AdjClosePrice - 1.0;
                if (Math.Sign(vxxChgYesterday) == Math.Sign(spyChgYesterday) && Math.Abs(spyChgYesterday) > (spyMinPctMove / 100.0) && Math.Abs(vxxChgYesterday) > (vxxMinPctMove / 100.0))       // Controversy, if they have the same sign, because usually they have the opposite sign
                {
                    nControversialDays++;
                    if (isTradeLongVXX == true)
                        pvDaily = pvDaily * (1.0 + vxxChg);
                    else if (isTradeLongVXX == false)
                    {
                        double newNAV = 2 * pvDaily - (vxxChg + 1.0) * pvDaily;     // 2 * pvDaily is the cash
                        pvDaily = newNAV;
                    }
                    // else we was in Cash today, so pvDaily = pvDaily;
                }
                else
                {// if no signal, short VXX with daily rebalancing
                    double newNAV = 2 * pvDaily - (vxxChg + 1.0) * pvDaily;     // 2 * pvDaily is the cash
                    pvDaily = newNAV;
                }

                pv[i].AdjClosePrice = pvDaily;
            }

            noteToUser = String.Format("{0:0.00%} of trading days are controversial days", (double)nControversialDays / (double)pv.Count());
        }   //DoBacktestInTheTimeInterval_VXX_SPY_Controversial()





    }   // class
}
