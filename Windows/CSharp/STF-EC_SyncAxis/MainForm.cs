using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace STF_EC_SyncAxis
{
    // ==================== STF-EC 步进驱动器 PDO (CiA 402, Profile 402) ====================
    // 厂商: Shanghai AMP&MOONS' Automation (VendorId 0x00000168)
    // 型号: STF-EC (ProductCode 0x02)
    //
    // 本例 = *同步轴 / 电子齿轮* 演示, 轴数由 config.deni 实际扫描到的从站数量决定 (master.SlaveCount),
    // 所有 STF-EC 共用同一 PDO 结构。提供的 config.deni 含 5 个 STF-EC (PhysAddr 0x1001~0x1005)。
    //
    // 字节布局来自 config.deni 实测 (字段顺序必须与 PDO 条目逐字节一致, 不可调换):
    //   RxPDO 输出 = 29 字节 (0x1600~0x1603)
    //   TxPDO 输入 = 35 字节 (0x1A00~0x1A03, 含 4 个 Touch Probe 位置值)
    //
    // 同步轴 *只* 用 CSP (Mode=8): 每个从站 ConfigureDC 启用 Sync0 (1ms), 0x6060 = 8。
    // 一个 "虚拟主轴" 位置计数器每周期推进, 每个真实轴按各自 "齿比" 跟随:
    //   output.TargetPosition = a.Base + (int)Math.Round(masterPos * a.Ratio)
    // 齿比 1:1 即用户口中的 "同一数据点同时映射" (所有轴完全一致); 齿比 ≠ 1 即电子齿轮。

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct STF_Output  // 29 字节 (RxPDO 0x1600+0x1601+0x1602+0x1603)
    {
        // 0x1600
        public ushort ControlWord;          // 0x6040:0  UINT
        public sbyte ModesOfOperation;      // 0x6060:0  SINT   (同步轴恒 CSP=8)
        public int TargetPosition;          // 0x607A:0  DINT
        // 0x1601
        public uint ProfileVelocity;        // 0x6081:0  UDINT  (本例不使用)
        public uint ProfileAcceleration;    // 0x6083:0  UDINT
        public uint ProfileDeceleration;    // 0x6084:0  UDINT
        // 0x1602
        public int TargetVelocity;          // 0x60FF:0  DINT   (PV 模式用, 本例不使用)
        // 0x1603
        public uint PhysicalOutputs;        // 0x60FE:1  UDINT  (数字输出)
        public ushort TouchProbeFunction;   // 0x60B8:0  UINT
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct STF_Input  // 35 字节 (TxPDO 0x1A00+0x1A01+0x1A02+0x1A03)
    {
        // 0x1A00  (注意: STF-EC 此 PDO 错误码在前, 状态字在后)
        public ushort ErrorCode;                // 0x603F:0  UINT
        public ushort StatusWord;               // 0x6041:0  UINT
        public sbyte ModesOfOperationDisplay;   // 0x6061:0  SINT
        // 0x1A01
        public int PositionActualValue;         // 0x6064:0  DINT
        // 0x1A02
        public int VelocityActualValue;         // 0x606C:0  DINT
        // 0x1A03
        public uint DigitalInputs;              // 0x60FD:0  UDINT
        public ushort TouchProbeStatus;         // 0x60B9:0  UINT
        public int TouchProbe1PosValue;         // 0x60BA:0  DINT
        public int TouchProbe1NegValue;         // 0x60BB:0  DINT
        public int TouchProbe2PosValue;         // 0x60BC:0  DINT
        public int TouchProbe2NegValue;         // 0x60BD:0  DINT
    }

    // ==================== 报警 ====================
    enum AlarmSeverity { Fault = 0, Warning = 1, Info = 2 }   // 颜色 Firebrick / DarkOrange / DimGray

    enum AlarmType
    {
        跟随误差, 掉OP, 驱动故障, AL状态, DC失步,
        掉站, 链路断, 主站掉OP, 紧急码, 丢帧, WKC短缺, 身份不符
    }

    sealed class AlarmEntry
    {
        public DateTime Time;
        public int AxisNo;            // ≥0 轴号(显示+1); -1 主站/全局
        public AlarmSeverity Sev;
        public AlarmType Type;
        public string Message;
        public bool Active;           // latch: 条件消失置 false 但保留(灰显)
        public string Key;            // 去重键 = "轴|类型"
    }

    /// <summary>
    /// 线程安全报警管理器。多生产者(事件回调线程 / PDO 线程经闩) + 单消费者(UI 50ms)。
    /// 生产侧只入队/置闩不碰 UI; 消费侧统一在 UI 线程 Drain + 呈现。
    /// </summary>
    sealed class AlarmManager
    {
        readonly object _lock = new object();
        readonly Dictionary<string, AlarmEntry> _active = new Dictionary<string, AlarmEntry>();
        readonly List<AlarmEntry> _history = new List<AlarmEntry>();
        readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();  // 事件回调线程→消费侧
        const int HISTORY_CAP = 500;
        public volatile bool Dirty;

        static string MakeKey(int axis, AlarmType t) => axis + "|" + t;

        // 任意线程可调; 事件回调线程建议走 Enqueue
        public void Raise(int axis, AlarmType type, AlarmSeverity sev, string msg)
        {
            lock (_lock)
            {
                string key = MakeKey(axis, type);
                if (_active.TryGetValue(key, out var e))
                {
                    if (!e.Active) { e.Active = true; e.Time = DateTime.Now; Dirty = true; }
                    if (e.Message != msg) { e.Message = msg; Dirty = true; }
                    return;
                }
                var n = new AlarmEntry { Time = DateTime.Now, AxisNo = axis, Sev = sev, Type = type, Message = msg, Active = true, Key = key };
                _active[key] = n;
                _history.Insert(0, n);
                if (_history.Count > HISTORY_CAP) _history.RemoveAt(_history.Count - 1);
                Dirty = true;
            }
        }

        public void Clear(int axis, AlarmType type)
        {
            lock (_lock)
            {
                string key = MakeKey(axis, type);
                if (_active.TryGetValue(key, out var e)) { e.Active = false; _active.Remove(key); Dirty = true; }
            }
        }

        public void Enqueue(Action a) => _pending.Enqueue(a);          // 事件回调线程用
        public void DrainPending() { while (_pending.TryDequeue(out var a)) { try { a(); } catch { } } }

        public void Snapshot(out int activeCount, out int activeFaults, out AlarmSeverity worst, out string topMsg)
        {
            lock (_lock)
            {
                activeCount = _active.Count; activeFaults = 0; worst = AlarmSeverity.Info; topMsg = null;
                var bestSev = AlarmSeverity.Info;
                foreach (var e in _active.Values)
                {
                    if (e.Sev == AlarmSeverity.Fault) activeFaults++;
                    if (e.Sev < worst) worst = e.Sev;            // Fault=0 最高
                    if (topMsg == null || e.Sev < bestSev)       // 取最高严重度的一条作横幅文字
                    {
                        bestSev = e.Sev;
                        topMsg = $"{(e.AxisNo < 0 ? "主站" : "轴" + (e.AxisNo + 1))} {e.Type}: {e.Message}";
                    }
                }
            }
        }

        public bool HasActiveFault() { lock (_lock) { foreach (var e in _active.Values) if (e.Sev == AlarmSeverity.Fault) return true; return false; } }
        public int HistoryCount() { lock (_lock) { return _history.Count; } }
        // 原子版: 锁内一次性 判Dirty+清Dirty+取快照, 消除"Dirty=false 在锁外"与生产者插入的竞争。
        public List<AlarmEntry> BuildRowsIfDirty() { lock (_lock) { if (!Dirty) return null; Dirty = false; return new List<AlarmEntry>(_history); } }

        // 仅当无激活故障才物理清掉已恢复历史; 返回是否清成功
        public bool AckAndPurge()
        {
            lock (_lock)
            {
                if (HasActiveFaultNoLock()) return false;
                _history.RemoveAll(e => !e.Active);
                Dirty = true;
                return true;
            }
        }
        bool HasActiveFaultNoLock() { foreach (var e in _active.Values) if (e.Sev == AlarmSeverity.Fault) return true; return false; }

        public void Reset() { lock (_lock) { _active.Clear(); _history.Clear(); while (_pending.TryDequeue(out _)) { } Dirty = true; } }

        // 报警复位: 清掉所有激活闩(含纯事件型如身份不符/主站掉OP, 它们无周期 Clear 来源) + 清理已恢复历史。
        // 仍存在的"轮询类"故障(掉站/AL/链路/跟随误差)会被下个 50ms DetectAlarms 重新升起并重新组停 = 不能 ack 掉活故障。
        public void ClearAllActiveAndPurge()
        {
            lock (_lock)
            {
                foreach (var e in _active.Values) e.Active = false;
                _active.Clear();
                _history.RemoveAll(e => !e.Active);
                Dirty = true;
            }
        }
    }

    /// <summary>
    /// 单个轴的控制状态 (UI 线程写控制变量, PDO 线程写快照, 互不阻塞)
    /// 同步轴扩展: Ratio (电子齿比) + Base (启动同步时实际位置基准)。
    /// </summary>
    sealed class AxisController
    {
        public readonly int SlaveIndex;
        public readonly int PhysAddr;
        public AxisController(int idx, int physAddr) { SlaveIndex = idx; PhysAddr = physAddr; }

        // 控制 (UI 写, PDO 读)
        public volatile bool ServoEnabled;
        public volatile bool FaultReset;

        // 同步轴扩展
        public double Ratio = 1.0;      // 电子齿比 (UI 写, PDO 读; double 无法 volatile, 改齿比是低频整量写, 足够安全)
        public int Base;                // 启动同步时的实际位置基准 (PDO 线程写/读)

        // 快照 (PDO 写, UI 读)
        public volatile ushort SnapStatusWord;
        public volatile int SnapActualPosition;
        public volatile int SnapTargetPosition;
        public volatile int SnapActualVelocity;
        public volatile ushort SnapErrorCode;
        public volatile string SnapDriveState = "---";

        // CSP 内部状态 (仅 PDO 线程访问)
        public int CurrentTarget;

        // —— 同步监控 / grace (PDO 线程拥有写) ——
        public long SyncStartCycle;             // 本轴 grace 起算周期; 0=未就绪
        public volatile bool GraceResetPending; // UI 改齿比/启动同步 → 下周期重置 grace
        public volatile bool SyncEligible;      // 过 grace 且就绪, 可判跟随误差
        public bool WasEligible;                // 曾真正就绪(掉OP 边沿判定用; 仅 PDO 线程)
        public int FollowErrConsec;             // 跟随误差连续超限计数 (仅 PDO 线程)
        public volatile int SnapFollowError;    // 最近跟随误差(带符号, 50ms UI 读)
        public int FollowErrLimit = 5000;       // 每轴跟随误差阈值(脉冲), 默认 5000=0.5圈
        public int PrevTarget;                  // 上周期下发目标, 用于估算每周期增量(跟随误差阈值随速度自适应)

        // —— 异常闩 (PDO 线程置, 50ms 消费侧据边沿 Raise/Clear) ——
        public volatile bool AlarmFollowError;
        public volatile bool AlarmDropEnable;
        public volatile bool AlarmFault;
    }

    /// <summary>
    /// STF-EC 同步轴 / 电子齿轮主窗体。
    /// 永远 CSP (Mode=8)。虚拟主轴位置每周期推进, 各轴按齿比跟随 = 凸轮的 1:N 直线特例。
    /// </summary>
    public partial class MainForm : Form
    {
        DarraEtherCAT master;
        volatile bool isRunning = false;
        private CancellationTokenSource _connectCts;
        Thread _pdoThread, _statusThread;       // 工作线程引用: 断开/关闭前先 Join 再 Close, 防 native UAF

        volatile AxisController[] axes = new AxisController[0];   // 引用字段 volatile: 后台线程读引用快照的可见性保证

        // 虚拟主轴 (PDO 线程拥有; UI 线程通过下面几个标志/字段交互)
        long _masterPos = 0;                    // 虚拟主轴位置 (脉冲), PDO 线程读写
        volatile bool _syncRunning = false;     // 是否自动同步推进
        volatile int _masterStep = 10;          // 每周期推进步长 (= 速度脉冲/秒 ÷ 1000)
        volatile bool _resyncRequested = false; // 请求重新快照 Base + 主轴清零 (启动同步时)
        volatile int _jogMasterDir = 0;         // 主轴手动点动方向: +1 / -1 / 0

        // 异步日志
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int LOG_MAX_TEXT = 50000;
        private const int LOG_QUEUE_CAP = 500;
        private int _processedLogCount;
        private readonly object _logProcLock = new object();   // OnLogUpdated 可能并发触发 → 串行化 _processedLogCount

        const int PULSES_PER_REV = 10000;          // STF-EC 默认细分 (0x2604 Steps per Rev), 角度换算用
        const int JOG_MASTER_STEP = 20;            // 主轴手动点动每周期步长 (脉冲)

        // 轴总览表列索引
        const int COL_SEL = 0, COL_AXIS = 1, COL_ADDR = 2, COL_RATIO = 3, COL_STATE = 4, COL_SW = 5,
                  COL_ACTPOS = 6, COL_TGTPOS = 7, COL_VEL = 8, COL_ENABLED = 9;

        // ==================== 报警字段 ====================
        readonly AlarmManager alarmMgr = new AlarmManager();
        long _pdoCycle = 0;                     // PDO 单调周期计数 (grace 计时基准, net472 不用 TickCount64)
        volatile bool _groupStopLatched = false; // 组停闭锁: 拦截 启动同步/全部使能, 需报警复位解锁

        // 报警表列索引
        const int ACOL_TIME = 0, ACOL_AXIS = 1, ACOL_SEV = 2, ACOL_TYPE = 3, ACOL_MSG = 4, ACOL_STATE = 5;

        // 报警阈值 / 防误报参数
        const int FE_CONSEC_N = 50;     // 跟随误差去抖周期数 (≈50ms@1ms)
        const int GRACE_CYCLES = 200;   // 启动同步/改齿比/刚使能后宽限 (≈200ms@1ms)
        const int PDO_LOSS_FAULT = 5;   // 连续丢帧故障阈值
        const int DC_DIFF_FAULT_NS = 500000; // DC 偏差故障阈值(半周期)
        const uint WKC_MISS_WARN = 1;   // 占位(WKC 短缺即 Warning, 持续靠 GetGroupDiag 升级)
        int _wkcMissConsec = 0;         // WKC 连续失配计数 (仅 50ms 线程)

        public MainForm()
        {
            InitializeComponent();
            InitGrid();
            InitAlarmGrid();
            BindEvents();
            _logFlushTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _logFlushTimer.Tick += LogFlushTimer_Tick;
            _logFlushTimer.Start();
        }

        // ==================== 事件绑定 ====================

        void BindEvents()
        {
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;

            gridAxes.CellEndEdit += GridAxes_CellEndEdit;

            // 虚拟主轴
            numMasterSpeed.ValueChanged += (s, e) => _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));
            btnSyncStart.Click += btnSyncStart_Click;
            btnSyncStop.Click += btnSyncStop_Click;
            btnMasterJogFwd.MouseDown += (s, e) => { if (!_groupStopLatched) _jogMasterDir = 1; };   // 闭锁期禁止推主轴, 防解锁后大跳变
            btnMasterJogFwd.MouseUp += (s, e) => _jogMasterDir = 0;
            btnMasterJogRev.MouseDown += (s, e) => { if (!_groupStopLatched) _jogMasterDir = -1; };
            btnMasterJogRev.MouseUp += (s, e) => _jogMasterDir = 0;

            // 全局控制 (作用于勾选轴; 未勾选 = 全部)
            btnAllEnable.Click += (s, e) =>
            {
                if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
                ForEachTarget(a => a.ServoEnabled = true, "全部使能");
            };
            btnAllDisable.Click += (s, e) => ForEachTarget(a => a.ServoEnabled = false, "全部去使能(急停)"); // 急停永远可用
            btnAllFaultReset.Click += (s, e) => ForEachTarget(a => a.FaultReset = true, "全部故障复位");

            // 报警复位
            btnAlarmAck.Click += btnAlarmAck_Click;
        }

        // ==================== 目标轴选择 (勾选「选」列; 未勾选 = 全部) ====================

        void ForEachTarget(Action<AxisController> act, string logName)
        {
            var targets = GetTargetAxes();
            foreach (var a in targets) act(a);
            Log($"{logName}: {targets.Count} 轴");
        }

        List<AxisController> GetTargetAxes()
        {
            var list = new List<AxisController>();
            for (int i = 0; i < axes.Length && i < gridAxes.Rows.Count; i++)
            {
                var cell = gridAxes.Rows[i].Cells[COL_SEL].Value;
                if (cell is bool b && b) list.Add(axes[i]);
            }
            if (list.Count == 0) list.AddRange(axes); // 未勾选 = 全部
            return list;
        }

        // ==================== 总览表 ====================

        void InitGrid()
        {
            gridAxes.AutoGenerateColumns = false;
            gridAxes.Columns.Clear();

            var chk = new DataGridViewCheckBoxColumn { HeaderText = "选", Width = 36, Name = "colSel" };
            gridAxes.Columns.Add(chk);                                                    // 0
            AddTextCol("轴", 44, true);                                                   // 1
            AddTextCol("地址", 64, true);                                                 // 2
            AddTextCol("齿比", 70, false);                                                // 3  可编辑
            AddTextCol("驱动状态", 150, true);                                            // 4
            AddTextCol("状态字", 70, true);                                               // 5
            AddTextCol("实际位置(°)", 140, true);                                         // 6
            AddTextCol("目标位置(°)", 140, true);                                         // 7
            AddTextCol("实际速度", 90, true);                                             // 8
            AddTextCol("使能", 52, true);                                                 // 9
        }

        void AddTextCol(string header, int width, bool readOnly)
        {
            gridAxes.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                ReadOnly = readOnly,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        void BuildAxisRows()
        {
            gridAxes.Rows.Clear();
            foreach (var a in axes)
            {
                int r = gridAxes.Rows.Add();
                var row = gridAxes.Rows[r];
                row.Cells[COL_SEL].Value = false;
                row.Cells[COL_AXIS].Value = (a.SlaveIndex + 1).ToString();
                row.Cells[COL_ADDR].Value = $"0x{a.PhysAddr:X4}";
                row.Cells[COL_RATIO].Value = a.Ratio.ToString("F3", CultureInfo.InvariantCulture);
                row.Cells[COL_STATE].Value = "---";
            }
        }

        // 解析「齿比」单元格 → AxisController.Ratio
        void GridAxes_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != COL_RATIO || e.RowIndex < 0 || e.RowIndex >= axes.Length) return;
            var row = gridAxes.Rows[e.RowIndex];
            var a = axes[e.RowIndex];
            string text = Convert.ToString(row.Cells[COL_RATIO].Value);
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out ratio))
            {
                a.Ratio = ratio;
                a.GraceResetPending = true;          // 改齿比后实际要重新追目标, grace 内不判跟随误差
                a.AlarmFollowError = false;
                Log($"轴{a.SlaveIndex + 1} 齿比 = {ratio:F3}");
            }
            else
            {
                Log($"轴{a.SlaveIndex + 1} 齿比输入无效, 保持 {a.Ratio:F3}");
            }
            // 规范化回写, 保持显示一致
            row.Cells[COL_RATIO].Value = a.Ratio.ToString("F3", CultureInfo.InvariantCulture);
        }

        // ==================== 虚拟主轴 ====================

        void btnSyncStart_Click(object sender, EventArgs e)
        {
            if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
            _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));
            foreach (var a in axes) a.GraceResetPending = true;   // 启动瞬间不判跟随误差 (实际正在追目标)
            _resyncRequested = true;   // PDO 线程下个周期重新快照各轴 Base + 主轴清零
            _syncRunning = true;
            Log($"启动同步: 主轴速度 {numMasterSpeed.Value} 脉冲/秒 (每周期 {_masterStep} 脉冲)");
        }

        void btnSyncStop_Click(object sender, EventArgs e)
        {
            _syncRunning = false;
            Log("停止同步 (各轴保持当前位置)");
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

        // 干净 OP = 基础态(低4位)==OP 且无 Error 位(0x10)。掉出 OP 含 OP→SafeOp 与 OP→OP+Error(0x18)。
        static bool IsCleanOP(EcState s) => ((int)s & 0x0F) == (int)EcState.OP && ((int)s & 0x10) == 0;

        // ==================== 连接 ====================

        string GetDeniPath()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDir, "EtherCATRestore", "config.deni");
        }

        async void btnConnect_Click(object sender, EventArgs e)
        {
            string deniPath = GetDeniPath();

            if (!File.Exists(deniPath))
            {
                MessageBox.Show($"配置文件不存在: {deniPath}\n请用主站 GUI 扫描 STF-EC 从站, 导出 config.deni 放到该目录\n(详见 README.md)", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;

            _connectCts = new CancellationTokenSource();
            var ct = _connectCts.Token;

            Log("正在初始化主站 (CSP 同步轴模式)...");

            DarraEtherCAT newMaster = null;
            string errorMsg = null;
            int slaveCount = 0;

            bool success = await Task.Run(() =>
            {
                try
                {
                    var buildResult = new DarraEtherCAT()
                        .SetENI(deniPath)
                        .EnableAutoStartup()
                        .Build();

                    if (!buildResult.Success)
                    {
                        errorMsg = $"主站初始化失败: {buildResult.Message}";
                        try { buildResult.Master?.Close(); } catch (Exception cex) { System.Diagnostics.Debug.WriteLine($"[Connect] 清理异常: {cex.Message}"); }
                        return false;
                    }

                    newMaster = buildResult.Master;
                    slaveCount = newMaster.SlaveCount;
                    if (slaveCount <= 0) { errorMsg = "未扫描到任何从站"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    if (!newMaster.SetState(EcState.PreOp)) { errorMsg = "PreOp 失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // 逐轴配置同步方式 (CSP: DC Sync0 1ms)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        var slave = newMaster.Slaves[i];

                        // ① 经 CoE 把 PDO 分配重映成 config.deni 的完整 4+4 (输出29/输入35)。
                        //    驱动默认 RxPDO 只有 11 字节, 不重映就和结构体(29)对不上, 任何模式都映射失败。
                        //    这是一次性初始化 (PreOp 态写, SM2 此时未激活), 不进控制循环。
                        if (slave.CoE != null)
                        {
                            try
                            {
                                slave.CoE.SDOWrite(0x1C12, 0, new byte[] { 0 });
                                slave.CoE.SDOWrite(0x1C12, 1, BitConverter.GetBytes((ushort)0x1600));
                                slave.CoE.SDOWrite(0x1C12, 2, BitConverter.GetBytes((ushort)0x1601));
                                slave.CoE.SDOWrite(0x1C12, 3, BitConverter.GetBytes((ushort)0x1602));
                                slave.CoE.SDOWrite(0x1C12, 4, BitConverter.GetBytes((ushort)0x1603));
                                slave.CoE.SDOWrite(0x1C12, 0, new byte[] { 4 });
                                slave.CoE.SDOWrite(0x1C13, 0, new byte[] { 0 });
                                slave.CoE.SDOWrite(0x1C13, 1, BitConverter.GetBytes((ushort)0x1A00));
                                slave.CoE.SDOWrite(0x1C13, 2, BitConverter.GetBytes((ushort)0x1A01));
                                slave.CoE.SDOWrite(0x1C13, 3, BitConverter.GetBytes((ushort)0x1A02));
                                slave.CoE.SDOWrite(0x1C13, 4, BitConverter.GetBytes((ushort)0x1A03));
                                slave.CoE.SDOWrite(0x1C13, 0, new byte[] { 4 });
                                Log($"轴{i + 1} PDO 重映 0x1C12/0x1C13 = 4+4 (29/35) 完成");
                            }
                            catch (Exception ex) { Log($"轴{i + 1} PDO 重映失败(将保持驱动默认): {ex.Message}"); }
                        }

                        if (slave.HasDC) slave.ConfigureDC(1000000); // Sync0 = 1ms
                        slave.CoE?.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));
                    }

                    if (!newMaster.SetState(EcState.SafeOp)) { errorMsg = "SafeOp 失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // PDO 尺寸自检: 用驱动实际进程映像字节数核对结构体, 避免 SDK 抛模糊的"结构体大小无效"
                    int expOut = Marshal.SizeOf<STF_Output>();   // 29 (RxPDO)
                    int expIn = Marshal.SizeOf<STF_Input>();     // 35 (TxPDO)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        int realOut = newMaster.Slaves[i].OutputsByteCount;
                        int realIn = newMaster.Slaves[i].InputsByteCount;
                        Log($"轴{i + 1} PDO 实测: 输出={realOut}B 输入={realIn}B (结构体 输出={expOut}/输入={expIn})");
                        if (realOut != expOut || realIn != expIn)
                        {
                            errorMsg = $"轴{i + 1} PDO 尺寸不符: 驱动实际 输出{realOut}/输入{realIn} 字节, 结构体 输出{expOut}/输入{expIn}。" +
                                       "驱动当前 PDO 映射 ≠ config.deni —— 需按驱动实际映射改 STF_Output/STF_Input, 或让 config.deni 经 CoE 真正重映 0x1C12/0x1600。";
                            return false;
                        }
                    }

                    // 逐轴 PDO 初始化 (SafeOp 后, OP 前): CSP, 目标位置 = 当前实际位置 (避免上电跳变)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var input = ref newMaster.Slaves[i].PDO.InputsMapping<STF_Input>();
                        ref var output = ref newMaster.Slaves[i].PDO.OutputsMapping<STF_Output>();
                        output.ModesOfOperation = 8;
                        output.TargetPosition = input.PositionActualValue;
                    }

                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }
                    if (!newMaster.SetState(EcState.OP)) { errorMsg = "OP 失败"; return false; }

                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var tOut = ref newMaster.Slaves[i].PDO.OutputsMapping<STF_Output>();
                        ref var tIn = ref newMaster.Slaves[i].PDO.InputsMapping<STF_Input>();
                        tOut.ControlWord = 0;
                        tOut.ModesOfOperation = 8;
                        tOut.TargetPosition = tIn.PositionActualValue;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                    return false;
                }
            });

            if (!success || ct.IsCancellationRequested)
            {
                Log(errorMsg ?? "已取消");
                if (newMaster != null)
                {
                    var cleanup = newMaster;
                    newMaster = null;
                    _ = Task.Run(() => { try { cleanup.Close(); } catch (Exception cex) { System.Diagnostics.Debug.WriteLine($"[Connect] 后台清理异常: {cex.Message}"); } });
                }
                if (master == null)
                {
                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                }
                return;
            }

            master = newMaster;

            // 建轴对象
            var list = new List<AxisController>();
            for (int i = 0; i < slaveCount; i++)
            {
                int physAddr;
                try { physAddr = master.Slaves[i].ConfigAddr; } catch { physAddr = 0x1001 + i; }
                var a = new AxisController(i, physAddr);
                a.CurrentTarget = master.Slaves[i].PDO.InputsMapping<STF_Input>().PositionActualValue;
                a.Base = a.CurrentTarget;
                list.Add(a);
            }
            axes = list.ToArray();

            // 报警复位 (新会话清空历史 + 解锁)
            alarmMgr.Reset();
            _groupStopLatched = false;
            _wkcMissConsec = 0;

            // 虚拟主轴复位
            _masterPos = 0;
            _syncRunning = false;
            _jogMasterDir = 0;
            _resyncRequested = false;
            _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));

            BuildAxisRows();

            RegisterEvents();
            DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
            _processedLogCount = DarraEtherCAT.Logs.Count;
            DarraEtherCAT.Logs.Updated += OnLogUpdated;

            isRunning = true;
            _pdoThread = new Thread(PdoControlLoop) { IsBackground = true }; _pdoThread.Start();
            _statusThread = new Thread(StatusUpdateLoop) { IsBackground = true }; _statusThread.Start();

            grpMaster.Enabled = true;
            grpGlobal.Enabled = true;
            lblStatus.Text = "已连接 (CSP 同步轴)";
            lblStatus.ForeColor = Color.Green;
            lblAxisCount.Text = $"轴数: {axes.Length}";
            Log($"连接成功 — {axes.Length} 轴 STF-EC, 先 [全部使能], 再 [启动同步]");
        }

        // ==================== 事件 ====================

        void RegisterEvents()
        {
            // ⚠ SDK 事件的 slaveIndex 是 1-based (SOEM ec_slave[0]=主站, 从站 1..N); 我们的 axes[] 是 0-based。
            //   每个 per-slave 事件必须 ax = si - 1 + 边界保护, 否则报警键与 50ms 轮询(0-based)对不上 → latch 永不清。
            //   日志显示用 si 本身即为 1-based 轴号 (= ax+1), 不再 +1。

            // 主站掉 OP → 全网失控, 组停
            master.Events.StateChanged += (s, ev) =>
            {
                Log($"[状态] 主站: {ev.OldState} -> {ev.NewState}");
                if (IsCleanOP(ev.OldState) && !IsCleanOP(ev.NewState))
                {
                    alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.主站掉OP, AlarmSeverity.Fault, $"{ev.OldState}→{ev.NewState}"));
                    GroupStop(-1, "主站掉出 OP");
                }
            };
            // 从站掉出 OP (含 OP→SafeOp / OP→OP+Error)
            master.Events.SlaveStateChanged += (mi, si, os, ns) =>
            {
                Log($"[状态] 轴{si}: {os} -> {ns}");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                if (IsCleanOP(os) && !IsCleanOP(ns))
                {
                    alarmMgr.Enqueue(() => alarmMgr.Raise(ax, AlarmType.掉OP, AlarmSeverity.Fault, $"{os}→{ns}"));
                    GroupStop(ax, $"轴{si} 掉出 OP ({ns})");
                }
            };
            // Emergency: 错误寄存器 bit4(通讯错误)/bit5(厂商) = 故障, 否则警告
            master.Events.EmergencyEvent += (mi, si, code, reg, b1, w1, w2) =>
            {
                Log($"[紧急] 轴{si} 错误码: 0x{code:X4} reg=0x{reg:X2}");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                var sev = (reg & 0x30) != 0 ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                alarmMgr.Enqueue(() => alarmMgr.Raise(ax, AlarmType.紧急码, sev, $"紧急码 0x{code:X4} reg=0x{reg:X2}"));
                if (sev == AlarmSeverity.Fault) GroupStop(ax, $"轴{si} 紧急事件 0x{code:X4}");
            };
            // 从站掉线
            master.Events.SlaveOffline += (si) =>
            {
                Log($"[离线] 轴{si} 已断开");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                alarmMgr.Enqueue(() => alarmMgr.Raise(ax, AlarmType.掉站, AlarmSeverity.Fault, "从站离线"));
                GroupStop(ax, $"轴{si} 掉站");
            };
            // 从站上线: 仅清离线报警, 禁止自动重使能 (需人工故障复位+使能)
            master.Events.SlaveOnline += (si) =>
            {
                Log($"[上线] 轴{si} 已恢复 (需人工 [全部故障复位]+[全部使能])");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                alarmMgr.Enqueue(() => alarmMgr.Clear(ax, AlarmType.掉站));
            };
            // PDO 丢帧 (全局): 连续 ≥ 阈值 = 故障
            master.Events.PDOFrameLoss += (mi, grp, con, tot) =>
            {
                Log($"[丢帧] 组{grp} 连续: {con}, 累计: {tot}");
                var sev = con >= PDO_LOSS_FAULT ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.丢帧, sev, $"连续{con} 累计{tot}"));
                if (sev == AlarmSeverity.Fault) GroupStop(-1, $"PDO 连续丢帧 {con}");
            };
            // DC 同步丢失 / 端口链路 / 冗余: 仅 Log。
            //   DC失步 / 链路断 报警由 50ms 轮询 (dc.IsInSync / PrimaryLinkBroken / master.LinkState) 单一权威管理,
            //   避免"事件 Raise + 轮询 Clear"抢同一去重键导致状态抖动/漏报。
            master.Events.DCSyncLost += (mi, si, diffNs) => Log($"[DC] 轴{si} 同步丢失 偏差 {diffNs}ns");
            master.Events.RedundancyModeChanged += (mi, oldMode, newMode) => Log($"[冗余] 模式 {oldMode} → {newMode}");
            master.Events.SlavePortLinkChanged += (mi, si, port, isUp) => Log($"[链路] 轴{si} 端口{port} {(isUp ? "Up" : "Down")}");
            // 从站身份不符 (接错型号; 全局 -1, 纯事件型 → [报警复位] 清)
            master.Events.SlaveIdentityMismatch += (s, args) =>
            {
                Log("[身份] 从站身份不符 (接错型号?)");
                alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.身份不符, AlarmSeverity.Fault, "从站身份不符(接错型号?)"));
                GroupStop(-1, "从站身份不符");
            };
        }

        // 组停: 同步运动一旦某轴 Fault, 继续动其它轴会撕裂机构 → 停主轴 + 全组去使能 + 闭锁。
        // 可从 事件回调线程 / UI 线程 调用 (只写 volatile 标志, 不碰 UI)。
        void GroupStop(int triggerAxis, string reason)
        {
            _syncRunning = false;
            _jogMasterDir = 0;
            var arr = axes;
            foreach (var a in arr) a.ServoEnabled = false;   // StepSync 中 !ServoEnabled → 钳到实际位置 + cw=0x06 安全停
            _groupStopLatched = true;
            Log($"⛔ 组停: {reason} (已停主轴并全组去使能, 闭锁中 — 排除后 [报警复位] 解锁)");
        }

        // 报警复位: 全轴无故障才清 latch + 物理清已恢复历史
        void btnAlarmAck_Click(object sender, EventArgs e)
        {
            // 先挡掉"活的驱动故障"(最常见): 状态字 Fault / 0x603F 错误码 → 提示先做 [全部故障复位]。
            foreach (var a in axes)
                if (IsFault(a.SnapStatusWord) || a.SnapErrorCode != 0)
                { Log("仍有轴处于驱动故障态, 请先 [全部故障复位] 再 [报警复位]"); return; }

            // 清所有激活闩(含纯事件型: 身份不符/主站掉OP/丢帧/紧急码 — 它们无周期 Clear 来源) + 解锁。
            // 若仍有"轮询类"活故障(掉站/AL/链路/跟随误差), 下个 50ms 周期会自动重新报警并重新组停 → 不能 ack 掉活故障。
            alarmMgr.ClearAllActiveAndPurge();
            _groupStopLatched = false;
            Log("报警已复位 (若故障源未排除将自动重新报警并组停)");
        }

        void OnLogUpdated()
        {
            try
            {
                lock (_logProcLock)
                {
                    var logs = DarraEtherCAT.Logs;
                    int total = logs.Count;
                    if (_processedLogCount > total) _processedLogCount = 0;
                    for (int i = _processedLogCount; i < total; i++)
                        Log($"[DLL] {logs[i]}");
                    _processedLogCount = total;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnLogUpdated] 异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ==================== PDO 控制线程 (虚拟主轴推进 + 各轴按齿比跟随) ====================

        void PdoControlLoop()
        {
            try
            {
                var m = master;
                if (m == null) return;

                while (isRunning && master != null)
                {
                    var arr = axes;
                    _pdoCycle++;

                    // ① 启动同步时重新快照各轴 Base + 主轴清零 (PDO 线程内完成, 保证原子)
                    if (_resyncRequested)
                    {
                        _resyncRequested = false;
                        _masterPos = 0;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            ref var input = ref m.Slaves[arr[i].SlaveIndex].PDO.InputsMapping<STF_Input>();
                            arr[i].Base = input.PositionActualValue;
                            arr[i].CurrentTarget = input.PositionActualValue;
                            arr[i].GraceResetPending = true;
                        }
                    }

                    // ② 推进虚拟主轴 (自动同步 + 手动点动)
                    if (_syncRunning) _masterPos += _masterStep;
                    int jog = _jogMasterDir;
                    if (jog != 0) _masterPos += jog * JOG_MASTER_STEP;
                    long masterPos = _masterPos;

                    // ③ 各轴按 Base + masterPos * Ratio 跟随
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var a = arr[i];
                        ref var input = ref m.Slaves[a.SlaveIndex].PDO.InputsMapping<STF_Input>();
                        ref var output = ref m.Slaves[a.SlaveIndex].PDO.OutputsMapping<STF_Output>();
                        StepSync(a, masterPos, ref input, ref output);

                        ushort sw = input.StatusWord;
                        a.SnapStatusWord = sw;
                        a.SnapActualPosition = input.PositionActualValue;
                        a.SnapActualVelocity = input.VelocityActualValue;
                        a.SnapErrorCode = input.ErrorCode;
                        a.SnapDriveState = ParseDriveState(sw);

                        // —— 报警检测 (PDO 热路径: 只置 volatile 闩, 不锁/不碰 UI; 决策与呈现在 50ms 消费侧) ——
                        int fe = input.PositionActualValue - output.TargetPosition;   // 跟随误差 = 实际 - 本周期下发目标
                        a.SnapFollowError = fe;
                        bool eligibleNow = a.ServoEnabled && IsOperationEnabled(sw) && a.Ratio > 0 && !a.FaultReset;
                        if (!eligibleNow) { a.SyncStartCycle = 0; a.FollowErrConsec = 0; }
                        else if (a.SyncStartCycle == 0 || a.GraceResetPending) { a.SyncStartCycle = _pdoCycle; a.GraceResetPending = false; a.FollowErrConsec = 0; }
                        bool pastGrace = eligibleNow && a.SyncStartCycle != 0 && (_pdoCycle - a.SyncStartCycle) >= GRACE_CYCLES;
                        a.SyncEligible = pastGrace;
                        if (pastGrace) a.WasEligible = true;

                        // 跟随误差阈值随速度自适应: CSP 下实际天然滞后目标 ~一个周期增量(=masterStep×Ratio),
                        // 高速/大齿比时固定 5000 会误报 → 阈值 = 静态值 + 4×|本周期目标增量|。
                        int cmdDelta = output.TargetPosition - a.PrevTarget;
                        a.PrevTarget = output.TargetPosition;
                        int effLimit = a.FollowErrLimit + 4 * Math.Abs(cmdDelta);

                        // A 跟随误差 (过 grace 才判, 连续超限去抖)
                        if (pastGrace)
                        {
                            if (Math.Abs(fe) > effLimit) { if (++a.FollowErrConsec >= FE_CONSEC_N) a.AlarmFollowError = true; }
                            else { a.FollowErrConsec = 0; a.AlarmFollowError = false; }
                        }
                        else a.AlarmFollowError = false;

                        // B 驱动故障 / 同步中掉 OP (PDO 内零拷贝量, 即时)
                        a.AlarmFault = IsFault(sw) || input.ErrorCode != 0;
                        // 掉OP 仅在"用户仍要它使能 且 非故障复位中"才判, 否则用户主动去使能/故障复位会误报。
                        a.AlarmDropEnable = a.WasEligible && !IsOperationEnabled(sw) && a.ServoEnabled && !a.FaultReset;

                        // 去使能 / 故障复位 → 复位本轴同步监控 (防 latch 残留 + 防故障复位误报掉OP)
                        if (!a.ServoEnabled || a.FaultReset) { a.WasEligible = false; a.AlarmFollowError = false; a.AlarmDropEnable = false; a.FollowErrConsec = 0; }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDO] 控制线程异常退出: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // CSP 同步轴: CiA402 使能握手 (0x06→0x07→0x0F), 使能后按齿比跟随虚拟主轴。
        // 未使能 / 正在握手 / 故障时, 目标位置恒等于当前实际位置 (无跳变)。
        void StepSync(AxisController a, long masterPos, ref STF_Input input, ref STF_Output output)
        {
            output.ModesOfOperation = 8;
            ushort sw = input.StatusWord;
            ushort cw = 0;

            if (a.FaultReset) { cw = 0x80; a.FaultReset = false; }
            else if (IsFault(sw)) { /* 等待故障复位 */ }
            else if (!a.ServoEnabled) { a.CurrentTarget = input.PositionActualValue; a.Base = input.PositionActualValue; }
            else if ((sw & 0x4F) == 0x00) { /* NotReadyToSwitchOn: 等待 */ }
            else if (IsSwitchOnDisabled(sw)) { cw = 0x06; a.CurrentTarget = input.PositionActualValue; a.Base = input.PositionActualValue; }
            else if (IsReadyToSwitchOn(sw)) { cw = 0x07; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsSwitchedOn(sw)) { cw = 0x0F; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsOperationEnabled(sw))
            {
                // 同步轴核心: 目标 = 基准 + 主轴位置 × 齿比。
                // 齿比 1:1 → 各轴目标增量一致 (同一数据点同时映射); 齿比 ≠ 1 → 电子齿轮。
                a.CurrentTarget = a.Base + (int)Math.Round(masterPos * a.Ratio);
                output.TargetPosition = a.CurrentTarget;
                cw = 0x0F;
            }
            output.ControlWord = cw;
            a.SnapTargetPosition = a.CurrentTarget;
        }

        // ==================== UI 刷新 ====================

        void StatusUpdateLoop()
        {
            while (isRunning)
            {
                try
                {
                    var arr = axes;
                    var m = master;

                    // —— 报警决策 (后台线程; alarmMgr 内部加锁线程安全; 不碰 UI) ——
                    alarmMgr.DrainPending();                 // 落地事件回调队列
                    DetectAlarms(arr, m);                    // PDO 闩 + SDK 属性轮询 + WKC/链路

                    long masterPos = _masterPos;
                    bool sync = _syncRunning;
                    // BeginInvoke(异步) 而非 Invoke(同步): 否则断开时 UI 线程 Join 本线程会与本线程的同步 Invoke 互等死锁。
                    BeginInvoke(new Action(() =>
                    {
                        lblMasterPos.Text = $"主轴位置: {masterPos * 360.0 / PULSES_PER_REV:F2}° ({masterPos})" + (sync ? "  ● 同步中" : "");
                        lblMasterPos.ForeColor = sync ? Color.SeaGreen : Color.DimGray;

                        for (int i = 0; i < arr.Length && i < gridAxes.Rows.Count; i++)
                        {
                            var a = arr[i];
                            ushort sw = a.SnapStatusWord;
                            var row = gridAxes.Rows[i];
                            row.Cells[COL_STATE].Value = a.SnapDriveState;
                            row.Cells[COL_SW].Value = $"0x{sw:X4}";
                            row.Cells[COL_ACTPOS].Value = $"{a.SnapActualPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapActualPosition})";
                            row.Cells[COL_TGTPOS].Value = $"{a.SnapTargetPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapTargetPosition})";
                            row.Cells[COL_VEL].Value = a.SnapActualVelocity.ToString();
                            row.Cells[COL_ENABLED].Value = a.ServoEnabled ? "ON" : "off";

                            var stateCell = row.Cells[COL_STATE];
                            if (IsFault(sw) || a.SnapErrorCode != 0) stateCell.Style.ForeColor = Color.Firebrick;
                            else if (IsOperationEnabled(sw)) stateCell.Style.ForeColor = Color.SeaGreen;
                            else stateCell.Style.ForeColor = Color.DimGray;

                            // 出问题的轴整行淡红; 正常复位白底
                            bool bad = IsFault(sw) || a.SnapErrorCode != 0 || a.AlarmFollowError || a.AlarmDropEnable;
                            row.DefaultCellStyle.BackColor = bad ? Color.FromArgb(252, 235, 235) : Color.White;
                        }

                        RefreshAlarmUI();
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[StatusUpdateLoop] 异常: {ex.GetType().Name}: {ex.Message}");
                }
                Thread.Sleep(50);
            }
        }

        // 后台线程: 据 PDO 闩 + SDK 属性轮询 升/降报警 (alarmMgr 线程安全)
        void DetectAlarms(AxisController[] arr, DarraEtherCAT m)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                var a = arr[i];
                // —— 来自 PDO 热路径的闩 (同步类) ——
                if (a.AlarmFollowError) RaiseFault(i, AlarmType.跟随误差, $"跟随误差 {a.SnapFollowError} 脉冲 > 限值 {a.FollowErrLimit}");
                else alarmMgr.Clear(i, AlarmType.跟随误差);
                if (a.AlarmDropEnable) RaiseFault(i, AlarmType.掉OP, "同步运行中掉出 OperationEnabled");
                else alarmMgr.Clear(i, AlarmType.掉OP);
                if (a.AlarmFault) RaiseFault(i, AlarmType.驱动故障, $"状态字Fault 或 错误码 0x{a.SnapErrorCode:X4}");
                else alarmMgr.Clear(i, AlarmType.驱动故障);

                // —— SDK 托管属性轮询 (50ms 安全) ——
                if (m == null) continue;
                try
                {
                    var slave = m.Slaves[a.SlaveIndex];
                    if (slave.IsLost || slave.State != EcState.OP) RaiseFault(i, AlarmType.掉站, $"State={slave.State} IsLost={slave.IsLost}");
                    else alarmMgr.Clear(i, AlarmType.掉站);

                    var al = slave.ErrorCode;                 // EcALState
                    if (al != EcALState.NoError) alarmMgr.Raise(i, AlarmType.AL状态, AlarmSeverity.Fault, $"AL: {al}");
                    else alarmMgr.Clear(i, AlarmType.AL状态);

                    if (slave.PrimaryLinkBroken) alarmMgr.Raise(i, AlarmType.链路断, AlarmSeverity.Fault, "主链路断开");
                    else if (slave.SecondaryLinkBroken) alarmMgr.Raise(i, AlarmType.链路断, AlarmSeverity.Warning, "副链路断开");
                    else alarmMgr.Clear(i, AlarmType.链路断);

                    var dc = slave.Diagnostics.DC;             // SlaveDCDiagnostics
                    if (!dc.IsInSync) alarmMgr.Raise(i, AlarmType.DC失步, AlarmSeverity.Warning, $"DC偏差 {dc.SyncTimeDifference}ns");
                    else alarmMgr.Clear(i, AlarmType.DC失步);
                }
                catch { /* master 正在关闭等 — 下个周期再来 */ }
            }

            // —— 主站级: 链路 + WKC (单次, 不分轴) ——
            if (m == null) return;
            try
            {
                var ls = m.LinkState;
                if (ls == EcLinkState.Disconnected) RaiseFault(-1, AlarmType.链路断, "主站链路断开");
                else if (ls == EcLinkState.PrimaryOnly || ls == EcLinkState.SecondaryOnly) alarmMgr.Raise(-1, AlarmType.链路断, AlarmSeverity.Warning, $"链路降级 {ls}");
                else alarmMgr.Clear(-1, AlarmType.链路断);

                // SDK: GetGroup*WKC(masterIndex, group) 是 static。masterIndex 须用实例的 MasterNumber(≥1, 0 是保留),
                // group 用 1(默认组; 0 被 native 拒)。exp 拿不到(返 0)时下面 exp>0 守卫自动静默, 不误报。
                ushort exp = DarraEtherCAT.GetGroupExpectedWKC(m.MasterNumber, 1);
                ushort act = DarraEtherCAT.GetGroupActualWKC(m.MasterNumber, 1);
                if (exp > 0 && act < exp)
                {
                    // WKC 铁律: 有从站掉了 → 报警+由 slave.IsLost 逐轴定位, *不* 停 OP/改 exp/去使能正常轴
                    _wkcMissConsec++;
                    var sev = _wkcMissConsec >= PDO_LOSS_FAULT ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                    alarmMgr.Raise(-1, AlarmType.WKC短缺, sev, $"实测WKC {act} < 期望 {exp} (连续{_wkcMissConsec})");
                }
                else { _wkcMissConsec = 0; alarmMgr.Clear(-1, AlarmType.WKC短缺); }
            }
            catch { }
        }

        // 升故障级报警 + (首次)触发组停闭锁
        void RaiseFault(int axis, AlarmType type, string msg)
        {
            alarmMgr.Raise(axis, type, AlarmSeverity.Fault, msg);
            if (!_groupStopLatched) GroupStop(axis, $"{(axis < 0 ? "主站" : "轴" + (axis + 1))} {type}");
        }

        // ==================== 报警 UI ====================

        void InitAlarmGrid()
        {
            gridAlarms.AutoGenerateColumns = false;
            gridAlarms.Columns.Clear();
            AddAlarmCol("时间", 64);
            AddAlarmCol("轴", 44);
            AddAlarmCol("严重度", 60);
            AddAlarmCol("类型", 96);
            var msg = new DataGridViewTextBoxColumn { HeaderText = "消息", ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            gridAlarms.Columns.Add(msg);
            AddAlarmCol("状态", 64);
        }

        void AddAlarmCol(string header, int width)
        {
            gridAlarms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, Width = width, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        }

        // UI 线程 (Invoke 内) 调用: 刷新横幅 + 报警列表
        void RefreshAlarmUI()
        {
            alarmMgr.Snapshot(out int active, out int faults, out _, out string top);
            int hist = alarmMgr.HistoryCount();

            if (active == 0)
            {
                pnlAlarmBanner.BackColor = Color.FromArgb(223, 240, 216);
                lblAlarmBanner.ForeColor = Color.SeaGreen;
                lblAlarmBanner.Text = "● 无报警 — 系统正常";
            }
            else
            {
                bool fault = faults > 0;
                pnlAlarmBanner.BackColor = fault ? Color.FromArgb(248, 215, 218) : Color.FromArgb(255, 243, 205);
                lblAlarmBanner.ForeColor = fault ? Color.Firebrick : Color.DarkOrange;
                lblAlarmBanner.Text = (fault ? "⛔ 报警(故障): " : "▲ 报警(警告): ") + (top ?? "");
            }
            lblAlarmCount.Text = $"激活:{active}  故障:{faults}  历史:{hist}";
            btnAlarmAck.Enabled = (active > 0 || hist > 0);

            var rows = alarmMgr.BuildRowsIfDirty();
            if (rows == null) return;
            gridAlarms.Rows.Clear();
            foreach (var en in rows)
            {
                int r = gridAlarms.Rows.Add();
                var row = gridAlarms.Rows[r];
                row.Cells[ACOL_TIME].Value = en.Time.ToString("HH:mm:ss");
                row.Cells[ACOL_AXIS].Value = en.AxisNo < 0 ? "主站" : (en.AxisNo + 1).ToString();
                row.Cells[ACOL_SEV].Value = en.Sev == AlarmSeverity.Fault ? "故障" : en.Sev == AlarmSeverity.Warning ? "警告" : "信息";
                row.Cells[ACOL_TYPE].Value = en.Type.ToString();
                row.Cells[ACOL_MSG].Value = en.Message;
                row.Cells[ACOL_STATE].Value = en.Active ? "激活" : "已恢复";
                if (en.Active && en.Sev == AlarmSeverity.Fault) { row.DefaultCellStyle.BackColor = Color.FromArgb(248, 215, 218); row.DefaultCellStyle.ForeColor = Color.Firebrick; }
                else if (en.Active && en.Sev == AlarmSeverity.Warning) { row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205); row.DefaultCellStyle.ForeColor = Color.DarkOrange; }
                else row.DefaultCellStyle.ForeColor = Color.Silver;
            }
        }

        // ==================== 断开 ====================

        void btnDisconnect_Click(object sender, EventArgs e) { Disconnect(); }

        // 先 Join 工作线程再 Close: 防止 DetectAlarms/PdoControlLoop 正访问 m.Slaves[...] 时 native 被释放 → UAF/AV。
        // StatusUpdateLoop 用 BeginInvoke(异步)刷 UI, 故 UI 线程 Join 它不会与其 Invoke 互等死锁。
        void JoinWorkers()
        {
            var t1 = _pdoThread; var t2 = _statusThread;
            _pdoThread = null; _statusThread = null;
            try { t1?.Join(500); } catch { }
            try { t2?.Join(500); } catch { }
        }

        void Disconnect()
        {
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { } catch (Exception ex) { Console.WriteLine($"[Disconnect] CTS Cancel 异常: {ex.Message}"); }

            _syncRunning = false;
            _jogMasterDir = 0;
            foreach (var a in axes) { a.ServoEnabled = false; a.FaultReset = false; }
            isRunning = false;

            try { DarraEtherCAT.Logs.Updated -= OnLogUpdated; } catch (Exception ex) { Console.WriteLine($"[Disconnect] 取消日志订阅异常: {ex.Message}"); }

            JoinWorkers();
            try { DarraEtherCAT.Abort(); } catch (Exception ex) { Console.WriteLine($"[Disconnect] Abort 异常: {ex.Message}"); }

            var m = master;
            master = null;
            if (m != null)
            {
                Task.Run(() =>
                {
                    try { m.Close(); } catch (Exception ex) { Console.WriteLine($"[Disconnect] Close异常: {ex.Message}"); }
                });
            }

            axes = new AxisController[0];
            gridAxes.Rows.Clear();

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            grpMaster.Enabled = false;
            grpGlobal.Enabled = false;
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            lblAxisCount.Text = "轴数: -";
            lblMasterPos.Text = "主轴位置: -";
            lblMasterPos.ForeColor = Color.DimGray;

            _groupStopLatched = false;
            pnlAlarmBanner.BackColor = Color.FromArgb(238, 238, 238);
            lblAlarmBanner.ForeColor = Color.DimGray;
            lblAlarmBanner.Text = "● 未连接";
            lblAlarmCount.Text = "激活:0  故障:0  历史:0";
            btnAlarmAck.Enabled = false;
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

            if (txtLog.TextLength > LOG_MAX_TEXT)
            {
                string text = txtLog.Text;
                int cutAt = text.IndexOf('\n', text.Length / 2);
                if (cutAt > 0) txtLog.Text = text.Substring(cutAt + 1);
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _logFlushTimer?.Stop();
            _logFlushTimer?.Dispose();

            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] CTS Cancel 异常: {ex.Message}"); }
            try { DarraEtherCAT.Logs.Updated -= OnLogUpdated; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] 取消日志订阅异常: {ex.Message}"); }

            isRunning = false;
            JoinWorkers();
            try { DarraEtherCAT.Abort(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] Abort 异常: {ex.Message}"); }

            var m = master;
            master = null;
            if (m != null)
            {
                try { m.Close(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] m.Close 异常: {ex.Message}"); }
            }
            base.OnFormClosing(e);
        }
    }
}
