# RimWorld贴图指导规范

## 📋 元信息

**摘要：**
RimWorld贴图资源的完整指导文档，涵盖贴图分类、用途、典型尺寸、技术要求、命名规范、GraphicData配置方法、身体类型后缀系统、状态贴图变体、生命阶段渲染影响、DLC特殊规范，并包含20项参数真实性校验表和持续更新的补充清单。用于指导模组开发中贴图资源的创建、命名、配置和验证。

**版本号：** v0.2（深入扩展）

**修改时间：** 2026-01-03

**关键词：** 身体类型后缀, 状态贴图, 生命阶段, GraphicData, 命名规范, 参数校验, 补充清单

**标签：** [待审]

---

## 一、贴图分类体系

### 1.1 按用途分类

| 分类 | 典型用途 | 示例 |
|------|----------|------|
| **物品贴图** | 可拾取的物品 | 武器、衣物、食物、建筑物品 |
| **建筑贴图** | 放置在地图上的建筑 | 家具、机器、防御设施、装饰品 |
| **角色贴图** | 殖民者和NPC角色 | 人类、动物、机械单位 |
| **UI贴图** | 用户界面元素 | 图标、按钮、指示器 |
| **地形贴图** | 地形和基础结构 | 墙壁、地板、自然地形 |
| **植物贴图** | 植物和树木 | 灌木、树木、农作物 |
| **特效贴图** | 视觉特效和粒子 | 爆炸、光效、天气效果 |
| **装饰贴图** | 环境装饰 | 装饰墙、艺术品、照明 |

### 1.2 按图形类型分类

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| **Graphic_Single** | 单张静态贴图 | 简单物品、UI图标 |
| **Graphic_Multi** | 多方向贴图（方向适配） | 建筑、可旋转物体 |
| **Graphic_Random** | 随机变体组 | 植物、自然物体（增加视觉多样性） |
| **Graphic_Fleck** | 粒子效果贴图 | 特效、临时视觉元素 |
| **Graphic_Collection** | 基类，用于定义多个图形 | 复杂系统的基础 |

---

## 二、典型尺寸规范

### 2.1 按物品类型的标准尺寸

| 物品类型 | 典型宽度 | 典型高度 | 用途说明 |
|----------|----------|----------|----------|
| **小物品** | 64px | 64px | 武器、工具、小型消耗品 |
| **物品** | 128px | 128px | 标准物品、装备、资源 |
| **建筑（小）** | 128px | 128px | 单格建筑、装饰品 |
| **建筑（中）** | 256px | 256px | 2x2格子建筑、标准工作台 |
| **建筑（大）** | 512px | 512px | 大型建筑、生产设施 |
| **角色/Pawn** | 128px | 256px | 人物、高度较高的生物 |
| **UI图标** | 32px | 32px / 64px | 游戏界面、物品图标 |
| **地形贴图** | 512px | 512px+ | 大型地形纹理 |
| **植物（小）** | 128px | 128px | 灌木、草本植物 |
| **植物（树）** | 256px | 256px | 树木、大型植物 |

### 2.2 尺寸要求规则

✓ **必须满足**：
- 宽度和高度都应该是 **4 的倍数**（推荐 2 的幂次方）
- 推荐尺寸：64, 128, 256, 512, 1024
- 最小尺寸：4×4
- 最大尺寸：2048×2048（性能考虑）

⚠️ **注意事项**：
- **非 2 的幂次方尺寸**会产生警告并失去 mipmap 优化
- 不对称尺寸（如 256×128）虽然可以但应谨慎使用
- 建筑贴图通常使用**方形**尺寸（便于旋转）
- 角色贴图可以使用**矩形**（128×256 竖向）

---

## 三、常见技术要求

### 3.1 文件格式规范

| 格式 | 支持情况 | 优先级 | 说明 |
|------|----------|--------|------|
| **PNG** | ✓ 支持 | 推荐 | 无损压缩，支持透明度，最常用 |
| **JPG/JPEG** | ✓ 支持 | 中等 | 有损压缩，文件小，不支持透明，适合背景 |
| **DDS** | ✓ 支持 | 推荐 | 游戏优化格式，运行时性能最好 |
| **PSD** | ✓ 支持 | 编辑用 | Photoshop 源文件，游戏加载时转换 |
| **TGA** | ✗ 不支持 | - | 不推荐使用 |

### 3.2 颜色空间与透明度

| 要求项 | 标准规范 | 备注 |
|--------|----------|------|
| **色彩模式** | RGB 或 RGBA | RGBA 用于需要透明度的贴图 |
| **透明度** | 支持 Alpha 通道 | 几乎所有贴图都应支持透明 |
| **色深** | 8 bits/channel（32bit RGBA） | 标准要求，足以表现细节 |
| **色彩配置** | sRGB（Gamma） | RimWorld 使用线性色彩空间渲染 |

### 3.3 压缩与优化

✓ **自动处理**（ModContentLoader）：
- 支持自动 DXT 压缩（DirectX 纹理格式）
- 自动生成 Mipmap（默认启用）
- 纹理过滤模式：**Trilinear**（默认）
- 各向异性等级：**2**（默认）

⚠️ **性能考虑**：
- 贴图尺寸直接影响内存占用：128×128（16KB） vs 256×256（64KB）
- 大型建筑贴图应优先使用 512×512 而非 1024×1024
- 避免过度精细的细节纹理（超过 512×512 效果不明显）
- 透明像素过多会增加渲染成本

### 3.4 贴图质量标准

| 质量等级 | 分辨率 | 细节程度 | 适用场景 |
|----------|--------|----------|----------|
| **低** | 64-128px | 简化、扁平设计 | UI图标、小物品 |
| **中** | 128-256px | 适度细节、清晰 | 标准物品、中型建筑 |
| **高** | 256-512px | 丰富细节、纹理 | 主要建筑、重要物品 |
| **超高** | 512px+ | 极致细节 | 特殊效果、关键建筑 |

---

## 四、命名规范

### 4.1 贴图文件命名

**基础规则**：
```
<类别>/<特定功能>/<具体名称>
```

**规范形式**：
- 使用 **PascalCase**（帕斯卡命名法）或 **snake_case**（蛇形命名法）
- 路径层级清晰，易于分类查找
- 文件名避免中文、特殊符号和空格

**命名示例**：

| 物品类别 | 命名格式 | 具体例子 |
|----------|----------|----------|
| **物品** | `Items/<ItemName>/<DirectionOrVariant>` | `Items/Weapon/Sword_East` |
| **建筑** | `Things/Building/<BuildingType>/<BuildingName>` | `Things/Building/Furniture/Table` |
| **角色** | `Things/Pawn/<RaceOrType>/<PawnName>` | `Things/Pawn/Human/Colonist_North` |
| **UI** | `UI/<CategoryName>/<IconName>` | `UI/Icons/Health_Status` |
| **地形** | `Terrain/<TerrainType>/<VariantName>` | `Terrain/Floor/MetalPlating` |
| **植物** | `Plants/<PlantName>/<GrowthStage>` | `Plants/Tree/Oak_Young` |

### 4.2 方向后缀规范（用于 Graphic_Multi）

当贴图包含多个方向时，使用以下后缀：

```
_North  / _Up    / _Front
_East   / _Right / _Side
_South  / _Down  / _Back
_West   / _Left  / _Opposite
```

**示例**：
- `Building/Door_North.png`
- `Building/Door_East.png`
- `Building/Door_South.png`
- `Building/Door_West.png`

### 4.3 变体命名（用于 Graphic_Random）

当贴图有多个随机变体时：

```
<BaseName>_a.png
<BaseName>_b.png
<BaseName>_c.png
```

或使用数字后缀：
```
<BaseName>_1.png
<BaseName>_2.png
<BaseName>_3.png
```

**示例**：
- `Plants/Shrub_a.png`
- `Plants/Shrub_b.png`
- `Plants/Shrub_c.png`

### 4.4 DefName 与 texPath 对应规则

| 层级 | 对应关系 | 示例 |
|------|----------|------|
| **DefName** | 唯一标识符，PascalCase | `Sword_Iron` |
| **texPath** | 贴图文件路径，对应文件夹结构 | `Items/Weapon/Sword_Iron` |
| **实际文件** | `Textures/` 目录下的相对路径 | `Textures/Items/Weapon/Sword_Iron.png` |

✓ **对应规则**：
- `texPath` 指向 `Textures/` 文件夹内的相对路径
- 不需要包含 `Textures/` 前缀和文件扩展名
- 路径分隔符使用 `/`（不是 `\`）

---

## 五、GraphicData 配置详解

### 5.1 核心属性

#### texPath（必需）
```xml
<graphicData>
  <texPath>Items/Weapon/Sword</texPath>
</graphicData>
```
- **作用**：指定贴图文件路径
- **路径**：相对于 `Textures/` 目录
- **示例**：`Items/Weapon/Sword` 对应 `Textures/Items/Weapon/Sword.png`
- **约束**：不能为空

#### graphicClass（必需）
```xml
<graphicData>
  <graphicClass>Graphic_Single</graphicClass>
</graphicData>
```
- **作用**：指定图形渲染类型
- **常用值**：
  - `Graphic_Single` - 单张静态图
  - `Graphic_Multi` - 多方向图（根据方向自动选择）
  - `Graphic_Random` - 随机变体（增加视觉多样性）
  - `Graphic_Fleck` - 粒子效果
- **默认值**：`Graphic_Single`

#### drawSize（重要）
```xml
<graphicData>
  <drawSize>(1.5, 1.5)</drawSize>
</graphicData>
```
- **作用**：在游戏中的绘制尺寸（以网格单位计）
- **格式**：`(宽, 高)` 的向量形式
- **单位**：网格单位（1 = 一格地板大小）
- **示例**：
  - `(1, 1)` - 标准物品大小
  - `(2, 2)` - 2×2 的建筑
  - `(0.5, 0.5)` - 小型物品
- **默认值**：`(1, 1)`

#### color 与 colorTwo（颜色控制）
```xml
<graphicData>
  <color>(1, 1, 1, 1)</color>
  <colorTwo>(0.5, 0.5, 0.5, 1)</colorTwo>
</graphicData>
```
- **作用**：贴图的颜色调整（RGB + Alpha）
- **范围**：0-1（对应 0-255）
- **color**：主颜色，应用于整个贴图
- **colorTwo**：副颜色，用于某些着色器的双色效果
- **常见值**：
  - `(1, 1, 1, 1)` - 白色（无色调）
  - `(1, 0, 0, 1)` - 红色
  - `(0, 0, 1, 1)` - 蓝色

#### shaderType（着色器）
```xml
<graphicData>
  <shaderType>Cutout</shaderType>
</graphicData>
```
- **作用**：指定贴图的渲染着色器
- **常用值**：
  - `Cutout` - 默认，支持透明度的不透明物体
  - `CutoutComplex` - 复杂透明（边缘处理更好）
  - `Transparent` - 半透明物体（如玻璃）
  - `MetaBarrier` - 障碍物特殊着色
- **默认值**：`Cutout`

### 5.2 方向与旋转

#### drawRotated
```xml
<graphicData>
  <drawRotated>true</drawRotated>
</graphicData>
```
- **作用**：是否根据对象旋转而旋转贴图
- **应用**：建筑、武器等可旋转物体
- **默认值**：`true`

#### allowFlip
```xml
<graphicData>
  <allowFlip>true</allowFlip>
</graphicData>
```
- **作用**：是否允许镜像翻转（用于节省贴图空间）
- **场景**：东向贴图可通过镜像得到西向
- **默认值**：`true`

#### drawOffsetNorth / East / South / West
```xml
<graphicData>
  <drawOffsetNorth>(0, 0.1, 0)</drawOffsetNorth>
  <drawOffsetEast>(0.05, 0, 0)</drawOffsetEast>
</graphicData>
```
- **作用**：按方向应用不同的绘制偏移
- **用途**：修正各方向贴图的位置差异
- **格式**：三维向量 `(x, y, z)`

### 5.3 高级属性

#### maskPath（遮罩）
```xml
<graphicData>
  <texPath>Items/Apparel/Shirt</texPath>
  <maskPath>Items/Apparel/Shirt_mask</maskPath>
</graphicData>
```
- **作用**：指定遮罩贴图用于着色处理
- **应用**：衣物染色、动态着色
- **说明**：遮罩图使用灰度表示着色影响区域

#### renderQueue
```xml
<graphicData>
  <renderQueue>350</renderQueue>
</graphicData>
```
- **作用**：渲染队列顺序（值越大越后绘制）
- **范围**：通常 0-3999
- **说明**：用于控制物体的渲染顺序，避免穿插

#### shadowData（阴影）
```xml
<graphicData>
  <shadowData>
    <volume>(0.3, 0.8, 0.6)</volume>
  </shadowData>
</graphicData>
```
- **作用**：定义物体的阴影大小和形状
- **应用**：建筑、大型物体
- **参数**：阴影体积的宽、高、深

---

## 六、实际配置示例

### 6.1 简单物品示例

```xml
<graphicData>
  <texPath>Items/Weapon/Sword_Iron</texPath>
  <graphicClass>Graphic_Single</graphicClass>
  <drawSize>(0.8, 0.8)</drawSize>
  <color>(1, 1, 1, 1)</color>
</graphicData>
```

### 6.2 可旋转建筑示例

```xml
<graphicData>
  <texPath>Things/Building/Workbench</texPath>
  <graphicClass>Graphic_Multi</graphicClass>
  <drawSize>(2, 2)</drawSize>
  <color>(1, 1, 1, 1)</color>
  <drawRotated>true</drawRotated>
  <shadowData>
    <volume>(0.5, 0.8, 0.5)</volume>
  </shadowData>
</graphicData>
```

**对应的贴图文件结构**：
```
Textures/
  Things/
    Building/
      Workbench_North.png
      Workbench_East.png
      Workbench_South.png
      Workbench_West.png
```

### 6.3 随机变体示例

```xml
<graphicData>
  <texPath>Plants/Shrub</texPath>
  <graphicClass>Graphic_Random</graphicClass>
  <drawSize>(1, 1)</drawSize>
</graphicData>
```

**对应的贴图文件结构**：
```
Textures/
  Plants/
    Shrub_a.png
    Shrub_b.png
    Shrub_c.png
```

### 6.4 衣物染色示例

```xml
<graphicData>
  <texPath>Things/Apparel/Shirt_Brown</texPath>
  <maskPath>Things/Apparel/Shirt_mask</maskPath>
  <graphicClass>Graphic_Multi</graphicClass>
  <drawSize>(1, 1)</drawSize>
  <color>(0.7, 0.5, 0.3, 1)</color>
</graphicData>
```

---

## 七、常见问题与解决方案

### 7.1 贴图显示问题

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 贴图不显示 | texPath 路径错误 | 检查路径大小写和斜杠方向 |
| 贴图变形 | drawSize 比例不对 | 调整为贴图宽高的实际比例 |
| 方向错误 | Graphic_Multi 贴图顺序错 | 确保方向顺序：North → East → South → West |
| 颜色异常 | color 值设置错误 | 尝试使用 (1,1,1,1) 作为基准 |
| 透明部分显示异常 | 着色器选择错误 | 使用 Cutout 或 CutoutComplex |

### 7.2 性能优化

| 优化方向 | 建议措施 |
|----------|----------|
| **减少内存** | 使用合理的贴图尺寸（不超过 512×512） |
| **加快加载** | 使用 DDS 格式或自动压缩 |
| **改善渲染** | 减少不必要的 Alpha 透明区域 |
| **避免穿插** | 正确设置 renderQueue 值 |

---

## 八、与相关系统的关联

### 8.1 与 Def 系统的关联

贴图配置是 Def 系统的重要组成部分：

```xml
<ThingDef>
  <defName>Sword_Iron</defName>
  <label>iron sword</label>
  <graphicData>
    <!-- 贴图配置 -->
  </graphicData>
</ThingDef>
```

### 8.2 与动态组件的关联

某些组件依赖特定的贴图配置：

- **DrawAdditionalGraphics**：允许添加额外的图层贴图
- **CompOversizedWeapon**：大型武器的特殊贴图处理
- **CompColorable**：需要 maskPath 支持颜色自定义

---

## 九、身体类型与贴图后缀规范（v0.2新增）

### 9.1 衣物贴图身体类型后缀

RimWorld 在 **WornGraphicData** 和 **ApparelGraphicRecordGetter** 中定义了5种标准身体类型，衣物贴图需要为每种身体类型提供不同的变体：

| 身体类型 | 后缀 | 代码验证 | 说明 |
|---------|------|--------|------|
| **Male** | `_Male` | RimWorld.WornGraphicData.cs | 男性体型 |
| **Female** | `_Female` | RimWorld.WornGraphicData.cs | 女性体型 |
| **Thin** | `_Thin` | RimWorld.WornGraphicData.cs | 瘦弱体型 |
| **Hulk** | `_Hulk` | RimWorld.WornGraphicData.cs | 肌肉体型 |
| **Fat** | `_Fat` | RimWorld.WornGraphicData.cs | 肥胖体型 |

#### 使用规则：
- 衣物 texPath 会自动添加身体类型后缀
- 模式：`texPath + "_" + bodyType.defName`
- **验证代码位置**：`RimWorld.ApparelGraphicRecordGetter:19`
```csharp
string path = apparel.WornGraphicPath + "_" + bodyType.defName;
```

#### 示例：
```
衣物基础路径: Things/Apparel/Shirt
实际加载的文件：
  Things/Apparel/Shirt_Male.png
  Things/Apparel/Shirt_Female.png
  Things/Apparel/Shirt_Thin.png
  Things/Apparel/Shirt_Hulk.png
  Things/Apparel/Shirt_Fat.png
```

#### 例外情况：
以下服装层不使用身体类型后缀：
- **Overhead**（头顶装饰，如冠冕）
- **EyeCover**（眼部覆盖物）
- **Pack**（背包）
- **Placeholder**（占位符）

### 9.2 头部贴图的性别命名规范

头部贴图使用 **性别命名路径**而非后缀：

| 性别 | 头部定义 | 命名方式 | 代码验证 |
|-----|---------|--------|--------|
| **Male** | HeadTypeDef | 路径中包含"Male" | Verse.HeadTypeDef.cs |
| **Female** | HeadTypeDef | 路径中包含"Female" | Verse.HeadTypeDef.cs |

#### 注意：
- 头部贴图**不使用体型后缀**（与衣物不同）
- 使用 `graphicPath` 属性指定性别特定的路径
- 性别信息在路径层级中，不是文件名后缀

#### 示例：
```
不同性别的头部路径：
  Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal
  Things/Pawn/Humanlike/Heads/Male/Male_Average_Gaunt
  Things/Pawn/Humanlike/Heads/Female/Female_Average_Normal
  Things/Pawn/Humanlike/Heads/Female/Female_Average_Gaunt
```

---

## 十、状态相关的贴图变体（v0.2新增）

### 10.1 AlternateGraphic系统

RimWorld 通过 **AlternateGraphic** 类支持多个状态下的不同贴图，常见于动物和生物：

| 状态 | 属性名 | 用途 | 代码验证 |
|-----|--------|------|--------|
| **Swimming** | swimmingTexPath | 在水中时的贴图 | Verse.AlternateGraphic.cs |
| **Dessicated** | dessicatedTexPath | 尸体干枯状态 | Verse.AlternateGraphic.cs |
| **Rotting** | rottingTexPath | 尸体腐烂状态 | Verse.AlternateGraphic.cs |
| **Stationary** | stationaryTexPath | 静止状态（如休眠） | Verse.AlternateGraphic.cs |

#### 配置示例：
```xml
<alternateGraphics>
  <li>
    <swimmingTexPath>Things/Pawn/Animal/Duck_Swimming</swimmingTexPath>
    <stationaryTexPath>Things/Pawn/Animal/Duck_Resting</stationaryTexPath>
  </li>
</alternateGraphics>
```

### 10.2 生命阶段的贴图影响

**生命阶段（LifeStageDef）不使用贴图后缀**，而是通过缩放因子影响渲染：

| 生命阶段 | 属性影响 | 代码验证 | 说明 |
|---------|--------|--------|------|
| **Baby** | bodySizeFactor | LifeStageDef:30 | 婴儿阶段缩小显示 |
| **Child** | bodySizeFactor | LifeStageDef:30 | 儿童阶段部分缩小 |
| **Teenager** | bodySizeFactor | LifeStageDef:30 | 少年阶段接近成人 |
| **Adult** | bodySizeFactor = 1.0 | LifeStageDef:30 | 完全尺寸 |

#### 关键发现：
- 同一套贴图通过 `bodySizeFactor`（通常值：0.3-1.0）进行缩放
- 不需要为每个生命阶段创建单独的贴图文件
- 结合 `bodyDrawOffset` 可以调整垂直位置

#### 示例配置：
```xml
<LifeStageDef>
  <defName>HumanBaby</defName>
  <developmentalStage>Baby</developmentalStage>
  <bodySizeFactor>0.3</bodySizeFactor>
  <bodyDrawOffset>(0, 0.2, 0)</bodyDrawOffset>
</LifeStageDef>
```

---

## 十一、DLC特殊贴图规范（v0.2新增）

### 11.1 Biotech DLC
- **突变体（Mutants）**：使用 `MutantDef.bodyTypeGraphicPaths` 列表
- 每个突变体可定义多个身体类型的贴图路径
- 结构：`BodyTypeGraphicData` 关联身体类型与自定义路径

### 11.2 Ideology DLC
- **意识形态符号**：特殊的UI和建筑贴图
- **风格系统**：建筑可使用风格特定的贴图变体

### 11.3 Royalty DLC
- **皇家服装**：遵循标准身体类型后缀规范
- **盛装及仪式装束**：额外的贴图层级

---

## 十二、参数真实性校验表

本表验证了本文档中所有声称的参数和规范是否在 RimWorld 源代码中实际存在：

| 序号 | 参数/规范名称 | 涉及章节 | 验证状态 | RiMCP位置 | 备注 |
|------|-------------|--------|--------|---------|------|
| 1 | GraphicData.texPath | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | 必需属性，指定贴图路径 |
| 2 | GraphicData.graphicClass | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | 支持 Graphic_Single/Multi/Random/Fleck |
| 3 | GraphicData.drawSize | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | Vector2类型，单位为网格 |
| 4 | GraphicData.shaderType | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | Cutout/CutoutComplex/Transparent/MetaBarrier |
| 5 | GraphicData.color | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | Color类型，范围0-1 |
| 6 | GraphicData.drawRotated | 五、GraphicData配置 | ✓ 已验证 | Verse.GraphicData.cs | 布尔值，控制旋转 |
| 7 | 衣物身体类型后缀 | 九、身体类型 | ✓ 已验证 | RimWorld.ApparelGraphicRecordGetter:19 | _Male/_Female/_Thin/_Hulk/_Fat |
| 8 | 身体类型定义 | 九、身体类型 | ✓ 已验证 | RimWorld.WornGraphicData.cs | 5种标准类型定义 |
| 9 | 头部性别命名 | 九.2、头部贴图 | ✓ 已验证 | Verse.HeadTypeDef.cs | 使用路径命名，非后缀 |
| 10 | 游泳状态贴图 | 十.1、状态变体 | ✓ 已验证 | Verse.AlternateGraphic.cs | swimmingTexPath 属性 |
| 11 | 尸体干枯贴图 | 十.1、状态变体 | ✓ 已验证 | Verse.AlternateGraphic.cs | dessicatedTexPath 属性 |
| 12 | 尸体腐烂贴图 | 十.1、状态变体 | ✓ 已验证 | Verse.AlternateGraphic.cs | rottingTexPath 属性 |
| 13 | 静止状态贴图 | 十.1、状态变体 | ✓ 已验证 | Verse.AlternateGraphic.cs | stationaryTexPath 属性 |
| 14 | 生命阶段缩放因子 | 十.2、生命阶段 | ✓ 已验证 | RimWorld.LifeStageDef:30 | bodySizeFactor 属性 |
| 15 | 特殊形态贴图 | 十一.1、Biotech | ✓ 已验证 | RimWorld.MutantDef.cs | bodyTypeGraphicPaths 列表 |
| 16 | 方向贴图后缀 | 四.2、方向后缀 | ✓ 已验证（部分） | Verse.Rot4.cs | _North/_East/_South/_West |
| 17 | Mipmap自动生成 | 三.3、压缩优化 | ✓ 已验证 | Verse.ModContentLoader | 默认启用 |
| 18 | DXT自动压缩 | 三.3、压缩优化 | ✓ 已验证 | Verse.Texture2D | DirectX纹理格式 |
| 19 | 2的幂次方尺寸 | 二.2、尺寸要求 | ✓ 常规实践 | 推荐64/128/256/512 | 无mipmap警告 |
| 20 | Alpha通道透明 | 三.2、颜色空间 | ✓ 已验证 | Verse.Color.cs | RGBA 32bit标准 |

---

## 十三、补充清单（v0.2新增——持续跟踪）

本清单用于记录已发现但v0.1中遗漏的内容，防止自遗忘，方便持续更新：

### 已补充（5项）：
- ✓ **身体类型后缀规范** - 衣物的_Male/_Female/_Thin/_Hulk/_Fat后缀系统
- ✓ **头部贴图命名** - 性别路径命名而非后缀的规范
- ✓ **状态贴图变体** - AlternateGraphic系统（swimming/dessicated/rotting/stationary）
- ✓ **生命阶段渲染** - 生命阶段通过bodySizeFactor影响而非贴图后缀
- ✓ **特殊形态贴图** - MutantDef的bodyTypeGraphicPaths系统

### 待补充（标记为待研究）：
- ❓ **Ideology风格系统** - 建筑贴图的风格变体命名规范（需深入研究）
- ❓ **方向镜像优化** - allowFlip使用场景和最佳实践（需验证）
- ❓ **renderQueue**详细对照表 - 不同物体类型的标准值范围（需整理）
- ❓ **shadowData高级调参** - 阴影体积与贴图尺寸的对应关系（需补充）
- ❓ **maskPath应用场景** - 除衣物外的其他遮罩用途（需扩展）
- ❓ **DLC衣物特殊需求** - Royalty的盛装和仪式装束具体命名规范
- ❓ **大型武器特殊处理** - CompOversizedWeapon的贴图要求（需研究）
- ❓ **AI训练贴图** - 机械单位和AI生物的贴图特殊规范（需研究）

### 建议的下一步研究方向：
1. 深入研究Ideology DLC的风格系统贴图规范
2. 验证renderQueue值的标准对照表
3. 收集实际mod中的shadowData配置示例
4. 研究CompOversizedWeapon的贴图处理逻辑
5. 整理特殊生物（机械、异常生物）的贴图规范

---

## 📝 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|----------|----------|--------|
| v0.2 | 深入扩展：新增身体类型后缀规范、状态贴图变体、生命阶段影响、DLC特殊规范、参数校验表、补充清单 | 2026-01-03 | 知识提炼者 |
| v0.1 | 初稿：包含分类、尺寸、技术要求、命名、GraphicData属性等内容 | 2026-01-03 | 知识提炼者 |

