using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Cutting Plane using Gomory fractional cuts, with a Revised Simplex (PFI) core and price-out.
    /// - Selects the most-fractional BASIC integer row (fractional RHS).
    /// - Slack-space cut: α·s ≥ f, α = frac(row_i(B^{-1})) (handles negatives).
    /// - Convert via s = b − A x ⇒ (αA)x ≤ αb − f.
    /// - Before adding: verifies the cut violates the current solution and is not a duplicate.
    /// - Warm-starts PFI and repeats until integer or no eligible row.
    /// </summary>
    public class CuttingPlaneSolver
    {
        private readonly LinearProblem problem;
        private const int DefaultMaxCuts = 25;

        public CuttingPlaneSolver(LinearProblem problem)
        {
            this.problem = problem;
        }

        public string Solve(out double[] bestX, out double bestZ)
        {
            var sb = new StringBuilder();
            bestX = Array.Empty<double>();
            bestZ = double.NaN;

            var isInt = BuildIntegralMask(problem);

            sb.AppendLine("=== Cutting Plane (Gomory Fractional) with PFI + Price-Out ===");

            // IMPORTANT: Create ONE PFI and REUSE IT (do NOT re-create inside the loop)
            var pfi = new RevisedSimplexPFI(problem);

            // Initial solve
            sb.AppendLine("---- PFI: Initial LP relaxation ----");
            {
                var pfiLog = pfi.Optimize(out var x0, out var z0);
                sb.AppendLine(pfiLog);
                if (AllFlaggedIntegersIntegral(x0, isInt))
                {
                    bestX = x0;
                    bestZ = z0;
                    sb.AppendLine("Already integer on flagged variables.");
                    return sb.ToString();
                }
            }

            int cuts = 0;
            while (cuts < DefaultMaxCuts)
            {
                // Current basic values and basis (NO extra solve here)
                var xb = pfi.ComputeXB();
                int m = pfi.Rows;
                int n = pfi.Cols;
                int[] Bidx = pfi.Basis;

                // Build current x for original vars
                double[] xCurr = new double[n];
                for (int i = 0; i < m; i++)
                {
                    int col = Bidx[i];
                    if (col < n) xCurr[col] = xb[i];
                }

                // Most-fractional BASIC integer row
                int chosenRow = -1;
                double f = 0.0;
                string basicName = "";
                double bestScore = -1.0; // closeness to 0.5

                for (int i = 0; i < m; i++)
                {
                    int col = Bidx[i];
                    if (col < n && col < isInt.Count && isInt[col])
                    {
                        double bi = xb[i];
                        double fi = Frac(bi);
                        if (fi > 1e-8 && fi < 1 - 1e-8)
                        {
                            double score = 0.5 - Math.Abs(fi - 0.5);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                chosenRow = i; f = fi; basicName = $"x{col + 1}";
                            }
                        }
                    }
                }

                if (chosenRow < 0)
                {
                    sb.AppendLine("No eligible fractional basic-integer row found. Stopping.");
                    break;
                }

                // α = frac(row_i(B^{-1})) via B^{-T} e_i
                var alphaRow = pfi.GetBinvRow(chosenRow);
                for (int j = 0; j < alphaRow.Length; j++)
                {
                    double v = alphaRow[j];
                    double fr = v - Math.Floor(v);
                    if (fr < 0) fr += 1.0;
                    alphaRow[j] = fr;
                }

                // Convert α·s ≥ f with s = b − A x  =>  (αA)x ≤ αb − f
                var A = problem.Constraints;
                var b = problem.RHS;

                double[] aCut = new double[n];     // (αA)
                for (int j = 0; j < n; j++)
                {
                    double sum = 0.0;
                    for (int r = 0; r < m; r++) sum += alphaRow[r] * A[r][j];
                    aCut[j] = sum;
                }

                double rhs = 0.0;                  // αb − f
                for (int r = 0; r < m; r++) rhs += alphaRow[r] * b[r];
                rhs -= f;

                // Violation check (NO solve): must be strictly violated at xCurr
                double lhs = 0.0;
                for (int j = 0; j < n; j++) lhs += aCut[j] * xCurr[j];

                if (lhs <= rhs + 1e-8)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Candidate cut from row C{chosenRow + 1} does not violate current solution (lhs={lhs:0.###} ≤ rhs={rhs:0.###}).");
                    sb.AppendLine("Stopping to avoid adding a non-cutting constraint.");
                    break;
                }

                // Duplicate guard
                if (IsDuplicate(problem.Constraints, problem.RHS, aCut, rhs))
                {
                    sb.AppendLine();
                    sb.AppendLine("New cut duplicates an existing row → stopping.");
                    break;
                }

                // Log both forms
                cuts++;
                sb.AppendLine();
                sb.AppendLine($"--- Gomory Cut #{cuts} from row C{chosenRow + 1} (basic {basicName}) ---");
                sb.AppendLine(RenderSlackCut(alphaRow, f));
                sb.AppendLine(RenderXCut(aCut, rhs));

                // ADD CUT, then WARM-START, then OPTIMIZE  (this order matters!)
                problem.Constraints.Add(aCut.ToList());
                problem.RHS.Add(rhs);
                pfi.AddRowAndWarmStart(aCut, rhs);

                sb.AppendLine($"---- PFI: After cut #{cuts} ----");
                var pfiLog2 = pfi.Optimize(out var xk, out var zk);
                sb.AppendLine(pfiLog2);

                if (AllFlaggedIntegersIntegral(xk, isInt))
                {
                    bestX = xk;
                    bestZ = zk;
                    sb.AppendLine("All flagged integer variables integral. Done.");
                    return sb.ToString();
                }
            }

            // Final (may be fractional)
            {
                var pfiLog = pfi.Optimize(out var xend, out var zend);
                sb.AppendLine(pfiLog);
                bestX = xend;
                bestZ = zend;
                sb.AppendLine($"Stopped after {cuts} cut(s). Returning current solution (may be fractional).");
            }

            return sb.ToString();
        }

        // ---------------- helpers ----------------

        private static List<bool> BuildIntegralMask(LinearProblem p)
        {
            int n = p.ObjectiveCoeffs.Count;
            var mask = new List<bool>(Enumerable.Repeat(false, n));
            if (p.IsBinary == null || p.IsBinary.Count == 0)
            {
                for (int i = 0; i < n; i++) mask[i] = true; // default: all integer
                return mask;
            }
            int m = Math.Min(n, p.IsBinary.Count);
            for (int i = 0; i < m; i++) mask[i] = p.IsBinary[i];
            return mask;
        }

        private static bool AllFlaggedIntegersIntegral(double[] x, List<bool> isInt, double eps = 1e-6)
        {
            int n = Math.Min(x.Length, isInt.Count);
            for (int i = 0; i < n; i++)
            {
                if (!isInt[i]) continue;
                if (Math.Abs(x[i] - Math.Round(x[i])) > eps) return false;
            }
            return true;
        }

        private static bool IsDuplicate(List<List<double>> A, List<double> B, double[] aNew, double bNew, double tol = 1e-8)
        {
            for (int i = 0; i < A.Count; i++)
            {
                if (Math.Abs(B[i] - bNew) > tol) continue;
                bool same = true;
                for (int j = 0; j < aNew.Length; j++)
                {
                    if (Math.Abs(A[i][j] - aNew[j]) > tol) { same = false; break; }
                }
                if (same) return true;
            }
            return false;
        }

        private static double Frac(double v)
        {
            double f = v - Math.Floor(v);
            if (f < 0) f += 1.0;
            return f;
        }

        private static string RenderSlackCut(double[] alpha, double f)
        {
            // α·s ≥ f   (equivalently  −α·s ≤ −f)
            string Join(double sign = +1.0)
            {
                var parts = new List<string>();
                for (int j = 0; j < alpha.Length; j++)
                {
                    double c = sign * alpha[j];
                    if (Math.Abs(c) < 1e-12) continue;
                    string term = $"{Math.Abs(c):0.###}s{j + 1}";
                    parts.Add(c >= 0 ? term : "− " + term);
                }
                if (parts.Count == 0) return "0";
                return parts[0] + string.Concat(parts.Skip(1).Select(p => " + " + p));
            }
            var ge = Join(+1);
            var le = Join(-1);
            return $"Slack-space cut:   {ge} ≥ {f:0.###}   (equivalently  {le} ≤ {(-f):0.###})";
        }

        private static string RenderXCut(double[] coeffs, double rhs)
        {
            var parts = new List<string>();
            for (int j = 0; j < coeffs.Length; j++)
            {
                if (Math.Abs(coeffs[j]) < 1e-12) continue;
                parts.Add($"{coeffs[j]:0.###}x{j + 1}");
            }
            var left = parts.Count == 0 ? "0" : string.Join(" + ", parts);
            return $"x-space cut added: {left} ≤ {rhs:0.###}";
        }
    }
}
