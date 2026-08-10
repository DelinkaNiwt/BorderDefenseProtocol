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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$formalHostShootPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostShoot.cs'
$diagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionDiagnostics.cs'

$formalHostShootText = Read-Source $formalHostShootPath
$diagnosticsText = Read-Source $diagnosticsPath

Assert-True (
    $formalHostShootText -match 'override bool CanHitTargetFrom'
) 'Dual formal host must still override CanHitTargetFrom at the engine boundary.'

Assert-True (
    $formalHostShootText -match 'TryEvaluateDualAdapterLegality'
) 'Dual formal host must define an explicit adapter legality resolver for composite execution.'

Assert-True (
    $formalHostShootText -match 'return anySideAllowed;'
) 'Dual formal host must return aggregated side legality when the dual adapter path is active.'

Assert-True (
    $formalHostShootText -match 'return base.CanHitTargetFrom\(root, targ\);'
) 'Non-dual formal hosts must still fall back to base CanHitTargetFrom behavior.'

Assert-True (
    $diagnosticsText -match 'event=dual_ranged_host_los_probe'
) 'Dual host adapter legality must keep the focused dual_ranged_host_los_probe diagnostics event.'

Write-Output 'DualRangedAdapterLegalitySmokeTests PASS'
