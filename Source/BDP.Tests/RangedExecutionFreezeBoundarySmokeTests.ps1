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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$requestPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionRequest.cs'
$preparedContextPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionPreparedContext.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$protocolServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'
$entryPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\RangedAttackEntry.cs'

$requestText = Get-Content -LiteralPath $requestPath -Raw -Encoding utf8
$preparedContextText = Get-Content -LiteralPath $preparedContextPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8
$protocolServiceText = Get-Content -LiteralPath $protocolServicePath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$entryText = Get-Content -LiteralPath $entryPath -Raw -Encoding utf8

Assert-True (
    ($requestText -match 'AttackContextSnapshot') -and
    ($requestText -notmatch 'RangedAttackModuleSession\s+ModuleSession') -and
    ($requestText -notmatch 'ConfirmedInputSnapshot') -and
    ($requestText -notmatch 'ConfirmedInteractionSnapshot')
) 'AttackExecutionRequest must carry only AttackContextSnapshot at the freeze boundary.'

Assert-True (
    ($preparedContextText -match 'AttackContextSnapshot') -and
    ($preparedContextText -notmatch 'RangedAttackModuleSession\s+ModuleSession') -and
    ($preparedContextText -notmatch 'ConfirmedInputSnapshot') -and
    ($preparedContextText -notmatch 'ConfirmedInteractionSnapshot')
) 'AttackExecutionPreparedContext must expose only AttackContextSnapshot as the frozen trunk.'

Assert-True (
    ($targetingSourceText -match 'AttackContextSnapshot\s*=\s*BuildExecutionAttackContextSnapshot\(confirmRecord\)') -and
    ($continuationPlannerText -match 'AttackContextSnapshot\s*=\s*verb\.HostAttackContextSnapshot != null[\s\S]*?CreateAttackContextSnapshot\(moduleSession\);')
) 'Both confirm entry and continuation entry must freeze unified AttackContextSnapshot; continuation prefers the host-complete snapshot (dual sequential single-lane session is incomplete) and falls back to session export.'

Assert-True (
    ($protocolServiceText -match 'CreateSession\(request\.Pawn,\s*result\)') -and
    ($protocolServiceText -match 'AttackContext\.FromSnapshot\(request\.AttackContextSnapshot\)') -and
    ($protocolServiceText -notmatch 'request\.ModuleSession')
) 'RangedAttackProtocolService must rebuild runtime session from the frozen AttackContextSnapshot.'

Assert-True (
    ($entryText -notmatch 'ConfirmedInputSnapshot') -and
    ($entryText -notmatch 'ConfirmedInteractionSnapshot') -and
    ($entryText -match 'AttackContext\s+AttackContext')
) 'RangedAttackEntry must carry AttackContext only, not parallel confirmed snapshot fields.'

Write-Output 'RangedExecutionFreezeBoundarySmokeTests PASS'
