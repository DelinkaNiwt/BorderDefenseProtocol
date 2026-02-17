---
标题：Job系统架构与调度机制
版本号: v1.1
更新日期: 2026-02-14
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld Job系统完整源码分析，覆盖五层架构（ThinkTree决策层→JobGiver生成层→Job数据层→JobDriver执行层→Toil原子层）、6个核心类详解（Pawn_JobTracker调度器+ThinkNode决策树+JobGiver叶节点+Job数据容器+JobDriver执行器+Toil最小单元）、Humanlike ThinkTree完整优先级结构（ConstantThinkTree紧急响应+MainThinkTree日常行为+MainColonistBehaviorCore动态排序）、Job完整生命周期6步流程（DetermineNextJob→StartJob→SetupToils→DriverTick→ReadyForNextToil→EndCurrentJob）、ToilCompleteMode 5种完成模式、JobCondition枚举、ThinkNode_SubtreesByTag 4个模组插入点、模组扩展Job标准3步流程、关键源码引用表
---

# Job系统架构与调度机制

**总览**：Job系统是RimWorld中Pawn行为的核心驱动——从"决定做什么"到"怎么做"到"做完了"的完整链条。整个系统由五层架构组成：**ThinkTree**（决策层，决定做什么）→ **JobGiver**（生成层，创建具体Job）→ **Job**（数据层，纯数据容器）→ **JobDriver**（执行层，定义行为序列）→ **Toil**（原子层，最小执行单元）。Pawn_JobTracker作为调度器统一管理整个流程。

## 1. 五层架构总览

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Job系统五层架构                                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ThinkTree（决策层）                                                 │
│  ├── ThinkNode_Priority 按序遍历子节点                               │
│  ├── ThinkNode_PrioritySorter 按GetPriority()动态排序               │
│  └── ThinkNode_SubtreesByTag 模组插入点                              │
│           │                                                         │
│           ▼                                                         │
│  JobGiver（生成层）                                                  │
│  ├── ThinkNode_JobGiver.TryGiveJob() 叶节点                        │
│  ├── JobGiver_Work → WorkGiver_Scanner 日常工作                     │
│  └── JobGiver_ConfigurableHostilityResponse 敌对响应                │
│           │                                                         │
│           ▼                                                         │
│  Job（数据层）                                                       │
│  ├── def: JobDef（定义行为类型）                                     │
│  ├── targetA/B/C: LocalTargetInfo（目标）                           │
│  └── 参数: expiryInterval, playerForced, 等                         │
│           │                                                         │
│           ▼                                                         │
│  JobDriver（执行层）                                                 │
│  ├── MakeNewToils() 定义Toil序列                                    │
│  ├── DriverTick() 每tick驱动                                        │
│  └── ReadyForNextToil() 推进到下一个Toil                            │
│           │                                                         │
│           ▼                                                         │
│  Toil（原子层）                                                      │
│  ├── initAction: 初始化动作                                         │
│  ├── tickAction: 每tick动作                                         │
│  └── defaultCompleteMode: 完成条件                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. 核心类详解

### 2.1 Pawn_JobTracker — 调度器

Job系统的中枢调度器，持有当前Job和Job队列，负责Job的启动、中断、切换。

**关键字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `curJob` | `Job` | 当前正在执行的Job |
| `curDriver` | `JobDriver` | 当前Job的执行器 |
| `jobQueue` | `JobQueue` | 待执行Job队列（FIFO） |
| `startingNewJob` | `bool` | 防止StartJob递归的锁 |
| `jobsGivenThisTick` | `int` | 本tick分配的Job数（上限10，防无限循环） |
| `lastDamageCheckTick` | `int` | 上次伤害检查tick |
| `DamageCheckMinInterval` | `int`(const) | 伤害检查最小间隔 = **180 ticks**（3秒） |

**关键方法**：

| 方法 | 说明 |
|------|------|
| `JobTrackerTick()` | 每tick调用，驱动curDriver.DriverTick()，检查Job过期 |
| `DetermineNextJob()` | 遍历ThinkTree获取下一个Job（先检查jobQueue，再遍历ThinkTree） |
| `StartJob(Job, JobCondition, ThinkNode, bool, bool, ThinkTreeDef, JobTag?, bool, bool, bool?, bool, bool)` | 启动新Job：中断旧Job → 创建JobDriver → 预留资源 → SetupToils |
| `EndCurrentJob(JobCondition, bool, bool)` | 结束当前Job：清理Driver → 释放预留 → TryFindAndStartJob() |
| `TryTakeOrderedJob(Job, JobTag?, bool)` | 玩家命令入口：检查可中断 → 清除队列 → 中断当前Job → 启动新Job |
| `CheckForJobOverride()` | 检查是否有更高优先级Job应打断当前Job（遍历ConstantThinkTree + MainThinkTree） |
| `Notify_DamageTaken(DamageInfo)` | 受伤通知：根据curJob.checkOverrideOnDamage决定是否调用CheckForJobOverride |
| `SuspendCurrentJob(JobCondition, bool)` | 暂停当前Job：将curJob入队jobQueue头部（仅suspendable=true的Job） |
| `TryFindAndStartJob()` | DetermineNextJob() → StartJob()的封装 |

**JobTrackerTick核心流程**（每tick）：

```csharp
// 简化伪代码
public void JobTrackerTick()
{
    // 1. 驱动当前JobDriver
    if (curDriver != null)
        curDriver.DriverTick();

    // 2. 每30 ticks检查ConstantThinkTree（紧急响应）
    if (pawn.IsHashIntervalTick(30))
    {
        ThinkResult result = pawn.thinker.thinkNodeRoot.TryIssueJobPackage(pawn, JobIssueParams.NonConstant);
        if (result.IsValid)
            CheckForJobOverride(); // 可能打断当前Job
    }

    // 3. 检查Job过期
    if (curJob != null && curJob.expiryInterval > 0)
    {
        if (Find.TickManager.TicksGame >= curJob.startTick + curJob.expiryInterval)
        {
            if (curJob.checkOverrideOnExpire)
                CheckForJobOverride();
            else
                EndCurrentJob(JobCondition.None);
        }
    }
}
```

### 2.2 ThinkTree / ThinkNode — 决策树

ThinkTree是Pawn的AI决策系统，由ThinkNode树形结构组成。每个Pawn种族有两棵ThinkTree：**ConstantThinkTree**（每30 ticks检查，处理紧急事务）和**MainThinkTree**（当前Job结束时遍历，分配日常行为）。

**ThinkNode继承体系**：

```
ThinkNode（抽象基类）
├── ThinkNode_Priority          ← 按序遍历子节点，返回第一个有效结果
├── ThinkNode_PrioritySorter    ← 按GetPriority()动态排序后遍历
├── ThinkNode_JobGiver          ← 叶节点，TryGiveJob()生成Job
├── ThinkNode_Subtree           ← 引用另一个ThinkTreeDef
├── ThinkNode_SubtreesByTag     ← 模组插入点（按insertTag匹配）
├── ThinkNode_Tagger            ← 给子节点结果打标签（JobTag）
├── ThinkNode_QueuedJob         ← 从jobQueue取Job
├── ThinkNode_Conditional*      ← 条件节点（20+子类）
│   ├── ThinkNode_ConditionalColonist
│   ├── ThinkNode_ConditionalDowned
│   ├── ThinkNode_ConditionalMentalState
│   └── ...
└── ThinkNode_JoinVoluntarilyJoinableLord ← Lord指令
```

**ThinkNode_Priority**：按子节点顺序遍历，返回第一个`TryIssueJobPackage()`成功的结果。这是ThinkTree中最常用的节点——Humanlike MainThinkTree的根节点就是ThinkNode_Priority。

**ThinkNode_PrioritySorter**：先对所有子节点调用`GetPriority()`获取优先级值，按优先级从高到低排序后遍历。用于MainColonistBehaviorCore中需求和工作的动态排序——饥饿时GetFood优先级最高，否则Work优先级最低。

**ThinkNode_SubtreesByTag**：模组插入点。模组定义ThinkTreeDef时设置`insertTag`和`insertPriority`，运行时自动注入到匹配的插入点。Humanlike ThinkTree提供4个插入点：

| insertTag | 位置 | 优先级 |
|-----------|------|--------|
| `Humanlike_PostMentalState` | 精神状态之后 | 高 |
| `Humanlike_PostDuty` | Lord指令之后 | 中高 |
| `Humanlike_PreMain` | MainColonistBehaviorCore之前 | 中 |
| `Humanlike_PostMain` | MainColonistBehaviorCore之后 | 低 |

### 2.3 JobGiver（ThinkNode_JobGiver）— 生成层

JobGiver是ThinkTree的叶节点，负责生成具体的Job实例。继承自`ThinkNode_JobGiver`，核心方法是`TryGiveJob(Pawn)`。

**关键JobGiver子类**：

| JobGiver | 职责 | 生成的Job |
|----------|------|----------|
| `JobGiver_Orders` | 征召后的玩家命令 | Wait_Combat（默认） |
| `JobGiver_ConfigurableHostilityResponse` | 敌对响应（3种模式） | AttackMelee/AttackStatic/Flee |
| `JobGiver_ReactToCloseMeleeThreat` | 近战威胁反应 | AttackMelee |
| `JobGiver_Work` | 日常工作分配（注意：直接继承ThinkNode而非ThinkNode_JobGiver，因为需要自行管理JobTag） | 各种工作Job |
| `JobGiver_GetFood` | 进食 | Ingest |
| `JobGiver_GetRest` | 休息 | LayDown |
| `JobGiver_GetJoy` | 娱乐 | 各种娱乐Job |
| `JobGiver_FleePotentialExplosion` | 逃离爆炸 | Flee |
| `JobGiver_AIFightEnemy` | AI战斗（非玩家） | AttackMelee/AttackStatic |

**JobGiver_Work工作分配流程**：

```
JobGiver_Work.TryGiveJob(pawn)
  → 遍历pawn.workSettings中启用的WorkTypeDef（按玩家设置的优先级排序）
    → 每个WorkType有多个WorkGiver（按naturalPriority排序）
      → WorkGiver_Scanner.HasJobOnThing(pawn, thing) / HasJobOnCell(pawn, cell)
        → WorkGiver_Scanner.JobOnThing(pawn, thing) / JobOnCell(pawn, cell)
          → 返回具体Job实例
```

**WorkGiver_Scanner**是日常工作的标准扩展点——模组重写`PotentialWorkThingsGlobal()`提供候选目标，重写`JobOnThing()`生成具体Job。

### 2.4 Job — 数据容器

Job是纯数据容器，不包含任何执行逻辑。所有行为逻辑在JobDriver中实现。

**关键字段**：

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `def` | `JobDef` | — | Job类型定义 |
| `targetA` | `LocalTargetInfo` | — | 主目标（Thing或IntVec3） |
| `targetB` | `LocalTargetInfo` | — | 次目标 |
| `targetC` | `LocalTargetInfo` | — | 第三目标 |
| `count` | `int` | -1 | 数量参数 |
| `playerForced` | `bool` | false | 是否玩家强制命令 |
| `expiryInterval` | `int` | -1 | 过期间隔（ticks），-1=不过期 |
| `checkOverrideOnExpire` | `bool` | false | 过期时是否重新评估 |
| `startTick` | `int` | — | Job开始的tick |
| `locomotionUrgency` | `LocomotionUrgency` | Jog | 移动紧迫度 |
| `bill` | `Bill` | null | 关联的工作台配方 |
| `verbToUse` | `Verb` | null | 使用的Verb |
| `haulMode` | `HaulMode` | Undefined | 搬运模式 |
| `exitMapOnArrival` | `bool` | false | 到达后离开地图 |
| `canBashDoors` | `bool` | false | 可以破门 |
| `canBashFences` | `bool` | false | 可以破栅栏 |
| `canUseRangedWeapon` | `bool` | true | 可以使用远程武器 |
| `killIncappedTarget` | `bool` | false | 击杀倒地目标 |
| `ignoreForbidden` | `bool` | false | 忽略禁止标记 |
| `overrideFacing` | `Rot4` | Invalid | 强制朝向 |

**JobDef关键字段**（XML配置）：

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `driverClass` | `Type` | — | JobDriver子类 |
| `casualInterruptible` | `bool` | true | 是否可被日常事务中断 |
| `checkOverrideOnDamage` | `CheckJobOverrideOnDamageMode` | Always | 受伤时是否重新评估 |
| `playerInterruptible` | `bool` | true | 玩家是否可中断 |
| `suspendable` | `bool` | **true** | 是否可暂停入队（注意：默认true，但大多数JobDef显式设为false） |
| `allowOpportunisticPrefix` | `bool` | false | 是否允许顺路搬运前缀 |
| `collideWithPawns` | `bool` | false | 移动时是否与Pawn碰撞 |
| `alwaysShowWeapon` | `bool` | false | 始终显示武器 |
| `neverShowWeapon` | `bool` | false | 始终隐藏武器 |
| `isIdle` | `bool` | false | 是否为空闲Job |
| `reportString` | `string` | — | 状态栏显示文本 |

**CheckJobOverrideOnDamageMode枚举**：

| 值 | 说明 |
|----|------|
| `Always` | 受伤时总是重新评估（默认，大多数工作Job） |
| `Never` | 受伤时不重新评估（Steal、Kidnap等不可中断任务） |
| `OnlyIfInstigatorNotJobTarget` | 仅当攻击者不是当前Job目标时重新评估（Flee） |

### 2.5 JobDriver — 执行器

JobDriver是Job的执行引擎，通过`MakeNewToils()`定义Toil序列，`DriverTick()`每tick驱动执行。

**关键方法**：

| 方法 | 说明 |
|------|------|
| `MakeNewToils()` | 抽象方法，子类重写定义Toil序列（`yield return`） |
| `SetupToils()` | 调用MakeNewToils()构建Toil列表 + 注册FailCondition |
| `DriverTick()` | 每tick调用：执行当前Toil的tickAction → 检查完成条件 |
| `ReadyForNextToil()` | 推进到下一个Toil，如果没有更多Toil则EndJobWith(Succeeded) |
| `EndJobWith(JobCondition)` | 结束Job并通知JobTracker |
| `AddEndCondition(Func<JobCondition>)` | 添加每tick检查的结束条件 |
| `AddFailCondition(Func<bool>)` | 添加失败条件（返回true时以Incompletable结束） |

**JobDriver执行流程**：

```
StartJob() → new JobDriver() → SetupToils()
  → MakeNewToils() 构建Toil列表
  → 注册FailCondition（如目标消失、Pawn倒下等）

每tick:
  DriverTick()
    → 检查所有EndCondition
    → 执行当前Toil.tickAction
    → 检查Toil完成条件（ToilCompleteMode）
    → 完成时 → ReadyForNextToil() → 下一个Toil或结束
```

**典型JobDriver示例**（JobDriver_AttackStatic简化）：

```csharp
protected override IEnumerable<Toil> MakeNewToils()
{
    // Toil 1: 移动到射击位置
    yield return Toils_Combat.GotoCastPosition(TargetIndex.A, closeIfDowned: true);

    // Toil 2: 执行射击
    var shoot = Toils_Combat.CastVerb(TargetIndex.A);
    shoot.FailOnDespawnedOrNull(TargetIndex.A);
    shoot.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    yield return shoot;

    // Toil 3: 回到Toil 1（循环射击）
    yield return Toils_Jump.JumpIfTargetNotDead(TargetIndex.A, shoot);
}
```

### 2.6 Toil — 最小执行单元

Toil是Job执行的原子操作，每个Toil代表一个不可分割的行为步骤。

**关键字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `initAction` | `Action` | Toil开始时执行一次 |
| `tickAction` | `Action` | 每tick执行 |
| `finishActions` | `List<Action>` | Toil结束时执行（无论成功/失败） |
| `defaultCompleteMode` | `ToilCompleteMode` | 完成条件 |
| `defaultDuration` | `int` | Delay模式的持续时间（ticks） |
| `handlingFacing` | `bool` | 是否自行处理朝向 |
| `socialMode` | `RandomSocialMode` | 社交互动模式 |
| `activeSkill` | `SkillDef` | 执行期间使用的技能 |

**ToilCompleteMode枚举**（5种完成模式）：

| 模式 | 说明 | 典型用途 |
|------|------|---------|
| `Instant` | initAction执行后立即完成 | 拾取物品、切换状态 |
| `PatherArrival` | Pawn到达寻路目标时完成 | 移动到目标位置 |
| `Delay` | 经过defaultDuration ticks后完成 | 等待、制作、治疗 |
| `FinishedBusy` | 等待外部系统标记完成（如Verb射击完成） | 射击、施法 |
| `Never` | 永不自动完成，需外部调用ReadyForNextToil() | 持续行为（巡逻） |

**常用Toil工厂方法**（Toils_*静态类）：

| 工厂类 | 方法 | 说明 |
|--------|------|------|
| `Toils_Goto` | `GotoThing/GotoCell` | 移动到目标 |
| `Toils_Combat` | `GotoCastPosition/CastVerb` | 战斗相关 |
| `Toils_Haul` | `StartCarryThing/CarryHauledThingToCell` | 搬运相关 |
| `Toils_Recipe` | `MakeUnfinishedThingIfNeeded/DoRecipeWork` | 制作相关 |
| `Toils_General` | `Wait/WaitWith/DoAtomic` | 通用操作 |
| `Toils_Jump` | `JumpIf/JumpIfTargetNotDead` | 条件跳转 |
| `Toils_Interpersonal` | `GotoInteractablePosition` | 社交相关 |

## 3. Humanlike ThinkTree完整优先级结构

### 3.1 HumanlikeConstant（ConstantThinkTree）

每30 ticks检查一次，处理紧急事务。按ThinkNode_Priority顺序：

```
HumanlikeConstant (ThinkNode_Priority)
├── [1] Despawned处理
├── [2] ThinkNode_ConditionalCanDoConstantThinkTreeJobNow
│   ├── JobGiver_FleePotentialExplosion     ← 逃离爆炸
│   ├── JobGiver_FindOxygen                 ← 寻找氧气
│   ├── JobGiver_BoardOrLeaveGravship       ← 登/离重力船
│   ├── JoinAutoJoinableCaravan子树         ← 加入远行队
│   ├── JobGiver_ConfigurableHostilityResponse ← 敌对响应★
│   └── 爬行停止检查
└── [3] ThinkNode_ConditionalCanDoLordJobNow
    └── LordDutyConstant子树               ← Lord紧急指令
```

**JobGiver_ConfigurableHostilityResponse**是战斗触发的核心——根据Pawn的`HostilityResponseMode`（3种模式）生成不同Job。前置检查：`PlayerForcedJobNowOrSoon`时不触发（玩家强制命令优先），`PsychicRitual`中不触发。

| 模式 | 行为 | 生成的Job | 搜索范围 |
|------|------|----------|---------|
| `Ignore` | 忽略威胁 | 无 | — |
| `Attack` | 主动攻击最近敌人 | AttackMelee / AttackStatic（最多2次射击，2000 tick过期） | 近战8格，远程=有效射程×0.66（2~20格） |
| `Flee` | 逃离威胁 | FleeAndCower | SelfDefenseUtility.ShouldStartFleeing()判定 |

### 3.2 Humanlike（MainThinkTree）

当前Job结束时遍历，分配日常行为。按ThinkNode_Priority顺序（简化版，标注关键层级）：

```
Humanlike (ThinkNode_Priority)
├── [1]  躺下时行为（Lovin/床上娱乐/保持躺下）
├── [2]  必须保持躺下（MustKeepLyingDown）
├── [3]  倒地（Downed子树：爬行逃离/去床位/永久空闲）
├── [4]  着火（BurningResponse子树：跳水/灭火/随机跑）
├── [5]  精神崩溃-危急（Berserk/SocialFighting等）
├── [6]  逃离威胁能力（Biotech: Abilities_Escape）
├── [7]  近战威胁反应（ReactToCloseMeleeThreat）
├── [8]  精神崩溃-非危急（Wander/Binge/Tantrum等）
├── [9]  被绳牵引（RopedPawn）
├── [10] ★模组插入点: Humanlike_PostMentalState
├── [11] 队列Job（QueuedJob）
├── [12] ★征召命令（Orders）← 仅殖民者，DraftedOrder标签
│   ├── JobGiver_MoveToStandable
│   └── JobGiver_Orders → Wait_Combat
├── [13] NPC自我治疗
├── [14] Lord指令-高优先级
├── [15] ★模组插入点: Humanlike_PostDuty
├── [16] 囚犯行为（逃跑/需求/空闲）
├── [17] 殖民者紧急工作+需求
│   ├── SeekAllowedArea / SeekSafeTemperature
│   ├── DropUnusedInventory
│   ├── ★JobGiver_Work(emergency=true) ← 紧急工作
│   ├── 饥饿时进食
│   ├── 自动喂养
│   ├── Lord指令-中优先级
│   ├── 捡回武器 / 优化服装 / 库存管理
│   └── 打包食物
├── [18] Trait行为
├── [19] ★模组插入点: Humanlike_PreMain
├── [20] ★MainColonistBehaviorCore ← 核心日常行为
├── [21] ★模组插入点: Humanlike_PostMain
├── [22] 空闲殖民者（娱乐/闲逛）
├── [23] 客人/非殖民者离开地图
└── [24] 最终后备（闲逛/IdleError）
```

### 3.3 MainColonistBehaviorCore（动态排序）

MainColonistBehaviorCore使用`ThinkNode_PrioritySorter`按`GetPriority()`动态排序——每次评估时，各JobGiver根据Pawn当前状态返回不同优先级值。

```
MainColonistBehaviorCore (ThinkNode_Tagger: SatisfyingNeeds)
└── ThinkNode_PrioritySorter
    ├── JobGiver_GetFood          ← 饥饿时优先级最高(9.5)
    ├── JobGiver_GetRest          ← 疲劳时优先级高(8.0)
    ├── JobGiver_SatisfyChemicalNeed
    ├── JobGiver_TakeDrugsForDrugPolicy
    ├── JobGiver_GetAgeReversal
    ├── JobGiver_MoveDrugsToInventory
    ├── JobGiver_GetNeuralSupercharge
    ├── JobGiver_GetHemogen       ← (Biotech)
    ├── JobGiver_MeditateInBed
    ├── JobGiver_SatifyChemicalDependency ← (Biotech)
    ├── ThinkNode_Priority_Learn  ← (Biotech)
    ├── ThinkNode_Priority_GetJoy ← 娱乐
    │   ├── JobGiver_GetJoy
    │   └── JobGiver_GetJoyInBed
    ├── JobGiver_Meditate
    ├── JobGiver_Reload
    └── JobGiver_Work             ← 工作（优先级通常最低）
```

**GetPriority()动态排序详细值**（按时间段变化）：

| JobGiver | 条件 | 优先级值 |
|----------|------|---------|
| `JobGiver_GetFood` | 饥饿时 | **9.5**（最高） |
| `ThinkNode_Priority_Learn` | 儿童未满足学习需求 | **9.1** |
| `JobGiver_Work` | 工作时间段 | **9.0** |
| `JobGiver_GetRest` | 睡觉时间段 / 疲劳<0.3 | **8.0** |
| `ThinkNode_Priority_GetJoy` | 娱乐时间段且Joy<0.95 | **7.0** |
| `ThinkNode_Priority_GetJoy` | 任意时间段且Joy<0.35 | **6.0** |
| `JobGiver_Work` | 任意时间段（非工作时间） | **5.5** |
| `JobGiver_Work` | 睡觉时间段 | **3.0** |
| `JobGiver_Work` | 娱乐/冥想时间段 | **2.0** |

**排序规则**：PrioritySorter先随机打乱子节点顺序（同优先级时随机选择），再循环找最高优先级节点尝试。因此在"任意"时间段：食物(9.5) > 休息(8) > 娱乐(6, 仅Joy<0.35) > 工作(5.5)。在"工作"时间段：食物(9.5) > 工作(9) > 休息(仅极度疲劳时8)。

## 4. Job完整生命周期

### 4.1 六步流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    Job生命周期 6步流程                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ① DetermineNextJob()                                          │
│     ├── 检查jobQueue是否有待执行Job                              │
│     └── 遍历ThinkTree获取新Job                                  │
│              │                                                  │
│  ② StartJob(job, lastJobEndCondition)                          │
│     ├── 中断旧Job（如果有）                                     │
│     ├── 创建JobDriver实例（job.def.driverClass）                │
│     ├── 预留资源（Reserve targetA/B/C）                         │
│     └── 调用SetupToils()                                       │
│              │                                                  │
│  ③ SetupToils()                                                │
│     ├── 调用MakeNewToils()构建Toil列表                          │
│     ├── 注册标准FailCondition                                   │
│     └── 执行第一个Toil的initAction                              │
│              │                                                  │
│  ④ DriverTick()  ← 每tick                                     │
│     ├── 检查所有EndCondition                                    │
│     ├── 执行当前Toil.tickAction                                 │
│     └── 检查ToilCompleteMode完成条件                            │
│              │                                                  │
│  ⑤ ReadyForNextToil()                                          │
│     ├── 执行当前Toil.finishActions                              │
│     ├── 推进curToilIndex++                                      │
│     ├── 如果还有Toil → 执行下一个Toil.initAction               │
│     └── 如果没有更多Toil → EndJobWith(Succeeded)               │
│              │                                                  │
│  ⑥ EndCurrentJob(condition)                                    │
│     ├── 执行Driver.Cleanup()                                   │
│     ├── 释放所有预留（ReleaseAllReservations）                  │
│     ├── 通知系统（Notify_JobEnded）                             │
│     └── TryFindAndStartJob() → 回到步骤①                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 JobCondition枚举

Job结束时的条件标记，决定后续行为：

注意：这是`[Flags]`位标志枚举（byte类型）。

| 值 | 数值 | 说明 | 典型场景 |
|----|------|------|---------|
| `None` | 0x00 | 无特殊条件 | 正常过期 |
| `Ongoing` | 0x01 | 仍在进行 | EndCondition检查返回此值表示继续 |
| `Succeeded` | 0x02 | 成功完成 | 所有Toil执行完毕 |
| `Incompletable` | 0x04 | 无法完成 | 目标消失、路径不可达 |
| `InterruptOptional` | 0x08 | 可选中断 | CheckForJobOverride发现更高优先级Job |
| `InterruptForced` | 0x10 | 强制中断 | 玩家命令、ConstantThinkTree紧急响应 |
| `QueuedNoLongerValid` | 0x20 | 队列Job失效 | 队列中的Job目标消失 |
| `Errored` | 0x40 | 错误 | 异常情况 |
| `ErroredPather` | 0x80 | 寻路错误 | 寻路系统异常 |

### 4.3 CheckForJobOverride流程

```csharp
// 简化伪代码
public void CheckForJobOverride()
{
    // 1. 遍历ConstantThinkTree
    ThinkResult constResult = pawn.thinker.constantThinkNodeRoot
        .TryIssueJobPackage(pawn, JobIssueParams.Constant);

    if (constResult.IsValid && constResult.Job != curJob)
    {
        // ConstantThinkTree返回了不同的Job → 强制中断（InterruptForced）
        StartJob(constResult.Job, JobCondition.InterruptForced, ...);
        return;
    }

    // 2. 遍历MainThinkTree
    ThinkResult mainResult = pawn.thinker.mainThinkNodeRoot
        .TryIssueJobPackage(pawn, JobIssueParams.NonConstant);

    if (mainResult.IsValid)
    {
        // 比较新Job的ThinkNode优先级与当前Job
        if (GetPriorityOf(mainResult.SourceNode) > GetPriorityOf(curDriver.SourceNode))
        {
            // 新Job优先级更高 → 可选中断（InterruptOptional，非强制）
            StartJob(mainResult.Job, JobCondition.InterruptOptional, ...);
        }
    }
}
```

## 5. 模组扩展Job系统

### 5.1 自定义Job标准3步

**步骤1：定义JobDef（XML）**

```xml
<JobDef>
    <defName>MyMod_CustomAction</defName>
    <driverClass>MyMod.JobDriver_CustomAction</driverClass>
    <reportString>performing custom action on TargetA.</reportString>
    <casualInterruptible>false</casualInterruptible>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
</JobDef>
```

**步骤2：实现JobDriver子类（C#）**

```csharp
public class JobDriver_CustomAction : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // 失败条件
        this.FailOnDespawnedOrNull(TargetIndex.A);

        // Toil 1: 移动到目标
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        // Toil 2: 执行动作（延迟完成）
        Toil doAction = ToilMaker.MakeToil("DoAction");
        doAction.initAction = () => { /* 初始化 */ };
        doAction.tickAction = () => { /* 每tick逻辑 */ };
        doAction.defaultCompleteMode = ToilCompleteMode.Delay;
        doAction.defaultDuration = 300; // 5秒
        doAction.WithProgressBarToilDelay(TargetIndex.A);
        yield return doAction;

        // Toil 3: 完成动作
        yield return Toils_General.DoAtomic(() => { /* 完成逻辑 */ });
    }
}
```

**步骤3：触发Job（Gizmo或JobGiver）**

```csharp
// 方式A: 通过Gizmo按钮触发
public override IEnumerable<Gizmo> GetGizmos()
{
    yield return new Command_Action
    {
        defaultLabel = "Custom Action",
        action = () =>
        {
            Job job = JobMaker.MakeJob(MyModDefOf.MyMod_CustomAction, target);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    };
}

// 方式B: 通过自定义JobGiver（ThinkTree自动分配）
public class JobGiver_CustomAction : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Thing target = FindTarget(pawn);
        if (target == null) return null;
        return JobMaker.MakeJob(MyModDefOf.MyMod_CustomAction, target);
    }
}
```

### 5.2 ThinkTree插入方式

模组通过`ThinkNode_SubtreesByTag`插入自定义行为到ThinkTree：

```xml
<ThinkTreeDef>
    <defName>MyMod_CustomBehavior</defName>
    <insertTag>Humanlike_PreMain</insertTag>
    <insertPriority>100</insertPriority>
    <thinkRoot Class="MyMod.JobGiver_CustomBehavior" />
</ThinkTreeDef>
```

4个插入点的推荐用途：

| 插入点 | 推荐用途 |
|--------|---------|
| `Humanlike_PostMentalState` | 紧急行为（优先级高于征召命令） |
| `Humanlike_PostDuty` | 中高优先级行为（在Lord指令之后） |
| `Humanlike_PreMain` | 日常行为扩展（在MainColonistBehaviorCore之前） |
| `Humanlike_PostMain` | 低优先级后备行为 |

### 5.3 WorkGiver_Scanner扩展日常工作

```csharp
public class WorkGiver_Scanner_CustomWork : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest
        => ThingRequest.ForDef(MyModDefOf.MyMod_CustomWorkbench);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return !t.IsForbidden(pawn) && pawn.CanReserve(t);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(MyModDefOf.MyMod_CustomWork, t);
    }
}
```

## 6. 关键源码引用表

| 文件 | 命名空间 | 核心内容 |
|------|---------|---------|
| `Pawn_JobTracker.cs` | `Verse.AI` | Job调度核心：DetermineNextJob/StartJob/EndCurrentJob/CheckForJobOverride/TryTakeOrderedJob/Notify_DamageTaken/SuspendCurrentJob |
| `Job.cs` | `Verse.AI` | Job数据容器：targetA/B/C + 参数字段 |
| `JobDef.cs` | `Verse` | JobDef定义：driverClass/casualInterruptible/checkOverrideOnDamage等 |
| `JobDriver.cs` | `Verse.AI` | JobDriver基类：MakeNewToils/DriverTick/ReadyForNextToil/SetupToils |
| `Toil.cs` | `Verse.AI` | Toil定义：initAction/tickAction/defaultCompleteMode |
| `ThinkNode.cs` | `Verse.AI` | ThinkNode基类 |
| `ThinkNode_Priority.cs` | `Verse.AI` | 按序遍历子节点 |
| `ThinkNode_PrioritySorter.cs` | `Verse.AI` | 按GetPriority()动态排序 |
| `ThinkNode_JobGiver.cs` | `Verse.AI` | JobGiver基类：TryGiveJob() |
| `ThinkNode_SubtreesByTag.cs` | `Verse.AI` | 模组插入点 |
| `JobGiver_Work.cs` | `RimWorld` | 工作分配器 |
| `JobGiver_ConfigurableHostilityResponse.cs` | `RimWorld` | 敌对响应 |
| `JobGiver_Orders.cs` | `RimWorld` | 征召命令 |
| `WorkGiver_Scanner.cs` | `RimWorld` | 工作扫描器基类 |
| `Humanlike.xml` | Core/Defs/ThinkTreeDefs | Humanlike + HumanlikeConstant ThinkTree |
| `SubTrees_Misc.xml` | Core/Defs/ThinkTreeDefs | MainColonistBehaviorCore子树 |
| `Jobs_Misc.xml` | Core/Defs/JobDefs | 通用JobDef（Wait_Combat/AttackStatic/AttackMelee/Flee等） |
| `Jobs_Work.xml` | Core/Defs/JobDefs | 工作JobDef（HaulToCell/DoBill/Mine等） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-14 | 初始版本：五层架构总览+6核心类详解（Pawn_JobTracker/ThinkNode/JobGiver/Job/JobDriver/Toil）+Humanlike ThinkTree完整优先级结构（ConstantThinkTree+MainThinkTree+MainColonistBehaviorCore动态排序）+Job生命周期6步流程+JobCondition枚举+CheckForJobOverride流程+模组扩展3步标准流程+ThinkTree 4个插入点+WorkGiver_Scanner扩展+关键源码引用表 | Claude Opus 4.6 |
| v1.1 | 2026-02-14 | 修正：suspendable默认值为true（非false）；CheckForJobOverride中MainThinkTree使用InterruptOptional（非InterruptForced），ConstantThinkTree使用InterruptForced | Claude Opus 4.6 |
