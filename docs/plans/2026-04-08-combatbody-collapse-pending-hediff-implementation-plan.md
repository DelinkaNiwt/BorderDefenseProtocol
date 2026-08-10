# CombatBody Collapse Pending Hediff Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a temporary collapse-pending hediff that is shown only during the 90-tick `Collapsing` phase, displays live countdown plus direct collapse reason in tooltip, and is removed when collapse exit cleanup starts.

**Architecture:** Keep collapse truth in `CombatBody` and make the hediff a pure display shell. The hediff reads `ICombatBodyReader.GetCollapseRemaining()` and `CollapseReason` at draw time, while attach/remove is driven only by the CombatBody collapse entry and exit transaction so there is still only one owner of collapse state.

**Tech Stack:** C# 7.3, RimWorld 1.6 Verse/RimWorld API, XML HediffDef, PowerShell smoke tests, `dotnet msbuild`.

---

### Task 1: Add failing smoke checks

**Files:**
- Modify: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`

**Step 1: Write the failing test**

- Assert a dedicated collapse-pending hediff def exists.
- Assert `TriggerCollapse(string reason)` attaches the collapse-pending hediff after entering `Collapsing`.
- Assert the exit transaction removes the collapse-pending hediff during collapse cleanup.
- Assert a dedicated hediff class exposes dynamic label/tip overrides that read collapse remaining and collapse reason.

**Step 2: Run test to verify it fails**

Run: `& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'`
Expected: FAIL on missing collapse-pending hediff behavior.

### Task 2: Implement the display hediff

**Files:**
- Create: `Source/BDP/Core/Hediffs/Hediff_BdpCombatBodyCollapsePending.cs`
- Modify: `1.6/Defs/Health/CombatBody/HediffDefs_CombatBody.xml`

**Step 1: Write minimal implementation**

- Add a new `HediffWithComps` subclass for the temporary collapse display.
- Override label/tip accessors to show:
  - fixed title `战斗体破裂中`
  - live remaining ticks formatted as seconds
  - direct collapse reason in tooltip
- Add a dedicated HediffDef for the collapse-pending display.

**Step 2: Run focused verification**

Run: `& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'`
Expected: still FAIL, but only on attach/remove hooks.

### Task 3: Attach on collapse and remove on exit

**Files:**
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`

**Step 1: Write minimal implementation**

- When `TriggerCollapse(string reason)` successfully enters `Collapsing`, add the collapse-pending hediff.
- When exit cleanup starts, remove the collapse-pending hediff before later collapse exit steps continue.

**Step 2: Run verification**

Run:
- `& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'`
- `dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal`

Expected: PASS.
