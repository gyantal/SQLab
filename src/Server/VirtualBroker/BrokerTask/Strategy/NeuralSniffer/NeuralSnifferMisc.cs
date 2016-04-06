using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker.Strategy.NeuralSniffer
{
    partial class NeuralSniffer
    {
        private static void EliminateOutliers(double p_outputOutlierThreshold, ref double[][] p_nnInput, ref double[][] p_nnOutput)
        {
            if (!Double.IsPositiveInfinity(p_outputOutlierThreshold))
            {
                // A. Fill outlierIndices list
                List<int> outlierIndices = new List<int>();
                for (int i = 0; i < p_nnInput.GetLength(0); i++)
                {
                    if (Math.Abs(p_nnOutput[i][0]) > p_outputOutlierThreshold)
                    {
                        outlierIndices.Add(i);
                    }
                }

                if (outlierIndices.Count != 0)
                {
                    int nNewTrainingSamples = p_nnInput.GetLength(0) - outlierIndices.Count;

                    double[][] outlierFreeInput = new double[nNewTrainingSamples][];
                    double[][] outlierFreeOutput = new double[nNewTrainingSamples][];
                    int outlierFreeIdx = 0;
                    for (int i = 0; i < p_nnInput.GetLength(0); i++)
                    {
                        if (!outlierIndices.Contains(i))
                        {
                            outlierFreeInput[outlierFreeIdx] = p_nnInput[i];
                            outlierFreeOutput[outlierFreeIdx] = p_nnOutput[i];
                            outlierFreeIdx++;
                        }
                    }

                    // replace
                    p_nnInput = outlierFreeInput;
                    p_nnOutput = outlierFreeOutput;
                }
            }
        }

    }
}
