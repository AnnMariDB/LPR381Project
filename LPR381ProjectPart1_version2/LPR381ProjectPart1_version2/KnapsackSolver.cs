using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381ProjectPart1_version2
{
    internal class KnapsackSolver
    {
        private readonly LinearProblem problem;
        private readonly int n;
        private readonly double[] valuesObj;
        private readonly double[] weightsConst;
        private readonly double capacityRHS;
        private readonly bool[] isBinary;

        //tracks best solution
        private double bestValue;
        private int[] bestTakenPattern;

        //for the ranking
        private int[] orderRank;

        private StringBuilder log;

        public KnapsackSolver(LinearProblem problem)
        {
            this.problem = problem ?? throw new ArgumentNullException(nameof(problem));

            //define lengths based on problem inputted...
            n = problem.ObjectiveCoeffs.Count;
            valuesObj = new double[n];
            weightsConst = new double[n];
            isBinary = new bool[n];

            //fill obj vallues
            for (int i = 0; i < n; i++) 
            {
                valuesObj[i] = problem.ObjectiveCoeffs[i]; 
            }

            if (problem.Constraints == null || problem.Constraints.Count == 0) 
            {
                throw new Exception("No constraint found. Knapsack needs at least one constraint.");
            }

            if (problem.Constraints.Count > 1) 
            {
                throw new Exception("Knapsack solver only supports ONE constraint (classic knapsack)." + "Multiple constraints inputted.");
            }

            
            var firstConstraint = problem.Constraints[0];
            if (firstConstraint.Count < n) 
            {
                throw new Exception("Constraint row length ");
            }

            //first constraints become weights
            for (int i = 0; i< n; i++) 
            {
                weightsConst[i] = firstConstraint[i];
            }

            capacityRHS = problem.RHS != null && problem.RHS.Count > 0 ? problem.RHS[0] : throw new Exception("No RHS for first consrtraint.");

            //binary flags, and making unlabeled d.v'd binary...
            if (problem.IsBinary != null &&  problem.IsBinary.Count >= n)
            {
                for (int i = 0; i < n; i++)
                {
                    isBinary[i] = problem.IsBinary[i];
                }
            }
            else
            {
                for(int i = 0; i < n; i++)
                {
                    isBinary[i] = true;
                }
            }

            //ranking by ratio, ties broen by bigger voptimal value
            orderRank = Enumerable.Range(0, n)
                .OrderByDescending(i => (weightsConst[i] == 0) ? double.PositiveInfinity : valuesObj[i] / weightsConst[i])
                .ThenByDescending(i => valuesObj[i])
                .ToArray();

            bestTakenPattern = new int[n];
            bestValue = double.NegativeInfinity;

            log = new StringBuilder();
        }

        public string Solve()
        {
            log.Clear();
            log.AppendLine("=== Knapsack - Branch & Bound ===");
            log.AppendLine($"Capacity = {capacityRHS}");
            log.AppendLine("Items (index: value, weight, ratio)");
            for(int i = 0;i < n; i++) 
            {
                log.AppendLine($"{i+1} : {valuesObj[i]}, {weightsConst[i]}, ratio = {(weightsConst[i] == 0?double.PositiveInfinity: valuesObj[i]/ weightsConst[i]):F3}");
            }
            log.AppendLine();
            log.AppendLine("Order used :" + string.Join(", ", orderRank.Select(i => (i+1).ToString())));

            // initial greedy search
            var greedyTaken = new int[n];
            double greedyValue = 0;
            double greedyWeight = 0;
            foreach (var indx in orderRank)
            {
                if (isBinary[indx] == false) continue;
                if (greedyWeight + weightsConst[indx] <= capacityRHS)
                {
                    greedyTaken[indx] = 1;
                    greedyWeight += weightsConst[indx];
                    greedyValue += valuesObj[indx];
                }
            }

            bestValue = greedyValue;
            Array.Copy(greedyTaken, bestTakenPattern, n);
            log.AppendLine($"Initial greedy feasible solution = {greedyValue}");
            log.AppendLine($"Greedy selection: {string.Join(", ", Enumerable.Range(0, n).Where(i => greedyTaken[i] ==1).Select(i => i+1))}");
            log.AppendLine();

            //recursive branch and bounding
            int[] currentTaken = new int[n];
            BranchRec(0, 0.0, 0.0, currentTaken);

            //reports best
            log.AppendLine();
            log.AppendLine("=== Best integer solution ===");
            log.AppendLine($"Value = {bestValue:F2}");

            log.AppendLine($"Optimal items taken: {string.Join(" ,", Enumerable.Range(0,n).Where(i => bestTakenPattern[i] ==1).Select(i => i + 1))}");
            return log.ToString();
        }

        private void BranchRec(int indx, double currentWeight, double currentValue, int[] taken)
        {
            //if all items were considered
            if (indx == orderRank.Length)
            {
                if (currentValue > bestValue) 
                {
                    bestValue = currentValue;
                    Array.Copy(taken, bestTakenPattern, n);
                    log.AppendLine($"New best at node: value={bestValue:F3}, weight={currentWeight:F3}, items={string.Join(", ", Enumerable.Range(0, n).Where(i => bestTakenPattern[i] == 1).Select(i => i + 1))}");
                    log.AppendLine();
                }
                return;
            }

            double bound = currentValue + FractionalUpperBound(indx, currentWeight);

            //pruning
            if (bound <= bestValue + 1e-9)
            {
                log.AppendLine($"Pruned at index = {indx} (bound {bound:F3} <= best {bestValue:F3})");
                return;
            }

            int item = orderRank[indx];

            if (currentWeight + weightsConst[item] <= capacityRHS)
            {
                taken[item] = 1;
                BranchRec(indx + 1, currentWeight + weightsConst[item], currentValue + valuesObj[item], taken);
                taken[item] = 0;
            }
            else
            {
                log.AppendLine($"Item {item + 1} cannot be included -- capacity.");
            }

            taken[item] = 0;
            BranchRec(indx +1, currentWeight, currentValue, taken);
        }

        private double FractionalUpperBound(int indxInOrder, double currentWeight)
        {
            //optimistic bounding for pruning

            double remCap = capacityRHS - currentWeight;
            double ub = 0.0;

            for (int i = indxInOrder; i < orderRank.Length; i++) 
            {
                int item = orderRank[i];
                if (remCap <= 0) break;
                if (isBinary[item] == false) continue;
                if (weightsConst[item] <= remCap)
                {
                    remCap -= weightsConst[item];
                    ub += valuesObj[item];
                }
                else
                {
                    if (weightsConst[item] > 0) 
                    {
                        ub += valuesObj[item] * (remCap / weightsConst[item]);
                        remCap = 0;
                        break;
                    }
                }
            }
            return ub;
        }
    }
}
