---
标题：BDP弹道管线v5重构设计方案
版本号: v1.0
更新日期: 2026-02-28
最后修改者: Claude Opus 4.6
标签：[文档][用户已确认][已完成][未锁定]
摘要: 基于双AI交叉验证报告识别的11个架构问题，对弹道管线进行v5重构。核心思路：模块只产出"意图"，宿主统一执行。新增飞行阶段状态机、LifecycleManager接口、HitResolver接口，将所有vanilla适配逻辑收归宿主内部。
---

# BDP弹道管线v5重构设计方案

## 1. 设计决策记录

| 决策点 | 选项 | 选择 | 理由 |
|--------|------|------|------|
| vanilla冲突处理 | 宿主集中适配层 / Harmony Patch / 完全覆写 | 宿主集中适配层 | 模块彻底纯净，所有hack一目了然，不违反零Harmony原则 |
| PathResolver契约 | 纯净Destination / 导航意图包 / FlightCommand | 纯净Destination | 契约最纯净，模块不知道origin存在 |
| 模块协作方式 | 宿主状态机 / 事件总线 / 保持现状 | 宿主状态机 | 状态转换明确，模块只读不写 |
| 生命周期管理 | 宿主统一管理 / 新增LifecycleManager接口 | 新增LifecycleManager接口 | 职责分离明确，观察者契约不再被违反 |
| 命中判定 | 宿主统一裁决 / 新增HitResolver接口 | 新增HitResolver接口 | 可扩展，命中修正有专门管线阶段 |
| RedirectFlight方法 | 单一方法+阶段参数 / 多方法改private | 单一方法+阶段参数 | 模块完全不知道RedirectFlight的存在 |

## 2. 新管线架构（v5）

### 2.1 每tick执行顺序

```
阶段0: PostLaunchInit          — 宿主内部，修复vanilla时序问题
阶段1: LifecycleCheck          — IBDPLifecycleManager
                                  模块报告状态（超时/丢锁），宿主决定是否销毁
阶段2: PathResolve              — IBDPPathResolver
                                  模块写ctx.Destination
                                  宿主拿到Destination后执行ApplyFlightRedirect()
阶段3: base.TickInterval        — vanilla引擎位置插值 + 拦截检查 + 到达判定
阶段4: PositionModifier         — IBDPPositionModifier（不变）
阶段5: TickObserver             — IBDPTickObserver（纯只读）
```

### 2.2 到达/命中执行顺序

```
阶段6: ArrivalHandler           — IBDPArrivalHandler
                                  ctx.Continue=true时宿主执行重定向+Phase转换
阶段7: HitResolve               — IBDPHitResolver
                                  模块可设ForceGround或OverrideTarget
                                  宿主根据结果决定Impact(who)
阶段8: ImpactHandler            — IBDPImpactHandler（不变）
```

### 2.3 与v4的差异

| 维度 | v4 | v5 |
|------|----|----|
| 管线阶段数 | 6 | 8（+LifecycleCheck, +HitResolve） |
| 飞行参数重算 | 模块直接调用host.RedirectFlight* | 宿主内部ApplyFlightRedirect |
| 模块间协作 | 共享宿主字段（IsOnFinalSegment等） | 宿主FlightPhase状态机，模块只读 |
| 生命周期 | TickObserver中Destroy | LifecycleManager中RequestDestroy |
| 命中修正 | 模块直接写usedTarget | HitResolver中报告，宿主执行 |
| vanilla适配 | 分散在模块和宿主中 | 集中在宿主ApplyFlightRedirect |

## 3. 飞行阶段状态机

### 3.1 状态定义

```csharp
public enum FlightPhase
{
    Direct,         // 普通直飞
    GuidedLeg,      // 引导段（飞向中间锚点）
    FinalApproach,  // 最终段（飞向最终目标）
    Tracking,       // 追踪中
    TrackingLost,   // 追踪丢失（等待重锁或超时）
    Free            // 自由飞行（追踪彻底过期）
}
```

### 3.2 状态转换图

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

### 3.3 转换触发机制

模块不直接修改Phase，而是通过管线上下文报告状态变化，宿主读取后统一转换：

| 转换 | 触发者 | 报告方式 | 宿主执行 |
|------|--------|---------|---------|
| Direct→GuidedLeg | GuidedModule.SetWaypoints | 宿主方法调用 | Phase = GuidedLeg |
| GuidedLeg→FinalApproach | GuidedModule.HandleArrival | ctx.RequestPhaseChange | Phase = FinalApproach |
| FinalApproach→Tracking | TrackingModule.ResolvePath | ctx.TrackingActivated | Phase = Tracking |
| Tracking→TrackingLost | TrackingModule.CheckLifecycle | 报告丢锁状态 | Phase = TrackingLost |
| TrackingLost→Tracking | TrackingModule.CheckLifecycle | 报告重锁成功 | Phase = Tracking |
| TrackingLost→Free | TrackingModule.CheckLifecycle | 报告超时 | Phase = Free |

## 4. 新增管线接口

### 4.1 IBDPLifecycleManager

```csharp
/// <summary>
/// 生命周期管理器——模块报告自身状态，宿主决定是否终止子弹。
/// 执行时机：阶段1（PathResolve之前）。
/// </summary>
public interface IBDPLifecycleManager
{
    void CheckLifecycle(Bullet_BDP host, ref LifecycleContext ctx);
}

public struct LifecycleContext
{
    /// <summary>模块请求销毁子弹。</summary>
    public bool RequestDestroy;
    /// <summary>销毁原因（供日志）。</summary>
    public string DestroyReason;
    /// <summary>请求Phase转换（如Tracking→TrackingLost）。</summary>
    public FlightPhase? RequestPhaseChange;
}
```

### 4.2 IBDPHitResolver

```csharp
/// <summary>
/// 命中修正器——在vanilla ImpactSomething之前修正命中目标。
/// 执行时机：阶段7。
/// </summary>
public interface IBDPHitResolver
{
    void ResolveHit(Bullet_BDP host, ref HitContext ctx);
}

public struct HitContext
{
    /// <summary>强制打地面（忽略usedTarget）。</summary>
    public bool ForceGround;
    /// <summary>覆盖命中目标。</summary>
    public Thing OverrideTarget;
}
```

### 4.3 PathContext扩展

```csharp
public struct PathContext
{
    public Vector3 Origin;          // 只读
    public Vector3 Destination;     // 可写
    /// <summary>追踪已激活（供宿主状态机转换）。</summary>
    public bool TrackingActivated;
}
```

### 4.4 ArrivalContext扩展

```csharp
public struct ArrivalContext
{
    public bool Continue;
    /// <summary>下一个目标点（Continue=true时有效）。</summary>
    public Vector3 NextDestination;
    /// <summary>请求阶段转换。</summary>
    public FlightPhase? RequestPhaseChange;
}
```

## 5. Bullet_BDP宿主重构

### 5.1 新增字段

```csharp
public FlightPhase Phase { get; private set; } = FlightPhase.Direct;
private List<IBDPLifecycleManager> lifecycleManagers;
private List<IBDPHitResolver> hitResolvers;
```

### 5.2 移除字段

- `IsOnFinalSegment` → 被 `Phase` 替代
- `IsTracking` → 被 `Phase == Tracking` 替代
- `TrackingExpired` → 被 `Phase == Free` 替代

### 5.3 ApplyFlightRedirect（统一飞行参数重算）

```csharp
private void ApplyFlightRedirect(Vector3 newDestination)
{
    Vector3 currentPos = DrawPos;
    Vector3 toDest = (newDestination - currentPos).Yto0();
    if (toDest.sqrMagnitude < 0.001f) return;

    float speedPerTick = def.projectile.SpeedTilesPerTick;
    float dist = toDest.magnitude;
    Vector3 dir = toDest.normalized;

    // vanilla适配：origin后退6格，恢复拦截因子有效性
    bool needsOriginOffset = (Phase == FlightPhase.GuidedLeg
                           || Phase == FlightPhase.Tracking
                           || Phase == FlightPhase.FinalApproach);
    if (needsOriginOffset)
    {
        const float ORIGIN_OFFSET = 6f;
        origin = currentPos - dir * ORIGIN_OFFSET;
        origin.y = currentPos.y;
    }
    else
    {
        origin = currentPos;
    }

    // 距离策略
    if (Phase == FlightPhase.Tracking && dist > speedPerTick * 3f)
    {
        int ticks = 60;
        destination = currentPos + dir * (ticks * speedPerTick);
        destination.y = currentPos.y;
        ticksToImpact = ticks;
    }
    else
    {
        destination = newDestination;
        ticksToImpact = Mathf.CeilToInt(dist / speedPerTick);
        if (ticksToImpact < 1) ticksToImpact = 1;
    }
}
```

### 5.4 TickInterval重构伪代码

```
阶段0: PostLaunchInit（保留，修复vanilla时序）

阶段1: LifecycleCheck
  foreach lifecycleManager:
    CheckLifecycle(host, ref lifecycleCtx)
    if (lifecycleCtx.RequestPhaseChange.HasValue)
      Phase = lifecycleCtx.RequestPhaseChange.Value
  if (lifecycleCtx.RequestDestroy)
    Destroy(); return;

阶段2: PathResolve
  var pathCtx = new PathContext(DrawPos, destination)
  foreach pathResolver:
    ResolvePath(host, ref pathCtx)
  if (pathCtx.TrackingActivated && Phase == FinalApproach)
    Phase = Tracking
  if (destination changed)
    ApplyFlightRedirect(pathCtx.Destination)

阶段3: base.TickInterval(delta)
  if (!Spawned) return

阶段4: PositionModifier（不变）
阶段5: TickObserver（不变，纯只读）
```

### 5.5 ImpactSomething重构伪代码

```
阶段6: ArrivalHandler
  foreach arrivalHandler:
    HandleArrival(host, ref arrCtx)
    if (arrCtx.Continue) break
  if (arrCtx.Continue)
    if (arrCtx.RequestPhaseChange.HasValue)
      Phase = arrCtx.RequestPhaseChange.Value
    ApplyFlightRedirect(arrCtx.NextDestination)
    return

阶段7: HitResolve
  var hitCtx = new HitContext()
  foreach hitResolver:
    ResolveHit(host, ref hitCtx)
  if (hitCtx.ForceGround)
    Impact(null); return
  if (hitCtx.OverrideTarget != null)
    usedTarget = new LocalTargetInfo(hitCtx.OverrideTarget)

阶段8: base.ImpactSomething()
```

## 6. 模块重构

### 6.1 TrackingModule

重构前接口：`IBDPProjectileModule + IBDPPathResolver + IBDPTickObserver + IBDPArrivalHandler`
重构后接口：`IBDPProjectileModule + IBDPPathResolver + IBDPLifecycleManager + IBDPHitResolver + IBDPArrivalHandler`

职责变化：

| 职责 | 重构前 | 重构后 |
|------|--------|--------|
| 追踪方向计算 | ResolvePath + 调用RedirectFlightTracking | ResolvePath只写ctx.Destination |
| 极近距离处理 | ResolvePath中同步usedTarget | ResolvePath写目标位置，宿主自动处理 |
| 超时自毁 | OnTick中Destroy | CheckLifecycle中RequestDestroy |
| 丢锁自毁 | OnTick中Destroy | CheckLifecycle中RequestDestroy |
| 丢锁报告 | 直接写host.IsTracking=false | CheckLifecycle中RequestPhaseChange |
| 命中保证 | ResolvePath中同步usedTarget | ResolveHit中OverrideTarget |
| 过期打地面 | 宿主检查TrackingExpired | ResolveHit中ForceGround |
| 到达继续飞 | HandleArrival中调用RedirectFlightTracking | HandleArrival中ctx.Continue + NextDestination |
| 飞行计时 | OnTick中flyingTicks++ | CheckLifecycle中flyingTicks++ |

### 6.2 GuidedModule

接口不变：`IBDPProjectileModule + IBDPArrivalHandler`

职责变化：

| 职责 | 重构前 | 重构后 |
|------|--------|--------|
| 设置锚点 | 直接写host.IsOnFinalSegment等 | 请求宿主Phase→GuidedLeg |
| 到达锚点 | 调用host.RedirectFlightGuided | ctx.Continue + ctx.NextDestination |
| 进入最终段 | 直接写host.IsOnFinalSegment=true | ctx.RequestPhaseChange = FinalApproach |
| 同步TrackingTarget | 直接写host.TrackingTarget | 宿主在Phase转换时同步 |

## 7. 问题解决映射

| 编号 | 问题 | 解决方式 | 状态 |
|------|------|---------|------|
| P1 | 命中判定越权 | IBDPHitResolver接口 | 本次解决 |
| P2 | 生命周期越权 | IBDPLifecycleManager接口 | 本次解决 |
| P3 | origin后退hack | 收归ApplyFlightRedirect | 本次解决 |
| P4 | 隐式耦合 | FlightPhase状态机 | 本次解决 |
| P5 | 初始化时序 | PostLaunchInit保留+Phase初始化集中 | 本次解决 |
| P7 | PathResolver越权 | 模块只写Destination | 本次解决 |
| P8 | TickObserver越权 | 改用LifecycleManager | 本次解决 |
| P9 | XML配置不一致 | 修复Argus trackingDelay | 本次解决 |
| P10 | 距离策略硬编码 | 收归ApplyFlightRedirect | 本次解决 |
| P11 | GuidedVerbState过重 | 后续独立任务 | 延后 |

## 8. 不变的部分

以下组件本次不改动：
- ExplosionModule — 纯IBDPImpactHandler，无越权问题
- TrailModule — 纯IBDPTickObserver，无越权问题
- BDPModuleFactory — 工厂模式不变，只需注册新接口类型
- ObstacleRouter — 静态工具，与管线无关
- GuidedTargetingHelper — UI层，与管线无关
- GuidedVerbState — Verb层，留作后续任务
- TargetSearcher — 静态工具，不变

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-28 | 初版：弹道管线v5重构设计方案 | Claude Opus 4.6 |
