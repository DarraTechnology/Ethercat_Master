namespace Panasonic_ConveyorSync
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

            // ── 主轴源 ──
            this.grpMaster = new System.Windows.Forms.GroupBox();
            this.rbVirtual = new System.Windows.Forms.RadioButton();
            this.rbEncoder = new System.Windows.Forms.RadioButton();
            this.cboEncoderAxis = new System.Windows.Forms.ComboBox();
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

            // ── 物料跟踪 ──
            this.grpTrack = new System.Windows.Forms.GroupBox();
            this.gridProducts = new System.Windows.Forms.DataGridView();
            this.btnInjectProduct = new System.Windows.Forms.Button();
            this.btnArmProbe = new System.Windows.Forms.Button();
            this.btnClearProducts = new System.Windows.Forms.Button();
            this.lblZoneFrom = new System.Windows.Forms.Label();
            this.numZoneFrom = new System.Windows.Forms.NumericUpDown();
            this.lblZoneTo = new System.Windows.Forms.Label();
            this.numZoneTo = new System.Windows.Forms.NumericUpDown();
            this.lblPickAxis = new System.Windows.Forms.Label();
            this.cboPickAxis = new System.Windows.Forms.ComboBox();
            this.chkDemo = new System.Windows.Forms.CheckBox();
            this.lblDemoInt = new System.Windows.Forms.Label();
            this.numDemoInterval = new System.Windows.Forms.NumericUpDown();
            this.lblCapSrc = new System.Windows.Forms.Label();
            this.cboCaptureSrc = new System.Windows.Forms.ComboBox();
            this.lblDiBit = new System.Windows.Forms.Label();
            this.numDiBit = new System.Windows.Forms.NumericUpDown();

            // ── 传送带可视化 ──
            this.grpConveyor = new System.Windows.Forms.GroupBox();
            this.picConveyor = new System.Windows.Forms.PictureBox();

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
            ((System.ComponentModel.ISupportInitialize)(this.gridProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZoneFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZoneTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDemoInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiBit)).BeginInit();
            this.grpTrack.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picConveyor)).BeginInit();
            this.grpConveyor.SuspendLayout();
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
            this.lblMode.Size = new System.Drawing.Size(240, 17);
            this.lblMode.Text = "模式: CSP 周期同步位置 (Mode=8) 主从同步";

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
            this.lblAxisCount.Location = new System.Drawing.Point(900, 17);
            this.lblAxisCount.Name = "lblAxisCount";
            this.lblAxisCount.Size = new System.Drawing.Size(56, 17);
            this.lblAxisCount.Text = "轴数: -";

            // grpMaster
            this.grpMaster.Controls.Add(this.rbVirtual);
            this.grpMaster.Controls.Add(this.rbEncoder);
            this.grpMaster.Controls.Add(this.cboEncoderAxis);
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
            this.grpMaster.Size = new System.Drawing.Size(1136, 104);
            this.grpMaster.Text = "主轴源 (各从轴按齿比+相位跟随主轴; 齿比1:1 = 同步, 齿比≠1 = 电子齿轮)";

            // rbVirtual
            this.rbVirtual.AutoSize = true;
            this.rbVirtual.Checked = true;
            this.rbVirtual.Location = new System.Drawing.Point(14, 26);
            this.rbVirtual.Name = "rbVirtual";
            this.rbVirtual.Size = new System.Drawing.Size(160, 21);
            this.rbVirtual.Text = "虚拟主轴 (速度滑块推进)";

            // rbEncoder
            this.rbEncoder.AutoSize = true;
            this.rbEncoder.Location = new System.Drawing.Point(14, 58);
            this.rbEncoder.Name = "rbEncoder";
            this.rbEncoder.Size = new System.Drawing.Size(124, 21);
            this.rbEncoder.Text = "编码器主轴 (选轴):";

            // cboEncoderAxis
            this.cboEncoderAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEncoderAxis.Location = new System.Drawing.Point(150, 56);
            this.cboEncoderAxis.Name = "cboEncoderAxis";
            this.cboEncoderAxis.Size = new System.Drawing.Size(110, 25);

            // lblMasterSpeed
            this.lblMasterSpeed.AutoSize = true;
            this.lblMasterSpeed.Location = new System.Drawing.Point(280, 28);
            this.lblMasterSpeed.Name = "lblMasterSpeed";
            this.lblMasterSpeed.Size = new System.Drawing.Size(86, 17);
            this.lblMasterSpeed.Text = "速度(脉冲/秒):";

            // numMasterSpeed
            this.numMasterSpeed.Location = new System.Drawing.Point(374, 26);
            this.numMasterSpeed.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.numMasterSpeed.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numMasterSpeed.Name = "numMasterSpeed";
            this.numMasterSpeed.Size = new System.Drawing.Size(130, 23);
            this.numMasterSpeed.ThousandsSeparator = true;
            this.numMasterSpeed.Value = new decimal(new int[] { 1000000, 0, 0, 0 });

            // btnSyncStart
            this.btnSyncStart.BackColor = System.Drawing.Color.Honeydew;
            this.btnSyncStart.Location = new System.Drawing.Point(524, 24);
            this.btnSyncStart.Name = "btnSyncStart";
            this.btnSyncStart.Size = new System.Drawing.Size(96, 30);
            this.btnSyncStart.Text = "启动同步";
            this.btnSyncStart.UseVisualStyleBackColor = false;

            // btnSyncStop
            this.btnSyncStop.BackColor = System.Drawing.Color.MistyRose;
            this.btnSyncStop.Location = new System.Drawing.Point(626, 24);
            this.btnSyncStop.Name = "btnSyncStop";
            this.btnSyncStop.Size = new System.Drawing.Size(84, 30);
            this.btnSyncStop.Text = "停止";
            this.btnSyncStop.UseVisualStyleBackColor = false;

            // btnMasterJogRev
            this.btnMasterJogRev.Location = new System.Drawing.Point(524, 60);
            this.btnMasterJogRev.Name = "btnMasterJogRev";
            this.btnMasterJogRev.Size = new System.Drawing.Size(90, 30);
            this.btnMasterJogRev.Text = "◀ 主轴-";

            // btnMasterJogFwd
            this.btnMasterJogFwd.Location = new System.Drawing.Point(620, 60);
            this.btnMasterJogFwd.Name = "btnMasterJogFwd";
            this.btnMasterJogFwd.Size = new System.Drawing.Size(90, 30);
            this.btnMasterJogFwd.Text = "主轴+ ▶";

            // lblMasterPos
            this.lblMasterPos.AutoSize = true;
            this.lblMasterPos.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMasterPos.ForeColor = System.Drawing.Color.DimGray;
            this.lblMasterPos.Location = new System.Drawing.Point(740, 40);
            this.lblMasterPos.Name = "lblMasterPos";
            this.lblMasterPos.Size = new System.Drawing.Size(80, 17);
            this.lblMasterPos.Text = "主轴位置: -";

            // grpOverview
            this.grpOverview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOverview.Controls.Add(this.gridAxes);
            this.grpOverview.Location = new System.Drawing.Point(12, 160);
            this.grpOverview.Name = "grpOverview";
            this.grpOverview.Size = new System.Drawing.Size(1136, 222);
            this.grpOverview.Text = "轴总览 (双击「角色/齿比/相位」列可改; 勾选「选」列指定全局操作目标, 未勾选 = 全部; 出问题的轴整行红)";

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
            this.gridAxes.Size = new System.Drawing.Size(1116, 190);

            // grpTrack (物料跟踪)
            this.grpTrack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)));
            this.grpTrack.Controls.Add(this.gridProducts);
            this.grpTrack.Controls.Add(this.btnInjectProduct);
            this.grpTrack.Controls.Add(this.btnArmProbe);
            this.grpTrack.Controls.Add(this.btnClearProducts);
            this.grpTrack.Controls.Add(this.lblZoneFrom);
            this.grpTrack.Controls.Add(this.numZoneFrom);
            this.grpTrack.Controls.Add(this.lblZoneTo);
            this.grpTrack.Controls.Add(this.numZoneTo);
            this.grpTrack.Controls.Add(this.lblPickAxis);
            this.grpTrack.Controls.Add(this.cboPickAxis);
            this.grpTrack.Controls.Add(this.chkDemo);
            this.grpTrack.Controls.Add(this.lblDemoInt);
            this.grpTrack.Controls.Add(this.numDemoInterval);
            this.grpTrack.Controls.Add(this.lblCapSrc);
            this.grpTrack.Controls.Add(this.cboCaptureSrc);
            this.grpTrack.Controls.Add(this.lblDiBit);
            this.grpTrack.Controls.Add(this.numDiBit);
            this.grpTrack.Location = new System.Drawing.Point(12, 388);
            this.grpTrack.Name = "grpTrack";
            this.grpTrack.Size = new System.Drawing.Size(560, 256);
            this.grpTrack.Text = "物料跟踪 (Touch Probe 捕获 + 飞行抓取)";

            // gridProducts
            this.gridProducts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.gridProducts.AllowUserToAddRows = false;
            this.gridProducts.AllowUserToDeleteRows = false;
            this.gridProducts.AllowUserToResizeRows = false;
            this.gridProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridProducts.Location = new System.Drawing.Point(10, 22);
            this.gridProducts.MultiSelect = false;
            this.gridProducts.Name = "gridProducts";
            this.gridProducts.ReadOnly = true;
            this.gridProducts.RowHeadersVisible = false;
            this.gridProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridProducts.Size = new System.Drawing.Size(540, 138);

            // btnInjectProduct
            this.btnInjectProduct.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnInjectProduct.BackColor = System.Drawing.Color.Honeydew;
            this.btnInjectProduct.Location = new System.Drawing.Point(10, 166);
            this.btnInjectProduct.Name = "btnInjectProduct";
            this.btnInjectProduct.Size = new System.Drawing.Size(96, 28);
            this.btnInjectProduct.Text = "注入物料";
            this.btnInjectProduct.UseVisualStyleBackColor = false;

            // btnArmProbe
            this.btnArmProbe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnArmProbe.Location = new System.Drawing.Point(114, 166);
            this.btnArmProbe.Name = "btnArmProbe";
            this.btnArmProbe.Size = new System.Drawing.Size(130, 28);
            this.btnArmProbe.Text = "武装触发 (probe1)";

            // btnClearProducts
            this.btnClearProducts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClearProducts.BackColor = System.Drawing.Color.MistyRose;
            this.btnClearProducts.Location = new System.Drawing.Point(250, 166);
            this.btnClearProducts.Name = "btnClearProducts";
            this.btnClearProducts.Size = new System.Drawing.Size(96, 28);
            this.btnClearProducts.Text = "清空物料";
            this.btnClearProducts.UseVisualStyleBackColor = false;

            // lblZoneFrom
            this.lblZoneFrom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblZoneFrom.AutoSize = true;
            this.lblZoneFrom.Location = new System.Drawing.Point(10, 206);
            this.lblZoneFrom.Name = "lblZoneFrom";
            this.lblZoneFrom.Size = new System.Drawing.Size(80, 17);
            this.lblZoneFrom.Text = "抓取区起(°):";

            // numZoneFrom
            this.numZoneFrom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numZoneFrom.DecimalPlaces = 1;
            this.numZoneFrom.Location = new System.Drawing.Point(96, 204);
            this.numZoneFrom.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numZoneFrom.Name = "numZoneFrom";
            this.numZoneFrom.Size = new System.Drawing.Size(72, 23);
            this.numZoneFrom.Value = new decimal(new int[] { 180, 0, 0, 0 });

            // lblZoneTo
            this.lblZoneTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblZoneTo.AutoSize = true;
            this.lblZoneTo.Location = new System.Drawing.Point(176, 206);
            this.lblZoneTo.Name = "lblZoneTo";
            this.lblZoneTo.Size = new System.Drawing.Size(56, 17);
            this.lblZoneTo.Text = "止(°):";

            // numZoneTo
            this.numZoneTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numZoneTo.DecimalPlaces = 1;
            this.numZoneTo.Location = new System.Drawing.Point(216, 204);
            this.numZoneTo.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numZoneTo.Name = "numZoneTo";
            this.numZoneTo.Size = new System.Drawing.Size(72, 23);
            this.numZoneTo.Value = new decimal(new int[] { 260, 0, 0, 0 });

            // lblPickAxis
            this.lblPickAxis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblPickAxis.AutoSize = true;
            this.lblPickAxis.Location = new System.Drawing.Point(300, 206);
            this.lblPickAxis.Name = "lblPickAxis";
            this.lblPickAxis.Size = new System.Drawing.Size(56, 17);
            this.lblPickAxis.Text = "抓取轴:";

            // cboPickAxis
            this.cboPickAxis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cboPickAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPickAxis.Location = new System.Drawing.Point(360, 203);
            this.cboPickAxis.Name = "cboPickAxis";
            this.cboPickAxis.Size = new System.Drawing.Size(110, 25);

            // chkDemo (演示: 自动来料 = 软件按间隔模拟光电周期触发, 无需传感器)
            this.chkDemo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkDemo.AutoSize = true;
            this.chkDemo.Location = new System.Drawing.Point(354, 171);
            this.chkDemo.Name = "chkDemo";
            this.chkDemo.Size = new System.Drawing.Size(112, 21);
            this.chkDemo.Text = "演示:自动来料";

            // lblDemoInt
            this.lblDemoInt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblDemoInt.AutoSize = true;
            this.lblDemoInt.Location = new System.Drawing.Point(468, 173);
            this.lblDemoInt.Name = "lblDemoInt";
            this.lblDemoInt.Size = new System.Drawing.Size(40, 17);
            this.lblDemoInt.Text = "间隔°";

            // numDemoInterval (每隔多少主轴度自动来一件)
            this.numDemoInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numDemoInterval.Location = new System.Drawing.Point(508, 170);
            this.numDemoInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            this.numDemoInterval.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numDemoInterval.Name = "numDemoInterval";
            this.numDemoInterval.Size = new System.Drawing.Size(44, 23);
            this.numDemoInterval.Value = new decimal(new int[] { 90, 0, 0, 0 });

            // lblCapSrc
            this.lblCapSrc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCapSrc.AutoSize = true;
            this.lblCapSrc.Location = new System.Drawing.Point(10, 231);
            this.lblCapSrc.Name = "lblCapSrc";
            this.lblCapSrc.Size = new System.Drawing.Size(56, 17);
            this.lblCapSrc.Text = "捕获源:";

            // cboCaptureSrc (光电接收方式: Touch Probe 硬件锁存 / 普通数字输入 DI)
            this.cboCaptureSrc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cboCaptureSrc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCaptureSrc.Items.AddRange(new object[] { "Touch Probe (EXT1 锁存)", "数字输入 DI (0x60FD)" });
            this.cboCaptureSrc.Location = new System.Drawing.Point(68, 228);
            this.cboCaptureSrc.Name = "cboCaptureSrc";
            this.cboCaptureSrc.Size = new System.Drawing.Size(170, 25);
            this.cboCaptureSrc.SelectedIndex = 0;

            // lblDiBit
            this.lblDiBit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblDiBit.AutoSize = true;
            this.lblDiBit.Location = new System.Drawing.Point(246, 231);
            this.lblDiBit.Name = "lblDiBit";
            this.lblDiBit.Size = new System.Drawing.Size(44, 17);
            this.lblDiBit.Text = "DI位:";

            // numDiBit (DI 模式下 0x60FD 的第几位作物料传感器)
            this.numDiBit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numDiBit.Location = new System.Drawing.Point(290, 228);
            this.numDiBit.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.numDiBit.Name = "numDiBit";
            this.numDiBit.Size = new System.Drawing.Size(48, 23);

            // grpConveyor (传送带可视化)
            this.grpConveyor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.grpConveyor.Controls.Add(this.picConveyor);
            this.grpConveyor.Location = new System.Drawing.Point(580, 388);
            this.grpConveyor.Name = "grpConveyor";
            this.grpConveyor.Size = new System.Drawing.Size(568, 256);
            this.grpConveyor.Text = "传送带可视化 (物料随主轴推进; 黄色高亮 = 抓取区; 红色指针 = 抓取轴)";

            // picConveyor
            this.picConveyor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.picConveyor.BackColor = System.Drawing.Color.White;
            this.picConveyor.Location = new System.Drawing.Point(10, 22);
            this.picConveyor.Name = "picConveyor";
            this.picConveyor.Size = new System.Drawing.Size(548, 224);
            this.picConveyor.TabStop = false;

            // pnlAlarmBanner (报警状态横幅)
            this.pnlAlarmBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAlarmBanner.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            this.pnlAlarmBanner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmBanner);
            this.pnlAlarmBanner.Controls.Add(this.lblAlarmCount);
            this.pnlAlarmBanner.Controls.Add(this.btnAlarmAck);
            this.pnlAlarmBanner.Location = new System.Drawing.Point(12, 650);
            this.pnlAlarmBanner.Name = "pnlAlarmBanner";
            this.pnlAlarmBanner.Size = new System.Drawing.Size(1136, 34);

            // lblAlarmBanner
            this.lblAlarmBanner.AutoSize = false;
            this.lblAlarmBanner.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAlarmBanner.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmBanner.Location = new System.Drawing.Point(8, 6);
            this.lblAlarmBanner.Name = "lblAlarmBanner";
            this.lblAlarmBanner.Size = new System.Drawing.Size(760, 22);
            this.lblAlarmBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAlarmBanner.Text = "● 未连接";

            // lblAlarmCount
            this.lblAlarmCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAlarmCount.AutoSize = false;
            this.lblAlarmCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblAlarmCount.Location = new System.Drawing.Point(810, 8);
            this.lblAlarmCount.Name = "lblAlarmCount";
            this.lblAlarmCount.Size = new System.Drawing.Size(200, 18);
            this.lblAlarmCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAlarmCount.Text = "激活:0  故障:0  历史:0";

            // btnAlarmAck
            this.btnAlarmAck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAlarmAck.Enabled = false;
            this.btnAlarmAck.Location = new System.Drawing.Point(1026, 3);
            this.btnAlarmAck.Name = "btnAlarmAck";
            this.btnAlarmAck.Size = new System.Drawing.Size(100, 27);
            this.btnAlarmAck.Text = "报警复位";

            // grpAlarm
            this.grpAlarm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpAlarm.Controls.Add(this.gridAlarms);
            this.grpAlarm.Location = new System.Drawing.Point(12, 690);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(1136, 130);
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
            this.gridAlarms.Size = new System.Drawing.Size(1130, 108);

            // grpGlobal
            this.grpGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGlobal.Controls.Add(this.btnAllEnable);
            this.grpGlobal.Controls.Add(this.btnAllDisable);
            this.grpGlobal.Controls.Add(this.btnAllFaultReset);
            this.grpGlobal.Enabled = false;
            this.grpGlobal.Location = new System.Drawing.Point(12, 826);
            this.grpGlobal.Name = "grpGlobal";
            this.grpGlobal.Size = new System.Drawing.Size(1136, 64);
            this.grpGlobal.Text = "全局控制 (作用于勾选轴; 未勾选 = 全部轴)";

            // btnAllEnable
            this.btnAllEnable.Location = new System.Drawing.Point(14, 24);
            this.btnAllEnable.Name = "btnAllEnable";
            this.btnAllEnable.Size = new System.Drawing.Size(110, 30);
            this.btnAllEnable.Text = "全部使能";

            // btnAllDisable
            this.btnAllDisable.BackColor = System.Drawing.Color.MistyRose;
            this.btnAllDisable.Location = new System.Drawing.Point(132, 24);
            this.btnAllDisable.Name = "btnAllDisable";
            this.btnAllDisable.Size = new System.Drawing.Size(150, 30);
            this.btnAllDisable.Text = "全部去使能 (急停)";
            this.btnAllDisable.UseVisualStyleBackColor = false;

            // btnAllFaultReset
            this.btnAllFaultReset.Location = new System.Drawing.Point(290, 24);
            this.btnAllFaultReset.Name = "btnAllFaultReset";
            this.btnAllFaultReset.Size = new System.Drawing.Size(110, 30);
            this.btnAllFaultReset.Text = "全部故障复位";

            // grpLog
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 896);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(1136, 86);
            this.grpLog.Text = "日志";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 19);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1130, 64);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1160, 992);
            this.Controls.Add(this.lblMode);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblAxisCount);
            this.Controls.Add(this.grpMaster);
            this.Controls.Add(this.grpOverview);
            this.Controls.Add(this.grpTrack);
            this.Controls.Add(this.grpConveyor);
            this.Controls.Add(this.pnlAlarmBanner);
            this.Controls.Add(this.grpAlarm);
            this.Controls.Add(this.grpGlobal);
            this.Controls.Add(this.grpLog);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1080, 900);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "松下 A6 传送带主从同步 + 物料跟踪  by Darra EtherCAT SDK";

            ((System.ComponentModel.ISupportInitialize)(this.numMasterSpeed)).EndInit();
            this.grpMaster.ResumeLayout(false);
            this.grpMaster.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAxes)).EndInit();
            this.grpOverview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZoneFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZoneTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDemoInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiBit)).EndInit();
            this.grpTrack.ResumeLayout(false);
            this.grpTrack.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picConveyor)).EndInit();
            this.grpConveyor.ResumeLayout(false);
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
        private System.Windows.Forms.RadioButton rbVirtual;
        private System.Windows.Forms.RadioButton rbEncoder;
        private System.Windows.Forms.ComboBox cboEncoderAxis;
        private System.Windows.Forms.Label lblMasterSpeed;
        private System.Windows.Forms.NumericUpDown numMasterSpeed;
        private System.Windows.Forms.Button btnSyncStart;
        private System.Windows.Forms.Button btnSyncStop;
        private System.Windows.Forms.Button btnMasterJogRev;
        private System.Windows.Forms.Button btnMasterJogFwd;
        private System.Windows.Forms.Label lblMasterPos;

        private System.Windows.Forms.GroupBox grpOverview;
        private System.Windows.Forms.DataGridView gridAxes;

        private System.Windows.Forms.GroupBox grpTrack;
        private System.Windows.Forms.DataGridView gridProducts;
        private System.Windows.Forms.Button btnInjectProduct;
        private System.Windows.Forms.Button btnArmProbe;
        private System.Windows.Forms.Button btnClearProducts;
        private System.Windows.Forms.Label lblZoneFrom;
        private System.Windows.Forms.NumericUpDown numZoneFrom;
        private System.Windows.Forms.Label lblZoneTo;
        private System.Windows.Forms.NumericUpDown numZoneTo;
        private System.Windows.Forms.Label lblPickAxis;
        private System.Windows.Forms.ComboBox cboPickAxis;
        private System.Windows.Forms.CheckBox chkDemo;
        private System.Windows.Forms.Label lblDemoInt;
        private System.Windows.Forms.NumericUpDown numDemoInterval;
        private System.Windows.Forms.Label lblCapSrc;
        private System.Windows.Forms.ComboBox cboCaptureSrc;
        private System.Windows.Forms.Label lblDiBit;
        private System.Windows.Forms.NumericUpDown numDiBit;

        private System.Windows.Forms.GroupBox grpConveyor;
        private System.Windows.Forms.PictureBox picConveyor;

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
