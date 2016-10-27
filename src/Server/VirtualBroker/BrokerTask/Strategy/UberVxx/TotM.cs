using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqCommon;
using System.Text;
using DbCommon;

namespace VirtualBroker
{
    struct SampleStats  // there are 30 of this in an array (and this happens 4*3regimes=12 times)
    {
        public string Name;
        public int DayOffset;   // 1 = T+1, so let it be 1-based
        //public bool? IsBullish;     // only needed in Backtests, not in Significance analysis of the day, so maybe create a separate variable for this later in QuickTester2, so that the code base can be the same.
        public List<Tuple<DateTime, double>> Samples;
        public double WinPct;

        public double AMean;
        public double GMean;
        public double Median;
        public double CorrectedStDev;
        public double StandardError;
        public double TvalueToZero;
        public double TvalueToAMean;
        public double PvalueToZero;
        public double PvalueToAMean;

        public List<Tuple<string, double>> AMeanPerYear;    // it is a List, not array, because the number of samples is unknown. However, we create it with a big estimated capacity efficiently.
    }

    class SampleStatsPerRegime  // only 3 instances of this for Bullish/Bearish/AllYear regime, so this can be a class
    {
        public string Name;
        public SampleStats[] TotMForward;
        public SampleStats[] TotMBackward;
        public SampleStats[] TotMidMForward;
        public SampleStats[] TotMidMBackward;
    }

    public partial class UberVxxStrategy : IBrokerStrategy
    {
        SampleStatsPerRegime m_winterStats;
        SampleStatsPerRegime m_summerStats;
        SampleStatsPerRegime m_allYearStats;

        public double? GetUberVxx_TotM_TotMM_Summer_Winter_ForecastVxx()    // forecast VXX, not SPY
        {
            if (!PrepareHistoricalStatsForAllRegimes())
            {
                Utils.Logger.Error("Error in PrepareHistoricalDataForTotM(). Returning NULL forecast.");
                return null;
            }

            //Utils.ConsoleWriteLine(null, false, $"TotM: Training set: SPY %Chg from {m_spy[0].Date.ToString("yyyy-MM-dd")}.");    // next Console line shows the "SPY" or "VXX" as for Win%
            Utils.Logger.Info($"TotM: Training set: SPY %Chg from {m_spy[0].Date.ToString("yyyy-MM-dd")}.");

            // In theory TotM vs. TotMM signals can conflict, because in 2001, market was closed for 4 days, because of the NY terrorist event. 
            // TotM -T+2 can conflict with TotMM-T-4 easily.
            //2001-08-31, TotMM-T-6
            //2001-09-04, TotMM-T-5, TotM-T+1
            //2001-09-05, TotMM-T-4, TotM-T+2 // if there is conflict: TotM wins. Priority. That is the stronger effect.// OR if there is conflict: go to Cash // or they can cancel each other out
            //2001-09-06, TotMM-T-3, TotM-T+3
            //2001-09-07, TotMM-T-2, TotM-T+4
            //2001-09-10, TotMM-T-1, TotM-T+5
            //2001-09-17,
            //in QuickTester v1.0 we allowed multiple signals and averaged them if one was bullish, other was bearish in the Mask. The Mask was binary anyway: Bullish or Bearish for a day.
            //However, here, we don't work with the Mask, and we prefer here to NOT combine them. Here we choose only a single one, which has the lowest offset.
            DateTime dateNowInET = Utils.ConvertTimeFromUtcToEt(DateTime.UtcNow);
            DateTime nextTradingDayUtc = Utils.GetNextUsaMarketOpenDayUtc(DateTime.UtcNow, false);
            DateTime nextTradingDayET_Date = Utils.ConvertTimeFromUtcToEt(nextTradingDayUtc).Date;
            SampleStatsPerRegime regimeToUse = IsBullishWinterDay(nextTradingDayET_Date) ? m_winterStats : m_summerStats;

            int nextDayTotMForwardInd, nextDayTotMBackwardInd, nextDayTotMidMForwardInd, nextDayTotMidMBackwardInd;
            CalculateOffsetInds(nextTradingDayET_Date, out nextDayTotMForwardInd, out nextDayTotMBackwardInd, out nextDayTotMidMForwardInd, out nextDayTotMidMBackwardInd);
            int nextDayMinOffsetInd = Math.Min(Math.Min(nextDayTotMForwardInd, nextDayTotMBackwardInd), Math.Min(nextDayTotMidMForwardInd, nextDayTotMidMBackwardInd));
            if (nextDayMinOffsetInd >= 8)
            {
                //- max length of the masks used: I suggested TotMM until Day+6, but Balazs suggested until Day+7. So, let's allow max Day+7 (inclusive)
                //(if we don't limit that, we recirculate to TotM, no point of going to Day+20 for example, so we have to stop somewhere)
                //> "That 14 calendar day means 21.6/30.4*14=9.94 trading days. (approx. 10 trading days), 
                //therefore TotMM-T+1 approximately equals to TotM-T+1+10 = TotM-T+11.  
                //(and therefore TotMM-T+7= TotM-T-(10-7)=TotM-T-3 or sometimes it is TotM-T-4)"
                //Decision: allow max Day+7, -7
                return null;
            }

            double? forecast = null;
            SampleStats statsToUse;
            // TotMidM should have the preference over TotM, because
            // Reason1:  TotM (18%) + TotMM (32%): 50%, give ToMM priority.  (see 'Table 16: Performance indicators of Sub-strategies using VXX'  https://docs.google.com/document/d/1OAlCYuPz5DXMDt5NiGaExQtmxVMClHbtVVyr4ViSs7g/edit)
            // Reason2: 2016-10-24: TotMM+6 and TotM-6 happened at the same time.
            //VBroker did choose: TotMidM+6, which was neutral, while TotM-6 would have been market bearish.
            //So, TotMidM had the preference over TotM. Is it good? Nobody knows. Quite random.
            //In this instance, TotMidM was better, because next day was very bullish. However,
            //thin only sample is no reason for keeping that. TotM sounds more logical that TotMidM,
            //but it is also true that more quants follow TotM, so the TotMidM is less crowded trade.
            //So, keep these TotMidM preference for a while.
            if (nextDayMinOffsetInd == nextDayTotMidMForwardInd)
            {
                statsToUse = regimeToUse.TotMidMForward[nextDayTotMidMForwardInd - 1];  // convert ind to zero based
            }
            else
            if (nextDayMinOffsetInd == nextDayTotMidMBackwardInd)
            {
                statsToUse = regimeToUse.TotMidMBackward[nextDayTotMidMBackwardInd - 1];    // convert ind to zero based
            }
            else
            if (nextDayMinOffsetInd == nextDayTotMForwardInd)
            {
                statsToUse = regimeToUse.TotMForward[nextDayTotMForwardInd - 1];    // convert ind to zero based
            }
            else
            if (nextDayMinOffsetInd == nextDayTotMBackwardInd)
            {
                statsToUse = regimeToUse.TotMBackward[nextDayTotMBackwardInd - 1];  // convert ind to zero based
            }
            else
                return null;    // return now, because there is no statsToUse

            //-Consider neighbour days too (Step 6 task): if a Neutral / Cash day is between 2 bullish days. The Neutral day can be played as Bullish too.I personally would think about playing this way in real life.
            //Can we backtest in Quicktester? Does it worth doing it?
            double winPct = statsToUse.WinPct;
            double tValue = statsToUse.TvalueToZero;  // TvalueToZero is a signed value. (p-value is not, that is a probability). So, the direction of T-value is fine to forecast
            double pValue= statsToUse.PvalueToZero;
            double significantPvalue = (Math.Abs(tValue) >= 0.0) ? uberVxxConfig.TotM_BullishDaySignificantPvalue : uberVxxConfig.TotM_BearishDaySignificantPvalue;
            if (pValue <= significantPvalue)  // Depending nSamples, but approx. T-value of 0.5 is p-value=30%, T-value 1.0 is about p-value: 15%,  T-Value of 1.7 is about p-value 4.5%
            {
                // In formating numbers you can use "0" as mandatory place and "#" as optional place (not Zero). So:
                Utils.ConsoleWriteLine(ConsoleColor.Green, false, $"{regimeToUse.Name},{statsToUse.Name}{statsToUse.DayOffset}; SPY Win%:{winPct*100:0.0}, T-val:{tValue:F2}, P-val:{pValue*100:F2}%. Signif. at {significantPvalue*100.0:0.##}%.");
                Utils.Logger.Info($"{regimeToUse.Name},{statsToUse.Name}{statsToUse.DayOffset}; SPY Win%:{winPct*100:0.0}, T-val:{tValue:F2}, P-val:{pValue*100:F2}%. Signif. at {significantPvalue * 100.0:0.##}%.");
                m_detailedReportSb.AppendLine($"<font color=\"#10ff10\">{regimeToUse.Name},{statsToUse.Name}{statsToUse.DayOffset}; SPY Win%:{winPct * 100:0.0}, T-val:{tValue:F2}, P-val:{pValue * 100:F2}%. Signif. at {significantPvalue * 100.0:0.##}%.</font>");

                forecast = -1.0 * Math.Sign(tValue);    //invert the prediction, because long SPY bullishness means short VXX, and we predict VXX.
            } else
            {
                Utils.ConsoleWriteLine(null, false, $"{statsToUse.Name}{statsToUse.DayOffset}:{regimeToUse.Name}, SPY Win%:{winPct * 100:0.0}, T-val:{tValue:F2}, P-val:{pValue * 100:F2}%. NOT Signif. at {significantPvalue * 100.0:0.##}%.");
                Utils.Logger.Info($"{statsToUse.Name}{statsToUse.DayOffset}:{regimeToUse.Name}, SPY Win%:{winPct * 100:0.0}, T-v:{tValue:F2}, P-v:{pValue * 100:F2}%. NOT Signif. at {significantPvalue * 100.0:0.##}%.");
                m_detailedReportSb.AppendLine($"{statsToUse.Name}{statsToUse.DayOffset}:{regimeToUse.Name}, SPY Win%:{winPct * 100:0.0}, T-val:{tValue:F2}, P-val:{pValue * 100:F2}%. NOT Signif. at {significantPvalue * 100.0:0.##}%.");
            }

            return forecast;
        }

        private bool PrepareHistoricalStatsForAllRegimes()
        {
            DateTime pvStartDate = m_spy[0].Date;
            DateTime pvEndDate = m_spy[m_spy.Length - 1].Date;

            // maybe (maybe not) put the Today date and tomorrow date too into m_spy. (but don't use them as samples for Training) That way we will easily know if tomorrow is TotM + ? or TotMidM + ?
            // or calculate it in any other way. Create a Separate function: GetAllOffset(DateTime p_day), but that will be very inefficient to do it for 6000 items in the array. So, just use it for Tomorrow.
            // or just put tomorrow into the sample temporarily.

            // 1.1 calculate totMForwardDayOffset
            DateTime iDate = new DateTime(pvStartDate.Year, pvStartDate.Month, 1);  // Time is 00:00, which is OK, but strangely it is visualized as "12:00 AM", but yeah. Correct. it is not noon.
            iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, true);    // this is day T+1
            int iDateOffset = 1;    // T+1
            while (iDate < pvStartDate) // marching forward until iDate = startDate
            {
                iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            m_spy[0].TotMForwardOffset = iDateOffset;
            for (int i = 1; i < m_spy.Length; i++)  // march over on p_quotes, not pv
            {
                if (m_spy[i].Date.Month != m_spy[i - 1].Date.Month)
                    iDateOffset = 1;    // T+1
                else
                    iDateOffset++;
                m_spy[i].TotMForwardOffset = iDateOffset;
            }

            // 1.2 calculate totMBackwardDayOffset
            iDate = new DateTime(pvEndDate.Year, pvEndDate.Month, 1);
            iDate = iDate.AddMonths(1);     // next month can be in the following year; this is the first calendar day of the next month
            iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);   // this is day T-1
            iDateOffset = 1;    // T-1
            while (iDate > pvEndDate)   // marching backward until iDate == endDate
            {
                iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            m_spy[m_spy.Length - 1].TotMBackwardOffset = iDateOffset;  // last day (today) is set
            for (int i = m_spy.Length - 2; i >= 0; i--)  // march over on p_quotes, not pv
            {
                if (m_spy[i].Date.Month != m_spy[i + 1].Date.Month)   // what if market closes for 3 months (or we don't have the data in DB)
                    iDateOffset = 1;    // T-1
                else
                    iDateOffset++;
                m_spy[i].TotMBackwardOffset = iDateOffset;
            }

            // 1.3 calculate totMidMForwardDayOffset
            iDate = new DateTime(pvStartDate.Year, pvStartDate.Month, 15);
            if (iDate > pvStartDate)
                iDate = iDate.AddMonths(-1);
            iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, true); // // this is day T+1
            iDateOffset = 1;    // T+1
            while (iDate < pvStartDate) // marching forward until iDate = startDate
            {
                iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            m_spy[0].TotMidMForwardOffset = iDateOffset;
            for (int i = 1; i < m_spy.Length; i++)  // march over on p_quotes, not pv
            {
                if (((m_spy[i].Date.Month == m_spy[i - 1].Date.Month) && m_spy[i].Date.Day >= 15 && m_spy[i - 1].Date.Day < 15) ||  // what if market closes for 3 months (or we don't have the data in DB)
                    (m_spy[i].Date.Month != m_spy[i - 1].Date.Month) && m_spy[i].Date.Day >= 15)   // if some months are skipped from data
                    iDateOffset = 1;    // T+1
                else
                    iDateOffset++;
                m_spy[i].TotMidMForwardOffset = iDateOffset;
            }

            // 1.4 calculate totMBackwardDayOffset
            iDate = new DateTime(pvEndDate.Year, pvEndDate.Month, 15);
            if (iDate <= pvEndDate)
                iDate = iDate.AddMonths(1); // next month can be in the following year; better to use AddMonths();
            iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);    // this is day T-1
            iDateOffset = 1;    // T-1
            while (iDate > pvEndDate)   // marching backward until iDate == endDate
            {
                iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            m_spy[m_spy.Length - 1].TotMidMBackwardOffset = iDateOffset;  // last day (today) is set
            for (int i = m_spy.Length - 2; i >= 0; i--)  // march over on p_quotes, not pv
            {
                if (((m_spy[i].Date.Month == m_spy[i + 1].Date.Month) && m_spy[i].Date.Day < 15 && m_spy[i + 1].Date.Day >= 15) ||  // what if market closes for 3 months (or we don't have the data in DB)
                    (m_spy[i].Date.Month != m_spy[i + 1].Date.Month) && m_spy[i].Date.Day < 15)   // if some months are skipped from data
                    iDateOffset = 1;    // T-1
                else
                    iDateOffset++;
                m_spy[i].TotMidMBackwardOffset = iDateOffset;
            }

            // in BackTester, the mask as string went into this function, but it is not needed just yet.
            // setting up stats.TotMForward[i].IsBullish in the QuickTester followed here. (based on Mask to be played)
            // only needed in Backtests, not in Significance analysis of the day, so maybe create a separate variable for this later in QuickTester2, so that the code base can be the same.
            m_winterStats = CreateSampleStatsPerRegime("Winter", (int)(m_spy.Length / 260.0 * 7.0 * 1.1)); // give a little 10% overhead, so List<> will be not re-allocated many times
            m_summerStats = CreateSampleStatsPerRegime("Summer", (int)(m_spy.Length / 260.0 * 6.0 * 1.1)); // give a little 10% overhead, so List<> will be not re-allocated many times
            m_allYearStats = CreateSampleStatsPerRegime("AllYear", (int)(m_spy.Length/260.0*12.0*1.1));   // give a little 10% overhead, so List<> will be not re-allocated many times

            double outlierBasicZscore_PctThreshold = Double.NaN;
            int nNegativeOutliers = 0, nPositiveOutliers = 0;
            if (uberVxxConfig.TotM_OutlierElimination != OutlierElimination.None)
            {
                //-calculate StDev.and http://www.itl.nist.gov/div898/handbook/eda/section3/eda35h.htm
                //	"Although it is common practice to use Z-scores to identify possible outliers, 
                //    Iglewicz and Hoaglin recommend using the modified Z - score"
                //- Balazs used +8% or lower than -8% percentages threshold for VXX TotM. So determine 8% is how many StDev away, 
                //	and if it 2.2 times, than use that for the other samples (like QQQ, which will have less StDev)
                //-(not likely, but) double check, that max. only 5 or 6% of the samples are eliminated. If 10% of the samples are eliminated
                //	as outliers. (10 out of 100), then those are not random outliers.

                double n = (double)m_spy.Length;
                List<double> samples = m_spy.Select(r => r.PctChg).ToList();
                double aMean = samples.Average();
                double correctedStDev = Math.Sqrt(samples.Sum(r => (r - aMean) * (r - aMean)) / (n - 1.0));     // it is about 1.1% for the SPX, http://www.investopedia.com/articles/04/021804.asp
                outlierBasicZscore_PctThreshold = correctedStDev * uberVxxConfig.OutlierBasicZscore_Zscore;
            }

            // create 2 lists, a Forward list, a backward list (maybe later to test day T+12..T+16) Jay's "Monthly 10", which is 4 days in the middle month
            double pctChgUsedSamplesTotal = 0.0;
            for (int i = 0; i < m_spy.Length; i++)  // march over on p_quotes, not pv
            {
                DateTime day = m_spy[i].Date;
                double pctChg = m_spy[i].PctChg;

                if (uberVxxConfig.TotM_OutlierElimination == OutlierElimination.BasicZscore)
                {
                    if (Math.Abs(pctChg) > outlierBasicZscore_PctThreshold)
                    {
                        if (pctChg > 0)
                            nPositiveOutliers++;
                        else if (pctChg < 0)
                            nNegativeOutliers++;

                        continue;
                    }
                }

                pctChgUsedSamplesTotal += pctChg;
                int totMForwardInd = m_spy[i].TotMForwardOffset, totMBackwardInd = m_spy[i].TotMBackwardOffset, totMidMForwardInd = m_spy[i].TotMidMForwardOffset, totMidMBackwardInd = m_spy[i].TotMidMBackwardOffset;

                m_allYearStats.TotMForward[totMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                m_allYearStats.TotMBackward[totMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                m_allYearStats.TotMidMForward[totMidMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                m_allYearStats.TotMidMBackward[totMidMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));

                if (IsBullishWinterDay(day))
                {
                    m_winterStats.TotMForward[totMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_winterStats.TotMBackward[totMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_winterStats.TotMidMForward[totMidMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_winterStats.TotMidMBackward[totMidMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                }
                else
                {
                    m_summerStats.TotMForward[totMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_summerStats.TotMBackward[totMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_summerStats.TotMidMForward[totMidMForwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                    m_summerStats.TotMidMBackward[totMidMBackwardInd - 1].Samples.Add(new Tuple<DateTime, double>(day, pctChg));
                }

            }

            if (uberVxxConfig.TotM_OutlierElimination != OutlierElimination.None)
            {
                //Maybe AdvancedOutlier elimination is not even needed because with the BasicZscore, here are how many samples are eliminated:
                //OutlierBasicZscore_Zscore = 2.7: SPY outliers skipped at 3.21 %.Pos:70,Neg: 57, 2.17 % of samples.
                //OutlierBasicZscore_Zscore = 4.0: SPY outliers skipped at 4.76 %.Pos:17,Neg: 18, 0.6 % of samples.
                //OutlierBasicZscore_Zscore = 5.0: SPY outliers skipped at 5.95 %.Pos:8,Neg: 9, 0.29 % of samples.
                //It is very even.Because there are big panic days, but there are buy upside days too. e.g.: Oct 13, 2008: up + 14.5 %

                double pctOutliers = ((double)(nPositiveOutliers + nNegativeOutliers) / m_spy.Length);
                //Console.WriteLine($"SPY outliers skipped at {outlierBasicZscore_PctThreshold * 100:0.##}%. Pos:{nPositiveOutliers},Neg:{nNegativeOutliers}, {pctOutliers * 100.0:0.##}% of samples.");
                Utils.Logger.Info($"SPY outliers skipped at {outlierBasicZscore_PctThreshold * 100:0.##}%. Pos:{nPositiveOutliers},Neg:{nNegativeOutliers}, {pctOutliers * 100.0:0.##}% of samples.");
                StrongAssert.True(pctOutliers < 0.05, Severity.NoException, "If 5%+ of the samples are eliminated, that means they are not random outliers. This is unexpected.");
            }
            int nUsedSamples = m_spy.Length - nNegativeOutliers - nPositiveOutliers;
            double pctChgTotalAMean = (nUsedSamples <= 0) ? 0.0 : pctChgUsedSamplesTotal / (double)(nUsedSamples);

            CalculateSampleStatsPerRegime(m_winterStats, pctChgTotalAMean);
            CalculateSampleStatsPerRegime(m_summerStats, pctChgTotalAMean);
            CalculateSampleStatsPerRegime(m_allYearStats, pctChgTotalAMean);

            StringBuilder sbStats = new StringBuilder(Environment.NewLine); // looks better in the log file if it starts in a blank new line
            RenderStatsForDisplaying("Winter", m_winterStats, false, sbStats);
            RenderStatsForDisplaying("Summer", m_summerStats, false, sbStats);
            RenderStatsForDisplaying("AllYear", m_allYearStats, false, sbStats);
            Utils.Logger.Info(sbStats.ToString());

            return true;
        }

        private void RenderStatsForDisplaying(string p_title, SampleStatsPerRegime p_stats, bool p_isHtml, StringBuilder p_sbStats)
        {
            RenderStatsForDisplaying(p_title + ", TotM", p_stats.TotMForward, p_stats.TotMBackward, p_isHtml, p_sbStats);
            RenderStatsForDisplaying(p_title + ", TotMidM", p_stats.TotMidMForward, p_stats.TotMidMBackward, p_isHtml, p_sbStats);
        }

        private void RenderStatsForDisplaying(string p_title, SampleStats[] p_forward, SampleStats[] p_backward, bool p_isHtml, StringBuilder p_sbStats)
        {
            if (p_isHtml)
                p_sbStats.Append(@"<b>" + p_title + @":</b><br> <table class=""strategyNoteTable1"">" +
                    @"<th>Day</th><th>nSamples</th><th>WinPct</th><th>&nbsp; aMean &nbsp; </th><th>gMean</th><th>Median</th><th>StDev</th><th>StError</th><th>t-value(0)</th>" +
                    @"<th><div title=""P is calculated by one tailed, one sample T-test"">p-value(0)</div></th>" +
                    @"<th><div title=""With at least 1-P=95% probability: the real population mean (of the daily%changes on day T) > 0 {or opposite if T-value negative}"">Signif>0</div></th>" +
                    @"<th>t-value(mean)</th>" +
                    @"<th><div title=""P is calculated by one tailed, one sample T-test"">p-value(mean)</div></th>" +
                    @"<th><div title=""With at least 1-P=95% probability: the real population mean (of the daily%changes on day T) > allDayMean {or opposite if T-value negative}"">Signif>mean</div></th>");
            else
                p_sbStats.AppendLine($"--- Stats for {p_title}:    (Win%, aMean, t-value, p-value)");

            bool isRowEven = false;     // 1st Row is Odd
            for (int i = 16; i >= 0; i--)   // write only from T-17 to T+17
            {
                if (p_backward[i].Samples.Count() == 0)
                    continue;

                RenderTableRow(p_title, "T-" + (i + 1).ToString(), isRowEven, ref p_backward[i], p_isHtml, p_sbStats);
                isRowEven = !isRowEven;
            }

            for (int i = 0; i <= 16; i++)  // write only from T-17 to T+17
            {
                if (p_forward[i].Samples.Count() == 0)
                    continue;
                RenderTableRow(p_title, "T+" + (i + 1).ToString(), isRowEven, ref p_forward[i], p_isHtml, p_sbStats);
                isRowEven = !isRowEven;
            }

            if (p_isHtml)
                p_sbStats.Append("</table>");
            else
                p_sbStats.Append(Environment.NewLine);
        }


        private static void RenderTableRow(string p_tableTitle, string p_rowTitle, bool p_isRowEven, ref SampleStats p_stat, bool p_isHtml, StringBuilder p_sb)
        {
            if (p_isHtml)
            {
                string aMeanPerYearRowId = "id" + (p_tableTitle + p_rowTitle).Replace(' ', '_').Replace(',', '_');
                string aMeanPerYearCSV = String.Join(", ", p_stat.AMeanPerYear.Select(r => r.Item1 + ":" + r.Item2.ToString("#0.000%")));

                p_sb.AppendFormat("<tr{0}><td>" + p_rowTitle + "</td>", (p_isRowEven) ? " class='even'" : "");
                p_sb.Append("<td>" + p_stat.Samples.Count() + "</td>");
                p_sb.Append("<td>" + p_stat.WinPct.ToString("#0.0%") + "</td>");
                //p_sb.AppendFormat(@"<td onclick=""document.getElementById('{0}').style.color = 'red'"">" + p_maskItem.AMean.ToString("#0.000%") + "</td>", aMeanPerYearRowId);
                //p_sb.AppendFormat(@"<td onclick=""document.getElementById('{0}').style.display = 'table-row'"">" + p_maskItem.AMean.ToString("#0.000%") + @"<button onclick=""document.getElementById('{0}').style.display = 'table-row'"">*</button></td>", aMeanPerYearRowId);
                //p_sb.AppendFormat(@"<td onclick=""document.getElementById('{0}').style.display = 'table-row'"">" + p_maskItem.AMean.ToString("#0.000%") + @"</td>", aMeanPerYearRowId);
                //p_sb.AppendFormat(@"<td>" + p_maskItem.AMean.ToString("#0.000%") + @"<a href="""" onclick=""document.getElementById('{0}').style.display = 'table-row'"">*</a></td>", aMeanPerYearRowId);
                //p_sb.AppendFormat(@"<td onclick=""document.getElementById('{0}').style.display = 'table-row'"">" + p_maskItem.AMean.ToString("#0.000%") + @"<span style=""color: #2581cc; font-size: x-small; vertical-align:super;"">i</span></td>", aMeanPerYearRowId);
                //p_sb.AppendFormat(@"<td{0} onclick=""InvertVisibilityOfTableRow('{1}')"">" + p_stat.AMean.ToString("#0.000%") + @"<span style=""color: #2581cc; font-size: x-small; vertical-align:super;"">i</span></td>", (p_stat.AMean > 0.0) ? " class='green'" : " class='red'", aMeanPerYearRowId);
                p_sb.AppendFormat(@"<td{0} onclick=""GlobalScopeInvertVisibilityOfTableRow('{1}')"">" + p_stat.AMean.ToString("#0.000%") + @"<span style=""color: #2581cc; font-size: x-small; vertical-align:super;"">i</span></td>", (p_stat.AMean > 0.0) ? " class='green'" : " class='red'", aMeanPerYearRowId);

                p_sb.Append("<td>" + p_stat.GMean.ToString("#0.000%") + "</td>");
                p_sb.Append("<td>" + p_stat.Median.ToString("#0.000%") + "</td>");
                p_sb.Append("<td>" + p_stat.CorrectedStDev.ToString("#0.000%") + "</td>");
                p_sb.Append("<td>" + p_stat.StandardError.ToString("#0.000%") + "</td>");
                p_sb.AppendFormat("<td{0} >" + p_stat.TvalueToZero.ToString("#0.00") + "</td>", (p_stat.TvalueToZero >= 1.0) ? " class='green'" : ((p_stat.TvalueToZero <= -1.0) ? " class='red'" : ""));
                p_sb.Append("<td>" + p_stat.PvalueToZero.ToString("#0.00%") + "</td>");
                p_sb.Append("<td>" + ((p_stat.PvalueToZero < 0.05) ? "Yes" : "") + "</td>");

                p_sb.Append("<td>" + p_stat.TvalueToAMean.ToString("#0.00") + "</td>");
                p_sb.Append("<td>" + p_stat.PvalueToAMean.ToString("#0.00%") + "</td>");
                p_sb.Append("<td>" + ((p_stat.PvalueToAMean < 0.05) ? "Yes" : "") + "</td>");

                p_sb.Append("</tr>");

                p_sb.AppendFormat(@"<tr{0} ID=""{1}"" style=""display:none;""><td colspan=""14"">{2} aMean:{3}</td>",
                        (p_isRowEven) ? " class='even'" : "", aMeanPerYearRowId, p_rowTitle, aMeanPerYearCSV);
                p_sb.Append("</tr>");
            }
            else
            {
                p_sb.AppendLine($"{p_rowTitle} (#{p_stat.Samples.Count()}): {p_stat.WinPct.ToString("#0.0%")}, {p_stat.AMean.ToString("#0.000%")}, {p_stat.TvalueToZero.ToString("#0.00")}, {p_stat.PvalueToZero.ToString("#0.00%")}");
            }

        }

        private void CalculateOffsetInds(DateTime p_dayDate, out int p_totMForwardInd, out int p_totMBackwardInd, out int p_totMidMForwardInd, out int p_totMidMBackwardInd)
        {
            p_totMForwardInd = 10;
            p_totMBackwardInd = 12;
            p_totMidMForwardInd = 20;
            p_totMidMBackwardInd = 1;

            // 1.1 calculate totMForwardDayOffset
            DateTime iDate = new DateTime(p_dayDate.Year, p_dayDate.Month, 1);
            iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, true); // this is day T+1
            int iDateOffset = 1;    // T+1
            while (iDate < p_dayDate) // marching forward until iDate = startDate
            {
                iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            p_totMForwardInd = iDateOffset;

            // 1.2 calculate totMBackwardDayOffset
            iDate = new DateTime(p_dayDate.Year, p_dayDate.Month, 1);
            iDate = iDate.AddMonths(1);     // next month can be in the following year; this is the first calendar day of the next month
            iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false); // this is day T-1
            iDateOffset = 1;    // T-1
            while (iDate > p_dayDate)   // marching backward until iDate == endDate
            {
                iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            p_totMBackwardInd = iDateOffset;  // last day (today) is set

            // 1.3 calculate totMidMForwardDayOffset
            iDate = new DateTime(p_dayDate.Year, p_dayDate.Month, 15);
            if (iDate > p_dayDate)
                iDate = iDate.AddMonths(-1);
            iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, true); // this is day T+1
            iDateOffset = 1;    // T+1
            while (iDate < p_dayDate) // marching forward until iDate = startDate
            {
                iDate = DbUtils.GetNextUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            p_totMidMForwardInd = iDateOffset;

            // 1.4 calculate totMBackwardDayOffset
            iDate = new DateTime(p_dayDate.Year, p_dayDate.Month, 15);
            if (iDate <= p_dayDate)
                iDate = iDate.AddMonths(1); // next month can be in the following year; better to use AddMonths();
            iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);   // this is day T-1

            iDateOffset = 1;    // T-1
            while (iDate > p_dayDate)   // marching backward until iDate == endDate
            {
                iDate = DbUtils.GetPreviousUsaMarketOpenDayLoc(iDate, false);
                iDateOffset++;
            }
            p_totMidMBackwardInd = iDateOffset;  // last day (today) is set

        }

        const int cMaxDayOffset = 30;
        private SampleStatsPerRegime CreateSampleStatsPerRegime(string p_name, int p_estimatedNsamplesPerDayOffset)
        {
            SampleStatsPerRegime stats = new SampleStatsPerRegime() { Name = p_name, TotMForward = new SampleStats[cMaxDayOffset], TotMBackward = new SampleStats[cMaxDayOffset], TotMidMForward = new SampleStats[cMaxDayOffset], TotMidMBackward = new SampleStats[cMaxDayOffset] };

            for (int i = 0; i < cMaxDayOffset; i++)
            {
                stats.TotMForward[i].Name = "TotM+";
                stats.TotMBackward[i].Name = "TotM-";
                stats.TotMidMForward[i].Name = "TotMidM+";
                stats.TotMidMBackward[i].Name = "TotMidM-";
                stats.TotMForward[i].DayOffset = i + 1;     // 1= T+1, 1 based
                stats.TotMBackward[i].DayOffset = i + 1;
                stats.TotMidMForward[i].DayOffset = i + 1;
                stats.TotMidMBackward[i].DayOffset = i + 1;
                stats.TotMForward[i].Samples = new List<Tuple<DateTime, double>>(p_estimatedNsamplesPerDayOffset);
                stats.TotMBackward[i].Samples = new List<Tuple<DateTime, double>>(p_estimatedNsamplesPerDayOffset);
                stats.TotMidMForward[i].Samples = new List<Tuple<DateTime, double>>(p_estimatedNsamplesPerDayOffset);
                stats.TotMidMBackward[i].Samples = new List<Tuple<DateTime, double>>(p_estimatedNsamplesPerDayOffset);
            }

            // in BackTester, the mask as string went into this function, but it is not needed just yet.
            // setting up stats.TotMForward[i].IsBullish in the QuickTester followed here. (based on Mask to be played)
            // only needed in Backtests, not in Significance analysis of the day, so maybe create a separate variable for this later in QuickTester2, so that the code base can be the same.

            return stats;
        }

        // "period from November to April inclusive has significantly stronger growth on average than the other months.". Stocks are sold at the start of May. "between April 30 and October 30, 2009, the FTSE 100 gained 20%"
        // Grim Reaper: I overfitted (SPY, from 1993): 1st May was Bullish, 1st November Bearish. I set up the range according to this. Bearish range: "(1st May, 1st November]". 
        // Later this was changed to "(1st May, 25th October]"
        // - Helloween day: 31st October
        // according to this: DJI_MonthlySeasonality_on2011-07-04.png, in 20 years:
        // leave the 1st May, as it is
        // However, October is Bullish. So in practice, I would set the turning point somewhere around 20th October. or 25th October instead of 1st November. With Grim Reaper help. I picked 25th October as turning point.
        private static bool IsBullishWinterDay(DateTime p_day)
        {
            //if (p_day < new DateTime(p_day.Year, 5, 1))  3.78% (WinterMask is TotM: .UUU), 7.13% (winterMask is Buy&Hold)
            //if (p_day <= new DateTime(p_day.Year, 5, 1))  4.05% (WinterMask is TotM: .UUU), 7.40% (winterMask is Buy&Hold)    So leave this. means 1st May is Bullish.
            if (p_day.Date <= new DateTime(p_day.Year, 5, 1)) // 1st of May should be bullish, because that is the First day of the Month.
                return true;
            //else if (p_day < new DateTime(p_day.Year, 11, 1)) => 3.98% (WinterMask is TotM: .UUU)  , 7.34% (winterMask is Buy&Hold)
            //else if (p_day <= new DateTime(p_day.Year, 11, 1)) => 4.05% (WinterMask is TotM: .UUU), 7.40% (winterMask is Buy&Hold), So leave this. 1st November should be in the Bearish period.
            //else if (p_day <= new DateTime(p_day.Year, 11, 1)) // 1st November was Bearish for SPY, from 1993. So, don't include it here. Buy on the 2nd November
            //else if (p_day <= new DateTime(p_day.Year, 10, 25)) //=> 3.98% (WinterMask is TotM: .UUU), 8.64% (winterMask is Buy&Hold),
            //else if (p_day <= new DateTime(p_day.Year, 10, 20)) //=> 3.98% (WinterMask is TotM: .UUU), 8.18% (winterMask is Buy&Hold),
            //else if (p_day <= new DateTime(p_day.Year, 10, 15)) //=> 3.98% (WinterMask is TotM: .UUU), 8.67% (winterMask is Buy&Hold),
            else if (p_day.Date <= new DateTime(p_day.Year, 10, 25)) //=> 3.98% (WinterMask is TotM: .UUU), 8.64% (winterMask is Buy&Hold),  I would play this. So. choose this as bullish period. (so, the Helloween pre-holiday days are included into the Bullish range)
                return false;
            else
                return true;       //1st November will come here, as Bullish.
        }

        private static void CalculateSampleStatsPerRegime(SampleStatsPerRegime p_statsPerRegime, double p_pctChgTotalAMean)
        {
            for (int i = 0; i <= 16; i++)   // write only from T-17 to T+17
            {
                CalculateSampleStatsPerDayOffset(ref p_statsPerRegime.TotMForward[i], p_pctChgTotalAMean);
                CalculateSampleStatsPerDayOffset(ref p_statsPerRegime.TotMBackward[i], p_pctChgTotalAMean);
                CalculateSampleStatsPerDayOffset(ref p_statsPerRegime.TotMidMForward[i], p_pctChgTotalAMean);
                CalculateSampleStatsPerDayOffset(ref p_statsPerRegime.TotMidMBackward[i], p_pctChgTotalAMean);
            }
        }

        private static void CalculateSampleStatsPerDayOffset(ref SampleStats p_sampleStats, double p_pctChgTotalAMean)
        {
            int nInt = p_sampleStats.Samples.Count();
            if (nInt == 0)
                return;

            double n = (double)nInt;
            List<double> samples = p_sampleStats.Samples.Select(r => r.Item2).ToList(); // only the PctChanges are needed, not the Dates; but keep the Date in the sample for Debugging reasons
            double aMean = samples.Average();
            double correctedStDev = Math.Sqrt(samples.Sum(r => (r - aMean) * (r - aMean)) / (n - 1.0));
            double standardError = correctedStDev / Math.Sqrt(n);

            p_sampleStats.WinPct = (double)samples.Count(r => r > 0) / n;

            p_sampleStats.AMean = aMean;
            p_sampleStats.GMean = samples.GMeanExtendingWithOne();
            p_sampleStats.Median = samples.Median();
            p_sampleStats.CorrectedStDev = correctedStDev;
            p_sampleStats.StandardError = standardError;


            p_sampleStats.TvalueToZero = (p_sampleStats.AMean - 0.0) / standardError;
            p_sampleStats.TvalueToAMean = (p_sampleStats.AMean - p_pctChgTotalAMean) / standardError;

            // PvalueToZero = P = probability that the observed aMean result is due to chance 
            if (!Double.IsNaN(p_sampleStats.TvalueToZero))
                p_sampleStats.PvalueToZero = Utils.TDistribution(p_sampleStats.TvalueToZero, nInt - 1, true);
            else
                p_sampleStats.PvalueToZero = Double.NaN;

            if (!Double.IsNaN(p_sampleStats.TvalueToAMean))
                p_sampleStats.PvalueToAMean = Utils.TDistribution(p_sampleStats.TvalueToAMean, nInt - 1, true);
            else
                p_sampleStats.PvalueToAMean = Double.NaN;

            p_sampleStats.AMeanPerYear = new List<Tuple<string, double>>();

            var years = p_sampleStats.Samples.Select(r => r.Item1.Year).Distinct().OrderBy(r => r).ToList();
            int nYears = years.Count();
            int[] nSamplesPerYear = new int[nYears];
            double[] aMeanPerYear = new double[nYears];
            for (int i = 0; i < n; i++)
            {
                int iYears = years.IndexOf(p_sampleStats.Samples[i].Item1.Year);
                nSamplesPerYear[iYears]++;
                aMeanPerYear[iYears] += p_sampleStats.Samples[i].Item2;
            }

            for (int i = 0; i < nYears; i++)
            {
                aMeanPerYear[i] /= (double)nSamplesPerYear[i];
                p_sampleStats.AMeanPerYear.Add(new Tuple<string, double>(years[i].ToString() + "(" + nSamplesPerYear[i] + ")", aMeanPerYear[i]));
            }


        }
        
    }
}
