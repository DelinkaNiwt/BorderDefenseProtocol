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

$surfacePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionSurfaceAccess.cs'
$verbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'

$surfaceText = Read-Source $surfacePath
$verbText = Read-Source $verbPath
$guardStart = $verbText.IndexOf('internal bool CanAcceptAutoRangedEntryStaging')
$stageStart = $verbText.IndexOf('internal void StageEntryModuleSession')
$guardBody = if (($guardStart -ge 0) -and ($stageStart -gt $guardStart)) {
    $verbText.Substring($guardStart, $stageStart - $guardStart)
} else {
    ''
}

Assert-True (
    ($guardStart -ge 0) -and
    ($guardBody -match 'RequiresFormalHostRuntimeTick\(\)') -and
    ($guardBody -match 'HostSessionToken\.AttackInstanceId') -and
    ($guardBody -match 'hostAttackContextSnapshot\s*!=\s*null') -and
    ($guardBody -match 'HostModuleSession\s*!=\s*null')
) 'BdpVerb_Shoot must expose a guard that treats warmup, pending plans, host snapshots, resident sessions, and attack-id-bearing host tokens as active attack bindings.'

Assert-True (
    ($surfaceText -match 'CanAcceptAutoRangedEntryStaging') -and
    ($surfaceText -match 'if\s*\(\s*!shootVerb\.CanAcceptAutoRangedEntryStaging\(\)\s*\)') -and
    ($surfaceText -match 'StageEntryModuleSession\(stagedSession\)')
) 'Auto ranged verb discovery must not stage a fresh entry session into an already active BDP ranged host.'

Write-Output 'AutoRangedEntryStagingGuardSmokeTests PASS'
