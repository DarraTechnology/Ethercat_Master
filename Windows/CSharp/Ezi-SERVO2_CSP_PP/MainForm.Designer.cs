namespace Darra_EtherCAT_Test
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblMode = new System.Windows.Forms.Label();
            this.cboMode = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.lblDriveStateTitle = new System.Windows.Forms.Label();
            this.lblDriveState = new System.Windows.Forms.Label();
            this.lblStatusWordTitle = new System.Windows.Forms.Label();
            this.lblStatusWord = new System.Windows.Forms.Label();
            this.lblErrorCodeTitle = new System.Windows.Forms.Label();
            this.lblErrorCode = new System.Windows.Forms.Label();
            this.lblActualPositionTitle = new System.Windows.Forms.Label();
            this.lblActualPosition = new System.Windows.Forms.Label();
            this.lblTargetPositionTitle = new System.Windows.Forms.Label();
            this.lblTargetPositionDisplay = new System.Windows.Forms.Label();
            this.lblActualVelocityTitle = new System.Windows.Forms.Label();
            this.lblActualVelocity = new System.Windows.Forms.Label();
            this.grpCSP = new System.Windows.Forms.GroupBox();
            this.btnCSP_Enable = new System.Windows.Forms.Button();
            this.btnCSP_Disable = new System.Windows.Forms.Button();
            this.btnCSP_FaultReset = new System.Windows.Forms.Button();
            this.lblCSP_SpeedTitle = new System.Windows.Forms.Label();
            this.numCSP_StepPerCycle = new System.Windows.Forms.NumericUpDown();
            this.lblCSP_SpeedCalc = new System.Windows.Forms.Label();
            this.lblCSP_AbsTitle = new System.Windows.Forms.Label();
            this.numCSP_AbsPosition = new System.Windows.Forms.NumericUpDown();
            this.btnCSP_AbsMove = new System.Windows.Forms.Button();
            this.lblCSP_RelTitle = new System.Windows.Forms.Label();
            this.numCSP_RelDistance = new System.Windows.Forms.NumericUpDown();
            this.btnCSP_RelMove = new System.Windows.Forms.Button();
            this.btnCSP_Home = new System.Windows.Forms.Button();
            this.btnCSP_JogReverse = new System.Windows.Forms.Button();
            this.btnCSP_JogForward = new System.Windows.Forms.Button();
            this.grpPP = new System.Windows.Forms.GroupBox();
            this.btnPP_Enable = new System.Windows.Forms.Button();
            this.btnPP_Disable = new System.Windows.Forms.Button();
            this.btnPP_FaultReset = new System.Windows.Forms.Button();
            this.lblPP_SpeedTitle = new System.Windows.Forms.Label();
            this.numPP_ProfileVelocity = new System.Windows.Forms.NumericUpDown();
            this.lblPP_SpeedUnit = new System.Windows.Forms.Label();
            this.lblPP_AbsTitle = new System.Windows.Forms.Label();
            this.numPP_AbsPosition = new System.Windows.Forms.NumericUpDown();
            this.btnPP_AbsMove = new System.Windows.Forms.Button();
            this.lblPP_RelTitle = new System.Windows.Forms.Label();
            this.numPP_RelDistance = new System.Windows.Forms.NumericUpDown();
            this.btnPP_RelMove = new System.Windows.Forms.Button();
            this.btnPP_Home = new System.Windows.Forms.Button();
            this.btnPP_JogReverse = new System.Windows.Forms.Button();
            this.btnPP_JogForward = new System.Windows.Forms.Button();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.grpStatus.SuspendLayout();
            this.grpCSP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_StepPerCycle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_AbsPosition)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_RelDistance)).BeginInit();
            this.grpPP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_ProfileVelocity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_AbsPosition)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_RelDistance)).BeginInit();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblMode
            // 
            this.lblMode.AutoSize = true;
            this.lblMode.Location = new System.Drawing.Point(12, 14);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(59, 12);
            this.lblMode.TabIndex = 0;
            this.lblMode.Text = "运行模式:";
            // 
            // cboMode
            // 
            this.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMode.Items.AddRange(new object[] {
            "CSP (周期同步位置)",
            "PP (轮廓位置)"});
            this.cboMode.Location = new System.Drawing.Point(78, 11);
            this.cboMode.Name = "cboMode";
            this.cboMode.Size = new System.Drawing.Size(200, 20);
            this.cboMode.TabIndex = 1;
            this.cboMode.SelectedIndexChanged += new System.EventHandler(this.cboMode_SelectedIndexChanged);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(290, 9);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(55, 21);
            this.btnConnect.TabIndex = 2;
            this.btnConnect.Text = "连接";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(351, 9);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(55, 21);
            this.btnDisconnect.TabIndex = 3;
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(415, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(41, 12);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "未连接";
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.lblDriveStateTitle);
            this.grpStatus.Controls.Add(this.lblDriveState);
            this.grpStatus.Controls.Add(this.lblStatusWordTitle);
            this.grpStatus.Controls.Add(this.lblStatusWord);
            this.grpStatus.Controls.Add(this.lblErrorCodeTitle);
            this.grpStatus.Controls.Add(this.lblErrorCode);
            this.grpStatus.Controls.Add(this.lblActualPositionTitle);
            this.grpStatus.Controls.Add(this.lblActualPosition);
            this.grpStatus.Controls.Add(this.lblTargetPositionTitle);
            this.grpStatus.Controls.Add(this.lblTargetPositionDisplay);
            this.grpStatus.Controls.Add(this.lblActualVelocityTitle);
            this.grpStatus.Controls.Add(this.lblActualVelocity);
            this.grpStatus.Location = new System.Drawing.Point(12, 39);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(640, 74);
            this.grpStatus.TabIndex = 5;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "状态监控";
            // 
            // lblDriveStateTitle
            // 
            this.lblDriveStateTitle.AutoSize = true;
            this.lblDriveStateTitle.Location = new System.Drawing.Point(10, 20);
            this.lblDriveStateTitle.Name = "lblDriveStateTitle";
            this.lblDriveStateTitle.Size = new System.Drawing.Size(35, 12);
            this.lblDriveStateTitle.TabIndex = 0;
            this.lblDriveStateTitle.Text = "驱动:";
            // 
            // lblDriveState
            // 
            this.lblDriveState.AutoSize = true;
            this.lblDriveState.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblDriveState.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblDriveState.Location = new System.Drawing.Point(42, 20);
            this.lblDriveState.Name = "lblDriveState";
            this.lblDriveState.Size = new System.Drawing.Size(28, 14);
            this.lblDriveState.TabIndex = 1;
            this.lblDriveState.Text = "---";
            // 
            // lblStatusWordTitle
            // 
            this.lblStatusWordTitle.AutoSize = true;
            this.lblStatusWordTitle.Location = new System.Drawing.Point(250, 20);
            this.lblStatusWordTitle.Name = "lblStatusWordTitle";
            this.lblStatusWordTitle.Size = new System.Drawing.Size(47, 12);
            this.lblStatusWordTitle.TabIndex = 2;
            this.lblStatusWordTitle.Text = "状态字:";
            // 
            // lblStatusWord
            // 
            this.lblStatusWord.AutoSize = true;
            this.lblStatusWord.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblStatusWord.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblStatusWord.Location = new System.Drawing.Point(300, 20);
            this.lblStatusWord.Name = "lblStatusWord";
            this.lblStatusWord.Size = new System.Drawing.Size(49, 14);
            this.lblStatusWord.TabIndex = 3;
            this.lblStatusWord.Text = "0x0000";
            // 
            // lblErrorCodeTitle
            // 
            this.lblErrorCodeTitle.AutoSize = true;
            this.lblErrorCodeTitle.Location = new System.Drawing.Point(440, 20);
            this.lblErrorCodeTitle.Name = "lblErrorCodeTitle";
            this.lblErrorCodeTitle.Size = new System.Drawing.Size(47, 12);
            this.lblErrorCodeTitle.TabIndex = 4;
            this.lblErrorCodeTitle.Text = "错误码:";
            // 
            // lblErrorCode
            // 
            this.lblErrorCode.AutoSize = true;
            this.lblErrorCode.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblErrorCode.ForeColor = System.Drawing.Color.DarkRed;
            this.lblErrorCode.Location = new System.Drawing.Point(490, 20);
            this.lblErrorCode.Name = "lblErrorCode";
            this.lblErrorCode.Size = new System.Drawing.Size(49, 14);
            this.lblErrorCode.TabIndex = 5;
            this.lblErrorCode.Text = "0x0000";
            // 
            // lblActualPositionTitle
            // 
            this.lblActualPositionTitle.AutoSize = true;
            this.lblActualPositionTitle.Location = new System.Drawing.Point(10, 44);
            this.lblActualPositionTitle.Name = "lblActualPositionTitle";
            this.lblActualPositionTitle.Size = new System.Drawing.Size(59, 12);
            this.lblActualPositionTitle.TabIndex = 6;
            this.lblActualPositionTitle.Text = "实际位置:";
            // 
            // lblActualPosition
            // 
            this.lblActualPosition.AutoSize = true;
            this.lblActualPosition.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblActualPosition.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblActualPosition.Location = new System.Drawing.Point(72, 44);
            this.lblActualPosition.Name = "lblActualPosition";
            this.lblActualPosition.Size = new System.Drawing.Size(42, 14);
            this.lblActualPosition.TabIndex = 7;
            this.lblActualPosition.Text = "0.00°";
            // 
            // lblTargetPositionTitle
            // 
            this.lblTargetPositionTitle.AutoSize = true;
            this.lblTargetPositionTitle.Location = new System.Drawing.Point(250, 44);
            this.lblTargetPositionTitle.Name = "lblTargetPositionTitle";
            this.lblTargetPositionTitle.Size = new System.Drawing.Size(59, 12);
            this.lblTargetPositionTitle.TabIndex = 8;
            this.lblTargetPositionTitle.Text = "目标位置:";
            // 
            // lblTargetPositionDisplay
            // 
            this.lblTargetPositionDisplay.AutoSize = true;
            this.lblTargetPositionDisplay.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblTargetPositionDisplay.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblTargetPositionDisplay.Location = new System.Drawing.Point(312, 44);
            this.lblTargetPositionDisplay.Name = "lblTargetPositionDisplay";
            this.lblTargetPositionDisplay.Size = new System.Drawing.Size(42, 14);
            this.lblTargetPositionDisplay.TabIndex = 9;
            this.lblTargetPositionDisplay.Text = "0.00°";
            // 
            // lblActualVelocityTitle
            // 
            this.lblActualVelocityTitle.AutoSize = true;
            this.lblActualVelocityTitle.Location = new System.Drawing.Point(440, 44);
            this.lblActualVelocityTitle.Name = "lblActualVelocityTitle";
            this.lblActualVelocityTitle.Size = new System.Drawing.Size(35, 12);
            this.lblActualVelocityTitle.TabIndex = 10;
            this.lblActualVelocityTitle.Text = "速度:";
            // 
            // lblActualVelocity
            // 
            this.lblActualVelocity.AutoSize = true;
            this.lblActualVelocity.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblActualVelocity.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblActualVelocity.Location = new System.Drawing.Point(475, 44);
            this.lblActualVelocity.Name = "lblActualVelocity";
            this.lblActualVelocity.Size = new System.Drawing.Size(14, 14);
            this.lblActualVelocity.TabIndex = 11;
            this.lblActualVelocity.Text = "0";
            // 
            // grpCSP
            // 
            this.grpCSP.Controls.Add(this.btnCSP_Enable);
            this.grpCSP.Controls.Add(this.btnCSP_Disable);
            this.grpCSP.Controls.Add(this.btnCSP_FaultReset);
            this.grpCSP.Controls.Add(this.lblCSP_SpeedTitle);
            this.grpCSP.Controls.Add(this.numCSP_StepPerCycle);
            this.grpCSP.Controls.Add(this.lblCSP_SpeedCalc);
            this.grpCSP.Controls.Add(this.lblCSP_AbsTitle);
            this.grpCSP.Controls.Add(this.numCSP_AbsPosition);
            this.grpCSP.Controls.Add(this.btnCSP_AbsMove);
            this.grpCSP.Controls.Add(this.lblCSP_RelTitle);
            this.grpCSP.Controls.Add(this.numCSP_RelDistance);
            this.grpCSP.Controls.Add(this.btnCSP_RelMove);
            this.grpCSP.Controls.Add(this.btnCSP_Home);
            this.grpCSP.Controls.Add(this.btnCSP_JogReverse);
            this.grpCSP.Controls.Add(this.btnCSP_JogForward);
            this.grpCSP.Enabled = false;
            this.grpCSP.Location = new System.Drawing.Point(14, 246);
            this.grpCSP.Name = "grpCSP";
            this.grpCSP.Size = new System.Drawing.Size(640, 120);
            this.grpCSP.TabIndex = 6;
            this.grpCSP.TabStop = false;
            this.grpCSP.Text = "CSP 模式控制 (周期同步位置, Mode=8)";
            // 
            // btnCSP_Enable
            // 
            this.btnCSP_Enable.Location = new System.Drawing.Point(10, 23);
            this.btnCSP_Enable.Name = "btnCSP_Enable";
            this.btnCSP_Enable.Size = new System.Drawing.Size(60, 26);
            this.btnCSP_Enable.TabIndex = 0;
            this.btnCSP_Enable.Text = "使能";
            this.btnCSP_Enable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // btnCSP_Disable
            // 
            this.btnCSP_Disable.Location = new System.Drawing.Point(76, 23);
            this.btnCSP_Disable.Name = "btnCSP_Disable";
            this.btnCSP_Disable.Size = new System.Drawing.Size(60, 26);
            this.btnCSP_Disable.TabIndex = 1;
            this.btnCSP_Disable.Text = "去使能";
            this.btnCSP_Disable.Click += new System.EventHandler(this.btnDisable_Click);
            // 
            // btnCSP_FaultReset
            // 
            this.btnCSP_FaultReset.Location = new System.Drawing.Point(142, 23);
            this.btnCSP_FaultReset.Name = "btnCSP_FaultReset";
            this.btnCSP_FaultReset.Size = new System.Drawing.Size(70, 26);
            this.btnCSP_FaultReset.TabIndex = 2;
            this.btnCSP_FaultReset.Text = "故障复位";
            this.btnCSP_FaultReset.Click += new System.EventHandler(this.btnFaultReset_Click);
            // 
            // lblCSP_SpeedTitle
            // 
            this.lblCSP_SpeedTitle.AutoSize = true;
            this.lblCSP_SpeedTitle.Location = new System.Drawing.Point(280, 29);
            this.lblCSP_SpeedTitle.Name = "lblCSP_SpeedTitle";
            this.lblCSP_SpeedTitle.Size = new System.Drawing.Size(59, 12);
            this.lblCSP_SpeedTitle.TabIndex = 3;
            this.lblCSP_SpeedTitle.Text = "插值速度:";
            // 
            // numCSP_StepPerCycle
            // 
            this.numCSP_StepPerCycle.Location = new System.Drawing.Point(345, 26);
            this.numCSP_StepPerCycle.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numCSP_StepPerCycle.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCSP_StepPerCycle.Name = "numCSP_StepPerCycle";
            this.numCSP_StepPerCycle.Size = new System.Drawing.Size(70, 21);
            this.numCSP_StepPerCycle.TabIndex = 4;
            this.numCSP_StepPerCycle.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numCSP_StepPerCycle.ValueChanged += new System.EventHandler(this.numCSP_StepPerCycle_ValueChanged);
            // 
            // lblCSP_SpeedCalc
            // 
            this.lblCSP_SpeedCalc.AutoSize = true;
            this.lblCSP_SpeedCalc.ForeColor = System.Drawing.Color.Gray;
            this.lblCSP_SpeedCalc.Location = new System.Drawing.Point(420, 29);
            this.lblCSP_SpeedCalc.Name = "lblCSP_SpeedCalc";
            this.lblCSP_SpeedCalc.Size = new System.Drawing.Size(155, 12);
            this.lblCSP_SpeedCalc.TabIndex = 5;
            this.lblCSP_SpeedCalc.Text = "步/周期 (= 10000 脉冲/秒)";
            // 
            // lblCSP_AbsTitle
            // 
            this.lblCSP_AbsTitle.AutoSize = true;
            this.lblCSP_AbsTitle.Location = new System.Drawing.Point(10, 58);
            this.lblCSP_AbsTitle.Name = "lblCSP_AbsTitle";
            this.lblCSP_AbsTitle.Size = new System.Drawing.Size(59, 12);
            this.lblCSP_AbsTitle.TabIndex = 6;
            this.lblCSP_AbsTitle.Text = "绝对(°):";
            // 
            // numCSP_AbsPosition
            // 
            this.numCSP_AbsPosition.DecimalPlaces = 1;
            this.numCSP_AbsPosition.Location = new System.Drawing.Point(71, 55);
            this.numCSP_AbsPosition.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numCSP_AbsPosition.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.numCSP_AbsPosition.Name = "numCSP_AbsPosition";
            this.numCSP_AbsPosition.Size = new System.Drawing.Size(100, 21);
            this.numCSP_AbsPosition.TabIndex = 7;
            this.numCSP_AbsPosition.Value = new decimal(new int[] {
            360,
            0,
            0,
            0});
            // 
            // btnCSP_AbsMove
            // 
            this.btnCSP_AbsMove.Location = new System.Drawing.Point(176, 54);
            this.btnCSP_AbsMove.Name = "btnCSP_AbsMove";
            this.btnCSP_AbsMove.Size = new System.Drawing.Size(45, 23);
            this.btnCSP_AbsMove.TabIndex = 8;
            this.btnCSP_AbsMove.Text = "运行";
            this.btnCSP_AbsMove.Click += new System.EventHandler(this.btnAbsMove_Click);
            // 
            // lblCSP_RelTitle
            // 
            this.lblCSP_RelTitle.AutoSize = true;
            this.lblCSP_RelTitle.Location = new System.Drawing.Point(240, 58);
            this.lblCSP_RelTitle.Name = "lblCSP_RelTitle";
            this.lblCSP_RelTitle.Size = new System.Drawing.Size(59, 12);
            this.lblCSP_RelTitle.TabIndex = 9;
            this.lblCSP_RelTitle.Text = "相对(°):";
            // 
            // numCSP_RelDistance
            // 
            this.numCSP_RelDistance.DecimalPlaces = 1;
            this.numCSP_RelDistance.Location = new System.Drawing.Point(301, 55);
            this.numCSP_RelDistance.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numCSP_RelDistance.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.numCSP_RelDistance.Name = "numCSP_RelDistance";
            this.numCSP_RelDistance.Size = new System.Drawing.Size(100, 21);
            this.numCSP_RelDistance.TabIndex = 10;
            this.numCSP_RelDistance.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            // 
            // btnCSP_RelMove
            // 
            this.btnCSP_RelMove.Location = new System.Drawing.Point(406, 54);
            this.btnCSP_RelMove.Name = "btnCSP_RelMove";
            this.btnCSP_RelMove.Size = new System.Drawing.Size(45, 23);
            this.btnCSP_RelMove.TabIndex = 11;
            this.btnCSP_RelMove.Text = "运行";
            this.btnCSP_RelMove.Click += new System.EventHandler(this.btnRelMove_Click);
            // 
            // btnCSP_Home
            // 
            this.btnCSP_Home.Location = new System.Drawing.Point(460, 54);
            this.btnCSP_Home.Name = "btnCSP_Home";
            this.btnCSP_Home.Size = new System.Drawing.Size(45, 23);
            this.btnCSP_Home.TabIndex = 12;
            this.btnCSP_Home.Text = "回零";
            this.btnCSP_Home.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // btnCSP_JogReverse
            // 
            this.btnCSP_JogReverse.Location = new System.Drawing.Point(10, 86);
            this.btnCSP_JogReverse.Name = "btnCSP_JogReverse";
            this.btnCSP_JogReverse.Size = new System.Drawing.Size(90, 26);
            this.btnCSP_JogReverse.TabIndex = 13;
            this.btnCSP_JogReverse.Text = "◄ 点动";
            this.btnCSP_JogReverse.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogReverse_MouseDown);
            this.btnCSP_JogReverse.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJogReverse_MouseUp);
            // 
            // btnCSP_JogForward
            // 
            this.btnCSP_JogForward.Location = new System.Drawing.Point(106, 86);
            this.btnCSP_JogForward.Name = "btnCSP_JogForward";
            this.btnCSP_JogForward.Size = new System.Drawing.Size(90, 26);
            this.btnCSP_JogForward.TabIndex = 14;
            this.btnCSP_JogForward.Text = "点动 ►";
            this.btnCSP_JogForward.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogForward_MouseDown);
            this.btnCSP_JogForward.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJogForward_MouseUp);
            // 
            // grpPP
            // 
            this.grpPP.Controls.Add(this.btnPP_Enable);
            this.grpPP.Controls.Add(this.btnPP_Disable);
            this.grpPP.Controls.Add(this.btnPP_FaultReset);
            this.grpPP.Controls.Add(this.lblPP_SpeedTitle);
            this.grpPP.Controls.Add(this.numPP_ProfileVelocity);
            this.grpPP.Controls.Add(this.lblPP_SpeedUnit);
            this.grpPP.Controls.Add(this.lblPP_AbsTitle);
            this.grpPP.Controls.Add(this.numPP_AbsPosition);
            this.grpPP.Controls.Add(this.btnPP_AbsMove);
            this.grpPP.Controls.Add(this.lblPP_RelTitle);
            this.grpPP.Controls.Add(this.numPP_RelDistance);
            this.grpPP.Controls.Add(this.btnPP_RelMove);
            this.grpPP.Controls.Add(this.btnPP_Home);
            this.grpPP.Controls.Add(this.btnPP_JogReverse);
            this.grpPP.Controls.Add(this.btnPP_JogForward);
            this.grpPP.Enabled = false;
            this.grpPP.Location = new System.Drawing.Point(12, 120);
            this.grpPP.Name = "grpPP";
            this.grpPP.Size = new System.Drawing.Size(640, 120);
            this.grpPP.TabIndex = 7;
            this.grpPP.TabStop = false;
            this.grpPP.Text = "PP 模式控制 (轮廓位置, Mode=1)";
            // 
            // btnPP_Enable
            // 
            this.btnPP_Enable.Location = new System.Drawing.Point(10, 23);
            this.btnPP_Enable.Name = "btnPP_Enable";
            this.btnPP_Enable.Size = new System.Drawing.Size(60, 26);
            this.btnPP_Enable.TabIndex = 0;
            this.btnPP_Enable.Text = "使能";
            this.btnPP_Enable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // btnPP_Disable
            // 
            this.btnPP_Disable.Location = new System.Drawing.Point(76, 23);
            this.btnPP_Disable.Name = "btnPP_Disable";
            this.btnPP_Disable.Size = new System.Drawing.Size(60, 26);
            this.btnPP_Disable.TabIndex = 1;
            this.btnPP_Disable.Text = "去使能";
            this.btnPP_Disable.Click += new System.EventHandler(this.btnDisable_Click);
            // 
            // btnPP_FaultReset
            // 
            this.btnPP_FaultReset.Location = new System.Drawing.Point(142, 23);
            this.btnPP_FaultReset.Name = "btnPP_FaultReset";
            this.btnPP_FaultReset.Size = new System.Drawing.Size(70, 26);
            this.btnPP_FaultReset.TabIndex = 2;
            this.btnPP_FaultReset.Text = "故障复位";
            this.btnPP_FaultReset.Click += new System.EventHandler(this.btnFaultReset_Click);
            // 
            // lblPP_SpeedTitle
            // 
            this.lblPP_SpeedTitle.AutoSize = true;
            this.lblPP_SpeedTitle.Location = new System.Drawing.Point(280, 29);
            this.lblPP_SpeedTitle.Name = "lblPP_SpeedTitle";
            this.lblPP_SpeedTitle.Size = new System.Drawing.Size(59, 12);
            this.lblPP_SpeedTitle.TabIndex = 3;
            this.lblPP_SpeedTitle.Text = "轮廓速度:";
            // 
            // numPP_ProfileVelocity
            // 
            this.numPP_ProfileVelocity.Location = new System.Drawing.Point(345, 26);
            this.numPP_ProfileVelocity.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.numPP_ProfileVelocity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPP_ProfileVelocity.Name = "numPP_ProfileVelocity";
            this.numPP_ProfileVelocity.Size = new System.Drawing.Size(90, 21);
            this.numPP_ProfileVelocity.TabIndex = 4;
            this.numPP_ProfileVelocity.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // lblPP_SpeedUnit
            // 
            this.lblPP_SpeedUnit.AutoSize = true;
            this.lblPP_SpeedUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblPP_SpeedUnit.Location = new System.Drawing.Point(440, 29);
            this.lblPP_SpeedUnit.Name = "lblPP_SpeedUnit";
            this.lblPP_SpeedUnit.Size = new System.Drawing.Size(47, 12);
            this.lblPP_SpeedUnit.TabIndex = 5;
            this.lblPP_SpeedUnit.Text = "脉冲/秒";
            // 
            // lblPP_AbsTitle
            // 
            this.lblPP_AbsTitle.AutoSize = true;
            this.lblPP_AbsTitle.Location = new System.Drawing.Point(10, 58);
            this.lblPP_AbsTitle.Name = "lblPP_AbsTitle";
            this.lblPP_AbsTitle.Size = new System.Drawing.Size(59, 12);
            this.lblPP_AbsTitle.TabIndex = 6;
            this.lblPP_AbsTitle.Text = "绝对(°):";
            // 
            // numPP_AbsPosition
            // 
            this.numPP_AbsPosition.DecimalPlaces = 1;
            this.numPP_AbsPosition.Location = new System.Drawing.Point(71, 55);
            this.numPP_AbsPosition.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numPP_AbsPosition.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.numPP_AbsPosition.Name = "numPP_AbsPosition";
            this.numPP_AbsPosition.Size = new System.Drawing.Size(100, 21);
            this.numPP_AbsPosition.TabIndex = 7;
            this.numPP_AbsPosition.Value = new decimal(new int[] {
            360,
            0,
            0,
            0});
            // 
            // btnPP_AbsMove
            // 
            this.btnPP_AbsMove.Location = new System.Drawing.Point(176, 54);
            this.btnPP_AbsMove.Name = "btnPP_AbsMove";
            this.btnPP_AbsMove.Size = new System.Drawing.Size(45, 23);
            this.btnPP_AbsMove.TabIndex = 8;
            this.btnPP_AbsMove.Text = "运行";
            this.btnPP_AbsMove.Click += new System.EventHandler(this.btnAbsMove_Click);
            // 
            // lblPP_RelTitle
            // 
            this.lblPP_RelTitle.AutoSize = true;
            this.lblPP_RelTitle.Location = new System.Drawing.Point(240, 58);
            this.lblPP_RelTitle.Name = "lblPP_RelTitle";
            this.lblPP_RelTitle.Size = new System.Drawing.Size(59, 12);
            this.lblPP_RelTitle.TabIndex = 9;
            this.lblPP_RelTitle.Text = "相对(°):";
            // 
            // numPP_RelDistance
            // 
            this.numPP_RelDistance.DecimalPlaces = 1;
            this.numPP_RelDistance.Location = new System.Drawing.Point(301, 55);
            this.numPP_RelDistance.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numPP_RelDistance.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.numPP_RelDistance.Name = "numPP_RelDistance";
            this.numPP_RelDistance.Size = new System.Drawing.Size(100, 21);
            this.numPP_RelDistance.TabIndex = 10;
            this.numPP_RelDistance.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            // 
            // btnPP_RelMove
            // 
            this.btnPP_RelMove.Location = new System.Drawing.Point(406, 54);
            this.btnPP_RelMove.Name = "btnPP_RelMove";
            this.btnPP_RelMove.Size = new System.Drawing.Size(45, 23);
            this.btnPP_RelMove.TabIndex = 11;
            this.btnPP_RelMove.Text = "运行";
            this.btnPP_RelMove.Click += new System.EventHandler(this.btnRelMove_Click);
            // 
            // btnPP_Home
            // 
            this.btnPP_Home.Location = new System.Drawing.Point(460, 54);
            this.btnPP_Home.Name = "btnPP_Home";
            this.btnPP_Home.Size = new System.Drawing.Size(45, 23);
            this.btnPP_Home.TabIndex = 12;
            this.btnPP_Home.Text = "回零";
            this.btnPP_Home.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // btnPP_JogReverse
            // 
            this.btnPP_JogReverse.Location = new System.Drawing.Point(10, 86);
            this.btnPP_JogReverse.Name = "btnPP_JogReverse";
            this.btnPP_JogReverse.Size = new System.Drawing.Size(90, 26);
            this.btnPP_JogReverse.TabIndex = 13;
            this.btnPP_JogReverse.Text = "◄ 点动";
            this.btnPP_JogReverse.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogReverse_MouseDown);
            this.btnPP_JogReverse.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJogReverse_MouseUp);
            // 
            // btnPP_JogForward
            // 
            this.btnPP_JogForward.Location = new System.Drawing.Point(106, 86);
            this.btnPP_JogForward.Name = "btnPP_JogForward";
            this.btnPP_JogForward.Size = new System.Drawing.Size(90, 26);
            this.btnPP_JogForward.TabIndex = 14;
            this.btnPP_JogForward.Text = "点动 ►";
            this.btnPP_JogForward.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogForward_MouseDown);
            this.btnPP_JogForward.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJogForward_MouseUp);
            // 
            // grpLog
            // 
            this.grpLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 372);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(644, 301);
            this.grpLog.TabIndex = 8;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "日志";
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(3, 17);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(638, 281);
            this.txtLog.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(668, 683);
            this.Controls.Add(this.lblMode);
            this.Controls.Add(this.cboMode);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.grpCSP);
            this.Controls.Add(this.grpPP);
            this.Controls.Add(this.grpLog);
            this.MinimumSize = new System.Drawing.Size(680, 474);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ezi-SERVO2  by Darra EtherCAT SDK";
            this.grpStatus.ResumeLayout(false);
            this.grpStatus.PerformLayout();
            this.grpCSP.ResumeLayout(false);
            this.grpCSP.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_StepPerCycle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_AbsPosition)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCSP_RelDistance)).EndInit();
            this.grpPP.ResumeLayout(false);
            this.grpPP.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_ProfileVelocity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_AbsPosition)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPP_RelDistance)).EndInit();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // 连接区域
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.ComboBox cboMode;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;

        // 状态监控
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.Label lblDriveStateTitle;
        private System.Windows.Forms.Label lblDriveState;
        private System.Windows.Forms.Label lblStatusWordTitle;
        private System.Windows.Forms.Label lblStatusWord;
        private System.Windows.Forms.Label lblErrorCodeTitle;
        private System.Windows.Forms.Label lblErrorCode;
        private System.Windows.Forms.Label lblActualPositionTitle;
        private System.Windows.Forms.Label lblActualPosition;
        private System.Windows.Forms.Label lblTargetPositionTitle;
        private System.Windows.Forms.Label lblTargetPositionDisplay;
        private System.Windows.Forms.Label lblActualVelocityTitle;
        private System.Windows.Forms.Label lblActualVelocity;

        // CSP 控制
        private System.Windows.Forms.GroupBox grpCSP;
        private System.Windows.Forms.Button btnCSP_Enable;
        private System.Windows.Forms.Button btnCSP_Disable;
        private System.Windows.Forms.Button btnCSP_FaultReset;
        private System.Windows.Forms.Label lblCSP_SpeedTitle;
        private System.Windows.Forms.NumericUpDown numCSP_StepPerCycle;
        private System.Windows.Forms.Label lblCSP_SpeedCalc;
        private System.Windows.Forms.Label lblCSP_AbsTitle;
        private System.Windows.Forms.NumericUpDown numCSP_AbsPosition;
        private System.Windows.Forms.Button btnCSP_AbsMove;
        private System.Windows.Forms.Label lblCSP_RelTitle;
        private System.Windows.Forms.NumericUpDown numCSP_RelDistance;
        private System.Windows.Forms.Button btnCSP_RelMove;
        private System.Windows.Forms.Button btnCSP_JogReverse;
        private System.Windows.Forms.Button btnCSP_JogForward;
        private System.Windows.Forms.Button btnCSP_Home;

        // PP 控制
        private System.Windows.Forms.GroupBox grpPP;
        private System.Windows.Forms.Button btnPP_Enable;
        private System.Windows.Forms.Button btnPP_Disable;
        private System.Windows.Forms.Button btnPP_FaultReset;
        private System.Windows.Forms.Label lblPP_SpeedTitle;
        private System.Windows.Forms.NumericUpDown numPP_ProfileVelocity;
        private System.Windows.Forms.Label lblPP_SpeedUnit;
        private System.Windows.Forms.Label lblPP_AbsTitle;
        private System.Windows.Forms.NumericUpDown numPP_AbsPosition;
        private System.Windows.Forms.Button btnPP_AbsMove;
        private System.Windows.Forms.Label lblPP_RelTitle;
        private System.Windows.Forms.NumericUpDown numPP_RelDistance;
        private System.Windows.Forms.Button btnPP_RelMove;
        private System.Windows.Forms.Button btnPP_JogReverse;
        private System.Windows.Forms.Button btnPP_JogForward;
        private System.Windows.Forms.Button btnPP_Home;

        // 日志
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox txtLog;
    }
}
