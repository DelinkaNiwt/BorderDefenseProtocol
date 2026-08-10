$ErrorActionPreference = "Stop"

function Assert {
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

$prepareRecordPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\PrepareRecord.cs'
$prepareContributionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\PrepareContribution.cs'
$prepareModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\RangedTrionPrepareModule.cs'
$surfaceAccessPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolSurfaceAccess.cs'
$gatePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackTrionGate.cs'
$gateResultPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedAttackTrionGateResult.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$roundStatePath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbRoundState.cs'
$emissionCursorPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbEmissionCursor.cs'
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$combatBodySessionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'

$prepareRecordText = Get-Content -LiteralPath $prepareRecordPath -Raw -Encoding utf8
$prepareContributionText = Get-Content -LiteralPath $prepareContributionPath -Raw -Encoding utf8
$prepareModuleExists = Test-Path -LiteralPath $prepareModulePath
$prepareModuleText = if ($prepareModuleExists) { Get-Content -LiteralPath $prepareModulePath -Raw -Encoding utf8 } else { '' }
$surfaceAccessText = Get-Content -LiteralPath $surfaceAccessPath -Raw -Encoding utf8
$gateExists = Test-Path -LiteralPath $gatePath
$gateText = if ($gateExists) { Get-Content -LiteralPath $gatePath -Raw -Encoding utf8 } else { '' }
$gateResultExists = Test-Path -LiteralPath $gateResultPath
$gateResultText = if ($gateResultExists) { Get-Content -LiteralPath $gateResultPath -Raw -Encoding utf8 } else { '' }
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$roundStateText = Get-Content -LiteralPath $roundStatePath -Raw -Encoding utf8
$emissionCursorText = Get-Content -LiteralPath $emissionCursorPath -Raw -Encoding utf8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$combatBodySessionText = if (Test-Path -LiteralPath $combatBodySessionPath) { Get-Content -LiteralPath $combatBodySessionPath -Raw -Encoding utf8 } else { '' }

Assert (
    ($prepareRecordText -match 'public float ResourceCost \{ get; set; \}') -and
    ($prepareRecordText -match 'public float MinimumRequired \{ get; set; \}')
) 'PrepareRecord must expose round ResourceCost and MinimumRequired.'

Assert (
    ($prepareContributionText -match 'public float AddedResourceCost \{ get; set; \}') -and
    ($prepareContributionText -match 'public float MinimumRequiredCandidate \{ get; set; \}') -and
    ($prepareContributionText -match 'public bool HasMinimumRequiredCandidate \{ get; set; \}')
) 'PrepareContribution must expose minimum-required contribution fields.'

Assert $prepareModuleExists 'RangedTrionPrepareModule must exist.'

Assert (
    ($prepareModuleText -match 'class\s+RangedTrionPrepareModule') -and
    ($prepareModuleText -match 'entry\.SourceResult\.Trion') -and
    ($prepareModuleText -match 'UseCost') -and
    ($prepareModuleText -match 'MinimumRequired')
) 'RangedTrionPrepareModule must derive round cost and threshold from entry.SourceResult.Trion.'

Assert (
    $surfaceAccessText -match 'new\s+RangedTrionPrepareModule\s*\('
) 'RangedAttackProtocolSurfaceAccess must assemble RangedTrionPrepareModule.'

Assert $gateExists 'RangedAttackTrionGate must exist.'
Assert $gateResultExists 'RangedAttackTrionGateResult must exist.'

Assert (
    ($gateText -match 'class\s+RangedAttackTrionGate') -and
    ($gateText -match 'TrionSurfaceAccess\.ResolveCommands') -and
    ($gateText -match 'TryAdmitWarmup') -and
    ($gateText -match 'TryCommitBeforeFirstEmission') -and
    ($gateText -match 'TryConsume\s*\(')
) 'RangedAttackTrionGate must resolve Trion commands and expose admission/commit entry points.'

Assert (
    ($gateResultText -match 'class\s+RangedAttackTrionGateResult') -and
    ($gateResultText -match 'Reason') -and
    ($gateResultText -match 'Message')
) 'RangedAttackTrionGateResult must carry structured failure data.'

Assert (
    ($verbText -match 'RangedVerbRoundState') -and
    ($verbText -match 'RangedVerbEmissionCursor') -and
    ($verbText -match 'TryEnsureRoundTrionAdmission') -and
    ($verbText -match 'TryCommitRoundTrionBeforeFirstEmission') -and
    ($verbText -match 'ShowInsufficientTrionMessage')
) 'BdpVerb_Shoot must delegate round charging and emission progress to dedicated collaborators while keeping the verb-side gates.'

Assert (
    ($verbText -notmatch 'HashSet<int>') -and
    ($verbText -match 'insufficientTrionPromptLatchedAttackInstanceId') -and
    ($verbText -match 'ResolveInsufficientTrionPromptSessionKey') -and
    ($verbText -match 'SyncInsufficientTrionPromptLatchToCurrentSession') -and
    ($verbText -match 'ResetInsufficientTrionPromptLatch') -and
    ($verbText -match 'HostSessionToken\.AttackInstanceId') -and
    ($verbText -match 'ShowInsufficientTrionMessage') -and
    ($verbText -match 'Messages\.Message')
) 'BdpVerb_Shoot must latch insufficient Trion prompts per attack session, not per pawn, and keep the latch aligned to current HostSessionToken attack-session identity.'

Assert (
    ($roundStateText -match 'class\s+RangedVerbRoundState') -and
    ($roundStateText -match 'HasCommittedRoundTrion') -and
    ($roundStateText -match 'TryEnsureRoundTrionAdmission') -and
    ($roundStateText -match 'TryCommitRoundTrionBeforeFirstEmission')
) 'RangedVerbRoundState must own round-local Trion bookkeeping and gate interaction.'

Assert (
    ($emissionCursorText -match 'class\s+RangedVerbEmissionCursor') -and
    ($emissionCursorText -match 'PendingWindowIndex') -and
    ($emissionCursorText -match 'PendingWindowProjectilePlanIndex') -and
    ($emissionCursorText -match 'PendingEmissionConsumedCount')
) 'RangedVerbEmissionCursor must own persisted emission progress bookkeeping.'

Assert (
    $projectileText -notmatch 'TryConsume\s*\('
) 'Projectile host must not charge Trion per projectile.'

Assert (
    $combatBodySessionText -notmatch 'TryAdmitWarmup|TryCommitBeforeFirstEmission|RangedAttackTrionGate'
) 'CombatBodySession must stay out of ranged round Trion charging.'

Write-Output 'RangedAttackTrionConsumption PASS'
