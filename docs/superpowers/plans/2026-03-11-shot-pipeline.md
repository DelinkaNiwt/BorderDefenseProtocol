# Shot Pipeline Refactor Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the BDP shooting system from partial pipeline (ballistics only) to full 3-phase pipeline (Aiming → Firing → Ballistics), eliminating duplicate code across Verb subclasses and enabling modular extension via XML-configured modules.

**Architecture:** 3-phase pipeline driven by `ShotPipeline` (stateless, Verb-held) and `ShotSession` (per-shot state). Aiming phase has two sub-steps: Targeting (multi-frame interactive) and Resolve (single-tick). Firing phase modules produce `FireIntent`. Ballistics phase delegates to existing v5 Bullet_BDP pipeline. Verb subclasses (Single/Dual/Combo) retain only `ExecuteFire()` dispatch logic.

**Tech Stack:** C# 7.3, RimWorld 1.6 modding API, Harmony patches. No unit test framework — compilation is the verification method.

**Design Document:** `docs/plans/2026-03-11-shot-pipeline-design.md`

**Compile Command:** `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

---

## File Structure

### New Files (ShotPipeline/)

| File | Responsibility |
|------|---------------|
| `Trigger/ShotPipeline/Interfaces/IShotModule.cs` | Base module interface with Priority |
| `Trigger/ShotPipeline/Interfaces/IShotAimModule.cs` | Aim phase resolve interface |
| `Trigger/ShotPipeline/Interfaces/IShotAimRenderer.cs` | Aim phase targeting render interface |
| `Trigger/ShotPipeline/Interfaces/IShotAimValidator.cs` | Aim phase target validation interface |
| `Trigger/ShotPipeline/Interfaces/IShotFireModule.cs` | Fire phase interface |
| `Trigger/ShotPipeline/Data/ShotContext.cs` | Read-only shot context snapshot |
| `Trigger/ShotPipeline/Data/AimIntent.cs` | Aim module output intent |
| `Trigger/ShotPipeline/Data/AimResult.cs` | Merged aim phase result |
| `Trigger/ShotPipeline/Data/AimValidation.cs` | Target validation result |
| `Trigger/ShotPipeline/Data/FireIntent.cs` | Fire module output intent |
| `Trigger/ShotPipeline/Data/FireResult.cs` | Merged fire phase result |
| `Trigger/ShotPipeline/ShotSession.cs` | Per-shot stateful data container |
| `Trigger/ShotPipeline/ShotPipeline.cs` | Pipeline host — module registry + phase execution |
| `Trigger/ShotPipeline/Config/ShotModuleConfig.cs` | Abstract module config base (XML serializable) |
| `Trigger/ShotPipeline/Config/LosCheckConfig.cs` | LOS check module config |
| `Trigger/ShotPipeline/Config/AnchorAimConfig.cs` | Anchor aim module config |
| `Trigger/ShotPipeline/Config/AutoRouteAimConfig.cs` | Auto-route aim module config |
| `Trigger/ShotPipeline/Config/AreaIndicatorConfig.cs` | Area indicator config (wrapper) |
| `Trigger/ShotPipeline/Config/VolleySpreadConfig.cs` | Volley spread module config |
| `Trigger/ShotPipeline/Config/TrionCostConfig.cs` | Trion cost module config |
| `Trigger/ShotPipeline/Config/FlightDataConfig.cs` | Flight data module config |
| `Trigger/ShotPipeline/Config/AutoRouteFireConfig.cs` | Auto-route fire module config |
| `Trigger/ShotPipeline/Modules/LosCheckModule.cs` | LOS validation (migrated from Verb TryCastShot) |
| `Trigger/ShotPipeline/Modules/AnchorAimModule.cs` | Anchor targeting (migrated from StartAnchorTargeting) |
| `Trigger/ShotPipeline/Modules/AutoRouteAimModule.cs` | Auto-route LOS redirect (migrated from VerbFlightState) |
| `Trigger/ShotPipeline/Modules/AreaIndicatorModule.cs` | Range indicator rendering (migrated from DrawAreaIndicators) |
| `Trigger/ShotPipeline/Modules/VolleySpreadModule.cs` | Volley spread calculation |
| `Trigger/ShotPipeline/Modules/TrionCostModule.cs` | Trion consumption (migrated from ChipUsageCostHelper calls) |
| `Trigger/ShotPipeline/Modules/FlightDataModule.cs` | Guided flight data prep (migrated from OnProjectileLaunched) |
| `Trigger/ShotPipeline/Modules/AutoRouteFireModule.cs` | Auto-route computation (migrated from VerbFlightState.PrepareAutoRoute) |

### Modified Files

| File | Changes |
|------|---------|
| `Trigger/Data/VerbChipConfig.cs` | Add `aimModules` and `fireModules` list fields |
| `Trigger/DualWeapon/Verb_BDPRangedBase.cs` | Integrate ShotPipeline, add ExecuteFire abstract method, move shared logic to pipeline |
| `Trigger/DualWeapon/Verb_BDPSingle.cs` | Slim to ExecuteFire + FiringPattern dispatch only |
| `Trigger/DualWeapon/Verb_BDPDual.cs` | Slim to ExecuteFire + dual-side alternation only |
| `Trigger/DualWeapon/Verb_BDPCombo.cs` | Slim to ExecuteFire + param averaging only |
| `Trigger/Comps/CompTriggerBody.VerbSystem.cs` | Pipeline initialization during Verb creation |
| `Trigger/UI/Command_BDPChipAttack.cs` | Targeting integration with pipeline session |
| `Projectiles/Bullet_BDP.cs` | Add InjectShotData(ShotSession) method |

### Deleted Files

| File | Reason |
|------|--------|
| `Projectiles/VerbFlightState.cs` | All functionality migrated to pipeline modules + ShotSession |

**All new files are under:** `模组工程/BorderDefenseProtocol/Source/BDP/`

---

## Chunk 1: Foundation — Interfaces, Data Models, Pipeline Core

### Task 1: Create Pipeline Interface Files

**Files:**
- Create: `Trigger/ShotPipeline/Interfaces/IShotModule.cs`
- Create: `Trigger/ShotPipeline/Interfaces/IShotAimModule.cs`
- Create: `Trigger/ShotPipeline/Interfaces/IShotAimRenderer.cs`
- Create: `Trigger/ShotPipeline/Interfaces/IShotAimValidator.cs`
- Create: `Trigger/ShotPipeline/Interfaces/IShotFireModule.cs`

- [ ] **Step 1: Create IShotModule.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击管线模块基接口
    /// 所有阶段模块的共同祖先
    /// </summary>
    public interface IShotModule
    {
        /// <summary>执行优先级（升序，值小先执行）</summary>
        int Priority { get; }
    }
}
```

- [ ] **Step 2: Create IShotAimModule.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准阶段模块——参与Resolve子步骤（单Tick，TryCastShot内）
    /// 产出AimIntent，由宿主合并为AimResult
    /// </summary>
    public interface IShotAimModule : IShotModule
    {
        AimIntent ResolveAim(ShotSession session);
    }
}
```

- [ ] **Step 3: Create IShotAimRenderer.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 可选接口：参与Targeting子步骤的渲染（多帧，DrawHighlight中）
    /// 用于绘制范围圈、弹道预览等瞄准指示
    /// </summary>
    public interface IShotAimRenderer
    {
        void RenderTargeting(ShotSession session, Verse.LocalTargetInfo mouseTarget);
    }
}
```

- [ ] **Step 4: Create IShotAimValidator.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 可选接口：参与Targeting子步骤的目标验证
    /// 无效时阻止射击并显示原因
    /// </summary>
    public interface IShotAimValidator
    {
        AimValidation ValidateTarget(ShotSession session, Verse.LocalTargetInfo target);
    }
}
```

- [ ] **Step 5: Create IShotFireModule.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击阶段模块——在TryCastShot内、AimResult确定后调用
    /// 产出FireIntent，由宿主合并为FireResult
    /// </summary>
    public interface IShotFireModule : IShotModule
    {
        FireIntent OnFire(ShotSession session);
    }
}
```

- [ ] **Step 6: Compile to verify interfaces**

Run: compile command
Expected: SUCCESS (interfaces have no implementation dependencies)

- [ ] **Step 7: Commit**

```
feat(ShotPipeline): add pipeline interface definitions

IShotModule, IShotAimModule, IShotAimRenderer,
IShotAimValidator, IShotFireModule
```

---

### Task 2: Create Data Model Files

**Files:**
- Create: `Trigger/ShotPipeline/Data/AimIntent.cs`
- Create: `Trigger/ShotPipeline/Data/AimResult.cs`
- Create: `Trigger/ShotPipeline/Data/AimValidation.cs`
- Create: `Trigger/ShotPipeline/Data/FireIntent.cs`
- Create: `Trigger/ShotPipeline/Data/FireResult.cs`
- Create: `Trigger/ShotPipeline/Data/ShotContext.cs`

- [ ] **Step 1: Create AimIntent.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准意图：IShotAimModule产出，多个模块的意图由宿主合并
    /// </summary>
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
        public float AccuracyMultiplier;  // 默认1.0
        public float ForcedMissRadius;

        /// <summary>创建默认意图（不修改任何值）</summary>
        public static AimIntent Default => new AimIntent { AccuracyMultiplier = 1f };
    }
}
```

- [ ] **Step 2: Create AimResult.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准结果：宿主合并所有AimIntent后产出，传递给射击阶段
    /// </summary>
    public class AimResult
    {
        public LocalTargetInfo FinalTarget;
        public Vector3 AimPoint;
        public List<IntVec3> AnchorPath;
        public float AnchorSpread;
        public float AccuracyMultiplier = 1f;
        public float ForcedMissRadius;
        public bool Abort;
        public string AbortReason;

        public bool HasGuidedPath => AnchorPath != null && AnchorPath.Count > 0;
    }
}
```

- [ ] **Step 3: Create AimValidation.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 目标验证结果：IShotAimValidator产出
    /// </summary>
    public struct AimValidation
    {
        public bool IsValid;
        public string InvalidReason;

        public static AimValidation Valid => new AimValidation { IsValid = true };

        public static AimValidation Invalid(string reason)
        {
            return new AimValidation { IsValid = false, InvalidReason = reason };
        }
    }
}
```

- [ ] **Step 4: Create FireIntent.cs**

```csharp
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击意图：IShotFireModule产出
    /// </summary>
    public struct FireIntent
    {
        // 投射物
        public ThingDef OverrideProjectileDef;

        // 发射参数修正
        public float SpreadRadius;
        public float DamageMultiplier;   // 默认1.0
        public float SpeedMultiplier;    // 默认1.0

        // 资源消耗
        public float TrionCost;
        public bool SkipTrionConsumption;

        // 控制标志
        public bool AbortShot;
        public string AbortReason;

        // 弹道管线注入
        public bool EnableAutoRoute;
        public ThingDef AutoRouteProjectileDef;

        public static FireIntent Default => new FireIntent
        {
            DamageMultiplier = 1f,
            SpeedMultiplier = 1f
        };
    }
}
```

- [ ] **Step 5: Create FireResult.cs**

```csharp
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击结果：宿主合并所有FireIntent后产出
    /// </summary>
    public class FireResult
    {
        public ThingDef ProjectileDef;
        public float SpreadRadius;
        public float DamageMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public float TrionCost;
        public bool SkipTrionConsumption;
        public bool EnableAutoRoute;
        public ThingDef AutoRouteProjectileDef;
        public bool Abort;
        public string AbortReason;
    }
}
```

- [ ] **Step 6: Create ShotContext.cs**

Reference existing types:
- `CompTriggerBody` at `Trigger/Comps/CompTriggerBody.Fields.cs`
- `VerbChipConfig` at `Trigger/Data/VerbChipConfig.cs`
- `RangedConfig` at `Trigger/Data/RangedConfig.cs` (check exact path)
- `GuidedConfig` at `Trigger/Data/GuidedConfig.cs` (check exact path)
- `FiringPattern` at `Trigger/Data/FiringPattern.cs`

```csharp
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击上下文：管线入口的初始数据快照，整个管线期间只读
    /// 构造时从Verb状态中采集所有必要数据
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
}
```

Note: Exact namespace/class references for `Verb_BDPRangedBase`, `SlotSide`, `RangedConfig`, `GuidedConfig`, `FiringPattern` must match the existing codebase. Check imports during implementation.

- [ ] **Step 7: Compile to verify data models**

Run: compile command
Expected: SUCCESS (data models are POCOs with no logic)

- [ ] **Step 8: Commit**

```
feat(ShotPipeline): add data model definitions

ShotContext, AimIntent, AimResult, AimValidation,
FireIntent, FireResult
```

---

### Task 3: Create ShotSession and ShotPipeline Core

**Files:**
- Create: `Trigger/ShotPipeline/ShotSession.cs`
- Create: `Trigger/ShotPipeline/Config/ShotModuleConfig.cs`
- Create: `Trigger/ShotPipeline/ShotPipeline.cs`

- [ ] **Step 1: Create ShotSession.cs**

```csharp
using System.Collections.Generic;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击会话：每次射击的有状态数据容器
    /// 瞄准开始时创建，射击完成后销毁
    /// </summary>
    public class ShotSession
    {
        // 只读上下文（构造时设置）
        public ShotContext Context { get; }

        // 瞄准阶段累积状态（Targeting子步骤期间模块可写）
        public List<IntVec3> AnchorPath;
        public LocalTargetInfo ValidatedTarget;

        // 自动绕行缓存（AutoRouteFireModule写入，LaunchProjectile读取）
        // 从VerbFlightState迁移而来
        public ObstacleRouter.RouteResult CachedRoute;
        public int AutoRouteAlternateIndex;

        // 阶段结果（宿主写入）
        public AimResult AimResult { get; internal set; }
        public FireResult FireResult { get; internal set; }

        public ShotSession(ShotContext context)
        {
            Context = context;
        }
    }
}
```

- [ ] **Step 2: Create ShotModuleConfig.cs**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 模块配置基类，XML可序列化
    /// 每个Config子类对应一个Module类，负责创建模块实例
    /// 与v5弹道管线的modExtensions→Module模式一致
    /// </summary>
    public abstract class ShotModuleConfig
    {
        /// <summary>执行优先级（升序，值小先执行）</summary>
        public int priority = 50;

        /// <summary>创建对应的模块实例</summary>
        public abstract IShotModule CreateModule();
    }
}
```

- [ ] **Step 3: Create ShotPipeline.cs**

This is the core orchestrator. Key responsibilities:
1. Hold module lists (sorted by Priority)
2. Build from VerbChipConfig (default modules + XML overrides)
3. Execute each phase: collect intents → merge → produce result

```csharp
using System.Collections.Generic;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击管线宿主：模块注册 + 阶段执行
    /// 无状态，Verb持有，芯片变更时重建
    /// </summary>
    public class ShotPipeline
    {
        // 模块列表（按Priority升序）
        private readonly List<IShotAimModule> aimModules = new List<IShotAimModule>();
        private readonly List<IShotFireModule> fireModules = new List<IShotFireModule>();

        // 可选接口缓存（从aimModules/fireModules中筛选）
        private readonly List<IShotAimRenderer> aimRenderers = new List<IShotAimRenderer>();
        private readonly List<IShotAimValidator> aimValidators = new List<IShotAimValidator>();

        /// <summary>
        /// 从VerbChipConfig构建管线
        /// 自动注入默认模块 + XML配置的额外模块
        /// </summary>
        public void Build(VerbChipConfig config)
        {
            aimModules.Clear();
            fireModules.Clear();
            aimRenderers.Clear();
            aimValidators.Clear();

            // 默认瞄准模块（所有芯片都需要）
            AddModule(new LosCheckModule(10));

            // 区域指示器（如果配置了）
            if (config?.areaIndicator != null)
                AddModule(new AreaIndicatorModule(40, config.areaIndicator));

            // 默认射击模块
            AddModule(new TrionCostModule(50));

            // XML配置的额外模块
            if (config?.aimModules != null)
                foreach (var cfg in config.aimModules)
                    AddModule(cfg.CreateModule());

            if (config?.fireModules != null)
                foreach (var cfg in config.fireModules)
                    AddModule(cfg.CreateModule());

            // 按Priority排序
            aimModules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            fireModules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        private void AddModule(IShotModule module)
        {
            if (module is IShotAimModule aim)
                aimModules.Add(aim);
            if (module is IShotFireModule fire)
                fireModules.Add(fire);
            if (module is IShotAimRenderer renderer)
                aimRenderers.Add(renderer);
            if (module is IShotAimValidator validator)
                aimValidators.Add(validator);
        }

        // ── Targeting 子步骤（多帧） ──

        /// <summary>渲染瞄准指示器（DrawHighlight中每帧调用）</summary>
        public void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget)
        {
            for (int i = 0; i < aimRenderers.Count; i++)
                aimRenderers[i].RenderTargeting(session, mouseTarget);
        }

        /// <summary>验证目标有效性（任一模块否决则无效）</summary>
        public AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target)
        {
            for (int i = 0; i < aimValidators.Count; i++)
            {
                var validation = aimValidators[i].ValidateTarget(session, target);
                if (!validation.IsValid)
                    return validation;
            }
            return AimValidation.Valid;
        }

        // ── Resolve 子步骤（单Tick） ──

        /// <summary>
        /// 执行瞄准解算：遍历aimModules，收集+合并AimIntent
        /// </summary>
        public AimResult ResolveAim(ShotSession session)
        {
            var result = new AimResult
            {
                FinalTarget = session.Context.Target,
                AimPoint = session.Context.Target.CenterVector3,
                AccuracyMultiplier = 1f
            };

            for (int i = 0; i < aimModules.Count; i++)
            {
                var intent = aimModules[i].ResolveAim(session);

                // 一票否决
                if (intent.AbortShot)
                {
                    result.Abort = true;
                    result.AbortReason = intent.AbortReason;
                    break;
                }

                // 目标覆盖：后者覆盖前者
                if (intent.OverrideTarget.HasValue)
                    result.FinalTarget = intent.OverrideTarget.Value;

                // 偏移累加
                if (intent.AimOffset.HasValue)
                    result.AimPoint += intent.AimOffset.Value;

                // 锚点路径：后者覆盖前者
                if (intent.AnchorPoints != null)
                {
                    result.AnchorPath = intent.AnchorPoints;
                    if (intent.AnchorSpread.HasValue)
                        result.AnchorSpread = intent.AnchorSpread.Value;
                }

                // 精度连乘
                result.AccuracyMultiplier *= intent.AccuracyMultiplier;

                // 偏移取最大
                if (intent.ForcedMissRadius > result.ForcedMissRadius)
                    result.ForcedMissRadius = intent.ForcedMissRadius;
            }

            session.AimResult = result;
            return result;
        }

        // ── Fire 阶段（单Tick） ──

        /// <summary>
        /// 执行射击阶段：遍历fireModules，收集+合并FireIntent
        /// </summary>
        public FireResult RunFire(ShotSession session)
        {
            var result = new FireResult
            {
                ProjectileDef = session.Context.VerbProps?.defaultProjectile,
                DamageMultiplier = 1f,
                SpeedMultiplier = 1f
            };

            for (int i = 0; i < fireModules.Count; i++)
            {
                var intent = fireModules[i].OnFire(session);

                // 一票否决
                if (intent.AbortShot)
                {
                    result.Abort = true;
                    result.AbortReason = intent.AbortReason;
                    break;
                }

                // 投射物覆盖
                if (intent.OverrideProjectileDef != null)
                    result.ProjectileDef = intent.OverrideProjectileDef;

                // 参数合并
                result.SpreadRadius += intent.SpreadRadius;
                result.DamageMultiplier *= intent.DamageMultiplier;
                result.SpeedMultiplier *= intent.SpeedMultiplier;
                result.TrionCost += intent.TrionCost;

                if (intent.SkipTrionConsumption)
                    result.SkipTrionConsumption = true;

                if (intent.EnableAutoRoute)
                {
                    result.EnableAutoRoute = true;
                    result.AutoRouteProjectileDef = intent.AutoRouteProjectileDef;
                }
            }

            session.FireResult = result;
            return result;
        }
    }
}
```

- [ ] **Step 4: Compile**

Run: compile command
Expected: May have reference errors for module classes not yet created. Create stub classes if needed.

- [ ] **Step 5: Create minimal module stubs to satisfy ShotPipeline references**

Create stub implementations for `LosCheckModule`, `AreaIndicatorModule`, `TrionCostModule` with empty bodies — just enough for compilation. These will be fully implemented in Chunk 2 and 3.

- [ ] **Step 6: Compile to verify foundation**

Run: compile command
Expected: SUCCESS

- [ ] **Step 7: Commit**

```
feat(ShotPipeline): add pipeline core — ShotSession, ShotPipeline, ShotModuleConfig
```

---

## Chunk 2: Aim Phase Modules

### Task 4: Implement LosCheckModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/LosCheckModule.cs`
- Create: `Trigger/ShotPipeline/Config/LosCheckConfig.cs`

**Migrating from:** `Verb_BDPRangedBase.TryCastShotCore()` lines ~176-190 (LOS check block)

- [ ] **Step 1: Implement LosCheckModule**

Implements both `IShotAimModule` (resolve) and `IShotAimValidator` (targeting validation).

Key logic to migrate from `Verb_BDPRangedBase.TryCastShotCore()`:
- `GenSight.LineOfSight(caster.Position, target.Cell, caster.Map)` check
- When guided mode active: check LOS to first anchor instead of final target
- On LOS failure: set `AbortShot = true`

```csharp
namespace BDP.Trigger.ShotPipeline
{
    public class LosCheckModule : IShotAimModule, IShotAimValidator
    {
        public int Priority { get; }

        public LosCheckModule(int priority) { Priority = priority; }

        public AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target)
        {
            var ctx = session.Context;
            // 引导模式时跳过直接LOS检查（会检查到首锚点的LOS）
            if (ctx.GuidedConfig != null && ctx.GuidedConfig.enabled)
                return AimValidation.Valid;

            bool hasLos = GenSight.LineOfSight(
                ctx.CasterPosition, target.Cell, ctx.Caster.Map);
            return hasLos
                ? AimValidation.Valid
                : AimValidation.Invalid("BDP_NoLineOfSight");
        }

        public AimIntent ResolveAim(ShotSession session)
        {
            var ctx = session.Context;
            var intent = AimIntent.Default;

            // 确定LOS检查目标
            var losTarget = ctx.Target;
            if (session.AnchorPath != null && session.AnchorPath.Count > 0)
            {
                // 引导模式：检查到首个锚点的LOS
                losTarget = new LocalTargetInfo(session.AnchorPath[0]);
            }

            if (!GenSight.LineOfSight(ctx.CasterPosition, losTarget.Cell, ctx.Caster.Map))
            {
                // 自动绕行模块可能在后续覆盖此结果
                // 此处只做基础LOS检查
                intent.AbortShot = true;
                intent.AbortReason = "LOS_Failed";
            }

            return intent;
        }
    }
}
```

- [ ] **Step 2: Create LosCheckConfig**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    public class LosCheckConfig : ShotModuleConfig
    {
        public override IShotModule CreateModule() => new LosCheckModule(priority);
    }
}
```

- [ ] **Step 3: Compile and commit**

```
feat(ShotPipeline): implement LosCheckModule

Migrates LOS validation from Verb TryCastShot to pipeline module.
Supports guided mode (check LOS to first anchor).
```

---

### Task 5: Implement AnchorAimModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/AnchorAimModule.cs`
- Create: `Trigger/ShotPipeline/Config/AnchorAimConfig.cs`

**Migrating from:** `Verb_BDPRangedBase.StartAnchorTargeting()` + `AnchorTargetingHelper`

- [ ] **Step 1: Implement AnchorAimModule**

This module handles guided missile anchor point targeting. It:
- During Targeting: renders anchor path preview
- During Resolve: reads accumulated anchor points from session, produces AimIntent

Reference: `StartAnchorTargeting()` at `Verb_BDPRangedBase.cs:~L420-460`
Reference: `AnchorTargetingHelper` at `Projectiles/AnchorTargetingHelper.cs`

```csharp
namespace BDP.Trigger.ShotPipeline
{
    public class AnchorAimModule : IShotAimModule, IShotAimRenderer
    {
        public int Priority { get; }
        private readonly float anchorSpread;

        public AnchorAimModule(int priority, float anchorSpread)
        {
            Priority = priority;
            this.anchorSpread = anchorSpread;
        }

        public void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget)
        {
            // 绘制锚点路径预览线
            // 迁移自 StartAnchorTargeting 中的预览渲染逻辑
            if (session.AnchorPath != null && session.AnchorPath.Count > 0)
            {
                // 使用现有 AnchorTargetingHelper 的绘制方法
                // 具体实现参考 AnchorTargetingHelper 的渲染代码
            }
        }

        public AimIntent ResolveAim(ShotSession session)
        {
            var intent = AimIntent.Default;
            var ctx = session.Context;

            // 只在引导模式启用时生效
            if (ctx.GuidedConfig == null || !ctx.GuidedConfig.enabled)
                return intent;

            // 从session读取累积的锚点路径
            if (session.AnchorPath != null && session.AnchorPath.Count > 0)
            {
                intent.AnchorPoints = session.AnchorPath;
                intent.AnchorSpread = anchorSpread;
            }

            return intent;
        }
    }
}
```

- [ ] **Step 2: Create AnchorAimConfig**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    public class AnchorAimConfig : ShotModuleConfig
    {
        public float anchorSpread = 1.0f;

        public override IShotModule CreateModule()
            => new AnchorAimModule(priority, anchorSpread);
    }
}
```

- [ ] **Step 3: Compile and commit**

```
feat(ShotPipeline): implement AnchorAimModule

Migrates guided anchor targeting to pipeline module.
Handles path preview rendering and anchor intent production.
```

---

### Task 6: Implement AutoRouteAimModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/AutoRouteAimModule.cs`
- Create: `Trigger/ShotPipeline/Config/AutoRouteAimConfig.cs`

**Migrating from:** `VerbFlightState.InterceptCastTarget()`, `VerbFlightState.GetLosCheckTarget()`

- [ ] **Step 1: Implement AutoRouteAimModule**

This module handles auto-route LOS redirect. When direct LOS fails but auto-route can find a path around obstacles, it overrides the abort from LosCheckModule.

Reference: `VerbFlightState.cs:L130-180` (InterceptCastTarget logic)

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 自动绕行瞄准模块
    /// Priority必须高于LosCheckModule（数值更大）
    /// 当LOS失败但有绕行路径时，取消Abort并设置绕行LOS目标
    /// </summary>
    public class AutoRouteAimModule : IShotAimModule, IShotAimValidator
    {
        public int Priority { get; }

        public AutoRouteAimModule(int priority) { Priority = priority; }

        public AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target)
        {
            // 自动绕行始终允许目标选择——路由计算在Fire阶段
            var ctx = session.Context;
            if (CanAutoRoute(ctx))
                return AimValidation.Valid;
            return AimValidation.Valid;  // 不阻止，LosCheckModule负责基础检查
        }

        public AimIntent ResolveAim(ShotSession session)
        {
            var intent = AimIntent.Default;
            var ctx = session.Context;

            if (!CanAutoRoute(ctx))
                return intent;

            // 如果有直接LOS，不需要绕行
            if (GenSight.LineOfSight(ctx.CasterPosition, ctx.Target.Cell, ctx.Caster.Map))
                return intent;

            // 尝试预计算绕行路径的首锚点作为LOS检查目标
            // 迁移自 VerbFlightState.InterceptCastTarget
            // 如果能找到绕行路径，取消LOS abort
            intent.AbortShot = false;  // 覆盖LosCheckModule的abort

            return intent;
        }

        private bool CanAutoRoute(ShotContext ctx)
        {
            // 检查是否配置了自动绕行能力
            return ctx.RangedConfig?.guided != null
                && ctx.RangedConfig.guided.enabled
                && !ctx.Verb.SupportsGuided;  // 手动引导优先，自动绕行作为fallback
        }
    }
}
```

Note: The exact auto-route logic is complex — during implementation, migrate line-by-line from `VerbFlightState.InterceptCastTarget()` and `PrepareAutoRoute()`.

- [ ] **Step 2: Create AutoRouteAimConfig**

- [ ] **Step 3: Compile and commit**

```
feat(ShotPipeline): implement AutoRouteAimModule

Migrates auto-route LOS redirect from VerbFlightState to pipeline.
```

---

### Task 7: Implement AreaIndicatorModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/AreaIndicatorModule.cs`

**Migrating from:** `Verb_BDPRangedBase.DrawAreaIndicators()` and `Verb_BDPSingle/Dual.DrawAreaIndicators()`

- [ ] **Step 1: Implement AreaIndicatorModule**

Only implements `IShotAimRenderer` (no resolve logic needed — pure display).

Reference: existing `IAreaIndicator` interface at `Trigger/IAreaIndicator.cs`
Reference: existing `CircleAreaIndicator` at `Trigger/CircleAreaIndicator.cs`

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 范围指示器模块：在瞄准模式中渲染范围圈
    /// 委托给现有 IAreaIndicator 实现
    /// </summary>
    public class AreaIndicatorModule : IShotAimRenderer
    {
        public int Priority { get; }
        private readonly AreaIndicatorConfig config;

        public AreaIndicatorModule(int priority, AreaIndicatorConfig config)
        {
            Priority = priority;
            this.config = config;
        }

        public void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget)
        {
            if (config?.indicator == null) return;
            config.indicator.DrawIndicator(session.Context.Caster, mouseTarget);
        }
    }
}
```

Note: No separate Config file needed — uses existing `AreaIndicatorConfig` from `Trigger/AreaIndicatorConfig.cs`.

- [ ] **Step 2: Compile and commit**

```
feat(ShotPipeline): implement AreaIndicatorModule

Migrates range indicator rendering to pipeline module.
Delegates to existing IAreaIndicator system.
```

---

## Chunk 3: Fire Phase Modules

### Task 8: Implement VolleySpreadModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/VolleySpreadModule.cs`
- Create: `Trigger/ShotPipeline/Config/VolleySpreadConfig.cs`

**Migrating from:** `VerbChipConfig.ranged.volleySpreadRadius` usage in Verb subclasses

- [ ] **Step 1: Implement VolleySpreadModule**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 齐射散布模块：注入齐射散布半径到FireIntent
    /// </summary>
    public class VolleySpreadModule : IShotFireModule
    {
        public int Priority { get; }
        private readonly float spreadRadius;

        public VolleySpreadModule(int priority, float spreadRadius)
        {
            Priority = priority;
            this.spreadRadius = spreadRadius;
        }

        public FireIntent OnFire(ShotSession session)
        {
            var intent = FireIntent.Default;
            intent.SpreadRadius = spreadRadius;
            return intent;
        }
    }
}
```

- [ ] **Step 2: Create VolleySpreadConfig**

```csharp
namespace BDP.Trigger.ShotPipeline
{
    public class VolleySpreadConfig : ShotModuleConfig
    {
        public float spreadRadius = 0f;

        public override IShotModule CreateModule()
            => new VolleySpreadModule(priority, spreadRadius);
    }
}
```

- [ ] **Step 3: Compile and commit**

---

### Task 9: Implement TrionCostModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/TrionCostModule.cs`
- Create: `Trigger/ShotPipeline/Config/TrionCostConfig.cs`

**Migrating from:** `ChipUsageCostHelper` calls in each Verb's TryCastShot

- [ ] **Step 1: Implement TrionCostModule**

Reference: find `ChipUsageCostHelper` usage in Verb_BDPSingle/Dual/Combo

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// Trion消耗模块：计算并执行Trion资源消耗
    /// </summary>
    public class TrionCostModule : IShotFireModule
    {
        public int Priority { get; }
        private readonly float costMultiplier;

        public TrionCostModule(int priority, float costMultiplier = 1f)
        {
            Priority = priority;
            this.costMultiplier = costMultiplier;
        }

        public FireIntent OnFire(ShotSession session)
        {
            var intent = FireIntent.Default;
            var ctx = session.Context;

            // 从芯片配置读取Trion消耗
            // 迁移自各Verb的TryCastShot末尾ChipUsageCostHelper调用
            float baseCost = 0f;  // TODO: 从ctx.ChipConfig读取实际消耗值
            intent.TrionCost = baseCost * costMultiplier;

            return intent;
        }
    }
}
```

- [ ] **Step 2: Create TrionCostConfig**

- [ ] **Step 3: Compile and commit**

```
feat(ShotPipeline): implement TrionCostModule and VolleySpreadModule
```

---

### Task 10: Implement FlightDataModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/FlightDataModule.cs`
- Create: `Trigger/ShotPipeline/Config/FlightDataConfig.cs`

**Migrating from:** `Verb_BDPRangedBase.OnProjectileLaunched()` — `gs.AttachManualFlight()` / `gs.AttachManualFlightIfActive()`

- [ ] **Step 1: Implement FlightDataModule**

This module prepares guided flight data in FireResult so that `LaunchProjectile()` can inject it into Bullet_BDP.

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 引导弹道数据模块：准备手动引导飞行数据
    /// 迁移自 VerbFlightState.AttachManualFlight
    /// </summary>
    public class FlightDataModule : IShotFireModule
    {
        public int Priority { get; }

        public FlightDataModule(int priority) { Priority = priority; }

        public FireIntent OnFire(ShotSession session)
        {
            // 引导数据已在AimResult.AnchorPath中
            // FlightDataModule确认数据就绪，不额外修改FireIntent
            // 实际注入在LaunchProjectile中通过读取session.AimResult完成
            return FireIntent.Default;
        }
    }
}
```

- [ ] **Step 2: Create FlightDataConfig**

- [ ] **Step 3: Compile and commit**

---

### Task 11: Implement AutoRouteFireModule

**Files:**
- Create: `Trigger/ShotPipeline/Modules/AutoRouteFireModule.cs`
- Create: `Trigger/ShotPipeline/Config/AutoRouteFireConfig.cs`

**Migrating from:** `VerbFlightState.PrepareAutoRoute()` + `VerbFlightState.AttachAutoRouteFlight()`

- [ ] **Step 1: Implement AutoRouteFireModule**

The most complex fire module. Migrates ObstacleRouter invocation and route caching from VerbFlightState.

Reference: `VerbFlightState.cs:L243-309` (PrepareAutoRoute + AttachAutoRouteFlight)

```csharp
namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 自动绕行射击模块
    /// 在首次射击时计算绕行路由，缓存到ShotSession
    /// 后续子弹交替使用左/右路径
    /// </summary>
    public class AutoRouteFireModule : IShotFireModule
    {
        public int Priority { get; }

        public AutoRouteFireModule(int priority) { Priority = priority; }

        public FireIntent OnFire(ShotSession session)
        {
            var intent = FireIntent.Default;
            var ctx = session.Context;

            // 检查是否需要自动绕行
            if (ctx.GuidedConfig == null || !ctx.GuidedConfig.enabled)
                return intent;

            // 手动锚点模式不走自动绕行
            if (session.AimResult.HasGuidedPath)
                return intent;

            // 有直接LOS不需要绕行
            if (GenSight.LineOfSight(ctx.CasterPosition, ctx.Target.Cell, ctx.Caster.Map))
                return intent;

            // 首次射击时计算路由（缓存到session）
            if (session.CachedRoute == null)
            {
                // 迁移自 VerbFlightState.PrepareAutoRoute
                // 使用 ObstacleRouter.ComputeIterativeRoute
                var autoRouteProjDef = ctx.Verb.GetAutoRouteProjectileDef();
                if (autoRouteProjDef != null)
                {
                    // TODO: 完整迁移 ObstacleRouter 调用
                    intent.EnableAutoRoute = true;
                    intent.AutoRouteProjectileDef = autoRouteProjDef;
                }
            }

            return intent;
        }
    }
}
```

- [ ] **Step 2: Create AutoRouteFireConfig**

- [ ] **Step 3: Compile and commit**

```
feat(ShotPipeline): implement FlightDataModule and AutoRouteFireModule

Migrates guided flight data and auto-route computation to pipeline modules.
```

---

## Chunk 4: Configuration Integration

### Task 12: Modify VerbChipConfig

**Files:**
- Modify: `Trigger/Data/VerbChipConfig.cs`

- [ ] **Step 1: Read current VerbChipConfig.cs**

Read the file to understand exact structure and add new fields.

- [ ] **Step 2: Add aimModules and fireModules fields**

Add to `VerbChipConfig` class:

```csharp
// 射击管线模块配置（XML注入）
public List<ShotModuleConfig> aimModules;
public List<ShotModuleConfig> fireModules;
```

Ensure proper `using BDP.Trigger.ShotPipeline;` import.

- [ ] **Step 3: Compile and commit**

```
feat(VerbChipConfig): add shot pipeline module configuration fields
```

---

## Chunk 5: Verb Layer Refactor

This is the core refactoring chunk. Each task must be done carefully to preserve all existing functionality.

### Task 13: Refactor Verb_BDPRangedBase — Integrate ShotPipeline

**Files:**
- Modify: `Trigger/DualWeapon/Verb_BDPRangedBase.cs` (553 lines → refactor)

**Critical:** This task modifies the most important file. Read the entire file before making changes.

- [ ] **Step 1: Read Verb_BDPRangedBase.cs completely**

Understand every method and field. Map each piece of logic to its pipeline destination.

- [ ] **Step 2: Add pipeline fields**

```csharp
// 射击管线
internal ShotPipeline shotPipeline;
internal ShotSession activeSession;
```

- [ ] **Step 3: Add pipeline initialization method**

```csharp
/// <summary>
/// 初始化射击管线（CompTriggerBody.RebuildVerbs时调用）
/// </summary>
internal void InitShotPipeline(VerbChipConfig config)
{
    shotPipeline = new ShotPipeline();
    shotPipeline.Build(config);
}
```

- [ ] **Step 4: Add BuildContext method**

```csharp
/// <summary>创建射击上下文快照</summary>
protected ShotContext BuildContext()
{
    var triggerComp = GetTriggerComp();
    var chipConfig = triggerComp?.GetChipExtension<VerbChipConfig>();
    return new ShotContext
    {
        Caster = CasterPawn,
        TriggerComp = triggerComp,
        Target = currentTarget,
        CasterPosition = caster.Position,
        Verb = this,
        ChipConfig = chipConfig,
        ChipSide = chipSide,
        ChipThing = GetCurrentChipThing(),  // 迁移自各子类
        RangedConfig = chipConfig?.ranged,
        GuidedConfig = GetGuidedConfig(),
        FiringPattern = GetFiringPattern(),  // 新增虚方法
        VerbProps = verbProps
    };
}
```

- [ ] **Step 5: Add ExecuteFire abstract method**

```csharp
/// <summary>
/// 子类实现：编排投射物发射
/// 管线的瞄准和射击模块已完成，此方法只负责"发几颗、怎么交替"
/// </summary>
protected abstract bool ExecuteFire(ShotSession session);

/// <summary>子类提供当前芯片Thing</summary>
protected abstract Thing GetCurrentChipThing();

/// <summary>子类提供发射模式</summary>
protected virtual FiringPattern GetFiringPattern() => FiringPattern.Sequential;
```

- [ ] **Step 6: Add unified LaunchProjectile method**

Migrate core launch logic from `TryCastShotCore()`. This is the critical migration — the ~130-line method needs to be preserved exactly.

```csharp
/// <summary>
/// 统一投射物发射方法（替代旧的TryCastShotCore部分逻辑）
/// 子类的ExecuteFire调用此方法来实际发射
/// </summary>
protected bool LaunchProjectile(ShotSession session, Thing chipEquipment)
{
    // 完整迁移自 TryCastShotCore 的发射逻辑
    // 包含：
    // 1. 获取投射物Def（从FireResult或默认）
    // 2. 计算origin（caster.DrawPos + 偏移）
    // 3. 计算destination（AimResult.AimPoint + 散布）
    // 4. ForcedMissRadius处理（来自AimResult）
    // 5. 掩体命中判定（保留原版逻辑）
    // 6. GenSpawn.Spawn(projectile)
    // 7. 弹道管线数据注入：InjectShotData
    // 具体代码在实施时从TryCastShotCore逐行迁移
    return true;
}
```

- [ ] **Step 7: Modify DrawHighlight to delegate to pipeline**

```csharp
public override void DrawHighlight(LocalTargetInfo target)
{
    // 管线渲染
    if (shotPipeline != null && activeSession != null)
        shotPipeline.RenderTargeting(activeSession, target);

    // 保留原版highlight
    base.DrawHighlight(target);
}
```

- [ ] **Step 8: Add session lifecycle methods**

```csharp
/// <summary>进入瞄准模式时创建session</summary>
protected void BeginTargetingSession()
{
    activeSession = new ShotSession(BuildContext());
}

/// <summary>射击完成后清理session</summary>
protected void EndTargetingSession()
{
    activeSession = null;
}
```

- [ ] **Step 9: Compile — fix all reference errors**

This will likely have many compile errors. Fix one by one:
- Missing imports
- Method signature mismatches
- Type references

- [ ] **Step 10: Commit**

```
refactor(Verb_BDPRangedBase): integrate ShotPipeline

Add pipeline/session fields, BuildContext, LaunchProjectile,
ExecuteFire abstract method. Delegate DrawHighlight to pipeline.
```

---

### Task 14: Refactor Verb_BDPSingle

**Files:**
- Modify: `Trigger/DualWeapon/Verb_BDPSingle.cs` (175 lines → slim down)

- [ ] **Step 1: Read Verb_BDPSingle.cs completely**

- [ ] **Step 2: Implement ExecuteFire and GetCurrentChipThing**

```csharp
protected override Thing GetCurrentChipThing()
{
    var triggerComp = GetTriggerComp();
    var slot = triggerComp?.GetActiveSlot(chipSide ?? SlotSide.Left);
    return slot?.chip;
}

protected override FiringPattern GetFiringPattern() => firingPattern;

protected override bool ExecuteFire(ShotSession session)
{
    var chipThing = GetCurrentChipThing();
    if (chipThing == null) return false;

    switch (firingPattern)
    {
        case FiringPattern.Simultaneous:
            return DoSimultaneousShot(session, chipThing);
        case FiringPattern.Sequential:
        default:
            return DoSequentialShot(session, chipThing);
    }
}
```

- [ ] **Step 3: Migrate DoSequentialShot and DoSimultaneousShot to use LaunchProjectile**

Replace `TryCastShotCore(chipThing)` calls with `LaunchProjectile(activeSession, chipThing)`.
Replace `FireVolleyLoop(...)` calls with loop calling `LaunchProjectile(...)`.

- [ ] **Step 4: Remove overridden TryCastShot** (now handled by base class)

The base class `Verb_BDPRangedBase.TryCastShot()` now handles the pipeline, then calls `ExecuteFire()`. Remove the `TryCastShot()` override from Single.

- [ ] **Step 5: Move DrawAreaIndicators logic to AreaIndicatorModule** (if not already done)

- [ ] **Step 6: Compile and commit**

```
refactor(Verb_BDPSingle): slim to ExecuteFire dispatch

Remove TryCastShot override, delegate pipeline logic to base class.
Only retain FiringPattern dispatch in ExecuteFire.
```

---

### Task 15: Refactor Verb_BDPDual

**Files:**
- Modify: `Trigger/DualWeapon/Verb_BDPDual.cs` (623 lines → slim down)

This is the most complex subclass due to dual-side alternation logic.

- [ ] **Step 1: Read Verb_BDPDual.cs completely**

- [ ] **Step 2: Implement ExecuteFire**

Keep all dual-side alternation logic (dualBurstIndex, leftRemaining, rightRemaining, GetCurrentShotSide) — this is structural, not pipeline logic.

```csharp
protected override bool ExecuteFire(ShotSession session)
{
    // InitDualBurst on first call
    // Determine which side fires (GetCurrentShotSide)
    // Call LaunchProjectile with appropriate side's chip
    // Maintain alternation counters
}
```

- [ ] **Step 3: Replace TryCastShotCore calls with LaunchProjectile**

- [ ] **Step 4: Remove overridden TryCastShot**

- [ ] **Step 5: Remove LOS check logic** (now in LosCheckModule)

The Dual verb has its own LOS checks in `InitDualBurst()` for left/right side validity. Migrate this to pipeline or keep as ExecuteFire precondition.

- [ ] **Step 6: Compile and commit**

```
refactor(Verb_BDPDual): slim to ExecuteFire dual-side dispatch

Remove TryCastShot override. Retain dual-side alternation logic.
LOS checks migrated to pipeline modules.
```

---

### Task 16: Refactor Verb_BDPCombo

**Files:**
- Modify: `Trigger/DualWeapon/Verb_BDPCombo.cs` (217 lines → slim down)

- [ ] **Step 1: Read Verb_BDPCombo.cs completely**

- [ ] **Step 2: Implement ExecuteFire**

Keep combo parameter averaging (avgBurstCount, avgTrionCost, etc).

```csharp
protected override bool ExecuteFire(ShotSession session)
{
    // ComputeEffectiveBurst
    // FiringPattern dispatch (similar to Single)
    // LaunchProjectile with combo parameters
}
```

- [ ] **Step 3: Remove overridden TryCastShot**

- [ ] **Step 4: Compile and commit**

```
refactor(Verb_BDPCombo): slim to ExecuteFire combo dispatch
```

---

## Chunk 6: Bridge and Cleanup

### Task 17: Modify Bullet_BDP — Add InjectShotData

**Files:**
- Modify: `Projectiles/Bullet_BDP.cs`

- [ ] **Step 1: Read Bullet_BDP.cs**

Understand current `TryInitGuidedFlight()` API.

- [ ] **Step 2: Add InjectShotData method**

```csharp
/// <summary>
/// 从ShotSession注入射击管线数据（替代旧的VerbFlightState注入）
/// 在LaunchProjectile中调用
/// </summary>
public void InjectShotData(ShotSession session)
{
    if (session == null) return;

    var aimResult = session.AimResult;
    var fireResult = session.FireResult;

    // 穿透力
    if (session.Context.RangedConfig != null)
        PassthroughPower = session.Context.RangedConfig.passthroughPower;

    // 引导弹道
    if (aimResult.HasGuidedPath)
    {
        TryInitGuidedFlight(
            aimResult.AnchorPath,
            aimResult.FinalTarget,
            aimResult.AnchorSpread);
    }
    // 自动绕行
    else if (fireResult.EnableAutoRoute && session.CachedRoute != null)
    {
        // 迁移自 VerbFlightState.AttachAutoRouteFlight
        // 交替分配左/右路径
        var anchors = GetAutoRouteAnchors(session);
        if (anchors != null)
        {
            TryInitGuidedFlight(
                anchors,
                aimResult.FinalTarget,
                aimResult.AnchorSpread);
        }
    }
}

private List<IntVec3> GetAutoRouteAnchors(ShotSession session)
{
    // 迁移自 VerbFlightState.AttachAutoRouteFlight 的交替逻辑
    var route = session.CachedRoute;
    if (route == null) return null;

    var index = session.AutoRouteAlternateIndex++;
    return (index % 2 == 0) ? route.LeftAnchors : route.RightAnchors;
}
```

- [ ] **Step 3: Compile and commit**

```
feat(Bullet_BDP): add InjectShotData method

New data injection entry point for shot pipeline integration.
Replaces VerbFlightState.AttachManualFlight/AttachAutoRouteFlight.
```

---

### Task 18: Update CompTriggerBody.VerbSystem — Pipeline Initialization

**Files:**
- Modify: `Trigger/Comps/CompTriggerBody.VerbSystem.cs`

- [ ] **Step 1: Read CompTriggerBody.VerbSystem.cs**

Focus on `CreateAndCacheChipVerbs()` and `CreateComboVerb()`.

- [ ] **Step 2: Add pipeline initialization after Verb creation**

In `CreateAndCacheChipVerbs()`, after setting verb fields, add:

```csharp
// 初始化射击管线
if (verb is Verb_BDPRangedBase rangedVerb)
{
    rangedVerb.InitShotPipeline(cfg);
}
```

Do the same in `CreateComboVerb()`.

- [ ] **Step 3: Compile and commit**

```
feat(CompTriggerBody): initialize shot pipeline during Verb creation
```

---

### Task 19: Update Command_BDPChipAttack — Targeting Integration

**Files:**
- Modify: `Trigger/UI/Command_BDPChipAttack.cs`

- [ ] **Step 1: Read Command_BDPChipAttack.cs**

Focus on `GizmoOnGUIInt()` where targeting starts.

- [ ] **Step 2: Add session creation on targeting start**

When the player clicks the attack gizmo (left click or right click), create the ShotSession:

```csharp
// In GizmoOnGUIInt, before StartAnchorTargeting or BeginTargeting:
if (verb is Verb_BDPRangedBase rangedVerb)
{
    rangedVerb.BeginTargetingSession();
}
```

- [ ] **Step 3: Compile and commit**

```
feat(Command_BDPChipAttack): integrate targeting session lifecycle
```

---

### Task 20: Delete VerbFlightState

**Files:**
- Delete: `Projectiles/VerbFlightState.cs`
- Modify: `Trigger/DualWeapon/Verb_BDPRangedBase.cs` (remove `gs` field and references)

**Critical:** This is the final cleanup. All VerbFlightState functionality must already be migrated.

- [ ] **Step 1: Grep for all VerbFlightState references**

```
grep -r "VerbFlightState" --include="*.cs"
grep -r "\.gs\." --include="*.cs"
grep -r "\.gs " --include="*.cs"
```

- [ ] **Step 2: For each reference, verify it's been migrated to pipeline**

Create a checklist of every reference and confirm its pipeline equivalent.

- [ ] **Step 3: Remove VerbFlightState field from Verb_BDPRangedBase**

Remove: `protected VerbFlightState gs;`
Remove: all `gs.` method calls (should be replaced by pipeline calls)

- [ ] **Step 4: Remove ExposeData for gs** (serialization)

VerbFlightState was serialized in `ExposeData()`. ShotSession is transient — no serialization needed.

- [ ] **Step 5: Delete VerbFlightState.cs**

- [ ] **Step 6: Compile — fix all remaining references**

This will surface any missed migration points.

- [ ] **Step 7: Commit**

```
refactor: delete VerbFlightState

All functionality migrated to ShotPipeline modules and ShotSession.
```

---

## Chunk 7: XML Defs and Integration

### Task 21: Update XML Definitions

**Files:**
- Modify: XML def files in `1.6/Defs/Trigger/` that define chip configurations

- [ ] **Step 1: List all ThingDef XML files with VerbChipConfig**

```
grep -r "VerbChipConfig" --include="*.xml" -l
```

- [ ] **Step 2: For chips with guided/auto-route capabilities, add pipeline module configs**

Add `aimModules` and `fireModules` lists. Most chips only need defaults (no XML changes). Only chips with special configurations (custom anchorSpread, custom priority) need explicit module entries.

- [ ] **Step 3: Compile and commit**

```
feat(XML): add shot pipeline module configurations to chip defs
```

---

### Task 22: Full Compilation and Integration Verification

- [ ] **Step 1: Full clean build**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Rebuild -v:minimal
```

Expected: 0 errors, 0 warnings (or acceptable warnings)

- [ ] **Step 2: Verify DLL output**

Check that `1.6/Assemblies/BDP.dll` is updated.

- [ ] **Step 3: Create integration test checklist**

Test in-game:
- [ ] 单侧Sequential芯片：单发射击正常
- [ ] 单侧Simultaneous芯片：齐射正常
- [ ] 双侧芯片：左右交替正常
- [ ] 组合技芯片：参数混合正常
- [ ] 引导弹道（手动锚点）：锚点选择→引导飞行正常
- [ ] 自动绕行：障碍物后目标可被自动绕行攻击
- [ ] 范围指示器：瞄准时范围圈正常显示
- [ ] Trion消耗：射击后Trion正常减少

- [ ] **Step 4: Final commit**

```
feat(ShotPipeline): complete shot pipeline refactor

3-phase pipeline (Aiming → Firing → Ballistics) fully integrated.
All existing functionality preserved.
VerbFlightState deleted, replaced by pipeline modules + ShotSession.
```

---

## Summary

| Chunk | Tasks | Key Deliverable |
|-------|-------|----------------|
| 1: Foundation | 1-3 | Interfaces, data models, pipeline core |
| 2: Aim Modules | 4-7 | LOS, Anchor, AutoRoute, AreaIndicator modules |
| 3: Fire Modules | 8-11 | Spread, Trion, FlightData, AutoRoute modules |
| 4: Config | 12 | VerbChipConfig integration |
| 5: Verb Refactor | 13-16 | Base class pipeline + subclass slimming |
| 6: Bridge & Cleanup | 17-20 | Bullet_BDP bridge, VerbFlightState deletion |
| 7: Integration | 21-22 | XML defs, full compilation, testing |

**Total: 22 tasks, ~7 chunks, estimated 30+ atomic commits**
