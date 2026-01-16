---
摘要: 基于CombatBodySnapshot问题重新评估报告，提供具体的代码修复指导和实现步骤
版本号: v1.0
修改时间: 2026-01-14
关键词: 修复指导, 代码实现, IsEssentialHediff, 值快照, Hediff管理
标签: [待审]
---

# CombatBodySnapshot 修复指导 v1.0

## 概述

本文档基于《CombatBodySnapshot问题重新评估报告_v1.0.md》，提供**两阶段修复方案**的具体实现指导。

### 修复路线

```
阶段1（立即修复，1-2天）
  ↓
修复IsEssentialHediff逻辑
确保Trion腺体不被移除
  ↓
验证战斗流程
  ↓
阶段2（根本修复，1-2周）
  ↓
转换为值快照
解决Hediff对象生命周期问题
  ↓
全面测试和优化
```

---

## 阶段1：快速修复（IsEssentialHediff正确实现）

### 步骤1：在CompTrion中定义IsEssentialHediff

#### 当前代码位置
`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\Source\ProjectTrion\Components\CompTrion.cs`

#### 修改方案

**在CompTrion中添加**：

```csharp
/// <summary>
/// 检查Hediff是否"必需"（不应该被Snapshot恢复逻辑处理）
///
/// 返回true的情况：
/// 1. Trion腺体（战斗体生成时植入，不在快照中）
/// 2. 战斗过程中新增的Hediff（由其他逻辑负责清理）
///
/// 返回false的情况：
/// 1. 快照中原本就有的Hediff（应该被恢复）
/// </summary>
public bool IsEssentialHediff(Hediff hediff)
{
    if (hediff == null)
        return false;

    // 检查1：Trion腺体（必须保留）
    if (hediff.def == TrionDefs.HediffTrionGland)
    {
        return true;
    }

    // 检查2：快照中不存在的Hediff（战斗新增，不处理）
    if (_snapshot != null && !_snapshot.hediffs.Contains(hediff))
    {
        return true;
    }

    // 其他情况：快照中的Hediff（应该被处理）
    return false;
}
```

**关键点**：
- `hediff.def == TrionDefs.HediffTrionGland` 需要替换为实际的腺体Def
- `_snapshot.hediffs.Contains(hediff)` 用对象引用比较

#### 添加到CompTrion的位置

在CompTrion类的合适位置（如PostSpawnSetup之后）添加此方法。

### 步骤2：在CombatBodySnapshot中暴露IsEssentialHediff

#### 当前代码位置
`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\Source\ProjectTrion\Core\CombatBodySnapshot.cs`

#### 修改方案1：通过回调（推荐）

**L221-226替换为**：

```csharp
/// <summary>
/// 检查Hediff是否是必须保留的（不应被覆盖）。
/// 由应用层通过回调定义。
/// </summary>
public Func<Hediff, bool> EssentialHediffChecker { get; set; }

private bool IsEssentialHediff(Hediff hediff)
{
    // 如果设置了回调，使用回调；否则默认返回false
    return EssentialHediffChecker?.Invoke(hediff) ?? false;
}
```

**优点**：
- 框架与应用层解耦
- CompTrion可以灵活定义规则
- 易于测试

#### 修改方案2：让CombatBodySnapshot持有CompTrion引用（备选）

```csharp
private CompTrion _compTrion;

public CombatBodySnapshot(CompTrion compTrion = null)
{
    _compTrion = compTrion;
}

private bool IsEssentialHediff(Hediff hediff)
{
    return _compTrion?.IsEssentialHediff(hediff) ?? false;
}
```

### 步骤3：修改RestoreToPawn逻辑（关键）

#### 当前代码问题

```csharp
// L137-142: 移除Hediff
foreach (var hediff in pawn.health.hediffSet.hediffs)
{
    if (!IsEssentialHediff(hediff))
    {
        toRemove.Add(hediff);
    }
}

// L151-157: 重新添加Hediff
foreach (var hediff in hediffs)
{
    if (!IsEssentialHediff(hediff))
    {
        pawn.health.AddHediff(hediff);
    }
}
```

**问题**：IsEssentialHediff返回值的含义混淆，导致逻辑错误

#### 修复方案

```csharp
public void RestoreToPawn(Pawn pawn)
{
    if (pawn == null)
    {
        Log.Error("CompTrion: 尝试向空Pawn恢复快照数据");
        return;
    }

    if (pawn.health == null || hediffs.Count == 0)
        return;

    // ============ 步骤1：移除战斗过程中新增的Hediff ============
    var toRemove = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        // 跳过"必需"Hediff（如Trion腺体、或其他战斗新增的）
        if (IsEssentialHediff(hediff))
        {
            continue;
        }

        // 其他Hediff都应该被移除（包括快照Hediff，稍后重新添加）
        toRemove.Add(hediff);
    }

    // 执行移除
    foreach (var hediff in toRemove)
    {
        pawn.health.RemoveHediff(hediff);
    }

    // ============ 步骤2：恢复快照中的Hediff ============
    foreach (var hediff in hediffs)
    {
        // 跳过必需Hediff（不应该在快照中）
        if (IsEssentialHediff(hediff))
        {
            Log.Warning($"CompTrion: 快照中包含必需Hediff {hediff.def.defName}，跳过恢复");
            continue;
        }

        // 重新添加快照Hediff
        // ⚠️ 注意：这里仍然存在对象身份问题（见阶段2修复）
        pawn.health.AddHediff(hediff);
    }

    // ============ 步骤3：恢复装备和物品 ============
    RestoreApparel(pawn);
    RestoreInventory(pawn);
}
```

### 步骤4：在GenerateCombatBody中建立关联

#### 当前代码位置
`CompTrion.cs` 中的GenerateCombatBody方法（需要找到该方法）

#### 修改方案

```csharp
public void GenerateCombatBody()
{
    // ... 前置检查 ...

    // 步骤1：创建快照并关联IsEssentialHediff
    _snapshot = new CombatBodySnapshot();
    _snapshot.EssentialHediffChecker = this.IsEssentialHediff;  // ← 关键：建立回调

    // 步骤2：保存当前肉身状态（此时没有Trion腺体）
    _snapshot.CaptureFromPawn(parent);

    // 步骤3：植入Trion腺体（标志战斗体激活）
    Hediff trionGland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, parent);
    parent.health.AddHediff(trionGland);

    // 步骤4：标记战斗状态
    _isInCombat = true;

    // 步骤5：其他初始化（注册组件、计算占用等）
    // ... 现有逻辑 ...
}
```

**关键点**：
- `_snapshot.EssentialHediffChecker = this.IsEssentialHediff` 必须在CaptureFromPawn前设置
- Trion腺体的植入必须在快照后执行
- 两者的顺序决定了IsEssentialHediff的工作原理

### 步骤5：验证和测试

#### 验证清单

```
✅ 检查点1：IsEssentialHediff是否正确识别Trion腺体？
  测试：_snapshot中不包含Trion腺体
  验证：IsEssentialHediff(trionGland) == true

✅ 检查点2：是否正确识别战斗新增Hediff？
  测试：战斗过程中添加伤口
  验证：IsEssentialHediff(warFundiff) == true

✅ 检查点3：是否正确识别快照Hediff？
  测试：快照中有旧伤口
  验证：IsEssentialHediff(oldWound) == false

✅ 检查点4：Restore后Trion腺体是否保留？
  测试：战斗结束→RestoreToPawn
  验证：pawn.health.hediffSet包含Trion腺体

✅ 检查点5：Restore后快照Hediff是否恢复？
  测试：战斗结束→RestoreToPawn
  验证：pawn.health.hediffSet包含快照中的旧伤口

✅ 检查点6：Restore后战斗伤害是否清除？
  测试：战斗结束→RestoreToPawn
  验证：pawn.health.hediffSet不包含战斗伤口
```

#### 单元测试方案

```csharp
[TestClass]
public class CombatBodySnapshotTests
{
    [TestMethod]
    public void TestIsEssentialHediff_TrionGland()
    {
        // 测试Trion腺体识别
        var pawn = CreateTestPawn();
        var comp = pawn.GetComp<CompTrion>();
        var gland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, pawn);

        Assert.IsTrue(comp.IsEssentialHediff(gland));
    }

    [TestMethod]
    public void TestRestore_TrionGlandPreserved()
    {
        // 测试Restore后Trion腺体是否保留
        var pawn = CreateTestPawn();
        var comp = pawn.GetComp<CompTrion>();

        // 生成战斗体
        comp.GenerateCombatBody();
        Assert.IsTrue(pawn.health.hediffSet.HasHediff(TrionDefs.HediffTrionGland));

        // 模拟战斗伤害
        AddTestHediff(pawn, HediffDefOf.Cut);  // 添加伤口

        // 恢复快照
        comp.EndCombat();

        // 验证
        Assert.IsTrue(pawn.health.hediffSet.HasHediff(TrionDefs.HediffTrionGland));
        Assert.IsFalse(pawn.health.hediffSet.HasHediff(HediffDefOf.Cut));
    }

    // ... 其他测试 ...
}
```

---

## 阶段2：根本修复（值快照实现）

### 为什么需要值快照？

**问题回顾**：
```csharp
// Capture时，Hediff处于状态S1
Hediff h = new Hediff { severity = 5 };
snapshot.hediffs.Add(h);

// 战斗过程中，Hediff被修改
h.severity = 8;  // 状态变成S2

// Restore时，h仍然是S2，不是S1
pawn.health.AddHediff(h);  // ✗ 错误：恢复了修改后的状态
```

### 步骤1：定义HediffSnapshot值类型

#### 实现代码

```csharp
using System;
using Verse;

namespace ProjectTrion.Core
{
    /// <summary>
    /// Hediff的值快照。
    /// 保存Hediff的完整状态，允许在Restore时重建对象。
    /// </summary>
    [System.Serializable]
    public struct HediffSnapshot : IExposable
    {
        /// <summary>
        /// Hediff的定义（不变）
        /// </summary>
        public HediffDef def;

        /// <summary>
        /// 严重程度
        /// </summary>
        public float severity;

        /// <summary>
        /// 受影响的身体部位（可能为null）
        /// </summary>
        public BodyPartRecord part;

        /// <summary>
        /// 是否为永久效果
        /// </summary>
        public bool permanent;

        /// <summary>
        /// 来源标签（如"ProjectTrion_Snapshot"）
        /// </summary>
        public string source;

        /// <summary>
        /// 从Hediff对象创建快照
        /// </summary>
        public static HediffSnapshot FromHediff(Hediff hediff)
        {
            if (hediff == null)
                return default;

            return new HediffSnapshot
            {
                def = hediff.def,
                severity = hediff.severity,
                part = hediff.Part,
                permanent = hediff.permanent,
                source = "ProjectTrion_Snapshot"
            };
        }

        /// <summary>
        /// 从快照重建Hediff对象
        /// </summary>
        public Hediff ToHediff(Pawn target)
        {
            if (def == null)
            {
                Log.Error("HediffSnapshot.ToHediff: def is null");
                return null;
            }

            // 创建新Hediff对象
            Hediff hediff = HediffMaker.MakeHediff(def, target, part);

            if (hediff != null)
            {
                hediff.severity = severity;
                hediff.permanent = permanent;
            }

            return hediff;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref severity, "severity");
            Scribe_References.Look(ref part, "part");
            Scribe_Values.Look(ref permanent, "permanent");
            Scribe_Values.Look(ref source, "source");
        }
    }
}
```

### 步骤2：修改CombatBodySnapshot使用值快照

#### 替换hediffs列表

```csharp
public class CombatBodySnapshot : IExposable
{
    // 改用值快照，而不是对象引用
    public List<HediffSnapshot> hediffSnapshots = new List<HediffSnapshot>();

    // 保留原有接口（用于后向兼容）
    [Obsolete("使用hediffSnapshots代替")]
    public List<Hediff> hediffs = new List<Hediff>();

    // ... 其他字段保持不变 ...
}
```

#### 修改CaptureFromPawn

```csharp
public void CaptureFromPawn(Pawn pawn)
{
    snapshotTick = Find.TickManager.TicksGame;

    // 捕获健康数据 ← 使用值快照
    hediffSnapshots.Clear();
    if (pawn.health?.hediffSet?.hediffs != null)
    {
        foreach (var hediff in pawn.health.hediffSet.hediffs)
        {
            // 跳过必需Hediff（应用层提供的规则）
            if (EssentialHediffChecker?.Invoke(hediff) ?? false)
            {
                continue;
            }

            // 保存值快照
            hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff));
        }
    }

    // 保留原有列表用于后向兼容（可选）
    hediffs.Clear();
    foreach (var snapshot in hediffSnapshots)
    {
        hediffs.Add(snapshot.ToHediff(pawn));
    }

    // ... 其他捕获逻辑保持不变 ...
}
```

#### 修改RestoreToPawn

```csharp
public void RestoreToPawn(Pawn pawn)
{
    if (pawn == null || pawn.health == null)
        return;

    // ============ 步骤1：移除战斗新增Hediff ============
    var toRemove = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        // 必需Hediff（如Trion腺体）保留
        if (EssentialHediffChecker?.Invoke(hediff) ?? false)
        {
            continue;
        }

        // 其他都移除
        toRemove.Add(hediff);
    }

    foreach (var hediff in toRemove)
    {
        pawn.health.RemoveHediff(hediff);
    }

    // ============ 步骤2：从快照值重建Hediff ============
    foreach (var snapshot in hediffSnapshots)
    {
        // 从快照值（而不是旧对象）重建新Hediff
        Hediff newHediff = snapshot.ToHediff(pawn);
        if (newHediff != null)
        {
            pawn.health.AddHediff(newHediff);
        }
    }

    // ============ 步骤3：恢复装备和物品 ============
    RestoreApparel(pawn);
    RestoreInventory(pawn);
}
```

### 步骤3：修改序列化逻辑

#### ExposeData

```csharp
public void ExposeData()
{
    Scribe_Values.Look(ref snapshotTick, "snapshotTick");

    // 序列化值快照（推荐）
    Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);

    // 保留原有逻辑用于后向兼容
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
        // 如果旧存档只有hediffs，转换为hediffSnapshots
        if (hediffSnapshots.NullOrEmpty() && !hediffs.NullOrEmpty())
        {
            hediffSnapshots = new List<HediffSnapshot>();
            foreach (var h in hediffs)
            {
                hediffSnapshots.Add(HediffSnapshot.FromHediff(h));
            }
        }
    }

    // ... 其他序列化逻辑 ...
    Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
    Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
    Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
}
```

### 步骤4：全面测试

#### 值快照测试

```csharp
[TestClass]
public class HediffSnapshotTests
{
    [TestMethod]
    public void TestFromHediff_PreservesSeverity()
    {
        var pawn = CreateTestPawn();
        var hediff = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
        hediff.severity = 0.5f;

        var snapshot = HediffSnapshot.FromHediff(hediff);

        Assert.AreEqual(0.5f, snapshot.severity);
    }

    [TestMethod]
    public void TestToHediff_RecreateCorrectly()
    {
        var pawn = CreateTestPawn();
        var snapshot = new HediffSnapshot
        {
            def = HediffDefOf.Cut,
            severity = 0.75f,
            part = pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Head).First()
        };

        var hediff = snapshot.ToHediff(pawn);

        Assert.IsNotNull(hediff);
        Assert.AreEqual(HediffDefOf.Cut, hediff.def);
        Assert.AreEqual(0.75f, hediff.severity);
    }

    [TestMethod]
    public void TestRestore_ObjectIdentityFixed()
    {
        // 验证Restore不再使用旧对象引用
        var pawn = CreateTestPawn();
        var comp = pawn.GetComp<CompTrion>();

        // 生成战斗体
        comp.GenerateCombatBody();

        // 获取快照中的Hediff对象
        var snapshotCount = comp.Snapshot.hediffSnapshots.Count;

        // 修改Pawn的Hediff
        var h = pawn.health.hediffSet.hediffs[0];
        var originalSeverity = h.severity;
        h.severity = 99f;  // 修改状态

        // Restore
        comp.EndCombat();

        // 验证：恢复的Hediff应该使用快照值，不是修改后的值
        var restoredHediff = pawn.health.hediffSet.GetFirstHediff(h.def);
        Assert.AreEqual(originalSeverity, restoredHediff.severity);
    }
}
```

---

## 关键改动总结

### 阶段1改动清单

| 文件 | 类 | 方法 | 改动 | 风险 |
|------|------|------|------|------|
| CompTrion.cs | CompTrion | 新增IsEssentialHediff | 新增public方法 | 低 |
| CompTrion.cs | CompTrion | GenerateCombatBody | 添加回调关联 | 低 |
| CombatBodySnapshot.cs | CombatBodySnapshot | IsEssentialHediff | 改为回调 | 低 |
| CombatBodySnapshot.cs | CombatBodySnapshot | RestoreToPawn | 修改逻辑 | **中** |

**风险说明**：
- IsEssentialHediff逻辑改动可能影响现有存档
- 需要添加后向兼容处理

### 阶段2改动清单

| 文件 | 类 | 改动 | 影响范围 | 工作量 |
|------|------|------|---------|--------|
| HediffSnapshot.cs | HediffSnapshot | 新增struct | 新文件 | 低 |
| CombatBodySnapshot.cs | CombatBodySnapshot | 替换hediffs列表 | 核心逻辑 | **中** |
| CombatBodySnapshot.cs | CombatBodySnapshot | CaptureFromPawn | 改用值快照 | 低 |
| CombatBodySnapshot.cs | CombatBodySnapshot | RestoreToPawn | 改用值快照 | 低 |
| CombatBodySnapshot.cs | CombatBodySnapshot | ExposeData | 序列化逻辑 | **中** |

**工作量估计**：
- 阶段1：4-6小时（包括测试）
- 阶段2：12-16小时（包括完整测试和后向兼容）
- 总计：16-22小时

---

## 集成检查清单

### 代码审查

- [ ] IsEssentialHediff是否与CompTrion的战斗体生命周期正确同步？
- [ ] EssentialHediffChecker回调是否在正确时机设置？
- [ ] RestoreToPawn中的移除和添加顺序是否正确？
- [ ] 是否处理了null hediff的情况？
- [ ] HediffSnapshot.ToHediff是否正确重建所有必要字段？

### 性能考虑

- [ ] 值快照是否会增加内存占用？（预期增加不大）
- [ ] 快照和恢复的性能是否可接受？（应该无显著差异）
- [ ] 序列化性能是否满足要求？（需要实测）

### 后向兼容性

- [ ] 旧存档加载时是否能正确转换hediffs到hediffSnapshots？
- [ ] 是否需要迁移脚本？
- [ ] 是否需要版本检查？

### 文档更新

- [ ] 是否需要更新CombatBodySnapshot的注释？
- [ ] 是否需要在设计文档中记录这些改动？
- [ ] 是否需要为应用层提供实现指导？

---

## 应用层实现指导

### 如何定义自己的IsEssentialHediff规则？

#### 方案1：在CompTrion中override

```csharp
public class CompTrion_Application : CompTrion
{
    public override bool IsEssentialHediff(Hediff hediff)
    {
        // 基类实现
        if (base.IsEssentialHediff(hediff))
            return true;

        // 应用层扩展规则
        // 例如：某些特殊debuff应该被保留
        if (hediff.def == MyCustomDefs.SpecialDebuff)
            return true;

        return false;
    }
}
```

#### 方案2：通过HediffDef扩展

```csharp
// 定义标记扩展
public class TrionEssentialHediffExt : DefModExtension
{
    public bool isEssential = false;
}

// 在IsEssentialHediff中使用
public bool IsEssentialHediff(Hediff hediff)
{
    var ext = hediff.def.GetModExtension<TrionEssentialHediffExt>();
    if (ext?.isEssential ?? false)
        return true;

    // ... 其他检查 ...
}

// 在XML中定义
<Hediff_Injury Name="Cut">
    <defName>Cut</defName>
    <!-- ... -->
    <modExtensions>
        <li Class="ProjectTrion.TrionEssentialHediffExt">
            <isEssential>false</isEssential>
        </li>
    </modExtensions>
</Hediff_Injury>
```

---

## 总结

### 立即行动（今日）

1. 在CompTrion中实现IsEssentialHediff方法
2. 在CombatBodySnapshot中添加EssentialHediffChecker回调
3. 修改RestoreToPawn的逻辑
4. 在GenerateCombatBody中建立回调关联
5. 编写单元测试验证

### 短期跟进（本周）

1. 在真实游戏中测试战斗流程
2. 验证Trion腺体是否被正确保留
3. 检查旧存档兼容性
4. 收集潜在问题反馈

### 中期规划（本月）

1. 开始阶段2值快照实现
2. 完全消除对象身份问题
3. 优化序列化性能
4. 更新所有文档

---

## 版本历史

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v1.0 | 完整修复指导文档。包含两阶段修复方案的具体代码、步骤、验证清单、应用层指导 | 2026-01-14 | 代码工程师 |

---

**代码工程师**
*2026-01-14*
