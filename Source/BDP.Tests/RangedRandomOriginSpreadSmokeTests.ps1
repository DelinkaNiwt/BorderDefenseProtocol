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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'

$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$emitPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionEmit.cs'
$planPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'
$devHarnessDefsRoot = Join-Path $devHarnessRoot '1.6\Defs'

$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$emitText = Get-Content -LiteralPath $emitPath -Raw -Encoding utf8
$planText = Get-Content -LiteralPath $planPath -Raw -Encoding utf8
$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$devHarnessDefsText = Get-ChildItem -LiteralPath $devHarnessDefsRoot -Filter '*.xml' -Recurse |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 } |
    Out-String

Assert-True (
    ($verbText -match 'ResolveRandomOriginSpreadOffset') -and
    ($verbText -match 'Rand\.Range')
) 'BdpVerb_Shoot must resolve launch-origin spread by random range sampling.'

Assert-True (
    ($verbText -notmatch 'ResolveCenteredSequenceRatio') -and
    ($verbText -notmatch 'OriginSpreadSequenceIndex') -and
    ($verbText -notmatch 'OriginSpreadSequenceCount') -and
    ($emitText -notmatch 'OriginSpreadSequenceIndex') -and
    ($planText -notmatch 'OriginSpreadSequenceIndex')
) 'Launch-origin spread must not use fixed sequence fields or centered sequence ratios.'

Assert-True (
    ($devHarnessDefsText -notmatch '<SpreadRadius>') -and
    ($devHarnessDefsText -notmatch '<ForwardMin>-') -and
    ($chipDefsText -match '<OriginSpread>') -and
    ($chipDefsText -match '<LateralMin>-0\.3</LateralMin>') -and
    ($chipDefsText -match '<LateralMax>0\.3</LateralMax>') -and
    ($chipDefsText -match '<ForwardMin>0</ForwardMin>') -and
    ($chipDefsText -match '<ForwardMax>0\.105</ForwardMax>')
) 'DevHarness volley chips must declare launch-origin spread as a random range, not SpreadRadius.'

Write-Output 'RangedRandomOriginSpreadSmokeTests PASS'
