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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'
$devHarnessSourceRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$pathLatchPath = Join-Path $devHarnessSourceRoot 'RangedModules\Samples\PathLatchModule.cs'
$driverPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInteractionDriver.cs'

$pathLatchText = Read-Source $pathLatchPath
$driverText = Read-Source $driverPath

Assert-True (
    $pathLatchText -match 'record\.InputState\.StepIndex\s*='
) 'PathLatch must still own its input step progress.'

Assert-True (
    $pathLatchText -notmatch 'record\.InteractionSession\.StepIndex\s*='
) 'PathLatch must no longer write shared interaction-session StepIndex directly.'

Assert-True (
    $driverText -match 'record\.InteractionSession\.StepIndex\s*=\s*record\.InputState\.StepIndex'
) 'TargetingInteractionDriver must remain the shared StepIndex sync owner.'

Write-Output 'PathLatchStepIndexOwnershipSmokeTests PASS'
