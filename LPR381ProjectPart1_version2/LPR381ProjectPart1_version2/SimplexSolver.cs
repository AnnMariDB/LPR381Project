using System;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Primal simplex for <= constraints with slacks. Works for max & min.
    /// Produces a SimplexResult for Branch & Bound and a text log for the UI.
    /// </summary>
    public class SimplexSolver
    {
        private readonly LinearProblem problem;

        public SimplexSolver(LinearProblem problem)
        {
            this.problem = problem;
        }

        // --- Legacy entry used by the Form (kept for backward compatibility) ---
        public string Solve()
        {
            SolveDetailed(out var res, out _, out _);
            return res.Log;
        }

        /// <summary>
        /// Main entry used by Branch & Bound. Returns a rich result and also
        /// the final tableau + basis (so other components can render/inspect them).
        /// </summary>
        public string SolveDetailed(out SimplexResult res, out double[,] finalTableau, out int[] finalBasis)
        {
            res = new SimplexResult();
            var sb = new StringBuilder();

            sb.AppendLine("=== Primal Simplex Method ===");
            sb.AppendLine("Original problem:");
            sb.AppendLine(problem.ToCanonicalForm());

            int numVars = problem.ObjectiveCoeffs.Count;
            int numConstraints = problem.Constraints.Count;

            // Build initial tableau: rows = constraints + Z row, cols = vars + slacks + RHS
            double[,] tableau = new double[numConstraints + 1, numVars + numConstraints + 1];
            int[] basis = new int[numConstraints];

            // Constraints + slacks
            for (int i = 0; i < numConstraints; i++)
            {
                for (int j = 0; j < numVars; j++)
                    tableau[i, j] = problem.Constraints[i][j];

                tableau[i, numVars + i] = 1.0;                                     // slack
                tableau[i, tableau.GetLength(1) - 1] = problem.RHS[i];             // RHS
                basis[i] = numVars + i;
            }

            // Objective row (Z row). We store the row as: coeffs then RHS.
            for (int j = 0; j < numVars; j++)
                tableau[numConstraints, j] = problem.IsMaximization
                    ? -problem.ObjectiveCoeffs[j]
                    : problem.ObjectiveCoeffs[j];

            sb.AppendLine();
            sb.AppendLine("----------------------------------------------------");
            sb.AppendLine("Initial Tableau:");
            sb.AppendLine(RenderTableau(tableau, basis, "t0"));

            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int iteration = 0;

            // Simplex loop
            while (true)
            {
                iteration++;

                // 1) Entering column (most negative reduced cost on Z row)
                int zRow = rows - 1;
                int pivotCol = -1;
                double mostNegative = 0.0;

                for (int j = 0; j < cols - 1; j++) // exclude RHS
                {
                    double v = tableau[zRow, j];
                    if (v < mostNegative)
                    {
                        mostNegative = v;
                        pivotCol = j;
                    }
                }

                // If no negative reduced cost -> optimal
                if (pivotCol == -1)
                {
                    res.IsOptimal = true;
                    sb.AppendLine("Optimal solution reached.");
                    break;
                }

                // 2) Leaving row (minimum ratio test with positive pivot column entries)
                int pivotRow = -1;
                double minRatio = double.MaxValue;

                for (int i = 0; i < rows - 1; i++)
                {
                    double a = tableau[i, pivotCol];
                    if (a > 1e-12) // strictly positive to avoid division by ~0
                    {
                        double ratio = tableau[i, cols - 1] / a;
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    // Unbounded in the direction of pivotCol
                    res.IsUnbounded = true;
                    sb.AppendLine("Problem is unbounded!");
                    finalTableau = tableau;
                    finalBasis = basis.ToArray();
                    res.FinalTableau = finalTableau;
                    res.FinalBasis = finalBasis;
                    res.Log = sb.ToString();
                    return res.Log;
                }

                sb.AppendLine();
                sb.AppendLine("----------------------------------------------------");
                sb.AppendLine($"Iteration {iteration}: Pivot column = {GetVariableName(pivotCol, numVars)}, " +
                              $"Pivot row = C{pivotRow + 1}, Min ratio = {minRatio:0.###}");

                Pivot(tableau, pivotRow, pivotCol);
                basis[pivotRow] = pivotCol;

                sb.AppendLine(RenderTableau(tableau, basis, $"t{iteration}"));
            }

            // If any RHS is negative after finishing, mark infeasible (rare here with standard form)
            for (int i = 0; i < rows - 1; i++)
            {
                if (tableau[i, cols - 1] < -1e-9)
                {
                    res.IsInfeasible = true;
                    break;
                }
            }

            // Extract solution for original variables + slacks from basis/RHS
            double[] solution = new double[numVars + numConstraints];
            for (int i = 0; i < rows - 1; i++)
                solution[basis[i]] = tableau[i, cols - 1];

            // Sign of objective:
            // Our Z row RHS is the current objective value of the *row expression* we kept.
            // For max problems we negated c in the Z row, so the RHS already equals optimal z.
            double z = tableau[rows - 1, cols - 1];

            // Fill result
            res.X = solution;
            res.ObjectiveValue = z;
            res.FinalTableau = tableau;
            res.FinalBasis = basis.ToArray();

            // Append a short summary to the log
            sb.AppendLine();
            sb.AppendLine("Optimal variable values:");
            for (int i = 0; i < numVars; i++)
                sb.AppendLine($"x{i + 1} = {solution[i]:0.###}");
            sb.AppendLine($"Optimal objective value: {z:0.###}");

            res.Log = sb.ToString();

            finalTableau = tableau;
            finalBasis = res.FinalBasis;

            return res.Log;
        }

        // ----------------- helpers -----------------

        private static void Pivot(double[,] tab, int pivotRow, int pivotCol)
        {
            int rows = tab.GetLength(0);
            int cols = tab.GetLength(1);

            double piv = tab[pivotRow, pivotCol];
            if (Math.Abs(piv) < 1e-12) return;

            // Normalize pivot row
            for (int j = 0; j < cols; j++)
                tab[pivotRow, j] /= piv;

            // Eliminate pivot column from other rows
            for (int i = 0; i < rows; i++)
            {
                if (i == pivotRow) continue;
                double factor = tab[i, pivotCol];
                if (Math.Abs(factor) < 1e-12) continue;

                for (int j = 0; j < cols; j++)
                    tab[i, j] -= factor * tab[pivotRow, j];
            }
        }

        private static string GetVariableName(int colIndex, int numOriginalVars)
        {
            return colIndex < numOriginalVars
                ? $"x{colIndex + 1}"
                : $"s{colIndex - numOriginalVars + 1}";
        }

        /// <summary>
        /// Public renderer so Branch & Bound can show tableaus.
        /// </summary>
        public static string RenderTableau(double[,] tableau, int[] basis, string iterationLabel)
        {
            var sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int numSlack = rows - 1;
            int numVars = cols - numSlack - 1; // (cols = vars + slacks + RHS)

            sb.Append(iterationLabel).Append('\t');
            for (int j = 0; j < numVars; j++) sb.Append($"x{j + 1}\t");
            for (int j = 0; j < numSlack; j++) sb.Append($"s{j + 1}\t");
            sb.AppendLine("RHS");

            // Z row
            sb.Append("Z\t");
            for (int j = 0; j < cols; j++)
                sb.Append($"{Math.Round(tableau[rows - 1, j], 3)}\t");
            sb.AppendLine();

            // Constraint rows
            for (int i = 0; i < rows - 1; i++)
            {
                sb.Append($"C{i + 1}\t");
                for (int j = 0; j < cols; j++)
                    sb.Append($"{Math.Round(tableau[i, j], 3)}\t");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
