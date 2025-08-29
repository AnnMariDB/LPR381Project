using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Cutting Plane with Gomory fractional cuts (slides) + opportunistic 0–1 knapsack cover cuts
    /// when the Gomory candidate is weak or duplicate. Uses RevisedSimplexPFI with price-out and
    /// dual-simplex fallback, warm-starting between cuts.
    /// </summary>
    public class CuttingPlaneSolver
    {
        private readonly LinearProblem problem;
        private readonly int maxCuts;

        private const double EPS = 1e-9;

        public CuttingPlaneSolver(LinearProblem problem, int maxCuts = 25)
        {
            this.problem = problem;
            this.maxCuts = Math.Max(1, maxCuts);
        }

        public string Solve(out double[] bestX, out double bestZ)
        {
            var sb = new StringBuilder();
            bestX = Array.Empty<double>();
            bestZ = double.NaN;

            var isInt = BuildIntegralMask(problem);

            sb.AppendLine("=== Cutting Plane (Gomory Fractional) with PFI + Price-Out ===");

            // Reuse one PFI instance across all cuts
            var pfi = new RevisedSimplexPFI(problem);

            // Initial LP relaxation
            sb.AppendLine("---- PFI: Initial LP relaxation ----");
            {
                var pfiLog = pfi.Optimize(out var x0, out var z0);
                sb.AppendLine(pfiLog);
                if (AllFlaggedIntegersIntegral(x0, isInt))
                {
                    bestX = x0; bestZ = z0;
                    sb.AppendLine("Already integer on flagged variables. Done.");
                    return sb.ToString();
                }
            }

            int cuts = 0;
            while (cuts < maxCuts)
            {
                // current basis state without re-solving
                var xb = pfi.ComputeXB();
                int m = pfi.Rows;
                int n = pfi.Cols;
                int[] Bidx = pfi.Basis;

                // current x for ORIGINAL vars
                double[] xCurr = new double[n];
                for (int i = 0; i < m; i++)
                {
                    int col = Bidx[i];
                    if (col < n) xCurr[col] = xb[i];
                }

                // --- choose BASIC integer row with largest fractional RHS
                int chosenRow = -1;
                double f = 0.0;
                string basicName = "";
                double largestFrac = 0.0;

                for (int i = 0; i < m; i++)
                {
                    int col = Bidx[i];
                    if (col < n && col < isInt.Count && isInt[col])
                    {
                        double bi = xb[i];
                        double fi = Frac(bi);
                        if (fi > largestFrac + 1e-10 && fi > 1e-8 && fi < 1 - 1e-8)
                        {
                            largestFrac = fi;
                            chosenRow = i;
                            f = fi;
                            basicName = $"x{col + 1}";
                        }
                    }
                }

                // If no eligible fractional BASIC integer rows: try cover cut once, then stop
                if (chosenRow < 0)
                {
                    if (TryAddCoverCutIfUseful(sb, problem, isInt, xCurr, pfi, ref cuts))
                        continue;

                    sb.AppendLine("No eligible fractional BASIC integer row found. Stopping.");
                    break;
                }

                // α = frac(row_i(B^{-1})) via B^{-T} e_i
                var alpha = pfi.GetBinvRow(chosenRow);
                for (int j = 0; j < alpha.Length; j++) alpha[j] = Frac(alpha[j]);

                // Convert α·s ≥ f with s = b − A x  =>  (αA)x ≤ αb − f
                var A = problem.Constraints;
                var b = problem.RHS;

                double[] aCut = new double[n]; // αA
                for (int j = 0; j < n; j++)
                {
                    double sum = 0.0;
                    for (int r = 0; r < m; r++) sum += alpha[r] * A[r][j];
                    aCut[j] = ClipTiny(sum);
                }

                double rhs = 0.0; // αb − f
                for (int r = 0; r < m; r++) rhs += alpha[r] * b[r];
                rhs -= f;
                rhs = ClipTiny(rhs);

                // Skip zero-ish rows
                if (aCut.All(v => Math.Abs(v) < 1e-12))
                {
                    // Try a stronger knapsack cover cut before giving up
                    if (TryAddCoverCutIfUseful(sb, problem, isInt, xCurr, pfi, ref cuts))
                        continue;

                    sb.AppendLine("Candidate Gomory cut ~0; stopping.");
                    break;
                }

                // Must be violated at current x
                double lhs = 0.0; for (int j = 0; j < n; j++) lhs += aCut[j] * xCurr[j];
                if (lhs <= rhs + 1e-8)
                {
                    // Try a stronger knapsack cover cut before giving up
                    if (TryAddCoverCutIfUseful(sb, problem, isInt, xCurr, pfi, ref cuts))
                        continue;

                    sb.AppendLine($"Gomory candidate not violated (lhs={lhs:0.###} ≤ rhs={rhs:0.###}). Stopping.");
                    break;
                }

                // Duplicate guard
                if (IsDuplicate(A, b, aCut, rhs))
                {
                    // Try a stronger knapsack cover cut before giving up
                    if (TryAddCoverCutIfUseful(sb, problem, isInt, xCurr, pfi, ref cuts))
                        continue;

                    sb.AppendLine("New Gomory cut duplicates an existing row → stopping.");
                    break;
                }

                // Log and add
                cuts++;
                sb.AppendLine();
                sb.AppendLine($"--- Gomory Cut #{cuts} from row C{chosenRow + 1} (basic {basicName}, f={f:0.###}) ---");
                sb.AppendLine(RenderSlackCut(alpha, f));
                sb.AppendLine(RenderXCut(aCut, rhs));

                problem.Constraints.Add(aCut.ToList());
                problem.RHS.Add(rhs);
                pfi.AddRowAndWarmStart(aCut, rhs);

                sb.AppendLine($"---- PFI: After cut #{cuts} ----");
                var pfiLog2 = pfi.Optimize(out var xk, out var zk);
                sb.AppendLine(pfiLog2);

                if (AllFlaggedIntegersIntegral(xk, isInt))
                {
                    bestX = xk; bestZ = zk;
                    sb.AppendLine("All flagged integer variables integral. Done.");
                    return sb.ToString();
                }
            }

            // Final (may be fractional)
            {
                var pfiLog = pfi.Optimize(out var xend, out var zend);
                sb.AppendLine(pfiLog);
                bestX = xend; bestZ = zend;
                sb.AppendLine($"Stopped after {cuts} cut(s). Returning current solution (may be fractional).");
            }

            return sb.ToString();
        }

        // ---------- Knapsack cover cut helper ----------

        /// <summary>
        /// If model looks like 0–1 knapsack (nonnegative row, binary flags) and current relaxation violates
        /// a simple minimal cover inequality, add it and reoptimize (warm-start). Returns true if a cut was added.
        /// </summary>
        private static bool TryAddCoverCutIfUseful(
            StringBuilder sb,
            LinearProblem problem,
            List<bool> isInt,
            double[] xCurr,
            RevisedSimplexPFI pfi,
            ref int cutsCounter)
        {
            // Binary variables?
            if (isInt.Count == 0 || !isInt.Any(v => v)) return false;

            int n = problem.ObjectiveCoeffs.Count;
            int mCons = problem.Constraints.Count;
            if (mCons == 0) return false;

            // Find a ≤ row with nonnegative coefficients (knapsack-like)
            int row = -1;
            for (int i = 0; i < mCons; i++)
            {
                if (problem.Constraints[i].Count < n) continue;
                if (problem.Constraints[i].Any(c => c < -1e-12)) continue; // require nonnegativity
                row = i; break;
            }
            if (row < 0) return false;

            var w = problem.Constraints[row].Take(n).ToArray(); // weights
            double B = problem.RHS[row];

            // Greedy minimal cover: pick heaviest items until sum > B
            var idx = Enumerable.Range(0, n)
                                .Where(j => isInt[j]) // only integer vars
                                .OrderByDescending(j => w[j])
                                .ToList();

            var cover = new List<int>();
            double sum = 0.0;
            foreach (var j in idx)
            {
                cover.Add(j);
                sum += w[j];
                if (sum > B + 1e-12) break;
            }

            if (sum <= B + 1e-12 || cover.Count == 0) return false;

            // Build cover cut: sum_{j in C} x_j ≤ |C| - 1
            double[] aCut = new double[n];
            foreach (var j in cover) aCut[j] = 1.0;
            double rhs = cover.Count - 1;

            // Check violation at current x
            double lhs = cover.Sum(j => xCurr[j]);
            if (lhs <= rhs + 1e-8) return false; // not violated ⇒ skip

            // Duplicate guard
            if (IsDuplicate(problem.Constraints, problem.RHS, aCut, rhs)) return false;

            cutsCounter++;
            sb.AppendLine();
            sb.AppendLine($"--- Knapsack Cover Cut #{cutsCounter} (row C{row + 1}) ---");
            sb.AppendLine("Inequality: " +
                          string.Join(" + ", cover.Select(j => $"x{j + 1}")) +
                          $" ≤ {rhs}");

            problem.Constraints.Add(aCut.ToList());
            problem.RHS.Add(rhs);
            pfi.AddRowAndWarmStart(aCut, rhs);

            sb.AppendLine($"---- PFI: After cover cut #{cutsCounter} ----");
            var pfiLog = pfi.Optimize(out var xk, out var zk);
            sb.AppendLine(pfiLog);

            return true;
        }

        // ---------------- helpers (unchanged-ish) ----------------

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
                    if (j >= A[i].Count || Math.Abs(A[i][j] - aNew[j]) > tol) { same = false; break; }
                }
                if (same) return true;
            }
            return false;
        }

        private static double Frac(double v)
        {
            double f = v - Math.Floor(v);
            if (f < 0) f += 1.0;
            if (f < 1e-12) f = 0.0;
            if (f > 1.0 - 1e-12) f = 0.0;
            return f;
        }

        private static double ClipTiny(double x) => Math.Abs(x) < 1e-12 ? 0.0 : x;

        private static string RenderSlackCut(double[] alpha, double f)
        {
            var partsPos = new List<string>();
            var partsNeg = new List<string>();
            for (int j = 0; j < alpha.Length; j++)
            {
                double c = alpha[j];
                if (Math.Abs(c) < 1e-12) continue;
                partsPos.Add($"{c:0.###}s{j + 1}");
                partsNeg.Add($"{(-c):0.###}s{j + 1}");
            }
            string ge = partsPos.Count == 0 ? "0" : string.Join(" + ", partsPos);
            string le = partsNeg.Count == 0 ? "0" : string.Join(" + ", partsNeg);
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
