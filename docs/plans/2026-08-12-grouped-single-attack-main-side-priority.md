# Grouped Single Attack Main-Side Priority Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make a grouped single-weapon attack submit only one attack per pawn, preferring the legal main-side source and falling back to the legal sub-side source.

**Architecture:** Keep the existing grouped targeting adapter, but replace its blind per-source fan-out with a per-pawn source selection helper. Side identity is read from the formal result rather than inferred from list order.

**Tech Stack:** C#, RimWorld/Verse targeting and job APIs, PowerShell smoke tests.

---

### Task 1: Regression test

**Files:**
- Create: `Source/BDP.Tests/GroupedSingleAttackMainSidePrioritySmokeTests.ps1`

1. Assert that grouped dispatch selects one source per pawn.
2. Assert that Main is preferred when legal and Sub is the fallback.
3. Run the test and confirm it fails because the current code blindly fans out every source.

### Task 2: Minimal grouped dispatch correction

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/GroupedAttackExecutionTargetingSource.cs`

1. Expose the formal result origin as a read-only internal property on the member targeting source.
2. Select one legal source for each pawn, preferring Main over Sub.
3. Submit only the selected source.
4. Keep group-wide validation and preview behavior unchanged.

### Task 3: Verification and version control

1. Run the new regression test.
2. Run related grouped targeting and dual ranged legality smoke tests.
3. Build `BDP.Core` and confirm zero errors.
4. Write the required reverse-chronological work log.
5. Commit only task files.

