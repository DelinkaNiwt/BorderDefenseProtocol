# 武装构型视觉局部覆盖与 VerbProperties 根因修正 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让武装型可以只覆盖动作视觉图层而保留动作姿态，并保证纯视觉武装型不会凭空创建 `VerbProperties`。

**Architecture:** 保留现有完整 `VisualPresetDefName` 作为整套视觉替换入口，新增中性的视觉图层覆盖字段。局部覆盖替换覆盖预设声明的主贴图和附加层，姿态、缩放策略、握持和其它非图层字段继续读取动作基础视觉预设；覆盖型未声明附加层时不继承基础附加层，避免把基础动作的专属装饰带入新武装外观。视觉覆盖名称沿正式表达结果和视觉投影链传递。`VerbProperties` 继续保持可选对象，只有覆盖块显式声明其字段时才复制或创建。

**Tech Stack:** C#、RimWorld Def/XML、PowerShell 冒烟测试、Release `dotnet msbuild`。

---

### Task 1: 建立红灯回归测试

**Files:**
- Modify: `Source/BDP.Tests/ChipArmamentFormMeleeOverrideSmokeTests.ps1`
- Modify: `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`

**Step 1: Write the failing test**

让临时型声明视觉图层局部覆盖字段，而不是替换整个视觉预设；测试同时要求弧月基础视觉预设仍保留、临时覆盖没有继承弧月附加层，且纯视觉覆盖结果不创建 `VerbProps`。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/ChipArmamentFormMeleeOverrideSmokeTests.ps1'
```

Expected: FAIL，原因是覆盖定义模型和表达链尚未携带局部视觉覆盖字段。

### Task 2: 传递局部视觉覆盖字段

**Files:**
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Defs/ChipArmamentFormOverrides.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Resolution/ChipArmamentFormExpressionService.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionPresentationConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/ChipExpressionEntryContract.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/ChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceDeclaration.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceMaterial.cs`
- Modify: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `Source/BDP/Core/Expressions/Model/VisualResidentEntry.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/DefaultExpressionSourceDeclarationProvider.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ComboFormalExpressionResultFactory.cs`
- Modify: `Source/BDP/Core/Combos/Config/ComboExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs`

**Step 1: Write minimal implementation**

新增 `VisualGraphicOverrideDefName`，作为视觉图层覆盖来源；完整视觉预设字段保持原有含义。所有复制和发布边界逐一转发该字段。

**Step 2: Run the focused test**

测试必须通过，并确认弧月基础 `VisualPresetDefName` 没有被临时型局部覆盖清空或替换。

### Task 3: 在姿态解析中合并主贴图

**Files:**
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseRequest.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/TriggerVisualLaunchOriginResolver.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Resolve base and override presets**

绘制入口继续以动作基础视觉预设解析姿态；若存在视觉图层覆盖预设，只用覆盖预设的 `GraphicData/OverlayLayers` 解析图层，不进入 `ReplaceTextureOnly` 的宿主装备姿态路径。

**Step 2: Preserve base presentation**

姿态、动作阶段、基础绘制缩放、握持锚点和枪口锚点仍由基础动作视觉预设处理。主贴图覆盖预设不得提供原版武器角度语义。

**Step 3: Run focused test**

确认临时型不再被判定为 `ReplaceTextureOnly`，且弧月 `DefaultAngle=-50` 仍是姿态来源。

### Task 4: 收紧 VerbProperties 覆盖边界

**Files:**
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Resolution/ChipArmamentFormExpressionService.cs`
- Modify: `Source/BDP.Tests/ChipArmamentFormMeleeOverrideSmokeTests.ps1`

**Step 1: Preserve null semantics**

纯视觉、Tool、Execution 或模块覆盖不得创建空 `VerbProperties`；显式 VerbProperties 字段覆盖才允许复制已有对象或创建覆盖对象。

**Step 2: Add regression assertions**

覆盖测试必须同时检查近战模式、`VerbProps == null` 和没有默认射程/精度/射击节奏的污染。

### Task 5: 验证、记录与提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志57.md`

**Step 1: Build**

```powershell
dotnet msbuild 'Source/BDP/BDP.csproj' -p:Configuration=Release -t:Build -v:minimal
dotnet msbuild 'Source/BDP.Content/BDP.Content.csproj' -p:Configuration=Release -t:Build -v:minimal
```

**Step 2: Run focused tests**

运行视觉、制造、表达解析和 VerbProperties 相关冒烟测试。

**Step 3: Run `git diff --check` and commit**

只提交本计划涉及的源代码、Def、测试、计划和日志；保留用户已有的无关贴图及缓存改动。
