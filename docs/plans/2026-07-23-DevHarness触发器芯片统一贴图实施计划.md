# DevHarness 触发器芯片统一贴图实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development and superpowers:verification-before-completion to implement this plan task-by-task.

**Goal:** 让 DevHarness 的全部 17 个芯片物品统一使用用户确认的 `BDP_TriggerChip.png`。

**Architecture:** 保留各芯片现有独立 `graphicData`，只统一其中的 `texPath`。新贴图只放入 DevHarness 的 1.6 资源目录，不修改主模组架构，也不覆盖仍被表达视觉使用的武器贴图。

**Tech Stack:** RimWorld 1.6 XML（可扩展标记语言）Def、PNG（便携式网络图形）资源、PowerShell 烟雾测试。

---

### 任务一：先固定统一贴图契约

**文件：**

- 新建：`模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessTriggerChipTextureSmokeTests.ps1`

**步骤一：编写失败的烟雾测试**

测试应：

1. 遍历 `BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips` 下的 XML 文件。
2. 提取所有直接继承 `BDP_ChipBase` 的 `ThingDef`，断言数量为 17。
3. 断言每个芯片块都包含 `<texPath>Things/Trigger/Chip/BDP_TriggerChip</texPath>`。
4. 断言 `1.6/Textures/Things/Trigger/Chip/BDP_TriggerChip.png` 存在。
5. 断言贴图 SHA-256（安全散列算法 256 位）为 `46D747674E99BF4ED5BFD409162FF442B9E0C99802EA560876E86840689B544A`。

**步骤二：运行测试并确认先失败**

运行：

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTriggerChipTextureSmokeTests.ps1'
```

预期：因统一贴图尚不存在或旧 `texPath` 仍存在而失败。

### 任务二：实施最小贴图替换

**文件：**

- 新建：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Textures/Things/Trigger/Chip/BDP_TriggerChip.png`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChip_Shield.xml`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_AbilityHediff.xml`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Invalid.xml`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_PassiveMixed.xml`
- 修改：`模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_SenkuKogetsu.xml`

**步骤一：复制源贴图**

将 `日志/芯片规范版.png` 原样复制为目标资源，不改尺寸、透明度或像素。

**步骤二：统一芯片贴图路径**

只把直接继承 `BDP_ChipBase` 的 17 个芯片物品路径改为：

```xml
<texPath>Things/Trigger/Chip/BDP_TriggerChip</texPath>
```

同步修正紧邻贴图路径且已经失真的旧说明注释，不改其他定义内容。

**步骤三：运行新增烟雾测试**

运行：

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTriggerChipTextureSmokeTests.ps1'
```

预期：输出 `DevHarnessTriggerChipTexture PASS`。

### 任务三：回归验证、工作日志与提交

**文件：**

- 修改或新建：`日志/Agent工作日志/Agent日志*.md`

**步骤一：验证 XML**

用 PowerShell 的 XML 解析器加载六个修改文件，预期全部解析成功。

**步骤二：运行相关既有测试**

至少运行：

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessChipTrionConfigSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessSenkuKogetsuAuthoringSmokeTests.ps1'
```

预期：均输出 `PASS`；若既有测试因当前工作区其他未提交改动失败，只记录与本次贴图改动是否相关，不修改计划外内容。

**步骤三：记录工作日志**

在最新且未满 20 条的工作日志顶部新增本次记录；若已满 20 条则创建下一个日志文件。

**步骤四：只提交本任务文件**

使用显式路径暂存并提交测试、贴图、六个 XML 文件和工作日志，不带入工作区已有修改。

