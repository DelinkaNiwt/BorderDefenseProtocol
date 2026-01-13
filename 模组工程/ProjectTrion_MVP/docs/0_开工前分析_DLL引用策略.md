---
摘要: RimWorld模组生态中的DLL依赖引用方式调研与MVP推荐方案
版本号: v0.1
修改时间: 2026-01-11
关键词: DLL引用,前置模组,HintPath,框架依赖,编译配置
标签: [待审]
---

# Trion_MVP DLL引用策略 v0.1

## 背景

MVP需要引用ProjectTrion_Framework编译后的DLL。为了确定最佳实践，调研了RimWorld模组生态中已有的框架模组及其依赖者。

---

## 调研发现

### 1. 真实案例：WeaponFitting → AncotLibrary

**WeaponFitting.csproj**（依赖AncotLibrary）：

```xml
<Reference Include="AncotLibrary">
  <HintPath>..\..\..\..\..\NiwtGames\Steam\steamapps\workshop\content\294100\2988801276\1.6\Assemblies\AncotLibrary.dll</HintPath>
</Reference>
```

**分析**：
- ✓ 使用绝对HintPath，指向**Steam workshop目录**中的DLL
- ✓ 路径明确，不存在歧义
- ✓ DLL来源清晰（2988801276是AncotLibrary的Steam ID）

---

### 2. 反例：Milira_米莉拉 → AncotLibrary / AlienRace

**Milira.csproj**（依赖AncotLibrary和AlienRace）：

```xml
<Reference Include="AncotLibrary" />
<Reference Include="AlienRace" />
```

**分析**：
- ✗ 没有HintPath
- ✗ 这种引用方式依赖于开发者的**本地环境配置**
- ✗ 无法在其他电脑上直接编译（除非配置特殊的搜索路径）
- ✗ 不是推荐做法

---

### 3. 实际DLL位置

| 模组 | DLL位置 |
|------|--------|
| AncotLibrary | `参考资源\模组资源\AncotLibrary\1.6\Assemblies\AncotLibrary.dll` |
| HumanoidAlienRaces（外星人框架） | `参考资源\模组资源\HumanoidAlienRaces_外星人框架\1.6\Assemblies\AlienRace.dll` |
| Milira（米莉拉） | `参考资源\模组资源\Milira_米莉拉\1.6\Assemblies\Milira.dll` |

---

## 推荐方案：模式A（基于WeaponFitting的成功案例）

### 核心原则
- ✓ 使用**绝对HintPath**引用前置框架DLL
- ✓ DLL来源明确且可追溯
- ✓ 项目可在任何装有编译环境的电脑上编译
- ✓ 符合RimWorld模组开发的业界实践

### MVP具体实现

#### 第1步：确认框架编译输出

ProjectTrion_Framework编译后，DLL应该在：
```
C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll
```

**注意**：如果框架的编译输出目录不同，需要先确认实际位置。

#### 第2步：配置MVP的.csproj

创建 `ProjectTrion_MVP\Source\ProjectTrion_MVP.csproj`，包含以下引用：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ProjectTrion_MVP</AssemblyName>
    <GenerateAssemblyInfo>False</GenerateAssemblyInfo>
    <TargetFramework>net48</TargetFramework>
  </PropertyGroup>
  <PropertyGroup>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <!-- RimWorld核心 -->
    <Reference Include="Assembly-CSharp">
      <HintPath>C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
    </Reference>

    <!-- ProjectTrion框架（前置依赖） -->
    <Reference Include="ProjectTrion">
      <HintPath>C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll</HintPath>
    </Reference>

    <!-- Unity引擎 -->
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.TextRenderingModule">
      <HintPath>C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.IMGUIModule" />

    <!-- Harmony补丁库 -->
    <Reference Include="0Harmony">
      <HintPath>C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\0Harmony.dll</HintPath>
    </Reference>

    <!-- 基础库 -->
    <Reference Include="System.Core" />
  </ItemGroup>
</Project>
```

#### 第3步：编译输出配置

MVP编译后，DLL应该输出到：
```
C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_MVP\1.6\Assemblies\ProjectTrion_MVP.dll
```

可在.csproj中配置：
```xml
<PropertyGroup>
  <OutputPath>..\1.6\Assemblies\</OutputPath>
</PropertyGroup>
```

---

## 方案对比

| 维度 | 方案A（推荐）<br/>绝对HintPath | 方案B<br/>相对路径 | 方案C<br/>无HintPath（Milira做法） |
|------|-----------|-----------|-----------|
| **编译可移植性** | ✓ 高（任何电脑可编译） | ✗ 低（依赖相对位置） | ✗ 低（需环境配置） |
| **依赖明确性** | ✓ 完全明确 | ⚠️ 相对明确 | ✗ 隐含不明 |
| **维护成本** | ✓ 低 | ⚠️ 中 | ✗ 高 |
| **多版本兼容** | ✓ 易于支持1.5/1.6切换 | ⚠️ 中等 | ✗ 困难 |
| **业界实践** | ✓ 主流做法（WeaponFitting） | ⚠️ 少见 | ✗ 非主流 |

---

## 相对路径方案（方案B，作为备选）

如果日后需要更灵活的相对引用（如支持多个目录位置），可考虑相对路径：

```xml
<Reference Include="ProjectTrion">
  <HintPath>..\..\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll</HintPath>
</Reference>
```

**适用场景**：
- 框架和MVP在同一个source control仓库中
- 需要频繁调整框架代码并同时编译MVP
- 团队约定了统一的目录结构

---

## 执行清单

- [ ] 确认ProjectTrion_Framework的编译输出目录
- [ ] 获取编译好的ProjectTrion.dll
- [ ] 创建MVP的.csproj文件，配置上述HintPath
- [ ] 在VS中打开项目，验证引用无误
- [ ] 编译一次，确保：
  - [ ] 编译成功（无CS0246等引用错误）
  - [ ] 输出DLL在 `1.6\Assemblies\` 目录
  - [ ] DLL大小合理（>100KB）

---

## 后续考虑

### 如果框架DLL位置变化

如需调整DLL位置（如框架重构），只需更新.csproj中的HintPath，无需改代码。

### 多版本支持

如需支持RimWorld 1.5和1.6，可创建两个.csproj：
```
ProjectTrion_MVP_1.6.csproj  → 引用1.6版本的ProjectTrion.dll
ProjectTrion_MVP_1.5.csproj  → 引用1.5版本的ProjectTrion.dll
```

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v0.1 | 初版：基于WeaponFitting案例的DLL引用方案推荐、三个方案对比 | 2026-01-11 | code-engineer |

