---
标题：建筑类型分类与DesignationCategory体系
版本号: v1.0
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld建筑双层分类体系——DesignationCategoryDef（建筑师菜单16个分类）与Building C#类继承（60+子类按8大功能分类）。涵盖Core 12 + DLC 4个DesignationCategory完整列表、Building子类继承体系、6大抽象模板继承链、8个核心Comp、BuildingProperties 100+字段分类、按交互方式分类
---

# 建筑类型分类与DesignationCategory体系

**总览**：RimWorld的建筑分类采用**双层体系**——`DesignationCategoryDef`定义建筑师菜单中的玩家可见分类（16个），`Building` C#类继承树定义引擎行为（60+子类）。两者独立运作：一个建筑的菜单归属由XML中`designationCategory`字段指定，其运行时行为由`thingClass`指定的C#类决定。此外，`ThingCategoryDef`树中的`Buildings`分支用于存储区过滤和贸易界面，但与建筑师菜单无关。

## 1. 双层分类体系总览

### 三个分类维度

| 维度 | 类型 | 作用 | 数量 |
|------|------|------|------|
| **DesignationCategoryDef** | XML Def | 建筑师菜单分类（玩家视角） | 16个 |
| **Building C#类** | C#继承 | 引擎运行时行为 | 60+子类 |
| **ThingCategoryDef** | XML Def树 | 存储区过滤/贸易界面 | Buildings下6个子分类 |

**关键认知**：
- `DesignationCategoryDef`决定建筑出现在建筑师菜单的哪个标签页——通过`BuildableDef.designationCategory`字段关联
- `thingClass`决定建筑的C#运行时行为——大多数建筑直接使用`Building`基类，只有需要特殊逻辑的才用子类
- `ThingCategoryDef`的Buildings分支（BuildingsProduction/BuildingsFurniture/BuildingsSecurity等）用于已建成物品的分类（如拆卸后的可搬运建筑），与建筑师菜单无关

### 建筑师菜单排序规则

`MainTabWindow_Architect`按`DesignationCategoryDef.order`**降序**排列标签页。order值越大越靠左（越优先）。DLC分类使用`preferredColumn`字段控制是否换行显示。

## 2. DesignationCategoryDef完整列表（16个）

按`order`降序排列（即建筑师菜单从左到右的顺序）：

### Core（12个）

| # | defName | label | order | 典型建筑 | 特殊说明 |
|---|---------|-------|-------|---------|---------|
| 1 | `Orders` | orders | 900 | —（纯指令） | 无建筑，全部是specialDesignator（取消/拆除/采矿/砍伐/狩猎/驯服/搬运等18个） |
| 2 | `Zone` | zone | 800 | —（纯区域） | 无建筑，全部是区域指令（储存区/种植区/家园区/屋顶区/清雪区等） |
| 3 | `Structure` | structure | 700 | 墙壁、门、柱子、栅栏 | 含SmoothWalls/RemoveFoundation特殊指令 |
| 4 | `Production` | production | 600 | 工作台（电炉/裁缝台/机加工台）、种植盆 | 工作台核心分类 |
| 5 | `Furniture` | furniture | 500 | 床、桌椅、灯、书架、衣架 | 最大的建筑分类 |
| 6 | `Power` | power | 400 | 发电机、电池、电线 | `showPowerGrid=true`；需研究`Electricity` |
| 7 | `Security` | security | 300 | 炮塔、沙袋、陷阱、路障 | 防御建筑核心分类 |
| 8 | `Misc` | misc | 250 | 通讯台、轨道贸易信标、运输舱发射器、坟墓 | 杂项 |
| 9 | `Floors` | floors | 200 | 各种地板（木/石/金属/地毯） | 含RemoveFloor/SmoothFloors/PaintFloor特殊指令 |
| 10 | `Joy` | recreation | 100 | 台球桌、象棋桌、电视、乐器 | 娱乐设施 |
| 11 | `Ship` | ship | 50 | 飞船反应堆、飞船计算核心、飞船引擎 | 需研究`ShipBasics` |
| 12 | `Temperature` | temperature | 25 | 加热器、冷却器、通风口、被动冷却器 | 温控设施 |

### DLC（4个）

| # | defName | label | order | DLC | 特殊说明 |
|---|---------|-------|-------|-----|---------|
| 13 | `Ideology` | ideology | 13 | Ideology | 意识形态建筑（祭坛、火炬柱、骷髅柱等）；`preferredColumn=1` |
| 14 | `Biotech` | biotech | 12 | Biotech | 机械师建筑（机械孵化器、基因组装器等）；需研究`Electricity`；`preferredColumn=1` |
| 15 | `Anomaly` | anomaly | 11 | Anomaly | 异常建筑（拘留平台、生物铁收割器等）；`minMonolithLevel=1` |
| 16 | `Odyssey` | odyssey | 10 | Odyssey | 奥德赛建筑；含RemoveFoundation特殊指令 |

### DesignationCategoryDef关键字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `order` | int | 排序权重，降序排列 |
| `specialDesignatorClasses` | List\<Type\> | 特殊指令类列表（如Designator_Cancel） |
| `showPowerGrid` | bool | 选中时是否显示电网覆盖层 |
| `researchPrerequisites` | List\<ResearchProjectDef\> | 解锁此分类所需的研究 |
| `preferredColumn` | int | 建筑师菜单中的列偏好（DLC用于换行） |
| `minMonolithLevel` | int | Anomaly DLC：所需最低巨石等级 |

## 3. Building C#类继承体系

`Verse.Building`继承自`ThingWithComps`，是所有建筑的C#基类。约60个直接/间接子类按功能分为8大类：

### 按功能分类

| 功能分类 | 核心C#类 | 典型建筑 | 关键特性 |
|---------|---------|---------|---------|
| **生产/工作** | `Building_WorkTable` | 电炉、裁缝台、机加工台 | ITab_Bills配方系统、BillStack |
| | `Building_ResearchBench` | 简易/高级研究台 | 研究项目选择 |
| | `Building_NutrientPasteDispenser` | 营养膏分配器 | 自动消耗原料产出食物 |
| | `Building_FermentingBarrel` | 发酵桶 | 时间进度条、温度敏感 |
| | `Building_PlantGrower` | 种植盆、日光灯 | sowTag种植标签 |
| **存储/容器** | `Building_Storage` | 货架、工具柜 | StorageSettings过滤、IStoreSettingsParent |
| | `Building_Casket` | 棺材基类 | 可容纳Thing的容器 |
| | `Building_CryptosleepCasket` | 低温休眠舱 | Casket子类，暂停生物老化 |
| | `Building_Crate` | 板条箱 | Casket子类，物品存储 |
| | `Building_Bookcase` | 书架 | 书籍存储与阅读 |
| **家具/床** | `Building_Bed` | 床、医疗床、婴儿床 | 所有权、医疗标记、bed_*字段 |
| | `Building_Throne` | 王座 | Royalty等级需求 |
| | `Building_MusicalInstrument` | 乐器 | 娱乐+技能训练 |
| | `Building_Art` | 雕塑（小/大/巨） | 艺术描述、品质影响美观 |
| **安防/陷阱** | `Building_Turret` → `Building_TurretGun` | 迷你炮塔、自动炮塔 | 自动瞄准射击、turretGunDef |
| | `Building_Trap` | 陷阱基类 | 触发判定、isTrap |
| | `Building_TrapDamager` | 尖刺陷阱 | 直接伤害 |
| | `Building_TrapExplosive` | IED陷阱 | 爆炸伤害 |
| | `Building_ProximityDetector` | 接近探测器 | Anomaly检测 |
| **电力** | `Building_Battery` | 电池 | 储电、短路风险 |
| | `Building_PowerSwitch` | 电力开关 | 电网分段控制 |
| **温控** | `Building_TempControl` | 加热器、冷却器 | 温度调节目标设定 |
| | `Building_AncientVent` | 远古通风口 | 被动温度均衡 |
| **可进入** | `Building_Enterable` | 可进入建筑基类 | Pawn进入→处理→退出流程 |
| | `Building_GrowthVat` | 成长舱 | Enterable子类，加速儿童成长 |
| | `Building_GeneExtractor` | 基因提取器 | Enterable子类，提取基因组 |
| | `Building_SubcoreScanner` | 亚核扫描器 | Enterable子类，生产亚核 |
| **特殊/自然** | `Building_Door` | 门、自动门 | 开关动画、通行判定 |
| | `Building_SteamGeyser` | 间歇泉 | 自然生成、地热发电基础 |
| | `Mineable` | 可开采岩石/矿脉 | mineableThing产出 |
| | `Building_ShipReactor` | 飞船反应堆 | 飞船启动倒计时 |
| | `Building_CommsConsole` | 通讯台 | 贸易/外交通讯 |
| | `Building_OrbitalTradeBeacon` | 轨道贸易信标 | 标记可交易区域 |
| | `Building_HoldingPlatform` | 拘留平台 | Anomaly实体拘留 |
| | `Building_FleshmassHeart` | 肉质心脏 | Anomaly肉质蔓延核心 |
| | `MapPortal` | 地图传送门 | Anomaly地下层入口 |

### 二级继承关系

```
Building (Verse)
├── Building_WorkTable          ← 所有工作台
├── Building_Turret
│   └── Building_TurretGun     ← 所有自动炮塔
├── Building_Trap
│   ├── Building_TrapDamager   ← 尖刺陷阱
│   ├── Building_TrapExplosive ← IED陷阱
│   └── Building_TrapReleaseEntity ← 释放实体陷阱(Anomaly)
├── Building_Casket
│   ├── Building_CryptosleepCasket ← 低温休眠舱
│   ├── Building_Crate         ← 板条箱
│   ├── Building_CorpseCasket  ← 石棺
│   └── Building_FleshSack    ← 肉袋(Anomaly)
├── Building_Enterable
│   ├── Building_GrowthVat     ← 成长舱
│   ├── Building_GeneExtractor ← 基因提取器
│   └── Building_SubcoreScanner ← 亚核扫描器
├── Building_Bed               ← 所有床（无子类）
├── Building_Storage           ← 所有存储建筑（无子类）
├── Building_Door              ← 所有门（无子类）
└── ... 其余40+直接子类（无二级继承）
```

> **关键认知**：大多数建筑直接使用`Building`基类（thingClass=Building），通过Comp组件和XML配置实现功能差异。只有需要特殊C#逻辑的建筑才使用子类。模组新增建筑通常不需要自定义C#类。

## 4. 抽象模板继承链

RimWorld通过XML `Abstract="True"` 模板提供建筑的默认配置。模组新建筑通常继承这些模板，避免重复配置。

### 核心模板继承树

```
BuildingBase                    ← 所有建筑的根模板
├── BenchBase                   ← 工作台模板
├── FurnitureBase               ← 家具模板（无品质）
│   └── FurnitureWithQualityBase ← 家具模板（有品质）
│       ├── TableBase           ← 桌子模板
│       └── AnimalBedFurnitureBase ← 动物床模板
├── ArtBuildingBase             ← 艺术品模板（thingClass=Building_Art）
├── TrapIEDBase                 ← IED陷阱模板（thingClass=Building_TrapExplosive）
├── BuildingNaturalBase         ← 自然建筑模板（岩石/间歇泉）
│   └── CocoonBase              ← 虫茧模板
├── CrateBase                   ← 板条箱模板
├── AncientBuildingBase         ← 远古建筑模板
├── AncientMechBuildingBase     ← 远古机械建筑模板
├── CrashedShipPartBase         ← 坠毁飞船部件模板
├── IdeoBuildingBase            ← 意识形态建筑模板(Ideology)
├── MechGestatorBase            ← 机械孵化器模板(Biotech)
├── SubcoreScannerBase          ← 亚核扫描器模板(Biotech)
└── ConditionCauserBase         ← 条件制造器模板(Royalty)
```

### 各模板提供的默认配置

| 模板 | 继承自 | 关键默认配置 |
|------|--------|------------|
| `BuildingBase` | — | `category=Building`, `thingClass=Building`, `selectable=true`, `drawerType=MapMeshOnly`, `terrainAffordanceNeeded=Light`, `SellPriceFactor=0.70` |
| `BenchBase` | BuildingBase | `terrainAffordanceNeeded=Medium`, `minifiedDef=MinifiedThing`(可搬运), `thingCategories=BuildingsProduction`, `CompProperties_ReportWorkSpeed`, `buildingTags=Production`, `workTableRoomRole=Workshop` |
| `FurnitureBase` | BuildingBase | `designationCategory=Furniture`, `minifiedDef=MinifiedThing`, `thingCategories=BuildingsFurniture`, `noRightClickDraftAttack=true` |
| `FurnitureWithQualityBase` | FurnitureBase | 在FurnitureBase基础上增加`CompQuality`（品质系统） |
| `ArtBuildingBase` | BuildingBase | `thingClass=Building_Art`, `CompQuality`+`CompProperties_Styleable`+`CompProperties_MeditationFocus`, `ITab_Art`, `passability=PassThroughOnly` |
| `TrapIEDBase` | BuildingBase | `thingClass=Building_TrapExplosive`, `designationCategory=Security`, `isTrap=true`, `tickerType=Normal`, `researchPrerequisites=IEDs`, `PlaceWorker_NeverAdjacentTrap` |

> **模组实践**：新增工作台继承`BenchBase`即可自动获得配方系统（ITab_Bills）、工作速度报告、生产标签等。新增家具继承`FurnitureWithQualityBase`自动获得品质系统。

## 5. 建筑核心Comp体系

建筑的大部分功能通过Comp组件实现，而非C#子类。以下是建筑最常用的8个核心Comp：

| # | Comp | Properties类 | 功能 | 典型使用者 |
|---|------|-------------|------|-----------|
| 1 | `CompPowerTrader` | `CompProperties_Power` | 消耗/产生电力 | 工作台、炮塔、灯、加热器 |
| 2 | `CompRefuelable` | `CompProperties_Refuelable` | 燃料消耗（木材/化合燃料等） | 火把、火炉、发电机 |
| 3 | `CompFlickable` | `CompProperties_Flickable` | 开关控制（玩家可手动开关） | 灯、加热器、炮塔 |
| 4 | `CompBreakdownable` | `CompProperties_Breakdownable` | 故障系统（随机故障需维修） | 工作台、炮塔、发电机 |
| 5 | `CompFacility` | `CompProperties_Facility` | 作为设施提供加成 | 工具柜→工作台、生命监护仪→医疗床 |
| 6 | `CompAffectedByFacilities` | `CompProperties_AffectedByFacilities` | 接收设施加成 | 工作台、医疗床、研究台 |
| 7 | `CompHeatPusher` | `CompProperties_HeatPusher` | 向房间推送热量 | 火把、熔炉、加热器 |
| 8 | `CompGlower` | `CompProperties_Glower` | 发光（提供照明） | 灯、火把、日光灯 |

### 其他常见建筑Comp

| Comp | 功能 | 典型使用者 |
|------|------|-----------|
| `CompQuality` | 品质系统（影响美观/效率） | 家具、艺术品、床 |
| `CompColorable` | 可染色 | 床、服装架 |
| `CompStyleable` | 可应用风格 | 艺术品、意识形态建筑 |
| `CompMeditationFocus` | 冥想焦点 | 艺术品、自然物、王座 |
| `CompAssignableToPawn` | 可分配给Pawn | 床、王座 |
| `CompGatherSpot` | 聚集点 | 篝火、桌子 |
| `CompSchedule` | 按时间表运行 | 日光灯（白天开/夜晚关） |

> **Comp组合模式**：一个典型的电力工作台通常组合`CompPowerTrader`+`CompFlickable`+`CompBreakdownable`+`CompAffectedByFacilities`+`CompHeatPusher`。这种组合完全通过XML配置，无需C#代码。

## 6. BuildingProperties关键字段

`RimWorld.BuildingProperties`是ThingDef中`<building>`块的C#映射类，包含100+字段。按功能分类：

### 基础属性

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `isEdifice` | bool | true | 是否为建筑实体（占据格子、阻挡移动） |
| `buildingTags` | List\<string\> | [] | 建筑标签（Production/Bed等，用于AI和查询） |
| `isInert` | bool | false | 是否惰性（不参与战斗目标选择） |
| `isTargetable` | bool | true | 是否可被瞄准攻击 |
| `claimable` | bool | true | 是否可被玩家认领 |
| `expandHomeArea` | bool | true | 建造时是否扩展家园区 |
| `paintable` | bool | false | 是否可涂漆 |
| `repairable` | bool | true | 是否可修复 |

### 工作台相关

| 字段 | 类型 | 说明 |
|------|------|------|
| `wantsHopperAdjacent` | bool | 是否需要相邻料斗（营养膏分配器） |
| `heatPerTickWhileWorking` | float | 工作时每tick产热量 |
| `workTableRoomRole` | RoomRoleDef | 工作台所属房间角色（Workshop） |
| `workTableNotInRoomRoleFactor` | float | 不在对应房间时的效率惩罚 |
| `unpoweredWorkTableWorkSpeedFactor` | float | 断电时工作速度系数 |
| `workTableCompleteSoundDef` | SoundDef | 工作完成音效 |

### 床相关（bed_*前缀）

| 字段 | 类型 | 说明 |
|------|------|------|
| `bed_healPerDay` | float | 每天治疗量 |
| `bed_defaultMedical` | bool | 默认是否为医疗床 |
| `bed_humanlike` | bool | 是否为人形床（vs动物床） |
| `bed_maxBodySize` | float | 最大体型限制 |
| `bed_slabBed` | bool | 是否为石板床（意识形态） |
| `bed_crib` | bool | 是否为婴儿床 |
| `bed_canBeMedical` | bool | 是否可设为医疗床 |

### 炮塔相关（turret*前缀）

| 字段 | 类型 | 说明 |
|------|------|------|
| `turretGunDef` | ThingDef | 炮塔武器定义 |
| `turretBurstWarmupTime` | FloatRange | 开火预热时间范围 |
| `turretBurstCooldownTime` | float | 连射冷却时间 |
| `turretTopDrawSize` | float | 炮塔顶部绘制大小 |
| `turretTopOffset` | Vector2 | 炮塔顶部偏移 |

### 门相关（door*前缀/sound*前缀）

| 字段 | 类型 | 说明 |
|------|------|------|
| `poweredDoorOpenSpeedFactor` | float | 通电时开门速度系数 |
| `unpoweredDoorOpenSpeedFactor` | float | 断电时开门速度系数 |
| `doorTempEqualizeIntervalClosed` | int | 关门时温度均衡间隔(ticks) |
| `doorTempEqualizeRate` | float | 温度均衡速率 |
| `roamerCanOpen` | bool | 漫游者是否可开门 |

### 存储相关

| 字段 | 类型 | 说明 |
|------|------|------|
| `fixedStorageSettings` | StorageSettings | 固定存储过滤（不可修改的部分） |
| `defaultStorageSettings` | StorageSettings | 默认存储过滤（玩家可调整） |
| `ignoreStoredThingsBeauty` | bool | 是否忽略存储物品的美观值 |
| `maxItemsInCell` | int | 每格最大物品数 |
| `preventDeteriorationOnTop` | bool | 防止顶部物品劣化 |
| `preventDeteriorationInside` | bool | 防止内部物品劣化 |

### 陷阱相关（trap*前缀）

| 字段 | 类型 | 说明 |
|------|------|------|
| `isTrap` | bool | 是否为陷阱 |
| `trapDestroyOnSpring` | bool | 触发后是否销毁 |
| `trapPeacefulWildAnimalsSpringChanceFactor` | float | 和平野生动物触发概率系数 |
| `trapDamageCategory` | DamageArmorCategoryDef | 陷阱伤害护甲类别 |

### 矿物相关（mineable*前缀）

| 字段 | 类型 | 说明 |
|------|------|------|
| `isNaturalRock` | bool | 是否为天然岩石 |
| `isResourceRock` | bool | 是否为资源岩石 |
| `mineableThing` | ThingDef | 开采产出物 |
| `mineableYield` | int | 开采产出数量 |
| `mineableDropChance` | float | 开采掉落概率 |

### AI行为

| 字段 | 类型 | 说明 |
|------|------|------|
| `ai_combatDangerous` | bool | AI是否视为战斗危险物 |
| `ai_chillDestination` | bool | AI是否视为休闲目的地 |
| `ai_neverTrashThis` | bool | 袭击者是否永不破坏此建筑 |

## 7. 按交互方式分类

从玩家/Pawn与建筑的交互方式角度，建筑可分为4大类：

### 可工作型（需殖民者操作）

殖民者需要前往建筑执行Job才能产生效果。

| 子类型 | 代表建筑 | 交互方式 | 关键C#类/Comp |
|--------|---------|---------|-------------|
| 工作台 | 电炉、裁缝台 | 选择配方→殖民者前往→执行制造Job | `Building_WorkTable` + ITab_Bills |
| 研究台 | 简易/高级研究台 | 选择研究项目→殖民者前往→执行研究Job | `Building_ResearchBench` |
| 种植设施 | 种植盆 | 殖民者前往→播种/收获 | `Building_PlantGrower` |
| 乐器 | 竖琴、钢琴 | 殖民者前往→演奏（娱乐+技能） | `Building_MusicalInstrument` |
| 通讯台 | 通讯台 | 殖民者前往→与派系/商队通讯 | `Building_CommsConsole` |

### 自动型（自动运行）

放置后自动运行，无需殖民者操作（但可能需要电力/燃料）。

| 子类型 | 代表建筑 | 自动行为 | 关键Comp |
|--------|---------|---------|---------|
| 炮塔 | 迷你炮塔、自动炮塔 | 自动检测敌人→瞄准→射击 | `Building_TurretGun` |
| 发电机 | 太阳能/风力/地热 | 自动产生电力 | `CompPowerPlant` |
| 温控 | 加热器、冷却器 | 自动调节房间温度 | `CompHeatPusher`/`Building_TempControl` |
| 灯光 | 立灯、日光灯 | 自动提供照明 | `CompGlower` |
| 陷阱 | 尖刺陷阱、IED | 自动触发（敌人踩上） | `Building_Trap` |

### 被动型（提供效果）

不需要操作也不自动运行，仅通过存在提供被动效果。

| 子类型 | 代表建筑 | 被动效果 | 机制 |
|--------|---------|---------|------|
| 美观 | 雕塑、花盆 | 提供Beauty值 | StatBase.Beauty |
| 家具 | 桌椅、地毯 | 提供舒适度/房间评分 | RoomStat |
| 存储 | 货架、工具柜 | 存储物品+防劣化 | `Building_Storage` |
| 设施加成 | 工具柜、生命监护仪 | 为相邻建筑提供属性加成 | `CompFacility` |
| 防御工事 | 沙袋、路障 | 提供掩体 | `fillPercent`+`cover` |

### 可进入型（Building_Enterable）

Pawn进入建筑内部，经过一段时间处理后退出。

| 建筑 | 进入条件 | 处理过程 | 产出 | DLC |
|------|---------|---------|------|-----|
| 低温休眠舱 | 手动指派 | 暂停老化 | — | Core |
| 成长舱 | 放入儿童 | 加速成长 | 成年Pawn | Biotech |
| 基因提取器 | 放入Pawn | 提取基因 | 基因组 | Biotech |
| 亚核扫描器 | 放入Pawn | 扫描亚核 | 亚核 | Biotech |

## 8. 关键类速查表

| 需求 | 推荐方案 | 说明 |
|------|---------|------|
| 新增工作台 | XML继承`BenchBase` + `thingClass=Building_WorkTable` | 自动获得配方系统 |
| 新增家具 | XML继承`FurnitureWithQualityBase` | 自动获得品质+家具分类 |
| 新增炮塔 | XML继承炮塔模板 + `thingClass=Building_TurretGun` | 需配置turretGunDef |
| 新增陷阱 | XML继承`TrapIEDBase`或自定义 | 需配置伤害/爆炸参数 |
| 新增存储建筑 | `thingClass=Building_Storage` + fixedStorageSettings | 配置存储过滤 |
| 新增可进入建筑 | 自定义C#类继承`Building_Enterable` | 需实现进入/处理/退出逻辑 |
| 新增床 | XML继承床模板 + `thingClass=Building_Bed` | 配置bed_*字段 |
| 新增门 | `thingClass=Building_Door` + 门相关字段 | 配置开关速度/音效 |
| 新增纯被动建筑 | XML继承`BuildingBase` + Comp组合 | 大多数情况不需要自定义C#类 |
| 新增电力建筑 | 添加`CompPowerTrader`/`CompPowerPlant` | 消耗或产生电力 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 初始版本：双层分类体系、16个DesignationCategoryDef、Building 60+子类、6大抽象模板、8核心Comp、BuildingProperties字段分类、交互方式分类、关键类速查 | Claude Opus 4.6 |
