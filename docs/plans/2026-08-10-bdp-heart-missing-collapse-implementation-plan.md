# BDP 心脏缺失崩解 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在 BDP 战斗体激活态下，当前装备 BDP 触发体的小人一旦缺失原版心脏，立即进入现有崩解流程。

**Architecture:** 复用现有 `Pawn_HealthTracker.CheckForStateChange`（小人健康状态变化检查）Harmony 补丁和 `ICombatBodyCommands.TriggerCollapse`（战斗体崩解命令）。只在 `Active`（激活态）且拥有当前 BDP 触发体时判定；心脏缺失后进入 `Collapsing`（崩解态），不修改原版死亡判定和 Core.dll。

**Tech Stack:** C# 7.3、RimWorld 1.6、Harmony、PowerShell smoke tests（烟雾测试）。

---

### Task 1: Add the failing regression assertion

**Files:**
- Modify: `Source/BDP.Tests/CombatBodyTriggerDropAndDownedCollapseSmokeTests.ps1`

**Step 1: Write the failing test**

Add assertions requiring the Content health-state patch to expose a heart-missing collapse predicate and request `TriggerCollapse("HeartMissing")` after the original state change completes.

**Step 2: Run test to verify it fails**

Run from `模组工程/BorderDefenseProtocol`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/CombatBodyTriggerDropAndDownedCollapseSmokeTests.ps1
```

Expected: `FAIL` because the current patch has no heart-missing predicate or `HeartMissing` collapse reason.

### Task 2: Implement the minimal heart-missing collapse check

**Files:**
- Modify: `Source/BDP.Content/CombatBody/Protection/Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs`

**Step 1: Add the helper predicate**

Add a small utility method that requires Active phase, the current primary BDP TriggerBody, valid health data, and an actual missing `BodyPartDefOf.Heart` record. Do not treat races without a vanilla heart as missing-heart cases.

**Step 2: Request collapse from the existing CheckForStateChange postfix**

After the existing manipulation-loss protection context is exited, call the helper and route through `CombatBodySurfaceAccess.ResolveCommands(pawn)?.TriggerCollapse("HeartMissing")`. The existing session command remains the single owner of phase transition, trigger disabling, pending Hediff, job interruption, and later finalization.

### Task 3: Verify and commit

**Files:**
- Modify: `Source/BDP.Tests/CombatBodyTriggerDropAndDownedCollapseSmokeTests.ps1`
- Modify: `Source/BDP.Content/CombatBody/Protection/Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs`

**Step 1: Run the regression smoke test**

Expected: `CombatBodyTriggerDropAndDownedCollapseSmokeTests PASS`.

**Step 2: Build the Content project**

```powershell
dotnet build Source/BDP.Content/BDP.Content.csproj --configuration Release --no-restore
```

Expected: exit code 0 with no compiler errors.

**Step 3: Commit only this feature**

```powershell
git add Source/BDP.Content/CombatBody/Protection/Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs Source/BDP.Tests/CombatBodyTriggerDropAndDownedCollapseSmokeTests.ps1
git commit -m "feat: collapse combat body when heart is missing"
```
