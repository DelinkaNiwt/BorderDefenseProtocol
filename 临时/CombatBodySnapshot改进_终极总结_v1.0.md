---
摘要: 从对话开始到现在的完整总结。包含：问题是什么、为什么要改、改哪些文件、怎么具体改、如何验证。
版本号: v1.0
修改时间: 2026-01-16
关键词: 完整总结, 修改清单, 具体代码, 为什么, 怎么改
标签: [定稿]
---

# CombatBodySnapshot 改进 - 终极总结 v1.0

## 第一部分：问题是什么

### 你的设计

```
T1（快照）→ 生成战斗体（移除肉身非忽略hediff）→ 战斗体销毁（恢复肉身）→ T1
```

预期行为：
```
某hediff剩余10秒 → 生成战斗体 → 解除战斗体 → 某hediff仍剩余10秒
```

### 现有代码的问题

```csharp
// CombatBodySnapshot.cs 现有代码
public void CaptureFromPawn(Pawn pawn)
{
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        hediffs.Add(hediff);  // ← 保存对象A的引用
    }
}

public void RestoreToPawn(Pawn pawn)
{
    // ... 先移除 ...
    foreach (var hediff in hediffs)
    {
        pawn.health.AddHediff(hediff);  // ← 尝试添加已被删除的对象A❌
    }
}
```

**为什么有问题**：
1. Capture保存的是Hediff对象引用（对象A）
2. GenerateCombatBody时调用`RemoveHediff(对象A)`将其删除
3. Restore时尝试再次`AddHediff(对象A)`
4. **但对象A已从HediffSet删除，处于无效状态**
5. RimWorld无法重新使用无效对象，会失败或异常

**结果**：某hediff可能变成5秒或错误状态❌

---

## 第二部分：为什么这样改

### 核心论据

RimWorld的Hediff API设计约定：
```
一个Hediff对象的生命周期是：
  创建 → 初始化 → 活跃 → (tick运行) → 删除 → 无效
```

**关键约束**：一旦对象经历`RemoveHediff` → `无效`，就无法再次使用

**解决方案**：保存值而非对象，Restore时创建新对象

```csharp
// 改进方案
快照时保存：def, duration=10, severity=0.5, part=...（值，不是对象）
恢复时创建：HediffMaker.MakeHediff(def, ...) → 新对象B
          新对象B.duration = 10
          AddHediff(新对象B) ✅
```

### 这不是运行时修改问题

**很重要**：你的设计中，肉身hediff在战斗期间**根本不存在**（被移除了）

所以不是"快照值被运行时修改"的问题

**而是**"对象生命周期约束"的问题

---

## 第三部分：修改清单

### 需要修改的文件

1. **新建**：`ProjectTrion/Core/HediffSnapshot.cs`
2. **修改**：`ProjectTrion/Core/CombatBodySnapshot.cs`（6处改动）
3. **修改**：`ProjectTrion/Components/CompTrion.cs`（1处改动）

---

## 第四部分：具体怎么改

### 改动1：新建HediffSnapshot.cs

**文件位置**：
```
ProjectTrion_Framework/Source/ProjectTrion/Core/HediffSnapshot.cs
```

**完整代码**：
```csharp
using System;
using Verse;
using RimWorld;

namespace ProjectTrion.Core
{
    /// <summary>
    /// Hediff的值快照（保存def、duration、severity、part等值）。
    /// 用于战斗体销毁时从快照值创建新的Hediff对象。
    /// </summary>
    public struct HediffSnapshot : IExposable
    {
        public HediffDef def;
        public int duration;
        public float severity;
        public BodyPartRecord part;

        /// <summary>从Hediff创建值快照</summary>
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

        /// <summary>从值快照创建新Hediff对象</summary>
        public Hediff ToHediff(Pawn pawn)
        {
            if (def == null)
                return null;

            try
            {
                var hediff = HediffMaker.MakeHediff(def, pawn, part);
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

### 改动2-6：修改CombatBodySnapshot.cs

#### 改动2：改成员变量（L20）

**改前**：
```csharp
public List<Hediff> hediffs = new List<Hediff>();
```

**改后**：
```csharp
public List<HediffSnapshot> hediffSnapshots = new List<HediffSnapshot>();
```

**为什么**：保存值而非对象引用

---

#### 改动3：修改CaptureFromPawn（L74-81）

**改前**：
```csharp
hediffs.Clear();
if (pawn.health?.hediffSet?.hediffs != null)
{
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        hediffs.Add(hediff);
    }
}
```

**改后**：
```csharp
hediffSnapshots.Clear();
if (pawn.health?.hediffSet?.hediffs != null)
{
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        if (!ShouldIgnoreHediff(hediff))
        {
            hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff));
        }
    }
}
```

**为什么**：
- 保存值快照而非对象
- 过滤被忽略的Hediff（如Trion腺体）

---

#### 改动4：修改RestoreToPawn（L133-157）

**改前**：
```csharp
if (pawn.health != null && hediffs.Count > 0)
{
    var toRemove = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        if (!IsEssentialHediff(hediff))
        {
            toRemove.Add(hediff);
        }
    }

    foreach (var hediff in toRemove)
    {
        pawn.health.RemoveHediff(hediff);
    }

    foreach (var hediff in hediffs)
    {
        if (!IsEssentialHediff(hediff))
        {
            pawn.health.AddHediff(hediff);
        }
    }
}
```

**改后**：
```csharp
if (pawn.health != null && hediffSnapshots.Count > 0)
{
    // 第一步：移除当前所有非忽略hediff
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

    // 第二步：从值快照创建新对象并添加
    foreach (var snapshot in hediffSnapshots)
    {
        if (!ShouldIgnoreHediff(snapshot))
        {
            var hediff = snapshot.ToHediff(pawn);
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
        }
    }
}
```

**为什么**：
- 从值快照创建新对象（而不是重用旧对象）
- 新对象状态完全来自快照，保证duration等正确

---

#### 改动5：改虚函数（L221-226）

**改前**：
```csharp
private bool IsEssentialHediff(Hediff hediff)
{
    return false;
}
```

**改后**：
```csharp
/// <summary>
/// 判断Hediff是否应该被忽略（不参与快照恢复）。
/// 返回true：该Hediff不被保存、不被移除、不被恢复（如Trion腺体）
/// 返回false：该Hediff参与快照恢复
/// </summary>
protected virtual bool ShouldIgnoreHediff(Hediff hediff)
{
    return false;
}

protected virtual bool ShouldIgnoreHediff(HediffSnapshot snapshot)
{
    return false;
}
```

**为什么**：
- 改函数名使意图更清晰
- 改为虚函数让应用层可以重写
- 提供两个重载版本（Hediff和HediffSnapshot）

---

#### 改动6：修改ExposeData（L231-238）

**改前**：
```csharp
public void ExposeData()
{
    Scribe_Values.Look(ref snapshotTick, "snapshotTick");
    Scribe_Collections.Look(ref hediffs, "hediffs", LookMode.Deep);
    Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
    Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
    Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
}
```

**改后**：
```csharp
public void ExposeData()
{
    // 版本控制（处理旧存档迁移）
    int version = 1;
    Scribe_Values.Look(ref version, "snapshotVersion", 1);

    if (version == 0)
    {
        // 旧版本：从对象引用迁移到值快照
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

    Scribe_Values.Look(ref snapshotTick, "snapshotTick");
    Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
    Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
    Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
}
```

**为什么**：
- 处理旧存档兼容性
- 版本0是旧的对象引用方案
- 版本1是新的值快照方案

---

### 改动7：应用层实现（CompTrion.cs）

**文件位置**：
```
ProjectTrion_Framework/Source/ProjectTrion/Components/CompTrion.cs
```

**添加这个虚函数的重写**（在CompTrion类中）：

```csharp
/// <summary>
/// 应用层实现：指定哪些Hediff不参与快照恢复。
/// 返回true：该Hediff不被快照处理（如Trion腺体）
/// 返回false：该Hediff参与快照处理（默认）
/// </summary>
protected override bool ShouldIgnoreHediff(Hediff hediff)
{
    if (hediff == null)
        return false;

    // 保护Trion腺体：它是系统级Hediff，与Trion生命周期绑定
    if (hediff.def == TrionDefs.HediffTrionGland)
        return true;

    // 其他需要保护的系统级Hediff可以在这里添加

    return false;
}
```

**为什么**：
- Trion腺体是小人的"基础系统"
- 战斗体生成/销毁时不应该移除它
- 这样它会被ShouldIgnoreHediff过滤掉

---

## 第五部分：验证方法

### 编译验证

```
1. 创建HediffSnapshot.cs后应能编译
2. 修改CombatBodySnapshot.cs后应能编译
3. 修改CompTrion.cs后应能编译
4. 检查是否有编译警告
```

### 单元测试验证

```csharp
[Test]
public void FullCycle_RecoveredHediffHasCorrectDuration()
{
    var pawn = CreateTestPawn();
    var snapshot = new TestCombatBodySnapshot();

    // 创建伤口，duration=600（10秒）
    var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
    injury.duration = 600;
    pawn.health.AddHediff(injury);

    // 快照
    snapshot.CaptureFromPawn(pawn);
    Assert.AreEqual(600, snapshot.hediffSnapshots[0].duration);

    // 生成战斗体：移除肉身hediff
    pawn.health.RemoveHediff(injury);
    Assert.AreEqual(0, pawn.health.hediffSet.hediffs.Count);

    // 销毁战斗体：恢复
    snapshot.RestoreToPawn(pawn);

    // 验证：duration应该仍是600，不是其他值
    var restored = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut);
    Assert.AreEqual(600, restored.duration);  // ✅ 成功标志
}
```

### 游戏测试验证

```
1. 创建小人，装备触发器
2. 添加某个伤口（如剪切伤，剩余10秒）
3. 生成战斗体
4. 立即销毁战斗体
5. 检查伤口是否仍然剩余10秒
   ✅ 是 = 成功
   ❌ 否（变成5秒或不存在）= 失败
```

---

## 第六部分：修改汇总

### 修改统计

| 位置 | 改动 | 文件 |
|------|------|------|
| 改动1 | 新建HediffSnapshot.cs | 新建 |
| 改动2 | 改成员变量L20 | CombatBodySnapshot.cs |
| 改动3 | 改CaptureFromPawn L74-81 | CombatBodySnapshot.cs |
| 改动4 | 改RestoreToPawn L133-157 | CombatBodySnapshot.cs |
| 改动5 | 改虚函数L221-226 | CombatBodySnapshot.cs |
| 改动6 | 改ExposeData L231-238 | CombatBodySnapshot.cs |
| 改动7 | 新增ShouldIgnoreHediff重写 | CompTrion.cs |

**总计**：3个文件，7处改动，~200行新代码

### 工作量

- 框架层改动：2小时
- 应用层改动：0.5小时
- 单元测试：2小时
- 游戏测试：1.5小时
- **总计**：6小时

---

## 第七部分：为什么是这样的最简方案

### 为什么不用其他方案

**方案A：不快照，只冻结hediff tick**
- ❌ 不符合你的设计（你要移除）
- ❌ 无法处理战斗体的新hediff

**方案B：保存对象引用+深拷贝**
- ❌ 深拷贝Hediff很复杂（它有回调、内部状态）
- ❌ RimWorld可能不支持Hediff对象复制

**方案C：保存值+创建新对象**
- ✅ 简单清晰
- ✅ 符合RimWorld API约定
- ✅ 最少代码改动
- ✅ 你的设计逻辑完全适配
- **选择这个**

---

## 结论

### 三句话总结

1. **问题**：RemoveHediff后的对象无效，无法再AddHediff
2. **方案**：保存值（def、duration、severity、part），Restore时创建新对象
3. **改动**：框架层6处改动（新增HediffSnapshot、改Capture/Restore/ExposeData/虚函数），应用层1处改动（重写ShouldIgnoreHediff保护Trion腺体）

### 验证成功标志

```
某hediff剩余10秒
→ 生成战斗体（hediff被移除）
→ 解除战斗体（从快照值创建新hediff）
→ 某hediff仍剩余10秒  ✅
```

---

**知识提炼者**
*2026-01-16*
