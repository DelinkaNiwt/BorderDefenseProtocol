# DevHarness Ability/Hediff Business Samples Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add concrete DevHarness-only business content for the placeholder `Ability` and `Hediff` test chips without touching the main BDP module.

**Architecture:** Keep all runtime behavior in `BorderDefenseProtocol.DevHarness`. Implement the `Hediff` purely through XML severity stages and implement the `Ability` with RimWorld's built-in smokepop ability comp so the main BDP expression pipeline only continues pointing at concrete defs.

**Tech Stack:** RimWorld XML defs, DevHarness mod content, PowerShell smoke tests.

---

### Task 1: Lock the expected DevHarness-only behavior

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessAbilityHediffBusinessSmokeTests.ps1`

**Step 1: Write the failing test**

- Assert DevHarness defines `BDP_TestAbility_ExpressionOnly` as a smokepop ability.
- Assert DevHarness defines `BDP_TestHediff_ExpressionOnly` with MoveSpeed x2 at base stage and x5 at severity 2.

**Step 2: Run test to verify it fails**

- Run the new PowerShell smoke test before any XML defs exist.

**Step 3: Write minimal implementation**

- Add one `AbilityDef` XML file under DevHarness.
- Add one `HediffDef` XML file under DevHarness.

**Step 4: Run test to verify it passes**

- Run the new smoke test again.

### Task 2: Keep existing placeholder chips pointing at the new concrete defs

**Files:**
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Trigger/ThingDefs_BDP_TestChips.xml`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`

**Step 1: Verify current links**

- Ensure the existing chips still point to `BDP_TestAbility_ExpressionOnly` and `BDP_TestHediff_ExpressionOnly`.

**Step 2: Adjust only if needed**

- Update labels/descriptions only if they now misdescribe the business meaning.

**Step 3: Run compatibility smoke test**

- Re-run existing DevHarness chip smoke coverage.

### Task 3: Validate DevHarness build surface

**Files:**
- Test: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj`

**Step 1: Build only if no new C# is needed**

- If implementation stays XML-only, a build is optional but harmless.

**Step 2: Run focused validation**

- Run the new smoke test and the existing `DevHarnessChipTrionConfigSmokeTests.ps1`.
- Build `BDP.DevHarness.csproj` if any source files change.
