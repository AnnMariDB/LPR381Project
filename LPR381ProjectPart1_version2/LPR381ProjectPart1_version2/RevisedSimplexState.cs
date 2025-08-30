using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381ProjectPart1_version2
{
    public sealed class RevisedSimplexState
    {
        public bool IsOptimal { get; set; }
        public bool OriginalIsMax { get; set; }

        public double[,] A { get; set; }
        public double[] b { get; set; }
        public double[] c { get; set; }
        public int nVars { get; set; }
        public int mCons { get; set; }

        public int[] Basis { get; set; }
        public int[] NonBasis { get; set; }
        public double[,] BInv { get; set; }
        public double[] xB { get; set; }
        public double[] y { get; set; }  // shadow prices
        public double[] ReducedCostsStd { get; set; }
        public double ZOriginal { get; set; }
    }
}
