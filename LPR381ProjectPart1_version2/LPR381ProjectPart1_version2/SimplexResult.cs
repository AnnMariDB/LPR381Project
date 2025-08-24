using System;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Container for LP results used by Branch & Bound and UI.
    /// </summary>
    public class SimplexResult
    {
        /// <summary>True if the current LP relaxation reached optimality.</summary>
        public bool IsOptimal { get; set; }

        /// <summary>True if the LP relaxation is infeasible.</summary>
        public bool IsInfeasible { get; set; }

        /// <summary>True if the LP relaxation is unbounded.</summary>
        public bool IsUnbounded { get; set; }

        /// <summary>Decision variable values (original vars first, then slacks) for the returned basis.</summary>
        public double[] X { get; set; } = Array.Empty<double>();

        /// <summary>Objective value at the returned solution (already with the correct sign for max/min).</summary>
        public double ObjectiveValue { get; set; }

        /// <summary>Final simplex tableau (including the Z row).</summary>
        public double[,] FinalTableau { get; set; } = new double[0, 0];

        /// <summary>Indices of basic columns in the final tableau (length = number of constraints).</summary>
        public int[] FinalBasis { get; set; } = Array.Empty<int>();

        /// <summary>Human‑readable log produced during simplex iterations.</summary>
        public string Log { get; set; } = "";
    }
}
