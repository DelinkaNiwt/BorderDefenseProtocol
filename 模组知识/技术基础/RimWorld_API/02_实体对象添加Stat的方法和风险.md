# RimWorld 实体对象添加 Stat 的方法详解

- 摘要：本文档系统讲解 RimWorld 模组开发中，向各类实体对象（Pawn、Thing、Building 等）添加/关联 Stat 的方法，包括适用场景、实现步骤、潜在风险及应对策略
- 版本号：1.0
- 修改时间：2025-01-19
- 关键词：StatDef, ThingDef, Gene, Hediff, ThingComp, StatPart, 实体对象, 属性系统, 模组兼容性
- 标签：[草稿]

---

## 1. 核心概念澄清

### 1.1 StatDef 的本质

在讲解具体方法之前，必须先理解一个关键点：**StatDef 本身不是"附加"给实体的数据，而是全局定义**。

```csharp
// StatDef 只是一个配置定义，存储在 DefDatabase 中
// 所有代码都可以通过 defName 引用它
public static StatDef Named(string defName);

// 使用方式
float value = pawn.GetStat(StatDef.Named("TrionMaxCap"));
```

StatDef 定义了：
- 标签、描述、默认值
- 数值范围和显示格式
- 计算逻辑（通过 StatWorker 和 StatPart）

**实体不"拥有"StatDef，实体只是按需获取 stat 值。**

### 1.2 stat 值的获取流程

```csharp
// 当调用 pawn.GetStat(statDef) 时
public float GetStatValue(StatDef stat, bool applyPostProcess = false)
{
    // 1. 获取 defaultBaseValue 作为基础
    float result = stat.defaultBaseValue;

    // 2. 依次应用各个 StatPart 的修正
    foreach (var part in stat.parts)
    {
        result = part.TransformValue(request, result);
    }

    // 3. 应用后处理
    if (applyPostProcess)
        result = stat.postProcessCurve.Evaluate(result);

    return result;
}
```

这意味着：**实体通过 StatPart 链式修正获取最终值**，而不是预先存储数值。

---

## 2. 方法一：Pawn 的 Stat 添加

Pawn（角色/小人）是 RimWorld 最复杂的实体，有多种途径可以影响其 stat 值。

### 2.1 方法 A：StatPart + StatExtension（推荐）

**适用场景**：希望 stat 对所有 Pawn 可用，通过外部修饰物（装备、基因等）影响数值。

#### 实现步骤

**步骤 1：定义 StatDef**

```xml
<StatDef>
    <defName>TrionMaxCap</defName>
    <label>Trion总量上限</label>
    <description>角色的最大Trion能量上限。</description>
    <alwaysHide>true</alwaysHide>
    <defaultBaseValue>500</defaultBaseValue>
    <minValue>0</minValue>
    <maxValue>10000</maxValue>
    <toStringStyle>FloatOne</toStringStyle>
</StatDef>
```

**步骤 2：创建 StatPart 实现修饰逻辑**

```csharp
using Verse;

namespace RimTrion
{
    public class StatPart_TrionFromGene : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                // 检查是否有 Trion 基因
                var trionGene = pawn.genes?.GetGene(DefDatabase<GeneDef>.GetNamed("TrionBaseGene"));
                if (trionGene != null)
                {
                    val *= 1.5f;  // 基因持有者 Trion 上限 1.5 倍
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                var trionGene = pawn.genes?.GetGene(DefDatabase<GeneDef>.GetNamed("TrionBaseGene"));
                if (trionGene != null)
                {
                    return "Trion潜能基因: +50%";
                }
            }
            return null;
        }
    }
}
```

**步骤 3：在 StatDef 中注册 StatPart**

```xml
<StatDef>
    <defName>TrionMaxCap</defName>
    <!-- 基础定义... -->
    <statParts>
        <li Class="RimTrion.StatPart_TrionFromGene"/>
        <li Class="RimWorld.StatPart_Apparel">
            <stat>TrionMaxCap</stat>  <!-- 穿戴装备的 Trion 修正 -->
        </li>
    </statParts>
</StatDef>
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 空引用异常 | req.Thing 可能为 null 或类型不匹配 | 中 |
| 性能问题 | 在 TransformValue 中执行复杂计算 | 低 |
| 模组兼容冲突 | 其他模组的 StatPart 顺序不可控 | 中 |

#### 应对方法

```csharp
// ✅ 正确的空值检查
public override void TransformValue(StatRequest req, ref float val)
{
    // 方式 1：使用 HasThing 检查
    if (!req.HasThing)
        return;

    // 方式 2：类型断言 + 转换
    if (req.Thing is not Pawn pawn)
        return;

    // 方式 3：直接使用 Pawn 类型
    if (req.Pawn != null)
    {
        var gene = req.Pawn.genes?.GetGene(geneDef);
        // ...
    }
}

// ✅ 性能优化：缓存 GeneDef 引用
private static GeneDef cachedGeneDef;
public override void TransformValue(StatRequest req, ref float val)
{
    if (cachedGeneDef == null)
        cachedGeneDef = GeneDef.Named("TrionBaseGene");
    // ...
}
```

---

### 2.2 方法 B：Gene 系统（持久化数据）

**适用场景**：需要跨存档保存的数据，如种族特性、遗传能力。

#### 实现步骤

**步骤 1：定义 GeneDef**

```xml
<GeneDef>
    <defName>TrionBaseGene</defName>
    <label>Trion潜能</label>
    <description>该角色拥有较高的Trion能量上限。</description>
    <iconPath>GeneIcons/TrionBase</iconPath>
    <geneClass>RimTrion.Gene_TrionBase</geneClass>
    <displayPriority>100</displayPriority>

    <!-- 直接修改 Trion 上限 -->
    <statFactors>
        <TrionMaxCap>1.5</TrionMaxCap>
    </statFactors>

    <!-- 同时也影响输出功率 -->
    <statOffsets>
        <TrionOutput>2.0</TrionOutput>
    </statOffsets>
</GeneDef>
```

**步骤 2：创建自定义 Gene 类（可选）**

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    public class Gene_TrionBase : Gene
    {
        // Gene 系统会自动处理 statFactors 和 statOffsets
        // 如果需要额外逻辑，可在此添加
    }
}
```

#### 使用场景示例

```xml
<!-- 在种族定义中分配基因 -->
<RaceDef>
    <defName>TrionUser</defName>
    <geneSet>
        <li>
            <li>TrionBaseGene</li>
            <li>TrionEfficiencyGene</li>
        </li>
    </geneSet>
</RaceDef>

<!-- 或者作为随机生成选项 -->
<GeneDef>
    <defName>TrionRandomGene</defName>
    <geneClass>RimTrion.Gene_TrionRandom</geneClass>
    <randomWeight>10</randomWeight>
    <!-- ... -->
</GeneDef>
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 存档膨胀 | 大量基因数据占用存档空间 | 低 |
| 卸载后残留 | 卸载模组后存档保留未知基因 | 中 |
| 遗传逻辑冲突 | 与其他基因系统不兼容 | 低 |

#### 应对方法

```csharp
// ✅ 检查基因是否存在，防止卸载后崩溃
public float GetTrionBonus(Pawn pawn)
{
    if (pawn.genes == null)
        return 1f;

    var gene = pawn.genes.GetGene(DefDatabase<GeneDef>.GetNamed("TrionBaseGene", false));
    return gene != null ? 1.5f : 1f;  // false 表示找不到不抛异常
}

// ✅ 模组检查辅助方法
public bool HasTrionMod()
{
    return ModLister.AllInstalledMods.Any(m => m.PackageId == "RimTrion.Mod");
}
```

---

### 2.3 方法 C：Hediff 系统（临时状态）

**适用场景**：临时性属性修改，如buff效果、状态异常、技能激活。

#### 实现步骤

**步骤 1：定义 HediffDef**

```xml
<HediffDef>
    <defName>TrionOverdrive</defName>
    <label>Trion爆发</label>
    <description>Trion能量正在超速运转，暂时提升输出功率。</description>
    <hediffClass>RimTrion.Hediff_TrionOverdrive</hediffClass>
    <defaultSeverity>1</defaultSeverity>
    <isBad>false</isBad>

    <stages>
        <li>
            <statOffsets>
                <TrionOutput>5.0</TrionOutput>
            </statOffsets>
        </li>
    </stages>
</HediffDef>
```

**步骤 2：创建自定义 HediffComp（可选）**

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    public class Hediff_TrionOverdrive : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            // 持续时间管理
            if (Severity > 0)
            {
                Severity -= 0.01f;  // 每tick减少
                if (Severity <= 0)
                {
                    pawn.health.RemoveHediff(this);
                }
            }
        }
    }
}
```

**步骤 3：应用 Hediff**

```csharp
// 在适当的地方调用
public void ApplyTrionOverdrive(Pawn target, float duration)
{
    var hediff = HediffMaker.MakeHediff(HediffDefOf.TrionOverdrive, target);
    hediff.Severity = duration;
    target.health.AddHediff(hediff);
}
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 内存泄漏 | Hediff 未正确移除导致永久存在 | 中 |
| 堆叠问题 | 多个相同 Hediff 叠加导致效果异常 | 中 |
| 存档脏数据 | 残留的 Hediff 引用无效 Def | 低 |

#### 应对方法

```csharp
// ✅ 防止 Hediff 堆叠
public void ApplyTrionOverdrive(Pawn target, float duration)
{
    var existing = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.TrionOverdrive);
    if (existing != null)
    {
        existing.Severity = Mathf.Max(existing.Severity, duration);
    }
    else
    {
        var hediff = HediffMaker.MakeHediff(HediffDefOf.TrionOverdrive, target);
        hediff.Severity = duration;
        target.health.AddHediff(hediff);
    }
}

// ✅ 使用 Exposable 系统安全序列化
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Defs.Look(ref someDef, "someDef");  // 自动处理 null Def
}
```

---

## 3. 方法二：Thing/Item 的 Stat 添加

Thing 包括武器、衣物、材料等物品。

### 3.1 方法 A：ThingDef 的 statFactors / statOffsets

**适用场景**：物品固有的属性，如武器伤害、防具护甲。

#### 实现步骤

**步骤 1：定义 ThingDef 时配置 stat**

```xml
<ThingDef>
    <defName>TrionTrigger_Basic</defName>
    <label>基础触发器</label>
    <description>能够释放基础Trion能量的触发器。</description>
    <thingClass>ThingWithComps</thingClass>
    <category>Item</category>
    <graphicData>
        <texPath>Things/Item/TrionTrigger</texPath>
        <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
        <Mass>0.5</Mass>
        <MarketValue>100</MarketValue>
    </statBases>

    <!-- 物品自有的 statFactors -->
    <statModifiers>
        <TrionMaxCap>10</TrionMaxCap>  <!-- 使用 statOffset 效果 -->
    </statModifiers>

    <!-- 或者使用 statFactors（乘算） -->
    <statFactors>
        <TrionOutput>1.1</TrionOutput>
    </statFactors>
</ThingDef>
```

**步骤 2：让装备影响 Pawn 的 Stat**

```xml
<StatDef>
    <defName>TrionMaxCap</defName>
    <alwaysHide>true</alwaysHide>
    <defaultBaseValue>500</defaultBaseValue>
    <statParts>
        <!-- 装备修正 -->
        <li Class="RimWorld.StatPart_Equipment">
            <stat>TrionMaxCap</stat>
        </li>
    </statParts>
</StatDef>
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 材质影响缺失 | statFactors 不考虑制造材料 | 低 |
| 公式复杂度 | 多个修正叠加导致数值异常 | 低 |
| 物品堆叠 | 堆叠物品的 stat 计算 | 中 |

---

### 3.2 方法 B：自定义 ThingComp

**适用场景**：需要复杂逻辑的物品属性，如充能系统、耐久度影响。

#### 实现步骤

**步骤 1：定义 CompDef**

```xml
<ThingDef>
    <defName>TrionTrigger_Advanced</defName>
    <comps>
        <li Class="RimTrion.CompProperties_TrionWeapon">
            <compClass>RimTrion.CompTrionWeapon</compClass>
            <!-- 配置参数 -->
            <trionCapacity>100</trionCapacity>
            <trionEfficiency>0.9</trionEfficiency>
        </li>
    </comps>
</ThingDef>
```

**步骤 2：创建 CompProperties**

```csharp
using Verse;

namespace RimTrion
{
    public class CompProperties_TrionWeapon : CompProperties
    {
        public float trionCapacity = 50f;
        public float trionEfficiency = 0.8f;

        public CompProperties_TrionWeapon()
        {
            this.compClass = typeof(CompTrionWeapon);
        }
    }
}
```

**步骤 3：创建 ThingComp**

```csharp
using Verse;

namespace RimTrion
{
    public class CompTrionWeapon : ThingComp
    {
        private float storedTrion;
        private float capacity;

        public CompProperties_TrionWeapon Props => (CompProperties_TrionWeapon)props;

        public float StoredTrion
        {
            get => storedTrion;
            set => storedTrion = value;
        }

        public float Capacity => Props.trionCapacity;

        public override void PostDraw()
        {
            // 显示 Trion 充能状态
        }

        public override void CompTick()
        {
            // 每 tick 恢复少量 Trion
            storedTrion += 0.1f;
            if (storedTrion > Capacity)
                storedTrion = Capacity;
        }

        // 实现 IExposable 以支持存档
        public override void ExposeData()
        {
            Scribe_Values.Look(ref storedTrion, "storedTrion");
            Scribe_Values.Look(ref capacity, "capacity");
        }
    }
}
```

**步骤 4：创建扩展方法访问 Comp**

```csharp
public static class TrionWeaponExtension
{
    public static float GetStoredTrion(this Thing thing)
    {
        return thing.TryGetComp<CompTrionWeapon>()?.StoredTrion ?? 0f;
    }

    public static bool HasTrionWeapon(this Thing thing)
    {
        return thing.TryGetComp<CompTrionWeapon>() != null;
    }
}
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 性能开销 | 每个物品实例都有独立对象 | 中 |
| 存档膨胀 | Comp 数据序列化增加存档大小 | 中 |
| 空引用 | 访问未初始化或已卸载的 Comp | 中 |

#### 应对方法

```csharp
// ✅ 使用 TryGetComp 安全访问
var comp = thing.TryGetComp<CompTrionWeapon>();
if (comp != null)
{
    // 安全使用
}

// ✅ 限制 Tick 执行频率
public override void CompTick()
{
    if (parent.Map == null) return;  // 未放置的物品不执行

    if (Find.TickManager.TicksGame % 60 == 0)  // 每60 tick 执行一次
    {
        // 逻辑代码
    }
}

// ✅ 存档数据清理
public override void DeSpawn(DestroyMode mode)
{
    // 物品被销毁时清理临时数据
    storedTrion = 0;
    base.DeSpawn(mode);
}
```

---

## 4. 方法三：Building 的 Stat 添加

Building（建筑）与 Thing 类似，但有特殊考量。

### 4.1 方法 A：ThingDef + statModifiers

**适用场景**：建筑的固定属性，如工作台效率、发电功率。

#### 实现步骤

**步骤 1：定义 BuildingDef**

```xml
<BuildingDef>
    <defName>TrionGenerator</defName>
    <label>Trion发生器</label>
    <description>持续产生Trion能量的装置。</description>
    <thingClass>Building</thingClass>
    <graphicData>
        <texPath>Things/Building/TrionGenerator</texPath>
        <graphicClass>Graphic_Single</graphicClass>
    </graphicData>

    <statBases>
        <MaxHitPoints>300</MaxHitPoints>
        <Flammability>0.2</Flammability>
    </statBases>

    <!-- 建筑自身的 Trion 输出 stat -->
    <statModifiers>
        <TrionGenerationRate>10</TrionGenerationRate>
    </statModifiers>
</BuildingDef>
```

**步骤 2：通过 PowerComp 影响其他实体**

```xml
<BuildingDef>
    <defName>TrionPowerStation</defName>
    <comps>
        <li Class="CompProperties_Power">
            <basePowerConsumption>-50</basePowerConsumption>  <!-- 产生电力 -->
        </li>
    </comps>
    <!-- 与其他建筑的互动 stat -->
</BuildingDef>
```

---

### 4.2 方法 B：BuildingComp（复杂逻辑）

**适用场景**：需要与周边建筑/实体互动的系统，如生产链、区域效果。

#### 实现步骤

**步骤 1：定义 BuildingComp**

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    public class CompProperties_TrionField : CompProperties
    {
        public float fieldRadius = 10f;
        public float trionBonusPerBuilding = 0.5f;

        public CompProperties_TrionField()
        {
            this.compClass = typeof(CompTrionField);
        }
    }

    public class CompTrionField : BuildingComp
    {
        private CompProperties_TrionField Props => (CompProperties_TrionField)props;

        public override void CompTick()
        {
            base.CompTick();

            // 每 60 tick 计算一次区域效果
            if (parent.Map == null || !parent.Spawned)
                return;

            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            // 查找范围内的 Pawn
            var nearbyPawns = GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.fieldRadius, true)
                .OfType<Pawn>();

            foreach (var pawn in nearbyPawns)
            {
                // 给范围内的 Pawn 添加 Trion 增益
                float bonus = Props.trionBonusPerBuilding;
                // 实际应用逻辑...
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            // 绘制效果范围
            GenDraw.DrawRadiusRing(parent.Position, Props.fieldRadius);
        }
    }
}
```

**步骤 2：在 BuildingDef 中注册**

```xml
<BuildingDef>
    <defName>TrionFieldGenerator</defName>
    <comps>
        <li Class="RimTrion.CompProperties_TrionField">
            <fieldRadius>15</fieldRadius>
            <trionBonusPerBuilding>1.0</trionBonusPerBuilding>
        </li>
    </comps>
</BuildingDef>
```

#### 风险分析

| 风险类型 | 风险描述 | 发生概率 |
|---------|---------|---------|
| 性能影响 | 频繁的区域搜索导致卡顿 | 高 |
| 范围重叠 | 多个建筑效果叠加 | 中 |
| 卸载后残留 | 存档中保留 Comp 引用 | 低 |

#### 应对方法

```csharp
// ✅ 使用缓存减少重复计算
private int lastCalculationTick = -999;
private float cachedBonus = 0f;

public override void CompTick()
{
    if (Find.TickManager.TicksGame - lastCalculationTick < 60)
        return;

    lastCalculationTick = Find.TickManager.TicksGame;

    // 只在范围内的实体变化时重新计算
    // ...
}

// ✅ 使用 JobDriver 或异步处理复杂逻辑
// ✅ 限制 Tick 频率
```

---

## 5. 方法对比与选择指南

### 5.1 方法对比表

| 方法 | 持久化 | 动态性 | 性能开销 | 兼容性 | 推荐场景 |
|------|--------|--------|---------|--------|---------|
| StatDef + StatPart | 否 | 高 | 低 | 好 | 核心属性计算 |
| Gene | 是 | 中 | 低 | 好 | 种族/遗传特性 |
| Hediff | 是 | 高 | 中 | 中 | 临时状态/BUFF |
| ThingDef statModifiers | 否 | 低 | 低 | 好 | 物品固定属性 |
| ThingComp | 是 | 高 | 中 | 中 | 复杂物品逻辑 |
| BuildingComp | 是 | 高 | 中 | 中 | 建筑区域效果 |

### 5.2 选择流程图

```
需要添加 stat 的实体是？
├── Pawn
│   ├── 需要跨存档持久化？
│   │   ├── 是 → Gene 系统
│   │   └── 否
│   │       ├── 临时状态？ → Hediff 系统
│   │       └── 永久修饰？ → StatPart + StatExtension
│   │
│   └── 需要外部配置？
│       ├── 是 → StatDef + StatPart (StatPart_Equipment 等)
│       └── 否 → StatWorker 重写
│
└── Thing/Item
    ├── 简单固定值？
    │   └── 是 → ThingDef statModifiers
    └── 复杂逻辑？
        └── 是 → 自定义 ThingComp
```

---

## 6. 通用风险与应对策略

### 6.1 模组卸载后的存档安全

| 风险 | 描述 | 应对策略 |
|------|------|---------|
| Def 空引用 | 存档引用了已卸载模组的 Def | 使用 `DefDatabase<XxxDef>.GetNamed("Xxx", false)` |
| Comp 数据残留 | 存档包含孤立 Comp 数据 | 实现 `IExposable` 时检查 Def 是否存在 |
| Gene 未知 | 存档中有未知基因 | 游戏会自动标记为 "UnknownGene" |

```csharp
// ✅ 安全加载 Def
GeneDef geneDef = DefDatabase<GeneDef>.GetNamedSilentFail("TrionBaseGene");
if (geneDef != null)
{
    // 安全使用
}

// ✅ Comp 数据清理
public override void ExposeData()
{
    base.ExposeData();

    // 检查 Def 是否仍存在
    if (Scribe.mode == LoadSaveMode.Loading)
    {
        if (!DefDatabase<ThingDef>.GetNamesForReading().Contains(stuffDefName))
        {
            stuffDefName = null;  // 使用默认值
        }
    }
}
```

### 6.2 性能优化原则

1. **减少 StatRequest 创建**：StatRequest 包含较多数据，频繁创建影响性能
2. **缓存引用**：将常用 Def 引用缓存为静态字段
3. **控制 Tick 频率**：非关键逻辑使用条件 Tick
4. **避免复杂计算**：TransformValue 中不做 O(n) 以上复杂度的操作

```csharp
// ✅ 静态缓存
private static readonly StatDef TrionMaxCapStat = StatDef.Named("TrionMaxCap");
private static readonly GeneDef TrionGene = GeneDef.Named("TrionBaseGene");

// ✅ 条件 Tick
public override void CompTick()
{
    if (parent.Map == null) return;  // 快速跳过
    if (Find.TickManager.TicksGame % 60 != 0) return;  // 每秒一次
}
```

### 6.3 模组兼容性

| 场景 | 问题 | 解决方案 |
|------|------|---------|
| 依赖其他模组的 Def | 目标模组未安装 | 条件检查 + 降级处理 |
| StatPart 执行顺序 | 结果受其他 Mod 影响的 StatPart 影响 | 避免依赖特定顺序 |
| Comp 冲突 | 多个 Mod 注册相同 CompClass | 使用不同的 CompClass 或条件合并 |

```csharp
// ✅ 条件依赖检查
public override void TransformValue(StatRequest req, ref float val)
{
    // 检查可选模组是否安装
    var otherMod = LoadedModManager.GetMod<RimOtherMod>();
    if (otherMod?.IsActive == true)
    {
        val *= otherMod.GetMultiplier();
    }
}

// ✅ 模组加载顺序处理
[EarlyInit]
public static void EarlyInit()
{
    // 在游戏初始化早期处理模组依赖
}
```

---

## 7. 存档安全检查清单

在发布模组前，确保以下检查点：

```csharp
// 1. 所有自定义 Def 都有 packageId 前缀
// 2. Comp 的 ExposeData 处理 null Def
// 3. Gene/Hediff 引用使用 SafeGet
// 4. 静态字段初始化使用 TryCatch
// 5. 提供了版本迁移逻辑（如果需要）

public override void ExposeData()
{
    base.ExposeData();

    Scribe_Values.Look(ref currentTrion, "currentTrion");
    Scribe_Defs.Look(ref sourceDef, "sourceDef");

    // 迁移：旧存档可能没有 sourceDef
    if (Scribe.mode == LoadSaveMode.Loading && sourceDef == null)
    {
        sourceDef = ThingDefOf.TrionGenerator;  // 默认值
    }
}
```

---

## 8. 总结

给 RimWorld 实体添加 stat 的核心要点：

1. **理解计算模型**：stat 值是动态计算的，不存储在实体上
2. **选择合适的系统**：Gene（持久化）、Hediff（临时）、StatPart（修饰）
3. **注意空值安全**：所有外部引用都要检查 null
4. **控制性能开销**：减少 Tick 频率，使用缓存
5. **考虑模组兼容性**：使用 SafeGet、条件检查

**最佳实践**：
- 优先使用原生系统（Gene、Hediff、ThingDef）
- 复杂逻辑使用 Comp 系统
- 永远不要假设 Def 一定存在

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|-------|---------|---------|--------|
| 1.0 | 初稿：完成 Pawn、Thing、Building 的添加方法 | 2025-01-19 | 知识提炼者 |
