using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            sb.AppendLine(IsMaximization ? "Maximize:" : "Minimize:");
            sb.AppendLine(string.Join(" + ", ObjectiveCoeffs) + " subject to:");

            for (int i = 0; i < Constraints.Count; i++)
            {
                sb.AppendLine(string.Join(" + ", Constraints[i]) + " <= " + RHS[i]);
            }

            //show binary variables if any
            if (IsBinary != null && IsBinary.Count > 0)
            {
                sb.AppendLine("\nBinary variables:");
                for (int i = 0; i < IsBinary.Count; i++)
                {
                    if (IsBinary[i]) sb.Append($"x{i + 1} ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
