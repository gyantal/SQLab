using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker.Strategy.NeuralSniffer
{
    public struct RangeNormalizationParams
    {
        public double Min;
        public double Max;
        public double Multiplier;
    }

    static class ExtensionMethodUtil
    {
        public static double StandardDeviation(this IEnumerable<double> source)
        {
            double avg = source.Average();
            return Math.Sqrt(source.Select(r => (r - avg) * (r - avg)).Sum() / (double)source.Count());
        }

        public static double[] ScalarProduct(this IEnumerable<double> source, double p_scalar)
        {
            return source.Select(r => r * p_scalar).ToArray();
        }

        //LinearApprox is not so good. In 23 years backtest, the deltaInc is $0.04. That is 4% daily profit on the first day, but the same $0.04 is only 0.0001% on the last day. It is the additive profit factor.
        public static double MAPEfromLinearApprox(this IEnumerable<double> p_source)
        {
            double firstVal = p_source.First();
            double lastVal = p_source.Last();
            int length = p_source.Count();
            double deltaInc = (lastVal - firstVal) / (double)(length - 1);
            var linApprox = Enumerable.Range(0, length).Select(r => firstVal + r * deltaInc);

            double result = Enumerable.Range(0, length).Select(r => Math.Abs((p_source.ElementAt(r) - linApprox.ElementAt(r)) / linApprox.ElementAt(r))).Sum() / length;
            return result;
        }

        public static double MAPEfromMultiplicativeApprox(this IEnumerable<double> p_source)
        {
            double lastVal = p_source.Last();
            int length = p_source.Count();
            double deltaMul = Math.Pow(lastVal, 1.0 / (length - 1));        // using 1 instead of 1.0 was a bug

            double[] mulApprox = new double[length];
            mulApprox[0] = 1;
            for (int i = 1; i < length; i++)
            {
                mulApprox[i] = mulApprox[i - 1] * deltaMul;
            }

            double result = Enumerable.Range(0, length).Select(r => Math.Abs((p_source.ElementAt(r) - mulApprox.ElementAt(r)) / mulApprox.ElementAt(r))).Sum() / length;
            return result;
        }
    }

        static partial class NeuralSnifferUtil
    {
        internal static double NaiveLearner1DForecast(double[] p_nnInput, double[] p_nnOutput, double[] p_inputRange, double p_testInput)
        {
            double[] bracketAvg = new double[p_inputRange.Length];
            int[] bracketNum = new int[p_inputRange.Length];

            for (int i = 0; i < p_nnInput.Length; i++)
            {
                double input = p_nnInput[i];
                double output = p_nnOutput[i];

                int indBracket = 0;
                while (p_inputRange[indBracket] < input)
                {
                    indBracket++;
                }

                bracketAvg[indBracket] += output;
                bracketNum[indBracket]++;
            }

            for (int i = 0; i < bracketAvg.Length; i++)
            {
                if (bracketNum[i] != 0)
                    bracketAvg[i] /= bracketNum[i];
            }

            //for (int i = 0; i < p_inputRange.Length; i++)
            //{
            //    Utils.Logger.Info(@"Bracket under " + p_inputRange[i] + " : " + bracketAvg[i]);

            //}

            int indBracketTest = 0;
            while (p_inputRange[indBracketTest] < p_testInput)
            {
                indBracketTest++;
            }

            return bracketAvg[indBracketTest];
        }

        public static RangeNormalizationParams NormalizeIntoUnitRange(double[][] p_array, int p_ind, double p_extraBoostToMultiplier)
        {
            double min = p_array.Select(r => r[p_ind]).Min();
            double max = p_array.Select(r => r[p_ind]).Max();

            for (int i = 0; i < p_array.Length; i++)
            {
                p_array[i][p_ind] = ((p_array[i][p_ind] - min) / (max - min) - 0.5) * 2.0 * p_extraBoostToMultiplier;
            }

            return new RangeNormalizationParams() { Min = min, Max = max, Multiplier = p_extraBoostToMultiplier };
        }

        public static double NormalizeWithoutMovingCenterStd(double[][] p_array, int p_ind, double p_extraBoostToMultiplier)
        {
            double mean = p_array.Select(r => r[p_ind]).Average();
            double std = p_array.Select(r => r[p_ind]).StandardDeviation();

            double multiplier = 1 / std * p_extraBoostToMultiplier;
            for (int i = 0; i < p_array.Length; i++)
            {
                p_array[i][p_ind] *= multiplier;
            }

            return multiplier;
        }

        public static void RenormalizeIntoUnitRange(double[] p_value, int p_ind, RangeNormalizationParams p_normParams)
        {
            p_value[p_ind] = ((p_value[p_ind] - p_normParams.Min) / (p_normParams.Max - p_normParams.Min) - 0.5) * 2.0 * p_normParams.Multiplier;
        }

        public static void RenormalizeWithoutMovingCenterStd(double[] p_value, int p_ind, double p_multiplier)
        {
            p_value[p_ind] *= p_multiplier;
        }

        public static void DenormalizeWithoutMovingCenterStd(double[] p_value, int p_ind, double p_multiplier)
        {
            p_value[p_ind] /= p_multiplier;
        }
    }
}
