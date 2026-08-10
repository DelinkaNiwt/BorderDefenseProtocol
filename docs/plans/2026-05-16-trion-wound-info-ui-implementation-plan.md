# Trion Wound Info UI Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不新增健康面板 UI 补丁的前提下，让战斗体伤口显示 Trion 流失信息，并让 Trion Gizmo 悬停详情只显示流失来源分项。

**Architecture:** Trion 账本仍是唯一流失真值来源，伤口系统只发布和查询伤口对应的流失值。Gizmo 通过只读接口读取账本快照并做展示聚合；伤口提示优先使用 RimWorld 原生 HediffComp 提示扩展，不改 HealthCard 展示层。

**Tech Stack:** RimWorld 1.6、Verse HediffComp、C#、PowerShell smoke tests。

---

### Task 1: 把 Trion 流失快照暴露到只读面

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/ITrionReader.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/ITrionCommands.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/TrionService.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/CompTrion.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionDrainSnapshotReaderSmokeTests.ps1`

**Step 1: Write the failing test**

Create `TrionDrainSnapshotReaderSmokeTests.ps1` to assert:
- `ITrionReader` contains `GetDrainSnapshot()`.
- `ITrionCommands` no longer owns `GetDrainSnapshot()`.
- `TrionService` still exposes a public `GetDrainSnapshot()` implementation.
- `CompTrion.GetDrainSnapshot()` returns a copied dictionary, not the live registry.

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionDrainSnapshotReaderSmokeTests.ps1"
```

Expected: FAIL because the reader does not yet expose the snapshot and the command interface still does.

**Step 3: Write minimal implementation**

- Add `using System.Collections.Generic;` to `ITrionReader.cs`.
- Add `IReadOnlyDictionary<TrionDrainKey, float> GetDrainSnapshot();` to `ITrionReader`.
- Remove `GetDrainSnapshot()` from `ITrionCommands`.
- Keep `TrionService.GetDrainSnapshot()` as the shared implementation for the reader surface.
- Change `CompTrion.GetDrainSnapshot()` to return `new Dictionary<TrionDrainKey, float>(drainRegistry)` after `EnsureInternalState()`.
- Update comments so they say the drain value is `Trion/秒`, not `每天消耗量`.

**Step 4: Run test to verify it passes**

Run the same smoke test. Expected: PASS.

---

### Task 2: 让 Trion Gizmo 悬停提示只显示流失详情

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoDrainDetailsSmokeTests.ps1`

**Step 1: Write the failing test**

Create `TrionGizmoDrainDetailsSmokeTests.ps1` to assert:
- `BuildTooltip()` no longer contains `可用:`、`当前:`、`总量:`、`锁定:`、`预测锁定:`、`恢复:`、`恢复冻结:` 这些资源总览行。
- `BuildTooltip()` reads `reader.GetDrainSnapshot()`.
- Tooltip has an empty-state text such as `当前没有持续流失。`.
- Tooltip groups entries by `Domain/Channel` instead of printing every raw key directly.

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoDrainDetailsSmokeTests.ps1"
```

Expected: FAIL because current tooltip repeats the resource overview.

**Step 3: Write minimal implementation**

- Add a private grouping method in `Gizmo_TrionStatus` that aggregates `reader.GetDrainSnapshot()` by `TrionDrainKey.Domain + "/" + TrionDrainKey.Channel`.
- Ignore entries whose value is `<= 0f`.
- Tooltip format:

```text
Trion流失详情
CombatBody/Wound：12.0/秒
Hediff/SomeDef：1.0/秒
```

- If there are no positive entries, return:

```text
Trion流失详情
当前没有持续流失。
```

**Step 4: Run test to verify it passes**

Run the same smoke test. Expected: PASS.

---

### Task 3: 抽出伤口 Trion 流失只读查询

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Wounds/CombatBodyWoundTrionBinding.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Wounds/CombatBodyWoundTrionDrainUtility.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionDrainUtilitySmokeTests.ps1`

**Step 1: Write the failing test**

Create `CombatBodyWoundTrionDrainUtilitySmokeTests.ps1` to assert:
- A new `CombatBodyWoundTrionDrainUtility` exists.
- It exposes an internal static query method for current wound drain.
- `CombatBodyWoundTrionBinding` no longer owns a private duplicate calculation method.

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionDrainUtilitySmokeTests.ps1"
```

Expected: FAIL because the utility does not exist yet.

**Step 3: Write minimal implementation**

- Create `CombatBodyWoundTrionDrainUtility`.
- Move the existing policy checks and per-second calculation into:

```csharp
internal static bool TryResolveDrainPerSecond(Hediff hediff, out float drainPerSecond)
```

- Keep current trial math unchanged:
  - `Severity`: `hediff.Severity * trionDrainPerSeverityPerSecond`
  - `RawBleedRate`: `rawBleedRate * trionDrainPerRawBleedRatePerSecond`
- Keep `MissingPart` handling exactly aligned with current policy.
- Update `CombatBodyWoundTrionBinding.UpdateWoundDrain()` to call the utility.

**Step 4: Run test to verify it passes**

Run the same smoke test. Expected: PASS.

---

### Task 4: 用 HediffComp 显示伤口 Trion 流失提示

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Wounds/CombatBodyWoundTrionInfoHediffComp.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Wounds/CombatBodyWoundTrionInfoHediffCompProperties.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBody/Wounds/CombatBodyWoundTrionInfoInjector.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Bootstrap/CombatBodyWoundInfoBootstrap.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionInfoSmokeTests.ps1`

**Step 1: Write the failing test**

Create `CombatBodyWoundTrionInfoSmokeTests.ps1` to assert:
- The comp overrides `CompTipStringExtra`.
- The comp uses `CombatBodyWoundTrionDrainUtility.TryResolveDrainPerSecond()`.
- The injector targets `Hediff_Injury` assignable defs first.
- The injector has a duplicate guard and does not require a HealthCard patch.

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionInfoSmokeTests.ps1"
```

Expected: FAIL because the display comp does not exist yet.

**Step 3: Write minimal implementation**

- `CombatBodyWoundTrionInfoHediffComp`:
  - Inherits `HediffComp`.
  - Overrides `CompTipStringExtra`.
  - Returns `null` when drain is `<= 0f`.
  - Returns `Trion流失：x.x/秒` when drain is positive.
- `CombatBodyWoundTrionInfoHediffCompProperties`:
  - Inherits `HediffCompProperties`.
  - Sets `compClass`.
- `CombatBodyWoundTrionInfoInjector`:
  - Iterates `DefDatabase<HediffDef>.AllDefsListForReading`.
  - Only targets defs whose `hediffClass` is assignable to `Hediff_Injury`.
  - Ensures `def.comps` exists.
  - Skips defs already containing `CombatBodyWoundTrionInfoHediffCompProperties`.
- `CombatBodyWoundInfoBootstrap`:
  - Uses `[StaticConstructorOnStartup]`.
  - Calls the injector once.

**Step 4: Run test to verify it passes**

Run the same smoke test. Expected: PASS.

---

### Task 5: Verify, build, log, and commit

**Files:**
- Modify: `日志/Agent工作日志/Agent日志04.md`
- Commit only the files changed by this plan.

**Step 1: Run focused smoke tests**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionDrainSnapshotReaderSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoDrainDetailsSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionDrainUtilitySmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionInfoSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundTrionDrainSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyWoundSeverityTrionDrainSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGeneGizmoSmokeTests.ps1"
```

Expected: all PASS.

**Step 2: Build**

Run:

```powershell
dotnet build "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj" -c Release
```

Expected: build succeeds.

**Step 3: Write work log**

Add one newest entry to `日志/Agent工作日志/Agent日志04.md` describing:
- Reader-side drain snapshot.
- Gizmo drain-only tooltip.
- Wound HediffComp display hook.
- Tests/build result.

**Step 4: Commit**

Stage only files changed by this plan, excluding build DLL/PDB and unrelated dirty files.

Run:

```powershell
git add -- "模组工程/BorderDefenseProtocol/docs/plans/2026-05-16-trion-wound-info-ui-implementation-plan.md" ...
git commit -m "feat: show Trion drain details"
```

Expected: commit succeeds with only planned source/test/doc/log files.
