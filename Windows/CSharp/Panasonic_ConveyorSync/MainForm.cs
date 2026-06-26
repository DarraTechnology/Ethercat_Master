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

namespace Panasonic_ConveyorSync
{
    // ==================== 松下 MINAS A6B 伺服 PDO (CiA 402) ====================
    // 厂商: Panasonic; PDO 用 ESI 默认映射 (RxPDO 0x1600 / TxPDO 0x1A00)。
    // RxPDO 0x1600 (输出, SM2) = 9 字节; TxPDO 0x1A00 (输入, SM3) = 23 字节。
    // 字段顺序必须与 PDO 条目顺序逐字节一致, 不可调换。
    //
    // 本例 = 传送带多轴协调 / 主从同步 + 物料跟踪 演示, 永远 CSP (Mode=8)。
    // 一根"虚拟主轴"(或选定从站的编码器) 每周期推进, 各从轴按各自"齿比 + 相位"跟随:
    //   output.TargetPosition = a.Base + (int)Math.Round(masterPos * a.Ratio) + a.Phase
    // 物料经传感器 (Touch Probe 0x60B8/0x60B9/0x60BA) 捕获主轴位置, 随带推进; 进入抓取区时触发飞行抓取。

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Output  // 9 字节 (RxPDO 0x1600)
    {
        public ushort ControlWord;          // 0x6040:0  UINT
        public sbyte ModesOfOperation;      // 0x6060:0  SINT  (本例恒 CSP=8)
        public int TargetPosition;          // 0x607A:0  DINT
        public ushort TouchProbeFunction;   // 0x60B8:0  UINT
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Input  // 23 字节 (TxPDO 0x1A00)
    {
        public ushort ErrorCode;                // 0x603F:0  UINT
        public ushort StatusWord;               // 0x6041:0  UINT
        public sbyte ModesOfOperationDisplay;   // 0x6061:0  SINT
        public int PositionActualValue;         // 0x6064:0  DINT
        public ushort TouchProbeStatus;         // 0x60B9:0  UINT
        public int TouchProbe1PosValue;         // 0x60BA:0  DINT  (probe1 上升沿锁存位置)
        public int FollowingErrorActualValue;   // 0x60F4:0  DINT
        public uint DigitalInputs;              // 0x60FD:0  UDINT
    }

    // ==================== 报警 ====================
    enum AlarmSeverity { Fault = 0, Warning = 1, Info = 2 }   // 颜色 Firebrick / DarkOrange / DimGray

    enum AlarmType
    {
        跟随误差, 掉OP, 驱动故障, AL状态, DC失步,
        掉站, 链路断, 主站掉OP, 紧急码, 丢帧, WKC短缺, 身份不符
    }

    enum ZoneState { 未到, 在区, 已过 }

    sealed class AlarmEntry
    {
        public DateTime Time;
        public int AxisNo;            // ≥0 轴号(显示+1); -1 主站/全局
        public AlarmSeverity Sev;
        public AlarmType Type;
        public string Message;
        public bool Active;
        public string Key;
    }

    /// <summary>
    /// 线程安全报警管理器。多生产者(事件回调线程 / PDO 线程经闩) + 单消费者(UI 50ms)。
    /// 生产侧只入队/置闩不碰 UI; 消费侧统一在 UI 线程 Drain + 呈现。(与 STF-EC_SyncAxis 一致)
    /// </summary>
    sealed class AlarmManager
    {
        readonly object _lock = new object();
        readonly Dictionary<string, AlarmEntry> _active = new Dictionary<string, AlarmEntry>();
        readonly List<AlarmEntry> _history = new List<AlarmEntry>();
        readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        const int HISTORY_CAP = 500;
        public volatile bool Dirty;

        static string MakeKey(int axis, AlarmType t) => axis + "|" + t;

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

        public void Enqueue(Action a) => _pending.Enqueue(a);
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
                    if (e.Sev < worst) worst = e.Sev;
                    if (topMsg == null || e.Sev < bestSev)
                    {
                        bestSev = e.Sev;
                        topMsg = $"{(e.AxisNo < 0 ? "主站" : "轴" + (e.AxisNo + 1))} {e.Type}: {e.Message}";
                    }
                }
            }
        }

        public bool HasActiveFault() { lock (_lock) { foreach (var e in _active.Values) if (e.Sev == AlarmSeverity.Fault) return true; return false; } }
        public int HistoryCount() { lock (_lock) { return _history.Count; } }
        public List<AlarmEntry> BuildRowsIfDirty() { lock (_lock) { if (!Dirty) return null; Dirty = false; return new List<AlarmEntry>(_history); } }

        public void Reset() { lock (_lock) { _active.Clear(); _history.Clear(); while (_pending.TryDequeue(out _)) { } Dirty = true; } }

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
    /// 单个轴的控制状态 (UI 线程写控制变量, PDO 线程写快照, 互不阻塞)。
    /// 传送带主从扩展: Ratio (电子齿比) + Phase (相位偏移) + Base (启动同步时实际位置基准)。
    /// </summary>
    sealed class AxisController
    {
        public readonly int SlaveIndex;
        public readonly int PhysAddr;
        public AxisController(int idx, int physAddr) { SlaveIndex = idx; PhysAddr = physAddr; }

        // 控制 (UI 写, PDO 读)
        public volatile bool ServoEnabled;
        public volatile bool FaultReset;

        // 主从扩展 (UI 写, PDO 读; double 无法 volatile, 改齿比是低频整量写足够安全)
        public double Ratio = 1.0;          // 电子齿比
        public volatile int Phase;          // 相位偏移 (脉冲)
        public int Base;                    // 启动同步时的实际位置基准 (PDO 线程写/读)

        // 快照 (PDO 写, UI 读)
        public volatile ushort SnapStatusWord;
        public volatile int SnapActualPosition;
        public volatile int SnapTargetPosition;
        public volatile int SnapActualVelocity;
        public volatile ushort SnapErrorCode;
        public volatile string SnapDriveState = "---";
        public volatile string SnapRole = "从";    // 主/从/抓取 (PDO 线程算)

        // CSP 内部状态 (仅 PDO 线程访问)
        public int CurrentTarget;
        public int PrevActualPos;       // 上周期实际位置, 用于估算实际速度 (松下默认 PDO 无速度对象)

        // —— 同步监控 / grace (PDO 线程拥有写) ——
        public long SyncStartCycle;
        public volatile bool GraceResetPending;
        public volatile bool SyncEligible;
        public bool WasEligible;
        public int FollowErrConsec;
        public volatile int SnapFollowError;
        public int FollowErrLimit = 100000;   // 松下 23 位编码器(8388608/圈), 默认 ≈4.3° 跟随误差阈值
        public int PrevTarget;

        // —— 异常闩 (PDO 线程置, 50ms 消费侧据边沿 Raise/Clear) ——
        public volatile bool AlarmFollowError;
        public volatile bool AlarmDropEnable;
        public volatile bool AlarmFault;

        // —— Touch Probe 状态机 (仅 PDO 线程) ——
        public int ProbeState;          // 0 空闲, 1 已武装, 2 已锁存待复位
        public ushort PrevProbeStatus;  // 上周期 TouchProbeStatus, 判 bit1 上升沿
    }

    /// <summary>传送带上被跟踪的一件物料。捕获时记主轴位置, 随带推进 Travel = 当前主轴位置 - 捕获位置。</summary>
    sealed class Product
    {
        public int Id;
        public string Source;           // "传感器" / "手动"
        public long CaptureMasterPos;   // 捕获时主轴位置 (脉冲)
        public long Travel;             // 自捕获以来行程 (脉冲), 每周期更新
        public ZoneState Zone;
        public bool Picked;             // 是否已被抓取轴飞行抓取
    }

    /// <summary>
    /// 松下 A6 传送带主从同步 + 物料跟踪 主窗体。永远 CSP (Mode=8)。
    /// 虚拟/编码器主轴每周期推进, 各从轴按 Base + masterPos*Ratio + Phase 跟随; 物料 Touch Probe 捕获 + 飞行抓取。
    /// </summary>
    public partial class MainForm : Form
    {
        DarraEtherCAT master;
        volatile bool isRunning = false;
        private CancellationTokenSource _connectCts;
        Thread _statusThread;

        volatile AxisController[] axes = new AxisController[0];

        // —— 主轴 (PDO 线程拥有 _masterPos; UI 经下列标志/字段交互) ——
        long _masterPos = 0;                    // 主轴位置 (脉冲)
        volatile bool _syncRunning = false;     // 是否自动同步推进
        volatile int _masterStep = 1000;        // 每周期推进步长 (= 脉冲/秒 ÷ 1000)
        volatile bool _resyncRequested = false; // 请求重新快照各轴 Base + 主轴清零
        volatile int _jogMasterDir = 0;         // 主轴手动点动方向 +1/-1/0

        // —— 编码器主轴模式 ——
        volatile bool _encoderMode = false;     // false=虚拟主轴, true=编码器主轴
        volatile int _encoderAxisIndex = 0;     // 作主轴源的从站索引
        long _encoderBase = 0;                   // 编码器主轴基准 (PDO 线程)
        volatile bool _encoderInitPending = false;

        // —— 物料跟踪 / 抓取 ——
        readonly object _prodLock = new object();
        readonly List<Product> _products = new List<Product>();
        int _nextProductId = 1;
        volatile bool _probeArmRequest = false;  // UI 武装请求
        int _injectRequest = 0;                  // UI 注入请求计数 (Interlocked 原子读改写, PDO 线程消费)
        volatile bool _clearRequest = false;
        double _zoneFromDeg = 180;               // 抓取区起 (主轴度; double 无法 volatile, 低频整量写)
        double _zoneToDeg = 260;                 // 抓取区止 (主轴度)
        volatile int _pickAxisIndex = -1;        // 抓取轴索引, -1=无
        // —— 捕获源 (光电接收方式) ——
        volatile int _captureSrc = 0;            // 0=Touch Probe 硬件锁存(EXT1), 1=数字输入 DI(0x60FD)
        volatile int _diBit = 0;                 // DI 模式下 0x60FD 的传感器位
        bool _prevDiHigh;                        // DI 模式上升沿检测 (仅 PDO 线程)
        // —— 演示 / 自动来料 (模拟光电周期触发, 无需真传感器) ——
        volatile bool _demoAuto = false;
        double _demoIntervalDeg = 90;            // 自动来料间隔 (主轴度)
        bool _demoWasOn;                         // 演示模式上升沿 (仅 PDO 线程)
        long _lastAutoInjectMasterPos;           // 上次自动来料时主轴位置 (仅 PDO 线程)
        // 抓取引擎 (PDO 线程)
        bool _pickEngaged = false;
        long _pickEngageMasterPos = 0;
        int _pickProductId = 0;

        // 异步日志
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int LOG_MAX_TEXT = 50000;
        private const int LOG_QUEUE_CAP = 500;
        private int _processedLogCount;
        private readonly object _logProcLock = new object();

        const int PULSES_PER_REV = 8388608;     // 松下 A6 23 位编码器 (2^23), 角度换算用
        const int JOG_MASTER_STEP = 4000;       // 主轴手动点动每周期步长 (脉冲)

        // 轴总览表列索引
        const int COL_SEL = 0, COL_AXIS = 1, COL_ROLE = 2, COL_RATIO = 3, COL_PHASE = 4, COL_STATE = 5,
                  COL_SW = 6, COL_ACTPOS = 7, COL_TGTPOS = 8, COL_VEL = 9, COL_ENABLED = 10;

        // 物料表列索引
        const int PCOL_ID = 0, PCOL_SRC = 1, PCOL_CAP = 2, PCOL_TRACK = 3, PCOL_ZONE = 4;

        // 报警字段
        readonly AlarmManager alarmMgr = new AlarmManager();
        long _pdoCycle = 0;
        volatile bool _groupStopLatched = false;

        // 报警表列索引
        const int ACOL_TIME = 0, ACOL_AXIS = 1, ACOL_SEV = 2, ACOL_TYPE = 3, ACOL_MSG = 4, ACOL_STATE = 5;

        // 报警阈值 / 防误报参数
        const int FE_CONSEC_N = 50;
        const int GRACE_CYCLES = 200;
        const int PDO_LOSS_FAULT = 5;
        int _wkcMissConsec = 0;

        public MainForm()
        {
            InitializeComponent();
            InitGrid();
            InitProductGrid();
            InitAlarmGrid();
            BindEvents();
            grpTrack.Enabled = false;
            numDiBit.Enabled = false; lblDiBit.Enabled = false;   // 默认 Touch Probe 源, DI 位禁用
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

            // 主轴源
            rbVirtual.CheckedChanged += MasterSource_Changed;
            rbEncoder.CheckedChanged += MasterSource_Changed;
            cboEncoderAxis.SelectedIndexChanged += (s, e) => { if (cboEncoderAxis.SelectedIndex >= 0) _encoderAxisIndex = cboEncoderAxis.SelectedIndex; };
            cboPickAxis.SelectedIndexChanged += (s, e) => _pickAxisIndex = cboPickAxis.SelectedIndex - 1;  // 0=(无) → -1
            numMasterSpeed.ValueChanged += (s, e) => _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));
            numZoneFrom.ValueChanged += (s, e) => _zoneFromDeg = (double)numZoneFrom.Value;
            numZoneTo.ValueChanged += (s, e) => _zoneToDeg = (double)numZoneTo.Value;

            btnSyncStart.Click += btnSyncStart_Click;
            btnSyncStop.Click += btnSyncStop_Click;
            btnMasterJogFwd.MouseDown += (s, e) => { if (!_groupStopLatched) _jogMasterDir = 1; };
            btnMasterJogFwd.MouseUp += (s, e) => _jogMasterDir = 0;
            btnMasterJogRev.MouseDown += (s, e) => { if (!_groupStopLatched) _jogMasterDir = -1; };
            btnMasterJogRev.MouseUp += (s, e) => _jogMasterDir = 0;

            // 物料
            btnInjectProduct.Click += (s, e) => { if (isRunning) { System.Threading.Interlocked.Increment(ref _injectRequest); Log("注入物料 (按当前主轴位置登记)"); } };
            btnArmProbe.Click += btnArmProbe_Click;
            btnClearProducts.Click += (s, e) => { _clearRequest = true; Log("清空物料"); };
            cboCaptureSrc.SelectedIndexChanged += (s, e) =>
            {
                _captureSrc = cboCaptureSrc.SelectedIndex < 0 ? 0 : cboCaptureSrc.SelectedIndex;
                numDiBit.Enabled = lblDiBit.Enabled = (_captureSrc == 1);
                Log(_captureSrc == 1 ? $"捕获源 = 数字输入 DI (0x60FD bit{_diBit})" : "捕获源 = Touch Probe (EXT1 锁存)");
            };
            numDiBit.ValueChanged += (s, e) => _diBit = (int)numDiBit.Value;
            chkDemo.CheckedChanged += (s, e) => { _demoAuto = chkDemo.Checked; Log(_demoAuto ? $"演示: 自动来料开 (每 {_demoIntervalDeg:F0}° 一件, 模拟光电触发, 无需真传感器)" : "演示: 自动来料关"); };
            numDemoInterval.ValueChanged += (s, e) => _demoIntervalDeg = (double)numDemoInterval.Value;

            // 全局控制
            btnAllEnable.Click += (s, e) =>
            {
                if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
                ForEachTarget(a => a.ServoEnabled = true, "全部使能");
            };
            btnAllDisable.Click += (s, e) => ForEachTarget(a => a.ServoEnabled = false, "全部去使能(急停)");
            btnAllFaultReset.Click += (s, e) => ForEachTarget(a => a.FaultReset = true, "全部故障复位");

            btnAlarmAck.Click += btnAlarmAck_Click;

            picConveyor.Paint += picConveyor_Paint;
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
            if (list.Count == 0) list.AddRange(axes);
            return list;
        }
        // ==================== 总览表 ====================

        void InitGrid()
        {
            gridAxes.AutoGenerateColumns = false;
            gridAxes.Columns.Clear();
            var chk = new DataGridViewCheckBoxColumn { HeaderText = "选", Width = 36, Name = "colSel" };
            gridAxes.Columns.Add(chk);                       // 0
            AddTextCol(gridAxes, "轴", 44, true);            // 1
            AddTextCol(gridAxes, "角色", 56, true);          // 2 (计算)
            AddTextCol(gridAxes, "齿比", 64, false);         // 3 可编辑
            AddTextCol(gridAxes, "相位(脉冲)", 90, false);   // 4 可编辑
            AddTextCol(gridAxes, "驱动状态", 140, true);     // 5
            AddTextCol(gridAxes, "状态字", 64, true);        // 6
            AddTextCol(gridAxes, "实际位置(°)", 130, true);  // 7
            AddTextCol(gridAxes, "目标位置(°)", 130, true);  // 8
            AddTextCol(gridAxes, "实际速度", 84, true);      // 9
            AddTextCol(gridAxes, "使能", 50, true);          // 10
        }

        void InitProductGrid()
        {
            gridProducts.AutoGenerateColumns = false;
            gridProducts.Columns.Clear();
            AddTextCol(gridProducts, "编号", 50, true);          // 0
            AddTextCol(gridProducts, "来源", 60, true);          // 1
            AddTextCol(gridProducts, "捕获位置(°)", 100, true);  // 2
            AddTextCol(gridProducts, "跟踪行程(°)", 100, true);  // 3
            AddTextCol(gridProducts, "区域", 70, true);          // 4
        }

        static void AddTextCol(DataGridView g, string header, int width, bool readOnly)
        {
            g.Columns.Add(new DataGridViewTextBoxColumn
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
                row.Cells[COL_ROLE].Value = "从";
                row.Cells[COL_RATIO].Value = a.Ratio.ToString("F3", CultureInfo.InvariantCulture);
                row.Cells[COL_PHASE].Value = a.Phase.ToString();
                row.Cells[COL_STATE].Value = "---";
            }
        }

        // 解析「齿比」/「相位」单元格
        void GridAxes_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= axes.Length) return;
            var row = gridAxes.Rows[e.RowIndex];
            var a = axes[e.RowIndex];
            if (e.ColumnIndex == COL_RATIO)
            {
                string text = Convert.ToString(row.Cells[COL_RATIO].Value);
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio)
                    || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out ratio))
                {
                    a.Ratio = ratio;
                    a.GraceResetPending = true;
                    a.AlarmFollowError = false;
                    Log($"轴{a.SlaveIndex + 1} 齿比 = {ratio:F3}");
                }
                else Log($"轴{a.SlaveIndex + 1} 齿比输入无效, 保持 {a.Ratio:F3}");
                row.Cells[COL_RATIO].Value = a.Ratio.ToString("F3", CultureInfo.InvariantCulture);
            }
            else if (e.ColumnIndex == COL_PHASE)
            {
                string text = Convert.ToString(row.Cells[COL_PHASE].Value);
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int phase)
                    || int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out phase))
                {
                    a.Phase = phase;
                    a.GraceResetPending = true;
                    a.AlarmFollowError = false;
                    Log($"轴{a.SlaveIndex + 1} 相位 = {phase} 脉冲");
                }
                else Log($"轴{a.SlaveIndex + 1} 相位输入无效, 保持 {a.Phase}");
                row.Cells[COL_PHASE].Value = a.Phase.ToString();
            }
        }

        // 连接成功后填充主轴源/抓取轴下拉
        void PopulateAxisCombos()
        {
            cboEncoderAxis.Items.Clear();
            cboPickAxis.Items.Clear();
            cboPickAxis.Items.Add("(无)");
            for (int i = 0; i < axes.Length; i++)
            {
                cboEncoderAxis.Items.Add($"轴{i + 1}");
                cboPickAxis.Items.Add($"轴{i + 1}");
            }
            if (cboEncoderAxis.Items.Count > 0) cboEncoderAxis.SelectedIndex = 0;
            cboPickAxis.SelectedIndex = 0;   // (无)
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

        static bool IsCleanOP(EcState s) => ((int)s & 0x0F) == (int)EcState.OP && ((int)s & 0x10) == 0;

        string GetDeniPath()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDir, "EtherCATRestore", "config.deni");
        }
        // ==================== 连接 ====================

        async void btnConnect_Click(object sender, EventArgs e)
        {
            string deniPath = GetDeniPath();
            if (!File.Exists(deniPath))
            {
                MessageBox.Show($"配置文件不存在: {deniPath}\n请用主站 GUI 扫描松下 MINAS A6B 从站, 导出 config.deni 放到该目录\n(详见 README.md)", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            Log("正在初始化主站 (CSP 主从同步模式)...");

            DarraEtherCAT newMaster = null;
            string errorMsg = null;
            int slaveCount = 0;

            _connectCts = new CancellationTokenSource();
            var ct = _connectCts.Token;

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

                    // 逐轴: 经 CoE 把 PDO 强制重映为松下默认单 PDO (0x1600 / 0x1A00), 让布局确定 + 启用 DC
                    for (int i = 0; i < slaveCount; i++)
                    {
                        var slave = newMaster.Slaves[i];
                        if (slave.CoE != null)
                        {
                            try
                            {
                                slave.CoE.SDOWrite(0x1C12, 0, new byte[] { 0 });
                                slave.CoE.SDOWrite(0x1C12, 1, BitConverter.GetBytes((ushort)0x1600));
                                slave.CoE.SDOWrite(0x1C12, 0, new byte[] { 1 });
                                slave.CoE.SDOWrite(0x1C13, 0, new byte[] { 0 });
                                slave.CoE.SDOWrite(0x1C13, 1, BitConverter.GetBytes((ushort)0x1A00));
                                slave.CoE.SDOWrite(0x1C13, 0, new byte[] { 1 });
                                Log($"轴{i + 1} PDO 重映 0x1C12←0x1600 / 0x1C13←0x1A00 (9/23) 完成");
                            }
                            catch (Exception ex) { Log($"轴{i + 1} PDO 重映失败(将保持驱动默认): {ex.Message}"); }
                        }

                        if (slave.HasDC) slave.ConfigureDC(1000000);   // Sync0 = 1ms (CSP 必须 DC)
                        slave.CoE?.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));
                    }

                    if (!newMaster.SetState(EcState.SafeOp)) { errorMsg = "SafeOp 失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // PDO 尺寸自检: 用驱动实际进程映像字节数核对结构体 (输出9 / 输入23)
                    int expOut = Marshal.SizeOf<PA_Output>();
                    int expIn = Marshal.SizeOf<PA_Input>();
                    for (int i = 0; i < slaveCount; i++)
                    {
                        int realOut = newMaster.Slaves[i].OutputsByteCount;
                        int realIn = newMaster.Slaves[i].InputsByteCount;
                        Log($"轴{i + 1} PDO 实测: 输出={realOut}B 输入={realIn}B (结构体 输出={expOut}/输入={expIn})");
                        if (realOut != expOut || realIn != expIn)
                        {
                            errorMsg = $"轴{i + 1} PDO 尺寸不符: 驱动实际 输出{realOut}/输入{realIn} 字节, 结构体 输出{expOut}/输入{expIn}。" +
                                       "驱动当前 PDO 映射 ≠ config.deni —— 需按驱动实际映射改 PA_Output/PA_Input, 或让 config.deni 经 CoE 真正重映 0x1C12/0x1600。";
                            return false;
                        }
                    }

                    // 逐轴 PDO 初始化 (SafeOp 后, OP 前): CSP, 目标位置 = 当前实际位置 (避免上电跳变)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var input = ref newMaster.Slaves[i].PDO.InputsMapping<PA_Input>();
                        ref var output = ref newMaster.Slaves[i].PDO.OutputsMapping<PA_Output>();
                        output.ModesOfOperation = 8;
                        output.TargetPosition = input.PositionActualValue;
                        output.TouchProbeFunction = 0;
                    }

                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }
                    if (!newMaster.SetState(EcState.OP)) { errorMsg = "OP 失败"; return false; }

                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var tOut = ref newMaster.Slaves[i].PDO.OutputsMapping<PA_Output>();
                        ref var tIn = ref newMaster.Slaves[i].PDO.InputsMapping<PA_Input>();
                        tOut.ControlWord = 0;
                        tOut.ModesOfOperation = 8;
                        tOut.TargetPosition = tIn.PositionActualValue;
                    }
                    return true;
                }
                catch (Exception ex) { errorMsg = ex.Message; return false; }
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
                if (master == null) { btnConnect.Enabled = true; btnDisconnect.Enabled = false; }
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
                a.CurrentTarget = master.Slaves[i].PDO.InputsMapping<PA_Input>().PositionActualValue;
                a.Base = a.CurrentTarget;
                a.PrevActualPos = a.CurrentTarget;   // 防首周期速度估算 (actPos-0)*1000 溢出
                list.Add(a);
            }
            axes = list.ToArray();

            alarmMgr.Reset();
            _groupStopLatched = false;
            _wkcMissConsec = 0;

            // 主轴 / 物料 复位
            _masterPos = 0;
            _syncRunning = false;
            _jogMasterDir = 0;
            _resyncRequested = false;
            _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));
            _encoderMode = rbEncoder.Checked;
            _encoderInitPending = true;
            _zoneFromDeg = (double)numZoneFrom.Value;
            _zoneToDeg = (double)numZoneTo.Value;
            _probeArmRequest = false;
            _pickEngaged = false;
            lock (_prodLock) { _products.Clear(); _nextProductId = 1; }

            BuildAxisRows();
            PopulateAxisCombos();
            gridProducts.Rows.Clear();

            RegisterEvents();
            DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
            _processedLogCount = DarraEtherCAT.Logs.Count;
            DarraEtherCAT.Logs.Updated += OnLogUpdated;

            isRunning = true;
            master.Events.ProcessDataCyclicSync += OnPdoCycle;   // 实时控制挂 PDO 周期回调 (随总线周期触发)
            _statusThread = new Thread(StatusUpdateLoop) { IsBackground = true }; _statusThread.Start();

            grpMaster.Enabled = true;
            grpGlobal.Enabled = true;
            grpTrack.Enabled = true;
            lblStatus.Text = "已连接 (CSP 主从同步)";
            lblStatus.ForeColor = Color.Green;
            lblAxisCount.Text = $"轴数: {axes.Length}";
            Log($"连接成功 — {axes.Length} 轴 松下 A6B, 先 [全部使能], 设抓取轴/抓取区, 再 [启动同步]");
        }
        // ==================== 事件 ====================

        void RegisterEvents()
        {
            // ⚠ SDK 事件 slaveIndex 是 1-based; axes[] 0-based, 每个 per-slave 事件须 ax = si - 1 + 边界保护。
            master.Events.StateChanged += (s, ev) =>
            {
                Log($"[状态] 主站: {ev.OldState} -> {ev.NewState}");
                if (IsCleanOP(ev.OldState) && !IsCleanOP(ev.NewState))
                {
                    alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.主站掉OP, AlarmSeverity.Fault, $"{ev.OldState}→{ev.NewState}"));
                    GroupStop(-1, "主站掉出 OP");
                }
            };
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
            master.Events.EmergencyEvent += (mi, si, code, reg, b1, w1, w2) =>
            {
                Log($"[紧急] 轴{si} 错误码: 0x{code:X4} reg=0x{reg:X2}");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                var sev = (reg & 0x30) != 0 ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                alarmMgr.Enqueue(() => alarmMgr.Raise(ax, AlarmType.紧急码, sev, $"紧急码 0x{code:X4} reg=0x{reg:X2}"));
                if (sev == AlarmSeverity.Fault) GroupStop(ax, $"轴{si} 紧急事件 0x{code:X4}");
            };
            master.Events.SlaveOffline += (si) =>
            {
                Log($"[离线] 轴{si} 已断开");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                alarmMgr.Enqueue(() => alarmMgr.Raise(ax, AlarmType.掉站, AlarmSeverity.Fault, "从站离线"));
                GroupStop(ax, $"轴{si} 掉站");
            };
            master.Events.SlaveOnline += (si) =>
            {
                Log($"[上线] 轴{si} 已恢复 (需人工 [全部故障复位]+[全部使能])");
                int ax = si - 1; if (ax < 0 || ax >= axes.Length) return;
                alarmMgr.Enqueue(() => alarmMgr.Clear(ax, AlarmType.掉站));
            };
            master.Events.PDOFrameLoss += (mi, grp, con, tot) =>
            {
                Log($"[丢帧] 组{grp} 连续: {con}, 累计: {tot}");
                var sev = con >= PDO_LOSS_FAULT ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.丢帧, sev, $"连续{con} 累计{tot}"));
                if (sev == AlarmSeverity.Fault) GroupStop(-1, $"PDO 连续丢帧 {con}");
            };
            master.Events.DCSyncLost += (mi, si, diffNs) => Log($"[DC] 轴{si} 同步丢失 偏差 {diffNs}ns");
            master.Events.RedundancyModeChanged += (mi, oldMode, newMode) => Log($"[冗余] 模式 {oldMode} → {newMode}");
            master.Events.SlavePortLinkChanged += (mi, si, port, isUp) => Log($"[链路] 轴{si} 端口{port} {(isUp ? "Up" : "Down")}");
            master.Events.SlaveIdentityMismatch += (s, args) =>
            {
                Log("[身份] 从站身份不符 (接错型号?)");
                alarmMgr.Enqueue(() => alarmMgr.Raise(-1, AlarmType.身份不符, AlarmSeverity.Fault, "从站身份不符(接错型号?)"));
                GroupStop(-1, "从站身份不符");
            };
        }

        void btnAlarmAck_Click(object sender, EventArgs e)
        {
            foreach (var a in axes)
                if (IsFault(a.SnapStatusWord) || a.SnapErrorCode != 0)
                { Log("仍有轴处于驱动故障态, 请先 [全部故障复位] 再 [报警复位]"); return; }
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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnLogUpdated] 异常: {ex.GetType().Name}: {ex.Message}"); }
        }

        // ==================== 断开 ====================

        void btnDisconnect_Click(object sender, EventArgs e) { Disconnect(); }

        void JoinWorkers()
        {
            var t2 = _statusThread;
            _statusThread = null;
            try { t2?.Join(500); } catch { }
        }

        void Disconnect()
        {
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { } catch (Exception ex) { Console.WriteLine($"[Disconnect] CTS Cancel 异常: {ex.Message}"); }

            _syncRunning = false;
            _jogMasterDir = 0;
            foreach (var a in axes) { a.ServoEnabled = false; a.FaultReset = false; }
            isRunning = false;
            try { master?.Events.ProcessDataCyclicSync -= OnPdoCycle; } catch { }   // 退订 PDO 周期回调, 防 master 释放后回调访问 m.Slaves[...] → native UAF
            try { DarraEtherCAT.Logs.Updated -= OnLogUpdated; } catch (Exception ex) { Console.WriteLine($"[Disconnect] 取消日志订阅异常: {ex.Message}"); }

            JoinWorkers();
            try { DarraEtherCAT.Abort(); } catch (Exception ex) { Console.WriteLine($"[Disconnect] Abort 异常: {ex.Message}"); }

            var m = master;
            master = null;
            if (m != null)
                Task.Run(() => { try { m.Close(); } catch (Exception ex) { Console.WriteLine($"[Disconnect] Close异常: {ex.Message}"); } });

            axes = new AxisController[0];
            gridAxes.Rows.Clear();
            gridProducts.Rows.Clear();
            lock (_prodLock) { _products.Clear(); }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            grpMaster.Enabled = false;
            grpGlobal.Enabled = false;
            grpTrack.Enabled = false;
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
            picConveyor.Invalidate();
            Log("已断开连接");
        }

        // ==================== 日志 ====================

        void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            System.Diagnostics.Debug.WriteLine(line);
            if (_logQueue.Count < LOG_QUEUE_CAP) _logQueue.Enqueue(line);
        }

        void LogFlushTimer_Tick(object sender, EventArgs e)
        {
            if (_logQueue.IsEmpty) return;
            var sb = new StringBuilder();
            int count = 0;
            while (_logQueue.TryDequeue(out string line) && count < 50) { sb.AppendLine(line); count++; }
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
            try { master?.Events.ProcessDataCyclicSync -= OnPdoCycle; } catch { }
            JoinWorkers();
            try { DarraEtherCAT.Abort(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] Abort 异常: {ex.Message}"); }
            var m = master;
            master = null;
            if (m != null) { try { m.Close(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnFormClosing] m.Close 异常: {ex.Message}"); } }
            base.OnFormClosing(e);
        }
    }
}
