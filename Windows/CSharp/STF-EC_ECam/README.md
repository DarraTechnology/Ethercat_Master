# STF-EC 电子凸轮 (Electronic Cam)

基于 Darra EtherCAT Master 类库的 **STF-EC 步进驱动器** *电子凸轮* 演示案例，WinForms 界面。一根**虚拟主轴**驱动多根从轴，每根从轴的位置按设定的**凸轮曲线**跟随主轴相位运动 —— 这就是包装、印刷、飞剪、灌装等场合里机械凸轮的电子化替代。

**轴数由 `config.deni` 实际扫描到的从站数量决定** (`master.SlaveCount`)，界面按轴数自动生成总览表；随附的 `config.deni` 含 **5 个 STF-EC** (PhysAddr 0x1001~0x1005)，可全部当作凸轮从轴。

> 风格与同目录 `STF-EC_CSP_PP` 多轴案例一致 (同一套 CiA 402 状态机 + PDO 控制线程 + 异步日志 + PDO 重映与尺寸自检)，本例把"逐轴 CSP/PP 点动定位"换成"逐轴 CSP 跟随虚拟主轴"。

## 电子凸轮原理

机械凸轮：主轴每转一圈，从动件按凸轮轮廓做一段确定的位移 —— 从动件位移是主轴转角的函数 `s = f(θ)`。
电子凸轮就是把这条凸轮轮廓做成**数学曲线 / 凸轮表 (cam table)**，由控制器实时计算下发：

- **虚拟主轴 (Master)**：本例不用真实编码器，用软件相位 `masterPhase ∈ [0,1)` 当主轴，每个 PDO 周期 (~1ms) 推进 `Δphase = 主轴RPM / 60 / 1000`，到 1 自动回 0 循环。一圈 = 相位 0→360°。
- **凸轮表 / 插值**：从轴位移 = `Cam(曲线, 主轴相位)`。本例用解析曲线函数代替离散凸轮表 (相当于无限细分 + 连续插值)，避免查表/线性插值的台阶。
- **挂载 (Engage)**：从轴勾选"挂载"才跟随主轴；脱开则停在原地。挂载一刻**快照当前实际位置作为基准 `Base`**，从轴目标 = `Base + Cam(...) * 行程`，所以挂载/启动**无跳变**。
- **相位偏移 (Phase Offset)**：每根从轴可设各自的相位偏移 (度)，让多轴在凸轮上错开 —— 例如 5 轴各偏 0/72/144/216/288° 即在一个周期内均匀分布。
- **曲线类型 + 行程**：全局选曲线 (正弦/摆线/直线) 与行程 (脉冲)，曲线给出 -1..1 的归一化位移，乘以行程得到实际脉冲偏移。

从轴目标位置公式 (每周期, 每挂载轴)：

```
output.TargetPosition = Base + (int)Round( Cam(曲线, frac(masterPhase + 相位偏移/360)) * 行程 )
```

## 为什么只用 CSP (Mode = 8)

电子凸轮要求**每个控制周期都精确下发一个新目标位置**，并与总线周期硬同步 —— 这正是 **CSP 周期同步位置** 的语义：

- 主站每周期 (1ms) 把算好的凸轮目标位置写入 `0x607A`，驱动器只做位置跟随，**轨迹由主站算**。
- 需要 **DC Sync0 (1ms)** 把各从站时钟对齐到同一节拍，多轴凸轮才不会彼此漂移；连接时 `slave.ConfigureDC(1000000)` 启用。
- PP (轮廓位置, Mode=1) 是驱动器**内部生成轨迹**的点到点定位，主站给不了逐周期的凸轮形状，**不适合凸轮**，本例不提供 PP 分支。

## 主站只算 CSP 跟随

主站职责很轻：① 推进虚拟主轴相位；② 对每根挂载从轴，用其相位偏移取曲线值算目标位置；③ 维持 CiA 402 使能握手 (`0x06 → 0x07 → 0x0F`，故障 `0x80`) 并把目标位置写进 RxPDO。不做内部插补、不做轨迹规划 —— 凸轮形状完全由曲线函数决定。

## 三种凸轮曲线 (`Cam(type, u)`，u = 主轴相位 ∈ [0,1)，返回 -1..1)

| 类型 | 名称 | 公式 | 特点 |
|------|------|------|------|
| 0 | 正弦 Sine | `sin(2πu)` | 一圈内一个完整正弦往复，加减速平滑，常用 |
| 1 | 摆线 Cycloidal | 0→0.5 平滑上升 0→1，0.5→1 镜像回落 1→0，再映射到 -1..1 | 起停加速度连续 (无冲击)，高速凸轮首选 |
| 2 | 直线/电子齿轮 | `2u-1` (线性斜坡) | 位移与主轴成正比 = **电子齿轮 (1:1 比例跟随)** 的特例 |

> **同步轴 = 凸轮的直线特例**：当凸轮曲线取"直线"时，从轴位移与主轴相位严格成比例，这就退化为电子齿轮 / 同步轴 —— 所以"电子齿轮"只是凸轮曲线里最简单的一根直线。

界面右侧 `凸轮设置` 面板实时绘制当前选中曲线的预览 (位移 = f(主轴相位))。

## 设备信息

| 项 | 值 |
|----|----|
| 厂商 | Shanghai AMP&MOONS' Automation (鸣志) |
| VendorId | `0x00000168` |
| 型号 | STF-EC |
| ProductCode | `0x02` |
| 轴数 | 随附 config.deni 含 5 轴 (实际以扫描为准, `master.SlaveCount`) |
| 协议 | CoE (CiA 402, Profile 402) |
| 同步 | DC Sync0 1ms (CSP 必需) |

## PDO 映射 (STF-EC 默认，与 STF-EC_CSP_PP 同)

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

> **RxPDO 输出 29 字节 / TxPDO 输入 35 字节**。0x1A00 中错误码在前、状态字在后，结构体 `STF_Input` 已照此排列。连接时经 CoE 把 `0x1C12 / 0x1C13` 重映成完整 4+4，并用 `OutputsByteCount / InputsByteCount` 与结构体 `Marshal.SizeOf` 自检。

## config.deni 用法 (同 STF-EC_CSP_PP)

随附的 `config.deni` 已含 **5 个 STF-EC**，放在 `EtherCATRestore/` 下，编译后由通配符自动复制到 `bin\Debug\EtherCATRestore\config.deni`，可直接加载运行。

换成你现场的拓扑时：用 **Darra 主站 GUI 扫描真实从站后导出** 覆盖它 (不能手工编写——带 SHA256 校验 + 逐字节 PDO/FMMU 启动指令，必须对应真实从站)。导出时保持 STF-EC **默认 PDO 分配** (RxPDO 29 / TxPDO 35)。**轴数变了界面会自动适配**，无需改代码；若改了 PDO 分配，必须同步修改 `MainForm.cs` 的 `STF_Output / STF_Input` 结构体。

## 操作步骤

1. **连接**：点「连接」，走 Init → PreOp → SafeOp → OP，逐轴启用 DC Sync0，进入 CSP。
2. **全部使能**：在「全局控制」点「全部使能」(未勾选「选」列 = 作用全部轴)，各轴进入 OperationEnabled。
3. **选曲线 + 行程**：右侧「凸轮设置」选曲线 (正弦/摆线/直线) 和行程脉冲；预览区即时显示曲线形状。
4. **(可选) 设相位偏移**：在轴总览表「相位偏移°」列给各轴填不同角度，让多轴在凸轮上错开。
5. **全部挂载**：点「全部挂载」(或逐轴勾「挂载」列)，把从轴挂到凸轮。
6. **启动凸轮**：在「虚拟主轴」设主轴 RPM，点「启动凸轮」—— 主轴相位开始推进，挂载轴按曲线跟随；「停止」让各轴停在当前位置，「相位归零」把主轴相位与基准重置 (无跳变)。

## 安全约定 (会真实驱动电机)

- 连接/上电后 **不会自动运动**：目标位置初始化为当前实际位置；未挂载或凸轮未运行时，从轴保持当前位置。
- **必须先「使能」再「挂载」再「启动凸轮」** 才会运动；挂载一刻快照 `Base`，跟随无跳变。
- 「停止」立即停止相位推进 (各轴保持)，「全部脱开 / 全部去使能」可随时退出跟随。

## 报警 / 诊断

界面底部「报警状态横幅 + 报警 / 诊断列表」实时反映总线与各凸轮从轴的健康状态。报警分三档严重度：**故障 (红)** / **警告 (橙)** / **信息 (灰)**；列表保留历史 (已恢复条目灰显)，故障源排除后点「报警复位」清理。

报警管理器线程安全：PDO 热路径只置 volatile 闩 + 事件回调线程入队，统一由 50ms UI 消费侧 `DrainPending` + `DetectAlarms` 决策呈现，生产侧不碰 UI。

### 报警类型

| 类型 | 来源 | 严重度 | 说明 |
|------|------|--------|------|
| 跟随误差 | PDO 闩 (50ms 决策) | 故障 | **挂载从轴 实际位置 vs 凸轮目标位置** 偏差连续超限 (去抖 ≈50ms)。阈值随凸轮速度自适应 (`静态值 + 4×每周期目标增量`)，避免高速/大行程误报 |
| 掉OP / 驱动故障 | PDO 闩 / 状态字 | 故障 | 凸轮运行中某轴掉出 OperationEnabled；或状态字 Fault / `0x603F` 错误码 |
| 掉站 / AL状态 | SDK 属性轮询 | 故障 | `slave.IsLost` / `State≠OP`；`slave.ErrorCode` (AL 状态) ≠ NoError |
| 链路断 | SDK 属性轮询 | 故障/警告 | 主链路断 = 故障；副链路断 / 主站链路降级 (PrimaryOnly/SecondaryOnly) = 警告 |
| DC失步 | SDK 属性轮询 | 警告 | `Diagnostics.DC.IsInSync=false`，多轴凸轮时钟漂移 |
| 主站掉OP / 丢帧 / WKC短缺 / 身份不符 / 紧急码 | 主站事件 / 主站级轮询 | 故障/警告 | 全局级 (主站) 报警；丢帧/WKC 连续超阈升为故障 |

### 组停 (Group Stop)

凸轮是多轴同步运动 —— 一旦某轴出现**故障级**报警，继续动其它轴会撕裂机构。故障触发 **组停**：

1. **停凸轮** (`_camRunning = false`，主轴相位停止推进)；
2. **全组去使能** (各轴钳到当前实际位置 + `cw=0x06` 安全停)；
3. **闭锁** (`_groupStopLatched`)：拦截「启动凸轮」「全部使能」「全部挂载」，需排除故障源后点「报警复位」解锁。
   「全部去使能 (急停)」「全部脱开」**永远可用**，不受闭锁限制。

> 「报警复位」不能 ack 掉**仍存在的活故障**：清掉激活闩后，下个 50ms 周期 `DetectAlarms` 会重新升起轮询类故障 (掉站/AL/链路/跟随误差) 并重新组停；纯事件型 (身份不符/主站掉OP/丢帧/紧急码) 因无周期 Clear 来源，由「报警复位」清。复位前若仍有轴处于驱动故障态，先做「全部故障复位」。

### 跟随误差的防误报 (grace)

凸轮"刚挂载/启动/改轨迹"时，从轴实际位置正追赶新目标，瞬态偏差大但属正常。为此每轴有 **grace 宽限** (≈200ms)：下列操作触发 grace 重置，宽限期内不判跟随误差：

- **启动凸轮** (所有已挂载轴)；
- **挂载 / 脱开** 切换 (总览表「挂载」列)；
- **改曲线** (曲线类型下拉)；
- **改行程** (行程脉冲)；
- **改相位偏移** (总览表「相位偏移°」列)。

> 同步类报警 (跟随误差/掉OP) 来自 PDO 热路径闩；通讯类报警 (掉站/链路/DC/WKC/丢帧) 来自 SDK 属性 + 主站事件轮询。两类经同一 `AlarmManager` 去重 (键 = `轴|类型`)，互不抢占。

## 用到的 SDK API (Darra EtherCAT Master 类库)

| 类别 | API |
|------|-----|
| 主站构建 | `new DarraEtherCAT().SetENI(deni).EnableAutoStartup().Build()` |
| 状态转换 | `master.SetState(EcState.PreOp / SafeOp / OP)` |
| DC 同步 | `slave.HasDC` / `slave.ConfigureDC(1000000)` |
| SDO 读写 | `slave.CoE.SDOWrite(idx, sub, bytes)` (PDO 重映 0x1C12/0x1C13) |
| PDO 尺寸 | `slave.OutputsByteCount` / `slave.InputsByteCount` (与结构体自检) |
| PDO 映射 | `slave.PDO.InputsMapping<T>()` / `slave.PDO.OutputsMapping<T>()` (ref 共享内存, 每周期取) |
| 事件 | `master.Events.StateChanged / SlaveStateChanged / EmergencyEvent / SlaveOffline / SlaveOnline / PDOFrameLoss / DCSyncLost / RedundancyModeChanged / SlavePortLinkChanged / SlaveIdentityMismatch` |
| 诊断 (报警轮询) | `slave.IsLost / State / ErrorCode (AL) / PrimaryLinkBroken / SecondaryLinkBroken / Diagnostics.DC.IsInSync`；`master.LinkState`；`DarraEtherCAT.GetGroupExpectedWKC / GetGroupActualWKC(MasterNumber, 1)` |
| 中止/关闭 | `DarraEtherCAT.Abort()` / `master.Close()` |

## 项目结构

```
STF-EC_ECam/
├── MainForm.cs              # 主窗体 (STF_Output/STF_Input + CiA402 + 虚拟主轴 + Cam() 曲线 + CSP 跟随线程 + AlarmManager 报警/诊断 + 组停闭锁)
├── MainForm.Designer.cs     # 界面布局 (虚拟主轴 / 凸轮设置+预览 / 轴总览 / 报警状态横幅+报警列表 / 全局控制 / 日志)
├── Program.cs               # 程序入口
├── STF-EC_ECam.csproj       # .NET Framework 4.7.2 (x64), NuGet Darra.EtherCAT.Master 2.3.0
├── config.deni              # 随附的 5 轴 STF-EC 配置 (源副本)
├── EtherCATRestore/         # 多轴状态恢复控制台 + 运行期 config.deni + lib DLL
└── Properties/
```
