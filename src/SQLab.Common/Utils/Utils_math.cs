using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqCommon
{
    public static partial class Utils
    {
        #region IsNear*
        public const double REAL_EPS = 1e-6;        // used for Doubles
        public const double REAL_EPS2 = REAL_EPS * REAL_EPS;
        public const float FLOAT_EPS = (float)1e-4; // used for Floats

        // Smallest such that 1.0+EPSILON != 1.0
        public const double DBL_EPSILON = 2.2204460492503131e-016;
        public const float FLT_EPSILON = 1.192092896e-07F;
        public const double DYNAMIC_DBL_EPSILON = -12;  // 12 digits precision for doubles. Do not use it for floats.
        public const float DYNAMIC_FLT_EPSILON = -9;   // 4.2 digits precision for floats. Do not use it for doubles!

        /// <summary> NaN causes ArithmeticException </summary>
        public static int NearSign(double x)
        {
            return IsNearZero(x) ? 0 : Math.Sign(x);
        }

        /// <summary> False for NaN </summary>
        public static bool IsNearZero(double x)
        {
            return (Math.Abs(x) <= REAL_EPS);
        }

        /// <summary> False for NaN </summary>
        public static bool IsNearZero2(double x)
        {
            return (Math.Abs(x) <= REAL_EPS2);
        }

        /// <summary> False if r1 or r2 is NaN.
        /// Negative epsilon specifies a dynamic epsilon (see EpsilonEqCmp).
        /// In this case returns true if both r1 and r2 are NaN.
        /// </summary>
        public static bool IsNear(double r1, double r2, double eps)
        {
            //if (eps <= -1)
            //    return new EpsilonEqCmp(eps, 0).Equals(r1, r2);
            // The following solution works even if the numbers are -DBL_MAX and DBL_MAX.
            // (As opposed to "fabs(r1-r2) < givenEps" which would fail on that.)
            return (r1 < r2) ? (r2 <= r1 + eps) : (r1 <= r2 + eps);
        }

        /// <summary> False if r1 or r2 is NaN </summary>
        public static bool IsNear(double r1, double r2)
        {
            return IsNear(r1, r2, REAL_EPS);
        }

        /// <summary> False if r1 or r2 is NaN </summary>
        public static bool IsLess(double r1, double r2)
        {
            return (r1 < r2) && !IsNear(r1, r2, REAL_EPS);
        }

        /// <summary> Negative epsilon specifies a dynamic epsilon (see EpsilonEqCmp) </summary>
        public static bool IsLess(double r1, double r2, double eps)
        {
            return (r1 < r2) && !IsNear(r1, r2, eps);
        }

        public static bool IsNear(float r1, float r2, float eps)
        {
            //if (eps <= -1)
            //    return new EpsilonEqCmp(0, eps).Equals(r1, r2);
            // The following solution works even if the numbers are -DBL_MAX and DBL_MAX.
            // (As opposed to "fabs(r1-r2) < givenEps" which would fail on that.)
            return (r1 < r2) ? (r2 <= r1 + eps) : (r1 <= r2 + eps);
        }

        public static bool IsNear(float r1, float r2)
        {
            return IsNear(r1, r2, FLOAT_EPS);
        }

        #endregion

    }
}
