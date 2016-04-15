using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public enum OutlierElimination
    {
        None, 
        BasicZscore,  // eliminates samples with a threshold. Almost all eliminated outliers are negative panicky days. This eliminates too much Bearish negative values from the set, and no Bullish positives. Not balanced.
        AdvancedHandlingSkew
    }


    public class UberVxxConfig
    {
        public string TotM_TrainingSetTicker = "SPY";
        public int TotM_TrainingSetnYears = 25; // temporarily, so that we can calculate the same as QuickTester for the longest history. Later we may go back to 20 years only.
        public OutlierElimination TotM_OutlierElimination = OutlierElimination.BasicZscore;    // we are convinced that outliers should be removed. See AdaptiveUberVXX_plan.txt
        public double OutlierBasicZscore_Zscore = 3.0; // StDev of SPY is 1.1%, so this will eliminate 4.4% %Chg days, Balazs used 8% threshold for VXX, which is about 8/3=2.7% threshold for SPY, which is about Zscore=2.5, because of Beta=3.

        // - the threshold for BullishEdge, BearishEdge (T-value, p-value) can be different. We are more willing to do Bearish LongVXX, as a hedge of other portfolios, and because that is the less overcrowded trade.
        public double TotM_BullishDaySignificantPvalue = 0.10;   // instead of the typical 5% significance level, we allow 10%
        public double TotM_BearishDaySignificantPvalue = 0.15;   // we are more lenient, we allow more days to be bearish


    }
}
