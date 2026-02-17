---
标题：Def类型总览与DefDatabase机制
版本号: v1.0
更新日期: 2026-02-16
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未锁定]
摘要: RimWorld所有Def类型的分类速查、DefDatabase泛型数据库机制、DefOf静态引用模式的详细技术参考。
---

# Def类型总览与DefDatabase机制

## 1. Def继承体系

### 1.1 基类链

```
Editable（基础可编辑对象）
└── Def（所有定义的基类）
    ├── BuildableDef（可建造物中间层）
    │   ├── ThingDef（物品/建筑/Pawn/投射物）
    │   └── TerrainDef（地形）
    └── 其他170+种直接子类
```

### 1.2 Editable基类

最底层基类，仅定义三个虚方法：

| 方法 | 用途 |
|------|------|
| `PostLoad()` | XML反序列化完成后的后处理 |
| `ResolveReferences()` | 解析交叉引用 |
| `ConfigErrors()` | 配置错误检查 |

### 1.3 Def基类字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `defName` | string | 唯一标识符，默认"UnnamedDef"，仅允许`[a-zA-Z0-9\-_]` |
| `label` | string | 玩家可见名称（需翻译） |
| `description` | string | 详细描述（需翻译） |
| `modExtensions` | List\<DefModExtension\> | 模组扩展数据 |
| `shortHash` | ushort | 短哈希（存档序列化用） |
| `index` | ushort | DefDatabase内索引 |
| `modContentPack` | ModContentPack | 来源模组 |
| `generated` | bool | 是否为隐含生成 |

### 1.4 BuildableDef中间层

在Def和ThingDef/TerrainDef之间，增加了建造相关字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `statBases` | List\<StatModifier\> | 基础属性值 |
| `costList` | List\<ThingDefCountClass\> | 建造材料 |
| `costStuffCount` | int | 材质材料数量 |
| `stuffCategories` | List\<StuffCategoryDef\> | 可用材质类别 |
| `researchPrerequisites` | List\<ResearchProjectDef\> | 科研前置 |
| `designationCategory` | DesignationCategoryDef | 建筑菜单分类 |
| `placeWorkers` | List\<Type\> | 放置验证器 |
| `terrainAffordanceNeeded` | TerrainAffordanceDef | 地形需求 |
| `altitudeLayer` | AltitudeLayer | 渲染层级 |
| `passability` | Traversability | 通行性 |

## 2. 完整Def类型分类速查

### 2.1 实体定义类

| Def类型 | 命名空间 | 定义对象 | 对应实例类 |
|---------|---------|---------|-----------|
| ThingDef | Verse | 物品/建筑/Pawn/投射物 | Thing及子类 |
| TerrainDef | Verse | 地形 | — |
| PawnKindDef | Verse | Pawn种类变体 | —（生成参数） |
| WorldObjectDef | RimWorld | 世界地图对象 | WorldObject |

### 2.2 状态与效果类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| HediffDef | Verse | 健康状态/伤口/疾病/植入体 |
| GeneDef | Verse | 基因 |
| TraitDef | RimWorld | 特性 |
| ThoughtDef | RimWorld | 想法/心情修正 |
| NeedDef | RimWorld | 需求 |
| InspirationDef | RimWorld | 灵感 |
| MentalStateDef | Verse | 精神状态 |
| MentalBreakDef | Verse | 精神崩溃 |

### 2.3 行为与AI类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| JobDef | Verse | 行为定义 |
| DutyDef | Verse.AI | 职责定义（Lord系统） |
| WorkTypeDef | Verse | 工作类型 |
| WorkGiverDef | RimWorld | 工作给予器 |
| ThinkTreeDef | Verse | 决策树 |

### 2.4 能力与战斗类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| AbilityDef | RimWorld | 能力 |
| DamageDef | Verse | 伤害类型 |
| ManeuverDef | Verse | 近战招式 |
| ToolCapacityDef | Verse | 工具能力 |
| WeaponClassDef | — | 武器分类 |

### 2.5 属性与配方类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| StatDef | RimWorld | 属性定义 |
| StatCategoryDef | RimWorld | 属性分类 |
| RecipeDef | Verse | 配方/手术 |
| ResearchProjectDef | Verse | 科研项目 |

### 2.6 派系与事件类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| FactionDef | RimWorld | 派系 |
| IncidentDef | RimWorld | 事件 |
| RaidStrategyDef | RimWorld | 袭击策略 |
| PawnsArrivalModeDef | RimWorld | 到达模式 |
| QuestScriptDef | RimWorld | 任务脚本 |
| StorytellerDef | RimWorld | 叙事者 |

### 2.7 视觉与音频类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| EffecterDef | Verse | 效果编排器 |
| FleckDef | Verse | 轻量粒子 |
| SoundDef | Verse | 音效 |
| SongDef | Verse | 音乐 |
| ShaderTypeDef | Verse | 着色器类型 |
| PawnRenderTreeDef | Verse | Pawn渲染树 |

### 2.8 世界与地图类

| Def类型 | 命名空间 | 定义对象 |
|---------|---------|---------|
| BiomeDef | RimWorld | 生态群系 |
| RoadDef | RimWorld | 道路 |
| RiverDef | RimWorld | 河流 |
| MapGeneratorDef | Verse | 地图生成器 |
| GenStepDef | Verse | 地图生成步骤 |
| WeatherDef | Verse | 天气 |
| GameConditionDef | Verse | 游戏条件（如日食） |

### 2.9 DLC专属类

| Def类型 | DLC | 定义对象 |
|---------|-----|---------|
| RoyalTitleDef | Royalty | 皇室头衔 |
| RoyalTitlePermitDef | Royalty | 皇室许可 |
| MemeDef | Ideology | 意识形态模因 |
| PreceptDef | Ideology | 戒律 |
| RitualBehaviorDef | Ideology | 仪式行为 |
| XenotypeDef | Biotech | 异种类型 |
| MutantDef | Anomaly | 变异体 |
| PsychicRitualDef | Anomaly | 心灵仪式 |

## 3. DefDatabase\<T\> 机制详解

### 3.1 内部数据结构

```csharp
public static class DefDatabase<T> where T : Def, new()
{
    private static List<T> defsList;                    // 有序列表
    private static Dictionary<string, T> defsByName;    // defName索引
    private static Dictionary<ushort, T> defsByShortHash; // shortHash索引（存档用）
}
```

### 3.2 注册流程（AddAllInMods）

1. 按模组`OverwritePriority`排序，再按`LoadOrder`排序
2. 遍历每个模组的Def
3. 同模组内defName重复 → 报错跳过
4. 跨模组defName重复 → **后加载的覆盖先加载的**（移除旧Def，添加新Def）
5. defName为"UnnamedDef" → 自动生成随机名称并报错

### 3.3 查询API

```csharp
// 按defName查询（推荐用DefOf代替）
ThingDef steel = DefDatabase<ThingDef>.GetNamed("Steel");
ThingDef maybe = DefDatabase<ThingDef>.GetNamedSilentFail("MayNotExist");

// 遍历
foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) { ... }

// 数量
int count = DefDatabase<ThingDef>.DefCount;

// 存档加载用
ThingDef def = DefDatabase<ThingDef>.GetByShortHash(hash);
```

### 3.4 ResolveAllReferences调用顺序

硬编码的优先顺序（`PlayDataLoader`中）：

```
1. ThingCategoryDef  （并行）— 被大量Def引用
2. RecipeDef         （并行）— ThingDef.AllRecipes依赖
3. 其他Def类型       （按类型遍历）
4. ThingDef          （最后）— 依赖最多
```

## 4. DefOf静态引用模式

### 4.1 基本用法

```csharp
[DefOf]
public static class ThingDefOf
{
    public static ThingDef Human;        // 字段名 = defName
    public static ThingDef Steel;

    [DefAlias("MeleeWeapon_Longsword")]  // 字段名 ≠ defName
    public static ThingDef Longsword;

    [MayRequire("Ludeon.RimWorld.Biotech")]  // DLC条件
    public static ThingDef Mech_Centipede;

    static ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}
```

### 4.2 绑定时机

- `RebindAllDefOfs(earlyTry=true)`：第一轮，静默模式
- `RebindAllDefOfs(earlyTry=false)`：第二轮，报错模式
- 两轮都在`PlayDataLoader.DoPlayLoad()`中执行
- **Mod构造函数中DefOf为null**，`[StaticConstructorOnStartup]`中可用

### 4.3 自定义DefOf

模组可创建自己的DefOf类：

```csharp
[DefOf]
public static class MyDefOf
{
    public static ThingDef MyCustomWeapon;
    public static HediffDef MyCustomHediff;
    public static AbilityDef MyCustomAbility;

    static MyDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(MyDefOf));
    }
}
```

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-16 | 创建文档：Def继承体系、170+种Def分类速查、DefDatabase机制、DefOf模式。基于Def/DefDatabase/DefOfHelper源码研究 | Claude Opus 4.6 |
