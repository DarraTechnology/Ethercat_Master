using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace Darra_EtherCAT_Test
{
    // ==================== CSP 模式 PDO (RxPDO 0x1600 / TxPDO 0x1A00) ====================
    // CSP 模式必须启用 DC Sync Event, 否则驱动器报 E-073 错误

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CSP_Output  // 12 bytes
    {
        public ushort ControlWord;        // 0x6040:0  UINT
        public int TargetPosition;        // 0x607A:0  DINT
        public uint PhysicalOutputs;      // 0x60FE:1  UDINT
        public ushort TouchProbeFunction; // 0x60B8:0  UINT
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CSP_Input  // 22 bytes
    {
        public ushort StatusWord;              // 0x6041:0  UINT
        public int PositionActualValue;        // 0x6064:0  DINT
        public uint DigitalInputs;             // 0x60FD:0  UDINT
        public ushort ErrorCode;               // 0x603F:0  UINT
        public ushort TouchProbeStatus;        // 0x60B9:0  UINT
        public int TouchProbe1PositiveValue;   // 0x60BA:0  DINT
        public int TouchProbe2PositiveValue;   // 0x60BC:0  DINT
    }

    // ==================== PP 模式 PDO (RxPDO 0x1601 / TxPDO 0x1A01) ====================
    // PP 模式: 驱动器内部生成轨迹, 使用 FreeRun 同步 (无需 DC Sync)

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PP_Output  // 11 bytes (手册默认 RxPDO 0x1601: CW+TP+PV+MO, 无 PhysicalOutputs)
    {
        public ushort ControlWord;       // 0x6040:0  UINT
        public int TargetPosition;       // 0x607A:0  DINT
        public uint ProfileVelocity;     // 0x6081:0  UDINT
        public sbyte ModesOfOperation;   // 0x6060:0  SINT
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PP_Input  // 17 bytes
    {
        public ushort StatusWord;              // 0x6041:0  UINT
        public int PositionActualValue;        // 0x6064:0  DINT
        public int VelocityActualValue;        // 0x606C:0  DINT
        public uint DigitalInputs;             // 0x60FD:0  UDINT
        public ushort ErrorCode;               // 0x603F:0  UINT
        public sbyte ModesOfOperationDisplay;  // 0x6061:0  SINT
    }

    /// <summary>
    /// Ezi-SERVO2 运动控制主窗体
    /// CSP (周期同步位置) 和 PP (轮廓位置) 分开控制
    /// </summary>
    public partial class MainForm : Form
    {
        DarraEtherCAT master;
        volatile bool isRunning = false;
        bool isCSP = true;
        // 连接取消令牌 — Disconnect/FormClosing 时取消正在进行的连接
        private CancellationTokenSource _connectCts;

        // 异步日志系统: 避免高频 Invoke 卡死 UI
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int LOG_MAX_TEXT = 50000;   // TextBox 最大字符数
        private const int LOG_QUEUE_CAP = 500;    // 队列上限, 超过丢弃
        private int _processedLogCount;            // DLL Logs 已处理的条目数

        const int SLAVE_IDX = 0;
        const int PULSES_PER_REV = 10000;

        // 控制变量 (UI 写, PDO 线程读)
        volatile bool servoEnabled = false;
        volatile bool faultReset = false;
        volatile int desiredPosition = 0;
        volatile bool triggerMove = false;
        volatile bool jogForward = false;
        volatile bool jogReverse = false;
        volatile int _cachedCSPStep = 10;         // numCSP_StepPerCycle 缓存 (UI写, PDO读)
        volatile int _cachedPPVelocity = 10000;   // numPP_ProfileVelocity 缓存 (UI写, PDO读)

        // 状态快照 (PDO 线程写, UI 读)
        volatile ushort snapStatusWord;
        volatile int snapActualPosition;
        volatile int snapTargetPosition;
        volatile int snapActualVelocity;
        volatile ushort snapErrorCode;
        volatile string snapDriveState = "---";

        public MainForm()
        {
            InitializeComponent();
            // 缓存控件初始值, 并订阅 ValueChanged 更新缓存 (避免 PDO 线程跨线程访问控件)
            _cachedCSPStep = (int)numCSP_StepPerCycle.Value;
            _cachedPPVelocity = (int)numPP_ProfileVelocity.Value;
            numPP_ProfileVelocity.ValueChanged += (s, ev) => _cachedPPVelocity = (int)numPP_ProfileVelocity.Value;
            _logFlushTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _logFlushTimer.Tick += LogFlushTimer_Tick;
            _logFlushTimer.Start();
        }

        // ==================== CiA 402 ====================

        static string ParseDriveState(ushort sw)
        {
            if ((sw & 0x4F) == 0x00) return "NotReadyToSwitchOn";
            if ((sw & 0x6F) == 0x40) return "SwitchOnDisabled";
            if ((sw & 0x6F) == 0x21) return "ReadyToSwitchOn";
            if ((sw & 0x6F) == 0x23) return "SwitchedOn";
            if ((sw & 0x6F) == 0x27) return "OperationEnabled";
            if ((sw & 0x6F) == 0x07) return "QuickStopActive";
            if ((sw & 0x4F) == 0x0F) return "FaultReaction";
            if ((sw & 0x4F) == 0x08) return "Fault";
            return "Unknown";
        }

        static bool IsOperationEnabled(ushort sw) => (sw & 0x6F) == 0x27;
        static bool IsSwitchOnDisabled(ushort sw) => (sw & 0x6F) == 0x40;
        static bool IsReadyToSwitchOn(ushort sw) => (sw & 0x6F) == 0x21;
        static bool IsSwitchedOn(ushort sw) => (sw & 0x6F) == 0x23;
        static bool IsFault(ushort sw) => (sw & 0x4F) == 0x08;

        // ==================== 模式切换 ====================

        void cboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 两个面板始终可见, 仅切换哪个启用
            // 连接前: 都禁用; 连接后不可切换 (cboMode.Enabled=false)
        }

        // ==================== 速度换算 ====================

        void numCSP_StepPerCycle_ValueChanged(object sender, EventArgs e)
        {
            int val = (int)numCSP_StepPerCycle.Value;
            _cachedCSPStep = val;
            lblCSP_SpeedCalc.Text = $"步/周期 (= {val * 1000} 脉冲/秒)";
        }

        // ==================== 连接 ====================

        string GetDeniPath()
        {
            string mode = cboMode.SelectedIndex == 0 ? "CSP" : "PP";
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDir, mode, "EtherCATRestore", "config.deni");
        }

        async void btnConnect_Click(object sender, EventArgs e)
        {
            isCSP = cboMode.SelectedIndex == 0;
            string deniPath = GetDeniPath();

            if (!File.Exists(deniPath))
            {
                MessageBox.Show($"配置文件不存在: {deniPath}\n请确认编译输出目录包含导出代码", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 立即禁用连接按钮, 启用断开按钮 (允许用户在连接过程中取消)
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            cboMode.Enabled = false;

            _connectCts = new CancellationTokenSource();
            var ct = _connectCts.Token;
            int ppProfileVelocity = (int)numPP_ProfileVelocity.Value;

            Log($"正在初始化主站 ({(isCSP ? "CSP" : "PP")} 模式)...");

            // 在后台线程执行所有耗时的 DLL 操作, 保持 UI 响应
            DarraEtherCAT newMaster = null;
            string errorMsg = null;
            bool success = await Task.Run<bool>(() =>
            {
                try
                {
                    // 构建主站
                    var buildResult = new DarraEtherCAT()
                        .SetENI(deniPath)
                        .EnableAutoStartup()
                        .Build();

                    if (!buildResult.Success)
                    {
                        errorMsg = $"主站初始化失败: {buildResult.Message}";
                        try { buildResult.Master?.Close(); } catch (Exception cex) { System.Diagnostics.Debug.WriteLine($"[Connect] 失败清理 Master.Close 异常: {cex.GetType().Name}: {cex.Message}"); }
                        return false;
                    }

                    newMaster = buildResult.Master;
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // 状态转换: Init → PreOp → SafeOp → OP
                    if (!newMaster.SetState(EcState.PreOp)) { errorMsg = "PreOp 状态转换失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    var slave = newMaster.Slaves[SLAVE_IDX];

                    if (isCSP)
                    {
                        // CSP 模式必须启用 DC Sync Event (手册 Section 4.4)
                        // 不启用 DC 则驱动器报 E-073 SynchronizationType Error
                        if (slave.HasDC)
                        {
                            slave.ConfigureDC(1000000); // Sync0 = 1ms, 匹配 CycleTime
                            System.Diagnostics.Debug.WriteLine("[CSP] DC Sync0 已启用 (1ms)");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[CSP] 警告: 从站不支持 DC, CSP 可能报 E-073");
                        }

                        // 非 Offload 模式在 1ms 周期下容易丢帧, 需要提高容错
                        if (slave.CoE != null)
                        {
                            // 0x10F1:02 Sync Error Counter Limit (默认12, +3/-1 → 连续丢4帧触发 7500h)
                            // 设为 uint16 最大值, 允许最大容忍
                            slave.CoE.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));
                            System.Diagnostics.Debug.WriteLine("[CSP] SyncErrorCounterLimit(0x10F1:02) = 65535");
                        }
                    }
                    else
                    {
                        // PP 模式: Free Run 同步 (不使用 DC Sync0)
                        // 驱动器使用内部定时器驱动轨迹生成器, 不依赖主站实时性
                        if (slave.CoE != null)
                        {
                            // Free Run 同步: SM2/SM3 SyncType = 0x0000
                            slave.CoE.SDOWrite(0x1C32, 1, BitConverter.GetBytes((ushort)0x0000));
                            slave.CoE.SDOWrite(0x1C33, 1, BitConverter.GetBytes((ushort)0x0000));

                            // 同步误差容限 (非实时主站)
                            slave.CoE.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));

                            // 忽略硬件限位 (0x2003=2: PP/Homing/CSP 均不响应)
                            // 无物理限位开关时 0x60FD 可能误报激活, 导致 PP 模式拒绝运动
                            slave.CoE.SDOWrite(0x2003, 0, new byte[] { 2 });

                            // 轮廓参数
                            slave.CoE.SDOWrite(0x6081, 0, BitConverter.GetBytes((uint)ppProfileVelocity));
                            byte[] v6083 = slave.CoE.SDORead(0x6083, 0);
                            byte[] v6084 = slave.CoE.SDORead(0x6084, 0);
                            if ((v6083?.Length >= 4 ? BitConverter.ToUInt32(v6083, 0) : 0) == 0)
                                slave.CoE.SDOWrite(0x6083, 0, BitConverter.GetBytes((uint)500000));
                            if ((v6084?.Length >= 4 ? BitConverter.ToUInt32(v6084, 0) : 0) == 0)
                                slave.CoE.SDOWrite(0x6084, 0, BitConverter.GetBytes((uint)500000));

                            // 定位容差 (默认0=精确到达, 设为10留容差)
                            slave.CoE.SDOWrite(0x6067, 0, BitConverter.GetBytes((uint)10));

                            // 预写目标位置 = 当前位置
                            byte[] v6064 = slave.CoE.SDORead(0x6064, 0);
                            int curPos = (v6064?.Length >= 4) ? BitConverter.ToInt32(v6064, 0) : 0;
                            slave.CoE.SDOWrite(0x607A, 0, BitConverter.GetBytes(curPos));
                        }
                    }

                    if (!newMaster.SetState(EcState.SafeOp)) { errorMsg = "SafeOp 状态转换失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // PDO 初始化 (SafeOp 后, OP 前)
                    if (isCSP)
                    {
                        ref var input = ref newMaster.Slaves[SLAVE_IDX].PDO.InputsMapping<CSP_Input>();
                        ref var output = ref newMaster.Slaves[SLAVE_IDX].PDO.OutputsMapping<CSP_Output>();
                        output.TargetPosition = input.PositionActualValue;
                    }
                    else
                    {
                        ref var input = ref newMaster.Slaves[SLAVE_IDX].PDO.InputsMapping<PP_Input>();
                        ref var output = ref newMaster.Slaves[SLAVE_IDX].PDO.OutputsMapping<PP_Output>();
                        output.ModesOfOperation = 1;
                        output.TargetPosition = input.PositionActualValue;
                        output.ProfileVelocity = (uint)ppProfileVelocity;
                    }

                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    if (!newMaster.SetState(EcState.OP)) { errorMsg = "OP 状态转换失败"; return false; }

                    // PP: 设置 PDO 输出初始值
                    if (!isCSP)
                    {
                        ref var tOut = ref newMaster.Slaves[SLAVE_IDX].PDO.OutputsMapping<PP_Output>();
                        ref var tIn = ref newMaster.Slaves[SLAVE_IDX].PDO.InputsMapping<PP_Input>();
                        tOut.ControlWord = 0;
                        tOut.ModesOfOperation = 1;
                        tOut.TargetPosition = tIn.PositionActualValue;
                        tOut.ProfileVelocity = (uint)ppProfileVelocity;
                        System.Diagnostics.Debug.WriteLine($"[PP] PDO输出初始化: TP={tOut.TargetPosition} PV={tOut.ProfileVelocity}");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                    return false;
                }
            });

            // 回到 UI 线程
            if (!success || ct.IsCancellationRequested)
            {
                Log(errorMsg ?? "已取消");
                if (newMaster != null)
                {
                    // 后台清理, 不阻塞 UI
                    var cleanup = newMaster;
                    newMaster = null;
                    _ = Task.Run(() => { try { cleanup.Close(); } catch (Exception cex) { System.Diagnostics.Debug.WriteLine($"[Connect] 后台清理 cleanup.Close 异常: {cex.GetType().Name}: {cex.Message}"); } });
                }
                // 恢复 UI (仅当 Disconnect 没有先执行时)
                if (master == null)
                {
                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                    cboMode.Enabled = true;
                }
                return;
            }

            master = newMaster;
            desiredPosition = isCSP
                ? master.Slaves[SLAVE_IDX].PDO.InputsMapping<CSP_Input>().PositionActualValue
                : master.Slaves[SLAVE_IDX].PDO.InputsMapping<PP_Input>().PositionActualValue;

            RegisterEvents();
            DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
            _processedLogCount = DarraEtherCAT.Logs.Count; // SetFilter 之后取 Count, 避免过滤变化导致重复
            DarraEtherCAT.Logs.Updated += OnLogUpdated;

            isRunning = true;
            new Thread(PdoControlLoop) { IsBackground = true }.Start();
            new Thread(StatusUpdateLoop) { IsBackground = true }.Start();

            grpCSP.Enabled = isCSP;
            grpPP.Enabled = !isCSP;
            lblStatus.Text = $"已连接 ({(isCSP ? "CSP" : "PP")})";
            lblStatus.ForeColor = System.Drawing.Color.Green;

            Log("连接成功, 可以开始控制电机");
        }

        // ==================== 事件 ====================

        void RegisterEvents()
        {
            master.Events.StateChanged += (s, ev) =>
                Log($"[状态] 主站: {ev.OldState} -> {ev.NewState}");
            master.Events.SlaveStateChanged += (mi, si, os, ns) =>
                Log($"[状态] 从站{si}: {os} -> {ns}");
            master.Events.EmergencyEvent += (mi, si, code, reg, b1, w1, w2) =>
                Log($"[紧急] 从站{si} 错误码: 0x{code:X4}");
            master.Events.SlaveOffline += (si) =>
                Log($"[离线] 从站{si} 已断开");
            master.Events.SlaveOnline += (si) =>
                Log($"[上线] 从站{si} 已恢复");
            master.Events.PDOFrameLoss += (mi, grp, con, tot) =>
                Log($"[丢帧] 组{grp} 连续: {con}, 累计: {tot}");
        }

        void OnLogUpdated()
        {
            try
            {
                var logs = DarraEtherCAT.Logs;
                int total = logs.Count;
                if (_processedLogCount > total)
                    _processedLogCount = 0; // 日志被清空/截断
                for (int i = _processedLogCount; i < total; i++)
                    Log($"[DLL] {logs[i]}");
                _processedLogCount = total;
            }
            catch (Exception ex)
            {
                // 日志回调本身不能再抛, 否则会污染调用方; 仅写 Debug 输出便于排查
                System.Diagnostics.Debug.WriteLine($"[OnLogUpdated] 异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ==================== PDO 控制线程 ====================

        void PdoControlLoop()
        {
            if (isCSP) PdoControlLoop_CSP();
            else PdoControlLoop_PP();
        }

        void PdoControlLoop_CSP()
        {
            try
            {
                var m = master;
                if (m == null) return;
                ref var input = ref m.Slaves[SLAVE_IDX].PDO.InputsMapping<CSP_Input>();
                ref var output = ref m.Slaves[SLAVE_IDX].PDO.OutputsMapping<CSP_Output>();
                int currentTarget = input.PositionActualValue;

                string lastState = "";
                ushort lastCW = 0xFFFF;
                int periodicCount = 0;

                System.Diagnostics.Debug.WriteLine("[CSP] PDO控制线程启动");

                while (isRunning && master != null)
                {
                    ushort sw = input.StatusWord;
                    ushort cw = 0;
                    string branch = "";

                    if (faultReset) { cw = 0x80; faultReset = false; branch = "FaultReset"; }
                    else if (IsFault(sw)) { branch = "Fault"; }
                    else if (!servoEnabled)
                    {
                        currentTarget = input.PositionActualValue;
                        desiredPosition = currentTarget;
                        branch = "Disabled";
                    }
                    else if ((sw & 0x4F) == 0x00)
                    {
                        // NotReadyToSwitchOn: 等待驱动器内部初始化完成 (自动转换到 SwitchOnDisabled)
                        branch = "NotReadyToSwitchOn(等待)";
                    }
                    else if (IsSwitchOnDisabled(sw))
                    {
                        cw = 0x06;
                        currentTarget = input.PositionActualValue;
                        desiredPosition = currentTarget;
                        branch = "SwitchOnDisabled->Shutdown";
                    }
                    else if (IsReadyToSwitchOn(sw))
                    {
                        cw = 0x07;
                        currentTarget = input.PositionActualValue;
                        output.TargetPosition = currentTarget;
                        branch = "ReadyToSwitchOn->SwitchOn";
                    }
                    else if (IsSwitchedOn(sw))
                    {
                        currentTarget = input.PositionActualValue;
                        output.TargetPosition = currentTarget;
                        cw = 0x0F;
                        branch = "SwitchedOn->EnableOp";
                    }
                    else if (IsOperationEnabled(sw))
                    {
                        int step = GetCSPStepPerCycle();

                        // 点动
                        if (jogForward) desiredPosition = currentTarget + step;
                        else if (jogReverse) desiredPosition = currentTarget - step;

                        // 位置插值
                        int diff = desiredPosition - currentTarget;
                        if (Math.Abs(diff) > step)
                            currentTarget += Math.Sign(diff) * step;
                        else
                            currentTarget = desiredPosition;

                        output.TargetPosition = currentTarget;
                        cw = 0x0F;
                        branch = "OpEnabled";
                    }
                    else { branch = $"Unknown(sw=0x{sw:X4})"; }

                    output.ControlWord = cw;

                    // 状态变化时输出
                    string curState = ParseDriveState(sw);
                    if (curState != lastState || cw != lastCW)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CSP] 状态={curState} sw=0x{sw:X4} cw=0x{cw:X4} branch={branch} " +
                            $"actPos={input.PositionActualValue} targetPos={output.TargetPosition} err=0x{input.ErrorCode:X4}");
                        lastState = curState;
                        lastCW = cw;
                    }

                    // 每500ms输出一次周期状态
                    if (++periodicCount >= 500)
                    {
                        periodicCount = 0;
                        System.Diagnostics.Debug.WriteLine($"[CSP] 周期: 状态={curState} sw=0x{sw:X4} cw=0x{cw:X4} " +
                            $"actPos={input.PositionActualValue} targetPos={output.TargetPosition} desired={desiredPosition}");
                    }

                    UpdateSnapshot(sw, input.PositionActualValue, currentTarget, 0, input.ErrorCode);
                    Thread.Sleep(1);
                }
                System.Diagnostics.Debug.WriteLine("[CSP] PDO控制线程正常退出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CSP] PDO控制线程异常退出: {ex.Message}\n{ex.StackTrace}");
            }
        }

        void PdoControlLoop_PP()
        {
            try
            {
                var m = master;
                if (m == null) return;
                ref var input = ref m.Slaves[SLAVE_IDX].PDO.InputsMapping<PP_Input>();
                ref var output = ref m.Slaves[SLAVE_IDX].PDO.OutputsMapping<PP_Output>();

                // PP 状态机 (绝对模式, Free Run)
                // 0=Idle, 1=PrepareTarget, 2=NewSetpoint(bit4=1), 3=ClearBit4, 4=WaitTarget
                int ppState = 0;
                int motionCycles = 0;
                output.ModesOfOperation = 1;

                ushort lastCW = 0xFFFF;
                string lastState = "";

                while (isRunning && master != null)
                {
                    ushort sw = input.StatusWord;
                    ushort cw = 0;
                    int profVel = GetPPProfileVelocity();
                    output.ProfileVelocity = (uint)profVel;
                    output.ModesOfOperation = 1;

                    if (faultReset) { cw = 0x80; ppState = 0; faultReset = false; }
                    else if (IsFault(sw)) { ppState = 0; }
                    else if (!servoEnabled) { ppState = 0; }
                    else if ((sw & 0x4F) == 0x00) { }
                    else if (IsSwitchOnDisabled(sw)) { cw = 0x06; }
                    else if (IsReadyToSwitchOn(sw)) { cw = 0x07; }
                    else if (IsSwitchedOn(sw)) { cw = 0x0F; }
                    else if (IsOperationEnabled(sw))
                    {
                        bool targetReached = (sw & 0x0400) != 0;
                        bool setPointAck = (sw & 0x1000) != 0;

                        // 点动
                        if ((jogForward || jogReverse) && ppState == 0 && targetReached)
                        {
                            int jogStep = Math.Max(1, profVel / 10);
                            desiredPosition = input.PositionActualValue + (jogForward ? jogStep : -jogStep);
                            triggerMove = true;
                        }

                        switch (ppState)
                        {
                            case 0: // Idle
                                cw = 0x0F;
                                output.TargetPosition = input.PositionActualValue;
                                if (triggerMove)
                                {
                                    output.TargetPosition = desiredPosition;
                                    ppState = 1;
                                    motionCycles = 0;
                                }
                                break;

                            case 1: // PrepareTarget: 等待数周期确保 TP 到达驱动器
                                cw = 0x0F;
                                output.TargetPosition = desiredPosition;
                                if (++motionCycles >= 10) { ppState = 2; motionCycles = 0; }
                                break;

                            case 2: // NewSetpoint: CW bit4=1, 等待 bit12=1
                                cw = 0x1F;
                                output.TargetPosition = desiredPosition;
                                motionCycles++;
                                if (setPointAck) { ppState = 3; motionCycles = 0; }
                                else if (motionCycles >= 3000) { ppState = 0; triggerMove = false; }
                                break;

                            case 3: // ClearBit4: 等待 bit12=0
                                cw = 0x0F;
                                motionCycles++;
                                if (!setPointAck) { ppState = 4; motionCycles = 0; }
                                else if (motionCycles >= 500) { ppState = 0; triggerMove = false; }
                                break;

                            case 4: // WaitTarget: 等待 bit10=1
                                cw = 0x0F;
                                motionCycles++;
                                if (targetReached) { ppState = 0; triggerMove = false; }
                                break;

                            default: cw = 0x0F; break;
                        }
                    }

                    output.ControlWord = cw;

                    // 状态变化日志
                    string curState = ParseDriveState(sw);
                    if (curState != lastState || cw != lastCW)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PP] {curState} sw=0x{sw:X4} cw=0x{cw:X4} " +
                            $"pos={input.PositionActualValue} target={output.TargetPosition} ppState={ppState}");
                        lastState = curState;
                        lastCW = cw;
                    }

                    UpdateSnapshot(sw, input.PositionActualValue, desiredPosition, input.VelocityActualValue, input.ErrorCode);
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PP] 异常: {ex.Message}");
            }
        }

        void UpdateSnapshot(ushort sw, int pos, int target, int vel, ushort err)
        {
            snapStatusWord = sw;
            snapActualPosition = pos;
            snapTargetPosition = target;
            snapActualVelocity = vel;
            snapErrorCode = err;
            snapDriveState = ParseDriveState(sw);
        }

        // 读取缓存值, 不访问 UI 控件 (PDO 线程安全)
        int GetCSPStepPerCycle() => Math.Max(1, _cachedCSPStep);
        int GetPPProfileVelocity() => Math.Max(1, _cachedPPVelocity);

        // ==================== UI 刷新 ====================

        void StatusUpdateLoop()
        {
            while (isRunning)
            {
                try
                {
                    var state = snapDriveState;
                    var sw = snapStatusWord;
                    var pos = snapActualPosition;
                    var target = snapTargetPosition;
                    var vel = snapActualVelocity;
                    var err = snapErrorCode;
                    double posDeg = pos * 360.0 / PULSES_PER_REV;
                    double targetDeg = target * 360.0 / PULSES_PER_REV;
                    bool reached = (sw & 0x0400) != 0;

                    Invoke(new Action(() =>
                    {
                        lblDriveState.Text = state + (reached ? " [到位]" : "");
                        lblStatusWord.Text = $"0x{sw:X4}";
                        lblActualPosition.Text = $"{posDeg:F2}\u00B0 ({pos})";
                        lblTargetPositionDisplay.Text = $"{targetDeg:F2}\u00B0 ({target})";
                        lblActualVelocity.Text = $"{vel}";
                        lblErrorCode.Text = $"0x{err:X4}";
                    }));
                }
                catch (ObjectDisposedException) { /* 表单已关闭, 正常退出路径 */ }
                catch (InvalidOperationException) { /* 句柄已销毁/未创建, 正常退出路径 */ }
                catch (Exception ex)
                {
                    // 未预期异常仅写 Debug, 避免后台线程崩溃影响 UI
                    System.Diagnostics.Debug.WriteLine($"[StatusUpdateLoop] 异常: {ex.GetType().Name}: {ex.Message}");
                }
                Thread.Sleep(50);
            }
        }

        // ==================== 运动控制 (共享事件) ====================

        void btnEnable_Click(object sender, EventArgs e) { servoEnabled = true; Log("使能中..."); System.Diagnostics.Debug.WriteLine($"[UI] 使能 servoEnabled=true"); }
        void btnDisable_Click(object sender, EventArgs e) { servoEnabled = false; Log("已去使能"); System.Diagnostics.Debug.WriteLine($"[UI] 去使能 servoEnabled=false"); }
        void btnFaultReset_Click(object sender, EventArgs e) { faultReset = true; Log("故障复位"); System.Diagnostics.Debug.WriteLine($"[UI] 故障复位"); }

        void btnAbsMove_Click(object sender, EventArgs e)
        {
            var num = isCSP ? numCSP_AbsPosition : numPP_AbsPosition;
            int pulses = (int)((double)num.Value * PULSES_PER_REV / 360.0);
            desiredPosition = pulses;
            triggerMove = true;
            Log($"绝对运动: {num.Value}\u00B0 ({pulses} 脉冲)");
            System.Diagnostics.Debug.WriteLine($"[UI] 绝对运动: desired={pulses} snapActPos={snapActualPosition}");
        }

        void btnRelMove_Click(object sender, EventArgs e)
        {
            var num = isCSP ? numCSP_RelDistance : numPP_RelDistance;
            int pulses = (int)((double)num.Value * PULSES_PER_REV / 360.0);
            desiredPosition = snapActualPosition + pulses;
            triggerMove = true;
            Log($"相对运动: {num.Value}\u00B0 ({pulses} 脉冲)");
            System.Diagnostics.Debug.WriteLine($"[UI] 相对运动: desired={desiredPosition} snapActPos={snapActualPosition} delta={pulses}");
        }

        void btnHome_Click(object sender, EventArgs e)
        {
            desiredPosition = 0;
            triggerMove = true;
            Log("回零");
            System.Diagnostics.Debug.WriteLine($"[UI] 回零: desired=0 snapActPos={snapActualPosition}");
        }

        void btnJogForward_MouseDown(object sender, MouseEventArgs e) { jogForward = true; }
        void btnJogForward_MouseUp(object sender, MouseEventArgs e) { jogForward = false; }
        void btnJogReverse_MouseDown(object sender, MouseEventArgs e) { jogReverse = true; }
        void btnJogReverse_MouseUp(object sender, MouseEventArgs e) { jogReverse = false; }

        // ==================== 断开 ====================

        void btnDisconnect_Click(object sender, EventArgs e) { Disconnect(); }

        void Disconnect()
        {
            Console.WriteLine("[Disconnect] 开始断开连接...");

            // 取消正在进行的连接操作 (CTS 可能已 Disposed, 忽略)
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { /* CTS 已 Disposed, 正常 */ } catch (Exception ex) { Console.WriteLine($"[Disconnect] CTS Cancel 异常: {ex.GetType().Name}: {ex.Message}"); }

            servoEnabled = false;
            jogForward = false;
            jogReverse = false;
            triggerMove = false;
            desiredPosition = 0;
            isRunning = false;

            // 取消事件订阅, 防止断开后继续触发
            try { DarraEtherCAT.Logs.Updated -= OnLogUpdated; } catch (Exception ex) { Console.WriteLine($"[Disconnect] 取消日志订阅异常: {ex.GetType().Name}: {ex.Message}"); }

            // 等待 PDO 控制线程退出循环 (循环周期 1ms, 等 20ms 足够)
            Thread.Sleep(20);

            // 先中断 DLL 层阻塞 (直接 P/Invoke, 不走 RunOnDllThread 队列)
            try { DarraEtherCAT.Abort(); } catch (Exception ex) { Console.WriteLine($"[Disconnect] Abort 异常: {ex.GetType().Name}: {ex.Message}"); }

            var m = master;
            master = null;

            if (m != null)
            {
                // 后台执行 Close, 不阻塞 UI 线程
                Console.WriteLine("[Disconnect] 后台关闭主站...");
                Task.Run(() =>
                {
                    try { m.Close(); }
                    catch (Exception ex) { Console.WriteLine($"[Disconnect] Close异常: {ex.Message}"); }
                    Console.WriteLine("[Disconnect] 主站关闭完成");
                });
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            cboMode.Enabled = true;
            grpCSP.Enabled = false;
            grpPP.Enabled = false;
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = System.Drawing.Color.Gray;
            lblDriveState.Text = "---";
            lblStatusWord.Text = "0x0000";
            lblActualPosition.Text = "0.00\u00B0";
            lblTargetPositionDisplay.Text = "0.00\u00B0";
            lblActualVelocity.Text = "0";
            lblErrorCode.Text = "0x0000";

            Log("已断开连接");
        }

        // ==================== 日志 ====================

        void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            System.Diagnostics.Debug.WriteLine(line);
            if (_logQueue.Count < LOG_QUEUE_CAP)
                _logQueue.Enqueue(line);
        }

        void LogFlushTimer_Tick(object sender, EventArgs e)
        {
            if (_logQueue.IsEmpty) return;

            var sb = new StringBuilder();
            int count = 0;
            while (_logQueue.TryDequeue(out string line) && count < 50)
            {
                sb.AppendLine(line);
                count++;
            }
            if (sb.Length == 0) return;

            txtLog.AppendText(sb.ToString());

            // 超过上限时截断前半部分
            if (txtLog.TextLength > LOG_MAX_TEXT)
            {
                string text = txtLog.Text;
                int cutAt = text.IndexOf('\n', text.Length / 2);
                if (cutAt > 0)
                    txtLog.Text = text.Substring(cutAt + 1);
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _logFlushTimer?.Stop();
            _logFlushTimer?.Dispose();

            // 取消进行中的连接 + 中断 DLL 阻塞 (FormClosing 阶段 CTS 可能已 Disposed)
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { /* CTS 已 Disposed, 正常 */ } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] CTS Cancel 异常: {ex.GetType().Name}: {ex.Message}"); }
            try { DarraEtherCAT.Logs.Updated -= OnLogUpdated; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] 取消日志订阅异常: {ex.GetType().Name}: {ex.Message}"); }

            isRunning = false;
            // 等待 PDO 控制线程退出, 防止 Close 释放内存后线程访问已释放的 IOmap
            Thread.Sleep(20);

            try { DarraEtherCAT.Abort(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] Abort 异常: {ex.GetType().Name}: {ex.Message}"); }

            var m = master;
            master = null;
            if (m != null)
            {
                // 同步关闭 (FormClosing 后进程退出, 必须等待)
                // AbortNetwork 已设置, Close 内部的操作会快速返回
                try { m.Close(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] m.Close 异常: {ex.GetType().Name}: {ex.Message}"); }
            }
            base.OnFormClosing(e);
        }
    }
}
