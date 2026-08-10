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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$bridgePath = Join-Path $bdpSourceRoot 'Core\Semantics\BdpDamageSemanticBridge.cs'
$addInjuryPatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_DamageWorker_AddInjury_SourceLabel.cs'
$mergePatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_Hediff_Injury_TryMergeWith_BdpSemantics.cs'

$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$addInjuryPatchText = Get-Content -LiteralPath $addInjuryPatchPath -Raw -Encoding utf8
$mergePatchText = Get-Content -LiteralPath $mergePatchPath -Raw -Encoding utf8

Assert-True (
    $bridgeText -match 'TryApplyInjurySource\s*\(\s*Hediff_Injury\s+injury\s*,\s*ISemanticContext\s+semanticContext\s*,\s*ThingDef\s+fallbackSourceDef\s*=\s*null\s*,\s*string\s+fallbackToolLabel\s*=\s*null\s*,\s*BodyPartGroupDef\s+fallbackBodyPartGroup\s*=\s*null\s*\)'
) 'Damage semantic bridge must accept fallback tool label and body-part-group so semantic host naming does not erase vanilla injury source detail.'

Assert-True (
    $bridgeText -match 'if\s*\(\s*!string\.IsNullOrEmpty\(fallbackToolLabel\)\s*\)'
) 'Damage semantic bridge must branch on whether a vanilla tool label exists.'

Assert-True (
    $bridgeText -match 'injury\.sourceToolLabel\s*=\s*fallbackToolLabel\s*;'
) 'Damage semantic bridge must preserve the vanilla tool label when one exists.'

Assert-True (
    $bridgeText -match 'injury\.sourceBodyPartGroup\s*=\s*fallbackBodyPartGroup\s*;'
) 'Damage semantic bridge must preserve the vanilla body-part-group fallback when no tool label exists.'

Assert-True (
    $addInjuryPatchText -match 'dinfo\.Tool\s*\?\.\s*label'
) 'Fresh injury source patch must pass through DamageInfo.Tool.label so the original melee tool label remains visible in wound brackets.'

Assert-True (
    $addInjuryPatchText -match 'dinfo\.WeaponBodyPartGroup'
) 'Fresh injury source patch must pass through DamageInfo.WeaponBodyPartGroup for tool-less vanilla fallback labeling.'

Assert-True (
    $mergePatchText -match '__instance\.sourceToolLabel'
) 'Merge refresh patch must preserve the existing injury tool label instead of wiping it during semantic source refresh.'

Assert-True (
    $mergePatchText -match '__instance\.sourceBodyPartGroup'
) 'Merge refresh patch must preserve the existing injury body-part-group fallback instead of wiping it during semantic source refresh.'

Write-Output 'MeleeInjurySourceToolLabel PASS'
