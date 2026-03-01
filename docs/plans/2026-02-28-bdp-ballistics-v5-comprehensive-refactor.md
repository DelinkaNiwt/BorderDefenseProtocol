---
标题：BDP弹道管线v5综合重构方案
版本号: v1.0
更新日期: 2026-02-28
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 综合Kernel V2方案与Pipeline v5方案，结合用户决策（全新接口组 + 一次性替换 + 距离策略配置化），形成最终架构修正方案。核心：模块只产出意图，宿主统一执行；全新接口组替换v4接口；FlightPhase状态机替代共享字段；距离策略可配置化。
---

# BDP弹道管线v5综合重构方案

## 1. 用户决策记录

| 决策点 | 选项 | 用户选择 | 影响 |
|--------|------|---------|------|
| 接口策略 | 增量扩展 / 全新接口组 / 混合 | **全新接口组** | 所有管线接口重新设计，现有模块全部适配 |
| 迁移策略 | Feature Flag / 双管线并行 / 一次性替换 | **一次性替换** | 无回滚机制，需充分测试后一次切换 |
| 距离策略 | 只搬位置 / 本次配置化 | **本次一并配置化** | 阈值提取到配置，ApplyFlightRedirect参数化 |

## 2. 解决的问题清单

| 编号 | 问题 | 解决方式 | 来源 |
|------|------|---------|------|
| P1 | 命中判定越权（usedTarget同步） | IBDPHitResolver接口 | 双报告一致 |
| P2 | 生命周期越权（OnTick自毁） | IBDPLifecyclePolicy接口 | 双报告一致 |
| P3 | origin后退hack分散 | 收归ApplyFlightRedirect | 双报告一致 |
| P4 | Guided/Tracking隐式耦合 | FlightPhase状态机 | 双报告一致 |
| P5 | 初始化时序多源真相 | PostLaunchInit + Phase初始化集中 | 双报告一致 |
| P7 | PathResolver越权改origin/TTI | 新接口只输出意图，宿主执行重定向 | GPT发现 |
| P8 | TickObserver执行Destroy | 新接口IVisualObserver强制只读 | GPT发现 |
| P9 | Argus trackingDelay注释不一致 | 修正XML注释 | GPT发现 |
| P10 | 距离策略硬编码 | 提取到FlightRedirectConfig | Claude发现 |
| P11 | GuidedVerbState职责过重 | **延后**（独立任务） | Claude发现 |

## 3. 新管线架构（v5）

### 3.1 每tick执行顺序（8阶段）

```
阶段0: PostLaunchInit          — 宿主内部，修复vanilla时序
阶段1: LifecycleCheck          — IBDPLifecyclePolicy
                                  模块报告状态，宿主决定销毁/Phase转换
阶段2: FlightIntent            — IBDPFlightIntentProvider
                                  模块输出FlightIntent（目标点+阶段建议）
                                  宿主执行ApplyFlightRedirect()
阶段3: base.TickInterval       — vanilla引擎位置插值 + 拦截检查 + 到达判定
阶段4: PositionModifier        — IBDPPositionModifier（不变）
阶段5: VisualObserve           — IBDPVisualObserver（纯只读，禁止副作用）
```

### 3.2 到达/命中执行顺序

```
阶段6: ArrivalPolicy           — IBDPArrivalPolicy
                                  ctx.Continue=true时宿主执行重定向+Phase转换
阶段7: HitResolve              — IBDPHitResolver
                                  模块可设ForceGround或OverrideTarget
                                  宿主根据结果决定Impact(who)
阶段8: Impact                  — IBDPImpactHandler（不变）
```

### 3.3 与v4的差异

| 维度 | v4 | v5 |
|------|----|-----|
| 管线阶段数 | 5（PathResolve→Engine→PositionMod→TickObserve→Arrival/Impact） | 8（+LifecycleCheck, +HitResolve, TickObserver→VisualObserver） |
| 飞行参数重算 | 模块调用host.RedirectFlight* | 宿主内部ApplyFlightRedirect |
| 模块间协作 | 共享宿主字段（IsOnFinalSegment等） | FlightPhase状态机，模块只读 |
| 生命周期 | TickObserver中Destroy | LifecyclePolicy中RequestDestroy |
| 命中修正 | 模块直接写usedTarget | HitResolver中报告，宿主执行 |
| vanilla适配 | 分散在模块和宿主中 | 集中在ApplyFlightRedirect |
| 距离策略 | 硬编码在RedirectFlightTracking | 配置化FlightRedirectConfig |

## 4. 飞行阶段状态机

### 4.1 状态定义

```csharp
/// <summary>
/// 子弹飞行阶段。宿主独占写权限，模块只读。
/// </summary>
public enum FlightPhase
{
    Direct,         // 普通直飞（无模块干预）
    GuidedLeg,      // 引导段（飞向中间锚点）
    FinalApproach,  // 最终段（飞向最终目标）
    Tracking,       // 追踪中（每tick修正方向）
    TrackingLost,   // 追踪丢失（等待重锁或超时）
    Free            // 自由飞行（追踪彻底过期，等待自然命中或超时销毁）
}
```

### 4.2 状态转换规则

```
Direct ──(SetWaypoints)──→ GuidedLeg
Direct ──(追踪激活)──→ Tracking

GuidedLeg ──(最后锚点到达)──→ FinalApproach

FinalApproach ──(追踪激活)──→ Tracking

Tracking ──(脱锁/目标无效)──→ TrackingLost

TrackingLost ──(重锁成功)──→ Tracking
TrackingLost ──(超时)──→ Free

Free ──(命中/超时)──→ 销毁
```

### 4.3 转换触发机制

模块不直接修改Phase，通过管线上下文报告，宿主统一转换：

| 转换 | 触发者 | 报告方式 | 宿主执行 |
|------|--------|---------|---------|
| Direct→GuidedLeg | GuidedModule.OnSpawn | 宿主方法SetWaypoints | Phase = GuidedLeg |
| GuidedLeg→FinalApproach | GuidedModule.HandleArrival | ctx.RequestPhaseChange | Phase = FinalApproach |
| FinalApproach→Tracking | TrackingModule.ProvideIntent | intent.TrackingActivated | Phase = Tracking |
| Tracking→TrackingLost | TrackingModule.CheckLifecycle | ctx.RequestPhaseChange | Phase = TrackingLost |
| TrackingLost→Tracking | TrackingModule.CheckLifecycle | ctx.RequestPhaseChange | Phase = Tracking |
| TrackingLost→Free | TrackingModule.CheckLifecycle | ctx.RequestPhaseChange | Phase = Free |

## 5. 全新接口定义

### 5.1 接口总览（v4→v5映射）

| v4接口 | v5接口 | 变化说明 |
|--------|--------|---------|
| IBDPProjectileModule | IBDPProjectileModule | **不变**（基础模块接口） |
| IBDPPathResolver | IBDPFlightIntentProvider | **替换**：输出FlightIntent而非直接改Destination |
| IBDPTickObserver | IBDPVisualObserver | **替换**：强制只读语义，禁止任何副作用 |
| IBDPArrivalHandler | IBDPArrivalPolicy | **替换**：输出ArrivalDecision，增加PhaseChange请求 |
| IBDPImpactHandler | IBDPImpactHandler | **不变**（无越权问题） |
| IBDPPositionModifier | IBDPPositionModifier | **不变**（无越权问题） |
| IBDPSpeedModifier | IBDPSpeedModifier | **不变**（一次性，无越权问题） |
| （无） | IBDPLifecyclePolicy | **新增**：生命周期决策 |
| （无） | IBDPHitResolver | **新增**：命中目标修正 |

### 5.2 IBDPLifecyclePolicy

```csharp
/// <summary>
/// 生命周期策略——模块报告自身状态，宿主决定是否终止子弹。
/// 执行时机：阶段1（FlightIntent之前）。
/// 权限：只能通过ctx请求，不可直接调用host.Destroy()。
/// </summary>
public interface IBDPLifecyclePolicy
{
    void CheckLifecycle(Bullet_BDP host, ref LifecycleContext ctx);
}

public struct LifecycleContext
{
    // 当前Phase（只读，供模块判断）
    public readonly FlightPhase CurrentPhase;

    // 模块请求销毁子弹
    public bool RequestDestroy;
    // 销毁原因（供日志和调试）
    public string DestroyReason;
    // 请求Phase转换（如Tracking→TrackingLost）
    public FlightPhase? RequestPhaseChange;
}
```

### 5.3 IBDPFlightIntentProvider

```csharp
/// <summary>
/// 飞行意图提供者——模块输出"下一步往哪飞"的意图，宿主统一执行重定向。
/// 执行时机：阶段2（LifecycleCheck之后，base.TickInterval之前）。
/// 权限：只能填写FlightIntent，不可调用host的任何重定向方法。
/// </summary>
public interface IBDPFlightIntentProvider
{
    void ProvideIntent(Bullet_BDP host, ref FlightIntentContext ctx);
}

public struct FlightIntentContext
{
    // 当前状态（只读）
    public readonly Vector3 CurrentPosition;
    public readonly Vector3 CurrentDestination;
    public readonly FlightPhase CurrentPhase;

    // 模块输出的意图
    public FlightIntent? Intent;
}

public struct FlightIntent
{
    // 目标点（必填）
    public Vector3 TargetPosition;
    // 追踪已激活标志（供宿主Phase转换）
    public bool TrackingActivated;
}
```

### 5.4 IBDPVisualObserver

```csharp
/// <summary>
/// 视觉观察者——纯只读，用于拖尾/特效/音效/日志。
/// 执行时机：阶段5（所有逻辑阶段完成后）。
/// 权限：禁止修改host任何状态，禁止调用Destroy/Impact。
/// </summary>
public interface IBDPVisualObserver
{
    void Observe(Bullet_BDP host);
}
```

### 5.5 IBDPArrivalPolicy

```csharp
/// <summary>
/// 到达策略——决定子弹到达目的地后继续飞还是进入命中流程。
/// 执行时机：阶段6（ticksToImpact≤0时）。
/// 权限：通过ctx表达决策，不可直接调用重定向方法。
/// </summary>
public interface IBDPArrivalPolicy
{
    void DecideArrival(Bullet_BDP host, ref ArrivalContext ctx);
}

public struct ArrivalContext
{
    // 当前状态（只读）
    public readonly FlightPhase CurrentPhase;

    // 继续飞行（true=跳过Impact，宿主执行重定向）
    public bool Continue;
    // 下一个目标点（Continue=true时有效）
    public Vector3 NextDestination;
    // 请求Phase转换
    public FlightPhase? RequestPhaseChange;
}
```

### 5.6 IBDPHitResolver

```csharp
/// <summary>
/// 命中修正器——在vanilla ImpactSomething之前修正命中目标。
/// 执行时机：阶段7（ArrivalPolicy决定不Continue之后）。
/// 权限：通过ctx表达修正意图，宿主统一应用到usedTarget。
/// </summary>
public interface IBDPHitResolver
{
    void ResolveHit(Bullet_BDP host, ref HitContext ctx);
}

public struct HitContext
{
    // 当前状态（只读）
    public readonly FlightPhase CurrentPhase;
    public readonly Thing VanillaHitThing;  // vanilla原始命中目标

    // 强制打地面（忽略usedTarget）
    public bool ForceGround;
    // 覆盖命中目标
    public Thing OverrideTarget;
}
```

## 6. 飞行重定向配置化

### 6.1 FlightRedirectConfig

```csharp
/// <summary>
/// 飞行重定向参数配置。从Def扩展读取，控制ApplyFlightRedirect行为。
/// </summary>
public class FlightRedirectConfig
{
    // origin后退距离（vanilla拦截适配）
    // 原因：vanilla的InterceptChanceFactorFromDistance在origin到拦截格距离²≤25时返回0
    // 每tick重定向导致origin贴近子弹，使前方墙壁拦截失效，需后退origin恢复拦截概率
    public float originOffset = 6f;

    // 远距离阈值（距离 > farThreshold × speedPerTick 时使用固定tick策略）
    // 原因：远距离时精确计算ticksToImpact意义不大（追踪每tick都会修正），
    // 固定tick可避免频繁大幅修改ticksToImpact导致的插值抖动
    public float farDistanceSpeedMult = 3f;

    // 远距离固定飞行tick数
    public int farDistanceFixedTicks = 60;

    // 是否需要origin偏移的Phase集合（默认：GuidedLeg/Tracking/FinalApproach）
    // Direct和Free阶段不需要偏移，因为不会高频重定向
}
```

### 6.2 ApplyFlightRedirect（宿主统一方法）

```csharp
/// <summary>
/// 统一飞行参数重算。仅宿主内部调用，模块禁止直接访问。
/// 集中处理vanilla适配（origin偏移）和距离策略。
/// </summary>
private void ApplyFlightRedirect(Vector3 newDestination)
{
    Vector3 currentPos = DrawPos;
    Vector3 toDest = (newDestination - currentPos).Yto0();
    if (toDest.sqrMagnitude < 0.001f) return;

    float speedPerTick = def.projectile.SpeedTilesPerTick;
    float dist = toDest.magnitude;
    Vector3 dir = toDest.normalized;
    var config = flightRedirectConfig;  // 从Def扩展读取

    // vanilla适配：origin后退，恢复拦截因子有效性
    // 仅在高频重定向Phase生效（GuidedLeg/Tracking/FinalApproach）
    bool needsOriginOffset = Phase == FlightPhase.GuidedLeg
                          || Phase == FlightPhase.Tracking
                          || Phase == FlightPhase.FinalApproach;
    if (needsOriginOffset)
    {
        origin = currentPos - dir * config.originOffset;
        origin.y = currentPos.y;
    }
    else
    {
        origin = currentPos;
    }

    // 距离策略（配置化）
    if (Phase == FlightPhase.Tracking && dist > speedPerTick * config.farDistanceSpeedMult)
    {
        // 远距离：固定tick，避免插值抖动
        int ticks = config.farDistanceFixedTicks;
        destination = currentPos + dir * (ticks * speedPerTick);
        destination.y = currentPos.y;
        ticksToImpact = ticks;
    }
    else
    {
        // 近距离：精确计算
        destination = newDestination;
        ticksToImpact = Mathf.CeilToInt(dist / speedPerTick);
        if (ticksToImpact < 1) ticksToImpact = 1;
    }
}
```

## 7. Bullet_BDP宿主重构

### 7.1 字段变更

**新增：**
```csharp
public FlightPhase Phase { get; private set; } = FlightPhase.Direct;
private FlightRedirectConfig flightRedirectConfig;
private List<IBDPLifecyclePolicy> lifecyclePolicies;
private List<IBDPFlightIntentProvider> flightIntentProviders;
private List<IBDPArrivalPolicy> arrivalPolicies;
private List<IBDPHitResolver> hitResolvers;
private List<IBDPVisualObserver> visualObservers;
```

**移除：**
```csharp
// 被FlightPhase替代
- public bool IsOnFinalSegment;    // → Phase == FinalApproach
- public bool IsTracking;          // → Phase == Tracking
- public bool TrackingExpired;     // → Phase == Free

// 被新接口列表替代
- private List<IBDPPathResolver> pathResolvers;
- private List<IBDPTickObserver> tickObservers;
- private List<IBDPArrivalHandler> arrivalHandlers;
```

**保留不变：**
```csharp
public LocalTargetInfo FinalTarget;
public LocalTargetInfo TrackingTarget;
public float PassthroughPower;
public int PassthroughCount;
public int LaunchTick;
private bool postLaunchInitDone;
private List<IBDPImpactHandler> impactHandlers;
private List<IBDPPositionModifier> positionModifiers;
private List<IBDPSpeedModifier> speedModifiers;
```

### 7.2 TickInterval重构伪代码

```
阶段0: PostLaunchInit（保留，修复vanilla时序）
  if (!postLaunchInitDone) { 初始化FinalTarget/TrackingTarget/Phase; }

阶段1: LifecycleCheck
  var lcCtx = new LifecycleContext(Phase)
  foreach policy in lifecyclePolicies:
    policy.CheckLifecycle(this, ref lcCtx)
  if (lcCtx.RequestPhaseChange.HasValue)
    Phase = lcCtx.RequestPhaseChange.Value
  if (lcCtx.RequestDestroy)
    Log("[BDP] Destroy: " + lcCtx.DestroyReason)
    Destroy(); return

阶段2: FlightIntent
  var fiCtx = new FlightIntentContext(DrawPos, destination, Phase)
  foreach provider in flightIntentProviders:
    provider.ProvideIntent(this, ref fiCtx)
  if (fiCtx.Intent.HasValue)
    var intent = fiCtx.Intent.Value
    if (intent.TrackingActivated && Phase == FinalApproach)
      Phase = Tracking
    ApplyFlightRedirect(intent.TargetPosition)

阶段3: base.TickInterval(delta)
  if (!Spawned) return

阶段4: PositionModifier（不变）
阶段5: VisualObserve
  foreach observer in visualObservers:
    observer.Observe(this)
```

### 7.3 ImpactSomething重构伪代码

```
阶段6: ArrivalPolicy
  var arrCtx = new ArrivalContext(Phase)
  foreach policy in arrivalPolicies:
    policy.DecideArrival(this, ref arrCtx)
    if (arrCtx.Continue) break
  if (arrCtx.Continue)
    if (arrCtx.RequestPhaseChange.HasValue)
      Phase = arrCtx.RequestPhaseChange.Value
    ApplyFlightRedirect(arrCtx.NextDestination)
    return

阶段7: HitResolve
  var hitCtx = new HitContext(Phase, usedTarget.Thing)
  foreach resolver in hitResolvers:
    resolver.ResolveHit(this, ref hitCtx)
  if (hitCtx.ForceGround)
    Impact(null); return
  if (hitCtx.OverrideTarget != null)
    usedTarget = new LocalTargetInfo(hitCtx.OverrideTarget)

阶段8: base.ImpactSomething()
```

## 8. 模块重构

### 8.1 TrackingModule

**v4接口：** IBDPProjectileModule + IBDPPathResolver + IBDPTickObserver + IBDPArrivalHandler
**v5接口：** IBDPProjectileModule + IBDPFlightIntentProvider + IBDPLifecyclePolicy + IBDPHitResolver + IBDPArrivalPolicy

| 职责 | v4实现 | v5实现 |
|------|--------|--------|
| 追踪方向计算 | ResolvePath + 调用RedirectFlightTracking | ProvideIntent只写intent.TargetPosition |
| 极近距离处理 | ResolvePath中同步usedTarget | ProvideIntent写目标位置，宿主自动处理 |
| 超时自毁 | OnTick中host.Destroy() | CheckLifecycle中ctx.RequestDestroy |
| 丢锁自毁 | OnTick中host.Destroy() | CheckLifecycle中ctx.RequestDestroy |
| 丢锁报告 | 直接写host.IsTracking=false | CheckLifecycle中ctx.RequestPhaseChange |
| 命中保证 | ResolvePath中同步usedTarget | ResolveHit中ctx.OverrideTarget |
| 过期打地面 | 宿主检查TrackingExpired | ResolveHit中ctx.ForceGround |
| 到达继续飞 | HandleArrival中调用RedirectFlightTracking | DecideArrival中ctx.Continue + NextDestination |
| 飞行计时 | OnTick中flyingTicks++ | CheckLifecycle中flyingTicks++ |

### 8.2 GuidedModule

**v4接口：** IBDPProjectileModule + IBDPArrivalHandler
**v5接口：** IBDPProjectileModule + IBDPArrivalPolicy

| 职责 | v4实现 | v5实现 |
|------|--------|--------|
| 设置锚点 | 直接写host.IsOnFinalSegment等 | 请求宿主Phase→GuidedLeg |
| 到达锚点 | 调用host.RedirectFlightGuided | ctx.Continue + ctx.NextDestination |
| 进入最终段 | 直接写host.IsOnFinalSegment=true | ctx.RequestPhaseChange = FinalApproach |
| 同步TrackingTarget | 直接写host.TrackingTarget | 宿主在Phase转换时同步 |

### 8.3 ExplosionModule

**v4接口：** IBDPProjectileModule + IBDPImpactHandler
**v5接口：** IBDPProjectileModule + IBDPImpactHandler（**不变**）

### 8.4 TrailModule

**v4接口：** IBDPProjectileModule + IBDPTickObserver
**v5接口：** IBDPProjectileModule + IBDPVisualObserver

| 职责 | v4实现 | v5实现 |
|------|--------|--------|
| 拖尾渲染 | OnTick中记录位置 | Observe中记录位置 |

唯一变化：接口名和方法名，逻辑不变。

## 9. 不变的部分

以下组件本次不改动：
- **IBDPPositionModifier** — 无越权问题
- **IBDPImpactHandler** — 无越权问题
- **IBDPSpeedModifier** — 一次性执行，无越权问题
- **ObstacleRouter** — 静态工具，与管线无关
- **GuidedTargetingHelper** — UI层，与管线无关
- **GuidedVerbState** — Verb层，留作后续独立任务（P11）
- **TargetSearcher** — 静态工具，不变
- **BDPModuleFactory** — 工厂模式不变，只需注册新接口类型

## 10. 权限硬约束（红线）

1. **仅宿主可写核心飞行字段**：origin、destination、ticksToImpact只在ApplyFlightRedirect中修改
2. **仅宿主可调用Destroy/Impact**：模块通过ctx.RequestDestroy/ForceGround请求
3. **仅宿主可写Phase**：模块通过ctx.RequestPhaseChange请求
4. **模块间零直接通信**：不通过宿主共享字段传递状态，Phase是唯一协作媒介且只读
5. **IBDPVisualObserver零副作用**：编码审查时作为检查项

## 11. 执行步骤（6步，一次性替换）

### 步骤1：新增类型定义
- FlightPhase枚举
- FlightRedirectConfig类
- 所有新struct（LifecycleContext, FlightIntentContext, FlightIntent, ArrivalContext, HitContext）
- 所有新接口（IBDPLifecyclePolicy, IBDPFlightIntentProvider, IBDPVisualObserver, IBDPArrivalPolicy, IBDPHitResolver）

### 步骤2：Bullet_BDP宿主重构
- 新增Phase字段和FlightRedirectConfig
- 新增ApplyFlightRedirect方法
- 重写TickInterval（8阶段调度）
- 重写ImpactSomething（3阶段调度）
- 移除IsOnFinalSegment/IsTracking/TrackingExpired
- 移除RedirectFlightGuided/RedirectFlightTracking
- 更新SpawnSetup中的接口缓存构建

### 步骤3：TrackingModule重构
- 实现IBDPFlightIntentProvider + IBDPLifecyclePolicy + IBDPHitResolver + IBDPArrivalPolicy
- 移除所有host.Destroy()调用
- 移除所有host.usedTarget写入
- 移除所有host.RedirectFlightTracking调用
- 移除所有host.IsTracking/TrackingExpired写入

### 步骤4：GuidedModule重构
- 实现IBDPArrivalPolicy
- 移除host.IsOnFinalSegment写入
- 移除host.RedirectFlightGuided调用
- 移除host.TrackingTarget直接写入

### 步骤5：TrailModule适配
- IBDPTickObserver → IBDPVisualObserver
- OnTick → Observe（方法名变更，逻辑不变）

### 步骤6：清理与XML修正
- 删除旧接口文件（IBDPPathResolver, IBDPTickObserver, IBDPArrivalHandler）
- 删除旧struct（PathContext旧版）
- 修正Argus trackingDelay XML注释（P9）
- 距离策略阈值写入XML Def扩展

## 12. 验收标准

1. TrackingModule/GuidedModule源码中不出现`Destroy()`、`Impact()`、`usedTarget`直接写入
2. TrackingModule/GuidedModule源码中不出现`origin`、`destination`、`ticksToImpact`直接写入
3. IBDPVisualObserver实现全部为只读行为（视觉/日志类）
4. 只有ApplyFlightRedirect一个位置写核心飞行字段
5. Phase是模块间唯一协作媒介，且模块只读
6. Argus（引导+追踪）在障碍、移动目标、护盾场景下行为稳定
7. 距离策略阈值可通过XML配置调整

## 13. 风险与缓解

| 风险 | 严重度 | 缓解措施 |
|------|--------|---------|
| 一次性替换导致行为偏移 | 高 | 重构前固化关键弹种（Hound/Viper/Argus/Hornet）的行为基线日志；重构后逐弹种对照验证 |
| 命中解析改动影响掩体/护盾语义 | 中 | HitResolver仅对Bullet_BDP局部解析，不改全局vanilla |
| 新接口组导致编译错误遗漏 | 低 | 旧接口文件删除后编译器会报所有未适配点 |
| 距离策略配置化引入新的调参负担 | 低 | 默认值与当前硬编码值一致，行为不变；配置化只是开放调整能力 |

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-28 | 初版：综合两份重构方案 + 用户决策，形成最终架构修正方案 | Claude Opus 4.6 |
