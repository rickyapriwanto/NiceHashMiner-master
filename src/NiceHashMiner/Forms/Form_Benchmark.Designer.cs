﻿namespace NiceHashMiner.Forms {
    partial class Form_Benchmark {
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

#region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.StartStopBtn = new System.Windows.Forms.Button();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.groupBoxBenchmarkProgress = new System.Windows.Forms.GroupBox();
            this.labelBenchmarkSteps = new System.Windows.Forms.Label();
            this.progressBarBenchmarkSteps = new System.Windows.Forms.ProgressBar();
            this.radioButton_SelectedUnbenchmarked = new System.Windows.Forms.RadioButton();
            this.radioButton_RE_SelectedUnbenchmarked = new System.Windows.Forms.RadioButton();
            this.checkBox_StartMiningAfterBenchmark = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.algorithmSettingsControl1 = new NiceHashMiner.Forms.Components.AlgorithmSettingsControl();
            this.algorithmsListView1 = new NiceHashMiner.Forms.Components.AlgorithmsListView();
            this.benchmarkOptions1 = new NiceHashMiner.Forms.Components.BenchmarkOptions();
            this.devicesListViewEnableControl1 = new NiceHashMiner.Forms.Components.DevicesListViewBenchmarkControl();
            this.groupBoxBenchmarkProgress.SuspendLayout();
            this.SuspendLayout();
            // 
            // StartStopBtn
            // 
            this.StartStopBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StartStopBtn.Location = new System.Drawing.Point(697, 499);
            this.StartStopBtn.Name = "StartStopBtn";
            this.StartStopBtn.Size = new System.Drawing.Size(75, 23);
            this.StartStopBtn.TabIndex = 100;
            this.StartStopBtn.Text = "&Start";
            this.StartStopBtn.UseVisualStyleBackColor = true;
            this.StartStopBtn.Click += new System.EventHandler(this.StartStopBtn_Click);
            // 
            // CloseBtn
            // 
            this.CloseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseBtn.Location = new System.Drawing.Point(778, 499);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.Size = new System.Drawing.Size(75, 23);
            this.CloseBtn.TabIndex = 101;
            this.CloseBtn.Text = "&Close";
            this.CloseBtn.UseVisualStyleBackColor = true;
            this.CloseBtn.Click += new System.EventHandler(this.CloseBtn_Click);
            // 
            // groupBoxBenchmarkProgress
            // 
            this.groupBoxBenchmarkProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxBenchmarkProgress.Controls.Add(this.labelBenchmarkSteps);
            this.groupBoxBenchmarkProgress.Controls.Add(this.progressBarBenchmarkSteps);
            this.groupBoxBenchmarkProgress.Location = new System.Drawing.Point(12, 475);
            this.groupBoxBenchmarkProgress.Name = "groupBoxBenchmarkProgress";
            this.groupBoxBenchmarkProgress.Size = new System.Drawing.Size(418, 47);
            this.groupBoxBenchmarkProgress.TabIndex = 108;
            this.groupBoxBenchmarkProgress.TabStop = false;
            this.groupBoxBenchmarkProgress.Text = "Benchmark progress status:";
            // 
            // labelBenchmarkSteps
            // 
            this.labelBenchmarkSteps.AutoSize = true;
            this.labelBenchmarkSteps.Location = new System.Drawing.Point(6, 24);
            this.labelBenchmarkSteps.Name = "labelBenchmarkSteps";
            this.labelBenchmarkSteps.Size = new System.Drawing.Size(116, 13);
            this.labelBenchmarkSteps.TabIndex = 109;
            this.labelBenchmarkSteps.Text = "Benchmark step (0/10)";
            // 
            // progressBarBenchmarkSteps
            // 
            this.progressBarBenchmarkSteps.Location = new System.Drawing.Point(162, 16);
            this.progressBarBenchmarkSteps.Name = "progressBarBenchmarkSteps";
            this.progressBarBenchmarkSteps.Size = new System.Drawing.Size(161, 23);
            this.progressBarBenchmarkSteps.TabIndex = 108;
            // 
            // radioButton_SelectedUnbenchmarked
            // 
            this.radioButton_SelectedUnbenchmarked.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_SelectedUnbenchmarked.AutoSize = true;
            this.radioButton_SelectedUnbenchmarked.Checked = true;
            this.radioButton_SelectedUnbenchmarked.Location = new System.Drawing.Point(21, 427);
            this.radioButton_SelectedUnbenchmarked.Name = "radioButton_SelectedUnbenchmarked";
            this.radioButton_SelectedUnbenchmarked.Size = new System.Drawing.Size(257, 17);
            this.radioButton_SelectedUnbenchmarked.TabIndex = 110;
            this.radioButton_SelectedUnbenchmarked.TabStop = true;
            this.radioButton_SelectedUnbenchmarked.Text = "Benchmark Selected Unbenchmarked Algorithms";
            this.radioButton_SelectedUnbenchmarked.UseVisualStyleBackColor = true;
            this.radioButton_SelectedUnbenchmarked.CheckedChanged += new System.EventHandler(this.RadioButton_SelectedUnbenchmarked_CheckedChanged_1);
            // 
            // radioButton_RE_SelectedUnbenchmarked
            // 
            this.radioButton_RE_SelectedUnbenchmarked.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_RE_SelectedUnbenchmarked.AutoSize = true;
            this.radioButton_RE_SelectedUnbenchmarked.Location = new System.Drawing.Point(21, 450);
            this.radioButton_RE_SelectedUnbenchmarked.Name = "radioButton_RE_SelectedUnbenchmarked";
            this.radioButton_RE_SelectedUnbenchmarked.Size = new System.Drawing.Size(189, 17);
            this.radioButton_RE_SelectedUnbenchmarked.TabIndex = 110;
            this.radioButton_RE_SelectedUnbenchmarked.Text = "Benchmark All Selected Algorithms";
            this.radioButton_RE_SelectedUnbenchmarked.UseVisualStyleBackColor = true;
            this.radioButton_RE_SelectedUnbenchmarked.CheckedChanged += new System.EventHandler(this.RadioButton_RE_SelectedUnbenchmarked_CheckedChanged);
            // 
            // checkBox_StartMiningAfterBenchmark
            // 
            this.checkBox_StartMiningAfterBenchmark.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBox_StartMiningAfterBenchmark.AutoSize = true;
            this.checkBox_StartMiningAfterBenchmark.Location = new System.Drawing.Point(350, 450);
            this.checkBox_StartMiningAfterBenchmark.Name = "checkBox_StartMiningAfterBenchmark";
            this.checkBox_StartMiningAfterBenchmark.Size = new System.Drawing.Size(161, 17);
            this.checkBox_StartMiningAfterBenchmark.TabIndex = 111;
            this.checkBox_StartMiningAfterBenchmark.Text = "Start mining after benchmark";
            this.checkBox_StartMiningAfterBenchmark.UseVisualStyleBackColor = true;
            this.checkBox_StartMiningAfterBenchmark.CheckedChanged += new System.EventHandler(this.CheckBox_StartMiningAfterBenchmark_CheckedChanged);
            // 
            // algorithmSettingsControl1
            // 
            this.algorithmSettingsControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.algorithmSettingsControl1.IsInBenchmark = false;
            this.algorithmSettingsControl1.Location = new System.Drawing.Point(620, 133);
            this.algorithmSettingsControl1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.algorithmSettingsControl1.Name = "algorithmSettingsControl1";
            this.algorithmSettingsControl1.Size = new System.Drawing.Size(238, 311);
            this.algorithmSettingsControl1.TabIndex = 397;
            // 
            // algorithmsListView1
            // 
            this.algorithmsListView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.algorithmsListView1.BackColor = System.Drawing.SystemColors.Control;
            this.algorithmsListView1.BenchmarkCalculation = null;
            this.algorithmsListView1.ComunicationInterface = null;
            this.algorithmsListView1.IsInBenchmark = false;
            this.algorithmsListView1.Location = new System.Drawing.Point(12, 133);
            this.algorithmsListView1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.algorithmsListView1.Name = "algorithmsListView1";
            this.algorithmsListView1.Size = new System.Drawing.Size(600, 288);
            this.algorithmsListView1.TabIndex = 109;
            // 
            // benchmarkOptions1
            // 
            this.benchmarkOptions1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.benchmarkOptions1.Location = new System.Drawing.Point(616, 13);
            this.benchmarkOptions1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.benchmarkOptions1.Name = "benchmarkOptions1";
            this.benchmarkOptions1.Size = new System.Drawing.Size(208, 112);
            this.benchmarkOptions1.TabIndex = 106;
            // 
            // devicesListViewEnableControl1
            // 
            this.devicesListViewEnableControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.devicesListViewEnableControl1.BenchmarkCalculation = null;
            this.devicesListViewEnableControl1.FirstColumnText = "Benckmark";
            this.devicesListViewEnableControl1.IsInBenchmark = false;
            this.devicesListViewEnableControl1.IsMining = false;
            this.devicesListViewEnableControl1.Location = new System.Drawing.Point(12, 15);
            this.devicesListViewEnableControl1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.devicesListViewEnableControl1.Name = "devicesListViewEnableControl1";
            this.devicesListViewEnableControl1.SaveToGeneralConfig = false;
            this.devicesListViewEnableControl1.Size = new System.Drawing.Size(600, 112);
            this.devicesListViewEnableControl1.TabIndex = 0;
            // 
            // Form_Benchmark
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(865, 534);
            this.Controls.Add(this.algorithmSettingsControl1);
            this.Controls.Add(this.checkBox_StartMiningAfterBenchmark);
            this.Controls.Add(this.radioButton_RE_SelectedUnbenchmarked);
            this.Controls.Add(this.radioButton_SelectedUnbenchmarked);
            this.Controls.Add(this.algorithmsListView1);
            this.Controls.Add(this.groupBoxBenchmarkProgress);
            this.Controls.Add(this.benchmarkOptions1);
            this.Controls.Add(this.StartStopBtn);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.devicesListViewEnableControl1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Benchmark";
            this.Text = "Benchmark";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormBenchmark_New_FormClosing);
            this.groupBoxBenchmarkProgress.ResumeLayout(false);
            this.groupBoxBenchmarkProgress.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private Components.DevicesListViewBenchmarkControl devicesListViewEnableControl1;
        private System.Windows.Forms.Button StartStopBtn;
        private System.Windows.Forms.Button CloseBtn;
        private Components.BenchmarkOptions benchmarkOptions1;
        private System.Windows.Forms.GroupBox groupBoxBenchmarkProgress;
        private System.Windows.Forms.Label labelBenchmarkSteps;
        private System.Windows.Forms.ProgressBar progressBarBenchmarkSteps;
        private Components.AlgorithmsListView algorithmsListView1;
        private System.Windows.Forms.RadioButton radioButton_SelectedUnbenchmarked;
        private System.Windows.Forms.RadioButton radioButton_RE_SelectedUnbenchmarked;
        private System.Windows.Forms.CheckBox checkBox_StartMiningAfterBenchmark;
        private Components.AlgorithmSettingsControl algorithmSettingsControl1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
