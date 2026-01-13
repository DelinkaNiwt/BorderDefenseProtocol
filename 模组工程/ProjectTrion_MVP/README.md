---
摘要: ProjectTrion_MVP模组项目总述，目录说明，快速开始
版本号: v0.1
修改时间: 2026-01-11
关键词: MVP,框架验证,项目说明,快速开始
标签: [草稿]
---

# ProjectTrion_MVP - 快速开始

## 📋 项目概述

**ProjectTrion_MVP** 是 ProjectTrion框架的验证模组，目的是通过10个精心设计的测试场景验证框架核心功能的可行性。

- **游戏版本**：RimWorld 1.6.4633
- **前置依赖**：ProjectTrion_Framework
- **开发状态**：第5步（脏原型）阶段
- **预期周期**：4-5个工作日

---

## 📁 目录结构

```
ProjectTrion_MVP/
├─ README.md                              # 本文件
├─ About/                                  # 模组信息
│  └─ About.xml                           # 模组描述
├─ docs/                                   # 工作流文档（重要）
│  ├─ 0_开工前检查报告.md                 # 开工前的完整评估
│  ├─ 0_开工前分析_DLL引用策略.md         # DLL依赖调研报告
│  ├─ 草稿/                               # 草稿区（临时文件）
│  └─ ...后续阶段文档...
├─ 1.6/                                    # RimWorld 1.6版本资源
│  ├─ Assemblies/                         # 编译输出DLL
│  │  └─ ProjectTrion_MVP.dll             # 主程序集
│  ├─ Defs/
│  │  ├─ ThingDefs_Buildings/             # 建筑定义
│  │  └─ RecipeDefs/                      # 配方定义
│  ├─ Languages/ChineseSimplified/Keyed/  # 中文本地化
│  ├─ Textures/                           # 纹理资源
│  └─ LoadFolders.xml                     # 版本加载配置
├─ Source/                                 # C#源代码
│  ├─ ProjectTrion_MVP.csproj             # 项目文件
│  ├─ ProjectTrion_MVP/
│  │  ├─ DefaultTrionStrategy.cs          # Strategy实现
│  │  ├─ Buildings/
│  │  │  └─ Building_TrionDetector.cs     # 检测仪
│  │  ├─ Components/
│  │  │  ├─ CompTrionComponents.cs        # 组件装备
│  │  │  └─ ...其他组件...
│  │  ├─ Hediffs/
│  │  │  └─ Hediff_TrionGland.cs          # 腺体植入
│  │  ├─ Things/
│  │  │  └─ ...组件物品定义...
│  │  └─ Utilities/
│  │     └─ ...辅助工具...
│  └─ Properties/
│     └─ AssemblyInfo.cs
└─ LoadFolders.xml                        # 根目录加载配置
```

---

## 🚀 快速开始

### 前置条件

1. **RimWorld 1.6.4633** 已安装
   - 路径：`C:\NiwtGames\Steam\steamapps\common\RimWorld`

2. **ProjectTrion_Framework** 已编译
   - DLL位置：`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll`

3. **Visual Studio 2022** 或更高版本

### 编译步骤

```bash
# 1. 打开项目文件
cd Source
# 2. 在VS中打开 ProjectTrion_MVP.csproj
# 3. 编译Release版本（Ctrl+Shift+B）
# 4. 确认输出：1.6\Assemblies\ProjectTrion_MVP.dll
```

### 测试步骤

```
1. 确认模组加载顺序：
   - ProjectTrion_Framework (前置)
   - ProjectTrion_MVP (主模组)

2. 启动RimWorld，加载新存档

3. 验证10个测试场景（详见设计文档）
```

---

## 📖 关键文档

| 文档 | 用途 | 位置 |
|------|------|------|
| **0_开工前检查报告** | 了解整体项目状态、设计决策、开工计划 | `docs/` |
| **0_开工前分析_DLL引用策略** | 理解如何引用框架DLL | `docs/` |
| 1_模组核心概念设计 | MVP的设计目标和10个测试场景 | 临时/ |
| 2_功能详细设计说明书 | 8个核心功能的实现细节 | 临时/ |
| 3_数据结构设计规范 | CompTrion和天赋系统的数据结构 | 临时/ |
| 5_配置参数定义 | 所有可配置参数和验证规则 | 临时/ |
| 7_RiMCP验证清单 | 所有使用的RimWorld API的验证 | 临时/ |

**建议阅读顺序**：
1. 本文件 (README)
2. 0_开工前检查报告.md
3. 1_模组核心概念设计.md
4. 2_功能详细设计说明书.md
5. 5_配置参数定义.md

---

## 🎯 MVP的核心目标

### 验证的功能（10个场景）

| # | 场景 | 验证内容 |
|----|------|---------|
| 1 | 植入→初始化 | 植入腺体后CompTrion自动附加 |
| 2 | 首次检测→天赋 | 检测仪首次扫描生成天赋 |
| 3 | 装备触发器和组件 | 组件正确注册到TriggerMount |
| 4 | 生成战斗体 | 快照保存和Reserved计算 |
| 5 | 基础消耗 | 每60Tick消耗计算正确 |
| 6 | 激活组件消耗 | 激活后消耗增加 |
| 7 | 伤口泄漏 | 受伤后漏率增加，关键部位加倍 |
| 8 | Bail Out触发 | Available≤0时自动脱离 |
| 9 | 快照恢复 | 战斗体摧毁后状态完全回滚 |
| 10 | 读档持久化 | Capacity和天赋存档后完整恢复 |

### 不在MVP范围内

- ❌ 复杂的UI系统
- ❌ 20+种组件和技能树
- ❌ 多Strategy同时存在
- ❌ 升级、融合、突变系统
- ❌ 深度性能优化

---

## ⚙️ 开发工作流

### 第5步：脏原型（Dirty Prototype）

**目标**：在2-3天内完成可测试的原型

**工作流**：
1. 快速评审设计文档
2. 按优先级实现核心功能
3. 编译验证，标注已知问题
4. 完成README和初步测试

**质量要求**：
- ✓ 核心功能可编译运行
- ✓ 无运行时异常（日志可见预期消息）
- ✓ 代码可读性基本满足

### 第6步：测试完善（Testing & Refinement）

基于玩家反馈，修复问题、优化性能

### 第7步：正式开发（Formal Development）

完整代码、文档、本地化、完整测试

---

## 🔑 关键实现点

### DLL引用（重要）

在 `Source/ProjectTrion_MVP.csproj` 中：

```xml
<Reference Include="ProjectTrion">
  <HintPath>C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll</HintPath>
</Reference>
```

**注意**：使用绝对路径确保可移植性

### 命名空间约定

```csharp
// 框架层（来自ProjectTrion_Framework）
using ProjectTrion.Core;           // 核心接口和枚举
using ProjectTrion.Components;     // CompTrion等

// MVP应用层
namespace ProjectTrion_MVP
{
    // DefaultTrionStrategy 实现
    // Building_TrionDetector 实现
    // 组件和其他应用逻辑
}
```

### 注释语言

- 代码注释：**简体中文优先**
- 技术文档：**中文+English并行**

---

## 📊 项目时间表

| 阶段 | 任务 | 预计耗时 | 状态 |
|------|------|---------|------|
| 筹备 | 需求确认、设计审核、编译配置 | 0.5天 | ✓ 完成 |
| 第5步 | 脏原型开发 | 2-3天 | ⏳ 进行中 |
| 第6步 | 测试和完善 | 1-2天 | 📅 计划中 |
| 第7步 | 正式开发 | 2-3天 | 📅 计划中 |

---

## ❓ 常见问题

### Q: 框架DLL在哪里获取？
A: ProjectTrion_Framework编译后，DLL在 `模组工程\ProjectTrion_Framework\1.6\Assemblies\ProjectTrion.dll`

### Q: 如何验证编译成功？
A: 检查 `1.6\Assemblies\ProjectTrion_MVP.dll` 是否存在且大小>100KB

### Q: 卸载时需要清理什么？
A: MVP本身无需特殊清理，但验证卸载后旧存档能正常加载

### Q: 如何跑10个测试场景？
A: 见设计文档中的"功能详细设计说明书"

---

## 📞 技术联系

**代码工程师**：code-engineer
**框架设计**：需求架构师
**审核和反馈**：通过docs目录的markdown文件交流

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v0.1 | 初版：项目说明、目录结构、快速开始、时间表 | 2026-01-11 | code-engineer |

