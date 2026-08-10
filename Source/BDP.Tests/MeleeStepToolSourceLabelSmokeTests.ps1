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

$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$formalHostPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostMelee.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$injuryPatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_DamageWorker_AddInjury_SourceLabel.cs'

$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$formalHostText = Get-Content -LiteralPath $formalHostPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8
$injuryPatchText = Get-Content -LiteralPath $injuryPatchPath -Raw -Encoding utf8

Assert-True (
    $formalHostText -match 'ApplyStepToolSurface'
) 'Formal melee host must support per-step tool-surface rebinding before each hit.'

Assert-True (
    ($meleeJobText -match 'ApplyStepToolSurface\(currentStepIndex\)') -and
    ($meleeJobText -match 'TryMeleeAttack')
) 'Melee job driver must rebind the selected step tool surface before launching each melee hit.'

Assert-True (
    $meleeVerbText -match 'damageInfo\.SetTool\(tool\);'
) 'Melee damage verb must still stamp DamageInfo.Tool from the currently bound step tool.'

Assert-True (
    $injuryPatchText -match 'dinfo\.Tool\s*\?\.\s*label'
) 'Injury source patch must still read DamageInfo.Tool.label so wound labels follow the selected step tool.'

Write-Output 'MeleeStepToolSourceLabel PASS'
