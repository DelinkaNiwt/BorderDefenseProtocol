# Harmony技术架构与设计模式深度分析

## 元信息

- **摘要**：从源代码层面分析Harmony的底层实现、关键设计模式、线程安全机制、补丁生成过程以及跨平台支持策略
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：IL补丁，设计模式，线程安全，Mono.Cecil，MonoMod，跨平台
- **标签**：[待审]，深度技术分析，源代码提取
- **源代码位置**：Harmony 2.4.2.0完整源代码

---

## 1. 核心设计模式分析

### 1.1 链式调用（Fluent Interface）模式

**实现位置**：[PatchProcessor.cs:36-82]

```csharp
public class PatchProcessor
{
    public PatchProcessor AddPrefix(HarmonyMethod prefix)
    {
        this.prefix = prefix;
        return this;  // 关键：返回this
    }

    public PatchProcessor AddPostfix(HarmonyMethod postfix)
    {
        this.postfix = postfix;
        return this;  // 关键：返回this
    }

    // ... 其他Add方法 ...

    public MethodInfo Patch()
    {
        // 应用补丁
    }
}
```

**使用示例**：
```csharp
harmony.CreateProcessor(method)
    .AddPrefix(prefixMethod)
    .AddPostfix(postfixMethod)
    .AddTranspiler(transpilerMethod)
    .Patch();
```

**优点**：
1. **可读性强** - 声明式风格，清晰表达补丁配置
2. **灵活扩展** - 支持按需添加补丁类型
3. **简洁代码** - 避免重复变量赋值

**来源**：[PatchProcessor.cs中所有Add*方法]

### 1.2 属性驱动（Attribute-Based）模式

**实现位置**：[HarmonyPatch.cs, HarmonyPrefix.cs, HarmonyPostfix.cs等]

```csharp
[HarmonyPatch(typeof(TargetClass), "Method")]
internal static class TargetClass_Method_Patch
{
    [HarmonyPrefix]
    public static bool Prefix() { }

    [HarmonyPostfix]
    public static void Postfix() { }
}
```

**自动扫描机制**：[Harmony.cs:83-89]

```csharp
public void PatchAll(Assembly assembly)
{
    AccessTools.GetTypesFromAssembly(assembly).DoIf(
        (Type type) => type.HasHarmonyAttribute(),  // 检查属性
        delegate(Type type)
        {
            CreateClassProcessor(type).Patch();  // 自动处理
        });
}
```

**优点**：
1. **声明式编程** - 属性明确表示意图
2. **自动扫描** - 无需手动注册补丁
3. **扩展性** - 支持添加新属性类型（HarmonyBefore, HarmonyAfter等）

**来源**：[HarmonyPatch及相关属性定义，Harmony.PatchAll方法]

### 1.3 处理器模式（Processor Pattern）

**两层处理器架构**：

```
Harmony实例
  ├─ PatchClassProcessor (类级别处理)
  │   └─ 扫描类中的所有补丁方法
  │       └─ 为每个补丁方法创建HarmonyMethod
  │           └─ 交给PatchProcessor处理
  │
  └─ PatchProcessor (方法级别处理)
      ├─ 汇总单个方法的所有补丁
      ├─ 调用PatchFunctions生成新方法
      └─ 更新全局补丁信息
```

**职责分离**：
- `PatchClassProcessor` - 类级别的遍历和组织
- `PatchProcessor` - 单个方法的补丁应用

**来源**：[Harmony.cs:73-76, PatchClassProcessor的存在，PatchProcessor的职责]

### 1.4 观察者模式（变形）

**优先级系统**：[文件HarmonyPriority.cs, HarmonyBefore.cs, HarmonyAfter.cs的存在]

```csharp
[HarmonyPatch(typeof(TargetClass), "Method")]
[HarmonyBefore("other.mod.harmony")]  // 在某个补丁前执行
[HarmonyAfter("another.mod.harmony")]  // 在某个补丁后执行
public static class MyPatch { }
```

**实现原理**（推断）：
- 补丁有优先级和前后依赖关系
- `PatchSorter`类（存在但未读）负责排序补丁执行顺序
- 形成有向图处理依赖关系

**来源**：[HarmonyPriority.cs, HarmonyBefore.cs, HarmonyAfter.cs, PatchSorter.cs]

---

## 2. 线程安全设计

### 2.1 全局补丁锁

**实现**：[PatchProcessor.cs:28]

```csharp
internal static readonly object locker = new object();
```

**使用范围**：[PatchProcessor.Patch(), Unpatch()等]

```csharp
public MethodInfo Patch()
{
    lock (locker)  // 所有补丁操作都在锁内
    {
        PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original) ?? new PatchInfo();
        patchInfo.AddPrefixes(instance.Id, prefix);
        // ... 其他补丁添加 ...
        MethodInfo methodInfo = PatchFunctions.UpdateWrapper(original, patchInfo);
        HarmonySharedState.UpdatePatchInfo(original, methodInfo, patchInfo);
        return methodInfo;
    }
}
```

**设计原理**：
1. **单一全局锁** - 所有补丁操作共享一个锁
2. **原子性** - 从获取补丁信息到更新全局状态是一个原子操作
3. **防止竞争** - 避免多个线程同时修改同一个方法的补丁

**来源**：[PatchProcessor.cs:116-140]

### 2.2 共享状态管理

**HarmonySharedState**（推断）：
- 维护全局的补丁信息映射
- 记录已补丁的方法及其补丁列表
- `GetPatchInfo(MethodBase)` - 获取方法的补丁信息
- `UpdatePatchInfo(MethodBase, MethodInfo, PatchInfo)` - 更新补丁信息

**来源**：[PatchProcessor中对HarmonySharedState的调用]

### 2.3 日志系统的线程安全

**LogLock类**：[Log.cs中的内嵌类]

```csharp
public class LogLock : IDisposable
{
    public LogLock()
    {
        logDisablers = Interlocked.Increment(ref logDisablers);
    }

    public void Dispose()
    {
        logDisablers = Interlocked.Decrement(ref logDisablers);
    }
}
```

**原理**：
- 使用`Interlocked`原子操作
- 支持嵌套使用（引用计数）
- 配合`using`语句实现自动释放

**来源**：[Verse.Log.cs - RiMCP验证]

---

## 3. 补丁生成核心流程

### 3.1 补丁应用的四个阶段

**阶段1：信息收集** [PatchProcessor.Patch():116-125]

```csharp
if ((object)original == null)
{
    throw new NullReferenceException("Null method for " + instance.Id);
}
if (!original.IsDeclaredMember())
{
    MethodBase declaredMember = original.GetDeclaredMember();
    throw new ArgumentException("You can only patch implemented methods/constructors.");
}
```

**验证**：
- 原方法不能为null
- 原方法必须是已实现的（不能是接口或抽象方法）

**阶段2：补丁汇总** [PatchProcessor.Patch():129-136]

```csharp
lock (locker)
{
    PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original) ?? new PatchInfo();
    patchInfo.AddPrefixes(instance.Id, prefix);
    patchInfo.AddPostfixes(instance.Id, postfix);
    patchInfo.AddTranspilers(instance.Id, transpiler);
    patchInfo.AddFinalizers(instance.Id, finalizer);
    patchInfo.AddInnerPrefixes(instance.Id, innerprefix);
    patchInfo.AddInnerPostfixes(instance.Id, innerpostfix);
```

**汇总机制**：
- PatchInfo保存该方法的所有补丁
- 支持多个Harmony实例的补丁共存
- 每个实例ID对应一组补丁

**阶段3：方法重写** [PatchProcessor.Patch():136]

```csharp
MethodInfo methodInfo = PatchFunctions.UpdateWrapper(original, patchInfo);
```

**核心操作**：
- `PatchFunctions`生成新的方法体
- 新方法体包含：前缀→原方法→后缀的逻辑
- 返回修改后的MethodInfo

**阶段4：全局更新** [PatchProcessor.Patch():137-138]

```csharp
HarmonySharedState.UpdatePatchInfo(original, methodInfo, patchInfo);
return methodInfo;
```

**更新**：
- 将新MethodInfo与补丁信息存储到全局状态
- 后续查询能获取最新的补丁信息

**来源**：[PatchProcessor.cs:116-140]

### 3.2 执行流程序列图

```
补丁执行顺序（基于源代码推断）：
  1. 前缀补丁们执行（若有多个，按优先级排序）
     ├─ 若前缀返回false，跳到步骤4
     └─ 若前缀返回true，继续

  2. 原方法执行
     ├─ 若抛异常，进入异常处理
     └─ 若正常返回，继续

  3. 后缀补丁们执行（若有多个，按优先级排序）
     ├─ 访问__result修改返回值
     ├─ 访问__state获取前缀传递的状态
     └─ 访问__exception捕获异常信息

  4. 最终返回值
```

**来源**：[PatchProcessor中的补丁类型定义和执行逻辑推断]

---

## 4. 依赖库与底层实现

### 4.1 Mono.Cecil 的角色

**功能**：IL（中间语言）的读写和操作

**包含的关键命名空间**：

| 命名空间 | 功能 |
|---------|------|
| `Mono.Cecil` | 核心API - 加载程序集、读写元数据 |
| `Mono.Cecil.Cil` | IL操作 - CodeInstruction的底层实现 |
| `Mono.Cecil.Metadata` | 元数据表操作 |
| `Mono.Cecil.PE` | PE文件格式处理 |
| `Mono.Cecil.Pdb / .Mdb` | 调试符号支持 |

**在Harmony中的使用**（推断）：
- 读取原方法的IL指令序列
- 构造新的IL指令序列（包含补丁逻辑）
- 修改方法的元数据信息
- 生成更新后的程序集

**来源**：[Mono.Cecil相关目录，PatchFunctions的调用]

### 4.2 MonoMod 框架的关键模块

#### MonoMod.Core - 平台抽象层

```
MonoMod.Core/
├─ Interop/               (互操作性)
│  └─ 调用非托管代码
├─ Platforms/
│  ├─ Architectures/      (x86/x64/ARM支持)
│  ├─ Memory/             (内存管理)
│  ├─ Runtimes/           (.NET/Mono/Core运行时)
│  └─ Systems/            (Windows/Linux/macOS OS接口)
└─ Utils/                 (工具函数)
```

**设计目的**：
- 隐藏平台差异
- 统一的低层接口
- 支持多个运行时环境

**来源**：[MonoMod.Core目录结构]

#### MonoMod.Cil - IL操作高级接口

**提供**：
- 高级IL操作API
- 对Mono.Cecil的包装和扩展
- 便利的代码转换工具

**来源**：[MonoMod.Cil目录]

### 4.3 跨平台本地库集成

**嵌入式本地库**：

| 库文件 | 目标平台 | 用途 |
|--------|---------|------|
| `exhelper_linux_x86_64.so` | Linux x86_64 | 底层内存操作 |
| `exhelper_linux_arm64.so` | Linux ARM64 | 底层内存操作 |
| `exhelper_macos_x86_64.dylib` | macOS x86_64 | 底层内存操作 |
| `exhelper_macos_arm64.dylib` | macOS ARM64 | 底层内存操作 |

**集成方式**（推断）：
- 编译时嵌入到程序集
- 运行时根据平台动态加载
- 通过P/Invoke调用

**来源**：[Source/0Harmony/ 中的.so和.dylib文件]

---

## 5. 高级补丁技术

### 5.1 Transpiler 补丁（IL级别修改）

**功能**：直接修改方法的IL指令

**签名**（推断）：

```csharp
[HarmonyTranspiler]
public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var codes = new List<CodeInstruction>(instructions);
    // 修改codes列表
    return codes;
}
```

**工具类**：
- `CodeInstruction` - 表示单个IL指令
- `CodeMatcher` - 指令匹配和修改
- `CodeMatch` - 匹配模式定义

**复杂度**：高，需要理解IL指令集

**来源**：[CodeInstruction.cs, CodeMatcher.cs, CodeMatch.cs]

### 5.2 Finalizer 补丁（异常处理）

**功能**：捕获原方法或其他补丁抛出的异常

**签名**（推断）：

```csharp
[HarmonyFinalizer]
public static Exception Finalizer(Exception __exception)
{
    if (__exception != null)
    {
        // 处理异常
        Log.Error("Exception: " + __exception.Message);
        return __exception;  // 继续抛异常
        // return null;       // 吃掉异常
    }
    return null;
}
```

**作用**：
- 记录异常而不中断游戏
- 可选择是否继续抛异常
- 修改异常类型或信息

**来源**：[HarmonyFinalizer.cs]

### 5.3 ReversePatcher（反向补丁）

**概念**：从原方法提取代码到自定义方法

**使用场景**：
- 调用游戏代码的私有方法
- 完整复制原方法供修改使用

**API**：[Harmony.cs:78-81]

```csharp
public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)
{
    return new ReversePatcher(this, original, standin);
}
```

**来源**：[ReversePatcher.cs存在但未详细读取]

---

## 6. 调试与诊断支持

### 6.1 DEBUG标志和日志

**激活条件**：[Harmony.cs:23-27]

```csharp
string environmentVariable = Environment.GetEnvironmentVariable("HARMONY_DEBUG");
if (environmentVariable != null && environmentVariable.Length > 0)
{
    environmentVariable = environmentVariable.Trim();
    DEBUG = environmentVariable == "1" || bool.Parse(environmentVariable);
}
```

**日志输出**：[Harmony.cs:33-57]

```csharp
if (DEBUG)
{
    Assembly assembly = typeof(Harmony).Assembly;
    Version version = assembly.GetName().Version;
    string value = assembly.Location;
    string value2 = Environment.Version.ToString();
    string value3 = Environment.OSVersion.Platform.ToString();

    FileLog.Log($"### Harmony id={id}, version={version}, location={value}, env/clr={value2}, platform={value3}");

    MethodBase outsideCaller = AccessTools.GetOutsideCaller();
    if ((object)outsideCaller.DeclaringType != null)
    {
        // 输出调用方信息
    }
}
```

**输出信息**：
- Harmony版本
- 库位置
- CLR版本
- 操作系统
- 调用方信息

### 6.2 HarmonyException

**文件**：[HarmonyException.cs - 存在]

**用途**（推断）：
- 包装Harmony相关的异常
- 提供更清晰的错误信息
- 支持堆栈跟踪增强

### 6.3 FileLog 诊断日志

**文件**：[FileLog.cs - 存在]

**功能**（推断）：
- 写入文件的日志系统
- 方便离线分析
- 记录补丁应用过程

---

## 7. 性能优化策略

### 7.1 反射缓存

**AccessTools 的缓存机制**：[AccessTools.cs, AccessCache.cs]

**缓存对象**（推断）：
- 类型查询结果
- 方法信息
- 字段信息
- 属性信息
- 构造函数

**优点**：
- 避免重复反射查询
- 提高类型系统性能
- 特别是在高频调用中有明显效果

**来源**：[AccessCache.cs的存在，AccessTools的使用]

### 7.2 编译优化

**C#编译器优化**：

```xml
<PropertyGroup>
  <Optimize>true</Optimize>  <!-- Release编译优化 -->
</PropertyGroup>
```

**JIT优化**：
- 运行时IL代码编译为本机代码
- 热路径优化
- 内联优化

### 7.3 执行效率对比

**补丁类型性能对比**（基于设计）：

| 补丁类型 | 执行成本 | 原因 |
|---------|---------|------|
| 前缀 | 低 | bool返回，快速path |
| 后缀 | 低 | void返回，顺序执行 |
| Transpiler | 中 | IL代码遍历修改 |
| 异常处理 | 中 | 异常路径特殊处理 |
| 反向补丁 | 中等 | 方法复制 |

---

## 8. 跨版本兼容性策略

### 8.1 程序集版本管理

**Harmony模组的版本支持**：[About/About.xml]

```xml
<modVersion>2.4.2.0</modVersion>
<supportedVersions>
  <li>1.2</li>
  <li>1.3</li>
  <li>1.4</li>
  <li>1.5</li>
  <li>1.6</li>
</supportedVersions>
```

**设计意图**：
- 单一Harmony程序集支持多个版本
- 加载配置由RimWorld处理
- 模组可声明支持的版本范围

### 8.2 加载文件夹机制

**LoadFolders.xml的作用**：[LoadFolders.xml]

```xml
<v1.6>
  <li>/</li>        <!-- 基础文件（所有版本共用） -->
  <li>Current</li>  <!-- 1.6特定文件（覆盖基础） -->
</v1.6>
```

**运行时行为**：
- RimWorld读取`<v1.6>`配置
- 依次加载`/`和`Current`目录的文件
- 同名文件后加载的覆盖先加载的
- 支持精细控制各版本的差异

### 8.3 编译时条件编译（推荐做法）

**DefineConstants 控制**：

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
  <DefineConstants>$(DefineConstants);RIMWORLD_1_6</DefineConstants>
</PropertyGroup>
```

**代码中使用**：

```csharp
#if RIMWORLD_1_6
    var newFeature = new Verse.SomeNewThing();
#else
    // 旧版本的实现
#endif
```

---

## 9. 扩展点设计

### 9.1 属性系统的扩展性

**现有属性**（来自文件列表）：
- HarmonyPatch
- HarmonyPrefix, HarmonyPostfix, HarmonyTranspiler, HarmonyFinalizer
- HarmonyPriority
- HarmonyBefore, HarmonyAfter
- HarmonyArgument
- HarmonyDelegate
- HarmonyException
- HarmonyMethod
- 其他...

**扩展点**：可添加新属性而无需修改核心逻辑

### 9.2 处理器系统的扩展性

**现有处理器**：
- PatchProcessor（方法级）
- PatchClassProcessor（类级）
- ReversePatcher（反向补丁）

**扩展可能**：可创建自定义处理器处理特殊补丁场景

---

## 10. 总结：Harmony 的架构精妙之处

### 10.1 清晰的分层设计

三层架构（应用→框架→底层）清晰地分离职责，便于理解和维护。

### 10.2 强大的属性驱动系统

属性标记补丁意图，框架自动扫描和应用，大大简化使用。

### 10.3 灵活的链式API

PatchProcessor的链式调用支持灵活的补丁组合。

### 10.4 细致的线程安全保护

全局锁确保多线程环境下的数据一致性。

### 10.5 多层次的补丁支持

前缀、后缀、转换器、异常处理等多种补丁类型支持从简单到复杂的场景。

### 10.6 完整的跨平台支持

嵌入式本地库和平台抽象层支持Windows、Linux、macOS。

### 10.7 优秀的调试和诊断能力

DEBUG日志、异常处理、补丁查询等工具支持开发和故障排查。

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含设计模式、线程安全、补丁生成流程、依赖库分析、跨平台支持等深度技术分析 | 2026-01-12 | Knowledge Refiner |

