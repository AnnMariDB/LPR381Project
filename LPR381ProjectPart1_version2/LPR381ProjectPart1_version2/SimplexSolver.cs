using System;
using System.Text;

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

            double[,] tableau = new double[numConstraints + 1, numVars + numConstraints + 1];
            int[] basis = new int[numConstraints];

            //fill constraints and add slack variables
            for (int i = 0; i < numConstraints; i++)
            {
                for (int j = 0; j < numVars; j++)
                    tableau[i, j] = problem.Constraints[i][j];

                tableau[i, numVars + i] = 1.0; // slack variable
                tableau[i, tableau.GetLength(1) - 1] = problem.RHS[i];
                basis[i] = numVars + i;
            }

            //fill objective row (Z row)
            for (int j = 0; j < numVars; j++)
                tableau[numConstraints, j] = problem.IsMaximization ? -problem.ObjectiveCoeffs[j] : problem.ObjectiveCoeffs[j];

            sb.AppendLine("\n----------------------------------------------------");
            sb.AppendLine("Initial Tableau:");
            sb.AppendLine(FormatTableau(tableau, basis, "ti"));

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
                    break;
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

                string iterLabel = iteration == tableau.GetLength(0) - 1 ? "t*" : "t" + iteration;
                sb.AppendLine("\n----------------------------------------------------");
                sb.AppendLine($"Iteration {iteration}: Pivot column = {GetVariableName(pivotCol, numVars)}," +
                              $" Pivot row = C{pivotRow + 1}, Min ratio = {minRatio}");

                Pivot(tableau, pivotRow, pivotCol);
                basis[pivotRow] = pivotCol;

                sb.AppendLine(FormatTableau(tableau, basis, iterLabel));
            }

            //extract solution
            double[] solution = new double[numVars + numConstraints];
            for (int i = 0; i < numConstraints; i++)
                solution[basis[i]] = tableau[i, tableau.GetLength(1) - 1];

            sb.AppendLine("Optimal variable values:");
            for (int i = 0; i < numVars; i++)
                sb.AppendLine($"x{i + 1} = {solution[i]}");

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

        private string FormatTableau(double[,] tableau, int[] basis, string iterationLabel)
        {
            StringBuilder sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int numVars = cols - rows; //number of original variables
            int numSlack = rows - 1;

            //header
            sb.Append(iterationLabel + "\t");
            for (int j = 0; j < numVars; j++)
                sb.Append($"x{j + 1}\t");
            for (int j = 0; j < numSlack; j++)
                sb.Append($"s{j + 1}\t");
            sb.AppendLine("RHS");

            //z row first
            sb.Append("Z\t");
            for (int j = 0; j < cols; j++)
                sb.Append($"{Math.Round(tableau[rows - 1, j], 3)}\t");
            sb.AppendLine();

            //constraint rows
            for (int i = 0; i < rows - 1; i++)
            {
                sb.Append($"C{i + 1}\t");
                for (int j = 0; j < cols; j++)
                    sb.Append($"{Math.Round(tableau[i, j], 3)}\t");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GetVariableName(int colIndex, int numOriginalVars)
        {
            return colIndex < numOriginalVars ? $"x{colIndex + 1}" : $"s{colIndex - numOriginalVars + 1}";
        }
    }
}
