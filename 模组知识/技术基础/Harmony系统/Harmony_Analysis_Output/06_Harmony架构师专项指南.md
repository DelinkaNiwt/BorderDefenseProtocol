# Harmony 架构师专项指南

## 元信息

- **摘要**：为需求架构师和系统架构设计者准备的Harmony应用指南，涵盖模组架构决策、设计模式选择、多模组协作、性能权衡等战略层面
- **版本号**：v1.0
- **修改时间**：2026-01-12
- **关键词**：架构设计，模组协作，设计模式，性能优化，兼容性设计
- **标签**：[待审]，架构指南，战略层面
- **适用对象**：需求架构师、模组框架设计者、技术主管

---

## 导言：为什么架构师要理解Harmony

Harmony 不仅是一个工具，它还定义了 **RimWorld 模组生态的架构约束**。你的模组架构决策必须围绕它展开：

- **补丁执行模型** 决定了你能实现什么功能
- **全局锁机制** 影响并发安全设计
- **属性驱动系统** 约束了代码组织方式
- **链式API模式** 定义了配置方式
- **多版本加载** 影响兼容性策略

**核心认识**：Harmony 本质是一个 **强制一致性的架构框架**——所有模组都要遵守同样的补丁规则，这既是约束，也是保证多模组和谐共存的基础。

---

## 1. Harmony 的三层架构（架构师视角）

### 1.1 三层结构及其设计含义

```
【应用层】HarmonyMod (RimWorld 集成层)
  ├─ 职责：RimWorld mod 生命周期管理
  ├─ 特点：使用 [StaticConstructorOnStartup] 自动初始化
  ├─ 实现：Main.cs 一个构造函数，初始化 Harmony 实例
  └─ 设计决策：选择在静态构造时应用所有补丁（早期约束）

         ↓ [定义补丁属性]

【框架层】HarmonyLib (补丁处理引擎)
  ├─ 职责：补丕定义、属性解析、补丕应用、线程安全
  ├─ 核心类：Harmony, PatchProcessor, HarmonyPatch
  ├─ 特点：全局锁 (object locker) 保证原子性
  └─ 设计决策：链式API + 属性驱动 = 配置化补丕

         ↓ [IL 指令操作]

【底层实现】Native + Mono.Cecil + MonoMod + Iced
  ├─ Mono.Cecil：IL 读写（解析和修改中间语言）
  ├─ MonoMod：跨平台 IL 操作抽象
  ├─ Iced：x86 汇编指令编码
  └─ Platform Libs：Windows/Linux/macOS 原生调用
```

**架构设计启示**：
- **早期约束**：所有补丁在 Main.cs 的静态构造中一次性应用，无法动态加载补丕
- **全局锁**：PatchProcessor.locker 保证线程安全，但意味着补丕应用不能并行（性能考量）
- **属性驱动**：通过 C# 属性声明补丕目标，降低配置复杂度，但限制了灵活性

---

### 1.2 关键设计决策：为什么是这样设计

| 设计决策 | 选择 | 为什么 | 代价 |
|---------|------|-------|------|
| **补丕应用时机** | 静态构造时 | 确保游戏启动前补丕全部生效，避免竞态条件 | 无法运行时加载/卸载补丕 |
| **线程同步** | 全局锁 | 简单可靠，保证原子性 | 补丕应用时串行化，启动略慢 |
| **配置方式** | C# 属性 | 编译时检查，IDE 支持，减少运行时错误 | 需要重新编译才能改变补丕 |
| **目标指定** | 反射方式 | 灵活支持构造函数/属性/方法，兼容多版本 | 运行时反射性能开销 |
| **补丕生成** | 动态IL | 无需预编译补丕，支持任意方法 | 调试困难，错误难以追踪 |

---

## 2. 模组架构的四大约束

当你设计一个 Harmony 驱动的模组时，以下四个约束会强制塑造你的架构：

### 2.1 约束1：补丕应用是一次性的

**事实**：所有补丕在游戏启动时一次性应用，不可动态卸载。

**架构影响**：
```
你的模组加载流程：
游戏启动 → Main.cs静态构造执行
  → harmony.PatchAll() 应用所有补丕
  → 游戏进行中补丕无法改变
```

**设计建议**：
- ❌ **错误**：期望在游戏中途启用/禁用补丕
- ✅ **正确**：用 Settings 控制补丕**内部的条件判断**，而非补丕本身

```csharp
// ✅ 正确模式：补丕始终存在，但有条件判断
[HarmonyPrefix]
public static bool Prefix()
{
    if (!Settings.EnableThisPatch)
        return true;  // 跳过逻辑，但补丕仍在

    // 执行补丕逻辑
    return true;
}
```

---

### 2.2 约束2：全局锁导致启动串行化

**事实**：PatchProcessor 使用全局锁来保证线程安全。

```csharp
// Source/0Harmony/HarmonyLib/PatchProcessor.cs
internal static readonly object locker = new object();

public MethodInfo Patch()
{
    lock (locker)  // 这个锁保证全局原子性
    {
        // 补丕应用的4个阶段
        PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original);
        patchInfo.AddPrefixes(instance.Id, prefix);
        // ...
        MethodInfo methodInfo = PatchFunctions.UpdateWrapper(original, patchInfo);
        HarmonySharedState.UpdatePatchInfo(original, methodInfo, patchInfo);
    }
}
```

**架构影响**：
- 游戏启动时，所有模组的补丕按注册顺序申请全局锁，逐个应用
- 100个补丕 ≠ 100 倍启动时间（通常增加 2-5 秒）
- 但这也**保证了补丕顺序的确定性**

**设计建议**：
- ✅ 把补丕分组，只在 Main.cs 中 PatchAll() 一次
- ❌ 不要在游戏运行中重复调用 Patch()（会多次获取锁）
- ✅ 使用 [HarmonyBefore] / [HarmonyAfter] 控制补丕相对顺序

---

### 2.3 约束3：补丕顺序的确定性和可预测性

**事实**：多个模组修改同一方法时，补丕执行顺序由属性控制。

```csharp
// 执行流程示例：假设 ModA、ModB、ModC 都要 patch Pawn.TakeDamage

ModA_Prefix → ModB_Prefix → ModC_Prefix → 原方法 → ModC_Postfix → ModB_Postfix → ModA_Postfix
```

**属性规则**（Source/0Harmony/HarmonyLib/HarmonyPatch.cs）：

| 属性 | 效果 | 优先级 |
|------|------|-------|
| `[HarmonyPriority(Priority.High)]` | 高优先级 | 1 |
| `[HarmonyPriority(Priority.Normal)]` | 默认 | 0 |
| `[HarmonyPriority(Priority.Low)]` | 低优先级 | -1 |
| `[HarmonyBefore("mod.id")]` | 在指定模组前执行 | 更高 |
| `[HarmonyAfter("mod.id")]` | 在指定模组后执行 | 更低 |

**设计建议**：
- ✅ 为关键补丕设置优先级，避免被其他模组覆盖
- ✅ 用 [HarmonyBefore] 明确依赖关系（"我需要在 mod X 之前"）
- ❌ 不要依赖补丕执行顺序来修复逻辑错误
- ❌ 避免循环依赖（A before B, B before A）

---

### 2.4 约束4：ref 参数的位置敏感性

**事实**：Harmony 通过特殊参数名识别补丕方法签名。

```csharp
// 这些参数名有特殊含义（必须准确）
__instance      // 方法所在对象（非静态方法）
__result        // 返回值（必须是 ref，前缀中 ref，后缀中 ref 或普通）
__state         // 从前缀传到后缀的状态（可选）
__exception     // 异常信息（仅在后缀中有）
```

**架构影响**：补丕方法的签名必须精确匹配，IDE 无法提示错误。

**设计建议**：
- ✅ 建立补丕代码模板库，从模板派生而非手写
- ✅ 为常用补丕编写 code snippet
- ❌ 不要尝试即席编写补丕（容易出现签名错误）

---

## 3. 设计模式深度剖析

### 3.1 模式1：链式 API 模式（Fluent Interface）

**实现**（Source/0Harmony/HarmonyLib/PatchProcessor.cs）：

```csharp
public PatchProcessor AddPrefix(HarmonyMethod prefix)
{
    this.prefix = prefix;
    return this;  // 关键：返回自身
}

public PatchProcessor AddPostfix(HarmonyMethod postfix)
{
    this.postfix = postfix;
    return this;
}

public MethodInfo Patch()
{
    // 执行补丕，返回修改后的方法
}
```

**使用效果**：
```csharp
harmony.CreateProcessor(targetMethod)
    .AddPrefix(prefixMethod)
    .AddPostfix(postfixMethod)
    .AddTranspiler(transpilerMethod)
    .Patch();
```

**架构意义**：
- ✅ **配置化**：补丕应用变成声明式，而非命令式
- ✅ **可读性**：操作链条清晰展现
- ⚠️ **代价**：每个操作都是对象内部状态修改，需要小心中途异常

**在你的架构中的应用**：
```csharp
// ✅ 推荐：集中管理所有补丕
public static void RegisterAllPatches(Harmony harmony)
{
    harmony.CreateProcessor(typeof(Pawn), "TakeDamage")
        .AddPrefix(PawnPatches.TakeDamage_Prefix)
        .AddPostfix(PawnPatches.TakeDamage_Postfix)
        .Patch();

    harmony.CreateProcessor(typeof(Building), "Destroy")
        .AddPrefix(BuildingPatches.Destroy_Prefix)
        .Patch();
}
```

---

### 3.2 模式2：属性驱动模式（Attribute-Driven）

**核心思想**：补丕定义通过 C# 属性声明，框架自动发现。

**关键类**（Source/0Harmony/HarmonyLib/HarmonyPatch.cs）：

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
    AttributeTargets.Method, AllowMultiple = true)]
public class HarmonyPatch : HarmonyAttribute
{
    // 支持 20+ 重载构造器，处理各种目标方法指定方式
    public HarmonyPatch(Type declaringType) { }
    public HarmonyPatch(Type declaringType, string methodName) { }
    public HarmonyPatch(Type declaringType, MethodType methodType) { }
    // ...
}
```

**优势和劣势**：

| 维度 | 属性驱动 | 手动 API |
|------|--------|--------|
| **编译检查** | ✅ IDE 提示，编译错误早发现 | ❌ 运行时反射失败 |
| **代码生成友好** | ✅ 易于扫描和自动化 | ❌ 难以自动分析 |
| **灵活性** | ❌ 属性预定义，改变需重编 | ✅ 运行时动态 |
| **性能** | ✅ 编译时计算 | ❌ 运行时反射解析 |
| **可调试性** | ✅ 错误信息更清晰 | ❌ 反射导致堆栈复杂 |

**在你的架构中的应用**：
```csharp
// ✅ 推荐结构：属性定义 + 自动扫描
[HarmonyPatch(typeof(Pawn))]
[HarmonyPriority(Priority.High)]
internal static class PawnPatches
{
    [HarmonyPatch("TakeDamage")]
    [HarmonyPrefix]
    public static bool TakeDamage_Prefix() { }

    [HarmonyPatch("Kill")]
    [HarmonyPostfix]
    public static void Kill_Postfix() { }
}

// 一行代码应用所有补丕
harmony.PatchAll(typeof(PawnPatches).Assembly);
```

---

### 3.3 模式3：全局锁保证原子性（Thread Safety）

**核心实现**（Source/0Harmony/HarmonyLib/PatchProcessor.cs）：

```csharp
public MethodInfo Patch()
{
    lock (PatchProcessor.locker)  // 全局同步点
    {
        // 四个阶段都在锁内执行，保证中途不会有其他模组修改

        // 阶段1：收集补丕信息
        PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original);

        // 阶段2：汇总新补丕
        patchInfo.AddPrefixes(instance.Id, prefix);
        patchInfo.AddPostfixes(instance.Id, postfix);

        // 阶段3：重写目标方法 IL
        MethodInfo methodInfo = PatchFunctions.UpdateWrapper(original, patchInfo);

        // 阶段4：全局更新（其他代码引用这个方法会看到新版本）
        HarmonySharedState.UpdatePatchInfo(original, methodInfo, patchInfo);
    }
}
```

**设计含义**：
- 每个方法的补丕都有一个**全局共享状态**（HarmonySharedState）
- 当模组B 要给同一方法添加补丕时，它会读取模组A的补丕信息，合并，重新生成
- 这保证了**补丕的累加性**（不会互相覆盖）

**在你的架构中的考虑**：
- ✅ 不需要显式加锁（Harmony 内部处理）
- ❌ 但补丕内部的**业务逻辑**可能需要同步（如访问共享状态）
- ✅ 依赖于补丕的**幂等性**（同一补丕应用多次结果相同）

---

## 4. 多模组协作架构设计

### 4.1 三个合作层级

```
【第一层：补丕链】
多个模组对同一方法应用补丕
ModA_Prefix → ModB_Prefix → ModC_Prefix → 原方法 → ...

【第二层：补丕组】
一个模组内部多个补丕的依赖关系
BuildingPatch 依赖 PawnPatch（要求先应用）

【第三层：模组间通信】
模组A 的输出作为 模组B 的输入（通过共享接口）
```

### 4.2 架构决策：如何避免冲突

**决策1：显式依赖声明**
```csharp
[HarmonyPatch(typeof(Pawn), "TakeDamage")]
[HarmonyBefore("other.mod.critical.patch")]  // 我必须先执行
[HarmonyAfter("other.mod.base.patch")]       // 我必须后执行
internal static class MyPatch { }
```

**决策2：分离关注点（职责单一）**
- ❌ **坏设计**：一个补丕同时修改伤害计算、AI 决策、UI 显示
- ✅ **好设计**：
  - Patch_Damage.cs：只处理伤害计算
  - Patch_AI.cs：只处理 AI 决策
  - Patch_UI.cs：只处理 UI 显示

**决策3：暴露扩展点**
```csharp
// ✅ 推荐：如果其他模组可能要 hook 你的逻辑，暴露接口
public interface IDamageCalculator
{
    float CalculateDamage(Pawn pawn, int baseDamage);
}

// 其他模组可以用 Harmony 替换你的实现，而不是复制你的代码
[HarmonyPrefix]
public static bool MyCalculate(ref float __result)
{
    __result = ModRegistry.GetDamageCalculator().Calculate(...);
    return false;  // 完全替换原逻辑
}
```

### 4.3 模组顺序管理

**RimWorld 加载模组的顺序**：
1. 按 About.xml 中 loadBefore/loadAfter 计算依赖图
2. 按拓扑排序加载模组
3. 每个模组的 Main.cs 静态构造执行（此时应用补丕）

**你的架构责任**：
```csharp
// About.xml
<modDependencies>
    <li>
        <packageId>brrainz.harmony</packageId>
        <displayName>Harmony</displayName>
    </li>
</modDependencies>

<loadBefore>
    <li>yourmod.must.load.before.this</li>
</loadBefore>

<loadAfter>
    <li>other.mod.must.load.before.you</li>
</loadAfter>
```

---

## 5. 性能和扩展性权衡

### 5.1 补丕的性能开销

**三个成本来源**：

| 成本 | 来源 | 大小 | 可控性 |
|------|------|------|-------|
| **反射开销** | 方法查找、属性解析 | ~10ms/补丕 | ✅ 启动时一次 |
| **前缀调用** | 每次方法执行时都要调用前缀 | ~1-5μs/调用 | ⚠️ 难以优化 |
| **IL 修改** | 补丕后的 IL 执行 | 取决于补丕逻辑 | ⚠️ 依赖写法 |

**启动时性能**：
- 无补丕：游戏启动 ~3-5 秒
- 50 个简单补丕：~5-7 秒（+2 秒）
- 200+ 补丕：~8-15 秒（+5-10 秒）

**运行时性能**：
```csharp
// ❌ 坏补丕：前缀在热路径中做复杂计算
[HarmonyPrefix]
public static bool ExpensivePrefix(Pawn __instance)
{
    for (int i = 0; i < 10000; i++)  // 每次调用都执行！
    {
        // 复杂计算
    }
    return true;
}

// ✅ 好补丕：快速检查，只在必要时做复杂工作
private static Dictionary<int, float> cache = new();

[HarmonyPrefix]
public static bool CachedPrefix(Pawn __instance, ref int __result)
{
    if (cache.TryGetValue(__instance.thingIDNumber, out var result))
    {
        __result = (int)result;
        return false;  // 直接返回，跳过原方法
    }
    return true;  // 执行原方法，然后后缀会缓存
}

[HarmonyPostfix]
public static void CachePostfix(Pawn __instance, ref int __result)
{
    cache[__instance.thingIDNumber] = __result;
}
```

### 5.2 扩展性设计

**决策1：补丕模块化**
```csharp
// ✅ 推荐：按功能域分离补丕
public class HarmonyInitializer
{
    public static void PatchAll(Harmony harmony)
    {
        harmony.PatchAll(typeof(PawnPatches).Assembly);
        harmony.PatchAll(typeof(BuildingPatches).Assembly);
        harmony.PatchAll(typeof(MapPatches).Assembly);
    }
}

// 便于：
// - 按需加载某个功能域
// - 禁用某个功能域
// - 测试单个功能域
```

**决策2：使用 abstract 基类减少重复**
```csharp
// ✅ 推荐：共通补丕模式提炼
public abstract class HealthModifierPatch
{
    protected abstract float GetModifier(Pawn pawn);

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref float __result)
    {
        __result *= GetModifier(__instance);
    }
}

// 具体实现
internal class StaminaPatch : HealthModifierPatch
{
    protected override float GetModifier(Pawn pawn)
    {
        return 1f + pawn.health.summaryHealth.SummaryHealthPercent * 0.5f;
    }
}
```

---

## 6. 常见架构陷阱

### 陷阱1：补丕顺序依赖

**错误模式**：
```csharp
// ❌ 坏做法：假设补丕执行顺序
// 假设 ModA 先执行，后执行 ModB

[HarmonyPostfix]
public static void ModB_Postfix(ref int __result)
{
    __result = (int)(__result * 1.5);  // 期望得到 ModA 修改后的结果
}

// 但如果其他模组插入到中间呢？ModA 可能不是最后的了！
```

**正确模式**：
```csharp
// ✅ 显式控制顺序
[HarmonyPriority(Priority.High)]
[HarmonyPostfix]
public static void MyPostfix(ref int __result)
{
    __result = (int)(__result * 1.5);
}

// 并在文档中说明：
// "此补丕应在所有伤害修改补丕之后执行"
```

### 陷阱2：修改全局状态而不同步

**错误模式**：
```csharp
// ❌ 坏做法：补丕中修改全局可变状态
private static List<Pawn> activePawns = new();

[HarmonyPostfix]
public static void Pawn_Spawn_Postfix(Pawn __instance)
{
    activePawns.Add(__instance);  // 多线程不安全！
}
```

**正确模式**：
```csharp
// ✅ 如果必须修改全局状态，使用同步
private static readonly object stateLock = new();
private static List<Pawn> activePawns = new();

[HarmonyPostfix]
public static void Pawn_Spawn_Postfix(Pawn __instance)
{
    lock (stateLock)
    {
        activePawns.Add(__instance);
    }
}
```

### 陷阱3：补丕间的信息泄漏

**错误模式**：
```csharp
// ❌ 坏做法：补丕之间共享私有状态
internal class TakeDamage_Patch
{
    private static int lastDamage;  // 前缀在这里设置

    [HarmonyPrefix]
    public static void Prefix(int damage)
    {
        lastDamage = damage;
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        // 后缀在这里读取
        // 但如果有其他模组的补丕在中间呢？
        Log.Message($"Damage was {lastDamage}");  // 可能错误！
    }
}
```

**正确模式**：
```csharp
// ✅ 使用 __state 在前缀和后缀间传递数据
[HarmonyPrefix]
public static void Prefix(int damage, out object __state)
{
    __state = damage;  // 前缀返回给后缀
}

[HarmonyPostfix]
public static void Postfix(object __state, Pawn __instance)
{
    int originalDamage = (int)__state;
    Log.Message($"Damage was {originalDamage}");  // 准确！
}
```

### 陷阱4：假设补丕不会被禁用

**错误模式**：
```csharp
// ❌ 坏做法：假设你的补丕总是会被应用
[HarmonyPrefix]
public static bool Prefix()
{
    // 期望游戏启动时这里总会执行一次初始化
    if (!initialized)
    {
        Initialize();
        initialized = true;
    }
    // ...
}
```

**正确模式**：
```csharp
// ✅ 在 Mod 类中初始化，而非补丕中
public class MyMod : Mod
{
    public MyMod(ModContentPack content) : base(content)
    {
        Initialize();  // 确保一定执行
    }

    static MyMod()
    {
        // 也可以在静态构造中初始化（优先执行）
    }
}
```

---

## 7. 版本兼容性架构

### 7.1 RimWorld 版本差异处理

**现状**（基于 LoadFolders.xml）：
```
Harmony 支持 1.2 → 1.6 的多个 RimWorld 版本
不同版本的 API 可能改变，需要条件编译或运行时检测
```

**架构方案1：条件编译（推荐）**
```csharp
#if RIMWORLD_1_5
    // 1.5 特定代码
    var result = oldMethodName();
#elif RIMWORLD_1_6
    // 1.6 特定代码（可能 API 改变）
    var result = newMethodName();
#endif
```

**架构方案2：运行时检测（灵活但性能差）**
```csharp
public static bool IsVersion16()
{
    return LoadedModManager.RunningModsListForReading
        .Any(m => m.Name.Contains("1.6"));
}

[HarmonyPrefix]
public static bool Prefix()
{
    if (IsVersion16())
    {
        // 1.6 逻辑
    }
    else
    {
        // 1.5 逻辑
    }
    return true;
}
```

**建议**：
- ✅ 优先用**条件编译**（零运行时成本）
- ⚠️ 运行时检测只用于**无法提前知道**的情况（如 DLC）

### 7.2 Harmony 版本兼容性

**问题**：RimWorld 游戏本身包含 Harmony，模组不应该带自己的 Harmony。

**正确做法**（HarmonyMod/About.xml）：
```xml
<!-- 添加依赖声明 -->
<modDependencies>
    <li>
        <packageId>brrainz.harmony</packageId>
        <displayName>Harmony</displayName>
        <steamWorkshopUrl>https://steamcommunity.com/workshop/filedetails/?id=2009463077</steamWorkshopUrl>
    </li>
</modDependencies>

<!-- 和 -->
<incompatibleWith>
    <!-- 列出那些试图带自己 Harmony 的模组 -->
</incompatibleWith>
```

---

## 8. 快速参考：架构决策检查清单

创建新模组时，用这个清单检查你的架构设计：

### 模组初始化
- [ ] Main.cs 中只做两件事：创建 Harmony 实例和调用 PatchAll()
- [ ] 不在 Main 中做任何除初始化外的业务逻辑
- [ ] Settings 在 GetSettings<T>() 中显式创建

### 补丕组织
- [ ] 补丕按功能域分类（PawnPatches.cs, BuildingPatches.cs 等）
- [ ] 每个补丕类使用 [HarmonyPatch] 标记
- [ ] 关键补丕设置 [HarmonyPriority] 或 [HarmonyBefore]/[HarmonyAfter]
- [ ] 补丕方法签名通过代码模板生成，避免手写错误

### 多模组协作
- [ ] About.xml 中明确 loadBefore/loadAfter 依赖
- [ ] 为其他模组暴露扩展点（接口或静态事件）
- [ ] 文档中说明补丕的执行顺序假设

### 性能考虑
- [ ] 前缀中的逻辑要快速（不做复杂计算）
- [ ] 热路径上的补丕考虑缓存策略
- [ ] 测试补丕是否导致启动时间明显增加

### 版本兼容性
- [ ] 不同 RimWorld 版本的 API 调用用条件编译隔离
- [ ] About.xml 中列出支持的 RimWorld 版本
- [ ] 不要自己带 Harmony，声明为依赖

### 代码质量
- [ ] 补丕异常都有 try-catch
- [ ] 使用 Log.Warning/Error 而非 Debug.Log
- [ ] 补丕中的全局状态访问都有同步保护
- [ ] 补丕的幂等性得到保证（应用多次结果相同）

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初始完成，包含三层架构、四大约束、设计模式、多模组协作、性能权衡、常见陷阱、版本兼容性、检查清单 | 2026-01-12 | Knowledge Refiner |
