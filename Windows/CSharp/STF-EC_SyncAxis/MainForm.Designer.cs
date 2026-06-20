namespace STF_EC_SyncAxis
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
            this.lblMode = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblAxisCount = new System.Windows.Forms.Label();

            // ── 虚拟主轴 ──
            this.grpMaster = new System.Windows.Forms.GroupBox();
            this.lblMasterSpeed = new System.Windows.Forms.Label();
            this.numMasterSpeed = new System.Windows.Forms.NumericUpDown();
            this.btnSyncStart = new System.Windows.Forms.Button();
            this.btnSyncStop = new System.Windows.Forms.Button();
            this.btnMasterJogRev = new System.Windows.Forms.Button();
            this.btnMasterJogFwd = new System.Windows.Forms.Button();
            this.lblMasterPos = new System.Windows.Forms.Label();

            // ── 轴总览表 ──
            this.grpOverview = new System.Windows.Forms.GroupBox();
            this.gridAxes = new System.Windows.Forms.DataGridView();

            // ── 全局控制 ──
            this.grpGlobal = new System.Windows.Forms.GroupBox();
            this.btnAllEnable = new System.Windows.Forms.Button();
            this.btnAllDisable = new System.Windows.Forms.Button();
            this.btnAllFaultReset = new System.Windows.Forms.Button();

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

            ((System.ComponentModel.ISupportInitialize)(this.numMasterSpeed)).BeginInit();
            this.grpMaster.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).BeginInit();
            this.grpOverview.SuspendLayout();
            this.pnlAlarmBanner.SuspendLayout();
            this.grpAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).BeginInit();
            this.grpGlobal.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            // lblMode
            this.lblMode.AutoSize = true;
            this.lblMode.ForeColor = System.Drawing.Color.DimGray;
            this.lblMode.Location = new System.Drawing.Point(14, 17);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(220, 17);
            this.lblMode.Text = "模式: CSP 周期同步位置 (Mode=8)";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(280, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(84, 28);
            this.btnConnect.Text = "连接";

            // btnDisconnect
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(370, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(84, 28);
            this.btnDisconnect.Text = "断开";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(470, 17);
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
            this.grpMaster.Controls.Add(this.lblMasterSpeed);
            this.grpMaster.Controls.Add(this.numMasterSpeed);
            this.grpMaster.Controls.Add(this.btnSyncStart);
            this.grpMaster.Controls.Add(this.btnSyncStop);
            this.grpMaster.Controls.Add(this.btnMasterJogRev);
            this.grpMaster.Controls.Add(this.btnMasterJogFwd);
            this.grpMaster.Controls.Add(this.lblMasterPos);
            this.grpMaster.Enabled = false;
            this.grpMaster.Location = new System.Drawing.Point(12, 50);
            this.grpMaster.Name = "grpMaster";
            this.grpMaster.Size = new System.Drawing.Size(956, 78);
            this.grpMaster.Text = "虚拟主轴 (各轴按齿比跟随; 齿比 1:1 = 同一数据点同时映射, 齿比≠1 = 电子齿轮)";

            // lblMasterSpeed
            this.lblMasterSpeed.AutoSize = true;
            this.lblMasterSpeed.Location = new System.Drawing.Point(14, 31);
            this.lblMasterSpeed.Name = "lblMasterSpeed";
            this.lblMasterSpeed.Size = new System.Drawing.Size(86, 17);
            this.lblMasterSpeed.Text = "速度(脉冲/秒):";

            // numMasterSpeed
            this.numMasterSpeed.Location = new System.Drawing.Point(108, 28);
            this.numMasterSpeed.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            this.numMasterSpeed.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numMasterSpeed.Name = "numMasterSpeed";
            this.numMasterSpeed.Size = new System.Drawing.Size(120, 23);
            this.numMasterSpeed.ThousandsSeparator = true;
            this.numMasterSpeed.Value = new decimal(new int[] { 10000, 0, 0, 0 });

            // btnSyncStart
            this.btnSyncStart.BackColor = System.Drawing.Color.Honeydew;
            this.btnSyncStart.Location = new System.Drawing.Point(248, 26);
            this.btnSyncStart.Name = "btnSyncStart";
            this.btnSyncStart.Size = new System.Drawing.Size(96, 30);
            this.btnSyncStart.Text = "启动同步";
            this.btnSyncStart.UseVisualStyleBackColor = false;

            // btnSyncStop
            this.btnSyncStop.BackColor = System.Drawing.Color.MistyRose;
            this.btnSyncStop.Location = new System.Drawing.Point(350, 26);
            this.btnSyncStop.Name = "btnSyncStop";
            this.btnSyncStop.Size = new System.Drawing.Size(84, 30);
            this.btnSyncStop.Text = "停止";
            this.btnSyncStop.UseVisualStyleBackColor = false;

            // btnMasterJogRev
            this.btnMasterJogRev.Location = new System.Drawing.Point(456, 26);
            this.btnMasterJogRev.Name = "btnMasterJogRev";
            this.btnMasterJogRev.Size = new System.Drawing.Size(90, 30);
            this.btnMasterJogRev.Text = "◀ 主轴-";

            // btnMasterJogFwd
            this.btnMasterJogFwd.Location = new System.Drawing.Point(550, 26);
            this.btnMasterJogFwd.Name = "btnMasterJogFwd";
            this.btnMasterJogFwd.Size = new System.Drawing.Size(90, 30);
            this.btnMasterJogFwd.Text = "主轴+ ▶";

            // lblMasterPos
            this.lblMasterPos.AutoSize = true;
            this.lblMasterPos.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMasterPos.ForeColor = System.Drawing.Color.DimGray;
            this.lblMasterPos.Location = new System.Drawing.Point(660, 33);
            this.lblMasterPos.Name = "lblMasterPos";
            this.lblMasterPos.Size = new System.Drawing.Size(80, 17);
            this.lblMasterPos.Text = "主轴位置: -";

            // grpOverview
            this.grpOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOverview.Controls.Add(this.gridAxes);
            this.grpOverview.Location = new System.Drawing.Point(12, 134);
            this.grpOverview.Name = "grpOverview";
            this.grpOverview.Size = new System.Drawing.Size(956, 300);
            this.grpOverview.Text = "轴总览 (双击「齿比」列可改; 勾选「选」列指定全局操作目标, 未勾选 = 全部; 出问题的轴整行红)";

            // gridAxes
            this.gridAxes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.gridAxes.AllowUserToAddRows = false;
            this.gridAxes.AllowUserToDeleteRows = false;
            this.gridAxes.AllowUserToResizeRows = false;
            this.gridAxes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAxes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystrokeOrF2;
            this.gridAxes.Location = new System.Drawing.Point(10, 22);
            this.gridAxes.MultiSelect = false;
            this.gridAxes.Name = "gridAxes";
            this.gridAxes.RowHeadersVisible = false;
            this.gridAxes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.gridAxes.Size = new System.Drawing.Size(936, 268);

            // pnlAlarmBanner (报警状态横幅)
            this.pnlAlarmBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAlarmBanner.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            this.pnlAlarmBanner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmBanner);
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmCount);
            this.pnlAlarmBanner.Controls.Add(this.btnAlarmAck);
            this.pnlAlarmBanner.Location = new System.Drawing.Point(12, 440);
            this.pnlAlarmBanner.Name = "pnlAlarmBanner";
            this.pnlAlarmBanner.Size = new System.Drawing.Size(956, 34);

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
            this.lblAlarmCount.Location = new System.Drawing.Point(636, 8);
            this.lblAlarmCount.Name = "lblAlarmCount";
            this.lblAlarmCount.Size = new System.Drawing.Size(200, 18);
            this.lblAlarmCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAlarmCount.Text = "激活:0  故障:0  历史:0";

            // btnAlarmAck
            this.btnAlarmAck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAlarmAck.Enabled = false;
            this.btnAlarmAck.Location = new System.Drawing.Point(846, 3);
            this.btnAlarmAck.Name = "btnAlarmAck";
            this.btnAlarmAck.Size = new System.Drawing.Size(100, 27);
            this.btnAlarmAck.Text = "报警复位";

            // grpAlarm
            this.grpAlarm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpAlarm.Controls.Add(this.gridAlarms);
            this.grpAlarm.Location = new System.Drawing.Point(12, 480);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(956, 148);
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
            this.gridAlarms.Size = new System.Drawing.Size(950, 126);

            // grpGlobal
            this.grpGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGlobal.Controls.Add(this.btnAllEnable);
            this.grpGlobal.Controls.Add(this.btnAllDisable);
            this.grpGlobal.Controls.Add(this.btnAllFaultReset);
            this.grpGlobal.Enabled = false;
            this.grpGlobal.Location = new System.Drawing.Point(12, 634);
            this.grpGlobal.Name = "grpGlobal";
            this.grpGlobal.Size = new System.Drawing.Size(956, 70);
            this.grpGlobal.Text = "全局控制 (作用于勾选轴; 未勾选 = 全部轴)";

            // btnAllEnable
            this.btnAllEnable.Location = new System.Drawing.Point(14, 26);
            this.btnAllEnable.Name = "btnAllEnable";
            this.btnAllEnable.Size = new System.Drawing.Size(110, 32);
            this.btnAllEnable.Text = "全部使能";

            // btnAllDisable
            this.btnAllDisable.BackColor = System.Drawing.Color.MistyRose;
            this.btnAllDisable.Location = new System.Drawing.Point(132, 26);
            this.btnAllDisable.Name = "btnAllDisable";
            this.btnAllDisable.Size = new System.Drawing.Size(150, 32);
            this.btnAllDisable.Text = "全部去使能 (急停)";
            this.btnAllDisable.UseVisualStyleBackColor = false;

            // btnAllFaultReset
            this.btnAllFaultReset.Location = new System.Drawing.Point(290, 26);
            this.btnAllFaultReset.Name = "btnAllFaultReset";
            this.btnAllFaultReset.Size = new System.Drawing.Size(110, 32);
            this.btnAllFaultReset.Text = "全部故障复位";

            // grpLog
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 710);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(956, 92);
            this.grpLog.Text = "日志";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 19);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(950, 102);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 810);
            this.Controls.Add(this.lblMode);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblAxisCount);
            this.Controls.Add(this.grpMaster);
            this.Controls.Add(this.grpOverview);
            this.Controls.Add(this.pnlAlarmBanner);
            this.Controls.Add(this.grpAlarm);
            this.Controls.Add(this.grpGlobal);
            this.Controls.Add(this.grpLog);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 750);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "STF-EC 同步轴/电子齿轮  by Darra EtherCAT SDK";

            ((System.ComponentModel.ISupportInitialize)(this.numMasterSpeed)).EndInit();
            this.grpMaster.ResumeLayout(false);
            this.grpMaster.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).EndInit();
            this.grpOverview.ResumeLayout(false);
            this.pnlAlarmBanner.ResumeLayout(false);
            this.grpAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).EndInit();
            this.grpGlobal.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAxisCount;

        private System.Windows.Forms.GroupBox grpMaster;
        private System.Windows.Forms.Label lblMasterSpeed;
        private System.Windows.Forms.NumericUpDown numMasterSpeed;
        private System.Windows.Forms.Button btnSyncStart;
        private System.Windows.Forms.Button btnSyncStop;
        private System.Windows.Forms.Button btnMasterJogRev;
        private System.Windows.Forms.Button btnMasterJogFwd;
        private System.Windows.Forms.Label lblMasterPos;

        private System.Windows.Forms.GroupBox grpOverview;
        private System.Windows.Forms.DataGridView gridAxes;

        private System.Windows.Forms.GroupBox grpGlobal;
        private System.Windows.Forms.Button btnAllEnable;
        private System.Windows.Forms.Button btnAllDisable;
        private System.Windows.Forms.Button btnAllFaultReset;

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
