# 用原版HP替代影子HP的设计方案

**日期**: 2026-03-05
**状态**: 设计草案
**作者**: Claude Opus 4.6

## 核心思路

不使用ShadowHPTracker，直接利用原版`HediffSet.GetPartHealth()`查询部位血量，但通过多个Harmony Patch拦截原版Injury的所有副作用。

## 需要的Patch清单

### 1. 修改伤害拦截点（Prefix → Postfix）

```csharp
[HarmonyPatch(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury",
    new Type[] { typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), typeof(DamageWorker.DamageResult) })]
public static class Patch_FinalizeAndAddInjury_Postfix
{
    static void Postfix(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo)
    {
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene == null || !gene.IsCombatBodyActive) return;

        // 原版已添加Injury，记录用于后续清理
        gene.TrackCombatInjury(injury);

        // 检查部位HP（用原版API）
        float partHP = pawn.health.hediffSet.GetPartHealth(injury.Part);

        // Trion消耗
        TrionCostHandler.Handle(pawn, injury.Severity);

        // 部位破坏检测
        if (partHP <= 0)
        {
            gene.PartDestruction.Handle(pawn, injury.Part);
            // 关键部位破坏 → 破裂
            if (IsCriticalPart(injury.Part))
            {
                CollapseHandler.Handle(pawn, false, true, $"关键部位破坏 ({injury.Part.def.defName})");
            }
        }
    }
}
```

### 2. 禁用出血

```csharp
[HarmonyPatch(typeof(Hediff_Injury), "BleedRate", MethodType.Getter)]
public static class Patch_Injury_BleedRate
{
    static void Postfix(Hediff_Injury __instance, ref float __result)
    {
        var pawn = __instance.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene != null && gene.IsCombatBodyActive && gene.IsCombatInjury(__instance))
        {
            __result = 0f;  // 战斗体伤口不出血
        }
    }
}
```

### 3. 禁用疼痛

```csharp
[HarmonyPatch(typeof(Hediff_Injury), "PainFactor", MethodType.Getter)]
public static class Patch_Injury_PainFactor
{
    static void Postfix(Hediff_Injury __instance, ref float __result)
    {
        var pawn = __instance.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene != null && gene.IsCombatBodyActive && gene.IsCombatInjury(__instance))
        {
            __result = 0f;  // 战斗体伤口无疼痛
        }
    }
}
```

### 4. 禁用死亡判定

```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDead")]
public static class Patch_ShouldBeDead
{
    static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
    {
        if (!__result) return;  // 原版判定不死亡，直接返回

        var pawn = __instance.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene != null && gene.IsCombatBodyActive)
        {
            __result = false;  // 战斗体激活时不会死亡（由破裂机制处理）
        }
    }
}
```

### 5. 禁用倒地判定

```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
public static class Patch_ShouldBeDowned
{
    static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
    {
        if (!__result) return;

        var pawn = __instance.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene != null && gene.IsCombatBodyActive)
        {
            // 选项A：完全不倒地
            __result = false;

            // 选项B：自定义倒地逻辑（如Trion<阈值时倒地）
            // if (gene.CompTrion.Cur > 10f) __result = false;
        }
    }
}
```

### 6. 禁用感染

```csharp
[HarmonyPatch(typeof(HediffComp_Immunizable), "CompPostTick")]
public static class Patch_Immunizable_Tick
{
    static bool Prefix(HediffComp_Immunizable __instance)
    {
        var hediff = __instance.parent;
        var pawn = hediff?.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();

        if (gene != null && gene.IsCombatBodyActive && gene.IsCombatInjury(hediff))
        {
            return false;  // 跳过感染逻辑
        }
        return true;
    }
}
```

### 7. 禁用永久伤疤

```csharp
[HarmonyPatch(typeof(HediffComp_GetsPermanent), "CompPostTick")]
public static class Patch_GetsPermanent_Tick
{
    static bool Prefix(HediffComp_GetsPermanent __instance)
    {
        var hediff = __instance.parent;
        var pawn = hediff?.pawn;
        var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();

        if (gene != null && gene.IsCombatBodyActive && gene.IsCombatInjury(hediff))
        {
            return false;  // 跳过永久伤疤逻辑
        }
        return true;
    }
}
```

### 8. Gene中追踪战斗体Injury

```csharp
// Gene_TrionGland.cs
public class Gene_TrionGland : Gene
{
    // 战斗体期间的Injury列表（用于解除时清理）
    private HashSet<Hediff_Injury> combatInjuries = new HashSet<Hediff_Injury>();

    public void TrackCombatInjury(Hediff_Injury injury)
    {
        if (IsCombatBodyActive)
        {
            combatInjuries.Add(injury);
        }
    }

    public bool IsCombatInjury(Hediff hediff)
    {
        return hediff is Hediff_Injury injury && combatInjuries.Contains(injury);
    }

    public void DeactivateCombatBody(bool isEmergency = false)
    {
        // ... 其他逻辑 ...

        // 移除所有战斗体Injury
        var toRemove = combatInjuries.ToList();
        foreach (var injury in toRemove)
        {
            if (injury != null && !injury.Destroyed)
            {
                pawn.health.RemoveHediff(injury);
            }
        }
        combatInjuries.Clear();

        // ... 其他逻辑 ...
    }

    public override void ExposeData()
    {
        base.ExposeData();
        // 注意：Hediff引用需要特殊序列化
        Scribe_Collections.Look(ref combatInjuries, "combatInjuries", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            combatInjuries ??= new HashSet<Hediff_Injury>();
            combatInjuries.RemoveWhere(h => h == null || h.Destroyed);
        }
    }
}
```

### 9. Trion流失机制

由于不再使用Hediff_CombatWound的Comp，需要另外实现Trion流失：

```csharp
// Gene_TrionGland.cs
public override void Tick()
{
    base.Tick();

    if (!IsCombatBodyActive) return;

    // 每60 tick（1秒）计算一次Trion流失
    if (pawn.IsHashIntervalTick(60))
    {
        float totalDrain = 0f;

        // 遍历所有战斗体Injury，累加流失
        foreach (var injury in combatInjuries)
        {
            if (injury != null && !injury.Destroyed)
            {
                // 流失公式：severity * 系数 / 天 * 60tick
                float drainPerSecond = (injury.Severity * 0.5f) / GenDate.TicksPerDay * 60f;
                totalDrain += drainPerSecond;
            }
        }

        if (totalDrain > 0f)
        {
            CompTrion.Consume(totalDrain);
        }
    }
}
```

## 需要删除的文件

- `ShadowHPTracker.cs`
- `ShadowHPHandler.cs`
- `Hediff_CombatWound.cs`
- `HediffComp_CombatWound.cs`
- `WoundAdapter.cs`（部分逻辑可能保留）

## 需要修改的文件

- `CombatBodyDamageHandler.cs`：改用`GetPartHealth`查询
- `PartDestructionHandler.cs`：部位破坏检测改用原版HP
- `Gene_TrionGland.cs`：添加Injury追踪和Trion流失逻辑
- `Patch_DamageWorker_FinalizeAndAddInjury.cs`：Prefix改Postfix

## 优缺点对比

### 优点
- 不需要维护独立的影子HP数据结构
- 利用原版部位HP计算（自动处理护甲、部位层级等）
- 代码总行数可能略少

### 缺点
- **Patch数量激增**：从1个增加到7-8个
- **性能开销**：属性getter被频繁调用（每帧多次），每次都要检查战斗体状态
- **兼容性风险**：每个Patch都是潜在的模组冲突点
- **遗漏风险**：原版可能还有其他副作用路径（特殊伤害类型、DLC特性）
- **UI定制困难**：原版Injury标签不支持"受伤次数"、"Trion流失"等信息
- **维护困难**：逻辑分散在多个Patch中，难以追踪完整流程

## 风险评估

1. **高风险**：BleedRate/PainFactor的Postfix会被**每帧多次调用**（UI渲染、能力计算等），性能影响未知
2. **中风险**：可能遗漏某些副作用路径（如DLC特殊机制、模组扩展的伤害类型）
3. **中风险**：与其他健康系统模组的兼容性（如医疗扩展、义肢模组）

## 建议

**不推荐此方案。** 当前影子HP方案虽然引入了额外数据结构，但：
- 拦截点单一，逻辑集中
- 完全隔离原版系统，兼容性更好
- 性能开销可控（只在受伤时触发）
- UI完全自定义

如果坚持使用原版HP，建议先实现一个最小原型验证性能影响。

---

**历史记录**:
- 2026-03-05: 初始设计（Claude Opus 4.6）
