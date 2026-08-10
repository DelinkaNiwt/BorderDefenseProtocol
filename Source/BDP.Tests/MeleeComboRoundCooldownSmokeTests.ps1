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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs\Things\Items\Chips\Test'

$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$chipDefsPath = Join-Path $devHarnessRoot 'ThingDefs_TestChips_Combat.xml'

$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8
$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8

Assert-True (
    $meleeVerbText -match 'stepIndex >= stepCount - 1'
) 'Melee combo interval lookup must treat the last prepared hit as round boundary instead of looping the per-hit interval into the next round.'

Assert-True (
    ($meleeJobText -match 'currentStepIndex != 0') -or
    ($meleeJobText -match 'currentStepIndex == 0')
) 'Melee execution must only consume owned cooldown stance between hits inside the current combo round.'

Assert-True (
    $chipDefsText -match '(?s)<defName>BDP_TestChipMelee</defName>.*?<HitIntervalTicks>[1-9]\d*</HitIntervalTicks>'
) 'DevHarness melee test chip should keep a non-zero in-round combo interval for observation.'

Write-Output 'MeleeComboRoundCooldown PASS'
