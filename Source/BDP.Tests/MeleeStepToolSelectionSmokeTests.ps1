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

$surfacePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\MeleeToolSurface.cs'
$contextPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\MeleeAttackExecutionContext.cs'
$selectorPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\VanillaCompatibleMeleeToolSelector.cs'
$bindingStatePath = Join-Path $bdpSourceRoot 'Core\VerbHosting\BdpFormalVerbBindingState.cs'
$hostManagerPath = Join-Path $bdpSourceRoot 'Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$formalHostPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostMelee.cs'
$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'

$surfaceText = if (Test-Path -LiteralPath $surfacePath) { Get-Content -LiteralPath $surfacePath -Raw -Encoding utf8 } else { '' }
$contextText = Get-Content -LiteralPath $contextPath -Raw -Encoding utf8
$selectorText = if (Test-Path -LiteralPath $selectorPath) { Get-Content -LiteralPath $selectorPath -Raw -Encoding utf8 } else { '' }
$bindingStateText = Get-Content -LiteralPath $bindingStatePath -Raw -Encoding utf8
$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding utf8
$formalHostText = Get-Content -LiteralPath $formalHostPath -Raw -Encoding utf8
$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8

Assert-True (
    $surfaceText -match 'class\s+MeleeToolSurface'
) 'BDP melee runtime must introduce a dedicated MeleeToolSurface model.'

Assert-True (
    $bindingStateText -match 'DeclaredMeleeToolSurfaces'
) 'BdpFormalVerbBindingState must retain all candidate melee tool surfaces for step selection.'

Assert-True (
    ($selectorText -match 'class\s+VanillaCompatibleMeleeToolSelector') -and
    ($selectorText -match 'Prepare')
) 'BDP melee runtime must provide a vanilla-compatible selector that prepares a full per-round step tool sequence.'

Assert-True (
    ($contextText -match 'PreparedStepTool') -or
    ($contextText -match 'StepToolSequence')
) 'MeleeAttackExecutionContext must carry the prepared step tool sequence for the current combo round.'

Assert-True (
    $formalHostText -match 'ApplyStepToolSurface'
) 'BdpVerb_FormalHostMelee must expose a step-local surface rebinding method.'

Assert-True (
    $hostManagerText -match 'DeclaredMeleeToolSurfaces'
) 'TriggerBodyVerbHostManager must propagate candidate melee tool surfaces into the formal binding state.'

Assert-True (
    ($meleeJobText -match 'ApplyStepToolSurface') -and
    ($meleeJobText -match 'TryMeleeAttack')
) 'JobDriver_BdpMeleeAttackExecution must rebind the selected step tool surface before launching each melee attack.'

Assert-True (
    ($meleeVerbText -match 'PreparedStepToolIndices') -or
    ($meleeVerbText -match 'preparedStepTool')
) 'BdpVerb_MeleeAttackDamage must persist reconstructable prepared step tool state for save/load recovery.'

Write-Output 'MeleeStepToolSelection PASS'
