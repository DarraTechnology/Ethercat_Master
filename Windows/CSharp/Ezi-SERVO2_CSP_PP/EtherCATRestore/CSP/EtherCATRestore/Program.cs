// Darra EtherCAT Master C# 调用案例
// 导出时间: 2026-03-11 02:00:52
// 主站状态: OP  从站数量: 1

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using DarraEtherCAT_Master;
using static DarraEtherCAT_Master.LogCategory;

namespace EtherCATRestore
{
    // 从站 1: Ezi-SERVO2 - 输出 PDO (12 字节)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Slave1_OutputPDO
    {
        public ushort ControlWord; // Control Word 0x6040:0
        public int TargetPosition; // Target Position 0x607A:0
        public uint PhysicalOutputs; // Physical outputs 0x60FE:1
        public ushort TouchProbeFunction; // Touch probe function 0x60B8:0
    }

    // 从站 1: Ezi-SERVO2 - 输入 PDO (22 字节)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Slave1_InputPDO
    {
        public ushort StatusWord; // Status Word 0x6041:0
        public int PositionActualValue; // Position Actual Value 0x6064:0
        public uint DigitalInputs; // Digital Inputs 0x60FD:0
        public ushort ErrorCode; // Error Code 0x603F:0
        public ushort TouchProbeStatus; // Touch probe status 0x60B9:0
        public int TouchProbe1PositiveValue; // Touch probe 1 positive value 0x60BA:0
        public int TouchProbe2PositiveValue; // Touch probe 2 positive value 0x60BC:0
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
                    throw new FileNotFoundException($"配置文件不存在: {deniPath}");

                // 流式 API 初始化主站
                // SetENI: 加载 DENI 配置 (包含网口、从站、DC、启动参数等)
                // EnableAutoStartup: 自动配置 DENI 中未覆盖的从站
                // Build: 执行初始化，扫描从站
                // 如需覆盖 DENI 中的网口，在 SetENI 之后调用:
                //   .SetNetwork("网口名称")
                var result = new DarraEtherCAT()
                    .SetENI(deniPath)
                    .EnableAutoStartup()
                    .Build();
                if (!result.Success) throw new Exception($"主站初始化失败: {result.Message}");
                master = result;
                Console.WriteLine($"主站初始化完成, 从站数量: {master.SlaveCount}");

                // ── 事件注册 (在状态转换前注册，确保捕获转换过程中的事件) ──

                // 主站状态变化 (Init → PreOp → SafeOp → OP，含异常降级)
                master.Events.StateChanged += (s, e) =>
                    Console.WriteLine($"[状态] 主站: {e.OldState} → {e.NewState}");

                // 从站状态变化
                master.Events.SlaveStateChanged += (masterIdx, slaveIdx, oldState, newState) =>
                    Console.WriteLine($"[状态] 从站{slaveIdx}: {oldState} → {newState}");

                // 紧急事件 (从站发送 Emergency 报文)
                master.Events.EmergencyEvent += (masterIdx, slaveIdx, code, reg, b1, w1, w2) =>
                    Console.WriteLine($"[紧急] 从站{slaveIdx} 错误码: 0x{code:X4} 寄存器: 0x{reg:X4}");

                // 从站离线/上线 (热插拔)
                master.Events.SlaveOffline += (slaveIdx) =>
                    Console.WriteLine($"[离线] 从站{slaveIdx} 已断开");
                master.Events.SlaveOnline += (slaveIdx) =>
                    Console.WriteLine($"[上线] 从站{slaveIdx} 已恢复");

                // PDO 连续丢帧告警
                master.Events.PDOFrameLoss += (masterIdx, group, consecutive, total) =>
                    Console.WriteLine($"[丢帧] 组{group} 连续丢帧: {consecutive}, 累计: {total}");

                // DC 同步丢失告警 (使用 DC 时按需启用)
                // master.Events.DCSyncLost += (masterIdx, slaveIdx, diffNs) =>
                //     Console.WriteLine($"[DC] 从站{slaveIdx} 同步偏差: {diffNs}ns");

                // 异常处理
                master.RegisterExceptionHandler((idx, msg, ex) =>
                    Console.WriteLine($"[异常] {msg}"));

                // ── 日志监听 ──
                // 过滤器: 仅显示错误、警告、一般信息 (可按需添加 Mailbox, PDO, Debug)
                DarraEtherCAT.Logs.SetFilter(LogCategory.Error, LogCategory.Warning, LogCategory.Message);
                DarraEtherCAT.Logs.Updated += () =>
                {
                    foreach (var entry in DarraEtherCAT.Logs)
                        Console.WriteLine($"[日志] {entry}");
                };

                // ── 诊断采集 (会影响性能，按需启用) ──
                // master.Diagnostics.Enabled = true;

                // ── 状态转换 (DENI 中的启动参数在状态转换时自动应用) ──
                {
                    var (ok, msg) = master.SetState(EcState.PreOp);
                    if (!ok) throw new Exception($"切换到 PreOp 失败: {msg}");
                    Console.WriteLine("  → PreOp 完成");
                }
                {
                    var (ok, msg) = master.SetState(EcState.SafeOp);
                    if (!ok) throw new Exception($"切换到 SafeOp 失败: {msg}");
                    Console.WriteLine("  → SafeOp 完成");
                }
                {
                    var (ok, msg) = master.SetState(EcState.OP);
                    if (!ok) throw new Exception($"切换到 OP 失败: {msg}");
                    Console.WriteLine("  → OP 完成");
                }
                Console.WriteLine($"主站已进入 OP 状态");

                // PDO 映射 (ref 引用指向共享内存，地址固定，只需获取一次)
                ref var input1 = ref master.Slaves[0].PDO.InputsMapping<Slave1_InputPDO>();
                ref var output1 = ref master.Slaves[0].PDO.OutputsMapping<Slave1_OutputPDO>();

                // PDO 读写循环
                Console.WriteLine("进入 PDO 循环 (Ctrl+C 退出)...\n");
                while (running)
                {
                    // 示例: output1.ControlWord = 0x0F;
                    // 示例: var status = input1.StatusWord;

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
