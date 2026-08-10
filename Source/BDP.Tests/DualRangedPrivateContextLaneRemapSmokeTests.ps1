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

$protocolPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$protocolText = Read-Source $protocolPath

Assert-True (
    ($protocolText -match 'BuildSessionAttackContext') -and
    ($protocolText -match 'CompositeExpressionKind\.DualWeapon') -and
    ($protocolText -match 'AttackContextKeys\.ModulePrivatePrefix') -and
    ($protocolText -match 'request\.AttackContextSnapshot') -and
    ($protocolText -match 'sourceResultId') -and
    ($protocolText -match 'GetModulePrivateKey\(slot\.MountIndex\)') -and
    ($protocolText -match 'node\.Clone\(\)')
) 'Dual lane session rebuild must remap composite private-context keys into the lane mount-index space instead of blindly reusing the composite snapshot.'

Write-Output 'DualRangedPrivateContextLaneRemapSmokeTests PASS'
