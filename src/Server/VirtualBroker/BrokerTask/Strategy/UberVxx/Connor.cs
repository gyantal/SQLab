using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqCommon;

namespace VirtualBroker
{
    public partial class UberVxxStrategy : IBrokerStrategy
    {
        public double? GetConnorForecast()
        {
            StrongAssert.True((m_vxx != null) && (m_vxx.Length >= 101), Severity.ThrowException, "GetConnorForecast() cannot continue because VXX historical data is not given. Throw exception. Notify administrators to investigate Log.");

            double m_probDailyFT = 0.0;
            double m_probDailyFTGoodFtRegimeThreshold = 0.470001;       // 2016-06-14: I have changed it from 0.48 to 0.47, because in the VXXAdaptiveConnorLiveBacktest I noticed that 0.48 is too close to the 0.49, which gave 0% CAGR. So, it is better to go down with this parameter to mitigate parameter optimization
            double[] vxxDailyFT = new double[100];
            for (int i = 0; i < 100; i++)
            {
                vxxDailyFT[i] = (m_vxx[i].PctChg >= 0 && m_vxx[i + 1].PctChg >= 0 || m_vxx[i].PctChg < 0 && m_vxx[i + 1].PctChg < 0) ? 1 : 0;
            }
            m_probDailyFT = vxxDailyFT.Average();

            // 3. Process it
            double dailyPercentChange = m_vxx[m_vxx.Length - 1].PctChg;
            //FTDirProb Regime Threshold:	0.47, >47% is good regime (for example 48% is still a good regime); but, 47.00 is a bed regime. In bad regime, we do MR, otherwise FT.
            // if today %change = 0, try to short VXX, because in general, 80% of the time, it is worth shorting VXX than going long
            double forecast = 0;
            bool isFTRegime = m_probDailyFT > m_probDailyFTGoodFtRegimeThreshold;
            if (isFTRegime)   // FT regime
                forecast = (dailyPercentChange > 0) ? 1 : -1;        // FT regime: if %change = 0, we bet VXX will go down (-1)
            else
                forecast = (dailyPercentChange >= 0) ? -1 : 1;// MR regime

            Utils.ConsoleWriteLine(ConsoleColor.Green, false, $"Connor: VXX %Chg:{dailyPercentChange * 100.0:F2}%,ProbFT:{m_probDailyFT * 100.0}%,Regime:{((isFTRegime) ? "FT" : "MR")},Forecast:{forecast * 100}%");
            Utils.Logger.Info($"VXX %Chg:{dailyPercentChange * 100.0:F2}%,ProbFT:{m_probDailyFT * 100.0}%,Regime:{((isFTRegime) ? "FT" : "MR")},Forecast:{forecast * 100}%");
            m_detailedReportSb.AppendLine($"<font color=\"#10ff10\">Connor: VXX %Chg:{dailyPercentChange * 100.0:F2}%,ProbFT:{m_probDailyFT * 100.0}%,Regime:{((isFTRegime) ? "FT" : "MR")},Forecast:{forecast * 100}%</font>");
            return forecast;
        }
    }
}
