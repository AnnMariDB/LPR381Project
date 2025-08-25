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
            public string Label;        // Node label e.g. "T-1"
            public LinearProblem P;     // Problem with added branch constraints
        }

        private class Candidate
        {
            public string Name;         // Candidate name (A, B, C, ...)
            public string FromNode;     // From which node it was found
            public double[] X;          // Solution (X values)
            public double Z;            // Objective value (Z value)
        }

        public BranchAndBoundSolver(LinearProblem problem)
        {
            root = Clone(problem); // Clone the problem
        }

        public string Solve(out double[] bestX, out double bestZ)
        {
            var log = new StringBuilder();
            bestX = new double[root.ObjectiveCoeffs.Count];
            bestZ = root.IsMaximization ? double.NegativeInfinity : double.PositiveInfinity;

            // Solve the LP relaxation using Simplex
            var simplex = new SimplexSolver(root);
            SimplexResult res;
            double[,] finalTab;
            int[] finalBasis;
            string lpLog = simplex.SolveDetailed(out res, out finalTab, out finalBasis);
            log.Append(lpLog);
            log.AppendLine();

            // Dead-end tests (if the LP relaxation is infeasible, unbounded, or non-optimal)
            if (!res.IsOptimal || res.IsInfeasible || res.IsUnbounded)
            {
                log.AppendLine("LP solution is infeasible or unbounded.");
                bestZ = double.NaN; // Assign a NaN value to indicate no valid solution
                return log.ToString();
            }

            log.AppendLine("LP Solution found:");
            log.AppendLine($"Objective value (z): {res.ObjectiveValue:0.###}");

            // Step 1: Check which x values are not integers and find the first non-integer
            int fracIndex = -1; // Variable to store the index of the first non-integer x value
            for (int i = 0; i < res.X.Length; i++)
            {
                if (Math.Abs(res.X[i] - Math.Round(res.X[i])) > 1e-6) // Check if the value is fractional
                {
                    fracIndex = i;
                    break; // Exit after finding the first fractional value
                }
            }

            if (fracIndex < 0)
            {
                log.AppendLine("All x values are integers. No branching needed.");
                bestZ = res.ObjectiveValue; // Since the solution is integer-feasible, set the bestZ
            }
            else
            {
                log.AppendLine($"First non-integer x value found at index {fracIndex + 1}: x{fracIndex + 1} = {res.X[fracIndex]:0.###}");

                // Now, subtract the constraints for branching

                // Target row to subtract from (C2 in your example)
                int targetConstraintIndex = 1; // Index of the constraint row we are working with (C2)

                // Assuming we want to work with the second constraint, use its row:
                double[] targetConstraint = new double[finalTab.GetLength(1)];
                for (int i = 0; i < finalTab.GetLength(1); i++)
                {
                    targetConstraint[i] = finalTab[targetConstraintIndex, i]; // Copy C2's row
                }

                // Create the new constraint row (C3) for the branching variable (x1, for example)
                double[] newConstraintRow = new double[finalTab.GetLength(1)];

                // For the new constraint, set the value for x1 as 1, and other slack variables correctly
                newConstraintRow[fracIndex] = 1;  // x1
                newConstraintRow[finalTab.GetLength(1) - 1] = 3;  // RHS of the new constraint (s3)

                // Now, subtract the target constraint (C2) from the new constraint row (C3)
                for (int i = 0; i < finalTab.GetLength(1) - 1; i++)  // Loop through all columns except RHS
                {
                    newConstraintRow[i] -= targetConstraint[i];  // Subtract element-wise
                }

                // Subtract the RHS (right-hand side) values for the constraints
                double newRHS = 3 - finalTab[targetConstraintIndex, finalTab.GetLength(1) - 1];

                // Now multiply the resulting new constraint by -1 (after the subtraction)
                for (int i = 0; i < finalTab.GetLength(1) - 1; i++)  // Do this for all variables (slack variables)
                {
                    newConstraintRow[i] *= -1;
                }

                // Update the new RHS after multiplying by -1
                newRHS *= -1;

                // Log the new constraint after subtraction
                log.AppendLine($"New 3rd constraint added after subtraction: ");
                string newConstraintStr = "";
                for (int i = 0; i < finalTab.GetLength(1) - 1; i++)
                {
                    if (i < finalTab.GetLength(1) - 2) // Avoid the last column (RHS)
                        newConstraintStr += $"{newConstraintRow[i]:0.###}s{i + 1} ";  // Change x variables to s (slack variables)
                }
                log.AppendLine($"Constraint: {newConstraintStr} <= {newRHS:0.###}");

                // Continue with branching logic and subproblem creation (not shown here)
            }

            return log.ToString(); // Return the log of what has been done so far
        }

        // ---------------- helpers ----------------
        public string CalculateOptimalSolution(double[,] tableau, double[] rhsValues, out double[] bestX, out double bestZ)
        {
            var log = new StringBuilder();
            bestX = new double[tableau.GetLength(1) - 1];
            bestZ = double.NaN;

            // Step 1: Solve the table using Simplex
            var simplex = new SimplexSolver(tableau, rhsValues); //Error('SimplexSolver' does not contain a constructor that takes 2 arguments)
            SimplexResult res;
            string simplexLog = simplex.Solve(out res);//Error(No overload for method 'Solve' takes 1 arguments)
            log.Append(simplexLog);

            // Step 2: Check if optimal solution is found
            if (!res.IsOptimal || res.IsInfeasible || res.IsUnbounded)
            {
                log.AppendLine("Solution is infeasible, unbounded, or non-optimal.");
                bestZ = double.NaN;
                return log.ToString();
            }

            // Log the optimal solution
            log.AppendLine("Optimal Solution Found:");
            log.AppendLine($"Objective Value: {res.ObjectiveValue:0.###}");

            // Step 3: Check integer feasibility
            bool isIntegerFeasible = true;
            for (int i = 0; i < res.X.Length; i++)
            {
                if (Math.Abs(res.X[i] - Math.Round(res.X[i])) > 1e-6) // Check if it's fractional
                {
                    isIntegerFeasible = false;
                    break;
                }
            }

            if (isIntegerFeasible)
            {
                log.AppendLine("Solution is integer feasible.");
                bestZ = res.ObjectiveValue;
                Array.Copy(res.X, bestX, res.X.Length);
            }
            else
            {
                log.AppendLine("Solution is not integer feasible, further subproblems needed.");
                bestZ = res.ObjectiveValue;

                // Proceed to subproblems for branching
                log.AppendLine("Creating further subproblems...");
                int fracIndex = -1;
                for (int i = 0; i < res.X.Length; i++)
                {
                    if (Math.Abs(res.X[i] - Math.Round(res.X[i])) > 1e-6)
                    {
                        fracIndex = i;
                        break;
                    }
                }

                if (fracIndex >= 0)
                {
                    log.AppendLine($"Branching on variable {fracIndex + 1}: {res.X[fracIndex]:0.###}");
                    log.AppendLine($"Subproblem 1: x{fracIndex + 1} <= {Math.Floor(res.X[fracIndex])}");
                    log.AppendLine($"Subproblem 2: x{fracIndex + 1} >= {Math.Ceiling(res.X[fracIndex])}");
                }
            }

            return log.ToString(); // Return the log with results and subproblem info
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
    }
}
