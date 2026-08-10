# CombatBody Wound Exit and Emergency Escape Badge Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修正战斗体退出后伤口 Trion（触力/能量）持续消耗残留，并为 Trion Gizmo（触力操作面板）增加未搭载、已搭载未就绪、已就绪三态紧急脱离徽标。

**Architecture:** 退出问题只调整 `CombatBodyExitTransaction`（战斗体退出事务）的清理顺序，在肉身恢复和相位退出后统一清理伤口派生运行时。徽标问题在 CombatBody（战斗体）域新增只读三态解析器，单向聚合 Trigger（触发体）装载、Chip（芯片）正式定义和 Expression（表达）发布结果；Trion 核心继续只消费通用贴图徽标。

**Tech Stack:** C#、RimWorld 1.6、Harmony（补丁库）、PowerShell（脚本语言）冒烟测试、PNG（便携式网络图形）贴图。

---

### Task 1: 锁定并修正退出清理顺序

**Files:**
- Create: `Source/BDP.Tests/CombatBodyExitWoundRuntimeCleanupOrderSmokeTests.ps1`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`

**Step 1: Write the failing test**

新增静态顺序测试，读取 `CombatBodyExitTransaction.cs` 并断言：

```powershell
$cooldownIndex = $normalized.IndexOf(
    'rawCombatBodyService.EnterCooldown(ResolveCooldownTicks(exitMode),ResolveExitReason(exitMode));')
$woundClearIndex = $normalized.IndexOf(
    'owner.WoundRuntime.ClearActiveRuntime(ownerPawn);')

Assert-True ($cooldownIndex -ge 0) 'Exit transaction must enter cooldown.'
Assert-True ($woundClearIndex -gt $cooldownIndex) 'Wound runtime must be cleared after body restore and phase exit.'
Assert-True (
    ([regex]::Matches($normalized, [regex]::Escape(
        'owner.WoundRuntime.ClearActiveRuntime(ownerPawn);'))).Count -eq 1
) 'Wound runtime must have one final cleanup.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodyExitWoundRuntimeCleanupOrderSmokeTests.ps1'
```

Expected: FAIL，提示伤口运行时清理仍位于进入冷却之前。

**Step 3: Write minimal implementation**

在 `CombatBodyExitTransaction.Execute(...)` 中，把：

```csharp
owner.WoundRuntime.ClearActiveRuntime(ownerPawn);
```

从 Trigger（触发体）关闭之后移到：

```csharp
rawCombatBodyService.EnterCooldown(ResolveCooldownTicks(exitMode), ResolveExitReason(exitMode));
owner.WoundRuntime.ClearActiveRuntime(ownerPawn);
```

不增加第二次清理，不改变 Trion 释放、恢复冻结或崩解清零顺序。

**Step 4: Run tests to verify they pass**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodyExitWoundRuntimeCleanupOrderSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyWoundLifecycleSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
```

Expected: 全部 PASS（通过）。

**Step 5: Commit**

```powershell
git add -- '模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyExitWoundRuntimeCleanupOrderSmokeTests.ps1' '模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs'
git commit -m "fix(bdp): clean wound drains after combat body exit"
```

### Task 2: 锁定紧急脱离三态与系统边界

**Files:**
- Create: `Source/BDP.Tests/CombatBodyEmergencyEscapeBadgeStateSmokeTests.ps1`
- Create: `Source/BDP/Core/CombatBody/External/CombatBodyEmergencyEscapeBadgeState.cs`
- Create: `Source/BDP/Core/CombatBody/External/CombatBodyEmergencyEscapeBadgeStateResolver.cs`
- Modify: `Source/BDP/Core/CombatBody/Flow/CombatBodyEmergencyEscapeResolver.cs`
- Modify: `Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs`
- Create: `1.6/Textures/UI/CombatBody/EmergencyEscapeStatus.png`

**Step 1: Write the failing test**

新增静态边界测试并断言：

```powershell
Assert-True (Test-Path -LiteralPath $statePath) 'Emergency escape badge state enum must exist.'
Assert-True (Test-Path -LiteralPath $resolverPath) 'Emergency escape badge state resolver must exist.'
Assert-True ($stateText -match 'NotInstalled') 'State must represent no mounted chip.'
Assert-True ($stateText -match 'InstalledNotReady') 'State must represent mounted but not ready.'
Assert-True ($stateText -match 'Ready') 'State must represent ready.'
Assert-True ($resolverText -match 'TriggerSurfaceAccess\.ResolveLoadoutReader') 'Mounted state must read Trigger formal loadout.'
Assert-True ($resolverText -match 'ChipSurfaceAccess\.Read') 'Mounted state must read the formal chip definition.'
Assert-True ($resolverText -match 'CachedCollapseEmergencyEscape') 'Collapsing state must preserve cached readiness.'
Assert-True ($providerText -notmatch 'TriggerSurfaceAccess|ChipSurfaceAccess|ExpressionSurfaceAccess') 'Gizmo provider must not scan source systems.'
Assert-True ($trionText -notmatch 'EmergencyEscape|emergency_escape') 'Trion core must not know emergency escape business.'
Assert-True (Test-Path -LiteralPath $texturePath) 'Emergency escape badge texture must exist.'
```

再断言提供器：

- `NotInstalled`（未搭载）不返回紧急脱离徽标。
- `InstalledNotReady`（已搭载未就绪）使用灰暗色和“紧急脱离：未就绪”。
- `Ready`（已就绪）使用高亮色和“紧急脱离：已就绪”。
- 徽标通过 `icon: EmergencyEscapeIcon` 传入贴图，不使用紧急脱离图形键。

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodyEmergencyEscapeBadgeStateSmokeTests.ps1'
```

Expected: FAIL，因为三态类型、状态解析器和贴图尚不存在。

**Step 3: Add the three-state model**

新增：

```csharp
internal enum CombatBodyEmergencyEscapeBadgeState
{
    NotInstalled = 0,
    InstalledNotReady = 1,
    Ready = 2
}
```

每个枚举成员写中文注释。

**Step 4: Add the read-only state resolver**

`CombatBodyEmergencyEscapeBadgeStateResolver`（紧急脱离徽标状态解析器）：

1. 用 `TriggerSurfaceAccess.ResolveLoadoutReader(pawn)` 获取正式装载口。
2. 遍历非绑定镜像槽位。
3. 用 `ChipSurfaceAccess.Read(slot.LoadedChip)` 读取已经校验的芯片契约。
4. 扫描基础表达条目和形态操作条目，判断是否声明被动键 `EmergencyEscape`。
5. 没有能力芯片时返回 `NotInstalled`。
6. `Collapsing`（崩解中）阶段优先读取 `HostState.CachedCollapseEmergencyEscape`。
7. 其它阶段用现有 `CombatBodyEmergencyEscapeResolver.Resolve(pawn)` 判断正式发布结果。

把现有字符串集中成：

```csharp
internal const string EmergencyEscapePassiveKey = "EmergencyEscape";
```

执行解析和徽标状态解析共用这一常量。

**Step 5: Generate and install the icon**

使用内置图像生成工具生成：

```text
透明小型科幻紧急脱离状态图标；中心为向外上方离开的简洁箭头，外圈为断开的六边形门框；白色单色实心轮廓；无文字、无阴影、无渐变、无细碎纹理；适合缩放到 14×14 像素。
```

先以纯色键控背景生成，再移除背景并验证透明通道。最终保存为：

```text
1.6/Textures/UI/CombatBody/EmergencyEscapeStatus.png
```

**Step 6: Emit the badge from the CombatBody provider**

在 `CombatBodyTrionGizmoExtensionProvider`（战斗体 Trion 徽标提供器）中：

```csharp
private static readonly Texture2D EmergencyEscapeIcon =
    ContentFinder<Texture2D>.Get("UI/CombatBody/EmergencyEscapeStatus");
```

先解析一次三态，再绘制战斗体状态徽标。战斗体徽标之后：

```csharp
if (escapeState == CombatBodyEmergencyEscapeBadgeState.NotInstalled)
{
    yield break;
}

bool ready = escapeState == CombatBodyEmergencyEscapeBadgeState.Ready;
yield return new TrionGizmoExtensionBadge(
    icon: EmergencyEscapeIcon,
    tooltip: ready ? "紧急脱离：已就绪" : "紧急脱离：未就绪",
    tint: ready ? EmergencyEscapeReadyTint : EmergencyEscapeInactiveTint);
```

崩解提示文本同样使用三态结果，不再自行重新解析。

**Step 7: Run tests to verify they pass**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodyEmergencyEscapeBadgeStateSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyEmergencyEscapeResolverSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
& '.\Source\BDP.Tests\TrionGizmoPanelExtensionContractSmokeTests.ps1'
& '.\Source\BDP.Tests\TrionGizmoPanelLayoutSmokeTests.ps1'
```

Expected: 全部 PASS（通过）。

**Step 8: Commit**

```powershell
git add -- '模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyEmergencyEscapeBadgeStateSmokeTests.ps1' '模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/External/CombatBodyEmergencyEscapeBadgeState.cs' '模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/External/CombatBodyEmergencyEscapeBadgeStateResolver.cs' '模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Flow/CombatBodyEmergencyEscapeResolver.cs' '模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs' '模组工程/BorderDefenseProtocol/1.6/Textures/UI/CombatBody/EmergencyEscapeStatus.png'
git commit -m "feat(bdp): show emergency escape readiness badge"
```

### Task 3: 完整验证与日志

**Files:**
- Modify: `日志/Agent工作日志/Agent日志11.md`

**Step 1: Run focused regression**

运行两项新测试及所有 CombatBody 伤口、紧急脱离、Trion Gizmo 相关测试。

Expected: 全部 PASS（通过）。

**Step 2: Parse XML**

Run:

```powershell
$files = Get-ChildItem -LiteralPath '.\1.6' -Recurse -Filter '*.xml'
foreach ($file in $files) {
    [xml](Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8) | Out-Null
}
```

Expected: 所有 XML（可扩展标记语言）解析成功。

**Step 3: Build**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Release
```

Expected: 0 errors（错误）；记录 warnings（警告）数量。

**Step 4: Inspect the icon**

查看最终 PNG（便携式网络图形），确认：

- 存在透明通道。
- 四角透明。
- 缩放到 14×14 后仍能识别。
- 没有键控色残边。

**Step 5: Add work log**

在未满 20 条的最新日志文件顶部写入：

- 退出伤口运行时清理顺序修正。
- 紧急脱离三态状态来源与解耦边界。
- 新增测试、贴图和验证结果。

**Step 6: Request code review**

使用 `superpowers:requesting-code-review`（请求代码审查）技能审查本任务精确差异，修正高优先级问题后重新验证。

**Step 7: Commit final verification and log**

只精确暂存本任务日志及必要验证文件，不吸收工作区已有无关改动。
