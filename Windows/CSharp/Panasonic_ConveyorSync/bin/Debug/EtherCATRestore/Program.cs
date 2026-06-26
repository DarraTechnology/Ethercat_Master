// Darra EtherCAT Master C# 调用案例 (松下 MINAS A6B 伺服 状态恢复)
// 主站状态: OP  从站数量: 由 config.deni 决定 (随附配置含多个松下 A6B 伺服)
//
// 本程序加载 config.deni 把全部松下 A6B 伺服从站恢复到 OP 状态, 逐轴最小 PDO 循环示例。
// config.deni 需用主站 GUI 扫描真实松下 A6B 从站后导出, 放在本目录 (见同目录说明文件)。

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace EtherCATRestore
{
    // 松下 MINAS A6B 默认 PDO 分配 (RxPDO 0x1600 / TxPDO 0x1A00)
    // 来源 ESI: panasonic_minas-a6bf_v1_9_1_0_117_0.xml
    // 字段顺序必须与 PDO 条目顺序逐字节一致, 不可调换

    // 松下 A6B - 输出 PDO (9 字节, RxPDO 0x1600)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Output
    {
        public ushort ControlWord;          // Control Word 0x6040:0  UINT
        public sbyte ModesOfOperation;      // Modes of Operation 0x6060:0  SINT (CSP=8)
        public int TargetPosition;          // Target Position 0x607A:0  DINT
        public ushort TouchProbeFunction;   // Touch probe function 0x60B8:0  UINT
    }

    // 松下 A6B - 输入 PDO (23 字节, TxPDO 0x1A00)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PA_Input
    {
        public ushort ErrorCode;                 // Error Code 0x603F:0  UINT
        public ushort StatusWord;                // Status Word 0x6041:0  UINT
        public sbyte ModesOfOperationDisplay;    // Modes of Operation Display 0x6061:0  SINT
        public int PositionActualValue;          // Position Actual Value 0x6064:0  DINT
        public ushort TouchProbeStatus;          // Touch probe status 0x60B9:0  UINT
        public int TouchProbe1PosValue;          // Touch probe 1 pos value 0x60BA:0  DINT
        public int FollowingErrorActualValue;    // Following Error Actual Value 0x60F4:0  DINT
        public uint DigitalInputs;               // Digital Inputs 0x60FD:0  UDINT
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
                    throw new FileNotFoundException($"配置文件不存在: {deniPath} (请用主站 GUI 扫描松下 A6B 后导出 config.deni 放到本目录)");

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
                            ref var input = ref master.Slaves[i].PDO.InputsMapping<PA_Input>();
                            Console.WriteLine($"  轴{i + 1}: SW=0x{input.StatusWord:X4} Err=0x{input.ErrorCode:X4} Pos={input.PositionActualValue}");
                        }
                    }
                    // 示例: 驱动某轴 (按 CiA402 状态机递推 ControlWord 0x06->0x07->0x0F)
                    // ref var output = ref master.Slaves[0].PDO.OutputsMapping<PA_Output>();
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
