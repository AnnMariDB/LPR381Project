using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    public static class LinearProblemParser
    {
        public static LinearProblem Parse(string objective, string[] constraints)
        {
            var problem = new LinearProblem();

            //detect problem type
            problem.IsMaximization = objective.Trim().ToLower().StartsWith("max");

            //parse objective coefficients
            var objParts = objective.Split(new[] { ' ', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            problem.ObjectiveCoeffs = objParts
                .Skip(1) // skip "max" or "min"
                .Where(p => IsNumeric(p))
                .Select(p => double.Parse(p.TrimStart('+')))
                .ToList();

            //parse constraints
            List<string> mergedConstraints = new List<string>();
            string pendingLine = null;

            foreach (var rawLine in constraints)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                //skip variable type declarations (bin, int, general)
                if (line.ToLower().StartsWith("bin") || line.ToLower().StartsWith("int") || line.ToLower().StartsWith("general"))
                    continue;

                //normalize operators (<=, >=, =) so they are separate tokens
                line = NormalizeConstraintLine(line);

                //merge lines if needed
                if ((line.StartsWith("<=") || line.StartsWith(">=") || line.StartsWith("=")) && pendingLine != null)
                {
                    mergedConstraints.Add((pendingLine + " " + line).Trim());
                    pendingLine = null;
                }
                else
                {
                    if (pendingLine != null)
                        mergedConstraints.Add(pendingLine);
                    pendingLine = line;
                }
            }
            if (pendingLine != null)
                mergedConstraints.Add(pendingLine);

            //parse merged constraints into coefficients and RHS
            foreach (var line in mergedConstraints)
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                int opIndex = Array.FindIndex(parts, p => p == "<=" || p == ">=" || p == "=");
                if (opIndex == -1)
                    throw new Exception("Constraint missing operator (<=, >=, =): " + line);

                var coeffs = parts.Take(opIndex)
                    .Where(p => IsNumeric(p))
                    .Select(p => double.Parse(p.TrimStart('+')))
                    .ToList();

                double rhs = double.Parse(parts[opIndex + 1].TrimStart('+'));

                problem.Constraints.Add(coeffs);
                problem.RHS.Add(rhs);
            }

            //parse binary variables
            var binLine = constraints.FirstOrDefault(l => l.ToLower().StartsWith("bin"));
            if (binLine != null)
            {
                problem.IsBinary = binLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(v => v.ToLower() == "bin")
                                          .ToList();
            }

            return problem;
        }

        //helper: split operator from numbers
        private static string NormalizeConstraintLine(string line)
        {
            line = line.Replace(">=", " >= ")
                       .Replace("<=", " <= ")
                       .Replace("=", " = ");
            return line;
        }

        //helper: check if string is numeric
        private static bool IsNumeric(string s)
        {
            double temp;
            return double.TryParse(s.TrimStart('+'), out temp);
        }
    }
}
