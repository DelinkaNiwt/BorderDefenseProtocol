---
摘要: 基于完整的战斗体生命周期设计，CombatBodySnapshot的最终改进方案。记录肉身非忽略hediff的值，战斗期间清空肉身，战斗后从值快照恢复。
版本号: v1.0
修改时间: 2026-01-16
关键词: 值快照, Hediff生命周期, 快照恢复, 战斗体生成, 对象重建
标签: [定稿]
---

# CombatBodySnapshot 值快照改进 - 最终方案 v1.0

## 执行摘要

### 用户的完整设计意图

```
T1（快照时刻）：记录肉身非忽略hediff的值
  ├─ 非忽略hediff：保存值快照（def, duration, severity, part）
  └─ 忽略hediff：不保存，不动

战斗体生成：
  ├─ 移除肉身所有非忽略hediff → 肉身干净
  ├─ 战斗体的新hediff开始运行
  └─ 肉身被冻结（hediff不存在 → 不会被tick修改）

战斗体期间：
  ├─ 肉身：空的健康页面（忽略hediff除外）
  └─ 战斗体：全新的健康页面（T2）

战斗体销毁：
  ├─ 移除战斗体所有hediff（T2完全消失）
  └─ 从值快照恢复肉身（非忽略hediff还原为T1状态）

结果：
  某hediff剩余10秒 → 生成战斗体 → 解除战斗体 → 某hediff仍剩余10秒
```

### 为什么需要值快照

**不是因为**：hediff在战斗期间被修改（实际上不存在）

**而是因为**：被RemoveHediff的hediff对象处于无效状态，无法再次使用
- 原对象已从HediffSet中删除
- 可能被RimWorld标记为无效或已清理
- 需要创建新对象来恢复

### 改进方案

1. ✅ **HediffSnapshot结构**：保存hediff的关键值（def, duration, severity, part）
2. ✅ **修改Capture**：保存值快照而非对象引用
3. ✅ **修改Restore**：从值快照创建新对象并添加
4. ✅ **提供忽略机制**：应用层指定哪些hediff被跳过
5. ✅ **向后兼容**：处理旧存档迁移

---

## 第一部分：为什么现有实现有问题

### 现有代码的问题

```csharp
// 现有实现
public void CaptureFromPawn(Pawn pawn)
{
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        hediffs.Add(hediff);  // ← 保存对象引用
    }
}

public void RestoreToPawn(Pawn pawn)
{
    var toRemove = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        toRemove.Add(hediff);
    }
    foreach (var hediff in toRemove)
    {
        pawn.health.RemoveHediff(hediff);  // ← 删除对象
    }

    foreach (var hediff in hediffs)
    {
        pawn.health.AddHediff(hediff);  // ← 试图重新添加已删除的对象❌
    }
}
```

### 对象生命周期问题

```
快照时：
  原Hediff对象（对象A）
  hediffs.Add(hediff)  → 保存引用

Restore时：
  RemoveHediff(hediff)  → 对象A被从HediffSet中删除
                         → 对象A可能被标记为无效
                         → 内部状态可能被清理

  AddHediff(对象A)  → 尝试添加一个无效对象❌
                      → 可能失败
                      → 可能产生未定义行为
```

### 正确的做法

创建新对象，使用保存的值初始化：

```csharp
// 改进实现
var hediff = snapshot.ToHediff(pawn);  // ← 创建新对象，使用保存的值
pawn.health.AddHediff(hediff);  // ← 添加新对象✅
```

---

## 第二部分：HediffSnapshot实现

### 完整代码

```csharp
using System;
using Verse;
using RimWorld;

namespace ProjectTrion.Core
{
    /// <summary>
    /// Hediff的值快照。
    ///
    /// 设计理由：
    /// - Hediff对象一旦从HediffSet中被RemoveHediff，就处于无效状态
    /// - 无法再次使用同一对象的AddHediff
    /// - 需要保存hediff的关键值（def, duration, severity, part）
    /// - Restore时用这些值创建新的hediff对象
    ///
    /// 快照保存的值：
    /// - def：Hediff定义（决定Hediff的类型）
    /// - duration：剩余时间（如果Hediff有持续期限）
    /// - severity：严重程度（控制Hediff的表现强度）
    /// - part：身体部位（如果Hediff与特定部位关联）
    /// </summary>
    public struct HediffSnapshot : IExposable
    {
        /// <summary>
        /// Hediff的定义（ID）。
        /// 决定了这是什么类型的hediff（伤口、疾病等）。
        /// </summary>
        public HediffDef def;

        /// <summary>
        /// Hediff的剩余duration（ticks）。
        /// 对于有持续时间的Hediff，这决定了何时自动过期。
        /// 例如：伤口恢复需要600 ticks（10秒）。
        /// </summary>
        public int duration;

        /// <summary>
        /// Hediff的severity（严重程度，0-1）。
        /// 控制Hediff的表现强度和影响。
        /// </summary>
        public float severity;

        /// <summary>
        /// Hediff所在的身体部位。
        /// 某些Hediff与特定部位关联（如某条腿上的伤口）。
        /// </summary>
        public BodyPartRecord part;

        /// <summary>
        /// 从Hediff对象创建值快照。
        ///
        /// 这是快照时刻发生的操作：
        /// 将Hediff的所有关键值保存为结构体字段，
        /// 这样即使原对象之后被删除，我们也能恢复。
        /// </summary>
        public static HediffSnapshot FromHediff(Hediff hediff)
        {
            if (hediff == null)
                return default;

            return new HediffSnapshot
            {
                def = hediff.def,
                duration = hediff.duration,
                severity = hediff.Severity,
                part = hediff.Part
            };
        }

        /// <summary>
        /// 从值快照创建新的Hediff对象。
        ///
        /// 这是恢复时刻发生的操作：
        /// 使用保存的值创建全新的Hediff对象。
        /// 这个新对象完全独立于原对象，不受RemoveHediff的影响。
        ///
        /// 返回的新对象状态与快照时完全相同：
        /// - def相同（同类型Hediff）
        /// - duration相同（剩余时间相同）
        /// - severity相同（严重程度相同）
        /// - part相同（同位置）
        /// </summary>
        public Hediff ToHediff(Pawn pawn)
        {
            if (def == null)
                return null;

            try
            {
                // 使用HediffMaker创建新的Hediff对象
                // 这是RimWorld的标准方式，确保对象被正确初始化
                var hediff = HediffMaker.MakeHediff(def, pawn, part);

                // 使用保存的值初始化新对象
                hediff.duration = duration;
                hediff.Severity = severity;

                return hediff;
            }
            catch (Exception ex)
            {
                Log.Error($"CompTrion: 无法从快照创建Hediff {def?.defName}: {ex.Message}");
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

---

## 第三部分：CombatBodySnapshot改进

### 关键改动总结

| 位置 | 改前 | 改后 | 原因 |
|------|------|------|------|
| 成员变量 | `List<Hediff> hediffs` | `List<HediffSnapshot> hediffSnapshots` | 保存值而非对象 |
| CaptureFromPawn | `hediffs.Add(hediff)` | `hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff))` | 保存值快照 |
| RestoreToPawn | `AddHediff(hediff)` | `AddHediff(snapshot.ToHediff(pawn))` | 创建新对象 |
| 虚函数 | `IsEssentialHediff(Hediff)` | `ShouldIgnoreHediff(Hediff)` | 改名，更清晰 |

### 完整改进代码

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
    /// 设计逻辑：
    /// T1（快照时刻）
    ///   ├─ 非忽略hediff：保存值快照
    ///   └─ 忽略hediff：不保存，继续存在
    ///
    /// 战斗体生成
    ///   ├─ 移除肉身所有非忽略hediff
    ///   └─ 肉身被冻结（空的健康页面）
    ///
    /// 战斗体销毁
    ///   ├─ 从值快照恢复肉身非忽略hediff
    ///   └─ 忽略hediff继续原样存在
    ///
    /// 用于战斗体摧毁时恢复肉身到快照时的状态。
    /// </summary>
    public class CombatBodySnapshot : IExposable
    {
        /// <summary>
        /// 快照保存的健康数据（值快照，非对象引用）。
        /// 包含所有快照时刻的非忽略hediff的值。
        /// </summary>
        public List<HediffSnapshot> hediffSnapshots = new List<HediffSnapshot>();

        /// <summary>
        /// 快照保存的穿戴服装列表。
        /// </summary>
        public List<Apparel> apparels = new List<Apparel>();

        /// <summary>
        /// 快照保存的装备（武器）列表。
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
        /// 从目标Pawn捕获当前物理状态（T1快照）。
        ///
        /// 捕获内容：
        /// - 所有非忽略hediff的值快照
        /// - 所有Apparel（服装）
        /// - 所有Equipment（装备）
        /// - 所有Inventory物品
        ///
        /// 不捕获：
        /// - 被忽略的hediff（如Trion腺体）
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
                    // 跳过被忽略的hediff（应用层指定的，如Trion腺体）
                    if (!ShouldIgnoreHediff(hediff))
                    {
                        // 保存值快照而非对象引用
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
        /// 恢复流程：
        /// 1. 移除肉身当前所有非忽略hediff（战斗体销毁时已经是空的）
        /// 2. 从值快照创建新的hediff对象并添加
        /// 3. 恢复装备和物品
        ///
        /// 不恢复：
        /// - 被忽略的hediff（保持原样）
        /// - 心理状态（保留战斗期间的变化）
        /// </summary>
        public void RestoreToPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CompTrion: 尝试向空Pawn恢复快照数据");
                return;
            }

            // 恢复健康数据
            if (pawn.health != null)
            {
                // 第一步：移除肉身当前所有非忽略hediff
                // （通常战斗体销毁时肉身已经是空的，但以防万一）
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

                // 第二步：从值快照恢复hediff
                // 关键点：创建新对象，使用保存的值初始化
                if (hediffSnapshots.Count > 0)
                {
                    foreach (var snapshot in hediffSnapshots)
                    {
                        // 应用层也可以在恢复时过滤
                        if (!ShouldIgnoreHediff(snapshot))
                        {
                            // 从值快照创建新的Hediff对象
                            var hediff = snapshot.ToHediff(pawn);
                            if (hediff != null)
                            {
                                pawn.health.AddHediff(hediff);
                            }
                        }
                    }
                }
            }

            // 恢复装备和物品
            RestoreApparel(pawn);
            RestoreInventory(pawn);

            // 不恢复心理状态
            // - 技能等级和经验值保留
            // - 心理状态和心情保留（战斗期间的心理变化保留）
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
        /// 判断Hediff是否应该被忽略（不参与快照）。
        ///
        /// 返回true的Hediff：
        /// - Capture时不被保存
        /// - Restore时不被移除、不被恢复
        /// - 战斗体生成/销毁时保持原样
        ///
        /// 典型用途：保护系统级hediff（如Trion腺体）
        ///
        /// 应用层应该重写此方法来指定具体规则。
        /// </summary>
        protected virtual bool ShouldIgnoreHediff(Hediff hediff)
        {
            // 框架默认：不忽略任何Hediff（所有都参与快照）
            // 应用层应该重写来指定忽略规则
            return false;
        }

        /// <summary>
        /// 判断HediffSnapshot是否应该在Restore时被忽略。
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
            // 版本控制（用于处理旧存档）
            int version = 1;
            Scribe_Values.Look(ref version, "snapshotVersion", 1);

            if (version == 0)
            {
                // 旧版本迁移：从对象引用转换为值快照
                List<Hediff> oldHediffs = new List<Hediff>();
                Scribe_Collections.Look(ref oldHediffs, "hediffs", LookMode.Deep);
                if (oldHediffs != null)
                {
                    foreach (var h in oldHediffs)
                    {
                        if (h != null)
                        {
                            hediffSnapshots.Add(HediffSnapshot.FromHediff(h));
                        }
                    }
                }
            }
            else
            {
                // 新版本：值快照
                Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);
            }

            // 其他快照数据
            Scribe_Values.Look(ref snapshotTick, "snapshotTick");
            Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
            Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
            Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
        }
    }
}
```

---

## 第四部分：应用层实现（CompTrion）

```csharp
public class CompTrion : ThingComp
{
    private CombatBodySnapshot _snapshot;

    /// <summary>
    /// 应用层实现：定义哪些Hediff应该被快照忽略。
    ///
    /// 返回true：该Hediff不参与快照（保护它，使其不被移除）
    /// 返回false：该Hediff参与快照（会被移除、保存、恢复）
    /// </summary>
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff == null)
            return false;

        // 规则1：Trion腺体是系统级hediff，与Trion生命周期绑定
        // 战斗体生成/销毁时应该保持原样
        if (hediff.def == TrionDefs.HediffTrionGland)
            return true;

        // 规则2：其他需要保护的系统级Hediff
        // （如果有的话在这里添加）
        // if (hediff.def == OtherSystemDef)
        //     return true;

        // 默认：其他Hediff都参与快照
        return false;
    }

    /// <summary>
    /// 战斗体生成前：创建肉身快照（T1）
    /// </summary>
    public void GenerateCombatBody()
    {
        // ... 其他战斗体生成逻辑 ...

        // 创建肉身快照
        // 这会调用CaptureFromPawn，其中会调用ShouldIgnoreHediff过滤
        _snapshot = new CombatBodySnapshot();
        _snapshot.CaptureFromPawn(this.Pawn);

        // ... 继续战斗体生成 ...
    }

    /// <summary>
    /// 战斗体销毁时：恢复肉身到快照状态（T1）
    ///
    /// 流程：
    /// 1. Restore会从值快照创建新的Hediff对象
    /// 2. 新对象状态与快照时完全相同
    /// 3. 忽略hediff保持原样
    /// </summary>
    public void DestroyCombatBody()
    {
        if (_snapshot != null)
        {
            // 恢复肉身到快照状态
            // 这会调用RestoreToPawn，其中会：
            // 1. 移除肉身当前所有非忽略hediff
            // 2. 从值快照创建新对象并添加
            _snapshot.RestoreToPawn(this.Pawn);
        }

        // ... 其他销毁逻辑 ...
    }
}
```

---

## 第五部分：验证测试

### 核心验证

```csharp
[TestFixture]
public class CombatBodySnapshotRestoreTests
{
    [Test]
    public void Restore_RecreatesHediffWithSameValues()
    {
        // 场景：快照一个有duration的Hediff（伤口），
        // 然后恢复，验证duration与快照时相同

        // Arrange
        var pawn = CreateTestPawn();
        var snapshot = new TestCombatBodySnapshot();

        // 创建一个伤口Hediff
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        injury.duration = 600;  // 10秒
        injury.Severity = 0.5f;
        pawn.health.AddHediff(injury);

        // Act - 快照
        snapshot.CaptureFromPawn(pawn);

        // 验证快照保存了正确的值
        Assert.AreEqual(1, snapshot.hediffSnapshots.Count);
        Assert.AreEqual(600, snapshot.hediffSnapshots[0].duration);
        Assert.AreEqual(0.5f, snapshot.hediffSnapshots[0].severity);

        // Act - 模拟战斗体生成时移除
        pawn.health.RemoveHediff(injury);
        Assert.AreEqual(0, pawn.health.hediffSet.hediffs.Count);

        // Act - 恢复
        snapshot.RestoreToPawn(pawn);

        // Assert - 验证恢复后的Hediff
        Assert.AreEqual(1, pawn.health.hediffSet.hediffs.Count);
        var restoredInjury = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut);
        Assert.IsNotNull(restoredInjury);
        Assert.AreEqual(600, restoredInjury.duration);      // ✅ 恢复为快照值
        Assert.AreEqual(0.5f, restoredInjury.Severity);     // ✅ 恢复为快照值
    }

    [Test]
    public void IgnoredHediff_NotAffectedBySnapshot()
    {
        // 场景：快照时有两个Hediff（一个被忽略），
        // 验证被忽略的不被保存、不被移除

        var pawn = CreateTestPawn();
        var snapshot = new TestCombatBodySnapshot();

        // 添加被忽略的Hediff（Trion腺体）
        var trionGland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, pawn);
        pawn.health.AddHediff(trionGland);

        // 添加非忽略的Hediff（伤口）
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        pawn.health.AddHediff(injury);

        // Act - 快照
        snapshot.CaptureFromPawn(pawn);

        // 验证只有伤口被快照，Trion腺体没有
        Assert.AreEqual(1, snapshot.hediffSnapshots.Count);
        Assert.AreEqual(HediffDefOf.Cut, snapshot.hediffSnapshots[0].def);

        // Act - 移除所有非忽略Hediff
        pawn.health.RemoveHediff(injury);

        // 验证Trion腺体仍然存在
        Assert.AreEqual(1, pawn.health.hediffSet.hediffs.Count);
        Assert.IsTrue(pawn.health.hediffSet.hediffs.Contains(trionGland));

        // Act - 恢复
        snapshot.RestoreToPawn(pawn);

        // Assert - 验证两个Hediff都存在且正确
        Assert.AreEqual(2, pawn.health.hediffSet.hediffs.Count);
        Assert.IsTrue(pawn.health.hediffSet.hediffs.Contains(trionGland));  // ✅ 未被动过
        Assert.IsNotNull(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut));  // ✅ 恢复了
    }

    [Test]
    public void FullCombatBodyLifecycle()
    {
        // 场景：完整的战斗体生命周期
        // T1 → 快照 → 生成战斗体 → 销毁战斗体 → T1

        var pawn = CreateTestPawn();
        var snapshot = new TestCombatBodySnapshot();

        // T1：肉身状态
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        injury.duration = 600;  // 10秒
        pawn.health.AddHediff(injury);

        // 快照
        snapshot.CaptureFromPawn(pawn);
        Assert.AreEqual(600, snapshot.hediffSnapshots[0].duration);

        // 生成战斗体：移除肉身非忽略Hediff
        pawn.health.RemoveHediff(injury);
        Assert.AreEqual(0, pawn.health.hediffSet.hediffs.Count);

        // 战斗体期间：肉身为空（没有新的Hediff产生）

        // 销毁战斗体：恢复肉身到T1
        snapshot.RestoreToPawn(pawn);

        // 验证：肉身完全回到T1状态
        Assert.AreEqual(1, pawn.health.hediffSet.hediffs.Count);
        var restored = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut);
        Assert.AreEqual(600, restored.duration);  // ✅ 10秒，未变过

        Log.Message("✅ 某hediff剩余10秒 → 生成战斗体 → 解除战斗体 → 某hediff仍剩余10秒");
    }
}

public class TestCombatBodySnapshot : CombatBodySnapshot
{
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff?.def == TrionDefs.HediffTrionGland)
            return true;
        return false;
    }
}
```

---

## 第六部分：关键要点总结

### 为什么用值快照而不是对象引用

| 方案 | 优点 | 缺点 | 结论 |
|------|------|------|------|
| **对象引用** | 省内存，避免复制 | 对象删除后无效，无法重新使用 | ❌ 不可行 |
| **值快照** | 删除后仍能恢复，创建新对象 | 多占用内存（但小） | ✅ 必须 |

### 为什么需要ShouldIgnoreHediff虚函数

| 问题 | 解决 |
|------|------|
| 应用层需要保护系统Hediff | 框架提供虚函数，应用层重写 |
| Trion腺体不应被快照处理 | Capture时跳过，Restore时也跳过 |
| 框架层不知道应用层规则 | 框架只提供机制，规则由应用层定义 |

### 设计意图验证

```
预期行为：
  某hediff剩余10秒
  → 生成战斗体（hediff被移除，保存快照值）
  → 战斗体期间（肉身为空）
  → 解除战斗体（从快照值恢复）
  → 某hediff仍剩余10秒  ✅ 完全正确

实现方式：
  1. Capture时保存hediff的值（def, duration, severity, part）
  2. GenerateCombatBody时移除hediff
  3. DestroyCombatBody时从保存的值创建新hediff对象
```

---

## 第七部分：执行清单

### 框架层改动（5处）

- [ ] **新建文件**：`HediffSnapshot.cs` （完整实现上面的代码）
- [ ] **修改CombatBodySnapshot**：
  - [ ] 改变量：`hediffs` → `hediffSnapshots`
  - [ ] 改CaptureFromPawn：保存值快照
  - [ ] 改RestoreToPawn：从值创建新对象
  - [ ] 改虚函数：`IsEssentialHediff` → `ShouldIgnoreHediff`
  - [ ] 改ExposeData：版本控制

### 应用层改动（1处）

- [ ] **CompTrion中重写**：`ShouldIgnoreHediff(Hediff)` 返回Trion腺体时true

### 测试（3个）

- [ ] 编写单元测试
- [ ] 运行游戏测试
- [ ] 验证恢复后duration正确

---

## 结论

你的设计（战斗体生成时移除肉身Hediff，销毁时恢复）**完全正确和清晰**。

值快照的作用不是保护"快照值被运行时修改"（因为Hediff被移除后根本不会被修改），而是处理**Hediff对象的生命周期问题**——被RemoveHediff的对象无法再次AddHediff，所以必须保存值并创建新对象。

这个改进方案保证了：
- ✅ 快照时冻结状态
- ✅ 战斗体生成时肉身干净
- ✅ 战斗体销毁时完全恢复到T1
- ✅ Trion腺体不被动
- ✅ 符合RimWorld的Hediff生命周期约束

---

## 版本历史

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|--------|--------|
| v1.0 | 完整的值快照改进方案。基于用户的完整设计意图（战斗体生成时移除肉身、销毁时恢复），说明值快照的真实作用（处理对象生命周期问题）。包含HediffSnapshot实现、CombatBodySnapshot改进、应用层指导、单元测试 | 2026-01-16 | 知识提炼者 |

---

**知识提炼者**
*2026-01-16*
