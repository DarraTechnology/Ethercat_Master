// Darra EtherCAT Master C# 调用案例 (STF-EC 多轴步进 状态恢复)
// 主站状态: OP  从站数量: 由 config.deni 决定 (随附配置含 5 个 STF-EC)
//
// 本程序加载 config.deni 把全部 STF-EC 从站恢复到 OP 状态, 逐轴最小 PDO 循环示例。
// config.deni 需用主站 GUI 扫描真实 STF-EC 从站后导出, 放在本目录 (见同目录说明文件)。

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace EtherCATRestore
{
    // STF-EC 默认 PDO 分配 (RxPDO 0x1600~0x1603 / TxPDO 0x1A00~0x1A03)
    // 字段顺序必须与 PDO 条目顺序逐字节一致

    // 从站 1: STF-EC - 输出 PDO (29 字节)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Slave1_OutputPDO
    {
        public ushort ControlWord;          // Control Word 0x6040:0
        public sbyte ModesOfOperation;      // Modes of Operation 0x6060:0
        public int TargetPosition;          // Target Position 0x607A:0
        public uint ProfileVelocity;        // Profile Velocity 0x6081:0
        public uint ProfileAcceleration;    // Profile Acceleration 0x6083:0
        public uint ProfileDeceleration;    // Profile Deceleration 0x6084:0
        public int TargetVelocity;          // Target Velocity 0x60FF:0
        public uint PhysicalOutputs;        // Physical outputs 0x60FE:1
        public ushort TouchProbeFunction;   // Touch probe function 0x60B8:0
    }

    // STF-EC - 输入 PDO (35 字节, 与 config.deni 实测逐字节一致)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Slave1_InputPDO
    {
        public ushort ErrorCode;                // Error Code 0x603F:0
        public ushort StatusWord;               // Status Word 0x6041:0
        public sbyte ModesOfOperationDisplay;   // Modes of Operation Display 0x6061:0
        public int PositionActualValue;         // Position Actual Value 0x6064:0
        public int VelocityActualValue;         // Velocity Actual Value 0x606C:0
        public uint DigitalInputs;              // Digital Inputs 0x60FD:0
        public ushort TouchProbeStatus;         // Touch probe status 0x60B9:0
        public int TouchProbe1PosValue;         // Touch probe pos1 pos 0x60BA:0
        public int TouchProbe1NegValue;         // Touch probe pos1 neg 0x60BB:0
        public int TouchProbe2PosValue;         // Touch probe pos2 pos 0x60BC:0
        public int TouchProbe2NegValue;         // Touch probe pos2 neg 0x60BD:0
    }

    class Program
    {
        static DarraEtherCAT master;
        static volatile bool running = true;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; running = false; };

            try
            {
                // 从命令行参数或默认路径加载 DENI 配置
                string deniPath = args.Length > 0 ? args[0] : "config.deni";
                if (!File.Exists(deniPath))
                    throw new FileNotFoundException($"配置文件不存在: {deniPath} (请用主站 GUI 扫描 STF-EC 后导出 config.deni 放到本目录)");

                // 流式 API 初始化主站
                // SetENI: 加载 DENI 配置 (包含网口、从站、DC、启动参数等)
                // EnableAutoStartup: 自动配置 DENI 中未覆盖的从站
                // Build: 执行初始化，扫描从站
                // 如需覆盖 DENI 中的网口，在 SetENI 之后调用 .SetNetwork("网口名称")
                var result = new DarraEtherCAT()
                    .SetENI(deniPath)
                    .EnableAutoStartup()
                    .Build();
                if (!result.Success) throw new Exception($"主站初始化失败: {result.Message}");
                master = result;
                Console.WriteLine($"主站初始化完成, 从站数量: {master.SlaveCount}");

                // ── 事件注册 (在状态转换前注册，确保捕获转换过程中的事件) ──
                master.Events.StateChanged += (s, e) =>
                    Console.WriteLine($"[状态] 主站: {e.OldState} → {e.NewState}");
                master.Events.SlaveStateChanged += (masterIdx, slaveIdx, oldState, newState) =>
                    Console.WriteLine($"[状态] 从站{slaveIdx}: {oldState} → {newState}");
                master.Events.EmergencyEvent += (masterIdx, slaveIdx, code, reg, b1, w1, w2) =>
                    Console.WriteLine($"[紧急] 从站{slaveIdx} 错误码: 0x{code:X4} 寄存器: 0x{reg:X4}");
                master.Events.SlaveOffline += (slaveIdx) =>
                    Console.WriteLine($"[离线] 从站{slaveIdx} 已断开");
                master.Events.SlaveOnline += (slaveIdx) =>
                    Console.WriteLine($"[上线] 从站{slaveIdx} 已恢复");
                master.Events.PDOFrameLoss += (masterIdx, group, consecutive, total) =>
                    Console.WriteLine($"[丢帧] 组{group} 连续丢帧: {consecutive}, 累计: {total}");

                // ── 日志监听 ──
                DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
                DarraEtherCAT.Logs.Updated += () =>
                {
                    foreach (var entry in DarraEtherCAT.Logs)
                        Console.WriteLine($"[日志] {entry}");
                };

                // ── 状态转换 (DENI 中的启动参数在状态转换时自动应用) ──
                if (!master.SetState(EcState.PreOp)) throw new Exception("切换到 PreOp 失败");
                Console.WriteLine("  → PreOp 完成");
                if (!master.SetState(EcState.SafeOp)) throw new Exception("切换到 SafeOp 失败");
                Console.WriteLine("  → SafeOp 完成");
                if (!master.SetState(EcState.OP)) throw new Exception("切换到 OP 失败");
                Console.WriteLine("  → OP 完成");
                Console.WriteLine($"主站已进入 OP 状态");

                // PDO 读写循环 (逐轴) — ref 引用指向共享内存, 每次循环按从站索引获取
                int n = master.SlaveCount;
                Console.WriteLine($"进入 PDO 循环, 共 {n} 轴 (Ctrl+C 退出)...\n");
                int tick = 0;
                while (running)
                {
                    // 每秒打印一次各轴状态字 / 错误码 / 实际位置 (只读, 不驱动电机)
                    if (++tick >= 1000)
                    {
                        tick = 0;
                        for (int i = 0; i < n; i++)
                        {
                            ref var input = ref master.Slaves[i].PDO.InputsMapping<Slave1_InputPDO>();
                            Console.WriteLine($"  轴{i + 1}: SW=0x{input.StatusWord:X4} Err=0x{input.ErrorCode:X4} Pos={input.PositionActualValue}");
                        }
                    }
                    // 示例: 驱动某轴 (按 CiA402 状态机递推 ControlWord 0x06->0x07->0x0F)
                    // ref var output = ref master.Slaves[0].PDO.OutputsMapping<Slave1_OutputPDO>();
                    // output.ControlWord = 0x0F; output.ModesOfOperation = 8; output.TargetPosition = ...;
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}");
            }
            finally
            {
                master?.Stop();
                master?.Close();
                master?.Dispose();
                Console.WriteLine("主站已关闭");
            }
        }
    }
}
