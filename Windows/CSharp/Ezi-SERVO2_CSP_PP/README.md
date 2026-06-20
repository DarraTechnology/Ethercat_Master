# Ezi-SERVO2 CSP / PP 运动控制

基于 Darra EtherCAT Master 类库的 **FASTECH Ezi-SERVO2 伺服电机** 单轴运动控制案例，WinForms 界面，支持 **CSP** (周期同步位置) 与 **PP** (轮廓位置) 两种运动模式。

> 风格与同目录 STF-EC 多轴案例一致 (同一套 CiA 402 状态机 + PDO 控制线程 + 异步日志)，按 Ezi-SERVO2 的对象字典与 PDO 布局适配，聚焦**单轴 + 双模式**。

## 设备信息

| 项 | 值 |
|----|----|
| 厂商 | FASTECH |
| 型号 | Ezi-SERVO2 (EtherCAT) |
| 轴数 | 单轴 |
| 协议 | CoE (CiA 402, Profile 402) |
| 同步 | CSP: DC Sync0 1ms；PP: Free Run |
| 脉冲/圈 | `PULSES_PER_REV = 10000` (默认，按驱动器细分调整 `MainForm.cs` 常量) |
| SDK | NuGet `Darra.EtherCAT.Master` 2.7.0 |

## 运动模式

### CSP (周期同步位置, Mode = 8)
- 主站每周期下发目标位置 (`0x607A`)，驱动器跟踪执行；点动 / 定位的轨迹由主站做位置插值。
- 需要 **DC Sync0 同步** (1ms 周期)，连接时由 `slave.ConfigureDC(1000000)` 启用。
- 适用于高精度实时轨迹控制。

### PP (轮廓位置, Mode = 1)
- 驱动器内部生成运动轨迹，主站下发目标位置 (`0x607A`) + 轮廓速度参数。
- 使用 **Free Run** 同步，无需 DC Sync。
- 适用于点到点定位。

## 为什么需要两份 config.deni (与 STF-EC 不同)

与 STF-EC (CSP / PP 共用一份 config) 不同：**Ezi-SERVO2 的 CSP 与 PP 使用不同的 PDO 映射** (CSP 用 `0x1600 / 0x1A00`，PP 用 `0x1601 / 0x1A01`)，因此本案例为两种模式各配一份 `config.deni`：

```
EtherCATRestore/
├── CSP/    # CSP 模式: config.deni (RxPDO 0x1600 / TxPDO 0x1A00) + lib DLL
└── PP/     # PP  模式: config.deni (RxPDO 0x1601 / TxPDO 0x1A01) + lib DLL
```

运行时按所选模式加载对应目录下的 `config.deni`。换成你现场的拓扑时，用 **Darra 主站 GUI** 扫描真实从站后**分别导出 CSP / PP 两份**覆盖 —— `config.deni` 不可手工编辑 (带 SHA-256 校验 + 逐字节 PDO/FMMU 启动指令，必须对应真实从站)。

## PDO 映射

| 模式 | RxPDO (输出) | TxPDO (输入) |
|------|-------------|-------------|
| CSP | `0x1600`: ControlWord + TargetPosition + PhysicalOutputs + TouchProbeFunction (**12 字节**) | `0x1A00`: StatusWord + PositionActualValue + DigitalInputs + ErrorCode + TouchProbeStatus + TouchProbe1/2 PositiveValue (**22 字节**) |
| PP | `0x1601`: ControlWord + TargetPosition + ProfileVelocity + ModesOfOperation (**11 字节**) | `0x1A01`: StatusWord + PositionActualValue + VelocityActualValue + DigitalInputs + ErrorCode + ModesOfOperationDisplay (**17 字节**) |

> 字段顺序与 `MainForm.cs` 中的 `CSP_Output / CSP_Input / PP_Output / PP_Input` 结构体逐字节对应；改了 PDO 分配必须同步改结构体。

## 对象字典索引 (CiA 402)

| 用途 | 索引 | 说明 |
|------|------|------|
| 控制字 | 0x6040 | 0x06 Shutdown → 0x07 Switch On → 0x0F Enable Operation；0x80 故障复位；PP 新设定点 bit4 |
| 状态字 | 0x6041 | 解析 CiA 402 驱动状态；bit10 到位，bit12 设定点应答 |
| 运行模式 | 0x6060 | CSP = 8 / PP = 1 |
| 目标位置 | 0x607A | 绝对 / 相对目标 |
| 实际位置 | 0x6064 | 反馈位置 |
| 实际速度 | 0x606C | 反馈速度 (PP 的 TxPDO) |
| 轮廓速度 | 0x6081 | PP 模式速度 |
| 错误码 | 0x603F | 驱动错误码 |

## 功能

- CiA 402 驱动状态机 (使能 / 去使能 / 故障复位)
- 绝对 / 相对位置运动
- 点动 (Jog) 控制 (松手即停)
- 回零 (目标位置 0)
- 实时状态显示 (StatusWord / 位置 / 速度 / 错误码)
- 异步日志系统 (避免高频 Invoke 卡 UI)

## 安全约定 (会真实驱动电机)

- 连接 / 上电后**不会自动运动**，目标位置初始化为当前实际位置。
- **必须用户显式点击「使能」** 才进入 OperationEnabled，之后才能运动。
- 点动**松手即停**。

## 用到的 SDK API (Darra EtherCAT Master 类库)

| 类别 | API |
|------|-----|
| 主站构建 | `new DarraEtherCAT().SetENI(deni).EnableAutoStartup().Build()` |
| 状态转换 | `master.SetState(EcState.PreOp / SafeOp / OP)` |
| DC 同步 | `slave.HasDC` / `slave.ConfigureDC(1000000)` (CSP) |
| PDO 映射 | `slave.PDO.InputsMapping<T>()` / `slave.PDO.OutputsMapping<T>()` (ref 共享内存，每周期取) |
| 事件 | `master.Events.StateChanged / SlaveStateChanged / EmergencyEvent / SlaveOffline / SlaveOnline / PDOFrameLoss` |
| 中止 / 关闭 | `DarraEtherCAT.Abort()` / `master.Close()` |

## 项目结构

```
Ezi-SERVO2_CSP_PP/
├── MainForm.cs              # 主窗体 (CSP/PP 结构体 + CiA 402 状态机 + PDO 控制线程)
├── MainForm.Designer.cs     # 界面布局
├── Program.cs               # 程序入口
├── Ezi-SERVO2 Test.csproj   # .NET Framework 4.7.2 (x64), NuGet Darra.EtherCAT.Master 2.7.0
├── EtherCATRestore/
│   ├── CSP/                 # CSP 模式: config.deni + lib DLL
│   └── PP/                  # PP  模式: config.deni + lib DLL
└── Properties/
```

## 依赖

- .NET Framework 4.7.2 (x64)
- NuGet 包 `Darra.EtherCAT.Master` 2.7.0 (含原生 `Darra.Core.dll`，编译时自动复制)
- 对应模式的 `config.deni` (从主站 GUI 扫描 Ezi-SERVO2 导出，必须随程序部署)
- 管理员权限运行 (网卡裸帧收发)

## 编译运行

1. 用 Visual Studio 打开 `Ezi-SERVO2 Test.csproj` (平台目标固定 x64，原生 DLL 仅 x64)。
2. 确认 `EtherCATRestore/CSP/` 或 `EtherCATRestore/PP/` 下有正确的 `config.deni` 和 DLL。
3. 编译，确认 `bin\Debug\` 下生成了对应的 `EtherCATRestore` 与 `Darra.Core.dll`。
4. 以**管理员权限**运行。
5. 选择模式 (CSP / PP) → 「连接」→ 「使能」→ 定位 / 点动。

> 提示：脉冲 / 角度换算默认 `PULSES_PER_REV = 10000`。若驱动器细分不同，改 `MainForm.cs` 中该常量。
