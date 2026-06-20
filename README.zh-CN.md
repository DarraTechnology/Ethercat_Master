# Darra EtherCAT Master — 案例集

**[English](README.md)** | **中文**

**官方网站**: [https://ethercat.darra.xyz/](https://ethercat.darra.xyz/)

**Darra EtherCAT Master** SDK 的真机可运行示例工程。每个案例都是**独立工程** —— 所需类库 (`DarraEtherCAT.dll` / `Darra.Core.dll`) 和一份示例网络配置 (`config.deni`) 都已随附，**无需从源码编译 SDK** 即可运行。

---

## SDK 介绍

Darra EtherCAT Master 是一套跨平台 EtherCAT 主站开发套件，覆盖从网络扫描到实时周期过程数据交换的完整工作流。

### 核心功能

| 类别 | 功能 |
|------|------|
| 实时数据 | PDO 周期交换 (支持结构体直接映射读写)、DC 分布式时钟 (Sync0 / Sync1) |
| 邮箱协议 | CoE (SDO 读写)、FoE (固件升级)、EoE (以太网隧道)、SoE、AoE、VoE |
| 应用层协议 | CiA 402 (伺服 / 步进驱动)、CiA 401 (数字 / 模拟 I/O)、MDP (模块化设备) |
| 功能安全 | FSoE (Safety over EtherCAT) |
| 可靠性 | Mode 2 双网卡链路冗余、热插拔检测与自动恢复 |
| 配置管理 | ENI / DENI 导入导出、ESI 从站描述文件管理、状态转换自动下发 SDO |
| 状态控制 | Init / PreOp / SafeOp / OP 完整状态机 |
| 诊断 | 逐从站 AL 状态、链路 / 端口状态、WKC、DC 同步、分级报警 |

### 支持平台

| 平台 | 传输模式 | 说明 |
|------|---------|------|
| Windows x64 | WDK 内核驱动 | 高性能低延迟 |
| Linux x64 | Raw Socket | 用户态，需 root 权限 |
| Linux x64 | LKM 内核模块 | 高性能低延迟，需加载内核模块 |

### 支持语言

| 语言 | 接口 | 状态 |
|------|------|------|
| C# (.NET Framework 4.7.2 / .NET 8+) | `DarraEtherCAT.dll` 托管类库 | 已发布 |
| C / C++ | `Darra.Core.dll` 原生 API | 已发布 |
| Python | ctypes / cffi 绑定 | 计划中 |

> 本仓库当前的案例均为 **Windows + C#**。C++ 与 Linux 案例后续补充。

---

## 案例列表

下列 Windows 案例均为 WinForms 程序，目标框架 **.NET Framework 4.7.2 (x64)**，引用 `Darra.EtherCAT.Master` NuGet 包，**驱动真实电机**。

| 案例 | 设备 | 模式 | 轴数 | 演示内容 |
|------|------|------|------|---------|
| [Ezi-SERVO2_CSP_PP](Windows/CSharp/Ezi-SERVO2_CSP_PP/) | Ezi-SERVO2 伺服 (FASTECH) | CSP + PP | 单轴 | CiA 402 状态机、点动、绝对 / 相对定位、回零 |
| [STF-EC_CSP_PP](Windows/CSharp/STF-EC_CSP_PP/) | STF-EC 步进 (鸣志 MOONS') | CSP + PP | 多轴 (自适应) | 多轴总览表、全局 + 单轴控制；同一份 DENI 还支持 PV / CSV / HM / 厂商模式 |
| [STF-EC_ECam](Windows/CSharp/STF-EC_ECam/) | STF-EC 步进 (鸣志 MOONS') | CSP (电子凸轮) | 多轴 (自适应) | 虚拟主轴 + 凸轮曲线 (正弦 / 摆线 / 直线) + 逐轴相位偏移、组停报警 |
| [STF-EC_SyncAxis](Windows/CSharp/STF-EC_SyncAxis/) | STF-EC 步进 (鸣志 MOONS') | CSP (电子齿轮) | 多轴 (自适应) | 虚拟主轴 + 逐轴齿比 (1:1 同步 / N:1 齿轮联动)、同步报警 |

**运动模式速览**

- **CSP** (周期同步位置, `0x6060 = 8`)：主站**每个总线周期**算出并下发一个新目标位置，驱动器只做跟随。需 **DC Sync0** (1 ms)。是插补、电子凸轮、电子齿轮的基础。
- **PP** (轮廓位置, `0x6060 = 1`)：主站发一个目标 + 轮廓速度，由**驱动器**内部生成梯形轨迹。Free-Run，无需 DC —— 适合点到点定位。
- **电子凸轮** 建立在 CSP 之上：一根虚拟主轴相位驱动每根从轴按凸轮曲线 `s = f(相位)` 运动。
- **电子齿轮** 是凸轮的线性特例：从轴位置 = 主轴位置 × 齿比。

多轴 STF-EC 案例的界面**按实际扫描到的从站数** (`master.SlaveCount`) 自动生成；随附 `config.deni` 含 **5 个 STF-EC**，但代码会自动适配你加载的任意拓扑。

---

## 快速开始 (Windows / C#)

1. **打开** 案例的 `.csproj` (平台目标固定 **x64**，原生 `Darra.Core.dll` 仅 x64)。
2. **编译。** NuGet 还原 `Darra.EtherCAT.Master`；随附的 `config.deni` 与原生 DLL 会复制到 `bin\Debug\`。
3. **以管理员权限运行** (网卡裸帧收发需要提权)。
4. **连接** —— 程序走 Init → PreOp → SafeOp → OP，按需启用 DC，并列出各轴。
5. **使能** 驱动 (CiA 402 握手 `0x06 → 0x07 → 0x0F`)，再按案例能力做点动 / 定位 / 凸轮 / 齿轮。

> **换成你自己的拓扑**：用 **Darra 主站 GUI** 扫描真实从站后导出 `config.deni` 覆盖随附文件。`config.deni` 不可手工编辑 —— 它带 SHA-256 校验 + 逐字节 PDO / FMMU 启动指令，必须对应真实设备。导出时保持 STF-EC **默认 PDO 分配** (RxPDO 29 B / TxPDO 35 B)；若改了映射，需同步修改 `MainForm.cs` 中的 `STF_Output` / `STF_Input` 结构体。

### 安全约定

每个案例**上电即安全**：未显式使能前不会运动，目标位置初始化为当前实际位置 (无跳变)。点动为**松手即停**。多轴案例中，**故障级**报警会触发 **组停 + 闭锁** 以保护耦合机构。

---

## 目录结构

案例按 **平台 › 语言 › 案例** 三级组织：

```
Darra_EtherCAT_Case/
├── README.md / README.zh-CN.md
└── Windows/
    └── CSharp/
        ├── Ezi-SERVO2_CSP_PP/    # 单轴伺服，CSP + PP
        ├── STF-EC_CSP_PP/        # 多轴步进，CSP + PP
        ├── STF-EC_ECam/          # 多轴电子凸轮 (CSP)
        └── STF-EC_SyncAxis/      # 多轴电子齿轮 (CSP)
```

每个案例目录都有**自己的 README**，含设备信息、PDO 映射、所用 SDK API、报警 / 诊断行为与分步操作。

---

## 许可与支持

本案例集作为 Darra EtherCAT Master SDK 的参考代码提供。SDK 文档、下载与支持请访问 **[ethercat.darra.xyz](https://ethercat.darra.xyz/)**。
