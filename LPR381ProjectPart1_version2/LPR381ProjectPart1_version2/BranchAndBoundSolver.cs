using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    public class BranchAndBoundSolver
    {
        private readonly LinearProblem root;

        private class Node
        {
            public LinearProblem P;          // Problem at this node (with branch constraints added)
            public List<int> Path;           // e.g., [1,2,1] -> "sub_problem 1.2.1"
            public string FromConstraint;    // Human text of the branching constraint added at this node (for log)
        }

        private class Candidate
        {
            public string Name;     // sub_problem label where it was found
            public double[] X;      // original vars only
            public double Z;        // objective value
        }

        public BranchAndBoundSolver(LinearProblem problem)
        {
            // Work on a deep copy to avoid mutating the UI copy
            root = problem.Clone();
        }

        public string Solve(out double[] bestX, out double bestZ)
        {
            var sb = new StringBuilder();
            var candidates = new List<Candidate>();

            int n = root.ObjectiveCoeffs.Count;

            // Build an "isIntegral" list aligned to original variables
            var isIntegral = BuildIntegralMask(root);

            // Incumbent init (depends on max/min)
            bool isMax = root.IsMaximization;
            bestX = new double[n];
            bestZ = isMax ? double.NegativeInfinity : double.PositiveInfinity;
            bool hasIncumbent = false;

            // DFS stack
            var stack = new Stack<Node>();
            stack.Push(new Node
            {
                P = root.Clone(),
                Path = new List<int> { 1 },
                FromConstraint = "root"
            });

            sb.AppendLine("=== Branch & Bound (Simplex) ===");

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                string label = LabelFromPath(node.Path);

                sb.AppendLine();
                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"{label}  (added: {node.FromConstraint})");

                // Solve this node’s LP relaxation
                var simplex = new SimplexSolver(node.P);
                SimplexResult res;
                double[,] finalTab;
                int[] finalBasis;
                simplex.SolveDetailed(out res, out finalTab, out finalBasis);

                // Show the node’s final optimal tableau (or last tableau on early exit)
                if (res.FinalTableau != null && res.FinalTableau.Length > 0 && res.FinalBasis != null && res.FinalBasis.Length > 0)
                {
                    sb.AppendLine("Final tableau for this sub-problem:");
                    sb.AppendLine(SimplexSolver.RenderTableau(res.FinalTableau, res.FinalBasis, label));
                }

                // Prune: infeasible or unbounded or not optimal
                if (res.IsInfeasible || res.IsUnbounded || !res.IsOptimal)
                {
                    if (res.IsInfeasible) sb.AppendLine("→ Pruned (infeasible).");
                    else if (res.IsUnbounded) sb.AppendLine("→ Pruned (unbounded).");
                    else sb.AppendLine("→ Pruned (no optimal solution at this node).");
                    continue;
                }

                // Extract original vars only (your Simplex returns x + slacks)
                var xFull = res.X ?? Array.Empty<double>();
                var x = new double[n];
                for (int i = 0; i < n && i < xFull.Length; i++) x[i] = xFull[i];
                double z = res.ObjectiveValue;

                // If integer-feasible on flagged vars → record candidate + update incumbent
                if (IsIntegerFeasible(x, isIntegral))
                {
                    sb.AppendLine("→ Integer feasible on flagged variables.");
                    candidates.Add(new Candidate { Name = label, X = x.ToArray(), Z = z });

                    if (!hasIncumbent ||
                        (isMax && z > bestZ + 1e-12) ||
                        (!isMax && z < bestZ - 1e-12))
                    {
                        hasIncumbent = true;
                        bestZ = z;
                        Array.Copy(x, bestX, n);
                        sb.AppendLine($"→ New incumbent: z = {bestZ:0.###}");
                    }
                    continue;
                }

                // Otherwise branch
                int k = BranchingRules.PickBranchVariable(x, isIntegral);
                if (k < 0)
                {
                    // No fractional on flagged vars, treat as candidate (covers all-continuous models too)
                    sb.AppendLine("→ No fractional flagged variable found; accepting as candidate.");
                    candidates.Add(new Candidate { Name = label, X = x.ToArray(), Z = z });
                    if (!hasIncumbent ||
                        (isMax && z > bestZ + 1e-12) ||
                        (!isMax && z < bestZ - 1e-12))
                    {
                        hasIncumbent = true;
                        bestZ = z;
                        Array.Copy(x, bestX, n);
                        sb.AppendLine($"→ New incumbent: z = {bestZ:0.###}");
                    }
                    continue;
                }

                double xi = x[k];
                double floor = Math.Floor(xi);
                double ceil = Math.Ceiling(xi);

                sb.AppendLine($"Branching on x{k + 1} = {xi:0.###} → "
                            + $"left: x{k + 1} ≤ {floor}, right: x{k + 1} ≥ {ceil}");

                // Left child: x_k <= floor
                var left = node.P.Clone();
                var aLeft = new double[n];
                aLeft[k] = 1.0;
                left.AddLeConstraint(aLeft, floor);

                // Right child: x_k >= ceil
                var right = node.P.Clone();
                var aRight = new double[n];
                aRight[k] = 1.0;
                right.AddGeConstraint(aRight, ceil);

                // Order of push controls DFS order. Push right first so left is popped next.
                var rightNode = new Node
                {
                    P = right,
                    Path = node.Path.Concat(new[] { 2 }).ToList(),
                    FromConstraint = $"x{k + 1} ≥ {ceil}"
                };
                var leftNode = new Node
                {
                    P = left,
                    Path = node.Path.Concat(new[] { 1 }).ToList(),
                    FromConstraint = $"x{k + 1} ≤ {floor}"
                };

                stack.Push(rightNode);
                stack.Push(leftNode);
            }

            // Summary
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine("Candidates found (integer-feasible):");
            if (candidates.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                // Sort for a tidy list (Max: desc z; Min: asc z)
                var ordered = root.IsMaximization
                    ? candidates.OrderByDescending(c => c.Z)
                    : candidates.OrderBy(c => c.Z);

                int idx = 1;
                foreach (var c in ordered)
                {
                    sb.AppendLine($"  {idx}. {c.Name}: z = {c.Z:0.###}, " +
                                  $"x = [{string.Join(", ", c.X.Select(v => v.ToString("0.###")))}]");
                    idx++;
                }
            }

            if (hasIncumbent)
            {
                sb.AppendLine();
                sb.AppendLine("Best incumbent:");
                for (int i = 0; i < n; i++)
                    sb.AppendLine($"x{i + 1} = {bestX[i]:0.###}");
                sb.AppendLine($"z = {bestZ:0.###}");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("No integer-feasible solution found.");
                bestZ = double.NaN;
            }

            return sb.ToString();
        }

        // ---------------- helpers ----------------

        private static string LabelFromPath(List<int> path)
        {
            return "sub_problem " + string.Join(".", path);
        }

        private static List<bool> BuildIntegralMask(LinearProblem p)
        {
            int n = p.ObjectiveCoeffs.Count;
            var mask = new List<bool>(Enumerable.Repeat(false, n));

            // If IsBinary is empty → assume all original variables are integral (common in IPs)
            if (p.IsBinary == null || p.IsBinary.Count == 0)
            {
                for (int i = 0; i < n; i++) mask[i] = true;
                return mask;
            }

            // If provided, pad/trim to n and use it directly
            int m = Math.Min(n, p.IsBinary.Count);
            for (int i = 0; i < m; i++)
                mask[i] = p.IsBinary[i];

            // Any missing flags beyond provided length stay false (continuous)
            return mask;
        }

        private static bool IsIntegerFeasible(double[] x, List<bool> isIntegral, double eps = 1e-6)
        {
            int n = Math.Min(x.Length, isIntegral.Count);
            for (int i = 0; i < n; i++)
            {
                if (!isIntegral[i]) continue;
                if (Math.Abs(x[i] - Math.Round(x[i])) > eps) return false;
            }
            return true;
        }
    }
}
