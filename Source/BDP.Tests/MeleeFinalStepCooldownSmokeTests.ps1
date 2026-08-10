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

$selectorPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\VanillaCompatibleMeleeToolSelector.cs'
$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$formalHostPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostMelee.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'

$selectorText = Get-Content -LiteralPath $selectorPath -Raw -Encoding utf8
$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$formalHostText = Get-Content -LiteralPath $formalHostPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8

Assert-True (
    $selectorText -match 'AdjustedMeleeSelectionWeight'
) 'Final cooldown ownership must be based on a vanilla-compatible tool selection layer rather than a custom synthetic cooldown rule.'

Assert-True (
    ($meleeJobText -match 'PrepareStepToolSequenceForCurrentRound') -and
    ($meleeJobText -match 'ApplyStepToolSurface\(currentStepIndex\)')
) 'Melee job driver must prepare a round sequence and bind the concrete final-step surface before the last hit starts.'

Assert-True (
    ($formalHostText -match 'verbProps = surface\.VerbProps \?\? bindingVerbProps;') -and
    ($formalHostText -match 'tool = surface\.Tool \?\? bindingTool;')
) 'Formal melee host must switch to the selected step surface so the vanilla cooldown chain reads the actual final-hit tool and verb props.'

Assert-True (
    $meleeVerbText -match 'stepIndex >= stepCount - 1'
) 'Melee damage verb must still treat the last prepared hit as the combo round boundary.'

Write-Output 'MeleeFinalStepCooldown PASS'
