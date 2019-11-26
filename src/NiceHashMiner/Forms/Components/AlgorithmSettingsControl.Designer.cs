﻿namespace NiceHashMiner.Forms.Components {
    partial class AlgorithmSettingsControl {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlgorithmSettingsControl));
            this.groupBoxSelectedAlgorithmSettings = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.labelSelectedAlgorithm = new System.Windows.Forms.Label();
            this.field_PowerUsage = new NiceHashMiner.Forms.Components.Field();
            this.fieldBoxBenchmarkSpeed = new NiceHashMiner.Forms.Components.Field();
            this.secondaryFieldBoxBenchmarkSpeed = new NiceHashMiner.Forms.Components.Field();
            this.groupBoxExtraLaunchParameters = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.richTextBoxExtraLaunchParameters = new System.Windows.Forms.RichTextBox();
            this.groupBoxSelectedAlgorithmSettings.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.groupBoxExtraLaunchParameters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBoxSelectedAlgorithmSettings
            // 
            this.groupBoxSelectedAlgorithmSettings.Controls.Add(this.flowLayoutPanel1);
            this.groupBoxSelectedAlgorithmSettings.Location = new System.Drawing.Point(3, 3);
            this.groupBoxSelectedAlgorithmSettings.Name = "groupBoxSelectedAlgorithmSettings";
            this.groupBoxSelectedAlgorithmSettings.Size = new System.Drawing.Size(229, 314);
            this.groupBoxSelectedAlgorithmSettings.TabIndex = 11;
            this.groupBoxSelectedAlgorithmSettings.TabStop = false;
            this.groupBoxSelectedAlgorithmSettings.Text = "Selected Algorithm Settings:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.labelSelectedAlgorithm);
            this.flowLayoutPanel1.Controls.Add(this.field_PowerUsage);
            this.flowLayoutPanel1.Controls.Add(this.fieldBoxBenchmarkSpeed);
            this.flowLayoutPanel1.Controls.Add(this.secondaryFieldBoxBenchmarkSpeed);
            this.flowLayoutPanel1.Controls.Add(this.groupBoxExtraLaunchParameters);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(223, 295);
            this.flowLayoutPanel1.TabIndex = 12;
            // 
            // labelSelectedAlgorithm
            // 
            this.labelSelectedAlgorithm.AutoSize = true;
            this.labelSelectedAlgorithm.Location = new System.Drawing.Point(3, 3);
            this.labelSelectedAlgorithm.Margin = new System.Windows.Forms.Padding(3, 3, 6, 0);
            this.labelSelectedAlgorithm.Name = "labelSelectedAlgorithm";
            this.labelSelectedAlgorithm.Size = new System.Drawing.Size(0, 13);
            this.labelSelectedAlgorithm.TabIndex = 17;
            // 
            // field_PowerUsage
            // 
            this.field_PowerUsage.AutoSize = true;
            this.field_PowerUsage.BackColor = System.Drawing.Color.Transparent;
            this.field_PowerUsage.EntryText = "";
            this.field_PowerUsage.LabelText = "Power Usage (W)";
            this.field_PowerUsage.Location = new System.Drawing.Point(3, 19);
            this.field_PowerUsage.Name = "field_PowerUsage";
            this.field_PowerUsage.Size = new System.Drawing.Size(220, 47);
            this.field_PowerUsage.TabIndex = 15;
            // 
            // fieldBoxBenchmarkSpeed
            // 
            this.fieldBoxBenchmarkSpeed.AutoSize = true;
            this.fieldBoxBenchmarkSpeed.BackColor = System.Drawing.Color.Transparent;
            this.fieldBoxBenchmarkSpeed.EntryText = "";
            this.fieldBoxBenchmarkSpeed.LabelText = "Benchmark Speed";
            this.fieldBoxBenchmarkSpeed.Location = new System.Drawing.Point(3, 72);
            this.fieldBoxBenchmarkSpeed.Name = "fieldBoxBenchmarkSpeed";
            this.fieldBoxBenchmarkSpeed.Size = new System.Drawing.Size(220, 47);
            this.fieldBoxBenchmarkSpeed.TabIndex = 1;
            // 
            // secondaryFieldBoxBenchmarkSpeed
            // 
            this.secondaryFieldBoxBenchmarkSpeed.AutoSize = true;
            this.secondaryFieldBoxBenchmarkSpeed.BackColor = System.Drawing.Color.Transparent;
            this.secondaryFieldBoxBenchmarkSpeed.EntryText = "";
            this.secondaryFieldBoxBenchmarkSpeed.LabelText = "Secondary Benchmark Speed";
            this.secondaryFieldBoxBenchmarkSpeed.Location = new System.Drawing.Point(3, 125);
            this.secondaryFieldBoxBenchmarkSpeed.Name = "secondaryFieldBoxBenchmarkSpeed";
            this.secondaryFieldBoxBenchmarkSpeed.Size = new System.Drawing.Size(220, 47);
            this.secondaryFieldBoxBenchmarkSpeed.TabIndex = 16;
            // 
            // groupBoxExtraLaunchParameters
            // 
            this.groupBoxExtraLaunchParameters.Controls.Add(this.pictureBox1);
            this.groupBoxExtraLaunchParameters.Controls.Add(this.richTextBoxExtraLaunchParameters);
            this.groupBoxExtraLaunchParameters.Location = new System.Drawing.Point(3, 178);
            this.groupBoxExtraLaunchParameters.Name = "groupBoxExtraLaunchParameters";
            this.groupBoxExtraLaunchParameters.Size = new System.Drawing.Size(217, 95);
            this.groupBoxExtraLaunchParameters.TabIndex = 14;
            this.groupBoxExtraLaunchParameters.TabStop = false;
            this.groupBoxExtraLaunchParameters.Text = "Extra Launch Parameters";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(193, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(18, 18);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // richTextBoxExtraLaunchParameters
            // 
            this.richTextBoxExtraLaunchParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxExtraLaunchParameters.Location = new System.Drawing.Point(3, 16);
            this.richTextBoxExtraLaunchParameters.Name = "richTextBoxExtraLaunchParameters";
            this.richTextBoxExtraLaunchParameters.Size = new System.Drawing.Size(211, 76);
            this.richTextBoxExtraLaunchParameters.TabIndex = 0;
            this.richTextBoxExtraLaunchParameters.Text = "";
            // 
            // AlgorithmSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxSelectedAlgorithmSettings);
            this.Name = "AlgorithmSettingsControl";
            this.Size = new System.Drawing.Size(235, 323);
            this.groupBoxSelectedAlgorithmSettings.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.groupBoxExtraLaunchParameters.ResumeLayout(false);
            this.groupBoxExtraLaunchParameters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxSelectedAlgorithmSettings;
        private System.Windows.Forms.GroupBox groupBoxExtraLaunchParameters;
        private System.Windows.Forms.RichTextBox richTextBoxExtraLaunchParameters;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private Field fieldBoxBenchmarkSpeed;
        private Field secondaryFieldBoxBenchmarkSpeed;
        private Field field_PowerUsage;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label labelSelectedAlgorithm;
    }
}
