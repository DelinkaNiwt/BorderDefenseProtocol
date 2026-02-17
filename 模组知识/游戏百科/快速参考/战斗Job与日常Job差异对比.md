---
标题：战斗Job与日常Job差异对比
版本号: v1.1
更新日期: 2026-02-14
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld战斗Job与日常Job的系统级差异分析，覆盖8维度对比表（触发方式/ThinkTree位置/典型JobDef/casualInterruptible/checkOverrideOnDamage/expiryInterval/suspendable/allowOpportunisticPrefix）、战斗打断日常工作3条路径（ConstantThinkTree敌对响应+受伤触发CheckForJobOverride+玩家征召清除队列）、日常工作恢复机制（suspendable入队+ThinkTree重新分配）、expiryInterval战斗目标重评估机制、TryTakeOrderedJob玩家命令完整流程、模组自定义战斗/工作Job关键字段模板、关键源码引用表
---

# 战斗Job与日常Job差异对比

**总览**：RimWorld中的Job按功能分为两大类——**战斗Job**（由玩家命令或AI敌对响应触发，高优先级，不可随意中断）和**日常Job**（由ThinkTree自动分配，低优先级，可被战斗打断）。两者在触发方式、中断规则、过期机制等8个维度存在系统级差异。理解这些差异是模组设计自定义行为的基础。

## 1. 核心差异总览表

| 维度 | 战斗Job | 日常Job |
|------|---------|---------|
| **触发方式** | 玩家命令（TryTakeOrderedJob）/ AI敌对响应（ConstantThinkTree） | ThinkTree自动分配（MainColonistBehaviorCore） |
| **ThinkTree位置** | Orders节点（高优先级）/ ConstantThinkTree | MainColonistBehaviorCore（低优先级） |
| **典型JobDef** | AttackStatic, AttackMelee, Wait_Combat, Flee | HaulToCell, DoBill, Mine, Ingest |
| **casualInterruptible** | `false`（不可被日常事务中断） | 默认`true`（可被更高优先级事务中断） |
| **checkOverrideOnDamage** | 默认`Always`（但战斗Job通常不需要，因为已在战斗中） | 默认`Always`（受伤时重新评估） |
| **expiryInterval** | 有（定期重新评估目标，如450-550 ticks） | 无（-1，完成即结束） |
| **suspendable** | `false`（显式设置） | `false`（显式设置；默认值实为`true`，但原版几乎所有JobDef都显式设为false） |
| **allowOpportunisticPrefix** | `false`（战斗不允许顺路搬运） | `true`（允许顺路搬运） |

**补充字段差异**：

| 字段 | 战斗Job | 日常Job |
|------|---------|---------|
| `alwaysShowWeapon` | `true`（显示武器） | 默认`false` |
| `collideWithPawns` | `true`（与Pawn碰撞） | 默认`false` |
| `isIdle` | Wait_Combat=`true` | Wait=`true`，工作Job=`false` |
| `neverShowWeapon` | `false` | 部分`true`（LayDown等） |

## 2. 战斗Job的XML定义实例

### 2.1 Wait_Combat（征召后默认）

```xml
<JobDef>
    <defName>Wait_Combat</defName>
    <driverClass>JobDriver_Wait</driverClass>
    <reportString>watching for targets.</reportString>
    <alwaysShowWeapon>true</alwaysShowWeapon>
    <suspendable>false</suspendable>
    <isIdle>true</isIdle>
    <!-- 注意：casualInterruptible默认true，但征召状态下Orders节点优先级高 -->
</JobDef>
```

### 2.2 AttackStatic（远程攻击）

```xml
<JobDef>
    <defName>AttackStatic</defName>
    <driverClass>JobDriver_AttackStatic</driverClass>
    <reportString>attacking TargetA.</reportString>
    <alwaysShowWeapon>true</alwaysShowWeapon>
    <casualInterruptible>false</casualInterruptible>  <!-- ★不可随意中断 -->
    <collideWithPawns>true</collideWithPawns>          <!-- ★与Pawn碰撞 -->
</JobDef>
```

### 2.3 AttackMelee（近战攻击）

```xml
<JobDef>
    <defName>AttackMelee</defName>
    <driverClass>JobDriver_AttackMelee</driverClass>
    <reportString>melee attacking TargetA.</reportString>
    <alwaysShowWeapon>true</alwaysShowWeapon>
    <casualInterruptible>false</casualInterruptible>  <!-- ★不可随意中断 -->
    <collideWithPawns>true</collideWithPawns>          <!-- ★与Pawn碰撞 -->
</JobDef>
```

### 2.4 Flee（逃跑）

```xml
<JobDef>
    <defName>Flee</defName>
    <driverClass>JobDriver_Flee</driverClass>
    <reportString>fleeing.</reportString>
    <checkOverrideOnDamage>OnlyIfInstigatorNotJobTarget</checkOverrideOnDamage>
    <!-- ★特殊：仅当攻击者不是逃跑目标时才重新评估 -->
    <isIdle>true</isIdle>
</JobDef>
```

## 3. 日常Job的XML定义实例

### 3.1 HaulToCell（搬运）

```xml
<JobDef>
    <defName>HaulToCell</defName>
    <driverClass>JobDriver_HaulToCell</driverClass>
    <reportString>hauling TargetA.</reportString>
    <suspendable>false</suspendable>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>  <!-- ★允许顺路搬运 -->
    <!-- casualInterruptible默认true -->
    <!-- checkOverrideOnDamage默认Always -->
</JobDef>
```

### 3.2 DoBill（工作台制作）

```xml
<JobDef>
    <defName>DoBill</defName>
    <driverClass>JobDriver_DoBill</driverClass>
    <reportString>doing bill at TargetA.</reportString>
    <suspendable>false</suspendable>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>  <!-- ★允许顺路搬运 -->
</JobDef>
```

### 3.3 Mine（采矿）

```xml
<JobDef>
    <defName>Mine</defName>
    <driverClass>JobDriver_Mine</driverClass>
    <reportString>digging at TargetA.</reportString>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>  <!-- ★允许顺路搬运 -->
</JobDef>
```

## 4. 战斗如何打断日常工作（3条路径）

### 4.1 路径1：ConstantThinkTree敌对响应

```
每30 ticks → Pawn_JobTracker.JobTrackerTick()
  → pawn.thinker.constantThinkNodeRoot.TryIssueJobPackage()
    → HumanlikeConstant ThinkTree遍历
      → ThinkNode_ConditionalCanDoConstantThinkTreeJobNow
        → ★征召状态(Drafted)的Pawn跳过此节点，不触发自动敌对响应
      → JobGiver_ConfigurableHostilityResponse.TryGiveJob()
        → 检测到敌人 → 生成AttackMelee/AttackStatic/Flee
          → CheckForJobOverride()
            → 新Job优先级 > 当前工作Job优先级
              → StartJob(combatJob, InterruptForced)
                → 当前工作Job被强制中断
```

**关键源码**（Pawn_JobTracker.JobTrackerTick简化）：

```csharp
// 每30 ticks检查ConstantThinkTree
if (pawn.IsHashIntervalTick(30))
{
    // ConstantThinkTree返回的Job会触发CheckForJobOverride
    // 如果返回了有效的战斗Job，会InterruptForced打断当前工作
}
```

### 4.2 路径2：受伤触发重新评估

```
Pawn受到伤害
  → Pawn_JobTracker.Notify_DamageTaken(dinfo)
    → 冷却检查：距上次检查 < 180 ticks → 跳过
    → playerForced的Job → 跳过（玩家强制命令不被伤害打断）
    → 检查curJob.def.checkOverrideOnDamage
      → Always（大多数工作Job的默认值）
        → CheckForJobOverride()
          → ConstantThinkTree检测到敌人
            → 生成战斗Job → InterruptForced打断当前工作
          → 或MainThinkTree发现更高优先级Job
            → InterruptOptional打断当前工作（非强制）
```

**关键源码**（Pawn_JobTracker.Notify_DamageTaken简化）：

```csharp
public void Notify_DamageTaken(DamageInfo dinfo)
{
    if (curJob == null) return;

    // 冷却检查：DamageCheckMinInterval = 180 ticks（3秒）
    // 防止频繁受伤导致过多的CheckForJobOverride调用
    if (Find.TickManager.TicksGame - lastDamageCheckTick < DamageCheckMinInterval)
        return;

    // playerForced的Job不会被伤害触发重新评估
    if (curJob.playerForced) return;

    lastDamageCheckTick = Find.TickManager.TicksGame;

    switch (curJob.def.checkOverrideOnDamage)
    {
        case CheckJobOverrideOnDamageMode.Always:
            CheckForJobOverride();  // 大多数工作Job走这条路
            break;
        case CheckJobOverrideOnDamageMode.Never:
            break;  // Steal/Kidnap等不可中断任务
        case CheckJobOverrideOnDamageMode.OnlyIfInstigatorNotJobTarget:
            if (dinfo.Instigator != curJob.targetA.Thing)
                CheckForJobOverride();  // Flee：仅当攻击者不是逃跑目标时
            break;
    }
}
```

### 4.3 路径3：玩家征召

```
玩家点击征召按钮
  → Pawn_DraftController.Drafted setter
    → 清除工作队列（jobQueue.Clear）
    → 唤醒休眠Pawn
    → 离开Lord
    → EndCurrentJob(InterruptForced)
      → TryFindAndStartJob()
        → Humanlike ThinkTree遍历
          → Orders节点（高优先级，仅征召时激活）
            → JobGiver_Orders.TryGiveJob()
              → 返回Wait_Combat
```

**关键源码**（Pawn_DraftController.Drafted setter简化）：

```csharp
public bool Drafted
{
    set
    {
        if (value == draftedInt) return;
        draftedInt = value;

        if (value) // 征召
        {
            pawn.jobs.ClearQueuedJobs();           // 清除工作队列
            pawn.pather?.StopDead();                // 停止移动
            if (pawn.CurJob?.def.isIdle == false)
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
        else // 解除征召
        {
            pawn.equipment?.GetGizmos();            // 刷新Gizmo
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }
}
```

## 5. 日常工作如何恢复

### 5.1 suspendable机制

`suspendable`的**默认值为`true`**（JobDef.cs中定义）。当高优先级Job需要打断当前Job时，如果当前Job的`suspendable=true`，则当前Job会被暂停并入队`jobQueue`头部。高优先级Job结束后，从队列恢复暂停的Job。

**但实际上**：原版几乎所有JobDef都**显式设置`suspendable=false`**（Wait_Combat、HaulToCell、DoBill、Mine等）。被打断后不恢复，而是重新从ThinkTree分配新的工作Job。

**原因**：工作Job通常涉及资源预留（Reserve），暂停后资源状态可能已变化（被其他Pawn使用），恢复时可能导致冲突。因此原版设计选择"重新分配"而非"恢复"。

### 5.2 ThinkTree重新分配

```
战斗Job结束（敌人消灭/逃离/解除征召）
  → EndCurrentJob(Succeeded/InterruptForced)
    → TryFindAndStartJob()
      → DetermineNextJob()
        → 检查jobQueue（通常为空，因为工作Job不入队）
        → 遍历MainThinkTree
          → MainColonistBehaviorCore
            → ThinkNode_PrioritySorter按当前Need排序
              → 分配新的工作Job（可能与之前不同）
```

## 6. expiryInterval机制（战斗Job特有）

战斗Job通过`expiryInterval`定期重新评估目标——确保Pawn不会一直攻击已经不是最优目标的敌人。

### 6.1 机制说明

| 字段 | 说明 | 典型值 |
|------|------|--------|
| `expiryInterval` | Job过期间隔（ticks） | 战斗Job通常450-550（随机化避免同步） |
| `checkOverrideOnExpire` | 过期时是否调用CheckForJobOverride | `true`（战斗Job） |
| `expireRequiresEnemiesNearby` | 仅附近有敌人时才过期 | `true`（部分战斗Job） |

### 6.2 流程

```
战斗Job启动 → startTick记录
  → 每tick: JobTrackerTick()检查
    → TicksGame >= startTick + expiryInterval?
      → 是 → checkOverrideOnExpire?
        → true → CheckForJobOverride()
          → 重新评估：可能切换目标、切换攻击方式、或继续当前Job
        → false → EndCurrentJob(None)
          → TryFindAndStartJob() → 分配新Job
```

### 6.3 AI战斗中的expiryInterval使用

AI战斗Job（由`JobGiver_AIFightEnemy`生成）通常在Job创建时设置随机化的expiryInterval：

```csharp
// JobGiver_AIFightEnemy简化
protected override Job TryGiveJob(Pawn pawn)
{
    Thing target = AttackTargetFinder.BestAttackTarget(pawn, ...);
    if (target == null) return null;

    Job job = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
    job.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange; // 约450-550
    job.checkOverrideOnExpire = true;
    job.expireRequiresEnemiesNearby = true;
    return job;
}
```

## 7. TryTakeOrderedJob完整流程（玩家命令）

当玩家右键下达命令时，通过`TryTakeOrderedJob`将Job强制分配给Pawn。

### 7.1 流程

```
玩家右键目标 → FloatMenuMakerMap生成选项
  → 选择选项 → 创建Job实例
    → pawn.jobs.TryTakeOrderedJob(job, tag, requestQueueing)
      → ① 检查IsCurrentJobPlayerInterruptible
      │   └── curJob.def.playerInterruptible == false? → 拒绝
      → ② 设置playerForced = true
      → ③ requestQueueing?
      │   ├── true → 入队jobQueue尾部（Shift+右键）
      │   └── false → 继续
      → ④ ClearQueuedJobs() 清除队列
      → ⑤ EndCurrentJob(InterruptForced) 中断当前Job
      → ⑥ StartJob(job) 启动新Job
```

### 7.2 关键源码

```csharp
// Pawn_JobTracker.TryTakeOrderedJob 简化
public bool TryTakeOrderedJob(Job job, JobTag? tag = null, bool requestQueueing = false)
{
    // 检查是否可中断
    if (curJob != null && !curJob.def.playerInterruptible)
        return false;

    job.playerForced = true;

    if (requestQueueing)
    {
        // Shift+右键：入队
        jobQueue.EnqueueLast(new QueuedJob(job, tag));
        return true;
    }

    // 正常命令：清除队列 + 中断当前 + 启动新Job
    ClearQueuedJobs();
    if (curJob != null)
        EndCurrentJob(JobCondition.InterruptForced);
    StartJob(job, lastJobEndCondition: JobCondition.InterruptForced);
    return true;
}
```

### 7.3 playerForced标记的影响

| 影响 | 说明 |
|------|------|
| 忽略Forbid | playerForced的Job忽略物品的Forbid标记 |
| 忽略区域限制 | 可以离开允许区域 |
| 优先级提升 | 在某些判定中获得更高优先级 |
| UI提示 | 状态栏显示"(forced)"标记 |

## 8. ConstantThinkTree vs MainThinkTree对比

| 维度 | ConstantThinkTree | MainThinkTree |
|------|-------------------|---------------|
| **检查频率** | 每30 ticks | 仅当前Job结束时 |
| **触发方式** | JobTrackerTick()中主动检查 | TryFindAndStartJob()中遍历 |
| **职责** | 紧急响应（逃爆炸、敌对响应、Lord紧急指令） | 日常行为分配（需求、工作、空闲） |
| **可打断当前Job** | 是（通过CheckForJobOverride） | 否（仅在当前Job结束后） |
| **Humanlike定义** | HumanlikeConstant | Humanlike |
| **典型JobGiver** | FleePotentialExplosion, ConfigurableHostilityResponse | GetFood, GetRest, Work |

## 9. 模组自定义Job关键字段模板

### 9.1 自定义战斗Job

```xml
<JobDef>
    <defName>MyMod_CombatAction</defName>
    <driverClass>MyMod.JobDriver_CombatAction</driverClass>
    <reportString>performing combat action on TargetA.</reportString>
    <!-- ★战斗Job关键字段 -->
    <casualInterruptible>false</casualInterruptible>
    <alwaysShowWeapon>true</alwaysShowWeapon>
    <collideWithPawns>true</collideWithPawns>
    <!-- 可选 -->
    <suspendable>false</suspendable>
</JobDef>
```

**触发方式推荐**：Gizmo按钮 → `TryTakeOrderedJob` → 自定义JobDriver，而非修改ThinkTree。

### 9.2 自定义工作Job

```xml
<JobDef>
    <defName>MyMod_WorkAction</defName>
    <driverClass>MyMod.JobDriver_WorkAction</driverClass>
    <reportString>working on TargetA.</reportString>
    <!-- ★工作Job关键字段 -->
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
    <!-- checkOverrideOnDamage默认Always，受伤时可被战斗打断 -->
    <suspendable>false</suspendable>
</JobDef>
```

**触发方式推荐**：自定义WorkGiver_Scanner + WorkTypeDef，通过ThinkTree自动分配。

### 9.3 完整JobDriver骨架（战斗Job）

```csharp
public class JobDriver_CombatAction : JobDriver
{
    private const int ActionDuration = 120; // 2秒

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        // 战斗Job通常不需要预留目标（目标可能移动/死亡）
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // 失败条件
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnDowned(TargetIndex.A);

        // Toil 1: 移动到攻击位置
        yield return Toils_Combat.GotoCastPosition(TargetIndex.A);

        // Toil 2: 执行攻击动作
        Toil attack = ToilMaker.MakeToil("Attack");
        attack.initAction = () =>
        {
            // 初始化攻击逻辑
        };
        attack.tickAction = () =>
        {
            // 每tick攻击逻辑
        };
        attack.defaultCompleteMode = ToilCompleteMode.Delay;
        attack.defaultDuration = ActionDuration;
        yield return attack;

        // Toil 3: 循环（如果目标还活着）
        yield return Toils_Jump.JumpIfTargetNotDead(TargetIndex.A, attack);
    }
}
```

## 10. 关键源码引用表

| 文件 | 命名空间 | 核心内容 |
|------|---------|---------|
| `Pawn_JobTracker.cs` | `Verse.AI` | TryTakeOrderedJob/CheckForJobOverride/Notify_DamageTaken/SuspendCurrentJob |
| `Pawn_DraftController.cs` | `RimWorld` | Drafted setter（征召/解除征召副作用链） |
| `JobDef.cs` | `Verse` | casualInterruptible/checkOverrideOnDamage/suspendable/allowOpportunisticPrefix等字段 |
| `Job.cs` | `Verse.AI` | expiryInterval/checkOverrideOnExpire/playerForced/expireRequiresEnemiesNearby |
| `JobGiver_ConfigurableHostilityResponse.cs` | `RimWorld` | 敌对响应3种模式（Ignore/Attack/Flee） |
| `JobGiver_AIFightEnemy.cs` | `RimWorld` | AI战斗Job生成（expiryInterval设置） |
| `JobGiver_Orders.cs` | `RimWorld` | 征召后默认Wait_Combat |
| `JobGiver_Work.cs` | `RimWorld` | 工作分配器 |
| `JobDriver_AttackStatic.cs` | `Verse.AI` | 远程攻击Driver |
| `JobDriver_AttackMelee.cs` | `Verse.AI` | 近战攻击Driver |
| `JobDriver_Wait.cs` | `Verse.AI` | Wait_Combat使用的Driver |
| `Jobs_Misc.xml` | Core/Defs/JobDefs | Wait_Combat/AttackStatic/AttackMelee/Flee定义 |
| `Jobs_Work.xml` | Core/Defs/JobDefs | HaulToCell/DoBill/Mine定义 |
| `Humanlike.xml` | Core/Defs/ThinkTreeDefs | Orders节点位置、MainColonistBehaviorCore引用 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-14 | 初始版本：8维度核心差异对比表+战斗/日常Job XML定义实例+战斗打断日常工作3条路径源码分析（ConstantThinkTree敌对响应+受伤触发CheckForJobOverride+玩家征召清除队列）+日常工作恢复机制（suspendable+ThinkTree重新分配）+expiryInterval战斗目标重评估机制+TryTakeOrderedJob玩家命令完整流程+ConstantThinkTree vs MainThinkTree对比+模组自定义战斗/工作Job关键字段模板+完整JobDriver骨架+关键源码引用表 | Claude Opus 4.6 |
| v1.1 | 2026-02-14 | 修正：suspendable默认值为true（非false），原版JobDef显式设为false；路径1补充征召Pawn跳过ConstantThinkTree自动敌对响应（ConditionalCanDoConstantThinkTreeJobNow）；路径2补充180 tick冷却+playerForced豁免+InterruptOptional vs InterruptForced区别；suspendable机制说明补充默认值 | Claude Opus 4.6 |