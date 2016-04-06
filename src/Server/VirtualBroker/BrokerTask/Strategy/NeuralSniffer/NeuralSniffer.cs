using AIFH_Vol3.Core.General.Data;
using AIFH_Vol3_Core.Core.ANN;
using AIFH_Vol3_Core.Core.ANN.Activation;
using AIFH_Vol3_Core.Core.ANN.Train;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker.Strategy.NeuralSniffer
{
    public partial class NeuralSniffer
    {
        // an extra randomness reduction was helpful; see http://neuralsniffer.wordpress.com/2010/12/13/combining-the-3-inputs-1-black-box-or-3-black-boxes-with-confidence/
        public double GetEnsembleRepeatForecast(int p_nEnsembleRepeat, EnsembleGroupSetup[] p_ensembleGroups, EnsembleAggregationStrategy p_ensembleAggregation, int p_maxEpoch, int p_iRebalance, int p_lookbackWindowSize, double p_outputOutlierThreshold, double p_inputOutlierClipInSD, double p_inputNormalizationBoost, double p_outputNormalizationBoost, int p_notNNStrategyType, double[] p_dateWeekDays, double[] p_barChanges, bool p_isLogForecast, out double p_avgTrainError)
        {
            double[] forecasts = new double[p_nEnsembleRepeat];
            double[] trainErrors = new double[p_nEnsembleRepeat];

            for (int i = 0; i < forecasts.Length; i++)
            {
                double trainError = Double.NaN;
                forecasts[i] = GetEnsembleForecast(p_ensembleGroups, p_ensembleAggregation, p_maxEpoch, p_iRebalance, p_lookbackWindowSize, p_outputOutlierThreshold, p_inputOutlierClipInSD, p_inputNormalizationBoost, p_outputNormalizationBoost, p_notNNStrategyType, p_dateWeekDays, p_barChanges, p_isLogForecast, out trainError);
                trainErrors[i] = trainError;
            }

            p_avgTrainError = trainErrors.Average();
            if (p_isLogForecast)
                Utils.Logger.Info("Forecast%: GetEnsembleRepeatForecast(): " + String.Concat(forecasts.Select(r => (r * 100).ToString("F4") + "%,").ToArray()));
            return forecasts.Select(r => Math.Sign(r)).Sum();
        }


        double GetEnsembleForecast(EnsembleGroupSetup[] p_ensembleGroups, EnsembleAggregationStrategy p_ensembleAggregation, int p_maxEpoch, int p_iRebalance, int p_lookbackWindowSize, double p_outputOutlierThreshold, double p_inputOutlierClipInSD, double p_inputNormalizationBoost, double p_outputNormalizationBoost, int p_notNNStrategyType, double[] p_dateWeekDays, double[] p_barChanges, bool p_isLogForecast, out double p_avgTrainError)
        {
            List<double>[] forecasts = new List<double>[p_ensembleGroups.Length];
            List<double>[] trainErrors = new List<double>[p_ensembleGroups.Length];

            for (int i = 0; i < p_ensembleGroups.Length; i++)
            {
                EnsembleGroupSetup group = p_ensembleGroups[i];
                forecasts[i] = new List<double>();
                trainErrors[i] = new List<double>();

                //PrepareMemberForecast();  TODO: prepare input/output only once
                int nMembersInGroup = group.NensembleGroupMembers;
                for (int m = 0; m < nMembersInGroup; m++)
                {
                    double trainError = Double.NaN;
                    forecasts[i].Add(GetMemberForecast(group.NNInputDesc, group.Nneurons, p_maxEpoch, p_iRebalance, p_lookbackWindowSize, group.BarChangeLookbackDaysInds, p_outputOutlierThreshold, p_inputOutlierClipInSD, p_inputNormalizationBoost, p_outputNormalizationBoost, p_notNNStrategyType, p_dateWeekDays, p_barChanges, out trainError));
                    trainErrors[i].Add(trainError);
                }

                if (p_isLogForecast)
                    Utils.Logger.Info("Forecast%: GetEnsembleForecast(): " + String.Concat(forecasts[i].Select(r => (r * 100).ToString("F4") + "%,").ToArray()));
            }

            int nTotalMember = p_ensembleGroups.Select(r => r.NensembleGroupMembers).Sum();
            StrongAssert.True(nTotalMember > 0 && p_ensembleGroups.Length != 0);
            p_avgTrainError = trainErrors.Select(r => r.Sum()).Sum() / (double)nTotalMember;

            switch (p_ensembleAggregation)  // instead of returning Sum(), return SIGN(Sum()), because the Meta mechanism will repeat this thing again: OK; we don't need; the Meta will take the Sign() at first
            {
                case EnsembleAggregationStrategy.ReturnTheFirstForecast:
                    return forecasts[0][0];
                case EnsembleAggregationStrategy.SumSignForecasts:
                    return forecasts.Select(r => r.Select(p => Math.Sign(p)).Sum()).Sum();   // SumSign() has lowest STD in the end than Avg() (I measured it)
                case EnsembleAggregationStrategy.AvgForecasts:
                    return forecasts.Select(r => r.Sum()).Sum() / (double)nTotalMember;
                case EnsembleAggregationStrategy.PlayOnlyIfAllMemberAgree:
                    {
                        int nPosForecasts = forecasts.Select(r => r.Count(p => p > 0)).Sum();
                        int nNegForecasts = forecasts.Select(r => r.Count(p => p < 0)).Sum();
                        StrongAssert.True(nPosForecasts + nNegForecasts <= nTotalMember);

                        if (nPosForecasts == nTotalMember || nNegForecasts == nTotalMember) // we asserted that nTotalMember != 0
                            return forecasts.Select(r => r.Sum()).Sum() / (double)nTotalMember; // % average is OK, because they are the same sign
                        else
                            return 0;   // if sign differs, go to cash
                    }
                case EnsembleAggregationStrategy.PlayOnlyIfAllGroupAgree_onGroupSumSign: // if sign differs, go to cash
                    {
                        int nPosGroupForecasts = forecasts.Select(r => r.Select(p => Math.Sign(p)).Sum()).Count(k => k > 0);
                        int nNegGroupForecasts = forecasts.Select(r => r.Select(p => Math.Sign(p)).Sum()).Count(k => k < 0);
                        StrongAssert.True(nPosGroupForecasts + nNegGroupForecasts <= p_ensembleGroups.Length);

                        if (nPosGroupForecasts == p_ensembleGroups.Length || nNegGroupForecasts == p_ensembleGroups.Length) // we asserted that p_ensembleGroups.Length != 0
                            return forecasts[0].Select(r => Math.Sign(r)).Sum(); // % the groups are the same sign. Return the first group sign; no point averaging the SumSign values; if 5 items gives negative, it will return '-5'
                        else
                            return 0;   // if sign differs, go to cash
                    }
                default:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }


        // calculate the forecast the next barChange for the day p_dateWeekDays[p_iRebalance]
        // double target = p_barChanges[iRebalance + 1]; // so target is the p_iRebalance+1 day %change; so the last index that can be used in training is p_barChanges[p_iRebalance] as output
        double GetMemberForecast(NNInputDesc p_nnInputDesc, int p_nNeurons, int p_maxEpoch, int p_iRebalance, int p_lookbackWindowSize, int[] p_barChangeLookbackDaysInds, double p_outputOutlierThreshold, double p_inputOutlierClipInSD, double p_inputNormalizationBoost, double p_outputNormalizationBoost, int p_notNNStrategyType, double[] p_dateWeekDays, double[] p_barChanges, out double p_trainError)
        {
            int barChangeDim = p_barChangeLookbackDaysInds == null ? 0 : p_barChangeLookbackDaysInds.Length;

            p_trainError = Double.MaxValue;
            StrongAssert.True(p_dateWeekDays.Length == p_barChanges.Length);

            bool isUseDateWeekDays = (p_nnInputDesc & NNInputDesc.WeekDays) != 0;
            bool isUseBarChange = (p_nnInputDesc & NNInputDesc.BarChange) != 0;
            bool isUseEurChanges = (p_nnInputDesc & NNInputDesc.EurChange) != 0;

            int inputDim = 0; // can be 3 or 5+1+1
            int dateWeekDaysInd = 0;
            int barChangeInd = 0;
            int eurChangeInd = 0;
            if (isUseDateWeekDays)
            {
                //inputVectorDim = inputVectorDim + size(p_dateWeekDays, 2);  %1 dim or 5 dimension if p_dateWeekDays is 5 dimensional
                //currBarChangeInd = currBarChangeInd + size(p_dateWeekDays, 2);
                //eurChangeInd = eurChangeInd + size(p_dateWeekDays, 2);
                inputDim += 1;
                barChangeInd += 1;
                eurChangeInd += 1;
            }
            if (isUseBarChange)
            {
                inputDim += barChangeDim;
                eurChangeInd += barChangeDim;
            }
            if (isUseEurChanges)
            {
                inputDim++;
            }






            // extract original data. We have to exclude outliers and normalize later
            double[][] nnInput = new double[p_lookbackWindowSize][];
            double[][] nnOutput = new double[p_lookbackWindowSize][];
            int inputIdx = p_iRebalance - p_lookbackWindowSize; // p_barChanges[p_iRebalance] can be used as a last output for the day p_iRebalance-1
            for (int i = 0; i < p_lookbackWindowSize; i++, inputIdx++)
            {
                nnInput[i] = new double[inputDim];
                if (isUseDateWeekDays)
                    nnInput[i][dateWeekDaysInd] = p_dateWeekDays[inputIdx];      // forecast the barChange for the day p_dateWeekDays[p_iRebalance]
                if (isUseBarChange)
                {
                    //                    nnInput[i][barChangeInd] = p_barChanges[inputIdx];      // forecast the barChange for the day p_dateWeekDays[p_iRebalance]
                    for (int j = 0; j < barChangeDim; j++)
                    {
                        int barChangeLookbackInd = p_barChangeLookbackDaysInds[j];
                        nnInput[i][barChangeInd + j] = p_barChanges[inputIdx - barChangeLookbackInd];
                    }
                }

                nnOutput[i] = new double[1];
                nnOutput[i][0] = p_barChanges[inputIdx + 1];        // we want to forecast the next day
            }

            // Exclude outliers if requisted
            EliminateOutliers(p_outputOutlierThreshold, ref nnInput, ref nnOutput);

            int nTrainingSamples = nnInput.GetLength(0);
            // normalize target and input?

            // 2bins, output is normalized??
            //for (int i = 0; i < nTrainingSamples; i++)
            //{
            //    //nnInput[i][currBarChangeInd] = Math.Sign(nnInput[i][0]);
            //    nnOutput[i][0] = Math.Sign(nnOutput[i][0]);
            //}

            // inputOutlierClipInSD: only use it for CurrChange (continous), not dayOfTheWeek (discrete)
            //Utils.StrongAssert(nnInput[0].Length == 1);   // we would like to normalize only the first input: currChange, not the dayOfTheWeek
            //double nnInputMean = nnInput.Select(r => r[0]).Average();
            //double nnInputSD = nnInput.Select(r => r[0]).StandardDeviation();
            //for (int i = 0; i < nnInput.Length; i++)
            //{
            //    if (nnInput[i][0] <  nnInputMean - nnInputSD * p_inputOutlierClipInSD)
            //    {
            //        nnInput[i][0] = nnInputMean - nnInputSD * p_inputOutlierClipInSD;
            //    }
            //    else if (nnInput[i][0] > nnInputMean + nnInputSD * p_inputOutlierClipInSD)
            //    {
            //        nnInput[i][0] = nnInputMean + nnInputSD * p_inputOutlierClipInSD;
            //    }
            //}

            // A. Normalize nnInput
            RangeNormalizationParams dateWeekDaysNormParam = new RangeNormalizationParams();
            double[] barChangeNormParam = new double[barChangeDim]; //I should fill Double.NaN;
            if (isUseDateWeekDays)
                dateWeekDaysNormParam = NeuralSnifferUtil.NormalizeIntoUnitRange(nnInput, dateWeekDaysInd, p_inputNormalizationBoost);
            if (isUseBarChange)
            {
                //barChangeNormParam = Utils.NormalizeWithoutMovingCenterStd(nnInput, barChangeInd, p_inputNormalizationBoost);
                for (int j = 0; j < barChangeDim; j++)
                {
                    barChangeNormParam[j] = NeuralSnifferUtil.NormalizeWithoutMovingCenterStd(nnInput, barChangeInd + j, p_inputNormalizationBoost);
                }
            }
            // B. Normalize nnInput
            double outputMultiplier = NeuralSnifferUtil.NormalizeWithoutMovingCenterStd(nnOutput, 0, p_outputNormalizationBoost);

            // C. generate testInput now and normalize
            double[] testInput = new double[inputDim];
            if (isUseDateWeekDays)
            {
                testInput[dateWeekDaysInd] = p_dateWeekDays[inputIdx];      // forecast the barChange for the day p_dateWeekDays[p_iRebalance]
                NeuralSnifferUtil.RenormalizeIntoUnitRange(testInput, dateWeekDaysInd, dateWeekDaysNormParam);
            }
            if (isUseBarChange)
            {
                //testInput[barChangeInd] = p_barChanges[inputIdx];      // forecast the barChange for the day p_dateWeekDays[p_iRebalance]
                for (int j = 0; j < barChangeDim; j++)
                {
                    int barChangeLookbackInd = p_barChangeLookbackDaysInds[j];
                    testInput[barChangeInd + j] = p_barChanges[inputIdx - barChangeLookbackInd];
                    NeuralSnifferUtil.RenormalizeWithoutMovingCenterStd(testInput, barChangeInd + j, barChangeNormParam[j]);
                }
            }


            if (p_notNNStrategyType == 1)
            {
                // Buy&hold
                return 1;
            }

            if (p_notNNStrategyType == 2 || p_notNNStrategyType == 3) // 2 = MR, 3 = FT
            {
                // deterministic MR and FT
                double returnInUpDays = p_notNNStrategyType == 2 ? -1 : 1;
                if (p_barChanges[p_iRebalance] < 0)
                    return -1 * returnInUpDays;
                else if (p_barChanges[p_iRebalance] > 0)
                    return returnInUpDays;
                else
                    return 0;
            }

            if (p_notNNStrategyType == 4) // Naive learner, 1 bins
            {
                return NeuralSnifferUtil.NaiveLearner1DForecast(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { Double.PositiveInfinity }, testInput[0]);
            }

            if (p_notNNStrategyType == 5) // Naive learner, 2 bins
            {
                return NeuralSnifferUtil.NaiveLearner1DForecast(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { 0.0, Double.PositiveInfinity }, testInput[0]);
            }

            if (p_notNNStrategyType == 6) // Naive learner, 4 bins
            {
                double inputStd = nnInput.Select(r => r[0]).ToArray().StandardDeviation();
                return NeuralSnifferUtil.NaiveLearner1DForecast(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { -0.6717, 0.0, 0.6717, Double.PositiveInfinity }.ScalarProduct(inputStd), testInput[0]);
                //%4 bins with equal distribution: %25% of the samples are under -0.6717 std away
                //range4Bins = [-0.6717  0 0.6717 inf];
                //tick4Bins = [2*-0.6717 -0.6717 0 0.6717];
                //%6 bins with equal distribution: 16.6%: -0.9678; 33.3%: -0.429; 50%: 0, 
                //range6Bins = [-0.96787  -0.429 0 0.429 0.96787 inf];
                //tick6Bins = [(-0.96787-0.96787+0.429) -0.96787  -0.429 0 0.429 0.96787];
                //%10 bins with equal distribution: 10%, 20%, 30%, 40%, 50%, 60%, 70%, 80%, 90%: -1.2801/-0.8391/-0.5216/-0.2513/0/0.2513/0.5216/0.8391/1.2801
                //range10Bins = [-1.2801 -0.8391 -0.5216 -0.2513 0 0.2513 0.5216 0.8391 1.2801 inf];
                //tick10Bins = [-1.2801-(1.2801-0.8391) -1.2801 -0.8391 -0.5216 -0.2513 0 0.2513 0.5216 0.8391 1.2801];
                //%20 bins with equal distribution: 5%, 10%, 20%, 30%, 40%, 50%, 60%, 70%, 80%, 90%: 
                //range20Bins = [-1.6445 -1.2801 -1.0343 -0.8391 -0.6717 -0.5216 -0.3828 -0.2513 -0.1244 0 0.1244 0.2513 0.3828 0.5216 0.6717 0.8391 1.0343 1.2801 1.6445 inf];
                //tick20Bins = [-1.6445-(1.6445-1.2801) -1.6445 -1.2801 -1.0343 -0.8391 -0.6717 -0.5216 -0.3828 -0.2513 -0.1244 0 0.1244 0.2513 0.3828 0.5216 0.6717 0.8391 1.0343 1.2801 1.6445];

            }

            if (p_notNNStrategyType != -1)
            {
                StrongAssert.Fail("Unrecognized p_notNNStrategyType");
            }


            // Matlab emulation network
            // consider the newFF() in Matlab:
            // 1. The input is not a layer; no activation function, no bias
            // 2. The middle layer has a bias, and tansig transfer function
            // 3. The output is a layer; having a bias (I checked); but has Linear activation  (in the default case); in the Matlab book, there are examples with tansig ouptput layers too
            // Jeff use a similar one
            //  I've been using a linear activation function on the output layer, and sigmoid or htan on the input and hidden lately for my prediction nets, 
            // and getting lower error rates than a uniform activation function. (uniform: using the same on every layer)
            BasicNetwork network = new BasicNetwork();
            //network.Logic = new Encog.Neural.Networks.Logic.FeedforwardLogic(); // the default is SimpleRecurrentLogic; but FeedforwardLogic() is faster, simpler
            network.AddLayer(new BasicLayer(new ActivationLinear(), false, inputDim)); // doesn't matter what is here. nor the act.function, neither the bias are used
            network.AddLayer(new BasicLayer(new ActivationTANH(), true, p_nNeurons));
            network.AddLayer(new BasicLayer(new ActivationLinear(), true, 1));
            network.FinalizeStructure();  // after this, the Layers.BiasWeight and Synapses.Weights are zero


            //Utils.Input1DVisualizer(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { 0.0, Double.MaxValue }, new double[] { });
            //Utils.Input1DVisualizer(nnInput.Select(r => r[0]).ToArray(), nnOutput.Select(r => r[0]).ToArray(), new double[] { 0.5, 1.5, 2.5, 3.5, 4.5}, new double[] { }); 

            double[] agyTestForecast = new double[1];
            double[] agyTestTrainError = new double[1];
            ResilientPropagation train = null;
            for (int i = 0; i < agyTestForecast.Length; i++)
            {
                network.Reset();
                //network.Reset(new Random(123));    // randomizes  Layers.BiasWeight and Synapses.Weights; if initialweights are left zero; they will be zeroWeights after Resilient and Backprog training. Only the last biasWeight will be non-zero. means: the output will be the same regardless of the input; Bad.

                // train the neural network
                var trainingData = BasicData.ConvertArrays(nnInput, nnOutput);
                train = new ResilientPropagation(network, trainingData); // err remained=0.99; with 100 random samples: accumulatedValue=0.69, 0.09, 0.57; up/down ratio stayed at 0.52 in all cases (ratio is stable)
                //train.NumThreads = 1; // default is 0; that means Encog can determine how many; (but I specificaly want 1 threads train, because I run 4 backtests in parallel; so I use the CPUs anyway)

                int epoch = 1;
                do
                {
                    train.Iteration();
                    //Utils.Logger.Info("Epoch #" + epoch + " Error:" + train.Error);
                    epoch++;
                } while ((epoch <= p_maxEpoch) && (train.LastError > 0.001));      // epoch = 5000?



                //var inputData = new BasicData(testInput);
                var ouput = network.ComputeRegression(testInput);
                //double[] ouput = outputData.Data;
                NeuralSnifferUtil.DenormalizeWithoutMovingCenterStd(ouput, 0, outputMultiplier);
                //double forecast = outputData[0];
                double forecast = ouput[0];
                //Utils.Logger.Info(@"Real%change: " + p_barChanges[p_iRebalance] * 100 + "%, Network forecast: " + outputData.ToString() + "%");
                agyTestForecast[i] = forecast;
                agyTestTrainError[i] = train.LastError;
            }

            //NNVisualizer(network, Enumerable.Range(0, 11).Select(r => new double[] { ((double)r / 10.0 - 0.5) * 2 * 0.001}).ToArray(), AppDomain.CurrentDomain.BaseDirectory + "NNchart.csv");
            //NNVisualizer(network, Enumerable.Range(0, 101).Select(r => new double[] { ((double)r / 100.0 - 0.5) * 2 * 0.10 * inputMultiplier }).ToArray(), AppDomain.CurrentDomain.BaseDirectory + "NNchart.csv");    // -10% to +10%

            p_trainError = agyTestTrainError[0];
            return agyTestForecast[0];
        }
    }
}
