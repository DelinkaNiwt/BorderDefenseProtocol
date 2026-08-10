# DevHarness Dynamic Test Chip Menu Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the hard-coded DevHarness test chip menu with dynamic discovery of test-mod chip defs, filtered by chip loadout side policy.

**Architecture:** Keep the change entirely inside `BorderDefenseProtocol.DevHarness`. Discover eligible `ThingDef` instances from the loaded def database using `modContentPack.PackageId == Niwt.BDP.DevHarness`, require a `ChipDefinitionConfig` mod extension, then filter the results by `ChipLoadoutSidePolicy` before building menu options.

**Tech Stack:** RimWorld `DefDatabase`, DevHarness UI code, PowerShell smoke tests, C# build.

---

### Task 1: Lock the dynamic menu contract

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessDynamicChipMenuSmokeTests.ps1`

**Step 1: Write the failing test**

- Assert `Window_LegacyTriggerSlots.cs` no longer contains the old hard-coded `yield return new TestChipMenuEntry(...)` list.
- Assert the file filters by `modContentPack.PackageId` and `ChipDefinitionConfig`.
- Assert the file filters by `ChipLoadoutSidePolicy.HandsOnly` and `ChipLoadoutSidePolicy.SpecialOnly`.

**Step 2: Run test to verify it fails**

- Run the new PowerShell smoke test and confirm it fails on the current hard-coded implementation.

**Step 3: Write minimal implementation**

- Replace the static menu list with dynamic discovery helpers in `Window_LegacyTriggerSlots.cs`.

**Step 4: Run test to verify it passes**

- Re-run the new smoke test.

### Task 2: Preserve menu loading behavior

**Files:**
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/Window_LegacyTriggerSlots.cs`

**Step 1: Keep menu item payloads simple**

- Continue passing a label plus `ThingDef.defName` into `LoadTestChip(...)`.

**Step 2: Sort for stable UX**

- Order discovered entries by label so the menu remains predictable.

**Step 3: Keep unsupported defs out**

- Skip defs without a chip contract or without matching loadout side policy.

### Task 3: Verify integration

**Files:**
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessDynamicChipMenuSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessAbilityHediffBusinessSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj`

**Step 1: Run focused smoke tests**

- Run the new dynamic menu smoke test and the existing DevHarness-focused smoke tests.

**Step 2: Build DevHarness**

- Run `dotnet msbuild BDP.DevHarness.csproj -p:Configuration=Debug -t:Build -v:minimal`.

**Step 3: Confirm no main-mod changes were required**

- Review the diff and ensure runtime code changes stay inside DevHarness plus test files/docs.
