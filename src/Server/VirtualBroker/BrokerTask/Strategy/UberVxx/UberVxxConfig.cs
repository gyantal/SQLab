using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    //On skype Balazs said: VXX StDev is 3.64% (from 2004), and sometimes he used 8% threshold(Zvalue= 2.2, eliminates 2.5% of the samples) or uses 11% threshold(Zvalue= 3.9, eliminates 1% of the stamples)
    //He also said he doesn't eliminate the samples, only maximizes them. Yes, I can understand, but I think if we have enough samples, it is better to kill these samples in general.
    //because if VXX goes up by 15% that is obviously so exceptional(political decision) thing, and it is a random event comparing to the effect what we intend to meausure.
    //>so for a TotM statistics, when only mild %change movement is expected, a 15% change is a random outlier(not caused by the TotM effect), => kill outliers
    //>on the other hand, measuring FOMC days, a 15% change a rare, but Expected outlier, (caused by the FED decision) we should keep the outlier, but maybe maximise it, how Balazs is doing.
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
