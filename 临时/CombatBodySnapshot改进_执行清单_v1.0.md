---
摘要: CombatBodySnapshot框架改进的执行清单。包含具体的代码改动点、文件位置、测试验证方法，可直接用于指导开发。
版本号: v1.0
修改时间: 2026-01-14
关键词: 执行清单, Hediff值快照, 快速参考, 集成步骤
标签: [定稿]
---

# CombatBodySnapshot 改进执行清单 v1.0

## 🎯 核心问题（3秒速读）

**问题**：Capture保存Hediff对象引用 → 运行时被修改（duration等) → Restore使用修改后的值 ❌

**方案**：改用值快照 → Capture保存值 → 运行时修改不影响 → Restore使用快照值 ✅

---

## 📋 框架层改动清单（5处代码改动）

### 改动1：创建HediffSnapshot.cs

**文件**：新建 `ProjectTrion_Framework/Source/ProjectTrion/Core/HediffSnapshot.cs`

**核心代码**：
```csharp
public struct HediffSnapshot : IExposable
{
    public HediffDef def;
    public int duration;      // ← 冻结快照时的值
    public float severity;    // ← 冻结快照时的值
    public BodyPartRecord part;

    public static HediffSnapshot FromHediff(Hediff hediff)
    {
        return new HediffSnapshot
        {
            def = hediff.def,
            duration = hediff.duration,    // ← 保存当前值
            severity = hediff.Severity,    // ← 保存当前值
            part = hediff.Part
        };
    }

    public Hediff ToHediff(Pawn pawn)
    {
        var h = HediffMaker.MakeHediff(def, pawn, part);
        h.duration = duration;            // ← 恢复为快照值
        h.Severity = severity;            // ← 恢复为快照值
        return h;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref duration, "duration", 0);
        Scribe_Values.Look(ref severity, "severity", 0f);
        Scribe_References.Look(ref part, "part");
    }
}
```

### 改动2：修改CombatBodySnapshot - 成员变量

**文件**：`ProjectTrion_Framework/Source/ProjectTrion/Core/CombatBodySnapshot.cs`

**改动位置**：L20 (原来是 `public List<Hediff> hediffs`)

**改前**：
```csharp
public List<Hediff> hediffs = new List<Hediff>();  // ❌ 对象引用
```

**改后**：
```csharp
public List<HediffSnapshot> hediffSnapshots = new List<HediffSnapshot>();  // ✅ 值快照
```

### 改动3：修改CaptureFromPawn

**文件**：`CombatBodySnapshot.cs`

**改动位置**：L74-81 (CaptureFromPawn方法内)

**改前**：
```csharp
hediffs.Clear();
if (pawn.health?.hediffSet?.hediffs != null)
{
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        hediffs.Add(hediff);  // ❌ 保存对象引用
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
        if (!ShouldIgnoreHediff(hediff))  // ← 过滤忽略的Hediff
        {
            hediffSnapshots.Add(HediffSnapshot.FromHediff(hediff));  // ✅ 保存值
        }
    }
}
```

### 改动4：修改RestoreToPawn

**文件**：`CombatBodySnapshot.cs`

**改动位置**：L133-157 (RestoreToPawn方法内)

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
    foreach (var hediff in hediffs)  // ❌ 直接使用对象
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
    // 第一步：移除当前Hediff（除了忽略的）
    var toRemove = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs)
    {
        if (!ShouldIgnoreHediff(hediff))  // ← 使用新函数名
        {
            toRemove.Add(hediff);
        }
    }
    foreach (var hediff in toRemove)
    {
        pawn.health.RemoveHediff(hediff);
    }

    // 第二步：从值快照创建新对象（使用快照值）
    foreach (var snapshot in hediffSnapshots)
    {
        if (!ShouldIgnoreHediff(snapshot))
        {
            var hediff = snapshot.ToHediff(pawn);  // ✅ 从值创建新对象
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
        }
    }
}
```

### 改动5：添加ShouldIgnoreHediff虚函数

**文件**：`CombatBodySnapshot.cs`

**改动位置**：L221-226 (替换原来的IsEssentialHediff)

**改前**：
```csharp
private bool IsEssentialHediff(Hediff hediff)
{
    return false;  // ❌ 总是false，设计意图不清
}
```

**改后**：
```csharp
/// <summary>
/// 判断Hediff是否应该被忽略（不参与快照）。
/// 应用层应该重写此方法来指定具体规则。
///
/// 返回true：该Hediff不参与快照恢复（如Trion腺体）
/// 返回false：该Hediff参与快照恢复
/// </summary>
protected virtual bool ShouldIgnoreHediff(Hediff hediff)
{
    return false;  // ✅ 框架默认：不忽略任何Hediff
}

protected virtual bool ShouldIgnoreHediff(HediffSnapshot snapshot)
{
    return false;  // ✅ 框架默认：不忽略任何快照
}
```

### 改动6：修改ExposeData（序列化）

**文件**：`CombatBodySnapshot.cs`

**改动位置**：L231-238

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
    // 版本处理（用于旧存档兼容）
    int version = 1;
    Scribe_Values.Look(ref version, "snapshotVersion", 1);

    if (version == 0)
    {
        // 旧版本迁移逻辑
        List<Hediff> oldHediffs = new List<Hediff>();
        Scribe_Collections.Look(ref oldHediffs, "hediffs", LookMode.Deep);
        if (oldHediffs != null)
        {
            foreach (var h in oldHediffs)
                hediffSnapshots.Add(HediffSnapshot.FromHediff(h));
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

---

## 🔧 应用层改动（CompTrion中）

**文件**：`ProjectTrion_Framework/Source/ProjectTrion/Components/CompTrion.cs`

**改动**：重写ShouldIgnoreHediff虚函数

```csharp
public class CompTrion : ThingComp
{
    // ... 其他代码 ...

    /// <summary>
    /// 应用层实现：定义哪些Hediff应该被快照忽略。
    ///
    /// 返回true的Hediff将在快照时被完全跳过，
    /// 在恢复时也不会被移除。
    /// </summary>
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff == null)
            return false;

        // 规则1：忽略Trion腺体（系统级，与Trion生命周期绑定）
        if (hediff.def == TrionDefs.HediffTrionGland)
            return true;

        // 规则2：其他需要保护的框架级Hediff
        // if (hediff.def == SomeOtherFrameworkDef)
        //     return true;

        // 默认：不忽略，参与快照
        return false;
    }

    // 不需要重写ShouldIgnoreHediff(HediffSnapshot)
    // 除非有特殊的恢复时过滤逻辑
}
```

---

## ✅ 验证检查清单

### 编译验证

- [ ] HediffSnapshot.cs 能正常编译
- [ ] CombatBodySnapshot.cs 能正常编译
- [ ] CompTrion.cs 能正常编译
- [ ] 无编译警告
- [ ] 无CS1572警告（缺少XML注释）

### 单元测试验证

```csharp
// 测试1：值快照的冻结性
[Test]
public void ValueSnapshot_NotAffectedByRuntimeModification()
{
    var hediff = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
    hediff.duration = 180;

    var snapshot = HediffSnapshot.FromHediff(hediff);
    hediff.duration = 0;  // 运行时修改

    Assert.AreEqual(180, snapshot.duration);  // ✅ 快照不受影响
}

// 测试2：恢复时使用快照值
[Test]
public void Restore_UsesSnapshotValueNotRuntimeValue()
{
    var snapshot = new CombatBodySnapshot();
    var pawn = CreateTestPawn();

    var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn);
    injury.duration = 600;
    pawn.health.AddHediff(injury);

    snapshot.CaptureFromPawn(pawn);
    injury.duration = 0;  // 运行时递减到0

    pawn.health.RemoveHediff(injury);
    snapshot.RestoreToPawn(pawn);

    var restored = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Cut);
    Assert.AreEqual(600, restored.duration);  // ✅ 恢复为快照值，不是0
}

// 测试3：忽略机制
[Test]
public void IgnoredHediff_NotAffectedBySnapshot()
{
    var snapshot = new TestCombatBodySnapshot();  // 重写ShouldIgnoreHediff
    var pawn = CreateTestPawn();

    var trionGland = HediffMaker.MakeHediff(TrionDefs.HediffTrionGland, pawn);
    pawn.health.AddHediff(trionGland);

    snapshot.CaptureFromPawn(pawn);
    snapshot.RestoreToPawn(pawn);

    Assert.IsTrue(pawn.health.hediffSet.Contains(trionGland));  // ✅ 不被移除
}
```

### 游戏测试验证

1. **创建快照**
   - [ ] 新建小人
   - [ ] 装备触发器，生成战斗体
   - [ ] 验证快照被创建
   - [ ] 检查快照中的Hediff数量

2. **战斗过程**
   - [ ] 在战斗中，小人受伤
   - [ ] 添加新的Hediff（如Cut伤口）
   - [ ] 验证Hediff.duration在每tick递减

3. **恢复过程**
   - [ ] 战斗体销毁，触发恢复
   - [ ] 验证快照中的Hediff被恢复
   - [ ] **关键验证**：恢复后的Hediff.duration是否为快照时的值
     - 期望：是（使用快照值）
     - 错误：否（使用运行时值）
   - [ ] 验证Trion腺体存在且未被移除

4. **再次激活战斗体**
   - [ ] 立即重新激活战斗体（不等待Hediff自动过期）
   - [ ] 验证战斗体能正常生成
   - [ ] 验证Trion腺体仍然存在

### 兼容性验证

- [ ] 加载旧存档（使用对象引用快照）
- [ ] 验证ExposeData能正确迁移数据
- [ ] 验证迁移后快照功能正常

---

## 🚀 执行步骤（按顺序）

### 第1天（6小时）

**步骤1**：创建HediffSnapshot.cs
- 时间：1.5小时
- 检查：能编译、无警告

**步骤2**：修改CombatBodySnapshot（改动1-6）
- 时间：1.5小时
- 检查：能编译、无警告

**步骤3**：在CompTrion中重写ShouldIgnoreHediff
- 时间：0.5小时
- 检查：能编译、无警告

**步骤4**：编写单元测试
- 时间：1.5小时
- 检查：所有测试通过

**步骤5**：代码审查
- 时间：1小时

### 第2天（3小时）

**步骤6**：游戏集成测试
- 时间：2小时
- 关键：验证快照时冻结、恢复时使用快照值

**步骤7**：旧存档兼容性测试
- 时间：1小时

---

## 📊 改动影响分析

| 方面 | 改动前 | 改动后 | 影响 |
|------|--------|--------|------|
| **Hediff保存** | 对象引用 | 值快照 | 运行时修改不影响快照 |
| **duration保存** | ❌ 不保存 | ✅ 保存值 | 能正确恢复duration |
| **severity保存** | ❌ 不保存 | ✅ 保存值 | 能正确恢复severity |
| **忽略机制** | 无 | ✅ ShouldIgnoreHediff | 应用层能保护系统Hediff |
| **向后兼容** | N/A | ✅ 版本迁移 | 旧存档能加载 |

---

## ⚠️ 常见坑

### 坑1：忘记过滤忽略的Hediff

**错误代码**：
```csharp
// ❌ 没有过滤
foreach (var snapshot in hediffSnapshots)
{
    pawn.health.AddHediff(snapshot.ToHediff(pawn));
}
```

**正确代码**：
```csharp
// ✅ 过滤忽略的Hediff
foreach (var snapshot in hediffSnapshots)
{
    if (!ShouldIgnoreHediff(snapshot))
    {
        pawn.health.AddHediff(snapshot.ToHediff(pawn));
    }
}
```

### 坑2：忘记重写ShouldIgnoreHediff

**错误**：CompTrion没有重写ShouldIgnoreHediff，导致Trion腺体不被保护

**验证方法**：
```csharp
// 在CompTrion中检查
public override bool ShouldIgnoreHediff(Hediff hediff)
{
    // ✅ 必须重写，指定规则
    return hediff.def == TrionDefs.HediffTrionGland;
}
```

### 坑3：没有处理null情况

**正确代码**：
```csharp
public Hediff ToHediff(Pawn pawn)
{
    if (def == null)  // ✅ 检查null
        return null;
    if (pawn == null)
        return null;

    try  // ✅ 错误处理
    {
        var hediff = HediffMaker.MakeHediff(def, pawn, part);
        hediff.duration = duration;
        hediff.Severity = severity;
        return hediff;
    }
    catch (Exception ex)
    {
        Log.Error($"无法创建Hediff: {ex.Message}");
        return null;
    }
}
```

---

## 📍 文件清单

**需要创建的文件**：
- [ ] `ProjectTrion_Framework/Source/ProjectTrion/Core/HediffSnapshot.cs` （新建）

**需要修改的文件**：
- [ ] `ProjectTrion_Framework/Source/ProjectTrion/Core/CombatBodySnapshot.cs` （改动6处）
- [ ] `ProjectTrion_Framework/Source/ProjectTrion/Components/CompTrion.cs` （重写1个函数）

**参考文档**：
- [ ] `CombatBodySnapshot框架改进方案_v1.0.md` （完整设计文档）

---

## 🎓 关键原理回顾

### 为什么需要值快照？

```
对象引用方案：
  Capture时保存 → 指向Hediff对象 → 运行时tick递减 → Restore时用修改后的对象
  结果：duration从180变成0 ❌

值快照方案：
  Capture时保存 → def, duration=180, severity=0.5 → 运行时对象修改 → Restore时用duration=180创建新对象
  结果：duration恢复为180 ✅
```

### 为什么需要忽略机制？

```
Trion腺体是小人的"基础系统"，不应该因为战斗体的生成/销毁而被移除。
快照时：跳过Trion腺体，不保存
恢复时：跳过Trion腺体，不移除也不重建
结果：Trion腺体保持原状 ✅
```

---

## ✨ 验证成功标志

当你完成所有改动后，成功的标志是：

```
1. ✅ 编译通过，无警告
2. ✅ 单元测试全通过
3. ✅ 快照时冻结：Hediff.duration在快照后被修改，快照值不变
4. ✅ 恢复时正确：恢复后Hediff.duration等于快照值，不是运行时值
5. ✅ 忽略机制：Trion腺体在快照/恢复过程中保持不变
6. ✅ 兼容性：旧存档能正常加载和迁移
```

---

**知识提炼者**
*2026-01-14*

