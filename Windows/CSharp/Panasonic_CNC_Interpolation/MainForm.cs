using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace Panasonic_CNC_Interpolation
{
    // ==================== 松下 MINAS A6B 伺服 PDO (CiA 402, Profile 402) ====================
    // 厂商: Panasonic  型号: MINAS A6B
    // PDO 用 ESI 默认映射 (来源 ESI: panasonic_minas-a6bf_v1_9_1_0_117_0.xml):
    //   RxPDO 0x1600 (输出, SM2) = 9 字节
    //   TxPDO 0x1A00 (输入, SM3) = 23 字节
    // 字段顺序 = PDO 条目顺序, 逐字节一致, 不可调换。
    //
    // 本例 = CNC 多轴轨迹插补: 多个伺服轴 (X/Y/Z) 联动走 G 代码描述的直线 (G01) / 圆弧 (G02/G03) 轨迹。
    // 只用 CSP (Mode=8): 主站每 PDO 周期 (1ms, DC Sync0) 把插补出的目标位置下发各轴, 从站做精插补。

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Output  // 9 字节 RxPDO 0x1600
    {
        public ushort ControlWord;            // 0x6040:0 UINT
        public sbyte ModesOfOperation;        // 0x6060:0 SINT (CSP=8)
        public int TargetPosition;            // 0x607A:0 DINT
        public ushort TouchProbeFunction;     // 0x60B8:0 UINT
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Input   // 23 字节 TxPDO 0x1A00
    {
        public ushort ErrorCode;                 // 0x603F:0 UINT
        public ushort StatusWord;                // 0x6041:0 UINT
        public sbyte ModesOfOperationDisplay;    // 0x6061:0 SINT
        public int PositionActualValue;          // 0x6064:0 DINT
        public ushort TouchProbeStatus;          // 0x60B9:0 UINT
        public int TouchProbe1PosValue;          // 0x60BA:0 DINT
        public int FollowingErrorActualValue;    // 0x60F4:0 DINT
        public uint DigitalInputs;               // 0x60FD:0 UDINT
    }

    // ==================== 报警 ====================
    enum AlarmSeverity { Fault = 0, Warning = 1, Info = 2 }   // 颜色 Firebrick / DarkOrange / DimGray

    enum AlarmType
    {
        跟随误差, 掉OP, 驱动故障, 软限位, AL状态, DC失步,
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

    // 轴在 CNC 坐标系中的角色 (G 代码里的 X/Y/Z 平面坐标映射到哪个物理从站)
    enum AxisRole { 禁用 = 0, X = 1, Y = 2, Z = 3 }

    /// <summary>
    /// 单个轴的控制状态 (UI 线程写控制变量, PDO 线程写快照, 互不阻塞)。
    /// CNC 插补: Role 决定本轴对应 G 代码哪个坐标; Origin 为程序坐标原点(脉冲)。
    /// </summary>
    sealed class AxisController
    {
        public readonly int SlaveIndex;
        public readonly int PhysAddr;
        public AxisController(int idx, int physAddr) { SlaveIndex = idx; PhysAddr = physAddr; }

        // 角色 (UI 写, PDO 读)
        public volatile AxisRole Role = AxisRole.禁用;

        // 控制 (UI 写, PDO 读)
        public volatile bool ServoEnabled;
        public volatile bool FaultReset;

        // 程序坐标原点 (设原点时 PDO 线程写 = 当前实际位置; 插补时 PDO 读)
        public int Origin;

        // 快照 (PDO 写, UI 读)
        public volatile ushort SnapStatusWord;
        public volatile int SnapActualPosition;
        public volatile int SnapTargetPosition;
        public volatile int SnapFollowError;       // 0x60F4 跟随误差
        public volatile ushort SnapErrorCode;
        public volatile string SnapDriveState = "---";

        // CSP 内部状态 (仅 PDO 线程访问)
        public int CurrentTarget;

        // —— 跟随误差监控 / grace (PDO 线程拥有写) ——
        public long SyncStartCycle;             // 本轴 grace 起算周期; 0=未就绪
        public volatile bool GraceResetPending; // 运行/设原点后 → 下周期重置 grace
        public bool WasEligible;                // 曾真正就绪(掉OP 边沿判定用; 仅 PDO 线程)
        public int FollowErrConsec;             // 跟随误差连续超限计数 (仅 PDO 线程)
        public int FollowErrLimit = 50000;      // 每轴跟随误差阈值(脉冲); 松下 23 位编码器一圈 = 8388608

        // —— 每轴限制 (UI 写, PDO 读; double 低频整量写, 同 Origin 处理) ——
        public double MinMm = 0, MaxMm = 0;     // 软限位 (程序坐标 mm); MinMm==MaxMm → 该轴不限位
        public double VmaxMmS = 0;              // 每轴最大速度 (mm/s); 0 → 不限速

        // —— 异常闩 (PDO 线程置, 50ms 消费侧据边沿 Raise/Clear) ——
        public volatile bool AlarmFollowError;
        public volatile bool AlarmDropEnable;
        public volatile bool AlarmFault;
        public volatile bool AlarmSoftLimit;    // 触及软限位
    }

    // ==================== CNC 运动段 ====================
    enum SegType { Line, ArcCW, ArcCCW }   // 直线 / 顺时针圆弧 / 逆时针圆弧

    /// <summary>
    /// 一段刀具轨迹 (规划阶段 UI 线程算好, 放 volatile 数组供 PDO 线程只读)。
    /// 坐标单位 mm。圆弧只在 XY 平面 (Z 沿弧长线性插值, 实现简单螺旋/平面圆弧)。
    /// </summary>
    sealed class MotionSegment
    {
        public SegType Type;
        // 起终点 (mm)
        public double X0, Y0, Z0, X1, Y1, Z1;
        // 圆弧专用: 圆心(mm) / 半径(mm) / 起止角(rad)
        public double Cx, Cy, Radius, StartAng, EndAng;
        // 段长 (沿轨迹弧长, mm)
        public double Length;
        // 速度规划 (mm/s)
        public double VEntry, VExit, VCruise;

        // 按段内已走弧长 s∈[0,Length] 求当前点 (x,y,z) mm
        public void PointAt(double s, out double x, out double y, out double z)
        {
            if (Length < 1e-9)
            {
                x = X1; y = Y1; z = Z1; return;
            }
            double t = s / Length;             // 0..1
            if (t < 0) t = 0; else if (t > 1) t = 1;
            if (Type == SegType.Line)
            {
                x = X0 + (X1 - X0) * t;
                y = Y0 + (Y1 - Y0) * t;
                z = Z0 + (Z1 - Z0) * t;
            }
            else
            {
                double ang = StartAng + (EndAng - StartAng) * t;
                x = Cx + Radius * Math.Cos(ang);
                y = Cy + Radius * Math.Sin(ang);
                z = Z0 + (Z1 - Z0) * t;        // Z 沿弧长线性 (平面圆弧时 Z0==Z1)
            }
        }

        // 段在某弧长处的单位切向 (用于拐角衔接速度算转角)
        public void TangentAt(double s, out double tx, out double ty)
        {
            if (Type == SegType.Line)
            {
                double dx = X1 - X0, dy = Y1 - Y0;
                double n = Math.Sqrt(dx * dx + dy * dy);
                if (n < 1e-9) { tx = 1; ty = 0; return; }
                tx = dx / n; ty = dy / n;
            }
            else
            {
                double t = Length < 1e-9 ? 0 : s / Length;
                double ang = StartAng + (EndAng - StartAng) * t;
                // 切向 = d/dang (cos,sin) * 方向号
                double sign = (EndAng >= StartAng) ? 1.0 : -1.0;   // 逆时针 ang 增 / 顺时针 ang 减
                tx = -Math.Sin(ang) * sign;
                ty = Math.Cos(ang) * sign;
                double n = Math.Sqrt(tx * tx + ty * ty);
                if (n < 1e-9) { tx = 1; ty = 0; return; }
                tx /= n; ty /= n;
            }
        }
    }

    /// <summary>
    /// 松下 A6B CNC 多轴轨迹插补主窗体。
    /// 永远 CSP (Mode=8)。规划好速度剖面后, OnPdoCycle 内按 a/jerk 推进弧长, 映射到几何得各轴目标位置。
    /// </summary>
    public partial class MainForm : Form
    {
        DarraEtherCAT master;
        volatile bool isRunning = false;
        private CancellationTokenSource _connectCts;
        Thread _statusThread;                   // UI 刷新线程引用: 断开/关闭前先 Join 再 Close, 防 native UAF

        volatile AxisController[] axes = new AxisController[0];   // 引用字段 volatile: 后台线程读引用快照的可见性保证

        // 松下 A6 23 位编码器: 一圈 = 8388608 脉冲
        const int PULSES_PER_REV = 8388608;

        // ==================== CNC 插补运行态 ====================
        // 规划阶段 (UI 线程) 一次性算好 _segs 放 volatile 数组, PDO 线程只读。
        volatile MotionSegment[] _segs = new MotionSegment[0];
        volatile bool _trajRunning = false;     // 是否正在沿轨迹推进
        volatile bool _trajPaused = false;      // 暂停 (推进冻结, 位置保持)
        volatile bool _trajResetReq = false;    // 请求复位轨迹执行态到首段起点

        // PDO 线程拥有的执行游标 (仅 OnPdoCycle 读写)
        int _segIdx = 0;                        // 当前段索引
        double _sLocal = 0;                     // 当前段内已走弧长 (mm)
        double _v = 0;                          // 当前合成进给速度 (mm/s)
        double _a = 0;                          // S 曲线模式当前加速度 (mm/s²)

        // 插补参数快照 (UI 写, PDO 读; 运行前一次性拷入, 运行中不改)。
        // double 不能加 volatile (CS0677); 这些都在 _trajRunning(volatile)=true 之前一次性写入,
        // 那次 volatile 写形成内存屏障, 保证 PDO 线程在见到 _trajRunning=true 时也看到这些值。
        double _planAccel = 500;                // 最大加速度 mm/s²
        double _planJerk = 5000;                // 最大加加速度 mm/s³
        volatile int _planPulsesPerMm = 10000;  // 脉冲/mm
        volatile bool _planSCurve = false;      // true=S曲线, false=梯形

        // UI 显示用快照 (PDO 写, UI 读; double 不能 volatile, 仅供显示, x64 对齐读写足够)
        volatile int _uiSegIdx = 0;
        volatile int _uiSegTotal = 0;
        double _uiProgress = 0;                 // 0..1 全路径完成比
        double _uiFeedMmMin = 0;                // 当前合成进给 mm/min
        // 实时刀位点 (PDO 写 mm, UI 读, 画红点)
        volatile float _uiToolX = 0;
        volatile float _uiToolY = 0;

        const double DT = 0.001;                // PDO 周期 1ms (秒)

        // 轴总览表列索引
        const int COL_AXIS = 0, COL_ADDR = 1, COL_ROLE = 2, COL_STATE = 3, COL_SW = 4,
                  COL_ACTPOS = 5, COL_TGTPOS = 6, COL_FE = 7, COL_ENABLED = 8,
                  COL_MIN = 9, COL_MAX = 10, COL_VMAX = 11;   // 软限位下/上限(mm) + 每轴最大速度(mm/s), 可编辑

        // ==================== 报警字段 ====================
        readonly AlarmManager alarmMgr = new AlarmManager();
        long _pdoCycle = 0;                     // PDO 单调周期计数 (grace 计时基准, net472 不用 TickCount64)
        volatile bool _groupStopLatched = false; // 组停闭锁: 拦截 运行/使能, 需报警复位解锁

        // 报警表列索引
        const int ACOL_TIME = 0, ACOL_AXIS = 1, ACOL_SEV = 2, ACOL_TYPE = 3, ACOL_MSG = 4, ACOL_STATE = 5;

        // 报警阈值 / 防误报参数
        const int FE_CONSEC_N = 50;     // 跟随误差去抖周期数 (≈50ms@1ms)
        const int GRACE_CYCLES = 200;   // 运行/设原点/刚使能后宽限 (≈200ms@1ms)
        const int PDO_LOSS_FAULT = 5;   // 连续丢帧故障阈值
        int _wkcMissConsec = 0;         // WKC 连续失配计数 (仅 50ms 线程)

        // 异步日志
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int LOG_MAX_TEXT = 50000;
        private const int LOG_QUEUE_CAP = 500;
        private int _processedLogCount;
        private readonly object _logProcLock = new object();   // OnLogUpdated 可能并发触发 → 串行化 _processedLogCount

        public MainForm()
        {
            InitializeComponent();
            InitGrid();
            InitAlarmGrid();
            BindEvents();
            txtGcode.Text = BuildRectGcode();      // 默认填一段矩形 G 代码
            pnlPreview.Paint += PnlPreview_Paint;
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

            // G 代码预设
            btnPresetRect.Click += (s, e) => { txtGcode.Text = BuildRectGcode(); Log("已填入矩形 G 代码"); RefreshPreviewFromText(); };
            btnPresetCircle.Click += (s, e) => { txtGcode.Text = BuildCircleGcode(); Log("已填入圆 G 代码"); RefreshPreviewFromText(); };
            btnPresetRoundRect.Click += (s, e) => { txtGcode.Text = BuildRoundRectGcode(); Log("已填入圆角矩形 G 代码"); RefreshPreviewFromText(); };
            btnPresetStar.Click += (s, e) => { txtGcode.Text = BuildStarGcode(); Log("已填入五角星 G 代码"); RefreshPreviewFromText(); };
            txtGcode.TextChanged += (s, e) => RefreshPreviewFromText();

            // 控制
            btnEnable.Click += (s, e) =>
            {
                if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
                foreach (var a in axes) if (a.Role != AxisRole.禁用) a.ServoEnabled = true;
                Log("使能 (所有已分配 X/Y/Z 轴)");
            };
            btnDisable.Click += (s, e) =>            // 急停永远可用
            {
                _trajRunning = false; _trajPaused = false;
                foreach (var a in axes) a.ServoEnabled = false;
                Log("去使能 (急停): 停止轨迹 + 全轴松开");
            };
            btnFaultReset.Click += (s, e) => { foreach (var a in axes) a.FaultReset = true; Log("故障复位 (所有轴)"); };
            btnSetOrigin.Click += btnSetOrigin_Click;
            btnRun.Click += btnRun_Click;
            btnPause.Click += btnPause_Click;
            btnStop.Click += btnStop_Click;

            // 报警复位
            btnAlarmAck.Click += btnAlarmAck_Click;
        }

        // ==================== 轴总览表 + X/Y/Z 角色映射 ====================

        void InitGrid()
        {
            gridAxes.AutoGenerateColumns = false;
            gridAxes.Columns.Clear();

            AddTextCol("轴", 40, true);                                                   // 0
            AddTextCol("地址", 64, true);                                                 // 1
            var role = new DataGridViewComboBoxColumn
            {
                HeaderText = "角色",
                Width = 64,
                Name = "colRole",
                FlatStyle = FlatStyle.Flat,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            role.Items.AddRange("禁用", "X", "Y", "Z");
            gridAxes.Columns.Add(role);                                                   // 2  可编辑下拉
            AddTextCol("驱动状态", 130, true);                                            // 3
            AddTextCol("状态字", 64, true);                                               // 4
            AddTextCol("实际位置(mm/脉冲)", 150, true);                                   // 5
            AddTextCol("目标位置(mm)", 100, true);                                        // 6
            AddTextCol("跟随误差", 80, true);                                             // 7
            AddTextCol("使能", 48, true);                                                 // 8
            AddTextCol("行程下限(mm)", 92, false);                                        // 9  可编辑 (软限位; 下=上=0 则该轴不限)
            AddTextCol("行程上限(mm)", 92, false);                                        // 10 可编辑
            AddTextCol("最大速度(mm/s)", 104, false);                                     // 11 可编辑 (0=不限)
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
                row.Cells[COL_AXIS].Value = (a.SlaveIndex + 1).ToString();
                row.Cells[COL_ADDR].Value = $"0x{a.PhysAddr:X4}";
                row.Cells[COL_ROLE].Value = a.Role.ToString();
                row.Cells[COL_STATE].Value = "---";
                row.Cells[COL_MIN].Value = a.MinMm.ToString("F1", CultureInfo.InvariantCulture);
                row.Cells[COL_MAX].Value = a.MaxMm.ToString("F1", CultureInfo.InvariantCulture);
                row.Cells[COL_VMAX].Value = a.VmaxMmS.ToString("F0", CultureInfo.InvariantCulture);
            }
        }

        // 解析「角色」下拉 → AxisController.Role; 同一角色只能给一个轴 (重复则把旧轴置禁用)
        void GridAxes_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= axes.Length) return;
            var row = gridAxes.Rows[e.RowIndex];
            var a = axes[e.RowIndex];

            // 软限位 / 最大速度 列 (可编辑数值)
            if (e.ColumnIndex == COL_MIN || e.ColumnIndex == COL_MAX || e.ColumnIndex == COL_VMAX)
            {
                string s = Convert.ToString(row.Cells[e.ColumnIndex].Value);
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                    && !double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) v = double.NaN;
                if (e.ColumnIndex == COL_MIN)
                {
                    if (!double.IsNaN(v)) a.MinMm = v; else Log($"轴{a.SlaveIndex + 1} 行程下限无效, 保持 {a.MinMm:F1}");
                    row.Cells[COL_MIN].Value = a.MinMm.ToString("F1", CultureInfo.InvariantCulture);
                }
                else if (e.ColumnIndex == COL_MAX)
                {
                    if (!double.IsNaN(v)) a.MaxMm = v; else Log($"轴{a.SlaveIndex + 1} 行程上限无效, 保持 {a.MaxMm:F1}");
                    row.Cells[COL_MAX].Value = a.MaxMm.ToString("F1", CultureInfo.InvariantCulture);
                }
                else
                {
                    if (!double.IsNaN(v) && v >= 0) a.VmaxMmS = v; else Log($"轴{a.SlaveIndex + 1} 最大速度无效, 保持 {a.VmaxMmS:F0}");
                    row.Cells[COL_VMAX].Value = a.VmaxMmS.ToString("F0", CultureInfo.InvariantCulture);
                }
                if (a.MaxMm < a.MinMm && !(a.MaxMm == 0 && a.MinMm == 0))
                    Log($"轴{a.SlaveIndex + 1} 软限位上限 < 下限, 该轴将被视为不限位");
                return;
            }

            if (e.ColumnIndex != COL_ROLE) return;
            string text = Convert.ToString(row.Cells[COL_ROLE].Value);
            AxisRole newRole = AxisRole.禁用;
            if (text == "X") newRole = AxisRole.X;
            else if (text == "Y") newRole = AxisRole.Y;
            else if (text == "Z") newRole = AxisRole.Z;

            // 角色唯一: 若该角色已被别的轴占用, 把那个轴改回禁用
            if (newRole != AxisRole.禁用)
            {
                for (int i = 0; i < axes.Length; i++)
                {
                    if (i == e.RowIndex) continue;
                    if (axes[i].Role == newRole)
                    {
                        axes[i].Role = AxisRole.禁用;
                        if (i < gridAxes.Rows.Count) gridAxes.Rows[i].Cells[COL_ROLE].Value = "禁用";
                        Log($"轴{i + 1} 原 {newRole} 角色被轴{e.RowIndex + 1} 抢占, 置禁用");
                    }
                }
            }
            a.Role = newRole;
            a.GraceResetPending = true;
            Log($"轴{a.SlaveIndex + 1} 角色 = {newRole}");
        }

        // 默认角色: slave0=X slave1=Y slave2=Z, 其余禁用
        void AssignDefaultRoles()
        {
            for (int i = 0; i < axes.Length; i++)
            {
                if (i == 0) axes[i].Role = AxisRole.X;
                else if (i == 1) axes[i].Role = AxisRole.Y;
                else if (i == 2) axes[i].Role = AxisRole.Z;
                else axes[i].Role = AxisRole.禁用;
            }
        }

        AxisController FindAxis(AxisRole role)
        {
            var arr = axes;
            foreach (var a in arr) if (a.Role == role) return a;
            return null;
        }

        // ==================== G 代码预设生成 ====================

        static string BuildRectGcode()
        {
            return "G00 X0 Y0\r\n" +
                   "G01 X60 Y0 F3000\r\n" +
                   "G01 X60 Y40\r\n" +
                   "G01 X0 Y40\r\n" +
                   "G01 X0 Y0\r\n";
        }

        // 圆: 圆心 (30,0) 相对起点 (0,0) → I=30 J=0; 逆时针整圆回到起点
        static string BuildCircleGcode()
        {
            return "G00 X0 Y0\r\n" +
                   "G03 X0 Y0 I30 J0 F3000\r\n";
        }

        static string BuildRoundRectGcode()
        {
            // 60x40 矩形, 四角 R10 圆角 (G02 顺时针外圆角); 从下边中段起逆时针走外轮廓
            var sb = new StringBuilder();
            sb.Append("G00 X10 Y0\r\n");
            sb.Append("G01 X50 Y0 F3000\r\n");
            sb.Append("G03 X60 Y10 I0 J10\r\n");      // 右下角 R10
            sb.Append("G01 X60 Y30\r\n");
            sb.Append("G03 X50 Y40 I-10 J0\r\n");     // 右上角 R10
            sb.Append("G01 X10 Y40\r\n");
            sb.Append("G03 X0 Y30 I0 J-10\r\n");      // 左上角 R10
            sb.Append("G01 X0 Y10\r\n");
            sb.Append("G03 X10 Y0 I10 J0\r\n");       // 左下角 R10
            return sb.ToString();
        }

        // 五角星: 5 个外顶点, 用直线连成五角星 (R=30, 中心(0,0))
        static string BuildStarGcode()
        {
            double R = 30;
            // 五角星按顶点跳序 0-2-4-1-3-0 连线
            int[] order = { 0, 2, 4, 1, 3, 0 };
            var pts = new (double x, double y)[5];
            for (int k = 0; k < 5; k++)
            {
                double ang = Math.PI / 2 + k * 2 * Math.PI / 5;   // 顶点朝上起
                pts[k] = (R * Math.Cos(ang), R * Math.Sin(ang));
            }
            var sb = new StringBuilder();
            sb.Append($"G00 X{pts[order[0]].x:F3} Y{pts[order[0]].y:F3}\r\n");
            for (int k = 1; k < order.Length; k++)
            {
                string f = k == 1 ? " F3000" : "";
                sb.Append($"G01 X{pts[order[k]].x:F3} Y{pts[order[k]].y:F3}{f}\r\n");
            }
            return sb.ToString();
        }

        // ==================== G 代码解析 (文本 → MotionSegment 几何, 未含速度规划) ====================
        // 支持: G00/G01 X Y Z F (直线快移/直线插补); G02 (CW) / G03 (CCW) X Y I J F (XY 平面圆弧)。
        // 坐标缺省 = 保持上一点对应分量 (模态)。F 单位 mm/min, 缺省沿用上一个 F。
        // 返回 null 表示有语法错误 (errMsg 给出原因)。

        List<MotionSegment> ParseGcode(string text, out double feedMmMin, out string errMsg)
        {
            errMsg = null;
            feedMmMin = (double)numFeed.Value;
            var segs = new List<MotionSegment>();
            double curX = 0, curY = 0, curZ = 0;          // 当前刀位 (mm)
            double curF = feedMmMin;
            bool firstMove = true;
            int modalG = -1;     // 模态运动 G (0/1/2/3); 省略 G 的续行沿用它 (标准 G 代码习惯)
            int lineNo = 0;

            var lines = text.Replace("\r", "").Split('\n');
            foreach (var raw in lines)
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0) continue;
                int semi = line.IndexOf(';'); if (semi >= 0) line = line.Substring(0, semi).Trim();   // ; 注释
                if (line.Length == 0) continue;

                var toks = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int gCode = -1;
                double? nx = null, ny = null, nz = null, ni = null, nj = null, nf = null;

                foreach (var tk in toks)
                {
                    if (tk.Length < 1) continue;
                    char c = char.ToUpperInvariant(tk[0]);
                    string num = tk.Substring(1);
                    if (c == 'G')
                    {
                        if (!int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out gCode))
                        { errMsg = $"第{lineNo}行: G 代码无效 '{tk}'"; return null; }
                    }
                    else if (c == 'X' || c == 'Y' || c == 'Z' || c == 'I' || c == 'J' || c == 'F')
                    {
                        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                        { errMsg = $"第{lineNo}行: 数值无效 '{tk}'"; return null; }
                        switch (c)
                        {
                            case 'X': nx = v; break;
                            case 'Y': ny = v; break;
                            case 'Z': nz = v; break;
                            case 'I': ni = v; break;
                            case 'J': nj = v; break;
                            case 'F': nf = v; break;
                        }
                    }
                    // 其它字母 (M/N 等) 本案例忽略
                }

                if (nf.HasValue) curF = nf.Value;
                bool hasCoord = nx.HasValue || ny.HasValue || nz.HasValue || ni.HasValue || nj.HasValue;
                if (gCode < 0)
                {
                    if (hasCoord && modalG >= 0) gCode = modalG;   // 模态续行: 省略 G 沿用上一运动 G (标准 G 代码)
                    else continue;                                  // 纯 F 行 / 无运动 → 跳过
                }
                if (gCode == 0 || gCode == 1 || gCode == 2 || gCode == 3) modalG = gCode;

                double tx = nx ?? curX, ty = ny ?? curY, tz = nz ?? curZ;

                if (gCode == 0 || gCode == 1)
                {
                    // G00 快移 / G01 直线插补 — 几何相同, 都作直线段 (本案例 G00 也按插补速度走, 安全)
                    var seg = new MotionSegment
                    {
                        Type = SegType.Line,
                        X0 = curX, Y0 = curY, Z0 = curZ, X1 = tx, Y1 = ty, Z1 = tz
                    };
                    double dx = tx - curX, dy = ty - curY, dz = tz - curZ;
                    seg.Length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (seg.Length > 1e-6) { segs.Add(seg); firstMove = false; }
                    curX = tx; curY = ty; curZ = tz;
                }
                else if (gCode == 2 || gCode == 3)
                {
                    // G02 CW / G03 CCW 圆弧 (XY 平面, I/J 为圆心相对起点的增量)
                    if (!ni.HasValue || !nj.HasValue)
                    { errMsg = $"第{lineNo}行: G{gCode:00} 圆弧缺少 I/J 圆心增量"; return null; }
                    double cx = curX + ni.Value, cy = curY + nj.Value;
                    double r0 = Math.Sqrt((curX - cx) * (curX - cx) + (curY - cy) * (curY - cy));
                    double r1 = Math.Sqrt((tx - cx) * (tx - cx) + (ty - cy) * (ty - cy));
                    if (r0 < 1e-6) { errMsg = $"第{lineNo}行: 圆弧半径为 0"; return null; }
                    if (Math.Abs(r0 - r1) > 0.05 * Math.Max(1.0, r0))
                    { errMsg = $"第{lineNo}行: 圆弧起终点到圆心半径不一致 ({r0:F3} vs {r1:F3})"; return null; }

                    double a0 = Math.Atan2(curY - cy, curX - cx);
                    double a1 = Math.Atan2(ty - cy, tx - cx);
                    bool ccw = (gCode == 3);

                    // 整理扫掠角: CCW → a1>a0; CW → a1<a0; 起终点重合 = 整圆
                    if (ccw)
                    {
                        while (a1 <= a0 + 1e-9) a1 += 2 * Math.PI;
                    }
                    else
                    {
                        while (a1 >= a0 - 1e-9) a1 -= 2 * Math.PI;
                    }

                    var seg = new MotionSegment
                    {
                        Type = ccw ? SegType.ArcCCW : SegType.ArcCW,
                        X0 = curX, Y0 = curY, Z0 = curZ, X1 = tx, Y1 = ty, Z1 = tz,
                        Cx = cx, Cy = cy, Radius = r0, StartAng = a0, EndAng = a1
                    };
                    seg.Length = r0 * Math.Abs(a1 - a0);
                    if (seg.Length > 1e-6) { segs.Add(seg); firstMove = false; }
                    curX = tx; curY = ty; curZ = tz;
                }
                else
                {
                    errMsg = $"第{lineNo}行: 不支持的 G 代码 G{gCode:00} (仅支持 G00/G01/G02/G03)";
                    return null;
                }
            }

            if (segs.Count == 0) { errMsg = firstMove ? "G 代码为空或无有效运动段" : "无有效运动段"; return null; }
            return segs;
        }

        // ==================== 前瞻 Look-Ahead 速度规划 ====================
        // 输入: 几何段列表 + 进给 F(mm/s) + 最大加速度 a(mm/s²) + 拐角偏差 δ(mm)。
        // 输出: 每段 VEntry / VExit / VCruise (mm/s) 满足:
        //   ① 入口/出口速度 ≤ 由相邻段转角算出的衔接速度 (GRBL junction deviation);
        //   ② v_exit² ≤ v_entry² + 2·a·len (单段加速能力); 反向也成立 (减速);
        //   ③ 首段入口 = 0, 末段出口 = 0。
        // 用 backward + forward 两遍扫描收敛。

        void PlanLookAhead(List<MotionSegment> segs, double feedMmS, double accel, double junctionDev, double vmaxX, double vmaxY, double vmaxZ)
        {
            int n = segs.Count;
            if (n == 0) return;

            // 巡航速度 = min(进给, 每轴最大速度折算到本段方向); 各段独立, 保证任何方向单轴速度都不超限
            foreach (var s in segs) s.VCruise = Math.Min(feedMmS, SegSpeedCap(s, vmaxX, vmaxY, vmaxZ, feedMmS));

            // —— 各相邻段公共顶点的衔接速度 vJunction[i] (= 段 i 的 VExit 上限 = 段 i+1 的 VEntry 上限) ——
            var vJ = new double[n + 1];
            vJ[0] = 0;          // 首段入口
            vJ[n] = 0;          // 末段出口
            for (int i = 0; i < n - 1; i++)
            {
                // 段 i 末切向 与 段 i+1 首切向 的夹角
                segs[i].TangentAt(segs[i].Length, out double t0x, out double t0y);
                segs[i + 1].TangentAt(0, out double t1x, out double t1y);
                double dot = t0x * t1x + t0y * t1y;
                if (dot > 1) dot = 1; else if (dot < -1) dot = -1;
                double theta = Math.Acos(dot);          // 0=共线, π=掉头
                // GRBL junction deviation: sinHalf = sin((π-θ)/2) = cos(θ/2)? 这里用偏转角 θ:
                //   实际转角偏转 = θ (切向夹角); v_j = sqrt(a·δ·sin(θ/2)/(1-sin(θ/2)))。
                double sinHalf = Math.Sin(theta / 2.0);
                double vj;
                if (sinHalf <= 1e-6) vj = feedMmS;                  // 几乎共线 → 不减速
                else if (sinHalf >= 1 - 1e-6) vj = 0;               // 掉头 → 停
                else vj = Math.Sqrt(accel * junctionDev * sinHalf / (1.0 - sinHalf));
                double capj = Math.Min(segs[i].VCruise, segs[i + 1].VCruise);   // 衔接速度不超相邻段巡航上限 (已含每轴限速)
                if (vj > capj) vj = capj;
                vJ[i + 1] = vj;
            }

            // —— 反向扫描 (从末段往前): 保证每段能从 VEntry 减速到 VExit 限制 ——
            // VExit[i] = vJ[i+1] (出口衔接限); VEntry[i] 受 sqrt(VExit² + 2·a·len) 限。
            for (int i = n - 1; i >= 0; i--)
            {
                double vExit = vJ[i + 1];
                double vEntryMax = Math.Sqrt(vExit * vExit + 2.0 * accel * segs[i].Length);
                double vEntry = Math.Min(vJ[i], vEntryMax);
                vJ[i] = Math.Min(vJ[i], vEntry);    // 收紧入口衔接速度
            }

            // —— 正向扫描 (从首段往后): 保证每段能从 VEntry 加速到 VExit ——
            for (int i = 0; i < n; i++)
            {
                double vEntry = vJ[i];
                double vExitMax = Math.Sqrt(vEntry * vEntry + 2.0 * accel * segs[i].Length);
                double vExit = Math.Min(vJ[i + 1], vExitMax);
                vJ[i + 1] = Math.Min(vJ[i + 1], vExit);
            }

            // —— 落到各段 ——
            for (int i = 0; i < n; i++)
            {
                segs[i].VEntry = vJ[i];
                segs[i].VExit = vJ[i + 1];
                // VCruise 已在上面按 进给 + 每轴限速 设好, 此处不覆盖 (覆盖会丢失限速)
            }
        }

        // 本段允许的最大合成进给 (mm/s): 把每轴最大速度折算到本段方向 —— 任何一轴的速度分量都不得超 vmax_i。
        // 直线: 轴 i 速度 = 路径速度 × |方向分量_i| → 路径速度 ≤ vmax_i / |方向分量_i|。
        // 圆弧: XY 切向分量沿弧可达 ±1 倍路径速度 → 直接以 vmaxX/Y 为上限 (保守安全)。
        static double SegSpeedCap(MotionSegment s, double vmaxX, double vmaxY, double vmaxZ, double feed)
        {
            double cap = feed;
            if (s.Type == SegType.Line)
            {
                double dx = Math.Abs(s.X1 - s.X0), dy = Math.Abs(s.Y1 - s.Y0), dz = Math.Abs(s.Z1 - s.Z0);
                double L = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (L < 1e-9) return cap;
                if (vmaxX > 0 && dx > 1e-9) cap = Math.Min(cap, vmaxX * L / dx);
                if (vmaxY > 0 && dy > 1e-9) cap = Math.Min(cap, vmaxY * L / dy);
                if (vmaxZ > 0 && dz > 1e-9) cap = Math.Min(cap, vmaxZ * L / dz);
            }
            else
            {
                if (vmaxX > 0) cap = Math.Min(cap, vmaxX);
                if (vmaxY > 0) cap = Math.Min(cap, vmaxY);
                double dz = Math.Abs(s.Z1 - s.Z0);
                if (vmaxZ > 0 && dz > 1e-9 && s.Length > 1e-9) cap = Math.Min(cap, vmaxZ * s.Length / dz);
            }
            return cap < 1e-6 ? feed : cap;   // 全不限 → 退回进给
        }

        // 运行前软限位检查: 扫描整条路径 (直线取端点, 圆弧采样 25 点), 任一坐标超出该轴 [下限,上限] 即返回错误说明。
        string CheckSoftLimits(List<MotionSegment> segs, AxisController ax, AxisController ay, AxisController az)
        {
            foreach (var s in segs)
            {
                // 圆弧按 ~2° 一点密采样, 避免两点间漏掉各轴几何极值 (整圆约 180 点); 直线取两端点。
                int N;
                if (s.Type == SegType.Line) N = 2;
                else { double sweep = Math.Abs(s.EndAng - s.StartAng); N = Math.Max(25, (int)Math.Ceiling(sweep / (Math.PI / 90.0))); if (N > 1440) N = 1440; }
                for (int k = 0; k < N; k++)
                {
                    double t = (N <= 1) ? 0 : (double)k / (N - 1);
                    s.PointAt(t * s.Length, out double px, out double py, out double pz);
                    string e = ChkAxisLimit(ax, "X", px) ?? ChkAxisLimit(ay, "Y", py) ?? ChkAxisLimit(az, "Z", pz);
                    if (e != null) return e;
                }
            }
            return null;
        }

        static string ChkAxisLimit(AxisController a, string name, double mm)
        {
            if (a == null || a.MaxMm <= a.MinMm) return null;   // 未设限位 (上限≤下限) → 不检查
            if (mm < a.MinMm - 1e-6 || mm > a.MaxMm + 1e-6)
                return $"{name} 轴坐标 {mm:F2} mm 超出软限位 [{a.MinMm:F1}, {a.MaxMm:F1}] mm";
            return null;
        }

        // ==================== 设原点 / 运行 / 暂停 / 停止 ====================

        // 设原点: 把当前各轴实际位置记为程序坐标原点 (PDO 线程下周期采)
        void btnSetOrigin_Click(object sender, EventArgs e)
        {
            var arr = axes;
            foreach (var a in arr) a.GraceResetPending = true;
            _trajResetReq = true;       // 顺带把轨迹执行态复位到首段起点
            _segReorigin = true;        // PDO 线程下周期把 Origin = 当前实际位置
            Log("设原点: 当前各轴实际位置 = 程序坐标原点");
        }
        volatile bool _segReorigin = false;

        void btnRun_Click(object sender, EventArgs e)
        {
            if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
            if (master == null) { Log("未连接"); return; }

            // 运行前必须先使能 (所有已分配角色的轴都要 OperationEnabled)
            var x = FindAxis(AxisRole.X); var y = FindAxis(AxisRole.Y);
            if (x == null || y == null) { Log("至少需要分配 X、Y 两个轴才能插补"); return; }
            foreach (var a in axes)
                if (a.Role != AxisRole.禁用 && !(a.ServoEnabled && IsOperationEnabled(a.SnapStatusWord)))
                { Log($"轴{a.SlaveIndex + 1}({a.Role}) 未使能, 请先 [使能] 等所有轴进入 OperationEnabled 再 [运行]"); return; }

            // 解析 + 规划 (UI 线程一次性算好)
            var segs = ParseGcode(txtGcode.Text, out double feedMmMin, out string err);
            if (segs == null) { Log("G 代码错误: " + err); MessageBox.Show("G 代码错误:\n" + err, "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // 运行前软限位检查: 扫描整条路径, 任一坐标超出该轴 [下限,上限] → 拒绝运行 (上电安全 + 防撞机)
            var z = FindAxis(AxisRole.Z);
            string limErr = CheckSoftLimits(segs, x, y, z);
            if (limErr != null) { Log("软限位: " + limErr); MessageBox.Show("超出软限位, 已拒绝运行:\n" + limErr, "软限位", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // 拷参数快照
            _planAccel = (double)numAccel.Value;
            _planJerk = (double)numJerk.Value;
            _planPulsesPerMm = (int)numPulsesPerMm.Value;
            _planSCurve = rbSCurve.Checked;
            double accel = _planAccel;
            double feedMmS = feedMmMin / 60.0;       // mm/min → mm/s
            double junctionDev = (double)numJunctionDev.Value;

            // 每轴最大速度 (0=不限), 折入前瞻规划的巡航上限 → 任何方向单轴速度都不超限
            double vmaxX = x.VmaxMmS, vmaxY = y.VmaxMmS, vmaxZ = z?.VmaxMmS ?? 0;
            PlanLookAhead(segs, feedMmS, accel, junctionDev, vmaxX, vmaxY, vmaxZ);

            double totalLen = 0; foreach (var s in segs) totalLen += s.Length;
            _segs = segs.ToArray();
            _uiSegTotal = _segs.Length;

            // 复位执行游标 + 启动
            _trajResetReq = true;
            _trajPaused = false;
            _trajRunning = true;
            Log($"运行: {_segs.Length} 段, 总长 {totalLen:F2} mm, 进给 {feedMmMin:F0} mm/min, " +
                $"加速度 {accel:F0} mm/s², {(_planSCurve ? "S曲线" : "梯形")}, 脉冲/mm {_planPulsesPerMm}");
            RefreshPreviewFromText();
        }

        void btnPause_Click(object sender, EventArgs e)
        {
            if (!_trajRunning) { Log("未在运行"); return; }
            _trajPaused = !_trajPaused;
            Log(_trajPaused ? "暂停 (位置保持)" : "继续");
        }

        void btnStop_Click(object sender, EventArgs e)
        {
            _trajRunning = false;
            _trajPaused = false;
            Log("停止轨迹 (各轴保持当前位置, 仍使能)");
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
                MessageBox.Show($"配置文件不存在: {deniPath}\n请用主站 GUI 扫描松下 A6B 从站, 导出 config.deni 放到该目录\n(详见 README.md)", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;

            _connectCts = new CancellationTokenSource();
            var ct = _connectCts.Token;

            Log("正在初始化主站 (CSP 多轴插补模式)...");

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

                    // 逐轴: PreOp 内经 CoE 把 PDO 强制重映成松下单 PDO (RxPDO 0x1600 / TxPDO 0x1A00) + ConfigureDC
                    for (int i = 0; i < slaveCount; i++)
                    {
                        var slave = newMaster.Slaves[i];

                        // ① PDO 重映成单 PDO (松下 A6B 默认 0x1600/0x1A00), 一次性初始化 (SM 此时未激活, 不进控制循环)。
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
                                Log($"轴{i + 1} PDO 重映 0x1C12=0x1600 / 0x1C13=0x1A00 (9/23) 完成");
                            }
                            catch (Exception ex) { Log($"轴{i + 1} PDO 重映失败(将保持驱动默认): {ex.Message}"); }
                        }

                        if (slave.HasDC) slave.ConfigureDC(1000000); // Sync0 = 1ms, CSP 必须 DC
                        slave.CoE?.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));
                    }

                    if (!newMaster.SetState(EcState.SafeOp)) { errorMsg = "SafeOp 失败"; return false; }
                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }

                    // PDO 尺寸自检: 用驱动实际进程映像字节数核对结构体
                    int expOut = Marshal.SizeOf<PA_Output>();   // 9 (RxPDO)
                    int expIn = Marshal.SizeOf<PA_Input>();     // 23 (TxPDO)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        int realOut = newMaster.Slaves[i].OutputsByteCount;
                        int realIn = newMaster.Slaves[i].InputsByteCount;
                        Log($"轴{i + 1} PDO 实测: 输出={realOut}B 输入={realIn}B (结构体 输出={expOut}/输入={expIn})");
                        if (realOut != expOut || realIn != expIn)
                        {
                            errorMsg = $"轴{i + 1} PDO 尺寸不符: 驱动实际 输出{realOut}/输入{realIn} 字节, 结构体 输出{expOut}/输入{expIn}。" +
                                       "驱动当前 PDO 映射 ≠ 松下默认 0x1600/0x1A00 —— 需按驱动实际映射改 PA_Output/PA_Input, 或经 CoE 真正重映。";
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
                a.CurrentTarget = master.Slaves[i].PDO.InputsMapping<PA_Input>().PositionActualValue;
                a.Origin = a.CurrentTarget;
                list.Add(a);
            }
            axes = list.ToArray();
            AssignDefaultRoles();           // slave0=X slave1=Y slave2=Z

            // 报警复位 (新会话清空历史 + 解锁)
            alarmMgr.Reset();
            _groupStopLatched = false;
            _wkcMissConsec = 0;

            // 插补执行态复位
            _trajRunning = false;
            _trajPaused = false;
            _segs = new MotionSegment[0];
            _trajResetReq = true;
            _segReorigin = false;

            BuildAxisRows();

            RegisterEvents();
            DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
            _processedLogCount = DarraEtherCAT.Logs.Count;
            DarraEtherCAT.Logs.Updated += OnLogUpdated;

            isRunning = true;
            master.Events.ProcessDataCyclicSync += OnPdoCycle;   // 实时控制挂 PDO 周期回调, 随总线周期触发
            _statusThread = new Thread(StatusUpdateLoop) { IsBackground = true }; _statusThread.Start();

            grpControl.Enabled = true;
            lblStatus.Text = "已连接 (CSP 插补)";
            lblStatus.ForeColor = Color.Green;
            lblAxisCount.Text = $"轴数: {axes.Length}";
            Log($"连接成功 — {axes.Length} 轴 松下 A6B (默认 X/Y/Z), 先 [设原点] → [使能] → [运行]");
            RefreshPreviewFromText();
        }

        // ==================== 事件 ====================

        void RegisterEvents()
        {
            // ⚠ SDK 事件的 slaveIndex 是 1-based (ec_slave[0]=主站, 从站 1..N); axes[] 是 0-based → ax = si - 1。
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
                Log($"[上线] 轴{si} 已恢复 (需人工 [故障复位]+[使能])");
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
            // DC失步 / 链路 报警由 50ms 轮询单一权威管理, 这里仅 Log (避免事件 Raise + 轮询 Clear 抢键)。
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

        // 组停: CNC 插补是多轴耦合运动, 某轴 Fault 继续动其它轴会撕裂工件/刀具 → 停轨迹 + 全轴去使能 + 闭锁。
        // 可从 事件回调线程 / UI 线程 / 50ms 线程 调用 (只写 volatile 标志, 不碰 UI)。
        void GroupStop(int triggerAxis, string reason)
        {
            _trajRunning = false;
            _trajPaused = false;
            var arr = axes;
            foreach (var a in arr) a.ServoEnabled = false;   // StepAxis 中 !ServoEnabled → 钳到实际位置 + cw=0x06 安全停
            _groupStopLatched = true;
            Log($"⛔ 组停: {reason} (已停轨迹并全轴去使能, 闭锁中 — 排除后 [报警复位] 解锁)");
        }

        // 报警复位: 全轴无故障才清 latch + 物理清已恢复历史
        void btnAlarmAck_Click(object sender, EventArgs e)
        {
            foreach (var a in axes)
                if (IsFault(a.SnapStatusWord) || a.SnapErrorCode != 0)
                { Log("仍有轴处于驱动故障态, 请先 [故障复位] 再 [报警复位]"); return; }

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

        // ==================== PDO 控制回调 (轨迹推进 + 各轴插补目标, 随总线周期 1ms 触发) ====================

        void OnPdoCycle(ushort mi)
        {
            try
            {
                var m = master;
                if (m == null || !isRunning) return;

                var arr = axes;
                _pdoCycle++;

                // ① 设原点: 把各轴 Origin 设为当前实际位置 (PDO 线程内完成, 原子)
                if (_segReorigin)
                {
                    _segReorigin = false;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        ref var input = ref m.Slaves[arr[i].SlaveIndex].PDO.InputsMapping<PA_Input>();
                        arr[i].Origin = input.PositionActualValue;
                        arr[i].CurrentTarget = input.PositionActualValue;
                        arr[i].GraceResetPending = true;
                    }
                }

                // ② 轨迹执行态复位
                if (_trajResetReq)
                {
                    _trajResetReq = false;
                    _segIdx = 0; _sLocal = 0; _v = 0; _a = 0;
                }

                // ③ 沿规划好的速度剖面推进合成弧长 → 映射几何 → 算各轴 mm 坐标
                var segs = _segs;
                bool allOpEnabled = AllAssignedOpEnabled(arr);
                bool canAdvance = _trajRunning && !_trajPaused && !_groupStopLatched && allOpEnabled && segs.Length > 0;

                double curX = 0, curY = 0, curZ = 0;
                bool haveCoord = false;

                if (segs.Length > 0 && _segIdx < segs.Length)
                {
                    var seg = segs[_segIdx];

                    if (canAdvance)
                    {
                        AdvanceSpeed(seg);                          // 据 a/jerk 更新 _v (与 _a, S 曲线)
                        double ds = _v * DT;                        // 本周期推进弧长
                        _sLocal += ds;

                        // 跨段进位: 把多余弧长带入下一段, 衔接速度继承 (_v 不清零)
                        while (_segIdx < segs.Length && _sLocal >= segs[_segIdx].Length)
                        {
                            _sLocal -= segs[_segIdx].Length;
                            _segIdx++;
                            if (_segIdx >= segs.Length) break;
                        }

                        if (_segIdx >= segs.Length)
                        {
                            // 走完末段: 停在末点
                            _segIdx = segs.Length - 1;
                            _sLocal = segs[_segIdx].Length;
                            _v = 0; _a = 0;
                            _trajRunning = false;
                            Log("轨迹完成");
                        }
                        seg = segs[_segIdx];
                    }

                    seg.PointAt(_sLocal, out curX, out curY, out curZ);
                    haveCoord = true;
                    _uiToolX = (float)curX; _uiToolY = (float)curY;
                }

                // UI 进度快照
                _uiSegIdx = _segIdx;
                _uiSegTotal = segs.Length;
                _uiProgress = ComputeProgress(segs, _segIdx, _sLocal);
                _uiFeedMmMin = _v * 60.0;

                // ④ 各轴: CiA402 握手 + (使能且有坐标时) 按角色把 mm 坐标 → 脉冲目标
                int ppmm = _planPulsesPerMm;
                for (int i = 0; i < arr.Length; i++)
                {
                    var a = arr[i];
                    ref var input = ref m.Slaves[a.SlaveIndex].PDO.InputsMapping<PA_Input>();
                    ref var output = ref m.Slaves[a.SlaveIndex].PDO.OutputsMapping<PA_Output>();

                    // 本轴目标 mm (按角色取 G 代码坐标分量); 禁用轴或无坐标 → 保持实际位置
                    double coordMm; bool drive;
                    if (haveCoord && a.Role != AxisRole.禁用 && canAdvance)
                    {
                        drive = true;
                        switch (a.Role) { case AxisRole.X: coordMm = curX; break; case AxisRole.Y: coordMm = curY; break; default: coordMm = curZ; break; }
                    }
                    else { drive = false; coordMm = 0; }

                    // 运行时软限位钳制 (防御; 正常已被运行前路径检查拦截): 超限 → 钳到限位 + 置闩 (50ms 升故障并组停)
                    if (drive && a.MaxMm > a.MinMm)
                    {
                        if (coordMm < a.MinMm) { coordMm = a.MinMm; a.AlarmSoftLimit = true; }
                        else if (coordMm > a.MaxMm) { coordMm = a.MaxMm; a.AlarmSoftLimit = true; }
                        else a.AlarmSoftLimit = false;
                    }
                    else a.AlarmSoftLimit = false;

                    // 目标脉冲用 long 计算 + 钳到 int32 (DINT) 范围: 大坐标×大脉冲当量 会超 int.MaxValue,
                    // 直接 (int) 转换会回绕成乱值 → CSP 目标瞬跳 → 轴猛冲/撞机。超范围按软限位处理(钳制+组停)。
                    int desired;
                    if (drive)
                    {
                        long tgt = (long)a.Origin + (long)Math.Round(coordMm * (double)ppmm);
                        if (tgt > int.MaxValue) { tgt = int.MaxValue; a.AlarmSoftLimit = true; }
                        else if (tgt < int.MinValue) { tgt = int.MinValue; a.AlarmSoftLimit = true; }
                        desired = (int)tgt;
                    }
                    else desired = 0;
                    StepAxis(a, desired, drive, ref input, ref output);

                    ushort sw = input.StatusWord;
                    a.SnapStatusWord = sw;
                    a.SnapActualPosition = input.PositionActualValue;
                    a.SnapFollowError = input.FollowingErrorActualValue;
                    a.SnapErrorCode = input.ErrorCode;
                    a.SnapDriveState = ParseDriveState(sw);

                    // —— 报警检测 (只置 volatile 闩, 不锁/不碰 UI) ——
                    int fe = input.PositionActualValue - output.TargetPosition;   // 跟随误差 = 实际 - 本周期下发目标
                    bool eligibleNow = a.Role != AxisRole.禁用 && a.ServoEnabled && IsOperationEnabled(sw) && !a.FaultReset;
                    if (!eligibleNow) { a.SyncStartCycle = 0; a.FollowErrConsec = 0; }
                    else if (a.SyncStartCycle == 0 || a.GraceResetPending) { a.SyncStartCycle = _pdoCycle; a.GraceResetPending = false; a.FollowErrConsec = 0; }
                    bool pastGrace = eligibleNow && a.SyncStartCycle != 0 && (_pdoCycle - a.SyncStartCycle) >= GRACE_CYCLES;
                    if (pastGrace) a.WasEligible = true;

                    if (pastGrace)
                    {
                        if (Math.Abs(fe) > a.FollowErrLimit) { if (++a.FollowErrConsec >= FE_CONSEC_N) a.AlarmFollowError = true; }
                        else { a.FollowErrConsec = 0; a.AlarmFollowError = false; }
                    }
                    else a.AlarmFollowError = false;

                    a.AlarmFault = IsFault(sw) || input.ErrorCode != 0;
                    a.AlarmDropEnable = a.WasEligible && !IsOperationEnabled(sw) && a.ServoEnabled && !a.FaultReset;

                    if (!a.ServoEnabled || a.FaultReset || a.Role == AxisRole.禁用)
                    { a.WasEligible = false; a.AlarmFollowError = false; a.AlarmDropEnable = false; a.AlarmSoftLimit = false; a.FollowErrConsec = 0; }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDO] 控制回调异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 是否所有已分配角色的轴都 OperationEnabled (插补推进前置条件)
        static bool AllAssignedOpEnabled(AxisController[] arr)
        {
            int assigned = 0;
            foreach (var a in arr)
            {
                if (a.Role == AxisRole.禁用) continue;
                assigned++;
                if (!(a.ServoEnabled && IsOperationEnabled(a.SnapStatusWord))) return false;
            }
            return assigned > 0;
        }

        // 据当前段衔接速度 + a/jerk 更新合成进给 _v (梯形: 加速度瞬时取 ±a; S曲线: 加速度按 jerk 斜坡)。
        // 目标: 段内先加速到 VCruise, 临近段末按 v_exit² = v² - 2·a·剩余弧长 提前减速到 VExit。
        void AdvanceSpeed(MotionSegment seg)
        {
            double aMax = _planAccel;
            double jerk = _planJerk;
            double remain = Math.Max(0, seg.Length - _sLocal);     // 段内剩余弧长
            double vTarget = seg.VCruise;

            // 末段减速: 为了在段末降到 VExit, 当前允许的最大速度 = sqrt(VExit² + 2·a·remain)。
            // S 曲线下加速度受 jerk 斜坡限制(不能瞬时拉到 -a), 实际需更长减速距离 → 预留 jerk 斜坡距离
            // (≈ 当前速度 × 加速度斜坡时间 a/jerk), 让减速提前触发, 防段末/终点过冲编程位置。
            double decelRemain = remain;
            if (_planSCurve && jerk > 1e-9) decelRemain = Math.Max(0, remain - _v * (aMax / jerk));
            double vDecelCap = Math.Sqrt(seg.VExit * seg.VExit + 2.0 * aMax * decelRemain);
            if (vDecelCap < vTarget) vTarget = vDecelCap;

            if (!_planSCurve)
            {
                // 梯形: 加速度瞬时 ±a
                if (_v < vTarget) { _v += aMax * DT; if (_v > vTarget) _v = vTarget; }
                else if (_v > vTarget) { _v -= aMax * DT; if (_v < vTarget) _v = vTarget; }
                _a = 0;
            }
            else
            {
                // S 曲线: 加速度按 jerk 斜坡升降, 不瞬时跳变。
                // 需要的加速度方向 = sign(vTarget - v); 距离差很小则把 a 拉回 0。
                double dv = vTarget - _v;
                double aDesired;
                if (Math.Abs(dv) < 1e-6) aDesired = 0;
                else
                {
                    // 末速逼近: 用能在 dv 内停住加速度的临界 a = dv 方向 * min(aMax, sqrt(2*jerk*|dv|))
                    double aCrit = Math.Sqrt(2.0 * jerk * Math.Abs(dv));
                    aDesired = Math.Sign(dv) * Math.Min(aMax, aCrit);
                }
                // a 以 jerk 斜坡逼近 aDesired
                if (_a < aDesired) { _a += jerk * DT; if (_a > aDesired) _a = aDesired; }
                else if (_a > aDesired) { _a -= jerk * DT; if (_a < aDesired) _a = aDesired; }
                _v += _a * DT;
                if (_v < 0) _v = 0;
            }
            if (_v < 0) _v = 0;
        }

        static double ComputeProgress(MotionSegment[] segs, int segIdx, double sLocal)
        {
            if (segs.Length == 0) return 0;
            double total = 0, done = 0;
            for (int i = 0; i < segs.Length; i++)
            {
                total += segs[i].Length;
                if (i < segIdx) done += segs[i].Length;
                else if (i == segIdx) done += Math.Min(sLocal, segs[i].Length);
            }
            return total < 1e-9 ? 1.0 : done / total;
        }

        // CSP 单轴: CiA402 使能握手 (0x06→0x07→0x0F), 使能后下发插补目标位置。
        // 未使能 / 正在握手 / 故障 / 不驱动时, 目标位置恒等于当前实际位置 (无跳变)。
        void StepAxis(AxisController a, int desiredTarget, bool drive, ref PA_Input input, ref PA_Output output)
        {
            output.ModesOfOperation = 8;
            ushort sw = input.StatusWord;
            ushort cw = 0;

            if (a.FaultReset) { cw = 0x80; a.FaultReset = false; }
            else if (IsFault(sw)) { /* 等待故障复位 */ }
            else if (!a.ServoEnabled) { a.CurrentTarget = input.PositionActualValue; a.Origin = drive ? a.Origin : input.PositionActualValue; }
            else if ((sw & 0x4F) == 0x00) { /* NotReadyToSwitchOn: 等待 */ }
            else if (IsSwitchOnDisabled(sw)) { cw = 0x06; a.CurrentTarget = input.PositionActualValue; }
            else if (IsReadyToSwitchOn(sw)) { cw = 0x07; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsSwitchedOn(sw)) { cw = 0x0F; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsOperationEnabled(sw))
            {
                if (drive) a.CurrentTarget = desiredTarget;
                else a.CurrentTarget = input.PositionActualValue;   // 不推进时钳到实际位置
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
                    alarmMgr.DrainPending();
                    DetectAlarms(arr, m);

                    int segIdx = _uiSegIdx, segTotal = _uiSegTotal;
                    double prog = _uiProgress, feed = _uiFeedMmMin;
                    bool running = _trajRunning, paused = _trajPaused;
                    int ppmm = _planPulsesPerMm;

                    BeginInvoke(new Action(() =>
                    {
                        lblProgress.Text = $"段: {(segTotal == 0 ? 0 : segIdx + 1)} / {segTotal}    " +
                                           $"完成: {prog * 100:F1}%    进给: {feed:F0} mm/min" +
                                           (running ? (paused ? "    ● 暂停" : "    ● 运行中") : "");
                        lblProgress.ForeColor = running ? (paused ? Color.DarkOrange : Color.SeaGreen) : Color.DimGray;

                        for (int i = 0; i < arr.Length && i < gridAxes.Rows.Count; i++)
                        {
                            var a = arr[i];
                            ushort sw = a.SnapStatusWord;
                            var row = gridAxes.Rows[i];
                            row.Cells[COL_STATE].Value = a.SnapDriveState;
                            row.Cells[COL_SW].Value = $"0x{sw:X4}";
                            double actMm = ppmm > 0 ? (a.SnapActualPosition - a.Origin) / (double)ppmm : 0;
                            double tgtMm = ppmm > 0 ? (a.SnapTargetPosition - a.Origin) / (double)ppmm : 0;
                            row.Cells[COL_ACTPOS].Value = $"{actMm:F3} ({a.SnapActualPosition})";
                            row.Cells[COL_TGTPOS].Value = $"{tgtMm:F3}";
                            row.Cells[COL_FE].Value = a.SnapFollowError.ToString();
                            row.Cells[COL_ENABLED].Value = a.ServoEnabled ? "ON" : "off";

                            var stateCell = row.Cells[COL_STATE];
                            if (IsFault(sw) || a.SnapErrorCode != 0) stateCell.Style.ForeColor = Color.Firebrick;
                            else if (IsOperationEnabled(sw)) stateCell.Style.ForeColor = Color.SeaGreen;
                            else stateCell.Style.ForeColor = Color.DimGray;

                            bool bad = IsFault(sw) || a.SnapErrorCode != 0 || a.AlarmFollowError || a.AlarmDropEnable;
                            row.DefaultCellStyle.BackColor = bad ? Color.FromArgb(252, 235, 235) : Color.White;
                        }

                        RefreshAlarmUI();
                        pnlPreview.Invalidate();          // 刷新刀位点
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
                if (a.AlarmFollowError) RaiseFault(i, AlarmType.跟随误差, $"跟随误差 {a.SnapFollowError} 脉冲 > 限值 {a.FollowErrLimit}");
                else alarmMgr.Clear(i, AlarmType.跟随误差);
                if (a.AlarmDropEnable) RaiseFault(i, AlarmType.掉OP, "插补运行中掉出 OperationEnabled");
                else alarmMgr.Clear(i, AlarmType.掉OP);
                if (a.AlarmFault) RaiseFault(i, AlarmType.驱动故障, $"状态字Fault 或 错误码 0x{a.SnapErrorCode:X4}");
                else alarmMgr.Clear(i, AlarmType.驱动故障);
                if (a.AlarmSoftLimit) RaiseFault(i, AlarmType.软限位, $"{a.Role} 轴超出软限位 [{a.MinMm:F1},{a.MaxMm:F1}]mm, 已钳制并停轨迹");
                else alarmMgr.Clear(i, AlarmType.软限位);

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

                ushort exp = DarraEtherCAT.GetGroupExpectedWKC(m.MasterNumber, 1);
                ushort act = DarraEtherCAT.GetGroupActualWKC(m.MasterNumber, 1);
                if (exp > 0 && act < exp)
                {
                    // WKC 铁律: 有从站掉了 → 报警 + 由 slave.IsLost 逐轴定位, 不停 OP / 不改 exp / 不去使能正常轴
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

        // ==================== 2D 路径预览 (GDI+) ====================

        // 仅用于预览: 重新解析当前 G 代码文本 (出错则清空预览段)。不规划速度, 只取几何画路径。
        void RefreshPreviewFromText()
        {
            try
            {
                var segs = ParseGcode(txtGcode.Text, out _, out string err);
                _previewSegs = (segs != null) ? segs.ToArray() : new MotionSegment[0];
            }
            catch { _previewSegs = new MotionSegment[0]; }
            if (pnlPreview.IsHandleCreated) pnlPreview.Invalidate();
        }
        volatile MotionSegment[] _previewSegs = new MotionSegment[0];

        void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = pnlPreview.ClientSize.Width;
            int h = pnlPreview.ClientSize.Height;
            const int margin = 14;

            var segs = _previewSegs;
            if (segs.Length == 0)
            {
                using (var f = new Font("Microsoft YaHei UI", 9F))
                    g.DrawString("无可预览路径 (G 代码为空或有误)", f, Brushes.Silver, margin, margin);
                return;
            }

            // 求路径包围盒 (mm)
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            void Acc(double x, double y) { if (x < minX) minX = x; if (y < minY) minY = y; if (x > maxX) maxX = x; if (y > maxY) maxY = y; }
            foreach (var s in segs)
            {
                Acc(s.X0, s.Y0); Acc(s.X1, s.Y1);
                if (s.Type != SegType.Line)
                {
                    // 圆弧用圆心±半径粗略扩展包围盒 (够预览)
                    Acc(s.Cx - s.Radius, s.Cy - s.Radius);
                    Acc(s.Cx + s.Radius, s.Cy + s.Radius);
                }
            }
            Acc(0, 0);   // 含原点
            double spanX = Math.Max(1e-6, maxX - minX);
            double spanY = Math.Max(1e-6, maxY - minY);
            double sx = (w - 2 * margin) / spanX;
            double sy = (h - 2 * margin) / spanY;
            double scale = Math.Min(sx, sy);
            // 居中
            double offX = margin + (w - 2 * margin - spanX * scale) / 2.0;
            double offY = margin + (h - 2 * margin - spanY * scale) / 2.0;

            // mm → 屏幕 (Y 翻转, 工件 +Y 朝上)
            PointF ToScr(double mx, double my)
                => new PointF((float)(offX + (mx - minX) * scale), (float)(h - (offY + (my - minY) * scale)));

            // 画原点 (绿十字)
            var op = ToScr(0, 0);
            using (var penO = new Pen(Color.SeaGreen, 1.5f))
            {
                g.DrawLine(penO, op.X - 7, op.Y, op.X + 7, op.Y);
                g.DrawLine(penO, op.X, op.Y - 7, op.X, op.Y + 7);
            }

            // 画编程路径 (蓝)
            using (var pen = new Pen(Color.RoyalBlue, 2f))
            {
                foreach (var s in segs)
                {
                    if (s.Type == SegType.Line)
                        g.DrawLine(pen, ToScr(s.X0, s.Y0), ToScr(s.X1, s.Y1));
                    else
                    {
                        const int N = 48;
                        var pts = new PointF[N + 1];
                        for (int k = 0; k <= N; k++)
                        {
                            double t = (double)k / N;
                            s.PointAt(t * s.Length, out double px, out double py, out _);
                            pts[k] = ToScr(px, py);
                        }
                        g.DrawLines(pen, pts);
                    }
                }
            }

            // 画实时刀位点 (红, 仅连接后有效)
            if (master != null)
            {
                var tp = ToScr(_uiToolX, _uiToolY);
                using (var b = new SolidBrush(Color.Firebrick))
                    g.FillEllipse(b, tp.X - 4, tp.Y - 4, 8, 8);
            }
        }

        // ==================== 断开 ====================

        void btnDisconnect_Click(object sender, EventArgs e) { Disconnect(); }

        // 先 Join UI 刷新线程再 Close: 防止 DetectAlarms/StatusUpdateLoop 正访问 m.Slaves[...] 时 native 被释放 → UAF/AV。
        // PDO 控制回调 OnPdoCycle 走 SDK 实时线程, 已在 isRunning=false 后退订 ProcessDataCyclicSync 防 UAF。
        void JoinWorkers()
        {
            var t2 = _statusThread;
            _statusThread = null;
            try { t2?.Join(500); } catch { }
        }

        void Disconnect()
        {
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { } catch (Exception ex) { Console.WriteLine($"[Disconnect] CTS Cancel 异常: {ex.Message}"); }

            _trajRunning = false;
            _trajPaused = false;
            foreach (var a in axes) { a.ServoEnabled = false; a.FaultReset = false; }
            isRunning = false;
            try { master?.Events.ProcessDataCyclicSync -= OnPdoCycle; } catch { }   // 退订 PDO 周期回调, 防 master 释放后回调仍访问 m.Slaves[...] → native UAF

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
            _segs = new MotionSegment[0];
            gridAxes.Rows.Clear();

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            grpControl.Enabled = false;
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            lblAxisCount.Text = "轴数: -";
            lblProgress.Text = "段: - / -    完成: -    进给: -";
            lblProgress.ForeColor = Color.DimGray;

            _groupStopLatched = false;
            pnlAlarmBanner.BackColor = Color.FromArgb(238, 238, 238);
            lblAlarmBanner.ForeColor = Color.DimGray;
            lblAlarmBanner.Text = "● 未连接";
            lblAlarmCount.Text = "激活:0  故障:0  历史:0";
            btnAlarmAck.Enabled = false;
            pnlPreview.Invalidate();
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
            try { master?.Events.ProcessDataCyclicSync -= OnPdoCycle; } catch { }   // 退订 PDO 周期回调, 防 master 释放后回调仍访问 m.Slaves[...] → native UAF
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
