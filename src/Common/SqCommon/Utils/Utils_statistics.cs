using SQCommon.MathNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqCommon
{
    public static partial class Utils
    {


        //http://stackoverflow.com/questions/12394570/math-log-vs-multiplication-complexity-in-terms-of-geometric-average-which-is-bet
        // multiply is much quicker, if there is no overflow problem; because I multiply numbers around 1.0, the final value will the the PV of the final thing. That is still OK.
        public static double GMeanExtendingWithOne(this IEnumerable<double> p_source)
        {
            int nCount = 0;
            double prod = 1.0;
            foreach (double d in p_source)
            {
                prod *= 1.0 + d;
                nCount++;
            }

            return Math.Pow(prod, 1.0 / ((double)nCount)) - 1.0;
        }

        // StatisticFormula g_statTool.Median() uses a string parameter, not a List
        public static double Median(this IList<double> p_source)
        {
            // we have to quick sort it, but that would be O(n*log n), while in this quick-sort kind of way we can do O(n)
            int nCount = p_source.Count;
            if (nCount == 0)
                return Double.NaN;
            else
            {
                List<double> newList = p_source.ToList();  // we will change the list items orders, because of ordering, so Clone it
                if (nCount % 2 == 1)
                    return newList.NthOrderStatistic((nCount - 1) / 2);  // C# lists are zero based, so -1 is used
                else
                    return (newList.NthOrderStatistic((int)Math.Floor(((double)nCount - 1.0) / 2.0)) + newList.NthOrderStatistic((int)Math.Ceiling(((double)nCount - 1.0) / 2.0))) / 2.0; // C# lists are zero based, so -1 is used
            }
        }

        //// http://stackoverflow.com/questions/4140719/calculate-median-in-c-sharp
        //Few notes:
        //This code replaces tail recursive code from the original version in book in to iterative loop.
        //It also eliminates unnecessary extra check from original version when start==end.
        //I've provided two version of Median, one that accepts IEnumerable and then creates a list. If you use the version that accepts IList then keep in mind it modifies the order in list.
        //Above methods calculates median or any i-order statistics in O(n) expected time. If you want O(n) worse case time then there is technique to use median-of-median. While this would improve worse case performance, it degrades average case because constant in O(n) is now larger. However if you would be calculating median mostly on very large data then its worth to look at.
        //The NthOrderStatistics method allows to pass random number generator which would be then used to choose random pivot during partition. This is generally not necessary unless you know your data has certain patterns so that last element won't be random enough or if somehow your code is exposed outside for targeted exploitation.
        //Definition of median is clear if you have odd number of elements. It's just the element with index (Count-1)/2 in sorted array. But when you even number of element (Count-1)/2 is not an integer anymore and you have two medians: Lower median Math.Floor((Count-1)/2) and Math.Ceiling((Count-1)/2). Some textbooks use lower median as "standard" while others propose to use average of two. This question becomes particularly critical for set of 2 elements. Above code returns lower median. If you wanted instead average of lower and upper then you need to call above code twice. In that case make sure to measure performance for your data to decide if you should use above code VS just straight sorting.
        //For .net 4.5+ you can add MethodImplOptions.AggresiveInlining attribute on Swap<T> method for slightly improved performance.
        /// <summary>
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Pivot is selected ranodmly if random number generator is supplied else its selected as last element in the list.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
        private static int Partition<T>(this IList<T> list, int start, int end, Random rnd = null) where T : IComparable<T>
        {
            if (rnd != null)
                list.Swap(end, rnd.Next(start, end));

            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
            {
                if (list[i].CompareTo(pivot) <= 0)
                    list.Swap(i, ++lastLow);
            }
            list.Swap(end, ++lastLow);
            return lastLow;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        public static T NthOrderStatistic<T>(this IList<T> list, int n, Random rnd = null) where T : IComparable<T>
        {
            return NthOrderStatistic(list, n, 0, list.Count - 1, rnd);
        }
        private static T NthOrderStatistic<T>(this IList<T> list, int n, int start, int end, Random rnd) where T : IComparable<T>
        {
            while (true)
            {
                var pivotIndex = list.Partition(start, end, rnd);
                if (pivotIndex == n)
                    return list[pivotIndex];

                if (n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
                return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        /// <summary>
        /// Note: specified list would be mutated in the process.
        /// </summary>
        public static T LowerMedian<T>(this IList<T> list) where T : IComparable<T>
        {
            return list.NthOrderStatistic((list.Count - 1) / 2);
        }

        public static double LowerMedian<T>(this IEnumerable<T> sequence, Func<T, double> getValue)
        {
            var list = sequence.Select(getValue).ToList();
            var mid = (list.Count - 1) / 2;
            return list.NthOrderStatistic(mid);
        }



        //
        // Summary:
        //     The t-distribution formula calculates the probability for the Student's t-distribution.
        //
        // Parameters:
        //   value:
        //     t-value for which you want the distribution.
        //
        //   degreeOfFreedom:
        //     Degree of freedom
        //
        //   oneTail:
        //     If set to true, one-tailed distribution is used. Otherwise, two-tailed distribution
        //     is used.
        //
        // Returns:
        //     t-distribution function probability.
        // 
        public static double TDistribution(double p_tValue, int degreeOfFreedom, bool p_oneTail) // calculates p-value from T-value
        {
            // we used it from System.Web.UI.DataVisualization.Charting, but it doesn't exist in DotNetCore
            // static StatisticFormula g_statTool = (new Chart()).DataManipulator.Statistics; 
            // p_sampleStats.PvalueToZero = g_statTool.TDistribution(p_sampleStats.TvalueToZero, nInt - 1, true);


            // Math.NET was suggested on StackOverflow.com http://www.mathdotnet.com/
            // https://en.wikipedia.org/wiki/Student%27s_t-distribution#Table_of_selected_values

            // for T-value (x) -1.65, it returns p-value = 5.01%        so, for negative T-values, it is OK.
            // for T-value (x) +1.65, it returns p-value = 94.99% (because it is a CDF),       so, for positive T-values, you have to invert it.
            double cdfStudent = StudentT.CDF(0, 1.0, degreeOfFreedom, p_tValue);
            if (p_tValue > 0)
                cdfStudent = 1.0 - cdfStudent;

            if (p_oneTail)
                return cdfStudent;
            else
                return 2.0 * cdfStudent;   // The two-tail P value is twice the one-tail P value, http://www.graphpad.com/guides/prism/6/statistics/index.htm?one-tail_vs__two-tail_p_values.htm
        }

        




    }


    
    
}

