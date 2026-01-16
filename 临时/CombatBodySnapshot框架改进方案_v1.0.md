---
摘要: 基于值快照设计的CombatBodySnapshot框架改进方案。修复对象引用导致的快照语义破坏问题，提供应用层忽略机制，确保"快照时冻结、恢复时完全回滚"的设计意图得以实现。
版本号: v1.0
修改时间: 2026-01-14
关键词: 值快照, Hediff生命周期, 快照语义, 设计缺陷修复, 框架改进
标签: [定稿]
---

# CombatBodySnapshot 框架改进方案 v1.0

## 执行摘要

### 原始设计意图（用户确认）

```
快照时刻 T0：肉身状态被完整记录和冻结
             ↓
             战斗体生成 → 肉身状态应保持快照状态
             ↓
恢复时刻 T1：肉身完全恢复到T0的快照状态
             （不是运行时已修改的状态）
```

### 现实问题（关键缺陷）

现有实现**保存Hediff对象引用**而非**值**，导致：
- ❌ Hediff.duration等属性在运行时被修改（每tick递减等）
- ❌ 快照恢复时使用的是运行时修改后的值，而非快照时的值
- ❌ **违反了"快照时冻结"的设计意图**

### 具体场景

```
快照时刻：伤口Hediff duration=3秒
运行时：每个tick都会duration -= 1
恢复时：用的是duration=0或负数的版本！（错误）
期望：应该恢复到duration=3的版本（正确）
```

### 修复方案（框架层改进）

1. ✅ **引入HediffSnapshot结构**：保存Hediff的值（def, duration, severity, part等）
2. ✅ **修改Capture逻辑**：保存值快照而非对象引用
3. ✅ **修改Restore逻辑**：从值快照重建新对象
4. ✅ **提供忽略机制**：应用层可指定某些Hediff不参与快照
5. ✅ **向后兼容**：支持旧存档的迁移

### 工作量

- **框架层改进**：6-8小时
- **测试覆盖**：4-6小时
- **应用层适配**：2-4小时（应用层决定具体规则）
- **总计**：12-18小时

---

## 第一部分：问题详细分析

### 问题的演示

假设一个具体场景：

```
T0（快照时）：
  小人肉身状态：
  - Hediff_Injury（伤口）
      duration: 180 ticks（约3秒）
      severity: 0.5
      part: Leg
      内存地址：0x12345678

快照操作：
  hediffs.Add(hediff);  // ← 保存引用，地址0x12345678

T0→T1（战斗过程，60 ticks）：
  RimWorld系统每tick执行：
    hediff.Tick()
      duration -= 1
      severity可能变化
      ...其他状态变化

T1（恢复时）：
  目标：恢复到T0的状态（duration=180, severity=0.5）
  实际：AddHediff(地址0x12345678)
      内容：duration=120（已减少60）
            severity=0.5（可能改变）
  结果：WRONG！应该是duration=180
```

### 为什么这是框架层的设计缺陷

1. **设计契约**：快照承诺"冻结状态"
2. **实现现实**：对象在运行时被修改
3. **结果**：设计意图无法实现，无论应用层如何实现

这**不是应用层可以修复**的问题，必须由框架层修复。

---

## 第二部分：改进方案详细设计

### 方案概述

```
旧方案：Capture → 保存对象引用 → 运行时修改 → Restore → 使用修改后的对象 ❌

新方案：Capture → 保存值快照 → Restore → 用值创建新对象 ✅
```

### 关键改进1：HediffSnapshot结构

```csharp
using System;
using Verse;
using RimWorld;

namespace ProjectTrion.Core
{
    /// <summary>
    /// Hediff的值快照（保存Hediff在快照时刻的完整状态）。
    ///
    /// 使用值快照而非对象引用，确保：
    /// 1. 快照时刻的状态被完整冻结
    /// 2. 运行时对Hediff对象的修改不影响快照
    /// 3. 恢复时使用快照时刻的值，而非运行时修改的值
    /// </summary>
    public struct HediffSnapshot : IExposable
    {
        /// <summary>
        /// Hediff的定义（ID）
        /// </summary>
        public HediffDef def;

        /// <summary>
        /// Hediff的剩余duration（ticks）。
        /// 对于有持续时间的Hediff（如伤口、疾病），这个值关键。
        /// </summary>
        public int duration;

        /// <summary>
        /// Hediff的severity（严重程度，0-1）。
        /// 控制Hediff的表现强度。
        /// </summary>
        public float severity;

        /// <summary>
        /// Hediff所在的身体部位。
        /// </summary>
        public BodyPartRecord part;

        /// <summary>
        /// 从Hediff对象创建值快照。
        ///
        /// 这是"冻结时刻"的操作，将Hediff的所有关键状态保存为值。
        /// </summary>
        public static HediffSnapshot FromHediff(Hediff hediff)
        {
            if (hediff == null)
                return default;

            return new HediffSnapshot
            {
                def = hediff.def,
                duration = hediff.duration,      // ← 冻结快照时刻的值
                severity = hediff.Severity,      // ← 冻结快照时刻的值
                part = hediff.Part               // ← 冻结快照时刻的值
            };
        }

        /// <summary>
        /// 从值快照创建新的Hediff对象。
        ///
        /// 这是"恢复"操作，使用快照中保存的值创建新对象。
        /// 新对象的状态与快照时刻完全相同，不会受到运行时修改的影响。
        /// </summary>
        public Hediff ToHediff(Pawn pawn)
        {
            if (def == null)
                return null;

            try
            {
                var hediff = HediffMaker.MakeHediff(def, pawn, part);

                // 使用快照中保存的值初始化
                hediff.duration = duration;      // ← 恢复快照时的duration
                hediff.Severity = severity;      // ← 恢复快照时的severity

                return hediff;
            }
            catch (Exception ex)
            {
                Log.Error($"CompTrion: 无法从快照创建Hediff: {def?.defName}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 序列化快照数据以保存到存档。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref duration, "duration", 0);
            Scribe_Values.Look(ref severity, "severity", 0f);
            Scribe_References.Look(ref part, "part");
        }
    }
}
```

### 关键改进2：修改CombatBodySnapshot

```csharp
using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProjectTrion.Core
{
    /// <summary>
    /// 战斗体生成时的肉身快照。
    ///
    /// 设计原则：
    /// - 快照时刻：肉身状态被完整记录和冻结
    /// - 快照内容：使用值快照而非对象引用，确保状态不被运行时修改
    /// - 恢复时刻：肉身完全恢复到快照时的状态
    ///
    /// 用于战斗体摧毁时恢复肉身到原始状态。
    /// </summary>
    public class CombatBodySnapshot : IExposable
    {
        /// <summary>
        /// 快照保存的健康数据（值快照）。
        /// 保存了快照时刻的所有Hediff状态。
        /// </summary>
        public List<HediffSnapshot> hediffSnapshots = new List<HediffSnapshot>();

        /// <summary>
        /// 快照保存的穿戴服装列表。
        /// </summary>
        public List<Apparel> apparels = new List<Apparel>();

        /// <summary>
        /// 快Snapshot保存的装备（武器）列表。
        /// </summary>
        public List<Thing> equipment = new List<Thing>();

        /// <summary>
        /// 快照保存的背包物品。
        /// </summary>
        public List<Thing> inventory = new List<Thing>();

        /// <summary>
        /// 快照的时间戳（游戏Tick）。
        /// </summary>
        public int snapshotTick;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatBodySnapshot()
        {
        }

        /// <summary>
        /// 从目标Pawn捕获当前物理状态。
        ///
        /// 这是"冻结时刻"的操作：
        /// - 保存所有Hediff的值快照（而非对象引用）
        /// - 保存所有Apparel、Equipment、Inventory
        /// - 标记快照的时间戳
        ///
        /// 快照包含：
        /// - 所有Hediff的状态（除非被ShouldIgnoreHediff忽略）
        /// - 所有Apparel（服装）
        /// - 所有Equipment（装备）
        /// - 所有Inventory物品
        ///
        /// 不快照：
        /// - 技能等级和经验值
        /// - 心理状态和心情
        /// - 社交关系
        /// </summary>
        public void CaptureFromPawn(Pawn pawn)
        {
            snapshotTick = Find.TickManager.TicksGame;

            // 捕获健康数据（值快照）
            hediffSnapshots.Clear();
            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    // 应用层可以通过重写ShouldIgnoreHediff来排除某些Hediff
                    if (!ShouldIgnoreHediff(hediff))
                    {
                        hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff));
                    }
                }
            }

            // 捕获穿戴服装
            apparels.Clear();
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    apparels.Add(apparel);
                }
            }

            // 捕获装备
            equipment.Clear();
            if (pawn.equipment?.AllEquipmentListForReading != null)
            {
                foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                {
                    equipment.Add(eq);
                }
            }

            // 捕获背包物品
            inventory.Clear();
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (var item in pawn.inventory.innerContainer)
                {
                    inventory.Add(item);
                }
            }
        }

        /// <summary>
        /// 将快照状态恢复到目标Pawn。
        ///
        /// 这是"恢复"操作：
        /// - 移除当前Pawn中所有非Essential Hediff
        /// - 从值快照创建新的Hediff对象并添加
        /// - 恢复Apparel、Equipment、Inventory
        ///
        /// 仅恢复物理状态（健康、装备、物品），不恢复心理状态。
        /// </summary>
        public void RestoreToPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CompTrion: 尝试向空Pawn恢复快照数据");
                return;
            }

            // 恢复健康数据
            if (pawn.health != null && hediffSnapshots.Count > 0)
            {
                // 第一步：移除当前所有Hediff（除了被忽略的）
                var toRemove = new List<Hediff>();
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (!ShouldIgnoreHediff(hediff))
                    {
                        toRemove.Add(hediff);
                    }
                }

                foreach (var hediff in toRemove)
                {
                    pawn.health.RemoveHediff(hediff);
                }

                // 第二步：从值快照创建新的Hediff对象并添加
                // 这保证了恢复时使用的是快照时的状态，而非运行时修改的状态
                foreach (var snapshot in hediffSnapshots)
                {
                    if (!ShouldIgnoreHediff(snapshot))  // 应用层也可以在恢复时忽略
                    {
                        var hediff = snapshot.ToHediff(pawn);
                        if (hediff != null)
                        {
                            pawn.health.AddHediff(hediff);
                        }
                    }
                }
            }

            // 恢复装备和物品
            RestoreApparel(pawn);
            RestoreInventory(pawn);

            // 不恢复心理状态
            // - 技能等级和经验值保留
            // - 心理状态和心情保留
            // - 社交关系保留
        }

        /// <summary>
        /// 恢复服装到Pawn。
        /// </summary>
        private void RestoreApparel(Pawn pawn)
        {
            if (pawn.apparel == null)
                return;

            // 脱下当前服装
            var currentApparel = new List<Apparel>(pawn.apparel.WornApparel);
            foreach (var apparel in currentApparel)
            {
                pawn.apparel.TryDrop(apparel);
            }

            // 穿上快照中的服装
            foreach (var apparel in apparels)
            {
                if (apparel != null && pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
                {
                    // RimWorld 1.6 兼容：Wear方法需要三个参数
                    pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: false);
                }
            }
        }

        /// <summary>
        /// 恢复背包物品到Pawn。
        /// </summary>
        private void RestoreInventory(Pawn pawn)
        {
            if (pawn.inventory == null)
                return;

            // 清空当前背包
            pawn.inventory.innerContainer.ClearAndDestroyContents();

            // 添加快照中的物品
            foreach (var item in inventory)
            {
                if (item != null)
                {
                    pawn.inventory.innerContainer.TryAdd(item, true);
                }
            }
        }

        /// <summary>
        /// 判断Hediff是否应该被忽略（不参与快照恢复）。
        ///
        /// 返回true的Hediff将在快照和恢复时被完全忽略：
        /// - Capture时：不保存该Hediff
        /// - Restore时：不移除该Hediff，不从快照重建
        ///
        /// 这用于保护那些与Trion系统生命周期绑定的Hediff，
        /// 例如小人本身拥有的系统性Hediff（如Trion腺体）。
        ///
        /// 应用层应该重写此方法来指定具体规则。
        ///
        /// 示例（应用层实现）：
        /// protected override bool ShouldIgnoreHediff(Hediff hediff)
        /// {
        ///     // 保护Trion腺体，使其不参与快照操作
        ///     if (hediff.def == TrionDefs.HediffTrionGland)
        ///         return true;
        ///
        ///     // 其他框架层系统Hediff也可以在这里添加
        ///     return base.ShouldIgnoreHediff(hediff);
        /// }
        /// </summary>
        protected virtual bool ShouldIgnoreHediff(Hediff hediff)
        {
            // 框架默认：不忽略任何Hediff（所有Hediff都参与快照）
            // 应用层应该重写此方法来指定忽略规则
            return false;
        }

        /// <summary>
        /// 判断HediffSnapshot是否应该被忽略（不参与恢复）。
        /// 用于恢复时的额外过滤。
        /// </summary>
        protected virtual bool ShouldIgnoreHediff(HediffSnapshot snapshot)
        {
            // 框架默认：不忽略任何快照
            return false;
        }

        /// <summary>
        /// 序列化快照数据以保存到存档。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref snapshotTick, "snapshotTick");
            Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);
            Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
            Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
            Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
        }
    }
}
```

---

## 第三部分：应用层实现指导

### 应用层需要做什么

应用层（CompTrion或其子类）需要重写ShouldIgnoreHediff来指定具体规则：

```csharp
public class CompTrion : ThingComp
{
    // ... 其他代码 ...

    private CombatBodySnapshot _snapshot;

    /// <summary>
    /// 应用层实现：定义哪些Hediff应该被快照忽略。
    /// </summary>
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff == null)
            return false;

        // 规则1：忽略Trion腺体（系统级Hediff，与Trion生命周期绑定）
        if (hediff.def == TrionDefs.HediffTrionGland)
            return true;

        // 规则2：如果需要忽略其他框架级Hediff，添加在这里
        // if (hediff.def == SomeFrameworkDef)
        //     return true;

        // 其他Hediff都参与快照恢复
        return false;
    }

    /// <summary>
    /// 战斗体生成前的准备：创建快照。
    /// </summary>
    public void GenerateCombatBody()
    {
        // ... 生成战斗体的逻辑 ...

        // 创建快照（这会调用CaptureFromPawn，其中会调用ShouldIgnoreHediff）
        _snapshot = new CombatBodySnapshot();
        _snapshot.CaptureFromPawn(this.Pawn);
    }

    /// <summary>
    /// 战斗体销毁时的恢复：从快照恢复。
    /// </summary>
    public void DestroyCombatBody()
    {
        if (_snapshot != null)
        {
            // 恢复肉身到快照状态
            // 这会调用RestoreToPawn，其中会：
            // 1. 移除所有非Ignore的Hediff
            // 2. 从值快照创建新对象并添加
            _snapshot.RestoreToPawn(this.Pawn);
        }
    }
}
```

---

## 第四部分：单元测试

### 测试1：值快照的完整性

```csharp
[TestFixture]
public class HediffSnapshotTests
{
    [Test]
    public void FromHediff_CapturesAllValues()
    {
        // Arrange
        var hediff = new Hediff_Injury
        {
            def = HediffDefOf.Cut,
            duration = 180,
            Severity = 0.5f
        };

        // Act
        var snapshot = HediffSnapshot.FromHediff(hediff);

        // Assert
        Assert.AreEqual(HediffDefOf.Cut, snapshot.def);
        Assert.AreEqual(180, snapshot.duration);
        Assert.AreEqual(0.5f, snapshot.severity);
    }

    [Test]
    public void ToHediff_RestoresAllValues()
    {
        // Arrange
        var pawn = PawnGenerationRequest.Default.KindDef.GetRandomPawn();
        var snapshot = new HediffSnapshot
        {
            def = HediffDefOf.Cut,
            duration = 180,
            severity = 0.5f,
            part = pawn.RaceProps.body.GetPartAtIndex(0)
        };

        // Act
        var hediff = snapshot.ToHediff(pawn);

        // Assert
        Assert.AreEqual(180, hediff.duration);
        Assert.AreEqual(0.5f, hediff.Severity);
        Assert.AreEqual(pawn.RaceProps.body.GetPartAtIndex(0), hediff.Part);
    }

    [Test]
    public void ValueSnapshot_NotAffectedByRuntimeChanges()
    {
        // Arrange
        var hediff = new Hediff_Injury
        {
            def = HediffDefOf.Cut,
            duration = 180,
            Severity = 0.5f
        };

        // 快照时刻
        var snapshot = HediffSnapshot.FromHediff(hediff);

        // Act：运行时修改Hediff
        hediff.duration = 0;  // 模拟tick递减
        hediff.Severity = 0.9f;  // 模拟severity变化

        // Assert：快照不受影响
        Assert.AreEqual(180, snapshot.duration);
        Assert.AreEqual(0.5f, snapshot.severity);
    }
}
```

### 测试2：快照的忽略机制

```csharp
[TestFixture]
public class CombatBodySnapshotIgnoreTests
{
    [Test]
    public void IgnoredHediff_NotCaptured()
    {
        // Arrange
        var snapshot = new TestCombatBodySnapshot();
        var pawn = CreateTestPawn();

        // 添加Trion腺体
        var trionGland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, pawn);
        pawn.health.AddHediff(trionGland);

        // 添加普通伤口
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        pawn.health.AddHediff(injury);

        // Act
        snapshot.CaptureFromPawn(pawn);

        // Assert
        Assert.AreEqual(1, snapshot.hediffSnapshots.Count);  // 只有普通伤口
        Assert.AreEqual(HediffDefOf.Cut, snapshot.hediffSnapshots[0].def);
    }

    [Test]
    public void IgnoredHediff_NotRemovedOnRestore()
    {
        // Arrange
        var snapshot = new TestCombatBodySnapshot();
        var pawn = CreateTestPawn();

        var trionGland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, pawn);
        pawn.health.AddHediff(trionGland);

        // Act
        snapshot.CaptureFromPawn(pawn);
        snapshot.RestoreToPawn(pawn);

        // Assert
        Assert.IsTrue(pawn.health.hediffSet.hediffs.Contains(trionGland));
    }
}

public class TestCombatBodySnapshot : CombatBodySnapshot
{
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff.def == TrionDefs.HediffTrionGland)
            return true;
        return false;
    }
}
```

### 测试3：快照恢复的正确性

```csharp
[TestFixture]
public class CombatBodySnapshotRestoreTests
{
    [Test]
    public void Restore_RecoversDurationCorrectly()
    {
        // Arrange
        var snapshot = new CombatBodySnapshot();
        var pawn = CreateTestPawn();

        // 创建一个有duration的Hediff
        var disease = HediffMaker.MakeHediff(HediffDefOf.Flu, pawn);
        disease.duration = 600;  // 10秒
        pawn.health.AddHediff(disease);

        // 快照
        snapshot.CaptureFromPawn(pawn);
        var originalDuration = snapshot.hediffSnapshots[0].duration;

        // 模拟运行时的修改（tick递减）
        disease.duration = 300;  // 5秒
        for (int i = 0; i < 100; i++)
        {
            disease.Tick();  // 继续递减
        }

        // Act：恢复
        pawn.health.RemoveHediff(disease);
        snapshot.RestoreToPawn(pawn);

        // Assert：恢复后的duration应该等于快照时的值
        var restoredDisease = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Flu);
        Assert.IsNotNull(restoredDisease);
        Assert.AreEqual(originalDuration, restoredDisease.duration);
    }

    [Test]
    public void Restore_RecoversSeverityCorrectly()
    {
        // Arrange
        var snapshot = new CombatBodySnapshot();
        var pawn = CreateTestPawn();

        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        injury.Severity = 0.5f;
        pawn.health.AddHediff(injury);

        snapshot.CaptureFromPawn(pawn);
        var originalSeverity = snapshot.hediffSnapshots[0].severity;

        // 运行时修改severity
        injury.Severity = 0.9f;

        // Act：恢复
        pawn.health.RemoveHediff(injury);
        snapshot.RestoreToPawn(pawn);

        // Assert
        var restored = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut);
        Assert.AreEqual(originalSeverity, restored.Severity, 0.01f);
    }
}
```

---

## 第五部分：序列化与存档兼容性

### 兼容性考虑

**问题**：旧存档使用的是对象引用，新版本改用值快照。

**解决方案**：在ExposeData中处理版本差异

```csharp
public void ExposeData()
{
    // 版本检查
    int version = 1;
    Scribe_Values.Look(ref version, "snapshotVersion", 1);

    if (version == 0)
    {
        // 旧版本处理：加载对象引用并转换为值快照
        List<Hediff> oldHediffs = new List<Hediff>();
        Scribe_Collections.Look(ref oldHediffs, "hediffs", LookMode.Deep);

        if (oldHediffs != null)
        {
            hediffSnapshots.Clear();
            foreach (var hediff in oldHediffs)
            {
                if (hediff != null)
                {
                    hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff));
                }
            }
        }
    }
    else
    {
        // 新版本：直接加载值快照
        Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);
    }

    Scribe_Values.Look(ref snapshotTick, "snapshotTick");
    Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
    Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
    Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
}
```

---

## 第六部分：集成检查清单

### 框架层集成

- [ ] 创建HediffSnapshot.cs文件，实现HediffSnapshot结构
- [ ] 修改CombatBodySnapshot.cs文件，改用值快照
- [ ] 添加ShouldIgnoreHediff虚函数
- [ ] 更新ExposeData处理新旧版本
- [ ] 更新XML注释和文档

### 应用层集成

- [ ] 在CompTrion中重写ShouldIgnoreHediff
- [ ] 指定Trion腺体的忽略规则
- [ ] 根据需要添加其他忽略规则
- [ ] 测试战斗流程中的快照恢复

### 测试集成

- [ ] 编译通过，无警告
- [ ] 所有单元测试通过
- [ ] 在游戏中进行快照/恢复测试
- [ ] 测试duration递减是否被正确恢复
- [ ] 测试severity变化是否被正确恢复
- [ ] 测试旧存档兼容性

### 文档集成

- [ ] 更新框架API文档，说明HediffSnapshot
- [ ] 更新CombatBodySnapshot的设计文档
- [ ] 在应用开发指南中添加"如何定义忽略规则"示例
- [ ] 记录值快照设计的原理

---

## 第七部分：工作量估算

| 任务 | 预计时间 | 优先级 |
|------|---------|--------|
| **框架层改进** | | |
| 1. 实现HediffSnapshot结构 | 1.5小时 | P0 |
| 2. 修改CombatBodySnapshot逻辑 | 1.5小时 | P0 |
| 3. 添加ShouldIgnoreHediff虚函数 | 0.5小时 | P0 |
| 4. 处理序列化版本兼容 | 1小时 | P1 |
| 5. 代码审查和优化 | 1小时 | P1 |
| **小计** | **5.5小时** | |
| **测试覆盖** | | |
| 6. 编写单元测试（3组，9个测试） | 3小时 | P0 |
| 7. 游戏集成测试 | 2小时 | P0 |
| 8. 旧存档兼容性测试 | 1.5小时 | P1 |
| **小计** | **6.5小时** | |
| **应用层适配** | | |
| 9. 应用层重写ShouldIgnoreHediff | 1小时 | P0 |
| 10. 应用层测试和验证 | 1.5小时 | P0 |
| **小计** | **2.5小时** | |
| **总计** | **14.5小时** |

---

## 第八部分：质量保证标准

### 功能完整性

- [ ] HediffSnapshot能正确保存和恢复Hediff的所有关键属性
- [ ] duration在运行时修改后不影响快照
- [ ] severity在运行时修改后不影响快照
- [ ] ShouldIgnoreHediff能正确过滤Hediff
- [ ] Restore能完全恢复快照时的状态

### 设计意图验证

- [ ] 快照时刻：状态被完整冻结 ✅
- [ ] 恢复时刻：使用快照值而非运行时值 ✅
- [ ] 忽略机制：应用层能指定哪些Hediff不参与快照 ✅

### 代码质量

- [ ] 遵循RimWorld代码规范
- [ ] 添加详细的XML注释
- [ ] 无编译警告
- [ ] 错误处理完整

### 向后兼容

- [ ] 旧存档能正常加载
- [ ] 旧存档中的数据能正确迁移
- [ ] 新旧版本能共存

---

## 第九部分：关键决策确认

### 决策1：值快照是必须的吗？

**分析**：
- 设计意图要求：快照时冻结、恢复时完全回滚
- 现实情况：对象引用会在运行时被修改
- 结论：✅ 值快照是**必须的**，不是可选的

### 决策2：应该在框架层还是应用层实现？

**分析**：
- 值快照的实现：框架层（HediffSnapshot）
- 忽略规则的定义：应用层（重写ShouldIgnoreHediff）
- 结论：✅ 框架层提供机制，应用层提供规则

### 决策3：需要处理向后兼容吗？

**分析**：
- 旧存档使用对象引用快照
- 新版本使用值快照
- 结论：✅ 需要处理版本兼容，提供迁移逻辑

---

## 第十部分：后续步骤

### 第一阶段（框架层改进）- 本周

1. ✅ 创建HediffSnapshot.cs，实现值快照结构
2. ✅ 修改CombatBodySnapshot.cs，改用值快照
3. ✅ 添加ShouldIgnoreHediff虚函数
4. ✅ 编写单元测试（9个）
5. ✅ 代码审查

### 第二阶段（应用层适配）- 本周末

6. ✅ 应用层重写ShouldIgnoreHediff（定义Trion腺体规则）
7. ✅ 游戏集成测试（验证快照/恢复流程）
8. ✅ 旧存档兼容性测试

### 第三阶段（文档和发布）- 下周

9. ✅ 更新框架文档
10. ✅ 更新应用开发指南
11. ✅ 版本发布

---

## 总结

### 问题的本质

现有实现通过保存**对象引用**而非**值**，导致快照时冻结的状态在运行时被修改，违反了设计意图。

### 解决方案的本质

框架层提供**值快照机制**（HediffSnapshot），确保快照时的状态被完整冻结，恢复时使用快照值而非运行时值。同时提供**忽略机制**（ShouldIgnoreHediff），让应用层能指定哪些系统级Hediff不参与快照。

### 关键改进

| 方面 | 旧设计 | 新设计 |
|------|--------|--------|
| **快照内容** | 对象引用 | 值快照（def, duration, severity, part） |
| **运行时修改影响** | ❌ 有（会改变快照值） | ✅ 无（快照值被冻结） |
| **恢复时使用的值** | 运行时修改后的值 | 快照时的值 |
| **忽略机制** | 无 | ✅ 提供ShouldIgnoreHediff虚函数 |
| **设计意图实现** | ❌ 违反 | ✅ 实现 |

---

## 版本历史

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|--------|--------|
| v1.0 | 完整的框架改进方案。包含：问题分析、HediffSnapshot实现、CombatBodySnapshot改进、单元测试、集成指南、兼容性处理 | 2026-01-14 | 知识提炼者 |

---

**知识提炼者**
*2026-01-14*

---

## 附录A：快速实现检查清单

### 今日（代码改动）

```
框架层：
  [ ] HediffSnapshot.cs - 创建新文件
  [ ] CombatBodySnapshot.cs - 修改为值快照
  [ ] CompTrion.cs - 重写ShouldIgnoreHediff（应用层）

单元测试：
  [ ] HediffSnapshotTests
  [ ] CombatBodySnapshotIgnoreTests
  [ ] CombatBodySnapshotRestoreTests
```

### 明日（验证测试）

```
游戏测试：
  [ ] 验证快照捕获正确
  [ ] 验证恢复后duration正确
  [ ] 验证恢复后severity正确
  [ ] 验证Trion腺体不被移除
  [ ] 验证战斗流程能完整走通
```

### 本周（文档和发布）

```
文档：
  [ ] 更新API文档
  [ ] 更新应用开发指南
  [ ] 添加示例代码

发布：
  [ ] 代码审查
  [ ] 测试兼容性
  [ ] 标记为稳定版
```

