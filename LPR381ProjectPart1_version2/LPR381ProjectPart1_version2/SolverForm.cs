using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace LPR381ProjectPart1_version2
{
    public partial class SolverForm : Form
    {
        private LinearProblem currentProblem;
        public SolverForm()
        {
            InitializeComponent();
        }
        private RevisedSimplex revisedSimplex;

        private void SolverForm_Load(object sender, EventArgs e)
        {
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(ofd.FileName);
                txtObjective.Text = lines[0];
                txtConstraints.Lines = lines.Skip(1).ToArray();
                MessageBox.Show("File loaded successfully!");
            }
        }

        private void btnRunSimplex_Click(object sender, EventArgs e)
        {
            try
            {
                currentProblem = LinearProblemParser.Parse(
                    txtObjective.Text,
                    txtConstraints.Lines
                );

                txtCanonical.Text = currentProblem.ToCanonicalForm();

                SimplexSolver solver = new SimplexSolver(currentProblem);
                string result = solver.Solve();

                txtResults.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnSaveResults_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtResults.Text))
            {
                MessageBox.Show("No results to save.");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, txtResults.Text);
                MessageBox.Show("Results saved!");
            }
        }

        private void txtConstraints_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnRevisedPrimalSimplex_Click(object sender, EventArgs e)
        {
            try
            {
                currentProblem = LinearProblemParser.Parse(txtObjective.Text, txtConstraints.Lines);
                txtCanonical.Text = currentProblem.ToCanonicalForm();

                int m = currentProblem.Constraints.Count;
                int n = currentProblem.ObjectiveCoeffs.Count;

                double[,] A_aug = new double[m, n + m];
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                        A_aug[i, j] = currentProblem.Constraints[i][j];
                    A_aug[i, n + i] = 1.0;
                }

                double[] b = currentProblem.RHS.ToArray();
                double[] c = new double[n + m];
                for (int j = 0; j < n; j++)
                    c[j] = currentProblem.IsMaximization ? currentProblem.ObjectiveCoeffs[j]
                                                         : -currentProblem.ObjectiveCoeffs[j];

                revisedSimplex = new RevisedSimplex(A_aug, b, c, currentProblem.IsMaximization, n);
                txtResults.Text = revisedSimplex.Solve();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Revised Simplex: " + ex.Message);
            }
        }

        private void btnGoToSensitivityAnalysis_Click(object sender, EventArgs e)
        {
            if (revisedSimplex == null || revisedSimplex.LastState == null || !revisedSimplex.LastState.IsOptimal)
            {
                MessageBox.Show("Please run the Revised Primal Simplex to optimality first.",
                                "Sensitivity Analysis",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            using (var f = new SensAnalysisForm(revisedSimplex.LastState))
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }
        private void btnBranchandBoundKnapsack_Click(object sender, EventArgs e)
        {
            try
            {
                currentProblem = LinearProblemParser.Parse(txtObjective.Text, txtConstraints.Lines);
                txtCanonical.Text = currentProblem.ToCanonicalForm();

                KnapsackSolver ks = new KnapsackSolver(currentProblem);
                string result = ks.Solve();

                txtResults.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}

