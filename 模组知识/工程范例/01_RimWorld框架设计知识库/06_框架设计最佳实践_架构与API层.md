---
摘要: 针对框架设计者的核心最佳实践，涵盖框架设计决策、架构模式选择、API设计、配置系统、扩展机制等全方位指导。重点是框架本身的设计原理，而非实现细节

版本号: 1.0
修改时间: 2026-01-07
关键词: 框架设计, 架构模式, API设计, 配置系统, 扩展机制, 设计决策, 单一职责, 目标用户, 依赖管理, HAR, VEF, ASF, WeaponFitting, AncotLibrary

标签: [待审]
---

## 目录

- [一、设计阶段最佳实践](#一设计阶段最佳实践)
- [二、架构选择最佳实践](#二架构选择最佳实践)
- [三、API设计最佳实践](#三api设计最佳实践)
- [四、配置系统最佳实践](#四配置系统最佳实践)
- [五、扩展机制最佳实践](#五扩展机制最佳实践)
- [六、框架级依赖管理](#六框架级依赖管理)
- [七、框架设计中的常见陷阱](#七框架设计中的常见陷阱)
- [八、快速决策速查表](#八快速决策速查表)

---

## 一、设计阶段最佳实践

### 1.1 明确框架的单一职责

**最佳实践：** 框架只做一件事，并把它做好

**验证框架：**
- ✓ **HAR** - 只做外形定制系统，不涉及战斗/生成/配置管理
- ✓ **WeaponFitting** - 只做武器配件系统，不做武器本身
- ✓ **AncotLibrary** - 只提供工具函数，不实现具体内容

**反面案例：**
- ✗ 框架既要做图形、又要做逻辑、还要做配置→难以维护

**实践指南：**

```
框架职责定义:
┌──────────────────────────────────────┐
│ 单一职责原则                           │
├──────────────────────────────────────┤
│ ✓ 明确定义"这个框架做什么"             │
│ ✓ 列出"这个框架不做什么"               │
│ ✓ 在有歧义时，优先选择最小范围         │
│ ✓ 其他职责交给其他框架或应用层        │
└──────────────────────────────────────┘

案例：WeaponFitting职责定义
做什么：
  ✓ 管理Fitting (配件) 物品
  ✓ 支持Fitting的装配/卸载
  ✓ 提供Fitting的兼容性检查

不做什么：
  ✗ 不定义武器本身 (交给VEF)
  ✗ 不实现战斗逻辑 (交给武器系统)
  ✗ 不管理Fitting的视觉效果 (交给HAR)
```

### 1.2 评估目标用户和使用场景

**最佳实践：** 清晰定位目标用户，设计应该匹配用户能力

**三类用户对应的框架设计：**

```
用户类型          框架特征              代表框架
─────────────────────────────────────────
模组使用者         XML配置为主           HAR (零编程)
(最多)             交互友好

模组开发者         提供API和工具          AncotLibrary
(中等)             文档完整

框架开发者         深度扩展能力           VEF (DLL模块化)
(最少)             内部复杂度允许高
```

**设计影响：**

```csharp
// ✓ 面向模组使用者的设计
// 配置驱动，无需编程
<MyFrameworkDef>
    <defName>Config1</defName>
    <value>100</value>
</MyFrameworkDef>

// ✓ 面向开发者的设计
// 清晰的API文档
public static void DoSomething(Thing thing, int level)
{
    /// <summary>对Thing应用框架效果</summary>
    /// <param name="thing">目标Thing</param>
    /// <param name="level">效果等级 (1-5)</param>
    // 实现
}

// ✓ 面向框架开发者的设计
// 提供扩展点
public interface IFrameworkExtension
{
    void Extend(Framework framework);
}
```

### 1.3 选择合适的复杂度等级

**最佳实践：** 从最简单的设计开始，只在必要时增加复杂度

**复杂度演化路径：**

```
Level 1 (最简单)
└─ 工具函数集合
   代表：AncotUtility
   例：public static float Calculate(int x) => x * 2;

   │
   ↓

Level 2 (中等)
└─ 配置Def系统
   代表：WeaponFitting
   例：UniqueWeaponCategoriesDef 驱动逻辑

   │
   ↓

Level 3 (较复杂)
└─ 建筑类继承 + 组件系统
   代表：CeleTechArsenalMKIII
   例：Building_Turret → 子类 + Comp聚合

   │
   ↓

Level 4 (复杂)
└─ 多系统协调 + 策略模式
   代表：ASF (4层策略)
   例：4层渲染策略选择

   │
   ↓

Level 5 (最复杂)
└─ 多框架 + 高定制性
   代表：VEF (5个DLL + 24个子系统)
   例：MVCF + KCSG + PipeSystem
```

**选择建议：**

```
问：我应该选择哪个复杂度？
├─ 如果功能很简单 → Level 1
├─ 如果需要XML配置 → Level 2
├─ 如果需要建筑/物品定制 → Level 3
├─ 如果需要多策略选择 → Level 4
└─ 如果多个大系统需要协调 → Level 5
```

### 1.4 评估可维护性

**最佳实践：** 优先选择易于维护的设计，而非功能最强的设计

**维护成本评估矩阵：**

| 因素 | 低成本 | 中成本 | 高成本 |
|-----|--------|--------|--------|
| 文件数 | <100 | 100-500 | >500 |
| 继承深度 | <3层 | 3-5层 | >5层 |
| 补丁数 | 0-5 | 5-15 | >15 |
| 配置方式 | 简单 | 中等 | 复杂 |
| 版本适配 | <2天 | 2-5天 | >1周 |

**建议：**
```
✓ 优先选择"低成本"设计
~ 需要理由才能选"中成本"
✗ 非必要不选"高成本"
```

---

## 二、架构选择最佳实践

### 2.1 架构模式对比与选择

**最佳实践：** 根据问题特征选择相应的架构模式

**四种基础架构模式：**

#### 模式1：单层架构（Simple Layer）

```
应用层
  │
  ├─ 工具类 A
  ├─ 工具类 B
  └─ 工具类 C

代表：AncotLibrary
文件数：100-300
复杂度：⭐
```

**适用场景：**
- ✓ 功能相对独立
- ✓ 无复杂状态管理
- ✓ 团队人数 <3

**不适用场景：**
- ✗ 需要多系统协调
- ✗ 复杂的扩展需求

#### 模式2：多层架构（Multi-Layer）

```
应用层 (使用框架)
  │
配置层 (XML Def)
  │
  ├─ 工具层 (Utils)
  │
业务层 (Implementation)
  │
数据层 (Cache/State)

代表：WeaponFitting
文件数：20-100
复杂度：⭐⭐
```

**适用场景：**
- ✓ 需要XML配置
- ✓ 有缓存和状态
- ✓ 团队人数 3-5

**关键特征：**
- 配置驱动逻辑
- 明确的层次分离
- 易于理解和维护

#### 模式3：模块化架构（Modular）

```
应用层
  │
  ├─ Module A
  │   ├─ Core
  │   ├─ Utils
  │   └─ Config
  │
  ├─ Module B
  │   ├─ Core
  │   ├─ Utils
  │   └─ Config
  │
  └─ Shared
      ├─ Library
      └─ Base Classes

代表：CeleTechArsenalMKIII
文件数：150-300
复杂度：⭐⭐⭐
```

**适用场景：**
- ✓ 有多个独立功能模块
- ✓ 模块间有少量依赖
- ✓ 团队人数 5-10

**关键特征：**
- 模块内聚
- 模块间解耦
- 共享基础设施

#### 模式4：多框架架构（Multi-Framework）

```
核心框架 (HAR/VEF Core)
  │
  ├─ Framework A (MVCF)
  │   ├─ Core Logic
  │   └─ Extension Points
  │
  ├─ Framework B (KCSG)
  │   ├─ Core Logic
  │   └─ Extension Points
  │
  └─ Framework C (PipeSystem)
      ├─ Core Logic
      └─ Extension Points

代表：VEF
文件数：>500
复杂度：⭐⭐⭐⭐⭐
```

**适用场景：**
- ✓ 多个大系统需要协调
- ✓ 支持广泛的模组扩展
- ✓ 团队人数 >10
- ✓ 长期维护计划

**关键特征：**
- 独立可用的子框架
- 明确的集成接口
- 完善的扩展点

### 2.2 架构演化策略

**最佳实践：** 从简单开始，逐步演化到复杂

```
演化路径示例：
Week 1-2: 单层 (工具函数)
  └─ 用户反馈：需要配置

Week 3-4: 多层 (加入配置层)
  └─ 用户反馈：功能独立，能否分离？

Week 5-8: 模块化 (拆分模块)
  └─ 用户反馈：能否支持多个框架集成？

Week 9+: 多框架 (独立框架模式)
  └─ 稳定架构

教训：
✓ 每个阶段重新评估是否需要演化
✓ 不要一开始就设计"最终"架构
✗ 过度设计导致复杂度无法管理
```

### 2.3 关键决策：是否使用继承

**最佳实践：** 优先使用组件/聚合，限制继承使用

**决策树：**

```
需要扩展类行为？
  ├─ 是新增一种类型 (同级)
  │  └─ 使用继承 ✓
  │     例：Building_Turret → Building_CMCTurret
  │
  ├─ 是增加现有类的能力
  │  └─ 使用组件 ✓✓ (首选)
  │     例：Comp_PowerTrader + Comp_Mannable
  │
  └─ 是临时需求/特例处理
     └─ 使用Def配置 ✓✓✓ (首选)
        例：在XML中添加flag或配置
```

**继承使用规则：**

```
✓ 允许的继承
├─ 深度不超过3层
│  Building → Building_Turret → Building_CMCTurretGun
│  (不要：→ 变种1 → 变种2 → ...)
│
├─ 每层增加明确的新能力
│  Building_Turret: 添加射击能力
│  Building_CMCTurretGun: 添加特定武器类型
│
└─ 共享代码比例 >50%

✗ 避免的继承
├─ 仅用于代码复用
│  (改用Extract Method或工具函数)
│
├─ 深层级的继承链 >3层
│  (改用组件或聚合)
│
├─ 多重继承 (C#不支持，不用考虑)
│
└─ 接口继承超过5个
   (说明设计过度复杂)
```

---

## 三、API设计最佳实践

### 3.1 公开API的最小化原则

**最佳实践：** 只公开必要的API，隐藏内部实现

**原则：**

```csharp
// ✓ 最小化API
public static class WF_Utility
{
    // 一个清晰的公开入口
    public static bool CanApply(Thing thing) { }

    public static void Apply(Thing thing) { }

    public static void Unapply(Thing thing) { }

    // 所有细节都隐藏在内部
    private static void _setupInternal() { }
    private static void _cleanupInternal() { }
}

// ✗ API爆炸
public static class BadUtility
{
    public static void Method1() { }
    public static void Method2() { }
    public static void Method3() { }
    // ... 50个方法 ...
    public static void Method50() { }

    // 问题：用户不知道该用哪个
}
```

### 3.2 API文档

**最佳实践：** 每个公开方法都有XML注释

```csharp
/// <summary>
/// 对指定Thing应用框架效果
/// </summary>
/// <param name="thing">目标Thing（必须是xxx类型）</param>
/// <param name="level">效果等级，1-5之间</param>
/// <returns>是否成功应用</returns>
/// <remarks>
/// 这个方法会修改Thing的属性。如果Thing已经应用过效果，
/// 会覆盖之前的设置。
/// </remarks>
/// <example>
/// <code>
/// var thing = Find.World.worldObjects.AllWorldObjects.OfType<Thing>().First();
/// if (MyFramework.CanApply(thing))
///     MyFramework.Apply(thing, 3);
/// </code>
/// </example>
public static bool Apply(Thing thing, int level)
{
    // 实现
}
```

### 3.3 参数验证

**最佳实践：** 在公开API的入口处验证参数

```csharp
public static void Apply(Thing thing, int level)
{
    // 验证参数
    if (thing == null)
        throw new ArgumentNullException(nameof(thing));

    if (level < 1 || level > 5)
        throw new ArgumentOutOfRangeException(nameof(level), "level必须在1-5之间");

    if (thing.Destroyed)
        throw new InvalidOperationException("无法对已销毁的Thing应用效果");

    // 后续逻辑可以假设参数有效
    ApplyInternal(thing, level);
}
```

### 3.4 返回值设计

**最佳实践：** 清晰表达成功/失败和错误原因

```csharp
// ✓ 方式1：布尔返回（简单）
public static bool CanApply(Thing thing) => thing != null && thing.Def.HasModExtension<MyExtension>();

// ✓ 方式2：异常（严重错误）
public static void Apply(Thing thing)
{
    if (thing == null)
        throw new ArgumentNullException(nameof(thing));
    // ...
}

// ✓ 方式3：结果对象（需要错误信息）
public class ApplyResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public int AppliedLevel { get; set; }
}

public static ApplyResult Apply(Thing thing, int level)
{
    if (thing == null)
        return new ApplyResult { Success = false, ErrorMessage = "Thing不存在" };
    // ...
}

// ✗ 避免：静默失败
public static void Apply(Thing thing, int level)
{
    if (thing == null)
        return;  // ❌ 调用者不知道发生了什么
}
```

---

## 四、配置系统最佳实践

### 4.1 Def设计原则

**最佳实践：** 配置应该是声明式的，不包含逻辑

```csharp
// ✓ 好的Def设计：声明性
public class WeaponFittingConfigDef : Def
{
    public List<ThingDef> allowedWeapons = new List<ThingDef>();
    public int maxSlots = 3;
    public float costMultiplier = 1.0f;
}

// ✗ 不好的Def设计：包含逻辑
public class BadConfigDef : Def
{
    public void ApplyLogic(Thing thing) { }  // ❌ 行为不应在Def中
    public int CalculateCost() { }            // ❌ 逻辑不应在Def中
}
```

### 4.2 配置验证

**最佳实践：** 在ResolveReferences中验证配置的有效性

```csharp
public class WeaponFittingConfigDef : Def
{
    public List<ThingDef> allowedWeapons = new List<ThingDef>();
    public int maxSlots = 3;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        // 验证1：检查必需字段
        if (allowedWeapons.NullOrEmpty())
            Log.Warning($"{defName}: allowedWeapons为空");

        // 验证2：检查值范围
        if (maxSlots <= 0)
            Log.Error($"{defName}: maxSlots必须>0，当前{maxSlots}");

        // 验证3：检查引用有效性
        for (int i = allowedWeapons.Count - 1; i >= 0; i--)
        {
            if (allowedWeapons[i] == null)
            {
                Log.Warning($"{defName}: allowedWeapons[{i}]为null，已删除");
                allowedWeapons.RemoveAt(i);
            }
        }
    }
}
```

### 4.3 XML组织

**最佳实践：** 按照Def类型和逻辑分组

```xml
<!-- ✓ 好的组织 -->
<!-- MyFramework/Defs/FrameworkDefs.xml -->
<Defs>
    <!-- 配置组：武器 -->
    <WeaponFittingConfigDef>
        <defName>Rifle_Config</defName>
        <!-- ... -->
    </WeaponFittingConfigDef>

    <!-- 配置组：特效 -->
    <FrameworkEffectDef>
        <defName>Effect_Apply</defName>
        <!-- ... -->
    </FrameworkEffectDef>
</Defs>

<!-- ✗ 不好的组织 -->
<!-- AllDefs.xml - 万能XML，难以维护 -->
<Defs>
    <WeaponFittingConfigDef>...</WeaponFittingConfigDef>
    <FrameworkEffectDef>...</FrameworkEffectDef>
    <!-- 500+ Def混在一起 -->
</Defs>
```

---

## 五、扩展机制最佳实践

### 5.1 扩展点设计

**最佳实践：** 清晰定义扩展点，便于第三方扩展

**三种扩展点模式：**

#### 模式1：配置扩展（最简单）

```csharp
// 框架定义基础Def
public class MyFrameworkConfigDef : Def { }

// 用户通过XML添加配置即可扩展
// <MyFrameworkConfigDef>
//     <defName>UserConfig</defName>
// </MyFrameworkConfigDef>

// 优点：无需编程，易于理解
// 缺点：灵活性有限
```

#### 模式2：继承扩展（中等）

```csharp
// 框架定义基础类
public class MyFrameworkBase
{
    public virtual void OnApply(Thing thing) { }
    public virtual void OnRemove(Thing thing) { }
}

// 用户可以继承并覆盖方法
public class UserImplementation : MyFrameworkBase
{
    public override void OnApply(Thing thing)
    {
        // 自定义逻辑
    }
}

// 优点：灵活性高，仍然简洁
// 缺点：需要编程知识
```

#### 模式3：接口扩展（最灵活）

```csharp
// 框架定义接口
public interface IFrameworkExtension
{
    string ExtensionName { get; }
    void Initialize();
    void Apply(Thing thing);
}

// 框架注册管理
public static class MyFramework
{
    private static List<IFrameworkExtension> extensions = new();

    public static void RegisterExtension(IFrameworkExtension ext)
    {
        extensions.Add(ext);
        ext.Initialize();
    }
}

// 用户实现接口
public class UserExtension : IFrameworkExtension
{
    public string ExtensionName => "MyUserExtension";

    public void Initialize() { }

    public void Apply(Thing thing) { }
}

// 优点：极度灵活，支持任意扩展
// 缺点：代码复杂度高
```

### 5.2 反射缓存

**最佳实践：** 如果使用反射，应该缓存结果

```csharp
// ❌ 不好：每次都反射
public static void InvokeExtensions(Thing thing)
{
    var type = thing.GetType();  // 反射！
    var method = type.GetMethod("OnFrameworkApply");  // 反射！
    if (method != null)
        method.Invoke(thing, null);  // 反射！
}

// ✓ 好：缓存反射结果
private static Dictionary<Type, MethodInfo> methodCache = new();

public static void InvokeExtensions(Thing thing)
{
    var type = thing.GetType();

    if (!methodCache.TryGetValue(type, out var method))
    {
        method = type.GetMethod("OnFrameworkApply");
        methodCache[type] = method;
    }

    if (method != null)
        method.Invoke(thing, null);
}
```

---

## 六、框架级依赖管理

### 6.1 依赖关系设计

**最佳实践：** 设计清晰的单向依赖关系

**推荐的依赖关系：**

```
独立库层
  ↑
  ├─ 功能框架A (依赖独立库)
  ├─ 功能框架B (依赖独立库)
  └─ 功能框架C (依赖独立库)

不推荐：
  ├─ A ← → B  (循环依赖)
  ├─ A ← → B ← → C  (循环依赖)
  └─ A → B → C → A  (循环依赖)
```

### 6.2 框架间通信

**最佳实践：** 通过明确的接口或事件系统进行框架间通信

```csharp
// ✓ 方式1：公开接口（推荐）
public interface IFrameworkA_PublicAPI
{
    void DoSomething(Thing thing);
    bool CanDoSomething(Thing thing);
}

// ✓ 方式2：事件系统（松耦合）
public static class FrameworkEvents
{
    public static event Action<Thing> OnFrameworkApply;

    public static void RaiseFrameworkApply(Thing thing)
        => OnFrameworkApply?.Invoke(thing);
}

// ✗ 不好：直接访问内部类
public void DoSomethingInFrameworkA()
{
    var internal = FrameworkA.internalObject;  // ❌ 耦合
    internal.DoSomething();
}
```

---

## 七、框架设计中的常见陷阱

### 7.1 设计决策陷阱

**陷阱1：功能蠕变**

```
问题：框架初衷是做A，逐渐加入B、C、D...
结果：功能混乱，难以维护

解决：
✓ 严格定义框架边界
✓ 新功能建议拒绝或作为独立框架
✓ 定期评估是否应该拆分
```

**陷阱2：过度设计**

```
问题：一开始就设计"最终"架构，复杂度很高
结果：代码复杂，难以理解，开发缓慢

解决：
✓ 从最简单的设计开始
✓ 有需求才演化
✓ 宁可重构也不要过度设计
```

**陷阱3：忽视用户反馈**

```
问题：用户说不好用，但开发者认为设计是对的
结果：框架流行度下降，被替代

解决：
✓ 定期听取用户意见
✓ 设计应该为用户服务
✓ 优先考虑易用性而非技术优雅
```

**陷阱4：深层继承链**

```
❌ 不好
Building
  → Building_Turret
    → Building_CMCTurret
      → Building_CMCTurretGun
        → Building_CMCTurretGun_MainBattery

✓ 好
Building
  → Building_Turret (只差异化需要的东西)
    → 其他通过字段/组件区分
```

### 7.2 架构决策陷阱

**陷阱5：不清晰的扩展点**

```
问题：框架想支持扩展，但没有清晰的API
结果：用户无法扩展或只能通过Harmony补丁

解决：
✓ 明确定义虚方法、接口、事件等扩展点
✓ 记录如何实现扩展
✓ 提供扩展示例
```

**陷阱6：循环依赖**

```
问题：FrameworkA依赖FrameworkB，FrameworkB又依赖FrameworkA
结果：版本管理混乱，难以单独测试

解决：
✓ 严格单向依赖
✓ 定期审视依赖图
✓ 抽象共享部分为独立库
```

---

## 八、快速决策速查表

### 8.1 架构模式速查

```
问题特征              建议架构        代表框架
────────────────────────────────────────────
简单工具集           单层             AncotLibrary
配置驱动            多层              WeaponFitting
单个复杂系统         模块化            CeleTechArsenalMKIII
多个大系统           多框架            VEF
```

### 8.2 复杂度决策

```
代码行数              建议选择
──────────────────────────
<2K                单层/工具类
2K-5K              多层/简单配置
5K-15K             模块化
>15K               多框架/重型设计
```

### 8.3 依赖关系决策

```
场景                      建议依赖数
────────────────────────────────
完全独立                  0
功能补充                  1-2
系统协调                  3-5
深度集成                  >5 (需要评估)
```

---

## 参考资源

### 分析的框架
1. **HumanoidAlienRaces** (HAR) - XML配置型
2. **VanillaExpandedFramework** (VEF) - 多框架协调型
3. **AdaptiveStorageFramework** (ASF) - 策略优化型
4. **WeaponFitting** - 配置驱动型
5. **AncotLibrary** - 工具库型

### 相关文档
- [00_RimWorld框架生态总览.md](00_RimWorld框架生态总览.md)
- [01_HumanoidAlienRaces框架详细设计.md](01_HumanoidAlienRaces框架详细设计.md)
- [02_VanillaExpandedFramework多框架系统分析.md](02_VanillaExpandedFramework多框架系统分析.md)
- [03_AdaptiveStorageFramework策略框架分析.md](03_AdaptiveStorageFramework策略框架分析.md)
- [04_RimWorld框架设计工程学指导.md](04_RimWorld框架设计工程学指导.md)
- [05_轻量框架模式分析_WeaponFitting等.md](05_轻量框架模式分析_WeaponFitting等.md)
- [07_框架使用最佳实践_实现与维护层.md](07_框架使用最佳实践_实现与维护层.md)

---

## 核心要点速记

### 设计决策
1. ✓ 明确单一职责
2. ✓ 定位目标用户
3. ✓ 选择合适复杂度
4. ✓ 评估维护成本

### 架构选择
1. ✓ 从简单开始
2. ✓ 逐步演化
3. ✓ 优先组件，限制继承
4. ✓ 管理依赖关系

### API和配置
1. ✓ 最小化公开API
2. ✓ 完整的文档
3. ✓ 配置是声明式的
4. ✓ 清晰的扩展点

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|--------|--------|-------|
| 1.0 | 拆分：框架设计最佳实践（架构与API层） | 2026-01-07 | Claude |
