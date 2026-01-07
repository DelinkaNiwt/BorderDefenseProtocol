# About.xml 详细配置指南

## 元信息

**摘要**：RimWorld模组的About.xml配置文件完整参考。涵盖所有可用标签、依赖管理、加载顺序、包ID命名等详细说明。

**版本**：v1.0
**修改时间**：2026-01-07
**关键词**：About.xml, 模组元数据, 依赖管理, 加载顺序, 配置参考
**标签**：[定稿]

---

## 概述

`About.xml` 是RimWorld模组的**必需**配置文件，位于 `About/About.xml`。它定义了模组的基本信息、依赖关系、兼容性和加载顺序。

### 重要提示

- 此文件的格式必须严格正确，否则游戏无法识别模组
- 所有文本内容**应该优先使用本地化**（不在About.xml中硬编码文本，改为在Languages中定义）
- PackageId必须**全局唯一**
- 依赖关系必须**正确指定**，否则可能加载顺序错误

---

## 文件位置和基本格式

### 位置

```
YourMod/
├── About/
│   └── About.xml          ← 必须在此位置
├── 1.6/
├── LoadFolders.xml
└── ...
```

### 基本XML结构

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <!-- 所有配置标签放在这里 -->
</ModMetaData>
```

---

## 标签详解

### 1. 基本信息标签

#### `<name>` - 模组显示名称（必需）

```xml
<name>我的模组名称</name>
```

**说明**：
- 游戏和Steam工坊显示的模组名称
- 可以包含中文、空格、特殊字符
- 推荐长度：2-50字符
- 应该简洁、能描述模组功能

**示例**：
```xml
<name>激光武器系统</name>
<name>Advanced Storage Framework</name>
<name>高级建筑扩展</name>
```

---

#### `<author>` - 作者名称（推荐）

```xml
<author>你的名字</author>
```

**说明**：
- 模组作者名称
- 可以是个人或团队
- 游戏中显示在模组详情中

**示例**：
```xml
<author>小王</author>
<author>Development Team</author>
<author>社区贡献者</author>
```

---

#### `<description>` - 模组描述（推荐）

```xml
<description>模组的详细说明</description>
```

**说明**：
- 模组功能和特性的详细描述
- Steam工坊会显示此内容
- 支持多行文本
- 推荐200-500字

**示例**：
```xml
<description>
这个模组添加了先进的能量储存系统。

特性：
- 支持多种能量类型
- 自动负载均衡
- 与原版建筑兼容

版本要求：RimWorld 1.5+
</description>
```

---

### 2. 版本信息标签

#### `<modVersion>` - 模组版本号（推荐）

```xml
<modVersion>1.0.0</modVersion>
```

**格式**：遵循语义化版本（Semantic Versioning）
```
主版本.次版本.补丁版本
1.0.0
```

**说明**：
- 主版本：重大更新
- 次版本：新功能、改进
- 补丁版本：bug修复

**示例**：
```xml
<modVersion>1.0.0</modVersion>  <!-- 初版发布 -->
<modVersion>1.1.0</modVersion>  <!-- 新增功能 -->
<modVersion>1.1.2</modVersion>  <!-- 修复bug -->
<modVersion>2.0.0</modVersion>  <!-- 大版本更新 -->
```

---

#### `<supportedVersions>` - 支持的游戏版本（必需）

```xml
<supportedVersions>
  <li>1.6</li>
</supportedVersions>
```

**说明**：
- 列出此模组支持的所有RimWorld版本
- 每个版本一个 `<li>` 标签
- 游戏会根据版本选择加载

**常见版本**：
```xml
<supportedVersions>
  <li>1.4</li>
  <li>1.5</li>
  <li>1.6</li>
</supportedVersions>
```

**注意**：
- 只列出真正支持的版本
- 即使代码完全相同，不同版本也应在LoadFolders中管理
- 不支持自动向下兼容

---

### 3. 依赖关系标签

#### `<dependencies>` - 模组依赖（可选）

```xml
<dependencies>
  <li>Ludeon.RimWorld.Royalty</li>
  <li>Ludeon.RimWorld.Biotech</li>
  <li>someauthor.somemod</li>
</dependencies>
```

**说明**：
- 列出此模组**必须依赖**的其他模组或DLC
- 游戏会确保依赖模组先加载
- 如果依赖模组未激活，此模组会被禁用

**官方DLC的Package ID**：
```
Ludeon.RimWorld.Royalty      （贵族DLC）
Ludeon.RimWorld.Biotech      （生物技术DLC）
Ludeon.RimWorld.Ideology     （意识形态DLC）
Ludeon.RimWorld.Odyssey      （奥德赛DLC）
Ludeon.RimWorld.Anomaly      （异常DLC）
```

**第三方模组示例**：
```xml
<dependencies>
  <!-- 官方DLC -->
  <li>Ludeon.RimWorld.Royalty</li>

  <!-- 框架类模组 -->
  <li>ludeon.rimworld.humanoidalienraces</li>
  <li>vanillaexpanded.framework</li>

  <!-- 其他必需模组 -->
  <li>harmony</li>
</dependencies>
```

**使用场景**：
- ✅ 你的模组使用了某个框架（如HAR）
- ✅ 你的模组需要某个DLC的功能
- ✅ 你的模组是另一个模组的扩展

**不要滥用**：
- ❌ 不要为了"推荐"而添加依赖
- ❌ 不要添加不是必需的依赖
- ❌ 使用 `<incompatibleWith>` 代替冲突关系

---

#### `<incompatibleWith>` - 不兼容模组（可选）

```xml
<incompatibleWith>
  <li>someauthor.conflictingmod</li>
</incompatibleWith>
```

**说明**：
- 列出与此模组**冲突**的其他模组
- 游戏会检测冲突并警告玩家
- 不会自动禁用冲突模组

**使用场景**：
- 两个模组修改同一个系统
- 两个模组添加相同的内容
- 加载两个会导致崩溃

**示例**：
```xml
<incompatibleWith>
  <li>someauthor.oldversion</li>
  <li>another.conflicting.mod</li>
</incompatibleWith>
```

---

### 4. 包ID标签

#### `<packageId>` - 模组全局唯一ID（推荐）

```xml
<packageId>yourname.modname</packageId>
```

**说明**：
- 模组的**全局唯一标识符**
- 用于避免模组冲突
- Steam工坊会根据此ID管理版本

**命名规范**（推荐）：
```
作者ID.模组名称(小写，用点或下划线分隔)

示例：
zhangsan.lasertechnology
zhangsan.advanced_storage
xiaoli.race_modification
devteam.framework_core
```

**格式要求**：
- 全小写
- 用英文字母、数字、点、下划线
- 全局唯一（搜索Steam确保没有重复）
- 长度2-50字符

**重要提示**：
- 一旦发布到Steam，**不要改变packageId**
- 改变packageId会导致Steam视为新模组
- 玩家的存档配置会丢失

---

#### `<publishedFileID>` - Steam工坊ID（自动生成）

```xml
<publishedFileID>1234567890</publishedFileID>
```

**说明**：
- Steam工坊的唯一ID
- **首次发布时留空或删除此标签**
- Steam自动生成后，复制粘贴此ID
- 用于Steam更新管理

**获取方式**：
1. 创建/编辑模组在游戏中
2. 上传到Steam工坊
3. Steam生成ID后，复制到About.xml
4. 下次更新时保持此ID不变

---

### 5. 加载顺序标签

#### `<loadBefore>` - 在某模组之前加载

```xml
<loadBefore>
  <li>someauthor.somemod</li>
  <li>someauthor.anothermod</li>
</loadBefore>
```

**说明**：
- 指定此模组应在哪些模组之前加载
- 游戏会调整加载顺序以满足此要求
- 通常用于框架或基础模组

**使用场景**：
- 你的模组是一个框架，其他模组需要扩展它
- 你的模组需要在某些模组之前初始化

**示例**：
```xml
<loadBefore>
  <li>someauthor.framework_extension</li>
  <li>someauthor.framework_addon</li>
</loadBefore>
```

---

#### `<loadAfter>` - 在某模组之后加载

```xml
<loadAfter>
  <li>someauthor.prerequisite</li>
  <li>someauthor.basefork</li>
</loadAfter>
```

**说明**：
- 指定此模组应在哪些模组之后加载
- 游戏会调整加载顺序以满足此要求
- 最常用的加载顺序控制方式

**使用场景**：
- 你的模组依赖于另一个模组的初始化
- 你的模组需要修改另一个模组的内容
- 你的模组是某个模组的扩展

**示例**：
```xml
<loadAfter>
  <!-- 框架 -->
  <li>ludeon.rimworld.humanoidalienraces</li>
  <li>vanillaexpanded.framework</li>

  <!-- 基础模组 -->
  <li>someauthor.base_mod</li>

  <!-- 核心 -->
  <li>Ludeon.RimWorld</li>
</loadAfter>
```

---

## 完整的About.xml示例

### 示例1：简单的内容模组（无依赖）

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>激光武器系统</name>
  <author>小王</author>
  <description>
添加先进的激光武器系统。

特性：
- 3种激光武器
- 能量消耗系统
- 与原版兼容

支持版本：RimWorld 1.6+
  </description>

  <modVersion>1.0.0</modVersion>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>

  <packageId>xiaowang.laser_weapons</packageId>
</ModMetaData>
```

---

### 示例2：依赖DLC的模组

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>生物技术扩展</name>
  <author>李四</author>
  <description>为生物技术DLC添加新的基因类型和能力。</description>

  <modVersion>1.0.0</modVersion>
  <supportedVersions>
    <li>1.5</li>
    <li>1.6</li>
  </supportedVersions>

  <!-- 必须有生物技术DLC -->
  <dependencies>
    <li>Ludeon.RimWorld.Biotech</li>
  </dependencies>

  <packageId>lisi.biotech_extension</packageId>
</ModMetaData>
```

---

### 示例3：框架类模组（有扩展）

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>高级储存框架</name>
  <author>王五</author>
  <description>提供高级储存系统框架，其他模组可扩展。</description>

  <modVersion>2.1.0</modVersion>
  <supportedVersions>
    <li>1.4</li>
    <li>1.5</li>
    <li>1.6</li>
  </supportedVersions>

  <packageId>wangwu.advanced_storage_framework</packageId>

  <!-- 此框架应在扩展之前加载 -->
  <!-- LoadFolders.xml可以通过IfModActive实现条件加载 -->
</ModMetaData>
```

---

### 示例4：依赖框架的模组

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>高级储存 - 工业扩展</name>
  <author>赵六</author>
  <description>为高级储存框架添加工业级储存选项。</description>

  <modVersion>1.0.0</modVersion>
  <supportedVersions>
    <li>1.5</li>
    <li>1.6</li>
  </supportedVersions>

  <!-- 依赖框架 -->
  <dependencies>
    <li>wangwu.advanced_storage_framework</li>
  </dependencies>

  <!-- 在框架之后加载 -->
  <loadAfter>
    <li>wangwu.advanced_storage_framework</li>
  </loadAfter>

  <packageId>zhaoliu.advanced_storage_industrial</packageId>
</ModMetaData>
```

---

### 示例5：复杂的模组（多依赖、多版本）

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>完整的种族系统</name>
  <author>开发团队</author>
  <description>
为RimWorld添加完整的自定义种族系统。

支持的DLC：
- 贵族
- 生物技术
- 意识形态

特性：
- 10+种可选种族
- 自定义能力
- 与其他模组兼容
  </description>

  <modVersion>3.2.1</modVersion>
  <supportedVersions>
    <li>1.4</li>
    <li>1.5</li>
    <li>1.6</li>
  </supportedVersions>

  <!-- 依赖框架 -->
  <dependencies>
    <li>ludeon.rimworld.humanoidalienraces</li>
  </dependencies>

  <!-- 某些实现与此模组冲突 -->
  <incompatibleWith>
    <li>someauthor.conflicting_race_mod</li>
  </incompatibleWith>

  <!-- 加载顺序控制 -->
  <loadAfter>
    <li>ludeon.rimworld.humanoidalienraces</li>
    <li>Ludeon.RimWorld</li>
  </loadAfter>

  <packageId>devteam.complete_race_system</packageId>
  <publishedFileID>2819384756</publishedFileID>
</ModMetaData>
```

---

## 常见问题

### Q: 我的模组显示"无法加载"怎么办？

**检查清单**：
- [ ] About.xml 文件是否在 About/ 目录下
- [ ] XML格式是否正确（用验证工具检查）
- [ ] `<supportedVersions>` 是否包含你的游戏版本
- [ ] 所有依赖模组是否已激活
- [ ] packageId 是否有特殊字符或空格

### Q: 加载顺序错误怎么办？

**解决方案**：
1. 检查 `<loadAfter>` 是否正确指向依赖模组
2. 确保依赖模组的packageId或标识符正确
3. 使用 `<dependencies>` 强制依赖关系
4. 在LoadFolders.xml中使用条件加载

### Q: 我的模组与另一个模组冲突怎么办？

**处理方式**：
```xml
<!-- 方式1：标记不兼容 -->
<incompatibleWith>
  <li>conflicting.mod.id</li>
</incompatibleWith>

<!-- 方式2：在LoadFolders中用IfModActive条件加载 -->
<!-- 参考《LoadFolders.xml配置指南》 -->
```

### Q: packageId可以改吗？

**严格禁止**（发布后）：
- ❌ 不要改变已发布模组的packageId
- ❌ Steam会将其视为新模组
- ❌ 玩家存档会失去配置
- ✅ 如果未发布，可以随时改

### Q: 为什么我的模组在列表中出现两次？

**原因**：packageId重复或不唯一

**解决**：
- 搜索Steam确保ID全局唯一
- 改变packageId并重新发布
- 删除旧版本

---

## 验证About.xml

### 使用XML验证工具

在线工具：
- https://www.xmlvalidation.com/
- https://codebeautify.org/xml-validator

验证步骤：
1. 复制About.xml内容
2. 粘贴到验证工具
3. 检查是否有错误

### 常见XML错误

| 错误 | 原因 | 修复 |
|------|------|------|
| `标签未闭合` | 少了 `</tag>` | 补充闭合标签 |
| `未转义的字符` | `<` 或 `&` | 改为 `&lt;` 或 `&amp;` |
| `引号不匹配` | 单双引号混用 | 统一使用 `"` |
| `缩进错误` | 嵌套关系混乱 | 检查标签嵌套 |

---

## 最佳实践

### 1. 清晰的描述

✅ **好的描述**：
```xml
<description>
添加激光武器系统，包括：
- 激光步枪：远程，高伤害
- 激光手枪：近程，低伤害
- 离子炮：重型，极高伤害

需要先解锁激光科技。
与所有DLC兼容。
</description>
```

❌ **不好的描述**：
```xml
<description>我的模组很棒</description>
```

### 2. 正确的依赖声明

✅ **正确的做法**：
```xml
<dependencies>
  <!-- 真正必需的模组 -->
  <li>ludeon.rimworld.humanoidalienraces</li>
</dependencies>

<loadAfter>
  <li>ludeon.rimworld.humanoidalienraces</li>
  <li>Ludeon.RimWorld</li>
</loadAfter>
```

❌ **错误的做法**：
```xml
<dependencies>
  <!-- 太多不必要的依赖 -->
  <li>各种模组...</li>
</dependencies>
```

### 3. 语义化版本

✅ **正确版本号**：
```xml
<modVersion>1.0.0</modVersion>  <!-- 初版 -->
<modVersion>1.1.0</modVersion>  <!-- 新功能 -->
<modVersion>1.1.1</modVersion>  <!-- 修复 -->
```

❌ **不规范版本号**：
```xml
<modVersion>1</modVersion>
<modVersion>v1.0.0</modVersion>
<modVersion>1.0.0.0</modVersion>
```

---

## 参考资源

- 《模组目录结构标准规范》- 完整的目录结构指南
- 《模组命名规范》- PackageId和其他命名
- 《快速启动指南》- 新模组快速开始

---

## 版本历史

| 版本 | 时间 | 改动 | 作者 |
|------|------|------|------|
| v1.0 | 2026-01-07 | 初版发布。详细说明了About.xml的所有标签、完整的工作示例、常见问题和最佳实践。 | 知识提炼者 |
