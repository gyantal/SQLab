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
            double m_probDailyFT = 0.0;
            double m_probDailyFTGoodFtRegimeThreshold = 0.480001;

            double[] vxxDailyFT = new double[100];
            for (int i = 0; i < 100; i++)
            {
                vxxDailyFT[i] = (m_vxx[i].PctChg >= 0 && m_vxx[i + 1].PctChg >= 0 || m_vxx[i].PctChg < 0 && m_vxx[i + 1].PctChg < 0) ? 1 : 0;
            }
            m_probDailyFT = vxxDailyFT.Average();

            // 3. Process it
            double dailyPercentChange = m_vxx[m_vxx.Length - 1].PctChg;
            //FTDirProb Regime Threshold:	0.48, >48% is good regime (for example 49% is still a good regime); but, 48.00 is a bed regime. In bad regime, we do MR, otherwise FT.
            // if today %change = 0, try to short VXX, because in general, 80% of the time, it is worth shorting VXX than going long
            double forecast = 0;
            bool isFTRegime = m_probDailyFT > m_probDailyFTGoodFtRegimeThreshold;
            if (isFTRegime)   // FT regime
                forecast = (dailyPercentChange > 0) ? 1 : -1;        // FT regime: if %change = 0, we bet VXX will go down (-1)
            else
                forecast = (dailyPercentChange >= 0) ? -1 : 1;// MR regime

            Utils.ConsoleWriteLine(ConsoleColor.Green, true, $"Connor: VXX %Chg:{dailyPercentChange * 100.0:F2}%,ProbFT:{m_probDailyFT * 100.0}%,Regime:{((isFTRegime) ? "FT" : "MR")},Forecast:{forecast * 100}%");
            Utils.Logger.Info($"VXX %Chg:{dailyPercentChange * 100.0:F2}%,ProbFT:{m_probDailyFT * 100.0}%,Regime:{((isFTRegime) ? "FT" : "MR")},Forecast:{forecast * 100}%");
            return forecast;
        }
    }
}
