# Harmony核心API使用指南

## 元信息

- **摘要**：详细说明Harmony框架的核心API、属性系统、补丁方法签名约定以及代码示例。所有内容基于Harmony源代码和RiMCP验证的RimWorld API。
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：补丁属性，前缀后缀，方法签名，参数注入，补丁应用
- **标签**：[待审]，代码示例来自Harmony源代码
- **源代码位置**：Harmony 2.4.2.0 - HarmonyLib命名空间

---

## 1. Harmony实例初始化

### 1.1 创建Harmony实例

**API**：`Harmony(string id)` [Harmony.cs:15-59]

```csharp
using HarmonyLib;

// 创建Harmony实例
var harmony = new Harmony("com.my.mod.harmony.id");
```

**参数说明**：
- `id`：字符串，Harmony实例的唯一标识符
  - 不能为null或空
  - 建议使用反向域名格式（如`com.company.project.harmony`）
  - 用于在日志和调试中区分不同的补丁集合
  - ✓ RiMCP验证位置：构造函数实现

**来源**：[Harmony.cs:15-59]

### 1.2 调试模式

**环境变量**：`HARMONY_DEBUG` [Harmony.cs:23-27]

```csharp
string environmentVariable = Environment.GetEnvironmentVariable("HARMONY_DEBUG");
if (environmentVariable != null && environmentVariable.Length > 0)
{
    environmentVariable = environmentVariable.Trim();
    DEBUG = environmentVariable == "1" || bool.Parse(environmentVariable);
}
```

**启用方式**：
- 设置环境变量：`HARMONY_DEBUG=1` 或 `HARMONY_DEBUG=true`
- 效果：详细的日志输出到`harmony.log.txt`文件

**日志内容**（从代码看）：
- Harmony版本号
- Harmony库位置
- 调用方信息
- 时间戳

**来源**：[Harmony.cs:33-57]

---

## 2. 补丁应用API

### 2.1 自动模式：PatchAll()

#### 2.1.1 扫描调用程序集 [Harmony.cs:61-66]

```csharp
public void PatchAll()
{
    MethodBase method = new StackTrace().GetFrame(1).GetMethod();
    Assembly assembly = method.ReflectedType.Assembly;
    PatchAll(assembly);
}
```

**功能**：
- 自动扫描调用该方法的程序集
- 查找所有带`[HarmonyPatch]`属性的类
- 为每个类创建`PatchClassProcessor`并应用补丁

**调用示例**（来自HarmonyMod.Main）：
```csharp
new Harmony("net.pardeike.rimworld.lib.harmony").PatchAll();
```

**来源**：[Harmony.cs:61-89]

#### 2.1.2 指定程序集扫描 [Harmony.cs:83-89]

```csharp
public void PatchAll(Assembly assembly)
{
    AccessTools.GetTypesFromAssembly(assembly).DoIf(
        (Type type) => type.HasHarmonyAttribute(),
        delegate(Type type)
        {
            CreateClassProcessor(type).Patch();
        });
}
```

**功能**：
- 扫描指定程序集中所有带Harmony属性的类
- 逐一应用补丁

**使用场景**：
- 动态扫描其他程序集（而不是调用程序集）

**来源**：[Harmony.cs:83-89]

#### 2.1.3 扫描未分类补丁 [Harmony.cs:91-106]

**API名称**：`PatchAllUncategorized()` 和 `PatchAllUncategorized(Assembly)`

**功能**（推断）：扫描没有被分类的补丁

**来源**：[Harmony.cs:91-106中的方法存在但源代码被截断]

### 2.2 手动模式：创建处理器

#### 2.2.1 方法级别补丁

**API**：`PatchProcessor CreateProcessor(MethodBase original)` [Harmony.cs:68-71]

```csharp
public PatchProcessor CreateProcessor(MethodBase original)
{
    return new PatchProcessor(this, original);
}
```

**使用示例**：
```csharp
var processor = harmony.CreateProcessor(targetMethod);
processor.AddPrefix(prefixMethod);
processor.AddPostfix(postfixMethod);
var patchedMethod = processor.Patch();
```

**返回值**：`PatchProcessor`实例，支持链式调用

**来源**：[Harmony.cs:68-71]

#### 2.2.2 类级别补丁

**API**：`PatchClassProcessor CreateClassProcessor(Type type)` [Harmony.cs:73-76]

```csharp
public PatchClassProcessor CreateClassProcessor(Type type)
{
    return new PatchClassProcessor(this, type);
}
```

**用途**：为某个类中的所有补丁方法创建处理器

**来源**：[Harmony.cs:73-76]

#### 2.2.3 反向补丁

**API**：`ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)` [Harmony.cs:78-81]

```csharp
public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)
{
    return new ReversePatcher(this, original, standin);
}
```

**功能**（推断）：创建反向补丁，从原方法复制代码到自定义方法

**来源**：[Harmony.cs:78-81]

---

## 3. 补丁属性系统

### 3.1 HarmonyPatch 属性 - 目标指定

**属性定义** [HarmonyPatch.cs:1-157]

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true)]
public class HarmonyPatch : HarmonyAttribute
```

**使用目标**：类、结构、方法、委托（可重复使用）

### 3.2 补丁目标指定方式

#### 3.2.1 仅类型

```csharp
[HarmonyPatch(typeof(TargetClass))]
class MyPatches
{
    // 会自动查找该类的默认方法或主要方法
}
```

#### 3.2.2 类型 + 方法名

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
```

#### 3.2.3 类型 + 方法名 + 参数类型

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName", typeof(int), typeof(string))]
```

**用途**：精确指定重载方法

#### 3.2.4 类型 + 方法类型（MethodType）

```csharp
[HarmonyPatch(typeof(TargetClass), MethodType.Constructor)]  // 构造函数
[HarmonyPatch(typeof(TargetClass), MethodType.Getter)]       // 属性getter
[HarmonyPatch(typeof(TargetClass), MethodType.Setter)]       // 属性setter
```

#### 3.2.5 支持特殊参数类型 [HarmonyPatch.cs:128-156]

```csharp
[HarmonyPatch(typeof(TargetClass), "Method",
    new Type[] { typeof(int), typeof(string) },
    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
```

**ArgumentType 枚举值**：
- `Normal` - 普通参数
- `Ref` - ref参数 (转换为 `Type.MakeByRefType()`)
- `Out` - out参数 (转换为 `Type.MakeByRefType()`)
- `Pointer` - 指针参数 (转换为 `Type.MakePointerType()`)

**来源**：[HarmonyPatch.cs:128-156]

### 3.3 补丁方法属性

#### 3.3.1 HarmonyPrefix - 前缀补丁 [HarmonyPrefix.cs]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPrefix : Attribute
{
}
```

**使用示例**：
```csharp
[HarmonyPatch(typeof(Environment), "GetStackTrace")]
internal static class Environment_GetStackTrace_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Exception e, bool needFileInfo, ref string __result)
    {
        // 补丁逻辑
        return true;  // 返回true继续执行原方法，false中止
    }
}
```

**来源**：[来自HarmonyMod的补丁示例]，[HarmonyPrefix.cs]

#### 3.3.2 HarmonyPostfix - 后缀补丁 [HarmonyPostfix.cs]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPostfix : Attribute
{
}
```

**使用示例**：
```csharp
[HarmonyPatch(typeof(Log), "ResetMessageCount")]
internal static class Log_ResetMessageCount_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ExceptionTools.seenStacktraces.Clear();
    }
}
```

**来源**：[来自HarmonyMod的补丁示例]，[HarmonyPostfix.cs]

#### 3.3.3 HarmonyTranspiler - 代码转换补丁

**属性定义**：[HarmonyTranspiler.cs - 存在但源代码未读]

**功能**：修改方法的IL代码

**用途**：高级补丁场景，对IL代码进行精细控制

#### 3.3.4 HarmonyFinalizer - 异常处理补丁

**属性定义**：[HarmonyFinalizer.cs - 存在但源代码未读]

**功能**：捕获原方法或其他补丁抛出的异常

### 3.4 其他属性

| 属性名 | 用途 | 来源 |
|--------|------|------|
| `HarmonyPriority` | 指定补丁执行顺序 | HarmonyPriority.cs |
| `HarmonyBefore` | 在某个补丁之前执行 | HarmonyBefore.cs |
| `HarmonyAfter` | 在某个补丁之后执行 | HarmonyAfter.cs |
| `HarmonyArgument` | 标记参数注入 | HarmonyArgument.cs |
| `HarmonyDelegate` | 委托相关配置 | HarmonyDelegate.cs |
| `HarmonyMethod` | 方法配置对象 | HarmonyMethod.cs |
| `HarmonyException` | 异常处理配置 | HarmonyException.cs |

**来源**：[HarmonyLib目录中的属性文件]

---

## 4. 补丁方法签名规范

### 4.1 前缀补临(Prefix)

#### 4.1.1 基本规则

- **返回类型**：`bool`
- **参数**：原方法的所有参数 + 特殊参数
- **返回值语义**：
  - `true` - 继续执行原方法
  - `false` - 阻止执行原方法，使用注入的`__result`作为返回值

#### 4.1.2 特殊参数

| 参数名 | 类型 | 用途 |
|--------|------|------|
| `__instance` | 原方法所在对象 | 访问原对象的字段和方法 |
| `__result` | ref 返回值类型 | 提供/修改方法返回值 |
| `__state` | 任意类型 | 传递数据到后缀补丁 |
| `__originalMethod` | MethodBase | 获取原始方法的反射信息 |

#### 4.1.3 示例 [从Environment_GetStackTrace_Patch.cs提取]

```csharp
public static bool Prefix(Exception e, bool needFileInfo, ref string __result)
{
    if (Settings.noStacktraceEnhancing)
    {
        return true;  // 让原方法继续执行
    }
    try
    {
        StackTrace trace = ((e == null) ? new StackTrace(needFileInfo) : new StackTrace(e, needFileInfo));
        __result = ExceptionTools.ExtractHarmonyEnhancedStackTrace(trace, forceRefresh: false, out var _);
        return false;  // 停止原方法，使用__result
    }
    catch (Exception)
    {
        return true;  // 异常时回退
    }
}
```

**来源**：[Environment_GetStackTrace_Patch.cs:10-26]

### 4.2 后缀补丁(Postfix)

#### 4.2.1 基本规则

- **返回类型**：通常为`void`，也可以返回值修改原方法返回值
- **参数**：原方法的所有参数 + 特殊参数
- **执行时机**：原方法执行完毕后（无论是否异常）

#### 4.2.2 特殊参数

| 参数名 | 类型 | 用途 |
|--------|------|------|
| `__instance` | 原方法所在对象 | 访问原对象 |
| `__result` | ref 返回值类型 | 获取/修改返回值 |
| `__state` | 任意类型 | 从前缀接收状态 |
| `__exception` | Exception | 捕获原方法抛出的异常（若有） |

#### 4.2.3 示例 [从Log_ResetMessageCount_Patch.cs提取]

```csharp
public static void Postfix()
{
    ExceptionTools.seenStacktraces.Clear();
}
```

**来源**：[Log_ResetMessageCount_Patch.cs:9-12]

### 4.3 代码转换补丁(Transpiler)

**功能**：直接修改IL指令

**参数**：`IEnumerable<CodeInstruction>`

**返回值**：修改后的`IEnumerable<CodeInstruction>`

**使用工具**：`CodeMatcher`和`CodeInstruction`类

**复杂度**：高，需要理解IL指令

---

## 5. 补丁应用流程（PatchProcessor）

### 5.1 API调用链

**来源**：[PatchProcessor.cs:30-140]

```csharp
public class PatchProcessor
{
    // 链式API
    public PatchProcessor AddPrefix(HarmonyMethod prefix)  // 返回this
    {
        this.prefix = prefix;
        return this;
    }

    public PatchProcessor AddPostfix(HarmonyMethod postfix)  // 返回this
    {
        this.postfix = postfix;
        return this;
    }

    public PatchProcessor AddTranspiler(HarmonyMethod transpiler)  // 返回this
    public PatchProcessor AddFinalizer(HarmonyMethod finalizer)  // 返回this

    // 应用补丁
    public MethodInfo Patch()
    {
        if ((object)original == null)
        {
            throw new NullReferenceException("Null method for " + instance.Id);
        }

        // 线程安全保护
        lock (locker)
        {
            // 1. 获取或创建PatchInfo
            PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original) ?? new PatchInfo();

            // 2. 添加补丁到PatchInfo
            patchInfo.AddPrefixes(instance.Id, prefix);
            patchInfo.AddPostfixes(instance.Id, postfix);
            patchInfo.AddTranspilers(instance.Id, transpiler);
            patchInfo.AddFinalizers(instance.Id, finalizer);

            // 3. 生成新方法
            MethodInfo methodInfo = PatchFunctions.UpdateWrapper(original, patchInfo);

            // 4. 更新全局补丁信息
            HarmonySharedState.UpdatePatchInfo(original, methodInfo, patchInfo);

            return methodInfo;
        }
    }

    public PatchProcessor Unpatch(HarmonyPatchType type, string harmonyID)
    {
        // 移除补丁的逻辑
    }

    public static IEnumerable<MethodBase> GetAllPatchedMethods()
    {
        lock (locker)
        {
            return HarmonySharedState.GetPatchedMethods();
        }
    }
}
```

### 5.2 线程安全机制

**关键点**：[PatchProcessor.cs:28]

```csharp
internal static readonly object locker = new object();
```

- 所有补丁操作都在同一个全局锁保护下
- 防止并发补丁应用导致的数据不一致

### 5.3 补丁查询API

**获取所有已补丁的方法**：

```csharp
var allPatchedMethods = PatchProcessor.GetAllPatchedMethods();
```

---

## 6. HarmonyMod实现示例分析

### 6.1 模组初始化流程

**来源**：[HarmonyMod/Main.cs:22-63]

```csharp
[StaticConstructorOnStartup]
public class Main : Mod
{
    static Main()
    {
        // 1. 初始化版本变量
        loadedHarmonyVersion = null;
        modVersion = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
            Assembly.GetExecutingAssembly(),
            typeof(AssemblyFileVersionAttribute),
            inherit: false)).Version;

        // 2. 获取已加载的Harmony程序集
        string[] HarmonyNames = new string[3] { "0Harmony", "Lib.Harmony", "HarmonyLib" };
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
            (Assembly a) => HarmonyNames.Contains(a.GetName().Name));

        // 3. 版本检测与冲突处理
        if (assembly != null)
        {
            loadedHarmonyVersion = assembly.GetName().Version ?? new Version(0, 0, 0, 0);

            string text = SafeLocation(assembly);  // 已加载Harmony的位置
            string text2 = SafeLocation(Assembly.GetExecutingAssembly());  // 当前模组位置
            string text3 = text2.Substring(0, text2.LastIndexOfAny(new char[2] { '\\', '/' }) + 1)
                + "0Harmony.dll";  // 期望的DLL位置

            Version version = new Version(0, 0, 0, 0);
            try
            {
                if (File.Exists(text3))
                {
                    version = AssemblyName.GetAssemblyName(text3).Version ?? new Version(0, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read version of our 0Harmony.dll from disk: " + ex.Message);
            }

            // 4. 版本比对：若加载的版本低于磁盘版本，记录错误
            if (text3 != text && version > loadedHarmonyVersion)
            {
                loadingError = "HARMONY LOADING PROBLEM\n\n..." + text;
                if (Regex.IsMatch(text, "data-[0-9A-F]{16}"))
                {
                    loadingError += "\n\nThe path looks like Harmony was loaded from memory...";
                }
                Log.Error(loadingError);
            }
        }

        // 5. 初始化Harmony并应用所有补丁
        try
        {
            new Harmony("net.pardeike.rimworld.lib.harmony").PatchAll();
        }
        catch (Exception ex2)
        {
            Log.Error("Lib.Harmony could not be initialized: " + ex2.Message);
        }
    }
}
```

### 6.2 关键RimWorld API 使用

**✓ RiMCP已验证**

| API | 作用 | 来源 |
|-----|------|------|
| `[StaticConstructorOnStartup]` | 标记静态构造函数在游戏启动时执行 | [Verse.StaticConstructorOnStartup - RiMCP验证] |
| `Mod(ModContentPack content)` | 基类构造函数 | [Verse.Mod - RiMCP验证] |
| `Log.Error(string)` | 记录错误消息 | [Verse.Log - RiMCP验证] |
| `Log.Warning(string)` | 记录警告消息 | [Verse.Log - RiMCP验证] |

### 6.3 补丁应用具体示例

#### 示例1：前缀补丁修改返回值

**来源**：[Environment_GetStackTrace_Patch.cs]

```csharp
[HarmonyPatch(typeof(Environment), "GetStackTrace")]
internal static class Environment_GetStackTrace_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Exception e, bool needFileInfo, ref string __result)
    {
        if (Settings.noStacktraceEnhancing)
        {
            return true;  // 让原方法继续
        }
        try
        {
            StackTrace trace = ((e == null) ? new StackTrace(needFileInfo) : new StackTrace(e, needFileInfo));
            __result = ExceptionTools.ExtractHarmonyEnhancedStackTrace(trace, forceRefresh: false, out var _);
            return false;  // 停止原方法，使用自定义__result
        }
        catch (Exception)
        {
            return true;  // 异常时回退
        }
    }
}
```

**说明**：
- 前缀修改返回值，支持按需启用/禁用
- 异常处理确保补丁失败时不会破坏游戏

#### 示例2：后缀补丁执行额外逻辑

**来源**：[Log_ResetMessageCount_Patch.cs]

```csharp
[HarmonyPatch(typeof(Log), "ResetMessageCount")]
internal static class Log_ResetMessageCount_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ExceptionTools.seenStacktraces.Clear();
    }
}
```

**说明**：
- 后缀在日志计数重置时，清空已看过的堆栈跟踪
- 无参数，无返回值

#### 示例3：后缀补丁扩展UI功能

**来源**：[VersionControl_DrawInfoInCorner_Patch.cs]

```csharp
[HarmonyPatch(typeof(VersionControl), "DrawInfoInCorner")]
internal static class VersionControl_DrawInfoInCorner_Patch
{
    public static void Postfix()
    {
        string text = $"Harmony v{Main.loadedHarmonyVersion}";
        Text.Font = GameFont.Small;
        GUI.color = Color.white.ToTransparent(0.5f);
        Vector2 vector = Text.CalcSize(text);
        Rect rect = new Rect(10f, 58f, vector.x, vector.y);
        Widgets.Label(rect, text);
        GUI.color = Color.white;

        if (Mouse.IsOver(rect))
        {
            TipSignal tip = new TipSignal("Harmony Mod v" + Main.modVersion);
            TooltipHandler.TipRegion(rect, tip);
            Widgets.DrawHighlight(rect);
        }

        if (Main.loadingError != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(Main.loadingError, "OK"));
            Main.loadingError = null;
        }
    }
}
```

**说明**：
- ✓ RiMCP已验证：`RimWorld.VersionControl.DrawInfoInCorner()` 是UI绘制方法
- 后缀补丁在原UI绘制后，添加Harmony版本信息显示
- 包含鼠标悬停提示和错误对话框显示

---

## 7. 设置持久化集成

**来源**：[Settings.cs]

```csharp
public class Settings : ModSettings
{
    [TweakValue("Harmony", 0f, 100f)]
    public static bool noStacktraceCaching;

    [TweakValue("Harmony", 0f, 100f)]
    public static bool noStacktraceEnhancing;

    private static void noStacktraceCaching_Changed()
    {
        Main.settings.Write();
    }

    private static void noStacktraceEnhancing_Changed()
    {
        Main.settings.Write();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref noStacktraceCaching, "noStacktraceCaching", defaultValue: false);
        Scribe_Values.Look(ref noStacktraceEnhancing, "noStacktraceEnhancing", defaultValue: false);
    }
}
```

**关键设计**：
- `[TweakValue]`属性标记可调试的值
- `ExposeData()`实现序列化/反序列化
- 参数变更回调自动保存设置

---

## 8. 关键工具类

### 8.1 AccessTools - 反射辅助工具

**功能**（从类名和使用看）：
- 缓存反射结果提高性能
- 简化类型、方法、字段查询
- 提供类型、构造函数、属性访问

**方法示例**（推断）：
- `AccessTools.GetTypesFromAssembly(assembly)`
- `AccessTools.TypeByName(typeName)`
- `AccessTools.GetOutsideCaller()`

**来源**：[AccessTools.cs存在，在Harmony.cs中被使用]

### 8.2 CodeMatcher - IL指令操作工具

**功能**：高级IL指令匹配和修改

**典型使用场景**：Transpiler补丁中

**来源**：[CodeMatcher.cs、CodeMatch.cs、CodeInstruction.cs]

### 8.3 Traverse - 对象遍历工具

**功能**：动态访问对象属性和字段，避免反射开销

**典型用法**（推断）：
```csharp
var value = Traverse.Create(obj).Field("fieldName").GetValue();
```

**来源**：[Traverse.cs存在]

---

## 9. 最佳实践（来自源代码观察）

### 9.1 补丁类命名规范

**模式**：`[TargetClass]_[TargetMethod]_Patch`

**例子**（来自HarmonyMod）：
- `Environment_GetStackTrace_Patch`
- `Log_ResetMessageCount_Patch`
- `VersionControl_DrawInfoInCorner_Patch`

**优点**：一目了然，易于维护

**来源**：[HarmonyMod中的实际补丁类命名]

### 9.2 异常处理

**原则**（从Environment_GetStackTrace_Patch看）：
- 前缀补丁包含try-catch
- 异常时返回true（回退到原方法）
- 记录警告而非直接抛异常

**来源**：[Environment_GetStackTrace_Patch.cs:16-25]

### 9.3 条件执行

**实现**（从Environment_GetStackTrace_Patch看）：
```csharp
if (Settings.noStacktraceEnhancing)
{
    return true;  // 可配置地启用/禁用补丁效果
}
```

**优点**：允许用户通过设置控制补丁行为

**来源**：[Environment_GetStackTrace_Patch.cs:12-14]

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含所有核心API、属性、补丁方法签名和实例分析 | 2026-01-12 | Knowledge Refiner |

