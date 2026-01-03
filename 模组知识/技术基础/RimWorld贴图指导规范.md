# RimWorld贴图指导规范（简化版）

## 📋 元信息

**摘要：** RimWorld贴图资源的核心指导文档，包含分类、尺寸、技术要求、命名规范、GraphicData配置、身体类型后缀、状态贴图和参数校验。

**版本号：** v0.2.1（简化版）

**修改时间：** 2026-01-03

**关键词：** 贴图尺寸, 命名规范, GraphicData, 身体类型后缀, 状态贴图, 参数校验

**标签：** [定稿]

---

## 一、贴图分类

### 用途分类
| 类型 | 用途 | 示例 |
|------|------|------|
| **物品** | 可拾取物品 | 武器、衣物、食物 |
| **建筑** | 放置在地图上 | 家具、机器、防御设施 |
| **角色** | 殖民者和NPC | 人类、动物、机械单位 |
| **UI** | 界面元素 | 图标、按钮 |

### 图形渲染类型
| 类型 | 说明 |
|------|------|
| **Graphic_Single** | 单张静态贴图 |
| **Graphic_Multi** | 多方向贴图（自动按方向选择） |
| **Graphic_Random** | 随机变体（增加视觉多样性） |
| **Graphic_Fleck** | 粒子特效 |

---

## 二、贴图尺寸规范

### 标准尺寸
| 物品类型 | 尺寸 | 物品类型 | 尺寸 |
|---------|------|---------|------|
| 小物品 | 64×64px | 建筑（中） | 256×256px |
| 标准物品 | 128×128px | 建筑（大） | 512×512px |
| 建筑（小） | 128×128px | 角色/Pawn | 128×256px |
| UI图标 | 32-64px | 树木 | 256×256px |

### 规则
✓ **必须满足**：2的幂次方尺寸（64, 128, 256, 512）获得最佳优化
⚠️ **注意**：非2的幂次方尺寸会失去mipmap优化和产生警告

---

## 三、文件格式与技术要求

### 格式
| 格式 | 优先级 | 说明 |
|------|--------|------|
| **PNG** | ✓ 推荐 | 无损压缩，支持透明度 |
| **DDS** | ✓ 推荐 | 游戏优化格式，性能最佳 |
| **JPG** | 中等 | 有损压缩，不支持透明 |
| **TGA** | ✗ 不支持 | 避免使用 |

### 颜色空间
- **色彩模式**：RGB 或 RGBA（需要透明度时用RGBA）
- **色深**：8 bits/channel（32bit RGBA）
- **自动处理**：DXT压缩、Mipmap生成、Trilinear过滤（默认启用）

---

## 四、命名规范

### 基础路径结构
```
<类别>/<类型>/<具体名称>
Items/Weapon/Sword_Iron
Things/Building/Furniture/Table
Things/Pawn/Human/Colonist_North
```

### 后缀规范
| 用途 | 后缀 | 示例 |
|------|------|------|
| **方向**（Graphic_Multi） | _North / _East / _South / _West | Door_North.png |
| **随机变体**（Graphic_Random） | _a, _b, _c 或 _1, _2, _3 | Shrub_a.png, Shrub_b.png |
| **身体类型**（衣物） | _Male / _Female / _Thin / _Hulk / _Fat | Shirt_Male.png |
| **性别**（头部） | 路径中包含 | Heads/Male/Male_Average |

### DefName与texPath对应
- **DefName**：唯一标识符（PascalCase）
- **texPath**：贴图相对路径（无Textures/前缀，无文件扩展名）
- **路径分隔符**：使用 `/` 而非 `\`

---

## 五、GraphicData配置要点

### 核心必需属性
```xml
<graphicData>
  <texPath>Items/Weapon/Sword</texPath>
  <graphicClass>Graphic_Single</graphicClass>
  <drawSize>(1, 1)</drawSize>
</graphicData>
```

| 属性 | 作用 | 常用值 |
|------|------|--------|
| **texPath** | 贴图文件路径 | Items/Weapon/Sword |
| **graphicClass** | 渲染类型 | Graphic_Single / Multi / Random |
| **drawSize** | 网格绘制尺寸 | (1, 1) 或 (2, 2) 等 |
| **shaderType** | 渲染着色器 | Cutout（默认）/ CutoutComplex / Transparent |
| **color** | 颜色调整 | (1,1,1,1)=无色调 |

### 高级属性
| 属性 | 作用 |
|------|------|
| **drawRotated** | 是否随对象旋转而旋转 |
| **allowFlip** | 是否允许镜像翻转 |
| **maskPath** | 遮罩贴图（用于染色） |
| **renderQueue** | 渲染顺序（0-3999） |
| **shadowData** | 阴影体积定义 |

---

## 六、实际配置示例

### 简单物品
```xml
<graphicData>
  <texPath>Items/Weapon/Sword_Iron</texPath>
  <graphicClass>Graphic_Single</graphicClass>
  <drawSize>(0.8, 0.8)</drawSize>
</graphicData>
```

### 可旋转建筑（4方向）
```xml
<graphicData>
  <texPath>Things/Building/Workbench</texPath>
  <graphicClass>Graphic_Multi</graphicClass>
  <drawSize>(2, 2)</drawSize>
  <drawRotated>true</drawRotated>
  <shadowData><volume>(0.5, 0.8, 0.5)</volume></shadowData>
</graphicData>
```
**贴图文件**：Workbench_North.png、Workbench_East.png 等

### 随机变体
```xml
<graphicData>
  <texPath>Plants/Shrub</texPath>
  <graphicClass>Graphic_Random</graphicClass>
</graphicData>
```
**贴图文件**：Shrub_a.png、Shrub_b.png、Shrub_c.png

### 衣物染色
```xml
<graphicData>
  <texPath>Things/Apparel/Shirt</texPath>
  <maskPath>Things/Apparel/Shirt_mask</maskPath>
  <graphicClass>Graphic_Multi</graphicClass>
  <color>(0.7, 0.5, 0.3, 1)</color>
</graphicData>
```

---

## 七、常见问题速查

| 问题 | 原因 | 解决 |
|------|------|------|
| 贴图不显示 | 路径错误 | 检查大小写和斜杠方向 |
| 贴图变形 | drawSize比例不对 | 调整为实际宽高比 |
| 方向错误 | Graphic_Multi顺序错 | 确保：North→East→South→West |
| 颜色异常 | color值设置错 | 使用 (1,1,1,1) 作基准 |
| 透明异常 | 着色器选择错 | 使用 Cutout 或 CutoutComplex |

---

## 八、身体类型与贴图后缀

### 衣物身体类型（必需）
衣物自动添加身体类型后缀：`texPath + "_" + bodyType.defName`

| 身体类型 | 后缀 | 代码位置 |
|---------|------|---------|
| **Male** | `_Male` | RimWorld.ApparelGraphicRecordGetter:19 |
| **Female** | `_Female` | 同上 |
| **Thin** | `_Thin` | 同上 |
| **Hulk** | `_Hulk` | 同上 |
| **Fat** | `_Fat` | 同上 |

**示例**：texPath="Things/Apparel/Shirt" → 加载 Shirt_Male.png、Shirt_Female.png 等

**例外**：Overhead、EyeCover、Pack、Placeholder 层不使用后缀

### 头部性别命名（非后缀）
头部使用**路径命名**而非后缀：
```
Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal.png
Things/Pawn/Humanlike/Heads/Female/Female_Average_Normal.png
```

---

## 九、状态相关的贴图变体

### AlternateGraphic系统
不同状态可使用不同贴图：

| 状态 | 属性名 | 用途 | 验证 |
|------|--------|------|------|
| **Swimming** | swimmingTexPath | 水中显示 | Verse.AlternateGraphic.cs |
| **Dessicated** | dessicatedTexPath | 尸体干枯 | 同上 |
| **Rotting** | rottingTexPath | 尸体腐烂 | 同上 |
| **Stationary** | stationaryTexPath | 静止状态 | 同上 |

### 生命阶段（不使用贴图后缀）
生命阶段通过缩放因子影响渲染，**同一套贴图**通过 `bodySizeFactor` 缩放显示：
- **Baby**：bodySizeFactor ≈ 0.3
- **Child**：bodySizeFactor ≈ 0.6
- **Teenager**：bodySizeFactor ≈ 0.85
- **Adult**：bodySizeFactor = 1.0

---

## 十、DLC特殊贴图

### Biotech
- **突变体**：使用 `MutantDef.bodyTypeGraphicPaths` 定义多身体类型贴图

### Ideology
- **风格系统**：建筑可使用风格特定贴图变体

### Royalty
- **皇家服装**：遵循标准身体类型后缀规范

---

## 十一、参数校验（核心20项）

| # | 参数 | 验证状态 | 代码位置 |
|---|------|--------|---------|
| 1 | GraphicData.texPath | ✓ | Verse.GraphicData.cs |
| 2 | GraphicData.graphicClass | ✓ | 同上 |
| 3 | GraphicData.drawSize | ✓ | 同上 |
| 4 | GraphicData.shaderType | ✓ | 同上 |
| 5 | GraphicData.color | ✓ | 同上 |
| 6 | GraphicData.drawRotated | ✓ | 同上 |
| 7 | 衣物身体类型后缀 | ✓ | RimWorld.ApparelGraphicRecordGetter:19 |
| 8 | 身体类型定义（5种） | ✓ | RimWorld.WornGraphicData.cs |
| 9 | 头部性别命名 | ✓ | Verse.HeadTypeDef.cs |
| 10 | Swimming状态贴图 | ✓ | Verse.AlternateGraphic.cs |
| 11 | Dessicated状态贴图 | ✓ | 同上 |
| 12 | Rotting状态贴图 | ✓ | 同上 |
| 13 | Stationary状态贴图 | ✓ | 同上 |
| 14 | 生命阶段bodySizeFactor | ✓ | RimWorld.LifeStageDef:30 |
| 15 | 特殊形态贴图 | ✓ | RimWorld.MutantDef.cs |
| 16 | 方向贴图后缀 | ✓ | Verse.Rot4.cs |
| 17 | Mipmap自动生成 | ✓ | Verse.ModContentLoader |
| 18 | DXT自动压缩 | ✓ | Verse.Texture2D |
| 19 | 2的幂次方尺寸优化 | ✓ | 推荐实践 |
| 20 | Alpha通道透明 | ✓ | Verse.Color.cs |

---

## 十二、已发现关键内容清单

### 已补充（5项）
- ✓ 身体类型后缀规范（_Male/_Female/_Thin/_Hulk/_Fat）
- ✓ 头部性别命名规范（路径中包含性别，非后缀）
- ✓ 状态贴图变体系统（swimming/dessicated/rotting/stationary）
- ✓ 生命阶段渲染（bodySizeFactor缩放，非贴图后缀）
- ✓ 特殊形态贴图（MutantDef的bodyTypeGraphicPaths）

### 待补充（8项）
- ❓ Ideology风格系统具体命名规范
- ❓ renderQueue值的标准对照表
- ❓ shadowData与贴图尺寸的对应关系
- ❓ maskPath应用场景（衣物外的其他用途）
- ❓ DLC衣物特殊需求细节
- ❓ 大型武器特殊处理（CompOversizedWeapon）
- ❓ 机械单位的贴图规范
- ❓ 异常生物的贴图规范

---

## 📝 历史记录

| 版本号 | 改动 | 修改时间 | 修改者 |
|--------|------|----------|--------|
| v0.2.1 | 简化版：应用奥卡姆剃刀原则，删除冗余示例和详细说明，保留核心知识 | 2026-01-03 | 知识提炼者 |
| v0.2 | 深入扩展：新增身体类型后缀、状态贴图、生命阶段、DLC规范、参数校验、补充清单 | 2026-01-03 | 知识提炼者 |
| v0.1 | 初稿：分类、尺寸、技术要求、命名、GraphicData配置 | 2026-01-03 | 知识提炼者 |
