# 新BDP 架构评估报告

> 基于代码第一性原理分析 | 分析对象: `Source/BDP/` (486个.cs文件) | 日期: 2026-04-24

---

## 一、总架构组成结构图

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                          BDPMod  (Harmony PatchAll)                           │
│                      BDP.csproj  →  .NET 4.8 / C# 7.3                        │
└────────────────────────────────────┬─────────────────────────────────────────┘
                                     │
          ┌──────────────────────────┼──────────────────────────┐
          │                          │                          │
     ┌────▼─────┐            ┌──────▼──────┐           ┌───────▼───────┐
     │ Bootstrap │            │   Patches   │           │  Support/     │
     │ (5 files) │            │  (13 files) │           │  Diagnostics  │
     └───────────┘            └─────────────┘           └───────────────┘
                                     │
    ┌────────────────────────────────┼────────────────────────────────────┐
    │                 RimWorld Pawn (Vanilla Comp 宿主)                    │
    │                                                                    │
    │   ┌──────────────┐    ┌──────────────────┐    ┌────────────────┐   │
    │   │  CompTrion   │    │CompCombatBodyHost │    │  Gene_Trion    │   │
    │   │  (Trion 能量)│    │ (战斗体状态机)     │    │ (Trion 基因)   │   │
    │   └──────────────┘    └──────────────────┘    └────────────────┘   │
    └────────────────────────────────────────────────────────────────────┘
                                     │
    ┌────────────────────────────────┼────────────────────────────────────┐
    │                   RimWorld Equipment (武器 Thing)                    │
    │                                                                    │
    │   ┌─────────────────────────────────────────────────────────────┐  │
    │   │              CompTriggerBody  (Trigger 真值 owner)           │  │
    │   │                                                             │  │
    │   │  ┌───────────────┐  ┌──────────────┐  ┌──────────────────┐  │  │
    │   │  │ mainSlots[]   │  │ subSlots[]   │  │ specialSlots[]   │  │  │
    │   │  │ (主侧槽位)     │  │ (副侧槽位)    │  │ (特殊侧槽位)      │  │  │
    │   │  └───────────────┘  └──────────────┘  └──────────────────┘  │  │
    │   │                                                             │  │
    │   │  ┌──────────────────────────────────────────────────────┐   │  │
    │   │  │         chipContainer (ThingOwner<Thing>)            │   │  │
    │   │  │         芯片正式容器                                   │   │  │
    │   │  └──────────────────────────────────────────────────────┘   │  │
    │   │                                                             │  │
    │   │  ┌──────────────────────────────────────────────────────┐   │  │
    │   │  │         TriggerBodyVerbHostManager                   │   │  │
    │   │  │         Formal Host Verb 绑定管理器                   │   │  │
    │   │  └──────────────────────────────────────────────────────┘   │  │
    │   │                                                             │  │
    │   │  ┌──────────────────────────────────────────────────────┐   │  │
    │   │  │         TriggerRuntimeServices                       │   │  │
    │   │  │         运行时服务根 (ExpressionService + 模块宿主)    │   │  │
    │   │  └──────────────────────────────────────────────────────┘   │  │
    │   └─────────────────────────────────────────────────────────────┘  │
    └────────────────────────────────────────────────────────────────────┘
                                     │
    ┌────────────────────────────────┼────────────────────────────────────┐
    │             对外正式表面 (Formal Surfaces)                            │
    │                                                                    │
    │  TriggerFormalSurfaces:                                            │
    │  ├─ TriggerLoadoutReaderSurface      → ITriggerLoadoutReader       │
    │  ├─ TriggerInteractionSurface        → ITriggerInteractionReader   │
    │  ├─ TriggerLoadoutCommandSurface     → ITriggerLoadoutCommands     │
    │  ├─ TriggerEventSurface              → ITriggerEvents              │
    │  └─ TriggerIntegrityDiagnosticsSurface → ITriggerIntegrityDiagnostics│
    └────────────────────────────────────┬───────────────────────────────┘
                                         │
    ┌────────────────────────────────────▼───────────────────────────────┐
    │                    TriggerRuntimeCoordinator                       │
    │                   (已发布投影的唯一 owner)                          │
    │                                                                    │
    │  RuntimeTick() 执行顺序:                                           │
    │  ① primary owner 守卫  →  ② post-load finalize                    │
    │  ③ 切换结算             →  ④ RebuildAndPublish()                  │
    │  ⑤ formal host tick     →  ⑥ VerbHostManager.Tick()               │
    │                                                                    │
    │  发布产物:                                                          │
    │  ├─ TriggerCombatProjectionState  (战斗投影: 含 ExpressionSnapshot) │
    │  └─ TriggerPresentationState     (表现投影: 含视觉/UI信息)          │
    └────────────────────────────────────┬───────────────────────────────┘
                                         │
         ┌───────────────────────────────┼───────────────────────────────┐
         │                               │                               │
    ┌────▼──────────┐          ┌────────▼────────┐          ┌───────────▼────┐
    │   EXPRESSIONS │          │ ATTACK EXECUTION │          │  VERB HOSTING  │
    │   表达系统     │          │   攻击执行系统    │          │   Verb 宿主层  │
    │   (82 files)  │          │   (95 files)     │          │   (5 files)    │
    └───────┬───────┘          └───────┬──────────┘          └───────┬────────┘
            │                         │                              │
    ┌───────▼───────┐        ┌───────▼──────────┐          ┌────────▼───────┐
    │    CHIPS      │        │   PROJECTILES    │          │  COMBAT BODY   │
    │   芯片系统     │        │   投射物系统      │          │  战斗体系统     │
    │  (22 files)   │        │   (37 files)     │          │  (31 files)    │
    └───────┬───────┘        └──────────────────┘          └───────┬────────┘
            │                                                      │
    ┌───────▼───────┐                                      ┌───────▼────────┐
    │    COMBOS     │                                      │BODY CONSTRAINTS│
    │   组合技系统   │                                      │ 身体约束系统    │
    │  (20 files)   │                                      │  (6 files)     │
    └───────────────┘                                      └───────┬────────┘
                                                                   │
    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  ┌────▼───────┐
    │    TRION     │    │ COMBAT LOG   │    │  SEMANTICS   │  │   GENES    │
    │   能量系统    │    │  战斗日志     │    │  语义上下文   │  │  基因系统   │
    │  (14 files)  │    │  (4 files)   │    │  (6 files)   │  │ (3 files)  │
    └──────────────┘    └──────────────┘    └──────────────┘  └────────────┘

    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
    │  ABILITIES   │    │   HEDIFFS    │    │ COMBAT MODEL │
    │   能力系统    │    │  健康状态     │    │  战斗数据模型  │
    │  (3 files)   │    │  (4 files)   │    │  (6 files)   │
    └──────────────┘    └──────────────┘    └──────────────┘
```

---

## 二、核心数据流: 攻击执行完整链路

```
                        ┌──────────────────────┐
                        │  玩家 / 自动战斗输入    │
                        └──────────┬───────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │    Patch 层拦截              │
                    │    TryGetAttackVerb          │
                    │    TryMeleeAttack            │
                    └──────────────┬──────────────┘
                                   │
         ┌─────────────────────────▼─────────────────────────┐
         │         AttackExecutionSurfaceAccess               │
         │         (攻击系统唯一正式入口)                       │
         │                                                   │
         │  ResolveEntry(pawn) → AttackExecutionService      │
         │  TryGetAutoRangedVerb / TryExecuteAutoMelee       │
         │  CreateTargetingSource                            │
         └─────────────────────────┬─────────────────────────┘
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 0: 读取已发布投影 (纯读, 不重算)              ║
         ║  TriggerCombatProjectionState                      ║
         ║  ├─ ExpressionSnapshot (表达快照, 冻结副本)         ║
         ║  ├─ ResultIndex (结果标识 → FormalExpressionResult) ║
         ║  └─ CompositeReferenceIndex (复合结果来源索引)       ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 1: AttackExecutionRequest                   ║
         ║  ├─ SessionToken (AttackSessionToken)             ║
         ║  │   ├─ AttackInstanceId (攻击实例标识)            ║
         ║  │   ├─ ResultId (命中结果标识)                    ║
         ║  │   ├─ ProjectionVersion (投影版本, 用于过期检测)  ║
         ║  │   └─ OwnerPawnThingId (宿主身份)                ║
         ║  ├─ AttackContextSnapshot (统一上下文冻结态)        ║
         ║  ├─ Pawn / Target / Reason / DispatchIntent       ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 2: AttackExecutionPreparedContext           ║
         ║  - 承载 Request + Projection + Result + Plan      ║
         ║  - 执行链从这里只读消费, 不反向写回表达层             ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼══════════════════════════════╗
         ║  Step 3: AttackExecutionService.TryBuildPlan()        ║
         ║                                                       ║
         ║  BuildCasts() → List<AttackExecutionCast>             ║
         ║    ├─ 单武器: BuildSingleResultCasts()                ║
         ║    │    ├─ 远程 → AppendRangedCasts()                 ║
         ║    │    │    ├─ Simultaneous → 单cast多emit             ║
         ║    │    │    └─ Sequential → 多cast顺序                ║
         ║    │    └─ 近战 → AppendMeleeCasts()                  ║
         ║    │                                                   ║
         ║    └─ 双持: BuildDualWeaponCasts()                    ║
         ║         ├─ LOS过滤: FilterDualRangedSidesByLegality() ║
         ║         │   每侧检查 RequiresDirectTargetLineOfSight   ║
         ║         │   不要求直射的侧直接放行; 要求的侧走 CanHit   ║
         ║         └─ 4种调度:                                    ║
         ║              Alternating  (交替, 默认)                  ║
         ║              Simultaneous (同时)                       ║
         ║              MainThenSub  (主侧→副侧顺序)               ║
         ║              MixedRhythm  (混合节奏按索引并列)           ║
         ║                                                       ║
         ║  BuildGroups() → List<AttackExecutionGroup>           ║
         ║    按GroupIndex归并Cast → Group                        ║
         ║    TimingMode: ImmediateTogether / SequenceInsideGroup ║
         ║    ExecutionKind: VerbSession / DirectEffect           ║
         ║                                                       ║
         ║  BuildSteps() → IReadOnlyList<AttackRuntimeStep>      ║
         ║    远程: 同组cast合并为一个step                          ║
         ║    近战: 每个cast保留为独立step                          ║
         ╚═════════════════════════╦══════════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 4: TryExecutePrepared()                    ║
         ║                                                   ║
         ║  ExecutionKind == DirectEffect (近战/纯效果)       ║
         ║    → effectEmitter.TryEmitGroup()                 ║
         ║                                                   ║
         ║  WeaponMode == Ranged                             ║
         ║    → rangedAttackExecutor.TryExecute(request)     ║
         ║                                                   ║
         ║  WeaponMode == Melee                              ║
         ║    → meleeAttackExecutor.TryExecute(request)      ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼══════════════════════════════╗
         ║  Step 5: 远程协议链 (RangedAttackProtocolService)       ║
         ║                                                       ║
         ║  Entry ──→ Aim ──→ Prepare ──→ Fire ──→ ProjectileInit║
         ║   │         │         │          │           │         ║
         ║   │    IAimStage  IPrepare   IFire    IProjectileInit  ║
         ║   │    Module[]   Module[]   Module[]  Module[]       ║
         ║   │                                                   ║
         ║   └── 每个阶段组合:                                     ║
         ║       基线模块 (全局) + 会话模块 (per-result)            ║
         ║       + Addon模块 (额外附加)                            ║
         ║                                                       ║
         ║       任一阶段中止 → 返回已得到的部分结果                 ║
         ║       中间产出: AimRecord / PrepareRecord / FireRecord  ║
         ║       最终产出: ProjectileInitPlan[] + VerbEmissionPlan  ║
         ╚═════════════════════════╦══════════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 6: Verb 宿主执行                             ║
         ║                                                   ║
         ║  BdpVerb_FormalHostShoot (正式远程宿主壳)           ║
         ║  ├─ 注册到原版 VerbTracker (外表是普通Verb)         ║
         ║  ├─ SyncFormalBinding() → 动态同步表面属性          ║
         ║  │   每帧从 BdpFormalVerbBinding 读取:              ║
         ║  │   VerbProps / Tool / Maneuver / SessionToken    ║
         ║  ├─ TryStartCastOn() → 走原版 Verb 起手流程         ║
         ║  │   暖机(warmup) → 命中判定 → 弹道计算              ║
         ║  ├─ 续射规划: RangedVerbContinuationPlanner         ║
         ║  └─ 会话代际清理:                                    ║
         ║      JobDriver 退出时只清自己这代会话                  ║
         ║      不放误清同壳上新种下的新会话                      ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 7: 投射物飞行 (RangedFlightProtocolService)  ║
         ║                                                   ║
         ║  Flight ──→ Arrival ──→ Hit ──→ Impact           ║
         ║    │           │         │         │               ║
         ║  IFlight   IArrival   IHit    IImpact              ║
         ║  Module[]  Module[]  Module[] Module[]             ║
         ║                                                   ║
         ║  模块会话通过 ProjectileInitPlan.AttackContextSnapshot ║
         ║  从发射时刻恢复, 保持飞行途中的上下文连续性               ║
         ╚═════════════════════════╦═════════════════════════╝
                                   │
         ╔═════════════════════════▼═════════════════════════╗
         ║  Step 8: 伤害应用 (原版 DamageWorker + BDP 语义)   ║
         ║                                                   ║
         ║  Patch_DamageWorker_AddInjury_SourceLabel         ║
         ║  Patch_DamageWorker_ExplosionDamageThing          ║
         ║  Patch_Explosion_StartExplosion                   ║
         ║  Patch_Hediff_Injury_TryMergeWith                 ║
         ║                                                   ║
         ║  → 所有伤害携带 SemanticContext:                    ║
         ║     Id / DisplayLabel / SourceKind / Instigator   ║
         ║  → 战斗日志可从伤害反查攻击来源身份                   ║
         ╚═══════════════════════════════════════════════════╝
```

---

## 三、分层架构与模块依赖关系

```
                          ┌────────────────┐
                          │    BDPMod.cs   │
                          │  (Harmony入口)  │
                          └───────┬────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    │             │             │
               ┌────▼───┐   ┌────▼───┐   ┌─────▼──────┐
               │Patches │   │Bootstrap│   │Diagnostics │
               │(依赖所有)│   │(注册器) │   │(全局日志)   │
               └────────┘   └────────┘   └────────────┘
                    │
    ┌───────────────┼───────────────────────────────┐
    │               │                               │
    │     ┌─────────▼──────────┐                    │
    │     │  CompTriggerBody   │◄── 持有 ──────────┐│
    │     │  (Trigger 真值)    │                   ││
    │     └────────┬───────────┘                   ││
    │              │                               ││
    │   ┌──────────▼──────────┐      ┌─────────────▼──┐
    │   │RuntimeCoordinator   │      │ VerbHostManager│
    │   │ 投影发布 + 同步      │      │ Formal Host绑定 │
    │   └──────────┬──────────┘      └────────┬───────┘
    │              │                          │
    │   ┌──────────▼──────────┐      ┌────────▼───────┐
    │   │ ExpressionService   │      │ BdpFormalVerb  │
    │   │ (表达系统总入口)     │      │ Binding/Host   │
    │   └──────────┬──────────┘      └────────┬───────┘
    │              │                          │
    │   ┌──────────▼──────────┐      ┌────────▼──────────┐
    │   │ExpressionSnapshot   │      │BdpVerb_Formal     │
    │   │Builder (总表构建)    │      │HostShoot / Melee  │
    │   └──────────┬──────────┘      └────────┬──────────┘
    │              │                          │
    │   ┌──────────▼──────────┐              │
    │   │SourceCollector      │              │
    │   │(来源材料收集)        │              │
    │   └──────────┬──────────┘              │
    │              │                          │
    │   ┌──────────▼──────────────────────┐  │
    │   │ Chip + Combo Contract 解释器     │  │
    │   │ 芯片契约 → 表达来源声明 → 条件评估  │  │
    │   └─────────────────────────────────┘  │
    │                                         │
    │              ┌──────────────────────────┘
    │              │
    │   ┌──────────▼──────────────┐
    │   │ AttackExecutionService  │
    │   │ (攻击执行编排器)         │
    │   │ Plan → Cast → Group →   │
    │   │ Step → Execute          │
    │   └──────────┬──────────────┘
    │              │
    │   ┌──────────▼──────────────┐
    │   │RangedAttackProtocol     │
    │   │Service (5段远程协议)     │
    │   └──────────┬──────────────┘
    │              │
    │   ┌──────────▼──────────────┐
    │   │RangedFlightProtocol     │
    │   │Service (4段飞行协议)     │
    │   └─────────────────────────┘
    │
    │         ┌──────────────────────────────────────┐
    │         │         横向支撑系统                   │
    │         │                                      │
    │         │  CombatBody ←→ Trion                 │
    │         │  (相位状态机)   (能量管理)             │
    │         │                                      │
    │         │  BodyConstraints (身体约束信号)        │
    │         │  CombatBodySession (激活/退出事务)     │
    │         │  Semantics (语义上下文)                │
    │         │  CombatLog (战斗日志表现)              │
    │         └──────────────────────────────────────┘
    │
    └── 所有跨模块通信均通过 SurfaceAccess 静态门面:
         - TrionSurfaceAccess.ResolveReader/Commands/Events(Pawn)
         - CombatBodySurfaceAccess.ResolveReader/Commands/Events(Pawn)
         - TriggerSurfaceAccess.ResolveLoadoutReader/InteractionReader/...
         - VerbHostSurfaceAccess.TryGetByResultId(Pawn, resultId, out binding)
         - AttackExecutionSurfaceAccess.ResolveEntry(Pawn)
         - ChipSurfaceAccess.ResolveDefinitionReader()
         - ComboSurfaceAccess.ResolveRuntimeIndex()
```

---

## 四、统一架构模式详解

### 4.1 Surface Access 模式 (每个子系统通用)

```
┌──────────────────────────────────────────────────────────────────┐
│              static SurfaceAccess (静态门面, 唯一入口)             │
│                                                                   │
│  ResolveReader(Pawn)     → IReader      (纯只读, 不产生副作用)     │
│  ResolveCommands(Pawn)   → ICommands    (唯一写入口)              │
│  ResolveEvents(Pawn)     → IEvents      (事件订阅口)              │
│                                                                   │
│  内部实现:                                                        │
│  ① 从 Pawn 定位 Comp (GetComp<T>)                                │
│  ② 从 Comp 获取 Service                                           │
│  ③ 返回 Service (它同时实现 Reader + Commands + Events)           │
│                                                                   │
│  实现此模式的子系统:  Trion / CombatBody / Trigger / Chip / Combo  │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 可插拔阶段协议模式 (RangedProtocol / FlightProtocol / TargetingProtocol)

```
┌──────────────────────────────────────────────────────────────────┐
│                    XxxStageService.Execute(ctx)                   │
│                                                                   │
│  组合策略:                                                        │
│    基线模块 (BaselineModules, 全局注册)                            │
│    + 会话模块 (SessionModules, per-result 挂载)                   │
│    + Addon模块 (附加模块, 透传到每个阶段)                          │
│                                                                   │
│  执行模型:                                                        │
│    for (module in baselineModules + sessionModules)               │
│        contribution = module.Execute(context, contribution)       │
│    for (addon in addonModules)                                    │
│        addon.Execute(context, contribution)                       │
│    return contribution                                            │
│                                                                   │
│  每个阶段产出: XxxContribution (记录 + 标签)                        │
│  任一步骤可返回 Aborted → 整个阶段中止, 返回已收集的部分结果         │
└──────────────────────────────────────────────────────────────────┘

已实现的阶段协议:

┌─────────────────────────────────────────────────────────────────┐
│ RangedAttackProtocol (5段, 攻击前半段)                           │
│   Entry  → Aim     → Prepare    → Fire     → ProjectileInit     │
│   (入口)   (瞄准)    (准备/Trion)  (发射)      (投射物初始化)      │
│                                                                  │
│  接口: IAimStageModule / IPrepareStageModule /                   │
│        IFireStageModule / IProjectileInitStageModule             │
│                                                                  │
│  已知模块: RangedTrionPrepareModule (Trion 消耗准备模块)           │
├─────────────────────────────────────────────────────────────────┤
│ RangedFlightProtocol (4段, 飞行后半段)                            │
│   Flight  → Arrival → Hit       → Impact                        │
│   (飞行)    (到达)    (命中判定)   (落地生效)                      │
│                                                                  │
│  接口: IFlightStageModule / IArrivalStageModule /                │
│        IHitStageModule / IImpactStageModule                      │
├─────────────────────────────────────────────────────────────────┤
│ TargetingProtocol (4段, 玩家交互层)                               │
│   Targeting → Preview  → Confirm   → ManualEntry                │
│   (瞄准选择)  (预览绘制)  (确认输入)   (手动录入)                   │
│                                                                  │
│  接口: ITargetingStageModule / IPreviewStageModule /             │
│        IConfirmStageModule / IManualEntryStageModule             │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Config → Contract 两层解析模式 (Chip / Combo / Expression)

```
┌─────────────────────┐         ┌──────────────────────────┐
│ Config Layer (配置)  │────────►│ Contract Layer (契约)     │
│                     │  解析    │                          │
│ 原始数据 (XML/memory)│  验证    │ 归一化后的一致性对象       │
│                     │  归一化  │                          │
│ ChipDefinitionConfig│         │ ChipDefinitionContract   │
│ ChipTrionConfig     │         │ ChipTrionContract        │
│ ChipLoadoutConfig   │         │ ChipLoadoutContract      │
│ ChipProfileConfig   │         │ ChipProfileContract      │
│ ChipExpressionConfig│         │ ChipExpressionContract   │
│                     │         │                          │
│ ComboDefinitionConfig│        │ ComboDefinitionContract  │
│ ComboExpressionConfig│        │ ComboTrionContract       │
│ ComboTrionConfig    │         │                          │
└─────────────────────┘         └──────────────────────────┘

流程: Reader → Config → Validator → ContractResolver → Contract
       (IChipDefinitionReader)     (DefaultChipDefinitionValidator)
                                   (DefaultChipDefinitionContractResolver)

缓存: ChipDefinitionCache 共享单例, ExpressionContractCache 解释缓存
```

---

## 五、Trigger 投影发布周期

```
┌─────────────────────────────────────────────────────────────────────┐
│                      投影发布生命周期                                 │
│                                                                      │
│  触发条件 (任一导致 MarkDirty):                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ ① LoadoutChanged      (芯片装入/卸下/销毁)                    │   │
│  │ ② DisableStateChanged (身体约束变化, 全体禁用/恢复)            │   │
│  │ ③ SwitchTransitionResolved (侧切换完成)                       │   │
│  │ ④ SlotActivationCommitted (槽位启用)                         │   │
│  │ ⑤ SlotDeactivated     (槽位停用)                             │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│                              ▼                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │               RebuildAndPublish()                             │   │
│  │                                                               │   │
│  │  ① SessionPolicy.ShouldPublishCombatProjection?               │   │
│  │     否 → 发布空投影, 结束                                      │   │
│  │                                                               │   │
│  │  ② TriggerProjectionBuildInput (快照 Trigger 内部状态)         │   │
│  │     MainSlots / SubSlots / SpecialSlots + SwitchContext       │   │
│  │                                                               │   │
│  │  ③ combatProjectionBuilder.Build()                            │   │
│  │     → ExpressionService.BuildSnapshot()                       │   │
│  │        → ExpressionSnapshotBuilder.Build()                    │   │
│  │           → SourceCollector (读芯片契约 → 来源材料)             │   │
│  │           → SingleSideExpressionBuilder (每侧独立构建)          │   │
│  │           → CompositeExpressionResolver (双持/组合技复合)       │   │
│  │           → SpecialWeaponOverride (特殊侧武器覆写)              │   │
│  │           → Assemble → ExpressionSnapshot                     │   │
│  │                                                               │   │
│  │  ④ presentationBuilder.Build()                                │   │
│  │     → 表现投影 (UI 显示用)                                     │   │
│  │                                                               │   │
│  │  ⑤ Publish() 发布新版本:                                       │   │
│  │     ├─ version++ (ProjectionVersion 递增)                     │   │
│  │     ├─ 写入 currentCombatProjection / currentPresentation     │   │
│  │     ├─ expressionService.SyncProjectedHosts()                 │   │
│  │     │   同步: Hediff / Ability / Passive 宿主状态              │   │
│  │     ├─ VerbHostManager.Refresh()                              │   │
│  │     │   同步: formal host shell 的 VerbProps / Tool / Maneuver│   │
│  │     └─ InterruptInvalidAttackSession()                        │   │
│  │         中断: 版本不匹配的进行中攻击会话                         │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 六、已实现子系统评估矩阵

```
子系统                  文件数   模块层级       完成度   评估
──────────────────────  ──────  ────────────  ──────   ──────────────────
AttackExecution          95     编排+协议+JD    ████░   核心编排完成; 双持/近战/远程均通路
Expressions              82     快照+投影+管道   ████░   快照构建/投影体系完整
Trigger                  60     真值+运行时+表面  ████░   三侧槽位/切换/投影/事件完整
CombatBody               31     状态机+快照+逃生  ████░   四相状态机/快照恢复/紧急逃生
Projectiles              37     协议+模块        ████░   四段飞行协议完成
Chips                    22     Config/Contract  ████░   配置/契约/验证链路完整
Combos                   20     Config/Contract  ███░░   Config/Contract 完成; Runtime 连接待验证
Trion                    14     服务+外部扩展     ████░   能量管理/Gizmo/扩展点齐备
Patches                  13     Harmony          ████░   接入点: 攻击/伤害/装备/渲染/日志
Verbs                     8     FormalHost+续射  ████░   FormalHost 壳/续射规划/游标控制
BodyConstraints           6     信号+语义        ████░   身体约束信号传播完善
CombatBodySession         6     事务+策略        ████░   激活/退出事务/Trion 绑定
Semantics                 6     上下文+桥        ████░   语义上下文/伤害桥/语义源枚举
VerbHosting               5     绑定+管理        ████░   Formal Binding / HostSlot 完整
Bootstrap                 5     注入+注册        ████░   Comp 注入/Gizmo 注册/Trion 注册
CombatModel               6     数据模型         ████░   执行风格/节奏/调度枚举完整
Hediffs                   4     Hediff+Comp      ████░   战斗体活性/崩解待定 Hediff
CombatLog                 4     表现记录         ███░░   记录/Snapshot 定义完成; 格式化器基础
Genes                     3     基因+桥          ████░   TrionGland 基因/Gizmo
Abilities                 3     Verb+Effect      ████░   Trion 消耗 Ability Effect

图例: ████░ = 基本完成, 可能有边缘细节待补
      ███░░ = 骨架完成, 部分功能待实现
      ██░░░ = 仅定义/接口层
```

---

## 七、Vanilla 集成点 (13个 Harmony Patch)

```
Patch                                         原版方法                          作用
──────────────────────────────────────────    ──────────────────────────────    ──────────────────────────
Patch_Pawn_TryGetAttackVerb                   Pawn.TryGetAttackVerb             自动远程入口: 注入 BDP FormalHost Verb
Patch_Pawn_MeleeVerbs_TryMeleeAttack          Pawn_MeleeVerbs.TryMeleeAttack    自动近战入口: 翻译为 BDP 正式攻击请求
Patch_Pawn_EquipmentTracker_EquipmentTrackerTick  Pawn_EquipmentTracker.Tick   装备 tick: 推进 TriggerBody 运行时
Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraint  HealthTracker.RemoveHediff  身体约束信号: Hediff 移除时通知
Patch_HediffSet_AddDirect_BodyConstraint      HediffSet.AddDirect               身体约束信号: Hediff 添加时通知
Patch_DamageWorker_AddInjury_SourceLabel      DamageWorker_AddInjury            伤害标签: 附加 BDP 来源标签
Patch_DamageWorker_ExplosionDamageThing_BdpSemantics  DamageWorker_Explosion    爆炸伤害: 注入 BDP 语义
Patch_Explosion_StartExplosion_BdpSemantics   Explosion.StartExplosion          爆炸起爆: 注入 BDP 语义
Patch_Hediff_Injury_TryMergeWith_BdpSemantics Hediff_Injury.TryMergeWith         伤害合并: 语义感知
Patch_LogEntry_ToGameStringFromPOV_BdpCombatLog  LogEntry.ToGameStringFromPOV   战斗日志: 注入 BDP 格式
Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual  PawnRenderUtility         渲染: 装备瞄准 BDP 视觉
Patch_Targeter_OrderPawnForceTarget_TargetingInput  Targeter                     瞄准: Targeting 输入桥接
Patch_Pawn_ExposeData_PostLoadAttackRecovery  Pawn.ExposeData                   读档恢复: 攻击会话恢复
```

---

## 八、架构优势与风险

### 优势

| # | 优势 | 代码证据 |
|---|------|---------|
| 1 | **严格模块边界** | 所有跨模块调用走 SurfaceAccess 静态门面, 无直接 Comp 类型引用 |
| 2 | **读写分离** | 每个子系统接口组: IReader (只读) / ICommands (只写) / IEvents (只订阅) |
| 3 | **可插拔阶段架构** | 三套协议共 13 个可插拔阶段 (5+4+4), 统一 `基线+会话+Addon` 组合 |
| 4 | **Gateway 唯一写入口** | AttackExecutionSurfaceAccess 是所有攻击请求的唯一入口 |
| 5 | **不持有业务真值** | 投影发布后冻结为 Snapshot, 执行链只读消费, 不回写 |
| 6 | **投影版本控制** | ProjectionVersion 单调递增, SessionToken 绑定版本 → 过期会话可安全中断 |
| 7 | **Vanilla 兼容** | FormalHost Verb 注册到原版 VerbTracker, 真正走原版命中/弹道/伤害 |
| 8 | **代际隔离** | JobDriver cleanup 只清自己这代会话 (AttackInstanceId + ResultId + Version 三重校验) |

### 风险与建议

| # | 风险 | 严重度 | 建议 |
|---|------|-------|------|
| 1 | Combos 运行时链路未完全验证 | 中 | 追踪 ComboRuntimeIndex → AttackExecution 的实际连接点 |
| 2 | CombatLog 格式化器仅基础实现 | 低 | 扩展 CombatLogPresentationFormatter 功能 |
| 3 | 无单元测试, 仅107个PS脚本烟雾测试 | 中 | 核心状态机 (CombatBody 相位 / Trigger 切换) 应加单元测试 |
| 4 | ExpressionContractCache 缓存失效策略未验证 | 低 | 验证芯片定义热重载时缓存是否正确清除 |
| 5 | RangeModuleResolver 模块发现机制 | 低 | 确认实际注册的模块列表是否满足设计需求 |
| 6 | 多 Trigger 持有场景 (多装备) | 低 | RuntimeTick 有 `IsCurrentPrimaryRuntimeOwner` 守卫, 但需要确认副手行为 |

---

## 九、关键设计决策追踪

以下从代码注释中提取, 反映架构演进决策:

```
┌─────────────────────────────────────────────────────────────────────┐
│ 决策                              代码证据                           │
├─────────────────────────────────────────────────────────────────────┤
│ "不使用独立流程壳"                 AttackExecutionService 标记为     │
│                                    internal sealed partial          │
│                                    Stages 文件仅包含内部编排步骤      │
├─────────────────────────────────────────────────────────────────────┤
│ "不再持有业务真值"                 TriggerRuntimeCoordinator.Publish │
│                                    注释 "这里只做纯读"               │
├─────────────────────────────────────────────────────────────────────┤
│ "FormalHost 只内部可见"            CompTriggerBody 注释              │
│                                    "只在 BDP 内部执行侧可见"          │
├─────────────────────────────────────────────────────────────────────┤
│ "不再复用宿主层主攻猜测"           TryResolveAutoPrimaryVerb 注释    │
│                                    "只读表达系统已选结果"             │
├─────────────────────────────────────────────────────────────────────┤
│ "不引入新的长期状态机"             pendingPostLoadProjectionRefresh  │
│                                    注释明确此约束                    │
├─────────────────────────────────────────────────────────────────────┤
│ "只消费快照不回写"                 PreparedContext 全链路只读传导     │
├─────────────────────────────────────────────────────────────────────┤
│ "读写分离: Gate 只负责准入"        AttackExecutionSurfaceAccess      │
│                                    "其它系统只允许从这里进入"         │
├─────────────────────────────────────────────────────────────────────┤
│ "Trigger 单一真值 owner"           CompTriggerBody 文件头注释        │
│                                    "不直接充当对外正式表面"           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 十、目录结构总览

```
Source/BDP/
├── BDP.csproj                    (.NET 4.8 / C# 7.3 Library)
├── BDPMod.cs                     (Harmony PatchAll 入口)
│
├── Core/
│   ├── Abilities/                (3 files)   能力动词 + Trion 消耗
│   ├── AttackExecution/          (95 files)  攻击编排 + 协议 + 模块 + JobDriver
│   │   ├── RangedProtocol/       (5段: Aim/Prepare/Fire/ProjectileInit + Model)
│   │   ├── RangedModules/        (模块化: Config/Runtime/Arbitration)
│   │   ├── TargetingProtocol/    (4段: Targeting/Preview/Confirm/ManualEntry)
│   │   └── Context/              (攻击统一上下文)
│   ├── BodyConstraints/          (6 files)   身体约束信号 + 语义解析
│   ├── Bootstrap/                (5 files)   启动注册: Comp注入 + Gizmo
│   ├── Chips/                    (22 files)  芯片: Config/Contract/Validation
│   ├── CombatBody/               (31 files)  战斗体: 状态机/快照/逃生/Gizmo
│   ├── CombatBodySession/        (6 files)   会话: 激活/退出事务
│   ├── CombatLog/                (4 files)   战斗日志表现
│   ├── CombatModel/              (6 files)   执行风格/节奏/调度模型
│   ├── Combos/                   (20 files)  组合技: Config/Contract/Def
│   ├── Expressions/              (82 files)  表达系统: 快照/管道/投影/运行时
│   ├── Genes/                    (3 files)   TrionGland 基因
│   ├── Hediffs/                  (4 files)   战斗体活性/崩解 Hediff
│   ├── Projectiles/              (37 files)  投射物: 飞行协议 + BdpProjectile
│   ├── Semantics/                (6 files)   语义上下文 + 伤害桥
│   ├── Trigger/                  (60 files)  Trigger: 状态/表面/运行时/切换/视觉
│   ├── Trion/                    (14 files)  Trion: 服务/表面/Gizmo/外部扩展
│   ├── VerbHosting/              (5 files)   Formal Host Verb 绑定 + 管理
│   └── Verbs/                    (8 files)   正式宿主壳 + 续射规划
│
├── Patches/                      (13 files)  Harmony 补丁
└── Support/Diagnostics/          (1 file)    全局诊断工具

共计: 486 个 .cs 源文件
```

---

> 分析完成日期: 2026-04-24
> 分析方法: 仅读取代码, 基于第一性原理分析, 不参考任何外部文档
