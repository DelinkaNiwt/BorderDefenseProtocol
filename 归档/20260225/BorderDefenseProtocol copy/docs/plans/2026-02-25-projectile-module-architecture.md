---
标题：BDP弹道子系统模块化架构设计
版本号: v1.0
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 针对BDP弹道子系统的架构重构方案。分析引擎继承约束，提出"统一基类 + 模块组合"架构，将拖尾、引导飞行、爆炸等机制从弹道类中解耦为独立模块，通过XML声明+运行时注入统一管理。消除Bullet_BDP与Projectile_ExplosiveBDP的代码重复，为未来机制扩展提供O(1)接入能力。
---

# BDP弹道子系统模块化架构设计

## 1. 问题背景

### 1.1 现状

当前BDP有两个弹道类：

- `Bullet_BDP : Bullet`（118行）——拖尾 + 引导飞行
- `Projectile_ExplosiveBDP : Projectile_Explosive`（111行）——拖尾 + 引导飞行

两者的逻辑**逐字相同**（字段、属性、SpawnSetup、TickInterval、InitGuidedFlight、ImpactSomething、ExposeData），唯一区别是基类不同。

### 1.2 根因

C# 单继承限制。`Bullet` 和 `Projectile_Explosive` 并列继承 `Projectile`，BDP 无法让两个弹道类共享中间基类。

### 1.3 扩展性问题

未来BDP需要更多弹道机制（如追踪、穿透、分裂等），且这些机制需要**自由组合**。当前架构下，每新增一种基类变体就要复制一遍所有共享逻辑，组合数爆炸。

## 2. 引擎约束分析

### 2.1 Projectile 继承树

```
Verse.Projectile (基类)
  ├─ RimWorld.Bullet              命中型：单体精确伤害
  └─ Verse.Projectile_Explosive   爆炸型：纯AOE爆炸
```

### 2.2 Bullet 与 Projectile_Explosive 的本质差异

| 特性 | Bullet | Projectile_Explosive |
|------|--------|---------------------|
| 调用 base.Impact | 是（Clamor + landedEffecter + Destroy） | **否** |
| 直接命中伤害 | `hitThing.TakeDamage(dinfo)` | **无** |
| AOE爆炸 | **无** | `GenExplosion.DoExplosion()` |
| 战斗日志 | `BattleLogEntry_RangedImpact` | **无** |
| BulletImpact通知 | 是（触发掩体AI等） | **无** |
| ExtraDamages | 是（武器特质额外伤害） | **无** |
| 延迟引爆 | **无** | `ticksToDetonation` + `TickInterval` |
| 未命中地面特效 | 是（水花/尘土） | **无** |

引擎设计者将"命中单体"和"AOE爆炸"视为两种正交的投射物类型。原版XML中**没有任何"命中+爆炸"双效果投射物**。

### 2.3 为什么不是 Explosive 继承 Bullet

- `Projectile_Explosive.Impact` **故意不调用** `base.Impact()`——它不需要命中伤害、战斗日志、BulletImpact通知等Bullet特有行为
- 延迟引爆机制（`landed=true` + `ticksToDetonation`）与Bullet的"命中即结算"逻辑冲突
- 强行继承会引入不需要的耦合，违反里氏替换原则

### 2.4 关键洞察

在BDP的设计理念中，**爆炸不是一种独立的投射物类型，而是对普通子弹的附加机制**——与拖尾、引导飞行等机制平等。这与引擎的设计理念不同，但完全可以在mod层面实现。

## 3. 架构方案：统一基类 + 模块组合

### 3.1 核心思想

- **统一基类**：所有BDP弹道统一继承 `Bullet`，只维护一个弹道类 `Bullet_BDP`
- **模块组合**：拖尾、引导、爆炸等机制各自封装为独立模块，挂载到弹道实例上
- **XML声明**：模块的存在由XML `modExtensions` 声明
- **运行时注入**：模块的动态数据（如引导路径点）由Verb端在发射后通过统一接口注入
- **弹道类无感知**：`Bullet_BDP` 只知道"我有一组模块"，不知道也不关心具体是什么模块

### 3.2 整体结构

```
引擎层（不可修改）：
  Projectile → Bullet

BDP弹道层：
  Bullet → Bullet_BDP（唯一弹道类，模块宿主薄壳）

BDP模块层：
  IBDPProjectileModule（接口）
    ├─ TrailModule          当前：拖尾
    ├─ GuidedModule         当前：引导飞行
    ├─ ExplosionModule      当前：爆炸（替代 Projectile_ExplosiveBDP）
    └─ ...                  未来：任意新机制，实现接口即可接入

配置层：
  DefModExtension 子类（BeamTrailConfig, BDPExplosionConfig, ...）
    → XML中声明模块存在 + 静态参数

工厂层：
  BDPModuleFactory
    → SpawnSetup时扫描 def.modExtensions，自动创建对应模块实例
```

### 3.3 模块接口

```csharp
/// <summary>
/// BDP弹道模块接口——所有弹道附加机制的统一抽象。
///
/// 设计原则：
///   - 接口只定义弹道生命周期中的 hook 点，不预设具体机制
///   - 每个 hook 都有明确的语义和调用时机
///   - 模块间通过宿主弹道的公开字段通信，不直接耦合
/// </summary>
public interface IBDPProjectileModule : IExposable
{
    /// <summary>
    /// 执行优先级。数值越小越先执行。
    /// 用途：控制模块间的执行顺序（如路径修改应先于视觉效果）。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 弹道生成时调用。用于初始化模块状态、缓存配置等。
    /// 调用时机：Bullet_BDP.SpawnSetup 中，base.SpawnSetup 之后。
    /// </summary>
    void OnSpawn(Bullet_BDP host, Map map);

    /// <summary>
    /// 每tick调用。用于持续性行为（视觉效果、飞行路径修正等）。
    /// 调用时机：Bullet_BDP.TickInterval 中，base.TickInterval 之后。
    /// prevPos/newPos 为本tick移动前后的世界坐标。
    /// </summary>
    void OnTick(Bullet_BDP host, Vector3 prevPos, Vector3 newPos);

    /// <summary>
    /// 弹道即将命中时调用。返回true表示"我拦截了此次Impact"。
    /// 用途：引导飞行的锚点推进、穿透继续飞行等需要阻止正常Impact的场景。
    /// 调用时机：Bullet_BDP.ImpactSomething 中，base.ImpactSomething 之前。
    /// 注意：任一模块返回true，后续模块的OnPreImpact仍会被调用（允许多模块协作），
    ///       但base.ImpactSomething不会执行。
    /// </summary>
    bool OnPreImpact(Bullet_BDP host);

    /// <summary>
    /// 弹道命中后调用。用于命中后的附加效果（爆炸、分裂生成等）。
    /// 调用时机：Bullet_BDP.Impact 中，base.Impact 之后。
    /// 注意：此时弹道已被Destroy，不可访问host的Map等属性，
    ///       必须使用参数传入的缓存值。
    /// </summary>
    void OnPostImpact(Thing hitThing, IntVec3 position, Map map);
}
```

### 3.4 Bullet_BDP 宿主实现

```csharp
/// <summary>
/// BDP统一弹道类——所有BDP投射物的唯一基类。
/// 自身是薄壳，所有机制差异由挂载的 IBDPProjectileModule 表达。
/// 模块在SpawnSetup时由工厂根据XML配置自动创建。
/// </summary>
public class Bullet_BDP : Bullet
{
    private List<IBDPProjectileModule> modules;

    /// <summary>获取指定类型的模块（供Verb端注入运行时数据）。</summary>
    public T GetModule<T>() where T : class, IBDPProjectileModule
    {
        if (modules == null) return null;
        for (int i = 0; i < modules.Count; i++)
            if (modules[i] is T t) return t;
        return null;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        modules = BDPModuleFactory.CreateModules(this, def);
        // 按优先级排序，确保执行顺序确定性
        modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        for (int i = 0; i < modules.Count; i++)
            modules[i].OnSpawn(this, map);
    }

    protected override void TickInterval(int delta)
    {
        Vector3 prev = DrawPos;
        base.TickInterval(delta);
        Vector3 cur = DrawPos;
        for (int i = 0; i < modules.Count; i++)
            modules[i].OnTick(this, prev, cur);
    }

    protected override void ImpactSomething()
    {
        bool intercepted = false;
        for (int i = 0; i < modules.Count; i++)
            if (modules[i].OnPreImpact(this))
                intercepted = true;
        if (!intercepted)
            base.ImpactSomething();
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        // 缓存：base.Impact 内部会 Destroy 自身
        Map map = base.Map;
        IntVec3 pos = base.Position;
        base.Impact(hitThing, blockedByShield);
        for (int i = 0; i < modules.Count; i++)
            modules[i].OnPostImpact(hitThing, pos, map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
    }
}
```

### 3.5 模块工厂

```csharp
/// <summary>
/// 模块工厂——根据ThingDef上的modExtensions自动创建对应模块。
/// 每种DefModExtension子类对应一种模块类型，通过注册表映射。
/// </summary>
public static class BDPModuleFactory
{
    // 注册表：DefModExtension类型 → 模块创建函数
    private static readonly Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>>
        registry = new Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>>();

    /// <summary>注册一种配置类型到模块创建函数的映射。</summary>
    public static void Register<TConfig>(Func<TConfig, IBDPProjectileModule> creator)
        where TConfig : DefModExtension
    {
        registry[typeof(TConfig)] = ext => creator((TConfig)ext);
    }

    /// <summary>扫描def的modExtensions，创建所有匹配的模块。</summary>
    public static List<IBDPProjectileModule> CreateModules(Bullet_BDP host, ThingDef def)
    {
        var result = new List<IBDPProjectileModule>();
        if (def.modExtensions == null) return result;
        foreach (var ext in def.modExtensions)
        {
            if (registry.TryGetValue(ext.GetType(), out var creator))
                result.Add(creator(ext));
        }
        return result;
    }
}
```

模块注册示例（在Mod初始化时执行）：

```csharp
BDPModuleFactory.Register<BeamTrailConfig>(cfg => new TrailModule(cfg));
BDPModuleFactory.Register<BDPExplosionConfig>(cfg => new ExplosionModule(cfg));
// 未来新模块：只需一行注册
```

### 3.6 数据流：静态配置 vs 运行时注入

所有模块的生命周期统一，但数据来源可以不同：

```
┌─────────────────────────────────────────────────────┐
│  模块的存在（"这颗子弹有什么能力"）                     │
│  → 统一由 XML modExtensions 声明                      │
│  → SpawnSetup 时由工厂自动创建                         │
│                                                       │
│  模块的参数                                            │
│  ├─ 静态参数（拖尾宽度、爆炸半径等）                    │
│  │  → 来自 XML DefModExtension 字段                   │
│  │  → 模块构造时传入，不变                              │
│  │                                                     │
│  └─ 动态参数（引导路径点、追踪目标等）                   │
│     → 来自 Verb 端运行时数据                           │
│     → 发射后通过 GetModule<T>() 统一接口注入            │
│                                                       │
│  弹道类对两者无感知，模块内部自行管理                     │
└─────────────────────────────────────────────────────┘
```

Verb端注入示例：

```csharp
// Verb 发射弹道后
protected override void OnProjectileLaunched(Projectile proj)
{
    if (proj is Bullet_BDP bdp)
    {
        // 通过统一接口获取模块，注入运行时数据
        var guided = bdp.GetModule<GuidedModule>();
        guided?.SetWaypoints(waypoints);
    }
}
```

### 3.7 模块间通信

模块之间不直接引用，通过两种方式间接通信：

1. **通过宿主的公开字段**：`origin`、`destination`、`ticksToImpact` 等 Projectile 的 public/protected 字段是所有模块的共享上下文。一个模块修改 `destination`（如引导模块改路径），另一个模块读取 `DrawPos`（如拖尾模块画线），自然协作。

2. **通过执行顺序**：`Priority` 控制模块的执行顺序。路径修改类模块（Priority低）先执行，视觉效果类模块（Priority高）后执行，读到的是修改后的状态。

## 4. XML 配置示例

```xml
<!-- 基础BDP子弹（无附加机制，等同原版Bullet行为） -->
<ThingDef ParentName="BaseBullet">
  <defName>Bullet_BDP_Plain</defName>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <projectile>
    <damageDef>Bullet</damageDef>
    <damageAmountBase>12</damageAmountBase>
    <speed>70</speed>
  </projectile>
</ThingDef>

<!-- 拖尾子弹 -->
<ThingDef ParentName="BaseBullet">
  <defName>Bullet_BDP_Beam</defName>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <projectile> ... </projectile>
  <modExtensions>
    <li Class="BDP.Trigger.BeamTrailConfig">
      <trailWidth>0.15</trailWidth>
      <trailColor>(0.3, 0.8, 1.0)</trailColor>
      <segmentDuration>8</segmentDuration>
    </li>
  </modExtensions>
</ThingDef>

<!-- 引导弹 + 拖尾（引导路径由Verb运行时注入） -->
<ThingDef ParentName="BaseBullet">
  <defName>Bullet_BDP_GuidedBeam</defName>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <projectile> ... </projectile>
  <modExtensions>
    <li Class="BDP.Trigger.BeamTrailConfig"> ... </li>
    <li Class="BDP.Trigger.GuidedFlightConfig" />
  </modExtensions>
</ThingDef>

<!-- 引导弹 + 拖尾 + 爆炸（三种机制自由组合） -->
<ThingDef ParentName="BaseBullet">
  <defName>Bullet_BDP_GuidedExplosiveBeam</defName>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <projectile> ... </projectile>
  <modExtensions>
    <li Class="BDP.Trigger.BeamTrailConfig"> ... </li>
    <li Class="BDP.Trigger.GuidedFlightConfig" />
    <li Class="BDP.Trigger.BDPExplosionConfig">
      <explosionRadius>2.9</explosionRadius>
      <explosionDamageDef>Bomb</explosionDamageDef>
    </li>
  </modExtensions>
</ThingDef>
```

所有组合都是同一个 `thingClass`，差异完全由 `modExtensions` 表达。

## 5. 对现有代码的影响

| 现有组件 | 变化 |
|---|---|
| `Bullet_BDP` | 重写为模块宿主薄壳 |
| `Projectile_ExplosiveBDP` | **删除**，由 ExplosionModule 替代 |
| `GuidedFlightController` | 不变，被 GuidedModule 内部持有 |
| `BDPEffectMapComponent` | 不变 |
| `BDPTrailSegment` | 不变 |
| `BeamTrailConfig` | 不变，作为 TrailModule 的配置源 |
| `GuidedVerbState.AttachGuidedFlight` | 简化：`bdp.GetModule<GuidedModule>()?.SetWaypoints(...)` |
| 引用 `Projectile_ExplosiveBDP` 的 XML | 改为 `Bullet_BDP` + `BDPExplosionConfig` |
| Verb_BDPGuided 等 | `OnProjectileLaunched` 改用 `GetModule<T>()` 注入 |

## 6. 风险与缓解

| 风险 | 评估 | 缓解 |
|---|---|---|
| 其他mod patch `Projectile_Explosive` 不影响BDP | 低风险 | BDP是独立系统，不依赖其他mod的爆炸patch |
| `base.Impact` 后 `this` 已Destroy | 已处理 | OnPostImpact 接口传入缓存的 map/pos，模块不访问 host |
| 模块序列化（存档兼容） | 中等 | `Scribe_Collections.Look` + `LookMode.Deep`，模块实现 `IExposable` |
| 延迟引爆需要阻止Destroy | 可解决 | 延迟引爆模块在 OnPreImpact 中拦截，自行管理 landed 状态 |
| 爆炸参数完整性 | 低风险 | ExplosionModule 从 BDPExplosionConfig 读取参数，调用 `GenExplosion.DoExplosion`，参数由XML完整配置 |

## 7. 设计原则总结

1. **统一基类**：只有一个弹道类 `Bullet_BDP`，消除继承分叉导致的代码重复
2. **模块平等**：所有附加机制（拖尾、引导、爆炸等）地位平等，都是模块
3. **积木式组合**：模块的存在统一由XML声明，弹道类无感知
4. **接口不预设内容**：接口只定义生命周期hook点，不假设具体机制
5. **数据来源透明**：静态参数来自XML，动态参数来自Verb端运行时注入，模块内部自行管理，外部无需区分

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-25 | 创建文档。引擎约束分析、统一基类+模块组合架构设计、接口定义、XML配置示例、现有代码影响评估。 | Claude Opus 4.6 |
