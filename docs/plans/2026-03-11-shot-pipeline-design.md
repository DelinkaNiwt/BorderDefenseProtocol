---
标题：射击系统管线化重构设计
版本号: v1.0
更新日期: 2026-03-11
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 将BDP射击系统从半管线化（仅弹道层）重构为全管线化（瞄准→射击→弹道），定义3阶段管线架构、接口体系、数据模型、模块映射和文件结构
---

# 射击系统管线化重构设计

## 1. 背景与动机

### 1.1 现状

BDP射击系统目前是**半管线化**状态：

| 层级 | 管线化？ | 现状 |
|------|---------|------|
| **Projectile层** (Bullet_BDP) | ✅ 已管线化 | v5 8阶段Pipeline，模块产出意图，宿主执行 |
| **Verb层** (瞄准+射击) | ❌ 未管线化 | 逻辑嵌入各Verb子类（Single/Dual/Combo），重复代码多 |
| **Verb→Projectile桥梁** | ⚠️ 手动注入 | VerbFlightState + OnProjectileLaunched 回调 |

### 1.2 问题

1. **代码重复**：Single/Dual/Combo 各自实现 TryCastShot()，LOS检查、Trion消耗等逻辑重复
2. **扩展困难**：添加新射击行为（蓄力、后坐力、弹药消耗等）需要修改每个Verb子类
3. **架构不一致**：弹道层有管线，瞄准/射击层没有，心智模型不统一

### 1.3 目标

1. **消除Verb子类间的重复代码**——共享逻辑迁入管线模块
2. **支持射击行为模块化扩展**——新功能通过模块注入，不改Verb代码
3. **统一Verb层和Projectile层的管线模型**——全流程使用「模块产出意图，宿主执行」模式

## 2. 总体架构

### 2.1 三阶段管线 + 嵌套模型

```
┌─────────────────────────────────────────────────────────┐
│  ShotPipeline（射击管线，Verb层持有，无状态）             │
│  ShotSession（射击会话，每次射击新建，有状态）             │
│                                                         │
│  ┌────────────────────────────┐                         │
│  │ Phase 1: Aiming（瞄准）     │                        │
│  │ ┌──────────┐ ┌───────────┐ │                         │
│  │ │Targeting │→│ Resolve   │ │                         │
│  │ │玩家交互  │ │ 内部解算  │ │                         │
│  │ │(多帧)    │ │ (单Tick)  │ │                         │
│  │ └──────────┘ └───────────┘ │                         │
│  │         产出 → AimResult    │                         │
│  ├────────────────────────────┤                         │
│  │ Phase 2: Firing（射击）     │                         │
│  │ (单Tick，TryCastShot内)    │                         │
│  │         产出 → FireResult   │                         │
│  ├────────────────────────────┤                         │
│  │ Phase 3: Ballistics（弹道） │                         │
│  │ = 现有v5管线 8阶段         │                         │
│  │ 输入：FireResult            │                         │
│  └────────────────────────────┘                         │
└─────────────────────────────────────────────────────────┘
```

### 2.2 核心原则（继承v5管线）

| 原则 | 说明 |
|------|------|
| **模块只产意图** | 模块通过 Intent 结构体表达"想做什么"，不直接执行副作用 |
| **宿主统一执行** | ShotPipeline 收集并合并所有模块意图后，由宿主执行 |
| **上下文只读** | 模块读 ShotContext，写 Intent。不可修改上下文 |
| **Priority排序** | 同一阶段内多个模块按 Priority 升序执行 |
| **XML注入** | 模块列表通过 VerbChipConfig 的 DefModExtension 配置 |
| **零耦合** | 模块之间不直接通信，通过上下文/意图中转 |

### 2.3 生命周期

```
ShotPipeline（管线结构）
  持有者：Verb
  生命周期：随Verb创建，芯片变更时重建
  内容：模块列表（按Priority排序）
  性质：无状态，可复用

ShotSession（管线状态）
  持有者：Verb.activeSession 字段
  生命周期：玩家进入瞄准模式时创建，射击完成后清空
  内容：ShotContext、AimResult、FireResult、瞄准累积状态
  性质：有状态，每次射击一个新实例
```

### 2.4 管线驱动点

```csharp
class Verb_BDPRangedBase
{
    ShotPipeline pipeline;       // 无状态，Verb持有
    ShotSession activeSession;   // 有状态，每次射击新建

    // 驱动点1：玩家进入瞄准模式
    void OnTargetingStart()
    {
        activeSession = new ShotSession(BuildContext());
        pipeline.BeginTargeting(activeSession);
    }

    // 驱动点2：每帧渲染（瞄准模式中）
    void DrawHighlight()
    {
        pipeline.RenderTargeting(activeSession, currentMouseTarget);
    }

    // 驱动点3：玩家确认目标 → TryCastShot
    bool TryCastShot()
    {
        // Phase 1 收尾：Resolve
        var aimResult = pipeline.ResolveAim(activeSession);
        if (aimResult.Abort) return false;

        // Phase 2：Firing
        var fireResult = pipeline.RunFire(activeSession);
        if (fireResult.Abort) return false;

        // Phase 3 入口：子类差异点
        return ExecuteFire(activeSession);
    }

    // 子类差异点
    protected abstract bool ExecuteFire(ShotSession session);
}
```

### 2.5 数据流

```
ShotContext（只读快照）
    ├─→ Targeting子步骤：模块渲染指示器，在Session中累积状态
    ├─→ Resolve子步骤：模块产出AimIntent → 宿主合并 → AimResult
    ├─→ Fire阶段：模块产出FireIntent → 宿主合并 → FireResult
    └─→ Ballistics：FireResult → Bullet_BDP.InjectShotData → v5管线
```

## 3. 数据模型

### 3.1 ShotContext（只读上下文）

```csharp
/// <summary>
/// 射击上下文：管线入口的初始数据快照，整个管线期间只读
/// </summary>
public class ShotContext
{
    // 施法者信息
    public Pawn Caster;
    public CompTriggerBody TriggerComp;

    // 目标信息
    public LocalTargetInfo Target;
    public IntVec3 CasterPosition;

    // 武器信息
    public Verb_BDPRangedBase Verb;
    public VerbChipConfig ChipConfig;
    public SlotSide? ChipSide;
    public Thing ChipThing;

    // 配置快照
    public RangedConfig RangedConfig;
    public GuidedConfig GuidedConfig;
    public FiringPattern FiringPattern;
    public VerbProperties VerbProps;
}
```

### 3.2 AimIntent / AimResult

```csharp
/// <summary>瞄准意图：模块产出</summary>
public struct AimIntent
{
    // 目标修正
    public LocalTargetInfo? OverrideTarget;
    public Vector3? AimOffset;

    // 引导瞄准
    public List<IntVec3> AnchorPoints;
    public float? AnchorSpread;

    // 控制标志
    public bool AbortShot;
    public string AbortReason;

    // 精度修正
    public float AccuracyMultiplier;     // 默认1.0
    public float ForcedMissRadius;
}

/// <summary>瞄准结果：宿主合并所有AimIntent后产出</summary>
public class AimResult
{
    public LocalTargetInfo FinalTarget;
    public Vector3 AimPoint;
    public List<IntVec3> AnchorPath;
    public float AnchorSpread;
    public float AccuracyMultiplier;
    public float ForcedMissRadius;
    public bool Abort;
    public string AbortReason;
    public bool HasGuidedPath => AnchorPath?.Count > 0;
}
```

### 3.3 FireIntent / FireResult

```csharp
/// <summary>射击意图：模块产出</summary>
public struct FireIntent
{
    // 投射物
    public ThingDef OverrideProjectileDef;

    // 发射参数修正
    public float SpreadRadius;
    public float DamageMultiplier;     // 默认1.0
    public float SpeedMultiplier;      // 默认1.0

    // 资源消耗
    public float TrionCost;
    public bool SkipTrionConsumption;

    // 控制标志
    public bool AbortShot;
    public string AbortReason;

    // 弹道管线注入
    public bool EnableAutoRoute;
    public ThingDef AutoRouteProjectileDef;
}

/// <summary>射击结果：宿主合并所有FireIntent后产出</summary>
public class FireResult
{
    public ThingDef ProjectileDef;
    public float SpreadRadius;
    public float DamageMultiplier;
    public float SpeedMultiplier;
    public float TrionCost;
    public bool EnableAutoRoute;
    public ThingDef AutoRouteProjectileDef;
    public bool Abort;
    public string AbortReason;
}
```

### 3.4 意图合并规则

```
AimIntent 合并：
  OverrideTarget     → 最后一个非null者生效（高Priority覆盖低Priority）
  AimOffset          → 累加
  AnchorPoints       → 最后一个非null者生效
  AbortShot          → 任一true则true（一票否决）
  AccuracyMultiplier → 连乘
  ForcedMissRadius   → 取最大值

FireIntent 合并：
  OverrideProjectileDef → 最后一个非null者生效
  SpreadRadius          → 累加
  DamageMultiplier      → 连乘
  SpeedMultiplier       → 连乘
  TrionCost             → 累加
  SkipTrionConsumption  → 任一true则true
  AbortShot             → 任一true则true
```

### 3.5 ShotSession

```csharp
/// <summary>
/// 射击会话：每次射击的有状态数据容器
/// </summary>
public class ShotSession
{
    // 只读上下文（构造时设置）
    public ShotContext Context { get; }

    // 瞄准阶段累积状态（Targeting子步骤期间模块可写）
    public List<IntVec3> AnchorPath;
    public LocalTargetInfo ValidatedTarget;

    // 阶段结果（宿主写入）
    public AimResult AimResult { get; internal set; }
    public FireResult FireResult { get; internal set; }
}
```

## 4. 接口体系

### 4.1 模块基接口

```csharp
public interface IShotModule
{
    int Priority { get; }
}
```

### 4.2 瞄准阶段接口

```csharp
/// <summary>瞄准模块（必须）：参与Resolve子步骤</summary>
public interface IShotAimModule : IShotModule
{
    AimIntent ResolveAim(ShotSession session);
}

/// <summary>可选：参与Targeting子步骤的渲染</summary>
public interface IShotAimRenderer
{
    void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget);
}

/// <summary>可选：参与Targeting子步骤的目标验证</summary>
public interface IShotAimValidator
{
    AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target);
}

public struct AimValidation
{
    public bool IsValid;
    public string InvalidReason;
}
```

### 4.3 射击阶段接口

```csharp
/// <summary>射击模块</summary>
public interface IShotFireModule : IShotModule
{
    FireIntent OnFire(ShotSession session);
}
```

### 4.4 模块接口组合示例

```csharp
// 锚点引导：渲染 + 解算
public class AnchorAimModule : IShotAimModule, IShotAimRenderer { ... }

// LOS检查：验证 + 解算
public class LosCheckModule : IShotAimModule, IShotAimValidator { ... }

// 范围指示器：仅渲染
public class AreaIndicatorModule : IShotAimRenderer { ... }

// Trion消耗：射击阶段
public class TrionCostModule : IShotFireModule { ... }
```

## 5. 现有功能迁移映射

### 5.1 瞄准阶段模块

| # | 模块 | 现有代码位置 | 职责 | Priority | 接口 |
|---|------|-------------|------|----------|------|
| 1 | LosCheckModule | 各Verb的TryCastShot内LOS检查 | 视线验证 | 10 | IShotAimModule + IShotAimValidator |
| 2 | AnchorAimModule | StartAnchorTargeting() + AnchorTargetingHelper | 引导锚点选择与预览 | 20 | IShotAimModule + IShotAimRenderer |
| 3 | AutoRouteAimModule | VerbFlightState.TryStartCastOn绕行重定向 | 自动绕行LOS替代 | 30 | IShotAimModule + IShotAimValidator |
| 4 | AreaIndicatorModule | DrawAreaIndicators()虚方法 | 范围圈渲染 | 40 | IShotAimRenderer |

### 5.2 射击阶段模块

| # | 模块 | 现有代码位置 | 职责 | Priority |
|---|------|-------------|------|----------|
| 5 | VolleySpreadModule | 各Verb内volleySpreadRadius | 齐射散布 | 10 |
| 6 | TrionCostModule | ChipUsageCostHelper | Trion消耗 | 50 |
| 7 | FlightDataModule | OnProjectileLaunched() + gs.AttachManualFlight | 引导弹道数据 | 60 |
| 8 | AutoRouteFireModule | gs.AttachAutoRouteFlight() + ObstacleRouter | 自动绕行路由 | 70 |

### 5.3 不迁移（保留在原位）

| 功能 | 原因 |
|------|------|
| FiringPattern判断 | 发射编排逻辑，留在Verb子类ExecuteFire() |
| Dual交替计数 | Verb_BDPDual内部状态 |
| Combo参数平均化 | Verb_BDPCombo内部逻辑 |
| burst管理 | Vanilla Verb内置机制 |

### 5.4 弹道阶段

v5管线完全不变。数据注入方式从 `gs.AttachXxx()` 改为 `bdpBullet.InjectShotData(session)`。

### 5.5 VerbFlightState 迁移

| VerbFlightState职责 | 迁移目标 |
|--------------------|---------|
| 缓存锚点数据 | ShotSession.AnchorPath |
| 自动绕行路由结果 | AutoRouteFireModule的FireIntent |
| LOS检查重定向 | LosCheckModule/AutoRouteAimModule |
| AttachManualFlight/AttachAutoRouteFlight | FlightDataModule/AutoRouteFireModule |
| TryStartCastOn拦截 | AutoRouteAimModule的ValidateTarget |

**VerbFlightState类将被删除。**

## 6. Verb子类重构

### 6.1 基类 Verb_BDPRangedBase

管线驱动逻辑集中在基类，子类只实现 `ExecuteFire()`：

```csharp
class Verb_BDPRangedBase
{
    ShotPipeline pipeline;
    ShotSession activeSession;

    // TryCastShot 统一流程（不再由子类各自实现）
    bool TryCastShot()
    {
        var aimResult = pipeline.ResolveAim(activeSession);
        if (aimResult.Abort) return false;

        var fireResult = pipeline.RunFire(activeSession);
        if (fireResult.Abort) return false;

        return ExecuteFire(activeSession);
    }

    protected abstract bool ExecuteFire(ShotSession session);

    // 统一发射工具方法
    protected bool LaunchProjectile(ShotSession session, SlotSide? side)
    {
        // 获取投射物Def、计算位置、应用散布、发射、注入弹道数据
    }
}
```

### 6.2 子类精简

```csharp
class Verb_BDPSingle : Verb_BDPRangedBase
{
    protected override bool ExecuteFire(ShotSession session)
    {
        // 根据FiringPattern发射（Sequential/Simultaneous）
    }
}

class Verb_BDPDual : Verb_BDPRangedBase
{
    protected override bool ExecuteFire(ShotSession session)
    {
        // 双侧交替发射
    }
}

class Verb_BDPCombo : Verb_BDPRangedBase
{
    protected override bool ExecuteFire(ShotSession session)
    {
        // 混合参数发射
    }
}
```

## 7. XML配置

### 7.1 模块配置方式

```xml
<ThingDef ParentName="BDPChipBase">
  <defName>Chip_GuidedMissile</defName>
  <modExtensions>
    <li Class="BDP.VerbChipConfig">
      <primaryVerbProps>...</primaryVerbProps>

      <!-- 射击管线模块 -->
      <aimModules>
        <li Class="BDP.LosCheckConfig">
          <priority>10</priority>
        </li>
        <li Class="BDP.AnchorAimConfig">
          <priority>20</priority>
          <anchorSpread>1.5</anchorSpread>
        </li>
      </aimModules>

      <fireModules>
        <li Class="BDP.TrionCostConfig">
          <priority>50</priority>
        </li>
        <li Class="BDP.FlightDataConfig">
          <priority>60</priority>
        </li>
      </fireModules>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.2 Config → Module 模式

```csharp
public abstract class ShotModuleConfig
{
    public int priority = 50;
    public abstract IShotModule CreateModule();
}
```

### 7.3 默认模块策略

ShotPipeline.Build() 自动注入默认模块（LosCheck、AreaIndicator、TrionCost），XML配置的模块作为额外/覆盖。

## 8. 文件结构

```
Source/BDP/
├── Trigger/
│   ├── ShotPipeline/                    ← 新增
│   │   ├── ShotPipeline.cs
│   │   ├── ShotSession.cs
│   │   ├── ShotContext.cs
│   │   ├── Data/
│   │   │   ├── AimIntent.cs
│   │   │   ├── AimResult.cs
│   │   │   ├── AimValidation.cs
│   │   │   ├── FireIntent.cs
│   │   │   └── FireResult.cs
│   │   ├── Interfaces/
│   │   │   ├── IShotModule.cs
│   │   │   ├── IShotAimModule.cs
│   │   │   ├── IShotAimRenderer.cs
│   │   │   ├── IShotAimValidator.cs
│   │   │   └── IShotFireModule.cs
│   │   ├── Modules/
│   │   │   ├── LosCheckModule.cs
│   │   │   ├── AnchorAimModule.cs
│   │   │   ├── AutoRouteAimModule.cs
│   │   │   ├── AreaIndicatorModule.cs
│   │   │   ├── VolleySpreadModule.cs
│   │   │   ├── TrionCostModule.cs
│   │   │   ├── FlightDataModule.cs
│   │   │   └── AutoRouteFireModule.cs
│   │   └── Config/
│   │       ├── ShotModuleConfig.cs
│   │       ├── LosCheckConfig.cs
│   │       ├── AnchorAimConfig.cs
│   │       ├── AutoRouteAimConfig.cs
│   │       ├── VolleySpreadConfig.cs
│   │       ├── TrionCostConfig.cs
│   │       ├── FlightDataConfig.cs
│   │       └── AutoRouteFireConfig.cs
│   ├── DualWeapon/                      ← 重构
│   │   ├── Verb_BDPRangedBase.cs        ← 集成ShotPipeline
│   │   ├── Verb_BDPSingle.cs            ← 精简为ExecuteFire
│   │   ├── Verb_BDPDual.cs              ← 精简为ExecuteFire
│   │   ├── Verb_BDPCombo.cs             ← 精简为ExecuteFire
│   │   └── ...
│   └── Data/
│       └── VerbChipConfig.cs            ← 新增aimModules/fireModules字段
├── Projectiles/                          ← 小改
│   └── Bullet_BDP.cs                    ← 新增InjectShotData()
│
│ ── 删除 ──
│ VerbFlightState.cs                     ← 功能迁入管线
```

## 9. 与v5弹道管线的关系

射击管线的 FireResult 作为弹道管线的输入，替代旧的 VerbFlightState 手动注入：

```
ShotPipeline (Phase 1+2)        Bullet_BDP Pipeline (Phase 3)
      瞄准 → 射击
      产出 FireResult ──桥梁──→ InjectShotData(session)
                                   ├─ PostLaunchInit
                                   ├─ LifecycleCheck
                                   ├─ FlightIntent
                                   ├─ base.TickInterval
                                   ├─ PositionModifier
                                   ├─ VisualObserve
                                   ├─ ArrivalPolicy
                                   ├─ HitResolve
                                   └─ Impact
```

## 10. 未来可扩展模块示例

验证接口设计的扩展性（不在本次实现）：

| 模块 | 阶段 | 功能 |
|------|------|------|
| AimPredictionModule | 瞄准 | 移动目标提前量预测 |
| ChargeAimModule | 瞄准 | 蓄力射击精度提升 |
| SuppressionAimModule | 瞄准 | 压制射击扩大散布 |
| RecoilModule | 射击 | 连射后坐力累积 |
| AmmoCostModule | 射击 | 弹药消耗检查 |
| OverheatModule | 射击 | 过热检测与终止 |
| CriticalHitModule | 射击 | 暴击倍率注入 |

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-11 | 完成射击系统管线化重构设计 | Claude Opus 4.6 |
