using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381ProjectPart1_version2
{
    public class RevisedSimplex
    {
        
        private readonly double[,] A;     // Augmented matrix [m x (n+m)]
        private readonly double[] b;      // RHS values
        private readonly double[] c;      // Objective coefficients (adjusted for max)
        private readonly bool originalIsMax;
        private readonly int nVars;       // decision variables
        private readonly int mCons;       // constraints

        private List<int> basis;          // indices of basic columns
        private List<int> nonBasis;       // indices of non-basic columns

        private const double EPS = 1e-9;
        public RevisedSimplexState LastState { get; private set; }

        public RevisedSimplex(double[,] A_aug, double[] b, double[] cMax, bool isMax, int nOriginal)
        {
            this.A = A_aug;
            this.b = b;
            this.c = cMax;
            this.originalIsMax = isMax;
            this.nVars = nOriginal;
            this.mCons = b.Length;

            // Initial basis = slack variables (last m columns)
            basis = Enumerable.Range(nVars, mCons).ToList();

            // Initial non-basis = decision variables (first n columns)
            nonBasis = Enumerable.Range(0, nVars).ToList();
        }

        // Formatting
        private static string FormatNumber(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < 1e-9)
                return ((int)Math.Round(value)).ToString();
            return value.ToString("0.00");
        }

        private static string VarName(int j, int nVars)
            => j < nVars ? $"x{j + 1}" : $"s{j - nVars + 1}";

        private static string FormatVector(double[] v)
            => "[" + string.Join(" ", v.Select(FormatNumber)) + "]";

        private static string FormatMatrix(double[,] M)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < M.GetLength(0); i++)
            {
                sb.Append("   "); // indent
                for (int j = 0; j < M.GetLength(1); j++)
                    sb.Append(FormatNumber(M[i, j])).Append("\t");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        //  Linear Algebra
        private static double[,] Clone(double[,] M)
        {
            int r = M.GetLength(0), c = M.GetLength(1);
            var R = new double[r, c];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    R[i, j] = M[i, j];
            return R;
        }

        private static double[,] Inverse(double[,] mat)
        {
            int n = mat.GetLength(0);
            var a = Clone(mat);
            var inv = new double[n, n];
            for (int i = 0; i < n; i++) inv[i, i] = 1;

            for (int col = 0; col < n; col++)
            {
                int piv = col;
                for (int r = col; r < n; r++)
                    if (Math.Abs(a[r, col]) > Math.Abs(a[piv, col]))
                        piv = r;

                if (Math.Abs(a[piv, col]) < EPS)
                    throw new InvalidOperationException("Singular matrix (cannot invert B).");

                if (piv != col)
                {
                    for (int j = 0; j < n; j++)
                    {
                        (a[col, j], a[piv, j]) = (a[piv, j], a[col, j]);
                        (inv[col, j], inv[piv, j]) = (inv[piv, j], inv[col, j]);
                    }
                }

                double diag = a[col, col];
                for (int j = 0; j < n; j++)
                {
                    a[col, j] /= diag;
                    inv[col, j] /= diag;
                }

                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double f = a[r, col];
                    for (int j = 0; j < n; j++)
                    {
                        a[r, j] -= f * a[col, j];
                        inv[r, j] -= f * inv[col, j];
                    }
                }
            }
            return inv;
        }

        private static double[] MatVec(double[,] M, double[] v)
        {
            int r = M.GetLength(0), c = M.GetLength(1);
            var res = new double[r];
            for (int i = 0; i < r; i++)
            {
                double sum = 0;
                for (int j = 0; j < c; j++) sum += M[i, j] * v[j];
                res[i] = sum;
            }
            return res;
        }

        private static double[] RowVecMat(double[] row, double[,] M)
        {
            int r = M.GetLength(0), c = M.GetLength(1);
            var res = new double[c];
            for (int j = 0; j < c; j++)
            {
                double sum = 0;
                for (int i = 0; i < r; i++) sum += row[i] * M[i, j];
                res[j] = sum;
            }
            return res;
        }

        //  Main
        public string Solve()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Revised Primal Simplex Method ===");
            
            
            // Infeasibility check at initialization
            if (b.Any(val => val < -EPS))
            {
                sb.AppendLine("Infeasible problem (initial RHS has negative values).");
                return sb.ToString();
            }

            int iteration = 0;
            while (true)
            {
                iteration++;

                // Build B
                var B = new double[mCons, mCons];
                for (int i = 0; i < mCons; i++)
                    for (int j = 0; j < mCons; j++)
                        B[i, j] = A[i, basis[j]];

                var B_inv = Inverse(B);

                // cb, y^T, xB
                var cb = basis.Select(j => c[j]).ToArray();
                var yT = RowVecMat(cb, B_inv);
                var xB = MatVec(B_inv, b);

                // Reduced costs and A*
                var reduced = new double[nonBasis.Count];
                var AstarCache = new Dictionary<int, double[]>();
                for (int idx = 0; idx < nonBasis.Count; idx++)
                {
                    int jcol = nonBasis[idx];
                    var a_j = new double[mCons];
                    for (int i = 0; i < mCons; i++) a_j[i] = A[i, jcol];

                    var Astar = MatVec(B_inv, a_j);
                    AstarCache[jcol] = Astar;

                    double y_a = yT.Zip(a_j, (yi, ai) => yi * ai).Sum();
                    reduced[idx] = y_a - c[jcol];
                }

                double zMax = cb.Zip(xB, (ci, xi) => ci * xi).Sum();
                double zOriginal = originalIsMax ? zMax : -zMax;

                // Print Iteration 
                sb.AppendLine();
                sb.AppendLine($"---------------- Iteration {iteration} ----------------");
                sb.AppendLine();

                sb.AppendLine("Xbv: " + string.Join(", ", basis.Select(j => VarName(j, nVars))));
                sb.AppendLine("Cbv: " + string.Join(", ", cb.Select(FormatNumber)));
                sb.AppendLine("CbvB^-1: " + FormatVector(yT));
                sb.AppendLine();

                sb.AppendLine("B matrix:");
                sb.AppendLine(FormatMatrix(B));
                sb.AppendLine();

                sb.AppendLine("B^-1 matrix:");
                sb.AppendLine(FormatMatrix(B_inv));
                sb.AppendLine();

                sb.AppendLine("Xnbv: " + string.Join(", ", nonBasis.Select(j => VarName(j, nVars))));
                sb.AppendLine("Cnbv: " + string.Join(", ", nonBasis.Select(j => FormatNumber(c[j]))));
                sb.AppendLine();

                sb.AppendLine("Product Form (A*):");
                foreach (var j in nonBasis)
                    sb.AppendLine($"   {VarName(j, nVars)}*: {FormatVector(AstarCache[j])}");
                sb.AppendLine();

                sb.AppendLine("Price Out (Reduced Costs):");
                for (int idx = 0; idx < nonBasis.Count; idx++)
                    sb.AppendLine($"   {VarName(nonBasis[idx], nVars)}: {FormatNumber(reduced[idx])}");
                sb.AppendLine();

                sb.AppendLine("Basic Solution (xB): " + FormatVector(xB));
                sb.AppendLine($"Z (original {(originalIsMax ? "MAX" : "MIN")}) = {FormatNumber(zOriginal)}");
                sb.AppendLine();

                // Optimality check
                if (reduced.All(r => r >= -EPS))
                {
                    sb.AppendLine("Optimal solution found.");
                    sb.AppendLine();

                    var x = new double[nVars + mCons];
                    for (int i = 0; i < mCons; i++)
                        x[basis[i]] = xB[i];

                    for (int j = 0; j < nVars; j++)
                        sb.AppendLine($"{VarName(j, nVars)} = {FormatNumber(x[j])}");
                    for (int j = nVars; j < nVars + mCons; j++)
                        sb.AppendLine($"{VarName(j, nVars)} = {FormatNumber(x[j])}");

                    sb.AppendLine($"Optimal Z = {FormatNumber(zOriginal)}");
                    var reducedStdAll = new double[nVars + mCons];
                    for (int jcol = 0; jcol < nVars + mCons; jcol++)
                    {
                        double yDotA = 0.0;
                        for (int i = 0; i < mCons; i++) yDotA += yT[i] * A[i, jcol];
                        reducedStdAll[jcol] = c[jcol] - yDotA;  // standard sign
                    }

                    // Save a full snapshot for the Sensitivity form
                    this.LastState = new RevisedSimplexState
                    {
                        IsOptimal = true,
                        OriginalIsMax = originalIsMax,
                        A = A,
                        b = b,
                        c = c,
                        nVars = nVars,
                        mCons = mCons,
                        Basis = basis.ToArray(),
                        NonBasis = nonBasis.ToArray(),
                        BInv = B_inv,
                        xB = xB,
                        y = yT,                         // shadow prices y^T = c_B^T B^{-1}
                        ReducedCostsStd = reducedStdAll, // r_std = c - yA
                        ZOriginal = zOriginal
                    };
                    break;
                }

                // Entering variable: most negative reduced cost
                int enterPos = 0;
                double bestRC = reduced[0];
                for (int idx = 1; idx < reduced.Length; idx++)
                {
                    if (reduced[idx] < bestRC)
                    {
                        bestRC = reduced[idx];
                        enterPos = idx;
                    }
                }
                int entering = nonBasis[enterPos];
                var d = AstarCache[entering];

                // Ratio test
                double minTheta = double.PositiveInfinity;
                int leavePos = -1;
                for (int i = 0; i < mCons; i++)
                {
                    if (d[i] > EPS)
                    {
                        double theta = xB[i] / d[i];
                        if (theta < minTheta - 1e-12)
                        {
                            minTheta = theta;
                            leavePos = i;
                        }
                    }
                }

                if (leavePos == -1)
                {
                    sb.AppendLine("Unbounded problem.");
                    break;
                }

                sb.AppendLine("Ratio Test (θ):");
                for (int i = 0; i < mCons; i++)
                {
                    string thetaStr = d[i] > EPS ? FormatNumber(xB[i] / d[i]) : "∞";
                    sb.AppendLine($"   {VarName(basis[i], nVars)}: θ = {thetaStr}");
                }
                sb.AppendLine($"Entering: {VarName(entering, nVars)}");
                sb.AppendLine($"Leaving: {VarName(basis[leavePos], nVars)}");
                sb.AppendLine();

                // Pivot
                int leaving = basis[leavePos];
                basis[leavePos] = entering;
                nonBasis.Remove(entering);
                nonBasis.Add(leaving);
            }

            return sb.ToString();
        }
    }
}

