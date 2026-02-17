---
标题：Harmony补丁系统与模式
版本号: v1.0
更新日期: 2026-02-16
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未完成][未锁定]
摘要: HarmonyX补丁系统的完整技术参考。涵盖Prefix/Postfix/Transpiler三种补丁类型、特性标注、优先级系统、特殊参数约定、实战模式、冲突处理和选择指南。基于HarmonyX源码和社区模组实例研究。
---

# Harmony补丁系统与模式

## 1. HarmonyX概述

HarmonyX是RimWorld内置的运行时方法补丁框架（`HarmonyLib`命名空间），允许模组在不修改原版DLL的情况下，在任意C#方法的执行前/后/中注入自定义逻辑。

RimWorld 1.6使用的是HarmonyX（Harmony 2.x的社区分支），与原版Harmony 2 API完全兼容。

## 2. 初始化与注册

### 方式1：Mod构造函数（最常用）

```csharp
public class MyMod : Mod
{
    public static Harmony harmony;

    public MyMod(ModContentPack content) : base(content)
    {
        harmony = new Harmony("com.myname.mymod");
        harmony.PatchAll();  // 扫描当前程序集所有[HarmonyPatch]类
    }
}
```

**时机**：Mod构造函数在DLL加载后、Def加载前执行。此时DefDatabase尚未构建。

### 方式2：[StaticConstructorOnStartup]

```csharp
[StaticConstructorOnStartup]
public static class MyPatches
{
    static MyPatches()
    {
        var harmony = new Harmony("com.myname.mymod");
        harmony.PatchAll();
    }
}
```

**时机**：所有Def加载完毕后执行。适合需要访问DefDatabase的补丁逻辑。

### 方式3：手动注册单个补丁类

```csharp
harmony.CreateClassProcessor(typeof(MySpecificPatch)).Patch();
```

适合条件性补丁（如仅在特定DLC激活时注册）。

### Harmony ID

- 使用反向域名格式：`"com.author.modname"`
- 用于标识补丁来源
- `harmony.UnpatchAll("com.author.modname")` 可卸载该ID的所有补丁

## 3. 补丁类型详解

### 3.1 Prefix（前置补丁）

在原方法执行**前**运行。

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
public static class MyPatch
{
    // 返回bool：true=继续执行原方法，false=跳过原方法
    public static bool Prefix(TargetClass __instance, ref float __result)
    {
        if (ShouldSkip(__instance))
        {
            __result = 0f;  // 设置返回值
            return false;   // 跳过原方法
        }
        return true;  // 正常执行
    }
}
```

**Prefix能力**：
- 读取/修改原方法参数（使用`ref`）
- 跳过原方法执行（返回false）
- 设置返回值（通过`ref __result`）
- 保存状态传递给Postfix（通过`out __state`）

**Prefix返回false的影响**：
- 跳过原方法
- 跳过所有后续Prefix（优先级更低的）
- **不跳过Postfix**——所有Postfix仍会执行

### 3.2 Postfix（后置补丁）

在原方法执行**后**运行。

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
public static class MyPatch
{
    // 无返回值，通过ref修改结果
    public static void Postfix(TargetClass __instance, ref float __result)
    {
        __result *= 1.5f;  // 修改返回值
    }
}
```

**Postfix能力**：
- 读取原方法返回值
- 修改返回值（通过`ref __result`）
- 读取原方法参数
- 读取Prefix传递的__state
- 执行后处理逻辑

**Postfix始终执行**——即使Prefix返回false跳过了原方法。

### 3.3 Transpiler（IL转译补丁）

在JIT编译时修改方法的IL指令流。

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
public static class MyPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator ilg)  // 可选：IL生成器，用于创建标签/局部变量
    {
        var codes = instructions.ToList();

        for (int i = 0; i < codes.Count; i++)
        {
            // 查找目标指令模式
            if (codes[i].opcode == OpCodes.Call
                && codes[i].OperandIs(targetMethod))
            {
                // 替换为自定义方法调用
                yield return new CodeInstruction(OpCodes.Call, myMethod);
                continue;
            }
            yield return codes[i];
        }
    }
}
```

**Transpiler能力**：
- 修改方法内部任意位置的逻辑
- 插入/删除/替换IL指令
- 修改分支跳转
- 添加局部变量

**Transpiler风险**：
- 依赖IL指令的精确位置和模式
- 游戏版本更新后最容易失效
- 与其他Transpiler补丁容易冲突
- 调试极其困难

## 4. 特性标注方式

### 类级标注（推荐）

```csharp
// 基本形式
[HarmonyPatch(typeof(Pawn), "Kill")]
public static class Pawn_Kill_Patch { ... }

// 指定方法重载（通过参数类型）
[HarmonyPatch(typeof(DamageWorker), "Apply",
    new Type[] { typeof(DamageInfo), typeof(Thing) })]
public static class DamageWorker_Apply_Patch { ... }

// 属性getter
[HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonist), MethodType.Getter)]
public static class Pawn_IsColonist_Patch { ... }

// 属性setter
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Faction), MethodType.Setter)]
public static class Pawn_Faction_Patch { ... }

// 构造函数
[HarmonyPatch(typeof(Pawn), MethodType.Constructor)]
public static class Pawn_Ctor_Patch { ... }
```

### 方法级标注

```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
public static class Pawn_Kill_Patch
{
    [HarmonyPrefix]
    public static bool MyPrefix(...) { ... }

    [HarmonyPostfix]
    public static void MyPostfix(...) { ... }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> MyTranspiler(...) { ... }
}
```

### 多目标补丁

```csharp
[HarmonyPatch]
public static class MultiTargetPatch
{
    // 动态指定目标方法列表
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ClassA), "Method1");
        yield return AccessTools.Method(typeof(ClassB), "Method2");
    }

    public static void Postfix(MethodBase __originalMethod)
    {
        Log.Message($"Called: {__originalMethod.Name}");
    }
}
```

## 5. 特殊参数约定

### 双下划线参数

| 参数 | 类型 | 可用于 | 说明 |
|------|------|--------|------|
| `__instance` | 目标类 | Prefix/Postfix | 实例方法的this（静态方法无此参数） |
| `__result` | 返回值类型 | Prefix(`ref`)/Postfix(`ref`) | 方法返回值 |
| `__state` | 任意类型 | Prefix(`out`)→Postfix | 跨Prefix/Postfix的状态传递 |
| `__originalMethod` | MethodBase | Prefix/Postfix | 被补丁的原方法信息 |
| `___fieldName` | 字段类型 | Prefix/Postfix | 访问私有字段（三下划线+字段名） |

### 原方法参数注入

直接使用与原方法参数**同名**的参数即可注入：

```csharp
// 原方法：public void TakeDamage(DamageInfo dinfo, float amount)
[HarmonyPrefix]
public static void Prefix(DamageInfo dinfo, ref float amount)
{
    amount *= 0.5f;  // 修改参数（需要ref）
}
```

### __state状态传递

```csharp
[HarmonyPrefix]
public static void Prefix(Pawn __instance, out Map __state)
{
    __state = __instance.MapHeld;  // 保存状态
}

[HarmonyPostfix]
public static void Postfix(Pawn __instance, Map __state)
{
    // 使用Prefix保存的状态
    if (__state != null) { ... }
}
```

## 6. Priority优先级系统

```csharp
public static class Priority
{
    public const int Last = 0;
    public const int VeryLow = 100;
    public const int Low = 200;
    public const int LowerThanNormal = 300;
    public const int Normal = 400;        // 默认
    public const int HigherThanNormal = 500;
    public const int High = 600;
    public const int VeryHigh = 700;
    public const int First = 800;
}
```

**执行顺序**：
- Prefix：Priority高→低（First最先执行）
- Postfix：Priority低→高（Last最先执行，First最后执行）
- Transpiler：按注册顺序链式执行

**使用方式**：

```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
public static class MyPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(...) { ... }
}
```

**Before/After排序**（更精确的控制）：

```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
[HarmonyBefore("com.othermod.patches")]  // 在指定模组之前执行
[HarmonyAfter("com.anothermod.patches")] // 在指定模组之后执行
public static class MyPatch { ... }
```

## 7. AccessTools工具类

HarmonyLib提供的反射工具类，简化私有成员访问：

```csharp
// 获取方法
MethodInfo method = AccessTools.Method(typeof(Pawn), "Kill");
MethodInfo method = AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo) });

// 获取字段
FieldInfo field = AccessTools.Field(typeof(Thing), "factionInt");

// 获取属性
PropertyInfo prop = AccessTools.Property(typeof(Pawn), "IsColonist");

// 获取内部类型
Type innerType = AccessTools.Inner(typeof(OuterClass), "InnerClass");

// 获取所有方法
var methods = AccessTools.GetDeclaredMethods(typeof(Pawn));
```

## 8. 实战模式集锦

### 模式A：条件拦截

```csharp
// 阻止特定Pawn倒地
[HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
public static class PreventDowned
{
    public static bool Prefix(Pawn ___pawn)
    {
        if (___pawn.health.hediffSet.HasHediff(MyDefOf.Invincible))
            return false;
        return true;
    }
}
```

### 模式B：属性修改

```csharp
// 修改移速计算结果
[HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
public static class SpeedBoost
{
    public static void Postfix(ref float __result, StatRequest req, StatDef ___stat)
    {
        if (___stat == StatDefOf.MoveSpeed && req.Thing is Pawn pawn
            && pawn.health.hediffSet.HasHediff(MyDefOf.SpeedBuff))
        {
            __result *= 1.5f;
        }
    }
}
```

### 模式C：死亡前后状态保存

```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
public static class DeathExplosion
{
    public static void Prefix(Pawn __instance, out Map __state)
    {
        __state = __instance.MapHeld;
    }

    public static void Postfix(Pawn __instance, Map __state)
    {
        if (__instance.Dead && __state != null)
        {
            var comp = __instance.GetComp<CompDeathExplosion>();
            comp?.Explode(__state);
        }
    }
}
```

### 模式D：临时修改+恢复

```csharp
// 临时修改派系以影响判断逻辑
[HarmonyPatch(typeof(HealthAIUtility), "CanRescueNow")]
public static class CanRescueSleeve
{
    public static void Prefix(Pawn rescuer, Pawn patient, out Faction __state)
    {
        __state = patient.factionInt;
        if (IsEmptySleeve(patient))
            patient.factionInt = rescuer.factionInt;  // 临时修改
    }

    public static void Postfix(Pawn patient, Faction __state)
    {
        patient.factionInt = __state;  // 恢复原值
    }
}
```

### 模式E：Transpiler插入方法调用

```csharp
// 在投射物命中判定中插入闪避检查
[HarmonyPatch(typeof(Projectile), "ImpactSomething")]
public static class DodgePatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        var codes = instructions.ToList();
        var dodgeCheck = AccessTools.Method(typeof(DodgePatch), "CheckDodge");
        var label = ilg.DefineLabel();

        for (int i = 0; i < codes.Count; i++)
        {
            if (IsTargetPattern(codes, i))
            {
                codes[i + 1].labels.Add(label);
                yield return codes[i];
                // 插入闪避检查
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(OpCodes.Call, dodgeCheck);
                yield return new CodeInstruction(OpCodes.Brfalse_S, label);
                yield return new CodeInstruction(OpCodes.Ret);
                continue;
            }
            yield return codes[i];
        }
    }

    public static bool CheckDodge(Pawn pawn)
    {
        if (pawn == null) return false;
        float chance = pawn.GetStatValue(MyDefOf.DodgeChance);
        return Rand.Chance(chance);
    }
}
```

## 9. 冲突处理与最佳实践

### 减少冲突的原则

1. **优先Postfix**：Postfix不会阻止其他补丁执行，冲突风险最低
2. **避免Prefix返回false**：这会跳过原方法和其他模组的Prefix
3. **Transpiler作为最后手段**：多个Transpiler修改同一方法极易冲突
4. **使用Priority和Before/After**：明确声明执行顺序
5. **检查null**：其他模组可能修改了你期望的数据

### 调试技巧

```csharp
// 列出某方法的所有补丁
var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(Pawn), "Kill"));
if (patches != null)
{
    foreach (var p in patches.Prefixes)
        Log.Message($"Prefix: {p.owner} priority={p.priority}");
    foreach (var p in patches.Postfixes)
        Log.Message($"Postfix: {p.owner} priority={p.priority}");
    foreach (var p in patches.Transpilers)
        Log.Message($"Transpiler: {p.owner}");
}
```

### 条件补丁（DLC/模组检测）

```csharp
public MyMod(ModContentPack content) : base(content)
{
    var harmony = new Harmony("com.myname.mymod");

    // 基础补丁
    harmony.PatchAll();

    // 条件补丁：仅Biotech激活时
    if (ModsConfig.BiotechActive)
    {
        harmony.CreateClassProcessor(typeof(BiotechSpecificPatch)).Patch();
    }

    // 条件补丁：仅特定模组存在时
    if (ModLister.GetActiveModWithIdentifier("othermod.id") != null)
    {
        harmony.CreateClassProcessor(typeof(CompatPatch)).Patch();
    }
}
```

## 10. Harmony vs 数据驱动方案选择

| 需求 | 数据驱动方案 | Harmony方案 | 推荐 |
|------|------------|------------|------|
| 添加新功能 | ThingComp/HediffComp | — | 数据驱动 |
| 添加新数据 | DefModExtension | — | 数据驱动 |
| 修改Def属性 | XPath Patch | — | 数据驱动 |
| 修改属性计算 | StatWorker子类 | Postfix | 数据驱动 |
| 修改AI行为 | ThinkNode/JobGiver | — | 数据驱动 |
| 添加渲染节点 | CompRenderNodes | — | 数据驱动 |
| 修改返回值 | — | Postfix | Harmony |
| 拦截方法执行 | — | Prefix | Harmony |
| 修改方法内部逻辑 | — | Transpiler | Harmony（最后手段） |

**核心原则**：Harmony是"最后手段"。每个Harmony补丁都是对原版代码结构的硬依赖，游戏更新时可能失效。优先使用RimWorld提供的数据驱动扩展点。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-16 | 创建文档。基于HarmonyX源码（Priority/HarmonyPatch/PatchProcessor）和社区模组实例（VEF/AlteredCarbon/GlitterworldDestroyer/NCL等）研究 | Claude Opus 4.6 |
