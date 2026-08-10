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
$bdpCoreRoot = Join-Path $repoRoot 'Source\BDP\Core'

$stagesPath = Join-Path $bdpCoreRoot 'AttackExecution\AttackExecutionService.Stages.cs'
$targetingSourcePath = Join-Path $bdpCoreRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$diagnosticsPath = Join-Path $bdpCoreRoot 'AttackExecution\AttackExecutionDiagnostics.cs'

$stagesText = Read-Source $stagesPath
$targetingSourceText = Read-Source $targetingSourcePath
$diagnosticsText = Read-Source $diagnosticsPath

Assert-True (
    $stagesText -match 'if\s*\(result\.WeaponMode\s*==\s*WeaponExpressionMode\.Melee\)\s*\{\s*return BuildDualMeleeWeaponCasts\(request,\s*result\);'
) 'BuildDualWeaponCasts must dispatch dual melee admission into a dedicated melee branch.'

Assert-True (
    ($stagesText -match 'BuildDualMeleeWeaponCasts') -and
    ($stagesText -match 'BuildDualRangedWeaponCasts') -and
    ($stagesText -match 'FilterDualMeleeSidesByLegality') -and
    ($stagesText -match 'CanExecuteDualMeleeSide') -and
    ($stagesText -match 'FilterDualRangedSidesByLegality') -and
    ($stagesText -match 'CanExecuteDualRangedSide')
) 'AttackExecutionService.Stages must keep melee and ranged dual legality as parallel branches.'

Assert-True (
    ($targetingSourceText -match 'EvaluateDualWeaponMeleeSideTargetLegality') -and
    ($targetingSourceText -match 'EvaluateDualWeaponRangedSideTargetLegality')
) 'AttackExecutionTargetingSource must split dual side legality into melee and ranged helpers.'

Assert-True (
    $targetingSourceText -match 'if\s*\(sourceResult\.WeaponMode\s*==\s*WeaponExpressionMode\.Melee\)\s*\{\s*return EvaluateDualWeaponMeleeSideTargetLegality'
) 'Manual dual targeting legality must route melee sides through the dedicated melee helper.'

Assert-True (
    ($targetingSourceText -match 'target\.HasThing') -and
    ($targetingSourceText -match 'ValidateTarget\(target,\s*false\)')
) 'Dual melee targeting legality must rely on thing-target and neutral validation instead of current-position hit checks.'

Assert-True (
    ($diagnosticsText -match 'LogDualMeleePlanStart') -and
    ($diagnosticsText -match 'LogDualMeleeSideLegality') -and
    ($diagnosticsText -match 'LogDualMeleePlanResult') -and
    ($diagnosticsText -match 'event=dual_melee_plan_start') -and
    ($diagnosticsText -match 'event=dual_melee_side_legality') -and
    ($diagnosticsText -match 'event=dual_melee_plan_result')
) 'AttackExecutionDiagnostics must give dual melee its own diagnostic event family after the split.'

Write-Output 'DualWeaponModeSplitLegalitySmokeTests PASS'
