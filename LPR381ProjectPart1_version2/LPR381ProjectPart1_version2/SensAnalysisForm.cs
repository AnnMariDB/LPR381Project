using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPR381ProjectPart1_version2
{
    public class SensAnalysisForm: Form
    {
        private readonly RevisedSimplexState S;
        private readonly int nTotal;

        private Label lblOptimalZ;
        private Label lblShadowPrices;
        private DataGridView gridSummary;
        private ComboBox cmbNonBasic, cmbBasic, cmbRHSIndex;
        private NumericUpDown numNewC, numDeltaC, numDeltaRHS;
        private Button btnShowRangeNB, btnApplyC_NB,
                       btnShowRangeB, btnApplyC_B,
                       btnShowRangeRHS, btnApplyRHS,
                       btnColumnNBRange, btnAddActivity, btnAddConstraint,
                       btnBuildDual, btnSolveDual, btnCheckDuality;
        private TextBox txtLog;

        private const double EPS = 1e-9;

        public SensAnalysisForm(RevisedSimplexState state)
        {
            this.S = state;
            this.nTotal = S.nVars + S.mCons;

            InitializeComponent();
            Populate();
            RenderShadowPrices();
            RenderSummaryGrid();
        }

        private void InitializeComponent()
        {
            this.Text = "Sensitivity Analysis";
            this.Size = new Size(1000, 700);

            lblOptimalZ = new Label { Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblShadowPrices = new Label { Location = new Point(250, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            gridSummary = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(600, 200),
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false
            };

            // Nonbasic variables
            cmbNonBasic = new ComboBox { Location = new Point(20, 270), Width = 120 };
            btnShowRangeNB = new Button { Text = "Show NB Range", Location = new Point(150, 270), Width = 120 };
            numNewC = new NumericUpDown { Location = new Point(280, 270), Width = 80, DecimalPlaces = 2, Minimum = -1000, Maximum = 1000 };
            btnApplyC_NB = new Button { Text = "Apply NB c", Location = new Point(370, 270), Width = 100 };

            // Basic variables
            cmbBasic = new ComboBox { Location = new Point(20, 310), Width = 120 };
            btnShowRangeB = new Button { Text = "Show B Range", Location = new Point(150, 310), Width = 120 };
            numDeltaC = new NumericUpDown { Location = new Point(280, 310), Width = 80, DecimalPlaces = 2, Minimum = -1000, Maximum = 1000 };
            btnApplyC_B = new Button { Text = "Apply B Δc", Location = new Point(370, 310), Width = 100 };

            // RHS
            cmbRHSIndex = new ComboBox { Location = new Point(20, 350), Width = 120 };
            btnShowRangeRHS = new Button { Text = "Show RHS Range", Location = new Point(150, 350), Width = 120 };
            numDeltaRHS = new NumericUpDown { Location = new Point(280, 350), Width = 80, DecimalPlaces = 2, Minimum = -1000, Maximum = 1000 };
            btnApplyRHS = new Button { Text = "Apply RHS Δ", Location = new Point(370, 350), Width = 100 };

            // Extra features
            btnColumnNBRange = new Button { Text = "NB Column Range", Location = new Point(20, 390), Width = 150 };
            btnAddActivity = new Button { Text = "Add Activity", Location = new Point(180, 390), Width = 120 };
            btnAddConstraint = new Button { Text = "Add Constraint", Location = new Point(310, 390), Width = 120 };

            btnBuildDual = new Button { Text = "Build Dual", Location = new Point(20, 430), Width = 100 };
            btnSolveDual = new Button { Text = "Solve Dual", Location = new Point(130, 430), Width = 100 };
            btnCheckDuality = new Button { Text = "Check Duality", Location = new Point(240, 430), Width = 120 };

            txtLog = new TextBox
            {
                Location = new Point(20, 470),
                Size = new Size(940, 170),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            this.Controls.AddRange(new Control[] {
                lblOptimalZ, lblShadowPrices, gridSummary,
                cmbNonBasic, btnShowRangeNB, numNewC, btnApplyC_NB,
                cmbBasic, btnShowRangeB, numDeltaC, btnApplyC_B,
                cmbRHSIndex, btnShowRangeRHS, numDeltaRHS, btnApplyRHS,
                btnColumnNBRange, btnAddActivity, btnAddConstraint,
                btnBuildDual, btnSolveDual, btnCheckDuality,
                txtLog
            });

            // Event handlers
            btnShowRangeNB.Click += btnShowRangeNB_Click;
            btnApplyC_NB.Click += btnApplyC_NB_Click;
            btnShowRangeB.Click += btnShowRangeB_Click;
            btnApplyC_B.Click += btnApplyC_B_Click;
            btnShowRangeRHS.Click += btnShowRangeRHS_Click;
            btnApplyRHS.Click += btnApplyRHS_Click;
            btnColumnNBRange.Click += btnColumnNBRange_Click;
            btnAddActivity.Click += btnAddActivity_Click;
            btnAddConstraint.Click += btnAddConstraint_Click;
            btnBuildDual.Click += btnBuildDual_Click;
            btnSolveDual.Click += btnSolveDual_Click;
            btnCheckDuality.Click += btnCheckDuality_Click;
        }

        private void Populate()
        {
            lblOptimalZ.Text = $"Optimal Z = {S.ZOriginal:0.###}";
            lblShadowPrices.Text = "Shadow Prices: [" + string.Join(", ", S.y.Select(v => v.ToString("0.###"))) + "]";

            cmbNonBasic.Items.AddRange(S.NonBasis.Select(j => VarName(j)).ToArray());
            cmbBasic.Items.AddRange(S.Basis.Select(j => VarName(j)).ToArray());
            cmbRHSIndex.Items.AddRange(Enumerable.Range(1, S.mCons).Select(i => $"Constraint {i}").ToArray());
        }

        private string VarName(int j) => j < S.nVars ? $"x{j + 1}" : $"s{j - S.nVars + 1}";

        private void RenderShadowPrices()
        {
            txtLog.AppendText("Shadow prices (dual values): " + string.Join(", ", S.y.Select(v => v.ToString("0.###"))) + "\r\n");
        }

        private void RenderSummaryGrid()
        {
            gridSummary.Columns.Clear();
            gridSummary.Columns.Add("Variable", "Variable");
            gridSummary.Columns.Add("Type", "Type");
            gridSummary.Columns.Add("Cj", "Objective Coeff.");
            gridSummary.Columns.Add("RC", "Reduced Cost");

            for (int j = 0; j < nTotal; j++)
            {
                string type = S.Basis.Contains(j) ? "Basic" : "Nonbasic";
                gridSummary.Rows.Add(VarName(j), type, S.c[j].ToString("0.###"), S.ReducedCostsStd[j].ToString("0.###"));
            }
        }

        // === Event handlers with plain-English logs ===

        private void btnShowRangeNB_Click(object sender, EventArgs e)
        {
            if (cmbNonBasic.SelectedIndex < 0) return;
            int j = S.NonBasis[cmbNonBasic.SelectedIndex];
            double rj = S.ReducedCostsStd[j];
            double cj = S.c[j];
            double lower = cj - rj;

            txtLog.AppendText(
                $"Nonbasic Variable {VarName(j)}:\r\n" +
                $"The allowable range for its objective coefficient is from {lower:0.###} upwards.\r\n" +
                $"As long as its coefficient is at least {lower:0.###}, the current solution remains optimal.\r\n"
            );
        }

        private void btnApplyC_NB_Click(object sender, EventArgs e)
        {
            if (cmbNonBasic.SelectedIndex < 0) return;
            int j = S.NonBasis[cmbNonBasic.SelectedIndex];
            double newC = (double)numNewC.Value;
            double rj = S.ReducedCostsStd[j];
            double rjPrime = rj + (newC - S.c[j]);
            bool ok = rjPrime >= -EPS;

            txtLog.AppendText(
                $"Applying a new objective coefficient {newC:0.###} to nonbasic variable {VarName(j)}:\r\n" +
                $"New reduced cost = {rjPrime:0.###}.\r\n" +
                $"{(ok ? "It is nonnegative, so the solution remains optimal." : "It is negative, so the solution is no longer optimal and re-optimization is needed.")}\r\n"
            );
        }

        private void btnShowRangeB_Click(object sender, EventArgs e)
        {
            if (cmbBasic.SelectedIndex < 0) return;
            int i = cmbBasic.SelectedIndex;
            var (lo, up) = BasicCoeffDeltaRange(i);

            txtLog.AppendText(
                $"Basic Variable {VarName(S.Basis[i])}:\r\n" +
                $"The allowable change in its objective coefficient is between {lo:0.###} and {up:0.###}.\r\n" +
                $"Any change within this range keeps the current basis optimal.\r\n"
            );
        }

        private void btnApplyC_B_Click(object sender, EventArgs e)
        {
            if (cmbBasic.SelectedIndex < 0) return;
            int i = cmbBasic.SelectedIndex;
            double delta = (double)numDeltaC.Value;
            var (lo, up) = BasicCoeffDeltaRange(i);
            bool ok = delta >= lo - EPS && delta <= up + EPS;

            txtLog.AppendText(
                $"Applying a change of {delta:0.###} to the objective coefficient of basic variable {VarName(S.Basis[i])}:\r\n" +
                $"{(ok ? "This change is within the allowable range, so the solution remains optimal." : "This change is outside the allowable range, so the solution is no longer optimal and re-optimization is needed.")}\r\n"
            );
        }

        private void btnShowRangeRHS_Click(object sender, EventArgs e)
        {
            if (cmbRHSIndex.SelectedIndex < 0) return;
            int i = cmbRHSIndex.SelectedIndex;
            var (lo, up) = RhsDeltaRange(i);

            txtLog.AppendText(
                $"Right-Hand Side of Constraint {i + 1}:\r\n" +
                $"The allowable change is between {lo:0.###} and {up:0.###}.\r\n" +
                $"Any change within this range keeps the solution feasible and optimal.\r\n"
            );
        }

        private void btnApplyRHS_Click(object sender, EventArgs e)
        {
            if (cmbRHSIndex.SelectedIndex < 0) return;
            int i = cmbRHSIndex.SelectedIndex;
            double delta = (double)numDeltaRHS.Value;
            var (lo, up) = RhsDeltaRange(i);
            bool ok = delta >= lo - EPS && delta <= up + EPS;

            txtLog.AppendText(
                $"Applying a change of {delta:0.###} to the right-hand side of constraint {i + 1}:\r\n" +
                $"{(ok ? "This keeps the solution feasible." : "This makes the solution infeasible, so the Dual Simplex method would be required.")}\r\n"
            );
        }

        private void btnColumnNBRange_Click(object sender, EventArgs e)
        {
            if (cmbNonBasic.SelectedIndex < 0 || cmbRHSIndex.SelectedIndex < 0) return;
            var j = S.NonBasis[cmbNonBasic.SelectedIndex];
            int i = cmbRHSIndex.SelectedIndex;
            double rj = S.ReducedCostsStd[j];
            double yi = S.y[i];

            if (Math.Abs(yi) < EPS)
            {
                txtLog.AppendText($"Changing the coefficient of variable {VarName(j)} in constraint {i + 1} has no effect, because the shadow price for that constraint is zero.\r\n");
                return;
            }

            double bound = rj / yi;
            txtLog.AppendText(
                $"Changing the coefficient of variable {VarName(j)} in constraint {i + 1}:\r\n" +
                $"The allowable change is {(yi > 0 ? $"less than or equal to {bound:0.###}" : $"greater than or equal to {bound:0.###}")}.\r\n"
            );
        }

        private void btnAddActivity_Click(object sender, EventArgs e)
        {
            txtLog.AppendText(
                "Add Activity:\r\n" +
                "Compute Reduced Cost = (Objective Coefficient of the new variable) – (Shadow Prices × Resource Usage of the new variable).\r\n" +
                "If Reduced Cost ≥ 0 → The new variable stays at zero and the current solution remains optimal.\r\n" +
                "If Reduced Cost < 0 → The new variable would improve the objective and must enter the solution (re-optimization required).\r\n"
            );
        }

        private void btnAddConstraint_Click(object sender, EventArgs e)
        {
            txtLog.AppendText(
                "Add Constraint:\r\n" +
                "Check whether the new constraint is satisfied by the current solution.\r\n" +
                "If the left-hand side is less than or equal to the right-hand side → Solution remains feasible.\r\n" +
                "If it is violated → Solution is infeasible, apply the Dual Simplex method to restore feasibility.\r\n"
            );
        }

        private void btnBuildDual_Click(object sender, EventArgs e)
        {
            txtLog.AppendText(
                "Dual Problem:\r\n" +
                "Objective: Minimize the sum of (Right-Hand-Side values × Shadow Prices).\r\n" +
                "Subject to: (Constraint Coefficients Transposed × Shadow Prices) ≥ Objective Coefficients.\r\n" +
                "Shadow Prices must be nonnegative.\r\n"
            );
        }

        private void btnSolveDual_Click(object sender, EventArgs e)
        {
            double dualVal = S.b.Zip(S.y, (bi, yi) => bi * yi).Sum();
            txtLog.AppendText($"Dual Objective Value = {dualVal:0.###}, computed as the sum of Right-Hand-Side values multiplied by Shadow Prices.\r\n");
        }

        private void btnCheckDuality_Click(object sender, EventArgs e)
        {
            double primalZ = S.ZOriginal;
            double dualVal = S.b.Zip(S.y, (bi, yi) => bi * yi).Sum();

            txtLog.AppendText(
                $"Check Duality:\r\n" +
                $"Primal Optimal Value = {primalZ:0.###}, Dual Optimal Value = {dualVal:0.###}.\r\n" +
                $"{(Math.Abs(primalZ - dualVal) < 1e-6 ? "They are equal → Strong Duality holds (both values match)." : "They differ → Only Weak Duality holds.")}\r\n"
            );
        }

        // === Helper math ===

        private (double lower, double upper) BasicCoeffDeltaRange(int basicRow)
        {
            int m = S.mCons;
            double[] w = new double[nTotal];
            for (int j = 0; j < nTotal; j++)
            {
                double sum = 0;
                for (int k = 0; k < m; k++)
                    sum += S.BInv[basicRow, k] * S.A[k, j];
                w[j] = sum;
            }

            double lower = double.NegativeInfinity, upper = double.PositiveInfinity;
            foreach (var j in S.NonBasis)
            {
                double rj = S.ReducedCostsStd[j];
                double wij = w[j];
                if (Math.Abs(wij) < EPS) continue;
                double bound = rj / wij;
                if (wij > 0) upper = Math.Min(upper, bound);
                else lower = Math.Max(lower, bound);
            }
            return (lower, upper);
        }

        private (double lower, double upper) RhsDeltaRange(int iRow)
        {
            int m = S.mCons;
            double lower = double.NegativeInfinity, upper = double.PositiveInfinity;
            for (int k = 0; k < m; k++)
            {
                double d = S.BInv[k, iRow];
                if (Math.Abs(d) < EPS) continue;
                double bound = -S.xB[k] / d;
                if (d > 0) lower = Math.Max(lower, bound);
                else upper = Math.Min(upper, bound);
            }
            return (lower, upper);
        }

    }
}
