namespace STF_EC_ECam
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent()
        {
            // ── 顶部连接栏 ──
            this.lblModeTitle = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblAxisCount = new System.Windows.Forms.Label();

            // ── 虚拟主轴 ──
            this.grpMaster = new System.Windows.Forms.GroupBox();
            this.lblMasterRpm = new System.Windows.Forms.Label();
            this.numMasterRpm = new System.Windows.Forms.NumericUpDown();
            this.btnCamStart = new System.Windows.Forms.Button();
            this.btnCamStop = new System.Windows.Forms.Button();
            this.btnResetPhase = new System.Windows.Forms.Button();
            this.lblMasterPhaseTitle = new System.Windows.Forms.Label();
            this.lblMasterPhase = new System.Windows.Forms.Label();

            // ── 凸轮设置 ──
            this.grpCam = new System.Windows.Forms.GroupBox();
            this.lblCurve = new System.Windows.Forms.Label();
            this.cboCurve = new System.Windows.Forms.ComboBox();
            this.lblAmplitude = new System.Windows.Forms.Label();
            this.numAmplitude = new System.Windows.Forms.NumericUpDown();
            this.lblPreviewTitle = new System.Windows.Forms.Label();
            this.pnlCamPreview = new System.Windows.Forms.Panel();

            // ── 轴总览表 ──
            this.grpOverview = new System.Windows.Forms.GroupBox();
            this.gridAxes = new System.Windows.Forms.DataGridView();

            // ── 全局控制 ──
            this.grpGlobal = new System.Windows.Forms.GroupBox();
            this.lblGlobalHint = new System.Windows.Forms.Label();
            this.btnAllEnable = new System.Windows.Forms.Button();
            this.btnAllDisable = new System.Windows.Forms.Button();
            this.btnAllFaultReset = new System.Windows.Forms.Button();
            this.btnAllEngage = new System.Windows.Forms.Button();
            this.btnAllDisengage = new System.Windows.Forms.Button();

            // ── 报警 / 诊断 ──
            this.pnlAlarmBanner = new System.Windows.Forms.Panel();
            this.lblAlarmBanner = new System.Windows.Forms.Label();
            this.lblAlarmCount = new System.Windows.Forms.Label();
            this.btnAlarmAck = new System.Windows.Forms.Button();
            this.grpAlarm = new System.Windows.Forms.GroupBox();
            this.gridAlarms = new System.Windows.Forms.DataGridView();

            // ── 日志 ──
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();

            ((System.ComponentModel.ISupportInitialize)(this.numMasterRpm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAmplitude)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).BeginInit();
            this.grpMaster.SuspendLayout();
            this.grpCam.SuspendLayout();
            this.grpOverview.SuspendLayout();
            this.grpGlobal.SuspendLayout();
            this.pnlAlarmBanner.SuspendLayout();
            this.grpAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).BeginInit();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            // lblModeTitle
            this.lblModeTitle.AutoSize = true;
            this.lblModeTitle.ForeColor = System.Drawing.Color.DimGray;
            this.lblModeTitle.Location = new System.Drawing.Point(14, 17);
            this.lblModeTitle.Name = "lblModeTitle";
            this.lblModeTitle.Size = new System.Drawing.Size(200, 17);
            this.lblModeTitle.Text = "模式: CSP 周期同步位置 (Mode=8)";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(300, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(84, 28);
            this.btnConnect.Text = "连接";

            // btnDisconnect
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(390, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(84, 28);
            this.btnDisconnect.Text = "断开";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(490, 17);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(44, 17);
            this.lblStatus.Text = "未连接";

            // lblAxisCount
            this.lblAxisCount.AutoSize = true;
            this.lblAxisCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblAxisCount.Location = new System.Drawing.Point(720, 17);
            this.lblAxisCount.Name = "lblAxisCount";
            this.lblAxisCount.Size = new System.Drawing.Size(56, 17);
            this.lblAxisCount.Text = "轴数: -";

            // grpMaster
            this.grpMaster.Controls.Add(this.lblMasterRpm);
            this.grpMaster.Controls.Add(this.numMasterRpm);
            this.grpMaster.Controls.Add(this.btnCamStart);
            this.grpMaster.Controls.Add(this.btnCamStop);
            this.grpMaster.Controls.Add(this.btnResetPhase);
            this.grpMaster.Controls.Add(this.lblMasterPhaseTitle);
            this.grpMaster.Controls.Add(this.lblMasterPhase);
            this.grpMaster.Enabled = false;
            this.grpMaster.Location = new System.Drawing.Point(12, 50);
            this.grpMaster.Name = "grpMaster";
            this.grpMaster.Size = new System.Drawing.Size(610, 96);
            this.grpMaster.Text = "虚拟主轴 (Electronic Cam Master)";

            // lblMasterRpm
            this.lblMasterRpm.AutoSize = true;
            this.lblMasterRpm.Location = new System.Drawing.Point(14, 33);
            this.lblMasterRpm.Name = "lblMasterRpm";
            this.lblMasterRpm.Size = new System.Drawing.Size(72, 17);
            this.lblMasterRpm.Text = "主轴(RPM):";

            // numMasterRpm
            this.numMasterRpm.Location = new System.Drawing.Point(92, 30);
            this.numMasterRpm.Maximum = new decimal(new int[] { 6000, 0, 0, 0 });
            this.numMasterRpm.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numMasterRpm.Name = "numMasterRpm";
            this.numMasterRpm.Size = new System.Drawing.Size(90, 23);
            this.numMasterRpm.Value = new decimal(new int[] { 60, 0, 0, 0 });

            // btnCamStart
            this.btnCamStart.BackColor = System.Drawing.Color.Honeydew;
            this.btnCamStart.Location = new System.Drawing.Point(14, 58);
            this.btnCamStart.Name = "btnCamStart";
            this.btnCamStart.Size = new System.Drawing.Size(96, 30);
            this.btnCamStart.Text = "启动凸轮";
            this.btnCamStart.UseVisualStyleBackColor = false;

            // btnCamStop
            this.btnCamStop.BackColor = System.Drawing.Color.MistyRose;
            this.btnCamStop.Location = new System.Drawing.Point(116, 58);
            this.btnCamStop.Name = "btnCamStop";
            this.btnCamStop.Size = new System.Drawing.Size(84, 30);
            this.btnCamStop.Text = "停止";
            this.btnCamStop.UseVisualStyleBackColor = false;

            // btnResetPhase
            this.btnResetPhase.Location = new System.Drawing.Point(206, 58);
            this.btnResetPhase.Name = "btnResetPhase";
            this.btnResetPhase.Size = new System.Drawing.Size(96, 30);
            this.btnResetPhase.Text = "相位归零";

            // lblMasterPhaseTitle
            this.lblMasterPhaseTitle.AutoSize = true;
            this.lblMasterPhaseTitle.Location = new System.Drawing.Point(330, 33);
            this.lblMasterPhaseTitle.Name = "lblMasterPhaseTitle";
            this.lblMasterPhaseTitle.Size = new System.Drawing.Size(68, 17);
            this.lblMasterPhaseTitle.Text = "主轴相位:";

            // lblMasterPhase
            this.lblMasterPhase.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblMasterPhase.ForeColor = System.Drawing.Color.RoyalBlue;
            this.lblMasterPhase.Location = new System.Drawing.Point(330, 53);
            this.lblMasterPhase.Name = "lblMasterPhase";
            this.lblMasterPhase.Size = new System.Drawing.Size(270, 32);
            this.lblMasterPhase.Text = "0.0 °";
            this.lblMasterPhase.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // grpCam
            this.grpCam.Controls.Add(this.lblCurve);
            this.grpCam.Controls.Add(this.cboCurve);
            this.grpCam.Controls.Add(this.lblAmplitude);
            this.grpCam.Controls.Add(this.numAmplitude);
            this.grpCam.Controls.Add(this.lblPreviewTitle);
            this.grpCam.Controls.Add(this.pnlCamPreview);
            this.grpCam.Location = new System.Drawing.Point(632, 50);
            this.grpCam.Name = "grpCam";
            this.grpCam.Size = new System.Drawing.Size(340, 252);
            this.grpCam.Text = "凸轮设置 (Cam Profile)";

            // lblCurve
            this.lblCurve.AutoSize = true;
            this.lblCurve.Location = new System.Drawing.Point(14, 30);
            this.lblCurve.Name = "lblCurve";
            this.lblCurve.Size = new System.Drawing.Size(56, 17);
            this.lblCurve.Text = "曲线类型:";

            // cboCurve
            this.cboCurve.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCurve.Location = new System.Drawing.Point(80, 27);
            this.cboCurve.Name = "cboCurve";
            this.cboCurve.Size = new System.Drawing.Size(240, 25);
            this.cboCurve.Items.AddRange(new object[] {
                "正弦 Sine",
                "摆线 Cycloidal",
                "直线/电子齿轮"});

            // lblAmplitude
            this.lblAmplitude.AutoSize = true;
            this.lblAmplitude.Location = new System.Drawing.Point(14, 64);
            this.lblAmplitude.Name = "lblAmplitude";
            this.lblAmplitude.Size = new System.Drawing.Size(68, 17);
            this.lblAmplitude.Text = "行程(脉冲):";

            // numAmplitude
            this.numAmplitude.Location = new System.Drawing.Point(90, 61);
            this.numAmplitude.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            this.numAmplitude.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numAmplitude.Name = "numAmplitude";
            this.numAmplitude.Size = new System.Drawing.Size(140, 23);
            this.numAmplitude.Value = new decimal(new int[] { 10000, 0, 0, 0 });

            // lblPreviewTitle
            this.lblPreviewTitle.AutoSize = true;
            this.lblPreviewTitle.ForeColor = System.Drawing.Color.DimGray;
            this.lblPreviewTitle.Location = new System.Drawing.Point(14, 94);
            this.lblPreviewTitle.Name = "lblPreviewTitle";
            this.lblPreviewTitle.Size = new System.Drawing.Size(220, 17);
            this.lblPreviewTitle.Text = "曲线预览 (从轴位移 = f(主轴相位)):";

            // pnlCamPreview
            this.pnlCamPreview.BackColor = System.Drawing.Color.White;
            this.pnlCamPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlCamPreview.Location = new System.Drawing.Point(50, 116);
            this.pnlCamPreview.Name = "pnlCamPreview";
            this.pnlCamPreview.Size = new System.Drawing.Size(240, 110);

            // grpOverview
            this.grpOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOverview.Controls.Add(this.gridAxes);
            this.grpOverview.Location = new System.Drawing.Point(12, 308);
            this.grpOverview.Name = "grpOverview";
            this.grpOverview.Size = new System.Drawing.Size(960, 252);
            this.grpOverview.Text = "轴总览 (勾选「选」列指定全局操作目标; 勾选「挂载」让该轴跟随凸轮; 相位偏移° 可编辑)";

            // gridAxes
            this.gridAxes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.gridAxes.AllowUserToAddRows = false;
            this.gridAxes.AllowUserToDeleteRows = false;
            this.gridAxes.AllowUserToResizeRows = false;
            this.gridAxes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAxes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridAxes.Location = new System.Drawing.Point(10, 22);
            this.gridAxes.MultiSelect = false;
            this.gridAxes.Name = "gridAxes";
            this.gridAxes.RowHeadersVisible = false;
            this.gridAxes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAxes.Size = new System.Drawing.Size(940, 220);

            // grpGlobal
            this.grpGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGlobal.Controls.Add(this.lblGlobalHint);
            this.grpGlobal.Controls.Add(this.btnAllEnable);
            this.grpGlobal.Controls.Add(this.btnAllDisable);
            this.grpGlobal.Controls.Add(this.btnAllFaultReset);
            this.grpGlobal.Controls.Add(this.btnAllEngage);
            this.grpGlobal.Controls.Add(this.btnAllDisengage);
            this.grpGlobal.Enabled = false;
            this.grpGlobal.Location = new System.Drawing.Point(12, 760);
            this.grpGlobal.Name = "grpGlobal";
            this.grpGlobal.Size = new System.Drawing.Size(960, 70);
            this.grpGlobal.Text = "全局控制 (作用于勾选轴; 未勾选 = 全部轴)";

            // btnAllEnable
            this.btnAllEnable.Location = new System.Drawing.Point(14, 26);
            this.btnAllEnable.Name = "btnAllEnable";
            this.btnAllEnable.Size = new System.Drawing.Size(96, 32);
            this.btnAllEnable.Text = "全部使能";

            // btnAllDisable
            this.btnAllDisable.Location = new System.Drawing.Point(118, 26);
            this.btnAllDisable.Name = "btnAllDisable";
            this.btnAllDisable.Size = new System.Drawing.Size(96, 32);
            this.btnAllDisable.Text = "全部去使能";

            // btnAllFaultReset
            this.btnAllFaultReset.Location = new System.Drawing.Point(222, 26);
            this.btnAllFaultReset.Name = "btnAllFaultReset";
            this.btnAllFaultReset.Size = new System.Drawing.Size(96, 32);
            this.btnAllFaultReset.Text = "全部故障复位";

            // btnAllEngage
            this.btnAllEngage.BackColor = System.Drawing.Color.Honeydew;
            this.btnAllEngage.Location = new System.Drawing.Point(340, 26);
            this.btnAllEngage.Name = "btnAllEngage";
            this.btnAllEngage.Size = new System.Drawing.Size(96, 32);
            this.btnAllEngage.Text = "全部挂载";
            this.btnAllEngage.UseVisualStyleBackColor = false;

            // btnAllDisengage
            this.btnAllDisengage.Location = new System.Drawing.Point(444, 26);
            this.btnAllDisengage.Name = "btnAllDisengage";
            this.btnAllDisengage.Size = new System.Drawing.Size(96, 32);
            this.btnAllDisengage.Text = "全部脱开";

            // lblGlobalHint
            this.lblGlobalHint.AutoSize = true;
            this.lblGlobalHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblGlobalHint.Location = new System.Drawing.Point(560, 34);
            this.lblGlobalHint.Name = "lblGlobalHint";
            this.lblGlobalHint.Size = new System.Drawing.Size(0, 17);

            // pnlAlarmBanner (报警状态横幅)
            this.pnlAlarmBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAlarmBanner.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            this.pnlAlarmBanner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmBanner);
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmCount);
            this.pnlAlarmBanner.Controls.Add(this.btnAlarmAck);
            this.pnlAlarmBanner.Location = new System.Drawing.Point(12, 566);
            this.pnlAlarmBanner.Name = "pnlAlarmBanner";
            this.pnlAlarmBanner.Size = new System.Drawing.Size(960, 34);

            // lblAlarmBanner
            this.lblAlarmBanner.AutoSize = false;
            this.lblAlarmBanner.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAlarmBanner.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmBanner.Location = new System.Drawing.Point(8, 6);
            this.lblAlarmBanner.Name = "lblAlarmBanner";
            this.lblAlarmBanner.Size = new System.Drawing.Size(620, 22);
            this.lblAlarmBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAlarmBanner.Text = "● 未连接";

            // lblAlarmCount
            this.lblAlarmCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAlarmCount.AutoSize = false;
            this.lblAlarmCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmCount.Location = new System.Drawing.Point(640, 8);
            this.lblAlarmCount.Name = "lblAlarmCount";
            this.lblAlarmCount.Size = new System.Drawing.Size(200, 18);
            this.lblAlarmCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAlarmCount.Text = "激活:0  故障:0  历史:0";

            // btnAlarmAck
            this.btnAlarmAck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAlarmAck.Enabled = false;
            this.btnAlarmAck.Location = new System.Drawing.Point(850, 3);
            this.btnAlarmAck.Name = "btnAlarmAck";
            this.btnAlarmAck.Size = new System.Drawing.Size(100, 27);
            this.btnAlarmAck.Text = "报警复位";

            // grpAlarm
            this.grpAlarm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpAlarm.Controls.Add(this.gridAlarms);
            this.grpAlarm.Location = new System.Drawing.Point(12, 606);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(960, 148);
            this.grpAlarm.Text = "报警 / 诊断";

            // gridAlarms
            this.gridAlarms.AllowUserToAddRows = false;
            this.gridAlarms.AllowUserToDeleteRows = false;
            this.gridAlarms.AllowUserToResizeRows = false;
            this.gridAlarms.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAlarms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAlarms.Location = new System.Drawing.Point(3, 19);
            this.gridAlarms.MultiSelect = false;
            this.gridAlarms.Name = "gridAlarms";
            this.gridAlarms.ReadOnly = true;
            this.gridAlarms.RowHeadersVisible = false;
            this.gridAlarms.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAlarms.Size = new System.Drawing.Size(954, 126);

            // grpLog
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 836);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(960, 86);
            this.grpLog.Text = "日志";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 19);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(954, 64);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 934);
            this.Controls.Add(this.lblModeTitle);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblAxisCount);
            this.Controls.Add(this.grpMaster);
            this.Controls.Add(this.grpCam);
            this.Controls.Add(this.grpOverview);
            this.Controls.Add(this.pnlAlarmBanner);
            this.Controls.Add(this.grpAlarm);
            this.Controls.Add(this.grpGlobal);
            this.Controls.Add(this.grpLog);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 894);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "STF-EC 电子凸轮  by Darra EtherCAT SDK";

            ((System.ComponentModel.ISupportInitialize)(this.numMasterRpm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAmplitude)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).EndInit();
            this.grpMaster.ResumeLayout(false);
            this.grpMaster.PerformLayout();
            this.grpCam.ResumeLayout(false);
            this.grpCam.PerformLayout();
            this.grpOverview.ResumeLayout(false);
            this.grpGlobal.ResumeLayout(false);
            this.grpGlobal.PerformLayout();
            this.pnlAlarmBanner.ResumeLayout(false);
            this.grpAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).EndInit();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblModeTitle;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAxisCount;

        private System.Windows.Forms.GroupBox grpMaster;
        private System.Windows.Forms.Label lblMasterRpm;
        private System.Windows.Forms.NumericUpDown numMasterRpm;
        private System.Windows.Forms.Button btnCamStart;
        private System.Windows.Forms.Button btnCamStop;
        private System.Windows.Forms.Button btnResetPhase;
        private System.Windows.Forms.Label lblMasterPhaseTitle;
        private System.Windows.Forms.Label lblMasterPhase;

        private System.Windows.Forms.GroupBox grpCam;
        private System.Windows.Forms.Label lblCurve;
        private System.Windows.Forms.ComboBox cboCurve;
        private System.Windows.Forms.Label lblAmplitude;
        private System.Windows.Forms.NumericUpDown numAmplitude;
        private System.Windows.Forms.Label lblPreviewTitle;
        private System.Windows.Forms.Panel pnlCamPreview;

        private System.Windows.Forms.GroupBox grpOverview;
        private System.Windows.Forms.DataGridView gridAxes;

        private System.Windows.Forms.GroupBox grpGlobal;
        private System.Windows.Forms.Label lblGlobalHint;
        private System.Windows.Forms.Button btnAllEnable;
        private System.Windows.Forms.Button btnAllDisable;
        private System.Windows.Forms.Button btnAllFaultReset;
        private System.Windows.Forms.Button btnAllEngage;
        private System.Windows.Forms.Button btnAllDisengage;

        private System.Windows.Forms.Panel pnlAlarmBanner;
        private System.Windows.Forms.Label lblAlarmBanner;
        private System.Windows.Forms.Label lblAlarmCount;
        private System.Windows.Forms.Button btnAlarmAck;
        private System.Windows.Forms.GroupBox grpAlarm;
        private System.Windows.Forms.DataGridView gridAlarms;

        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox txtLog;
    }
}
