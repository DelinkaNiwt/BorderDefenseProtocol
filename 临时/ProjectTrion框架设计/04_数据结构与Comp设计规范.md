---
摘要: Trion系统的完整数据模型设计，包括4个核心Comp的详细数据结构定义、属性范围、序列化方案
版本号: 0.1
修改时间: 2026-01-08
关键词: 数据结构,Comp设计,属性定义,数据模型,序列化规范
标签: [待审]
---

# Trion系统数据结构与Comp设计规范

## 第一部分：Comp架构总览

```
Pawn.AllComps
├── TrionPawnComp (关键数据，单例)
│   └─ Trion能量数据中心
├── TriggerSystemComp (关键数据，单例)
│   └─ 触发器装备与激活状态
├── CombatBodyComp (关键数据，单例)
│   └─ 战斗体与肉身状态转换
└── TrionConsumptionEngine (执行引擎，单例)
    └─ 能量消耗计算和Bail Out判定
```

**设计原则：**
- 每个Comp单一职责
- Comp间通过接口通信，避免直接数据访问
- 所有可配置参数外挂到XML，不硬编码
- 数据必须可序列化保存

---

## 第二部分：TrionPawnComp 详细设计

### 2.1 数据结构定义

```csharp
public class TrionPawnComp : ThingComp
{
    // ===== 基础容量管理 =====
    public float totalTrionCapacity = 1000f;      // 个体Trion总容量（范围：500-5000）
    public float currentTrionAmount = 1000f;      // 当前Trion总量

    // ===== 预留占用管理 =====
    public float bailOutReserve = 400f;           // Bail Out预留占用（范围：200-500）
    public float moduleReserve = 0f;              // 其他模块预留占用（来自TriggerSystemComp）

    // ===== 输出能力 =====
    public float peakOutputRate = 100f;           // 瞬时输出功率（范围：50-200，用于技能释放检查）
    public float sustainedOutputRate = 10f;       // 持续输出功率（范围：5-20，用于基础消耗）

    // ===== 恢复机制 =====
    public float recoveryRate = 5f;               // 每小时恢复Trion（范围：1-20，默认5/h）
    public int recoveryTickCounter = 0;           // 恢复计时器

    // ===== 战斗状态 =====
    public bool isCombatActive = false;           // 战斗体是否激活

    // ===== 调试与统计 =====
    public float cumulativeConsumption = 0f;      // 累计消耗统计
}
```

### 2.2 属性定义与范围

| 属性名 | 类型 | 范围 | 默认值 | 说明 |
|--------|------|------|--------|------|
| `totalTrionCapacity` | float | 500-5000 | 1000 | 个体Trion总容量 |
| `currentTrionAmount` | float | 0-totalCapacity | totalCapacity | 当前能量值 |
| `bailOutReserve` | float | 200-500 | 400 | Bail Out占用 |
| `moduleReserve` | float | 0-总容量*0.5 | 0 | 模块占用 |
| `peakOutputRate` | float | 50-200 | 100 | 瞬时输出功率 |
| `sustainedOutputRate` | float | 5-20 | 10 | 持续输出功率 |
| `recoveryRate` | float | 1-20 | 5 | 每小时恢复量 |

### 2.3 公共接口

```csharp
// 查询接口
public float GetTotalTrion() => totalTrionCapacity;
public float GetCurrentTrion() => currentTrionAmount;
public float GetAvailableTrion() => currentTrionAmount - bailOutReserve - moduleReserve;
public float GetReservedTrion() => bailOutReserve + moduleReserve;

// 操作接口
public void ConsumeTrion(float amount)
{
    currentTrionAmount = Mathf.Max(0, currentTrionAmount - amount);
}

public void RecoverTrion(float amount)
{
    currentTrionAmount = Mathf.Min(totalTrionCapacity, currentTrionAmount + amount);
}

// 触发接口
public void RequestBailOut()
{
    // 通知CombatBodyComp解除战斗体
    var combatBodyComp = parent.GetComp<CombatBodyComp>();
    if (combatBodyComp != null && combatBodyComp.isCombatBodyActive)
    {
        combatBodyComp.DissolveCombatBody();
        // 扣除Bail Out占用的能量（可选）
        ConsumeTrion(bailOutReserve);
    }
}

// 序列化
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Values.Look(ref totalTrionCapacity, "totalTrionCapacity", 1000f);
    Scribe_Values.Look(ref currentTrionAmount, "currentTrionAmount", 1000f);
    Scribe_Values.Look(ref bailOutReserve, "bailOutReserve", 400f);
    Scribe_Values.Look(ref moduleReserve, "moduleReserve", 0f);
    Scribe_Values.Look(ref peakOutputRate, "peakOutputRate", 100f);
    Scribe_Values.Look(ref sustainedOutputRate, "sustainedOutputRate", 10f);
    Scribe_Values.Look(ref recoveryRate, "recoveryRate", 5f);
}
```

---

## 第三部分：TriggerSystemComp 详细设计

### 3.1 数据结构定义

```csharp
public class ComponentSlot
{
    public int slotIndex;                          // 槽位编号 0-8
    public TriggerComponentType type;              // 组件类型
    public ActivationState activationState;        // 激活状态

    // 组件信息
    public float occupancy = 0f;                   // 预留占用（20-50）
    public float activationCost = 0f;              // 激活消耗（5-50）
    public int activationDuration = 0;             // 激活引导时间（1-5 Tick）
    public float leakageMultiplier = 1f;           // 激活时的能量流失加成
}

public enum ActivationState
{
    Inactive,      // 未激活
    Activating,    // 激活中（引导过程）
    Active,        // 已激活
    Cooling,       // 冷却中
    Destroyed      // 组件被摧毁
}

public class TriggerSystemComp : ThingComp
{
    // 槽位配置（主×4 + 副×4 + 特×1）
    public List<ComponentSlot> mainSlots = new();     // 主触发器槽位
    public List<ComponentSlot> subSlots = new();      // 副触发器槽位
    public ComponentSlot specialSlot = null;          // 特殊槽位（Bail Out等）

    // 激活状态追踪
    public int mainActiveSlot = -1;                // 当前激活的主槽位（-1表示无）
    public int subActiveSlot = -1;                 // 当前激活的副槽位（-1表示无）
    public Dictionary<int, int> activationCounter = new(); // 激活计时器

    // 缓存计算
    private float cachedTotalReserve = 0f;
    private bool reserveNeedsRecalc = true;

    // 初始化（在游戏加载时调用）
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            mainSlots.Add(new ComponentSlot { slotIndex = i });
            subSlots.Add(new ComponentSlot { slotIndex = i + 4 });
        }
        specialSlot = new ComponentSlot { slotIndex = 8 };
    }
}
```

### 3.2 公共接口

```csharp
// 装备/卸除接口
public bool EquipComponent(int slotIndex, ComponentSlot component)
{
    // 验证槽位
    // 安装组件
    // 更新预留占用
    reserveNeedsRecalc = true;
    return true;
}

public void UnequipComponent(int slotIndex)
{
    // 强制停用该组件
    // 清除占用
    reserveNeedsRecalc = true;
}

// 激活/停用接口
public bool RequestActivation(int slotIndex)
{
    var slot = GetSlot(slotIndex);
    if (slot == null || slot.activationState != ActivationState.Inactive)
        return false;

    // 检查能量是否足够
    var trionComp = parent.GetComp<TrionPawnComp>();
    if (trionComp.GetAvailableTrion() < slot.activationCost)
        return false;

    // 启动激活过程
    slot.activationState = ActivationState.Activating;
    activationCounter[slotIndex] = slot.activationDuration;
    return true;
}

public void FinishActivation(int slotIndex)
{
    // 检查同一手臂的其他槽位，停用旧的
    // 设置新槽位为Active
    // 消耗能量
}

public void DeactivateAll()
{
    // 停用所有激活的组件
}

// 预留占用计算
public float GetTotalReserveOccupancy()
{
    if (reserveNeedsRecalc)
    {
        cachedTotalReserve = 0f;
        foreach (var slot in mainSlots.Concat(subSlots))
        {
            if (slot.activationState != ActivationState.Inactive
                && slot.activationState != ActivationState.Destroyed)
            {
                cachedTotalReserve += slot.occupancy;
            }
        }
        if (specialSlot?.activationState == ActivationState.Active)
        {
            cachedTotalReserve += specialSlot.occupancy;
        }
        reserveNeedsRecalc = false;
    }
    return cachedTotalReserve;
}

// 序列化
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Collections.Look(ref mainSlots, "mainSlots");
    Scribe_Collections.Look(ref subSlots, "subSlots");
    Scribe_Deep.Look(ref specialSlot, "specialSlot");
    Scribe_Values.Look(ref mainActiveSlot, "mainActiveSlot", -1);
    Scribe_Values.Look(ref subActiveSlot, "subActiveSlot", -1);
}
```

---

## 第四部分：CombatBodyComp 详细设计

### 4.1 数据结构定义

```csharp
public class WoundData
{
    public BodyPartRecord bodyPart;                 // 伤口位置
    public float severity = 1f;                    // 伤口严重程度（1-10）
    public float leakageRate => severity * 0.5f;  // 泄漏速率
    public long tickWhenOccurred;                  // 发生时刻（用于恢复计算）

    // 伤口特征
    public bool isLethal = false;                  // 致命伤口（如供能器官受损）
}

public class PawnSnapshot
{
    public List<Hediff> hediffs;                   // 快照时的所有Hediff
    public Thing[] equipment;                      // 装备列表
    public int[] bodyPartHealth;                   // 身体部位健康值
    public float health;                           // 总体生命值

    // 序列化辅助
    public void Save(Pawn pawn)
    {
        health = pawn.health.summaryHealth.SummaryHealthPercent;
        hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
        // ...保存其他数据
    }

    public void Restore(Pawn pawn)
    {
        // 还原快照数据到Pawn
    }
}

public class CombatBodyComp : ThingComp
{
    // 战斗体状态
    public bool isCombatBodyActive = false;

    // 肉身快照
    public PawnSnapshot lastPawnSnapshot = null;

    // 伤口列表
    public List<WoundData> wounds = new();

    // 核心部位引用（缓存）
    private BodyPartRecord coreOrgan = null;
}
```

### 4.2 公共接口

```csharp
// 战斗体管理
public void GenerateCombatBody()
{
    // 保存肉身快照
    lastPawnSnapshot = new PawnSnapshot();
    lastPawnSnapshot.Save(parent as Pawn);

    // 禁用物品
    // 设置状态
    isCombatBodyActive = true;
}

public void DissolveCombatBody()
{
    // 清空伤口列表
    wounds.Clear();

    // 恢复肉身
    RestorePawn();

    isCombatBodyActive = false;
}

private void RestorePawn()
{
    if (lastPawnSnapshot != null)
    {
        lastPawnSnapshot.Restore(parent as Pawn);
    }
}

// 伤口管理
public void RegisterWound(BodyPartRecord part, float severity)
{
    // 检查是否为致命部位
    bool isLethal = part == coreOrgan;

    // 创建伤口
    var wound = new WoundData
    {
        bodyPart = part,
        severity = Mathf.Clamp01(severity),
        isLethal = isLethal,
        tickWhenOccurred = Find.TickManager.TicksGame
    };

    wounds.Add(wound);

    // 如果是致命伤口，触发Bail Out
    if (isLethal)
    {
        parent.GetComp<TrionPawnComp>().RequestBailOut();
    }
}

public float GetTotalLeakageRate()
{
    return wounds.Sum(w => w.leakageRate);
}

public bool IsCoreDamaged()
{
    return wounds.Any(w => w.isLethal);
}

// 序列化
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Values.Look(ref isCombatBodyActive, "isCombatBodyActive");
    Scribe_Deep.Look(ref lastPawnSnapshot, "lastPawnSnapshot");
    Scribe_Collections.Look(ref wounds, "wounds");
}
```

---

## 第五部分：TrionConsumptionEngine 详细设计

### 5.1 数据结构定义

```csharp
public class TrionConsumptionEngine : ThingComp
{
    // 消耗参数（来自配置）
    public float maintenanceCost = 0.5f;          // 每Tick维持消耗
    public float baseActionCost = 1f;             // 基础动作消耗
    public float skillActivationCost = 50f;       // 技能释放消耗（如旋空）

    // 运行时状态
    public float currentConsumptionRate = 0f;     // 当前消耗速率
    public bool isHighConsumption = false;        // 是否处于高消耗状态

    // Tick计数（用于计算消耗周期）
    public int tickCounter = 0;
    public const int CONSUMPTION_CHECK_INTERVAL = 10; // 每10 Tick计算一次（性能优化）
}
```

### 5.2 消耗计算逻辑

```csharp
public override void CompTick()
{
    base.CompTick();

    var pawn = parent as Pawn;
    var trionComp = pawn.GetComp<TrionPawnComp>();
    var combatComp = pawn.GetComp<CombatBodyComp>();
    var triggerComp = pawn.GetComp<TriggerSystemComp>();

    if (trionComp == null || !combatComp.isCombatBodyActive)
        return;

    // 1. 计算维持消耗
    float totalConsumption = maintenanceCost;

    // 2. 计算伤口泄漏
    totalConsumption += combatComp.GetTotalLeakageRate();

    // 3. 计算动作消耗（基础）
    totalConsumption += baseActionCost;

    // 4. 计算激活组件的额外消耗
    totalConsumption += GetActivationExtraCost(triggerComp);

    // 5. 扣除能量
    trionComp.ConsumeTrion(totalConsumption);

    // 6. 检查Bail Out条件
    if (CheckBailOutCondition(trionComp, combatComp))
    {
        trionComp.RequestBailOut();
    }
}

private float GetActivationExtraCost(TriggerSystemComp triggerComp)
{
    float extraCost = 0f;
    // 根据当前激活的组件增加消耗
    return extraCost;
}

private bool CheckBailOutCondition(TrionPawnComp trionComp, CombatBodyComp combatComp)
{
    // 条件1：可用Trion ≤ 0
    if (trionComp.GetAvailableTrion() <= 0)
        return true;

    // 条件2：核心部位受损
    if (combatComp.IsCoreDamaged())
        return true;

    return false;
}
```

---

## 第六部分：数据一致性与验证

### 6.1 数据不变性检查

```csharp
public class TrionDataValidator
{
    public static bool ValidateTrionData(Pawn pawn)
    {
        var trionComp = pawn.GetComp<TrionPawnComp>();

        // 检查1：当前能量不超过总容量
        if (trionComp.GetCurrentTrion() > trionComp.GetTotalTrion())
            return false;

        // 检查2：当前能量不为负
        if (trionComp.GetCurrentTrion() < 0)
            return false;

        // 检查3：预留占用不超过总容量
        if (trionComp.GetReservedTrion() > trionComp.GetTotalTrion())
            return false;

        // 检查4：可用余量逻辑正确
        float expectedAvailable = trionComp.GetTotalTrion() - trionComp.GetReservedTrion();
        if (Math.Abs(trionComp.GetAvailableTrion() - expectedAvailable) > 0.01f)
            return false;

        return true;
    }
}
```

---

## 第七部分：配置参数外挂化

**示例 ThingDef 配置：**

```xml
<Defs>
  <ThingDef ParentName="BasePawn">
    <defName>TrionWarrior</defName>
    <!-- ... Pawn配置 ... -->
    <comps>
      <li Class="ProjectTrion.TrionPawnCompProperties">
        <baseTrionCapacity>1000</baseTrionCapacity>
        <bailOutReserve>400</bailOutReserve>
        <peakOutputRate>100</peakOutputRate>
        <sustainedOutputRate>10</sustainedOutputRate>
        <recoveryRate>5</recoveryRate>
      </li>
      <li Class="ProjectTrion.TriggerSystemCompProperties">
        <componentOccupancies>
          <li>
            <type>Weapon</type>
            <occupancy>10</occupancy>
          </li>
        </componentOccupancies>
      </li>
    </comps>
  </ThingDef>
</Defs>
```

---

## 版本历史

| 版本 | 日期 | 改动 |
|------|------|------|
| 0.1 | 2026-01-08 | 初版：完整数据结构、属性定义、公共接口、序列化规范 |

