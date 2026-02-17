---
标题：机械体与普通Pawn系统差异
版本号: v1.1
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 机械体（Mechanoid）与有机体Pawn的系统级差异完整技术参考——FleshTypeDef差异（isOrganic控制出血/腐烂/伤口渲染）、Pawn组件初始化差异（机械体缺失skills/story/genes/relations等12个Tracker）、需求系统差异（无食物/休息，有MechEnergy）、健康系统差异（无感染/无出血/机械修复替代医疗/手术无灵感加成）、死亡与倒地差异（非玩家机械体100%倒地即死）、行为与AI差异（专用ThinkTree/工作模式/固定技能）、战斗差异（EMP有效/NerveStun无效/不同精神状态）、社交/基因/渲染差异
---

# 机械体与普通Pawn系统差异

**总览**：机械体（`IsMechanoid=true`）与有机体Pawn在RimWorld中有**10个维度**的系统级差异。核心分界线是`FleshTypeDef.isOrganic`——机械体为`false`，有机体为`true`。这个标志位直接或间接影响出血、腐烂、伤口渲染、需求、健康、社交、基因等几乎所有子系统。代码中有**60+处`IsMechanoid`检查点**分散在各系统中。

**核心差异速查矩阵**：

| # | 维度 | 有机体Pawn | 机械体Pawn | 判定属性 |
|---|------|-----------|-----------|---------|
| 1 | **FleshType** | Normal/Insectoid（isOrganic=true） | Mechanoid（isOrganic=false） | `RaceProps.FleshType` |
| 2 | **需求** | Food, Rest, Joy, Mood等18种 | 仅MechEnergy（玩家机械体） | `RaceProps.foodType`, `needsRest` |
| 3 | **健康/修复** | 医疗治疗、自然愈合、感染风险 | 机械修复（CompMechRepairable）、无感染 | `IsFlesh`, `isImmuneToInfections` |
| 4 | **死亡/倒地** | 倒地→可救援（玩家0%即死） | 非玩家100%倒地即死 | `IsMechanoid` in CheckForStateChange |
| 5 | **行为/AI** | Humanlike ThinkTree + 玩家命令 | Mechanoid ThinkTree + 工作模式 | `RaceProps.thinkTreeMain` |
| 6 | **战斗/眩晕** | NerveStun有效、EMP无效 | EMP有效、NerveStun无效 | `StunHandler.CanBeStunnedByDamage` |
| 7 | **社交** | 完整社交系统（关系/心情/互动） | 无社交（无relations/story） | `IsFlesh` → relations |
| 8 | **基因** | 完整基因系统 | 无基因系统 | `Humanlike` → genes |
| 9 | **技能** | 技能系统（20个技能，可升级） | 固定技能等级（mechFixedSkillLevel） | `Humanlike` → skills |
| 10 | **渲染** | 头发/头型/服装层/呼吸特效 | 无头发/无呼吸/LifeStage贴图 | `PawnBreathMoteMaker` |

## 1. FleshType差异

两个FleshTypeDef定义了有机体与机械体的根本分界：

| 字段 | Normal（有机体） | Mechanoid（机械体） |
|------|-----------------|-------------------|
| `isOrganic` | true | false |
| `corpseCategory` | CorpsesAnimal | CorpsesMechanoid |
| `damageEffecter` | Damage_HitFlesh | Damage_HitMechanoid |
| `genericWounds` | WoundFleshA/B/C | WoundMechA/B/C |
| `bandagedWounds` | BandagedA/B/C | **无**（机械体不可包扎） |
| `hediffWounds` | Scarification, MissingBodyPart等 | **无** |

**`isOrganic`的影响链**：
- `RaceProps.IsFlesh` = `FleshType.isOrganic` → 控制是否有`Pawn_RelationsTracker`和`Pawn_PsychicEntropyTracker`
- `RaceProps.IsMechanoid` = `FleshType == Mechanoid` → 60+处专用检查
- `RaceProps.Animal` = `!ToolUser && IsFlesh && !IsAnomalyEntity` → 机械体不是动物

> **关键发现**：`IsFlesh`和`IsMechanoid`不是互斥的——理论上可以有`isOrganic=true`但`FleshType != Mechanoid`的非有机体（如Odyssey的Drone）。模组自定义FleshType时需注意这两个属性的独立性。

## 2. Pawn组件初始化差异

`PawnComponentsUtility.CreateInitialComponents()`按种族属性条件初始化Pawn的Tracker组件：

**所有Pawn共有**（无条件）：ageTracker, health, records, inventory, meleeVerbs, verbTracker, carryTracker, needs, mindState, ownership, thinker, jobs, stances

**ToolUser条件**（机械体✅，因为Intelligence≥1）：equipment, apparel

**Humanlike条件**（机械体❌，因为Intelligence需≥2）：

| 组件 | 职责 | 机械体替代方案 |
|------|------|--------------|
| `skills` | 技能系统 | `mechFixedSkillLevel`固定值 |
| `story` | 背景故事/特性 | 无 |
| `guest` | 囚犯/访客 | 无 |
| `guilt` | 罪恶感 | 无 |
| `workSettings` | 工作优先级 | 动态添加（仅玩家监管机械体） |
| `royalty` | 皇室头衔 | 无 |
| `ideo` | 意识形态 | 无 |
| `style` | 风格偏好 | 无 |
| `surroundings` | 环境感知 | 无 |
| `genes` | 基因系统 | 无 |
| `styleObserver` | 风格观察 | 无 |

**IsFlesh条件**（机械体❌）：

| 组件 | 职责 | 机械体替代方案 |
|------|------|--------------|
| `relations` | 社交关系 | 动态添加（仅玩家监管机械体，用于Overseer关系） |
| `psychicEntropy` | 心灵熵/Psyfocus | 无 |

**ShouldHaveAbilityTracker**（机械体✅，`Humanlike || IsMechanoid`）：abilities

**动态组件**（`AddAndRemoveDynamicComponents`）：
- 玩家殖民地机械体（`IsColonyMech`）额外获得：`drafter`（征召控制器）
- 玩家监管机械体（`IsPlayerOverseerSubject`）额外获得：`relations`、`workSettings`

> **关键发现**：机械体虽然有`equipment`和`apparel` Tracker（因为是ToolUser），但BaseMechanoid的XML中没有配置任何服装层级，实际上不穿戴服装。武器通过PawnKindDef的`weaponMoney`配置。

## 3. 需求系统差异

| 维度 | 有机体Pawn | 机械体Pawn |
|------|-----------|-----------|
| **食物** | Need_Food（持续下降，需进食） | 无（`foodType=None`） |
| **休息** | Need_Rest（持续下降，需睡眠） | 无（`needsRest=false`） |
| **心情** | Need_Mood（Seeker型，影响精神崩溃） | 无 |
| **娱乐** | Need_Joy（持续下降） | 无 |
| **能量** | 无 | Need_MechEnergy（`playerMechsOnly=true`，仅玩家机械体） |
| **其他** | Beauty, Comfort, Outdoors等 | 无 |

**Need_MechEnergy特殊行为**：
- 持续消耗，通过充电器补充（`JobGiver_GetEnergy_Charger`）
- 归零→自动关机（`SelfShutdown` Hediff + Job）
- 关机期间以1/天速率恢复，≥15时自动解除
- `NeedDef.playerMechsOnly=true`：仅当`IsMechanoid && Faction==OfPlayer && OverseerSubject!=null`时激活

> **关键发现**：机械体的Need系统极度精简——仅1个Need（MechEnergy），且仅限玩家控制的机械体。敌对机械体没有任何Need。

## 4. 健康系统差异

| 维度 | 有机体Pawn | 机械体Pawn |
|------|-----------|-----------|
| **出血** | 受伤出血（`bleedRateFactor`） | 不出血（`bloodDef=Filth_MachineBits`，掉落机械碎片） |
| **感染** | 伤口有感染风险 | 免疫感染（`isImmuneToInfections=true`） |
| **疾病** | 可患病（HediffGiverSet） | 无疾病（BaseMechanoid无hediffGiverSets） |
| **治疗** | 医疗治疗（Doctor工作） | 机械修复（`CompMechRepairable`，机械师专用） |
| **手术** | 灵感加成（`SurgeryOutcomeComp_Inspired`） | 无灵感加成（`!patient.RaceProps.IsMechanoid`） |
| **手术成功率** | 医术技能影响（`SurgeryOutcomeComp_SurgeonSuccessChance`） | 不受医术影响（`!patient.RaceProps.IsMechanoid`） |
| **年龄伤病** | 按lifeExpectancy生成（`AgeInjuryUtility`） | 极长寿命（2500年），实际不生成年龄伤病 |
| **身体部位移除** | 标准手术 | 移除时视为已有假肢（`Recipe_RemoveBodyPart`） |

**机械修复流程**：
```
机械体受伤 → 机械师发现（WorkGiver_RepairMech）
  → 检查：IsMechanoid && CompMechRepairable && HasInjury
    → 执行修复Job → 直接移除Hediff_Injury
```

> **关键发现**：机械体的健康系统本质上是"简化版"——没有感染、疾病、出血、年龄退化，修复是直接移除伤害而非渐进治疗。这使得机械体在战斗中更"可预测"。

## 5. 死亡与倒地差异

`CheckForStateChange()`中的倒地即死概率：

| Pawn类型 | 倒地即死概率 | 条件 |
|---------|------------|------|
| **玩家殖民者** | 0% | `Faction.IsPlayer` → 跳过整个DoD判定 |
| **玩家机械体** | 0% | `Faction.IsPlayer` → 跳过整个DoD判定 |
| **非玩家机械体** | **100%** | `IsMechanoid ? 1f` |
| **非玩家人形** | 按人口意图曲线 | `PopulationIntent × enemyDeathOnDownedChanceFactor` |
| **动物** | 50% | `Animal ? 0.5f` |
| **异常实体** | 按威胁曲线 | `DeathOnDownedChance_EntityFromThreatCurve` |

**即死保护**（`FinalizeAndAddInjury`）：
- 条件：`pawn.IsColonist && !IgnoreInstantKillProtection && ExternalViolenceFor(pawn)`
- 机制：最多7次迭代削减伤害至不致死
- 机械体：**不适用**（`IsColonist`对机械体返回false）

> **关键发现**：非玩家机械体100%倒地即死是硬编码的——这意味着敌对机械体永远不会被俘虏。玩家机械体虽然不触发DoD，但也没有即死保护（IsColonist=false）。

## 6. 行为与AI差异

**ThinkTree对比**：

| 维度 | Humanlike ThinkTree | Mechanoid ThinkTree |
|------|--------------------|--------------------|
| **精神状态** | 多种MentalState | 仅BerserkMechanoid |
| **征召** | DraftedOrder（玩家命令） | DraftedOrder（仅玩家机械体） |
| **Lord/Duty** | LordDuty子树 | LordDuty子树 |
| **工作** | 完整Work系统（20+工作类型） | 工作模式系统（Work/Escort/Recharge/SelfShutdown） |
| **自动关机** | 无 | `ThinkNode_ConditionalLowEnergy` → `JobGiver_SelfShutdown` |
| **停用** | 无 | `ThinkNode_ConditionalDeactivated` → `JobGiver_Deactivated` |
| **充电** | 无 | `JobGiver_GetEnergy_Charger` |

**工作系统差异**：
- 有机体：`Pawn_WorkSettings`管理20+工作类型优先级，技能影响效率
- 机械体：`mechEnabledWorkTypes`白名单限定可用工作，`mechFixedSkillLevel`固定技能等级（默认10），`mechWorkTypePriorities`设定优先级
- 机械体工作限制：`JobGiver_Work`中`canBeDoneByMechs`字段过滤不适合机械体的WorkGiver

**精神状态差异**：
- 有机体：多种MentalState（Berserk, Wander, Hide, SocialFight等）
- 机械体：仅`BerserkMechanoid`（`MentalStateUtility`中`IsMechanoid ? mechStateDef : stateDef`）

> **关键发现**：机械体的AI是"受限版Humanlike"——共享Lord/Duty系统和征召系统，但工作系统被白名单限制，精神状态被简化为仅Berserk。

## 7. 战斗与眩晕差异

**眩晕机制**（`StunHandler.CanBeStunnedByDamage`）：

| 伤害类型 | 有机体 | 机械体 | 判定条件 |
|---------|--------|--------|---------|
| **Stun** | ✅ | ✅ | 无条件（`def == DamageDefOf.Stun → true`） |
| **EMP** | ❌ | ✅ | `!IsFlesh`（非有机体才受EMP影响） |
| **MechBandShockwave** | ❌ | ✅ | `IsMechanoid`（仅机械体） |
| **NerveStun** | ✅ | ❌ | `!IsMechanoid`（非机械体才受神经眩晕） |

**其他战斗差异**：
- `DamageWorker_Nerve`：神经伤害对机械体不追加NerveStun（`!IsMechanoid`）
- `CompShield`：机械体护盾始终显示Gizmo（不限于玩家阵营）
- `CompProjectileInterceptor`：殖民者或机械体才显示弹幕盾Gizmo
- `AttackTargetFinder`：休眠机械体（`!Awake()`）不被视为有效攻击目标

> **关键发现**：EMP是机械体的克星——有机体完全免疫EMP，机械体完全免疫NerveStun。这是RimWorld战斗系统的核心不对称设计。

## 8. 社交与基因差异

**社交系统**：
- 有机体：完整社交系统——`Pawn_RelationsTracker`（关系）、`Pawn_StoryTracker`（背景/特性）、`Pawn_GuestTracker`（囚犯）
- 机械体：无社交系统。玩家监管机械体动态获得`relations`（仅用于Overseer关系），但无story/guest/guilt

**基因系统**：
- 有机体（Humanlike）：完整基因系统——`Pawn_GeneTracker`管理Endogene/Xenogene
- 机械体：无基因系统（`genes == null`）

**命名系统**：
- 有机体（Humanlike）：NameTriple（名/姓/昵称）
- 机械体：NameSingle（单名，如"Militor 1"），通过`Dialog_NamePawn`的`IsMechanoid`分支处理

> **关键发现**：机械体的社交隔离是彻底的——没有心情、没有关系（除Overseer）、没有背景故事、没有特性。这使得机械体在叙事层面是"工具"而非"角色"。

## 9. 渲染差异

| 维度 | 有机体Pawn | 机械体Pawn |
|------|-----------|-----------|
| **呼吸特效** | 有（`PawnBreathMoteMaker`，`Humanlike && !IsMechanoid`） | 无 |
| **LifeStage贴图** | 使用最后一个LifeStage | 使用第一个LifeStage（`IsMechanoid ? First() : Last()`） |
| **头发/头型** | 有（Humanlike） | 无 |
| **服装渲染** | 多层服装叠穿 | 无服装 |
| **伤口覆盖层** | 肉体伤口（WoundFlesh）+ 包扎（Bandaged） | 机械伤口（WoundMech），无包扎 |
| **选择框** | 标准 | 标准（`Selector`中`Humanlike || IsMechanoid`都可被选中） |

## 10. 关键源码引用表

| 类/文件 | 方法/字段 | 关键内容 |
|---------|---------|---------|
| `FleshTypeDef` | `isOrganic` | 有机体/机械体根本分界 |
| `RaceProperties` | `IsMechanoid`, `IsFlesh` | 种族类型判定属性 |
| `RaceProperties` | `ShouldHaveAbilityTracker` | `Humanlike \|\| IsMechanoid` |
| `PawnComponentsUtility` | `CreateInitialComponents()` | Pawn组件初始化（按种族条件） |
| `PawnComponentsUtility` | `AddAndRemoveDynamicComponents()` | 动态组件（drafter/relations/workSettings） |
| `Pawn_NeedsTracker` | `AddOrRemoveNeedsAsAppropriate()` | Need过滤（playerMechsOnly） |
| `Need_MechEnergy` | `NeedInterval()` | 机械体能量管理+自动关机 |
| `Pawn_HealthTracker` | `CheckForStateChange()` | 倒地即死概率（IsMechanoid→1f） |
| `DamageWorker_AddInjury` | `FinalizeAndAddInjury()` | 即死保护（仅IsColonist） |
| `StunHandler` | `CanBeStunnedByDamage()` | EMP/NerveStun差异判定 |
| `CompMechRepairable` | 类定义 | 机械修复替代医疗 |
| `MechWorkUtility` | `IsMechanoidWorkType()` | 机械体工作类型过滤 |
| `Pawn_WorkSettings` | `LimitInitialActiveWorks` | `!IsMechanoid`（机械体不限制初始工作） |
| `QualityUtility` | `GenerateQualityCreatedByPawn()` | `IsMechanoid ? mechFixedSkillLevel : skills` |
| `MentalStateUtility` | `TryGetMentalStateDef()` | `IsMechanoid ? mechStateDef : stateDef` |
| `PawnBreathMoteMaker` | `TryMakeBreathMote()` | `Humanlike && !IsMechanoid`（无呼吸特效） |
| `ThinkTreeDef` | Mechanoid | 机械体专用行为树 |
| `SurgeryOutcomeComp_Inspired` | `Affects()` | `!IsMechanoid`（无灵感加成） |
| `RecordsUtility` | `Notify_PawnKilled/Downed` | 机械体使用不同记录统计 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-13 | 创建文档：机械体与有机体Pawn的10维度系统差异——FleshTypeDef差异（isOrganic+corpseCategory+damageEffecter+伤口贴图）、Pawn组件初始化差异（CreateInitialComponents 3层条件+AddAndRemoveDynamicComponents动态组件）、需求系统差异（仅MechEnergy+playerMechsOnly过滤）、健康系统差异（无感染/出血/疾病+CompMechRepairable+手术无灵感）、死亡与倒地差异（非玩家机械体100%DoD+无即死保护）、行为与AI差异（Mechanoid ThinkTree+工作模式+固定技能+仅BerserkMechanoid）、战斗与眩晕差异（EMP/NerveStun不对称+护盾Gizmo+休眠目标排除）、社交与基因差异（无relations/story/genes+仅Overseer关系+NameSingle）、渲染差异（无呼吸/LifeStage贴图选择/无头发服装）、模组开发启示（继承vs自定义决策+RimWT Trion兵映射6维度），含源码引用表19项 | Claude Opus 4.6 |
| v1.1 | 2026-02-15 | 移除RimWT项目特定建议至独立汇总文件 | Claude Opus 4.6 |
