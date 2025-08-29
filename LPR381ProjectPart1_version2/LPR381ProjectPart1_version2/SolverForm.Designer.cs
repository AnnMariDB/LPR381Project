using System.Windows.Forms;

namespace LPR381ProjectPart1_version2
{
    partial class SolverForm
    {
        //only one declaration of components
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSaveResults = new System.Windows.Forms.Button();
            this.btnRunSimplex = new System.Windows.Forms.Button();
            this.btnLoadFile = new System.Windows.Forms.Button();
            this.txtResults = new System.Windows.Forms.TextBox();
            this.txtCanonical = new System.Windows.Forms.TextBox();
            this.txtConstraints = new System.Windows.Forms.TextBox();
            this.txtObjective = new System.Windows.Forms.TextBox();
            this.lblSolveUsing = new System.Windows.Forms.Label();
            this.btnRevisedPrimalSimplex = new System.Windows.Forms.Button();
            this.btnBranchandBoundSimplex = new System.Windows.Forms.Button();
            this.btnCuttingPlane = new System.Windows.Forms.Button();
            this.btnBranchandBoundKnapsack = new System.Windows.Forms.Button();
            this.btnGoToSensitivityAnalysis = new System.Windows.Forms.Button();
            this.pnlBtnBack = new System.Windows.Forms.Panel();
            this.lblOtherOptions = new System.Windows.Forms.Label();
            this.pnlBtnBack.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSaveResults
            // 
            this.btnSaveResults.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnSaveResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveResults.Location = new System.Drawing.Point(28, 495);
            this.btnSaveResults.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSaveResults.Name = "btnSaveResults";
            this.btnSaveResults.Size = new System.Drawing.Size(255, 43);
            this.btnSaveResults.TabIndex = 6;
            this.btnSaveResults.Text = "Save Results";
            this.btnSaveResults.UseVisualStyleBackColor = false;
            this.btnSaveResults.Click += new System.EventHandler(this.btnSaveResults_Click);
            // 
            // btnRunSimplex
            // 
            this.btnRunSimplex.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnRunSimplex.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunSimplex.Location = new System.Drawing.Point(27, 95);
            this.btnRunSimplex.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRunSimplex.Name = "btnRunSimplex";
            this.btnRunSimplex.Size = new System.Drawing.Size(255, 54);
            this.btnRunSimplex.TabIndex = 5;
            this.btnRunSimplex.Text = "Primal Simplex";
            this.btnRunSimplex.UseVisualStyleBackColor = false;
            this.btnRunSimplex.Click += new System.EventHandler(this.btnRunSimplex_Click);
            // 
            // btnLoadFile
            // 
            this.btnLoadFile.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnLoadFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoadFile.Location = new System.Drawing.Point(27, 6);
            this.btnLoadFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLoadFile.Name = "btnLoadFile";
            this.btnLoadFile.Size = new System.Drawing.Size(255, 32);
            this.btnLoadFile.TabIndex = 4;
            this.btnLoadFile.Text = "Load File";
            this.btnLoadFile.UseVisualStyleBackColor = false;
            this.btnLoadFile.Click += new System.EventHandler(this.btnLoadFile_Click);
            // 
            // txtResults
            // 
            this.txtResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResults.Location = new System.Drawing.Point(12, 342);
            this.txtResults.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtResults.Multiline = true;
            this.txtResults.Name = "txtResults";
            this.txtResults.ReadOnly = true;
            this.txtResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResults.Size = new System.Drawing.Size(677, 218);
            this.txtResults.TabIndex = 3;
            this.txtResults.WordWrap = false;
            // 
            // txtCanonical
            // 
            this.txtCanonical.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCanonical.Location = new System.Drawing.Point(12, 196);
            this.txtCanonical.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtCanonical.Multiline = true;
            this.txtCanonical.Name = "txtCanonical";
            this.txtCanonical.ReadOnly = true;
            this.txtCanonical.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtCanonical.Size = new System.Drawing.Size(677, 126);
            this.txtCanonical.TabIndex = 2;
            // 
            // txtConstraints
            // 
            this.txtConstraints.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConstraints.Location = new System.Drawing.Point(12, 73);
            this.txtConstraints.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtConstraints.Multiline = true;
            this.txtConstraints.Name = "txtConstraints";
            this.txtConstraints.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtConstraints.Size = new System.Drawing.Size(677, 112);
            this.txtConstraints.TabIndex = 1;
            this.txtConstraints.TextChanged += new System.EventHandler(this.txtConstraints_TextChanged);
            // 
            // txtObjective
            // 
            this.txtObjective.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtObjective.Location = new System.Drawing.Point(12, 18);
            this.txtObjective.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtObjective.Name = "txtObjective";
            this.txtObjective.Size = new System.Drawing.Size(677, 30);
            this.txtObjective.TabIndex = 0;
            // 
            // lblSolveUsing
            // 
            this.lblSolveUsing.AutoSize = true;
            this.lblSolveUsing.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSolveUsing.Location = new System.Drawing.Point(45, 60);
            this.lblSolveUsing.Name = "lblSolveUsing";
            this.lblSolveUsing.Size = new System.Drawing.Size(202, 20);
            this.lblSolveUsing.TabIndex = 7;
            this.lblSolveUsing.Text = " Solve using algorithm:";
            this.lblSolveUsing.Click += new System.EventHandler(this.label1_Click);
            // 
            // btnRevisedPrimalSimplex
            // 
            this.btnRevisedPrimalSimplex.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnRevisedPrimalSimplex.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRevisedPrimalSimplex.Location = new System.Drawing.Point(27, 154);
            this.btnRevisedPrimalSimplex.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRevisedPrimalSimplex.Name = "btnRevisedPrimalSimplex";
            this.btnRevisedPrimalSimplex.Size = new System.Drawing.Size(255, 54);
            this.btnRevisedPrimalSimplex.TabIndex = 8;
            this.btnRevisedPrimalSimplex.Text = "Revised Primal Simplex";
            this.btnRevisedPrimalSimplex.UseVisualStyleBackColor = false;
            this.btnRevisedPrimalSimplex.Click += new System.EventHandler(this.btnRevisedPrimalSimplex_Click);
            // 
            // btnBranchandBoundSimplex
            // 
            this.btnBranchandBoundSimplex.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnBranchandBoundSimplex.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBranchandBoundSimplex.Location = new System.Drawing.Point(27, 213);
            this.btnBranchandBoundSimplex.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnBranchandBoundSimplex.Name = "btnBranchandBoundSimplex";
            this.btnBranchandBoundSimplex.Size = new System.Drawing.Size(255, 59);
            this.btnBranchandBoundSimplex.TabIndex = 9;
            this.btnBranchandBoundSimplex.Text = "Branch and Bound Simplex";
            this.btnBranchandBoundSimplex.UseVisualStyleBackColor = false;
            // 
            // btnCuttingPlane
            // 
            this.btnCuttingPlane.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnCuttingPlane.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCuttingPlane.Location = new System.Drawing.Point(27, 277);
            this.btnCuttingPlane.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCuttingPlane.Name = "btnCuttingPlane";
            this.btnCuttingPlane.Size = new System.Drawing.Size(255, 54);
            this.btnCuttingPlane.TabIndex = 10;
            this.btnCuttingPlane.Text = "Cutting Plane";
            this.btnCuttingPlane.UseVisualStyleBackColor = false;
            // 
            // btnBranchandBoundKnapsack
            // 
            this.btnBranchandBoundKnapsack.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnBranchandBoundKnapsack.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBranchandBoundKnapsack.Location = new System.Drawing.Point(27, 336);
            this.btnBranchandBoundKnapsack.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnBranchandBoundKnapsack.Name = "btnBranchandBoundKnapsack";
            this.btnBranchandBoundKnapsack.Size = new System.Drawing.Size(255, 53);
            this.btnBranchandBoundKnapsack.TabIndex = 11;
            this.btnBranchandBoundKnapsack.Text = "Knapsack (Branch and Bound) ";
            this.btnBranchandBoundKnapsack.UseVisualStyleBackColor = false;
            this.btnBranchandBoundKnapsack.Click += new System.EventHandler(this.btnBranchandBoundKnapsack_Click);
            // 
            // btnGoToSensitivityAnalysis
            // 
            this.btnGoToSensitivityAnalysis.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnGoToSensitivityAnalysis.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGoToSensitivityAnalysis.Location = new System.Drawing.Point(28, 448);
            this.btnGoToSensitivityAnalysis.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnGoToSensitivityAnalysis.Name = "btnGoToSensitivityAnalysis";
            this.btnGoToSensitivityAnalysis.Size = new System.Drawing.Size(255, 42);
            this.btnGoToSensitivityAnalysis.TabIndex = 12;
            this.btnGoToSensitivityAnalysis.Text = "Go To Sensitivity Analysis";
            this.btnGoToSensitivityAnalysis.UseVisualStyleBackColor = false;
            // 
            // pnlBtnBack
            // 
            this.pnlBtnBack.BackColor = System.Drawing.Color.Turquoise;
            this.pnlBtnBack.Controls.Add(this.lblOtherOptions);
            this.pnlBtnBack.Controls.Add(this.btnGoToSensitivityAnalysis);
            this.pnlBtnBack.Controls.Add(this.btnBranchandBoundKnapsack);
            this.pnlBtnBack.Controls.Add(this.btnCuttingPlane);
            this.pnlBtnBack.Controls.Add(this.btnBranchandBoundSimplex);
            this.pnlBtnBack.Controls.Add(this.btnRevisedPrimalSimplex);
            this.pnlBtnBack.Controls.Add(this.lblSolveUsing);
            this.pnlBtnBack.Controls.Add(this.btnLoadFile);
            this.pnlBtnBack.Controls.Add(this.btnRunSimplex);
            this.pnlBtnBack.Controls.Add(this.btnSaveResults);
            this.pnlBtnBack.Location = new System.Drawing.Point(724, 12);
            this.pnlBtnBack.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlBtnBack.Name = "pnlBtnBack";
            this.pnlBtnBack.Size = new System.Drawing.Size(308, 549);
            this.pnlBtnBack.TabIndex = 13;
            // 
            // lblOtherOptions
            // 
            this.lblOtherOptions.AutoSize = true;
            this.lblOtherOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOtherOptions.Location = new System.Drawing.Point(83, 409);
            this.lblOtherOptions.Name = "lblOtherOptions";
            this.lblOtherOptions.Size = new System.Drawing.Size(135, 20);
            this.lblOtherOptions.TabIndex = 13;
            this.lblOtherOptions.Text = " Other options:";
            // 
            // SolverForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.BackgroundImage = global::LPR381ProjectPart1_version2.Properties.Resources.abstract_geometric_background_in_flat_design_free_vector;
            this.ClientSize = new System.Drawing.Size(1048, 583);
            this.Controls.Add(this.pnlBtnBack);
            this.Controls.Add(this.txtObjective);
            this.Controls.Add(this.txtConstraints);
            this.Controls.Add(this.txtCanonical);
            this.Controls.Add(this.txtResults);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "SolverForm";
            this.Text = "Linear Programming Solver - Part 1";
            this.Load += new System.EventHandler(this.SolverForm_Load);
            this.pnlBtnBack.ResumeLayout(false);
            this.pnlBtnBack.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnSaveResults;
        private Button btnRunSimplex;
        private Button btnLoadFile;
        private TextBox txtResults;
        private TextBox txtCanonical;
        private TextBox txtConstraints;
        private TextBox txtObjective;
        private Label lblSolveUsing;
        private Button btnRevisedPrimalSimplex;
        private Button btnBranchandBoundSimplex;
        private Button btnCuttingPlane;
        private Button btnBranchandBoundKnapsack;
        private Button btnGoToSensitivityAnalysis;
        private Panel pnlBtnBack;
        private Label lblOtherOptions;
    }
}

