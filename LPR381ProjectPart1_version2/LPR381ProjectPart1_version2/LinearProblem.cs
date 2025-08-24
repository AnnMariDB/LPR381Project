using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    public class LinearProblem
    {
        public bool IsMaximization { get; set; }
        public List<double> ObjectiveCoeffs { get; set; } = new List<double>();
        public List<List<double>> Constraints { get; set; } = new List<List<double>>();
        public List<double> RHS { get; set; } = new List<double>();
        public List<bool> IsBinary { get; set; } = new List<bool>();

        public string ToCanonicalForm()
        {
            StringBuilder sb = new StringBuilder();

            //objective
            sb.Append("Obj Func(z): ");
            for (int i = 0; i < ObjectiveCoeffs.Count; i++)
            {
                sb.Append((IsMaximization ? -ObjectiveCoeffs[i] : ObjectiveCoeffs[i]) + $"x{i + 1}");
                if (i < ObjectiveCoeffs.Count - 1)
                    sb.Append(" + ");
            }
            sb.AppendLine(" = 0");

            //constraints with slack variables
            for (int i = 0; i < Constraints.Count; i++)
            {
                sb.Append($"c{i + 1}: ");
                for (int j = 0; j < Constraints[i].Count; j++)
                {
                    sb.Append(Constraints[i][j] + $"x{j + 1}");
                    if (j < Constraints[i].Count - 1)
                        sb.Append(" + ");
                }
                sb.Append($" + s{i + 1} = {RHS[i]}");
                sb.AppendLine();
            }

            return sb.ToString();

        }
        // LinearProblem.cs  (add inside the class)
        public LinearProblem Clone()
        {
            return new LinearProblem
            {
                IsMaximization = this.IsMaximization,
                ObjectiveCoeffs = new List<double>(this.ObjectiveCoeffs),
                Constraints = this.Constraints.Select(row => new List<double>(row)).ToList(),
                RHS = new List<double>(this.RHS),
                IsBinary = new List<bool>(this.IsBinary)
            };
        }

        /// <summary>
        /// Adds a <= constraint: a·x <= b (slack will be added by the simplex)
        /// </summary>
        public void AddLeConstraint(double[] a, double b)
        {
            Constraints.Add(new List<double>(a));
            RHS.Add(b);
        }

        /// <summary>
        /// Adds a >= constraint by flipping sign (we store only <= internally)
        /// </summary>
        public void AddGeConstraint(double[] a, double b)
        {
            var neg = a.Select(v => -v).ToArray();
            AddLeConstraint(neg, -b);
        }

    }
}
