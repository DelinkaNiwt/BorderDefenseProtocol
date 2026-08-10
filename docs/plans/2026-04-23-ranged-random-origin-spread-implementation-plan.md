# Ranged Random Origin Spread Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace fixed sequence-based launch-origin spread with per-projectile random range spread.

**Architecture:** Move spread semantics from `SpreadRadius + sequence index` to an explicit local-axis random range. Carry that range through expression, execution, fire, and projectile-init plans, then sample with `Rand.Range` only at real projectile emission time.

**Tech Stack:** C#, RimWorld/Verse, XML Defs, PowerShell smoke tests, `dotnet msbuild`.

---

### Task 1: Add Random Spread Data Model

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ChipAttackExecutionConfig.cs`
- Modify: `Source/BDP/Core/CombatModel/SingleAttackExecutionStyle.cs`

**Step 1: Replace author config field**

In `ChipAttackExecutionConfig`, remove:

```csharp
public float SpreadRadius = 0f;
```

Add a nested or adjacent config class:

```csharp
public ChipAttackOriginSpreadConfig OriginSpread;

public sealed class ChipAttackOriginSpreadConfig
{
    public float LateralMin = 0f;
    public float LateralMax = 0f;
    public float ForwardMin = 0f;
    public float ForwardMax = 0f;
}
```

**Step 2: Replace runtime execution style field**

In `SingleAttackExecutionStyle`, remove:

```csharp
public float volleySpreadRadius;
```

Add:

```csharp
public bool HasOriginSpreadRange;
public float OriginSpreadLateralMin;
public float OriginSpreadLateralMax;
public float OriginSpreadForwardMin;
public float OriginSpreadForwardMax;
```

Update `Clone()` to copy those fields.

**Step 3: Remove unused fixed spread provider shape**

Remove `IAttackOriginSpreadProvider` from `SingleAttackExecutionStyle` if no consumer remains.

Delete `TryResolveOriginOffset(...)` if it remains unused after this task.

**Expected result:** The public author config and internal execution style no longer describe spread as one fixed radius.

---

### Task 2: Translate New XML Config

**Files:**
- Modify: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`

**Step 1: Map new config**

Replace:

```csharp
volleySpreadRadius = execution.SpreadRadius
```

With logic that copies `execution.OriginSpread` into `SingleAttackExecutionStyle`.

Recommended helper:

```csharp
private static void ApplyOriginSpreadRange(
    SingleAttackExecutionStyle target,
    ChipAttackOriginSpreadConfig source)
{
    if (target == null || source == null)
    {
        return;
    }

    target.HasOriginSpreadRange = source.LateralMin != 0f
        || source.LateralMax != 0f
        || source.ForwardMin != 0f
        || source.ForwardMax != 0f;
    target.OriginSpreadLateralMin = Mathf.Min(source.LateralMin, source.LateralMax);
    target.OriginSpreadLateralMax = Mathf.Max(source.LateralMin, source.LateralMax);
    target.OriginSpreadForwardMin = Mathf.Min(source.ForwardMin, source.ForwardMax);
    target.OriginSpreadForwardMax = Mathf.Max(source.ForwardMin, source.ForwardMax);
}
```

**Expected result:** XML `OriginSpread` becomes formal execution style data.

---

### Task 3: Replace Emit Spread Payload

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionEmit.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/FireEmitRecord.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/ProjectileInitPlan.cs`

**Step 1: Remove fixed sequence fields**

Remove from all three payload types:

```csharp
OriginSpreadRadius
OriginSpreadSequenceIndex
OriginSpreadSequenceCount
```

**Step 2: Add random range fields**

Add equivalent fields:

```csharp
public bool HasOriginSpreadRange { get; set; }
public float OriginSpreadLateralMin { get; set; }
public float OriginSpreadLateralMax { get; set; }
public float OriginSpreadForwardMin { get; set; }
public float OriginSpreadForwardMax { get; set; }
```

For `ProjectileInitPlan.ExposeData`, persist these new fields with new save keys:

```csharp
Scribe_Values.Look(ref hasOriginSpreadRange, "hasOriginSpreadRange", false);
Scribe_Values.Look(ref originSpreadLateralMin, "originSpreadLateralMin", 0f);
Scribe_Values.Look(ref originSpreadLateralMax, "originSpreadLateralMax", 0f);
Scribe_Values.Look(ref originSpreadForwardMin, "originSpreadForwardMin", 0f);
Scribe_Values.Look(ref originSpreadForwardMax, "originSpreadForwardMax", 0f);
```

Do not preserve old save keys unless later explicitly requested.

**Expected result:** Runtime payloads no longer carry sequence spread state.

---

### Task 4: Propagate Random Range Through Execution

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Fire/FireStageService.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`

**Step 1: Update `BuildSingleEmit` signature**

Remove:

```csharp
float originSpreadRadius,
int originSpreadSequenceIndex,
int originSpreadSequenceCount
```

Add:

```csharp
bool hasOriginSpreadRange,
float originSpreadLateralMin,
float originSpreadLateralMax,
float originSpreadForwardMin,
float originSpreadForwardMax
```

**Step 2: Pass the same random range to every emitted projectile**

When `single.HasOriginSpreadRange` is true, copy the range into each emit.

Do this for both:

- `Simultaneous` ranged rhythm.
- Non-simultaneous repeated shots.

**Step 3: Update `FireStageService` and `ProjectileInitStageService`**

Copy the new range fields from baseline emit -> fire emit -> projectile init plan.

**Expected result:** Every projectile plan knows the configured random range, but no projectile has a fixed sequence position.

---

### Task 5: Randomize At Real Emission Time

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: Rename spread resolver**

Replace:

```csharp
ResolveDeclaredOriginSpreadOffset(...)
ResolveCenteredSequenceRatio(...)
```

With:

```csharp
ResolveRandomOriginSpreadOffset(...)
```

**Step 2: Implement random local-axis sampling**

Use:

```csharp
float lateralOffset = Rand.Range(plan.OriginSpreadLateralMin, plan.OriginSpreadLateralMax);
float forwardOffset = Rand.Range(plan.OriginSpreadForwardMin, plan.OriginSpreadForwardMax);
return rightDir * lateralOffset + shootDir * forwardOffset;
```

Keep source/target direction calculation near the current implementation.

Return `Vector3.zero` if:

- `plan == null`
- `!plan.HasOriginSpreadRange`
- target invalid
- shoot direction invalid

**Expected result:** The true launch point is randomized independently per projectile.

---

### Task 6: Update Test XML Samples

**Files:**
- Modify: `BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml`
- Modify: `Source/BDP.Tests/DevHarnessPathLatchVolleyChipSmokeTests.ps1`

**Step 1: Replace old spread XML**

Replace:

```xml
<SpreadRadius>0.3</SpreadRadius>
```

With:

```xml
<OriginSpread>
  <LateralMin>-0.3</LateralMin>
  <LateralMax>0.3</LateralMax>
  <ForwardMin>0</ForwardMin>
  <ForwardMax>0.105</ForwardMax>
</OriginSpread>
```

Apply to:

- `BDP_TestChipRangedVolley`
- `BDP_TestChipPathLatchVolley`

**Step 2: Update smoke assertions**

Change assertions from `<SpreadRadius>` matching to `OriginSpread` range matching.

**Expected result:** Test chips declare random ranges, not fixed radius.

---

### Task 7: Add Regression Smoke Test

**Files:**
- Create: `Source/BDP.Tests/RangedRandomOriginSpreadSmokeTests.ps1`

**Step 1: Assert random resolver exists**

Check `BdpVerb_Shoot.cs` contains:

```powershell
'ResolveRandomOriginSpreadOffset'
'Rand.Range'
```

**Step 2: Assert fixed sequence resolver is gone**

Check `BdpVerb_Shoot.cs` does not contain:

```powershell
'ResolveCenteredSequenceRatio'
'OriginSpreadSequenceIndex'
'OriginSpreadSequenceCount'
```

**Step 3: Assert XML no longer uses old spread**

Check test chip XML does not contain:

```powershell
'<SpreadRadius>'
```

And does contain:

```powershell
'<OriginSpread>'
'<LateralMin>-0.3</LateralMin>'
'<LateralMax>0.3</LateralMax>'
```

**Expected result:** Future fixed-sequence spread regressions fail fast.

---

### Task 8: Verify Build And Tests

**Files:**
- No source edits.

**Step 1: Run focused smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedRandomOriginSpreadSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessPathLatchVolleyChipSmokeTests.ps1'
```

Expected: both PASS.

**Step 2: Build main mod**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj'
```

Expected: Build succeeded.

**Step 3: Build dev harness**

Run:

```powershell
dotnet msbuild '.\Source\BDP.DevHarness\BDP.DevHarness.csproj'
```

Expected: Build succeeded.

**Step 4: Manual in-game check**

Use `BDP_TestChipPathLatchVolley`.

Expected:

- 紫点仍围绕副手真实发射点中心。
- 同一目标连续齐射时，紫点不会每轮固定落在同一排位置。
- 每发紫点都落在配置区间内。

---

## Notes

- This plan intentionally removes fixed sequence spread semantics.
- Do not run `git commit` unless the user explicitly asks.
- If existing comments are garbled, repair touched comments while editing the same block.
