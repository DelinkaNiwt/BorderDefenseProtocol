$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$targetingSourcePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$groupedTargetingSourcePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\GroupedAttackExecutionTargetingSource.cs'
$diagnosticsPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionDiagnostics.cs'
$targetingSourceText = Read-Source $targetingSourcePath
$groupedTargetingSourceText = Read-Source $groupedTargetingSourcePath
$diagnosticsText = Read-Source $diagnosticsPath

Assert-True (
    $targetingSourceText -match 'TryEvaluateDualWeaponTargetLegality'
) 'AttackExecutionTargetingSource must add a dedicated dual manual-target legality gate.'

Assert-True (
    ($targetingSourceText -match 'context\.Result\.CompositeKind\s*==\s*CompositeExpressionKind\.DualWeapon') -or
    ($targetingSourceText -match 'context\.Result\.CompositeKind\s*!=\s*CompositeExpressionKind\.DualWeapon')
) 'Manual targeting legality must branch explicitly for dual composite results.'

Assert-True (
    $targetingSourceText -match 'allowMain \|\| allowSub'
) 'Manual dual targeting legality must allow confirmation when at least one side remains legal.'

Assert-True (
    $targetingSourceText -match 'TryGetDualWeaponCompositeReference'
) 'Manual dual targeting legality must resolve per-side source results from the composite reference.'

Assert-True (
    $targetingSourceText -notmatch 'if\s*\(resolvedSpec\s*!=\s*null\s*&&\s*resolvedSpec\.RequiresDirectTargetLineOfSight\)\s*\{\s*return sourceVerb\.CanHitTarget\(target\);\s*\}\s*return useValidateTarget\s*\?\s*sourceVerb\.ValidateTarget\(target,\s*false\)\s*:\s*sourceVerb\.CanHitTarget\(target\);'
) 'Non-necessary dual sides must not be screened by sourceVerb CanHitTarget/ValidateTarget at the dual adapter layer.'

Assert-True (
    $groupedTargetingSourceText -notmatch 'public void OrderForceTarget\(LocalTargetInfo target\)[\s\S]*source\.ValidateTarget\(target,\s*false\)'
) 'Grouped manual targeting must not pre-screen members with ValidateTarget before each member can enter its own confirmation chain.'

Assert-True (
    ($targetingSourceText -match 'LogManualDualTargetingSideLegality') -and
    ($diagnosticsText -match 'LogManualDualTargetingSideLegality')
) 'Manual dual targeting side legality must be visible in attack-execution diagnostics.'

Write-Output 'DualRangedManualTargetingLegalitySmokeTests PASS'
