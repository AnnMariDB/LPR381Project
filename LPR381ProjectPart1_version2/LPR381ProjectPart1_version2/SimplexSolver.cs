using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381ProjectPart1_version2
{
    public class SimplexSolver
    {
        private readonly LinearProblem problem;

        public SimplexSolver(LinearProblem problem)
        {
            this.problem = problem;
        }

        public string Solve()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("=== Primal Simplex Method ===");
            sb.AppendLine("Original problem:");
            sb.AppendLine(problem.ToCanonicalForm());

            int numVars = problem.ObjectiveCoeffs.Count;
            int numConstraints = problem.Constraints.Count;

            //build initial tabel
            double[,] tableau = new double[numConstraints + 1, numVars + numConstraints + 1];
            int[] basis = new int[numConstraints]; // basic variable indices

            //fill constraints with slack variables
            for (int i = 0; i < numConstraints; i++)
            {
                for (int j = 0; j < numVars; j++)
                    tableau[i, j] = problem.Constraints[i][j];

                tableau[i, numVars + i] = 1.0; //slack variable
                tableau[i, tableau.GetLength(1) - 1] = problem.RHS[i];

                basis[i] = numVars + i;
            }

            //fill objective row
            for (int j = 0; j < numVars; j++)
                tableau[numConstraints, j] = problem.IsMaximization ? -problem.ObjectiveCoeffs[j] : problem.ObjectiveCoeffs[j];

            sb.AppendLine("\nInitial Tableau:");
            sb.AppendLine(FormatTableau(tableau, basis));

            int iteration = 0;

            while (true)
            {
                iteration++;
                int pivotCol = -1;
                double mostNegative = 0;

                //determine entering variable
                for (int j = 0; j < tableau.GetLength(1) - 1; j++)
                {
                    if (tableau[numConstraints, j] < mostNegative)
                    {
                        mostNegative = tableau[numConstraints, j];
                        pivotCol = j;
                    }
                }

                if (pivotCol == -1)
                {
                    sb.AppendLine("Optimal solution reached.\n");
                    break; //all coefficients non-negative
                }

                //determine leaving variable
                int pivotRow = -1;
                double minRatio = double.MaxValue;
                for (int i = 0; i < numConstraints; i++)
                {
                    double coeff = tableau[i, pivotCol];
                    if (coeff > 0)
                    {
                        double ratio = tableau[i, tableau.GetLength(1) - 1] / coeff;
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    sb.AppendLine("Problem is unbounded!");
                    return sb.ToString();
                }

                sb.AppendLine($"\nIteration {iteration}: Pivot column = x{pivotCol + 1}, Pivot row = C{pivotRow + 1}, Min ratio = {minRatio}");

                //do pivot
                Pivot(tableau, pivotRow, pivotCol);
                basis[pivotRow] = pivotCol;

                sb.AppendLine(FormatTableau(tableau, basis));
            }

            //extract solution
            double[] solution = new double[numVars + numConstraints];
            for (int i = 0; i < numConstraints; i++)
                solution[basis[i]] = tableau[i, tableau.GetLength(1) - 1];

            sb.AppendLine("Optimal variable values:");
            for (int i = 0; i < numVars; i++)
                sb.AppendLine($"x{i + 1} = {solution[i]}");

            //optimal objective value ---
            double optimalValue = tableau[numConstraints, tableau.GetLength(1) - 1];

            sb.AppendLine($"Optimal objective value: {optimalValue}");

            return sb.ToString();
        }

        private void Pivot(double[,] tableau, int pivotRow, int pivotCol)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            double pivotValue = tableau[pivotRow, pivotCol];

            //normalize pivot row
            for (int j = 0; j < cols; j++)
                tableau[pivotRow, j] /= pivotValue;

            //zero out other rows
            for (int i = 0; i < rows; i++)
            {
                if (i == pivotRow) continue;
                double factor = tableau[i, pivotCol];
                for (int j = 0; j < cols; j++)
                    tableau[i, j] -= factor * tableau[pivotRow, j];
            }
        }

        private string FormatTableau(double[,] tableau, int[] basis)
        {
            StringBuilder sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            //header
            sb.Append("Basic\t");
            for (int j = 0; j < cols - 1; j++)
                sb.Append($"x{j + 1}\t");
            sb.AppendLine("RHS");

            for (int i = 0; i < rows; i++)
            {
                if (i < rows - 1)
                    sb.Append($"C{i + 1}\t"); // pivot row label as C1, C2, ...
                else
                    sb.Append("Z\t");

                for (int j = 0; j < cols; j++)
                {
                    double displayValue = tableau[i, j];
                    // show positive Z row for maximization
                    if (i == rows - 1 && tableau[rows - 1, j] < 0 && problem.IsMaximization)
                        displayValue = -displayValue;

                    sb.Append($"{Math.Round(displayValue, 3)}\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
