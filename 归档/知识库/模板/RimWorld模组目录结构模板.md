# RimWorld模组目录结构模板

## 摘要

本文档定义了RimWorld模组的标准目录结构和文件组织规范，基于对六个框架性模组（AdaptiveStorageFramework、AncotLibrary、AutoBlink、HumanoidAlienRaces、VanillaExpandedFramework、WeaponFitting）的深度分析总结。提供统一的参考模板，确保模组能够被游戏正确加载和运行，并支持多版本兼容和框架扩展。

**版本号**：v0.3  
**修改时间**：2026-01-02 14:30  
**关键词**：RimWorld,模组开发,目录结构,框架设计,多版本兼容,组件化架构,XML配置  
**标签**：[定稿]

---

## 一、必要文件

### 1.1 About/About.xml（必须）

模组元数据文件，定义模组的基本信息。基于框架模组分析，推荐包含框架特定的元数据字段。

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
    <name>模组名称</name>
    <author>作者名</author>
    <packageId>com.author.modname</packageId>
    <supportedVersions>
        <li>1.5</li>
        <li>1.6</li>
    </supportedVersions>
    <description>模组描述</description>
    <url>项目主页URL（可选）</url>
    
    <!-- 框架模组特有字段 -->
    <isFramework>true</isFramework>  <!-- 是否为框架模组 -->
    <frameworkVersion>1.0.0</frameworkVersion>  <!-- 框架版本号 -->
    
    <modDependencies>
        <li>
            <packageId>brrainz.harmony</packageId>
            <displayName>Harmony</displayName>
            <steamWorkshopUrl>steam://url/CommunityFilePage/2009463077</steamWorkshopUrl>
            <downloadUrl>https://github.com/pardeike/HarmonyRimWorld/releases/latest</downloadUrl>
        </li>
        <!-- 框架模组依赖 -->
        <li IfModActive="com.author.framework">
            <packageId>com.author.framework</packageId>
            <displayName>基础框架</displayName>
        </li>
    </modDependencies>
    
    <loadAfter>
        <li>Ludeon.RimWorld</li>
        <li>Ludeon.RimWorld.Royalty</li>
        <li>Ludeon.RimWorld.Biotech</li>
        <!-- 框架模组加载顺序 -->
        <li>brrainz.harmony</li>
        <li>com.author.framework</li>
    </loadAfter>
    
    <loadBefore>
        <li>其他模组ID</li>  <!-- 在此模组之前加载 -->
    </loadBefore>
    
    <incompatibleWith>
        <li>冲突模组ID</li>
    </incompatibleWith>
    
    <!-- 框架模组扩展接口 -->
    <frameworkExtensions>
        <li>ComponentSystem</li>  <!-- 支持的组件系统 -->
        <li>ConditionalGraphics</li>  <!-- 条件图形系统 -->
        <li>PluginArchitecture</li>  <!-- 插件架构 -->
    </frameworkExtensions>
</ModMetaData>
```

**关键字段说明**：
- `name`：模组显示名称
- `author`：作者名
- `packageId`：唯一标识符，建议使用反向域名格式（如com.author.modname）
- `supportedVersions`：支持的RimWorld版本列表
- `modDependencies`：依赖的其他模组
- `loadAfter`：需要在哪些模组之后加载
- `incompatibleWith`：不兼容的模组列表
- `isFramework`：是否为框架模组（框架模组特有）
- `frameworkExtensions`：框架提供的扩展接口（框架模组特有）

### 1.2 LoadFolders.xml（推荐）

多版本支持时必须，用于指定不同游戏版本加载的目录。

**基础配置：**
```xml
<loadFolders>
    <v1.5>
        <li>1.5</li>
        <li>Common</li>
    </v1.5>
    <v1.6>
        <li>1.6</li>
        <li>Common</li>
    </v1.6>
</loadFolders>
```

**高级配置（条件加载）：**
```xml
<loadFolders>
    <v1.5>
        <li>1.5</li>
        <li>Common</li>
        <!-- 条件加载DLC内容 -->
        <li IfModActive="Ludeon.RimWorld.Biotech">1.5/Mods/Biotech</li>
        <li IfModActive="Ludeon.RimWorld.Ideology">1.5/Mods/Ideology</li>
    </v1.5>
    <v1.6>
        <li>1.6</li>
        <li>Common</li>
        <!-- 条件加载DLC内容 -->
        <li IfModActive="Ludeon.RimWorld.Biotech">1.6/Mods/Biotech</li>
        <li IfModActive="Ludeon.RimWorld.Ideology">1.6/Mods/Ideology</li>
        <li IfModActive="Ludeon.RimWorld.Odyssey">1.6/Mods/Odyssey</li>
    </v1.6>
</loadFolders>
```

**条件加载说明：**
- `IfModActive="模组ID"`：只有当指定模组激活时才加载该目录
- 常用于DLC兼容、可选功能模块
- 支持多个条件加载目录

---

## 二、标准目录结构

基于六个框架模组的分析，推荐以下目录结构，特别适合框架性模组和复杂功能模组：

```
模组根目录/
├── About/                          # 模组信息（必须）
│   ├── About.xml                   # 模组元数据（必须）
│   ├── Preview.png                 # 预览图（推荐）
│   ├── PublishedFileId.txt         # Steam创意工坊ID（可选）
│   └── ModIcon.png                 # 模组图标（可选）
│
├── LoadFolders.xml                 # 版本加载配置（推荐）
│
├── Docs/                           # 文档目录（框架模组推荐）
│   ├── README.md                   # 项目说明
│   ├── LICENSE                     # 许可证文件
│   ├── CHANGELOG.md                # 更新日志
│   ├── API_Reference.md            # API参考文档（框架模组必须）
│   ├── Tutorials/                  # 教程文档
│   │   ├── Getting_Started.md      # 快速开始
│   │   ├── Component_System.md     # 组件系统使用
│   │   └── Examples/               # 示例代码
│   └── .gitignore                  # Git忽略配置（可选）
│
├── 1.5/                           # 1.5版本专用内容
│   ├── Assemblies/                # 编译后的DLL
│   ├── Defs/                      # XML定义文件
│   │   ├── ThingDef/             # 物品定义
│   │   ├── Apparel/              # 服装定义
│   │   ├── Buildings/            # 建筑定义
│   │   ├── Weapons/              # 武器定义
│   │   ├── Hediff/               # 疾病/增强定义
│   │   ├── JobDef/               # 工作定义
│   │   ├── Research/             # 研究项目
│   │   ├── SoundDefs/            # 音效定义
│   │   ├── ComponentDefs/        # 组件定义（框架模组特有）
│   │   ├── RaceDefs/             # 种族定义（如HumanoidAlienRaces）
│   │   ├── FrameworkDefs/        # 框架定义（如VanillaExpandedFramework）
│   │   └── ...
│   ├── Patches/                   # XML补丁
│   │   ├── Harmony.xml           # Harmony补丁
│   │   ├── DefPatches/           # 定义补丁
│   │   └── ...
│   └── Source/                    # C#源代码（可选）
│       └── 项目名/
│           ├── *.cs
│           ├── *.csproj
│           ├── Components/       # 组件类（框架模组推荐）
│           ├── Systems/          # 系统类
│           ├── Patches/          # Harmony补丁类
│           └── Utils/            # 工具类
│
├── 1.6/                           # 1.6版本专用内容
│   ├── Assemblies/
│   ├── Defs/
│   ├── Patches/
│   └── Source/
│
├── Common/                        # 跨版本共享内容（推荐）
│   ├── Languages/                 # 本地化文件
│   │   ├── English/
│   │   │   ├── Keyed/            # 键值对翻译
│   │   │   │   └── ModName.xml
│   │   │   └── Strings/          # 名称列表
│   │   │       └── Names/
│   │   │           ├── First.txt
│   │   │           └── Last.txt
│   │   └── Chinese/              # 中文翻译
│   ├── Textures/                  # 纹理资源
│   │   ├── Things/               # 物品纹理
│   │   │   ├── Apparel/
│   │   │   ├── Buildings/
│   │   │   └── ...
│   │   ├── UI/                   # 界面图标
│   │   │   ├── Commands/
│   │   │   └── ...
│   │   └── World/                # 世界地图纹理
│   ├── Sounds/                    # 音频资源
│   │   ├── Interact/             # 交互音效
│   │   ├── Weapon/               # 武器音效
│   │   └── ...
│   ├── Configs/                  # 配置文件（框架模组推荐）
│   │   ├── FrameworkSettings.xml  # 框架设置
│   │   ├── ComponentConfigs/      # 组件配置
│   │   └── ...
│   └── ...
│
├── Framework/                     # 框架核心（框架模组特有）
│   ├── Core/                      # 核心系统
│   │   ├── BaseSystems/           # 基础系统类
│   │   ├── Interfaces/            # 接口定义
│   │   └── Utilities/             # 核心工具
│   ├── Components/                # 组件库
│   │   ├── BaseComponents/        # 基础组件
│   │   ├── ConditionalComponents/ # 条件组件
│   │   └── Specialized/           # 专用组件
│   ├── Plugins/                   # 插件系统（如VanillaExpandedFramework）
│   │   ├── PluginBase/            # 插件基类
│   │   ├── PluginManager/         # 插件管理器
│   │   └── BuiltInPlugins/        # 内置插件
│   └── Extensions/                # 扩展功能
│       ├── HarmonyExtensions/     # Harmony扩展
│       ├── XMLExtensions/         # XML扩展
│       └── UtilityExtensions/     # 工具扩展
│
└── (单版本模组可选结构)
    ├── Assemblies/                # DLL（单版本时）
    ├── Defs/                      # XML定义（单版本时）
    ├── Source/                    # C#源代码（单版本时）
    ├── Languages/
    ├── Textures/
    └── Sounds/
```
```

---

## 三、关键目录详解

### 3.1 Framework/（框架模组特有）

基于VanillaExpandedFramework、HumanoidAlienRaces等框架模组的分析，框架目录是框架模组的核心。

**用途**：
- 存放框架的核心系统和组件库
- 提供插件架构和扩展接口
- 实现跨模组的通用功能

**最佳实践**：
- 将核心功能与具体实现分离
- 提供清晰的接口定义
- 支持插件式扩展
- 实现版本兼容性处理

### 3.2 Defs/ComponentDefs/（框架模组推荐）

基于AutoBlink、WeaponFitting等模组的组件化设计模式。

**用途**：
- 存放组件定义XML文件
- 定义可配置的组件属性
- 支持条件加载和动态配置

**示例结构**：
```
Defs/
├── ComponentDefs/
│   ├── CompProperties_Blink.xml      # 自动折跃组件定义
│   ├── CompProperties_WeaponFitting.xml  # 武器配件组件定义
│   ├── CompProperties_Storage.xml    # 智能存储组件定义
│   └── CompProperties_Race.xml       # 种族组件定义
```

### 3.3 Source/Components/（组件化架构推荐）

基于组件化设计模式，将功能模块化。

**用途**：
- 存放组件实现类
- 实现具体的功能逻辑
- 提供统一的接口和扩展点

**最佳实践**：
- 每个组件职责单一
- 组件间通过事件通信
- 支持条件激活和配置
- 提供性能优化机制

### 3.4 Patches/DefPatches/（框架模组推荐）

基于Harmony补丁系统，实现定义文件的动态修改。

**用途**：
- 存放XML补丁文件
- 实现定义文件的运行时修改
- 支持条件补丁和版本适配

### 3.5 Assemblies/

存放编译后的.dll程序集文件。

**用途**：
- 存放C#代码编译后的DLL
- 可放在版本目录或Common中

**框架模组特有考虑**：
- 将框架核心DLL与具体实现DLL分离
- 支持插件式加载机制
- 实现动态依赖管理
- 如果有源代码，建议同时提供Source目录

**示例**：
```
Assemblies/
└── ModName.dll
```

### 3.2 Defs/

存放XML定义文件，定义游戏中的各种实体。

**组织原则**：
- 按Def类型分目录组织
- 避免单个文件过大（建议<500行）
- 使用清晰的命名规范

**常见Def类型**：
```
Defs/
├── ThingDef/              # 物品、建筑、生物
├── Apparel/               # 服装装备
├── Buildings/             # 建筑设施
├── Weapons/               # 武器
├── Hediff/                # 疾病、增强效果
├── JobDef/                # 工作任务
├── Research/              # 研究项目
├── AbilityDefs/           # 能力定义
├── FactionDefs/           # 阵营定义
├── PawnKindDef/           # 生物种类
├── RecipeDefs/            # 配方
├── SoundDefs/             # 音效定义
├── ThoughtDefs/           # 思想
├── Stats/                 # 属性定义（如AncotLibrary）
├── Effects/               # 效果定义（如粒子效果）
├── ThinkTreeDefs/         # 行为树定义
├── CultureDefs/           # 文化定义
├── BodyDefs/              # 身体结构定义
├── BodyTypeDefs/          # 体型定义
├── LifeStageDefs/         # 生命周期定义
├── TerrainDefs/           # 地形定义
├── PlantDefs/             # 植物定义
├── AnimalDefs/            # 动物定义
├── IncidentDefs/          # 事件定义
├── QuestScriptDefs/       # 任务脚本定义
├── RuleDefs/              # 规则定义
├── MainButtonDefs/        # 主按钮定义
├── WorkGiverDefs/         # 工作提供者定义
├── DesignationCategoryDefs/ # 指定分类定义
└── ...
```

**Def组织深度（参考AncotLibrary模式）：**
- 按功能模块深度细分
- 避免单个文件过大
- 保持清晰的层次结构

### 3.3 Source/

存放C#源代码（可选但推荐）。

**用途**：
- 便于其他开发者学习和修改
- 支持源代码调试
- 便于代码审查和贡献

**示例**：
```
Source/
└── ModName/
    ├── ModName.csproj
    ├── Properties/
    │   └── AssemblyInfo.cs
    ├── CompModName.cs
    ├── JobDriver_ModName.cs
    └── ...
```

### 3.4 Languages/

存放本地化文件。

**结构**：
```
Languages/
├── English/
│   ├── Keyed/
│   │   └── ModName.xml
│   └── Strings/
│       └── Names/
│           ├── First.txt
│           └── Last.txt
└── Chinese/
    ├── Keyed/
    │   └── ModName.xml
    └── Strings/
        └── Names/
            ├── First.txt
            └── Last.txt
```

**Keyed示例**：
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
    <ModName.Label>模组名称</ModName.Label>
    <ModName.Description>模组描述</ModName.Description>
</LanguageData>
```

### 3.5 Textures/

存放纹理资源。

**组织方式**：
```
Textures/
├── Things/               # 物品纹理
│   ├── Apparel/
│   ├── Buildings/
│   ├── Items/
│   └── ...
├── UI/                   # 界面图标
│   ├── Commands/
│   ├── Gizmos/
│   └── ...
└── World/                # 世界地图纹理
    └── ...
```

### 3.6 Sounds/

存放音频资源。

**组织方式**：
```
Sounds/
├── Interact/             # 交互音效
├── Weapon/               # 武器音效
├── Ambient/              # 环境音效
└── ...
```

### 3.7 Patches/

存放XML补丁文件。

**用途**：
- 修改游戏或其他模组的现有Def
- 使用<Operation>标签进行补丁操作

**示例**：
```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
    <Operation Class="PatchOperationAdd">
        <xpath>Defs/ThingDef[defName="Human"]/race</xpath>
        <value>
            <li>CustomTag</li>
        </value>
    </Operation>
</Patch>
```

---

## 四、模组类型与适用结构

基于RimWorld模组生态的广泛分析，不同类型的模组应采用不同的目录结构策略：

### 4.1 内容型模组（Content Mods）

**特点**：添加新物品、生物、建筑等游戏内容
**典型规模**：小型到中型
**推荐结构**：
```
模组根目录/
├── About/
├── Defs/
│   ├── ThingDef/
│   ├── Research/
│   └── ...
├── Textures/
├── Languages/
└── Patches/
```

### 4.2 功能型模组（Feature Mods）

**特点**：添加新机制或功能
**典型规模**：中型
**推荐结构**：
```
模组根目录/
├── About/
├── Assemblies/
├── Defs/
│   ├── JobDef/
│   ├── WorkGiverDef/
│   └── ...
├── Source/
├── Textures/
└── Languages/
```

### 4.3 框架型模组（Framework Mods）

**特点**：为其他模组提供基础功能
**典型规模**：大型
**推荐结构**：
```
模组根目录/
├── About/
├── Framework/
│   ├── Core/
│   ├── Components/
│   └── Extensions/
├── Assemblies/
├── Defs/
├── Source/
├── Docs/
└── Common/
```

### 4.4 大修型模组（Overhaul Mods）

**特点**：深度修改游戏核心机制
**典型规模**：超大型
**推荐结构**：
```
模组根目录/
├── About/
├── 1.5/
│   ├── Assemblies/
│   ├── Defs/
│   └── Patches/
├── 1.6/
│   ├── Assemblies/
│   ├── Defs/
│   └── Patches/
├── Common/
├── Docs/
└── Tools/
```

## 五、命名规范

### 5.1 通用命名原则

- **一致性**：整个模组使用统一的命名风格
- **可读性**：名称应该清晰表达用途
- **避免冲突**：使用模组前缀防止命名冲突
- **版本兼容**：考虑未来扩展性

### 5.2 Def命名规范

**XML定义命名**：
```
ThingDef_模组前缀_功能描述
CompProperties_模组前缀_组件功能
JobDriver_模组前缀_工作类型
```

**示例**：
```xml
<ThingDef_AutoBlink_BlinkDevice>
<CompProperties_WeaponFitting_AccessorySlot>
<JobDriver_HumanoidAliens_CustomWork>
```

### 5.3 文件命名规范

**XML文件**：
```
功能类别_具体描述.xml
Weapons_EnergyGuns.xml
Buildings_Production.xml
```

**C#文件**：
```
类型前缀_功能描述.cs
CompAutoBlink.cs
JobDriverCustomWork.cs
```

**资源文件**：
```
类型_名称_状态.扩展名
Weapon_LaserGun_Icon.png
Apparel_Helmet_North.png
```

### 5.4 目录命名规范

**标准目录**：
```
Defs/          # XML定义文件
Assemblies/    # 编译后的DLL
Source/        # C#源代码
Textures/      # 纹理资源
Sounds/        # 音频资源
Languages/     # 本地化文件
Patches/       # XML补丁
```

**子目录组织**：
```
Defs/ThingDef/Weapons/
Defs/Hediff/Addictions/
Textures/Things/Buildings/
```

---

## 五、多版本兼容策略

### 5.1 版本目录结构

```
模组根目录/
├── 1.5/                   # 1.5特定内容
│   ├── Assemblies/
│   │   └── ModName_1.5.dll
│   └── Defs/
│       └── ...
├── 1.6/                   # 1.6特定内容
│   ├── Assemblies/
│   │   └── ModName_1.6.dll
│   └── Defs/
│       └── ...
└── Common/                # 共享内容
    ├── Languages/
    ├── Textures/
    └── Sounds/
```

### 5.2 LoadFolders.xml配置

```xml
<loadFolders>
    <v1.5>
        <li>1.5</li>
        <li>Common</li>
    </v1.5>
    <v1.6>
        <li>1.6</li>
        <li>Common</li>
    </v1.6>
</loadFolders>
```

### 5.3 API差异处理

- 1.5和1.6的API可能有差异
- 使用条件编译或版本检测
- 为不同版本提供不同的实现

---

## 六、最佳实践

### 6.1 目录组织

1. **按功能分类**：将相关内容放在同一目录
2. **避免过深嵌套**：建议不超过3层
3. **使用清晰的命名**：目录名应能反映其内容
4. **保持一致性**：在整个模组中使用相同的组织方式

### 6.2 文件管理

1. **控制文件大小**：单个XML文件建议<500行
2. **模块化设计**：将功能分散到多个文件
3. **提供源代码**：如果使用C#，建议提供Source目录
4. **版本控制**：使用Git等工具管理代码

### 6.3 资源优化

1. **压缩纹理**：使用适当的纹理格式和尺寸
2. **优化音频**：使用合适的音频质量和格式
3. **共享资源**：将跨版本资源放在Common目录
4. **清理无用资源**：定期清理未使用的资源文件

### 6.4 依赖管理

1. **明确声明依赖**：在About.xml中列出所有依赖
2. **处理冲突**：声明不兼容的模组
3. **加载顺序**：设置正确的loadAfter顺序
4. **版本兼容**：明确支持的游戏版本

---

## 七、检查清单

### 7.1 必须项

- [ ] About/About.xml存在且格式正确
- [ ] packageId唯一且格式正确
- [ ] supportedVersions声明支持的版本
- [ ] 所有必要的DLL文件存在

### 7.2 推荐项

- [ ] LoadFolders.xml配置正确（多版本）
- [ ] 提供预览图Preview.png
- [ ] 提供模组图标ModIcon.png
- [ ] 提供源代码Source目录
- [ ] 提供本地化文件
- [ ] Common目录用于共享资源

### 7.3 可选项

- [ ] PublishedFileId.txt（Steam创意工坊）
- [ ] 详细的文档和说明
- [ ] 示例代码或配置

---

## 八、常见问题

### 8.1 模组无法加载

**可能原因**：
- About.xml格式错误
- packageId重复
- 缺少必要的DLL文件
- 依赖的模组未安装

**解决方法**：
- 检查XML格式是否正确
- 确保packageId唯一
- 检查所有依赖是否满足

### 8.2 多版本兼容问题

**可能原因**：
- LoadFolders.xml配置错误
- 版本目录结构不正确
- API差异未处理

**解决方法**：
- 检查LoadFolders.xml格式
- 确保版本目录结构正确
- 使用条件编译处理API差异

### 8.3 资源加载失败

**可能原因**：
- 资源路径错误
- 资源文件损坏
- 资源格式不支持

**解决方法**：
- 检查资源路径是否正确
- 验证资源文件完整性
- 使用支持的资源格式

---

## 九、参考资源

### 9.1 官方文档
- RimWorld Modding Wiki
- Ludeon Studios官方文档

### 9.2 社区资源
- RimWorld Modding Discord
- Steam创意工坊热门模组
- Reddit r/RimWorld

### 9.3 开发工具
- Harmony补丁框架
- RimWorld Modding Toolkit
- XML编辑器推荐

---

## 历史记录

| 版本号 | 改动摘要 | 修改时间 | 修改者 |
|--------|----------|----------|--------|
| v0.3 | 重构版本，移除设计模式内容，按模组类型重新组织目录结构，扩展至内容型、功能型、框架型、大修型分类 | 2026-01-02 14:30 | 知识提炼者 |
| v0.2 | 扩展版本，基于六个框架模组分析，增加Framework目录和组件化架构支持 | 2026-01-02 10:15 | 知识提炼者 |
| v0.1 | 初始版本，基于AutoBlink、CeleTechArsenalMKIII、Milira三个参考模组分析总结 | 2026-01-01 21:20 | 知识提炼者 |
