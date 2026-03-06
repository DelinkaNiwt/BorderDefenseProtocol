---
标题：VanillaAdapter重构实施指南
版本号: v1.0
更新日期: 2026-03-03
最后修改者: Claude Sonnet 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: VanillaAdapter适配层的详细实施指南，包括集成步骤、代码修改清单、测试验证方案
---

# VanillaAdapter重构实施指南

## 一、概述

### 1.1 目标

将vanilla兼容逻辑从模块中剥离，集中到VanillaAdapter适配层，实现：
- **职责清晰**：模块只负责弹道逻辑，适配层负责vanilla兼容
- **易于维护**：所有hack集中在一个地方，有详细注释
- **易于扩展**：新增弹道类型不需要关心vanilla冲突

### 1.2 影响范围

**需要修改的文件**：
1. `Bullet_BDP.cs` — 集成适配层
2. `TrackingModule.cs` — 删除ResolveHit方法
3. `BDP.csproj` — 添加VanillaAdapter.cs到编译列表

**新增文件**：
1. `VanillaAdapter.cs` — 适配层实现（已创建）

**不需要修改的文件**：
- `GuidedModule.cs` — 不涉及vanilla冲突
- `Verb_BDPDualRanged.cs` — 只调用宿主API
- 其他模块和配置文件

### 1.3 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 破坏现有功能 | 中 | 保留原有逻辑，只是重新组织 |
| 引入新bug | 低 | 适配层逻辑简单，易于测试 |
| 性能下降 | 极低 | 零额外开销，只是调用重组 |
| 兼容性问题 | 低 | 不改变序列化格式 |

---

## 二、实施步骤

### 步骤1：添加VanillaAdapter到项目

**操作**：
1. 确认`VanillaAdapter.cs`已创建在`Source/BDP/Trigger/Projectiles/`目录
2. 打开`BDP.csproj`，确认文件已自动包含（VS会自动检测新文件）
3. 如果未自动包含，手动添加：
   ```xml
   <Compile Include="Trigger\Projectiles\VanillaAdapter.cs" />
   ```

**验证**：
- 编译项目，确认无编译错误
- 检查`obj/Debug/BDP.csproj.CoreCompileInputs.cache`，确认包含VanillaAdapter

---

### 步骤2：修改Bullet_BDP.cs

#### 2.1 添加适配层实例

**位置**：`Bullet_BDP.cs` 第83行附近（在`firstRedirectConsumed`字段后）

**添加代码**：
```csharp
// ── Vanilla适配层 ──
/// <summary>Vanilla兼容适配层——集中处理vanilla机制冲突。</summary>
private VanillaAdapter vanillaAdapter = new VanillaAdapter();
```

#### 2.2 修改SpawnSetup方法

**位置**：`Bullet_BDP.cs` 第262行 `SpawnSetup`方法

**在方法末尾添加**（在`modules[i].OnSpawn(this)`循环后）：
```csharp
// 配置适配层策略
bool hasTracking = GetModule<TrackingModule>() != null;
bool hasGuided = GetModule<GuidedModule>() != null;

vanillaAdapter.ConfigureStrategy(
    needsOriginOffset: hasTracking || hasGuided,
    needsUsedTargetSync: hasTracking
);

// 记录真实发射点（在Launch后）
if (!respawningAfterLoad)
    vanillaAdapter.RecordTrueOrigin(origin);
```

**完整修改后的SpawnSetup**：
```csharp
public override void SpawnSetup(Map map, bool respawningAfterLoad)
{
    base.SpawnSetup(map, respawningAfterLoad);

    // 首次生成时通过工厂创建模块（读档时模块由ExposeData恢复）
    if (!respawningAfterLoad)
    {
        modules = BDPModuleFactory.CreateModules(def);
        modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    // 初始化最终目标（引导弹会在ApplyWaypoints时覆盖）
    FinalTarget = intendedTarget;

    // 初始化发射时间戳
    if (!respawningAfterLoad)
        LaunchTick = Find.TickManager.TicksGame;

    // 初始化穿体穿透力
    if (!respawningAfterLoad)
    {
        var chipConfig = equipmentDef?.GetModExtension<WeaponChipConfig>();
        PassthroughPower = chipConfig?.passthroughPower ?? 0f;
    }

    // 读取飞行重定向配置（无配置时用默认值）
    redirectConfig = def.GetModExtension<FlightRedirectConfig>()
        ?? new FlightRedirectConfig();

    // 建立管线参与者缓存
    BuildPipelineCache();

    // 初始化显示位置（必须在OnSpawn之前，避免模块读到默认值）
    modifiedDrawPos = base.DrawPos;

    // 通知所有模块
    for (int i = 0; i < modules.Count; i++)
        modules[i].OnSpawn(this);

    // ★ 配置适配层策略
    bool hasTracking = GetModule<TrackingModule>() != null;
    bool hasGuided = GetModule<GuidedModule>() != null;

    vanillaAdapter.ConfigureStrategy(
        needsOriginOffset: hasTracking || hasGuided,
        needsUsedTargetSync: hasTracking
    );

    // ★ 记录真实发射点（在Launch后）
    if (!respawningAfterLoad)
        vanillaAdapter.RecordTrueOrigin(origin);
}
```

#### 2.3 修改ApplyFlightRedirect方法

**位置**：`Bullet_BDP.cs` 第188行 `ApplyFlightRedirect`方法

**修改origin计算部分**（第212-225行）：

**原代码**：
```csharp
// Phase为GuidedLeg/Tracking/FinalApproach时origin后退，恢复vanilla沿途拦截
bool needOriginOffset = Phase == FlightPhase.GuidedLeg
    || Phase == FlightPhase.Tracking
    || Phase == FlightPhase.FinalApproach;

if (needOriginOffset && !isFirstRedirect)
{
    origin = currentPos - dir * cfg.originOffset;
    origin.y = currentPos.y;
}
else
{
    origin = currentPos;
}
```

**新代码**：
```csharp
// ★ 使用适配层计算origin
origin = vanillaAdapter.ComputeAdaptedOrigin(
    currentPos, dir, Phase, isFirstRedirect);
origin.y = currentPos.y;
```

**说明**：
- 删除了`needOriginOffset`判断逻辑
- 删除了`cfg.originOffset`的使用（适配层内部使用固定的6格）
- 如果需要自定义偏移距离，可以在SpawnSetup中调用`vanillaAdapter.SetOriginOffsetDistance(cfg.originOffset)`

#### 2.4 修改ImpactSomething方法

**位置**：`Bullet_BDP.cs` 第448行 `ImpactSomething`方法

**在阶段6（ArrivalPolicy）后，阶段7（HitResolve）前添加**：

**原代码**（第450-482行）：
```csharp
// 阶段6：ArrivalPolicy——决定继续飞还是命中
if (arrivalPolicies.Count > 0)
{
    // ... ArrivalPolicy逻辑 ...
}

// 阶段7：HitResolve——修正命中判定
if (hitResolvers.Count > 0)
{
    var hitCtx = new HitContext(Phase, usedTarget);
    for (int i = 0; i < hitResolvers.Count; i++)
        hitResolvers[i].ResolveHit(this, ref hitCtx);

    if (hitCtx.ForceGround)
    {
        Impact(null);
        return;
    }

    if (hitCtx.OverrideTarget.IsValid)
        usedTarget = hitCtx.OverrideTarget;
}

// 阶段8：vanilla命中判定 → Impact
base.ImpactSomething();
```

**新代码**：
```csharp
// 阶段6：ArrivalPolicy——决定继续飞还是命中
if (arrivalPolicies.Count > 0)
{
    // ... ArrivalPolicy逻辑不变 ...
}

// ★ 阶段6.5：适配层命中前检查
var impactCheck = vanillaAdapter.CheckBeforeImpact(
    Phase, TrackingTarget, ref usedTarget);

if (impactCheck.ForceGround)
{
    if (TrackingDiag.Enabled)
        Log.Message($"[VanillaAdapter] ForceGround: {impactCheck.Reason}");
    Impact(null);
    return;
}

// 阶段7：HitResolve——修正命中判定（简化版）
// 注意：usedTarget同步已由适配层处理，HitResolve不再需要OverrideTarget
if (hitResolvers.Count > 0)
{
    var hitCtx = new HitContext(Phase, usedTarget);
    for (int i = 0; i < hitResolvers.Count; i++)
        hitResolvers[i].ResolveHit(this, ref hitCtx);

    // ForceGround已由适配层处理，这里只保留兼容性
    if (hitCtx.ForceGround)
    {
        Impact(null);
        return;
    }

    // OverrideTarget已由适配层处理，这里只保留兼容性
    if (hitCtx.OverrideTarget.IsValid)
        usedTarget = hitCtx.OverrideTarget;
}

// 阶段8：vanilla命中判定 → Impact
base.ImpactSomething();
```

**说明**：
- 适配层在阶段6.5执行，早于HitResolve
- HitResolve保留兼容性，但主要逻辑已移到适配层
- 未来可以考虑完全移除HitResolve的ForceGround/OverrideTarget逻辑

#### 2.5 修改ExposeData方法

**位置**：`Bullet_BDP.cs` 第582行 `ExposeData`方法

**在方法末尾添加**（在`modules`序列化后）：
```csharp
// 序列化适配层
vanillaAdapter.ExposeData();
```

**完整修改后的ExposeData**：
```csharp
public override void ExposeData()
{
    base.ExposeData();

    // v5 Phase状态机
    var phase = Phase;
    Scribe_Values.Look(ref phase, "bdpPhase", FlightPhase.Direct);
    Phase = phase;

    Scribe_TargetInfo.Look(ref FinalTarget, "bdpFinalTarget");
    Scribe_TargetInfo.Look(ref TrackingTarget, "bdpTrackingTarget");
    Scribe_Values.Look(ref PassthroughPower, "bdpPassthroughPower", 0f);
    Scribe_Values.Look(ref PassthroughCount, "bdpPassthroughCount", 0);
    Scribe_Values.Look(ref LaunchTick, "bdpLaunchTick", 0);
    Scribe_Values.Look(ref launchSpeedMult, "bdpLaunchSpeedMult", 1f);
    Scribe_Values.Look(ref postLaunchInitDone, "bdpPostLaunchInit", false);
    Scribe_Values.Look(ref arrivalRedirectCount, "bdpArrivalRedirects", 0);
    Scribe_Values.Look(ref firstRedirectConsumed, "bdpFirstRedirectConsumed", false);
    Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
    if (modules == null)
        modules = new List<IBDPProjectileModule>();

    // ★ 序列化适配层
    vanillaAdapter.ExposeData();
}
```

---

### 步骤3：修改TrackingModule.cs

#### 3.1 删除IBDPHitResolver接口实现

**位置**：`TrackingModule.cs` 第34行 类声明

**原代码**：
```csharp
public class TrackingModule : IBDPProjectileModule,
    IBDPFlightIntentProvider, IBDPLifecyclePolicy,
    IBDPHitResolver, IBDPArrivalPolicy
```

**新代码**：
```csharp
public class TrackingModule : IBDPProjectileModule,
    IBDPFlightIntentProvider, IBDPLifecyclePolicy,
    IBDPArrivalPolicy
```

#### 3.2 删除ResolveHit方法

**位置**：`TrackingModule.cs` 第431-459行

**删除整个方法**：
```csharp
// ══════════════════════════════════════════
//  IBDPHitResolver — 命中修正
// ══════════════════════════════════════════

public void ResolveHit(Bullet_BDP host, ref HitContext ctx)
{
    var cfg = GetConfig(host);
    if (cfg == null) return;

    // 追踪过期（TrackingLost/Free）打地面——防止vanilla无视距离命中usedTarget
    if (ctx.CurrentPhase == FlightPhase.TrackingLost
        || ctx.CurrentPhase == FlightPhase.Free)
    {
        ctx.ForceGround = true;
        if (TrackingDiag.Enabled)
            Log.Message($"[BDP-Track] HitResolve ForceGround Phase={ctx.CurrentPhase}");
        return;
    }

    // 极近距离命中保证——追踪中且目标有效时覆盖usedTarget
    if ((ctx.CurrentPhase == FlightPhase.Tracking
        || ctx.CurrentPhase == FlightPhase.FinalApproach)
        && IsTargetValid(host.TrackingTarget))
    {
        ctx.OverrideTarget = host.TrackingTarget;
        if (TrackingDiag.Enabled)
            Log.Message($"[BDP-Track] HitResolve Override={host.TrackingTarget} Phase={ctx.CurrentPhase}");
    }
}
```

**说明**：
- 这些逻辑已完全移到VanillaAdapter
- TrackingModule现在只负责纯粹的追踪逻辑

---

### 步骤4：编译和测试

#### 4.1 编译项目

**命令**：
```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

**预期结果**：
- 编译成功，无错误
- 生成`BDP.dll`和`BDP.pdb`

**常见错误**：
1. `VanillaAdapter未定义` → 检查文件是否在正确目录
2. `FlightPhase未定义` → 检查using语句
3. `TrackingDiag未定义` → 检查命名空间

#### 4.2 游戏内测试

**测试场景1：追踪弹墙壁拦截**
1. 创建测试地图，放置墙壁
2. 装备追踪武器，向墙壁后的敌人射击
3. **预期**：子弹被墙壁拦截（不穿墙）
4. **验证**：开启Dev Mode，查看日志无`[VanillaAdapter]`错误

**测试场景2：追踪弹目标切换**
1. 创建测试地图，放置多个敌人
2. 装备追踪武器，射击敌人A
3. 子弹飞行途中，敌人A死亡，子弹切换到敌人B
4. **预期**：子弹命中敌人B（不是隔空命中敌人A）
5. **验证**：查看日志有`[VanillaAdapter] SyncUsedTarget`

**测试场景3：引导弹折线飞行**
1. 创建测试地图，放置障碍物
2. 装备引导武器，向障碍物后的敌人射击
3. **预期**：子弹绕过障碍物，命中目标
4. **验证**：查看日志无`[VanillaAdapter]`错误

**测试场景4：追踪弹丢锁**
1. 创建测试地图，放置敌人
2. 装备追踪武器，射击敌人
3. 子弹飞行途中，敌人死亡
4. **预期**：子弹打地面（不是隔空命中尸体）
5. **验证**：查看日志有`[VanillaAdapter] ForceGround: Phase=TrackingLost`

#### 4.3 性能测试

**测试方法**：
1. 创建大规模战斗场景（20+ pawns）
2. 同时发射多个追踪弹
3. 监控TPS（Ticks Per Second）

**预期结果**：
- TPS无明显下降（适配层零额外开销）
- 内存占用无明显增加

---

## 三、回滚方案

如果测试发现问题，可以快速回滚：

### 方案A：完全回滚

1. 删除`VanillaAdapter.cs`
2. 恢复`Bullet_BDP.cs`的修改（使用git）
3. 恢复`TrackingModule.cs`的修改（使用git）
4. 重新编译

### 方案B：部分回滚

保留VanillaAdapter，但不使用：

1. 在`Bullet_BDP.SpawnSetup`中注释掉适配层配置：
   ```csharp
   // vanillaAdapter.ConfigureStrategy(...);
   // vanillaAdapter.RecordTrueOrigin(...);
   ```

2. 在`Bullet_BDP.ApplyFlightRedirect`中恢复原逻辑：
   ```csharp
   // origin = vanillaAdapter.ComputeAdaptedOrigin(...);
   // 恢复原代码
   ```

3. 在`Bullet_BDP.ImpactSomething`中注释掉适配层调用：
   ```csharp
   // var impactCheck = vanillaAdapter.CheckBeforeImpact(...);
   ```

4. 恢复`TrackingModule.ResolveHit`方法

---

## 四、后续优化

### 4.1 完全移除HitResolve

在确认适配层稳定后，可以考虑：
1. 删除`IBDPHitResolver`接口
2. 删除`Bullet_BDP`中的`hitResolvers`缓存
3. 删除`ImpactSomething`中的HitResolve调用

### 4.2 配置化origin偏移距离

如果需要XML配置origin偏移距离：

1. 在`FlightRedirectConfig`中添加字段：
   ```csharp
   public float vanillaOriginOffset = 6f;
   ```

2. 在`Bullet_BDP.SpawnSetup`中配置：
   ```csharp
   vanillaAdapter.SetOriginOffsetDistance(redirectConfig.vanillaOriginOffset);
   ```

### 4.3 添加诊断日志开关

在`VanillaAdapter`中添加：
```csharp
public static bool EnableDiag = false;

public bool TrySyncUsedTarget(...)
{
    // ...
    if (EnableDiag)  // 替代TrackingDiag.Enabled
        Log.Message($"[VanillaAdapter] SyncUsedTarget: {trackingTarget}");
    // ...
}
```

---

## 五、常见问题

### Q1：为什么不用Harmony Patch修改CheckForFreeIntercept？

**A**：
- Harmony Patch会影响所有投射物，不只是BDP
- 后退origin的方案更简单，不需要额外的Patch
- 保留了未来使用Harmony的可能性（通过`GetInterceptOrigin()`）

### Q2：为什么origin偏移距离固定为6格？

**A**：
- vanilla的拦截距离阈值是5-12格
- 6格是一个经验值，覆盖大部分场景
- 如果需要自定义，可以通过`SetOriginOffsetDistance()`配置

### Q3：适配层会影响性能吗？

**A**：
- 不会，适配层只是计算逻辑，没有额外数据结构
- 所有方法都是简单的条件判断和赋值
- 相比原来的实现，性能完全相同

### Q4：如果vanilla更新，拦截机制变化怎么办？

**A**：
- 只需修改`VanillaAdapter.ComputeAdaptedOrigin`方法
- 模块代码完全不受影响
- 这正是适配层的价值所在

### Q5：为什么不直接删除HitResolve接口？

**A**：
- 保留兼容性，避免破坏现有模块
- 未来可能有其他模块需要HitResolve
- 可以在确认稳定后再删除

---

## 六、总结

### 6.1 修改清单

| 文件 | 修改类型 | 行数变化 |
|------|----------|----------|
| VanillaAdapter.cs | 新增 | +300 |
| Bullet_BDP.cs | 修改 | +20, -15 |
| TrackingModule.cs | 删除 | -30 |
| 总计 | - | +290 |

### 6.2 收益

1. **职责清晰**：模块不再包含vanilla hack
2. **易于维护**：所有hack集中在一个地方
3. **易于理解**：新人看模块代码不会被困扰
4. **易于扩展**：新增弹道类型不需要关心vanilla冲突
5. **零性能开销**：只是代码重组，没有额外计算

### 6.3 风险

1. **测试覆盖**：需要充分测试各种场景
2. **兼容性**：需要验证存档兼容性
3. **文档更新**：需要更新相关设计文档

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-03 | 初始版本：完整的VanillaAdapter重构实施指南 | Claude Sonnet 4.6 |
