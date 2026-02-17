---
标题：RimWorld模组初始化Skill设计文档
版本号: v1.0
更新日期: 2026-02-17
最后修改者: Claude Sonnet 4.5
标签: [文档][用户未确认][已完成][未锁定]
摘要: rimworld-mod-init skill的完整设计方案，包括交互流程、文件生成逻辑和技术实现细节
---

# RimWorld模组初始化Skill设计文档

## 1. 项目概述

### 1.1 目标
创建一个名为`rimworld-mod-init`的skill，用于快速创建并初始化RimWorld模组的目录结构和必要文件。

### 1.2 核心特性
- 交互式问答引导，适合非专业开发者
- 提供预设模板快速开始
- 智能生成文件内容，避免创建不需要的文件
- 支持单版本和多版本模组结构
- 自动配置C#项目文件（如果需要）
- 优先使用简体中文填写描述性内容

## 2. 需求总结

### 2.1 功能需求
1. **可配置创建**：根据用户需求决定创建什么样的目录结构
2. **交互方式**：混合方式（场景化问题 + 渐进式引导 + 预设模板）
3. **预设模板**：
   - 纯内容添加（武器、装备、建筑等）
   - 简单功能模组（需要C#代码）
   - 框架/库模组（为其他模组提供功能）
   - 完全自定义
4. **智能生成**：根据用户回答决定生成哪些文件和内容
5. **版本支持**：默认单版本（1.6），可选多版本支持
6. **信息收集**：收集常用信息（name、author、packageId、description、url、dependencies）
7. **C#项目支持**：生成完整的.csproj文件，包含RimWorld程序集引用
8. **路径配置**：混合方式，优先使用环境变量，否则使用用户提供的路径
9. **创建位置**：智能选择（当前目录、RimWorld Mods目录、自定义路径）
10. **完成后操作**：显示摘要和下一步建议

### 2.2 非功能需求
1. **易用性**：问题表述通俗易懂，照顾非专业开发者
2. **本地化**：所有交互界面和描述性内容使用简体中文
3. **灵活性**：提供预设模板的同时保留完全自定义的能力
4. **可靠性**：验证用户输入，处理错误和边界情况

## 3. 设计方案

### 3.1 整体架构

**Skill名称**：`rimworld-mod-init`

**核心流程**：

```
开始
  ↓
1. 欢迎和快速选择
   - 显示："创建RimWorld模组"
   - 提供预设模板选项
  ↓
2. 收集基础信息
   - 模组名称、作者、描述、URL
  ↓
3. 技术配置问答
   - 根据预设模板调整问题
   - 是否需要C#代码、多版本支持、依赖项等
  ↓
4. 路径和环境配置
   - 选择创建位置
   - 配置RimWorld路径（如果需要）
  ↓
5. 生成确认
   - 显示目录结构预览
   - 询问是否确认创建
  ↓
6. 执行创建
   - 创建目录结构
   - 生成配置文件
  ↓
7. 显示摘要
   - 显示创建结果
   - 提供下一步建议
  ↓
结束
```

### 3.2 交互式问答流程

#### 阶段1：快速选择
```
欢迎使用RimWorld模组初始化工具！

请选择模组类型（这会影响后续的默认选项）：
1. 纯内容模组 - 添加武器、装备、建筑等，不需要编写代码
2. 功能模组 - 需要编写C#代码来实现新功能或修改游戏机制
3. 框架/库模组 - 为其他模组提供功能支持
4. 完全自定义 - 我想自己决定所有细节

你的选择：
```

#### 阶段2：基础信息收集
```
模组名称（英文，如：MyAwesomeMod）：
作者名称（英文，如：YourName）：
模组描述（中文，简短描述你的模组功能）：
项目URL（可选，如GitHub链接，直接回车跳过）：
```

#### 阶段3：技术配置问答

**如果选择"纯内容模组"**：
```
你的模组需要添加哪些类型的内容？（可多选，用逗号分隔）
1. 武器和装备
2. 建筑和家具
3. 研究项目
4. 派系和事件
5. 其他（会创建通用的Defs目录）

你的选择：
```

**如果选择"功能模组"或"框架/库模组"**：
```
你的模组需要编写C#代码吗？（是/否）
如果是，你的模组需要使用Harmony来修改游戏原有功能吗？（是/否）
```

**通用问题**：
```
你的模组需要支持多个游戏版本吗？（是/否）
如果是，需要支持哪些版本？（如：1.5,1.6）

你的模组依赖其他模组吗？（是/否）
如果是，请输入依赖的模组packageId（如：brrainz.harmony，多个用逗号分隔）
```

#### 阶段4：路径配置
```
请选择模组创建位置：
1. 当前目录（开发推荐）
2. RimWorld Mods目录（测试方便）
3. 自定义路径

你的选择：
```

**如果需要C#代码**：
```
请输入RimWorld安装路径（用于配置程序集引用）：
默认：C:\NiwtGames\Steam\steamapps\common\RimWorld
直接回车使用默认值，或输入自定义路径：
```

#### 阶段5：确认预览
```
即将创建以下结构：

MyAwesomeMod/
├── About/
│   └── About.xml
├── 1.6/
│   ├── Assemblies/
│   ├── Defs/
│   │   ├── ThingDefs/
│   │   └── ResearchProjectDefs/
│   └── Textures/
└── Source/
    └── MyAwesomeMod.csproj

确认创建？（是/否）
```

### 3.3 文件生成逻辑

#### 3.3.1 About.xml生成规则

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
    <name>模组名称（用户输入的英文名）</name>
    <author>作者名称（用户输入）</author>
    <packageId>作者名.模组名（自动生成）</packageId>
    <description>模组描述（用户输入的中文描述）</description>
    <supportedVersions>
        <li>1.6</li>  <!-- 根据用户选择的版本 -->
    </supportedVersions>

    <!-- 如果用户提供了URL -->
    <url>项目URL</url>

    <!-- 如果用户指定了依赖项 -->
    <modDependencies>
        <li>
            <packageId>依赖的模组packageId</packageId>
            <displayName>依赖的模组显示名称</displayName>
        </li>
    </modDependencies>
</ModMetaData>
```

#### 3.3.2 LoadFolders.xml生成规则

仅在多版本支持时生成：

```xml
<?xml version="1.0" encoding="UTF-8"?>
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

#### 3.3.3 .csproj文件生成规则

仅在需要C#代码时生成：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <OutputPath>../1.6/Assemblies</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <PropertyGroup>
    <!-- 优先使用环境变量，否则使用用户提供的路径 -->
    <RimWorldPath Condition="'$(RimWorldPath)' == ''">用户提供的路径</RimWorldPath>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>$(RimWorldPath)\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(RimWorldPath)\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
      <Private>False</Private>
    </Reference>

    <!-- 如果依赖Harmony -->
    <Reference Include="0Harmony">
      <HintPath>$(RimWorldPath)\RimWorldWin64_Data\Managed\0Harmony.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
```

#### 3.3.4 目录结构创建规则

**单版本结构**：
```
ModName/
├── About/
│   └── About.xml
├── 1.6/
│   ├── Assemblies/        (如果需要C#代码)
│   ├── Defs/              (根据内容类型创建子目录)
│   │   ├── ThingDefs/     (如果选择了武器装备)
│   │   ├── ResearchProjectDefs/  (如果选择了研究项目)
│   │   └── ...
│   └── Textures/          (总是创建)
└── Source/                (如果需要C#代码)
    └── ModName.csproj
```

**多版本结构**：
```
ModName/
├── About/
│   └── About.xml
├── LoadFolders.xml
├── Common/
│   └── Textures/
├── 1.5/
│   ├── Assemblies/
│   └── Defs/
├── 1.6/
│   ├── Assemblies/
│   └── Defs/
└── Source/
    └── ModName.csproj
```

#### 3.3.5 packageId生成规则

格式：`作者名.模组名`
- 移除空格和特殊字符
- 保持驼峰命名
- 例如：作者"John Doe"，模组"My Awesome Mod" → `JohnDoe.MyAwesomeMod`

## 4. 技术实现细节

### 4.1 实现方式

使用Python编写skill，主要结构：

```python
# 主要函数
def main():
    # 1. 显示欢迎信息
    # 2. 收集用户输入（通过交互式问答）
    # 3. 构建配置对象
    # 4. 显示预览
    # 5. 执行创建
    # 6. 显示摘要

# 辅助函数
def collect_basic_info() -> dict
def collect_technical_config(preset: str) -> dict
def generate_about_xml(config: dict) -> str
def generate_csproj(config: dict, rimworld_path: str) -> str
def generate_loadfolders_xml(versions: list) -> str
def create_directory_structure(base_path: str, config: dict)
def generate_package_id(author: str, mod_name: str) -> str
```

### 4.2 配置对象结构

```python
config = {
    'mod_name': 'MyMod',
    'author': 'Author',
    'description': '模组描述',
    'url': 'https://...',
    'package_id': 'Author.MyMod',
    'preset': 'content',  # content/function/framework/custom
    'needs_code': False,
    'needs_harmony': False,
    'multi_version': False,
    'versions': ['1.6'],
    'dependencies': [],
    'content_types': ['weapons', 'buildings'],
    'create_location': 'current',  # current/mods/custom
    'custom_path': None,
    'rimworld_path': 'C:\\...',
}
```

### 4.3 错误处理

- 验证模组名称和作者名称（不能为空，不能包含非法字符）
- 验证路径存在性（RimWorld安装路径、创建目标路径）
- 检查目标目录是否已存在（避免覆盖）
- 验证packageId格式
- 处理文件写入失败的情况

### 4.4 边界情况处理

- 用户输入为空时使用默认值
- 路径包含中文或特殊字符时的处理
- Windows/Linux路径分隔符的兼容性
- 用户中断操作时的清理

### 4.5 输出摘要格式

```
✓ 模组创建成功！

创建位置：C:\Projects\MyMod

目录结构：
MyMod/
├── About/
│   └── About.xml
├── 1.6/
│   ├── Assemblies/
│   ├── Defs/
│   │   └── ThingDefs/
│   └── Textures/
└── Source/
    └── MyMod.csproj

下一步建议：
1. 在Source/目录下开始编写C#代码
2. 在Defs/ThingDefs/目录下创建XML定义文件
3. 在Textures/目录下添加贴图资源
4. 使用Visual Studio或Rider打开MyMod.csproj开始开发

环境变量配置（可选，用于团队协作）：
设置环境变量 RimWorldPath=C:\NiwtGames\Steam\steamapps\common\RimWorld
```

### 4.6 中文本地化要点

- 所有用户交互界面使用中文
- About.xml的description字段使用中文
- 生成的注释和说明使用中文
- 技术字段（packageId、目录名、文件名）使用英文
- 错误提示使用中文

## 5. 实现计划

详细的实现计划将通过`writing-plans` skill生成。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-17 | 完成rimworld-mod-init skill的完整设计方案 | Claude Sonnet 4.5 |
