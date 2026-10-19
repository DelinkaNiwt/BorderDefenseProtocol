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
$repoRoot = Split-Path -Parent $sourceRoot

$recordText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\RangedFlightProtocol\Model\SegmentCollisionRecord.cs')
$collisionText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs')
$projectileText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\BdpProjectile.cs')
$contractText = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\Visual\ProjectileVisualAttachmentContracts.cs')
$attachmentText = Read-Source (Join-Path $sourceRoot 'BDP.Content\Projectiles\BeamTrail\BeamTrailAttachment.cs')

Assert-True (
    ($recordText -match 'HasFirstObjectiveBlockerVisualPosition') -and
    ($recordText -match 'FirstObjectiveBlockerVisualPosition')
) 'SegmentCollisionRecord must separate the logical blocker position from the visual-safe blocker position.'

Assert-True (
    ($collisionText -match 'TryResolveFirstObjectiveBlockerVisualPosition') -and
    ($collisionText -match 'FirstObjectiveBlockerVisualPosition') -and
    ($collisionText -match 'FirstObjectiveBlockerProgress') -and
    ($collisionText -match 'VisualBlockerBackoffDistance')
) 'SegmentCollisionService must derive the visual blocker endpoint from the real path and back it away from the blocker cell.'

Assert-True (
    ($projectileText -match 'pendingObjectiveBlockerVisualExactPosition') -and
    ($projectileText -match 'HasFirstObjectiveBlockerVisualPosition') -and
    ($projectileText -match 'ResolveCurrentTerminalVisualEndpointIsSafe') -and
    ($projectileText -match 'IsVisualEndpointSafe')
) 'BdpProjectile must keep physical and visual terminal positions separate and publish visual endpoint safety.'

Assert-True (
    ($contractText -match 'IsVisualEndpointSafe') -and
    ($attachmentText -match 'IsVisualEndpointSafe') -and
    ($attachmentText -notmatch 'CenterVector3') -and
    ($attachmentText -notmatch 'LiveFinalTarget')
) 'The visual termination contract must carry safety, while BeamTrail must not rebuild terminal geometry from target semantics.'

Write-Output 'ProjectileTerminalVisualEndpointSafetySmokeTests PASS'
