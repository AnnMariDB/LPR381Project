using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381ProjectPart1_version2
{
    /// <summary>
    /// Revised Simplex using Product Form of the Inverse (PFI) with price-out.
    /// Adds a dual-simplex fallback whenever the current basis is infeasible (xB has negatives),
    /// which is typical immediately after adding a Gomory cut.
    /// Standard form: A x + I s = b, x>=0, s>=0. Starts with slack basis.
    /// For Min problems, costs are flipped so we maximize internally.
    /// </summary>
    public sealed class RevisedSimplexPFI
    {
        private readonly bool isMax;
        private readonly int n0;      // # original vars
        private int m;                // # constraints/slacks
        private int n;                // # original vars (= n0)
        private List<List<double>> A; // m x n
        private List<double> b;       // m
        private double[] c;           // costs (flipped if min)
        private double zSign;         // +1 max, -1 min

        // basis indices: 0..n-1 for x, n..n+m-1 for slacks
        private int[] basis;
        private readonly List<Eta> etas = new List<Eta>(); // PFI eta stack

        private const double EPS = 1e-9;
        private readonly StringBuilder log = new StringBuilder();

        private struct Eta { public int r; public double[] d; }

        public RevisedSimplexPFI(LinearProblem p)
        {
            isMax = p.IsMaximization;
            zSign = isMax ? +1.0 : -1.0;

            n0 = p.ObjectiveCoeffs.Count;
            n = n0;
            m = p.Constraints.Count;

            A = p.Constraints.Select(row => new List<double>(row)).ToList();
            b = new List<double>(p.RHS);

            c = new double[n0];
            for (int j = 0; j < n0; j++) c[j] = zSign * p.ObjectiveCoeffs[j];

            basis = new int[m];
            for (int i = 0; i < m; i++) basis[i] = n + i; // slacks basic

            log.AppendLine("PFI: initial basis = slacks, B^{-1} = I.");
        }

        /// <summary>Optimize to optimality (or detect infeasible/unbounded). Returns a detailed log.</summary>
        public string Optimize(out double[] xOrig, out double z)
        {
            xOrig = new double[n0];
            z = double.NaN;

            int it = 0;
            while (true)
            {
                it++;

                // Current basic values (x_B = B^{-1}b)
                var xB = ApplyBInv(b.ToArray());

                // ========= Dual-simplex fallback if basis infeasible (some xB_i < 0) =========
                int infeasRow = -1;
                double mostNeg = 0.0;
                for (int i = 0; i < m; i++)
                {
                    if (xB[i] < -1e-10 && xB[i] < mostNeg)
                    {
                        mostNeg = xB[i];
                        infeasRow = i;
                    }
                }
                if (infeasRow >= 0)
                {
                    // Dual multipliers y^T = c_B^T B^{-1}
                    var y = ApplyBInvT(GetCB());

                    // Reduced costs for all nonbasic columns
                    int totalCols = n + m;
                    double[] rc = new double[totalCols];
                    for (int j = 0; j < n; j++)
                    {
                        if (IsBasic(j)) { rc[j] = 0; continue; }
                        rc[j] = c[j] - Dot(y, GetAcol(j));
                    }
                    for (int s = 0; s < m; s++)
                    {
                        int j = n + s;
                        if (IsBasic(j)) { rc[j] = 0; continue; }
                        rc[j] = -y[s]; // slack column
                    }

                    // Pick entering via dual ratio:
                    // among j with d_rj < 0 and rc_j < 0 (for maximization), minimize rc_j / (-d_rj)
                    int enter = -1; double bestRatio = double.PositiveInfinity;
                    for (int j = 0; j < totalCols; j++)
                    {
                        if (IsBasic(j)) continue;

                        var aCol = (j < n) ? GetAcol(j) : UnitCol(j - n, m);
                        var d = ApplyBInv(aCol);
                        double d_r = d[infeasRow];

                        if (d_r < -1e-12 && rc[j] < -1e-12)
                        {
                            double ratio = rc[j] / (-d_r);
                            if (ratio < bestRatio)
                            {
                                bestRatio = ratio;
                                enter = j;
                            }
                        }
                    }

                    if (enter < 0)
                    {
                        log.AppendLine($"PFI (dual): infeasible row C{infeasRow + 1} but no entering column satisfies dual conditions. LP infeasible.");
                        return log.ToString();
                    }

                    // Dual pivot
                    var aEnter = (enter < n) ? GetAcol(enter) : UnitCol(enter - n, m);
                    var dEnter = ApplyBInv(aEnter);
                    int leaveCol = basis[infeasRow];
                    basis[infeasRow] = enter;
                    etas.Add(new Eta { r = infeasRow, d = dEnter });

                    log.AppendLine($"PFI (dual) pivot: enter {ColName(enter)}, leave {ColName(leaveCol)} at row C{infeasRow + 1}");
                    // Continue loop to recompute xB and check feasibility again
                    continue;
                }
                // ===================== End dual fallback =====================

                // Price-out: y^T = c_B^T B^{-1}
                var cB = GetCB();
                var yT = ApplyBInvT(cB);

                // Reduced costs for primal step
                int total = n + m;
                double[] red = new double[total];
                for (int j = 0; j < n; j++)
                {
                    if (IsBasic(j)) { red[j] = 0; continue; }
                    red[j] = c[j] - Dot(yT, GetAcol(j));
                }
                for (int s = 0; s < m; s++)
                {
                    int j = n + s;
                    if (IsBasic(j)) { red[j] = 0; continue; }
                    red[j] = -yT[s];
                }

                // Select entering (max positive reduced cost)
                int eCol = -1; double best = 0.0;
                for (int j = 0; j < total; j++)
                    if (!IsBasic(j) && red[j] > best + EPS) { best = red[j]; eCol = j; }

                log.AppendLine($"PFI Iter {it}: max reduced cost = {best:0.###} at col {ColName(eCol)}");

                if (eCol < 0)
                {
                    // Optimal and feasible
                    var full = BuildFullSolution(xB);
                    for (int j = 0; j < n0 && j < full.Length; j++) xOrig[j] = full[j];
                    z = zSign * Dot(c, xOrig);
                    log.AppendLine("PFI: optimal reached.");
                    return log.ToString();
                }

                // Primal ratio test
                var aE = (eCol < n) ? GetAcol(eCol) : UnitCol(eCol - n, m);
                var dVec = ApplyBInv(aE);

                int lRow = -1; double theta = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (dVec[i] > EPS)
                    {
                        double t = xB[i] / dVec[i];
                        if (t < theta) { theta = t; lRow = i; }
                    }
                }
                if (lRow < 0)
                {
                    log.AppendLine("PFI: Unbounded (no positive d_i).");
                    return log.ToString();
                }

                int lCol = basis[lRow];
                basis[lRow] = eCol;
                etas.Add(new Eta { r = lRow, d = dVec });

                log.AppendLine($"PFI pivot: enter {ColName(eCol)}, leave {ColName(lCol)} at row C{lRow + 1}, θ = {theta:0.###}");
            }
        }

        /// <summary>Add <= row a·x <= bNew. Warm-start with new slack basic.</summary>
        public void AddRowAndWarmStart(double[] a, double bNew)
        {
            A.Add(new List<double>(a));
            b.Add(bNew);
            m += 1;

            var newBasis = new int[m];
            Array.Copy(basis, newBasis, m - 1);
            newBasis[m - 1] = n + (m - 1); // new slack is basic
            basis = newBasis;

            // Resize eta vectors
            for (int k = 0; k < etas.Count; k++)
            {
                var e = etas[k];
                var d2 = new double[m];
                Array.Copy(e.d, d2, m - 1);
                d2[m - 1] = 0.0;
                etas[k] = new Eta { r = e.r, d = d2 };
            }

            log.AppendLine($"PFI: added cut row (<=). Warm-start with new slack s{m} basic.");
        }

        // ------------ Exposed helpers for CuttingPlane ------------
        public int Rows => m;
        public int Cols => n;
        public int[] Basis => basis.ToArray();
        public string GetLog() => log.ToString();
        public double[] ComputeXB() => ApplyBInv(b.ToArray());
        public double[] GetBinvRow(int rowIndex)
        {
            var e = UnitCol(rowIndex, m);
            return ApplyBInvT(e);
        }

        // ---------------- Internal math helpers ----------------
        private bool IsBasic(int col) { for (int i = 0; i < m; i++) if (basis[i] == col) return true; return false; }
        private double[] GetAcol(int j) { var v = new double[m]; for (int i = 0; i < m; i++) v[i] = A[i][j]; return v; }
        private double[] UnitCol(int idx, int len) { var v = new double[len]; if (idx >= 0 && idx < len) v[idx] = 1.0; return v; }
        private double[] GetCB() { var v = new double[m]; for (int i = 0; i < m; i++) v[i] = (basis[i] < n) ? c[basis[i]] : 0.0; return v; }
        private double[] BuildFullSolution(double[] xB) { var full = new double[n + m]; for (int i = 0; i < m; i++) full[basis[i]] = xB[i]; return full; }
        private string ColName(int idx) => idx < 0 ? "(none)" : (idx < n ? $"x{idx + 1}" : $"s{idx - n + 1}");
        private double Dot(double[] a, double[] b) { double s = 0.0; for (int i = 0; i < a.Length && i < b.Length; i++) s += a[i] * b[i]; return s; }

        /// <summary>Apply B^{-1} using forward order of etas.</summary>
        private double[] ApplyBInv(double[] v)
        {
            for (int k = 0; k < etas.Count; k++)
            {
                var e = etas[k]; int r = e.r; var d = e.d;
                double yr = v[r] / d[r];
                v[r] = yr;
                for (int i = 0; i < v.Length; i++) if (i != r) v[i] -= d[i] * yr;
            }
            return v;
        }

        /// <summary>Apply B^{-T} using REVERSE order of etas (correct for transpose).</summary>
        private double[] ApplyBInvT(double[] v)
        {
            for (int k = etas.Count - 1; k >= 0; k--)
            {
                var e = etas[k]; int r = e.r; var d = e.d;
                double s = 0.0; for (int i = 0; i < v.Length; i++) if (i != r) s += d[i] * v[i];
                double yr = (v[r] - s) / d[r];
                v[r] = yr; // others unchanged
            }
            return v;
        }
    }
}
