# Panasonic_ConveyorSync —— 传送带多轴协调 / 主从同步 + 物料跟踪

松下 **MINAS A6B** 伺服, **CSP (周期同步位置, Mode=8)** 主从同步演示: 一根**虚拟主轴**或**编码器主轴**驱动多条传送带 (从轴) 按各自**齿比 + 相位**跟随; 物料经传感器 (**Touch Probe** 位置捕获) 登记后随带推进, 进入**抓取区**时由抓取轴**飞行同步**抓取。对应物流分拣 / 产线对接的典型 EtherCAT 用法。

> 配套主程序 = 本目录 `Panasonic_ConveyorSync.csproj`; 状态恢复最小示例 = `EtherCATRestore/EtherCATRestore/`。
>
> **部署 / 安装驱动 / 配置驱动 / 导出 config.deni / 校准（齿比、相位、抓取区+传感器偏置）完整流程见上级目录 [COMMISSIONING.md](../../../COMMISSIONING.md)。**

---

## 设备与 PDO 映射

| 项 | 值 |
|----|----|
| 厂商 / 型号 | Panasonic MINAS A6B 伺服 (ESI: `panasonic_minas-a6bf`) |
| 应用层 | CiA 402 |
| 模式 | **CSP** (`0x6060 = 8`), DC Sync0 1 ms |
| RxPDO `0x1600` (输出, 9 B) | Controlword `0x6040` / Modes `0x6060` / Target Position `0x607A` / **Touch Probe Function `0x60B8`** |
| TxPDO `0x1A00` (输入, 23 B) | Error `0x603F` / Statusword `0x6041` / Mode Disp `0x6061` / **Pos Actual `0x6064`** / **Touch Probe Status `0x60B9`** / **Touch Probe1 Pos `0x60BA`** / Following Error `0x60F4` / Digital Inputs `0x60FD` |

字节布局来自 ESI 默认映射, 与 `PA_Output` / `PA_Input` 结构体逐字节一致。连接时经 CoE 把 `0x1C12 ← 0x1600` / `0x1C13 ← 0x1A00` 强制重映为单 PDO, 使布局确定。

---

## 主从同步原理

- 每个总线周期推进一个**主轴位置** `masterPos` (虚拟模式: 速度滑块自增; 编码器模式: 读选定从站的 `0x6064` 实际位置)。
- 每个**从轴**目标位置 = `Base + masterPos × 齿比 + 相位`。齿比 1:1 = 完全同步; 齿比 ≠ 1 = 电子齿轮 (变速带); 相位 = 工位错位。
- **物料跟踪**: 物料在传感器处被 Touch Probe 捕获 (或手动注入), 记录捕获时主轴位置; 之后行程 = `masterPos − 捕获位置`, 即随带向下游移动的距离。
- **飞行抓取**: 当某物料行程进入抓取区 `[起°, 止°]`, 抓取轴从该刻起以带速同步跟随物料 (`Base + (masterPos − 进区时主轴位置) × 齿比`); 物料离区即标记"已抓取"。

---

## 用到的 SDK API

| 用途 | API |
|------|-----|
| 初始化 | `new DarraEtherCAT().SetENI(deni).EnableAutoStartup().Build()` |
| 状态机 | `master.SetState(EcState.PreOp / SafeOp / OP)` |
| PDO 重映 | `slave.CoE.SDOWrite(0x1C12/0x1C13, …)` |
| DC | `slave.HasDC`, `slave.ConfigureDC(1000000)` |
| **实时控制** | `master.Events.ProcessDataCyclicSync` (随总线周期触发, **非** `Thread.Sleep` 自旋) |
| 过程数据 | `slave.PDO.InputsMapping<PA_Input>()` / `OutputsMapping<PA_Output>()` (ref 零拷贝) |
| **位置捕获** | Touch Probe: 写 `0x60B8 = 0x0011` 武装 → 读 `0x60B9` bit1 锁存 → `0x60BA` 锁存位置 |
| 诊断 | `slave.IsLost / .State / .ErrorCode / .PrimaryLinkBroken / .Diagnostics.DC`, `GetGroupExpectedWKC/ActualWKC` |
| 事件 | `StateChanged / SlaveStateChanged / EmergencyEvent / SlaveOffline / PDOFrameLoss …` |

---

## 操作步骤

1. 用 VS 打开 `Panasonic_ConveyorSync.csproj` (平台 x64), 编译, **以管理员身份运行**。
2. **连接** —— 走 Init → PreOp → (重映 PDO + DC) → SafeOp → 尺寸自检 (9/23) → OP, 列出各轴。
3. 选 **主轴源** (虚拟 / 编码器), 在轴表里设各从轴**齿比 / 相位**, 选**抓取轴**与**抓取区**。
4. **全部使能** → **启动同步**: 从轴随主轴运动。
5. 选 **捕获源** (Touch Probe / DI), **武装触发** 后由光电登记物料; 或点 **注入物料** 手动登记; 或勾 **演示:自动来料** 让系统按间隔自动来料。在"物料跟踪"表与"传送带可视化"看物料推进、进区、飞行抓取。

### 安全

上电即安全: 未使能不运动, 目标位置初始化为当前实际位置 (无跳变)。点动松手即停。任一轴故障 / 掉 OP / 跟随误差超限 → **组停 + 闭锁** (停主轴 + 全组去使能), 需排除后 **报警复位** 解锁。

---

## 怎么测试 / 怎么接光电

EtherCAT 主站**没有纯软件模拟器** —— 要让总线进 OP、`ProcessDataCyclicSync` 周期回调跑起来, 至少要 **真网卡 + 1 台真松下 A6 驱动 + GUI 导出的 config.deni**。但可分三档由省钱到全真逐级验证, 不必一开始就备齐全套:

| 档 | 需要的硬件 | 测什么 | 怎么做 |
|----|-----------|--------|--------|
| **A 逻辑档** | 1~3 台 A6 驱动 (电机可空载, **无需光电**) | 主从同步 + 物料跟踪 + 飞行抓取全链路 | 虚拟主轴 + **演示:自动来料** (或手动注入); 编码器主轴可手转电机看从轴跟随 |
| **B 真触发档** | A 档 + 一个 24V 按钮/接近开关 (**无需光电**) | 真实捕获路径 | 按钮接驱动 **EXT1** 锁存脚 (Touch Probe 源) 或任意 **SI** (DI 源), 每按一次 = 一次真捕获 |
| **C 全真档** | B 档把按钮换成真光电 | 现场真工况 | 光电装带子上方, 物料流过自动捕获 |

**光电接线 (松下 A6B)**: 光电传感器 (NPN/PNP 三线, 24VDC) 输出 → 驱动 **X4 I/O 连接器**:
- **Touch Probe 源 (捕获源 = Touch Probe)**: 接 **EXT1** 外部锁存输入, 在 PANATERM 把该输入分配成"外部锁存/Touch Probe 触发", 硬件锁存位置零延迟 (传送带飞拍首选)。
- **DI 源 (捕获源 = 数字输入 DI)**: 接任意 **SI** 输入, 该输入反映在 `0x60FD DigitalInputs`, 在界面"DI位"里填它对应的位号; 软件每 1ms 边沿检测 (接线随意, ~1ms 抖动)。

具体 EXT1/SI 引脚号 + 输入功能分配 (Pr4.xx) 见松下 A6 手册 / PANATERM。

> **演示:自动来料** 是软件模拟光电周期触发 —— 没接传感器时也能看到物料一件件来、跟踪、飞拍的完整动画; 它**不替代**真传感器, 只是上真硬件前的逻辑演示与可视化。

---

## 校准要点

| 校准项 | 怎么做 |
|--------|--------|
| **齿比 / 相位** | 齿比 1.0 = 与主轴同步；机械变速带按实测填（移动主轴 X，量从轴带行程，比 = 从/主）；相位让工位/卡爪对齐 |
| **抓取区 + 传感器偏置（关键）** | 量传感器到抓取头的距离（折成主轴度数）→ 填"抓取区 起/止(°)"。校准法：放一件物料触发，jog 主轴直到物料到抓取头，读这段主轴行程 = 抓取区中心 |
| **编码器主轴方向** | 编码器主轴模式下确认从轴跟随方向与带运动一致 |

完整部署与校准流程见 [COMMISSIONING.md](../../../COMMISSIONING.md) §8。

---

## 能测哪些功能 / 测试最大范围

> 常见疑问澄清：**不止预设动作** —— 主轴速度、齿比、相位、抓取区、捕获源都可调；物料可手动注入 / 自动来料 / 真传感器捕获。也**不是纯软件模拟** —— 在真伺服上做真主从 CSP；只有「传送带可视化」是纯软件画图。

**A. 不接硬件就能测：** 编译、启动、界面。（无真从站进不了 OP，同步 / 跟踪动不了 —— 主站**无从站模拟器**。）

**B. 接 ≥2 台真松下 A6（+ config.deni）能测：**

| 功能 | 怎么验证 |
|------|---------|
| 主从同步（齿比 1:1） | 启动同步，从轴随主轴等速走 |
| 电子齿轮（齿比 ≠1） | 改某轴齿比 2.0 / 0.5，看它按比例走 |
| 相位偏移 | 改相位，看工位错位 |
| 虚拟主轴 | 速度滑块 + 手动点动主轴 |
| 编码器主轴 | 选编码器源 + 手转该电机，从轴跟随 |
| 物料跟踪 | 注入 / 演示来料，物料表 + 可视化随主轴推进 |
| **Touch Probe 真捕获** | 光电接 EXT1，捕获源选 Touch Probe，武装 → 物料过传感器即捕获（连续多件） |
| **DI 捕获** | 光电接 SI，捕获源选 DI + 填位号 |
| 飞行抓取 | 物料进抓取区，抓取轴同步跟随 + 可视化红指针 |
| 演示自动来料 | 勾选 → **不接传感器也看整条流程** |
| 报警 / 组停 | 拔站 / 故障 → 红横幅 + 全组去使能 |

**测试最大范围（边界）**：主从同步 + 物料跟踪 + 飞行抓取的演示级实现；单主轴 + 多从轴；抓取区按主轴度数；一次跟踪 / 抓取队列里第一件在区者。**不含** 多主轴、相机视觉定位、复杂分拣调度、配方 —— 超出本案例范围。

---

## 直接运行（下载即跑）

仓库已带编译产物，**无需装 VS / 编译**：

1. 下载 / 克隆仓库 → 进 `Windows/CSharp/Panasonic_ConveyorSync/bin/Debug/`。
2. **以管理员身份**运行 `Panasonic_ConveyorSync.exe`。
3. 前提：已按 [COMMISSIONING.md](../../../COMMISSIONING.md) 装好主站驱动并绑定专用网卡。
4. 把 GUI 导出的松下从站 `config.deni` 放到 `bin/Debug/EtherCATRestore/config.deni`（没有它程序能开，但「连接」会提示缺文件）。
5. 「连接」→ 选主轴源 / 设齿比相位 / 选抓取轴 → 使能 → 启动同步 →（勾「演示:自动来料」或接传感器）。

> 想改源码后重编：用 VS 打开 `Panasonic_ConveyorSync.csproj`（平台 x64）编译；或命令行 MSBuild。

---

## 自己修改 / 更换硬件

| 想做什么 | 怎么改 |
|---------|--------|
| **换成你的真实从站** | 用主站 GUI 扫描导出 `config.deni` 覆盖 `EtherCATRestore/config.deni`。轴数自适应，界面自动生成。 |
| **换不同型号伺服**（仍 CiA402/CSP） | 改 `MainForm.cs` 的 `PA_Output` / `PA_Input` 结构体对齐你从站 PDO；本案例需 Touch Probe 对象（输出 `0x60B8`、输入 `0x60B9`/`0x60BA`）与数字输入 `0x60FD`，换型号时确保 PDO 含这些或改捕获方式。连接有 PDO 尺寸自检。 |
| **改主 / 从 / 抓取角色 + 齿比 / 相位** | 界面轴表 + 下拉，无需改代码。 |
| **改光电接法** | 界面「捕获源」选 Touch Probe(EXT1 锁存) 或 DI(`0x60FD` 位)；接线见 [COMMISSIONING.md](../../../COMMISSIONING.md) §8。 |
| **改抓取区 / 传感器偏置** | 界面「抓取区 起/止(°)」；校准见 COMMISSIONING §8。 |
| **改主轴速度 / 来料间隔** | 界面直接改。 |
| **改控制逻辑** | 实时逻辑在 `MainForm.Logic.cs`（`OnPdoCycle` / `StepAxis` / `HandleProducts` / `UpdatePick`）；连接 / 清理在 `MainForm.cs`。改完重编。 |

> 长时间连续运行（虚拟主轴 >30 分钟、齿比 1.0、1e6 脉冲/秒级）时主轴位置会逼近 int32 上限；本案例已做钳制防回绕乱跳（最坏停在限位），现场长跑可按需加位置回卷处理。

---

## config.deni 说明

本案例需一份**松下 MINAS A6B 从站**的 `config.deni` 才能运行, 放在 `EtherCATRestore/EtherCATRestore/` 下。

`config.deni` **不可手工编写** —— 它带 SHA-256 校验 + 逐字节 PDO / FMMU 启动指令, 必须用 **Darra 主站 GUI** 扫描真实从站后导出。导出时保持松下**默认 PDO 分配** (RxPDO `0x1600` / TxPDO `0x1A00`, 9 / 23 字节); 若改了映射, 需同步修改 `MainForm.cs` 的 `PA_Output` / `PA_Input`。轴数由实际扫描到的从站数决定 (`master.SlaveCount`), 界面自动适配。
