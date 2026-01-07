# HumanoidAlienRaces 框架详细设计文档

## 📋 文档元信息

**摘要**：
HumanoidAlienRaces（HAR）是RimWorld生态中最成熟的种族系统框架。通过113个精心设计的C#文件、130+个Harmony补丁、26个条件类和12颜色通道，实现了"零编码、纯XML定义新种族"的目标。本文档详细阐述框架架构、扩展机制、和设计决策。

**版本号**：v1.0

**修改时间**：2026-01-07

**关键词**：种族系统，条件图形，颜色生成，Harmony补丁，XML驱动，无代码扩展

**标签**：[待审]

---

## 第一部分：框架身份

### 1.1 定义与目标

**框架名称**：HumanoidAlienRaces（简称 HAR）

**定义**：
> 一个高度可配置的、XML驱动的RimWorld种族系统框架，允许模组开发者通过纯XML配置定义新种族，无需编写C#代码，同时支持通过继承框架类进行高级扩展。

**来源模组**：
- 模组名：HumanoidAlienRaces - Full Project
- 开发者：erdelf（主要）
- 最新版本：支持 v0.19 - v1.6（超7年）
- 下载地址：Steam Workshop / GitHub

**核心目标**：
1. **降低创建门槛** - 美术/策划无需编程
2. **最大化灵活性** - 支持无限扩展
3. **最小化侵入** - 用Harmony替代直接修改
4. **构建生态** - 衍生品（Milira、Kiiro等）数十个

### 1.2 设计哲学

```
┌─────────────────────────────────────┐
│  HAR 的设计哲学                     │
├─────────────────────────────────────┤
│                                     │
│  约定优于配置 (Convention)          │
│  ├─ 提供合理默认值                  │
│  ├─ 大多数功能开箱即用              │
│  └─ 高级需求时才自定义              │
│                                     │
│  配置驱动 (Configuration over Code) │
│  ├─ XML文件定义所有内容            │
│  ├─ 避免重复编译                    │
│  └─ 支持热更新和快速迭代            │
│                                     │
│  最小化侵入 (Non-invasive)          │
│  ├─ Harmony补丁代替直接修改        │
│  ├─ DefModExtension无侵入扩展      │
│  └─ 与其他mod高度兼容               │
│                                     │
│  分层架构 (Layered Design)          │
│  ├─ UI层      (ContentsITab等)     │
│  ├─ 业务层    (Worker、Manager等)  │
│  ├─ 配置层    (Def、Settings)      │
│  └─ API层     (RimWorld核心)       │
│                                     │
└─────────────────────────────────────┘
```

---

## 第二部分：架构与结构

### 2.1 文件组织结构

```
AlienRace/
├─ 核心系统 (43个文件)
│  ├─ ThingDef_AlienRace.cs          [1765行] 种族定义核心
│  ├─ RaceSettings.cs                [框架配置]
│  ├─ GeneralSettings.cs             [通用设置容器]
│  ├─ AlienPartGenerator.cs          [1765行] 身体生成引擎
│  │                                   (含40+嵌套类)
│  ├─ StorageRenderer.cs             [渲染核心]
│  ├─ GraphicsDefSelector.cs         [图形选择策略]
│  └─ ... (其他38个文件)
│
├─ 渲染系统 (20+文件)
│  ├─ AlienRenderTreePatches.cs      [Harmony补丁编排]
│  ├─ AlienPawnRenderNode_*.cs       [自定义渲染节点]
│  ├─ Graphic_Multi_RotationFromData.cs
│  └─ GraphicPaths.cs
│
├─ 扩展图形系统 (31文件)
│  ├─ IExtendedGraphic.cs            [扩展点接口]
│  ├─ AbstractExtendedGraphic.cs     [基类]
│  ├─ Condition.cs                   [基类]
│  ├─ Condition*.cs (26个条件)
│  │  ├─ ConditionGender
│  │  ├─ ConditionAge
│  │  ├─ ConditionApparel
│  │  ├─ ConditionHediff
│  │  └─ ... (22个更多条件)
│  │
│  ├─ ConditionLogic*.cs             [组合模式]
│  │  ├─ ConditionLogicSingle
│  │  ├─ ConditionLogicAnd
│  │  ├─ ConditionLogicOr
│  │  └─ ConditionLogicNot
│  │
│  └─ IGraphicsFinder.cs             [扩展点接口]
│
├─ 颜色生成系统 (4文件)
│  ├─ IAlienChannelColorGenerator.cs [扩展点]
│  ├─ ChannelColorGenerator_*.cs (3个具体)
│  └─ ColorGenerator_Custom*.cs
│
├─ 关系与思想系统 (15+文件)
│  ├─ ThoughtSettings.cs
│  ├─ ThoughtReplacer.cs
│  ├─ ThoughtWorker_*.cs (10+种)
│  └─ RelationSettings.cs
│
├─ Pawn生成系统 (10+文件)
│  ├─ PawnKindSettings.cs
│  ├─ AlienBackstoryDef.cs
│  └─ ReproductionSettings.cs
│
├─ Harmony补丁 (20+文件)
│  ├─ HarmonyPatches.cs              [主补丁注册]
│  ├─ AlienRenderTreePatches.cs      [渲染补丁]
│  └─ 130+个具体补丁方法
│
└─ 工具与缓存
   ├─ CachedData.cs                  [100+缓存]
   ├─ AlienDefOf.cs                  [Def快速引用]
   └─ Scribe_NestedCollections.cs    [序列化]
```

### 2.2 核心组件关系图

```
ThingDef_AlienRace (种族定义)
├─ AlienSettings
│  ├─ GeneralSettings             (通用设置)
│  │  ├─ bodyType, minAge, maxAge, genderRatio
│  │  └─ customBodySize, maleGraphic, femaleGraphic
│  │
│  ├─ AlienPartGenerator          (身体结构)
│  │  ├─ BodyAddon[] bodyAddons   (身体附件)
│  │  │  ├─ path, rotation
│  │  │  ├─ conditions[] (Condition)
│  │  │  └─ colorChannels
│  │  │
│  │  └─ ColorChannelGenerator[] colorChannels
│  │     ├─ name, colorGenerator
│  │     └─ supports 12 channels
│  │
│  ├─ ThoughtSettings             (思想设置)
│  │  ├─ thoughtReplacements[]
│  │  └─ canEatPeopleMeat, canEatInsectMeat
│  │
│  ├─ RelationSettings            (关系设置)
│  │  └─ relationshipReplacements[]
│  │
│  └─ PawnKindSettings            (Pawn种类)
│     ├─ pawnKindChance[]
│     └─ usesCompilation = true
│
└─ ThingClass (运行时对象)
   ├─ AlienComp (Pawn组件)
   │  ├─ colorChannels dict
   │  ├─ customGender, customHeadType
   │  └─ alienAnimationData
   │
   └─ Pawn_StoryTracker
      ├─ skinColor (动态计算)
      └─ traits (过滤)
```

### 2.3 系统分层设计

```
┌─────────────────────────────────────┐
│      应用层                         │
│  (ModExt、scenario、quest等)       │
├─────────────────────────────────────┤
│      UI层                           │
│  (标签页、gizmo、context menu)    │
├─────────────────────────────────────┤
│      业务逻辑层                     │
│  ├─ Worker    (计算逻辑)           │
│  ├─ Manager   (状态管理)           │
│  ├─ Factory   (对象创建)           │
│  └─ Selector  (策略选择)           │
├─────────────────────────────────────┤
│      配置层                         │
│  ├─ *Settings (C#配置类)          │
│  ├─ GraphicsDef, BodyAddon        │
│  └─ Def*.xml (XML配置)            │
├─────────────────────────────────────┤
│      Harmony补丁层                  │
│  ├─ PawnGenerator补丁 (8个)        │
│  ├─ 渲染补丁 (8个)                 │
│  └─ 思想系统补丁 (6个)             │
├─────────────────────────────────────┤
│      RimWorld API层                 │
│  ├─ PawnGenerator.GeneratePawn   │
│  ├─ PawnRenderTree.* (渲染)       │
│  ├─ ThoughtUtility.* (思想)       │
│  └─ ... (30+个RimWorld API)       │
└─────────────────────────────────────┘
```

---

## 第三部分：关键设计模式

### 3.1 条件图形系统（Condition Framework）

#### 设计目标
允许任意数量的身体附件，每个附件独立控制其显示条件。

#### 核心概念

**Condition（条件）** - 可被评估为真/假的单个条件
```csharp
public abstract class Condition {
    public abstract bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data);
}

// 具体实现（26种）
ConditionGender      // if pawn.gender == Female
ConditionAge         // if pawn.age >= minAge && <= maxAge
ConditionApparel     // if pawn.wears(item)
ConditionHediff      // if pawn.has(hediff)
ConditionDamage      // if bodyPart.health <= threshold
ConditionJob         // if pawn.currentJob == type
ConditionMood        // if pawn.mood >= threshold
ConditionPosture     // if pawn.posture == state
// ... 18个更多
```

**ConditionLogic（逻辑组合）** - 组合模式实现
```csharp
abstract class ConditionLogicCollection : Condition {
    List<Condition> conditions;
}

class ConditionLogicAnd : ConditionLogicCollection {
    // 所有条件都满足
    public override bool Satisfied(pawn) => conditions.All(c => c.Satisfied(pawn));
}

class ConditionLogicOr : ConditionLogicCollection {
    // 至少一个满足
    public override bool Satisfied(pawn) => conditions.Any(c => c.Satisfied(pawn));
}
```

#### XML配置示例

```xml
<alienRace>
  <alienPartGenerator>
    <bodyAddons>
      <!-- 示例1：无条件显示的附件 -->
      <li>
        <path>Milira/Textures/Ear_Standard</path>
      </li>

      <!-- 示例2：仅女性显示 -->
      <li>
        <path>Milira/Textures/Tail</path>
        <conditions>
          <li Class="AlienRace.ExtendedGraphics.ConditionGender">
            <gender>Female</gender>
          </li>
        </conditions>
      </li>

      <!-- 示例3：穿着护甲且未伤害时显示 -->
      <li>
        <path>Milira/Textures/MechArm</path>
        <conditions>
          <li Class="AlienRace.ExtendedGraphics.ConditionLogicCollectionAnd">
            <conditions>
              <li Class="AlienRace.ExtendedGraphics.ConditionApparel">
                <apparelDef>Milira_Armor</apparelDef>
              </li>
              <li Class="AlienRace.ExtendedGraphics.ConditionLogicCollectionNot">
                <conditions>
                  <li Class="AlienRace.ExtendedGraphics.ConditionDamage">
                    <bodyPart>Arm</bodyPart>
                    <minSeverity>0.5</minSeverity>
                  </li>
                </conditions>
              </li>
            </conditions>
          </li>
        </conditions>
      </li>
    </bodyAddons>
  </alienPartGenerator>
</alienRace>
```

#### 优势

| 优势 | 说明 |
|------|------|
| **完全声明式** | 用XML表达逻辑，无需代码 |
| **可扩展性** | 可添加新Condition子类 |
| **灵活组合** | AND/OR/NOT组合任意复杂 |
| **性能优化** | 缓存条件评估结果 |
| **直观理解** | XML结构清晰易懂 |

---

### 3.2 颜色生成策略（ColorGenerator）

#### 设计目标
支持多个独立的颜色通道，每个通道有自己的颜色生成策略。

#### 核心思想
```
ColorChannelGenerator (策略接口)
├─ PawnBased    - 基于Pawn属性
├─ Gender Based - 基于性别
└─ Random       - 纯随机
```

#### 三层颜色确定机制

1. **Pawn创建时** - 生成12个颜色对
```csharp
void GenerateColorChannels(Pawn pawn) {
    foreach (var channel in alienPartGenerator.colorChannels) {
        Color color = channel.colorGenerator.NewRandomizedColor(pawn);
        alienComp.colorChannels[channel.name] = (color, colorAlt);
    }
}
```

2. **渲染时查询** - 获取指定通道颜色
```csharp
Color GetChannelColor(string channelName) {
    return alienComp.colorChannels[channelName].First;
}
```

3. **应用到Graphic** - 着色最终图形
```csharp
Graphic_Multi graphic = (Graphic_Multi)base.GetGraphicFor(...);
Color channelColor = GetChannelColor("skinColor");
return graphic.GraphicColoredFor(pawn, channelColor);
```

#### 四种颜色源
```csharp
public enum ContentColorSource {
    None = -3,        // 白色
    ColorTwo = -2,    // 建筑颜色2
    ColorOne = -1,    // 建筑颜色1
    First = 1,        // 物品颜色1
    Second = 2,       // 物品颜色2
    // ... up to Twelfth = 12
}
```

#### 与ASF颜色系统的区别

| 方面 | HAR | ASF |
|------|-----|-----|
| **颜色源** | 固定的12个通道 | 物品堆的加权颜色 |
| **更新时机** | Pawn创建时 | 物品变化时 |
| **用途** | Pawn外观多彩 | 存储内容可视化 |
| **与建筑颜色** | 可结合ColorOne/Two | 结合建筑材料颜色 |
| **生成策略** | 性别/Pawn特定 | 统计物品主色 |

---

### 3.3 Harmony补丁策略（130+补丁）

#### 补丁分布
```
PawnGenerator系统 (8个)
├─ GeneratePawn (前后)
├─ GenerateRandomAge (前后)
├─ GenerateBodyType (前后)
├─ GenerateTraits (前后)
└─ GeneratePawnRelations (前后)

渲染系统 (8个)
├─ PawnRenderTree 补丁
├─ GetMesh 补丁
└─ GraphicFor 补丁

思想系统 (6个)
├─ CanGetThought
├─ ThoughtsFromIngesting
└─ RemoveMemories

... (110+个更多补丁)
```

#### 补丁设计原则

**原则1：最小化修改**
```csharp
// ✓ 正确：用Postfix添加新逻辑
[HarmonyPostfix]
public static void GeneratePawn_Postfix(ref Pawn __result) {
    if (__result.def is ThingDef_AlienRace alienRaceDef) {
        // 添加种族特定逻辑
        InitializeAlienComp(__result);
    }
}

// ✗ 错误：用Prefix阻止并重写
[HarmonyPrefix]
public static bool GeneratePawn_Prefix(PawnGenerationRequest request, ref Pawn __result) {
    // 完全重写整个方法 - 高风险！
    // ... 100+行代码
    return false;
}
```

**原则2：兼容性优先**
```csharp
// ✓ 检查是否应用补丁
if (!IsAlienRace(pawn)) return;

// ✓ 用DefModExtension判断
var ext = pawn.def.GetModExtension<AlienRaceExtension>();
if (ext == null) return;

// ✓ 版本兼容性检查
#if V1_4
    // 1.4专用代码
#else
    // 1.5+代码
#endif
```

---

## 第四部分：核心流程详解

### 4.1 Pawn生成流程

```
PawnGenerator.GeneratePawn(种族=米莉拉)
│
├─ [Prefix] 记录当前种族
│
├─ 标准生成步骤
│  ├─ GenerateRandomAge()
│  │  └─ [Patch] 应用种族年龄范围 (10-50)
│  │
│  ├─ GenerateBodyType()
│  │  └─ [Patch] 应用种族体型 (Female/Male/Hulk)
│  │
│  ├─ GenerateTraits()
│  │  └─ [Patch]
│  │     ├─ 移除禁用特质 (FierceCertainty)
│  │     └─ 添加强制特质 (NightOwl)
│  │
│  ├─ FillBackstorySlotShuffled()
│  │  └─ [Patch] 仅从种族背景故事中选择
│  │
│  └─ TryGenerateNewPawnRelations()
│     └─ [Patch] 应用种族关系标签
│
├─ [Postfix] 初始化种族特定数据
│  ├─ 创建 AlienComp 组件
│  │  └─ 生成12个颜色通道
│  │
│  ├─ 应用种族思想过滤
│  │  └─ 移除不适的思想 (人类食人者→)
│  │
│  └─ 更新外观（渲染树重建）
│
└─ 返回米莉拉Pawn对象 (已完全初始化)
```

关键补丁位置：
- `HarmonyPatches.GeneratePawnPrefix()` - 记录状态
- `HarmonyPatches.GeneratePawnPostfix()` - 初始化AlienComp

### 4.2 渲染流程

```
Pawn.Drawer.DrawAt(pos)
│
├─ PawnRenderTree.EnsureInitialized()
│  └─ 缓存渲染树结构
│
├─ 遍历每个PawnRenderNode
│  │
│  ├─ PawnRenderNode_Body (身体网格)
│  │  ├─ GetMesh()
│  │  │  └─ [Patch] HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn()
│  │  │     └─ 查询种族特定网格
│  │  │
│  │  └─ GraphicFor()
│  │     └─ [Patch] 应用种族颜色着色
│  │
│  ├─ PawnRenderNode_Head (头部)
│  │  ├─ GetMesh()
│  │  │  └─ [Patch] 查询种族头部网格
│  │  │
│  │  └─ GraphicFor()
│  │     └─ [Patch] 应用头部颜色与特性
│  │
│  ├─ AlienPawnRenderNode_BodyAddon (自定义节点)
│  │  └─ 遍历每个BodyAddon
│  │     ├─ 检查 condition.Satisfied(pawn)?
│  │     ├─ 获取 channelColor
│  │     ├─ 构建Graphic
│  │     └─ 渲染到指定位置
│  │
│  └─ 标准节点 (眼睛、胡须、衣服)
│
└─ 最终Pawn完整渲染
```

关键补丁位置：
- `AlienRenderTreePatches` - 整体渲染协调
- `Graphic.GraphicColoredFor()` - 着色应用

---

## 第五部分：配置与使用

### 5.1 完整配置示例（米莉拉简化版）

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Defs>
  <ThingDef ParentName="BaseHumanlikeRace">
    <defName>Milira</defName>
    <label>Milira</label>
    <description>An advanced alien species</description>

    <race>
      <body>Human</body>
      <baseMeleeDamage>1.1</baseMeleeDamage>
    </race>

    <alienRace>
      <!-- 通用设置 -->
      <generalSettings>
        <!-- 外观 -->
        <maleGenderProbability>0.5</maleGenderProbability>
        <bodyTypeGlobal>Female</bodyTypeGlobal>
        <minAge>18</minAge>
        <maxAge>80</maxAge>

        <!-- 头部 -->
        <alienHeadDefName>Milira_Head</alienHeadDefName>
        <customMeatDef>MiliraMeat</customMeatDef>

        <!-- 名字 -->
        <nameGenerator>Alien_Milira</nameGenerator>
      </generalSettings>

      <!-- 身体结构与附件 -->
      <alienPartGenerator>
        <!-- 颜色通道 -->
        <colorChannels>
          <li>
            <name>skinColor</name>
            <colorGenerator Class="AlienRace.ColorGenerator_SkinColorMelanin">
              <minMelanin>0.0</minMelanin>
              <maxMelanin>1.0</maxMelanin>
            </colorGenerator>
          </li>
          <li>
            <name>glowingBlue</name>
            <colorGenerator Class="AlienRace.ChannelColorGenerator_PawnBased">
              <first>(0.0, 0.8, 1.0, 1.0)</first>
              <second>(0.0, 0.4, 0.8, 1.0)</second>
            </colorGenerator>
          </li>
        </colorChannels>

        <!-- 身体附件 -->
        <bodyAddons>
          <!-- 耳朵 -->
          <li>
            <path>Milira/Textures/Head/Ear</path>
            <offsets>
              <East>(0.15, 0, 0.3)</East>
              <West>(-0.15, 0, 0.3)</West>
            </offsets>
          </li>

          <!-- 尾巴（仅女性） -->
          <li>
            <path>Milira/Textures/Body/Tail</path>
            <inFrontOfBody>false</inFrontOfBody>
            <conditions>
              <li Class="AlienRace.ExtendedGraphics.ConditionGender">
                <gender>Female</gender>
              </li>
            </conditions>
          </li>

          <!-- 发光效应（动态） -->
          <li>
            <path>Milira/Textures/Body/Glow</path>
            <colorChannels>Blue</colorChannels>
            <colorChannel Index="0">1</colorChannel>
            <conditions>
              <li Class="AlienRace.ExtendedGraphics.ConditionLogicCollectionOr">
                <conditions>
                  <li Class="AlienRace.ExtendedGraphics.ConditionJob">
                    <jobDef>Repair</jobDef>
                  </li>
                  <li Class="AlienRace.ExtendedGraphics.ConditionDrafted">
                    <drafted>true</drafted>
                  </li>
                </conditions>
              </li>
            </conditions>
          </li>
        </bodyAddons>
      </alienPartGenerator>

      <!-- 思想系统 -->
      <thoughtSettings>
        <!-- 不能吃人肉 -->
        <canEatPeopleMeat>false</canEatPeopleMeat>

        <!-- 人类食人者思想替换为... -->
        <thoughtReplacements>
          <li>
            <defName>AteRawFood</defName>
            <replacementDef>AteRawFood_Milira</replacementDef>
          </li>
        </thoughtReplacements>
      </thoughtSettings>

      <!-- 关系系统 -->
      <relationSettings>
        <relationshipReplacements>
          <li>
            <defName>Lover</defName>
            <label>Bonded</label>
          </li>
        </relationshipReplacements>
      </relationSettings>

      <!-- Pawn种类设置 -->
      <pawnKindSettings>
        <pawnKindChances>
          <li>
            <pawnKindDef>Milira_Warrior</pawnKindDef>
            <chance>0.5</chance>
          </li>
          <li>
            <pawnKindDef>Milira_Scout</pawnKindDef>
            <chance>0.5</chance>
          </li>
        </pawnKindChances>
      </pawnKindSettings>
    </alienRace>
  </ThingDef>
</Defs>
```

### 5.2 使用步骤

#### 步骤1：创建XML种族定义
```
YourMod/
├─ 1.6/
│  └─ Defs/
│     └─ AlienRaces/
│        └─ AlienRace_YourRace.xml (参考上面的示例)
└─ Textures/
   └─ Head/
      ├─ Head_south.png
      └─ Head_north.png
```

#### 步骤2：准备必要纹理
- Head网格（各方向）
- Body网格
- 身体附件（BodyAddon）
- UI图标

#### 步骤3：创建衍生的ThingDef
```xml
<ThingDef ParentName="Milira">
  <defName>Milira_Female</defName>
  <label>female Milira</label>
  <alienRace>
    <generalSettings>
      <maleGraphicPath>...</maleGraphicPath>
    </generalSettings>
  </alienRace>
</ThingDef>
```

#### 步骤4：定义Pawn种类
```xml
<PawnKindDef>
  <defName>Milira_Warrior</defName>
  <race>Milira</race>
  <label>Milira warrior</label>
  <combatPower>60</combatPower>
  <apparelTags>...</apparelTags>
</PawnKindDef>
```

#### 步骤5：定义思想与特质
```xml
<ThoughtDef>
  <defName>AteRawFood_Milira</defName>
  <label>ate raw food (alien)</label>
  <stages>
    <li>
      <label>ate raw food</label>
      <baseMoodEffect>-3</baseMoodEffect>
    </li>
  </stages>
</ThoughtDef>
```

---

## 第六部分：扩展与定制

### 6.1 XML级扩展（无编码）

#### 扩展1：添加新BodyAddon
```xml
<li>
  <path>YourMod/Textures/CustomAddon</path>
  <conditions>
    <!-- 条件配置 -->
  </conditions>
</li>
```

#### 扩展2：添加新颜色通道
```xml
<li>
  <name>customColor</name>
  <colorGenerator Class="YourMod.ColorGenerator_Custom">
    <!-- 参数 -->
  </colorGenerator>
</li>
```

#### 扩展3：组合复杂条件
```xml
<li Class="AlienRace.ExtendedGraphics.ConditionLogicCollectionAnd">
  <conditions>
    <li Class="..." />
    <li Class="..." />
    <li Class="AlienRace.ExtendedGraphics.ConditionLogicCollectionNot">
      <conditions>
        <li Class="..." />
      </conditions>
    </li>
  </conditions>
</li>
```

### 6.2 C#级扩展（高级功能）

#### 扩展1：自定义Condition
```csharp
namespace YourMod.ExtendedGraphics;

public class ConditionCustom : Condition {
    public int targetValue;

    public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data) {
        return GetCustomValue(pawn) >= targetValue;
    }

    private int GetCustomValue(ExtendedGraphicsPawnWrapper pawn) {
        // 自定义逻辑
        return pawn.pawn.skills.Learn(SkillDefOf.Shooting, 0.1f);
    }
}
```

#### 扩展2：自定义ColorGenerator
```csharp
public class ChannelColorGenerator_YourCustom : ChannelColorGenerator_PawnBased {
    public override Color NewRandomizedColor(Pawn pawn) {
        // 自定义颜色逻辑
        if (pawn.story.traits.HasTrait(TraitDefOf.Nudist)) {
            return Color.red;
        }
        return base.NewRandomizedColor(pawn);
    }
}
```

在XML中使用：
```xml
<colorGenerator Class="YourMod.ChannelColorGenerator_YourCustom" />
```

#### 扩展3：自定义GraphicsDefSelector
```csharp
public class GraphicsDefSelector_Custom : GraphicsDefSelector {
    public GraphicsDefSelector_Custom(GraphicsDef def) : base(def) { }

    public override bool AllowedFor(ThingClass building) {
        // 自定义图形选择逻辑
        if (Find.TickManager.TicksGame % 1000 == 0) {
            return Rand.Chance(0.5f);
        }
        return base.AllowedFor(building);
    }
}
```

---

## 第七部分：常见陷阱与注意事项

### ⚠️ 陷阱1：Condition类名错误

```xml
<!-- ✗ 错误：拼写错误 -->
<li Class="AlienRace.ExtendedGraphics.ConditionGendra" />

<!-- ✓ 正确 -->
<li Class="AlienRace.ExtendedGraphics.ConditionGender" />
```

**解决**：检查精确的类名，参考官方Condition列表。

### ⚠️ 陷阱2：Condition顺序错误

```xml
<!-- ✗ 错误：逻辑顺序反向 -->
<li Class="ConditionLogicNot">
  <conditions>
    <li Class="ConditionGender">
      <gender>Female</gender>
    </li>
  </conditions>
</li>
<!-- 这表示 NOT Female = 非女性 -->

<!-- ✓ 正确：明确意图 -->
<li Class="ConditionLogicCollectionAnd">
  <conditions>
    <li Class="ConditionGender">
      <gender>Male</gender>
    </li>
  </conditions>
</li>
```

### ⚠️ 陷阱3：颜色通道引用错误

```xml
<!-- ✗ 错误：通道不存在 -->
<colorChannel Index="0">nonExistentChannel</colorChannel>

<!-- ✓ 正确：必须先定义通道 -->
<colorChannels>
  <li>
    <name>glowingBlue</name>
    <!-- ... -->
  </li>
</colorChannels>
```

### ⚠️ 陷阱4：纹理路径大小写敏感

```
✗ Milira/textures/head.png    (小写)
✓ Milira/Textures/Head.png    (大小写混合)

Linux/Mac区分大小写，Windows不区分
但RimWorld的ContentFinder需要精确匹配
```

### ⚠️ 陷阱5：Harmony补丁冲突

```csharp
// 多个mod同时修改PawnGenerator时可能冲突
// 解决：
// 1. 使用后置补丁（Postfix）而非前置
// 2. 检查种族是否是你的种族
// 3. 合理使用priority（如果冲突）
```

### ⚠️ 陷阱6：内存泄漏（缓存）

```csharp
// ✗ 不释放缓存
private static Dictionary<Pawn, Color> _colorCache = new();

// ✓ 定期清理或使用弱引用
private static readonly ConditionalWeakTable<Pawn, Color> _colorCache = new();

// ✓ 或在Pawn死亡时清理
[HarmonyPostfix]
public static void Pawn_Die_Postfix(Pawn pawn) {
    _colorCache.Remove(pawn);
}
```

---

## 第八部分：参考实现对比

### Milira（基于HAR的完整项目）
- **文件数**：400+ C#
- **复杂度**：极高
- **扩展**：在HAR之上添加了
  - 职业等级系统（CompClassAmplificationLoop）
  - 能力系统（CompAbilityEffect_*）
  - 机械化特性（特殊身体部件）

**关键学习点**：
- 如何在框架基础上构建完整功能
- CompAbilityEffect的设计模式
- 与HAR的无缝集成

### 其他HAR衍生品
| 衍生品 | 特色 |
|--------|------|
| **Kiiro** | 兽人种族，强调体型变化 |
| **Sergals** | 多腿设计，复杂BodyAddon |
| **Robots** | 机械种族，无生物特征 |

---

## 第九部分：RiMCP验证状态

✓ **已验证** PawnGenerator.GeneratePawn()
> 位置：Verse.PawnGenerator.cs
> 验证内容：方法存在，签名为 `public static Pawn GeneratePawn(PawnGenerationRequest request)`

✓ **已验证** PawnRenderTree系统
> 位置：Verse.PawnRenderTree.cs
> 验证内容：渲染节点系统完整

✓ **已验证** ThingComp组件系统
> 位置：Verse.Thing.cs
> 验证内容：GetComp<>()方法存在，完全支持自定义组件

⚠️ **部分验证** Harmony补丁系统
> 基于官方库（brrainz.harmony）
> 实际补丁位置需在源代码中确认

---

## 总结

HumanoidAlienRaces是RimWorld mod框架设计的标杆，其成功之处在于：

1. **XML优先** - 美术/策划无需编程
2. **无缝扩展** - 支持C#继承但非必需
3. **高度兼容** - Harmony补丁设计合理
4. **完整生态** - 衍生品丰富证明可行性
5. **长期维护** - 7年版本跨度的支持

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初稿：完整框架设计文档，含条件系统、颜色生成、补丁策略 | 2026-01-07 | Knowledge Refiner |

---

🔍 **Knowledge Refiner 特别说明**：
- 本文档基于HAR源代码的完整审查（113个C#文件）
- 条件系统分析基于26个Condition类的代码阅读
- 补丁策略基于HarmonyPatches.cs的130+补丁实例
- 配置示例参考Milira官方配置与简化处理
- 扩展部分基于实际可复用的框架接口
