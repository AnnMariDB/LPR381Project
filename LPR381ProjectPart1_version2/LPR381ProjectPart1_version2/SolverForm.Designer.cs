using System.Windows.Forms;

namespace LPR381ProjectPart1_version2
{
    partial class SolverForm
    {
        //only one declaration of components
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtObjective;
        private System.Windows.Forms.TextBox txtConstraints;
        private System.Windows.Forms.TextBox txtCanonical;
        private System.Windows.Forms.TextBox txtResults;
        private System.Windows.Forms.Button btnLoadFile;
        private System.Windows.Forms.Button btnRunSimplex;
        private System.Windows.Forms.Button btnSaveResults;

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
            this.txtObjective = new System.Windows.Forms.TextBox();
            this.txtConstraints = new System.Windows.Forms.TextBox();
            this.txtCanonical = new System.Windows.Forms.TextBox();
            this.txtResults = new System.Windows.Forms.TextBox();
            this.btnLoadFile = new System.Windows.Forms.Button();
            this.btnRunSimplex = new System.Windows.Forms.Button();
            this.btnSaveResults = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtObjective
            // 
            this.txtObjective.Location = new System.Drawing.Point(12, 12);
            this.txtObjective.Name = "txtObjective";
            this.txtObjective.Size = new System.Drawing.Size(400, 22);
            this.txtObjective.TabIndex = 0;
            // 
            // txtConstraints
            // 
            this.txtConstraints.Location = new System.Drawing.Point(12, 40);
            this.txtConstraints.Multiline = true;
            this.txtConstraints.Name = "txtConstraints";
            this.txtConstraints.Size = new System.Drawing.Size(400, 100);
            this.txtConstraints.TabIndex = 1;
            this.txtConstraints.TextChanged += new System.EventHandler(this.txtConstraints_TextChanged);
            // 
            // txtCanonical
            // 
            this.txtCanonical.Location = new System.Drawing.Point(12, 150);
            this.txtCanonical.Multiline = true;
            this.txtCanonical.Name = "txtCanonical";
            this.txtCanonical.ReadOnly = true;
            this.txtCanonical.Size = new System.Drawing.Size(400, 100);
            this.txtCanonical.TabIndex = 2;
            // 
            // txtResults
            // 
            this.txtResults.Location = new System.Drawing.Point(12, 260);
            this.txtResults.Multiline = true;
            this.txtResults.Name = "txtResults";
            this.txtResults.ReadOnly = true;
            this.txtResults.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResults.Size = new System.Drawing.Size(400, 150);
            this.txtResults.TabIndex = 3;
            this.txtResults.WordWrap = false;
            // 
            // btnLoadFile
            // 
            this.btnLoadFile.Location = new System.Drawing.Point(430, 12);
            this.btnLoadFile.Name = "btnLoadFile";
            this.btnLoadFile.Size = new System.Drawing.Size(75, 23);
            this.btnLoadFile.TabIndex = 4;
            this.btnLoadFile.Text = "Load File";
            this.btnLoadFile.Click += new System.EventHandler(this.btnLoadFile_Click);
            // 
            // btnRunSimplex
            // 
            this.btnRunSimplex.Location = new System.Drawing.Point(430, 50);
            this.btnRunSimplex.Name = "btnRunSimplex";
            this.btnRunSimplex.Size = new System.Drawing.Size(75, 23);
            this.btnRunSimplex.TabIndex = 5;
            this.btnRunSimplex.Text = "Run Simplex";
            this.btnRunSimplex.Click += new System.EventHandler(this.btnRunSimplex_Click);
            // 
            // btnSaveResults
            // 
            this.btnSaveResults.Location = new System.Drawing.Point(430, 90);
            this.btnSaveResults.Name = "btnSaveResults";
            this.btnSaveResults.Size = new System.Drawing.Size(75, 23);
            this.btnSaveResults.TabIndex = 6;
            this.btnSaveResults.Text = "Save Results";
            this.btnSaveResults.Click += new System.EventHandler(this.btnSaveResults_Click);
            // 
            // SolverForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 430);
            this.Controls.Add(this.txtObjective);
            this.Controls.Add(this.txtConstraints);
            this.Controls.Add(this.txtCanonical);
            this.Controls.Add(this.txtResults);
            this.Controls.Add(this.btnLoadFile);
            this.Controls.Add(this.btnRunSimplex);
            this.Controls.Add(this.btnSaveResults);
            this.Name = "SolverForm";
            this.Text = "Linear Programming Solver - Part 1";
            this.Load += new System.EventHandler(this.SolverForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}

