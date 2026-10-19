# 触发器装配台界面调整 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修正护盾范围图、补充芯片库容量，并让触发器装配台左栏布局与三栏标题统一符合截图要求。

**Architecture:** 继续在 Content 的装配窗口层实现。护盾图复用现有几何绘制器；芯片仓容量通过设施读取口聚合，不把设施扫描逻辑放进 UI；左栏只调整布局坐标与必要文本入口，右栏增加与其他面板一致的标题栏。事务、解析模型和 Core 不变。

**Tech Stack:** C# 7.3、RimWorld 1.6 UI/Unity IMGUI、PowerShell 静态契约测试、MSBuild。

---

### Task 1: 为本轮几何、容量、布局和标题写失败契约

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerMeleeAndShieldDataPathSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerLeftMiddlePanelSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblyWindowSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblyContentBoundarySmokeTests.ps1`

**Step 1: 写测试断言**

- 护盾范围契约要求使用 `ChartGeometryRenderer.DrawFilledPolygon` 和 `-angle / 2f`、`angle / 2f` 对称端点，不再以 0 度作为单侧起点。
- 设施契约要求读取口暴露容量上限，容器暴露容量值，中栏窗口把容量参数传入标题绘制，并使用双参数 `ChipCount` 文案。
- 左栏契约要求去除主副槽说明、`OperatorFront`、旧中栏开关，增加水平居中偏移和列高居中计算，标题使用新语言键。
- 右栏契约要求拥有标题栏绘制入口、`DetailPanel` 标题语言键和内容滚动区位于标题下方。

**Step 2: 运行测试确认当前实现失败**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyContentBoundarySmokeTests.ps1
```

Expected: FAIL，失败应指向旧扇形起点、缺失容量读取、旧左栏文字/布局或右栏标题，而不是测试脚本错误。

**Step 3: 提交失败契约**

```powershell
git add -- '模组工程\BorderDefenseProtocol\Source\BDP.Tests'
git commit -m "test: 增加装配台界面调整契约"
```

### Task 2: 修正护盾范围图的中心和对称扇形

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: 生成以正北为中线的弧点**

增加带成员注释的弧点构造逻辑；角度端点使用 `-angle / 2f` 与 `angle / 2f`，通过已有 `CoveragePoint` 转成屏幕坐标。

**Step 2: 绘制几何填充**

用 `ChartGeometryRenderer.DrawFilledPolygon(rect, center, arcPoints, fillColor)` 替换当前从圆心向单侧画粗线的半透明填充；边界线继续从同一圆心绘制，保留圆形轮廓和文字。

**Step 3: 运行护盾契约**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerMeleeAndShieldDataPathSmokeTests.ps1
```

Expected: PASS。

### Task 3: 补充芯片库容量上限读取和显示

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Facility/IAssemblerFacilityProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Facility/DefaultAssemblerFacilityProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Building/CompChipContainer.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Window_TriggerAssembly.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipInventory.cs`
- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml`

**Step 1: 暴露单仓容量**

在 `CompChipContainer` 增加内部只读容量属性，复用已有 `ResolveMaxCapacity`，不改变容器接收校验。

**Step 2: 聚合有效连接仓容量**

在设施读取口增加容量上限方法；默认实现遍历已有 `EnumerateActiveStorageComps()`，把每个有效芯片仓容量相加。

**Step 3: 传入并显示当前/上限**

窗口同时读取芯片列表和容量上限；库存面板标题绘制接受两个数值，使用语言包文案 `库存芯片 · {0} / {1} 枚`，并为更长文本扩大右侧标题矩形。

**Step 4: 运行容量契约**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyContentBoundarySmokeTests.ps1
```

Expected: PASS。

### Task 4: 收紧左栏文字和主体布局

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml`

**Step 1: 删除标记的多余元素**

移除 `DrawSlotColumn` 的主副槽说明参数和绘制，移除中央操作者“正面”标签；同步删除只由这些入口使用的语言键。中栏兼容性开关已在前一提交删除，本轮保持不存在。

**Step 2: 修正主副槽标题和垂直位置**

让 `DrawSlotColumn` 用完整标题行绘制 `主手槽`/`副手槽`，并按 `GetSlotColumnHeight` 返回的标题+卡片总高度计算垂直起点。

**Step 3: 修正三块主体水平位置**

在滚动视图内计算可视内容宽度和左右等距偏移，将舞台、特殊槽位、Trion 条统一放在同一居中内容矩形内，命中区域继续使用实际绘制坐标。

**Step 4: 更新左栏标题文案**

将现有左栏小标题和中文标题语言值改为 `01 // ASSEMBLY STATUS` 与“装配状态”。

**Step 5: 运行左栏契约**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerLeftMiddlePanelSmokeTests.ps1
```

Expected: PASS。

### Task 5: 给右栏补齐统一标题

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/Panel_ChipDetail.cs`
- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml`

**Step 1: 绘制右栏标题栏**

复用左栏/中栏的深青色标题风格，增加 `03 // CHIP DETAIL` 小标题和“芯片详情”主标题；即使没有选中芯片也保留标题。

**Step 2: 下移详情滚动区**

将详情内容的 `outRect` 从标题栏下方开始，空白状态只清空内容，不覆盖标题。

**Step 3: 运行标题契约**

```powershell
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblerChipDetailPanelSmokeTests.ps1
pwsh -NoProfile -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
```

Expected: PASS。

### Task 6: 完整验证、记录并提交

**Files:**
- Create or modify: `日志/Agent工作日志/Agent日志107.md`

**Step 1: 运行相关回归测试**

```powershell
$names = @('ChartGeometryRendererSmokeTests.ps1','TriggerAssemblerDetailRendererContractSmokeTests.ps1','TriggerAssemblerMeleeAndShieldDataPathSmokeTests.ps1','TriggerAssemblerChipDetailPanelSmokeTests.ps1','TriggerAssemblerLeftMiddlePanelSmokeTests.ps1','TriggerAssemblyWindowSmokeTests.ps1','TriggerAssemblyContentBoundarySmokeTests.ps1','UnifiedModAssemblyLayoutSmokeTests.ps1')
foreach ($name in $names) { & (Join-Path '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests' $name) }
```

Expected: 全部 PASS。

**Step 2: 构建 Content**

```powershell
dotnet msbuild '.\模组工程\BorderDefenseProtocol\Source\BDP.Content\BDP.Content.csproj' /t:Build /p:Configuration=Release /p:Platform=AnyCPU /nologo
```

Expected: exit 0；生成 DLL/PDB 不纳入提交。

**Step 3: 检查差异、写日志并提交**

```powershell
git diff --check
git add -- '模组工程\BorderDefenseProtocol\Source\BDP.Content' '模组工程\BorderDefenseProtocol\Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml' '日志\Agent工作日志\Agent日志107.md'
git commit -m "fix: 调整装配台界面布局与标题"
```
