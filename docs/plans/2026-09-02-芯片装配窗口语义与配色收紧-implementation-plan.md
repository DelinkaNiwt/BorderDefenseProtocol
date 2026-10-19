# 芯片装配窗口语义与配色收紧 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 统一右栏底色和芯片名称显示，修正左栏 Trion 资源条语义并压缩布局，使常规装配信息无需滚动即可完整显示。

**Architecture:** 保留现有窗口三栏、槽位交互和预览快照；新增仅供 Content（正式内容）窗口使用的芯片显示名称格式化器。右栏只替换外层底色绘制，资源条只更换展示字段和绘制方式，左栏通过删除重复摘要并收紧资源卡片高度释放空间。

**Tech Stack:** C#（C#语言）、RimWorld Verse UI（边缘世界 Verse 用户界面）、UnityEngine 颜色/矩形绘制、PowerShell 冒烟测试、dotnet MSBuild（.NET 构建）。

---

### Task 1: 增加失败契约测试

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblyWindowSmokeTests.ps1`

**Step 1: Write the failing test**

- 要求 `Panel_ChipDetail` 不再调用 `Widgets.DrawMenuSection`，并声明与左/中栏一致的面板颜色。
- 要求存在 `ChipDisplayNameFormatter`，中栏、左栏、右栏详情标题和 Tooltip 均通过它显示芯片名称。
- 要求资源条使用 `PreviewReserved`，不再读取 `Cur` 作为主数值，不再调用 `TrionReserved`/`TrionAvailable` 两行文案。
- 要求资源条绘制剩余段、预占用段和分界线，并将标题语言键改为“芯片预占用”。
- 要求 `TrionCapacityHeight` 小于原来的 82，确保删除摘要后布局确实收紧。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
```

Expected: FAIL because the new formatter、资源条字段和右栏底色契约尚未存在。

**Step 3: Commit**

```powershell
git add -- 模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1 模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblyWindowSmokeTests.ps1
git commit -m "test: 增加芯片装配窗口语义契约"
```

### Task 2: 实现界面层芯片名称格式化

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipDisplayNameFormatter.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipInventory.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: Implement the minimal formatter**

- 从现有 `BDP_ChipManufacturing_ProductLabel` 语言键传入空名称，取得当前语言的产品前缀。
- 只移除名称开头完全匹配的前缀；无前缀或空文本原样返回。
- 不修改 `Thing.Label`、Def、芯片快照或存档数据。

**Step 2: Replace all in-scope display reads**

- 中栏卡片标题和 Tooltip 使用格式化名称。
- 左栏槽位名称、槽位 Tooltip 使用格式化名称；由来源功能名称拼接的名称保持原有语义。
- 右栏详情 Hero（详情头部）标题使用格式化名称。

**Step 3: Run tests to verify it passes**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
```

Expected: 名称格式化相关断言 PASS。

### Task 3: 修正右栏底色

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipDetail.cs`

**Step 1: Replace the default section background**

- 用与左栏/中栏相同色值的 `Widgets.DrawBoxSolidWithOutline` 绘制右栏外框。
- 保留现有内部滚动区域和详情内容绘制，不改变右栏数据布局。

**Step 2: Run the focused contract test**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
```

Expected: 右栏颜色契约 PASS。

### Task 4: 重做 Trion 预占用资源条并收紧左栏

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml`

**Step 1: Change the data meaning**

- `DrawTrionCapacity` 右上数字改为 `preview.PreviewReserved / preview.Max`。
- 标题语言键改为“芯片预占用”。
- 移除“当前预占用”和“可用”两行标签及其不再需要的语言键。

**Step 2: Draw the full two-tone track**

- 整条轨道先绘制剩余容量色，再绘制累计预占用色。
- 按 `PreviewReserved / Max` 的比例裁剪预占用段，并在比例位置绘制细分界线。
- 资源条保留边框和必要内边距，卡片高度收紧到约 60 像素。

**Step 3: Run the focused tests**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
```

Expected: 资源条语义和左栏布局契约 PASS。

### Task 5: 完整验证、记录并提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志104.md`

**Step 1: Run all existing checks**

```powershell
$testRoot='模组工程\BorderDefenseProtocol\Source\BDP.Tests'
$tests=@('ChartGeometryRendererSmokeTests.ps1','TriggerAssemblerDetailRendererContractSmokeTests.ps1','TriggerAssemblyWindowSmokeTests.ps1','UnifiedModAssemblyLayoutSmokeTests.ps1','TriggerAssemblyContentBoundarySmokeTests.ps1')
foreach($test in $tests){ & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $testRoot $test); if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE } }
dotnet msbuild '模组工程\BorderDefenseProtocol\Source\BDP.Content\BDP.Content.csproj' /t:Build /p:Configuration=Release /p:Platform=AnyCPU
if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE }
git diff --check
```

Expected: 5 个测试 PASS，`BDP.Content` Release 构建成功，差异检查通过。

**Step 2: Append the work log**

- 记录右栏底色、名称显示层、资源条语义和左栏高度收紧。
- 记录未改变底层芯片数据；记录测试、构建和最终需要进游戏复核的事项。

**Step 3: Commit source, tests and log**

```powershell
git add -- 模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipDisplayNameFormatter.cs 模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipInventory.cs 模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs 模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs 模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipDetail.cs 模组工程/BorderDefenseProtocol/Languages/ChineseSimplified\ (简体中文)/Keyed/Windows.xml 日志/Agent工作日志/Agent日志104.md
git commit -m "feat: 统一芯片装配窗口显示语义"
```

不提交构建生成的 `1.6/Assemblies/BDP.Content.dll` 和 `.pdb`，保留项目当前对生成物的版本控制边界。
