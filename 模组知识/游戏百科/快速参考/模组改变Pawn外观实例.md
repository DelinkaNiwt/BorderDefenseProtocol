---
标题：模组改变Pawn外观实例
版本号: v1.0
更新日期: 2026-02-16
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 社区模组改变Pawn外观的5种技术路径深度分析：HAR外星人框架（ThingDef_AlienRace自定义种族ThingDef+GraphicPaths替换身体/头部/骨架贴图路径+AlienPartGenerator.BodyAddon附加视觉元素尾巴/角/翅膀+AlienComp.CompRenderNodes注入AlienPawnRenderNode_BodyAddon+AlienRenderTreePatches Harmony补丁替换Body/Head的GraphicFor+ApparelGraphicsOverrides种族专属衣物贴图+customDrawSize缩放）、CeleTech装备驱动体型切换（Comp_BodyshapeAjuster Notify_Equipped修改story.bodyType+保存恢复原始体型）、AncotLibrary条件渲染节点（PawnRenderNode_BodyPart按身体部位存在性控制渲染）、Milira机械体发型系统（Dialog_MilianHairStyleConfig自定义UI切换发型）、VEF美学缩放（AestheticScaling.CachedPawnData基于基因/Hediff的Pawn缩放），含5种技术路径对比+选型决策树+源码引用表
---

# 模组改变Pawn外观实例

**总览**：社区模组通过**5种技术路径**改变Pawn外观——HAR框架级替换（最全面，替换整个种族的渲染管线）、CompRenderNodes注入（标准扩展点，附加视觉元素）、Story字段修改（直接改体型/头型/发型）、Harmony补丁GraphicFor（替换贴图解析逻辑）、自定义PawnRenderNode/Worker（自定义绘制行为）。优先级原则：**原版扩展点 > 自定义Node/Worker > Harmony补丁**——原版扩展点（renderNodeProperties/CompRenderNodes）无侵入最安全，自定义Node/Worker灵活但需C#，Harmony补丁最强但兼容风险最高。

```
┌─────────────────────────────────────────────────────────────────┐
│         模组改变Pawn外观的5种技术路径（按侵入性排列）            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  路径1：renderNodeProperties（纯XML） ← 最简单                 │
│    └── GeneDef/HediffDef/ThingDef声明视觉节点，自动注入渲染树  │
│                                                                 │
│  路径2：CompRenderNodes（C#，无侵入） ← 推荐                   │
│    └── ThingComp.CompRenderNodes()返回自定义渲染节点列表       │
│                                                                 │
│  路径3：Story字段修改（C#，轻量） ← 体型/发型切换              │
│    └── 直接修改story.bodyType/hairDef等 + SetAllGraphicsDirty  │
│                                                                 │
│  路径4：自定义PawnRenderNode/Worker（C#，中量）                 │
│    └── 重写GraphicFor/OffsetFor/ScaleFor/CanDrawNow            │
│                                                                 │
│  路径5：Harmony补丁（C#，高侵入） ← 最后手段                   │
│    └── Patch原版Node的GraphicFor()，替换贴图解析逻辑           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 1. HAR（Humanoid Alien Races）——框架级种族外观替换

### 1.1 架构总览

HAR是RimWorld社区最核心的种族外观框架，通过**自定义ThingDef子类 + Harmony补丁 + CompRenderNodes**三层协作，实现完整的种族外观替换。

**核心架构**：
```
ThingDef_AlienRace : ThingDef（种族定义层）
├── AlienSettings
│   ├── GraphicPaths          ← 替换身体/头部/骨架/头骨/襁褓贴图路径
│   ├── AlienPartGenerator    ← 自定义绘制尺寸/头部偏移/BodyAddon附加物
│   │   ├── customDrawSize         ← 身体缩放（Vector2）
│   │   ├── customHeadDrawSize     ← 头部缩放
│   │   ├── headOffset             ← 头部偏移（按方向/体型/性别）
│   │   ├── bodyAddons             ← 附加视觉元素列表（尾巴/角/翅膀等）
│   │   └── colorChannels          ← 自定义颜色通道
│   ├── StyleSettings         ← 种族专属发型/胡须/纹身限制
│   └── ApparelGraphicsOverrides  ← 种族专属衣物贴图覆盖
│       ├── pathPrefix             ← 衣物贴图路径前缀替换
│       ├── individualPaths        ← 单件衣物贴图覆盖
│       ├── overrides/fallbacks    ← 衣物贴图替换/回退规则
│       └── bodyTypeFallback       ← 体型回退（无对应体型贴图时）
└── AlienComp : ThingComp（运行时组件）
    └── CompRenderNodes()      ← 注入BodyAddon渲染节点
```

### 1.2 ★ GraphicPaths——种族贴图路径替换

**核心思路**：每个种族定义独立的贴图路径集，替换原版`Things/Pawn/Humanlike/Bodies/`和`Things/Pawn/Humanlike/Heads/`。

```csharp
public class GraphicPaths
{
    public ExtendedGraphicTop body    = new() { path = "Things/Pawn/Humanlike/Bodies/" };
    public ExtendedGraphicTop head    = new() { path = "Things/Pawn/Humanlike/Heads/" };
    public ExtendedGraphicTop skeleton = new() { path = "Things/Pawn/Humanlike/HumanoidDessicated" };
    public ExtendedGraphicTop skull   = new() { path = "Things/Pawn/Humanlike/Heads/None_Average_Skull" };
    public ExtendedGraphicTop swaddle = new() { path = "Things/Pawn/Humanlike/Apparel/SwaddledBaby/..." };
    public ExtendedGraphicTop bodyMasks = new() { path = "" };  // 身体遮罩
    public ExtendedGraphicTop headMasks = new() { path = "" };  // 头部遮罩
    public ApparelGraphicsOverrides apparel = new();             // 衣物贴图覆盖
    public ShaderTypeDef skinShader;                             // 自定义皮肤着色器
    public Color skinColor;                                      // 皮肤阴影颜色
}
```

**通过Harmony补丁生效**：`AlienRenderTreePatches`对`PawnRenderNode_Body.GraphicFor()`和`PawnRenderNode_Head.GraphicFor()`做Prefix补丁，检测Pawn是否为`ThingDef_AlienRace`，是则使用种族自定义路径替代原版路径。

### 1.3 ★ BodyAddon——附加视觉元素（尾巴/角/翅膀）

**核心思路**：通过`AlienComp.CompRenderNodes()`注入`AlienPawnRenderNode_BodyAddon`节点，每个BodyAddon是一个独立的渲染层。

**注入流程**：
```
AlienComp.CompRenderNodes()
  → 遍历alienProps.alienPartGenerator.bodyAddons
  → 每个BodyAddon创建AlienPawnRenderNodeProperties_BodyAddon
  → 生成AlienPawnRenderNode_BodyAddon节点
  → 返回节点列表 → 被PawnRenderTree.SetupDynamicNodes()收集
```

**AlienPawnRenderNodeWorker_BodyAddon**——自定义Worker控制绘制行为：

```csharp
public class AlienPawnRenderNodeWorker_BodyAddon : PawnRenderNodeWorker
{
    // 条件显示：检查BodyAddon的CanDrawAddon()和skipFlags
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        BodyAddon addon = AddonFromNode(node);
        return addon.CanDrawAddon(parms.pawn)
            && !addon.useSkipFlags.Any(rsfd => parms.skipFlags.HasFlag(rsfd));
    }

    // 方向偏移：按朝向/体型/性别/头型计算精确偏移
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        BodyAddon ba = AddonFromNode(node).props.addon;
        DirectionalOffset offsets = (pawn.gender == Gender.Female) ? ba.femaleOffsets : ba.offsets;
        Vector3 offset = offsets.GetOffset(parms.facing)
            ?.GetOffset(parms.Portrait, pawn.story?.bodyType, pawn.story?.headType);
        offset.y = ba.inFrontOfBody ? (0.3f + offset.y) : (-0.3f - offset.y);
        // 北向层级反转、东向X轴翻转...
    }

    // 缩放：可选跟随Pawn绘制尺寸或固定尺寸
    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        Vector2 scale = ba.drawSize * (ba.scaleWithPawnDrawsize
            ? (ba.alignWithHead ? customHeadDrawSize : customDrawSize)
            : Vector2.one * 1.5f);
    }
}
```

**BodyAddon核心配置字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `path` | string | 贴图路径 |
| `drawSize` / `drawSizePortrait` | Vector2 | 绘制尺寸（世界/肖像） |
| `offsets` / `femaleOffsets` | DirectionalOffset | 四方向×体型×头型偏移 |
| `inFrontOfBody` | bool | 在身体前方还是后方 |
| `layerInvert` | bool | 北向时层级反转 |
| `alignWithHead` | bool | 跟随头部还是身体 |
| `scaleWithPawnDrawsize` | bool | 是否跟随Pawn缩放 |
| `useSkipFlags` | List | 跳过渲染的条件标志 |

### 1.4 ApparelGraphicsOverrides——种族专属衣物贴图

**核心问题**：非人类种族的体型可能没有对应的衣物贴图（原版衣物只有Male/Female/Thin/Hulk/Fat/Child体型贴图）。

**三层解决方案**：
1. `pathPrefix`——全局路径前缀替换（如`"Alien/Apparel/"`替代`"Things/Pawn/Humanlike/Apparel/"`）
2. `individualPaths`——单件衣物精确覆盖（`Dictionary<ThingDef, ExtendedGraphicTop>`）
3. `bodyTypeFallback`——体型回退（无对应体型贴图时回退到指定体型，如`Male`）

```csharp
public class ApparelGraphicsOverrides
{
    public ExtendedGraphicTop pathPrefix;                              // 全局路径前缀
    public Dictionary<ThingDef, ExtendedGraphicTop> individualPaths;   // 单件覆盖
    public List<ApparelReplacementOption> overrides;                   // 替换规则
    public List<ApparelReplacementOption> fallbacks;                   // 回退规则
    public BodyTypeDef bodyTypeFallback;                               // 体型回退
    public BodyTypeDef femaleBodyTypeFallback;                         // 女性体型回退
}
```

> **要点**：
> 1. HAR是**框架级**解决方案——通过ThingDef子类+50+个Harmony补丁全面接管种族渲染，其他种族模组（如VRE原版种族扩展）几乎都依赖HAR
> 2. BodyAddon通过`CompRenderNodes()`注入——这是原版1.5+提供的标准扩展点，HAR在此之前用Harmony补丁实现，1.6版本已迁移到原版API
> 3. 衣物兼容是种族模组最大的工作量——每个自定义种族都需要处理衣物贴图适配问题

## 2. CeleTech（天工铸造）——装备驱动体型切换

### 2.1 Comp_BodyshapeAjuster（装备改变体型）

**核心思路**：装备穿戴时修改`pawn.story.bodyType`，卸下时恢复原始体型。最轻量的外观修改方式。

```csharp
public class Comp_BodyshapeAjuster : ThingComp
{
    private BodyTypeDef BodyShape;    // 保存原始体型
    private bool ChangedBS = false;

    public override void Notify_Equipped(Pawn pawn)
    {
        // 仅对Hulk/Fat体型生效——强制切换为标准Male/Female
        if (pawn.story.bodyType == BodyTypeDefOf.Hulk || pawn.story.bodyType == BodyTypeDefOf.Fat)
        {
            BodyShape = pawn.story.bodyType;
            ChangedBS = true;
            pawn.story.bodyType = (pawn.gender == Gender.Male)
                ? BodyTypeDefOf.Male : BodyTypeDefOf.Female;
        }
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        if (ChangedBS)
        {
            pawn.story.bodyType = BodyShape;  // 恢复原始体型
            ChangedBS = false;
        }
    }
}
```

**设计模式**：`Notify_Equipped`/`Notify_Unequipped`回调 + 保存/恢复原始值。适用于任何"装备改变外观"场景。

**注意**：此实现缺少`SetAllGraphicsDirty()`调用——实际使用中可能依赖其他系统（如装备变化本身触发的渲染刷新）来更新外观。严格实现应在修改后显式调用。

> **要点**：
> 1. 直接修改`story.bodyType`是最简单的体型切换方式——但必须保存原始值并在卸下时恢复
> 2. 此模式可扩展到发型/头型/肤色——任何`Pawn_StoryTracker`字段都可用同样的保存/修改/恢复模式

## 3. AncotLibrary——条件渲染节点

### 3.1 PawnRenderNode_BodyPart（按身体部位控制渲染）

**核心思路**：自定义渲染节点在构造时检查Pawn是否拥有指定身体部位，决定是否渲染。用于"有手才显示手部装饰"等场景。

```csharp
public class PawnRenderNode_BodyPart : PawnRenderNode
{
    public bool canDrawNow = true;

    public PawnRenderNode_BodyPart(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
        var bodyPartProps = props as PawnRenderNodeProperties_BodyPart;
        canDrawNow = AncotUtility.HasNamedBodyPart(
            bodyPartProps.bodyPart, bodyPartProps.bodyPartLabel, pawn);
    }
}
```

**设计模式**：在构造函数中一次性判定，通过`canDrawNow`标记控制后续渲染。配合自定义`PawnRenderNodeWorker`的`CanDrawNow()`使用。

> **要点**：
> 1. 构造时判定 vs 每帧判定——AncotLibrary选择构造时判定（性能好），但截肢后需要`SetAllGraphicsDirty()`触发重建才能更新
> 2. 此模式适合"身体部位存在性"条件——更复杂的条件（如Hediff状态、基因激活）应在Worker的`CanDrawNow()`中每帧检查

## 4. Milira（米莉拉）——机械体自定义发型系统

### 4.1 Dialog_MilianHairStyleConfig（发型切换UI）

**核心思路**：为机械体单位提供自定义发型切换对话框，通过Gizmo触发，修改`story.hairDef`后调用`SetAllGraphicsDirty()`。

**技术路径**：Gizmo按钮 → 打开`Dialog_MilianHairStyleConfig` → 玩家选择发型 → 修改`pawn.story.hairDef` → `SetAllGraphicsDirty()`。

**特殊之处**：原版机械体没有发型系统（缺少`Pawn_StoryTracker`），Milira通过自定义种族定义为机械体添加了story组件，使其支持发型切换。

## 5. VEF（原版扩展框架）——美学缩放系统

### 5.1 AestheticScaling（基因/Hediff驱动的Pawn缩放）

**核心思路**：`CachedPawnData`缓存每个Pawn的缩放数据，基于基因和Hediff的`statOffsets`/`statFactors`计算缩放倍率，通过Harmony补丁修改渲染尺寸。

**缩放来源**：基因的`BodySize` stat修改 → 计算百分比变化 → 应用到渲染尺寸。同时按比例调整健康值（大型Pawn更耐打）。

## 6. 5种技术路径对比与选型指南

| 维度 | renderNodeProperties | CompRenderNodes | Story字段修改 | 自定义Node/Worker | Harmony补丁 |
|------|---------------------|-----------------|-------------|------------------|-------------|
| **需要C#** | 否 | 是 | 是 | 是 | 是 |
| **侵入性** | 无 | 无 | 低 | 低 | 高 |
| **兼容风险** | 无 | 无 | 低 | 低 | 中~高 |
| **灵活性** | 低 | 中 | 中 | 高 | 最高 |
| **适用场景** | 附加视觉元素 | 动态视觉元素 | 体型/发型切换 | 条件渲染/自定义偏移 | 替换贴图解析 |
| **典型实例** | 基因/Hediff视觉 | HAR BodyAddon | CeleTech体型 | AncotLib部位检查 | HAR种族贴图 |
| **代表模组** | 原版Biotech | HAR, AncotLib | CeleTech | AncotLib, HAR | HAR |

**选型决策树**：

```
需要创建全新种族（不同身体/头部贴图）？
  └── 是 → HAR框架（ThingDef_AlienRace + GraphicPaths + BodyAddon）

需要附加视觉元素（角/尾巴/翅膀/光环）？
  ├── 来自基因/Hediff → renderNodeProperties（纯XML）
  └── 来自装备/Comp → CompRenderNodes()（C#）

需要切换体型/发型/头型？
  ├── 装备触发 → Notify_Equipped修改story字段
  ├── Hediff触发 → HediffComp中修改story字段
  └── 基因触发 → Gene子类中修改story字段

需要条件性显示/隐藏视觉元素？
  ├── 简单条件（身体部位存在） → 自定义PawnRenderNode构造时判定
  └── 复杂条件（状态/Hediff） → 自定义PawnRenderNodeWorker.CanDrawNow()

需要替换原版贴图解析逻辑？
  └── Harmony补丁GraphicFor()（最后手段）
```

## 7. 关键源码引用表

| 类 | 命名空间 | 模组 | 职责 |
|----|---------|------|------|
| `ThingDef_AlienRace` | AlienRace | HAR | 种族ThingDef子类（AlienSettings容器） |
| `GraphicPaths` | AlienRace | HAR | 种族贴图路径集（body/head/skeleton/skull） |
| `AlienPartGenerator` | AlienRace | HAR | 种族外观生成器（drawSize/headOffset/bodyAddons/colorChannels） |
| `AlienPartGenerator.AlienComp` | AlienRace | HAR | 运行时组件（CompRenderNodes注入BodyAddon） |
| `AlienPawnRenderNode_BodyAddon` | AlienRace | HAR | BodyAddon渲染节点（重写GraphicFor/MeshSetFor/GetMesh） |
| `AlienPawnRenderNodeWorker_BodyAddon` | AlienRace | HAR | BodyAddon渲染Worker（CanDrawNow/OffsetFor/ScaleFor） |
| `AlienPawnRenderNodeProperties_BodyAddon` | AlienRace | HAR | BodyAddon节点属性（addon/addonIndex/graphic/alienComp） |
| `AlienRenderTreePatches` | AlienRace | HAR | Harmony补丁集（Body/Head GraphicFor替换+DrawSize修改） |
| `ApparelGraphicsOverrides` | AlienRace.ApparelGraphics | HAR | 种族衣物贴图覆盖（pathPrefix/individualPaths/fallbacks） |
| `Comp_BodyshapeAjuster` | TOT_DLL_test | CeleTech(天工铸造) | 装备改变体型（Hulk/Fat→Male/Female） |
| `PawnRenderNode_BodyPart` | AncotLibrary | AncotLibrary | 按身体部位存在性控制渲染 |
| `Dialog_MilianHairStyleConfig` | Milira | Milira(米莉拉) | 机械体发型切换UI |
| `CachedPawnData` | VEF.AestheticScaling | VEF(原版扩展框架) | 基因/Hediff驱动的Pawn缩放缓存 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-16 | 创建文档：5种模组改变Pawn外观技术路径分析（HAR框架级种族外观替换——ThingDef_AlienRace架构总览+GraphicPaths 7路径替换+BodyAddon附加视觉元素AlienComp.CompRenderNodes注入+AlienPawnRenderNodeWorker_BodyAddon自定义Worker含CanDrawNow/OffsetFor/ScaleFor+BodyAddon 8核心配置字段+ApparelGraphicsOverrides三层衣物贴图适配、CeleTech装备驱动体型切换Comp_BodyshapeAjuster保存恢复模式、AncotLibrary条件渲染节点PawnRenderNode_BodyPart构造时判定、Milira机械体发型系统Dialog_MilianHairStyleConfig、VEF美学缩放AestheticScaling.CachedPawnData），含5种技术路径6维度对比表+选型决策树+源码引用表13项 | Claude Opus 4.6 |
