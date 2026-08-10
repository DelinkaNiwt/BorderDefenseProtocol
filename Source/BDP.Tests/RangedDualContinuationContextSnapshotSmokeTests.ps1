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

$verbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'
$continuationPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'
$protocolPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$verbText = Read-Source $verbPath
$continuationText = Read-Source $continuationPath
$protocolText = Read-Source $protocolPath

Assert-True (
    ($verbText -match 'AttackContextSnapshot\s+HostAttackContextSnapshot') -and
    ($verbText -match 'hostAttackContextSnapshot') -and
    ($verbText -match 'Scribe_Deep\.Look\(ref\s+hostAttackContextSnapshot,\s*"hostAttackContextSnapshot"\)') -and
    ($verbText -match 'ResolveProtocolAttackContextSnapshot')
) 'BdpVerb_Shoot must keep a frozen attack-context snapshot for dual continuation when no single host module session exists.'

Assert-True (
    ($continuationText -match 'TryApplyHostAttackContextSnapshot') -and
    ($continuationText -match 'verb\.HostAttackContextSnapshot') -and
    ($continuationText -match 'AttackContext\.FromSnapshot\(verb\.HostAttackContextSnapshot\)') -and
    ($continuationText -match 'source\s*=\s*"published_result_with_host_snapshot"')
) 'RangedVerbContinuationPlanner must apply the stored host context snapshot before falling back to an empty published-result session.'

Assert-True (
    ($protocolText -match 'outerEntry\.AttackContext\s*=\s*BuildMergedAttackContext') -and
    ($protocolText -match 'BuildMergedAttackContext') -and
    ($protocolText -match 'AttackContext\.FromSnapshot')
) 'Dual ranged protocol merge must publish a usable merged attack context on the outer entry.'

Write-Output 'RangedDualContinuationContextSnapshotSmokeTests PASS'
