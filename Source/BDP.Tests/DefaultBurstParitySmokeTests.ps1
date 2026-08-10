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

$executorPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultRangedAttackExecutor.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbContinuationPlanner.cs'
$emissionCursorPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbEmissionCursor.cs'
$jobDriverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$contextPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedAttackExecutionContext.cs'
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$assemblerPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedBurstEmissionAssembler.cs'
$emissionPlanPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedVerbEmissionPlan.cs'
$emissionWindowPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedVerbEmissionWindowPlan.cs'

$executorText = Get-Content -LiteralPath $executorPath -Raw -Encoding utf8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$emissionCursorText = Get-Content -LiteralPath $emissionCursorPath -Raw -Encoding utf8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8
$contextText = Get-Content -LiteralPath $contextPath -Raw -Encoding utf8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$assemblerExists = Test-Path -LiteralPath $assemblerPath
$assemblerText = if ($assemblerExists) { Get-Content -LiteralPath $assemblerPath -Raw -Encoding utf8 } else { '' }
$emissionPlanText = Get-Content -LiteralPath $emissionPlanPath -Raw -Encoding utf8
$emissionWindowExists = Test-Path -LiteralPath $emissionWindowPath
$emissionWindowText = if ($emissionWindowExists) { Get-Content -LiteralPath $emissionWindowPath -Raw -Encoding utf8 } else { '' }

# 默认回归原则：
# 1. 顺序 burst 若未显式要求外层持续推进，应尽量回归原版 Verb burst 会话
# 2. AutoAttackOrder / ForceTargetOrder 也不能只绑定第一条 step 的单发计划
# 3. 宿主仍通过既有 BindVerbEmissionPlan 统一消费，不新开并行发射体系

Assert-True $assemblerExists 'Ranged burst emission assembler must exist.'

Assert-True (
    $assemblerText -match 'class\s+RangedBurstEmissionAssembler'
) 'Ranged burst emission assembler must be implemented as a dedicated helper.'

Assert-True (
    $assemblerText -match 'TryBuild'
) 'Ranged burst emission assembler must expose a TryBuild entry.'

Assert-True $emissionWindowExists 'Ranged verb emission window plan must exist.'

Assert-True (
    $emissionWindowText -match 'class\s+RangedVerbEmissionWindowPlan'
) 'Ranged verb emission window plan must be implemented as a dedicated model.'

Assert-True (
    $emissionPlanText -match 'IReadOnlyList<RangedVerbEmissionWindowPlan>\s+Windows'
) 'Ranged verb emission plan must declare ordered emission windows.'

Assert-True (
    $emissionPlanText -notmatch 'EmissionMode\s*\{'
) 'Ranged verb emission plan must no longer flatten the whole session into one emission mode field.'

Assert-True (
    $emissionPlanText -notmatch 'IReadOnlyList<ProjectileInitPlan>\s+ProjectilePlans'
) 'Ranged verb emission plan must no longer flatten all projectile plans into one top-level list.'

Assert-True (
    $executorText -match 'RangedBurstEmissionAssembler'
) 'DefaultRangedAttackExecutor must use the shared burst assembler.'

Assert-True (
    ($verbText -match 'RangedVerbContinuationPlanner') -and
    ($continuationPlannerText -match 'RangedBurstEmissionAssembler')
) 'BdpVerb_Shoot continuous preparation must delegate shared burst assembly through RangedVerbContinuationPlanner.'

Assert-True (
    $jobDriverText -match 'PrepareContinuation\('
) 'JobDriver_BdpRangedAttackExecution must reuse the verb continuation planner entry instead of duplicating burst assembly.'

Assert-True (
    $executorText -match 'BindVerbEmissionPlan\s*\(\s*immediateEmissionPlan\s*\)'
) 'ImmediateCast ranged execution must bind the assembled burst emission plan.'

Assert-True (
    $continuationPlannerText -match 'BindVerbEmissionPlan\s*\(\s*emissionPlan\s*\)'
) 'AutoAttackOrder ranged preparation must bind the assembled burst emission plan through the continuation planner.'

Assert-True (
    $jobDriverText -match 'PrepareContinuation\(\s*target,\s*AttackExecutionReason\.Manual,\s*AttackDispatchIntent\.ForceTargetOrder\s*\)'
) 'ForceTargetOrder ranged preparation must reuse the shared continuation planner entry.'

Assert-True (
    $executorText -notmatch 'BindVerbEmissionPlan\s*\(\s*context\.ProtocolResult\s*!=\s*null\s*\?\s*context\.ProtocolResult\.VerbEmissionPlan\s*:\s*null\s*\)'
) 'ImmediateCast ranged execution must not directly bind only the first step protocol emission plan.'

Assert-True (
    $verbText -notmatch 'BindVerbEmissionPlan\s*\(\s*protocolResult\.VerbEmissionPlan\s*\)'
) 'AutoAttackOrder ranged preparation must not directly bind only the first step protocol emission plan.'

Assert-True (
    $jobDriverText -notmatch 'BindVerbEmissionPlan\s*\(\s*protocolResult\.VerbEmissionPlan\s*\)'
) 'ForceTargetOrder ranged preparation must not directly bind only the first step protocol emission plan.'

Assert-True (
    $attackSurfaceText -match 'snapshot\.PrimaryRanged'
) 'Auto-ranged bridge must read the expression-selected PrimaryRanged instead of relying on vanilla primary-verb capture.'

Assert-True (
    $verbText -match 'burstShotsLeft = ResolveRemainingWindowCount'
) 'BdpVerb_Shoot must delegate burst progression by remaining emission windows.'

Assert-True (
    ($verbText -match 'RangedVerbEmissionCursor') -and
    ($emissionCursorText -match 'ResolveRemainingWindowCount') -and
    ($emissionCursorText -match 'TryBindNextWindowPlan')
) 'RangedVerbEmissionCursor must own emission-window progression and projectile consumption state.'

Assert-True (
    $verbText -notmatch 'burstShotsLeft = ResolveBoundProjectilePlanCount'
) 'BdpVerb_Shoot must not keep using flattened projectile count as burst session count.'

Assert-True (
    $verbText -notmatch 'pendingVerbEmissionPlan\.EmissionMode'
) 'BdpVerb_Shoot must not branch on a flattened plan-level emission mode anymore.'

Assert-True (
    $assemblerText -match 'AppendWindows'
) 'Ranged burst emission assembler must aggregate ordered emission windows.'

Assert-True (
    $assemblerText -notmatch 'cast\.ResultId == context\.Result\.Id'
) 'Ranged burst emission assembler must not reject mixed-rhythm follow-up windows only because source result id changes.'

Assert-True (
    $contextText -match 'TryCreateForStep'
) 'RangedAttackExecutionContext must still allow per-step context creation for explicit continuous paths.'

Write-Output 'DefaultBurstParity PASS'
