---
标题：XML配置系统与Def反序列化机制
版本号: v1.0
更新日期: 2026-02-16
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未锁定]
摘要: RimWorld XML到Def的完整反序列化流程、ParentName继承机制、字段类型映射规则、交叉引用延迟解析、MayRequire条件加载的技术参考。
---

# XML配置系统与Def反序列化机制

## 1. XML文件组织

### 1.1 目录结构

```
MyMod/
├── Defs/                    ← 主Def目录（递归扫描所有.xml）
│   ├── ThingDefs/           ← 子目录名随意，仅为人类组织
│   ├── HediffDefs/
│   └── AnyName.xml          ← 文件名随意，可混放不同Def类型
├── 1.5/Defs/                ← 版本特定Def（优先于根Defs/）
├── Patches/                 ← XPath补丁（修改其他模组/原版Def）
└── Languages/               ← 本地化
```

**关键规则**：
- 目录名和文件名**不影响**Def加载，游戏递归扫描所有`.xml`
- 一个XML文件可包含**多种Def类型**
- 版本目录（如`1.5/`）下的Def**覆盖**根目录同名Def

### 1.2 XML基本格式

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- 根节点名 = Def类型名 -->
  <ThingDef>
    <defName>MyItem</defName>
    <label>my item</label>
    <!-- ... -->
  </ThingDef>

  <!-- 同一文件可定义多个Def -->
  <HediffDef>
    <defName>MyHediff</defName>
    <!-- ... -->
  </HediffDef>
</Defs>
```

## 2. 反序列化流程

### 2.1 完整管线

```
LoadModXML()
  → 读取所有模组的Defs/*.xml文件
  ↓
CombineIntoUnifiedXML()
  → 合并为统一XML文档
  ↓
ApplyPatches()
  → 应用Patches/目录下的XPath补丁
  ↓
XmlInheritance.TryRegisterAllFrom()
  → 注册所有XML节点到继承系统
  ↓
XmlInheritance.Resolve()
  → 解析ParentName继承，合并父子节点
  ↓
ParseAndProcessXML()
  → 对每个Def节点调用DirectXmlToObject.ObjectFromXml<T>()
  ↓
Def对象创建完毕（Def引用字段仍为null）
  ↓
DefDatabase<T>.AddAllInMods()
  → 注册到各类型数据库
  ↓
DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences()
  → 解析所有Def间交叉引用（两轮）
  ↓
DefDatabase<T>.ResolveAllReferences()
  → 调用每个Def的ResolveReferences()
```

### 2.2 DirectXmlToObject核心逻辑

`ObjectFromXml<T>()`的处理优先级：

1. `IsNull="true"` → 返回null/default
2. 有`LoadDataFromXmlCustom`方法 → 调用自定义加载
3. SlateRef类型 → ParseHelper解析
4. 纯文本节点 → ParseHelper.FromString解析
5. CDATA节点 → 仅string类型
6. `[Flags]`枚举 → 列表解析后按位OR
7. `List<T>` → ListFromXml递归
8. `Dictionary<K,V>` → DictionaryFromXml递归
9. 复合对象 → 逐字段递归解析
   - Def类型字段 → **延迟注册**到CrossRefLoader
   - 其他类型字段 → 递归ObjectFromXml

## 3. 字段类型映射

### 3.1 基本类型

| C#类型 | XML写法 | 示例 |
|--------|---------|------|
| int | 整数 | `<stackLimit>75</stackLimit>` |
| float | 小数 | `<fillPercent>0.5</fillPercent>` |
| bool | true/false | `<useHitPoints>true</useHitPoints>` |
| string | 文本 | `<label>steel</label>` |
| Color | (R,G,B,A) | `<color>(0.5,0.5,0.5,1)</color>` |
| IntVec2 | (x,z) | `<size>(2,1)</size>` |
| IntVec3 | (x,y,z) | `<interactionCellOffset>(0,0,-1)</interactionCellOffset>` |
| Vector2 | (x,y) | `<uiIconOffset>(0,0.03)</uiIconOffset>` |
| FloatRange | min~max | `<startingHpRange>0.5~1.0</startingHpRange>` |
| IntRange | min~max | `<deepLumpSizeRange>2~12</deepLumpSizeRange>` |
| Type | 完整类名 | `<thingClass>Building_Door</thingClass>` |

### 3.2 枚举

```xml
<!-- 普通枚举：直接写枚举值名 -->
<category>Item</category>
<techLevel>Industrial</techLevel>
<tickerType>Rare</tickerType>

<!-- [Flags]枚举：用列表表示按位OR -->
<wornGraphicFlags>
  <li>Head</li>
  <li>Body</li>
</wornGraphicFlags>
```

### 3.3 Def引用

```xml
<!-- 单个Def引用：值为defName字符串 -->
<defaultStuff>WoodLog</defaultStuff>
<filthLeaving>Filth_RubbleBuilding</filthLeaving>

<!-- Def引用列表 -->
<researchPrerequisites>
  <li>Smithing</li>
  <li>Machining</li>
</researchPrerequisites>

<!-- 条件Def引用 -->
<defaultStuff MayRequire="Ludeon.RimWorld.Biotech">ArchitePlasma</defaultStuff>
```

### 3.4 列表

```xml
<!-- 简单列表 -->
<thingCategories>
  <li>ResourcesRaw</li>
  <li>Manufactured</li>
</thingCategories>

<!-- 复合对象列表 -->
<tools>
  <li>
    <label>handle</label>
    <capacities>
      <li>Blunt</li>
    </capacities>
    <power>9</power>
    <cooldownTime>2</cooldownTime>
  </li>
</tools>

<!-- 多态列表（Class属性） -->
<comps>
  <li Class="CompProperties_Forbiddable"/>
  <li Class="CompProperties_Glower">
    <glowRadius>5</glowRadius>
  </li>
</comps>
```

### 3.5 StatModifier特殊格式

```xml
<!-- 标签名 = StatDef.defName，值 = 数值 -->
<statBases>
  <MaxHitPoints>100</MaxHitPoints>
  <MarketValue>1.9</MarketValue>
  <Mass>0.3</Mass>
  <WorkToMake>2000</WorkToMake>
</statBases>

<equippedStatOffsets>
  <MoveSpeed>-0.1</MoveSpeed>
</equippedStatOffsets>
```

### 3.6 字典

```xml
<!-- Dictionary<K,V>格式 -->
<myDict>
  <li>
    <key>KeyValue</key>
    <value>123</value>
  </li>
</myDict>
```

## 4. ParentName继承机制

### 4.1 基本语法

```xml
<!-- 定义模板（Name属性 = 被引用的标识） -->
<ThingDef Name="BaseGun" Abstract="True">
  <thingClass>ThingWithComps</thingClass>
  <category>Item</category>
  <equipmentType>Primary</equipmentType>
</ThingDef>

<!-- 继承模板（ParentName属性 = 引用的Name） -->
<ThingDef ParentName="BaseGun">
  <defName>Gun_Rifle</defName>
  <label>rifle</label>
  <!-- 继承了thingClass/category/equipmentType -->
  <!-- 可覆盖或追加字段 -->
</ThingDef>
```

### 4.2 继承规则

| 规则 | 说明 |
|------|------|
| 字段覆盖 | 子节点同名字段**替换**父节点字段 |
| 字段追加 | 子节点独有字段**追加**到结果 |
| 列表替换 | 列表字段（如comps）**整体替换**，不合并 |
| 多级继承 | 支持A→B→C链式继承，递归解析 |
| Abstract | `Abstract="True"`的Def不注册到DefDatabase |
| 跨模组 | 模组可继承原版或其他模组的模板 |

### 4.3 原版常用继承链

```
BaseThingBugged（最基础）
├── BaseThing
│   ├── BaseResource → 资源类物品
│   ├── BaseMeleeWeapon_Sharp → 锋利近战武器
│   ├── BaseMeleeWeapon_Blunt → 钝器近战武器
│   ├── BaseHumanMakeableGun → 可制造枪械
│   │   └── BaseGun → 枪械
│   └── BaseApparel → 衣物
├── BasePawn → Pawn基类
│   ├── BaseAnimal → 动物
│   └── BaseMechanoid → 机械体
└── BaseBuildingNonEdifice → 非建筑物
    └── BaseBuilding → 建筑
```

## 5. 交叉引用延迟解析

### 5.1 为什么需要延迟

XML反序列化时，被引用的Def可能尚未加载（取决于模组加载顺序和文件扫描顺序）。因此Def类型字段不能立即解析。

### 5.2 注册与解析

```
反序列化阶段：
  遇到Def类型字段 → DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef()
  → 记录"对象X的字段Y想要引用defName=Z的Def"

解析阶段（两轮）：
  第一轮（Silent）→ 尝试解析，找不到不报错（隐含Def尚未生成）
  DefGenerator.GenerateImpliedDefs_PreResolve() → 生成隐含Def
  第二轮（LogErrors）→ 再次解析，找不到报错
```

### 5.3 MayRequire条件加载

```xml
<!-- 字段级 -->
<defaultStuff MayRequire="Ludeon.RimWorld.Biotech">ArchitePlasma</defaultStuff>

<!-- 列表项级 -->
<comps>
  <li Class="CompProperties_X" MayRequire="Ludeon.RimWorld.Royalty">
    <!-- 仅Royalty激活时加载 -->
  </li>
</comps>

<!-- MayRequireAnyOf：任一模组激活即可 -->
<someField MayRequireAnyOf="ModA,ModB">SomeValue</someField>
```

对应模组未激活时，该节点被**静默跳过**。

## 6. Def生命周期方法

| 方法 | 调用时机 | Def引用状态 | 适合做什么 |
|------|---------|-----------|-----------|
| 构造函数 | XML反序列化创建对象时 | 全部null | 设置字段默认值 |
| `PostLoad()` | XML字段填充完毕后 | 全部null | 图形初始化、字段后处理 |
| `ResolveReferences()` | 交叉引用解析后 | **已就绪** | 缓存引用、构建索引、验证 |
| `ConfigErrors()` | DevMode错误检查 | 已就绪 | 返回配置错误 |

**关键**：`PostLoad()`时Def引用字段仍为null，不要在此访问其他Def。`ResolveReferences()`时所有引用已解析完毕。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-16 | 创建文档：XML组织规则、反序列化流程、字段类型映射、ParentName继承、交叉引用延迟解析、MayRequire机制。基于DirectXmlToObject/XmlInheritance/DirectXmlCrossRefLoader源码研究 | Claude Opus 4.6 |
