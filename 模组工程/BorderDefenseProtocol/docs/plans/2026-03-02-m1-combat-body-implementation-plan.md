---
标题：M1里程碑实现计划 - 战斗体核心循环
版本号: v1.0
更新日期: 2026-03-02
最后修改者: Claude Sonnet 4.6
标签: [文档][用户未确认][进行中][未锁定]
摘要: BDP战斗体系统M1里程碑的详细实现计划。采用渐进式实现+按需验证策略，分为3个阶段（阶段0前置验证→阶段1快照系统→阶段2双层FSM→阶段3 UI与配置）。关键组件给出代码级细节，其他组件给出任务级描述。目标：完整的"激活→战斗体状态→主动解除→恢复"循环可在游戏内跑通。
---

# M1里程碑实现计划 - 战斗体核心循环

## 前置文档

- 需求来源：`docs/第六阶段：深度系统设计/战斗体系统需求设计文档.md`（v4.1，43条需求）
- 路线图：`docs/第六阶段：深度系统设计/战斗体系统开发路线图.md`（v1.0）
- 架构设计：`docs/第六阶段：深度系统设计/战斗体系统架构设计文档.md`（v1.0）
- 技术参考：`docs/第六阶段：深度系统设计/战斗体系统RimWorld原生技术参考分析.md`（v1.0）

---

## 一、实现策略确认

### 1.1 策略选择（基于brainstorming对话）

通过5轮问答确认的实现策略：

| 决策项 | 选择 | 说明 |
|--------|------|------|
| 详细程度 | 混合方式 | 关键组件（双层FSM、快照系统）代码级细节，其他组件任务级描述 |
| 实现顺序 | 依赖驱动 | 按依赖关系从底层到上层：快照容器 → Gene扩展 → HediffComp → Gizmo → XML |
| 验证策略 | 分阶段验证 | 底层组件代码审查，上层组件游戏内测试 |
| 占位值策略 | XML配置 | 所有占位值通过XML的HediffDef/ThingDef配置，便于后续调整 |
| 风险处理 | 风险前置验证 | 中等严重度风险（Hediff回滚、容器转移）做前置验证 |
| 实现方法 | 渐进式实现+按需验证 | 3个实现组，每组都有明确的依赖关系和交付物 |

### 1.2 M1目标

**核心目标**：完整的"激活→战斗体状态→主动解除→恢复"循环可在游戏内跑通

**覆盖需求**：路线图M1的25条需求（NR-002~004, 005~007, 018~019, 022~025, 026~028, 029~031, 033~034, 037, 039）

**不包含内容**：
- 伤害系统（M2）
- 死亡拦截与破裂（M3）
- 紧急脱离（M4）
- 集成清理（M5）

---

## 二、总体结构和阶段划分

### 2.1 实现阶段总览

```
阶段0：前置验证（2个中等风险）
  ├─ 验证任务1：Hediff快照回滚机制验证
  └─ 验证任务2：容器转移顺序验证

阶段1：快照系统组（底层基础）
  ├─ CombatBodySnapshot类（IExposable容器）
  ├─ Hediff快照逻辑
  ├─ Need快照逻辑
  └─ 衣物/物品容器

阶段2：双层FSM组（核心逻辑）
  ├─ Gene_TrionGland扩展（外层FSM）
  ├─ HediffComp_CombatBodyActive（内层FSM）
  ├─ 激活11步原子操作
  └─ 解除流程（主动/被动）

阶段3：UI与配置组（用户交互）
  ├─ 静态可否决事件系统
  ├─ Gizmo生成（激活/解除按钮）
  ├─ HediffDef XML配置
  └─ BDP_DefOf引用
```

### 2.2 依赖关系

```
阶段0（前置验证）
  ↓ 产出参考代码
阶段1（快照系统）← 独立实现
  ↓ 提供快照能力
阶段2（双层FSM）← 依赖阶段1
  ↓ 提供状态和接口
阶段3（UI与配置）← 依赖阶段2
  ↓
M1交付
```

### 2.3 验证策略

| 阶段 | 验证方式 | 验证重点 |
|------|----------|----------|
| 阶段0 | 独立技术验证 | 产出可直接使用的参考代码 |
| 阶段1 | 代码审查 + 序列化测试 | IExposable正确性、容器所有权 |
| 阶段2 | 代码审查 + 状态转换测试 | FSM状态转换逻辑、协议接口 |
| 阶段3 | 游戏内完整流程测试 | 6个测试场景全部通过 |

### 2.4 新建文件清单

```
BDP/Combat/                              L2（全新模块）
├── Snapshot/
│   └── CombatBodySnapshot.cs           IExposable Memento容器
├── Hediffs/
│   └── HediffComp_CombatBodyActive.cs  内层FSM（Channeling/Established/DismissChanneling）
└── CombatBodyModuleInit.cs             [StaticConstructorOnStartup] 事件注册

Defs/Combat/
└── HediffDefs_CombatBody.xml           战斗体Hediff定义（含HediffComp + 全部XML特性配置）
```

### 2.5 修改文件清单

| 文件 | 修改内容 | 层级 |
|------|----------|------|
| `Core/Genes/Gene_TrionGland.cs` | 扩展：外层FSM枚举、快照引用、激活/解除方法、静态事件、Gizmo生成、ExposeData | L0 |
| `Core/Defs/BDP_DefOf.cs` | 新增：`BDP_CombatBodyActive`、`BDP_Exhaustion` HediffDef引用 | L0 |

---

## 三、阶段0：前置验证任务

### 3.1 验证任务1：Hediff快照回滚机制验证

**风险来源**：路线图标注"Hediff快照回滚时的副作用控制"为中等严重度风险

**验证目标**：
- 确认Hediff可以通过`Scribe_Collections.Look(LookMode.Deep)`完整序列化
- 验证通过`Pawn_HealthTracker.AddHediff/RemoveHediff` API回放Hediff的可行性
- 识别回滚时可能触发的副作用（如Hediff添加时的通知、缓存更新等）
- 确定需要拦截的副作用清单

**验证方法**：
1. 创建测试场景：给Pawn添加多种Hediff（伤口、疾病、永久伤残、植入物）
2. 使用`Scribe_Collections.Look`序列化Hediff列表到内存
3. 移除所有Hediff
4. 从序列化数据反序列化并通过`Pawn_HealthTracker.AddHediff`回放
5. 观察并记录回放过程中触发的副作用（日志、事件、缓存更新等）
6. 测试`BodyPartRecord`引用的跨存档一致性（使用`Scribe_BodyParts.Look`）

**预期产出**：
- 验证代码片段（可直接用于CombatBodySnapshot实现）
- 需要拦截的副作用清单
- `RestoreScope`受控还原上下文的设计方案（如需要）

**成功标准**：
- [ ] Hediff列表可以完整序列化和反序列化
- [ ] 回放后Pawn的健康状态与快照时一致
- [ ] 识别出所有需要处理的副作用
- [ ] `DirtyCache()`调用时机明确

---

### 3.2 验证任务2：容器转移顺序验证

**风险来源**：路线图标注"Apparel/Inventory容器转移顺序错误"为中等严重度风险

**验证目标**：
- 确认`ThingOwner`的所有权转移规则
- 验证衣物从`pawn.apparel.wornApparel`转移到自定义`ThingOwner`的正确顺序
- 验证物品从`pawn.inventory.innerContainer`转移到自定义`ThingOwner`的正确顺序
- 确认恢复时的反向转移流程

**验证方法**：
1. 创建测试场景：Pawn穿戴多件衣物，背包有多个物品
2. 创建自定义`ThingOwner`容器（模拟快照容器）
3. 测试转移顺序：
   - 方案A：`Remove()` → `TryAdd()`
   - 方案B：`TryTransferToContainer()`
   - 方案C：`TryDrop()` → `TryAdd()`
4. 测试恢复顺序：
   - 衣物：从快照容器 → `pawn.apparel.Wear(dropReplacedApparel:false)`
   - 物品：从快照容器 → `pawn.inventory.innerContainer`
5. 观察`holdingOwner`冲突和容器状态

**预期产出**：
- 正确的转移顺序流程图
- 验证代码片段（可直接用于CombatBodySnapshot实现）
- 容器一致性检查点清单

**成功标准**：
- [ ] 衣物和物品可以无损转移到快照容器
- [ ] 恢复时可以正确穿戴/入包
- [ ] 无`holdingOwner`冲突错误
- [ ] 容器状态始终一致

---

### 3.3 阶段0总结

**并行执行**：2个验证任务独立进行，可并行执行

**产出物**：
- 验证任务1：Hediff快照回滚参考代码 + 副作用处理方案
- 验证任务2：容器转移参考代码 + 转移顺序流程图

**验证通过标准**：所有成功标准的检查点全部通过

**进入下一阶段条件**：阶段0的2个验证任务全部完成并通过

---

## 四、阶段1：快照系统组

### 4.1 CombatBodySnapshot类设计（代码级细节）

**文件位置**：`BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**类结构**：
```csharp
public class CombatBodySnapshot : IExposable
{
    // ===== 字段 =====

    // Hediff快照列表（深拷贝）
    private List<Hediff> hediffSnapshots;

    // 需求值快照（DefName → 当前值）
    private Dictionary<string, float> needValues;

    // 衣物容器（物理转移）
    private ThingOwner<Apparel> apparelContainer;

    // 物品容器（物理转移）
    private ThingOwner<Thing> inventoryContainer;

    // 所属Pawn引用（用于容器初始化）
    private Pawn pawn;

    // ===== 构造函数 =====

    public CombatBodySnapshot(Pawn pawn)
    {
        this.pawn = pawn;
        this.apparelContainer = new ThingOwner<Apparel>(this);
        this.inventoryContainer = new ThingOwner<Thing>(this);
        this.hediffSnapshots = new List<Hediff>();
        this.needValues = new Dictionary<string, float>();
    }

    // ===== 核心方法 =====

    /// <summary>
    /// 拍摄快照（在激活引导完成时调用）
    /// </summary>
    public void TakeSnapshot()
    {
        // 1. Hediff快照（深拷贝）
        SnapshotHediffs();

        // 2. 需求值快照
        SnapshotNeeds();

        // 3. 衣物物理转移
        TransferApparelToSnapshot();

        // 4. 物品物理转移
        TransferInventoryToSnapshot();
    }

    /// <summary>
    /// 恢复快照（在解除时调用）
    /// </summary>
    public void RestoreSnapshot()
    {
        // 1. Hediff恢复（通过健康系统API）
        RestoreHediffs();

        // 2. 需求值恢复
        RestoreNeeds();

        // 3. 衣物恢复
        RestoreApparel();

        // 4. 物品恢复
        RestoreInventory();
    }

    // ===== 序列化 =====

    public void ExposeData()
    {
        // Hediff列表（LookMode.Deep确保完整序列化）
        Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);

        // 需求值字典
        Scribe_Collections.Look(ref needValues, "needValues", LookMode.Value, LookMode.Value);

        // 衣物容器
        Scribe_Deep.Look(ref apparelContainer, "apparelContainer", this);

        // 物品容器
        Scribe_Deep.Look(ref inventoryContainer, "inventoryContainer", this);

        // Pawn引用
        Scribe_References.Look(ref pawn, "pawn");
    }
}
```

---

### 4.2 Hediff快照逻辑（代码级细节）

**关键方法实现**：

```csharp
private void SnapshotHediffs()
{
    hediffSnapshots.Clear();

    // 遍历所有Hediff并深拷贝
    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
    {
        // 使用Scribe系统的深拷贝机制
        // 注意：具体实现基于阶段0验证任务1的结果
        hediffSnapshots.Add(DeepCopyHediff(hediff));
    }
}

private void RestoreHediffs()
{
    // 使用受控还原上下文（RestoreScope，如阶段0验证确定需要）
    // using (new RestoreScope())
    // {
        // 1. 移除所有当前Hediff（战斗体Hediff + 战斗体期间产生的Hediff）
        List<Hediff> toRemove = new List<Hediff>(pawn.health.hediffSet.hediffs);
        foreach (Hediff hediff in toRemove)
        {
            pawn.health.RemoveHediff(hediff);  // 使用健康系统API
        }

        // 2. 从快照恢复Hediff
        foreach (Hediff hediff in hediffSnapshots)
        {
            pawn.health.AddHediff(hediff);  // 使用健康系统API
        }

        // 3. 刷新缓存
        pawn.health.hediffSet.DirtyCache();
    // }
}
```

**技术要点**（基于架构文档v1.1）：
- Hediff回滚采用**健康系统API优先**：通过`Pawn_HealthTracker.AddHediff/RemoveHediff`操作
- 对回滚时不希望触发的副作用采用**受控还原上下文（RestoreScope）** + 定向拦截
- 具体实现细节在阶段0验证任务1中确定

---

### 4.3 Need快照逻辑（任务级描述）

**实现任务**：
- 遍历Pawn的需求列表，记录被冻结的需求（Food、Rest、Comfort）的当前值
- 存储为`Dictionary<string, float>`（DefName → CurLevel）
- 恢复时：重新添加需求（如果被`disablesNeeds`移除），设置`need.CurLevel = savedValue`

**关键点**：
- 只记录被冻结的需求，心理需求（Joy、Beauty、Outdoors）不记录
- Need对象不可脱离Pawn，只存数值
- Comfort使用`disablesNeeds`移除，恢复时需要重新添加

---

### 4.4 衣物/物品容器（任务级描述）

**衣物转移任务**：
- 激活时：`pawn.apparel.wornApparel` → `apparelContainer`
  - 使用`Remove()`从Pawn移除
  - 使用`TryAdd()`添加到快照容器
- 恢复时：`apparelContainer` → `pawn.apparel`
  - 先从快照容器转移出（`Remove()`）
  - 再`pawn.apparel.Wear(dropReplacedApparel:false)`
  - 具体顺序基于阶段0验证任务2的结果

**物品转移任务**：
- 激活时：`pawn.inventory.innerContainer` → `inventoryContainer`
- 恢复时：`inventoryContainer` → `pawn.inventory.innerContainer`
- 使用`ThingOwner.TryTransferToContainer()`确保所有权正确转移

**容器一致性检查**：
- 转移前检查`holdingOwner`状态
- 转移后验证容器计数
- 添加断言确保无物品丢失

---

### 4.5 阶段1验证标准

**代码审查检查点**：
- [ ] CombatBodySnapshot实现IExposable
- [ ] 所有字段正确序列化
- [ ] Hediff回滚使用健康系统API
- [ ] 容器转移遵循所有权规则（基于阶段0验证结果）

**序列化测试**：
- [ ] 创建快照 → 保存游戏 → 读档 → 验证快照数据完整
- [ ] BodyPartRecord引用跨存档一致
- [ ] 衣物/物品容器正确序列化

**阶段1交付物**：
- `CombatBodySnapshot.cs`完整实现
- 通过代码审查和序列化测试

---

## 五、阶段2：双层FSM组

### 5.1 Gene_TrionGland扩展（代码级细节）

**文件位置**：`BDP/Core/Genes/Gene_TrionGland.cs`（扩展现有文件）

**新增字段**：
```csharp
// 外层FSM状态枚举
public enum CombatBodyState { Inactive, Active, Cooldown }

// 新增字段
private CombatBodyState outerState = CombatBodyState.Inactive;
private CombatBodySnapshot snapshot;  // 快照引用
private int cooldownEndTick;          // CD结束tick

// 静态可否决事件（L0不依赖L1/L2）
public static event Func<Pawn, CanActivateResult> QueryCanActivate;
public static event Action<Pawn, CombatBodyEndReason> OnPassiveBreak;

// 属性（供外部查询）
public CombatBodyState OuterState => outerState;
```

**新增方法**：
```csharp
/// <summary>
/// 激活战斗体（Gizmo调用）
/// </summary>
public void InitiateChanneling()
{
    // 1. 前置检查
    if (outerState != CombatBodyState.Inactive) return;
    if (outerState == CombatBodyState.Cooldown && Find.TickManager.TicksGame < cooldownEndTick) return;

    // 2. 可否决事件检查
    var result = QueryCanActivate?.Invoke(pawn);
    if (result?.Vetoed == true)
    {
        Messages.Message(result.BlockReason, MessageTypeDefOf.RejectInput);
        return;
    }

    // 3. Trion检查和分配
    var comp = pawn.GetComp<CompTrion>();
    if (comp.Available < ActivationCost) return;
    comp.Allocate(ActivationCost);

    // 4. 添加战斗体Hediff（通过DefName字符串，无C#依赖）
    pawn.health.AddHediff(BDP_DefOf.BDP_CombatBodyActive);

    // 5. 状态切换
    outerState = CombatBodyState.Active;
}

/// <summary>
/// 主动解除战斗体（Gizmo调用）
/// </summary>
public void DismissChanneling()
{
    if (outerState != CombatBodyState.Active) return;

    // 通知HediffComp进入DismissChanneling状态
    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(BDP_DefOf.BDP_CombatBodyActive);
    var comp = hediff?.TryGetComp<HediffComp_CombatBodyActive>();
    comp?.StartDismissChanneling();
}

/// <summary>
/// 战斗体结束回调（HediffComp唯一回调入口）
/// </summary>
public void OnCombatBodyEnded(CombatBodyEndReason reason)
{
    var comp = pawn.GetComp<CompTrion>();

    if (reason == CombatBodyEndReason.Voluntary)
    {
        // 主动解除路径
        comp.Release(ActivationCost);  // 返还占用量
        comp.SetFrozen(false);
        // CD = 0（接口保留，数值后调）
        cooldownEndTick = Find.TickManager.TicksGame;
    }
    else // Passive
    {
        // 被动破裂路径
        OnPassiveBreak?.Invoke(pawn, reason);  // 广播事件（紧急脱离在Combat/模块处理）
        comp.ForceDeplete();  // 清零
        comp.SetFrozen(false);
        pawn.health.AddHediff(BDP_DefOf.BDP_Exhaustion);  // 枯竭debuff
        // CD = 长CD（占位值从XML读取）
        cooldownEndTick = Find.TickManager.TicksGame + ExhaustionCooldownTicks;
    }

    // 共同流程：快照恢复
    snapshot?.RestoreSnapshot();
    snapshot = null;

    // 状态切换
    outerState = CombatBodyState.Cooldown;
}

/// <summary>
/// Tick处理（CD倒计时）
/// </summary>
public override void Tick()
{
    base.Tick();

    if (outerState == CombatBodyState.Cooldown && Find.TickManager.TicksGame >= cooldownEndTick)
    {
        outerState = CombatBodyState.Inactive;
    }
}
```

**ExposeData扩展**：
```csharp
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Values.Look(ref outerState, "outerState", CombatBodyState.Inactive);
    Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
    Scribe_Deep.Look(ref snapshot, "snapshot", pawn);
}
```

---

### 5.2 HediffComp_CombatBodyActive（代码级细节）

**文件位置**：`BDP/Combat/Hediffs/HediffComp_CombatBodyActive.cs`

**HediffCompProperties定义**：
```csharp
public class HediffCompProperties_CombatBodyActive : HediffCompProperties
{
    public int channelingTicks = 120;           // 激活引导时间（占位值）
    public int dismissChannelingTicks = 60;     // 解除引导时间（占位值）
    public float maintenanceDrainRate = 1.0f;   // 维持消耗速率（占位值）

    public HediffCompProperties_CombatBodyActive()
    {
        compClass = typeof(HediffComp_CombatBodyActive);
    }
}
```

**HediffComp实现**：
```csharp
public class HediffComp_CombatBodyActive : HediffComp
{
    // 内层FSM状态
    public enum InnerState { Channeling, Established, DismissChanneling }

    private InnerState innerState = InnerState.Channeling;
    private int stateStartTick;

    public HediffCompProperties_CombatBodyActive Props => (HediffCompProperties_CombatBodyActive)props;

    public override void CompPostMake()
    {
        base.CompPostMake();
        innerState = InnerState.Channeling;
        stateStartTick = Find.TickManager.TicksGame;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        switch (innerState)
        {
            case InnerState.Channeling:
                TickChanneling();
                break;
            case InnerState.Established:
                TickEstablished();
                break;
            case InnerState.DismissChanneling:
                TickDismissChanneling();
                break;
        }
    }

    private void TickChanneling()
    {
        int elapsed = Find.TickManager.TicksGame - stateStartTick;

        // 检查打断条件（受伤/倒地/失去意识）
        if (CheckInterruption())
        {
            TriggerInterruption();
            return;
        }

        // 引导完成
        if (elapsed >= Props.channelingTicks)
        {
            ExecuteActivation();  // 11步原子操作
            innerState = InnerState.Established;
            stateStartTick = Find.TickManager.TicksGame;
        }
    }

    private void TickEstablished()
    {
        // M1中只检查Trion耗尽（其他破裂条件在M3补全）
        var comp = Pawn.GetComp<CompTrion>();
        if (comp.Available <= 0)
        {
            TriggerPassiveCollapse(CombatBodyEndReason.TrionDepleted);
        }
    }

    private void TickDismissChanneling()
    {
        int elapsed = Find.TickManager.TicksGame - stateStartTick;

        // 被动破裂优先
        var comp = Pawn.GetComp<CompTrion>();
        if (comp.Available <= 0)
        {
            TriggerPassiveCollapse(CombatBodyEndReason.TrionDepleted);
            return;
        }

        // 解除完成
        if (elapsed >= Props.dismissChannelingTicks)
        {
            var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
            gene?.OnCombatBodyEnded(CombatBodyEndReason.Voluntary);
            Pawn.health.RemoveHediff(parent);
        }
    }

    /// <summary>
    /// 执行激活的11步原子操作（NR-004）
    /// </summary>
    private void ExecuteActivation()
    {
        var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        var comp = Pawn.GetComp<CompTrion>();

        // 1. 拍摄快照
        gene.snapshot = new CombatBodySnapshot(Pawn);
        gene.snapshot.TakeSnapshot();

        // 2. Hediff替换（快照中已处理）

        // 3-4. 衣物/物品转移（快照中已处理）

        // 5. 需求冻结（由HediffDef XML驱动，无需代码）

        // 6. 芯片注册（已在Gene.InitiateChanneling中完成）

        // 7. 冻结Trion恢复
        comp.SetFrozen(true);

        // 8. 注册维持消耗
        comp.RegisterDrain("CombatBodyMaintenance", Props.maintenanceDrainRate);

        // 9. 强制征召
        if (Pawn.drafter == null)
            Pawn.drafter = new Pawn_DraftController(Pawn);
        Pawn.drafter.Drafted = true;

        // 10. Spawn战斗体外观Apparel
        SpawnCombatBodyApparel();

        // 11. 影子HP初始化（M2补全，M1中为空方法占位）
    }

    /// <summary>
    /// 检查引导打断条件
    /// </summary>
    private bool CheckInterruption()
    {
        // 受伤、倒地、失去意识
        return Pawn.Downed || !Pawn.Awake();
    }

    /// <summary>
    /// 引导打断处理
    /// </summary>
    private void TriggerInterruption()
    {
        var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        var comp = Pawn.GetComp<CompTrion>();

        // 返还Trion
        comp.Release(gene.ActivationCost);

        // 移除Hediff
        Pawn.health.RemoveHediff(parent);

        // Gene状态回到Inactive
        gene.outerState = CombatBodyState.Inactive;
    }

    /// <summary>
    /// 触发被动破裂
    /// </summary>
    private void TriggerPassiveCollapse(CombatBodyEndReason reason)
    {
        var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        gene?.OnCombatBodyEnded(reason);
        Pawn.health.RemoveHediff(parent);
    }

    /// <summary>
    /// 主动解除入口（Gene调用）
    /// </summary>
    public void StartDismissChanneling()
    {
        if (innerState == InnerState.Established)
        {
            innerState = InnerState.DismissChanneling;
            stateStartTick = Find.TickManager.TicksGame;
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref innerState, "innerState", InnerState.Channeling);
        Scribe_Values.Look(ref stateStartTick, "stateStartTick", 0);
    }
}
```

---

### 5.3 阶段2验证标准

**代码审查检查点**：
- [ ] 双层FSM状态转换逻辑正确
- [ ] Gene与HediffComp的协议接口清晰
- [ ] 11步原子操作完整执行
- [ ] 被动破裂优先级正确（主动解除引导中Trion耗尽→被动破裂）

**状态转换逻辑测试**：
- [ ] 激活引导 → 完成 → Established
- [ ] 激活引导 → 打断 → Inactive（快照未生成）
- [ ] Established → 主动解除 → Cooldown
- [ ] Established → Trion耗尽 → 被动破裂 → Cooldown
- [ ] 主动解除引导中Trion耗尽 → 被动破裂优先

**阶段2交付物**：
- Gene_TrionGland扩展完成
- HediffComp_CombatBodyActive完整实现
- 通过代码审查和状态转换测试

---

## 六、阶段3：UI与配置组

### 6.1 静态可否决事件系统（任务级描述）

**文件位置**：`BDP/Combat/CombatBodyModuleInit.cs`

**实现任务**：
- 创建`[StaticConstructorOnStartup]`类
- 在静态构造函数中注册`Gene_TrionGland.QueryCanActivate`事件
- 检查逻辑：
  - 是否装备触发体（查询`CompTriggerBody`）
  - 触发体是否有可用槽位
  - 其他模块的否决条件（可扩展）

**事件参数设计**：
```csharp
public class CanActivateResult
{
    public bool Vetoed { get; set; }
    public string BlockReason { get; set; }
}
```

**实现示例**：
```csharp
[StaticConstructorOnStartup]
public static class CombatBodyModuleInit
{
    static CombatBodyModuleInit()
    {
        // 注册激活前置检查
        Gene_TrionGland.QueryCanActivate += CheckCanActivate;
    }

    private static CanActivateResult CheckCanActivate(Pawn pawn)
    {
        // 检查是否装备触发体
        var comp = pawn.GetComp<CompTriggerBody>();
        if (comp == null || !comp.IsEquipped)
        {
            return new CanActivateResult
            {
                Vetoed = true,
                BlockReason = "需要装备触发体"
            };
        }

        // 其他检查...

        return new CanActivateResult { Vetoed = false };
    }
}
```

**关键点**：
- Gene（L0）定义事件，Combat/（L2）注册处理逻辑
- 零耦合，未来新增检查只需添加订阅者

---

### 6.2 Gizmo生成（任务级描述）

**实现位置**：`Gene_TrionGland.GetGizmos()`方法中

**激活Gizmo**：
- 显示条件：`outerState == Inactive && cooldownEndTick <= 当前tick`
- 点击调用：`InitiateChanneling()`
- 图标：战斗体激活图标（从资源加载）
- 标签：从XML翻译键读取（如"BDP_ActivateCombatBody"）
- 描述：显示Trion消耗和前置条件

**解除Gizmo**：
- 显示条件：`outerState == Active`
- 点击调用：`DismissChanneling()`
- 图标：战斗体解除图标
- 标签：从XML翻译键读取（如"BDP_DismissCombatBody"）
- 描述：显示主动解除的效果

**CD显示**：
- 在Cooldown状态时显示剩余CD时间
- 使用`Gizmo_Status`或自定义Gizmo类
- 显示格式："冷却中：X秒"

---

### 6.3 HediffDef XML配置（任务级描述）

**文件位置**：`Defs/Combat/HediffDefs_CombatBody.xml`

**战斗体Hediff配置**：
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <HediffDef>
        <defName>BDP_CombatBodyActive</defName>
        <hediffClass>HediffWithComps</hediffClass>
        <label>战斗体激活</label>
        <description>Trion战斗体激活状态</description>
        <defaultLabelColor>(0.8, 0.8, 1.0)</defaultLabelColor>
        <isBad>false</isBad>

        <stages>
            <li>
                <!-- NR-019 疼痛免疫 -->
                <painFactor>0</painFactor>

                <!-- NR-031 精神崩溃阻止 -->
                <blocksMentalBreaks>true</blocksMentalBreaks>

                <!-- NR-029 生理需求冻结 -->
                <hungerRateFactor>0</hungerRateFactor>
                <restFallFactor>0</restFallFactor>
                <disablesNeeds>
                    <li>Comfort</li>
                </disablesNeeds>

                <!-- NR-018 无限体力 -->
                <capMods>
                    <li>
                        <capacity>Moving</capacity>
                        <postFactor>1.0</postFactor>
                    </li>
                </capMods>

                <!-- NR-033 环境免疫 -->
                <statOffsets>
                    <ComfyTemperatureMin>-200</ComfyTemperatureMin>
                    <ComfyTemperatureMax>200</ComfyTemperatureMax>
                    <ToxicResistance>1</ToxicResistance>
                </statOffsets>

                <!-- NR-033 疾病免疫（新感染） -->
                <makeImmuneTo>
                    <li>Flu</li>
                    <li>Plague</li>
                    <li>Malaria</li>
                    <li>SleepingSickness</li>
                    <li>FibrousMechanites</li>
                    <li>SensoryMechanites</li>
                </makeImmuneTo>
            </li>
        </stages>

        <comps>
            <li Class="BDP.Combat.HediffCompProperties_CombatBodyActive">
                <!-- 占位值，从XML读取 -->
                <channelingTicks>120</channelingTicks>
                <dismissChannelingTicks>60</dismissChannelingTicks>
                <maintenanceDrainRate>1.0</maintenanceDrainRate>
            </li>
        </comps>
    </HediffDef>

    <!-- 枯竭debuff -->
    <HediffDef>
        <defName>BDP_Exhaustion</defName>
        <hediffClass>HediffWithComps</hediffClass>
        <label>Trion枯竭</label>
        <description>战斗体被动破裂后的虚弱状态</description>
        <defaultLabelColor>(0.5, 0.5, 0.5)</defaultLabelColor>
        <isBad>true</isBad>

        <stages>
            <li>
                <!-- 占位效果，数值设计阶段调整 -->
                <capMods>
                    <li>
                        <capacity>Moving</capacity>
                        <postFactor>0.8</postFactor>
                    </li>
                    <li>
                        <capacity>Manipulation</capacity>
                        <postFactor>0.8</postFactor>
                    </li>
                </capMods>
            </li>
        </stages>
    </HediffDef>
</Defs>
```

---

### 6.4 BDP_DefOf引用（任务级描述）

**文件位置**：`BDP/Core/Defs/BDP_DefOf.cs`（扩展现有文件）

**新增引用**：
```csharp
[DefOf]
public static class BDP_DefOf
{
    // 现有引用...

    // M1新增
    public static HediffDef BDP_CombatBodyActive;
    public static HediffDef BDP_Exhaustion;

    static BDP_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BDP_DefOf));
    }
}
```

---

### 6.5 阶段3验证标准

**游戏内测试场景**（路线图验证场景）：

1. **激活流程**：
   - [ ] 点击Gizmo → 引导条显示 → 完成 → 战斗体状态
   - [ ] 检查：衣物消失、外观Apparel出现、需求面板只剩心理需求、已征召

2. **主动解除**：
   - [ ] 点击解除Gizmo → 引导 → 完成 → 真身恢复
   - [ ] 检查：衣物恢复、需求恢复到快照值、Trion占用释放、外观Apparel消失

3. **引导打断**：
   - [ ] 引导期间造成伤害 → 打断 → 无任何效果（快照未生成）

4. **Trion不恢复**：
   - [ ] 激活后等待 → Trion不增长，维持消耗持续扣减

5. **生理冻结**：
   - [ ] 激活后长时间等待 → 饥饿/休息不下降

6. **存档测试**：
   - [ ] 激活状态保存/读档 → 状态一致（快照、FSM状态、衣物容器）

**阶段3交付物**：
- 静态可否决事件系统实现
- Gizmo生成逻辑
- 完整的XML配置文件
- BDP_DefOf引用更新
- 通过全部6个游戏内测试场景

---

## 七、验证与交付

### 7.1 M1完整验证清单

**代码质量检查**：
- [ ] 所有代码遵循"最简原则"
- [ ] 注释丰富（中文注释）
- [ ] 无编译错误和警告
- [ ] 遵循架构分层规则（L0/L1/L2依赖正确）

**功能完整性检查**：
- [ ] 覆盖路线图M1的25条需求
- [ ] 双层FSM状态转换正确
- [ ] 快照系统序列化正确
- [ ] 11步激活操作完整执行

**风险缓解验证**：
- [ ] Hediff回滚副作用可控（阶段0验证通过）
- [ ] 容器转移顺序正确（阶段0验证通过）
- [ ] 无低严重度风险问题

**游戏内验证**：
- [ ] 通过全部6个测试场景
- [ ] 无崩溃、无错误日志
- [ ] 性能正常（无明显卡顿）

---

### 7.2 M1交付物清单

**代码文件**：
1. `BDP/Combat/Snapshot/CombatBodySnapshot.cs`（新建）
2. `BDP/Combat/Hediffs/HediffComp_CombatBodyActive.cs`（新建）
3. `BDP/Combat/CombatBodyModuleInit.cs`（新建）
4. `BDP/Core/Genes/Gene_TrionGland.cs`（扩展）
5. `BDP/Core/Defs/BDP_DefOf.cs`（扩展）

**配置文件**：
1. `Defs/Combat/HediffDefs_CombatBody.xml`（新建）

**文档**：
1. 阶段0验证报告（2个验证任务的结果）
2. M1实现总结（覆盖需求、已知问题、后续工作）

---

### 7.3 M1覆盖需求对照表

| 需求 | 实现方式 | 验证方式 |
|------|----------|----------|
| NR-002 激活前置 | Gene Gizmo前置检查 + QueryCanActivate事件 | 测试场景1 |
| NR-003 激活引导 | HediffComp Channeling状态 + 打断检测 | 测试场景1、3 |
| NR-004 激活效果 | 11步原子操作 | 测试场景1 |
| NR-005 主动解除触发 | Gizmo按钮 | 测试场景2 |
| NR-006 主动解除过程 | HediffComp DismissChanneling + 引导计时 | 测试场景2 |
| NR-007 主动解除结算 | Trion.Release(allocated) | 测试场景2 |
| NR-018 无限体力 | HediffDef XML capMods | 测试场景1 |
| NR-019 疼痛免疫 | HediffDef XML painFactor=0 | 测试场景1 |
| NR-022 不修复缺陷 | 结构性保证：快照镜像真身 | 代码审查 |
| NR-023 Trion不恢复 | CompTrion.SetFrozen(true) | 测试场景4 |
| NR-024 无法治疗 | 结构性保证：战斗体伤口非原版伤口类（M2） | 代码审查 |
| NR-025 生理冻结 | 快照 + XML需求冻结 | 测试场景5 |
| NR-026 快照内容 | CombatBodySnapshot: Hediff+Need+Apparel+Inventory | 测试场景6 |
| NR-027 Hediff替换 | TakeSnapshot → RemoveAll → AddCombatBody | 测试场景1、2 |
| NR-028 衣物/物品/外观 | ThingOwner物理转移 + Apparel Spawn/Destroy | 测试场景1、2 |
| NR-029 禁用生理需求 | XML: hungerRateFactor=0, restFallFactor=0, disablesNeeds | 测试场景5 |
| NR-030 心理需求正常 | 不禁用心理类Need | 测试场景5 |
| NR-031 崩溃阻止 | XML: blocksMentalBreaks=true | 游戏内观察 |
| NR-033 环境免疫 | XML: ComfyTemp±200, ToxicResistance=1, makeImmuneTo | 游戏内观察 |
| NR-034 维持消耗 | CompTrion.RegisterDrain | 测试场景4 |
| NR-037 解除引导 | HediffComp DismissChanneling计时 | 测试场景2 |
| NR-039 征召行为 | 激活时Drafted=true，解除时不操作 | 测试场景1、2 |

---

### 7.4 已知限制（M1范围外）

以下功能在M1中**不实现**，留待后续里程碑：

- **伤害系统**（M2）：影子HP、伤害拦截、伤口Hediff
- **死亡拦截与破裂**（M3）：头部/心脏被毁、倒地即破裂、完整破裂条件
- **紧急脱离**（M4）：传送机制、脱离芯片、信标建筑
- **集成清理**（M5）：移除CompTriggerBody旧代码、全场景回归

M1中的被动破裂**仅支持Trion耗尽**这一条件，其他破裂条件在M3补全。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-02 | 初版。基于brainstorming对话5轮问答确认实现策略（混合详细度、依赖驱动、分阶段验证、XML配置、风险前置验证、渐进式实现）。分为3个阶段（阶段0前置验证→阶段1快照系统→阶段2双层FSM→阶段3 UI与配置），关键组件给出代码级细节，其他组件给出任务级描述。覆盖M1的25条需求，定义6个游戏内测试场景。 | Claude Sonnet 4.6 |

