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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$protocolPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$protocolText = Get-Content -LiteralPath $protocolPath -Raw -Encoding utf8

Assert-True (
    $protocolText -match 'private static IReadOnlyList<ProjectileInitPlan> MergeProjectilePlansByOuterEmitOrder'
) 'RangedAttackProtocolService must merge projectile plans by the outer emit order.'

Assert-True (
    ($protocolText -match 'for \(int i = 0; i < outerEntry\.StepEmits\.Count; i\+\+\)') -and
    ($protocolText -match 'string sourceResultId = outerEntry\.StepEmits\[i\]\?\.SourceResultId')
) 'Merged projectile order must iterate the outer emit sequence and read each emit sourceResultId.'

Assert-True (
    ($protocolText -match 'private static PrepareRecord BuildMergedPrepareRecord') -and
    ($protocolText -match 'private static FireRecord BuildMergedFireRecord') -and
    ($protocolText -match 'private static RangedVerbEmissionPlan BuildMergedVerbEmissionPlan') -and
    ($protocolText -match 'private static RangedProjectionSeed BuildMergedProjectionSeed')
) 'RangedAttackProtocolService must provide the merged dual result builders.'

Write-Output 'DualRangedProtocolMergedPlanOrderSmokeTests PASS'
