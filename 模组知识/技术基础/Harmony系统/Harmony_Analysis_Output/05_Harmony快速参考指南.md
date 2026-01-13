# Harmony快速参考指南

## 元信息

- **摘要**：Harmony API的快速参考速查表，包含最常用的API、属性、方法签名模板、常见模式代码示例
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：API速查，代码模板，常见模式，快速参考
- **标签**：[待审]，实践速查表
- **适用对象**：模组开发者、Harmony使用者

---

## 1. 核心API速查表

### 1.1 Harmony 实例创建

```csharp
// 创建实例
var harmony = new Harmony("com.yourname.modname");

// 自动应用所有补丁
harmony.PatchAll();

// 扫描指定程序集
harmony.PatchAll(typeof(YourClass).Assembly);
```

### 1.2 手动补丁API

```csharp
// 创建方法补丁处理器
var processor = harmony.CreateProcessor(targetMethod);

// 链式添加补丁
processor
    .AddPrefix(prefixMethod)
    .AddPostfix(postfixMethod)
    .AddTranspiler(transpilerMethod)
    .Patch();

// 创建类处理器
var classProcessor = harmony.CreateClassProcessor(typeof(PatchClass));
classProcessor.Patch();

// 创建反向补丁
var reversePatcher = harmony.CreateReversePatcher(originalMethod, standinMethod);
```

### 1.3 查询已应用补丁

```csharp
var allPatchedMethods = PatchProcessor.GetAllPatchedMethods();
foreach (var method in allPatchedMethods)
{
    Log.Message($"Patched: {method.FullDescription()}");
}
```

---

## 2. 属性速查表

### 2.1 目标指定：HarmonyPatch

```csharp
// 仅类型
[HarmonyPatch(typeof(TargetClass))]

// 类型 + 方法名
[HarmonyPatch(typeof(TargetClass), "MethodName")]

// 类型 + 方法名 + 参数
[HarmonyPatch(typeof(TargetClass), "MethodName", typeof(int), typeof(string))]

// 方法类型（构造函数、属性等）
[HarmonyPatch(typeof(TargetClass), MethodType.Constructor)]
[HarmonyPatch(typeof(TargetClass), MethodType.Getter)]
[HarmonyPatch(typeof(TargetClass), MethodType.Setter)]

// 参数特殊类型
[HarmonyPatch(typeof(TargetClass), "Method",
    new Type[] { typeof(int), typeof(string) },
    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
```

**ArgumentType 枚举**：
- `Normal` - 普通参数
- `Ref` - ref参数
- `Out` - out参数
- `Pointer` - 指针参数

### 2.2 补丁类型属性

```csharp
[HarmonyPrefix]         // 前缀补丁
[HarmonyPostfix]        // 后缀补丁
[HarmonyTranspiler]     // IL转换补丁
[HarmonyFinalizer]      // 异常处理补丁
```

### 2.3 执行顺序控制

```csharp
[HarmonyPriority(Priority.High)]     // 优先级设置
[HarmonyBefore("other.mod.harmony")] // 在某补丁前执行
[HarmonyAfter("other.mod.harmony")]  // 在某补丁后执行
```

---

## 3. 前缀补丁模板

### 3.1 基础模板

```csharp
[HarmonyPrefix]
public static bool Prefix(/* 原方法参数列表 */)
{
    // 补丁逻辑
    return true;  // true=继续原方法，false=阻止原方法
}
```

### 3.2 完整参数模板

```csharp
[HarmonyPrefix]
public static bool Prefix(
    /* 原方法参数 */
    ref object __instance,              // 原对象（非静态方法）
    ref string __result,                // 返回值（ref）
    ref object __state                  // 传递给后缀的状态
)
{
    // 访问原对象
    if (__instance is MyClass obj)
    {
        // obj.field = ...
    }

    // 设置返回值并阻止原方法
    __result = "custom result";
    return false;

    // 或继续原方法
    return true;
}
```

### 3.3 异常处理模板

```csharp
[HarmonyPrefix]
public static bool Prefix(/* 参数 */)
{
    try
    {
        // 补丁逻辑
        return false;  // 阻止原方法
    }
    catch (Exception ex)
    {
        Log.Warning("Patch failed: " + ex);
        return true;  // 回退到原方法
    }
}
```

---

## 4. 后缀补丁模板

### 4.1 基础模板

```csharp
[HarmonyPostfix]
public static void Postfix(/* 原方法参数列表 */)
{
    // 补丁逻辑（在原方法后执行）
}
```

### 4.2 访问返回值

```csharp
[HarmonyPostfix]
public static void Postfix(ref int __result)
{
    // 修改返回值
    __result *= 2;
}
```

### 4.3 访问原对象

```csharp
[HarmonyPostfix]
public static void Postfix(
    Thing __instance,
    ref int __result
)
{
    if (__instance is Pawn pawn)
    {
        __result += pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
    }
}
```

### 4.4 接收状态并处理异常

```csharp
[HarmonyPostfix]
public static void Postfix(
    object __state,                     // 从前缀接收
    ref int __result,
    __exception Exception __exception   // 异常信息
)
{
    if (__exception != null)
    {
        Log.Warning("Method threw: " + __exception.Message);
    }

    if (__state is MyState state)
    {
        // 使用前缀传递的状态
    }
}
```

---

## 5. 常见补丁模式

### 5.1 数值增强模式

```csharp
[HarmonyPatch(typeof(Pawn), "get_BodySize")]
internal static class Pawn_BodySize_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        // 增加20%体型
        __result *= 1.2f;
    }
}
```

### 5.2 条件拦截模式

```csharp
[HarmonyPatch(typeof(Building), "Destroy")]
internal static class Building_Destroy_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Building __instance)
    {
        // 特定建筑不可摧毁
        if (__instance.def.defName == "ImportantBuilding")
        {
            Messages.Message("Cannot destroy!", MessageTypeDefOf.RejectInput);
            return false;
        }
        return true;
    }
}
```

### 5.3 事件触发模式

```csharp
[HarmonyPatch(typeof(Pawn), "Kill")]
internal static class Pawn_Kill_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance)
    {
        // 在生命值变为0前做什么
        MyModSystem.OnPawnAboutToDie(__instance);
    }
}
```

### 5.4 缓存优化模式

```csharp
private static Dictionary<Thing, float> cachedValues = new();

[HarmonyPatch(typeof(Thing), "GetHashCode")]
internal static class Thing_GetHashCode_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Thing __instance, ref int __result)
    {
        if (cachedValues.TryGetValue(__instance, out var cached))
        {
            __result = (int)cached;
            return false;  // 返回缓存值，跳过原方法
        }
        return true;  // 执行原方法并缓存
    }

    [HarmonyPostfix]
    public static void Postfix(Thing __instance, ref int __result)
    {
        cachedValues[__instance] = __result;
    }
}
```

### 5.5 多步骤补丁模式

```csharp
[HarmonyPatch(typeof(Job), ".ctor")]
internal static class Job_Constructor_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ref JobDef def, ref object __state)
    {
        // 阶段1：记录原始def
        __state = def;
    }

    [HarmonyPostfix]
    public static void Postfix(Job __instance, object __state)
    {
        // 阶段2：修改创建后的Job
        var originalDef = (JobDef)__state;
        MyModSystem.OnJobCreated(__instance, originalDef);
    }
}
```

---

## 6. UI补丁快速模板

### 6.1 简单标签添加

```csharp
[HarmonyPatch(typeof(SomeWindow), "DoWindowContents")]
internal static class SomeWindow_DoWindowContents_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Rect inRect)
    {
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(inRect.x, inRect.y + 10, 100, 30), "Custom Text");
    }
}
```

### 6.2 按钮添加

```csharp
[HarmonyPostfix]
public static void Postfix(Rect inRect)
{
    Rect buttonRect = new Rect(inRect.x, inRect.y, 100, 30);
    if (Widgets.ButtonText(buttonRect, "Click Me"))
    {
        Messages.Message("Button clicked!", MessageTypeDefOf.Neutral);
    }
}
```

### 6.3 交互提示

```csharp
[HarmonyPostfix]
public static void Postfix(Rect inRect)
{
    Rect hoverRect = new Rect(inRect.x, inRect.y, 50, 50);
    if (Mouse.IsOver(hoverRect))
    {
        TooltipHandler.TipRegion(hoverRect, "Hover text here");
        Widgets.DrawHighlight(hoverRect);
    }
}
```

---

## 7. RimWorld API常用对象

### 7.1 全局访问

```csharp
Find.World              // 世界对象
Find.CurrentMap         // 当前地图
Find.WindowStack        // 窗口栈
Find.LetterStack        // 信件栈
Find.Selector           // 选择器
```

### 7.2 日志输出

```csharp
Log.Message("info");        // 普通信息
Log.Warning("warning");     // 警告（黄色）
Log.Error("error");         // 错误（红色）
```

### 7.3 UI对话框

```csharp
Find.WindowStack.Add(
    new Dialog_MessageBox("Message", "OK")
);

Find.WindowStack.Add(
    new Dialog_Input("Input prompt", true, s => OnInput(s))
);
```

### 7.4 消息提示

```csharp
Messages.Message("Text", MessageTypeDefOf.Neutral);
Messages.Message("Success", MessageTypeDefOf.PositiveEvent);
Messages.Message("Error", MessageTypeDefOf.NegativeEvent);
```

---

## 8. 设置管理快速模板

### 8.1 简单设置类

```csharp
public class Settings : ModSettings
{
    public static bool option1 = true;
    public static float option2 = 0.5f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref option1, "option1", true);
        Scribe_Values.Look(ref option2, "option2", 0.5f);
    }
}
```

### 8.2 在补丁中使用设置

```csharp
[HarmonyPrefix]
public static bool Prefix()
{
    if (!Settings.option1)
    {
        return true;  // 禁用此补丁
    }

    var strength = Settings.option2;
    // 使用strength...
}
```

---

## 9. 反射工具常用方法

### 9.1 AccessTools 速查

```csharp
using HarmonyLib;

// 获取类型
var type = AccessTools.TypeByName("NamespaceName.ClassName");

// 获取方法
var method = AccessTools.Method(typeof(Class), "MethodName");

// 获取字段
var field = AccessTools.Field(typeof(Class), "fieldName");

// 获取属性getter/setter
var getter = AccessTools.PropertyGetter(typeof(Class), "PropertyName");
var setter = AccessTools.PropertySetter(typeof(Class), "PropertyName");

// 获取构造函数
var ctor = AccessTools.Constructor(typeof(Class), new Type[] { typeof(int) });

// 从程序集获取所有类型
var types = AccessTools.GetTypesFromAssembly(assembly);
```

### 9.2 Traverse 速查

```csharp
using HarmonyLib;

// 创建遍历
var traverse = Traverse.Create(obj);

// 访问字段
traverse.Field("fieldName").SetValue(newValue);
var value = traverse.Field("fieldName").GetValue();

// 访问属性
traverse.Property("PropertyName").SetValue(newValue);

// 嵌套访问
traverse.Field("parent").Field("child").SetValue(value);

// 调用方法
traverse.Method("MethodName", new Type[] { typeof(int) }, new object[] { 5 }).GetValue();
```

---

## 10. 常见错误快速排查

| 错误症状 | 可能原因 | 解决方案 |
|---------|---------|---------|
| 补丁未应用 | 目标方法不存在或属性错误 | 检查方法名、参数类型、验证RimWorld源代码 |
| 游戏崩溃 | 补丁代码异常 | 添加try-catch，启用HARMONY_DEBUG |
| 返回值为null | 使用__result时签名错误 | 确保__result是ref参数，类型正确 |
| 多Harmony冲突 | 多个Harmony版本同时加载 | 确保只有一个Harmony模组，检查依赖 |
| Transpiler无效 | IL指令修改错误 | 使用CodeMatcher工具，逐步调试 |
| 设置未保存 | ExposeData未实现 | 实现ModSettings.ExposeData()，调用Write() |

---

## 11. 环境变量设置

```csharp
// 启用Harmony调试
Environment.SetEnvironmentVariable("HARMONY_DEBUG", "1");

// 禁用日志创建
Environment.SetEnvironmentVariable("HARMONY_NO_LOG", "1");
```

---

## 12. 补丁性能建议

```csharp
// ❌ 坏：复杂的前缀，即使返回true也有开销
[HarmonyPrefix]
public static bool Prefix()
{
    for (int i = 0; i < 1000; i++)
    {
        // 复杂逻辑
    }
    return true;  // 最终还是执行原方法
}

// ✅ 好：快速检查，阻止时执行自定义逻辑
[HarmonyPrefix]
public static bool Prefix()
{
    if (ShouldIntercept())
    {
        DoCustomLogic();
        return false;  // 只在需要时才做复杂工作
    }
    return true;  // 快速通过
}
```

---

## 13. 多版本支持速查

### 13.1 条件编译

```csharp
#if RIMWORLD_1_5
    // 仅1.5版本代码
#elif RIMWORLD_1_6
    // 仅1.6版本代码
#endif
```

### 13.2 运行时检测

```csharp
public static bool IsVersion16()
{
    return LoadedModManager.RunningModsListForReading
        .Any(m => m.PackageId == "Ludeon.RimWorld");
}
```

---

## 快速查找索引

| 功能 | 相关文档章节 |
|------|-----------|
| 创建Harmony实例 | 1.1 |
| 前缀补丁 | 3, 5 |
| 后缀补丁 | 4, 5 |
| 属性列表 | 2 |
| 常见模式 | 5 |
| UI补丁 | 6 |
| 错误排查 | 10 |
| 反射工具 | 9 |
| 设置系统 | 8 |
| 性能优化 | 12 |

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含API速查、属性速查、常见模板、快速排查表 | 2026-01-12 | Knowledge Refiner |

