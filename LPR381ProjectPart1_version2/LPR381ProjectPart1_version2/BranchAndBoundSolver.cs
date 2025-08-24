using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Branch & Bound over LP relaxations solved by SimplexSolver.
    /// Produces a log of each explored node and a clear candidate summary (A, B, C, ...) and Best.
    /// </summary>
    public class BranchAndBoundSolver
    {
        private readonly LinearProblem root;

        private class Node
        {
            public string Label;        // e.g. "T-1", "T-2", ...
            public LinearProblem P;     // problem with added branch constraints
        }

        private class Candidate
        {
            public string Name;     // A, B, C, ...
            public string FromNode; // node label where integer feasible was found
            public double[] X;
            public double Z;
        }

        public BranchAndBoundSolver(LinearProblem problem)
        {
            root = Clone(problem);
        }

        public string Solve(out double[] bestX, out double bestZ)
        {
            var log = new StringBuilder();
            var stack = new Stack<Node>();
            var candidates = new List<Candidate>();
            int nodeCounter = 0;
            int candidateCounter = 0;

            bestX = new double[root.ObjectiveCoeffs.Count];
            bestZ = root.IsMaximization ? double.NegativeInfinity : double.PositiveInfinity;

            // Start with the root node (LP relaxation)
            stack.Push(new Node
            {
                Label = NextNodeLabel(++nodeCounter),
                P = Clone(root)
            });

            log.AppendLine("=== Branch & Bound (Simplex over LP relaxations) ===");
            log.AppendLine();

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                log.AppendLine($"--- Solving node {node.Label} ---");

                // Solve LP relaxation at this node
                var simplex = new SimplexSolver(node.P);
                SimplexResult res;
                double[,] finalTab;
                int[] finalBasis;
                string lpLog = simplex.SolveDetailed(out res, out finalTab, out finalBasis);
                log.Append(lpLog);
                log.AppendLine();

                // Dead-end tests
                if (!res.IsOptimal || res.IsInfeasible || res.IsUnbounded)
                {
                    log.AppendLine($"Node {node.Label} pruned (infeasible/unbounded/non-optimal).");
                    log.AppendLine();
                    continue;
                }

                // Bounding (simple)
                bool pruneByBound = false;
                if (root.IsMaximization)
                {
                    if (res.ObjectiveValue <= bestZ - 1e-9) pruneByBound = true;
                }
                else
                {
                    if (res.ObjectiveValue >= bestZ + 1e-9) pruneByBound = true;
                }
                if (pruneByBound)
                {
                    log.AppendLine($"Node {node.Label} pruned by bound (LP z = {res.ObjectiveValue:0.###}, incumbent z = {bestZ:0.###}).");
                    log.AppendLine();
                    continue;
                }

                // Integrality check (only those flagged in IsBinary are enforced as integers)
                bool allInt = IsIntegerSolution(res.X, node.P.IsBinary);
                if (allInt)
                {
                    // Record candidate
                    var cand = new Candidate
                    {
                        Name = ((char)('A' + candidateCounter)).ToString(),
                        FromNode = node.Label,
                        X = res.X.ToArray(),
                        Z = res.ObjectiveValue
                    };
                    candidates.Add(cand);
                    candidateCounter++;

                    // Update incumbent
                    bool better =
                        (root.IsMaximization && cand.Z > bestZ + 1e-9) ||
                        (!root.IsMaximization && cand.Z < bestZ - 1e-9);

                    if (better)
                    {
                        Array.Copy(cand.X, bestX, cand.X.Length);
                        bestZ = cand.Z;
                    }

                    log.AppendLine($"** Candidate {cand.Name} (from {cand.FromNode}) **");
                    for (int i = 0; i < cand.X.Length; i++)
                        log.AppendLine($"x{i + 1} = {cand.X[i]:0.###}");
                    log.AppendLine($"z = {cand.Z:0.###}");
                    log.AppendLine();
                    continue;
                }

                // Not integer yet -> branch
                int fracIndex = BranchingRules.PickBranchVariable(res.X, node.P.IsBinary);
                if (fracIndex < 0)
                {
                    // Safety: nothing to branch on (should not happen if allInt was false)
                    log.AppendLine($"Node {node.Label}: no fractional integral variable found; skipping.");
                    log.AppendLine();
                    continue;
                }

                double xi = res.X[fracIndex];
                double low = Math.Floor(xi);
                double high = Math.Ceiling(xi);

                log.AppendLine($"Branching on x{fracIndex + 1} = {xi:0.###}  -->  (1) x{fracIndex + 1} ≤ {low}  and  (2) x{fracIndex + 1} ≥ {high}");
                log.AppendLine();

                // Create child problems:
                // left child: x_i ≤ floor(xi)
                var left = new Node
                {
                    Label = NextNodeLabel(++nodeCounter),
                    P = Clone(node.P)
                };
                AddLeqConstraint(left.P, fracIndex, low);

                // right child: x_i ≥ ceil(xi)  ->  -x_i ≤ -ceil(xi)
                var right = new Node
                {
                    Label = NextNodeLabel(++nodeCounter),
                    P = Clone(node.P)
                };
                AddGeqConstraint(right.P, fracIndex, high);

                // DFS: push right first so left is processed next
                stack.Push(right);
                stack.Push(left);
            }

            // ---- Candidate summary (like your slides) ----
            if (candidates.Count == 0)
            {
                log.AppendLine("No integer-feasible candidates were found.");
                return log.ToString();
            }

            log.AppendLine("===== Candidate Summary =====");
            Candidate bestCand = candidates[0];
            foreach (var c in candidates)
            {
                log.AppendLine($"Candidate {c.Name}:  z = {c.Z:0.###}");
                for (int i = 0; i < c.X.Length; i++)
                    log.AppendLine($"  x{i + 1} = {c.X[i]:0.###}");
                log.AppendLine();

                if (root.IsMaximization)
                {
                    if (c.Z > bestCand.Z + 1e-9) bestCand = c;
                }
                else
                {
                    if (c.Z < bestCand.Z - 1e-9) bestCand = c;
                }
            }

            // Ensure bestX/bestZ are consistent with bestCand
            Array.Copy(bestCand.X, bestX, bestCand.X.Length);
            bestZ = bestCand.Z;

            log.AppendLine($"Best: Candidate {bestCand.Name}  (z = {bestCand.Z:0.###})");
            for (int i = 0; i < bestCand.X.Length; i++)
                log.AppendLine($"  x{i + 1} = {bestCand.X[i]:0.###}");

            return log.ToString();
        }

        // ---------------- helpers ----------------

        private static bool IsIntegerSolution(double[] x, List<bool> isIntegral)
        {
            int n = Math.Min(x.Length, isIntegral.Count);
            for (int i = 0; i < n; i++)
            {
                if (!isIntegral[i]) continue;
                if (Math.Abs(x[i] - Math.Round(x[i])) > 1e-6) return false;
            }
            return true;
        }

        private static string NextNodeLabel(int counter)
        {
            // Matches the style in your slides (T-1, T-2, ...)
            return "T-" + counter;
        }

        private static LinearProblem Clone(LinearProblem p)
        {
            var q = new LinearProblem
            {
                IsMaximization = p.IsMaximization,
                ObjectiveCoeffs = p.ObjectiveCoeffs.ToList(),
                RHS = p.RHS.ToList(),
                IsBinary = p.IsBinary.ToList()
            };
            foreach (var row in p.Constraints)
                q.Constraints.Add(row.ToList());
            return q;
        }

        private static void AddLeqConstraint(LinearProblem p, int varIndex, double rhs)
        {
            var row = new List<double>(new double[p.ObjectiveCoeffs.Count]);
            row[varIndex] = 1.0;
            p.Constraints.Add(row);
            p.RHS.Add(rhs);
        }

        private static void AddGeqConstraint(LinearProblem p, int varIndex, double rhs)
        {
            // >= rhs  ==>  -x_i <= -rhs  (Simplex uses <= with slack)
            var row = new List<double>(new double[p.ObjectiveCoeffs.Count]);
            row[varIndex] = -1.0;
            p.Constraints.Add(row);
            p.RHS.Add(-rhs);
        }
    }
}
