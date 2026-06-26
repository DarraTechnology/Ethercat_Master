namespace Panasonic_CNC_Interpolation
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

            // ── 轴总览表 (含 X/Y/Z 角色映射) ──
            this.grpOverview = new System.Windows.Forms.GroupBox();
            this.gridAxes = new System.Windows.Forms.DataGridView();

            // ── G 代码输入 ──
            this.grpGcode = new System.Windows.Forms.GroupBox();
            this.txtGcode = new System.Windows.Forms.TextBox();
            this.btnPresetRect = new System.Windows.Forms.Button();
            this.btnPresetCircle = new System.Windows.Forms.Button();
            this.btnPresetRoundRect = new System.Windows.Forms.Button();
            this.btnPresetStar = new System.Windows.Forms.Button();

            // ── 参数 ──
            this.grpParams = new System.Windows.Forms.GroupBox();
            this.lblFeed = new System.Windows.Forms.Label();
            this.numFeed = new System.Windows.Forms.NumericUpDown();
            this.lblAccel = new System.Windows.Forms.Label();
            this.numAccel = new System.Windows.Forms.NumericUpDown();
            this.lblJerk = new System.Windows.Forms.Label();
            this.numJerk = new System.Windows.Forms.NumericUpDown();
            this.lblPulsesPerMm = new System.Windows.Forms.Label();
            this.numPulsesPerMm = new System.Windows.Forms.NumericUpDown();
            this.lblJunctionDev = new System.Windows.Forms.Label();
            this.numJunctionDev = new System.Windows.Forms.NumericUpDown();
            this.lblProfile = new System.Windows.Forms.Label();
            this.rbTrapezoid = new System.Windows.Forms.RadioButton();
            this.rbSCurve = new System.Windows.Forms.RadioButton();

            // ── 2D 路径预览 ──
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.pnlPreview = new System.Windows.Forms.Panel();

            // ── 控制 ──
            this.grpControl = new System.Windows.Forms.GroupBox();
            this.btnEnable = new System.Windows.Forms.Button();
            this.btnDisable = new System.Windows.Forms.Button();
            this.btnFaultReset = new System.Windows.Forms.Button();
            this.btnSetOrigin = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();

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

            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).BeginInit();
            this.grpOverview.SuspendLayout();
            this.grpGcode.SuspendLayout();
            this.grpParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAccel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numJerk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPulsesPerMm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numJunctionDev)).BeginInit();
            this.grpPreview.SuspendLayout();
            this.grpControl.SuspendLayout();
            this.pnlAlarmBanner.SuspendLayout();
            this.grpAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).BeginInit();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            // lblMode
            this.lblMode.AutoSize = true;
            this.lblMode.ForeColor = System.Drawing.Color.DimGray;
            this.lblMode.Location = new System.Drawing.Point(14, 17);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(260, 17);
            this.lblMode.Text = "模式: CSP 多轴轨迹插补 (Mode=8) — G01/G02/G03";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(330, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(84, 28);
            this.btnConnect.Text = "连接";

            // btnDisconnect
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(420, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(84, 28);
            this.btnDisconnect.Text = "断开";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(520, 17);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(44, 17);
            this.lblStatus.Text = "未连接";

            // lblAxisCount
            this.lblAxisCount.AutoSize = true;
            this.lblAxisCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblAxisCount.Location = new System.Drawing.Point(900, 17);
            this.lblAxisCount.Name = "lblAxisCount";
            this.lblAxisCount.Size = new System.Drawing.Size(56, 17);
            this.lblAxisCount.Text = "轴数: -";

            // grpOverview
            this.grpOverview.Controls.Add(this.gridAxes);
            this.grpOverview.Location = new System.Drawing.Point(12, 50);
            this.grpOverview.Name = "grpOverview";
            this.grpOverview.Size = new System.Drawing.Size(620, 196);
            this.grpOverview.Text = "轴总览 (双击「角色」列指定 X/Y/Z 或 禁用; 出问题的轴整行红)";

            // gridAxes
            this.gridAxes.AllowUserToAddRows = false;
            this.gridAxes.AllowUserToDeleteRows = false;
            this.gridAxes.AllowUserToResizeRows = false;
            this.gridAxes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAxes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAxes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridAxes.Location = new System.Drawing.Point(3, 19);
            this.gridAxes.MultiSelect = false;
            this.gridAxes.Name = "gridAxes";
            this.gridAxes.RowHeadersVisible = false;
            this.gridAxes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.gridAxes.Size = new System.Drawing.Size(614, 174);

            // grpGcode
            this.grpGcode.Controls.Add(this.txtGcode);
            this.grpGcode.Controls.Add(this.btnPresetRect);
            this.grpGcode.Controls.Add(this.btnPresetCircle);
            this.grpGcode.Controls.Add(this.btnPresetRoundRect);
            this.grpGcode.Controls.Add(this.btnPresetStar);
            this.grpGcode.Location = new System.Drawing.Point(12, 252);
            this.grpGcode.Name = "grpGcode";
            this.grpGcode.Size = new System.Drawing.Size(620, 250);
            this.grpGcode.Text = "G 代码 (G00/G01 X Y Z F; G02/G03 X Y I J F; 单位 mm)";

            // txtGcode
            this.txtGcode.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.txtGcode.Location = new System.Drawing.Point(10, 22);
            this.txtGcode.Multiline = true;
            this.txtGcode.Name = "txtGcode";
            this.txtGcode.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtGcode.Size = new System.Drawing.Size(600, 184);
            this.txtGcode.WordWrap = false;

            // btnPresetRect
            this.btnPresetRect.Location = new System.Drawing.Point(10, 212);
            this.btnPresetRect.Name = "btnPresetRect";
            this.btnPresetRect.Size = new System.Drawing.Size(110, 30);
            this.btnPresetRect.Text = "矩形";

            // btnPresetCircle
            this.btnPresetCircle.Location = new System.Drawing.Point(126, 212);
            this.btnPresetCircle.Name = "btnPresetCircle";
            this.btnPresetCircle.Size = new System.Drawing.Size(110, 30);
            this.btnPresetCircle.Text = "圆";

            // btnPresetRoundRect
            this.btnPresetRoundRect.Location = new System.Drawing.Point(242, 212);
            this.btnPresetRoundRect.Name = "btnPresetRoundRect";
            this.btnPresetRoundRect.Size = new System.Drawing.Size(110, 30);
            this.btnPresetRoundRect.Text = "圆角矩形";

            // btnPresetStar
            this.btnPresetStar.Location = new System.Drawing.Point(358, 212);
            this.btnPresetStar.Name = "btnPresetStar";
            this.btnPresetStar.Size = new System.Drawing.Size(110, 30);
            this.btnPresetStar.Text = "五角星";

            // grpParams
            this.grpParams.Controls.Add(this.lblFeed);
            this.grpParams.Controls.Add(this.numFeed);
            this.grpParams.Controls.Add(this.lblAccel);
            this.grpParams.Controls.Add(this.numAccel);
            this.grpParams.Controls.Add(this.lblJerk);
            this.grpParams.Controls.Add(this.numJerk);
            this.grpParams.Controls.Add(this.lblPulsesPerMm);
            this.grpParams.Controls.Add(this.numPulsesPerMm);
            this.grpParams.Controls.Add(this.lblJunctionDev);
            this.grpParams.Controls.Add(this.numJunctionDev);
            this.grpParams.Controls.Add(this.lblProfile);
            this.grpParams.Controls.Add(this.rbTrapezoid);
            this.grpParams.Controls.Add(this.rbSCurve);
            this.grpParams.Location = new System.Drawing.Point(648, 50);
            this.grpParams.Name = "grpParams";
            this.grpParams.Size = new System.Drawing.Size(420, 196);
            this.grpParams.Text = "插补参数";

            // lblFeed
            this.lblFeed.AutoSize = true;
            this.lblFeed.Location = new System.Drawing.Point(14, 28);
            this.lblFeed.Name = "lblFeed";
            this.lblFeed.Size = new System.Drawing.Size(112, 17);
            this.lblFeed.Text = "进给 F (mm/min):";

            // numFeed
            this.numFeed.Location = new System.Drawing.Point(160, 25);
            this.numFeed.Maximum = new decimal(new int[] { 200000, 0, 0, 0 });
            this.numFeed.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numFeed.Name = "numFeed";
            this.numFeed.Size = new System.Drawing.Size(120, 23);
            this.numFeed.ThousandsSeparator = true;
            this.numFeed.Value = new decimal(new int[] { 3000, 0, 0, 0 });

            // lblAccel
            this.lblAccel.AutoSize = true;
            this.lblAccel.Location = new System.Drawing.Point(14, 58);
            this.lblAccel.Name = "lblAccel";
            this.lblAccel.Size = new System.Drawing.Size(140, 17);
            this.lblAccel.Text = "最大加速度 (mm/s²):";

            // numAccel
            this.numAccel.Location = new System.Drawing.Point(160, 55);
            this.numAccel.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numAccel.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numAccel.Name = "numAccel";
            this.numAccel.Size = new System.Drawing.Size(120, 23);
            this.numAccel.ThousandsSeparator = true;
            this.numAccel.Value = new decimal(new int[] { 500, 0, 0, 0 });

            // lblJerk
            this.lblJerk.AutoSize = true;
            this.lblJerk.Location = new System.Drawing.Point(14, 88);
            this.lblJerk.Name = "lblJerk";
            this.lblJerk.Size = new System.Drawing.Size(140, 17);
            this.lblJerk.Text = "Jerk (mm/s³, S曲线):";

            // numJerk
            this.numJerk.Location = new System.Drawing.Point(160, 85);
            this.numJerk.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numJerk.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numJerk.Name = "numJerk";
            this.numJerk.Size = new System.Drawing.Size(120, 23);
            this.numJerk.ThousandsSeparator = true;
            this.numJerk.Value = new decimal(new int[] { 5000, 0, 0, 0 });

            // lblPulsesPerMm
            this.lblPulsesPerMm.AutoSize = true;
            this.lblPulsesPerMm.Location = new System.Drawing.Point(14, 118);
            this.lblPulsesPerMm.Name = "lblPulsesPerMm";
            this.lblPulsesPerMm.Size = new System.Drawing.Size(80, 17);
            this.lblPulsesPerMm.Text = "脉冲 / mm:";

            // numPulsesPerMm
            this.numPulsesPerMm.Location = new System.Drawing.Point(160, 115);
            this.numPulsesPerMm.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            this.numPulsesPerMm.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPulsesPerMm.Name = "numPulsesPerMm";
            this.numPulsesPerMm.Size = new System.Drawing.Size(120, 23);
            this.numPulsesPerMm.ThousandsSeparator = true;
            // 示例值: 松下 A6 23 位编码器(8388608/转) 直连 10mm 导程丝杠 1:1 ≈ 838861 脉冲/mm。务必按你的机械实测校准 (见 README 校准流程)。
            this.numPulsesPerMm.Value = new decimal(new int[] { 838861, 0, 0, 0 });

            // lblJunctionDev
            this.lblJunctionDev.AutoSize = true;
            this.lblJunctionDev.Location = new System.Drawing.Point(14, 148);
            this.lblJunctionDev.Name = "lblJunctionDev";
            this.lblJunctionDev.Size = new System.Drawing.Size(132, 17);
            this.lblJunctionDev.Text = "前瞻拐角偏差 δ (mm):";

            // numJunctionDev
            this.numJunctionDev.DecimalPlaces = 3;
            this.numJunctionDev.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numJunctionDev.Location = new System.Drawing.Point(160, 145);
            this.numJunctionDev.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numJunctionDev.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numJunctionDev.Name = "numJunctionDev";
            this.numJunctionDev.Size = new System.Drawing.Size(120, 23);
            this.numJunctionDev.Value = new decimal(new int[] { 5, 0, 0, 131072 });

            // lblProfile
            this.lblProfile.AutoSize = true;
            this.lblProfile.Location = new System.Drawing.Point(296, 28);
            this.lblProfile.Name = "lblProfile";
            this.lblProfile.Size = new System.Drawing.Size(68, 17);
            this.lblProfile.Text = "速度剖面:";

            // rbTrapezoid
            this.rbTrapezoid.AutoSize = true;
            this.rbTrapezoid.Checked = true;
            this.rbTrapezoid.Location = new System.Drawing.Point(298, 52);
            this.rbTrapezoid.Name = "rbTrapezoid";
            this.rbTrapezoid.Size = new System.Drawing.Size(73, 21);
            this.rbTrapezoid.TabStop = true;
            this.rbTrapezoid.Text = "梯形";

            // rbSCurve
            this.rbSCurve.AutoSize = true;
            this.rbSCurve.Location = new System.Drawing.Point(298, 78);
            this.rbSCurve.Name = "rbSCurve";
            this.rbSCurve.Size = new System.Drawing.Size(89, 21);
            this.rbSCurve.Text = "S 曲线";

            // grpPreview
            this.grpPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.grpPreview.Controls.Add(this.pnlPreview);
            this.grpPreview.Location = new System.Drawing.Point(648, 252);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Size = new System.Drawing.Size(420, 250);
            this.grpPreview.Text = "2D 路径预览 (蓝=编程路径, 红=刀位点, 绿=原点)";

            // pnlPreview
            this.pnlPreview.BackColor = System.Drawing.Color.White;
            this.pnlPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(3, 19);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(414, 228);

            // grpControl
            this.grpControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpControl.Controls.Add(this.btnEnable);
            this.grpControl.Controls.Add(this.btnDisable);
            this.grpControl.Controls.Add(this.btnFaultReset);
            this.grpControl.Controls.Add(this.btnSetOrigin);
            this.grpControl.Controls.Add(this.btnRun);
            this.grpControl.Controls.Add(this.btnPause);
            this.grpControl.Controls.Add(this.btnStop);
            this.grpControl.Controls.Add(this.lblProgress);
            this.grpControl.Enabled = false;
            this.grpControl.Location = new System.Drawing.Point(12, 508);
            this.grpControl.Name = "grpControl";
            this.grpControl.Size = new System.Drawing.Size(1056, 92);
            this.grpControl.Text = "控制";

            // btnEnable
            this.btnEnable.BackColor = System.Drawing.Color.Honeydew;
            this.btnEnable.Location = new System.Drawing.Point(14, 24);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(100, 32);
            this.btnEnable.Text = "使能";
            this.btnEnable.UseVisualStyleBackColor = false;

            // btnDisable
            this.btnDisable.BackColor = System.Drawing.Color.MistyRose;
            this.btnDisable.Location = new System.Drawing.Point(120, 24);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(130, 32);
            this.btnDisable.Text = "去使能 (急停)";
            this.btnDisable.UseVisualStyleBackColor = false;

            // btnFaultReset
            this.btnFaultReset.Location = new System.Drawing.Point(256, 24);
            this.btnFaultReset.Name = "btnFaultReset";
            this.btnFaultReset.Size = new System.Drawing.Size(100, 32);
            this.btnFaultReset.Text = "故障复位";

            // btnSetOrigin
            this.btnSetOrigin.Location = new System.Drawing.Point(362, 24);
            this.btnSetOrigin.Name = "btnSetOrigin";
            this.btnSetOrigin.Size = new System.Drawing.Size(100, 32);
            this.btnSetOrigin.Text = "设原点";

            // btnRun
            this.btnRun.BackColor = System.Drawing.Color.Honeydew;
            this.btnRun.Location = new System.Drawing.Point(500, 24);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(100, 32);
            this.btnRun.Text = "运行";
            this.btnRun.UseVisualStyleBackColor = false;

            // btnPause
            this.btnPause.Location = new System.Drawing.Point(606, 24);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(100, 32);
            this.btnPause.Text = "暂停";

            // btnStop
            this.btnStop.BackColor = System.Drawing.Color.MistyRose;
            this.btnStop.Location = new System.Drawing.Point(712, 24);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(100, 32);
            this.btnStop.Text = "停止";
            this.btnStop.UseVisualStyleBackColor = false;

            // lblProgress
            this.lblProgress.AutoSize = false;
            this.lblProgress.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblProgress.ForeColor = System.Drawing.Color.DimGray;
            this.lblProgress.Location = new System.Drawing.Point(14, 62);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(1030, 22);
            this.lblProgress.Text = "段: - / -    完成: -    进给: -";

            // pnlAlarmBanner
            this.pnlAlarmBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAlarmBanner.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            this.pnlAlarmBanner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmBanner);
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmCount);
            this.pnlAlarmBanner.Controls.Add(this.btnAlarmAck);
            this.pnlAlarmBanner.Location = new System.Drawing.Point(12, 606);
            this.pnlAlarmBanner.Name = "pnlAlarmBanner";
            this.pnlAlarmBanner.Size = new System.Drawing.Size(1056, 34);

            // lblAlarmBanner
            this.lblAlarmBanner.AutoSize = false;
            this.lblAlarmBanner.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAlarmBanner.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmBanner.Location = new System.Drawing.Point(8, 6);
            this.lblAlarmBanner.Name = "lblAlarmBanner";
            this.lblAlarmBanner.Size = new System.Drawing.Size(700, 22);
            this.lblAlarmBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAlarmBanner.Text = "● 未连接";

            // lblAlarmCount
            this.lblAlarmCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAlarmCount.AutoSize = false;
            this.lblAlarmCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmCount.Location = new System.Drawing.Point(736, 8);
            this.lblAlarmCount.Name = "lblAlarmCount";
            this.lblAlarmCount.Size = new System.Drawing.Size(200, 18);
            this.lblAlarmCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAlarmCount.Text = "激活:0  故障:0  历史:0";

            // btnAlarmAck
            this.btnAlarmAck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAlarmAck.Enabled = false;
            this.btnAlarmAck.Location = new System.Drawing.Point(946, 3);
            this.btnAlarmAck.Name = "btnAlarmAck";
            this.btnAlarmAck.Size = new System.Drawing.Size(100, 27);
            this.btnAlarmAck.Text = "报警复位";

            // grpAlarm
            this.grpAlarm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpAlarm.Controls.Add(this.gridAlarms);
            this.grpAlarm.Location = new System.Drawing.Point(12, 646);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(1056, 138);
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
            this.gridAlarms.Size = new System.Drawing.Size(1050, 116);

            // grpLog
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 790);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(1056, 92);
            this.grpLog.Text = "日志";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 19);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1050, 70);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 894);
            this.Controls.Add(this.lblMode);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblAxisCount);
            this.Controls.Add(this.grpOverview);
            this.Controls.Add(this.grpGcode);
            this.Controls.Add(this.grpParams);
            this.Controls.Add(this.grpPreview);
            this.Controls.Add(this.grpControl);
            this.Controls.Add(this.pnlAlarmBanner);
            this.Controls.Add(this.grpAlarm);
            this.Controls.Add(this.grpLog);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 840);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "松下 A6 CNC 多轴轨迹插补  by Darra EtherCAT SDK";

            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).EndInit();
            this.grpOverview.ResumeLayout(false);
            this.grpGcode.ResumeLayout(false);
            this.grpGcode.PerformLayout();
            this.grpParams.ResumeLayout(false);
            this.grpParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAccel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numJerk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPulsesPerMm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numJunctionDev)).EndInit();
            this.grpPreview.ResumeLayout(false);
            this.grpControl.ResumeLayout(false);
            this.pnlAlarmBanner.ResumeLayout(false);
            this.grpAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAlarms)).EndInit();
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

        private System.Windows.Forms.GroupBox grpOverview;
        private System.Windows.Forms.DataGridView gridAxes;

        private System.Windows.Forms.GroupBox grpGcode;
        private System.Windows.Forms.TextBox txtGcode;
        private System.Windows.Forms.Button btnPresetRect;
        private System.Windows.Forms.Button btnPresetCircle;
        private System.Windows.Forms.Button btnPresetRoundRect;
        private System.Windows.Forms.Button btnPresetStar;

        private System.Windows.Forms.GroupBox grpParams;
        private System.Windows.Forms.Label lblFeed;
        private System.Windows.Forms.NumericUpDown numFeed;
        private System.Windows.Forms.Label lblAccel;
        private System.Windows.Forms.NumericUpDown numAccel;
        private System.Windows.Forms.Label lblJerk;
        private System.Windows.Forms.NumericUpDown numJerk;
        private System.Windows.Forms.Label lblPulsesPerMm;
        private System.Windows.Forms.NumericUpDown numPulsesPerMm;
        private System.Windows.Forms.Label lblJunctionDev;
        private System.Windows.Forms.NumericUpDown numJunctionDev;
        private System.Windows.Forms.Label lblProfile;
        private System.Windows.Forms.RadioButton rbTrapezoid;
        private System.Windows.Forms.RadioButton rbSCurve;

        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Panel pnlPreview;

        private System.Windows.Forms.GroupBox grpControl;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnDisable;
        private System.Windows.Forms.Button btnFaultReset;
        private System.Windows.Forms.Button btnSetOrigin;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblProgress;

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
