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
$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8

Assert-True (
    $meleeJobText -notmatch 'verb\.state == VerbState\.Bursting \|\| pawn\.stances\.FullBodyBusy'
) 'Melee execution must not treat every FullBodyBusy state as a hard combo-step block.'

Assert-True (
    $meleeJobText -match 'Stance_Cooldown'
) 'Melee execution must explicitly inspect cooldown stance ownership.'

Assert-True (
    $meleeJobText -match 'busyStance\.verb == verb'
) 'Melee execution must only consume cooldown stances that belong to the current formal melee verb.'

Assert-True (
    ($meleeJobText -match 'CancelBusyStanceHard\(\)') -or
    ($meleeJobText -match 'SetStance\(new Stance_Mobile\(\)\)')
) 'Melee execution must actively release its own combo-internal cooldown stance before the next prepared step.'

Write-Output 'MeleeComboPacingOwnership PASS'
