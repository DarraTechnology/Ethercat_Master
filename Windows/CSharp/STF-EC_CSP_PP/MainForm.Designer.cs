namespace STF_EC_CSP_PP
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
            this.cboMode = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblAxisCount = new System.Windows.Forms.Label();

            // ── 轴总览表 ──
            this.grpOverview = new System.Windows.Forms.GroupBox();
            this.gridAxes = new System.Windows.Forms.DataGridView();

            // ── 全局控制 ──
            this.grpGlobal = new System.Windows.Forms.GroupBox();
            this.lblGlobalHint = new System.Windows.Forms.Label();
            this.btnAllEnable = new System.Windows.Forms.Button();
            this.btnAllDisable = new System.Windows.Forms.Button();
            this.btnAllFaultReset = new System.Windows.Forms.Button();
            this.btnAllHome = new System.Windows.Forms.Button();
            this.btnAllEStop = new System.Windows.Forms.Button();

            // ── 选定轴控制 ──
            this.grpAxis = new System.Windows.Forms.GroupBox();
            this.lblSelAxis = new System.Windows.Forms.Label();
            this.btnEnable = new System.Windows.Forms.Button();
            this.btnDisable = new System.Windows.Forms.Button();
            this.btnFaultReset = new System.Windows.Forms.Button();
            this.lblJog = new System.Windows.Forms.Label();
            this.btnJogReverse = new System.Windows.Forms.Button();
            this.btnJogForward = new System.Windows.Forms.Button();
            this.lblMove = new System.Windows.Forms.Label();
            this.numTargetDeg = new System.Windows.Forms.NumericUpDown();
            this.btnAbsMove = new System.Windows.Forms.Button();
            this.btnRelMove = new System.Windows.Forms.Button();
            this.btnHome = new System.Windows.Forms.Button();
            this.lblParam = new System.Windows.Forms.Label();
            this.numParam = new System.Windows.Forms.NumericUpDown();
            this.lblParamHint = new System.Windows.Forms.Label();

            // ── 日志 ──
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();

            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).BeginInit();
            this.grpOverview.SuspendLayout();
            this.grpGlobal.SuspendLayout();
            this.grpAxis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTargetDeg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParam)).BeginInit();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            // lblModeTitle
            this.lblModeTitle.AutoSize = true;
            this.lblModeTitle.Location = new System.Drawing.Point(14, 17);
            this.lblModeTitle.Name = "lblModeTitle";
            this.lblModeTitle.Size = new System.Drawing.Size(59, 17);
            this.lblModeTitle.Text = "运行模式:";

            // cboMode
            this.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMode.Location = new System.Drawing.Point(80, 14);
            this.cboMode.Name = "cboMode";
            this.cboMode.Size = new System.Drawing.Size(200, 25);
            this.cboMode.Items.AddRange(new object[] {
                "CSP 周期同步位置 (Mode=8)",
                "PP  轮廓位置 (Mode=1)"});

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(296, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(84, 28);
            this.btnConnect.Text = "连接";

            // btnDisconnect
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(386, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(84, 28);
            this.btnDisconnect.Text = "断开";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(486, 17);
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

            // grpOverview
            this.grpOverview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOverview.Controls.Add(this.gridAxes);
            this.grpOverview.Location = new System.Drawing.Point(12, 50);
            this.grpOverview.Name = "grpOverview";
            this.grpOverview.Size = new System.Drawing.Size(960, 250);
            this.grpOverview.Text = "轴总览 (点击行选中, 勾选「选」列指定全局操作目标)";

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
            this.gridAxes.Size = new System.Drawing.Size(940, 218);

            // grpGlobal
            this.grpGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGlobal.Controls.Add(this.lblGlobalHint);
            this.grpGlobal.Controls.Add(this.btnAllEnable);
            this.grpGlobal.Controls.Add(this.btnAllDisable);
            this.grpGlobal.Controls.Add(this.btnAllFaultReset);
            this.grpGlobal.Controls.Add(this.btnAllHome);
            this.grpGlobal.Controls.Add(this.btnAllEStop);
            this.grpGlobal.Enabled = false;
            this.grpGlobal.Location = new System.Drawing.Point(12, 306);
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

            // btnAllHome
            this.btnAllHome.Location = new System.Drawing.Point(326, 26);
            this.btnAllHome.Name = "btnAllHome";
            this.btnAllHome.Size = new System.Drawing.Size(96, 32);
            this.btnAllHome.Text = "全部回零";

            // btnAllEStop
            this.btnAllEStop.BackColor = System.Drawing.Color.MistyRose;
            this.btnAllEStop.Location = new System.Drawing.Point(440, 26);
            this.btnAllEStop.Name = "btnAllEStop";
            this.btnAllEStop.Size = new System.Drawing.Size(130, 32);
            this.btnAllEStop.Text = "全部急停 (去使能)";
            this.btnAllEStop.UseVisualStyleBackColor = false;

            // lblGlobalHint
            this.lblGlobalHint.AutoSize = true;
            this.lblGlobalHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblGlobalHint.Location = new System.Drawing.Point(590, 34);
            this.lblGlobalHint.Name = "lblGlobalHint";
            this.lblGlobalHint.Size = new System.Drawing.Size(0, 17);

            // grpAxis
            this.grpAxis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpAxis.Controls.Add(this.lblSelAxis);
            this.grpAxis.Controls.Add(this.btnEnable);
            this.grpAxis.Controls.Add(this.btnDisable);
            this.grpAxis.Controls.Add(this.btnFaultReset);
            this.grpAxis.Controls.Add(this.lblJog);
            this.grpAxis.Controls.Add(this.btnJogReverse);
            this.grpAxis.Controls.Add(this.btnJogForward);
            this.grpAxis.Controls.Add(this.lblMove);
            this.grpAxis.Controls.Add(this.numTargetDeg);
            this.grpAxis.Controls.Add(this.btnAbsMove);
            this.grpAxis.Controls.Add(this.btnRelMove);
            this.grpAxis.Controls.Add(this.btnHome);
            this.grpAxis.Controls.Add(this.lblParam);
            this.grpAxis.Controls.Add(this.numParam);
            this.grpAxis.Controls.Add(this.lblParamHint);
            this.grpAxis.Enabled = false;
            this.grpAxis.Location = new System.Drawing.Point(12, 382);
            this.grpAxis.Name = "grpAxis";
            this.grpAxis.Size = new System.Drawing.Size(960, 158);
            this.grpAxis.Text = "选定轴控制";

            // lblSelAxis
            this.lblSelAxis.AutoSize = true;
            this.lblSelAxis.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSelAxis.Location = new System.Drawing.Point(14, 24);
            this.lblSelAxis.Name = "lblSelAxis";
            this.lblSelAxis.Size = new System.Drawing.Size(80, 17);
            this.lblSelAxis.Text = "选定轴: -";

            // btnEnable
            this.btnEnable.Location = new System.Drawing.Point(14, 50);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(84, 30);
            this.btnEnable.Text = "使能";

            // btnDisable
            this.btnDisable.Location = new System.Drawing.Point(104, 50);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(84, 30);
            this.btnDisable.Text = "去使能";

            // btnFaultReset
            this.btnFaultReset.Location = new System.Drawing.Point(194, 50);
            this.btnFaultReset.Name = "btnFaultReset";
            this.btnFaultReset.Size = new System.Drawing.Size(84, 30);
            this.btnFaultReset.Text = "故障复位";

            // lblJog
            this.lblJog.AutoSize = true;
            this.lblJog.Location = new System.Drawing.Point(308, 57);
            this.lblJog.Name = "lblJog";
            this.lblJog.Size = new System.Drawing.Size(72, 17);
            this.lblJog.Text = "点动(按住):";

            // btnJogReverse
            this.btnJogReverse.Location = new System.Drawing.Point(390, 50);
            this.btnJogReverse.Name = "btnJogReverse";
            this.btnJogReverse.Size = new System.Drawing.Size(84, 30);
            this.btnJogReverse.Text = "◀ 反转";

            // btnJogForward
            this.btnJogForward.Location = new System.Drawing.Point(480, 50);
            this.btnJogForward.Name = "btnJogForward";
            this.btnJogForward.Size = new System.Drawing.Size(84, 30);
            this.btnJogForward.Text = "正转 ▶";

            // lblMove
            this.lblMove.AutoSize = true;
            this.lblMove.Location = new System.Drawing.Point(14, 100);
            this.lblMove.Name = "lblMove";
            this.lblMove.Size = new System.Drawing.Size(56, 17);
            this.lblMove.Text = "定位(°):";

            // numTargetDeg
            this.numTargetDeg.DecimalPlaces = 1;
            this.numTargetDeg.Location = new System.Drawing.Point(78, 97);
            this.numTargetDeg.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numTargetDeg.Minimum = new decimal(new int[] { 1000000, 0, 0, -2147483648 });
            this.numTargetDeg.Name = "numTargetDeg";
            this.numTargetDeg.Size = new System.Drawing.Size(100, 23);
            this.numTargetDeg.Value = new decimal(new int[] { 90, 0, 0, 0 });

            // btnAbsMove
            this.btnAbsMove.Location = new System.Drawing.Point(188, 95);
            this.btnAbsMove.Name = "btnAbsMove";
            this.btnAbsMove.Size = new System.Drawing.Size(84, 28);
            this.btnAbsMove.Text = "绝对运行";

            // btnRelMove
            this.btnRelMove.Location = new System.Drawing.Point(278, 95);
            this.btnRelMove.Name = "btnRelMove";
            this.btnRelMove.Size = new System.Drawing.Size(84, 28);
            this.btnRelMove.Text = "相对运行";

            // btnHome
            this.btnHome.Location = new System.Drawing.Point(388, 95);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(84, 28);
            this.btnHome.Text = "回零";

            // lblParam
            this.lblParam.AutoSize = true;
            this.lblParam.Location = new System.Drawing.Point(590, 57);
            this.lblParam.Name = "lblParam";
            this.lblParam.Size = new System.Drawing.Size(86, 17);
            this.lblParam.Text = "CSP 步/周期:";

            // numParam
            this.numParam.Location = new System.Drawing.Point(700, 54);
            this.numParam.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            this.numParam.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numParam.Name = "numParam";
            this.numParam.Size = new System.Drawing.Size(120, 23);
            this.numParam.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // lblParamHint
            this.lblParamHint.AutoSize = true;
            this.lblParamHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblParamHint.Location = new System.Drawing.Point(590, 100);
            this.lblParamHint.Name = "lblParamHint";
            this.lblParamHint.Size = new System.Drawing.Size(0, 17);

            // grpLog
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 546);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(960, 162);
            this.grpLog.Text = "日志";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 19);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(954, 140);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 718);
            this.Controls.Add(this.lblModeTitle);
            this.Controls.Add(this.cboMode);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblAxisCount);
            this.Controls.Add(this.grpOverview);
            this.Controls.Add(this.grpGlobal);
            this.Controls.Add(this.grpAxis);
            this.Controls.Add(this.grpLog);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 640);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "STF-EC 多轴步进  by Darra EtherCAT SDK";

            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).EndInit();
            this.grpOverview.ResumeLayout(false);
            this.grpGlobal.ResumeLayout(false);
            this.grpGlobal.PerformLayout();
            this.grpAxis.ResumeLayout(false);
            this.grpAxis.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTargetDeg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParam)).EndInit();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblModeTitle;
        private System.Windows.Forms.ComboBox cboMode;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAxisCount;

        private System.Windows.Forms.GroupBox grpOverview;
        private System.Windows.Forms.DataGridView gridAxes;

        private System.Windows.Forms.GroupBox grpGlobal;
        private System.Windows.Forms.Label lblGlobalHint;
        private System.Windows.Forms.Button btnAllEnable;
        private System.Windows.Forms.Button btnAllDisable;
        private System.Windows.Forms.Button btnAllFaultReset;
        private System.Windows.Forms.Button btnAllHome;
        private System.Windows.Forms.Button btnAllEStop;

        private System.Windows.Forms.GroupBox grpAxis;
        private System.Windows.Forms.Label lblSelAxis;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnDisable;
        private System.Windows.Forms.Button btnFaultReset;
        private System.Windows.Forms.Label lblJog;
        private System.Windows.Forms.Button btnJogReverse;
        private System.Windows.Forms.Button btnJogForward;
        private System.Windows.Forms.Label lblMove;
        private System.Windows.Forms.NumericUpDown numTargetDeg;
        private System.Windows.Forms.Button btnAbsMove;
        private System.Windows.Forms.Button btnRelMove;
        private System.Windows.Forms.Button btnHome;
        private System.Windows.Forms.Label lblParam;
        private System.Windows.Forms.NumericUpDown numParam;
        private System.Windows.Forms.Label lblParamHint;

        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox txtLog;
    }
}
