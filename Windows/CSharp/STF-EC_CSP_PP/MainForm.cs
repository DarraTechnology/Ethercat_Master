using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace STF_EC_CSP_PP
{
    // ==================== STF-EC 步进驱动器 PDO (CiA 402, Profile 402) ====================
    // 厂商: Shanghai AMP&MOONS' Automation (VendorId 0x00000168)
    // 型号: STF-EC (ProductCode 0x02)
    //
    // 本例支持 *多轴* — 轴数由 config.deni 里实际扫描到的从站数量决定 (master.SlaveCount),
    // 所有 STF-EC 共用同一 PDO 结构。提供的 config.deni 含 5 个 STF-EC (PhysAddr 0x1001~0x1005)。
    //
    // 字节布局来自 config.deni 实测 (字段顺序必须与 PDO 条目逐字节一致, 不可调换):
    //   RxPDO 输出 = 29 字节 (0x1600~0x1603)
    //   TxPDO 输入 = 35 字节 (0x1A00~0x1A03, 含 4 个 Touch Probe 位置值)
    //
    // 默认 PDO 同时包含 CSP 与 PP 需要的全部对象 (运行模式 0x6060 / 目标位置 0x607A /
    // 轮廓速度 0x6081 等), 因此单一 config.deni 即可, 模式在连接时统一切换:
    //   CSP: 每个从站 ConfigureDC 启用 Sync0 (1ms), 0x6060 = 8
    //   PP : Free Run 同步 (SM2/SM3 SyncType = 0), 0x6060 = 1

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct STF_Output  // 29 字节 (RxPDO 0x1600+0x1601+0x1602+0x1603)
    {
        // 0x1600
        public ushort ControlWord;          // 0x6040:0  UINT
        public sbyte ModesOfOperation;      // 0x6060:0  SINT   (CSP=8 / PP=1)
        public int TargetPosition;          // 0x607A:0  DINT
        // 0x1601
        public uint ProfileVelocity;        // 0x6081:0  UDINT  (PP 轮廓速度)
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

    /// <summary>
    /// 单个轴的控制状态 (UI 线程写控制变量, PDO 线程写快照, 互不阻塞)
    /// </summary>
    sealed class AxisController
    {
        public readonly int SlaveIndex;
        public readonly int PhysAddr;
        public AxisController(int idx, int physAddr) { SlaveIndex = idx; PhysAddr = physAddr; }

        // 控制 (UI 写, PDO 读)
        public volatile bool ServoEnabled;
        public volatile bool FaultReset;
        public volatile int DesiredPosition;
        public volatile bool TriggerMove;
        public volatile bool JogForward;
        public volatile bool JogReverse;
        public volatile int CachedCspStep = 10;
        public volatile int CachedPpVelocity = 10000;

        // 快照 (PDO 写, UI 读)
        public volatile ushort SnapStatusWord;
        public volatile int SnapActualPosition;
        public volatile int SnapTargetPosition;
        public volatile int SnapActualVelocity;
        public volatile ushort SnapErrorCode;
        public volatile string SnapDriveState = "---";

        // CSP/PP 内部状态机 (仅 PDO 线程访问)
        public int CurrentTarget;
        public int PpState;
        public int MotionCycles;
    }

    /// <summary>
    /// STF-EC 多轴步进驱动器运动控制主窗体
    /// CSP (Mode=8) / PP (Mode=1) 共用同一 config.deni, 连接时统一切换。
    /// </summary>
    public partial class MainForm : Form
    {
        volatile DarraEtherCAT master;
        volatile bool isRunning = false;
        Thread _pdoThread;
        Thread _statusThread;
        bool isCSP = true;
        private CancellationTokenSource _connectCts;

        AxisController[] axes = new AxisController[0];
        int _selectedAxisIndex = 0;
        bool _loadingParam = false;

        // 异步日志
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int LOG_MAX_TEXT = 50000;
        private const int LOG_QUEUE_CAP = 500;
        private int _processedLogCount;

        const int PULSES_PER_REV = 10000;          // STF-EC 默认细分 (0x2604 Steps per Rev), 角度换算用
        const uint DEFAULT_PROFILE_ACCEL = 500000; // PP 默认加/减速度 (脉冲/秒²)

        // 轴总览表列索引
        const int COL_SEL = 0, COL_AXIS = 1, COL_ADDR = 2, COL_STATE = 3, COL_SW = 4,
                  COL_ERR = 5, COL_ACTPOS = 6, COL_TGTPOS = 7, COL_VEL = 8, COL_REACHED = 9, COL_ENABLED = 10;

        public MainForm()
        {
            InitializeComponent();
            InitGrid();
            BindEvents();
            cboMode.SelectedIndex = 0;
            _logFlushTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _logFlushTimer.Tick += LogFlushTimer_Tick;
            _logFlushTimer.Start();
        }

        // ==================== 事件绑定 ====================

        void BindEvents()
        {
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
            cboMode.SelectedIndexChanged += (s, e) => UpdateParamUiForSelected();

            gridAxes.SelectionChanged += GridAxes_SelectionChanged;

            btnAllEnable.Click += (s, e) => ForEachTarget(a => a.ServoEnabled = true, "全部使能");
            btnAllDisable.Click += (s, e) => ForEachTarget(a => a.ServoEnabled = false, "全部去使能");
            btnAllFaultReset.Click += (s, e) => ForEachTarget(a => a.FaultReset = true, "全部故障复位");
            btnAllHome.Click += (s, e) => ForEachTarget(a => { a.DesiredPosition = 0; a.TriggerMove = true; }, "全部回零");
            btnAllEStop.Click += (s, e) => ForEachTarget(a => { a.ServoEnabled = false; a.JogForward = a.JogReverse = false; }, "全部急停");

            btnEnable.Click += (s, e) => WithSelected(a => { a.ServoEnabled = true; Log($"轴{a.SlaveIndex + 1} 使能"); });
            btnDisable.Click += (s, e) => WithSelected(a => { a.ServoEnabled = false; Log($"轴{a.SlaveIndex + 1} 去使能"); });
            btnFaultReset.Click += (s, e) => WithSelected(a => { a.FaultReset = true; Log($"轴{a.SlaveIndex + 1} 故障复位"); });
            btnHome.Click += (s, e) => WithSelected(a => { a.DesiredPosition = 0; a.TriggerMove = true; Log($"轴{a.SlaveIndex + 1} 回零"); });

            btnAbsMove.Click += (s, e) => WithSelected(a =>
            {
                int pulses = DegToPulses((double)numTargetDeg.Value);
                a.DesiredPosition = pulses; a.TriggerMove = true;
                Log($"轴{a.SlaveIndex + 1} 绝对运动 {numTargetDeg.Value}° ({pulses})");
            });
            btnRelMove.Click += (s, e) => WithSelected(a =>
            {
                int pulses = DegToPulses((double)numTargetDeg.Value);
                a.DesiredPosition = a.SnapActualPosition + pulses; a.TriggerMove = true;
                Log($"轴{a.SlaveIndex + 1} 相对运动 {numTargetDeg.Value}° ({pulses})");
            });

            btnJogForward.MouseDown += (s, e) => WithSelected(a => a.JogForward = true);
            btnJogForward.MouseUp += (s, e) => WithSelected(a => a.JogForward = false);
            btnJogReverse.MouseDown += (s, e) => WithSelected(a => a.JogReverse = true);
            btnJogReverse.MouseUp += (s, e) => WithSelected(a => a.JogReverse = false);

            numParam.ValueChanged += (s, e) =>
            {
                if (_loadingParam) return;
                WithSelected(a =>
                {
                    if (isCSP) a.CachedCspStep = Math.Max(1, (int)numParam.Value);
                    else a.CachedPpVelocity = Math.Max(1, (int)numParam.Value);
                });
                UpdateParamHint();
            };
        }

        static int DegToPulses(double deg) => (int)(deg * PULSES_PER_REV / 360.0);

        AxisController Selected =>
            (axes.Length > 0 && _selectedAxisIndex >= 0 && _selectedAxisIndex < axes.Length)
                ? axes[_selectedAxisIndex] : null;

        void WithSelected(Action<AxisController> act)
        {
            var a = Selected;
            if (a != null) act(a);
        }

        void ForEachTarget(Action<AxisController> act, string logName)
        {
            var targets = GetTargetAxes();
            foreach (var a in targets) act(a);
            Log($"{logName}: {targets.Count} 轴");
        }

        // 勾选了「选」列的轴; 未勾选任何 = 全部
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
            AddTextCol("驱动状态", 150, true);                                            // 3
            AddTextCol("状态字", 70, true);                                               // 4
            AddTextCol("错误码", 70, true);                                               // 5
            AddTextCol("实际位置(°)", 130, true);                                         // 6
            AddTextCol("目标位置(°)", 130, true);                                         // 7
            AddTextCol("实际速度", 90, true);                                             // 8
            AddTextCol("到位", 48, true);                                                 // 9
            AddTextCol("使能", 48, true);                                                 // 10
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
                row.Cells[COL_STATE].Value = "---";
            }
            if (gridAxes.Rows.Count > 0)
            {
                _selectedAxisIndex = 0;
                gridAxes.Rows[0].Selected = true;
            }
        }

        void GridAxes_SelectionChanged(object sender, EventArgs e)
        {
            if (gridAxes.CurrentRow != null)
                _selectedAxisIndex = gridAxes.CurrentRow.Index;
            else if (gridAxes.SelectedRows.Count > 0)
                _selectedAxisIndex = gridAxes.SelectedRows[0].Index;
            UpdateParamUiForSelected();
        }

        void UpdateParamUiForSelected()
        {
            isCSP = cboMode.SelectedIndex == 0;
            var a = Selected;
            lblSelAxis.Text = a != null ? $"选定轴: 轴{a.SlaveIndex + 1} (0x{a.PhysAddr:X4})" : "选定轴: -";

            _loadingParam = true;
            if (isCSP)
            {
                lblParam.Text = "CSP 步/周期:";
                numParam.Value = Clamp(a?.CachedCspStep ?? 10, numParam.Minimum, numParam.Maximum);
            }
            else
            {
                lblParam.Text = "PP 轮廓速度:";
                numParam.Value = Clamp(a?.CachedPpVelocity ?? 10000, numParam.Minimum, numParam.Maximum);
            }
            _loadingParam = false;
            UpdateParamHint();
        }

        static decimal Clamp(int v, decimal min, decimal max)
        {
            decimal d = v;
            if (d < min) d = min;
            if (d > max) d = max;
            return d;
        }

        void UpdateParamHint()
        {
            if (isCSP)
                lblParamHint.Text = $"= {(int)numParam.Value * 1000} 脉冲/秒 (1ms 周期插值步长)";
            else
                lblParamHint.Text = "脉冲/秒 (PP 轮廓速度 0x6081)";
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

        // ==================== 连接 ====================

        string GetDeniPath()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDir, "EtherCATRestore", "config.deni");
        }

        async void btnConnect_Click(object sender, EventArgs e)
        {
            isCSP = cboMode.SelectedIndex == 0;
            string deniPath = GetDeniPath();

            if (!File.Exists(deniPath))
            {
                MessageBox.Show($"配置文件不存在: {deniPath}\n请用主站 GUI 扫描 STF-EC 从站, 导出 config.deni 放到该目录\n(详见 README.md)", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            cboMode.Enabled = false;

            _connectCts = new CancellationTokenSource();
            var ct = _connectCts.Token;
            bool csp = isCSP;

            Log($"正在初始化主站 ({(csp ? "CSP" : "PP")} 模式)...");

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

                    // 逐轴配置同步方式 + 启动参数
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

                        if (csp)
                        {
                            if (slave.HasDC) slave.ConfigureDC(1000000); // Sync0 = 1ms
                            slave.CoE?.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));
                        }
                        else if (slave.CoE != null)
                        {
                            // 仅一次性同步配置 (属于初始化, 不是控制): PP 走 Free Run + 放宽同步误差容限。
                            // 运动控制量 (目标位置 0x607A / 轮廓速度 0x6081 / 加减速 0x6083·0x6084) 全在 RxPDO 里,
                            // 由 PDO 每周期下发, 不再用 SDO —— 之前对 0x6083/0x6084 的 SDO 读写是冗余的, 已删。
                            slave.CoE.SDOWrite(0x1C32, 1, BitConverter.GetBytes((ushort)0x0000)); // SM2 Free Run
                            slave.CoE.SDOWrite(0x1C33, 1, BitConverter.GetBytes((ushort)0x0000)); // SM3 Free Run
                            slave.CoE.SDOWrite(0x10F1, 2, BitConverter.GetBytes((ushort)65535));  // 同步误差容限
                        }
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

                    // 逐轴 PDO 初始化 (SafeOp 后, OP 前)
                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var input = ref newMaster.Slaves[i].PDO.InputsMapping<STF_Input>();
                        ref var output = ref newMaster.Slaves[i].PDO.OutputsMapping<STF_Output>();
                        output.ModesOfOperation = (sbyte)(csp ? 8 : 1);
                        output.TargetPosition = input.PositionActualValue;
                        if (!csp)
                        {
                            output.ProfileVelocity = 10000;
                            output.ProfileAcceleration = DEFAULT_PROFILE_ACCEL;
                            output.ProfileDeceleration = DEFAULT_PROFILE_ACCEL;
                        }
                    }

                    if (ct.IsCancellationRequested) { errorMsg = "已取消"; return false; }
                    if (!newMaster.SetState(EcState.OP)) { errorMsg = "OP 失败"; return false; }

                    for (int i = 0; i < slaveCount; i++)
                    {
                        ref var tOut = ref newMaster.Slaves[i].PDO.OutputsMapping<STF_Output>();
                        ref var tIn = ref newMaster.Slaves[i].PDO.InputsMapping<STF_Input>();
                        tOut.ControlWord = 0;
                        tOut.ModesOfOperation = (sbyte)(csp ? 8 : 1);
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
                    cboMode.Enabled = true;
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
                a.DesiredPosition = master.Slaves[i].PDO.InputsMapping<STF_Input>().PositionActualValue;
                a.CurrentTarget = a.DesiredPosition;
                list.Add(a);
            }
            axes = list.ToArray();
            BuildAxisRows();
            UpdateParamUiForSelected();

            RegisterEvents();
            DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
            _processedLogCount = DarraEtherCAT.Logs.Count;
            DarraEtherCAT.Logs.Updated += OnLogUpdated;

            isRunning = true;
            _pdoThread = new Thread(PdoControlLoop) { IsBackground = true };
            _pdoThread.Start();
            _statusThread = new Thread(StatusUpdateLoop) { IsBackground = true };
            _statusThread.Start();

            grpGlobal.Enabled = true;
            grpAxis.Enabled = true;
            lblStatus.Text = $"已连接 ({(isCSP ? "CSP" : "PP")})";
            lblStatus.ForeColor = Color.Green;
            lblAxisCount.Text = $"轴数: {axes.Length}";
            Log($"连接成功 — {axes.Length} 轴 STF-EC, 可以开始控制");
        }

        // ==================== 事件 ====================

        void RegisterEvents()
        {
            // 注: SDK 事件 slaveIndex 为 1-based (SOEM ec_slave[0]=主站, 从站 1..N), 即已是轴号, 不再 +1。
            master.Events.StateChanged += (s, ev) => Log($"[状态] 主站: {ev.OldState} -> {ev.NewState}");
            master.Events.SlaveStateChanged += (mi, si, os, ns) => Log($"[状态] {(si == 0 ? "主站" : $"轴{si}")}: {os} -> {ns}");
            master.Events.EmergencyEvent += (mi, si, code, reg, b1, w1, w2) => Log($"[紧急] 轴{si} 错误码: 0x{code:X4}");
            master.Events.SlaveOffline += (si) => Log($"[离线] 轴{si} 已断开");
            master.Events.SlaveOnline += (si) => Log($"[上线] 轴{si} 已恢复");
            master.Events.PDOFrameLoss += (mi, grp, con, tot) => Log($"[丢帧] 组{grp} 连续: {con}, 累计: {tot}");
        }

        void OnLogUpdated()
        {
            try
            {
                var logs = DarraEtherCAT.Logs;
                int total = logs.Count;
                if (_processedLogCount > total) _processedLogCount = 0;
                for (int i = _processedLogCount; i < total; i++)
                    Log($"[DLL] {logs[i]}");
                _processedLogCount = total;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnLogUpdated] 异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ==================== PDO 控制线程 (遍历全部轴) ====================

        void PdoControlLoop()
        {
            try
            {
                var m = master;
                if (m == null) return;
                bool csp = isCSP;

                while (isRunning && master != null)
                {
                    var arr = axes;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var a = arr[i];
                        ref var input = ref m.Slaves[a.SlaveIndex].PDO.InputsMapping<STF_Input>();
                        ref var output = ref m.Slaves[a.SlaveIndex].PDO.OutputsMapping<STF_Output>();
                        if (csp) StepCsp(a, ref input, ref output);
                        else StepPp(a, ref input, ref output);

                        a.SnapStatusWord = input.StatusWord;
                        a.SnapActualPosition = input.PositionActualValue;
                        a.SnapActualVelocity = input.VelocityActualValue;
                        a.SnapErrorCode = input.ErrorCode;
                        a.SnapDriveState = ParseDriveState(input.StatusWord);
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDO] 控制线程异常退出: {ex.Message}\n{ex.StackTrace}");
            }
        }

        void StepCsp(AxisController a, ref STF_Input input, ref STF_Output output)
        {
            output.ModesOfOperation = 8;
            ushort sw = input.StatusWord;
            ushort cw = 0;

            if (a.FaultReset) { cw = 0x80; a.FaultReset = false; }
            else if (IsFault(sw)) { }
            else if (!a.ServoEnabled) { a.CurrentTarget = input.PositionActualValue; a.DesiredPosition = a.CurrentTarget; }
            else if ((sw & 0x4F) == 0x00) { /* NotReadyToSwitchOn: 等待 */ }
            else if (IsSwitchOnDisabled(sw)) { cw = 0x06; a.CurrentTarget = input.PositionActualValue; a.DesiredPosition = a.CurrentTarget; }
            else if (IsReadyToSwitchOn(sw)) { cw = 0x07; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsSwitchedOn(sw)) { cw = 0x0F; a.CurrentTarget = input.PositionActualValue; output.TargetPosition = a.CurrentTarget; }
            else if (IsOperationEnabled(sw))
            {
                int step = Math.Max(1, a.CachedCspStep);
                if (a.JogForward) a.DesiredPosition = a.CurrentTarget + step;
                else if (a.JogReverse) a.DesiredPosition = a.CurrentTarget - step;

                int diff = a.DesiredPosition - a.CurrentTarget;
                if (Math.Abs(diff) > step) a.CurrentTarget += Math.Sign(diff) * step;
                else a.CurrentTarget = a.DesiredPosition;

                output.TargetPosition = a.CurrentTarget;
                cw = 0x0F;
            }
            output.ControlWord = cw;
            a.SnapTargetPosition = a.CurrentTarget;
        }

        void StepPp(AxisController a, ref STF_Input input, ref STF_Output output)
        {
            ushort sw = input.StatusWord;
            ushort cw = 0;
            int profVel = Math.Max(1, a.CachedPpVelocity);
            output.ProfileVelocity = (uint)profVel;
            output.ProfileAcceleration = DEFAULT_PROFILE_ACCEL;
            output.ProfileDeceleration = DEFAULT_PROFILE_ACCEL;
            output.ModesOfOperation = 1;

            if (a.FaultReset) { cw = 0x80; a.PpState = 0; a.FaultReset = false; }
            else if (IsFault(sw)) { a.PpState = 0; }
            else if (!a.ServoEnabled) { a.PpState = 0; }
            else if ((sw & 0x4F) == 0x00) { }
            else if (IsSwitchOnDisabled(sw)) { cw = 0x06; }
            else if (IsReadyToSwitchOn(sw)) { cw = 0x07; }
            else if (IsSwitchedOn(sw)) { cw = 0x0F; }
            else if (IsOperationEnabled(sw))
            {
                bool targetReached = (sw & 0x0400) != 0;
                bool setPointAck = (sw & 0x1000) != 0;

                if ((a.JogForward || a.JogReverse) && a.PpState == 0 && targetReached)
                {
                    int jogStep = Math.Max(1, profVel / 10);
                    a.DesiredPosition = input.PositionActualValue + (a.JogForward ? jogStep : -jogStep);
                    a.TriggerMove = true;
                }

                switch (a.PpState)
                {
                    case 0:
                        cw = 0x0F;
                        output.TargetPosition = input.PositionActualValue;
                        if (a.TriggerMove) { output.TargetPosition = a.DesiredPosition; a.PpState = 1; a.MotionCycles = 0; }
                        break;
                    case 1:
                        cw = 0x0F; output.TargetPosition = a.DesiredPosition;
                        if (++a.MotionCycles >= 10) { a.PpState = 2; a.MotionCycles = 0; }
                        break;
                    case 2:
                        cw = 0x1F; output.TargetPosition = a.DesiredPosition; a.MotionCycles++;
                        if (setPointAck) { a.PpState = 3; a.MotionCycles = 0; }
                        else if (a.MotionCycles >= 3000) { a.PpState = 0; a.TriggerMove = false; }
                        break;
                    case 3:
                        cw = 0x0F; a.MotionCycles++;
                        if (!setPointAck) { a.PpState = 4; a.MotionCycles = 0; }
                        else if (a.MotionCycles >= 500) { a.PpState = 0; a.TriggerMove = false; }
                        break;
                    case 4:
                        cw = 0x0F; a.MotionCycles++;
                        if (targetReached) { a.PpState = 0; a.TriggerMove = false; }
                        break;
                    default: cw = 0x0F; break;
                }
            }
            output.ControlWord = cw;
            a.SnapTargetPosition = a.DesiredPosition;
        }

        // ==================== UI 刷新 ====================

        void StatusUpdateLoop()
        {
            while (isRunning)
            {
                try
                {
                    var arr = axes;
                    BeginInvoke(new Action(() =>
                    {
                        for (int i = 0; i < arr.Length && i < gridAxes.Rows.Count; i++)
                        {
                            var a = arr[i];
                            ushort sw = a.SnapStatusWord;
                            bool reached = (sw & 0x0400) != 0;
                            var row = gridAxes.Rows[i];
                            row.Cells[COL_STATE].Value = a.SnapDriveState + (reached ? " [到位]" : "");
                            row.Cells[COL_SW].Value = $"0x{sw:X4}";
                            row.Cells[COL_ERR].Value = $"0x{a.SnapErrorCode:X4}";
                            row.Cells[COL_ACTPOS].Value = $"{a.SnapActualPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapActualPosition})";
                            row.Cells[COL_TGTPOS].Value = $"{a.SnapTargetPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapTargetPosition})";
                            row.Cells[COL_VEL].Value = a.SnapActualVelocity.ToString();
                            row.Cells[COL_REACHED].Value = reached ? "✓" : "";
                            row.Cells[COL_ENABLED].Value = a.ServoEnabled ? "ON" : "off";

                            var stateCell = row.Cells[COL_STATE];
                            if (IsFault(sw) || a.SnapErrorCode != 0) stateCell.Style.ForeColor = Color.Firebrick;
                            else if (IsOperationEnabled(sw)) stateCell.Style.ForeColor = Color.SeaGreen;
                            else stateCell.Style.ForeColor = Color.DimGray;
                        }
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

        // ==================== 断开 ====================

        void btnDisconnect_Click(object sender, EventArgs e) { Disconnect(); }

        // 硬 join 两个工作线程 (PDO + 状态), 各最多等 500ms。StatusUpdateLoop 已改 BeginInvoke,
        // 故 UI 线程在此 join 不会与其 marshal 形成死锁; join 后再 Close 可消除 native 指针 use-after-free。
        void JoinWorkers()
        {
            try { _pdoThread?.Join(500); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[JoinWorkers] PDO join 异常: {ex.Message}"); }
            try { _statusThread?.Join(500); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[JoinWorkers] Status join 异常: {ex.Message}"); }
            _pdoThread = null;
            _statusThread = null;
        }

        void Disconnect()
        {
            try { _connectCts?.Cancel(); } catch (ObjectDisposedException) { } catch (Exception ex) { Console.WriteLine($"[Disconnect] CTS Cancel 异常: {ex.Message}"); }

            foreach (var a in axes) { a.ServoEnabled = false; a.JogForward = a.JogReverse = false; a.TriggerMove = false; }
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
            cboMode.Enabled = true;
            grpGlobal.Enabled = false;
            grpAxis.Enabled = false;
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            lblAxisCount.Text = "轴数: -";
            lblSelAxis.Text = "选定轴: -";
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
