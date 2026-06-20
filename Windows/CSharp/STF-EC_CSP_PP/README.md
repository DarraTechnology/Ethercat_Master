# STF-EC 多轴步进驱动器 CSP/PP 运动控制

基于 Darra EtherCAT Master 类库的 **STF-EC 步进驱动器** *多轴* 运动控制案例，WinForms 界面，支持 **CSP** (周期同步位置) 和 **PP** (轮廓位置) 两种模式。

**轴数由 `config.deni` 实际扫描到的从站数量决定** (`master.SlaveCount`)，界面按轴数自动生成总览表；随附的 `config.deni` 含 **5 个 STF-EC** (PhysAddr 0x1001~0x1005)。

界面分三块：**轴总览表** (逐轴 驱动状态/状态字/错误码/实际位置/目标位置/速度/到位/使能, 可勾选) + **全局控制** (全部使能/去使能/故障复位/回零/急停, 作用于勾选轴) + **选定轴控制** (单轴 使能/点动/定位/回零)。

> 风格与同目录 `Ezi-SERVO2_CSP_PP` 案例一致 (同一套 CiA 402 状态机 + PDO 控制线程 + 异步日志)，按 STF-EC 的对象字典和 PDO 布局适配，并扩展为多轴。

## 设备信息

| 项 | 值 |
|----|----|
| 厂商 | Shanghai AMP&MOONS' Automation (鸣志) |
| VendorId | `0x00000168` |
| 型号 | STF-EC |
| ProductCode | `0x02` |
| 轴数 | 随附 config.deni 含 5 轴 (实际以扫描为准, `master.SlaveCount`) |
| 协议 | CoE (CiA 402, Profile 402) |
| ESI | `AMA Stepper EtherCAT v3.1.6.xml` |

## 运动模式

### CSP (周期同步位置, Mode = 8)
- 主站每周期下发目标位置 (`0x607A`)，驱动器跟踪执行；点动/定位由主站做位置插值。
- 需要 DC Sync0 同步 (1ms 周期)，连接时由 `slave.ConfigureDC(1000000)` 启用。

### PP (轮廓位置, Mode = 1)
- 驱动器内部生成运动轨迹，主站下发目标位置 (`0x607A`) + 轮廓速度 (`0x6081`)。
- 使用 Free Run 同步 (SM2/SM3 SyncType=0)，无需 DC Sync，适合点到点定位。

## DENI 的 PDO 支持哪些运行模式

`config.deni` 的默认 PDO 同时映射了 **目标位置 0x607A** 和 **目标速度 0x60FF**，加上完整的 控制字/状态字/模式字/实际位置/实际速度，所以这一份 PDO **不改映射** 即可驱动 CiA 402 的 **6 种模式** (由 `0x6060` 选择)：

| 模式 | 0x6060 | 关键 PDO 对象 | 本 PDO | 同步 |
|------|--------|---------------|--------|------|
| PP 轮廓位置 | 1 | 0x607A + 0x6081/0x6083/0x6084 | ✅ | Free Run |
| PV 轮廓速度 | 3 | 0x60FF + 0x6083/0x6084 | ✅ | Free Run |
| HM 回零 | 6 | 0x6040 bit4 + 0x6041；0x6098/0x6099/0x609A 经 SDO | ✅ | Free Run |
| CSP 周期同步位置 | 8 | 0x607A (每周期下发) | ✅ | DC Sync0 |
| CSV 周期同步速度 | 9 | 0x60FF (每周期下发) | ✅ | DC Sync0 |
| Q 厂商程序 | -1 | 0x6060=-1 + 0x6040 启动；0x2007 段号经 SDO | ✅ | Free Run |

**唯一不支持**：轮廓扭矩 PT (Mode=4) —— 需要映射 **0x6071 Target torque** (本 PDO 未映)，且仅 **StepSERVO (SSDC)** 才有扭矩模式，STF 纯步进的 `0x6502 Supported drive modes` 不含扭矩位。

> 本示例界面默认提供 **CSP / PP** 两种 (最常用)。要跑 PV/CSV/HM/Q，只需把 `0x6060` 写成对应值 + 走相应控制时序，**PDO 不用改、config.deni 不用重导**。

## 为什么只有一份 config.deni

与 Ezi-SERVO2 (CSP/PP 用两份 config) 不同：**STF-EC 的默认 PDO 分配同时包含 CSP 与 PP 需要的全部对象**，所以 **CSP/PP 共用同一份 config.deni**，模式在运行时切换：

- CSP：连接时 `ConfigureDC(1000000)` 启用 Sync0，`Modes of Operation (0x6060)` = 8。
- PP：连接时写 `0x1C32:1 = 0` / `0x1C33:1 = 0` 切 Free Run，`0x6060` = 1。

`0x6060` (运行模式) 在 RxPDO 0x1600 中，每周期由 PDO 直接下发，无需额外 SDO。

## config.deni (随附 5 轴版, 可换成你自己的)

随附的 `config.deni` 已含 **5 个 STF-EC**，放在 `EtherCATRestore/EtherCATRestore/config.deni`，编译后由通配符自动复制到 `bin\Debug\EtherCATRestore\`，可直接加载运行。

换成你现场的拓扑时：用 **Darra 主站 GUI 扫描真实从站后导出** 覆盖它 (不能手工编写——带 SHA256 校验 + 逐字节 PDO/FMMU 启动指令，必须对应真实从站)。**轴数变了界面会自动适配**，无需改代码。

导出时保持 STF-EC **默认 PDO 分配** (本 config.deni 实测):
- RxPDO (`0x1C12`) = `0x1600 + 0x1601 + 0x1602 + 0x1603` = **29 字节**
- TxPDO (`0x1C13`) = `0x1A00 + 0x1A01 + 0x1A02 + 0x1A03` = **35 字节** (含 4 个 Touch Probe 位置值)

> 若改了 PDO 分配，必须同步修改 `MainForm.cs` 中的 `STF_Output` / `STF_Input` 结构体，使字段顺序逐字节对应。

## PDO 映射 (STF-EC 默认, CSP/PP 共用)

| 方向 | PDO | 条目 | 字节 |
|------|-----|------|------|
| RxPDO 输出 | 0x1600 | ControlWord(0x6040) + ModesOfOperation(0x6060) + TargetPosition(0x607A) | 7 |
| | 0x1601 | ProfileVelocity(0x6081) + ProfileAcceleration(0x6083) + ProfileDeceleration(0x6084) | 12 |
| | 0x1602 | TargetVelocity(0x60FF) | 4 |
| | 0x1603 | PhysicalOutputs(0x60FE:1) + TouchProbeFunction(0x60B8) | 6 |
| TxPDO 输入 | 0x1A00 | ErrorCode(0x603F) + StatusWord(0x6041) + ModesOfOperationDisplay(0x6061) | 5 |
| | 0x1A01 | PositionActualValue(0x6064) | 4 |
| | 0x1A02 | VelocityActualValue(0x606C) | 4 |
| | 0x1A03 | DigitalInputs(0x60FD) + TouchProbeStatus(0x60B9) + TouchProbe1/2 Pos/Neg(0x60BA~0x60BD) | 18 |

> **TxPDO 共 35 字节** (不是早期文档写的 19) —— 0x1A03 实际还映了 4 个 Touch Probe 位置值 (0x60BA~0x60BD)，已据本 `config.deni` 实测在 `STF_Input` 补齐。
> 注意 0x1A00 中 **错误码在前、状态字在后**，结构体 `STF_Input` 已照此排列。

## 对象字典索引 (CiA 402)

| 用途 | 索引 | 说明 |
|------|------|------|
| 控制字 | 0x6040 | 0x06 Shutdown → 0x07 Switch On → 0x0F Enable Operation；0x80 故障复位；PP 新设定点 bit4 (0x1F) |
| 状态字 | 0x6041 | 解析 CiA402 驱动状态；bit10 到位，bit12 设定点应答 |
| 运行模式 | 0x6060 | 本例 CSP=8 / PP=1 (经 RxPDO 下发); PDO 同样支持 PV=3 / HM=6 / CSV=9 / Q=-1, 见上「运行模式」 |
| 目标位置 | 0x607A | 绝对/相对目标 |
| 实际位置 | 0x6064 | 反馈位置 |
| 实际速度 | 0x606C | 反馈速度 |
| 轮廓速度 | 0x6081 | PP 模式速度 |
| 轮廓加/减速度 | 0x6083 / 0x6084 | PP 模式加减速 |
| 错误码 | 0x603F | 驱动错误码 |
| 同步误差计数上限 | 0x10F1:02 | 非实时主站放宽到 65535 容忍偶发丢帧 |

## 功能

- 连接 / 断开 (走 Init → PreOp → SafeOp → OP 状态转换), 一次连接全部轴
- **多轴总览表**: 逐轴实时 驱动状态 / 状态字 / 错误码 / 实际位置 / 目标位置 / 速度 / 到位 / 使能; Fault 或非零错误码红字标记
- **全局控制** (作用于勾选轴, 未勾选 = 全部轴): 全部使能 / 全部去使能 / 全部故障复位 / 全部回零 / 全部急停
- **选定轴控制** (点击总览表选行): 单轴 CiA 402 使能 (0x06 → 0x07 → 0x0F) / 去使能 / 故障复位 (0x80)
- 点动 Jog：按住正转/反转，松手即停 (作用于选定轴)
- 定位：输入角度，选绝对/相对运行 (作用于选定轴)；回零 (目标位置 0)
- CSP 步/周期、PP 轮廓速度 **逐轴独立** 缓存可调
- 异步日志系统 (避免高频 Invoke 卡 UI)

## 安全约定 (会真实驱动电机)

- 连接/上电后 **不会自动运动**，目标位置初始化为当前实际位置。
- **必须用户显式点击「使能」** 才进入 OperationEnabled，之后才能运动。
- 点动 **松手即停**。

## 用到的 SDK API (Darra EtherCAT Master 类库)

| 类别 | API |
|------|-----|
| 主站构建 | `new DarraEtherCAT().SetENI(deni).EnableAutoStartup().Build()` |
| 网卡覆盖 | `.SetNetwork(name, null)` (可选, 默认用 DENI 中网口) |
| 状态转换 | `master.SetState(EcState.PreOp / SafeOp / OP)` |
| DC 同步 | `slave.HasDC` / `slave.ConfigureDC(1000000)` |
| SDO 读写 | `slave.CoE.SDOWrite(idx, sub, bytes)` / `slave.CoE.SDORead(idx, sub)` |
| PDO 映射 | `slave.PDO.InputsMapping<T>()` / `slave.PDO.OutputsMapping<T>()` (ref 共享内存) |
| 事件 | `master.Events.StateChanged / SlaveStateChanged / EmergencyEvent / SlaveOffline / SlaveOnline / PDOFrameLoss` |
| 中止/关闭 | `DarraEtherCAT.Abort()` / `master.Close()` |

## 项目结构

```
STF-EC_CSP_PP/
├── MainForm.cs              # 主窗体 (STF_Output/STF_Input 结构体 + CiA 402 状态机 + PDO 控制线程)
├── MainForm.Designer.cs     # 界面布局
├── Program.cs               # 程序入口
├── STF-EC_CSP_PP.csproj     # .NET Framework 4.7.2 (x64), NuGet Darra.EtherCAT.Master 2.3.0
├── config.deni             # 随附的 5 轴 STF-EC 配置 (源副本)
├── EtherCATRestore/
│   └── EtherCATRestore/     # 独立的多轴状态恢复控制台 (net8.0) + lib DLL
│       ├── config.deni      # 随附 5 轴版 (运行期读取; 换成你导出的即可)
│       ├── Program.cs
│       ├── run.bat
│       └── lib/
└── Properties/
```

## 依赖

- .NET Framework 4.7.2 (x64)
- NuGet 包 `Darra.EtherCAT.Master` 2.3.0 (含 native `Darra.Core.dll`，编译时自动复制)
- `EtherCATRestore/EtherCATRestore/config.deni` (从主站 GUI 扫描 STF-EC 导出，必须随程序一起部署)
- Npcap / WinPcap (用于网卡裸帧收发)

## 编译运行

1. 用 Visual Studio 打开 `STF-EC_CSP_PP.csproj` (平台目标固定 x64，native DLL 仅 x64)。
2. `config.deni` 已随附 (5 轴)；换自己的拓扑时用主站 GUI 扫描导出，覆盖 `EtherCATRestore/EtherCATRestore/config.deni`。
3. 编译，确认输出目录 `bin\Debug\` 下生成了 `EtherCATRestore\config.deni` 及 `Darra.Core.dll`。
4. 以 **管理员权限** 运行 (网卡裸帧需要)。
5. 选择模式 (CSP / PP) → 「连接」→ 总览表出现各轴 → 「全部使能」或选某轴「使能」→ 定位 / 点动。

> 提示：脉冲/角度换算默认 `PULSES_PER_REV = 10000` (STF-EC 默认细分 `0x2604 Steps per Rev`)。若驱动器细分不同，改 `MainForm.cs` 中该常量。
