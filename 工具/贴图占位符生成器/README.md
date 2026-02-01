# RimWorld 贴图占位符生成器

> ⚠️ **占位贴图工具** - 模组开发验证阶段快速生成规范贴图
>
> 自动生成带网格线和尺寸标注的占位贴图，支持多种后缀类型，验证 XML 配置、路径和命名规范。
>
> **最终应由真实贴图替换这些占位符**

> **v1.1 更新：** 新增灵活的 `directions` 参数，精确控制生成数量。所有旧示例保持兼容，无需修改。

## ✅ 核心功能

| 类型 | 说明 | 文件数 | 示例 |
|------|------|--------|------|
| **Single** | 单张贴图，无后缀 | 1 | `Sword_Iron.png` |
| **Multi** | 4方向贴图 | 4 | `Door_north.png`, `Door_east.png` 等 |
| **Random** | 随机变体贴图 | 3-8 | `Shrub_a.png`, `Shrub_b.png` 等 |
| **Apparel** | 衣物 - 体型×方向组合 | **20** | `Shirt_Male_north.png`, `Shirt_Female_east.png` 等 |

## 📌 命名规范（重点）

根据 **RimWorld贴图指导规范v1.0.2** 的源代码验证：

```yaml
方向后缀:
  必须小写: _north, _east, _south, _west
  ✗ 错误: _North, _East, _South, _West

衣物后缀:
  体型大写: _Male, _Female, _Thin, _Hulk, _Fat

衣物命名顺序:
  <base>_<bodytype>_<direction>
  例如: TShirt_Male_north.png (体型→方向)
  ✗ 错误: TShirt_north_Male.png (顺序错误)
```

## 🚀 快速开始

### 1. 安装依赖
```bash
pip install Pillow
```

### 2. 使用示例配置
```bash
# 生成单张贴图
python texture_generator.py --config 示例_单张贴图.json --output ./output/

# 生成4方向建筑
python texture_generator.py --config 示例_4方向建筑.json --output ./output/

# 生成衣物贴图（体型×方向 = 20个文件）
python texture_generator.py --config 示例_衣物贴图.json --output ./output/

# 生成混合项目（所有类型）
python texture_generator.py --config 示例_混合项目.json --output ./output/
```

### 3. 命令行快速生成
```bash
# 单张贴图
python texture_generator.py --width 128 --height 128 --name Sword_Iron \
  --suffix single --output ./output/

# 4方向贴图
python texture_generator.py --width 256 --height 256 --name Door_Steel \
  --suffix multi --output ./output/

# 衣物贴图（自动生成 5体型×4方向）
python texture_generator.py --width 128 --height 128 --name Apparel_TShirt \
  --suffix apparel --output ./output/
```

### 4. 查看所有选项
```bash
python texture_generator.py --help
```

## 📋 示例文件说明

### 基础示例（v1.0+，完全兼容 v1.1）

| 文件 | 用途 | v1.1 兼容性 |
|------|------|----------|
| `示例_单张贴图.json` | 单张贴图配置（如武器、UI图标） | ✅ 无需修改 |
| `示例_4方向建筑.json` | 建筑物4方向贴图配置 | ✅ 无需修改 |
| `示例_衣物贴图.json` | 衣物贴图配置（完整 5体型×4方向=20张） | ✅ 无需修改 |
| `示例_随机变体.yaml` | 随机变体贴图配置（植物、装饰） | ✅ 需 PyYAML |
| `示例_混合项目.json` | 完整项目示例（包含所有类型） | ✅ 无需修改 |

### v1.1 新增示例（展示灵活配置）

| 文件 | 用途 | 特点 |
|------|------|------|
| `示例_灵活配置.json` | 综合演示各种灵活配置 | 7 个配置示例，生成 15 个文件 |
| `示例_衣物贴图_v11.json` | 专门演示 directions 参数 | 展示 1/4/20 张三种配置方式 |

### 快速开始推荐

**刚开始使用？** 运行基础示例了解各种类型：
```bash
python texture_generator.py --config 示例_混合项目.json --output ./output/
```

**需要精确控制？** 参考新示例：
```bash
python texture_generator.py --config 示例_灵活配置.json --output ./output/
```

**了解 directions 用法？** 查看：
```bash
python texture_generator.py --config 示例_衣物贴图_v11.json --output ./output/
```

---

**兼容性提示：** v1.0 的所有示例在 v1.1 中仍完全可用，无需修改。新的 `directions` 参数是可选的。

## 🎨 色彩预设

```yaml
预设名称: blue, red, green, purple, yellow, gray

自定义RGB:
  color_scheme:
    r: 100
    g: 150
    b: 200
```

## 📐 支持的尺寸（2的幂次方）

```
64, 128, 256, 512, 1024
```

## ⚙️ 配置参数精细控制

### Apparel 类型 - 灵活指定体型和方向

**参数说明：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `suffix_names` | 数组 | `["_Male", "_Female", "_Thin", "_Hulk", "_Fat"]` | 要生成的体型列表 |
| `directions` | 数组 | `["_north", "_east", "_south", "_west"]` | **新增** - 要生成的方向列表 |

**灵活示例：**

1. **生成 1 张（女性正面）**
   ```json
   {
     "suffix_type": "apparel",
     "suffix_names": ["_Female"],
     "directions": ["_north"]
   }
   ```
   生成：`Apparel_Name_Female_north.png` (1张)

2. **生成 4 张（女性全方向）**
   ```json
   {
     "suffix_type": "apparel",
     "suffix_names": ["_Female"]
     // directions 不指定，使用默认4个方向
   }
   ```
   生成：`Apparel_Name_Female_north.png` 等 (4张)

3. **生成 8 张（男性和女性，仅北西两个方向）**
   ```json
   {
     "suffix_type": "apparel",
     "suffix_names": ["_Male", "_Female"],
     "directions": ["_north", "_west"]
   }
   ```
   生成：`Apparel_Name_Male_north.png`, `Apparel_Name_Male_west.png`, `Apparel_Name_Female_north.png`, `Apparel_Name_Female_west.png` (8张)

4. **生成 20 张（所有体型所有方向，完整套装）**
   ```json
   {
     "suffix_type": "apparel",
     "suffix_names": ["_Male", "_Female", "_Thin", "_Hulk", "_Fat"]
     // directions 不指定，使用默认4个方向
     // 5 体型 × 4 方向 = 20 张
   }
   ```

### Multi 类型 - 灵活指定方向

类似地，Multi 类型也支持灵活指定方向：

```json
{
  "suffix_type": "multi",
  "suffix_names": ["_north", "_west"]  // 仅生成北和西两个方向
}
```
生成：`Building_Name_north.png`, `Building_Name_west.png` (2张)

### Random 类型 - 灵活指定变体

```json
{
  "suffix_type": "random",
  "suffix_names": ["_a", "_b"]  // 仅生成两个变体
}
```
生成：`Plant_Name_a.png`, `Plant_Name_b.png` (2张)

---

**核心原则：** 用户指定多少就生成多少，绝不多生一张，也绝不少生。

## 🔧 高级用法

### 如何添加掩膜贴图？

衣物掩膜用于颜色系统。如果需要生成掩膜贴图：

1. **方式A：手工添加后缀**
   - 运行脚本生成基础贴图：`Shirt_Male_north.png` 等
   - 复制并重命名：`Shirt_Male_north.png` → `Shirt_Male_northm.png`
   - 编辑掩膜贴图（只保留可染色区域）
   - 在 XML 中指定 maskPath：
     ```xml
     <graphicData>
       <texPath>Things/Apparel/Shirt</texPath>
       <maskPath>Things/Apparel/Shirt_Mask</maskPath>
     </graphicData>
     ```

2. **方式B：使用配置手工扩展**
   - 修改示例配置，添加两遍循环生成掩膜版本
   - 或使用 Python 脚本自行扩展

### 如何添加头部贴图？

头部贴图使用**文件夹分离**（非后缀系统）：

```
Things/Pawn/Humanlike/Heads/
├── Male/
│   ├── Average_Normal.png
│   ├── Narrow_Normal.png
│   └── ...
├── Female/
│   ├── Average_Normal.png
│   └── ...
└── ...
```

配置中：
```xml
<headGraphicsPath>Things/Pawn/Humanlike/Heads</headGraphicsPath>
```

游戏自动加载 `/Male/` 和 `/Female/` 子文件夹中的贴图。

如果需要生成占位符：
1. 手工创建目录结构
2. 运行脚本两次：一次用 `Things/Pawn/Humanlike/Heads/Male/Head`，一次用 `Female`
3. 或自行编写扩展脚本

## 📊 输出说明

脚本执行完成后生成：

- `生成报告.txt` - 详细的生成日志
- `生成文件列表.json` - 所有生成的文件列表
- 生成的 PNG 文件（放在 `--output` 指定的目录）

## ⚠️ 注意事项

- ✅ 所有后缀必须符合 RimWorld 命名规范
- ✅ 尺寸必须是 2 的幂次方（64, 128, 256, 512, 1024）
- ✅ 文件名只能使用英文字母、数字、下划线
- ✅ 这些是**占位符贴图**，仅用于验证配置，最终需替换为真实贴图

## 🔗 相关文档

### 本项目文档
- **CHANGELOG.md** - 详细的 v1.1 修改日志
- **示例文件更新说明.md** - 示例文件兼容性说明
- **VERIFICATION_REPORT.txt** - 完整的验证报告

### RimWorld 贴图规范
- **RimWorld贴图指导规范.md** - 完整的贴图规范（位于 `模组知识/技术基础/`）

---

版本：v1.1
创建日期：2026-01-24
最后更新：2026-01-24
