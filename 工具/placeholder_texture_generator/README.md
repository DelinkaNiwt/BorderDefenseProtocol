# 占位贴图生成器（Placeholder Texture Generator）

用于按照 RimWorld 贴图指导规范生成规范的占位贴图，用于模组开发测试和原型制作。

## 📋 功能说明

- **自动生成分类占位贴图**：按照贴图指导规范的分类自动生成所有类别的占位贴图
- **规范命名**：遵循文档中的命名规范（方向后缀、身体类型后缀、状态后缀等）
- **标准尺寸**：使用规范文档中定义的标准像素尺寸
- **目录结构**：自动创建规范的目录结构
- **多种变体**：支持方向变体、身体类型变体、随机变体、状态变体等
- **遮罩生成**：为衣物自动生成遮罩贴图

## 📁 生成的贴图分类

### 物品（Items）
- `Items/Weapon/` - 武器
  - Sword_Iron.png (64×64)
  - Gun_Pistol.png (64×64)

### 衣物（Apparel）
- `Things/Apparel/` - 衣物（包含5种身体类型和遮罩）
  - Shirt_[Male|Female|Thin|Hulk|Fat].png (128×128)
  - Shirt_[Male|Female|Thin|Hulk|Fat]_mask.png (遮罩)
  - Pants_[Male|Female|Thin|Hulk|Fat].png (128×128)

### 建筑（Buildings）
- `Things/Building/Furniture/` - 家具
  - Table_[North|East|South|West].png (256×256)
- `Things/Building/Production/` - 生产设施
  - Workbench_[North|East|South|West].png (256×256)
  - Stove_[North|East|South|West].png (128×128)
- `Things/Building/Defense/` - 防御设施
  - Turret_[North|East|South|West].png (128×128)

### 角色（Pawn）
- `Things/Pawn/Human/` - 人物
  - Colonist_[North|East|South|West].png (128×256)
- `Things/Pawn/Humanlike/Heads/Male/` - 男性头部
  - Male_Average_Normal.png (64×64)
- `Things/Pawn/Humanlike/Heads/Female/` - 女性头部
  - Female_Average_Normal.png (64×64)
- `Things/Pawn/Animal/` - 动物（包含状态变体）
  - Dog_[North|East|South|West].png (128×128)
  - Dog_[Swimming|Stationary].png (128×128 - 状态贴图)

### 植物（Plants）
- `Plants/` - 植物
  - Shrub_[a|b|c].png (128×128 - 随机变体)
  - Tree_Oak.png (256×256)

### UI图标（UI）
- `UI/Icons/` - UI图标
  - Health_Status.png (64×64)
  - Happy_Status.png (64×64)

### 地形（Terrain）
- `Terrain/Floor/` - 地面
  - MetalPlating.png (512×512)

## 🚀 使用方法

### 前置需求
```bash
pip install Pillow
```

### 运行脚本
```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio\工具\placeholder_texture_generator"
python generate_placeholders.py
```

### 输出位置
生成的占位贴图将自动保存到：
```
C:\NiwtDatas\Projects\RimworldModStudio\参考资源\通用资源\占位贴图\
```

## 📐 贴图规范

### 像素尺寸
所有生成的贴图严格遵循规范文档中的尺寸要求：
- 小物品：64×64px
- 标准物品：128×128px
- 建筑（小）：128×128px
- 建筑（中）：256×256px
- 建筑（大）：512×512px
- 角色/Pawn：128×256px
- UI图标：64×64px
- 树木：256×256px

### 命名规范
遵循规范文档中的命名约定：
- **方向后缀**：`_North`、`_East`、`_South`、`_West`（用于 Graphic_Multi）
- **身体类型后缀**：`_Male`、`_Female`、`_Thin`、`_Hulk`、`_Fat`（用于衣物）
- **随机变体**：`_a`、`_b`、`_c` 或 `_1`、`_2`、`_3`（用于 Graphic_Random）
- **状态后缀**：`_Swimming`、`_Stationary` 等（用于 AlternateGraphic）
- **性别路径**：头部使用路径命名而非后缀
- **遮罩后缀**：`_mask`（用于衣物染色）

### 目录结构
遵循规范文档中的路径分类：
```
Items/<ItemType>/<ItemName>.png
Things/Apparel/<ApparelName>.png
Things/Building/<BuildingType>/<BuildingName>.png
Things/Pawn/<PawnType>/<PawnName>.png
Things/Pawn/Humanlike/Heads/<Gender>/<HeadName>.png
Plants/<PlantName>.png
UI/Icons/<IconName>.png
Terrain/<TerrainType>/<TerrainName>.png
```

## 🎨 占位贴图特征

生成的占位贴图具有以下特征，便于测试和识别：

1. **背景颜色**：不同类别使用不同颜色便于区分
   - 武器：灰色 (#888888)
   - 衣物：红色 (#FF6B6B)
   - 建筑：棕色/灰色等
   - 头部：肤色 (#F4A460)
   - 植物：绿色 (#2D5016)

2. **网格线**：显示像素位置和尺寸参考
   - 每 32 像素一条网格线
   - 便于验证尺寸和对齐

3. **尺寸标签**：贴图中央显示像素尺寸
   - 例如：`128×128`
   - 便于快速识别贴图规格

4. **遮罩贴图**：灰度遮罩，用于测试衣物染色功能

## 📝 扩展说明

### 添加新的占位贴图

在 `generate_placeholders.py` 中修改 `PLACEHOLDER_TEXTURES` 字典即可：

```python
'Your/Path': {
    'YourTexture': {
        'size': (128, 128),
        'color': '#RRGGBB',
        'label': 'Your Label',
        'directions': ['North', 'East', 'South', 'West'],  # 可选
        'variants': ['a', 'b', 'c'],  # 可选
        'states': {'swimming': True},  # 可选
        'mask': True,  # 可选，仅衣物使用
    },
},
```

然后重新运行脚本。

### 自定义颜色

修改 `color` 字段使用任意 16 进制颜色值（`#RRGGBB` 格式）。

## 🔍 验证清单

运行脚本后，可以验证：

- [ ] 所有目录结构正确创建
- [ ] 所有贴图文件按规范命名
- [ ] 像素尺寸符合规范要求
- [ ] 方向后缀完整（North/East/South/West）
- [ ] 身体类型后缀完整（Male/Female/Thin/Hulk/Fat）
- [ ] 衣物遮罩贴图已生成
- [ ] 状态贴图已生成

## 📌 相关文档

- **贴图指导规范**：`模组知识/技术基础/RimWorld贴图指导规范.md`
- **参数校验表**：文档中第十二章
- **命名规范**：文档中第四章
- **尺寸规范**：文档中第二章

## 🛠️ 故障排查

### 找不到输出目录
确保路径存在：`参考资源/通用资源/`

### PIL 导入错误
安装依赖：`pip install Pillow`

### 权限错误
确保对输出目录有写入权限

## 📊 输出示例

```
🎨 开始生成占位贴图...
📁 输出路径: C:\...\参考资源\通用资源\占位贴图

📂 Items/Weapon/
  ✓ Sword_Iron.png (64×64)
  ✓ Gun_Pistol.png (64×64)

📂 Things/Apparel/
  ✓ Shirt_[Male|Female|Thin|Hulk|Fat].png (128×128)
  ✓ Shirt_[Male|Female|Thin|Hulk|Fat]_mask.png (遮罩)

... [更多分类]

============================================================
✅ 生成完成！
📊 统计信息:
   - 创建目录数: 12
   - 创建贴图文件数: 48
   - 总文件数: 48
============================================================
```

## 💡 提示

- 占位贴图仅用于测试和开发，实际模组需要用真实贴图替换
- 可以在规范文档的基础上扩展脚本，添加更多贴图分类
- 脚本完全遵循《RimWorld 贴图指导规范》中的所有要求
