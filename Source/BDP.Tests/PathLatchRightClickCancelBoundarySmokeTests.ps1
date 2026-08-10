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
$devHarnessSourceRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$pathLatchPath = Join-Path $devHarnessSourceRoot 'RangedModules\Samples\PathLatchModule.cs'
$pathLatchText = Read-Source $pathLatchPath

Assert-True (
    $pathLatchText -notmatch 'Event\s+currentEvent\s*=\s*Event\.current'
) 'PathLatch right-click cancel must not read raw Event.current in Preview.'

Assert-True (
    $pathLatchText -notmatch 'Find\.Targeter\?\.StopTargeting\('
) 'PathLatch right-click cancel must not stop Targeter directly.'

Assert-True (
    $pathLatchText -notmatch 'record\.InteractionSession\.Cancel\('
) 'PathLatch right-click cancel must not cancel interaction session directly.'

Assert-True (
    $pathLatchText -match 'PressedButton\s*==\s*TargetingInputButton\.Right'
) 'PathLatch must consume right-click through the neutral input frame.'

Assert-True (
    $pathLatchText -match 'state\.InputState\.Reset\(\)'
) 'PathLatch right-click cancel must still clear its private input state.'

Assert-True (
    $pathLatchText -match 'record\.AdvanceDecision\.Kind\s*=\s*TargetingAdvanceKind\.Cancel'
) 'PathLatch right-click cancel must now express cancel through AdvanceDecision.'

Write-Output 'PathLatchRightClickCancelBoundarySmokeTests PASS'
