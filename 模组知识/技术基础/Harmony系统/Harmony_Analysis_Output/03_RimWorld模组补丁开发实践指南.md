# RimWorld模组补丁开发实践指南

## 元信息

- **摘要**：基于Harmony模组实现和RiMCP验证的RimWorld API，提供模组开发者的实际开发指导。包括项目结构、初始化流程、补丁编写、设置系统、错误处理等实践经验。
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：模组初始化，补丁编写，设置管理，UI集成，错误处理
- **标签**：[待审]，基于Harmony源代码实现
- **验证来源**：RiMCP验证的RimWorld API + Harmony源代码

---

## 1. 项目结构设计

### 1.1 标准模组目录结构（基于Harmony）

```
YourModName/
├── About/
│   ├── About.xml          # 模组元数据
│   └── Manifest.xml       # 模组清单
├── LoadFolders.xml        # 版本加载配置
├── Current/               # 当前版本的内容
│   ├── Source/
│   │   └── YourMod/
│   │       ├── YourMod.csproj
│   │       └── (各种.cs文件)
│   └── Assemblies/
│       └── YourMod.dll
├── 1.5/                   # 旧版本兼容
│   ├── Source/
│   └── Assemblies/
└── Readme.md
```

**来源**：[Harmony模组实际目录结构]

### 1.2 About.xml 元数据 [Harmony/About/About.xml - 参考]

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>Your Mod Name</name>
  <author>Author Name</author>
  <packageId>author.modname</packageId>
  <modVersion>1.0.0.0</modVersion>
  <url>https://github.com/your/repo</url>
  <supportedVersions>
    <li>1.2</li>
    <li>1.3</li>
    <li>1.4</li>
    <li>1.5</li>
    <li>1.6</li>
  </supportedVersions>
  <loadBefore>
    <li>Ludeon.RimWorld</li>
  </loadBefore>
  <description>Description of your mod</description>
</ModMetaData>
```

**关键元素**（基于Harmony示例）：
- `packageId` - 模组的唯一标识符（反向域名格式）
- `loadBefore` - 指定在某些模组之前加载
- `supportedVersions` - 支持的RimWorld版本列表

**注意**：如果要使用Harmony库，需要在`modDependencies`中声明依赖

**来源**：[Harmony/About/About.xml]

### 1.3 LoadFolders.xml 版本管理 [Harmony/LoadFolders.xml - 参考]

```xml
<loadFolders>
  <v1.2>
    <li>/</li>
    <li>1.4</li>
  </v1.2>
  <v1.3>
    <li>/</li>
    <li>1.4</li>
  </v1.3>
  <v1.4>
    <li>/</li>
    <li>1.4</li>
  </v1.4>
  <v1.5>
    <li>/</li>
    <li>1.5</li>
  </v1.5>
  <v1.6>
    <li>/</li>
    <li>Current</li>
  </v1.6>
</loadFolders>
```

**逻辑**（来自Harmony实现）：
- 每个RimWorld版本加载多个目录
- 先加载`/`（根目录），再加载版本特定目录
- 后加载的目录覆盖前面的同名文件
- 支持多版本共存

**来源**：[Harmony/LoadFolders.xml]

### 1.4 C#项目配置参考

**关键配置**（基于HarmonyMod.csproj）：

```xml
<PropertyGroup>
  <TargetFramework>net4.7.2</TargetFramework>
  <Platform>x64</Platform>
  <LangVersion>12.0</LangVersion>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>

<ItemGroup>
  <Reference Include="Assembly-CSharp.dll">
    <!-- RimWorld游戏代码 -->
  </Reference>
  <Reference Include="UnityEngine.CoreModule.dll">
    <!-- Unity引擎核心 -->
  </Reference>
</ItemGroup>
```

**说明**：
- `TargetFramework`: net4.7.2是RimWorld的标准.NET版本
- `Platform`: x64（RimWorld仅支持64位）
- `LangVersion`: C#版本号（支持新语言特性）
- `AllowUnsafeBlocks`: 若需要直接指针操作设为true

**来源**：[HarmonyMod.csproj配置项]

---

## 2. 模组初始化流程设计

### 2.1 入口点：Main类

**模板**（基于HarmonyMod.Main）：

```csharp
using HarmonyLib;
using Verse;
using System;
using System.Reflection;

namespace YourModNamespace;

[StaticConstructorOnStartup]
public class Main : Mod
{
    public static Settings settings;
    public static Version loadedHarmonyVersion;
    public static string loadingError;

    static Main()
    {
        // 初始化逻辑：在游戏启动时自动执行
        try
        {
            // 1. 初始化设置
            loadedHarmonyVersion = null;

            // 2. 检查依赖
            // ... 版本检查逻辑 ...

            // 3. 应用补丁
            new Harmony("author.modname.harmony").PatchAll();

            // 4. 初始化其他系统
            // ... 自定义初始化 ...
        }
        catch (Exception ex)
        {
            Log.Error("YourMod initialization failed: " + ex.Message);
        }
    }

    public Main(ModContentPack content)
        : base(content)
    {
        // ✓ RiMCP验证：Mod构造函数初始化
        settings = GetSettings<Settings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        // 模组设置UI绘制（可选）
    }

    public override string SettingsCategory()
    {
        return "Your Mod Name";  // 设置类别名称
    }
}
```

**关键点**：
1. **[StaticConstructorOnStartup]** 属性：指示RimWorld在启动时自动调用静态构造函数
2. **静态构造函数**：真实的初始化代码（创建Harmony实例、应用补丁等）
3. **实例构造函数**：调用GetSettings初始化设置对象
4. **异常处理**：wrap初始化代码以防止崩溃

**来源**：[HarmonyMod/Main.cs:11-70]，[✓ RiMCP验证Verse.Mod和Verse.StaticConstructorOnStartup]

### 2.2 版本检查机制

**实现**（基于HarmonyMod.Main）：

```csharp
static Main()
{
    // 获取已加载的Harmony版本
    string[] HarmonyNames = new string[3] { "0Harmony", "Lib.Harmony", "HarmonyLib" };
    Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
        (Assembly a) => HarmonyNames.Contains(a.GetName().Name));

    if (assembly != null)
    {
        loadedHarmonyVersion = assembly.GetName().Version ?? new Version(0, 0, 0, 0);

        // 获取磁盘上的版本
        string text = assembly.Location;
        string text3 = text.Substring(0, text.LastIndexOfAny(new char[2] { '\\', '/' }) + 1)
            + "0Harmony.dll";

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
            Log.Warning("Could not read version: " + ex.Message);
        }

        // 版本冲突检测
        if (version > loadedHarmonyVersion)
        {
            loadingError = "Version conflict detected...";
            Log.Error(loadingError);
        }
    }
}
```

**原理**：
1. 扫描已加载程序集，找Harmony库
2. 读取磁盘上的Harmony.dll版本
3. 若磁盘版本更新但已加载的是旧版，记录错误
4. 在UI中显示错误信息

**来源**：[HarmonyMod/Main.cs:23-54]

---

## 3. 补丁编写实践

### 3.1 补丁类的标准化编写

**模板**（基于HarmonyMod）：

```csharp
using HarmonyLib;
using YourNamespace;  // 目标类所在命名空间

namespace YourModNamespace;

[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
internal static class TargetClass_TargetMethod_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(/* 参数 */)
    {
        // 补丁逻辑
        return true;  // 继续执行原方法
    }

    [HarmonyPostfix]
    public static void Postfix(/* 参数 */)
    {
        // 后续逻辑
    }
}
```

**命名规范**（基于HarmonyMod实践）：
- 类名：`[TargetClass]_[TargetMethod]_Patch`
- 命名空间：与模组其他代码一致
- 访问修饰符：internal（仅模组内使用）
- static：补丁方法必须是静态的

**来源**：[HarmonyMod中的Environment_GetStackTrace_Patch、Log_ResetMessageCount_Patch等]

### 3.2 前缀补丁实现模式

#### 3.2.1 基础前缀（仅执行额外逻辑）

```csharp
[HarmonyPatch(typeof(Verse.Log), "Message")]
internal static class Log_Message_Patch
{
    [HarmonyPrefix]
    public static void Prefix(string text)
    {
        // 在原方法前执行额外逻辑
        MyLoggingSystem.LogMessage(text);
    }
}
```

**说明**：
- 前缀无返回值则无条件执行
- 原方法无论如何都会执行

#### 3.2.2 条件前缀（可阻止原方法）

```csharp
[HarmonyPatch(typeof(Environment), "GetStackTrace")]
internal static class Environment_GetStackTrace_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Exception e, bool needFileInfo, ref string __result)
    {
        if (ShouldIntercept(e))
        {
            __result = GetCustomStackTrace(e);
            return false;  // 阻止原方法
        }
        return true;  // 继续执行原方法
    }
}
```

**来源**：[Environment_GetStackTrace_Patch.cs:10-26]

**说明**：
- 返回bool类型
- true：原方法继续执行
- false：跳过原方法，使用__result

#### 3.2.3 状态传递前缀

```csharp
[HarmonyPrefix]
public static void Prefix(ref object __state)
{
    __state = new MyState();  // 保存状态给后缀使用
}

[HarmonyPostfix]
public static void Postfix(object __state)
{
    var state = (MyState)__state;
    // 使用前缀传递的状态
}
```

**用途**：前后补丁间共享数据

### 3.3 后缀补丁实现模式

#### 3.3.1 基础后缀（执行额外逻辑）

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

**来源**：[Log_ResetMessageCount_Patch.cs:9-12]

**说明**：
- 无参数版本（最简单）
- 原方法完成后执行

#### 3.3.2 访问原对象和返回值的后缀

```csharp
[HarmonyPostfix]
public static void Postfix(Thing __instance, ref int __result)
{
    // __instance：原方法所属对象
    // __result：原方法返回值（可修改）

    if (__instance is Pawn pawn)
    {
        __result += GetBonusValue(pawn);  // 修改返回值
    }
}
```

#### 3.3.3 异常捕获后缀

```csharp
[HarmonyPostfix]
public static void Postfix(__exception Exception e)
{
    if (e != null)
    {
        Log.Warning("Method failed with exception: " + e.Message);
    }
}
```

**说明**：
- `__exception`参数接收原方法抛出的异常
- 若无异常，该参数为null

### 3.4 错误处理最佳实践

**原则**（基于HarmonyMod实现）：

```csharp
[HarmonyPrefix]
public static bool Prefix(/* 参数 */)
{
    try
    {
        // 补丁逻辑
        return false;  // 阻止原方法（若补丁成功）
    }
    catch (Exception ex)
    {
        // 记录错误但不崩溃
        Log.Warning("Patch failed: " + ex.Message);
        return true;  // 回退到原方法
    }
}
```

**来源**：[Environment_GetStackTrace_Patch.cs:16-25]

**原理**：
- try-catch包装补丁代码
- 异常时返回true让原方法继续
- 记录警告而非直接抛异常
- 确保补丁失败不会破坏游戏

---

## 4. 设置系统集成

### 4.1 设置类设计

**模板**（基于HarmonyMod.Settings）：

```csharp
using Verse;
using LudeonTK;

namespace YourModNamespace;

public class Settings : ModSettings
{
    // 持久化的设置字段
    [TweakValue("YourMod", 0f, 100f)]
    public static bool featureEnabled = true;

    [TweakValue("YourMod", 0f, 1f)]
    public static float featureIntensity = 0.5f;

    // 设置变更回调
    private static void featureEnabled_Changed()
    {
        Main.settings.Write();  // 立即保存设置
    }

    private static void featureIntensity_Changed()
    {
        Main.settings.Write();
    }

    // 序列化实现
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref featureEnabled, "featureEnabled", defaultValue: true);
        Scribe_Values.Look(ref featureIntensity, "featureIntensity", defaultValue: 0.5f);
    }
}
```

**关键设计**（来自HarmonyMod）：
- 继承`ModSettings`
- 使用`[TweakValue]`标记可调试的值
- 在`ExposeData()`中声明要序列化的字段
- 提供变更回调自动保存

**来源**：[Settings.cs]，[✓ RiMCP验证Verse.ModSettings]

### 4.2 设置的条件使用

**实现**（基于HarmonyMod补丁）：

```csharp
[HarmonyPrefix]
public static bool Prefix(/* 参数 */)
{
    if (!Settings.featureEnabled)
    {
        return true;  // 禁用此补丁
    }

    // 使用featureIntensity进行自定义逻辑
    var intensity = Settings.featureIntensity;

    return false;  // 执行自定义逻辑
}
```

**优点**：
- 允许用户通过设置启用/禁用补丁功能
- 支持参数化控制补丁行为

**来源**：[Environment_GetStackTrace_Patch.cs:12-14]

---

## 5. UI集成与信息显示

### 5.1 UI绘制补丁

**实现**（基于VersionControl_DrawInfoInCorner_Patch）：

```csharp
[HarmonyPatch(typeof(VersionControl), "DrawInfoInCorner")]
internal static class VersionControl_DrawInfoInCorner_Patch
{
    public static void Postfix()
    {
        // 准备绘制内容
        string text = $"Your Mod v{Main.modVersion}";

        // 设置UI样式
        Text.Font = GameFont.Small;
        GUI.color = Color.white.ToTransparent(0.5f);

        // 计算绘制区域
        Vector2 vector = Text.CalcSize(text);
        Rect rect = new Rect(10f, 58f, vector.x, vector.y);

        // 绘制标签
        Widgets.Label(rect, text);
        GUI.color = Color.white;

        // 交互逻辑（鼠标悬停）
        if (Mouse.IsOver(rect))
        {
            TipSignal tip = new TipSignal("Your Mod Detailed Info");
            TooltipHandler.TipRegion(rect, tip);
            Widgets.DrawHighlight(rect);
        }

        // 处理错误对话框（若有）
        if (Main.loadingError != null)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(Main.loadingError, "OK"));
            Main.loadingError = null;
        }
    }
}
```

**关键技术**（来自代码）：
- `Text.Font` - 设置字体大小
- `GUI.color` - 设置绘制颜色
- `Text.CalcSize()` - 计算文本大小
- `Rect` - UI矩形区域
- `Widgets.Label()` - 绘制标签
- `Mouse.IsOver()` - 检测鼠标位置
- `TooltipHandler.TipRegion()` - 显示提示
- `Find.WindowStack.Add()` - 显示对话框

**来源**：[VersionControl_DrawInfoInCorner_Patch.cs:10-32]

### 5.2 UI补丁的设计原则

**观察自VersionControl_DrawInfoInCorner_Patch**：
1. **独占式绘制**：后缀补丁在原UI后绘制额外信息
2. **样式统一**：遵循游戏UI风格（颜色、字体等）
3. **交互支持**：提供鼠标悬停提示
4. **模态窗口**：用于显示关键信息（如错误）

---

## 6. 补丁的目标选择策略

### 6.1 目标方法选择原则

**原则1：最小化补丁范围**
- 尽量补丁最小的、最相关的方法
- 避免补丁通用基类方法（影响面太广）

**原则2：选择稳定的公开API**
- 优先补丁public方法
- 避免补丁private实现细节
- 跨版本兼容性更强

**原则3：考虑执行频率**
- 避免补丁Update等高频调用的方法
- 避免性能关键路径的补丁

### 6.2 目标方法查询技巧

**基于Harmony能力**：

```csharp
// 按方法名查询
var method = AccessTools.Method(typeof(TargetClass), "MethodName");

// 按方法名+参数类型查询
var method = AccessTools.Method(typeof(TargetClass), "MethodName",
    new Type[] { typeof(int), typeof(string) });

// 按属性getter查询
var method = AccessTools.PropertyGetter(typeof(TargetClass), "PropertyName");

// 按属性setter查询
var method = AccessTools.PropertySetter(typeof(TargetClass), "PropertyName");

// 查找构造函数
var ctor = AccessTools.Constructor(typeof(TargetClass),
    new Type[] { typeof(int) });
```

---

## 7. 多版本兼容性设计

### 7.1 LoadFolders.xml策略（基于Harmony）

**关键概念**：版本特定的文件覆盖

```xml
<loadFolders>
  <v1.4>
    <li>/</li>         <!-- 通用代码 -->
    <li>1.4</li>       <!-- 1.4特定代码覆盖通用代码 -->
  </v1.4>
  <v1.6>
    <li>/</li>         <!-- 通用代码 -->
    <li>Current</li>   <!-- 最新代码覆盖通用代码 -->
  </v1.6>
</loadFolders>
```

**工作原理**：
1. RimWorld识别版本号（如v1.4或v1.6）
2. 按先后顺序加载相应目录中的文件
3. 后加载的同名文件覆盖先加载的

**优点**：
- 支持多版本在一个模组包中共存
- 大部分代码可共享（放在`/`目录）
- 版本特定代码放在版本目录

**来源**：[Harmony/LoadFolders.xml]

### 7.2 条件编译（C#预处理指令）

**技术**：使用符号实现条件编译

```csharp
#if RIMWORLD_1_4
    // 仅1.4版本编译的代码
#elif RIMWORLD_1_6
    // 仅1.6版本编译的代码
#endif
```

**配置**：在.csproj中定义符号

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);RIMWORLD_1_6</DefineConstants>
</PropertyGroup>
```

---

## 8. 调试与故障排除

### 8.1 启用Harmony调试日志

**方法**：设置环境变量（来自Harmony.cs:23-27）

```
HARMONY_DEBUG=1
```

**输出**：`harmony.log.txt`文件，包含：
- Harmony版本、位置、CLR版本
- 所有补丁应用的详细过程
- 错误和警告信息

### 8.2 查询已应用的补丁

**API**：[PatchProcessor.cs:108-114]

```csharp
var allPatches = PatchProcessor.GetAllPatchedMethods();

foreach (var method in allPatches)
{
    Log.Message($"Patched: {method.FullDescription()}");
}
```

### 8.3 常见错误处理

**错误1：目标方法不存在**
- 原因：目标方法名、参数类型不匹配
- 解决：检查RimWorld源代码确认正确的方法签名

**错误2：版本加载冲突**
- 原因：多个Harmony版本同时加载
- 解决：确保只有一个Harmony模组，检查依赖关系

**错误3：补丁导致游戏崩溃**
- 原因：补丁代码异常未处理
- 解决：包装补丁代码在try-catch中

---

## 9. 性能考虑

### 9.1 补丁执行效率

**观察自Harmony设计**：
- 前缀补丁执行成本：低（bool返回）
- 后缀补丁执行成本：低（void返回）
- Transpiler补丁成本：高（IL代码修改）

**建议**：
- 避免高频调用方法的复杂补丁
- 使用前缀的条件return快速exit
- 考虑缓存计算结果

### 9.2 反射缓存

**Harmony的AccessTools设计**：
- 缓存反射结果提高查询效率
- 避免重复反射相同的类型/方法/字段

**建议**：
- 在初始化时进行反射查询
- 缓存常用的字段/属性访问

---

## 10. 最佳实践总结

| 实践 | 理由 | 来源 |
|------|------|------|
| 补丁类命名`[Class]_[Method]_Patch` | 易于识别和维护 | HarmonyMod实例 |
| 补丁代码包含异常处理 | 防止补丁失败导致游戏崩溃 | Environment_GetStackTrace_Patch |
| 使用设置条件启用/禁用补丁 | 让用户自主控制补丁功能 | HarmonyMod.Settings |
| 优先补丁public方法 | 跨版本兼容性更强 | 补丁设计原则 |
| 避免补丁高频调用方法 | 性能考虑 | Harmony设计理念 |
| 版本特定代码分离到版本目录 | 支持多版本共存 | LoadFolders.xml |

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含项目结构、初始化流程、补丁编写、设置系统、UI集成、多版本支持等实践指导 | 2026-01-12 | Knowledge Refiner |

