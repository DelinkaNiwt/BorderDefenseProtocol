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
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\WeaponVisualStageResolver.cs'

Assert-True (Test-Path -LiteralPath $resolverPath) 'WeaponVisualStageResolver must exist.'
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8

Assert-True (
    ($resolverText -match 'pawn\.stances\.curStance') -and
    ($resolverText -match 'BdpVerb_FormalHostShoot') -and
    ($resolverText -match 'HostSessionToken') -and
    ($resolverText -match 'TriggerCombatProjectionState') -and
    ($resolverText -match 'TriggerVisualRuntimeState')
) 'The resolver must read the vanilla stance and the existing formal-session truth surfaces.'

Assert-True (
    ($resolverText -notmatch 'Scribe') -and
    ($resolverText -notmatch 'Tick\(\)') -and
    ($resolverText -notmatch 'TryPreparePlan') -and
    ($resolverText -notmatch 'TryExecute\(')
) 'The resolver must remain read-only and must not introduce a second persisted or ticking state machine.'

Assert-True (
    ($resolverText -match 'ActiveAttackParticipantResultIds') -and
    ($resolverText -notmatch 'ActiveCastResultIds') -and
    ($resolverText -match 'CompositeReferenceIndex') -and
    ($resolverText -match 'SourceResultIds') -and
    ($resolverText -match 'token\.ResultId') -and
    ($resolverText -match 'ResultIndex') -and
    ($resolverText -match 'ExpressionSourceReferenceMatcher\.AreSameChipInstance')
) 'Participants must expand whole-round or restored host results and match their source chip identity through the shared matcher.'

Assert-True (
    ($resolverText -match 'token\.IsValid') -and
    ($resolverText -match 'token\.BelongsTo\(pawn\)') -and
    ($resolverText -match 'token\.ProjectionVersion\s*!=\s*combatProjection\.ProjectionVersion') -and
    ($resolverText -match 'visualRuntimeState\.ActiveHostResultId') -and
    ($resolverText -match 'visualRuntimeState\.AttackInstanceId')
) 'The formal host session must agree on owner, projection, host result and available attack identity.'

$warmupIndex = $resolverText.IndexOf('currentStance is Stance_Warmup')
$burstingIndex = $resolverText.IndexOf('hostVerb.Bursting')
$cooldownIndex = $resolverText.IndexOf('currentStance is Stance_Cooldown')
Assert-True (
    ($warmupIndex -ge 0) -and
    ($burstingIndex -gt $warmupIndex) -and
    ($cooldownIndex -gt $burstingIndex)
) 'Stage priority must be warmup, then bursting, then final cooldown so burst intervals remain firing.'

Assert-True (
    ($resolverText -match 'hostVerb\.WarmupProgress') -and
    ($resolverText -match 'hostVerb\.WarmupTicksLeft') -and
    ($resolverText -match 'AdjustedCooldownTicks\(hostVerb, pawn\)') -and
    ($resolverText -match 'Mathf\.Clamp01')
) 'Stage progress must use vanilla warmup/cooldown truth and clamp the published value.'

Write-Output 'WeaponVisualStageResolverSmokeTests PASS'
