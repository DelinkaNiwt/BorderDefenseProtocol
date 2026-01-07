# RimWorld Mod Example - 模组示例

## 元信息

**摘要**：展示RimWorld模组的标准结构、最佳实践和完整示例的参考模组。

**版本**：v1.0
**修改时间**：2026-01-07
**关键词**：示例、模板、教学、最佳实践、参考

---

## 项目说明

这是一个**完全符合RimWorld模组规范**的示例模组，用于演示：

✅ **标准的目录结构**
- 按规范组织Defs/、Languages/、Textures/等
- 清晰的版本管理（1.6/）
- 共享资源与版本特定内容的分离

✅ **完整的配置文件**
- `About/About.xml` - 模组元数据
- `LoadFolders.xml` - 版本加载配置
- `.gitignore` - Git版本控制配置

✅ **详细的代码注释**
- 所有Def文件都包含详尽的注释
- 解释每个配置项的用途
- 提供扩展的建议

✅ **多语言支持**
- 英文本地化示例
- 易于扩展到其他语言

✅ **完整的文档体系**
- `docs/` - 项目工作流文档
- 参考指南和教学资料

---

## 快速开始

### 1. 复制此示例为新项目

```bash
cp -r 模组工程/示例 模组工程/我的新模组
cd 模组工程/我的新模组
```

### 2. 修改About.xml

编辑 `About/About.xml`，更改：
```xml
<name>My Awesome Mod</name>
<author>Your Name</author>
<description>Your mod description</description>
<packageId>your.unique.modid</packageId>
```

### 3. 添加你的内容

在 `1.6/Defs/` 中添加你的Def文件：
- `ThingDefs_YourItems.xml` - 物品定义
- `RecipeDefs_YourRecipes.xml` - 配方
- `ResearchDefs_YourTechs.xml` - 研究

### 4. 更新本地化

在 `Content/Languages/English/Keyed/YourMod.xml` 中添加文本条目

### 5. 测试

在RimWorld中启用模组，进行测试

---

## 目录结构

```
示例/
├── About/
│   ├── About.xml                    # 模组元数据（必需）
│   ├── ModIcon.png                  # 模组图标（推荐）
│   └── Preview.png                  # 预览图（推荐）
│
├── 1.6/                             # RimWorld 1.6 版本内容
│   └── Defs/
│       ├── ThingDefs/               # 物品定义
│       │   └── ThingDefs_ExampleItems.xml
│       ├── RecipeDefs/              # 配方定义
│       ├── ResearchDefs/            # 研究定义
│       └── ...（其他Defs）
│
├── Content/                         # 跨版本共享资源
│   └── Languages/
│       └── English/
│           └── Keyed/
│               └── ExampleMod.xml   # 英文本地化
│
├── docs/                            # 项目文档（工作流）
│   ├── README.md                    # 文档说明
│   ├── 1_创意方案.md                 # 阶段文档
│   ├── 2_需求和系统设计.md           # ...
│   └── ...（按9步工作流组织）
│
├── LoadFolders.xml                  # 版本加载配置
├── README.md                        # 此文件
├── LICENSE                          # 开源许可证
└── .gitignore                       # Git忽略配置
```

### 各目录说明

| 目录 | 说明 |
|------|------|
| **About/** | 模组元数据、图标、预览 |
| **1.6/** | 版本特定内容（RimWorld 1.6） |
| **1.6/Defs/** | 游戏定义文件（XML） |
| **Content/** | 跨版本共享的资源 |
| **Content/Languages/** | 游戏内文本本地化 |
| **docs/** | 项目工作流文档 |

---

## 示例内容

### 已包含的示例Def

1. **Example_Component_Basic** - 简单物品
   - 展示基础物品定义
   - 堆叠、重量、价格配置

2. **Example_Building_SimpleWorkbench** - 工作台
   - 展示可放置的建筑
   - 工作交互配置
   - 建造成本设置

3. **Example_Weapon_SimpleMelee** - 近战武器
   - 展示近战武器定义
   - 伤害配置
   - 工具系统

4. **Example_Weapon_SimpleGun** - 远程武器
   - 展示远程武器定义
   - 射击配置
   - 弹药系统

5. **Example_Projectile_SimpleBullet** - 弹药
   - 展示弹药定义
   - 伤害和速度配置

### 如何扩展

参考这些示例，你可以创建：
- 更复杂的物品（带特殊效果）
- 新的工作类型
- 特殊能力和特性
- 建筑和生产系统
- 等等

详见《RimWorld Defs分类规范》

---

## 配置文件说明

### About.xml

定义模组的基本信息：
- 名称、作者、描述
- 支持的RimWorld版本
- 依赖关系和兼容性
- 模组包ID（唯一标识）

### LoadFolders.xml

控制版本加载：
- 指定各版本加载的文件夹
- DLC条件加载（IfModActive）
- 加载顺序优先级

### .gitignore

排除不需要版本控制的文件：
- 编译输出（bin/、obj/）
- IDE特定文件（.vs/、.vscode/）
- 临时文件

---

## 命名规范

此示例使用前缀 **Example_** 来避免与其他模组冲突。

### 模组前缀

选择一个3-5字母的前缀：
```
Example_Weapon_LaserRifle       ✓ 正确
WeaponLaserRifle               ✗ 缺少前缀
example_weapon_laser_rifle     ✗ 应为PascalCase
```

### Def命名格式

```xml
<defName>[前缀]_[分类]_[名称]</defName>

示例：
Example_Component_Basic         (物品)
Example_Building_Workbench      (建筑)
Example_Weapon_SimpleMelee      (武器)
Example_Recipe_CraftBasic       (配方)
```

详见《RimWorld模组命名规范》

---

## 本地化支持

### 添加新语言

1. 创建新的语言目录：
   ```
   Content/Languages/ChineseSimplified/Keyed/ExampleMod.xml
   ```

2. 复制English版本并翻译：
   ```xml
   <Example_Component_Basic_label>基础组件</Example_Component_Basic_label>
   ```

### 在Defs中引用本地化

使用Keyed键而不是硬编码标签：
```xml
<!-- 不推荐：硬编码 -->
<label>basic component</label>

<!-- 推荐：使用Keyed引用 -->
<label>Example_Component_Basic_label</label>
```

---

## 项目文档

项目文档存放在 `docs/` 目录中，遵循9步工作流：

1. **1_创意方案.md** - 创意总监的设计方案
2. **2_需求和系统设计.md** - 需求分析和系统架构
3. **3_知识获取清单.md** - 项目所需知识
4. **4_设计文档_验证清单.md** - 最终设计和验证
5. **5_原型代码.md** - 原型开发记录
6. **6_测试报告.md** - 测试结果
7. **7_正式版.md** - 正式版发布
8. **8_经验总结.md** - 项目经验
9. **草稿/** - 临时草稿

详见 `docs/README.md`

---

## 发布到Steam工坊

当模组完成后：

1. **准备文件**
   ```
   About/ModIcon.png      (必需)
   About/Preview.png      (推荐)
   About/About.xml        (必需)
   ```

2. **打开RimWorld**
   - 创建或编辑mod
   - 选择此模组文件夹
   - 上传到Steam工坊

3. **获取Published File ID**
   - Steam会自动生成ID
   - 更新About.xml中的publishedFileID

---

## 参考资源

📖 **规范文档**
- [RimWorld模组目录结构标准规范](../../../工作区指导规范/RimWorld模组目录结构标准规范.md)
- [RimWorld Defs分类规范](../../../工作区指导规范/RimWorld_Defs分类规范.md)
- [RimWorld模组命名规范](../../../工作区指导规范/RimWorld模组命名规范.md)

🚀 **快速开始**
- [模组快速启动指南](../../../工作区指导规范/模组快速启动指南.md)

📚 **参考模组**
- 参考资源/模组资源/ - 真实模组示例

🔧 **工具和资源**
- RiMCP - RimWorld代码查询工具
- Visual Studio Code - 推荐的开发工具

---

## 常见问题

**Q: 我的模组在游戏中没有出现？**

A: 检查以下项目：
- [ ] About/About.xml 是否存在
- [ ] LoadFolders.xml 是否正确配置
- [ ] Defs文件是否在正确的版本目录
- [ ] XML文件是否有语法错误

**Q: 如何支持多个RimWorld版本？**

A:
1. 创建1.5/和1.6/目录
2. 在各目录下复制Defs
3. 修改LoadFolders.xml支持多版本

**Q: 我的物品不显示？**

A: 检查：
- [ ] defName是否唯一
- [ ] graphicData中的texPath是否正确
- [ ] XML标签是否闭合

---

## 许可证

本示例模组遵循[开源许可证](LICENSE)

---

## 版本历史

| 版本 | 日期 | 改动 | 作者 |
|------|------|------|------|
| v1.0 | 2026-01-07 | 初版发布。完整的模组示例，包含5个Def示例、配置文件和详尽注释。 | 知识提炼者 |

---

## 反馈和建议

如有改进建议，请联系项目维护者或在相关社区提出。

**Happy Modding! 🎮**

---

*Last Updated: 2026-01-07*
