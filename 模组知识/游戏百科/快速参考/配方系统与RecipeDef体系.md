---
标题：配方系统与RecipeDef体系
版本号: v1.0
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld配方（Recipe）系统完整分析——RecipeDef 80+字段按功能分8大类、双向绑定机制（recipeUsers vs ThingDef.recipes）、IngredientCount材料需求三层过滤体系（fixedIngredientFilter/defaultIngredientFilter/Bill.ingredientFilter）、IngredientValueGetter两种计量模式（Volume/Nutrition）、产品定义两种类型（products标准产出/specialProducts特殊产出）、RecipeWorker两大分支（通用/手术）、手术配方完整体系（Recipe_Surgery 10+子类+成功率+并发症）、配方可用性5层条件链、模组扩展4种模式
---

# 配方系统与RecipeDef体系

**总览**：`RecipeDef`是RimWorld生产和手术系统的核心定义，80+字段覆盖材料需求、产出物品、技能要求、工作量、执行条件等所有维度。配方分两大类：**生产配方**（在工作台上制造物品）和**手术配方**（在Pawn身上执行手术）。配方与工作台通过双向绑定关联——`RecipeDef.recipeUsers`和`ThingDef.recipes`两条路径等效。

## 1. RecipeDef核心字段分类

### 按功能分8大类

| # | 功能类别 | 关键字段 | 说明 |
|---|---------|---------|------|
| 1 | **基础信息** | `defName`, `label`, `description`, `jobString` | 标识和显示文本 |
| 2 | **工作量** | `workAmount`, `workSpeedStat`, `workTableSpeedStat`, `efficiencyStat`, `workTableEfficiencyStat` | 制造时间和速度 |
| 3 | **材料需求** | `ingredients`, `fixedIngredientFilter`, `defaultIngredientFilter`, `allowMixingIngredients`, `ingredientValueGetterClass` | 输入材料定义 |
| 4 | **产出** | `products`, `specialProducts`, `productHasIngredientStuff`, `useIngredientsForColor` | 输出物品定义 |
| 5 | **技能** | `skillRequirements`, `workSkill`, `workSkillLearnFactor` | 技能门槛和经验 |
| 6 | **手术** | `appliedOnFixedBodyParts`, `addsHediff`, `removesHediff`, `surgerySuccessChanceFactor`, `deathOnFailedSurgeryChance`, `anesthetize` | 医疗手术专用 |
| 7 | **解锁条件** | `researchPrerequisite`, `researchPrerequisites`, `factionPrerequisiteTags`, `memePrerequisitesAny`, `mechanitorOnlyRecipe` | 可用性前置条件 |
| 8 | **关联** | `recipeUsers`, `requiredGiverWorkType`, `workerClass`, `workerCounterClass`, `unfinishedThingDef` | 工作台绑定和执行逻辑 |

### 工作量相关字段详解

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `workAmount` | float | — | 基础工作量（ticks），如组件=5000 |
| `workSpeedStat` | StatDef | null | 殖民者工作速度Stat（如`CookSpeed`、`GeneralLaborSpeed`） |
| `workTableSpeedStat` | StatDef | `WorkTableWorkSpeedFactor` | 工作台速度因子Stat |
| `efficiencyStat` | StatDef | null | 殖民者效率Stat（影响产出数量） |
| `workTableEfficiencyStat` | StatDef | `WorkTableEfficiencyFactor` | 工作台效率因子Stat |
| `workSkillLearnFactor` | float | 1.0 | 技能经验获取倍率 |

> **关键认知**：`workSpeedStat`和`workTableSpeedStat`影响制造速度（多快完成），`efficiencyStat`和`workTableEfficiencyStat`影响产出效率（产出多少）。两者独立计算。

## 2. 配方与工作台的双向绑定

配方与工作台通过两条等效路径关联：

| 路径 | 定义位置 | XML示例 | 说明 |
|------|---------|---------|------|
| **RecipeDef → ThingDef** | RecipeDef.`recipeUsers` | `<recipeUsers><li>TableMachining</li></recipeUsers>` | 配方声明自己可在哪些工作台使用 |
| **ThingDef → RecipeDef** | ThingDef.`recipes` | `<recipes><li>SmeltWeapon</li></recipes>` | 工作台声明自己支持哪些配方 |

**解析时机**：`RecipeDef.AllRecipeUsers`属性在运行时合并两条路径的结果。两种方式完全等效，选择取决于组织偏好：
- 通用配方（如熔炼）适合用`recipeUsers`——一处定义，多台使用
- 工作台专属配方适合用`ThingDef.recipes`——配方和工作台定义在一起

### requiredGiverWorkType字段

`requiredGiverWorkType`限制哪种WorkType的殖民者可以执行此配方：

| 值 | 说明 | 典型配方 |
|----|------|---------|
| `null`（默认） | 任何WorkType的殖民者都可执行 | 大多数制造配方 |
| `Cooking` | 仅烹饪工作类型 | 所有烹饪配方 |
| `Smithing` | 仅锻造工作类型 | 锻造配方 |
| `Tailoring` | 仅裁缝工作类型 | 裁缝配方 |
| `Crafting` | 仅手工工作类型 | 手工制造配方 |

> **关键认知**：`requiredGiverWorkType`不是限制工作台，而是限制殖民者——同一工作台上的不同配方可以要求不同WorkType的殖民者。

## 3. 材料需求体系

### 三层过滤架构

```
fixedIngredientFilter（不可变层）
    ↓ 初始化时复制到
defaultIngredientFilter（默认层，可被XML覆盖）
    ↓ 初始化时复制到
Bill.ingredientFilter（玩家可调层）
```

| 层级 | 定义位置 | 可修改性 | 说明 |
|------|---------|---------|------|
| `fixedIngredientFilter` | RecipeDef XML | 不可修改 | 配方允许的材料总范围（硬限制） |
| `defaultIngredientFilter` | RecipeDef XML | 不可修改 | 默认启用的材料子集（如排除人肉） |
| `Bill.ingredientFilter` | 运行时 | 玩家可调 | 玩家在Bill配置界面调整的过滤器 |

**过滤器关系**：`Bill.ingredientFilter`只能在`fixedIngredientFilter`的范围内调整。`defaultIngredientFilter`决定新建Bill时的初始过滤状态。

### IngredientCount（材料需求项）

每个`IngredientCount`定义一项材料需求：

```xml
<ingredients>
  <li>
    <filter>                    <!-- ThingFilter：允许哪些物品满足此需求 -->
      <thingDefs>
        <li>Steel</li>
      </thingDefs>
    </filter>
    <count>12</count>           <!-- 需要的数量（按IngredientValueGetter计量） -->
  </li>
</ingredients>
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `filter` | ThingFilter | 可接受的物品过滤器 |
| `count` | float | 需要的数量（计量方式由`ingredientValueGetterClass`决定） |

**IsFixedIngredient**：当filter只允许一种ThingDef时，该IngredientCount被视为"固定材料"——UI显示具体物品名而非类别名。

### IngredientValueGetter（两种计量模式）

| 计量模式 | 类 | 说明 | 典型配方 |
|---------|---|------|---------|
| **体积计量**（默认） | `IngredientValueGetter_Volume` | count=物品数量 | 制造配方（12个钢铁） |
| **营养计量** | `IngredientValueGetter_Nutrition` | count=营养值总和 | 烹饪配方（0.5营养值的食材） |

> **关键认知**：烹饪配方使用`IngredientValueGetter_Nutrition`+`allowMixingIngredients=true`，允许混合不同食材凑够营养值。制造配方使用默认的Volume模式，按物品个数计量。

### allowMixingIngredients

| 值 | 行为 | 典型场景 |
|----|------|---------|
| `false`（默认） | 每个IngredientCount只能用一种ThingDef满足 | 制造配方（12个钢铁，不能混用钢铁和银） |
| `true` | 可混合多种ThingDef满足同一IngredientCount | 烹饪配方（混合肉类和蔬菜凑够营养值） |

## 4. 产出定义

### 两种产出类型

| 类型 | 字段 | 说明 | 典型配方 |
|------|------|------|---------|
| **标准产出** | `products` | `List<ThingDefCountClass>`，直接指定产出物品和数量 | 制造组件、烹饪 |
| **特殊产出** | `specialProducts` | `List<SpecialProductType>`，从材料动态生成 | 屠宰、熔炼 |

### products（标准产出）

```xml
<products>
  <ComponentIndustrial>1</ComponentIndustrial>   <!-- ThingDef名=数量 -->
  <Steel>5</Steel>                                <!-- 可多个产出 -->
</products>
```

产出数量受效率影响：`实际数量 = Ceil(定义数量 × efficiency)`

### specialProducts（特殊产出）

| SpecialProductType | 说明 | 产出来源 |
|-------------------|------|---------|
| `Butchery` | 屠宰产出 | 调用`Thing.ButcherProducts()`，产出肉+皮+特殊掉落 |
| `Smelted` | 熔炼产出 | 调用`Thing.SmeltProducts()`，产出原材料（按比例回收） |

### productHasIngredientStuff

当`productHasIngredientStuff=true`时，产品的Stuff材料继承自主要材料（`dominantIngredient`）。例如：制造木质长弓时，产品的Stuff=木材。

## 5. RecipeWorker体系

### 继承体系

```
RecipeWorker（基类，通用配方）
└── Recipe_Surgery（手术基类）
    ├── Recipe_InstallImplant        ← 安装植入体
    ├── Recipe_InstallArtificialBodyPart ← 安装假肢
    ├── Recipe_RemoveImplant         ← 移除植入体
    ├── Recipe_RemoveBodyPart        ← 截肢
    ├── Recipe_AdministerIngestible   ← 给药
    ├── Recipe_AdministerUsableItem   ← 使用物品
    ├── Recipe_ExecuteByCut          ← 处决
    ├── Recipe_ChangeHediffLevel     ← 修改Hediff等级
    ├── Recipe_RemoveHediff          ← 移除Hediff
    └── Recipe_BloodTransfusion      ← 输血（Biotech）
```

### RecipeWorker核心方法

| 方法 | 说明 | 调用时机 |
|------|------|---------|
| `AvailableOnNow(Thing, BodyPartRecord)` | 配方是否对此目标可用 | UI显示配方列表时 |
| `ApplyOnPawn(Pawn, BodyPartRecord, Pawn, List\<Thing\>, Bill)` | 执行手术效果 | 手术完成时 |
| `GetPartsToApplyOn(Pawn, RecipeDef)` | 返回可应用的身体部位列表 | 手术目标选择时 |
| `ConsumeIngredient(Thing, RecipeDef, Map)` | 消耗材料 | 制造/手术完成时 |
| `Notify_IterationCompleted(Pawn, List\<Thing\>)` | 一次迭代完成通知 | 产品生成后 |

### 手术配方专用字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `appliedOnFixedBodyParts` | List\<BodyPartDef\> | 可应用的身体部位列表 |
| `appliedOnFixedBodyPartGroups` | List\<BodyPartGroupDef\> | 可应用的身体部位组 |
| `addsHediff` | HediffDef | 手术成功后添加的Hediff |
| `removesHediff` | HediffDef | 手术移除的Hediff |
| `changesHediffLevel` | HediffDef | 修改等级的Hediff |
| `hediffLevelOffset` | int | Hediff等级变化量 |
| `surgerySuccessChanceFactor` | float | 手术成功率因子（默认1.0） |
| `deathOnFailedSurgeryChance` | float | 手术失败致死概率 |
| `anesthetize` | bool | 是否需要麻醉（默认true） |
| `isViolation` | bool | 是否为暴力行为（影响好感度） |
| `targetsBodyPart` | bool | 是否需要选择身体部位 |

## 6. 配方可用性条件链

`RecipeDef.AvailableNow`属性按序检查5层条件：

| # | 条件 | 说明 |
|---|------|------|
| 1 | `researchPrerequisite` | 单个研究项目已完成 |
| 2 | `memePrerequisitesAny` | 玩家意识形态包含任一指定Meme |
| 3 | `researchPrerequisites` | 所有研究项目已完成 |
| 4 | `factionPrerequisiteTags` | 玩家派系拥有指定标签 |
| 5 | `fromIdeoBuildingPreceptOnly` | 意识形态建筑戒律解锁 |

> **关键认知**：`researchPrerequisite`（单数）和`researchPrerequisites`（复数）是两个独立字段——前者要求单个研究，后者要求所有研究都完成。

## 7. 原版配方XML示例对比

### 制造配方（Make_ComponentIndustrial）

```xml
<RecipeDef>
  <defName>Make_ComponentIndustrial</defName>
  <workAmount>5000</workAmount>                    <!-- 基础工作量 -->
  <workSpeedStat>GeneralLaborSpeed</workSpeedStat> <!-- 殖民者速度Stat -->
  <unfinishedThingDef>UnfinishedComponent</unfinishedThingDef> <!-- 长工期 -->
  <ingredients>
    <li>
      <filter><thingDefs><li>Steel</li></thingDefs></filter>
      <count>12</count>                            <!-- 12个钢铁（Volume计量） -->
    </li>
  </ingredients>
  <products><ComponentIndustrial>1</ComponentIndustrial></products>
  <skillRequirements><Crafting>8</Crafting></skillRequirements>
  <workSkill>Crafting</workSkill>
</RecipeDef>
```

### 烹饪配方（CookMealSimple）

```xml
<RecipeDef ParentName="CookMealBase">
  <defName>CookMealSimple</defName>
  <workSpeedStat>CookSpeed</workSpeedStat>
  <requiredGiverWorkType>Cooking</requiredGiverWorkType>  <!-- 限烹饪工 -->
  <allowMixingIngredients>true</allowMixingIngredients>    <!-- 允许混合 -->
  <ingredientValueGetterClass>IngredientValueGetter_Nutrition</ingredientValueGetterClass>
  <ingredients>
    <li>
      <filter><categories><li>FoodRaw</li></categories></filter>
      <count>0.5</count>                           <!-- 0.5营养值（Nutrition计量） -->
    </li>
  </ingredients>
  <products><MealSimple>1</MealSimple></products>
</RecipeDef>
```

### 手术配方（安装仿生眼）

```xml
<RecipeDef ParentName="SurgeryInstallImplantBase">
  <defName>InstallBionicEye</defName>
  <workerClass>Recipe_InstallArtificialBodyPart</workerClass>
  <appliedOnFixedBodyParts><li>Eye</li></appliedOnFixedBodyParts>
  <addsHediff>BionicEye</addsHediff>
  <ingredients>
    <li>
      <filter><thingDefs><li>BionicEye</li></thingDefs></filter>
      <count>1</count>
    </li>
  </ingredients>
  <skillRequirements><Medicine>5</Medicine></skillRequirements>
</RecipeDef>
```

## 8. 模组扩展配方的4种模式

| # | 模式 | 需要C# | 适用场景 | 典型实例 |
|---|------|--------|---------|---------|
| 1 | **纯XML定义** | 否 | 标准制造/烹饪/手术 | 新武器制造、新食物烹饪 |
| 2 | **自定义RecipeWorker** | 是 | 特殊产出逻辑 | 非标准手术效果 |
| 3 | **自定义IngredientValueGetter** | 是 | 特殊材料计量 | 按重量/按价值计量 |
| 4 | **自定义RecipeWorkerCounter** | 是 | 特殊库存计数 | TargetCount模式的自定义计数逻辑 |

> **关键认知**：绝大多数模组配方只需纯XML定义——RecipeDef的字段足够覆盖标准制造和手术场景。只有需要非标准产出逻辑（如条件性产出、随机产出）才需要自定义RecipeWorker。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 初始版本：RecipeDef 80+字段按8大类分类、双向绑定机制、三层材料过滤架构、IngredientValueGetter两种计量、产出定义两种类型、RecipeWorker继承体系含Recipe_Surgery 10+子类、手术专用字段、可用性5层条件链、3个原版XML示例对比、模组扩展4种模式 | Claude Opus 4.6 |
