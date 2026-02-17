---
标题：Pawn形态与状态切换先例
版本号: v1.1
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld中5种Pawn形态/状态切换机制的完整源码分析，按变化深度排列：变异体转化（MutantDef+Pawn_MutantTracker，Turn() 20+副作用链+Revert()恢复局限+3个MutantDef实例对比）、非人化（Hediff_Inhumanized，行为改变+完全可逆Rehumanize）、死眠（Gene_Deathrest+Hediff_Deathrest+Need_Deathrest三层架构，主动/被动触发+自动恢复）、机械体自动关机（Need_MechEnergy，能量归零→SelfShutdown Hediff+Job→能量≥15自动解除）、机械体停用（CompMechanoid.Deactivate()/WakeUp()），含5种先例变化维度矩阵、源码引用表
---

# Pawn形态与状态切换先例

**总览**：RimWorld有**5种Pawn形态/状态切换机制**，按"变化深度"从深到浅排列。变异体转化是最全面的形态改变（行为/外观/需求/战斗/社交全维度），非人化是Hediff驱动的行为改变，死眠和机械体关机/停用是不同实现的休眠状态。每种机制的实现路径不同，但都遵循一个共同模式：**状态标记 + 系统通知 + 副作用链**。

```
┌─────────────────────────────────────────────────────────────────────┐
│              5种Pawn形态/状态切换先例（按变化深度排列）                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  第1种：变异体转化（Anomaly）★★★ 全面改变                           │
│    └── MutantDef + Pawn_MutantTracker.Turn()/Revert()              │
│        改变：行为/外观/需求/战斗/社交/装备/派系/基因 全维度          │
│                                                                     │
│  第2种：非人化（Anomaly）★★☆ 行为改变                               │
│    └── Hediff_Inhumanized + AnomalyUtility                         │
│        改变：社交/聚会/思想/道德判定，外观和能力不变                 │
│                                                                     │
│  第3种：死眠（Biotech）★★☆ 休眠状态                                 │
│    └── Gene_Deathrest + Need_Deathrest + Hediff_Deathrest          │
│        改变：进入休眠Job，周期性维护，可绑定建筑获得增益             │
│                                                                     │
│  第4种：机械体自动关机（Biotech）★☆☆ 休眠状态                       │
│    └── Need_MechEnergy + SelfShutdown Hediff + Job                 │
│        改变：能量归零→躺下→缓慢恢复→能量≥15自动解除                │
│                                                                     │
│  第5种：机械体停用（Biotech）★☆☆ 休眠状态                           │
│    └── CompMechanoid.Deactivate()/WakeUp()                         │
│        改变：手动停用→休眠→手动唤醒（停用期间不可自动恢复）         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**变化维度分析框架**：

| 维度 | 变异体转化 | 非人化 | 死眠 | 机械体关机 | 机械体停用 |
|------|-----------|--------|------|-----------|-----------|
| **行为/AI** | ✅ 替换ThinkTree | ✅ 禁止社交/聚会 | ✅ 进入Deathrest Job | ✅ 进入SelfShutdown Job | ✅ 进入Sleep |
| **外观** | ✅ 皮肤/头型/体型/动画 | ❌ | ❌ | ❌ | ❌ |
| **需求** | ✅ 禁用(可白名单) | ❌ | ❌ | ✅ 能量停止消耗 | ❌ |
| **战斗** | ✅ 自定义tools/verbs | ❌ | ❌ | ✅ 护盾禁用 | ✅ 护盾禁用 |
| **装备** | ✅ 掉落/销毁 | ❌ | ❌ | ❌ | ❌ |
| **派系** | ✅ 可改变 | ❌ | ❌ | ❌ | ❌ |
| **基因** | ✅ 重置/禁用 | ❌ | ❌ | ❌ | ❌ |
| **社交** | ✅ 禁用 | ✅ 禁用部分 | ❌ | ❌ | ❌ |
| **可逆性** | 部分（Revert不完全） | 完全 | 完全（自动） | 完全（自动） | 完全（手动） |
| **实现复杂度** | 极高（专用Tracker） | 低（单Hediff） | 高（三层架构） | 中（Need+Hediff+Job） | 低（Comp） |

## 1. ★ 变异体转化（Mutant Transformation）

### 1.1 MutantDef核心字段分类

`MutantDef`有100+字段，按功能分5大维度：

**行为维度**：

| 字段 | 类型 | 说明 | Shambler | Ghoul |
|------|------|------|----------|-------|
| `thinkTree` | ThinkTreeDef | 替换AI行为树 | Shambler | Ghoul |
| `thinkTreeConstant` | ThinkTreeDef | 常驻行为树 | ShamblerConstant | GhoulConstant |
| `workDisables` | WorkTags | 禁用工作类型 | AllWork+Shooting | AllWork+Shooting |
| `canBeDrafted` | bool | 能否被征召 | false | true(Ghoul可征召) |
| `canOpenDoors` | bool | 能否开门 | false | true(默认) |
| `passive` | bool | 是否被动（不主动攻击） | false | false |
| `canAttackWhileCrawling` | bool | 倒地后能否爬行攻击 | true | false |
| `respectsAllowedArea` | bool | 是否遵守区域限制 | false | true(默认) |

**外观维度**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `useCorpseGraphics` | bool | 使用尸体贴图（Shambler） |
| `skinColorOverride` | Color? | 覆盖皮肤颜色（Ghoul: 灰绿色） |
| `hairColorOverride` | Color? | 覆盖头发颜色 |
| `bodyTypeGraphicPaths` | List | 自定义体型贴图路径 |
| `forcedHeadTypes` | List | 强制头型（Ghoul: 4种专用头型） |
| `hairTagFilter` / `beardTagFilter` | TagFilter | 发型/胡须过滤 |
| `renderNodeProperties` | List | 自定义渲染节点（伤口覆盖层等） |
| `standingAnimation` | AnimationDef | 站立动画（Shambler: 摇晃） |
| `woundColor` | Color? | 伤口颜色 |

**需求维度**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `disableNeeds` | bool | 禁用所有Need |
| `needWhitelist` | List | Need白名单（Ghoul: 仅Food） |
| `overrideFoodType` | bool | 覆盖食物类型 |
| `foodType` | FoodTypeFlags | 食物类型（Ghoul: 仅肉食） |
| `allowEatingCorpses` | bool | 允许吃尸体 |
| `canUseDrugs` | bool | 能否使用药物 |
| `drugWhitelist` | List | 药物白名单 |

**战斗维度**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `tools` | List | 自定义近战工具（替换原版） |
| `verbs` | List | 自定义Verb |
| `abilities` | List | 赋予能力 |
| `abilityWhitelist` | List | 能力白名单 |
| `deathOnDownedChance` | float | 倒地即死概率（Shambler/Ghoul: 0.25） |

**社交/身份维度**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `consideredSubhuman` | bool | 视为亚人类 |
| `disablesIdeo` | bool | 禁用意识形态 |
| `incapableOfSocialInteractions` | bool | 禁用社交 |
| `clearsEgo` | bool | 清除自我意识 |
| `namePrefix` | string | 名称前缀（"shambler "） |
| `overrideLabel` | bool | 覆盖标签显示 |
| `defaultFaction` | FactionDef | 默认派系（Entities） |

### 1.2 Turn()完整副作用链

**位置**：`RimWorld.Pawn_MutantTracker.Turn()` L150

```csharp
public void Turn(bool clearLord = false)
{
    if (hasTurned) { Log.Error(...); return; }
    hasTurned = true;
    // ... 20+步骤副作用链
}
```

**完整副作用链（按执行顺序）**：

```
Turn(clearLord)
  │
  ├── 1. hasTurned = true                          ← 状态标记
  ├── 2. 添加mutant Hediff（def.hediff）            ← 核心Hediff
  ├── 3. 添加givesHediffs列表                       ← 附加Hediff（如Regeneration）
  ├── 4. 终止妊娠（如果terminatePregnancy）          ← 特殊处理
  ├── 5. 初始化Pawn_CallTracker                     ← 声音系统
  ├── 6. 刷新Need列表                               ← 禁用/启用Need
  ├── 7. 通知工作类型变更                            ← 禁用工作
  ├── 8. 清除意识形态（如果disablesIdeo）            ← 身份改变
  ├── 9. 清除访客状态                                ← 社交改变
  ├── 10. 通知皇室系统（Royalty）                    ← 清除头衔
  ├── 11. 重置医疗设置                               ← 医疗改变
  ├── 12. 更新攻击目标缓存                           ← 战斗改变
  ├── 13. 清除Lord（如果clearLord）                  ← AI改变
  ├── 14. 设置敌意响应为Attack（非passive时）         ← 战斗行为
  ├── 15. 放弃所有所有权                             ← 财产改变
  ├── 16. 通知殖民者不可用                           ← 工作改变
  ├── 17. 发送任务信号"BecameMutant"                 ← 任务系统
  ├── 18. 通知任务管理器（视为死亡）                  ← 任务系统
  ├── 19. 通知机械师系统（如果是机械师）              ← Biotech联动
  ├── 20. 给亲属添加思想（relativeTurnedThought）     ← 社交影响
  ├── 21. 重置基因                                   ← 基因改变
  ├── 22. 通知能力变更                               ← 能力改变
  ├── 23. 清除所有政策（服装/食物/药物/阅读）          ← 政策改变
  ├── 24. HandleEquipment()                          ← 装备处理
  ├── 25. ResolveGraphics()                          ← 外观改变
  ├── 26. 设置站立动画                               ← 动画改变
  ├── 27. 标记殖民者栏脏                             ← UI更新
  ├── 28. 发现Codex条目                              ← 百科更新
  └── 29. 标记everLostEgo（如果clearsEgo）            ← 永久标记
```

### 1.3 Revert()恢复流程与局限

**位置**：`RimWorld.Pawn_MutantTracker.Revert()` L255

```csharp
public void Revert(bool beingKilled = false)
{
    hasTurned = false;
    pawn.mutant = null;  // ★ 直接置空mutant引用
    // 移除mutant Hediff和givesHediffs
    // 恢复Need、工作类型、派系、意识形态
    // 清除动画、能力、图形
}
```

**恢复了什么 vs 没恢复什么**：

| 维度 | 恢复？ | 说明 |
|------|--------|------|
| mutant状态 | ✅ | `hasTurned=false`, `pawn.mutant=null` |
| Hediff | ✅ | 移除mutantHediff和givesHediffs |
| Need | ✅ | `AddOrRemoveNeedsAsAppropriate()` |
| 工作类型 | ✅ | `Notify_DisabledWorkTypesChanged()` |
| 派系 | ✅ | 恢复originalFaction |
| 意识形态 | ✅ | 恢复originalIdeo |
| 皇室 | ✅ | `Notify_Resurrected()` |
| 外观 | ✅ | `SetAllGraphicsDirty()` |
| **装备** | ❌ | Turn时已掉落/销毁，不恢复 |
| **基因** | ❌ | Turn时已Reset，不恢复原始基因 |
| **所有权** | ❌ | Turn时已UnclaimAll，不恢复 |
| **政策** | ❌ | Turn时已清除，不恢复 |
| **任务** | ❌ | Turn时已通知"死亡"，不恢复 |

> **关键发现**：Revert()是"部分恢复"——核心身份（派系、意识形态、Need、工作）恢复，但物质层面（装备、基因、所有权）不恢复。这说明变异体转化在设计上是"准永久"的，Revert主要用于特殊场景（如AwokenCorpse死亡时clearMutantStatusOnDeath）。

### 1.4 三个MutantDef实例对比

| 维度 | Shambler（蹒跚者） | Ghoul（食尸鬼） | AwokenCorpse（觉醒尸体） |
|------|-------------------|----------------|------------------------|
| **定位** | 敌对僵尸 | 可控战斗单位 | 特殊敌对实体 |
| **派系** | Entities | 保持原派系 | Entities |
| **可征召** | ❌ | ✅ | ❌ |
| **Need** | 全禁用 | 仅Food | 全禁用 |
| **装备** | 保留（标记为尸体穿戴） | 全部销毁 | 保留 |
| **外观** | 尸体贴图+伤口覆盖层 | 专用皮肤/头型/发型 | 尸体贴图 |
| **攻击方式** | 牙齿+双手抓 | 牙齿+双爪 | 默认 |
| **特殊能力** | 无 | GhoulFrenzy等（白名单） | UnnaturalCorpseSkip |
| **爬行攻击** | ✅ | ❌ | ❌ |
| **死亡清除** | ✅ clearMutantStatusOnDeath | ❌ | ❌ |
| **倒地即死** | 25% | 25% | 0% |
| **再生** | ❌ | ✅ Regeneration | ✅ RapidRegeneration |


### 1.5 HandleEquipment装备处理

**位置**：`RimWorld.Pawn_MutantTracker.HandleEquipment()` L324

```csharp
private void HandleEquipment()
{
    // 1. 掉落主武器
    if (pawn.equipment?.Primary != null)
        pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out _, pawn.PositionHeld);

    // 2. 处理服装
    if (def.disableApparel)
    {
        if (pawn.MapHeld != null)
            pawn.apparel.DropAll(pawn.Position);    // 在地图上→掉落
        else
            pawn.apparel.DestroyAll();               // 不在地图上→销毁
    }

    // 3. 尸体标记
    if (def.isConsideredCorpse)
    {
        foreach (Apparel item in pawn.apparel.WornApparel)
            if (item.def.apparel.careIfWornByCorpse)
                item.WornByCorpse = true;            // 标记为尸体穿戴（降低价值）
    }
}
```

三种装备处理模式：
- **Shambler**：`disableApparel=false` + `isConsideredCorpse=true` → 保留服装但标记为尸体穿戴
- **Ghoul**：`disableApparel=true` → 掉落/销毁所有服装
- **AwokenCorpse**：默认 → 保留服装

## 2. 非人化（Inhumanized）

### 2.1 Hediff_Inhumanized实现

**位置**：`Verse.Hediff_Inhumanized` — 极简实现，仅在PostAdd/PostRemoved时更新StudyManager缓存。

```csharp
public class Hediff_Inhumanized : Hediff
{
    public override void PostAdd(DamageInfo? dinfo)
    {
        if (!ModsConfig.AnomalyActive) { pawn.health.RemoveHediff(this); return; }
        base.PostAdd(dinfo);
        Find.StudyManager.UpdateStudiableCache(pawn, pawn.MapHeld);
    }
    public override void PostRemoved()
    {
        base.PostRemoved();
        Find.StudyManager.UpdateStudiableCache(pawn, pawn.MapHeld);
    }
}
```

非人化的"效果"不在Hediff类中实现，而是**分散在各系统的检查点**中——通过`pawn.Inhumanized()`扩展方法查询。

### 2.2 系统影响范围

`pawn.Inhumanized()`被30+处代码检查，影响范围：

| 系统 | 影响 | 检查位置 |
|------|------|---------|
| **社交互动** | 禁止闲聊、深谈、浪漫、求婚、善意 | InteractionWorker_Chitchat/DeepTalk/RomanceAttempt/MarriageProposal/KindWords |
| **聚会** | 排除出聚会参与者 | GatheringsUtility |
| **思想** | 排除多种情境思想 | 30+个ThoughtDef的nullifyingHediffs |
| **道德判定** | 视为Psychopath（无道德约束） | RelationsUtility, Pawn_StoryTracker |
| **研究** | 可被研究（如囚犯） | CompStudiable |
| **暗黑恐惧** | 免疫超自然黑暗恐惧 | ThoughtWorker_UnnaturalDarkness |

### 2.3 Rehumanize完全可逆

**位置**：`RimWorld.AnomalyUtility.Rehumanize()` L158

```csharp
public static void Rehumanize(this Pawn pawn)
{
    Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Inhumanized);
    if (firstHediffOfDef == null)
        Log.Error("Tried to re-humanized a pawn that was not inhumanized.");
    else
        pawn.health.RemoveHediff(firstHediffOfDef);
}
```

> **关键发现**：非人化是最轻量的"状态切换"——单个Hediff的添加/移除，所有效果通过分散检查实现，完全可逆且无副作用。这是"Hediff作为状态标记"模式的典范。

## 3. 死眠（Deathrest）

### 3.1 三层架构

| 层级 | 类 | 职责 |
|------|-----|------|
| **基因层** | `Gene_Deathrest` | 管理死眠容量、绑定建筑、进度追踪、Gizmo |
| **需求层** | `Need_Deathrest` | 追踪死眠需求值，判断是否需要死眠 |
| **Hediff层** | `Hediff_Deathrest` | 死眠期间的状态标记，驱动TickDeathresting |

**协作流程**：
```
Need_Deathrest达到阈值 → 提示需要死眠
  → 玩家手动或系统强制 → SanguophageUtility.TryStartDeathrest()
    → 添加Hediff_Deathrest → Gene_Deathrest.Notify_DeathrestStarted()
      → Pawn进入Deathrest Job → 每Tick推进进度
        → 进度完成 → 移除Hediff → Gene_Deathrest.Notify_DeathrestEnded()
          → 结束Job → Pawn恢复正常
```

### 3.2 触发路径

**主动触发**：玩家通过Gizmo命令Pawn进入死眠床/棺材。

**被动触发（致命伤害）**：`ForceDeathrestOrComa()` — 当Deathless基因的Pawn受到致命伤害时：

```csharp
// Verse.Pawn_HealthTracker.ForceDeathrestOrComa() L789
if (pawn.CanDeathrest())
{
    SanguophageUtility.TryStartDeathrest(pawn, DeathrestStartReason.LethalDamage);
    GeneUtility.OffsetHemogen(pawn, -9999f);  // 清空Hemogen
}
else
{
    SanguophageUtility.TryStartRegenComa(pawn, DeathrestStartReason.LethalDamage);
}
if (!Downed) { forceDowned = true; MakeDowned(dinfo, hediff); }
```

**TryStartDeathrest核心**（`RimWorld.SanguophageUtility` L34）：
```csharp
public static bool TryStartDeathrest(Pawn pawn, DeathrestStartReason reason)
{
    // 前置检查：DLC激活、已生成、未在死眠、有Deathrest基因
    gene_Deathrest.autoWake = reason != DeathrestStartReason.PlayerForced;
    pawn.health.AddHediff(HediffDefOf.Deathrest);  // ★ 核心：添加Hediff触发整个流程
    return true;
}
```

### 3.3 恢复机制

死眠完成后，`Hediff_Deathrest.ShouldRemove`返回true → 自动移除 → `PostRemoved()`通知Gene并结束Job。


## 4. 机械体自动关机（Mech Self-Shutdown）

### 4.1 Need_MechEnergy关机/恢复逻辑

**位置**：`RimWorld.Need_MechEnergy.NeedInterval()` L109

```csharp
public override void NeedInterval()
{
    float num = 400f;  // 150 ticks/interval, 400 intervals/day
    if (!IsSelfShutdown)
        CurLevel -= FallPerDay / num;      // 正常消耗
    else
        CurLevel += 1f / num;              // 关机时恢复（1/天）

    if (CurLevel <= 0f)
        selfShutdown = true;               // ★ 能量归零→标记关机
    else if (CurLevel >= 15f || pawn.CurJobDef == JobDefOf.MechCharge)
        selfShutdown = false;              // ★ 能量≥15或正在充电→解除关机

    // Hediff同步
    if (firstHediffOfDef != null && !selfShutdown)
        pawn.health.RemoveHediff(firstHediffOfDef);  // 解除→移除Hediff
    else if (firstHediffOfDef == null && selfShutdown)
        pawn.health.AddHediff(HediffDefOf.SelfShutdown);  // 关机→添加Hediff

    // 强制进入SelfShutdown Job
    if (selfShutdown && pawn.Spawned && !pawn.Downed && pawn.CurJobDef != JobDefOf.SelfShutdown)
    {
        if (pawn.Drafted) pawn.drafter.Drafted = false;  // 取消征召
        pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.SelfShutdown, result),
            JobCondition.InterruptForced, ...);
    }
}
```

### 4.2 SelfShutdown状态判定

```csharp
public bool IsSelfShutdown
{
    get
    {
        if (pawn.CurJobDef == JobDefOf.SelfShutdown)
            return pawn.GetPosture().Laying();  // 必须在执行SelfShutdown Job且已躺下
        return false;
    }
}
```

### 4.3 系统影响

自动关机期间的系统影响：

| 系统 | 影响 | 检查方式 |
|------|------|---------|
| **能力** | 禁用（PawnCapacitiesHandler检查IsDeactivated） | `!pawn.IsDeactivated()` |
| **护盾** | 禁用（CompShield/CompProjectileInterceptor） | `p.IsSelfShutdown()` |
| **浮动菜单** | 可被搬运到充电器 | FloatMenuOptionProvider |
| **Lord** | 通知PawnLost | CompMechanoid.ToSleep() |
| **UI** | 显示Zzz图标 | PawnUIOverlay |
| **倒地即死** | 排除（不触发DoD系统） | CheckForStateChange |

> **关键发现**：机械体自动关机是"Need驱动的自动状态切换"的典范——Need监控→阈值触发→Hediff标记→Job执行→Need恢复→自动解除。整个流程无需外部干预，完全自洽。

## 5. 机械体停用（Mech Deactivation）

### 5.1 CompMechanoid.Deactivate()/WakeUp()

**位置**：`RimWorld.CompMechanoid` L86

```csharp
public void Deactivate()
{
    deactivated = true;
    ToSleep();                              // 继承自CompCanBeDormant
    Pawn.health.CheckForStateChange(null, null);  // 触发状态检查
}

public override void ToSleep()
{
    base.ToSleep();
    Pawn?.jobs.EndCurrentJob(JobCondition.InterruptForced);
    Pawn?.GetLord()?.Notify_PawnLost(Pawn, PawnLostCondition.Incapped);
    active = false;
}

public override void WakeUp()
{
    if (!Deactivated)  // ★ 停用状态下WakeUp()无效
    {
        base.WakeUp();
        active = true;
    }
}
```

### 5.2 与自动关机的区别

| 维度 | 自动关机（SelfShutdown） | 手动停用（Deactivate） |
|------|------------------------|----------------------|
| **触发** | Need_MechEnergy归零 | 手动命令 |
| **恢复** | 自动（能量≥15） | 手动WakeUp()（需先取消deactivated） |
| **实现层** | Need + Hediff + Job | Comp（CompMechanoid） |
| **能量消耗** | 停止消耗，缓慢恢复 | 停止消耗，不恢复 |
| **WakeUp可用** | 是 | 否（被deactivated阻止） |

> **关键发现**：停用是"不可自动恢复的休眠"——与自动关机的关键区别在于`WakeUp()`被`Deactivated`标志阻止。这是"手动控制的状态锁定"模式。

## 6. 对比分析：5种先例的实现模式

| 模式 | 先例 | 核心特征 | 适用场景 |
|------|------|---------|---------|
| **专用Tracker** | 变异体转化 | 独立Tracker类管理全部状态 | 需要全面改变Pawn的场景 |
| **Hediff标记** | 非人化 | 单Hediff + 分散检查点 | 轻量行为改变，完全可逆 |
| **三层架构** | 死眠 | Gene+Need+Hediff协作 | 复杂周期性状态 |
| **Need驱动** | 机械体关机 | Need监控→Hediff→Job→自动恢复 | 资源耗尽的自动响应 |
| **Comp驱动** | 机械体停用 | Comp标志位 + 手动控制 | 手动控制的状态锁定 |

## 7. 关键源码引用表

| 类 | 方法/字段 | 命名空间 | 行号 | 机制 |
|----|---------|---------|------|------|
| `Pawn_MutantTracker` | `Turn()` | RimWorld | L150 | 变异体转化（20+副作用链） |
| `Pawn_MutantTracker` | `Revert()` | RimWorld | L255 | 变异体恢复（部分恢复） |
| `Pawn_MutantTracker` | `HandleEquipment()` | RimWorld | L324 | 装备处理（掉落/销毁/尸体标记） |
| `MutantDef` | 类定义 | RimWorld | L8 | 变异体定义（100+字段） |
| `Hediff_Inhumanized` | 类定义 | Verse | L3 | 非人化Hediff（极简实现） |
| `AnomalyUtility` | `Inhumanized()` | RimWorld | L149 | 非人化检查（扩展方法） |
| `AnomalyUtility` | `Rehumanize()` | RimWorld | L158 | 恢复人性（移除Hediff） |
| `Gene_Deathrest` | 类定义 | RimWorld | — | 死眠基因管理 |
| `Hediff_Deathrest` | 类定义 | Verse | L6 | 死眠Hediff |
| `Need_Deathrest` | `Deathresting` | RimWorld | L23 | 死眠状态判定 |
| `SanguophageUtility` | `TryStartDeathrest()` | RimWorld | L34 | 启动死眠 |
| `SanguophageUtility` | `TryStartRegenComa()` | RimWorld | L69 | 启动再生昏迷 |
| `Pawn_HealthTracker` | `ForceDeathrestOrComa()` | Verse | L789 | 致命伤害强制死眠/昏迷 |
| `Need_MechEnergy` | `NeedInterval()` | RimWorld | L109 | 机械体能量管理+自动关机 |
| `Need_MechEnergy` | `IsSelfShutdown` | RimWorld | L24 | 自动关机状态判定 |
| `CompMechanoid` | `Deactivate()` | RimWorld | L86 | 机械体停用 |
| `CompMechanoid` | `ToSleep()` | RimWorld | L93 | 进入休眠 |
| `CompMechanoid` | `WakeUp()` | RimWorld | L102 | 唤醒（停用时无效） |
| `RestUtility` | `IsDeactivated()` | RimWorld | L481 | 停用状态检查（扩展方法） |
| `RestUtility` | `IsSelfShutdown()` | RimWorld | L472 | 自动关机检查（扩展方法） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-12 | 创建文档：5种Pawn形态/状态切换先例完整源码分析（变异体转化MutantDef 100+字段5维度分类+Turn() 20+副作用链+Revert()恢复局限+3个MutantDef实例对比+HandleEquipment 3模式、非人化Hediff_Inhumanized极简实现+30+检查点影响范围+Rehumanize完全可逆、死眠Gene_Deathrest+Need_Deathrest+Hediff_Deathrest三层架构+主动/被动触发+ForceDeathrestOrComa、机械体自动关机Need_MechEnergy关机/恢复逻辑+SelfShutdown Hediff+Job+6系统影响、机械体停用CompMechanoid.Deactivate()/WakeUp()+与自动关机区别），含5种先例变化维度矩阵、5种实现模式对比、RimWT战斗体切换启示7维度、源码引用表20项 | Claude Opus 4.6 |
| v1.1 | 2026-02-15 | 移除RimWT项目特定建议至独立汇总文件 | Claude Opus 4.6 |
