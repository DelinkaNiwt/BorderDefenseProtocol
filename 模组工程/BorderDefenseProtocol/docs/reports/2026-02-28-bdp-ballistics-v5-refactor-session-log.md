# BDP弹道管线 v4→v5 重构任务日志

> 日期：2026-02-28
> 范围：BorderDefenseProtocol 弹道管线架构重构（v4→v5）
> 状态：全部完成，编译通过（0警告0错误）

---

## 一、任务目标

v4核心问题：TrackingModule和GuidedModule越权——直接写origin/destination/ticksToImpact、
调用Destroy()、改写usedTarget、通过宿主共享布尔字段（IsOnFinalSegment/IsTracking）隐式耦合。

v5目标：**模块只产出意图，宿主统一执行。FlightPhase状态机作为模块间唯一协作媒介。**

### 红线约束
1. `origin`/`destination`/`ticksToImpact` 只在 `ApplyFlightRedirect` 中写入
2. `Destroy()`/`Impact()` 只在宿主TickInterval/ImpactSomething中调用
3. `Phase` setter只在宿主内部
4. 模块间零直接通信，Phase是唯一协作媒介且模块只读
5. IBDPVisualObserver实现零副作用

---

## 二、执行步骤与产出

### 步骤1：新增类型定义 ✅

| 新增文件 | 内容 |
|----------|------|
| `Pipeline/FlightPhase.cs` | 枚举：Direct, GuidedLeg, FinalApproach, Tracking, TrackingLost, Free |
| `Data/FlightRedirectConfig.cs` | DefModExtension：originOffset, farDistanceSpeedMult, farDistanceFixedTicks |
| `Pipeline/IBDPLifecyclePolicy.cs` | 接口 + LifecycleContext（RequestDestroy, DestroyReason, RequestPhaseChange） |
| `Pipeline/IBDPFlightIntentProvider.cs` | 接口 + FlightIntentContext + FlightIntent结构体 |
| `Pipeline/IBDPVisualObserver.cs` | 接口：Observe(host)，零副作用 |
| `Pipeline/IBDPArrivalPolicy.cs` | 接口 + ArrivalContextV5（Continue, NextDestination, RequestPhaseChange） |
| `Pipeline/IBDPHitResolver.cs` | 接口 + HitContext（ForceGround, OverrideTarget） |

### 步骤2：Bullet_BDP宿主重构 ✅

文件：`Projectiles/Bullet_BDP.cs`（完整重写）

移除：
- IsOnFinalSegment, IsTracking, TrackingExpired 布尔字段
- RedirectFlightGuided(), RedirectFlightTracking() 方法
- pathResolvers, tickObservers, arrivalHandlers 旧管线缓存

新增：
- `Phase` 属性（internal setter）
- `FlightRedirectConfig redirectConfig`（从def.modExtensions读取）
- `InitGuidedFlight(LocalTargetInfo)` — 设Phase=GuidedLeg
- `ApplyFlightRedirect(Vector3)` — 唯一飞行参数写入点（origin后退 + 远/近距离策略）

管线调度：
- TickInterval 6阶段：PostLaunchInit → LifecycleCheck → FlightIntent → base.TickInterval → PositionModifier → VisualObserve
- ImpactSomething 3阶段：ArrivalPolicy → HitResolve → Impact

### 步骤3：TrackingModule重构 ✅

文件：`Projectiles/TrackingModule.cs`（完整重写，~475行）

接口迁移：
- v4: IBDPPathResolver + IBDPTickObserver + IBDPArrivalHandler
- v5: IBDPFlightIntentProvider + IBDPLifecyclePolicy + IBDPHitResolver + IBDPArrivalPolicy

关键迁移点：
- CheckLifecycle：flyingTicks计时、超时自毁、丢锁倒计时/重锁 → 通过ctx.RequestDestroy/RequestPhaseChange
- ProvideIntent：追踪转向逻辑 → 写ctx.Intent替代直写origin/destination
- ResolveHit：追踪过期→ctx.ForceGround，追踪中→ctx.OverrideTarget
- DecideArrival：追踪中到达→ctx.Continue+NextDestination
- 移除所有越权操作（host.Destroy/usedTarget/RedirectFlightTracking直写）

### 步骤4：GuidedModule重构 ✅

文件：`Projectiles/GuidedModule.cs`（~140行）

接口迁移：v4 IBDPArrivalHandler → v5 IBDPArrivalPolicy

关键迁移点：
- DecideArrival：到达锚点→ctx.Continue+NextDestination+RequestPhaseChange=GuidedLeg
- 进入最终段→RequestPhaseChange=FinalApproach，用FinalTarget实时位置替代预计算路径点
- SetWaypoints：调用host.InitGuidedFlight(finalTarget)替代直写host.IsOnFinalSegment

### 步骤5：TrailModule适配 ✅

文件：`Projectiles/TrailModule.cs`（~80行）

变更：IBDPTickObserver → IBDPVisualObserver，OnTick → Observe，逻辑不变。

### 步骤6：清理与修正 ✅

| 操作 | 详情 |
|------|------|
| 旧接口清理 | IBDPPathResolver.cs / IBDPTickObserver.cs / IBDPArrivalHandler.cs → tombstone注释 |
| BDPTrackingConfig | 移除 forceUsedTargetOnFinalApproach, forceUsedTargetOnArrival |
| IBDPProjectileModule | 注释从v4更新到v5（管线接口列表） |
| XML | Argus弹种trackingDelay注释修正 |

### 追加：日志性能优化 ✅

| 修改 | 修改前 | 修改后 |
|------|--------|--------|
| TrackingDiag.Enabled | `true`（默认开启） | `false`（默认关闭） |
| TrackingDiag.Interval | `10`（每10tick） | `60`（每秒1条） |
| ApplyFlightRedirect日志 | 每次重定向都输出 | 加 `TTI % Interval` 间隔守卫 |
| FlightIntent日志 | 每tick有意图就输出 | 加 `TTI % Interval` 间隔守卫 |
| 事件型日志 | Phase转换/销毁/命中 | 保持不变（低频，仅状态变化时触发） |

---

## 三、架构对比

### v4 问题模型
```
模块 ──直接写──► 宿主字段（origin/dest/IsTracking/usedTarget/Destroy）
模块 ◄──隐式读── 宿主共享布尔（IsOnFinalSegment/IsTracking/TrackingExpired）
```
模块越权 + 隐式耦合，新增模块必须了解其他模块写了哪些字段。

### v5 意图模型
```
模块 ──写ctx──► Context结构体 ──宿主读取──► 统一执行
模块 ◄──只读── FlightPhase（唯一协作媒介）
```
模块零越权，Phase状态机取代散落布尔，宿主在固定调度点统一执行所有状态变更。

### 接口映射
```
v4 IBDPPathResolver    → v5 IBDPFlightIntentProvider
v4 IBDPTickObserver    → v5 IBDPLifecyclePolicy + IBDPVisualObserver
v4 IBDPArrivalHandler  → v5 IBDPArrivalPolicy
(无)                   → v5 IBDPHitResolver（新增）
```

---

## 四、编译验证

```
dotnet build BDP.csproj -c Release
已成功生成。 0个警告 0个错误
```

---

## 五、未涉及 / 后续事项

- BDPModuleFactory.cs — 未修改（工厂逻辑不变，模块构造签名未变）
- ExplosionModule.cs — 未修改（IBDPImpactHandler接口保留不变）
- 存档兼容性 — v5不兼容v4存档（Phase序列化key变更，可接受）
- FlightRedirectConfig — XML中尚未为任何弹种显式配置，全部使用默认值
- 运行时测试 — 需在游戏内验证各弹种（TestPulse/Hound/Hornet/Argus）行为正确

</content>
</invoke>