using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker.Strategy.NeuralSniffer
{
    [Flags]
    public enum NNInputDesc
    {
        None = 0x0,
        BarChange = 0x1,
        WeekDays = 0x2,
        EurChange = 0x4
    }

    public struct EnsembleGroupSetup    // we play ensemble groups: one group is homogenious, it has the same parameters
    {
        public NNInputDesc NNInputDesc;
        public int Nneurons;
        public int MachineLearningMethod; // Perceptron, Feedforward, GRNN, Radial, Annealing, etc. (not used now)
        public int[] BarChangeLookbackDaysInds;

        public int NensembleGroupMembers;
    }

    public enum EnsembleAggregationStrategy
    {
        ReturnTheFirstForecast, // for deterministics strategies
        SumSignForecasts,
        AvgForecasts,
        PlayOnlyIfAllMemberAgree,
        PlayOnlyIfAllGroupAgree_onGroupSumSign,    // if a group contains 10 members, than SumSign is applied inside the group only
    }


    class NNConfig
    {
        public int lookbackWindowSize = 200;
        public double outputOutlierThreshold = 0.04;
        public int maxEpoch = 99;  // use 40-50
        static int generalNensembleGroupMembers = 5;
        public EnsembleGroupSetup[] ensembleGroups = new EnsembleGroupSetup[] {    // keep the ensembleMembers number odd; because if it is even, 5 Up prediction and 5 down prediction can cancel each other
                new EnsembleGroupSetup() { Nneurons = 1, NNInputDesc = NNInputDesc.BarChange, BarChangeLookbackDaysInds= new int[] { 0, 1 }, NensembleGroupMembers = generalNensembleGroupMembers },
                new EnsembleGroupSetup() { Nneurons = 1, NNInputDesc = NNInputDesc.WeekDays, NensembleGroupMembers = generalNensembleGroupMembers },
            };
        public EnsembleAggregationStrategy ensembleAggregation = EnsembleAggregationStrategy.PlayOnlyIfAllGroupAgree_onGroupSumSign;
        public int nEnsembleRepeat = 11; // 11 How many ensembles to run parallel? (each ensemble are the same; each contain ensembleGroups) (this is aggregated by SumSign() in every case: to decrease randomness)

        // not importants
        public int notNNStrategy = -1;     // -1 = use NN, 1: B&H, 2 = MR, 3 = FT, 4: deterministic 1bins, 5: determ. 2bins, 6: determ. 4bins
        public string seriesToPredict = "";    // if it is empty, that means the "RUT"

        public double inputNormalizationBoost = 1;
        public double outputNormalizationBoost = 1;   // was 30, but switched to 1
        public double inputOutlierClipInSD = Double.PositiveInfinity; // 1, 2, Double.PosInfinity

    }
}
