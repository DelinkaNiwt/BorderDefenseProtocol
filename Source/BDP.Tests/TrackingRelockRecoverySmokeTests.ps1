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

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modProjectsRoot = Split-Path -Parent $repoRoot

$trackingPathBuilderPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingPathBuilder.cs'
$trackingPathBuilderText = Read-Source $trackingPathBuilderPath

Assert-True (
    ($trackingPathBuilderText -match '\bResolveRecoveryEnd\b') -and
    ($trackingPathBuilderText -match 'distanceToTarget\s*<=\s*configuredRecoveryDistance') -and
    ($trackingPathBuilderText -match 'return\s+targetPos;') -and
    ($trackingPathBuilderText -match 'config\.RelockWindow\)') -and
    ($trackingPathBuilderText -notmatch 'RelockWindow\s*\*\s*0\.55f')
) 'Tracking relock recovery must allow a true return-to-target within the configured relock window instead of truncating every relock into an over-short recovery leg.'

Write-Output 'TrackingRelockRecoverySmokeTests PASS'
