---
标题：BDP投射物模块系统（PMS）重构设计
版本号: v1.0
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: BDP弹道子系统全面重构方案。采用"统一Bullet基类 + 模块组合"架构，将拖尾、引导飞行、爆炸等机制从弹道类中解耦为独立模块（IBDPProjectileModule），通过XML声明+运行时注入统一管理。同时消除Verb层的Guided类重复，将引导瞄准逻辑上提到Verb_BDPRangedBase。覆盖双武器系统和齐射系统的适配兼容性。
---

# BDP投射物模块系统（PMS）重构设计

## 1. 问题背景

### 1.1 现状

当前BDP有两个弹道类：
- `Bullet_BDP : Bullet`（118行）——拖尾 + 引导飞行
- `Projectile_ExplosiveBDP : Projectile_Explosive`（111行）——拖尾 + 引导飞行

两者的逻辑**逐字相同**（字段、属性、SpawnSetup、TickInterval、InitGuidedFlight、ImpactSomething、ExposeData），唯一区别是基类不同。

Verb层同样存在重复：
- `Verb_BDPGuided`（124行）和 `Verb_BDPGuidedVolley`（85行）有 ~85% 代码重复

### 1.2 根因

C# 单继承限制。`Bullet` 和 `Projectile_Explosive` 并列继承 `Projectile`，BDP无法让两个弹道类共享中间基类。

### 1.3 扩展性问题

未来BDP需要更多弹道机制（追踪、穿透、分裂等），且需要**自由组合**。当前架构下每新增一种基类变体就要复制所有共享逻辑，组合数爆炸。

### 1.4 关键洞察

在BDP的设计理念中，**爆炸不是一种独立的投射物类型，而是对普通子弹的附加机制**——与拖尾、引导飞行等机制平等。这与引擎的设计理念不同，但完全可以在mod层面实现。

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
| 调用 base.Impact | 是 | **否** |
| 直接命中伤害 | `hitThing.TakeDamage(dinfo)` | **无** |
| AOE爆炸 | **无** | `GenExplosion.DoExplosion()` |
| 战斗日志 | `BattleLogEntry_RangedImpact` | **无** |
| BulletImpact通知 | 是 | **无** |
| 延迟引爆 | **无** | `ticksToDetonation` |

引擎将"命中单体"和"AOE爆炸"视为两种正交的投射物类型。原版XML中没有"命中+爆炸"双效果投射物。

## 3. 架构方案：统一基类 + 分层Hook模块组合

### 3.1 核心思想

- **统一基类**：所有BDP弹道统一继承 `Bullet`，只维护一个弹道类 `Bullet_BDP`
- **模块组合**：拖尾、引导、爆炸等机制各自封装为独立模块，挂载到弹道实例上
- **XML声明**：模块的存在由XML `modExtensions` 声明
- **运行时注入**：模块的动态数据由Verb端在发射后通过统一接口注入
- **弹道类无感知**：`Bullet_BDP` 只知道"我有一组模块"，不关心具体是什么模块
- **不硬编码行为**：接口只定义hook点，具体行为由模块实现决定

### 3.2 整体结构

```
引擎层（不可修改）：
  Projectile → Bullet

BDP弹道层：
  Bullet → Bullet_BDP（唯一弹道类，模块宿主薄壳）

BDP模块层：
  IBDPProjectileModule（接口）
    ├─ TrailModule          拖尾渲染
    ├─ GuidedModule         引导飞行（折线弹道）
    ├─ ExplosionModule      AOE爆炸
    └─ ...                  未来任意新机制，实现接口即可接入

配置层：
  DefModExtension 子类（BeamTrailConfig, BDPGuidedConfig, BDPExplosionConfig, ...）
    → XML中声明模块存在 + 静态参数

工厂层：
  BDPModuleFactory
    → SpawnSetup时扫描 def.modExtensions，自动创建对应模块实例
```

### 3.3 模块接口

```csharp
/// BDP弹道模块接口——所有弹道附加机制的统一抽象。
/// 设计原则：
///   - 接口只定义弹道生命周期中的 hook 点，不预设具体机制
///   - 每个 hook 都有明确的语义和调用时机
///   - 模块间通过宿主弹道的公开字段通信，不直接耦合
public interface IBDPProjectileModule : IExposable
{
    /// 执行优先级。数值越小越先执行。
    int Priority { get; }

    /// 弹道生成时调用。
    /// 时机：Bullet_BDP.SpawnSetup 中，base.SpawnSetup 之后。
    void OnSpawn(Bullet_BDP host, Map map);

    /// 每tick调用。
    /// 时机：Bullet_BDP.TickInterval 中，base.TickInterval 之后。
    void OnTick(Bullet_BDP host, Vector3 prevPos, Vector3 newPos);

    /// 弹道即将命中时调用（ImpactSomething 层）。
    /// 返回 true = "我拦截了此次 ImpactSomething"。
    /// 时机：base.ImpactSomething 之前。
    bool OnPreImpact(Bullet_BDP host);

    /// 弹道命中处理（Impact 层）。
    /// 返回 true = "我处理了 Impact，不要走默认 Bullet.Impact"。
    /// 时机：base.Impact 之前。
    /// 策略：first-handler-wins（第一个返回true的模块接管，后续不再调用OnImpact）。
    /// 原因：OnImpact处理者可能调用Destroy()，后续模块访问host不安全。
    bool OnImpact(Bullet_BDP host, Thing hitThing, bool blockedByShield,
                  Map map, IntVec3 pos);

    /// Impact完成后调用。无论谁处理的Impact，都会触发。
    /// 时机：Impact处理完毕后。此时弹道可能已Destroy。
    /// 参数为缓存值，不依赖host。
    void OnPostImpact(Thing hitThing, IntVec3 pos, Map map);
}
```

### 3.4 分层Hook设计

接口的5个hook分布在弹道生命周期的不同层级：

```
SpawnSetup
  └─ OnSpawn（初始化）

TickInterval
  └─ OnTick（持续行为）

ImpactSomething（命中判定层）
  └─ OnPreImpact（可拦截整个Impact流程）
       ↓ 未拦截
Impact（命中处理层）
  ├─ OnImpact（可替代默认Impact行为，first-handler-wins）
  │    ↓ 无人处理
  │  base.Impact（默认Bullet命中逻辑）
  └─ OnPostImpact（附加效果，全部调用）
```

关键分层：
- **OnPreImpact**（ImpactSomething层）：GuidedModule在此拦截锚点推进，阻止Impact发生
- **OnImpact**（Impact层）：ExplosionModule在此替代默认命中行为，改为爆炸
- 两层天然隔离，不需要模块间互相感知

### 3.5 Bullet_BDP 宿主实现

```csharp
public class Bullet_BDP : Bullet
{
    private List<IBDPProjectileModule> modules;

    // ── 模块访问 ──

    /// 供Verb端注入运行时数据
    public T GetModule<T>() where T : class, IBDPProjectileModule
    {
        if (modules == null) return null;
        for (int i = 0; i < modules.Count; i++)
            if (modules[i] is T t) return t;
        return null;
    }

    // ── 供模块修改飞行参数 ──

    /// 重定向飞行路径（GuidedModule锚点推进用）
    public void RedirectFlight(Vector3 newOrigin, Vector3 newDestination)
    {
        origin = newOrigin;
        destination = newDestination;
        ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
        if (ticksToImpact < 1) ticksToImpact = 1;
    }

    // ── 生命周期hook分发 ──

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad)
            modules = BDPModuleFactory.CreateModules(this, def);
        if (modules != null)
        {
            modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnSpawn(this, map);
        }
    }

    protected override void TickInterval(int delta)
    {
        Vector3 prev = DrawPos;
        base.TickInterval(delta);
        Vector3 cur = DrawPos;
        if (modules != null)
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnTick(this, prev, cur);
    }

    protected override void ImpactSomething()
    {
        if (modules != null)
        {
            bool intercepted = false;
            for (int i = 0; i < modules.Count; i++)
                if (modules[i].OnPreImpact(this))
                    intercepted = true;
            if (intercepted) return;
        }
        base.ImpactSomething();
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = base.Map;
        IntVec3 pos = base.Position;

        // OnImpact: first-handler-wins
        bool handled = false;
        if (modules != null)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].OnImpact(this, hitThing, blockedByShield, map, pos))
                {
                    handled = true;
                    break;
                }
            }
        }

        if (!handled)
            base.Impact(hitThing, blockedByShield);

        // OnPostImpact: 全部调用，传入缓存值
        if (modules != null)
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

序列化说明：`Scribe_Collections.Look` + `LookMode.Deep` 保存每个元素的具体类型名，反序列化时通过类型名反射创建实例。只要每个模块类有无参构造函数即可。读档时模块从ExposeData恢复，不走工厂。

### 3.6 模块工厂

```csharp
public static class BDPModuleFactory
{
    private static readonly Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>>
        registry = new();

    public static void Register<TConfig>(Func<TConfig, IBDPProjectileModule> creator)
        where TConfig : DefModExtension
    {
        registry[typeof(TConfig)] = ext => creator((TConfig)ext);
    }

    public static List<IBDPProjectileModule> CreateModules(Bullet_BDP host, ThingDef def)
    {
        var result = new List<IBDPProjectileModule>();
        if (def.modExtensions == null) return result;
        foreach (var ext in def.modExtensions)
            if (registry.TryGetValue(ext.GetType(), out var creator))
                result.Add(creator(ext));
        return result;
    }
}
```

注册时机（BDPMod构造函数或StaticConstructorOnStartup）：

```csharp
BDPModuleFactory.Register<BeamTrailConfig>(cfg => new TrailModule(cfg));
BDPModuleFactory.Register<BDPGuidedConfig>(cfg => new GuidedModule(cfg));
BDPModuleFactory.Register<BDPExplosionConfig>(cfg => new ExplosionModule(cfg));
```

### 3.7 具体模块职责

**TrailModule**（从现有Bullet_BDP拖尾逻辑提取）：
- `OnSpawn`：从BeamTrailConfig缓存Material
- `OnTick`：调用`BDPEffectMapComponent.CreateSegment()`
- 其他hook：空实现
- 配置类：`BeamTrailConfig`（已有，不变）

**GuidedModule**（从现有Bullet_BDP + GuidedFlightController提取）：
- 内部持有`GuidedFlightController`
- `OnSpawn`：空（等待Verb注入路径点）
- `OnPreImpact`：委托`GuidedFlightController.TryAdvanceWaypoint()`，成功则调用`host.RedirectFlight()`，返回true
- 其他hook：空实现
- 公开方法：`SetWaypoints(List<Vector3>)` 供Verb端注入
- 配置类：新建`BDPGuidedConfig : DefModExtension`（标记类，仅声明"此弹道支持引导"）
- `ExposeData`：序列化GuidedFlightController

**ExplosionModule**（替代Projectile_ExplosiveBDP）：
- `OnImpact`：调用`GenExplosion.DoExplosion()` + `host.Destroy()`，返回true
- 其他hook：空实现
- 配置类：新建`BDPExplosionConfig : DefModExtension`，字段包括`explosionRadius`、`explosionDamageDef`等
- `ExposeData`：无需额外序列化（配置来自def，运行时无状态）

### 3.8 数据流：静态配置 vs 运行时注入

```
模块的存在（"这颗子弹有什么能力"）
  → 统一由 XML modExtensions 声明
  → SpawnSetup 时由工厂自动创建

模块的参数
  ├─ 静态参数（拖尾宽度、爆炸半径等）
  │  → 来自 XML DefModExtension 字段
  │  → 模块构造时传入，不变
  │
  └─ 动态参数（引导路径点、追踪目标等）
     → 来自 Verb 端运行时数据
     → 发射后通过 GetModule<T>() 统一接口注入

弹道类对两者无感知，模块内部自行管理
```

### 3.9 模块间通信

模块之间不直接引用，通过两种方式间接通信：

1. **通过宿主的公开字段**：`origin`、`destination`、`DrawPos`等Projectile字段是所有模块的共享上下文
2. **通过执行顺序**：`Priority`控制模块执行顺序。路径修改类模块（Priority低）先执行，视觉效果类模块（Priority高）后执行

## 4. Verb层重构

### 4.1 消除Verb_BDPGuided和Verb_BDPGuidedVolley

引导瞄准逻辑上提到`Verb_BDPRangedBase`，通过`WeaponChipConfig.supportsGuided`条件化执行。

**Verb_BDPRangedBase新增内容：**
- `GuidedVerbState gs` 字段
- `StartGuidedTargeting()` 方法（从Verb_BDPGuided上提）
- `OrderForceTarget` 重写（引导模式拦截）
- `GetLosCheckTarget` 重写（引导模式返回第一锚点）
- `TryStartCastOn` 重写（引导模式拦截castTarg）
- `OnProjectileLaunched` 统一入口：`bdp.GetModule<GuidedModule>()?.SetWaypoints()`

**子类影响：**
- `Verb_BDPShoot`：不变
- `Verb_BDPVolley`：不变
- `Verb_BDPGuided`：**删除**
- `Verb_BDPGuidedVolley`：**删除**
- `Verb_BDPDualRanged`：简化，移除引导特殊处理，改用基类gs和OnProjectileLaunched
- `Verb_BDPDualVolley`：同上

### 4.2 双武器引导适配

`Verb_BDPDualRanged`和`Verb_BDPDualVolley`的双侧引导逻辑（`LeftIsGuided`/`RightIsGuided`、`CurrentShotIsGuided`、LOS检查）保留在Verb层，因为这是"哪一侧需要注入引导路径"的发射控制逻辑。

重构后：
- `GuidedVerbState`的双侧字段保留
- Dual Verb重写`OnProjectileLaunched`：只在`gs.CurrentShotIsGuided`为true时注入路径

### 4.3 双武器弹道类型差异

左右手不同弹道 = 不同ThingDef = 不同modExtensions = 不同模块组合。`Verb_BDPDualRanged`通过动态切换`verbProps.defaultProjectile`发射不同ThingDef的`Bullet_BDP`实例，各自从自己的ThingDef读取模块配置。天然支持，无需额外处理。

### 4.4 Command_BDPChipAttack适配

```csharp
// 旧：类型检查
if (verb is Verb_BDPGuided guided)
    guided.StartGuidedTargeting();

// 新：配置检查
if (verb is Verb_BDPRangedBase ranged && ranged.SupportsGuided)
    ranged.StartGuidedTargeting();
```

### 4.5 重构后Verb继承树

```
Verb_BDPRangedBase（内置引导瞄准支持，按配置条件化）
  ├─ Verb_BDPShoot（单发射击）
  ├─ Verb_BDPVolley（单侧齐射）
  ├─ Verb_BDPDualRanged（双侧交替连射）
  └─ Verb_BDPDualVolley（双侧齐射）
```

### 4.6 BDPGuidedConfig与WeaponChipConfig的分工

```
WeaponChipConfig.supportsGuided → Verb层：是否启用引导瞄准UI
WeaponChipConfig.maxAnchors     → Verb层：瞄准时限制锚点数量
WeaponChipConfig.anchorSpread   → Verb层：构建路径点时应用散布
BDPGuidedConfig（存在即启用）    → 弹道层：弹道支持引导飞行
```

芯片XML中verbClass的变化：
- `Verb_BDPGuided` → `Verb_BDPShoot`
- `Verb_BDPGuidedVolley` → `Verb_BDPVolley`

## 5. XML配置示例

所有BDP弹道统一使用`thingClass=BDP.Trigger.Bullet_BDP`，机制差异完全由`modExtensions`表达：

- 无modExtensions → 普通子弹（等同原版Bullet行为）
- 加`BeamTrailConfig` → 拖尾
- 加`BDPGuidedConfig` → 支持引导飞行
- 加`BDPExplosionConfig` → 爆炸
- 任意组合 → 自由叠加

示例：引导 + 拖尾 + 爆炸三种机制组合

```xml
<ThingDef ParentName="BaseBullet">
  <defName>BDP_Bullet_GuidedExplosive</defName>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <projectile>
    <damageDef>Bomb</damageDef>
    <damageAmountBase>8</damageAmountBase>
    <speed>55</speed>
  </projectile>
  <modExtensions>
    <li Class="BDP.Trigger.BeamTrailConfig">
      <trailWidth>0.15</trailWidth>
      <trailColor>(0.4, 1.0, 0.45, 1.0)</trailColor>
      <segmentDuration>30</segmentDuration>
      <decaySharpness>10.0</decaySharpness>
    </li>
    <li Class="BDP.Trigger.BDPGuidedConfig" />
    <li Class="BDP.Trigger.BDPExplosionConfig">
      <explosionRadius>2.9</explosionRadius>
    </li>
  </modExtensions>
</ThingDef>
```

## 6. 文件级影响清单

### 删除（3个文件）

| 文件 | 原因 |
|---|---|
| `Projectile_ExplosiveBDP.cs` | 由ExplosionModule替代 |
| `Verb_BDPGuided.cs` | 逻辑上提到Verb_BDPRangedBase |
| `Verb_BDPGuidedVolley.cs` | 同上 |

### 新建（7个文件）

| 文件 | 职责 |
|---|---|
| `IBDPProjectileModule.cs` | 模块接口 |
| `BDPModuleFactory.cs` | 模块工厂 |
| `TrailModule.cs` | 拖尾模块 |
| `GuidedModule.cs` | 引导飞行模块 |
| `ExplosionModule.cs` | 爆炸模块 |
| `BDPGuidedConfig.cs` | 引导配置（标记类） |
| `BDPExplosionConfig.cs` | 爆炸配置 |

### 重写（1个文件）

| 文件 | 说明 |
|---|---|
| `Bullet_BDP.cs` | 模块宿主薄壳 |

### 修改（6个文件）

| 文件 | 变化 |
|---|---|
| `Verb_BDPRangedBase.cs` | 新增引导瞄准支持（+~85行） |
| `Verb_BDPDualRanged.cs` | 简化引导处理（-~30行） |
| `Verb_BDPDualVolley.cs` | 同上（-~30行） |
| `GuidedVerbState.cs` | AttachGuidedFlight改用GetModule |
| `Command_BDPChipAttack.cs` | 类型检查改为配置检查 |
| XML定义文件 | thingClass和verbClass替换 |

### 不受影响的子系统

微内核（CompTrion等）、槽位状态机（CompTriggerBody）、芯片效果（IChipEffect）、近战Verb（Verb_BDPMelee）、DualVerbCompositor、UI子系统——全部无变化。

## 7. 设计原则总结

1. **统一基类**：只有一个弹道类`Bullet_BDP`，消除继承分叉导致的代码重复
2. **模块平等**：所有附加机制地位平等，都是模块
3. **积木式组合**：模块的存在统一由XML声明，弹道类无感知
4. **分层Hook**：ImpactSomething层和Impact层分离，模块间天然隔离
5. **不硬编码行为**：接口只定义hook点，具体行为由模块实现决定
6. **数据来源透明**：静态参数来自XML，动态参数来自Verb端运行时注入

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-25 | 创建文档。基于doc1(M3问题分析)和doc2(模块化架构初稿)的讨论，确定方案C（统一Bullet基类+分层Hook模块组合），完成接口设计、宿主实现、工厂、Verb层重构、双武器/齐射兼容性分析、文件影响清单。 | Claude Opus 4.6 |
