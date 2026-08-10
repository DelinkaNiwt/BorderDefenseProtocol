$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$formalText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
) -Raw -Encoding utf8
$factoryText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
) -Raw -Encoding utf8
$manualText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
) -Raw -Encoding utf8
$abilityCompPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\CompAbilityEffect_BdpExpressionUseRequirements.cs'
$attackEntryText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultAttackExecutionEntry.cs'
) -Raw -Encoding utf8
$monitorPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Requirements\ComboUseRequirementMonitor.cs'
$coordinatorText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
) -Raw -Encoding utf8
$automaticPaths = @(
    'Source\BDP\Core\Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs',
    'Source\BDP\Core\Expressions\Projection\DefaultExpressionHostSynchronizer.cs',
    'Source\BDP\Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs',
    'Source\BDP\Core\Expressions\Projection\DefaultPrimaryExpressionSelector.cs',
    'Source\BDP\Core\Expressions\Runtime\ExpressionSustainDrainService.cs'
)
$automaticText = ($automaticPaths | ForEach-Object {
    Get-Content -LiteralPath (Join-Path $repoRoot $_) -Raw -Encoding utf8
}) -join "`n"

Assert-True (
    $formalText -match 'PawnRequirementCheckResult\s+UseRequirementCheck'
) 'Formal Combo results must carry a separate use-requirement check result.'
Assert-True (
    ($factoryText -match 'UseRequirementCheck') -and
    ($factoryText -match 'IsAvailable\s*=\s*true')
) 'An unmet Combo use requirement must not remove the formal result.'
Assert-True (
    ($manualText -match 'ComboUseRequirementService') -and
    ($manualText -match 'disabledReason')
) 'Manual Combo buttons must remain present and expose a disabled reason.'
Assert-True (
    Test-Path -LiteralPath $abilityCompPath
) 'Combo Ability buttons must have a dedicated expression-use requirement adapter.'
$abilityCompText = Get-Content -LiteralPath $abilityCompPath -Raw -Encoding utf8
Assert-True (
    ($abilityCompText -match 'GizmoDisabled') -and
    ($abilityCompText -match 'Valid\s*\(') -and
    ($attackEntryText -match 'ComboUseRequirementService') -and
    ($attackEntryText -match 'result\.ComboDefName')
) 'Ability and AttackExecution must both perform a final shared Combo requirement check.'
Assert-True (
    (Test-Path -LiteralPath $monitorPath) -and
    ($coordinatorText -match 'ComboUseRequirementMonitor') -and
    ($coordinatorText -match 'MarkDirty')
) 'A 60-tick monitor must refresh the existing projection only when Combo satisfaction changes.'
Assert-True (
    $automaticText -match 'UseRequirementCheck'
) 'Automatic expression consumers must reject a blocked Combo result.'

$formalAbilityPath = Join-Path $repoRoot '1.6\Content\Defs\Abilities\SenkuKogetsu\AbilityDefs_SenkuKogetsu.xml'
$formalAbilityText = Get-Content -LiteralPath $formalAbilityPath -Raw -Encoding utf8
Assert-True (
    $formalAbilityText -match 'CompProperties_AbilityEffect_BdpExpressionUseRequirements'
) 'The formal Combo Ability host must register the Core use-requirement adapter.'

Write-Output 'ComboUseRequirementRuntimeSmokeTests PASS'
