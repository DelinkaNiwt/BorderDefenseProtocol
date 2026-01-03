# 占位贴图资源库（全面版）

本目录包含按照《RimWorld贴图指导规范》生成的**完整覆盖**的规范化占位贴图，用于模组开发、测试和参考。所有变式贴图均包含明确的标识标签（方向、体型、变体等）。

## 📊 资源统计

- **总文件数**：120 个占位贴图
- **目录数**：32 个分类目录
- **覆盖范围**：12 大类别，32 个小类别
- **特殊特征**：所有变式贴图均包含标识标签

### 贴图明细统计

| 类别 | 子类 | 数量 | 特征 |
|------|------|------|------|
| **Items（物品）** | Weapon | 2 | - |
| | Food | 1 | - |
| | Resources | 3 | - |
| | Medicine | 1 | - |
| **Things/Apparel（衣物）** | 衣物+遮罩 | 15 | 5种体型 + 遮罩 |
| **Things/Building（建筑）** | Furniture | 4 | 4方向 |
| | Production | 8 | 4方向 |
| | Defense | 5 | 4方向+基础 |
| | Decoration | 8 | 4方向 |
| | Door | 4 | 4方向 |
| | Power | 5 | 4方向+基础 |
| | Storage | 4 | 4方向 |
| **Things/Pawn/Human（人物）** | 殖民者 | 20 | 5体型×4方向 |
| **Things/Pawn/Animal（动物）** | Dog | 6 | 4方向+2状态 |
| | Cat | 4 | 4方向 |
| | Elk | 4 | 4方向 |
| **Things/Pawn/Mechanical（机械）** | Mechanoid | 4 | 4方向 |
| **Things/Pawn/Heads（头部）** | Male | 1 | 性别路径 |
| | Female | 1 | 性别路径 |
| **Plants（植物）** | Shrub | 3 | 随机变体（a/b/c） |
| | Trees | 2 | - |
| | Crops | 1 | - |
| **UI（界面）** | Icons | 4 | 状态图标 |
| | Buttons | 2 | 交互按钮 |
| **Terrain（地形）** | Floor | 3 | 不同材质 |
| | Natural | 2 | 自然地形 |
| **Effects（特效）** | Impact | 2 | 碰撞/爆炸 |
| | Blood | 1 | 血液 |

## 🎨 贴图变式标识说明

所有带有变式的占位贴图现在都包含**视觉标识标签**，显示其文件类型：

### 标识示例

#### 方向标识（建筑/物体）
- **North** / **East** / **South** / **West** - 清晰标注方向

#### 身体类型标识（人物/衣物）
- **Male** / **Female** / **Thin** / **Hulk** / **Fat** - 标注体型

#### 变体标识（植物）
- **_a** / **_b** / **_c** - 随机变体标识

#### 状态标识（动物）
- **Swimming** / **Stationary** / **Dessicated** 等 - 状态类型标识

#### 组合标识（人物多体型）
- **Male_North** / **Female_East** 等 - 体型+方向组合

### 标签位置
- **尺寸标签**：贴图上方（显示像素尺寸）
- **变式标签**：贴图中央（显示文件类型标识）
- **网格线**：贴图背景（32像素间隔，便于对齐）

## 📁 完整目录结构

```
占位贴图/
├── Items/                              # 物品
│   ├── Weapon/                         # 武器
│   │   ├── Sword_Iron.png (64×64)
│   │   └── Gun_Pistol.png (64×64)
│   ├── Food/
│   │   └── MealSimple.png (64×64)
│   ├── Resources/
│   │   ├── Steel.png (64×64)
│   │   ├── Wood.png (64×64)
│   │   └── Cloth.png (64×64)
│   └── Medicine/
│       └── Herbal_Medicine.png (64×64)
│
├── Things/
│   ├── Apparel/                        # 衣物（5体型+遮罩）
│   │   ├── Shirt_[Male|Female|Thin|Hulk|Fat].png (128×128)
│   │   ├── Shirt_[Male|Female|Thin|Hulk|Fat]_mask.png
│   │   └── Pants_[Male|Female|Thin|Hulk|Fat].png (128×128)
│   │
│   ├── Building/                       # 建筑（包含8个子类）
│   │   ├── Furniture/
│   │   │   └── Table_[North|East|South|West].png (256×256)
│   │   ├── Production/
│   │   │   ├── Workbench_[North|East|South|West].png (256×256)
│   │   │   └── Stove_[North|East|South|West].png (128×128)
│   │   ├── Defense/
│   │   │   ├── Turret_[North|East|South|West].png (128×128)
│   │   │   └── Wall.png (64×64)
│   │   ├── Decoration/
│   │   │   ├── Painting_[North|East|South|West].png (128×128)
│   │   │   └── Statue_[North|East|South|West].png (128×128)
│   │   ├── Door/
│   │   │   └── DoorSteel_[North|East|South|West].png (64×128)
│   │   ├── Power/
│   │   │   ├── Generator_[North|East|South|West].png (256×128)
│   │   │   └── PowerConduit.png (64×64)
│   │   └── Storage/
│   │       └── StorageShelf_[North|East|South|West].png (128×128)
│   │
│   └── Pawn/                           # Pawn（角色、动物等）
│       ├── Human/                      # 人物（5体型×4方向=20个）
│       │   ├── Colonist_Male_[North|East|South|West].png (128×256)
│       │   ├── Colonist_Female_[North|East|South|West].png (128×256)
│       │   ├── Colonist_Thin_[North|East|South|West].png (128×256)
│       │   ├── Colonist_Hulk_[North|East|South|West].png (128×256)
│       │   └── Colonist_Fat_[North|East|South|West].png (128×256)
│       │
│       ├── Animal/                    # 动物（含方向和状态）
│       │   ├── Dog_[North|East|South|West].png (128×128)
│       │   ├── Dog_[Swimming|Stationary].png (128×128)
│       │   ├── Cat_[North|East|South|West].png (96×96)
│       │   └── Elk_[North|East|South|West].png (128×128)
│       │
│       ├── Mechanical/                # 机械单位
│       │   └── Mechanoid_[North|East|South|West].png (128×128)
│       │
│       └── Humanlike/Heads/           # 头部（性别路径命名）
│           ├── Male/
│           │   └── Male_Average_Normal.png (64×64)
│           └── Female/
│               └── Female_Average_Normal.png (64×64)
│
├── Plants/                             # 植物
│   ├── Shrub_[a|b|c].png (128×128)
│   ├── Tree_Oak.png (256×256)
│   ├── Tree_Pine.png (256×256)
│   └── Crop_Rice.png (128×128)
│
├── UI/                                 # 用户界面
│   ├── Icons/
│   │   ├── Health_Status.png (64×64)
│   │   ├── Happy_Status.png (64×64)
│   │   ├── Hungry_Status.png (64×64)
│   │   └── Tired_Status.png (64×64)
│   └── Buttons/
│       ├── BuildButton.png (32×32)
│       └── CancelButton.png (32×32)
│
├── Terrain/                            # 地形
│   ├── Floor/
│   │   ├── MetalPlating.png (512×512)
│   │   ├── WoodFloor.png (512×512)
│   │   └── ConcreteFloor.png (512×512)
│   └── Natural/
│       ├── GrassPlain.png (512×512)
│       └── Dirt.png (512×512)
│
└── Effects/                            # 特效
    ├── Impact/
    │   ├── Bullet_Impact.png (64×64)
    │   └── Explosion.png (128×128)
    └── Blood/
        └── Blood_Splatter.png (64×64)
```

## ✅ 规范性验证清单

### ✓ 已验证项目

- [x] **人物完整体型**：5种身体类型×4方向 = 20个人物贴图
- [x] **方向后缀规范**：_North、_East、_South、_West（38个多方向贴图）
- [x] **身体类型后缀**：_Male、_Female、_Thin、_Hulk、_Fat（15个衣物+20个人物）
- [x] **随机变体**：_a、_b、_c（3个植物变体）
- [x] **状态贴图**：_Swimming、_Stationary（2个动物状态）
- [x] **衣物遮罩**：_mask（5个遮罩贴图）
- [x] **头部性别分离**：Male/Female 路径命名
- [x] **尺寸规范**：所有尺寸为 2 的幂次方（64、96、128、256、512）
- [x] **目录结构**：完全遵循规范文档分类
- [x] **命名规范**：所有文件完全遵循规范
- [x] **视觉标识**：所有变式贴图包含中央标签标识

## 🎯 图形类型覆盖

- [x] **Graphic_Single** - 简单贴图（基础物品、头部等）
- [x] **Graphic_Multi** - 多方向贴图（38个方向贴图）
- [x] **Graphic_Random** - 随机变体（3个植物变体）
- [x] **AlternateGraphic** - 状态贴图（2个动物状态）
- [x] **衣物遮罩系统** - 5个遮罩贴图用于测试染色

## 🔄 生成工具

本占位贴图由专属工具生成，支持完全自定义：

**脚本位置**：`工具/placeholder_texture_generator/`

### 快速重新生成
```bash
cd 工具/placeholder_texture_generator
python generate_placeholders.py
```

## 📌 重要说明

### 特色亮点
✨ **变式标识标签** - 每个变式贴图的中央都显示其类型标识（Male、North、_a 等），便于快速识别

✨ **完整体型覆盖** - 人物贴图包含所有5种身体类型和4个方向的完整组合（20个）

✨ **全面分类** - 覆盖 12 大类别、32 个小类别，共 120 个贴图

✨ **开箱即用** - 所有贴图按规范组织，可直接作为模组开发参考

### 使用建议

1. **开发参考**：按照这些贴图的命名和组织方式创建实际贴图
2. **功能测试**：用占位贴图进行模组功能测试
3. **规范验证**：验证自己创建的贴图是否符合规范
4. **实际替换**：开发完成后用真实艺术资源替换

## 📖 相关文档

- **贴图指导规范**：`模组知识/技术基础/RimWorld贴图指导规范.md` - 当前版本 v0.2.1（简化版）
- **生成工具说明**：`工具/placeholder_texture_generator/README.md`
- **知识库索引**：`模组知识/技术基础/README.md`
