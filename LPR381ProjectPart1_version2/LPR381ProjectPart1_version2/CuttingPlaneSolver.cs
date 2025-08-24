using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Very light Gomory-style Cutting Plane loop that keeps using your SimplexSolver.
    /// Notes:
    /// - Written for C# 7.3 (no nullable reference types).
    /// - Uses a simple (safe) cut for the first fractional binary variable:  x_i <= floor(x_i).
    ///   This compiles and integrates cleanly with your LinearProblem (<= handled by slack).
    /// - If you later expose tableau/basis from SimplexSolver, you can replace BuildCut(...)
    ///   with a real Gomory fractional cut from a fractional row.
    /// </summary>
    public class CuttingPlaneSolver
    {
        private readonly LinearProblem problem;
        private const int DefaultMaxCuts = 10;
        private const double Eps = 1e-6;

        public CuttingPlaneSolver(LinearProblem problem)
        {
            this.problem = problem;
        }

        /// <summary>
        /// Runs LP relaxation, then iteratively adds simple cuts until all flagged-binary vars are integer or maxCuts reached.
        /// Returns a human-readable log, and outputs the incumbent vector and objective value.
        /// </summary>
        public string Solve(out double[] bestX, out double bestZ)
        {
            var sb = new StringBuilder();
            bestX = new double[0];
            bestZ = double.NaN;

            sb.AppendLine("=== Cutting Plane Algorithm (Gomory-style, simplified) ===");

            // Work on a deep copy so the original LinearProblem isn't mutated from the UI.
            var work = Clone(problem);

            // 1) Relax integrality (we already use LP Simplex)
            var simplex = new SimplexSolver(work);
            string lpResult = simplex.Solve();
            sb.AppendLine("Initial LP Relaxation:");
            sb.AppendLine(lpResult);

            // Try to recover the last solution Simplex found using the helper we add below
            // If you have a richer SimplexResult already, you can swap this out to read it directly.
            double[] x = ExtractSolutionFromLog(lpResult, work.ObjectiveCoeffs.Count);
            double z = ExtractObjectiveFromLog(lpResult);

            if (x.Length == work.ObjectiveCoeffs.Count)
            {
                if (IsIntegerVector(x, work.IsBinary))
                {
                    bestX = x;
                    bestZ = z;
                    sb.AppendLine("LP relaxation is already integer-feasible for flagged binaries. Done.");
                    LogSolution(sb, bestX, bestZ);
                    return sb.ToString();
                }
            }

            int addedCuts = 0;
            while (addedCuts < DefaultMaxCuts)
            {
                // Re-solve current LP
                simplex = new SimplexSolver(work);
                lpResult = simplex.Solve();
                sb.AppendLine();
                sb.AppendLine($"--- Re-solve after {addedCuts} cut(s) ---");
                sb.AppendLine(lpResult);

                x = ExtractSolutionFromLog(lpResult, work.ObjectiveCoeffs.Count);
                z = ExtractObjectiveFromLog(lpResult);

                if (x.Length != work.ObjectiveCoeffs.Count)
                {
                    sb.AppendLine("Could not parse a complete primal solution from Simplex log; stopping.");
                    break;
                }

                // If solution is integer on binary vars -> we’re done
                if (IsIntegerVector(x, work.IsBinary))
                {
                    bestX = x;
                    bestZ = z;
                    sb.AppendLine("All flagged-binary variables are integer. Cutting Plane finished.");
                    LogSolution(sb, bestX, bestZ);
                    return sb.ToString();
                }

                // Build a simple cut: for first fractional binary xi, add xi <= floor(xi)
                List<double> coeffs;
                double rhs;
                if (!BuildSimpleBinaryCut(x, work.IsBinary, out coeffs, out rhs))
                {
                    // No fractional binary found (maybe only continuous vars remain fractional)
                    sb.AppendLine("No fractional flagged-binary variables found; stopping.");
                    break;
                }

                work.Constraints.Add(coeffs);
                work.RHS.Add(rhs);
                addedCuts++;

                sb.AppendLine($"Added cut #{addedCuts}:");
                sb.AppendLine(RenderCut(coeffs, rhs));
            }

            // One final solve and return what we have
            simplex = new SimplexSolver(work);
            lpResult = simplex.Solve();
            sb.AppendLine();
            sb.AppendLine($"Reached max cuts ({DefaultMaxCuts}) or stopping condition. Returning current solution.");
            sb.AppendLine(lpResult);

            x = ExtractSolutionFromLog(lpResult, work.ObjectiveCoeffs.Count);
            z = ExtractObjectiveFromLog(lpResult);
            if (x.Length == work.ObjectiveCoeffs.Count)
            {
                bestX = x;
                bestZ = z;
                LogSolution(sb, bestX, bestZ);
            }

            return sb.ToString();
        }

        // ---------------- helpers ----------------

        private static LinearProblem Clone(LinearProblem p)
        {
            var copy = new LinearProblem
            {
                IsMaximization = p.IsMaximization
            };
            copy.ObjectiveCoeffs.AddRange(p.ObjectiveCoeffs);
            foreach (var row in p.Constraints)
                copy.Constraints.Add(new List<double>(row));
            copy.RHS.AddRange(p.RHS);
            copy.IsBinary.AddRange(p.IsBinary);
            return copy;
        }

        private static bool IsIntegerVector(double[] x, List<bool> isBinaryFlags)
        {
            if (x == null || isBinaryFlags == null) return false;
            int n = Math.Min(x.Length, isBinaryFlags.Count);
            for (int i = 0; i < n; i++)
            {
                if (isBinaryFlags[i])
                {
                    if (Math.Abs(x[i] - Math.Round(x[i])) > Eps)
                        return false;
                    // if strictly binary, you can also enforce 0/1 here if you like:
                    // if (Math.Round(x[i]) != 0 && Math.Round(x[i]) != 1) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Very simple cut: pick first fractional flagged-binary xi and add  xi <= floor(xi).
        /// Returns false if none found.
        /// </summary>
        private static bool BuildSimpleBinaryCut(double[] x, List<bool> isBinaryFlags, out List<double> coeffs, out double rhs)
        {
            coeffs = new List<double>();
            rhs = 0;

            int n = isBinaryFlags == null ? 0 : isBinaryFlags.Count;
            if (x == null || x.Length == 0 || n == 0) return false;

            int m = Math.Max(n, x.Length); // constraint vector must match number of decision vars
            for (int i = 0; i < n; i++)
            {
                if (!isBinaryFlags[i]) continue;

                double frac = x[i] - Math.Floor(x[i]);
                if (frac > Eps && (1.0 - frac) > Eps)
                {
                    // create xi <= floor(xi)
                    coeffs = Enumerable.Repeat(0.0, m).ToList();
                    coeffs[i] = 1.0;
                    rhs = Math.Floor(x[i]);
                    return true;
                }
            }

            return false;
        }

        private static string RenderCut(List<double> coeffs, double rhs)
        {
            var parts = new List<string>();
            for (int j = 0; j < coeffs.Count; j++)
            {
                if (Math.Abs(coeffs[j]) < 1e-12) continue;
                parts.Add($"{coeffs[j]:0.###}x{j + 1}");
            }
            var left = parts.Count == 0 ? "0" : string.Join(" + ", parts);
            return $"    {left} <= {rhs:0.###}";
        }

        private static void LogSolution(StringBuilder sb, double[] x, double z)
        {
            sb.AppendLine();
            sb.AppendLine("Incumbent solution:");
            for (int i = 0; i < x.Length; i++)
                sb.AppendLine($"x{i + 1} = {x[i]:0.###}");
            sb.AppendLine($"z = {z:0.###}");
        }

        // --- Minimal parsers so we can read your current Simplex output without changing SimplexSolver ---

        private static double[] ExtractSolutionFromLog(string log, int n)
        {
            // Looks for lines like "x1 = 1.234"
            var x = new double[n];
            if (string.IsNullOrEmpty(log)) return new double[0];

            var lines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int filled = 0;
            foreach (var line in lines)
            {
                // crude parse
                // example: "x3 = 0.75"
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("x")) continue;
                var eq = trimmed.IndexOf('=');
                if (eq < 0) continue;

                var name = trimmed.Substring(0, Math.Max(0, trimmed.IndexOf(' '))).Trim();
                if (!name.StartsWith("x")) continue;

                int index;
                if (!int.TryParse(name.Substring(1), out index)) continue;
                if (index < 1 || index > n) continue;

                double val;
                if (!double.TryParse(trimmed.Substring(eq + 1).Trim(), out val)) continue;

                x[index - 1] = val;
                filled++;
            }

            return filled == n ? x : new double[0];
        }

        private static double ExtractObjectiveFromLog(string log)
        {
            // Looks for a line like "Optimal objective value: <number>"
            if (string.IsNullOrEmpty(log)) return double.NaN;

            var lines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var t = line.Trim();
                var key = "Optimal objective value:";
                if (t.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    var rest = t.Substring(key.Length).Trim();
                    double val;
                    if (double.TryParse(rest, out val))
                        return val;
                }
            }
            return double.NaN;
        }
    }
}
