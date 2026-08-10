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

$shootVerbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'
$shootVerbText = Read-Source $shootVerbPath

Assert-True (
    $shootVerbText -match 'public override void WarmupComplete\(\)[\s\S]*bool\s+canRefreshPendingEmission'
) 'WarmupComplete must explicitly decide whether it can refresh the pending emission plan at fire time.'

Assert-True (
    $shootVerbText -match 'public override void WarmupComplete\(\)[\s\S]*if\s*\(canRefreshPendingEmission\)[\s\S]*ClearPendingEmissionPlan\(\)[\s\S]*rebuildSucceeded\s*=\s*TryPreparePendingEmission\(warmupTarget\);'
) 'WarmupComplete must discard the pre-warm pending emission plan and rebuild it from the live warmup target before firing.'

Assert-True (
    $shootVerbText -notmatch 'public override void WarmupComplete\(\)[\s\S]*bool\s+rebuildSucceeded\s*=\s*HasPendingEmissionPlan\(\)\s*\|\|\s*TryPreparePendingEmission\(warmupTarget\);'
) 'WarmupComplete must not directly reuse a pre-warm pending emission plan at the actual fire boundary.'

Write-Output 'RangedWarmupFireTimeRefreshSmokeTests PASS'
