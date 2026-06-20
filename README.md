# Darra EtherCAT Master — Examples

**English** | **[中文](README.zh-CN.md)**

**Official Website**: [https://ethercat.darra.xyz/](https://ethercat.darra.xyz/)

Ready-to-run, real-hardware example projects for the **Darra EtherCAT Master** SDK. Every example is a **standalone project** — the required libraries (`DarraEtherCAT.dll` / `Darra.Core.dll`) and a sample network configuration (`config.deni`) are already bundled, so you do **not** need to build the SDK from source to run them.

---

## About the SDK

Darra EtherCAT Master is a cross-platform EtherCAT master stack that covers the full workflow from network scanning to real-time cyclic process-data exchange.

### Key Features

| Category | Features |
|----------|----------|
| Real-Time Data | PDO cyclic exchange with direct **struct mapping**, DC distributed clock (Sync0 / Sync1) |
| Mailbox Protocols | CoE (SDO), FoE (firmware update), EoE (Ethernet tunnel), SoE, AoE, VoE |
| Application Profiles | CiA 402 (servo / stepper), CiA 401 (digital / analog I/O), MDP (modular devices) |
| Safety | FSoE (Safety over EtherCAT) |
| Reliability | Mode 2 dual-NIC line redundancy, hot-plug detection & auto-recovery |
| Configuration | ENI / DENI import-export, ESI management, automatic startup-SDO download |
| State Machine | Init / PreOp / SafeOp / OP with full transition control |
| Diagnostics | Per-slave AL status, link/port state, WKC, DC-sync, severity-graded alarms |

### Supported Platforms

| Platform | Transport | Notes |
|----------|-----------|-------|
| Windows x64 | WDK kernel driver | High performance, low latency |
| Linux x64 | Raw Socket | User mode, requires root |
| Linux x64 | LKM kernel module | High performance, low latency |

### Supported Languages

| Language | Interface | Status |
|----------|-----------|--------|
| C# (.NET Framework 4.7.2 / .NET 8+) | `DarraEtherCAT.dll` managed library | Released |
| C / C++ | `Darra.Core.dll` native API | Released |
| Python | ctypes / cffi bindings | Planned |

> The examples in this repository are currently **C# on Windows**. C++ and Linux examples are on the way.

---

## Examples

All Windows examples are WinForms apps targeting **.NET Framework 4.7.2 (x64)** and reference the `Darra.EtherCAT.Master` NuGet package. They drive **real motors** over EtherCAT.

| Example | Device | Mode | Axes | What it shows |
|---------|--------|------|------|---------------|
| [Ezi-SERVO2_CSP_PP](Windows/CSharp/Ezi-SERVO2_CSP_PP/) | Ezi-SERVO2 servo (FASTECH) | CSP + PP | 1 | CiA 402 state machine, jog, absolute / relative positioning, homing |
| [STF-EC_CSP_PP](Windows/CSharp/STF-EC_CSP_PP/) | STF-EC stepper (MOONS') | CSP + PP | Multi (auto) | Multi-axis overview table, global + per-axis control; one DENI also supports PV / CSV / HM / vendor mode |
| [STF-EC_ECam](Windows/CSharp/STF-EC_ECam/) | STF-EC stepper (MOONS') | CSP (Electronic Cam) | Multi (auto) | Virtual master + cam curves (sine / cycloid / linear) + per-axis phase offset, group-stop alarms |
| [STF-EC_SyncAxis](Windows/CSharp/STF-EC_SyncAxis/) | STF-EC stepper (MOONS') | CSP (Electronic Gear) | Multi (auto) | Virtual master + per-axis gear ratio (1:1 lockstep, N:1 gearing), sync alarms |

**Motion-mode primer**

- **CSP** (Cyclic Synchronous Position, `0x6060 = 8`): the master computes and sends a fresh target position **every bus cycle**; the drive only follows. Requires **DC Sync0** (1 ms). This is the basis for interpolation, electronic cam, and electronic gear.
- **PP** (Profile Position, `0x6060 = 1`): the master sends one target + profile velocity; the **drive** generates the trapezoidal trajectory internally. Free-Run, no DC needed — ideal for point-to-point moves.
- **Electronic Cam** builds on CSP: a virtual master phase drives each slave through a cam curve `s = f(phase)`.
- **Electronic Gear** is the linear special case of the cam: slave position = master position × ratio.

The multi-axis STF-EC examples size their UI from the **actually scanned** slave count (`master.SlaveCount`); the bundled `config.deni` contains **5 STF-EC** slaves but the code adapts automatically to whatever topology you load.

---

## Getting Started (Windows / C#)

1. **Open** the example's `.csproj` in Visual Studio (platform target is fixed to **x64** — the native `Darra.Core.dll` is x64 only).
2. **Build.** NuGet restores `Darra.EtherCAT.Master`; the bundled `config.deni` and native DLLs are copied to `bin\Debug\`.
3. **Run as Administrator** (raw Ethernet frame access requires elevation).
4. **Connect** — the app walks Init → PreOp → SafeOp → OP, enables DC where needed, and lists the axes.
5. **Enable** a drive (CiA 402 handshake `0x06 → 0x07 → 0x0F`), then jog / position / cam / gear as the example allows.

> **Use your own topology**: replace the bundled `config.deni` with one **exported from the Darra Master GUI** after scanning your real slaves. `config.deni` is not hand-editable — it carries an SHA-256 checksum plus byte-exact PDO / FMMU startup commands that must match the real devices. Keep the STF-EC **default PDO assignment** (RxPDO 29 B / TxPDO 35 B); if you change the mapping, update the `STF_Output` / `STF_Input` structs in `MainForm.cs` accordingly.

### Safety

Every example is **safe on power-up**: nothing moves until you explicitly enable a drive, and target positions are initialized to the current actual position (no jump). Jog is dead-man (release = stop). Fault-grade alarms in the multi-axis examples trigger a **group stop + latch** to protect coupled mechanisms.

---

## Directory Structure

Examples are organized as **Platform › Language › Example**:

```
Darra_EtherCAT_Case/
├── README.md / README.zh-CN.md
└── Windows/
    └── CSharp/
        ├── Ezi-SERVO2_CSP_PP/    # single-axis servo, CSP + PP
        ├── STF-EC_CSP_PP/        # multi-axis stepper, CSP + PP
        ├── STF-EC_ECam/          # multi-axis electronic cam (CSP)
        └── STF-EC_SyncAxis/      # multi-axis electronic gear (CSP)
```

Each example folder has its **own README** with device info, PDO mapping, the SDK APIs it uses, alarm/diagnostics behavior, and step-by-step operation.

---

## License & Support

These examples are provided as reference code for the Darra EtherCAT Master SDK. For SDK documentation, downloads, and support, visit **[ethercat.darra.xyz](https://ethercat.darra.xyz/)**.
