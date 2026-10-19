# 触发器装配台交互逻辑 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让触发器装配台的槽位选择、预占用统计和拖拽高亮遵循实际装配状态与槽位类型。

**Architecture:** 在 Content 装配窗口层移除选择态到列表过滤的耦合，中栏只保留搜索和分类筛选。预占用快照回归正式 loadout reader 的实际累计值；新增一个小型窗口层兼容性工具，供库存卡片和槽位高亮共同使用，事务层保持不变。

**Tech Stack:** C# 7.3、RimWorld 1.6 UI/Unity IMGUI、PowerShell 静态契约测试、MSBuild。

---

### Task 1: 为三处交互规则写失败契约测试

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblyWindowSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerLeftMiddlePanelSmokeTests.ps1`

**Step 1: 写测试断言**

- 断言库存面板不再存在 `showUnavailable` 兼容性开关、兼容性隐藏条件或把选中槽位传入列表筛选方法。
- 断言预览服务和窗口调用只使用正式 reader 的快照入口，不再传入 `hasPreviewSlot`、`previewSide`、`previewIndex`、`previewChip`。
- 断言 `PreviewReserved` 直接等于实际已装配累计值。
- 断言槽位绘制的 `dropTarget` 通过共享兼容性工具判断拖拽芯片和目标侧别，且不再只用 `IsDragging` 判断。

**Step 2: 运行测试确认当前实现失败**

Run:

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1
```

Expected: FAIL，失败原因应分别指向旧的预览参数、默认筛选状态或无条件拖拽高亮，而不是脚本语法错误。

**Step 3: 提交测试契约**

```powershell
git add -- '模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1' '模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1'
git commit -m "test: 增加装配台交互逻辑契约"
```

### Task 2: 修正预占用快照只读取实际装配状态

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/TriggerAssemblyPreviewService.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Window_TriggerAssembly.cs`

**Step 1: 收紧快照接口**

将 `BuildSnapshot` 参数收敛为 `Pawn pawn, ITriggerLoadoutReader reader`，删除选中槽位和选中芯片的替换模拟代码；保留 `CalculateLoadedCapacity` 的镜像去重逻辑。

**Step 2: 更新窗口调用**

`DrawAssemblyPanel` 只把 pawn 与正式 `loadoutReader` 传给预览服务。快照中的 `Reserved` 与 `PreviewReserved` 都设为同一个 `loadedReserved`。

**Step 3: 运行相关测试**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
```

Expected: 预占用契约通过；其余尚未实现的交互契约仍可能失败。

### Task 3: 移除槽位选择造成的列表筛选并统一兼容性判断

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/TriggerAssemblyChipCompatibility.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipInventory.cs`

**Step 1: 新增窗口层兼容性工具**

提供一个带 XML/C# 成员注释的内部静态方法，读取 `ChipSnapshotAccess`，将 `TriggerSide.Special` 映射到 `ChipSlotRegion.Special`，其余侧别映射到 `ChipSlotRegion.MainSub`；空或无效定义返回不兼容。

**Step 2: 移除中栏兼容性筛选入口**

删除 `showUnavailable` 状态、过滤条中的“显示不可用”控件以及 `ShouldShowChip` 中按 `hasSelectedSlot` 和 `compatible` 隐藏芯片的分支。`ShouldShowChip` 与 `CountVisibleChips` 只保留搜索和分类判断。

**Step 3: 复用兼容性工具**

移除库存面板内重复的兼容性定义实现，使用新工具只计算卡片视觉；搜索与分类筛选保持原逻辑。

**Step 4: 运行中栏测试**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1
```

Expected: 列表选择契约通过。

### Task 4: 让拖拽高亮只显示匹配槽位

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`

**Step 1: 接入共享兼容性工具**

在 `DrawSlot` 的 `dropTarget` 条件中同时检查拖拽芯片非空、非绑定镜像槽位以及芯片与目标槽位侧别兼容。

**Step 2: 运行交互测试**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1
```

Expected: 两个目标测试 PASS。

### Task 5: 完整验证、记录并提交

**Files:**
- Create: `日志/Agent工作日志/Agent日志106.md`

**Step 1: 运行完整契约测试**

```powershell
$tests = Get-ChildItem '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests' -Filter '*SmokeTests.ps1' | Sort-Object Name
foreach ($test in $tests) { & $test.FullName }
```

Expected: 所有测试输出 PASS 且无错误。

**Step 2: 构建 Content**

```powershell
dotnet msbuild '.\模组工程\BorderDefenseProtocol\Source\BDP.Content\BDP.Content.csproj' /t:Build /p:Configuration=Release /p:Platform=AnyCPU
```

Expected: Build succeeded；生成的 `1.6/Assemblies/BDP.Content.dll` 与 `.pdb` 保留为工作区生成物，不纳入本次提交。

**Step 3: 检查差异和工作区**

```powershell
git diff --check
git status --short
```

Expected: 只有本次源代码、测试、文档/日志提交记录，生成 DLL/PDB 仍保持未跟踪提交状态。

**Step 4: 写工作日志**

记录根因、默认显示不可用策略、实际累计预占用修复、按槽位类型高亮以及验证结果，控制在 20 条以内。

**Step 5: 提交实现**

```powershell
git add -- '模组工程\BorderDefenseProtocol\Source\BDP.Content\Assembly\Window' '日志\Agent工作日志\Agent日志106.md'
git commit -m "fix: 修正装配台交互状态逻辑"
```
