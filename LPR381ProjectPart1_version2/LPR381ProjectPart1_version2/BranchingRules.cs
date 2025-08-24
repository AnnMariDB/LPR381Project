using System;
using System.Collections.Generic;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Branching rules for Branch & Bound.
    /// </summary>
    public static class BranchingRules
    {
        /// <summary>
        /// Pick a variable index to branch on from the current LP solution x.
        /// Only variables flagged as integral in isIntegral are considered.
        /// Returns -1 if all integral vars are (nearly) integers.
        /// Strategy: choose the var whose fractional part is closest to 0.5.
        /// </summary>
        public static int PickBranchVariable(double[] x, List<bool> isIntegral)
        {
            if (x == null || isIntegral == null) return -1;

            int idx = -1;
            double bestScore = double.NegativeInfinity; // higher is better (closer to 0.5)

            int n = Math.Min(x.Length, isIntegral.Count);
            for (int i = 0; i < n; i++)
            {
                if (!isIntegral[i]) continue;

                double xi = x[i];
                double frac = Math.Abs(xi - Math.Round(xi));
                if (frac <= 1e-6) continue; // already integral

                // closeness to 0.5 (maximise this)
                double score = 0.5 - Math.Abs(frac - 0.5);
                if (score > bestScore)
                {
                    bestScore = score;
                    idx = i;
                }
            }
            return idx;
        }
    }
}
