# Changelog

## 已实现功能

### ETG.1500 Feature Packs

| 功能 | 标准 | 说明 |
|------|------|------|
| CoE (CANopen over EtherCAT) | ETG.1500.1 | 支持 SDO 上传/下载、完整访问、分段传输、PDO 映射配置、紧急消息（Emergency）及 SDO 信息服务 |
| EoE (Ethernet over EtherCAT) | ETG.1500.2 | 支持通过 EtherCAT 网络透传标准以太网帧，实现从站 TCP/IP 通信，支持 IP 参数设置与 ARP 代理 |
| FoE (File Access over EtherCAT) | ETG.1500.3 | 支持通过 EtherCAT 网络进行文件读写操作，可用于从站固件更新与文件传输 |
| SoE (Servo Profile over EtherCAT) | ETG.1500.4 | 支持基于 SERCOS 接口的伺服驱动配置协议，实现 IDN 读写、命令执行及通知管理 |
| VoE (Vendor Specific over EtherCAT) | ETG.1500.5 | 支持厂商自定义邮箱协议，用于实现非标准化的私有数据交换 |
| AoE (ADS over EtherCAT) | ETG.1500.6 | 支持 ADS（Automation Device Specification）协议的路由与通信，可用于 Beckhoff 兼容设备的调试与诊断 |
| 电缆冗余 (Cable Redundancy) | ETG.1500.0 | 支持环形拓扑的线缆冗余功能，在网络线缆断开时自动切换冗余链路，保障通信不中断 |
| 热连接 (Hot Connect) | ETG.1500.0 | 支持从站设备的热插拔，可在运行过程中动态添加或移除从站，无需停止主站 |

### ETG.x Feature Packs

| 功能 | 标准 | 说明 |
|------|------|------|
| EtherCAT 主站核心协议 | ETG.1000 | 实现 EtherCAT 主站核心状态机（INIT → PRE-OP → SAFE-OP → OP），支持过程数据通信（PDO）、邮箱通信、AL 状态管理及错误处理 |
| 分布式时钟 (Distributed Clocks, DC) | ETG.1000.4 | 支持 DC 同步模式，包括系统时间分发、从站时钟同步、SYNC0/SYNC1 信号配置、漂移补偿及时钟抖动优化 |
| ESI (EtherCAT Slave Information) | ETG.2000 | 支持解析 ESI XML 文件（从站描述文件），自动获取从站 PDO 映射、SDO 对象字典、同步管理器配置等信息 |
| ENI (EtherCAT Network Information) | ETG.2100 | 支持解析 ENI XML 文件（网络配置文件），实现基于配置文件的主站初始化与网络拓扑管理 |
| 诊断功能 (Diagnostics) | ETG.1000.6 | 支持 EtherCAT 网络诊断，包括 WKC 检测、CRC 错误统计、帧丢失检测、从站状态监控及链路状态报告 |
| EtherCAT 从站信息接口 (SII/EEPROM) | ETG.1000.6 | 支持读写从站 SII（Slave Information Interface）EEPROM，用于获取从站标识、邮箱配置及引导信息 |
| 主站冗余 (Master Redundancy) | ETG.1000 | 支持主站冗余切换，在主主站故障时由备份主站接管网络控制，保障系统高可用 |
| 邮箱网关 (Mailbox Gateway) | ETG.1500 | 支持作为邮箱网关将上层应用的邮箱请求转发至 EtherCAT 从站，支持多协议网关模式 |
