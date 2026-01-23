# Phase 1 深度拆分 - 任务架构设计

**文档版本**: 1.0
**基于**: 技术可行性评估报告 (20250123)
**目标**: 将v2.2 Phase 1拆分为可执行的子任务
**范围**: RimWorld 1.5, Biotech DLC required
**预期工期**: 3-4周（基于高效执行）

---

## 0. Phase 1 总体目标回顾

### 核心目标

**将RimWorld的"血量经济"转为"能量经济"，实现虚拟体系统的物理法则验证**

关键要素:
- Pawn拥有Trion能量（总量、占用量、消耗量）
- 变身生成虚拟体，肉身冻结为快照
- 虚拟体受伤消耗Trion，泄漏Trion
- 解除时恢复肉身，虚拟体伤害消失

### Phase 1验收标准（最小可行）

**MVP场景**: 小人A装备触发器 → 变身 → 被砍断手臂 → Trion加速泄漏 → 主动解除 → 手臂恢复正常

**定量指标**:
- Trion容量正确计算（根据天赋）
- 虚拟体变身/解除无数据丢失
- 伤害转化为Trion消耗（1伤害 = 1Trion）
- 断肢泄漏速率 ≥ 5 Trion/秒
- 解除时肉身100%恢复

---

## 1. Phase 1 子系统分解

### 全景依赖关系图

```
┌─────────────────────────────────────────────────────────┐
│                  Phase 1 全景架构                         │
└─────────────────────────────────────────────────────────┘

【底层支撑】
    ├─ S1: StatDef定义系统
    │   ├─ TrionMaxCap (Trion总容量)
    │   └─ TrionOutput (Trion输出功率)
    │
    ├─ S2: Gene_Trion资源系统
    │   ├─ 继承Gene_Resource
    │   └─ 实现ITrionSource接口
    │
    └─ S5: Harmony Patch层 (独立工作)
        ├─ PreApplyDamage补丁
        ├─ PostApplyDamage补丁
        └─ Pawn.Kill补丁

【核心逻辑】
    ├─ S3: TrionSnapshot快照系统
    │   ├─ 快照创建 (Activation)
    │   └─ 快照恢复 (Deactivation)
    │
    └─ S4: Hediff_TrionBody状态管理
        ├─ 虚拟体生命周期
        └─ HediffComp_TrionLeaker (泄漏计算)

【集成测试】
    └─ S6: Bailout脱出系统
        ├─ 能量耗尽检测
        └─ 解除逻辑触发

【执行顺序约束】
    S1 ─→ S2 ─→ (S3 ∥ S4 ∥ S5) ─→ S6

    说明:
    - S1和S2是基础，必须先完成
    - S3、S4、S5可并行开发（无依赖）
    - S6依赖前面所有子系统
```

### 子系统详情表

| 系统ID | 名称 | 复杂度 | 依赖 | 预期时间 | 风险 |
|--------|------|--------|------|---------|------|
| S1 | StatDef定义 | ★☆☆ | 无 | 2h | 低 |
| S2 | Gene_Trion实现 | ★★☆ | S1 | 4h | 低 |
| S3 | TrionSnapshot快照 | ★★★ | S2 | 8h | 中 |
| S4 | Hediff_TrionBody | ★★★ | S2, S3 | 8h | 中 |
| S5 | Harmony补丁 | ★★☆ | S2, S4 | 6h | 中 |
| S6 | Bailout脱出 | ★★☆ | 所有 | 4h | 低 |

**总工期**: 32小时（约4个工作日，假设高效执行）

---

## 2. 详细系统设计

### S1: StatDef 定义系统

#### 2.1.1 职责

定义两个核心属性，其他所有系统通过这两个属性计算数值：
- **TrionMaxCap**: Pawn的Trion总容量（由Trion天赋基因提供）
- **TrionOutput**: Pawn的Trion输出功率（限制能力释放）

#### 2.1.2 实现步骤

**Step 1: 定义StatDef XML**

位置: `Defs/StatDefs/Stats_Trion.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <!-- Trion最大容量 -->
    <StatDef Name="TrionMaxCap">
        <defName>TrionMaxCap</defName>
        <label>Trion maximum capacity</label>
        <description>The maximum amount of Trion energy this pawn can store.</description>
        <category>BaseMisc</category>
        <defaultBaseValue>0</defaultBaseValue>
        <toStringStyle>Integer</toStringStyle>
        <displayPriorityInCategory>100</displayPriorityInCategory>
        <minValue>0</minValue>
    </StatDef>

    <!-- Trion输出功率 -->
    <StatDef Name="TrionOutput">
        <defName>TrionOutput</defName>
        <label>Trion output power</label>
        <description>The power output capacity of this pawn. Determines what abilities can be used.</description>
        <category>BaseMisc</category>
        <defaultBaseValue>0</defaultBaseValue>
        <toStringStyle>Integer</toStringStyle>
        <displayPriorityInCategory>99</displayPriorityInCategory>
        <minValue>0</minValue>
    </StatDef>
</Defs>
```

**Step 2: 定义StatModifier类（C#代码）**

```csharp
// RimTrion/Source/StatModifiers/TrionCapModifier.cs

namespace RimTrion.Stats
{
    public static class TrionStatCalculators
    {
        /// <summary>
        /// 计算Pawn的Trion最大容量
        /// 主要来源: Trion天赋基因
        /// </summary>
        public static void TrionMaxCapCalculator(StatRequest req, ref float value)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn?.genes == null)
                    return;

                // 从Gene_Trion基因读取
                Gene_Trion trionGene = pawn.genes.GetFirstGeneOfType<Gene_Trion>();
                if (trionGene != null)
                {
                    value = trionGene.Capacity; // 直接使用Gene中的容量值
                }
            }
        }

        /// <summary>
        /// 计算Pawn的Trion输出功率
        /// 主要来源: Trion天赋等级
        /// </summary>
        public static void TrionOutputCalculator(StatRequest req, ref float value)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn?.genes == null)
                    return;

                Gene_Trion trionGene = pawn.genes.GetFirstGeneOfType<Gene_Trion>();
                if (trionGene != null)
                {
                    // 根据天赋等级计算输出功率
                    // 示例: S级=80, A级=60, B级=40, C级=20, D级=10
                    value = trionGene.CalculateOutputPower();
                }
            }
        }
    }
}
```

**Step 3: 注册Modifier**

在主模组初始化类中注册：

```csharp
// RimTrion/Source/RimTrionMod.cs

public class RimTrionMod : Mod
{
    public override void OnInitialize()
    {
        base.OnInitialize();

        // 注册Stat计算器
        StatDefOf.TrionMaxCap.GetStatValueForBuilding =
            TrionStatCalculators.TrionMaxCapCalculator;
        StatDefOf.TrionOutput.GetStatValueForBuilding =
            TrionStatCalculators.TrionOutputCalculator;
    }
}
```

#### 2.1.3 验收标准

- [ ] 两个StatDef在游戏中正确加载
- [ ] Pawn的TrionMaxCap值 = Gene_Trion.Capacity
- [ ] Pawn的TrionOutput值 = Gene_Trion.OutputPower
- [ ] 在开发者菜单中可看到这两个Stat值

#### 2.1.4 关键假设

- 假设1: Gene_Trion会在Pawn生成时自动添加（通过基因系统）
- 假设2: StatDef的defaultBaseValue为0不会引起崩溃

#### 2.1.5 风险点

- **风险**: StatModifier的动态注册可能不生效
  - 缓解: 使用StatWorker而不是直接注册方法

---

### S2: Gene_Trion 资源系统实现

#### 2.2.1 职责

- 管理Pawn的Trion能量（总量、占用量、消耗量）
- 提供能量扣费接口（Consume方法）
- 与UI系统集成（继承Gene_Resource自动获得UI）
- 数据存档（ExposeData）

#### 2.2.2 实现步骤

**Step 1: 定义GeneDef XML**

位置: `Defs/GeneDefs/Genes_Trion.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <GeneDef Name="Gene_TrionBase">
        <defName>Gene_Trion</defName>
        <label>Trion ability</label>
        <description>This pawn can generate and control Trion energy, the foundation of the Trigger system.</description>
        <iconPath>UI/Icons/Genes/Gene_Trion</iconPath>
        <geneClass>RimTrion.Gene_Trion</geneClass>
        <resourceLabel>Trion</resourceLabel>
        <resourceGizmoType>RimTrion.GeneGizmo_TrionResource</resourceGizmoType>
        <biostatValue>0</biostatValue>
    </GeneDef>
</Defs>
```

**Step 2: 实现Gene_Trion类**

```csharp
// RimTrion/Source/Gene/Gene_Trion.cs

using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace RimTrion
{
    public class Gene_Trion : Gene_Resource
    {
        // ============ 字段 ============

        /// <summary>
        /// Trion容量决定因素：天赋等级
        /// 可能的值: S, A, B, C, D, E
        /// </summary>
        private TrionTalentLevel talentLevel = TrionTalentLevel.C;

        /// <summary>
        /// 占用量：已装备的触发器消耗的容量
        /// </summary>
        private float reserved = 0f;

        /// <summary>
        /// 是否正在虚拟体形态
        /// </summary>
        private bool isInTrionBody = false;

        // ============ 属性 ============

        /// <summary>
        /// 初始化容量（根据天赋等级）
        /// </summary>
        public override float InitialResourceMax
        {
            get
            {
                return talentLevel switch
                {
                    TrionTalentLevel.S => 1500f,
                    TrionTalentLevel.A => 1200f,
                    TrionTalentLevel.B => 900f,
                    TrionTalentLevel.C => 600f,
                    TrionTalentLevel.D => 300f,
                    TrionTalentLevel.E => 150f,
                    _ => 600f,
                };
            }
        }

        public override float MinLevelForAlert => Max * 0.2f; // 20%为警告线

        protected override Color BarColor => new Color(0.2f, 0.8f, 1f); // 青蓝色

        protected override Color BarHighlightColor => new Color(0.5f, 1f, 1f);

        public float Reserved => reserved;

        public float Available => Value - Reserved;

        public bool IsInTrionBody => isInTrionBody;

        public TrionTalentLevel TalentLevel => talentLevel;

        // ============ 公共方法 ============

        /// <summary>
        /// 消耗Trion能量
        /// </summary>
        public bool TryConsume(float amount, bool isCore = false)
        {
            if (Available < amount)
            {
                if (isCore)
                {
                    // 核心损耗时强制消耗，触发Bailout
                    return true; // 返回true表示应该触发Bailout
                }
                return false;
            }

            Value -= amount;
            return true;
        }

        /// <summary>
        /// 预留容量（装备触发器时调用）
        /// </summary>
        public bool TryReserve(float amount)
        {
            if (Available < amount)
                return false;

            reserved += amount;
            return true;
        }

        /// <summary>
        /// 释放容量（卸除触发器时调用）
        /// </summary>
        public void Unreserve(float amount)
        {
            reserved = Mathf.Max(0, reserved - amount);
        }

        /// <summary>
        /// 进入虚拟体形态
        /// </summary>
        public void EnterTrionBody()
        {
            isInTrionBody = true;
        }

        /// <summary>
        /// 离开虚拟体形态
        /// </summary>
        public void ExitTrionBody()
        {
            isInTrionBody = false;
        }

        /// <summary>
        /// 计算输出功率（用于能力释放检查）
        /// </summary>
        public float CalculateOutputPower()
        {
            return talentLevel switch
            {
                TrionTalentLevel.S => 80f,
                TrionTalentLevel.A => 60f,
                TrionTalentLevel.B => 40f,
                TrionTalentLevel.C => 20f,
                TrionTalentLevel.D => 10f,
                TrionTalentLevel.E => 5f,
                _ => 20f,
            };
        }

        // ============ 生命周期 ============

        public override void PostAdd()
        {
            base.PostAdd();

            // 初始化时，检查Pawn是否有Trion天赋Trait
            if (pawn.story?.traits != null)
            {
                var trionTrait = pawn.story.traits.TraitsListForReading
                    .FirstOrDefault(t => t.def.defName.Contains("TrionTalent"));

                if (trionTrait != null)
                {
                    // 从Trait中读取天赋等级
                    // 这里假设Trait的label中包含等级信息
                    talentLevel = ExtractTalentLevel(trionTrait);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reserved, "reserved", 0f);
            Scribe_Values.Look(ref isInTrionBody, "isInTrionBody", false);
            Scribe_Values.Look(ref talentLevel, "talentLevel", TrionTalentLevel.C);
        }

        // ============ 辅助方法 ============

        private TrionTalentLevel ExtractTalentLevel(Trait trait)
        {
            // 从Trait标签中提取天赋等级
            // 实际实现取决于Trait设计
            return TrionTalentLevel.C;
        }
    }

    /// <summary>
    /// Trion天赋等级枚举
    /// </summary>
    public enum TrionTalentLevel
    {
        S, A, B, C, D, E
    }
}
```

**Step 3: 实现GeneGizmo UI类**

```csharp
// RimTrion/Source/Gene/GeneGizmo_TrionResource.cs

namespace RimTrion
{
    public class GeneGizmo_TrionResource : GeneGizmo_Resource
    {
        public GeneGizmo_TrionResource(Gene_Trion gene) : base(gene)
        {
            // 调用父类构造（复用官方UI代码）
        }

        // 可以在这里扩展UI显示逻辑
        // 例如添加"占用量"的显示
    }
}
```

#### 2.2.3 验收标准

- [ ] Gene_Trion在Pawn上正确添加和显示
- [ ] UI进度条正常显示Trion值
- [ ] TryConsume / TryReserve 方法工作正常
- [ ] 存档/读档能保存Trion数值和占用量
- [ ] 计算的OutputPower根据天赋等级正确返回

#### 2.2.4 关键假设

- 假设1: Trion天赋Trait将在另外阶段定义
- 假设2: Gene系统在RW 1.5稳定（已由可行性报告验证）

#### 2.2.5 风险点

- **风险**: GeneGizmo_Resource的构造可能需要额外参数
  - 缓解: 参考官方源码的GeneGizmo实现

---

### S3: TrionSnapshot 快照系统

#### 2.3.1 职责

- 变身时保存肉身完整状态（Hediff、Needs、装备）
- 解除时恢复肉身到快照状态
- 保证数据安全（不存引用，只存数据）
- 处理GC问题（避免悬垂指针）

#### 2.3.2 实现步骤

**Step 1: 定义快照数据结构**

```csharp
// RimTrion/Source/Snapshot/TrionSnapshot.cs

using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RimTrion
{
    /// <summary>
    /// Pawn肉身状态的完整快照
    /// 用于虚拟体变身时保存，解除时恢复
    /// </summary>
    public class TrionSnapshot : IExposable
    {
        // ============ 保存的数据 ============

        /// <summary>
        /// 保存的Hediff列表（只存数据，不存引用）
        /// </summary>
        public List<HediffSnapshot> SavedHediffs = new List<HediffSnapshot>();

        /// <summary>
        /// 保存的Needs值
        /// </summary>
        public Dictionary<NeedDef, float> SavedNeeds = new Dictionary<NeedDef, float>();

        /// <summary>
        /// 保存的装备（手上拿的武器等）
        /// </summary>
        public List<Thing> SavedEquipment = new List<Thing>();

        /// <summary>
        /// 保存的库存（背包里的东西）
        /// </summary>
        public List<Thing> SavedInventory = new List<Thing>();

        /// <summary>
        /// 保存的时间戳（用于调试）
        /// </summary>
        public int SaveTick = 0;

        // ============ 公共方法 ============

        /// <summary>
        /// 从Pawn创建快照
        /// </summary>
        public static TrionSnapshot Create(Pawn pawn)
        {
            if (pawn == null)
                return null;

            TrionSnapshot snapshot = new TrionSnapshot();
            snapshot.SaveTick = Find.TickManager.TicksGame;

            // 保存Hediff
            if (pawn.health?.hediffSet != null)
            {
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    // 跳过某些特殊Hediff（如虚拟体状态）
                    if (hediff.def.defName == "Hediff_TrionBody")
                        continue;

                    HediffSnapshot hs = HediffSnapshot.FromHediff(hediff);
                    snapshot.SavedHediffs.Add(hs);
                }
            }

            // 保存Needs
            if (pawn.needs != null)
            {
                foreach (Need need in pawn.needs.AllNeeds)
                {
                    snapshot.SavedNeeds[need.def] = need.CurLevel;
                }
            }

            // 保存装备（浅拷贝引用，因为装备不应该被移除）
            if (pawn.equipment != null)
            {
                snapshot.SavedEquipment.AddRange(pawn.equipment.AllEquipmentListForReading);
            }

            // 保存库存
            if (pawn.inventory != null)
            {
                snapshot.SavedInventory.AddRange(pawn.inventory.innerContainer);
            }

            return snapshot;
        }

        /// <summary>
        /// 将快照恢复到Pawn
        /// 注意: 只恢复Hediff和Needs，装备由战斗体系统管理
        /// </summary>
        public void Restore(Pawn pawn)
        {
            if (pawn == null)
                return;

            // 第一步: 清除所有Hediff（包括战斗体期间产生的伤口）
            pawn.health.RemoveAllHediffs();

            // 第二步: 重建保存的Hediff（使用MakeHediff确保PostMake调用）
            foreach (HediffSnapshot hs in SavedHediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(hs.def, pawn, hs.bodyPart);

                if (hediff is Hediff_Injury injury)
                {
                    injury.Severity = hs.severity;
                }
                else if (hediff is HediffWithComps withComps)
                {
                    hediff.Severity = hs.severity;
                }

                pawn.health.AddHediff(hediff);
            }

            // 第三步: 恢复Needs
            foreach (var kvp in SavedNeeds)
            {
                Need need = pawn.needs.TryGetNeed(kvp.Key);
                if (need != null)
                {
                    need.CurLevel = kvp.Value;
                }
            }

            // 第四步: 通知系统状态改变
            pawn.health.summaryHealth.Notify_HealthChanged();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref SavedHediffs, "savedHediffs", LookMode.Deep);
            Scribe_Collections.Look(ref SavedNeeds, "savedNeeds", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref SavedEquipment, "savedEquipment", LookMode.Reference);
            Scribe_Collections.Look(ref SavedInventory, "savedInventory", LookMode.Deep);
            Scribe_Values.Look(ref SaveTick, "saveTick", 0);
        }
    }

    /// <summary>
    /// Hediff的数据快照（不包含引用）
    /// </summary>
    public class HediffSnapshot : IExposable
    {
        public HediffDef def;
        public float severity;
        public BodyPartRecord bodyPart;

        public HediffSnapshot() { }

        public HediffSnapshot(HediffDef def, float severity, BodyPartRecord bodyPart)
        {
            this.def = def;
            this.severity = severity;
            this.bodyPart = bodyPart;
        }

        public static HediffSnapshot FromHediff(Hediff hediff)
        {
            return new HediffSnapshot
            {
                def = hediff.def,
                severity = hediff.Severity,
                bodyPart = hediff.Part,
            };
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref severity, "severity", 0f);
            Scribe_References.Look(ref bodyPart, "bodyPart");
        }
    }
}
```

**Step 2: 单元测试框架**

```csharp
// RimTrion/Source/Tests/TrionSnapshotTests.cs

#if DEBUG

using RimWorld;
using Verse;
using NUnit.Framework;

namespace RimTrion.Tests
{
    [TestFixture]
    public class TrionSnapshotTests
    {
        private Pawn testPawn;

        [SetUp]
        public void Setup()
        {
            // 创建测试用Pawn
            testPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
        }

        [Test]
        public void TestSnapshotCreation()
        {
            // 给Pawn添加一些Hediff（伤口、疾病等）
            testPawn.health.AddHediff(HediffDefOf.Bruise);

            // 创建快照
            TrionSnapshot snapshot = TrionSnapshot.Create(testPawn);

            // 验证快照包含Hediff
            Assert.AreEqual(1, snapshot.SavedHediffs.Count);
        }

        [Test]
        public void TestSnapshotRestore()
        {
            // 创建快照
            TrionSnapshot snapshot = TrionSnapshot.Create(testPawn);

            // 给Pawn添加新伤口（模拟战斗体期间的伤害）
            testPawn.health.AddHediff(HediffDefOf.Cut);

            // 恢复快照
            snapshot.Restore(testPawn);

            // 验证新伤口被移除
            Assert.IsNull(testPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut));
        }

        [Test]
        public void TestSnapshotPreservesLimbs()
        {
            // 移除一条腿（永久缺失）
            var legs = testPawn.RaceProps.body.AllParts.FindAll(p => p.def == BodyPartDefOf.Leg);
            if (legs.Count > 0)
            {
                testPawn.health.AddHediff(HediffDefOf.MissingBodyPart, legs[0]);
            }

            // 创建快照
            TrionSnapshot snapshot = TrionSnapshot.Create(testPawn);

            // 恢复后，缺失肢体仍然存在
            snapshot.Restore(testPawn);
            Assert.IsNotNull(testPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.MissingBodyPart));
        }
    }
}

#endif
```

#### 2.3.3 验收标准

- [ ] 快照能正确保存Hediff数据（不涉及复杂Hediff）
- [ ] 快照恢复后肉身状态100%匹配快照
- [ ] 战斗体期间的伤口在恢复后完全消失
- [ ] 永久缺失肢体在快照中保留
- [ ] Needs值正确保存和恢复
- [ ] 单元测试全部通过

#### 2.3.4 关键假设

- 假设1: Phase 1不处理复杂Hediff（如寄生虫、怀孕）
- 假设2: Needs会在另外的Patch中锁定（不是快照的职责）

#### 2.3.5 风险点

- **风险**: 某些Hediff的MakeHediff后PostMake可能失败
  - 缓解: 使用try-catch包装，失败时记录日志
- **风险**: BodyPartRecord可能不可Scribe
  - 缓解: 使用bodyPart.Index代替

---

### S4: Hediff_TrionBody 虚拟体状态管理

#### 2.4.1 职责

- 代表Pawn的虚拟体形态
- 维护快照数据
- 驱动伤口泄漏计算
- 协调虚拟体的生命周期

#### 2.4.2 实现步骤

**Step 1: 定义HediffDef XML**

位置: `Defs/HediffDefs/Hediffs_Trion.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <HediffDef Name="Hediff_TrionBodyBase">
        <defName>Hediff_TrionBody</defName>
        <label>Trion body</label>
        <description>A virtual body made of Trion energy. This status hediff represents the Trigger transformation.</description>
        <hediffClass>RimTrion.Hediff_TrionBody</hediffClass>
        <comps>
            <li Class="RimTrion.HediffComp_TrionLeaker" />
        </comps>
        <stages>
            <li>
                <label>active</label>
            </li>
        </stages>
    </HediffDef>
</Defs>
```

**Step 2: 实现Hediff_TrionBody类**

```csharp
// RimTrion/Source/Hediff/Hediff_TrionBody.cs

using RimWorld;
using Verse;

namespace RimTrion
{
    /// <summary>
    /// 虚拟体Hediff
    /// 表示Pawn已进入Trigger变身状态
    /// 持有肉身快照数据
    /// </summary>
    public class Hediff_TrionBody : Hediff
    {
        private TrionSnapshot snapshot;

        public TrionSnapshot Snapshot => snapshot;

        public override void PostAdd()
        {
            base.PostAdd();

            // 创建快照
            snapshot = TrionSnapshot.Create(pawn);

            if (snapshot == null)
            {
                Log.Error($"Failed to create snapshot for {pawn.Name}");
                return;
            }

            // 进入虚拟体形态
            Gene_Trion trionGene = pawn.genes?.GetFirstGeneOfType<Gene_Trion>();
            if (trionGene != null)
            {
                trionGene.EnterTrionBody();
            }

            // 移除所有外伤（只保留永久性缺失）
            pawn.health.RemoveAllHediffs();

            // 重新添加虚拟体Hediff本身（RemoveAllHediffs会删除它）
            pawn.health.hediffSet.AddDirect(this, null, null);

            Log.Message($"{pawn.Name} entered Trion body at tick {Find.TickManager.TicksGame}");
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            // 离开虚拟体形态
            Gene_Trion trionGene = pawn.genes?.GetFirstGeneOfType<Gene_Trion>();
            if (trionGene != null)
            {
                trionGene.ExitTrionBody();
            }

            // 恢复快照
            if (snapshot != null)
            {
                snapshot.Restore(pawn);
            }

            // 施加Trion枯竭debuff
            pawn.health.AddHediff(HediffDefOf.TraumaticShock); // 临时使用，后续替换为自定义debuff

            Log.Message($"{pawn.Name} exited Trion body at tick {Find.TickManager.TicksGame}");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref snapshot, "snapshot");
        }
    }
}
```

**Step 3: 实现HediffComp_TrionLeaker 泄漏组件**

```csharp
// RimTrion/Source/Hediff/HediffComp_TrionLeaker.cs

using RimWorld;
using Verse;

namespace RimTrion
{
    /// <summary>
    /// Hediff组件: Trion泄漏逻辑
    /// 每隔一定周期计算伤口泄漏速率，扣除Trion
    /// </summary>
    public class HediffComp_TrionLeaker : HediffComp
    {
        private const int LEAK_TICK_INTERVAL = 60; // 每60 ticks计算一次泄漏

        private int lastLeakTick = 0;

        public override void CompTick()
        {
            base.CompTick();

            // 只在虚拟体形态时计算泄漏
            if (!parent.pawn.IsInTrionBody())
                return;

            // 每60ticks计算一次
            if (Find.TickManager.TicksGame - lastLeakTick < LEAK_TICK_INTERVAL)
                return;

            lastLeakTick = Find.TickManager.TicksGame;

            // 计算泄漏速率
            float leakRate = CalculateLeakRate();

            // 扣除Trion
            Gene_Trion trionGene = parent.pawn.genes?.GetFirstGeneOfType<Gene_Trion>();
            if (trionGene != null && leakRate > 0)
            {
                bool success = trionGene.TryConsume(leakRate);

                // 如果Trion耗尽，触发Bailout
                if (!success && trionGene.Available <= 0)
                {
                    // TODO: 触发Bailout
                }
            }
        }

        /// <summary>
        /// 根据伤口计算泄漏速率（Trion/周期）
        /// </summary>
        private float CalculateLeakRate()
        {
            float rate = 0f;

            foreach (Hediff hediff in parent.pawn.health.hediffSet.hediffs)
            {
                if (hediff == parent)
                    continue; // 跳过虚拟体本身

                // 根据伤口类型计算泄漏
                if (hediff is Hediff_Injury injury)
                {
                    if (injury.IsBleeding)
                    {
                        // 出血伤口: 小泄漏
                        rate += 2f;
                    }
                    else if (injury.Severity > 0.5f)
                    {
                        // 严重伤口: 中等泄漏
                        rate += 3f;
                    }
                    else
                    {
                        // 轻伤: 轻微泄漏
                        rate += 1f;
                    }
                }
                else if (hediff is Hediff_MissingPart missingPart)
                {
                    // 断肢: 大量泄漏
                    rate += 5f;
                }
            }

            return rate;
        }
    }
}
```

**Step 4: 扩展方法（为Pawn添加便利方法）**

```csharp
// RimTrion/Source/Extensions/PawnExtensions.cs

using RimWorld;
using Verse;

namespace RimTrion
{
    public static class PawnExtensions
    {
        /// <summary>
        /// 检查Pawn是否在虚拟体形态
        /// </summary>
        public static bool IsInTrionBody(this Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff(HediffDefOf.Hediff_TrionBody);
        }

        /// <summary>
        /// 获取Pawn的虚拟体Hediff
        /// </summary>
        public static Hediff_TrionBody GetTrionBody(this Pawn pawn)
        {
            return pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hediff_TrionBody)
                as Hediff_TrionBody;
        }

        /// <summary>
        /// 获取Pawn的Gene_Trion
        /// </summary>
        public static Gene_Trion GetTrionGene(this Pawn pawn)
        {
            return pawn.genes?.GetFirstGeneOfType<Gene_Trion>();
        }
    }
}
```

#### 2.4.3 验收标准

- [ ] Hediff_TrionBody添加时自动创建快照
- [ ] 虚拟体形态下Pawn的hediffSet只包含虚拟体相关Hediff
- [ ] 快照恢复时肉身伤口完全消失
- [ ] HediffComp_TrionLeaker每60ticks计算一次泄漏
- [ ] 泄漏计算的数值合理（轻伤+重伤+断肢=合理的泄漏速度）

#### 2.4.4 关键假设

- 假设1: Pawn在虚拟体期间不会死亡（由Bailout处理）
- 假设2: 快照数据有效（S3需先完成）

---

### S5: Harmony Patch 层

#### 2.5.1 职责

- 拦截PreApplyDamage处理护盾逻辑
- 拦截PostApplyDamage计算伤害消耗
- 拦截Pawn.Kill检查Bailout条件

#### 2.5.2 实现步骤

**Step 1: 编写Harmony Patches类**

```csharp
// RimTrion/Source/Patches/DamagePatch.cs

using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTrion.Patches
{
    /// <summary>
    /// 伤害处理Patch集合
    /// </summary>
    [HarmonyPatch]
    public static class DamagePatches
    {
        /// <summary>
        /// PreApplyDamage: 在伤害应用前，检查是否有护盾阻挡
        /// </summary>
        [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.TakeDamage))]
        [HarmonyPrefix]
        private static bool PreApplyDamage(Pawn ___pawn, DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            // 只处理虚拟体形态的Pawn
            if (!___pawn.IsInTrionBody())
                return true; // 继续原逻辑

            // TODO: 护盾检查逻辑（Phase 2实现）
            // 如果护盾成功格挡，则:
            // absorbed = true;
            // 扣除护盾Trion消耗
            // return false; // 阻止原伤害逻辑

            return true; // 允许原伤害逻辑继续
        }

        /// <summary>
        /// PostApplyDamage: 伤害应用后，计算伤害对应的Trion消耗
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
        [HarmonyPostfix]
        private static void PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            // 只处理虚拟体形态
            if (!__instance.IsInTrionBody())
                return;

            Gene_Trion trionGene = __instance.GetTrionGene();
            if (trionGene == null)
                return;

            // 将伤害转化为Trion消耗
            // 简单规则: 1 damage = 1 Trion消耗
            float trionCost = totalDamageDealt;

            trionGene.TryConsume(trionCost);

            // 如果Trion耗尽，标记为应该Bailout
            if (trionGene.Available <= 0)
            {
                // 触发破裂逻辑（在Kill补丁中处理）
            }
        }

        /// <summary>
        /// Pawn.Kill: 在Pawn死亡时，检查是否应该触发Bailout而不是真正死亡
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPrefix]
        private static bool PreventKill(Pawn __instance, DamageInfo? dinfo)
        {
            // 只处理虚拟体形态
            if (!__instance.IsInTrionBody())
                return true; // 继续原死亡逻辑

            // 在虚拟体形态下，不允许真正死亡
            // 而是触发解除逻辑
            TrionBailout(__instance);
            return false; // 阻止原死亡逻辑
        }

        /// <summary>
        /// Bailout脱出逻辑
        /// 移除虚拟体Hediff，触发快照恢复
        /// </summary>
        private static void TrionBailout(Pawn pawn)
        {
            Hediff_TrionBody trionBody = pawn.GetTrionBody();
            if (trionBody != null)
            {
                // 移除虚拟体Hediff，自动触发PostRemoved恢复快照
                pawn.health.RemoveHediff(trionBody);
            }

            Log.Message($"{pawn.Name} bailed out from Trion body!");
        }
    }
}
```

**Step 2: 注册Harmony Patches**

```csharp
// RimTrion/Source/RimTrionMod.cs

using HarmonyLib;
using Verse;

namespace RimTrion
{
    public class RimTrionMod : Mod
    {
        public RimTrionMod(ModContentPack content) : base(content)
        {
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            // 创建Harmony实例
            var harmony = new Harmony("RimTrion.Patches");

            // 自动发现和应用所有标记了[HarmonyPatch]的类
            harmony.PatchAll();

            Log.Message("RimTrion Harmony patches applied!");
        }
    }
}
```

#### 2.5.3 验收标准

- [ ] 虚拟体形态下，Pawn受伤时Trion被扣除
- [ ] 伤害消耗 = 伤害值（1:1转换）
- [ ] 虚拟体形态下Pawn不会真正死亡
- [ ] Pawn死亡时自动触发Bailout并恢复肉身
- [ ] 没有虚拟体Hediff的Pawn不受影响

#### 2.5.4 关键假设

- 假设1: Harmony库已正确配置
- 假设2: Pawn_HealthTracker.TakeDamage是伤害处理的入口

---

### S6: Bailout 脱出系统

#### 2.6.1 职责

- 检查Bailout触发条件（Trion耗尽或致命伤害）
- 执行脱出流程（Hediff移除 → 快照恢复 → debuff施加）
- 管理占用量流失和能量清零逻辑

#### 2.6.2 实现步骤

**Step 1: 创建Bailout管理器**

```csharp
// RimTrion/Source/Bailout/TrionBailoutManager.cs

using RimWorld;
using Verse;

namespace RimTrion
{
    /// <summary>
    /// Bailout脱出管理
    /// 协调虚拟体解除的各个环节
    /// </summary>
    public static class TrionBailoutManager
    {
        /// <summary>
        /// 执行Bailout脱出
        /// </summary>
        public static void DoBailout(Pawn pawn, TrionBailoutReason reason)
        {
            if (!pawn.IsInTrionBody())
                return;

            Gene_Trion trionGene = pawn.GetTrionGene();
            Hediff_TrionBody trionBody = pawn.GetTrionBody();

            if (trionGene == null || trionBody == null)
                return;

            // 第一步: 根据脱出原因计算占用量流失
            float reservedLoss = 0f;

            switch (reason)
            {
                case TrionBailoutReason.EnergyDepleted:
                    // 能量耗尽: 所有占用量流失
                    reservedLoss = trionGene.Reserved;
                    break;

                case TrionBailoutReason.LethaldDamage:
                    // 致命伤害: 所有占用量流失
                    reservedLoss = trionGene.Reserved;
                    break;

                case TrionBailoutReason.ManualExit:
                    // 主动解除: 占用量全部返还
                    reservedLoss = 0f;
                    break;
            }

            // 第二步: 清零Trion（根据reason决定是流失还是返还）
            float trionAfterBailout = trionGene.Value - reservedLoss;
            trionGene.Value = Mathf.Max(0, trionAfterBailout);

            // 第三步: 移除虚拟体Hediff（自动触发快照恢复）
            pawn.health.RemoveHediff(trionBody);

            // 第四步: 施加Bailout debuff（如果不是主动解除）
            if (reason != TrionBailoutReason.ManualExit)
            {
                // TODO: 添加自定义TrionExhaustion debuff
                // pawn.health.AddHediff(HediffDefOf.TrionExhaustion);
            }

            // 第五步: 发送消息
            string msg = reason switch
            {
                TrionBailoutReason.EnergyDepleted => $"{pawn.Name}'s Trion energy was depleted. Bailout triggered.",
                TrionBailoutReason.LethaldDamage => $"{pawn.Name} suffered fatal damage. Emergency bailout!",
                TrionBailoutReason.ManualExit => $"{pawn.Name} exited Trion body.",
                _ => "Unknown bailout reason",
            };
            Messages.Message(msg, pawn, MessageTypeDefOf.NeedsAttention);
        }

        /// <summary>
        /// 检查Bailout触发条件
        /// 由Patch定期调用
        /// </summary>
        public static void CheckBailoutConditions(Pawn pawn)
        {
            if (!pawn.IsInTrionBody())
                return;

            Gene_Trion trionGene = pawn.GetTrionGene();
            if (trionGene == null)
                return;

            // 条件1: Trion耗尽
            if (trionGene.Available <= 0)
            {
                DoBailout(pawn, TrionBailoutReason.EnergyDepleted);
                return;
            }

            // 条件2: 核心部位破损（由Kill Patch处理）
            // 见 DamagePatches.PreventKill
        }
    }

    /// <summary>
    /// Bailout脱出的原因
    /// </summary>
    public enum TrionBailoutReason
    {
        /// <summary>Trion能量耗尽</summary>
        EnergyDepleted,

        /// <summary>致命伤害（头部或心脏）</summary>
        LethaldDamage,

        /// <summary>主动解除（玩家主动)</summary>
        ManualExit,
    }
}
```

**Step 2: 集成到HediffComp_TrionLeaker**

修改S4中的HediffComp_TrionLeaker，在泄漏计算后检查Bailout条件：

```csharp
public override void CompTick()
{
    base.CompTick();

    if (!parent.pawn.IsInTrionBody())
        return;

    if (Find.TickManager.TicksGame - lastLeakTick < LEAK_TICK_INTERVAL)
        return;

    lastLeakTick = Find.TickManager.TicksGame;

    float leakRate = CalculateLeakRate();
    Gene_Trion trionGene = parent.pawn.genes?.GetFirstGeneOfType<Gene_Trion>();

    if (trionGene != null && leakRate > 0)
    {
        trionGene.TryConsume(leakRate);

        // 检查Bailout条件
        TrionBailoutManager.CheckBailoutConditions(parent.pawn);
    }
}
```

#### 2.6.3 验收标准

- [ ] Trion耗尽时自动触发Bailout
- [ ] Bailout后快照成功恢复，肉身伤口消失
- [ ] 占用量和消耗的Trion被正确清零
- [ ] debuff成功施加（后续实现）
- [ ] 主动解除不扣除占用量

#### 2.6.4 关键假设

- 假设1: Kill Patch正确拦截致命伤害
- 假设2: Hediff移除后PostRemoved自动调用

---

## 3. 子系统依赖与集成

### 3.1 执行顺序

```
【第一阶段】 (并行, 1-2天)
├─ S1: StatDef定义 (2h)
└─ S2: Gene_Trion实现 (4h, 依赖S1)

【第二阶段】 (并行, 2-3天)
├─ S3: TrionSnapshot (8h)
├─ S4: Hediff_TrionBody (8h, 依赖S3)
└─ S5: Harmony Patch (6h)

【第三阶段】 (2天)
├─ S6: Bailout系统 (4h, 依赖S4+S5)
└─ 集成测试 (8h)

总计: ~40小时 = 5个工作日 (高效执行)
```

### 3.2 接口定义

#### Gene_Trion 公开接口

```csharp
public class Gene_Trion
{
    // 查询
    public float Capacity { get; }          // 总容量
    public float Value { get; set; }        // 当前值
    public float Reserved { get; }          // 占用量
    public float Available => Value - Reserved; // 可用量
    public bool IsInTrionBody { get; }      // 是否在虚拟体形态

    // 操作
    public bool TryConsume(float amount);
    public bool TryReserve(float amount);
    public void Unreserve(float amount);
    public void EnterTrionBody();
    public void ExitTrionBody();
}
```

#### HediffComp_TrionLeaker 公开接口

```csharp
public class HediffComp_TrionLeaker : HediffComp
{
    // 计算泄漏（可被外部调用查询）
    public float CalculateLeakRate();
}
```

#### TrionBailoutManager 公开接口

```csharp
public static class TrionBailoutManager
{
    public static void DoBailout(Pawn pawn, TrionBailoutReason reason);
    public static void CheckBailoutConditions(Pawn pawn);
}
```

---

## 4. 集成测试计划

### 4.1 测试场景 (MVP Scenario)

**场景**: 小人A装备有Trigger能力 → 变身 → 被砍断手臂 → Trion加速泄漏 → 主动解除 → 手臂恢复正常

**测试步骤**:

1. **创建测试Pawn**
   - 创建一个拥有Gene_Trion的Pawn
   - 验证TrionMaxCap = 600 (C级天赋)
   - 验证初始Trion = 600

2. **变身测试**
   - 通过调试命令或UI添加Hediff_TrionBody
   - 验证快照成功创建
   - 验证肉身hediffSet被清空
   - 验证虚拟体UI显示Trion进度条

3. **受伤测试**
   - 给虚拟体Pawn造成10点伤害（砍）
   - 验证Trion被扣除10点（现在=590）
   - 验证Pawn的health.hediffSet包含Cut Hediff

4. **断肢泄漏测试**
   - 继续造成50点伤害到同一肢体（累计60点）
   - 验证肢体断裂（MissingBodyPart Hediff）
   - 验证接下来的Tick中Trion以5/60s的速率泄漏
   - 等待12秒，验证Trion减少约60点

5. **解除测试**
   - 移除Hediff_TrionBody（模拟主动解除）
   - 验证快照恢复：
     - 断裂肢体恢复（MissingBodyPart被移除）
     - Cut Hediff被移除
     - 肉身回到原始状态
   - 验证Trion值保持（因为是主动解除，占用量返还）

### 4.2 边界测试

| 测试 | 条件 | 预期结果 |
|------|------|--------|
| 极端泄漏 | 两条腿+两只手臂都断裂 | Trion每60ticks泄漏20点，可持续约30秒 |
| 零Trion解除 | Trion耗尽时强制解除 | 快照恢复成功，肉身所有伤口消失 |
| 没有Gene_Trion的Pawn | 尝试变身 | 添加Hediff失败或报错（由系统处理） |
| Mod兼容性 | 其他Mod Patch同样位置 | Harmony priority机制控制顺序 |

### 4.3 性能测试

| 测试 | 指标 |
|------|------|
| 快照创建时间 | < 1ms (100个Hediff) |
| 快照恢复时间 | < 2ms |
| 泄漏计算时间 | < 0.5ms (60ticks一次) |
| FPS影响 | < 1% 下降 (60fps基准) |

---

## 5. 验收标准汇总

### Phase 1验收清单

#### 功能验收

- [ ] S1: StatDef正确定义和计算
- [ ] S2: Gene_Trion正确继承和实现
- [ ] S3: TrionSnapshot完整保存和恢复
- [ ] S4: Hediff_TrionBody正确管理虚拟体生命周期
- [ ] S5: Harmony Patch正确拦截伤害
- [ ] S6: Bailout正确触发和执行

#### 质量验收

- [ ] 所有Unit Tests通过
- [ ] MVP场景完整运行
- [ ] 边界测试全部通过
- [ ] 性能测试满足指标
- [ ] 代码注释完整（≥80%）
- [ ] 0个Critical Error日志

#### 文档验收

- [ ] API文档完整
- [ ] 使用示例清晰
- [ ] 已知限制标注
- [ ] 后续扩展建议提出

---

## 6. 风险与缓解

### 高风险项

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| TrionSnapshot复杂Hediff不兼容 | 中 | 高 | 黑名单 + 捕异常 |
| Harmony Patch冲突 | 低 | 高 | Priority + 版本检查 |
| 性能泄漏 | 中 | 中 | 周期限制(60tick) |

### 中等风险项

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| GC悬垂指针 | 低 | 中 | 只存数据不存引用 |
| Needs锁定失败 | 低 | 中 | 备用方案:快照保存Needs |
| UI进度条显示异常 | 低 | 低 | 使用官方GeneGizmo |

---

## 7. 后续Phase预告

### Phase 2: 触发器系统 (预计4-6周)
- CompTriggerHolder 插槽容器
- TriggerChip 芯片系统
- 武器生成与CompEphemeral
- 组合技(Combo)系统

### Phase 3: 兵器与建筑 (预计6-8周)
- Trion士兵 (Bamster, Marmod)
- 全局并行限制系统
- MapComponent_TrionGrid 能源网
- Trion 建筑（发电机、炮台）

---

**文档版本**: 1.0
**最后更新**: 2026-01-23
**下一阶段**: Phase 1开发启动前评审

