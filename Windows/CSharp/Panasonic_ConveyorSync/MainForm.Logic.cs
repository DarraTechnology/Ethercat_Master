using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using DarraEtherCAT_Master;

namespace Panasonic_ConveyorSync
{
    // MainForm 的实时逻辑 / 物料跟踪 / 抓取 / 报警决策 / 可视化部分 (与 MainForm.cs 同一 partial class)
    public partial class MainForm
    {
        // ==================== 主轴源 / 同步 UI ====================

        void MasterSource_Changed(object sender, EventArgs e)
        {
            _encoderMode = rbEncoder.Checked;
            numMasterSpeed.Enabled = !_encoderMode;
            cboEncoderAxis.Enabled = _encoderMode;
            _encoderInitPending = true;          // 切源后重新快照编码器基准
            if (_syncRunning) _resyncRequested = true;
            Log(_encoderMode ? $"主轴源 = 编码器 (轴{_encoderAxisIndex + 1})" : "主轴源 = 虚拟主轴");
        }

        void btnSyncStart_Click(object sender, EventArgs e)
        {
            if (_groupStopLatched) { Log("报警闭锁中, 请先 [报警复位]"); return; }
            _masterStep = Math.Max(1, (int)(numMasterSpeed.Value / 1000m));
            foreach (var a in axes) a.GraceResetPending = true;   // 启动瞬间不判跟随误差 (正在追目标)
            _resyncRequested = true;
            _syncRunning = true;
            Log(_encoderMode
                ? "启动同步: 从轴跟随编码器主轴"
                : $"启动同步: 主轴速度 {numMasterSpeed.Value} 脉冲/秒 (每周期 {_masterStep} 脉冲)");
        }

        void btnSyncStop_Click(object sender, EventArgs e)
        {
            _syncRunning = false;
            Log("停止同步 (各轴保持当前位置)");
        }

        void btnArmProbe_Click(object sender, EventArgs e)
        {
            if (!isRunning) return;
            _probeArmRequest = !_probeArmRequest;
            int probeIdx = _encoderMode ? _encoderAxisIndex : 0;
            btnArmProbe.BackColor = _probeArmRequest ? Color.Khaki : System.Drawing.SystemColors.Control;
            Log(_probeArmRequest
                ? $"已武装 Touch Probe (轴{probeIdx + 1} probe1 上升沿; 现场接传感器, 物料经过即捕获)"
                : "已解除 Touch Probe 武装");
        }

        // ==================== PDO 控制回调 (随总线周期 1ms 触发) ====================

        void OnPdoCycle(ushort mi)
        {
            try
            {
                var m = master;
                if (m == null || !isRunning) return;
                var arr = axes;
                _pdoCycle++;

                // 编码器主轴基准初始化
                if (_encoderInitPending)
                {
                    _encoderInitPending = false;
                    if (_encoderMode && _encoderAxisIndex >= 0 && _encoderAxisIndex < arr.Length)
                    {
                        ref var ein = ref m.Slaves[arr[_encoderAxisIndex].SlaveIndex].PDO.InputsMapping<PA_Input>();
                        _encoderBase = ein.PositionActualValue;
                    }
                }

                // ① 启动同步: 重新快照各轴 Base + 主轴清零 (PDO 线程内完成, 保证原子)
                if (_resyncRequested)
                {
                    _resyncRequested = false;
                    _masterPos = 0;
                    if (_encoderMode && _encoderAxisIndex >= 0 && _encoderAxisIndex < arr.Length)
                    {
                        ref var ein = ref m.Slaves[arr[_encoderAxisIndex].SlaveIndex].PDO.InputsMapping<PA_Input>();
                        _encoderBase = ein.PositionActualValue;
                    }
                    for (int i = 0; i < arr.Length; i++)
                    {
                        ref var input = ref m.Slaves[arr[i].SlaveIndex].PDO.InputsMapping<PA_Input>();
                        arr[i].Base = input.PositionActualValue;
                        arr[i].CurrentTarget = input.PositionActualValue;
                        arr[i].GraceResetPending = true;
                    }
                    _pickEngaged = false; _pickProductId = 0;
                }

                // ② 推进主轴位置 (虚拟自增 / 编码器读数)
                if (_encoderMode && _encoderAxisIndex >= 0 && _encoderAxisIndex < arr.Length)
                {
                    ref var ein = ref m.Slaves[arr[_encoderAxisIndex].SlaveIndex].PDO.InputsMapping<PA_Input>();
                    _masterPos = (long)ein.PositionActualValue - _encoderBase;
                }
                else
                {
                    if (_syncRunning) _masterPos += _masterStep;
                    int jog = _jogMasterDir;
                    if (jog != 0) _masterPos += jog * JOG_MASTER_STEP;
                }
                long masterPos = _masterPos;

                // ③ 物料: 清空 / 注入 / Touch Probe 捕获 / 行程更新
                HandleProducts(m, arr, masterPos);

                // ④ 抓取区 / 飞行抓取判定
                bool pickActive; long pickTravel;
                UpdatePick(masterPos, out pickActive, out pickTravel);

                // ⑤ 各轴 StepAxis (按角色: 主/从/抓取) + 快照 + 报警闩
                for (int i = 0; i < arr.Length; i++)
                {
                    var a = arr[i];
                    ref var input = ref m.Slaves[a.SlaveIndex].PDO.InputsMapping<PA_Input>();
                    ref var output = ref m.Slaves[a.SlaveIndex].PDO.OutputsMapping<PA_Output>();

                    string role = ComputeRoleString(i);
                    a.SnapRole = role;
                    StepAxis(a, role, masterPos, pickActive, pickTravel, ref input, ref output);

                    ushort sw = input.StatusWord;
                    a.SnapStatusWord = sw;
                    int actPos = input.PositionActualValue;
                    a.SnapActualPosition = actPos;
                    a.SnapActualVelocity = ClampToInt((long)(actPos - a.PrevActualPos) * 1000);   // 估算 脉冲/秒 (dt=1ms; 松下默认 PDO 无速度对象; long 防溢出)
                    a.PrevActualPos = actPos;
                    a.SnapErrorCode = input.ErrorCode;
                    a.SnapDriveState = ParseDriveState(sw);

                    // —— 报警检测 (PDO 热路径: 只置 volatile 闩, 决策与呈现在 50ms 消费侧) ——
                    int fe = actPos - output.TargetPosition;
                    a.SnapFollowError = fe;
                    bool eligibleNow = a.ServoEnabled && IsOperationEnabled(sw) && a.Ratio > 0 && !a.FaultReset && role != "主";
                    if (!eligibleNow) { a.SyncStartCycle = 0; a.FollowErrConsec = 0; }
                    else if (a.SyncStartCycle == 0 || a.GraceResetPending) { a.SyncStartCycle = _pdoCycle; a.GraceResetPending = false; a.FollowErrConsec = 0; }
                    bool pastGrace = eligibleNow && a.SyncStartCycle != 0 && (_pdoCycle - a.SyncStartCycle) >= GRACE_CYCLES;
                    a.SyncEligible = pastGrace;
                    if (pastGrace) a.WasEligible = true;

                    long cmdDelta = (long)output.TargetPosition - a.PrevTarget;
                    a.PrevTarget = output.TargetPosition;
                    long effLimit = (long)a.FollowErrLimit + 4L * Math.Abs(cmdDelta);   // long 防 4×Δ 溢出致 effLimit 变负误报

                    if (pastGrace)
                    {
                        if (Math.Abs(fe) > effLimit) { if (++a.FollowErrConsec >= FE_CONSEC_N) a.AlarmFollowError = true; }
                        else { a.FollowErrConsec = 0; a.AlarmFollowError = false; }
                    }
                    else a.AlarmFollowError = false;

                    a.AlarmFault = IsFault(sw) || input.ErrorCode != 0;
                    a.AlarmDropEnable = a.WasEligible && !IsOperationEnabled(sw) && a.ServoEnabled && !a.FaultReset;

                    if (!a.ServoEnabled || a.FaultReset) { a.WasEligible = false; a.AlarmFollowError = false; a.AlarmDropEnable = false; a.FollowErrConsec = 0; }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDO] 控制回调异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // ==================== 物料跟踪 ====================

        // 清空 / 注入 / Touch Probe 捕获 / 行程与区域更新 (PDO 线程)
        void HandleProducts(DarraEtherCAT m, AxisController[] arr, long masterPos)
        {
            if (_clearRequest)
            {
                _clearRequest = false;
                lock (_prodLock) { _products.Clear(); }
                _pickEngaged = false; _pickProductId = 0;
            }

            int inj = System.Threading.Interlocked.Exchange(ref _injectRequest, 0);   // 原子读+清零, 不丢请求
            if (inj > 0)
            {
                lock (_prodLock)
                    for (int k = 0; k < inj; k++)
                        _products.Add(new Product { Id = _nextProductId++, Source = "手动", CaptureMasterPos = masterPos, Travel = 0, Zone = ZoneState.未到 });
            }

            // 演示: 自动来料 (按主轴行程周期自动登记物料, 模拟光电周期触发 — 无需真传感器即可看整条流程)
            if (_demoAuto && _syncRunning)
            {
                if (!_demoWasOn) { _demoWasOn = true; _lastAutoInjectMasterPos = masterPos; }
                if (Math.Abs((masterPos - _lastAutoInjectMasterPos) * 360.0 / PULSES_PER_REV) >= _demoIntervalDeg)
                {
                    lock (_prodLock)
                        _products.Add(new Product { Id = _nextProductId++, Source = "演示", CaptureMasterPos = masterPos, Travel = 0, Zone = ZoneState.未到 });
                    _lastAutoInjectMasterPos = masterPos;
                }
            }
            else _demoWasOn = false;

            // 真实光电接收: 在 probe 轴上按"捕获源"读传感器 —— ① Touch Probe 硬件锁存 / ② 数字输入 DI。
            // (虚拟模式默认轴0, 编码器模式用编码器轴; 现场把光电接到该驱动对应输入)
            int probeIdx = _encoderMode ? _encoderAxisIndex : 0;
            if (probeIdx >= 0 && probeIdx < arr.Length)
            {
                var pa = arr[probeIdx];
                ref var pin = ref m.Slaves[pa.SlaveIndex].PDO.InputsMapping<PA_Input>();
                ref var pout = ref m.Slaves[pa.SlaveIndex].PDO.OutputsMapping<PA_Output>();
                if (_captureSrc == 0)
                {
                    // ① Touch Probe 硬件锁存 (光电接驱动 EXT1 锁存脚, 边沿→位置零延迟, 传送带飞拍首选)
                    if (_probeArmRequest)
                    {
                        if (pa.ProbeState == 0) { pout.TouchProbeFunction = 0x0011; pa.ProbeState = 1; }   // bit0 enable TP1 + bit4 正沿
                        else if (pa.ProbeState == 1)
                        {
                            if ((pin.TouchProbeStatus & 0x0002) != 0 && (pa.PrevProbeStatus & 0x0002) == 0)   // bit1 上升沿 = 已锁存
                            {
                                lock (_prodLock)
                                    _products.Add(new Product { Id = _nextProductId++, Source = "传感器", CaptureMasterPos = masterPos, Travel = 0, Zone = ZoneState.未到 });
                                Log($"Touch Probe 捕获物料 #{_nextProductId - 1} (锁存位置 {pin.TouchProbe1PosValue})");
                                pa.ProbeState = 2;
                            }
                        }
                        else if (pa.ProbeState == 2) { pout.TouchProbeFunction = 0; pa.ProbeState = 3; }     // 关闭锁存功能, 进入等待
                        else if (pa.ProbeState == 3) { if ((pin.TouchProbeStatus & 0x0002) == 0) pa.ProbeState = 0; }  // 等驱动清 status bit1 → 干净重新武装 (防漏抓第二件)
                    }
                    else if (pa.ProbeState != 0 || pout.TouchProbeFunction != 0) { pout.TouchProbeFunction = 0; pa.ProbeState = 0; }
                    pa.PrevProbeStatus = pin.TouchProbeStatus;
                }
                else
                {
                    // ② 数字输入 DI (光电接驱动任意 SI, 反映在 0x60FD; 软件每 1ms 边沿检测, 接线随意, ~1ms 抖动)
                    if (pout.TouchProbeFunction != 0) pout.TouchProbeFunction = 0;
                    pa.ProbeState = 0;
                    bool high = ((pin.DigitalInputs >> _diBit) & 1u) != 0;
                    if (_probeArmRequest && high && !_prevDiHigh)
                    {
                        lock (_prodLock)
                            _products.Add(new Product { Id = _nextProductId++, Source = "传感器", CaptureMasterPos = masterPos, Travel = 0, Zone = ZoneState.未到 });
                        Log($"DI 捕获物料 #{_nextProductId - 1} (0x60FD bit{_diBit} 上升沿)");
                    }
                    _prevDiHigh = high;
                }
            }

            // 行程 + 区域状态
            lock (_prodLock)
                foreach (var p in _products)
                {
                    p.Travel = masterPos - p.CaptureMasterPos;
                    double deg = p.Travel * 360.0 / PULSES_PER_REV;
                    if (p.Picked) p.Zone = ZoneState.已过;
                    else if (deg < _zoneFromDeg) p.Zone = ZoneState.未到;
                    else if (deg <= _zoneToDeg) p.Zone = ZoneState.在区;
                    else p.Zone = ZoneState.已过;
                }
        }

        // 飞行抓取: 找到第一个在抓取区且未抓取的物料, 抓取轴同步跟随; 物料离区即标记已抓取 (PDO 线程)
        void UpdatePick(long masterPos, out bool pickActive, out long pickTravel)
        {
            pickActive = false; pickTravel = 0;
            double from = _zoneFromDeg, to = _zoneToDeg;
            Product inZone = null;
            lock (_prodLock)
                foreach (var p in _products)
                {
                    if (p.Picked) continue;
                    double deg = p.Travel * 360.0 / PULSES_PER_REV;
                    if (deg >= from && deg <= to) { inZone = p; break; }
                }

            if (inZone != null)
            {
                if (!_pickEngaged) { _pickEngaged = true; _pickEngageMasterPos = masterPos; _pickProductId = inZone.Id; Log($"飞行抓取: 物料#{inZone.Id} 进入抓取区, 抓取轴同步跟随"); }
                if (_pickProductId == inZone.Id) { pickActive = true; pickTravel = masterPos - _pickEngageMasterPos; }
            }
            else if (_pickEngaged)
            {
                lock (_prodLock)
                    foreach (var p in _products)
                        if (p.Id == _pickProductId) { p.Picked = true; break; }
                Log($"抓取完成: 物料#{_pickProductId} 已抓取");
                _pickEngaged = false; _pickProductId = 0;
            }
        }

        string ComputeRoleString(int i)
        {
            if (_encoderMode && i == _encoderAxisIndex) return "主";
            if (i == _pickAxisIndex) return "抓取";
            return "从";
        }

        // long → int32(DINT) 钳制, 防 CSP 目标位置回绕乱跳
        static int ClampToInt(long v) => v > int.MaxValue ? int.MaxValue : (v < int.MinValue ? int.MinValue : (int)v);

        // CSP 单轴: CiA402 使能握手 (0x06→0x07→0x0F)。按角色决定目标:
        //   主  = 编码器参考轴, 不驱动 (目标=实际, 保持);
        //   从  = 同步时按 Base + masterPos*Ratio + Phase 跟随, 否则保持;
        //   抓取= 仅当物料在抓取区时按 pickTravel 飞行跟随, 否则保持。
        void StepAxis(AxisController a, string role, long masterPos, bool pickActive, long pickTravel, ref PA_Input input, ref PA_Output output)
        {
            output.ModesOfOperation = 8;
            ushort sw = input.StatusWord;
            ushort cw = 0;
            int actual = input.PositionActualValue;

            if (a.FaultReset) { cw = 0x80; a.FaultReset = false; }
            else if (IsFault(sw)) { /* 等待故障复位 */ }
            else if (!a.ServoEnabled) { a.CurrentTarget = actual; a.Base = actual; }
            else if ((sw & 0x4F) == 0x00) { /* NotReadyToSwitchOn: 等待 */ }
            else if (IsSwitchOnDisabled(sw)) { cw = 0x06; a.CurrentTarget = actual; a.Base = actual; }
            else if (IsReadyToSwitchOn(sw)) { cw = 0x07; a.CurrentTarget = actual; output.TargetPosition = a.CurrentTarget; }
            else if (IsSwitchedOn(sw)) { cw = 0x0F; a.CurrentTarget = actual; output.TargetPosition = a.CurrentTarget; }
            else if (IsOperationEnabled(sw))
            {
                cw = 0x0F;
                if (role == "主")
                {
                    a.CurrentTarget = actual; a.Base = actual;            // 参考轴: 保持, 不与编码器外驱冲突
                }
                else if (role == "抓取")
                {
                    // 用 long 算 + 钳到 int32(DINT): 虚拟主轴长跑后 masterPos*Ratio 会超 int.MaxValue,
                    // 直接 (int) 转换会回绕成乱值 → CSP 目标瞬跳 ±2^32 → 撕裂机构。钳制后最坏是停在限位, 不乱冲。
                    if (pickActive) a.CurrentTarget = ClampToInt((long)a.Base + (long)Math.Round(pickTravel * a.Ratio) + a.Phase);  // 飞行同步
                    else { a.CurrentTarget = actual; a.Base = actual; }   // 区外保持, 并随时重设基准以便下次干净起步
                }
                else // 从轴
                {
                    if (_syncRunning) a.CurrentTarget = ClampToInt((long)a.Base + (long)Math.Round(masterPos * a.Ratio) + a.Phase);
                    else { a.CurrentTarget = actual; a.Base = actual; }
                }
                output.TargetPosition = a.CurrentTarget;
            }
            output.ControlWord = cw;
            a.SnapTargetPosition = a.CurrentTarget;
        }
        // ==================== UI 刷新 (50ms 后台线程, BeginInvoke 异步刷 UI) ====================

        void StatusUpdateLoop()
        {
            while (isRunning)
            {
                try
                {
                    var arr = axes;
                    var m = master;

                    alarmMgr.DrainPending();
                    DetectAlarms(arr, m);

                    long masterPos = _masterPos;
                    bool sync = _syncRunning;
                    bool enc = _encoderMode;
                    BeginInvoke(new Action(() =>
                    {
                        lblMasterPos.Text = $"主轴位置: {masterPos * 360.0 / PULSES_PER_REV:F2}° ({masterPos})" +
                            (enc ? "  [编码器]" : "") + (sync ? "  ● 同步中" : "");
                        lblMasterPos.ForeColor = sync ? Color.SeaGreen : Color.DimGray;

                        for (int i = 0; i < arr.Length && i < gridAxes.Rows.Count; i++)
                        {
                            var a = arr[i];
                            ushort sw = a.SnapStatusWord;
                            var row = gridAxes.Rows[i];
                            row.Cells[COL_ROLE].Value = a.SnapRole;
                            row.Cells[COL_STATE].Value = a.SnapDriveState;
                            row.Cells[COL_SW].Value = $"0x{sw:X4}";
                            row.Cells[COL_ACTPOS].Value = $"{a.SnapActualPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapActualPosition})";
                            row.Cells[COL_TGTPOS].Value = $"{a.SnapTargetPosition * 360.0 / PULSES_PER_REV:F2} ({a.SnapTargetPosition})";
                            row.Cells[COL_VEL].Value = a.SnapActualVelocity.ToString();
                            row.Cells[COL_ENABLED].Value = a.ServoEnabled ? "ON" : "off";

                            var stateCell = row.Cells[COL_STATE];
                            if (IsFault(sw) || a.SnapErrorCode != 0) stateCell.Style.ForeColor = Color.Firebrick;
                            else if (IsOperationEnabled(sw)) stateCell.Style.ForeColor = Color.SeaGreen;
                            else stateCell.Style.ForeColor = Color.DimGray;

                            bool bad = IsFault(sw) || a.SnapErrorCode != 0 || a.AlarmFollowError || a.AlarmDropEnable;
                            row.DefaultCellStyle.BackColor = bad ? Color.FromArgb(252, 235, 235) : Color.White;
                        }

                        RefreshProductGrid();
                        RefreshAlarmUI();
                        picConveyor.Invalidate();
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[StatusUpdateLoop] 异常: {ex.GetType().Name}: {ex.Message}"); }
                Thread.Sleep(50);
            }
        }

        void DetectAlarms(AxisController[] arr, DarraEtherCAT m)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                var a = arr[i];
                if (a.AlarmFollowError) RaiseFault(i, AlarmType.跟随误差, $"跟随误差 {a.SnapFollowError} 脉冲 > 限值 {a.FollowErrLimit}");
                else alarmMgr.Clear(i, AlarmType.跟随误差);
                if (a.AlarmDropEnable) RaiseFault(i, AlarmType.掉OP, "同步运行中掉出 OperationEnabled");
                else alarmMgr.Clear(i, AlarmType.掉OP);
                if (a.AlarmFault) RaiseFault(i, AlarmType.驱动故障, $"状态字Fault 或 错误码 0x{a.SnapErrorCode:X4}");
                else alarmMgr.Clear(i, AlarmType.驱动故障);

                if (m == null) continue;
                try
                {
                    var slave = m.Slaves[a.SlaveIndex];
                    if (slave.IsLost || slave.State != EcState.OP) RaiseFault(i, AlarmType.掉站, $"State={slave.State} IsLost={slave.IsLost}");
                    else alarmMgr.Clear(i, AlarmType.掉站);

                    var al = slave.ErrorCode;
                    if (al != EcALState.NoError) alarmMgr.Raise(i, AlarmType.AL状态, AlarmSeverity.Fault, $"AL: {al}");
                    else alarmMgr.Clear(i, AlarmType.AL状态);

                    if (slave.PrimaryLinkBroken) alarmMgr.Raise(i, AlarmType.链路断, AlarmSeverity.Fault, "主链路断开");
                    else if (slave.SecondaryLinkBroken) alarmMgr.Raise(i, AlarmType.链路断, AlarmSeverity.Warning, "副链路断开");
                    else alarmMgr.Clear(i, AlarmType.链路断);

                    var dc = slave.Diagnostics.DC;
                    if (!dc.IsInSync) alarmMgr.Raise(i, AlarmType.DC失步, AlarmSeverity.Warning, $"DC偏差 {dc.SyncTimeDifference}ns");
                    else alarmMgr.Clear(i, AlarmType.DC失步);
                }
                catch { /* master 正在关闭等 — 下个周期再来 */ }
            }

            if (m == null) return;
            try
            {
                var ls = m.LinkState;
                if (ls == EcLinkState.Disconnected) RaiseFault(-1, AlarmType.链路断, "主站链路断开");
                else if (ls == EcLinkState.PrimaryOnly || ls == EcLinkState.SecondaryOnly) alarmMgr.Raise(-1, AlarmType.链路断, AlarmSeverity.Warning, $"链路降级 {ls}");
                else alarmMgr.Clear(-1, AlarmType.链路断);

                ushort exp = DarraEtherCAT.GetGroupExpectedWKC(m.MasterNumber, 1);
                ushort act = DarraEtherCAT.GetGroupActualWKC(m.MasterNumber, 1);
                if (exp > 0 && act < exp)
                {
                    _wkcMissConsec++;
                    var sev = _wkcMissConsec >= PDO_LOSS_FAULT ? AlarmSeverity.Fault : AlarmSeverity.Warning;
                    alarmMgr.Raise(-1, AlarmType.WKC短缺, sev, $"实测WKC {act} < 期望 {exp} (连续{_wkcMissConsec})");
                }
                else { _wkcMissConsec = 0; alarmMgr.Clear(-1, AlarmType.WKC短缺); }
            }
            catch { }
        }

        void RaiseFault(int axis, AlarmType type, string msg)
        {
            alarmMgr.Raise(axis, type, AlarmSeverity.Fault, msg);
            if (!_groupStopLatched) GroupStop(axis, $"{(axis < 0 ? "主站" : "轴" + (axis + 1))} {type}");
        }

        // 组停: 同步运动一旦某轴 Fault, 继续动其它轴会撕裂机构 → 停主轴 + 全组去使能 + 闭锁。
        void GroupStop(int triggerAxis, string reason)
        {
            _syncRunning = false;
            _jogMasterDir = 0;
            _pickEngaged = false;
            var arr = axes;
            foreach (var a in arr) a.ServoEnabled = false;
            _groupStopLatched = true;
            Log($"⛔ 组停: {reason} (已停主轴并全组去使能, 闭锁中 — 排除后 [报警复位] 解锁)");
        }
        // ==================== 报警 UI ====================

        void InitAlarmGrid()
        {
            gridAlarms.AutoGenerateColumns = false;
            gridAlarms.Columns.Clear();
            AddAlarmCol("时间", 64);
            AddAlarmCol("轴", 44);
            AddAlarmCol("严重度", 60);
            AddAlarmCol("类型", 96);
            var msg = new DataGridViewTextBoxColumn { HeaderText = "消息", ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            gridAlarms.Columns.Add(msg);
            AddAlarmCol("状态", 64);
        }

        void AddAlarmCol(string header, int width)
        {
            gridAlarms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, Width = width, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        }

        void RefreshAlarmUI()
        {
            alarmMgr.Snapshot(out int active, out int faults, out _, out string top);
            int hist = alarmMgr.HistoryCount();
            if (active == 0)
            {
                pnlAlarmBanner.BackColor = Color.FromArgb(223, 240, 216);
                lblAlarmBanner.ForeColor = Color.SeaGreen;
                lblAlarmBanner.Text = "● 无报警 — 系统正常";
            }
            else
            {
                bool fault = faults > 0;
                pnlAlarmBanner.BackColor = fault ? Color.FromArgb(248, 215, 218) : Color.FromArgb(255, 243, 205);
                lblAlarmBanner.ForeColor = fault ? Color.Firebrick : Color.DarkOrange;
                lblAlarmBanner.Text = (fault ? "⛔ 报警(故障): " : "▲ 报警(警告): ") + (top ?? "");
            }
            lblAlarmCount.Text = $"激活:{active}  故障:{faults}  历史:{hist}";
            btnAlarmAck.Enabled = (active > 0 || hist > 0);

            var rows = alarmMgr.BuildRowsIfDirty();
            if (rows == null) return;
            gridAlarms.Rows.Clear();
            foreach (var en in rows)
            {
                int r = gridAlarms.Rows.Add();
                var row = gridAlarms.Rows[r];
                row.Cells[ACOL_TIME].Value = en.Time.ToString("HH:mm:ss");
                row.Cells[ACOL_AXIS].Value = en.AxisNo < 0 ? "主站" : (en.AxisNo + 1).ToString();
                row.Cells[ACOL_SEV].Value = en.Sev == AlarmSeverity.Fault ? "故障" : en.Sev == AlarmSeverity.Warning ? "警告" : "信息";
                row.Cells[ACOL_TYPE].Value = en.Type.ToString();
                row.Cells[ACOL_MSG].Value = en.Message;
                row.Cells[ACOL_STATE].Value = en.Active ? "激活" : "已恢复";
                if (en.Active && en.Sev == AlarmSeverity.Fault) { row.DefaultCellStyle.BackColor = Color.FromArgb(248, 215, 218); row.DefaultCellStyle.ForeColor = Color.Firebrick; }
                else if (en.Active && en.Sev == AlarmSeverity.Warning) { row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205); row.DefaultCellStyle.ForeColor = Color.DarkOrange; }
                else row.DefaultCellStyle.ForeColor = Color.Silver;
            }
        }

        // ==================== 物料表 ====================

        void RefreshProductGrid()
        {
            List<Product> snap;
            lock (_prodLock) snap = new List<Product>(_products);
            gridProducts.Rows.Clear();
            foreach (var p in snap)
            {
                int r = gridProducts.Rows.Add();
                var row = gridProducts.Rows[r];
                row.Cells[PCOL_ID].Value = p.Id.ToString();
                row.Cells[PCOL_SRC].Value = p.Source;
                row.Cells[PCOL_CAP].Value = (p.CaptureMasterPos * 360.0 / PULSES_PER_REV).ToString("F1");
                row.Cells[PCOL_TRACK].Value = (p.Travel * 360.0 / PULSES_PER_REV).ToString("F1");
                row.Cells[PCOL_ZONE].Value = p.Picked ? "已抓取" : p.Zone.ToString();
                if (p.Picked) row.DefaultCellStyle.ForeColor = Color.Silver;
                else if (p.Zone == ZoneState.在区) { row.DefaultCellStyle.BackColor = Color.FromArgb(255, 247, 205); row.DefaultCellStyle.ForeColor = Color.DarkGoldenrod; }
                else row.DefaultCellStyle.ForeColor = Color.Black;
            }
        }

        // ==================== 传送带可视化 (GDI+) ====================

        void picConveyor_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int W = picConveyor.Width, H = picConveyor.Height;
            g.Clear(Color.White);

            if (!isRunning) { using (var f = new Font("Microsoft YaHei UI", 10F)) g.DrawString("未连接", f, Brushes.Silver, 12, 12); return; }

            double from = _zoneFromDeg, to = _zoneToDeg;
            double maxDeg = Math.Max(to + 60, 360);
            int marginL = 60, marginR = 16;
            int beltY = H / 2, beltH = 46;
            int x0 = marginL, x1 = W - marginR;
            int beltW = Math.Max(10, x1 - x0);
            Func<double, int> X = deg => x0 + (int)(Math.Max(0, Math.Min(maxDeg, deg)) / maxDeg * beltW);

            // 传送带带体
            using (var belt = new SolidBrush(Color.FromArgb(225, 228, 232)))
                g.FillRectangle(belt, x0, beltY - beltH / 2, beltW, beltH);
            g.DrawRectangle(Pens.Gray, x0, beltY - beltH / 2, beltW, beltH);

            // 抓取区高亮
            int zx0 = X(from), zx1 = X(to);
            using (var zb = new SolidBrush(Color.FromArgb(90, 255, 215, 80)))
                g.FillRectangle(zb, zx0, beltY - beltH / 2, Math.Max(2, zx1 - zx0), beltH);
            using (var zp = new Pen(Color.Goldenrod, 1.5f))
                g.DrawRectangle(zp, zx0, beltY - beltH / 2, Math.Max(2, zx1 - zx0), beltH);
            using (var f = new Font("Microsoft YaHei UI", 8F))
            {
                g.DrawString("传感器", f, Brushes.DimGray, 6, beltY - 8);
                g.DrawString($"抓取区 {from:F0}~{to:F0}°", f, Brushes.DarkGoldenrod, zx0, beltY - beltH / 2 - 16);
            }
            // 传感器竖线 (x=0 处)
            using (var sp = new Pen(Color.SteelBlue, 2f)) g.DrawLine(sp, x0, beltY - beltH / 2 - 6, x0, beltY + beltH / 2 + 6);

            // 物料块
            List<Product> snap;
            lock (_prodLock) snap = new List<Product>(_products);
            using (var sf = new Font("Microsoft YaHei UI", 7.5F))
                foreach (var p in snap)
                {
                    double deg = p.Travel * 360.0 / PULSES_PER_REV;
                    if (deg < 0 || deg > maxDeg) continue;
                    int px = X(deg);
                    var col = p.Picked ? Color.Silver : (p.Zone == ZoneState.在区 ? Color.Goldenrod : Color.SteelBlue);
                    using (var pb = new SolidBrush(col)) g.FillRectangle(pb, px - 9, beltY - 9, 18, 18);
                    g.DrawRectangle(Pens.DimGray, px - 9, beltY - 9, 18, 18);
                    g.DrawString("#" + p.Id, sf, Brushes.Black, px - 9, beltY + 11);
                }

            // 抓取轴指针 (飞行抓取中, 指向当前被抓取物料的行程位置)
            if (_pickEngaged && _pickAxisIndex >= 0)
            {
                long travel = 0;
                lock (_prodLock)
                    foreach (var p in snap)
                        if (p.Id == _pickProductId) { travel = p.Travel; break; }
                int px = X(travel * 360.0 / PULSES_PER_REV);
                var tri = new[] { new Point(px, beltY - beltH / 2 - 8), new Point(px - 7, beltY - beltH / 2 - 22), new Point(px + 7, beltY - beltH / 2 - 22) };
                g.FillPolygon(Brushes.Firebrick, tri);
                using (var f = new Font("Microsoft YaHei UI", 7.5F)) g.DrawString($"抓取轴(轴{_pickAxisIndex + 1})", f, Brushes.Firebrick, px - 24, beltY - beltH / 2 - 38);
            }
        }
    }
}
