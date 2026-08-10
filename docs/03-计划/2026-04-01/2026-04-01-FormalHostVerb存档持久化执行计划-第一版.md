---
标题：FormalHostVerb 存档持久化执行计划
版本号: v1.1
更新日期: 2026-04-01
最后修改者: Claude Sonnet 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 基于《FormalHostVerb存档持久化设计分析》给出的可直接落地的代码修改执行计划，含修改文件、修改位置、具体代码。不含任何推测性修改。
---

# FormalHostVerb 存档持久化执行计划

> 参考设计文档：`docs/04-架构评估/2026-04-01/2026-04-01-FormalHostVerb存档持久化设计分析-第一版.md`

## 任务总览

| 任务 | 文件 | 性质 |
|------|------|------|
| T1 | `Core/Verbs/BdpVerb_Shoot.cs` | 为 `HostResultId` 添加 `ExposeData()` |
| T2 | `Core/VerbHosting/TriggerBodyVerbHostManager.cs` | 添加 verb 壳的序列化与恢复方法 |
| T3 | `Core/Trigger/State/CompTriggerBody.Lifecycle.cs` | 集成 T2 的调用到存读档流程 |
| T4 | `Core/AttackExecution/AttackExecutionPostLoadRecovery.cs` | 修正 stance 终止逻辑（有效暖机续接，旧存档兼容） |
| T5 | `Core/Verbs/BdpVerb_Shoot.cs` | 读档后惰性重建 emission plan（暖机 + burst 剩余发数续接） |

---

## T1：BdpVerb_Shoot — 添加 `HostResultId` 持久化

### 文件
`Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

### 问题
`HostResultId` 是自动属性，无法直接被 `Scribe_Values.Look(ref ...)` 使用。
它是 verb 壳与表达系统之间的唯一纽带，读档后重建 cast 需要它。

### 修改内容

**第一处**：把自动属性改为显式 backing field（只改 `HostResultId`，其余属性不动）

```csharp
// 改前：
public string HostResultId { get; set; }

// 改后：
private string hostResultId;
public string HostResultId
{
    get { return hostResultId; }
    set { hostResultId = value; }
}
```

**第二处**：在类中添加 `ExposeData()` 重写（位置建议放在 `Reset()` 附近）

```csharp
/// <summary>
/// 持久化 BDP Verb 层自身的最小存档字段。
/// base.ExposeData() 已处理 verbState、loadID 等原版 Verb 基础字段。
/// 这里只补充 BDP 特有的、影响读档后正确续接的 HostResultId。
/// </summary>
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Values.Look(ref hostResultId, "hostResultId");
}
```

### 验证点
- 编译通过
- 存档后 XML 内应能搜到 `<hostResultId>` 字段

---

## T2：TriggerBodyVerbHostManager — 添加 verb 壳序列化

### 文件
`Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`

### 问题
`bindings` 字典中的 verb 壳对象不在存档树中，导致 `Stance_Warmup.verb` 和 `Job.verbToUse` 的引用在读档时无法解析。

### 修改内容

**第一处**：添加两个临时存储字段（用于 LoadingVars → PostLoadInit 之间的跨阶段中转）

在类的字段区（`bindings` 字典附近）添加：

```csharp
/// <summary>
/// LoadingVars 阶段反序列化得到的远程壳临时存储。
/// 仅在 PostLoadInit 阶段的 RestoreShellsPostLoad 调用前有效，之后置 null。
/// </summary>
private List<BdpVerb_FormalHostShoot> pendingLoadedRangedShells;

/// <summary>
/// LoadingVars 阶段反序列化得到的近战壳临时存储。
/// 仅在 PostLoadInit 阶段的 RestoreShellsPostLoad 调用前有效，之后置 null。
/// </summary>
private List<BdpVerb_FormalHostMelee> pendingLoadedMeleeShells;
```

**第二处**：添加 `ExposeVerbShells()` 方法

位置建议在 `Tick()` 方法之后，`TryGetBinding()` 之前：

```csharp
/// <summary>
/// 把当前管理器持有的全部正式宿主 verb 壳纳入 RimWorld 存档树。
/// 必须在 Saving 和 LoadingVars 两个阶段都调用，保证存档条目对称。
/// 壳的 binding 状态（verbProps 等）由 Refresh() 重新注入，不在这里存储。
/// </summary>
public void ExposeVerbShells()
{
    List<BdpVerb_FormalHostShoot> rangedShells = null;
    List<BdpVerb_FormalHostMelee> meleeShells = null;

    if (Scribe.mode == LoadSaveMode.Saving)
    {
        // 存档时按固定槽位顺序收集所有壳对象
        EnsureFormalBindings();
        rangedShells = new List<BdpVerb_FormalHostShoot>(CompTriggerBody.FormalHostSlots.Length);
        meleeShells = new List<BdpVerb_FormalHostMelee>(CompTriggerBody.FormalHostSlots.Length);
        for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
        {
            BdpFormalVerbHostSlot slot = CompTriggerBody.FormalHostSlots[i];
            BdpFormalVerbBinding binding = TryGetBinding(slot);
            rangedShells.Add(binding?.RangedVerb);
            meleeShells.Add(binding?.MeleeVerb);
        }
    }

    // Deep-save 所有 verb 壳，使它们进入存档树，读档时引用可被解析
    Scribe_Collections.Look(ref rangedShells, "formalRangedShells", LookMode.Deep);
    Scribe_Collections.Look(ref meleeShells, "formalMeleeShells", LookMode.Deep);

    // LoadingVars 阶段暂存加载结果，供 PostLoadInit 阶段使用
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
        pendingLoadedRangedShells = rangedShells;
        pendingLoadedMeleeShells = meleeShells;
    }
}
```

**第三处**：添加 `RestoreShellsPostLoad()` 方法

位置紧接 `ExposeVerbShells()` 之后：

```csharp
/// <summary>
/// PostLoadInit 阶段：用加载出来的 verb 壳重建 bindings 并重新注入运行时引用。
/// 必须在 CompTriggerBody 调用 RefreshProjectedOutputs() 之前执行。
/// </summary>
public void RestoreShellsPostLoad(CompTriggerBody verbOwner)
{
    List<BdpVerb_FormalHostShoot> rangedShells = pendingLoadedRangedShells;
    List<BdpVerb_FormalHostMelee> meleeShells = pendingLoadedMeleeShells;
    pendingLoadedRangedShells = null;
    pendingLoadedMeleeShells = null;

    for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
    {
        BdpFormalVerbHostSlot slot = CompTriggerBody.FormalHostSlots[i];

        // 优先使用从存档加载的壳；加载失败则新建（降级保护）
        BdpVerb_FormalHostShoot rangedVerb =
            rangedShells != null && i < rangedShells.Count ? rangedShells[i] : null;
        BdpVerb_FormalHostMelee meleeVerb =
            meleeShells != null && i < meleeShells.Count ? meleeShells[i] : null;

        if (rangedVerb == null)
        {
            rangedVerb = CreateRangedShell(slot);
        }

        if (meleeVerb == null)
        {
            meleeVerb = CreateMeleeShell(slot);
        }

        // 重新注入运行时引用（owner、slot、verbTracker、caster）
        // loadID 由基类 ExposeData() 已恢复，此处重设值相同，无副作用
        rangedVerb.InitializeFormalHost(verbOwner, slot);
        meleeVerb.InitializeFormalHost(verbOwner, slot);

        // 填入 bindings 表（不重置 verbState，保留读档恢复的执行状态）
        if (!bindings.ContainsKey(slot))
        {
            bindings[slot] = new BdpFormalVerbBinding
            {
                Slot = slot,
                State = CreateUnavailableState(slot),
                RangedVerb = rangedVerb,
                MeleeVerb = meleeVerb
            };
        }
        else
        {
            bindings[slot].RangedVerb = rangedVerb;
            bindings[slot].MeleeVerb = meleeVerb;
        }
    }
}
```

### 验证点
- `FormalHostSlots.Length` = 8，`rangedShells` 和 `meleeShells` 各应有 8 条
- 编译通过
- 两个 `pending*` 字段在 `RestoreShellsPostLoad()` 结束后被置 null

---

## T3：CompTriggerBody.Lifecycle.cs — 集成序列化调用

### 文件
`Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`

### 当前 `PostExposeData()` 结构（简化）

```csharp
public override void PostExposeData()
{
    base.PostExposeData();

    if (Scribe.mode == LoadSaveMode.Saving)  // ← EnsureInternalState 只在此处调用
    {
        EnsureInternalState();
        EnsureChipContainer();
        EnsureSlots();
    }

    Scribe_Collections.Look(ref mainSlots, ...);
    Scribe_Collections.Look(ref subSlots, ...);
    Scribe_Collections.Look(ref specialSlots, ...);
    Scribe_Deep.Look(ref chipContainer, ...);
    Scribe_Deep.Look(ref mainSwitchContext, ...);
    Scribe_Deep.Look(ref subSwitchContext, ...);
    Scribe_Deep.Look(ref specialSwitchContext, ...);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
        BeginPostLoadRestorePhase();
        try
        {
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();
            RestoreSlotTruth();
            RebuildContainerFromSlotTruth();
            RefreshProjectedOutputs();
        }
        finally
        {
            EndPostLoadRestorePhase();
        }
    }
}
```

### 修改内容

**第一处**：把 `EnsureInternalState()` 从 Saving 专属块提出来，改为任何 Scribe 阶段都调用

原因：`LoadingVars` 阶段 `verbHostManager.ExposeVerbShells()` 需要 `verbHostManager` 已存在。
`verbHostManager` 在构造函数里已初始化，这个调用是幂等的（null 保护），无副作用。

```csharp
// 改前：
if (Scribe.mode == LoadSaveMode.Saving)
{
    EnsureInternalState();
    EnsureChipContainer();
    EnsureSlots();
}

// 改后：
EnsureInternalState();  // 所有 Scribe 阶段都需要保证 verbHostManager 非 null
if (Scribe.mode == LoadSaveMode.Saving)
{
    EnsureChipContainer();
    EnsureSlots();
}
```

**第二处**：在其他 `Scribe_*.Look` 调用之后，`PostLoadInit` 块之前，添加 verb 壳序列化调用

```csharp
    Scribe_Deep.Look(ref specialSwitchContext, "specialSwitchContext");

    // [新增] 正式宿主 verb 壳必须进入存档树，保证 Stance_Warmup.verb 和
    // Job.verbToUse 的引用在读档后可被正确解析。
    // Saving 和 LoadingVars 两个阶段都必须调用（Scribe 对称性要求）。
    verbHostManager.ExposeVerbShells();

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
```

**第三处**：在 `PostLoadInit` 块内，`RefreshProjectedOutputs()` 之前，添加 verb 壳恢复调用

```csharp
        EnsureInternalState();
        EnsureChipContainer();
        EnsureSlots();
        // [新增] 用 LoadingVars 阶段加载的 verb 壳重建 bindings，
        // 并重新注入 owner/slot 等运行时引用。
        // 必须在 RefreshProjectedOutputs() 之前执行，
        // 因为 Refresh 会调用 SyncFormalBinding 对每个壳注入 verbProps。
        verbHostManager.RestoreShellsPostLoad(this);
        RestoreSlotTruth();
        RebuildContainerFromSlotTruth();
        RefreshProjectedOutputs();
```

### 修改后完整结构（关键片段）

```csharp
public override void PostExposeData()
{
    base.PostExposeData();

    EnsureInternalState();                    // [修改] 从 Saving 块提出，始终执行
    if (Scribe.mode == LoadSaveMode.Saving)
    {
        EnsureChipContainer();
        EnsureSlots();
    }

    Scribe_Collections.Look(ref mainSlots, "mainSlots", LookMode.Deep);
    Scribe_Collections.Look(ref subSlots, "subSlots", LookMode.Deep);
    Scribe_Collections.Look(ref specialSlots, "specialSlots", LookMode.Deep);
    Scribe_Deep.Look(ref chipContainer, "chipContainer", this);
    Scribe_Deep.Look(ref mainSwitchContext, "mainSwitchContext");
    Scribe_Deep.Look(ref subSwitchContext, "subSwitchContext");
    Scribe_Deep.Look(ref specialSwitchContext, "specialSwitchContext");

    verbHostManager.ExposeVerbShells();        // [新增]

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
        BeginPostLoadRestorePhase();
        try
        {
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();
            verbHostManager.RestoreShellsPostLoad(this);  // [新增]
            RestoreSlotTruth();
            RebuildContainerFromSlotTruth();
            RefreshProjectedOutputs();
        }
        finally
        {
            EndPostLoadRestorePhase();
        }
    }
}
```

### 验证点
- 编译通过
- 存档 XML 中可以找到 `<formalRangedShells>` 和 `<formalMeleeShells>` 节点
- 每个节点内应有 8 个子项（对应 8 个 FormalHostSlots）
- 读档日志中不再出现 "is referenced but not deep-saved" 和 "Could not resolve reference" 报错

---

## T4：AttackExecutionPostLoadRecovery — 修正 stance 终止逻辑

### 文件
`Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`

### 问题
完成 T1-T5 后，verb 对象在读档后可以正常解析，有效的暖机 stance 应续接执行而非被终止。
原实现（`return IsBdpFormalHostVerb(busyStance.verb)`）一律终止所有 BDP busy stance，在 T5 修复后将错误中断合法的暖机续接。T4 需修正为：
- 允许 verb 已正确加载且可用的 BDP 暖机 stance 继续执行
- 仅终止 verb 为 null（旧存档兼容）或 verb 不可用（`Available()` = false）的 stance

### 修改内容

修改 `HasStaleBdpBusyStance` 方法：

```csharp
// 改前：
private static bool HasStaleBdpBusyStance(Pawn pawn)
{
    if (!(pawn?.stances?.curStance is Stance_Busy busyStance))
    {
        return false;
    }

    return IsBdpFormalHostVerb(busyStance.verb);
}

// 改后（T5 联动版本）：
private static bool HasStaleBdpBusyStance(Pawn pawn)
{
    if (!(pawn?.stances?.curStance is Stance_Busy busyStance))
    {
        return false;
    }

    // 防御性：verb 为 null（旧存档兼容：T1-T5 修复前保存的存档，引用解析失败）
    if (busyStance.verb == null)
    {
        return HasStaleBdpJob(pawn);
    }

    // T1-T5 修复后：verb 已正确加载，有效的暖机 stance 应续接执行，不应被终止。
    // 仅在 verb 不可用时（表达系统绑定已失效）才终止。
    if (IsBdpFormalHostVerb(busyStance.verb))
    {
        return !busyStance.verb.Available();
    }

    return false;
}
```

### 验证点
- 使用 T1-T5 修复后保存的存档，暖机中读档后 stance 继续执行（不被错误终止）
- 使用修复前保存的旧存档（verb 为 null），读档后 stance 被正确终止，不再出现 NullReferenceException
- verb 不可用（`Available()` = false）的 stance 被正确终止

---

## T5：BdpVerb_Shoot — 读档后惰性重建 emission plan

### 文件
`Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

### 问题
`TryStartCastOn()` 在暖机开始前调用 `TryPreparePendingEmission()` 准备 emission plan。读档后 `TryStartCastOn()` 不会再次调用，导致：

- **暖机续接时**：`WarmupComplete()` 触发，但 emission plan 为 null → `state = Idle`，一发不射
- **Burst 续接时**：`TryCastShot()` 发现 emission plan 为 null → 返回 false，`burstShotsLeft` 递减至 0，剩余发数全部白费

原版已持久化的数据提供了充分的重建材料：
- `burstShotsLeft`（vanilla `Verb.ExposeData()`）：剩余发数，续接时无需重置
- `currentTarget`（vanilla `Verb.ExposeData()`）：攻击目标，重建 emission plan 的必要入参
- `HostResultId`（T1 新增）：表达结果 ID，确定使用哪套 verbProps 和弹道参数

### 修改内容

**第一处**：修改 `WarmupComplete()` — 暖机续接时惰性重建 emission plan

```csharp
public override void WarmupComplete()
{
    // [新增] 读档后恢复路径：TryStartCastOn() 未重新调用，emission plan 为 null。
    // 使用已持久化的 HostResultId + currentTarget 重建计划，保证暖机结束后正常发射。
    if (!HasPendingEmissionPlan()
        && currentTarget.IsValid
        && !string.IsNullOrWhiteSpace(HostResultId))
    {
        TryPreparePendingEmission(currentTarget);
    }

    base.WarmupComplete();
}
```

**第二处**：修改 `TryCastShot()` — burst 续接时惰性重建 emission plan

```csharp
protected override bool TryCastShot()
{
    // [新增] 读档后恢复路径：burst 进行中时 emission plan 丢失（plan 不持久化）。
    // 使用已持久化的 HostResultId + currentTarget 重建计划。
    // 注意：不重置 burstShotsLeft —— 原版已持久化的剩余发数即为此次续接的发射上限。
    if (!HasPendingEmissionPlan()
        && state == VerbState.Bursting
        && currentTarget.IsValid
        && !string.IsNullOrWhiteSpace(HostResultId))
    {
        TryPreparePendingEmission(currentTarget);
    }

    if (!TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window))
    {
        // ... 原有逻辑不变 ...
    }
    // ... 其余代码不变 ...
}
```

### 验证点
- 暖机中存档 → 读档后暖机倒计时完成 → 正常发射（不再 state = Idle）
- Burst 3/5 时存档 → 读档后续接剩余 2 发 → burst 正常完成，不多发不少发
- 若 `HostResultId` 失效（极少见），`TryPreparePendingEmission` 返回 false，当前攻击安全终止

---

## 执行顺序与依赖关系

```
T1（BdpVerb_Shoot.ExposeData）
  │
  ├──▶ T5（BdpVerb_Shoot 惰性重建 emission plan）
  │        ← 依赖 T1：需 HostResultId 字段可用
  │
  ▼
T2（TriggerBodyVerbHostManager 序列化方法）
  │    ← 依赖 T1：verb 对象的 ExposeData 需先存在才有意义
  ▼
T3（CompTriggerBody.Lifecycle.cs 集成调用）
  │    ← 依赖 T2：调用 T2 提供的方法

T4（修正 stance 终止逻辑，可并行，建议最后）
     ← 依赖 T5 的语义：T5 后有效暖机 stance 不应被终止
```

**推荐执行顺序：T1 → T2 → T3 → T5 → 编译验证 → T4 → 完整测试**

---

## 测试检查清单

### 存档阶段
- [ ] 激活芯片，不在攻击中存档 → 无警告，读档后芯片状态正常
- [ ] 激活芯片，在暖机中存档 → 无 "not deep-saved" 警告

### 读档阶段
- [ ] 读档日志中无 "Could not resolve reference to object with loadID Verb_BDP_FormalHost_..."
- [ ] 读档后无 NullReferenceException
- [ ] 读档后 pawn 恢复正常状态（不卡死、不持续刷 Exception）

### 暖机续接
- [ ] 暖机中存档 → 读档后暖机继续 → 暖机完成后正常射击
- [ ] 暖机中存档 → 读档后 HostResultId 对应的表达结果仍可用 → 发射计划成功生成

### 降级场景
- [ ] Burst 进行中存档 → 读档后剩余发数续接 → burst 正常完成（不多发，不少发）
- [ ] 激活后不攻击直接存档 → 读档后可正常发起攻击

---

## 风险说明

| 风险 | 等级 | 说明 |
|------|------|------|
| `EnsureInternalState()` 提前调用导致副作用 | 低 | 该方法只做 null 检查和对象创建，幂等，无业务副作用 |
| 旧存档 `formalRangedShells` 节点缺失 | 低 | `Scribe_Collections.Look` 在节点缺失时返回 null，`RestoreShellsPostLoad` 对 null 列表有降级保护，会新建壳 |
| T5 重建 emission plan 时 `HostResultId` 对应的表达结果已失效 | 低 | `RefreshProjectedOutputs()` 先于 T5 消费点执行，正常情况结果已重新注入；若结果不存在，`TryPreparePendingEmission` 返回 false，当前攻击安全终止（降级） |
| melee verb 壳导致近战 stance 问题 | 极低 | 近战 stance 不使用 `Stance_Warmup`，近战攻击路径不涉及 verbToUse 持久化，当前错误日志中无近战相关报错 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-04-01 | 初版，完整覆盖 T1-T4 四个任务的文件位置、修改内容和验证点 | Claude Sonnet 4.6 |
| v1.1 | 2026-04-01 | 新增 T5（惰性重建 emission plan）；修正 T4 角色（续接保护而非全量终止）；修正 burst 测试项；更新执行顺序 | Claude Sonnet 4.6 |
