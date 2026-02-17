---
标题：派系Pawn特征与生成机制
版本号: v1.1
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 派系Pawn特征与生成机制完整技术参考——三层架构（FactionDef→PawnGroupMaker→PawnKindDef）、PawnKindDef关键字段8组分类（战斗/装备预算/装备筛选/品质/背景/基因/技能/行为控制）、5派系代表性PawnKindDef对比表（部落战士/城镇守卫/佣兵枪手/精英佣兵/帝国士兵×10维度）、PawnGroupMaker组合机制（4种kindDef类型+战术变体实例）、点数预算分配算法（ChoosePawnGenOptionsByPoints迭代加权随机+MaxPawnCost曲线限制+PawnWeightFactorByMostExpensivePawnCostFractionCurve）、装备生成流程（PawnWeaponGenerator标签匹配+预算筛选+PawnApparelGenerator层级填充）
---

# 派系Pawn特征与生成机制

**总览**：RimWorld中派系Pawn的特征由三层架构决定：

```
FactionDef层          → PawnGroupMaker层        → PawnKindDef层
（派系整体特征）        （场景组合配置）           （个体Pawn定义）
maxPawnCostPerTotal     kindDef(Combat/Trader/    combatPower, weaponMoney,
PointsCurve,            Settlement/Peaceful),     apparelMoney, weaponTags,
xenotypeSet,            commonality(权重),         apparelTags, itemQuality,
backstoryFilters        options(PawnKindDef→       skills, techHediffs,
                        selectionWeight映射)       xenotypeSet
```

FactionDef定义派系级约束（最大Pawn点数曲线、默认异种概率、背景故事过滤），PawnGroupMaker定义场景级组合（战斗/贸易/定居点各用哪些PawnKind、权重多少），PawnKindDef定义个体级属性（战斗力、装备预算、技能、品质等）。生成时，`PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints()`在总点数预算内迭代加权随机选取PawnKindDef，再由`PawnGenerator`按PawnKindDef的字段生成具体Pawn。

## 1. PawnKindDef关键字段分组

### 1.1 战斗属性

| 字段 | 类型 | 说明 |
|------|------|------|
| `combatPower` | float | 战斗力点数（=生成代价），决定在点数预算中的消耗 |
| `isFighter` | bool | 是否为战斗型（影响AI行为优先级） |
| `canBeSapper` | bool | 能否担任工兵（挖墙绕防御） |
| `factionLeader` | bool | 是否为派系领袖（每组最多1个） |

### 1.2 装备预算

| 字段 | 类型 | 说明 |
|------|------|------|
| `weaponMoney` | FloatRange | 武器预算范围（银币），决定可选武器的市场价值上限 |
| `apparelMoney` | FloatRange | 服装预算范围（银币） |
| `techHediffsMoney` | FloatRange | 植入体预算范围（银币） |
| `techHediffsChance` | float | 生成植入体的概率（0~1） |

### 1.3 装备筛选

| 字段 | 类型 | 说明 |
|------|------|------|
| `weaponTags` | List&lt;string&gt; | 武器标签过滤（如`NeolithicMeleeDecent`、`IndustrialGunAdvanced`） |
| `apparelTags` | List&lt;string&gt; | 服装标签过滤（如`Neolithic`、`SpacerMilitary`） |
| `apparelRequired` | List&lt;ThingDef&gt; | 必须穿戴的服装（如帝国士兵必须穿FlakJacket） |
| `apparelDisallowTags` | List&lt;string&gt; | 禁止的服装标签 |
| `techHediffsTags` | List&lt;string&gt; | 植入体标签过滤（如`Simple`、`Advanced`、`ImplantEmpireCommon`） |
| `techHediffsRequired` | List&lt;HediffDef&gt; | 必须安装的植入体（如帝国士兵必须有`DeathAcidifier`） |
| `specificApparelRequirements` | List | 特定部位服装要求（bodyPartGroup+apparelLayer+stuff+alternateTagChoices） |

### 1.4 品质与耐久

| 字段 | 类型 | 说明 |
|------|------|------|
| `itemQuality` | QualityCategory | 装备品质（Poor/Normal/Good等），默认Normal |
| `gearHealthRange` | FloatRange | 装备耐久度范围（如0.5~1.8表示50%~180%耐久） |
| `biocodeWeaponChance` | float | 武器绑定概率（绑定后仅本人可用） |

### 1.5 背景与年龄

| 字段 | 类型 | 说明 |
|------|------|------|
| `backstoryFiltersOverride` | List | 覆盖派系级背景故事过滤（如帝国士兵用`ImperialFighter`） |
| `backstoryCryptosleepCommonality` | float | 冬眠背景概率 |
| `maxGenerationAge` | int | 最大生成年龄 |

### 1.6 基因与异种

| 字段 | 类型 | 说明 |
|------|------|------|
| `xenotypeSet` | XenotypeSet | PawnKind级异种概率集（覆盖派系级） |
| `useFactionXenotypes` | bool | 是否使用派系级异种（false=使用自身xenotypeSet） |

> **说明**：异种概率有两级——FactionDef.xenotypeSet（派系级默认）和PawnKindDef.xenotypeSet（个体级覆盖）。当`useFactionXenotypes=false`时，使用PawnKindDef自身的xenotypeSet。帝国士兵就是典型例子：`useFactionXenotypes=false`，自带25% Hussar概率。

### 1.7 技能

| 字段 | 类型 | 说明 |
|------|------|------|
| `skills` | List&lt;SkillRange&gt; | 技能范围约束（如Shooting 10~15） |
| `requiredWorkTags` | List&lt;WorkTags&gt; | 必须的工作标签（如`Violent`确保能战斗） |
| `disallowedTraits` | List&lt;TraitDef&gt; | 禁止的特性（如精英佣兵禁止`Brawler`） |

### 1.8 行为控制

| 字段 | 类型 | 说明 |
|------|------|------|
| `combatEnhancingDrugsChance` | float | 携带战斗增强药物概率 |
| `combatEnhancingDrugsCount` | IntRange | 携带药物数量范围 |
| `initialWillRange` | FloatRange | 初始意志力（影响俘虏招募难度） |
| `initialResistanceRange` | FloatRange | 初始抵抗力（影响俘虏招募时间） |
| `chemicalAddictionChance` | float | 化学品成瘾概率 |
| `invNutrition` | float | 携带食物营养值 |
| `inventoryOptions` | InventoryOptionRoot | 携带物品配置（如10%概率携带工业药物） |

## 2. 各派系代表性PawnKindDef对比表

| 维度 | 部落战士 Tribal_Warrior | 城镇守卫 Town_Guard | 佣兵枪手 Mercenary_Gunner | 精英佣兵 Mercenary_Elite | 帝国士兵 Empire_Fighter_Trooper |
|------|----------------------|-------------------|------------------------|----------------------|-------------------------------|
| **派系** | TribeSavage | OutlanderCivil | Pirate | Pirate | Empire |
| **combatPower** | 50 | 60 | 85 | 130 | 65 |
| **weaponMoney** | 150 | 250~400 | 330~650 | 500~1400 | 1100~2500 |
| **apparelMoney** | 200~300 | 400~600 | 1000~1500 | 2500~3500 | 5000~8000 |
| **itemQuality** | Poor | *(默认Normal)* | Normal | Normal | Normal |
| **weaponTags** | NeolithicMeleeDecent | Gun | Gun | IndustrialGunAdvanced | IndustrialGunAdvanced |
| **apparelTags** | Neolithic | IndustrialBasic/Advanced/MilitaryBasic | Industrial系4种 | Industrial系4种+SpacerMilitary | Industrial系4种 |
| **techHediffsChance** | *(无)* | 4% | 15% | 35% | 30% |
| **techHediffsMoney** | *(无)* | 200~700 | 700~1200 | 1000~1200 | 1000~1500 |
| **techHediffsRequired** | *(无)* | *(无)* | *(无)* | *(无)* | **DeathAcidifier** |
| **xenotypeSet** | *(用派系级)* | *(用派系级)* | *(用派系级)* | *(用派系级)* | 25% Hussar, 3% Genie/Neanderthal |
| **skills** | *(无约束)* | *(无约束)* | Shooting 4~14 | Shooting 10~15 | Shooting 4~10 |
| **biocodeWeaponChance** | *(无)* | *(无)* | 20% | 30% | 15% |
| **combatEnhancingDrugs** | *(无)* | *(无)* | 5% | 80%（1~2个） | 15% |
| **gearHealthRange** | 0.5~1.8 | 0.6~2 | 0.7~3.2 | 1~1 | *(默认)* |
| **initialResistanceRange** | 5~9 | 15~24 | 6~10 | 15~23 | 15~24 |

> **关键发现**：
> 1. 装备预算差距巨大——部落战士weaponMoney仅150，帝国士兵高达1100~2500，相差7~17倍
> 2. 帝国士兵是唯一强制要求`DeathAcidifier`的PawnKind——死后尸体酸化，防止装备被掠夺
> 3. 精英佣兵80%概率携带战斗增强药物，是所有标准PawnKind中最高的
> 4. `gearHealthRange`控制装备耐久度随机范围——部落装备可能只有50%耐久，佣兵枪手装备可达320%

## 3. PawnGroupMaker组合机制

### 3.1 PawnGroupMaker结构

| 字段 | 类型 | 说明 |
|------|------|------|
| `kindDef` | PawnGroupKindDef | 组类型：`Combat`/`Peaceful`/`Trader`/`Settlement`/`Settlement_RangedOnly`/`Miners`/`Hunters`等 |
| `commonality` | float | 同类型多个PawnGroupMaker之间的选择权重 |
| `options` | List&lt;PawnGenOption&gt; | PawnKindDef→selectionWeight映射（战斗/定居点用） |
| `traders` | List&lt;PawnGenOption&gt; | 商人PawnKind（贸易用） |
| `carriers` | List&lt;PawnGenOption&gt; | 驮兽PawnKind（贸易用） |
| `guards` | List&lt;PawnGenOption&gt; | 护卫PawnKind（贸易用） |
| `disallowedStrategies` | List&lt;RaidStrategyDef&gt; | 禁止的袭击策略（如纯近战组禁止Siege） |
| `maxTotalPoints` | float | 最大总点数限制（如流浪者组限制1000点） |

### 3.2 海盗派系战术变体实例

海盗派系（Pirate）拥有6个Combat PawnGroupMaker，通过commonality权重控制出现概率：

| # | 战术描述 | commonality | 特点 |
|---|---------|-------------|------|
| 1 | 混合（远程+近战） | **100** | 13种PawnKind全覆盖，最常见 |
| 2 | 纯近战 | 30 | 仅Thrasher/Slasher/Boss，禁止Siege |
| 3 | 纯远程 | 20 | 无近战单位 |
| 4 | 爆破特化 | 15 | Grenadier为主+少量远程，禁止Siege |
| 5 | 纯狙击 | 10 | 仅Mercenary_Sniper |
| 6 | 纯流浪者 | 2.5 | 仅Drifter，maxTotalPoints=1000 |

> **设计模式**：同一派系通过多个Combat PawnGroupMaker实现战术多样性。commonality=100的混合组占总权重的56%（100/177.5），纯流浪者组仅1.4%。`disallowedStrategies`防止不合理组合（如纯近战不能围攻）。

### 3.3 部落派系战术变体

部落派系（TribeSavage）拥有4个Combat PawnGroupMaker：

| # | 战术描述 | commonality | 特点 |
|---|---------|-------------|------|
| 1 | 混合（远程+近战） | **100** | 8种PawnKind |
| 2 | 纯远程 | 60 | 仅弓箭手/猎人/重弓手/酋长 |
| 3 | 纯近战 | 60 | 仅忏悔者/战士/狂战士/酋长 |
| 4 | 破墙混合 | 5 | 含Tribal_Breacher |

> **对比**：部落的纯远程和纯近战权重相等（各60），远高于海盗的对应比例。这反映了部落的战术特点——要么全弓箭手齐射，要么全近战冲锋。

## 4. 点数预算分配算法

### 4.1 核心流程：ChoosePawnGenOptionsByPoints

`PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints()`是Pawn组生成的核心算法：

```
输入：pointsTotal（总点数预算）, options（PawnGenOption列表）, groupParms（组参数）
输出：chosenOptions（选中的PawnKindDef+异种列表）

1. 初始化 pointsLeft = pointsTotal
2. while循环：
   a. GetOptions() → 获取所有可选PawnKind（过滤掉cost > pointsLeft的）
      - MaxPawnCost() 限制单个Pawn最大点数
      - Biotech启用时，每个PawnKind展开为多个异种变体（概率加权）
      - CanUseOption() 检查：cost ≤ pointsLeft, cost ≤ maxCost, 领袖唯一性
   b. 加权随机选择：
      weight = selectionWeight × PawnWeightFactorByMostExpensivePawnCostFractionCurve(cost/highestCost)
      → 低cost/highestCost比值的PawnKind获得更高权重（倾向选便宜的填充数量）
   c. 选中 → 加入chosenOptions, pointsLeft -= cost
   d. 无可选项 → 退出循环
3. 返回 chosenOptions
```

### 4.2 MaxPawnCost限制

`MaxPawnCost()`决定单个Pawn的最大允许点数：

```csharp
float maxCost = faction.def.maxPawnCostPerTotalPointsCurve.Evaluate(totalPoints);
maxCost = Min(maxCost, totalPoints / raidStrategy.minPawns);  // 策略最少人数约束
maxCost = Max(maxCost, minPointsToGenerate * 1.2f);           // 保证至少能生成1个
```

**各派系maxPawnCostPerTotalPointsCurve对比**：

| 总点数 | 海盗(Pirate) | 部落(TribeSavage) |
|--------|-------------|------------------|
| 0 | 35 | 35 |
| 70 | 50 | 50 |
| 700 | 100 | — |
| 800 | — | 100 |
| 1300 | 150 | 150 |
| 100000 | 10000 | 10000 |

> **说明**：低点数时（<70），单个Pawn最大只能花35~50点——这确保了低威胁袭击不会出现单个精英单位。海盗在700点时解锁100点上限，部落稍晚在800点。

### 4.3 PawnWeightFactorByMostExpensivePawnCostFractionCurve

选择权重的修正曲线——`cost/highestCost`越低（相对便宜），权重越高：

```
weightFactor = PawnWeightFactorByMostExpensivePawnCostFractionCurve.Evaluate(cost / highestCost)
```

这意味着算法倾向于用便宜的PawnKind填充数量，而非集中预算在少数精英上。结果是：低点数袭击=大量低级单位，高点数袭击=少量精英+大量低级单位混合。

## 5. 装备生成流程

### 5.1 武器生成：PawnWeaponGenerator.TryGenerateWeaponFor()

```
1. 收集所有武器-材质对（AllWeaponPairs）
2. 过滤：
   a. weaponTags匹配（PawnKindDef.weaponTags中至少一个匹配ThingDef.weaponTags）
   b. 市场价值 ≤ weaponMoney.max（预算上限）
   c. techLevel ≤ 派系科技等级（部落不会拿到工业武器）
   d. IsDerpWeapon排除（防止高级材质+低级武器的荒谬组合）
3. 加权随机选择：
   weight = GetCommonality() × 意识形态偏好 × 异种偏好
4. 生成武器Thing，设置品质（itemQuality）和耐久度（gearHealthRange）
5. biocodeWeaponChance概率绑定武器
```

### 5.2 服装生成：PawnApparelGenerator

```
1. 处理apparelRequired（必须穿戴的服装，如帝国士兵的FlakJacket）
2. 处理specificApparelRequirements（特定部位要求）
3. 迭代填充剩余槽位：
   a. 过滤：apparelTags匹配 + 预算范围内 + 不与已穿戴冲突
   b. 加权随机选择
   c. 扣减apparelMoney预算
   d. 重复直到预算耗尽或无可选服装
4. 设置品质和耐久度
```

### 5.3 植入体生成

```
1. techHediffsChance概率检查 → 不通过则跳过
2. 处理techHediffsRequired（必须安装的，如DeathAcidifier）
3. techHediffsMoney预算内随机选择植入体：
   a. techHediffsTags匹配
   b. 市场价值在预算范围内
4. 安装到对应身体部位
```

> **关键发现**：装备生成是"标签+预算"双重过滤模型。weaponTags/apparelTags决定"能选什么"，weaponMoney/apparelMoney决定"选多贵的"。这意味着模组设计PawnKindDef时，标签控制装备类型，预算控制装备等级。

## 6. 关键源码引用表

| 类/文件 | 方法/字段 | 关键内容 |
|---------|---------|---------|
| `RimWorld.PawnKindDef` | 100+字段 | Pawn个体定义（combatPower/装备预算/标签/品质/技能/基因） |
| `RimWorld.PawnGroupMaker` | kindDef, options, commonality | Pawn组配置（场景类型+PawnKind权重映射） |
| `RimWorld.PawnGenOption` | kind, selectionWeight, Cost | PawnKind→权重+代价映射 |
| `RimWorld.PawnGroupKindDef` | workerClass | 组类型定义（Combat/Trader/Settlement等） |
| `RimWorld.PawnGroupMakerUtility` | `ChoosePawnGenOptionsByPoints()` | 核心点数预算分配算法（迭代加权随机） |
| `RimWorld.PawnGroupMakerUtility` | `MaxPawnCost()` | 单Pawn最大点数限制（曲线+策略+最小保证） |
| `RimWorld.PawnGroupMakerUtility` | `GetOptions()` | 可选PawnKind收集（异种展开+CanUseOption过滤） |
| `RimWorld.PawnGroupKindWorker_Normal` | `GeneratePawns()` | Combat/Settlement组的Pawn生成入口 |
| `RimWorld.PawnWeaponGenerator` | `TryGenerateWeaponFor()` | 武器生成（标签匹配+预算筛选+加权随机） |
| `RimWorld.PawnApparelGenerator` | — | 服装生成（必须穿戴+标签匹配+预算迭代填充） |
| `RimWorld.PawnGenerator` | `GenerateGearFor()` | 装备生成总入口 |
| `Core/Defs/FactionDefs/Factions_Misc.xml` | Pirate FactionDef | 海盗派系6个Combat PawnGroupMaker+maxPawnCostCurve |
| `Core/Defs/PawnKindDefs_Humanlikes/PawnKinds_Mercenary.xml` | 海盗PawnKindDef | Drifter→Scavenger→Gunner→Elite→Boss完整层级 |
| `Core/Defs/PawnKindDefs_Humanlikes/PawnKinds_Tribal.xml` | 部落PawnKindDef | Penitent→Warrior→Archer→HeavyArcher→Chief层级 |
| `Royalty/Defs/PawnKinds/PawnKinds_Empire.xml` | 帝国PawnKindDef | Trooper→Janissary→Cataphract层级+DeathAcidifier |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-14 | 创建文档：三层架构总览（FactionDef→PawnGroupMaker→PawnKindDef）、PawnKindDef关键字段8组分类（战斗4/装备预算4/装备筛选7/品质3/背景3/基因2/技能3/行为控制7）、5派系代表性PawnKindDef对比表（部落战士/城镇守卫/佣兵枪手/精英佣兵/帝国士兵×16维度）、PawnGroupMaker组合机制（结构8字段+海盗6战术变体+部落4战术变体）、点数预算分配算法（ChoosePawnGenOptionsByPoints流程+MaxPawnCost曲线对比+权重修正曲线）、装备生成流程（武器5步+服装4步+植入体4步）、模组开发启示（BORDER门PawnKindDef 7决策+敌对惑星国家PawnGroupMaker 4决策）、源码引用表15项 | Claude Opus 4.6 |
| v1.1 | 2026-02-15 | 移除RimWT项目特定建议至独立汇总文件 | Claude Opus 4.6 |
