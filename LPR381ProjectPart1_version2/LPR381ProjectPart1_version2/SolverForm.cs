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
                if (lines.Length == 0)
                {
                    MessageBox.Show("Selected file is empty.");
                    return;
                }

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

                var solver = new SimplexSolver(currentProblem);
                string result = solver.Solve();

                txtResults.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnBranchandBoundSimplex_Click(object sender, EventArgs e)
        {
            try
            {
                currentProblem = LinearProblemParser.Parse(
                    txtObjective.Text,
                    txtConstraints.Lines
                );

                var bb = new BranchAndBoundSolver(currentProblem);
                var log = bb.Solve(out var x, out var z);

                var sb = new StringBuilder();
                sb.AppendLine("=== Branch & Bound (Simplex) ===");
                sb.AppendLine(log);
                if (x != null && x.Length > 0)
                {
                    sb.AppendLine("\nIncumbent (integer) solution:");
                    for (int i = 0; i < x.Length; i++)
                        sb.AppendLine($"x{i + 1} = {x[i]:0.###}");
                    sb.AppendLine($"z = {z:0.###}");
                }
                txtResults.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnCuttingPlane_Click(object sender, EventArgs e)
        {
            try
            {
                currentProblem = LinearProblemParser.Parse(
                    txtObjective.Text,
                    txtConstraints.Lines
                );

                // Optional: show canonical before solving
                txtCanonical.Text = currentProblem.ToCanonicalForm();

                var cp = new CuttingPlaneSolver(currentProblem);
                var log = cp.Solve(out var x, out var z);

                var sb = new StringBuilder();
                sb.AppendLine("=== Cutting Plane (Gomory) ===");
                sb.AppendLine(log);
                if (x != null && x.Length > 0)
                {
                    sb.AppendLine("\nCurrent integer solution:");
                    for (int i = 0; i < x.Length; i++)
                        sb.AppendLine($"x{i + 1} = {x[i]:0.###}");
                    sb.AppendLine($"z = {z:0.###}");
                }

                txtResults.Text = sb.ToString();
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

        // If you have any extra buttons hooked up in the Designer:
        private void button1_Click(object sender, EventArgs e)
        {
        }
    }
}
