这是一个非常关键的问题。因为我们选择了继承 `Gene_Resource`，所以在 XML 定义中，我们不仅要定义基因本身，还要配置它作为“资源条”的 UI 属性。

以下是适配 **Phase 1** 逻辑的 `GeneDef` 标准写法。

### 📄 文件路径建议
`Defs/Genes/Gene_Trion.xml`

### 💻 XML 代码实现

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Defs>

  <GeneDef>
    <defName>Trion_Core</defName>
    <label>trion core</label>
    <description>Carriers of this gene possess a specialized organ that generates and stores Trion particles. This biological energy source allows for the materialization of virtual bodies and weapons.\n\nWithout this gene, a pawn cannot utilize Trion technology regardless of their stats.</description>
  
    <!-- 核心绑定：指向我们刚才写的 C# 类 -->
    <geneClass>RimTrion.Gene_Trion</geneClass>
  
    <!-- 基础属性 -->
    <iconPath>UI/Icons/Genes/Gene_TrionCore</iconPath> <!-- 请确保有一张 png 图片 -->
    <displayCategory>Archite</displayCategory> <!-- 或自定义分类 -->
    <marketValueFactor>1.5</marketValueFactor>
  
    <!-- 生物学成本 -->
    <biostatCpx>3</biostatCpx> <!-- 复杂度: 3 -->
    <biostatMet>-2</biostatMet> <!-- 代谢率: -2 (消耗更多食物来维持能量) -->
  
    <!-- === 资源条 UI 配置 (Resource Gene 特有) === -->
  
    <resourceLabel>Trion</resourceLabel>
    <resourceGizmoType>RimWorld.GeneGizmo_Resource</resourceGizmoType> <!-- 使用原版样式 -->
  
    <!-- 颜色配置 (Trion 标志性的青蓝色) -->
    <resourceBarColor>(0.2, 0.8, 1.0)</resourceBarColor> 
    <!-- 当能量极低时的警告色 -->
    <resourceBarColorEmpty>(0.8, 0.2, 0.2)</resourceBarColorEmpty>
  
    <!-- 描述：鼠标悬停在能量条上时显示的文本 -->
    <resourceDescription>Trion is the biological energy used to maintain the virtual body and power triggers. If it reaches zero, the virtual body will collapse.\n\nCapacity is determined by the TrionMaxCap stat.</resourceDescription>
  
    <!-- === 核心数值绑定 === -->
    <!-- 
      根据架构文档，MaxCap 是由 StatDef 决定的。
      但如果 StatDef 默认是 0，我们需要这个基因本身提供一个“基础值”。
      否则刚装上基因的小人上限还是 0。
    -->
    <statOffsets>
      <TrionMaxCap>1000</TrionMaxCap> <!-- 基础带 1000 点上限 -->
      <TrionOutput>500</TrionOutput>   <!-- 基础带 500 点输出功率 -->
    </statOffsets>
  
    <!-- 是否在世界地图界面显示 (远征时) -->
    <showGizmoOnWorldView>true</showGizmoOnWorldView>

    <!-- 视觉效果：如果有眼球发光等设定可以在这里加 RenderNode，Phase 1 暂时留空 -->
  
  </GeneDef>

</Defs>
```

---

### 🔍 架构师深度解析 (Why this works?)

#### 1. `<geneClass>` 的桥梁作用
这是整段代码的灵魂。
*   原版基因使用的是 `RimWorld.Gene` 或 `RimWorld.Gene_Resource`。
*   这里写了 `RimTrion.Gene_Trion`，游戏加载时会反射实例化我们在上一步写的 C# 类。
*   **验证点**: 如果进入游戏后，能量条出来了但逻辑不对（比如不扣费），检查这里是否拼写正确。

#### 2. `<statOffsets>` 的必要性
在 C# 代码中，我们写了 `Max => pawn.GetStatValue(TrionMaxCap)`。
在 `StatDef` 中，我们定义了 `defaultBaseValue` 为 0。
*   如果不加这个 `statOffsets`，小人装上基因后，`Max` 依然是 0。能量条会是一个空的、不可填充的灰条。
*   **设计意图**: 这个基因即是“容器”也是“开关”。只有拥有这个基因，`TrionMaxCap` 才有实际意义。

#### 3. `<biostatMet>` (代谢率)
*   我将其设为 `-2`。这意味着拥有此基因的小人，饥饿速度会变为 125%。
*   **设定依据**: Trion 是一种生物能量，需要消耗卡路里来制造。这在 Gameplay 上是一个软性平衡——你获得了强大的护盾，但你的殖民地粮食消耗增加了。

#### 4. 图片资源 (`iconPath`)
*   RimWorld 对缺失图片的容忍度很低，可能会报错或显示粉色方块。
*   **Phase 1 临时方案**: 如果你还没画图，可以直接借用原版的图来测试：
    `<iconPath>UI/Icons/Genes/Gene_Hemogen</iconPath>` (借用血原质的图标)

### 🛠️ 常见报错排查

如果您加载模组时出现 **"XML error: <resourceLabel> doesn't correspond to any field in type GeneDef"**：
*   **原因**: 您的 C# 类定义可能出了问题，或者 XML 解析器没把这个 Def 识别为 Resource Gene 的子类。
*   **其实**: `GeneDef` 本身包含 `resourceLabel` 字段。只要 XML 结构正确通常不会报错。最常见的错误是把 `<resourceLabel>` 写到了 `<statOffsets>` 里面去。请严格保持层级与上述代码一致。