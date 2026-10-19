# 该脚本包含中文字面量，必须以 UTF-8 BOM 保存，避免 Windows PowerShell 误判编码。
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
$projectileText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\BdpProjectile.cs')
$collisionText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs')
$verbText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_Shoot.cs')

Assert-True (
    ($collisionText -match 'TryResolveVisualEndpointBeforeCell') -and
    ($collisionText -match 'DoesSubPathTraverseCell') -and
    ($collisionText -match 'VisualBlockerBackoffDistance')
) 'SegmentCollisionService must expose a path-based visual endpoint resolver for non-hard cover interception.'

Assert-True (
    ($projectileText -match 'TryApplyTerminalCoverVisualAnchor') -and
    ($projectileText -match 'ProjectileLaunchDisposition\.CoverIntercept') -and
    ($projectileText -match 'TryResolveVisualEndpointBeforeCell') -and
    ($projectileText -match 'FreezeTerminalVisualExactPosition')
) 'BdpProjectile must clamp the visual endpoint for a cover-intercept impact without changing the physical hit target.'

Assert-True (
    ($verbText -match 'aimChanceStandard') -and
    ($verbText -match 'aimChanceIgnoringPosture') -and
    ($verbText -match 'trajectoryCoverBlockChance') -and
    ($verbText -match 'coverOriginCell')
) 'Launch diagnostics must expose the independent aim and trajectory-cover facts needed to diagnose low hit rate.'

Write-Output 'ProjectileCoverInterceptVisualEndpointSmokeTests PASS'
