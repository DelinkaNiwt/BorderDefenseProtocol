# Harmony 工程师实战开发手册

## 元信息

- **摘要**：为代码工程师和模组开发者准备的Harmony实战指南，包含5分钟速成、按场景组织的API速查、30+实战代码模板（可直接复制）、常见错误排查、调试技巧
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：代码模板，API速查，实战开发，错误排查，调试技巧
- **标签**：[待审]，工程师手册，实战速查
- **适用对象**：模组代码工程师、Harmony 开发者、一线补丁编写者

---

## 导言：3 分钟从零到能写补丕

### 三个基本概念

**概念1：补丕是"拦截器"**
```csharp
// 游戏调用：pawn.TakeDamage(10);

// 你的补丕可以：
// 1️⃣ 在前缀中修改参数
Prefix: damage = 5;  // 改成 5
pawn.TakeDamage(5);   // 游戏执行修改后的

// 2️⃣ 在后缀中修改返回值
pawn.TakeDamage(10);
Postfix: result *= 2;  // 结果翻倍

// 3️⃣ 在前缀中完全阻止
Prefix: return false;  // 游戏不执行原方法，用你的返回值
```

**概念2：补丕靠属性找目标**
```csharp
[HarmonyPatch(typeof(Pawn), "TakeDamage")]  // 找到这个方法
[HarmonyPrefix]  // 这是个前缀补丕
public static bool Prefix() { }  // 这是补丕方法
```

**概念3：补丕的特殊参数**
```csharp
public static bool Prefix(
    Pawn __instance,        // 方法所在对象（非静态方法）
    int damage,             // 原方法的参数
    ref int __result        // 返回值（前缀中可以赋值阻止原方法）
)
{
    // 修改 __instance 的字段
    __instance.health.hediffs.Clear();

    // 修改 damage（原参数无法 ref）
    // → 要修改参数需要用 Transpiler

    // 设置 __result 并返回 false 可以完全替换方法
    __result = 999;
    return false;  // 阻止原方法，返回 999

    // 返回 true 表示继续执行原方法
    return true;
}
```

---

## 1. 5 分钟速成：最常用的 3 种补丕模式

### 模式1：改返回值（最常见，70% 的补丕）

```csharp
// 目标：改变某个方法的返回值
[HarmonyPatch(typeof(Pawn), "get_BodySize")]  // 注意：属性用 get_ 前缀
internal static class Pawn_BodySize_Patch
{
    [HarmonyPostfix]  // 后缀：原方法执行后修改结果
    public static void Postfix(Pawn __instance, ref float __result)
    {
        __result *= 1.2f;  // 增加 20%
    }
}
```

**复制这个模板到你的代码中，改三个地方**：
1. `typeof(Pawn)` → 你要 patch 的类
2. `"get_BodySize"` → 你要 patch 的方法名
3. `__result *= 1.2f;` → 你的修改逻辑

---

### 模式2：条件拦截（第二常见，20% 的补丕）

```csharp
// 目标：在某些条件下阻止方法执行
[HarmonyPatch(typeof(Building), "Destroy")]
internal static class Building_Destroy_Patch
{
    [HarmonyPrefix]  // 前缀：在原方法前执行
    public static bool Prefix(Building __instance)
    {
        // 如果条件满足，返回 false 阻止原方法
        if (__instance.def.defName == "ImportantBuilding")
        {
            Messages.Message("Cannot destroy!", MessageTypeDefOf.RejectInput);
            return false;  // 🛑 阻止销毁
        }

        return true;  // ✅ 继续执行原方法
    }
}
```

**复制这个模板到你的代码中，改四个地方**：
1. `typeof(Building)` → 目标类
2. `"Destroy"` → 目标方法
3. `if (__instance.def.defName == ...)` → 你的条件
4. `return false;` → 返回值或其他处理

---

### 模式3：事件触发（第三常见，10% 的补丕）

```csharp
// 目标：在方法执行时触发事件/回调
[HarmonyPatch(typeof(Pawn), "Kill")]
internal static class Pawn_Kill_Patch
{
    [HarmonyPrefix]  // 在 Kill 之前执行
    public static void Prefix(Pawn __instance)
    {
        // 做你的事，例如记录日志
        MyModSystem.OnPawnAboutToDie(__instance);
    }

    // 或在之后执行
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        MyModSystem.OnPawnDied(__instance);
    }
}
```

---

## 2. 按场景组织的 API 速查表

### 场景A：我要改变一个数值（最常见）

| 你的需求 | 代码 | 说明 |
|---------|------|------|
| 改返回值 | `ref float __result` | 后缀中使用，能修改 |
| 访问原对象 | `TypeName __instance` | 非静态方法时可访问 |
| 访问原参数 | 补丕方法的同名参数 | 但不能修改（需 Transpiler） |
| 获取原对象的字段 | `Traverse.Create(__instance).Field("fieldName").GetValue()` | 绕过 private |
| 修改原对象的字段 | `Traverse.Create(__instance).Field("fieldName").SetValue(newVal)` | 绕过 private |

```csharp
// 示例：修改 Pawn 的血量
[HarmonyPostfix]
public static void Postfix(Pawn __instance, ref float __result)
{
    // 1. 访问原对象
    if (__instance.IsWounded)
    {
        // 2. 修改返回值
        __result *= 0.8f;
    }

    // 3. 访问私有字段
    var maxHealth = Traverse.Create(__instance)
        .Field("maxHealth")
        .GetValue<float>();
}
```

---

### 场景B：我要阻止某个操作

| 你的需求 | 代码 | 说明 |
|---------|------|------|
| 完全阻止 | `return false;` | 在 Prefix 中，阻止原方法 |
| 条件阻止 | `if (...) return false; return true;` | 只在某条件下阻止 |
| 替换返回值 | 先设置 `__result`，再 `return false` | 用自定义值替换结果 |
| 执行替代逻辑 | 在 Prefix 中执行，`return false` | 自己做完整逻辑 |

```csharp
[HarmonyPrefix]
public static bool Prefix(Building __instance, ref int __result)
{
    // 如果是特殊建筑，阻止并返回自定义值
    if (__instance.def.defName == "ReactorCore")
    {
        __result = 99999;  // 无法摧毁
        return false;  // 阻止原方法
    }

    return true;  // 继续执行
}
```

---

### 场景C：我要在方法前后做额外操作

| 你的需求 | 代码 | 说明 |
|---------|------|------|
| 方法前 | `[HarmonyPrefix]` + `void/bool` | 返回 bool 时能阻止 |
| 方法后 | `[HarmonyPostfix]` | 总是执行原方法（除非前缀阻止） |
| 方法前后配套 | Prefix + Postfix 同时使用 | 用 `__state` 传递数据 |
| 异常处理 | `[HarmonyFinalizer]` | 捕获异常（高级） |

```csharp
// 前后配套例子
[HarmonyPrefix]
public static void Prefix(Pawn __instance, out object __state)
{
    // 记录原始状态
    __state = new { health = __instance.health };
    Log.Message($"Before: {__instance.Name}");
}

[HarmonyPostfix]
public static void Postfix(Pawn __instance, object __state)
{
    // 恢复或验证
    Log.Message($"After: {__instance.Name}");
}
```

---

### 场景D：我要修改方法的参数

**这很特殊**：普通补丕**不能修改参数**！需要用 Transpiler。

```csharp
// ❌ 错误尝试：这改不了原方法的参数
[HarmonyPrefix]
public static void Prefix(int damage)
{
    damage = 5;  // ❌ 只改了本地副本，原方法收到的还是原值
}

// ✅ 正确方式1：用 ref（只能修改引用类型的字段或 ref 参数）
[HarmonyPrefix]
public static void Prefix(Pawn __instance)
{
    __instance.health = 100;  // ✅ 修改对象字段可以
}

// ✅ 正确方式2：修改引用类型字段
public class MyRef
{
    public int value;
}

[HarmonyPrefix]
public static void Prefix(MyRef damage)  // 对象引用
{
    damage.value = 5;  // ✅ 修改对象字段有效
}
```

---

### 场景E：我要查询或遍历对象的数据

```csharp
// API 速查
var allPatched = PatchProcessor.GetAllPatchedMethods();
foreach (var method in allPatched)
{
    Log.Message($"Patched: {method.FullDescription()}");
}

// 用 AccessTools 获取类型/方法/字段
var type = AccessTools.TypeByName("RimWorld.Building");
var method = AccessTools.Method(typeof(Pawn), "TakeDamage");
var field = AccessTools.Field(typeof(Pawn), "health");
var ctor = AccessTools.Constructor(typeof(Building), new Type[] { typeof(int) });

// 用 Traverse 访问私有成员
var traverse = Traverse.Create(pawn);
var skills = traverse.Field("skills").GetValue();  // 私有字段
var level = traverse.Property("SkillLevel").GetValue();  // 私有属性
```

---

## 3. 30+ 实战代码模板（可直接复制）

### 3.1 数值修改（8 个模板）

**模板1：简单乘法修改**
```csharp
[HarmonyPatch(typeof(Pawn), "get_BodySize")]
internal static class Pawn_BodySize_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        __result *= 1.2f;  // 增加 20%
    }
}
```

**模板2：条件加法**
```csharp
[HarmonyPatch(typeof(Pawn), "get_MarketValue")]
internal static class Pawn_MarketValue_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.IsPrisoner)
            __result += 500;  // 囚犯加 500
    }
}
```

**模版3：范围限制**
```csharp
[HarmonyPatch(typeof(Thing), "get_MaxHitPoints")]
internal static class Thing_MaxHP_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        __result = Mathf.Clamp(__result, 1, 9999);  // 限制在 1-9999
    }
}
```

**模板4：基于百分比的修改**
```csharp
[HarmonyPatch(typeof(Pawn), "get_HealthPercent")]
internal static class Pawn_HealthPercent_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        // 如果有伤，打折
        if (__instance.health.HasHediffs)
            __result *= 0.9f;  // 减少 10%
    }
}
```

**模板5：多条件累加**
```csharp
[HarmonyPatch(typeof(Pawn), "get_DefensePower")]
internal static class Pawn_Defense_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        float bonus = 0f;
        if (__instance.IsColonist) bonus += 10f;
        if (__instance.IsPrisoner) bonus -= 5f;
        if (__instance.IsSlaveOfColony) bonus += 2f;
        __result += bonus;
    }
}
```

**模板6：查表替换**
```csharp
private static Dictionary<string, float> modifiers = new()
{
    { "Human", 1.0f },
    { "Mechanoid", 0.5f },
    { "Animal", 0.3f }
};

[HarmonyPatch(typeof(Pawn), "get_BodySize")]
internal static class Pawn_BodySize_Lookup_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        if (modifiers.TryGetValue(__instance.def.defName, out var mod))
            __result *= mod;
    }
}
```

**模板7：缓存值**
```csharp
private static Dictionary<int, float> cache = new();

[HarmonyPatch(typeof(Pawn), "get_ComputedHealth")]
internal static class Pawn_Health_Cache_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance, ref float __result)
    {
        if (cache.TryGetValue(__instance.thingIDNumber, out var cached))
        {
            __result = cached;
            return false;  // 返回缓存，跳过原方法
        }
        return true;
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        cache[__instance.thingIDNumber] = __result;  // 缓存新值
    }
}
```

**模板8：指数运算**
```csharp
[HarmonyPatch(typeof(Pawn), "get_CombatRating")]
internal static class Pawn_Combat_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        // 强者越来越强（二次方）
        __result = __result * __result / 100f;
    }
}
```

---

### 3.2 条件判断和阻止（8 个模板）

**模板9：简单条件阻止**
```csharp
[HarmonyPatch(typeof(Building), "Destroy")]
internal static class Building_Destroy_Safe_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Building __instance)
    {
        if (__instance.def.defName == "ImportantBuilding")
            return false;  // 阻止销毁
        return true;
    }
}
```

**模板10：多条件判断**
```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
internal static class Pawn_Kill_Condition_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance)
    {
        // 多个条件任意满足就阻止
        if (__instance.IsColonist ||
            __instance.health.hediffSet.HasHediff(HediffDefOf.Resurrection) ||
            __instance.def.defName == "SpecialCreature")
            return false;

        return true;
    }
}
```

**模板11：基于地图/位置的判断**
```csharp
[HarmonyPatch(typeof(Thing), "Destroy")]
internal static class Thing_Destroy_Location_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Thing __instance)
    {
        // 只在特定地图上阻止
        if (__instance.MapHeld == Find.CurrentMap && Find.CurrentMap.mapPawns.Count < 5)
            return false;
        return true;
    }
}
```

**模板12：弹出消息后阻止**
```csharp
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
internal static class Pawn_TakeDamage_Alert_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance, int damage)
    {
        if (damage > 100)
        {
            Messages.Message(
                $"{__instance.Name} take {damage} damage!",
                MessageTypeDefOf.NegativeEvent
            );
            return false;  // 阻止伤害
        }
        return true;
    }
}
```

**模板13：返回自定义值代替原方法**
```csharp
[HarmonyPatch(typeof(Pawn), "get_IsDeathrestingOrDeathrest")]
internal static class Pawn_DeathRest_Override_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance, ref bool __result)
    {
        // 如果有特定 hediff，认为是死亡休息
        if (__instance.health.hediffSet.HasHediff(HediffDefOf.Hibernation))
        {
            __result = true;
            return false;  // 用自定义值替换原方法
        }
        return true;
    }
}
```

**模板14：概率阻止**
```csharp
[HarmonyPatch(typeof(Building), "Destroy")]
internal static class Building_Destroy_Random_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Building __instance)
    {
        // 有 30% 概率阻止
        if (Rand.Chance(0.3f))
        {
            Messages.Message("Indestructible by luck!", MessageTypeDefOf.Neutral);
            return false;
        }
        return true;
    }
}
```

**模板15：日志告警后继续**
```csharp
[HarmonyPatch(typeof(Pawn), "get_IsBurning")]
internal static class Pawn_Burning_Log_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref bool __result)
    {
        if (__result)
            Log.Warning($"{__instance.Name} is on fire!");
    }
}
```

**模板16：状态机式判断**
```csharp
public enum BuildingState { Normal, Protected, Destroyed }

[HarmonyPatch(typeof(Building), "TakeDamage")]
internal static class Building_State_Machine_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Building __instance)
    {
        var state = GetBuildingState(__instance);
        return state switch
        {
            BuildingState.Protected => false,  // 保护状态，阻止伤害
            BuildingState.Destroyed => true,   // 已摧毁，继续
            _ => true
        };
    }

    private static BuildingState GetBuildingState(Building b)
    {
        // 实现状态检查逻辑
        return BuildingState.Normal;
    }
}
```

---

### 3.3 访问和修改对象（6 个模板）

**模板17：访问私有字段**
```csharp
[HarmonyPatch(typeof(Pawn), "GenerateInitialHediffs")]
internal static class Pawn_Hediff_Access_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        // 访问私有字段 skills
        var skillsField = Traverse.Create(__instance).Field("skills");
        var skills = skillsField.GetValue<Skills>();

        Log.Message($"Skills: {skills}");
    }
}
```

**模板18：修改私有字段**
```csharp
[HarmonyPatch(typeof(Pawn), ".ctor")]  // 构造函数
internal static class Pawn_Constructor_Field_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        // 修改私有字段
        Traverse.Create(__instance)
            .Field("cachedBiologicalAge")
            .SetValue(99);
    }
}
```

**模板19：访问私有属性**
```csharp
[HarmonyPatch(typeof(Pawn), "get_IsWounded")]
internal static class Pawn_Property_Access_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref bool __result)
    {
        // 访问私有属性
        var healthPercent = Traverse.Create(__instance)
            .Property("HealthPercent")
            .GetValue<float>();

        if (healthPercent < 0.3f)
            __result = true;  // 强制认为受伤
    }
}
```

**模板20：调用私有方法**
```csharp
[HarmonyPatch(typeof(Pawn), "SomePublicMethod")]
internal static class Pawn_Private_Method_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        // 调用私有方法
        Traverse.Create(__instance)
            .Method("PrivateMethod", new Type[] { typeof(int) }, new object[] { 5 })
            .GetValue();
    }
}
```

**模板21：批量访问集合**
```csharp
[HarmonyPatch(typeof(Pawn), "get_AllHediffs")]
internal static class Pawn_Hediffs_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref List<Hediff> __result)
    {
        // 遍历并修改
        foreach (var hediff in __result)
        {
            if (hediff.def == HediffDefOf.Scar)
                hediff.Severity *= 0.5f;  // 减少疤痕严重度
        }
    }
}
```

**模板22：复制对象状态**
```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
internal static class Pawn_Kill_State_Copy_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, out object __state)
    {
        // 保存原始状态（前缀）
        __state = new
        {
            Name = __instance.Name,
            Health = __instance.health.summaryHealth.SummaryHealthPercent,
            Position = __instance.Position
        };
    }

    [HarmonyPostfix]
    public static void Postfix(object __state)
    {
        // 恢复使用（后缀）
        var state = (dynamic)__state;
        Log.Message($"Pawn {state.Name} died at {state.Position}");
    }
}
```

---

### 3.4 UI 和消息（4 个模板）

**模板23：添加标签到窗口**
```csharp
[HarmonyPatch(typeof(SomeWindow), "DoWindowContents")]
internal static class SomeWindow_Label_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Rect inRect)
    {
        Widgets.Label(
            new Rect(inRect.x, inRect.y + 50, 200, 30),
            "Custom Text"
        );
    }
}
```

**模板24：添加按钮**
```csharp
[HarmonyPatch(typeof(SomeWindow), "DoWindowContents")]
internal static class SomeWindow_Button_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Rect inRect)
    {
        Rect buttonRect = new Rect(inRect.x, inRect.y, 100, 30);
        if (Widgets.ButtonText(buttonRect, "Click Me"))
        {
            Messages.Message("Clicked!", MessageTypeDefOf.Neutral);
            // 执行你的逻辑
        }
    }
}
```

**模板25：添加悬停提示**
```csharp
[HarmonyPatch(typeof(InspectPane), "DrawInspectString")]
internal static class InspectPane_Tooltip_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Rect inRect)
    {
        Rect hoverRect = new Rect(inRect.x, inRect.y, 50, 50);
        if (Mouse.IsOver(hoverRect))
        {
            TooltipHandler.TipRegion(hoverRect, "Hover tip here");
            Widgets.DrawHighlight(hoverRect);
        }
    }
}
```

**模板26：修改 UI 颜色**
```csharp
[HarmonyPatch(typeof(Dialog), "DoWindowContents")]
internal static class Dialog_Color_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Rect inRect)
    {
        GUI.color = new Color(1f, 0.5f, 0.5f);  // 红色
        Widgets.Label(new Rect(inRect.x, inRect.y, 200, 30), "Red Text");
        GUI.color = Color.white;  // 恢复白色
    }
}
```

---

### 3.5 日志和调试（3 个模板）

**模板27：详细日志记录**
```csharp
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
internal static class Pawn_TakeDamage_Log_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, int damage)
    {
        Log.Message($"[TakeDamage] {__instance.Name} takes {damage} damage");
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        Log.Message($"[TakeDamage] Health now: {__instance.health.summaryHealth.SummaryHealthPercent:P}");
    }
}
```

**模板28：异常安全**
```csharp
[HarmonyPatch(typeof(Pawn), "SomeMethod")]
internal static class Pawn_Safe_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance)
    {
        try
        {
            // 补丕逻辑
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Patch failed: {ex}");
            return true;  // 回退到原方法
        }
    }
}
```

**模板29：调试模式条件**
```csharp
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
internal static class Pawn_TakeDamage_Debug_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, int damage)
    {
#if DEBUG
        Log.Warning($"DEBUG: {__instance.Name} takes {damage}");
#endif
    }
}
```

---

### 3.6 设置和可配置（3 个模板）

**模板30：使用设置控制补丕**
```csharp
// 在你的 Settings 类中
public class Settings : ModSettings
{
    public static bool enableDamageModification = true;
    public static float damageMultiplier = 1.0f;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref enableDamageModification, "enableDamageModification", true);
        Scribe_Values.Look(ref damageMultiplier, "damageMultiplier", 1.0f);
    }
}

// 在补丕中使用
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
internal static class Pawn_TakeDamage_Settings_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance, ref int damage)
    {
        if (!Settings.enableDamageModification)
            return true;

        damage = (int)(damage * Settings.damageMultiplier);
        return true;
    }
}
```

**模板31：版本检测控制**
```csharp
[HarmonyPatch(typeof(Pawn), "SomeMethod")]
internal static class Pawn_Version_Check_Patch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        // 1.5 和 1.6 API 不同，检测版本
#if RIMWORLD_1_5
        // 1.5 逻辑
        return true;
#elif RIMWORLD_1_6
        // 1.6 逻辑
        return true;
#endif
    }
}
```

**模板32：模组兼容性检查**
```csharp
// 检查其他模组是否存在
private static bool IsModLoaded(string packageId)
{
    return LoadedModManager.RunningModsListForReading
        .Any(m => m.PackageId == packageId);
}

[HarmonyPatch(typeof(Pawn), "SomeMethod")]
internal static class Pawn_Compat_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance)
    {
        // 如果依赖的模组没加载，这个补丕不做任何事
        if (!IsModLoaded("other.mod.id"))
            return true;

        // 补丕逻辑
        return true;
    }
}
```

---

## 4. 常见错误快速排查表

| 症状 | 可能原因 | 检查项 | 解决方案 |
|------|--------|--------|---------|
| 补丕没有应用 | 类名/方法名错误 | 检查 `typeof(ClassName)` 和 `"MethodName"` 是否准确 | 从 RimWorld 源代码复制准确的类名、方法名 |
| 补丕没有应用 | 参数类型错误 | 确认原方法的参数，例如是 `int` 还是 `float` | 对比游戏源代码中的方法签名 |
| 补丕没有应用 | 属性名拼错 | `[HarmonyPatch]` 拼写，`[HarmonyPrefix]` 等 | 检查属性的大小写和拼写 |
| 游戏崩溃 | 补丕中的异常未处理 | 补丕方法中有 try-catch 吗？ | 添加 try-catch，用 Log.Error 记录 |
| 游戏崩溃 | 访问空对象 | `__instance` 是否可能为 null？ | 添加 `if (__instance == null) return;` |
| 返回值为 null | `__result` 签名错误 | `ref float __result` 而非 `float __result` | 必须用 ref 关键字 |
| 返回值为 null | `__result` 类型错误 | 方法返回 `int` 但用了 `float __result` | 类型必须匹配原方法 |
| 参数无法修改 | 尝试修改值类型参数 | `void Prefix(int damage)` 无法改 damage | 用 Transpiler，或修改对象字段 |
| 多模组冲突 | 多个 Harmony 版本 | 是否加载了多个 Harmony 模组？ | 确保只有一个 Harmony（游戏自带） |
| 后缀补丕不执行 | 前缀阻止了方法 | 前缀是否 `return false`？ | 确认前缀确实允许原方法执行 |
| `Transpiler` 无效 | IL 指令修改错误 | CodeMatcher 的指令搜索是否正确？ | 逐句测试 CodeMatcher 的查找 |
| 设置未保存 | 未实现 `ExposeData()` | ModSettings 是否实现了此方法？ | 必须覆盖 `ExposeData()` 并调用 Write() |
| 性能差 | 前缀中做复杂计算 | 前缀是否在热路径中？ | 把复杂逻辑移到后缀，或用缓存 |

---

## 5. 调试技巧

### 5.1 启用 Harmony 调试输出

```csharp
// 在 Main.cs 中，在 harmony.PatchAll() 之前
Environment.SetEnvironmentVariable("HARMONY_DEBUG", "1");

var harmony = new Harmony("your.mod.id");
harmony.PatchAll();

// 现在 Harmony 会输出详细的补丕应用日志
// 查看 Player.log 文件
```

### 5.2 查询已应用补丕

```csharp
// 在任意补丕中或 Mod.cs 中
[StaticConstructorOnStartup]
public class DebugPatches
{
    static DebugPatches()
    {
        var allPatches = PatchProcessor.GetAllPatchedMethods();
        Log.Message($"Total patched methods: {allPatches.Count()}");

        foreach (var method in allPatches)
        {
            Log.Message($"  - {method.FullDescription()}");
        }
    }
}
```

### 5.3 验证补丕是否被应用

```csharp
// 检查特定方法是否被补丕
var targetMethod = AccessTools.Method(typeof(Pawn), "TakeDamage");
var patchInfo = HarmonySharedState.GetPatchInfo(targetMethod);

if (patchInfo != null)
{
    Log.Message($"Method is patched!");
    Log.Message($"  Prefixes: {patchInfo.Prefixes.Count()}");
    Log.Message($"  Postfixes: {patchInfo.Postfixes.Count()}");
}
else
{
    Log.Message($"Method is NOT patched!");
}
```

### 5.4 逐步调试补丕执行

```csharp
// 在补丕前后添加日志
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
internal static class Pawn_TakeDamage_Debug_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance, int damage)
    {
        Log.Message($"[PREFIX] {__instance.Name} about to take {damage}");
        return true;
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        Log.Message($"[POSTFIX] {__instance.Name} health now: {__instance.health.summaryHealth.SummaryHealthPercent:P}");
    }
}
```

### 5.5 捕获异常堆栈

```csharp
[HarmonyPatch(typeof(Pawn), "SomeMethod")]
internal static class Pawn_Exception_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance)
    {
        try
        {
            // 补丕逻辑
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in patch:\n{ex}");
            Log.Error($"Stack trace:\n{ex.StackTrace}");
            return true;  // 继续执行原方法
        }
    }
}
```

---

## 6. 快速参考卡

### 补丕方法的特殊参数

```
__instance   → 非静态方法中，原对象
__result     → 返回值（必须用 ref）
__state      → 前缀传给后缀的状态对象
__exception  → 异常对象（仅后缀和 Finalizer）
```

### 常用 API 一行速查

```csharp
var type = AccessTools.TypeByName("Namespace.ClassName");
var method = AccessTools.Method(typeof(Class), "MethodName");
var field = AccessTools.Field(typeof(Class), "fieldName");
var value = Traverse.Create(obj).Field("name").GetValue();
Traverse.Create(obj).Field("name").SetValue(newValue);

Log.Message("info");
Log.Warning("warning");
Log.Error("error");

Messages.Message("UI text", MessageTypeDefOf.Neutral);
```

### 补丕属性快速复制

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
[HarmonyPriority(Priority.High)]
[HarmonyBefore("other.mod.id")]
[HarmonyAfter("another.mod.id")]
internal static class TargetClass_TargetMethod_Patch
{
    [HarmonyPrefix]
    public static bool Prefix() { }

    [HarmonyPostfix]
    public static void Postfix() { }
}
```

---

## 7. 工作流清单

每次写新补丕时，用这个清单：

- [ ] 从源代码找到准确的类名和方法名
- [ ] 从模板库选择最接近的模板
- [ ] 复制模板，改三个地方（类名、方法名、逻辑）
- [ ] 补丕方法签名用 IDE 的自动补全（减少拼写错误）
- [ ] 关键补丕添加 `[HarmonyPriority]` 或 `[HarmonyBefore]/[HarmonyAfter]`
- [ ] 异常处理（try-catch）
- [ ] 运行游戏测试
- [ ] 启用 HARMONY_DEBUG 查看补丕是否应用
- [ ] 查看 Player.log 检查是否有错误
- [ ] 如果补丕没应用，检查属性和参数
- [ ] 如果补丕有异常，添加更多日志定位
- [ ] 完成后，记录到文档（特别是如果有特殊的前置依赖）

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含 5 分钟速成、按场景 API 速查、32 个实战模板、错误排查表、调试技巧、工作流清单 | 2026-01-12 | Knowledge Refiner |
