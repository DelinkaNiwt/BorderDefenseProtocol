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
$autoRoutePath = Join-Path $devHarnessSourceRoot 'RangedModules\Samples\PathLatchAutoRouteResolver.cs'
$pathLatchText = Read-Source $pathLatchPath
$autoRouteText = if (Test-Path -LiteralPath $autoRoutePath) { Read-Source $autoRoutePath } else { '' }

Assert-True (
    $pathLatchText -match 'TryValidateCurrentSegmentLineOfSight'
) 'PathLatch must own a shared current-segment LOS legality helper.'

Assert-True (
    $pathLatchText -match 'TryValidatePreviewSegmentCandidate[\s\S]*TryValidateCurrentSegmentLineOfSight'
) 'PathLatch preview must read the module-owned current-segment LOS helper.'

Assert-True (
    $pathLatchText -match 'TryValidateSegmentCandidate[\s\S]*TryValidateCurrentSegmentLineOfSight'
) 'PathLatch confirm-time segment validation must reuse the same module-owned current-segment LOS helper.'

Assert-True (
    $pathLatchText -notmatch 'TargetingSegmentLegalityRequest\.FromRecord'
) 'PathLatch must not route its segment legality through the neutral TargetingSegmentLegalityRequest bridge.'

Assert-True (
    $pathLatchText -notmatch 'record\.SegmentLegality\.Evaluate'
) 'PathLatch must not delegate its segment legality to the shared neutral SegmentLegality service.'

Assert-True (
    ($autoRouteText -match 'GenSight\.LineOfSight') -and
    ($pathLatchText -match 'TryResolveAutoRouteForFinalTarget')
) 'PathLatch auto-route must stay module-owned and use vanilla LOS rather than the shared SegmentLegality bridge.'

Write-Output 'PathLatchSegmentLegalityOwnershipSmokeTests PASS'
