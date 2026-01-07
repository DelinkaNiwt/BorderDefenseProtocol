# RimWorld Defs 分类规范

## 元信息

**摘要**：标准化RimWorld Defs文件的分类和组织方式。定义Defs/目录的标准子目录结构和命名规范，便于快速查找、维护和扩展。

**版本**：v1.0
**修改时间**：2026-01-07
**关键词**：Defs分类、文件组织、XML定义、目录结构、最佳实践
**标签**：[定稿]

---

## 概述

RimWorld模组的游戏定义（Defs）是XML格式的数据文件，定义了游戏中的物品、建筑、研究、工作等内容。合理的Defs分类能够：

1. **加速查找**：快速定位所需的定义
2. **便于维护**：逻辑清晰，易于修改和扩展
3. **降低冲突**：减少文件重复和覆盖
4. **提升可读性**：新开发者易于理解

---

## 标准Defs分类

### 第一层：功能分类

按照游戏系统的功能划分为以下主要分类：

| 分类 | 用途 | 典型Def文件 |
|------|------|-----------|
| **Ability** | 特殊能力/技能 | AbilityDef |
| **Apparel** | 服装/装甲 | ApparelDef |
| **Body & Part** | 身体和身体部位 | BodyDef, BodyPartDef, BodyTypeGroupDef |
| **Building** | 建筑和结构 | BuildingDef |
| **Damage** | 伤害类型 | DamageDef |
| **Effect** | 视觉效果 | EffecterDef, MoteDefPreDrawn 等 |
| **Hair & Head** | 发型和头型 | HairDef, HeadTypeDef |
| **Hediff** | 生化增强和特性 | HediffDef |
| **Job** | 工作定义 | JobDef |
| **Pawn & Trait** | 奇偶特性 | TraitDef, PawnKindDef |
| **Recipe** | 制作配方 | RecipeDef |
| **Research** | 科技树 | ResearchProjectDef |
| **Sound** | 音效定义 | SoundDef |
| **Stats** | 统计值 | StatDef, StatCategoryDef |
| **Thing & Item** | 物品和东西 | ThingDef, ThingCategoryDef |
| **Thought** | 思想/心情 | ThoughtDef |
| **Weapon** | 武器 | ThingDef(Weapon类) |
| **WorkGiver** | 工作分配 | WorkGiverDef |

---

## 标准目录结构

### 基础结构

```
Defs/
├── Ability/                     # 特殊能力
├── Apparel/                     # 服装
├── Body/                        # 身体定义
│   ├── BodyDefs/
│   ├── BodyPartDefs/
│   └── BodyTypeGroupDefs/
├── BuildingDefs/                # 建筑
│   ├── BuildingDefs_Power/      # 电力相关
│   ├── BuildingDefs_Storage/    # 储存相关
│   ├── BuildingDefs_Production/ # 生产相关
│   ├── BuildingDefs_Security/   # 防卫相关
│   └── BuildingDefs_Misc/       # 其他
├── DamageDefs/                  # 伤害
├── EffectDefs/                  # 效果
├── Hair/                        # 发型和头型
│   ├── HairDefs/
│   └── HeadTypeDefs/
├── HediffDefs/                  # 生化增强
│   ├── HediffDefs_Bionic/       # 仿生增强
│   ├── HediffDefs_Mental/       # 心理特性
│   └── HediffDefs_Physical/     # 生理特性
├── JobDefs/                     # 工作
├── Pawn/                        # 奇偶相关
│   ├── PawnKindDefs/
│   ├── TraitDefs/
│   └── CharacterDefs/
├── RecipeDefs/                  # 配方
│   ├── RecipeDefs_Production/   # 生产配方
│   ├── RecipeDefs_Research/     # 研究相关
│   └── RecipeDefs_Meals/        # 食物配方
├── ResearchDefs/                # 科技树
├── SoundDefs/                   # 音效
├── StatDefs/                    # 统计
│   ├── StatDefs_Pawn/           # 角色统计
│   ├── StatDefs_Thing/          # 物品统计
│   └── StatDefs_Building/       # 建筑统计
├── ThingDefs/                   # 物品
│   ├── ThingDefs_Apparel/       # 服装物品
│   ├── ThingDefs_Building/      # 建筑物品
│   ├── ThingDefs_Item/          # 一般物品
│   ├── ThingDefs_Weapon/        # 武器物品
│   │   ├── ThingDefs_Weapon_Melee/
│   │   └── ThingDefs_Weapon_Ranged/
│   └── ThingDefs_Misc/          # 其他
├── ThinkTreeDefs/               # 思维树
├── ThoughtDefs/                 # 思想
│   ├── ThoughtDefs_Positive/    # 正面思想
│   ├── ThoughtDefs_Negative/    # 负面思想
│   └── ThoughtDefs_Neutral/     # 中立思想
├── WorkGiverDefs/               # 工作分配
└── Misc/                        # 杂项（无法分类的其他定义）
    ├── DesignationCategoryDefs/
    ├── MainButtonDefs/
    ├── MenuOptionDefs/
    └── ...
```

---

## 详细分类说明

### 1. Ability/ - 特殊能力

**包含**：AbilityDef等能力定义

**用途**：定义角色可以使用的特殊技能或能力

**文件命名**：
- `AbilityDefs.xml` - 基础能力
- `AbilityDefs_Combat.xml` - 战斗能力
- `AbilityDefs_Social.xml` - 社交能力

**示例**：
```xml
<AbilityDef>
  <defName>MyMod_ChargeAttack</defName>
  <label>冲刺攻击</label>
  ...
</AbilityDef>
```

---

### 2. Apparel/ - 服装

**包含**：ApparelDef, ApparelLayerDef等

**用途**：定义角色可以穿戴的服装和装甲

**文件命名**：
- `ApparelDefs_Armor.xml` - 装甲
- `ApparelDefs_Clothing.xml` - 衣服
- `ApparelDefs_Accessories.xml` - 配饰

**备注**：ThingDef中type="Apparel"的也属于此类

---

### 3. Body/ - 身体定义

**子分类**：

#### 3.1 BodyDefs/
**包含**：BodyDef，定义种族的身体结构

**文件命名**：
- `BodyDefs_Humanoid.xml` - 人形
- `BodyDefs_Animal.xml` - 动物
- `BodyDefs_MyRace.xml` - 自定义种族

#### 3.2 BodyPartDefs/
**包含**：BodyPartDef，定义身体部位

**文件命名**：
- `BodyPartDefs_Standard.xml` - 标准部位
- `BodyPartDefs_Custom.xml` - 自定义部位

#### 3.3 BodyTypeGroupDefs/
**包含**：BodyTypeGroupDef，身体类型组

---

### 4. BuildingDefs/ - 建筑

**子分类**：

```
BuildingDefs/
├── BuildingDefs_Power/          # 电力
│   ├── PowerPlants.xml          # 电力厂
│   ├── PowerDistribution.xml    # 配电
│   └── PowerStorage.xml         # 储能
├── BuildingDefs_Storage/        # 储存
│   ├── Containers.xml           # 容器
│   └── Shelves.xml              # 架子
├── BuildingDefs_Production/     # 生产
│   ├── Crafting.xml             # 工坊
│   ├── Research.xml             # 研究台
│   └── Growing.xml              # 农田
├── BuildingDefs_Security/       # 防卫
│   ├── Turrets.xml              # 枪塔
│   ├── Walls.xml                # 墙体
│   └── Traps.xml                # 陷阱
├── BuildingDefs_Beauty/         # 美观
│   └── Decorations.xml
├── BuildingDefs_Furniture/      # 家具
│   ├── Beds.xml
│   └── Chairs.xml
├── BuildingDefs_Temperature/    # 温度
│   ├── Coolers.xml
│   └── Heaters.xml
└── BuildingDefs_Misc/           # 其他
```

**文件命名约定**：
- `BuildingDefs_Power_*.xml` - 电力类
- `BuildingDefs_Security_*.xml` - 防卫类
- 等等

---

### 5. DamageDefs/ - 伤害

**包含**：DamageDef，伤害类型定义

**文件命名**：
- `DamageDefs.xml` - 标准伤害
- `DamageDefs_Energy.xml` - 能量伤害
- `DamageDefs_Custom.xml` - 自定义伤害

---

### 6. EffectDefs/ - 视觉效果

**包含**：EffecterDef, MoteDefPreDrawn, FleckDef等

**子分类**：

```
EffectDefs/
├── EffecterDefs/                # Effecter效果
├── MoteDefs/                    # Mote效果
└── FleckDefs/                   # Fleck闪烁效果
```

---

### 7. Hair/ - 发型和头型

**子分类**：

#### 7.1 HairDefs/
**包含**：HairDef，角色发型

**文件命名**：
- `HairDefs.xml` - 标准发型
- `HairDefs_Custom.xml` - 自定义发型

#### 7.2 HeadTypeDefs/
**包含**：HeadTypeDef，头型

---

### 8. HediffDefs/ - 生化增强和特性

**子分类**：

```
HediffDefs/
├── HediffDefs_Bionic/           # 仿生增强
│   ├── Eyes.xml
│   ├── Limbs.xml
│   └── Organs.xml
├── HediffDefs_Mental/           # 心理特性
│   ├── Addictions.xml           # 成瘾
│   ├── Disorders.xml            # 精神疾病
│   └── Traits.xml               # 特质
├── HediffDefs_Physical/         # 生理特性
│   ├── Diseases.xml             # 疾病
│   ├── Injuries.xml             # 伤口
│   └── BodyParts.xml            # 身体部位
├── HediffDefs_Radiation/        # 辐射相关
├── HediffDefs_Gene/             # 基因相关
└── HediffDefs_Misc/             # 其他
```

**重要**：HediffDef用途广泛，可以表示：
- 疾病
- 伤口
- 义肢/移植
- 特性/特质
- 成瘾

---

### 9. JobDefs/ - 工作

**包含**：JobDef，工作类型定义

**文件命名**：
- `JobDefs.xml` - 标准工作
- `JobDefs_Production.xml` - 生产工作
- `JobDefs_Combat.xml` - 战斗工作
- `JobDefs_Custom.xml` - 自定义工作

---

### 10. Pawn/ - 奇偶相关

**子分类**：

```
Pawn/
├── PawnKindDefs/                # 角色类型
│   ├── PawnKindDefs_Humanoid/
│   └── PawnKindDefs_Animal/
├── TraitDefs/                   # 特质
├── CharacterDefs/               # 角色
└── FactionDefs/                 # 派系
```

---

### 11. RecipeDefs/ - 配方

**子分类**：

```
RecipeDefs/
├── RecipeDefs_Production/       # 生产配方
│   ├── Crafting.xml
│   ├── Cooking.xml
│   └── Manufacturing.xml
├── RecipeDefs_Research/         # 研究相关
│   └── Research.xml
├── RecipeDefs_Meals/            # 食物配方
│   ├── Meals.xml
│   └── Ingredients.xml
└── RecipeDefs_Surgery/          # 手术
    └── Surgery.xml
```

---

### 12. ResearchDefs/ - 科技树

**包含**：ResearchProjectDef，科研项目

**文件命名**：
- `ResearchDefs.xml` - 标准科研
- `ResearchDefs_Tier1.xml` - 第一阶段
- `ResearchDefs_Tier2.xml` - 第二阶段

---

### 13. SoundDefs/ - 音效

**包含**：SoundDef，游戏音效定义

**文件命名**：
- `SoundDefs.xml` - 标准音效
- `SoundDefs_Combat.xml` - 战斗音效
- `SoundDefs_Ambient.xml` - 背景音

---

### 14. StatDefs/ - 统计值

**子分类**：

```
StatDefs/
├── StatDefs_Pawn/               # 角色统计
│   ├── Combat.xml               # 战斗统计
│   ├── Work.xml                 # 工作统计
│   └── Social.xml               # 社交统计
├── StatDefs_Thing/              # 物品统计
│   ├── Weapon.xml               # 武器统计
│   ├── Apparel.xml              # 服装统计
│   └── Building.xml             # 建筑统计
└── StatDefs_Category/           # 统计分类
    └── StatCategoryDefs.xml
```

---

### 15. ThingDefs/ - 物品

**最复杂的分类，细分多种物品类型**：

```
ThingDefs/
├── ThingDefs_Apparel/           # 服装物品
├── ThingDefs_Building/          # 建筑物品
├── ThingDefs_Item/              # 一般物品
│   ├── Items_Resources.xml      # 资源
│   ├── Items_Components.xml     # 组件
│   ├── Items_Medicine.xml       # 医药
│   └── Items_Misc.xml           # 其他
├── ThingDefs_Weapon/            # 武器物品
│   ├── Melee/
│   │   ├── Swords.xml
│   │   ├── Blunt.xml
│   │   └── Misc.xml
│   └── Ranged/
│       ├── Guns.xml
│       ├── Bows.xml
│       └── Misc.xml
├── ThingDefs_Animal/            # 动物
│   ├── Animals_Farm.xml         # 农场动物
│   ├── Animals_Wild.xml         # 野生动物
│   └── Animals_Mythical.xml     # 神话生物
├── ThingDefs_Plant/             # 植物
│   ├── Plants_Crop.xml          # 农作物
│   ├── Plants_Wild.xml          # 野生植物
│   └── Plants_Tree.xml          # 树木
├── ThingDefs_Pawn/              # 角色
│   └── Pawns_Humanlike.xml
├── ThingDefs_Projectile/        # 弹药
│   └── Projectiles.xml
└── ThingDefs_Misc/              # 其他物品
```

**ThingCategoryDefs**（物品分类）：
```
ThingDefs/
└── ThingCategoryDefs/
    ├── Categories.xml           # 基础分类
    └── Categories_Custom.xml    # 自定义分类
```

---

### 16. ThinkTreeDefs/ - 思维树

**包含**：ThinkTreeDef，决策树定义

**文件命名**：
- `ThinkTreeDefs.xml` - 标准思维树
- `ThinkTreeDefs_Combat.xml` - 战斗逻辑

---

### 17. ThoughtDefs/ - 思想

**子分类**：

```
ThoughtDefs/
├── ThoughtDefs_Positive/        # 正面思想（心情+）
│   ├── Comfort.xml              # 舒适类
│   ├── Social.xml               # 社交类
│   └── Achievement.xml          # 成就类
├── ThoughtDefs_Negative/        # 负面思想（心情-）
│   ├── Discomfort.xml           # 不适类
│   ├── Horror.xml               # 恐怖类
│   └── Sadness.xml              # 悲伤类
└── ThoughtDefs_Neutral/         # 中立思想
    └── Status.xml               # 状态类
```

---

### 18. WorkGiverDefs/ - 工作分配

**包含**：WorkGiverDef，工作分配器定义

**文件命名**：
- `WorkGiverDefs.xml` - 标准工作分配
- `WorkGiverDefs_Production.xml` - 生产分配

---

### 19. Misc/ - 杂项

**包含**：无法分类到上述类别的定义**

```
Misc/
├── DesignationCategoryDefs/     # 指令分类
├── MainButtonDefs/              # 主界面按钮
├── MenuOptionDefs/              # 菜单选项
├── FactionDefs/                 # 派系定义
├── RaidStrategyDefs/            # 突袭策略
├── LetterDefs/                  # 信件类型
├── IncidentDefs/                # 事件类型
├── TimeAssignmentDefs/          # 时间分配
├── WeatherDefs/                 # 天气
└── BiomeDefs/                   # 生物群落
```

---

## 命名规范

### Def文件命名

**格式**：`[类型]Defs[_分类].xml`

**示例**：
- `ThingDefs.xml` - 物品定义
- `ThingDefs_Weapons.xml` - 武器物品
- `ThingDefs_Weapon_Melee.xml` - 近战武器
- `RecipeDefs_Production.xml` - 生产配方
- `BuildingDefs_Power_Generators.xml` - 电力发电机

**规则**：
1. 使用DefType名称（ThingDef、RecipeDef等）
2. 用下划线_分隔分类层次
3. 避免使用中文（便于跨语言兼容）
4. 文件名简洁但具有描述性

---

### Def Name属性

**格式**：`[模组前缀]_[分类]_[名称]`

**示例**：
```xml
<ThingDef>
  <defName>MyMod_Weapon_LaserRifle</defName>
  ...
</ThingDef>

<RecipeDef>
  <defName>MyMod_Recipe_BuildAdvancedComputer</defName>
  ...
</RecipeDef>
```

**规则**：
1. 必须以模组名前缀开头，避免冲突
2. 使用下划线分隔层次
3. 使用PascalCase（首字母大写）
4. 不使用中文

---

## 目录创建检查表

创建新模组时，根据功能选择需要的目录：

### 最小化（内容很少）
- [ ] BuildingDefs/
- [ ] ThingDefs/
- [ ] RecipeDefs/

### 基础（小型内容模组）
- [ ] Ability/
- [ ] BuildingDefs/
- [ ] RecipeDefs/
- [ ] ResearchDefs/
- [ ] ThingDefs/
- [ ] WorkGiverDefs/

### 标准（中型内容模组）
- [ ] Ability/
- [ ] Apparel/
- [ ] BuildingDefs/
- [ ] DamageDefs/
- [ ] HediffDefs/
- [ ] JobDefs/
- [ ] RecipeDefs/
- [ ] ResearchDefs/
- [ ] StatDefs/
- [ ] ThingDefs/
- [ ] ThoughtDefs/
- [ ] WorkGiverDefs/

### 完整（大型系统/种族模组）
- [ ] 上述所有目录
- [ ] Hair/
- [ ] Pawn/
- [ ] Body/

---

## 最佳实践

### 1. 单个文件不要过大

**建议**：每个文件不超过200个定义

**过大的文件会**：
- 降低加载速度
- 增加维护难度
- 提高冲突风险

### 2. 按功能而非数量分类

**好的分类**：按功能分别创建 `RecipeDefs_Production.xml` 和 `RecipeDefs_Surgery.xml`

**不好的分类**：仅按数量创建 `RecipeDefs_1.xml` 和 `RecipeDefs_2.xml`

### 3. 对比参考模组

在创建自己的分类前，先查看：
- AncotLibrary 的标准分类
- Milira 的复杂分类
- CeleTech 的大型内容分类

### 4. 使用Parent继承减少重复

```xml
<!-- 定义基础 -->
<ThingDef Name="MyMod_WeaponBase">
  <damage>10</damage>
  <warmupTime>1.5</warmupTime>
</ThingDef>

<!-- 继承基础，避免重复 -->
<ThingDef ParentName="MyMod_WeaponBase">
  <defName>MyMod_Sword</defName>
  <label>sword</label>
</ThingDef>
```

### 5. 版本特定定义

如果某个Def仅在特定版本有效，创建版本特定的文件：

```
ThingDefs/
├── ThingDefs.xml                # 所有版本通用
├── ThingDefs_1.5_Only.xml       # 1.5独有
└── ThingDefs_1.6_Only.xml       # 1.6独有
```

---

## 常见问题

**Q: 我的定义不知道放在哪个目录？**

A: 按优先级查找：
1. 看DefType名称（如ThingDef → ThingDefs/）
2. 看功能内容（如种族定义 → Pawn/）
3. 实在不知道 → 放在Misc/

**Q: 可以混合不同DefType吗？**

A: 不推荐。即使一个文件中包含多个DefType，仍应按主要DefType分类到对应目录。

**Q: 子目录层数有上限吗？**

A: 建议不超过3层（目录/子目录/文件），过深会降低查找效率。

---

## 参考模组的分类对标

| 参考模组 | 目录数 | 分类特点 |
|---------|-------|--------|
| AncotLibrary | 98 | 按DefType标准分类 |
| Milira | 505 | 细分多个分类，清晰的层次 |
| CeleTech | 180 | 按功能域划分（武器、建筑等） |
| WeaponFitting | 中等 | 包含模组特定定义 |

---

## 版本历史

| 版本 | 修改时间 | 修改内容 | 修改者 |
|------|---------|---------|------|
| v1.0 | 2026-01-07 | 初版发布。基于参考模组分析，定义了19个标准Def分类及其子分类，提供了命名规范和最佳实践。 | 知识提炼者 |
