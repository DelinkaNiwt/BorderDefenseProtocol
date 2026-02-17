---
标题：敌对派系与玩家Pawn系统差异
版本号: v1.1
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 敌对派系Pawn与玩家Pawn的系统级差异完整技术参考——倒地即死概率（玩家0%/非玩家人形按人口意图曲线/机械体100%/动物50%）、即死保护（仅IsColonist+外部暴力，7次迭代削减）、Pawn组件差异（玩家独有outfits/drugs/timetable/drafter等6组件）、AI与行为差异（玩家命令vs Lord/Duty状态机）、装备生成与掉落差异（PawnKindDef随机生成+敌对掉落自动Forbid+destroyGearOnDrop）、Need与Mood处理差异、医疗差异、Storyteller交互差异
---

# 敌对派系与玩家Pawn系统差异

**总览**：RimWorld中同一种族的Pawn，因所属派系不同（玩家 vs 敌对 vs 中立），在系统层面有**8个维度**的显著差异。核心判定属性是`pawn.Faction.IsPlayer`和`pawn.IsColonist`。这些差异不是种族层面的（不像机械体vs有机体），而是**运行时派系层面**的——同一个Pawn改变派系后，其系统行为会随之改变。

**核心差异速查矩阵**：

| # | 维度 | 玩家Pawn | 敌对派系Pawn | 判定属性 |
|---|------|---------|------------|---------|
| 1 | **倒地即死** | 0%（永不触发） | 按人口意图曲线×难度系数 | `Faction.IsPlayer` |
| 2 | **即死保护** | ✅（7次迭代削减） | ❌ | `IsColonist` |
| 3 | **Pawn组件** | 完整（outfits/drugs/drafter等） | 精简（无outfits/drugs/timetable） | `Faction.IsPlayer` |
| 4 | **AI/行为** | 玩家命令驱动 | Lord/Duty状态机驱动 | ThinkTree分支 |
| 5 | **装备生成** | 场景/手动配置 | PawnKindDef随机生成 | `PawnGenerator` |
| 6 | **装备掉落** | 正常掉落 | 自动Forbid | `HostileTo(OfPlayer)` |
| 7 | **医疗** | 自动治疗 | 默认不治疗（需俘虏后设置） | `playerSettings` |
| 8 | **Storyteller** | 保护机制（适应系统） | 威胁来源 | 适应系统 |

## 1. 倒地即死概率（Death on Downed）

`Pawn_HealthTracker.CheckForStateChange()`中的DoD判定是派系差异最关键的体现：

**触发前提**：`ShouldBeDowned() && ExternalViolenceFor(pawn) && !Faction.IsPlayer && !HostFaction.IsPlayer`

→ 玩家Pawn（包括殖民者和玩家机械体）**完全跳过**DoD判定。

**非玩家Pawn的DoD概率优先级链**（8级，短路求值）：

| 优先级 | 条件 | 概率 | 说明 |
|--------|------|------|------|
| 1 | `overrideDeathOnDownedChance ≥ 0` | 覆盖值 | 代码直接设置 |
| 2 | `IsMutant && Def.deathOnDownedChance ≥ 0` | 变异体值 | Shambler/Ghoul: 0.25 |
| 3 | `Deathless基因 && Faction==OfPlayer` | 0% | 不死基因玩家Pawn豁免 |
| 4 | `PawnKindDef.overrideDeathOnDownedChance` | 覆盖值 | PawnKindDef级别覆盖 |
| 5 | `Faction==OfEntities` | 按威胁曲线 | 异常实体 |
| 6 | `Animal` | 50% | 动物固定50% |
| 7 | `IsMechanoid` | **100%** | 机械体必死 |
| 8 | 其他人形 | 人口意图曲线 × 难度系数 | 默认路径 |

**人口意图曲线**（`DeathOnDownedChance_NonColonyHumanlikeFromPopulationIntentCurve`）：
- 人口意图高（缺人）→ DoD概率低（~26%），更多俘虏机会
- 人口意图低（人多）→ DoD概率高（~80%），减少俘虏
- 乘以`difficulty.enemyDeathOnDownedChanceFactor`（难度系数）

> **关键发现**：DoD系统是RimWorld的"隐形人口调节器"——游戏通过动态调整敌人倒地即死概率来控制俘虏供给，维持殖民地人口平衡。模组设计敌方单位时，可通过`PawnKindDef.overrideDeathOnDownedChance`精确控制。

## 2. 即死保护（Instant Kill Protection）

`DamageWorker_AddInjury.FinalizeAndAddInjury()`中的即死保护：

**触发条件**：`pawn.IsColonist && !IgnoreInstantKillProtection && ExternalViolenceFor(pawn) && !Rand.Chance(allowInstantKillChance)`

| 条件 | 说明 |
|------|------|
| `IsColonist` | 仅殖民者（不含机械体、囚犯、奴隶） |
| `ExternalViolenceFor` | 仅外部暴力（不含自残、手术） |
| `allowInstantKillChance` | 难度滑块（默认1.0=无保护，0.0=完全保护） |

**保护机制**：最多7次迭代，每次将伤害削减至`partHealth - num`（num每次翻倍），直到`WouldDieAfterAddingHediff`返回false。

**各类Pawn的即死保护状态**：

| Pawn类型 | IsColonist | 即死保护 |
|---------|-----------|---------|
| 自由殖民者 | ✅ | ✅ |
| 奴隶 | ❌ | ❌ |
| 囚犯 | ❌ | ❌ |
| 玩家机械体 | ❌ | ❌ |
| 敌对人形 | ❌ | ❌ |
| 敌对机械体 | ❌ | ❌ |

> **关键发现**：即死保护是RimWorld最强的玩家保护机制，但范围极窄——仅限自由殖民者。奴隶、囚犯、机械体都不受保护。

## 3. Pawn组件差异

`PawnComponentsUtility.AddAndRemoveDynamicComponents()`按派系动态添加/移除组件：

**玩家派系独有组件**（`Faction.IsPlayer`条件）：

| 组件 | 职责 | 非玩家Pawn |
|------|------|-----------|
| `outfits` | 服装策略 | null |
| `drugs` | 药物策略 | null |
| `timetable` | 时间表 | null |
| `reading` | 阅读追踪 | null |
| `inventoryStock` | 库存追踪 | null |
| `drafter` | 征召控制器 | null（非玩家无法征召） |

**玩家或囚犯共有组件**（`IsPlayer || HostFaction.IsPlayer`）：

| 组件 | 职责 |
|------|------|
| `foodRestriction` | 食物限制 |
| `playerSettings` | 玩家设置（医疗等级、区域限制） |

**关键行为差异**：
- `drafter == null`意味着非玩家Pawn**不可被征召**——所有行为由Lord/Duty系统驱动
- `playerSettings == null`意味着非玩家Pawn**没有医疗等级设置**——默认不接受治疗

> **关键发现**：Pawn从敌对派系转为玩家派系时（如俘虏招募），`AddAndRemoveDynamicComponents`会自动添加所有玩家组件。这是"招募"在系统层面的核心实现。

## 4. AI与行为差异

**Humanlike ThinkTree中的派系分支**：

```
ThinkTree: Humanlike
  ├── [所有Pawn] Despawned / Downed / MentalState
  ├── [玩家Pawn] ThinkNode_ConditionalOfPlayerFaction
  │     └── DraftedOrder → JobGiver_Orders（玩家命令）
  │     └── Idle → 自动工作/社交/娱乐
  ├── [所有Pawn] LordDuty（Lord状态机驱动）
  ├── [非玩家] 无DraftedOrder → 完全由Lord控制
  └── [无Lord] 默认行为（游荡/战斗）
```

**Lord/Duty系统**：
- 敌对Pawn的所有行为由`Lord`管理——Lord持有一组Pawn，通过`LordJob`的`StateGraph`驱动行为
- 袭击者的Lord由`IncidentWorker_RaidEnemy`创建，状态图包含：集结→进攻→撤退
- 玩家Pawn也可以有Lord（如仪式、远行队），但主要行为由玩家命令驱动

**战斗AI差异**：
- 玩家Pawn：`JobGiver_AIFightEnemies`中`allowManualCastWeapons = !IsColonist`——殖民者不自动使用手动施放武器
- 敌对Pawn：`allowManualCastWeapons = true`——AI自动使用所有武器能力

> **关键发现**：玩家Pawn和敌对Pawn共享同一个ThinkTree，差异通过`ThinkNode_ConditionalOfPlayerFaction`分支实现。这意味着模组可以通过自定义ThinkNode条件来精确控制不同派系Pawn的行为。

## 5. 装备生成与掉落差异

**装备生成**：
- 玩家Pawn：场景初始装备 + 手动装备管理
- 敌对Pawn：`PawnGenerator.GenerateGearFor()`按`PawnKindDef`的`weaponMoney`/`apparelMoney`随机生成

**装备掉落**：
- 玩家Pawn：掉落时不Forbid
- 敌对Pawn：`Pawn_ApparelTracker.DropAll()`中`forbid = pawn.Faction.HostileTo(Faction.OfPlayer)`——敌对Pawn的装备掉落后自动Forbid
- `PawnKindDef.destroyGearOnDrop`：部分PawnKind（如变异体）掉落时直接销毁装备而非掉落

> **关键发现**：敌对Pawn装备自动Forbid是防止玩家在战斗中捡拾敌人装备的设计。模组自定义敌方单位时，可通过`destroyGearOnDrop`控制装备是否可被玩家获取。

## 6. Need与Mood处理差异

**技术层面**：所有Humanlike Pawn（无论派系）都有完整的Need系统运行。但：

| 维度 | 玩家Pawn | 非玩家Pawn |
|------|---------|-----------|
| **Need面板** | 可见 | 不可见（除非选中） |
| **精神崩溃** | 影响游戏体验 | 影响AI行为（但玩家通常不关心） |
| **心情Debuff** | 玩家需要管理 | 自动运行 |
| **特定Need** | 部分Need有`nullifiedIfNotColonist`标记 | 被nullify的思想不生效 |
| **Thought过滤** | 完整 | `ThoughtUtility`中多处`IsColonist`检查 |

> **关键发现**：非玩家Pawn的Need系统是"静默运行"的——系统在后台完整计算，但对玩家体验几乎无影响。这意味着俘虏招募后，其Need状态是连续的（不会重置）。

## 7. 医疗差异

| 维度 | 玩家Pawn | 非玩家Pawn |
|------|---------|-----------|
| **自动治疗** | ✅（Doctor自动寻找伤员） | ❌（需手动设置） |
| **医疗等级** | `playerSettings.medCare`控制 | 无playerSettings → 默认不治疗 |
| **俘虏治疗** | — | 俘虏获得`playerSettings`后可设置医疗等级 |
| **倒地救援** | 自动搬运到床位 | 敌对Pawn不被救援（除非俘虏） |

## 8. Storyteller交互差异

| 维度 | 玩家Pawn | 敌对Pawn |
|------|---------|---------|
| **死亡影响** | 降低适应系数（减少后续威胁） | 无直接影响 |
| **人口计算** | 计入殖民者人口 | 不计入 |
| **威胁点数** | Pawn点数贡献（增加威胁） | 作为威胁本身 |
| **任务系统** | 死亡触发任务失败/更新 | 死亡可能触发任务完成 |

**适应系统**：殖民者死亡 → `adaptDays`大幅降低 → 后续威胁减少。这是RimWorld的"怜悯机制"——连续损失殖民者后，游戏会降低难度。

> **关键发现**：Storyteller的适应系统只关心玩家Pawn的死亡——敌对Pawn的死亡不影响适应系数。这意味着模组设计的"高价值敌方单位"被击杀后不会降低游戏难度。

## 9. 关键源码引用表

| 类/文件 | 方法/字段 | 关键内容 |
|---------|---------|---------|
| `Pawn_HealthTracker` | `CheckForStateChange()` | DoD概率8级优先级链 |
| `DamageWorker_AddInjury` | `FinalizeAndAddInjury()` | 即死保护（IsColonist+7次迭代） |
| `PawnComponentsUtility` | `AddAndRemoveDynamicComponents()` | 派系动态组件（outfits/drugs/drafter等） |
| `PawnComponentsUtility` | `CreateInitialComponents()` | 基础组件初始化 |
| `Pawn_ApparelTracker` | `DropAll()` | 敌对装备自动Forbid |
| `PawnGenerator` | `GenerateGearFor()` | 敌对Pawn装备随机生成 |
| `ThoughtUtility` | `nullifiedIfNotColonist` | 非殖民者思想过滤 |
| `JobGiver_AIFightEnemies` | `allowManualCastWeapons` | `!IsColonist`（敌对AI自动使用手动武器） |
| `HealthTuning` | `DeathOnDownedChance_*Curve` | DoD人口意图曲线 |
| `StorytellerUtilityPopulation` | `PopulationIntent` | 人口意图计算 |
| `Pawn_DraftController` | `Drafted` setter | 征召副作用链 |
| `PawnKindDef` | `overrideDeathOnDownedChance` | DoD概率覆盖 |
| `PawnKindDef` | `destroyGearOnDrop` | 装备掉落时销毁 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-13 | 创建文档：敌对派系与玩家Pawn的8维度系统差异——倒地即死概率（8级优先级链+人口意图曲线+难度系数）、即死保护（IsColonist+ExternalViolence+7次迭代+各类Pawn状态表）、Pawn组件差异（玩家独有6组件+共有2组件+动态添加机制）、AI与行为差异（ThinkTree派系分支+Lord/Duty系统+战斗AI allowManualCastWeapons）、装备生成与掉落差异（PawnKindDef随机生成+敌对自动Forbid+destroyGearOnDrop）、Need与Mood处理差异（静默运行+nullifiedIfNotColonist）、医疗差异（playerSettings控制+俘虏治疗）、Storyteller交互差异（适应系统+人口计算），含模组开发启示（RimWT敌方Trion兵4决策+派系切换5影响）、源码引用表13项 | Claude Opus 4.6 |
| v1.1 | 2026-02-15 | 移除RimWT项目特定建议至独立汇总文件 | Claude Opus 4.6 |
