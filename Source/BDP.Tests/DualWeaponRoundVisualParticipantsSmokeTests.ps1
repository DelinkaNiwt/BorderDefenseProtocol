$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$runtimeStatePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerVisualRuntimeState.cs'
$runtimeOwnerPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerVisualRuntimeStateOwner.cs'
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionVisualRuntimeBridge.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\WeaponVisualStageResolver.cs'
$planPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionPlan.cs'
$stagesPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'

foreach ($path in @($runtimeStatePath, $runtimeOwnerPath, $bridgePath, $shootVerbPath, $resolverPath, $planPath, $stagesPath)) {
    Assert-True (Test-Path -LiteralPath $path) "Required source file is missing: $path"
}

$runtimeStateText = Get-Content -LiteralPath $runtimeStatePath -Raw -Encoding utf8
$runtimeOwnerText = Get-Content -LiteralPath $runtimeOwnerPath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$planText = Get-Content -LiteralPath $planPath -Raw -Encoding utf8
$stagesText = Get-Content -LiteralPath $stagesPath -Raw -Encoding utf8

# 攻击计划必须继续从实际构建出的 casts 收集整轮参与者；这会自然排除 dual 合法性检查未存活的一侧。
Assert-True (
    ($planText -match 'IReadOnlyList<string>\s+InvolvedResultIds') -and
    ($stagesText -match 'InvolvedResultIds\s*=\s*CollectInvolvedResultIds\(casts\)')
) 'The execution plan must retain the actually surviving result ids for the entire attack round.'

# 运行态必须把整轮参与者与当前步骤、当前发射源分别建模。
Assert-True (
    ($runtimeStateText -match 'IReadOnlyList<string>\s+ActiveAttackParticipantResultIds') -and
    ($runtimeStateText -match 'ActiveAttackParticipantResultIds\s*!=\s*null\s*&&\s*ActiveAttackParticipantResultIds\.Count\s*>\s*0') -and
    ($runtimeStateText -match 'ActiveAttackParticipantResultIds\s*=\s*new List<string>\(\)') -and
    ($runtimeStateText -match 'IReadOnlyList<string>\s+ActiveCastResultIds') -and
    ($runtimeStateText -match 'IReadOnlyList<string>\s+ActiveEmitSourceResultIds')
) 'Visual runtime state must model round participants separately from the current cast and emit focus.'

# 发布和清理必须覆盖新增集合，避免攻击结束或投影切换后把武器永久留在动作阶段。
Assert-True (
    ($runtimeOwnerText -match 'IReadOnlyList<string>\s+activeAttackParticipantResultIds') -and
    ($runtimeOwnerText -match 'ActiveAttackParticipantResultIds\s*=\s*CloneList\(activeAttackParticipantResultIds\)') -and
    ($runtimeOwnerText -match 'ClearExecutionState[\s\S]*ActiveAttackParticipantResultIds\s*=\s*new List<string>\(\)')
) 'The visual runtime owner must publish and clear the complete round participant set.'

# 初始上下文只能发布当前步骤焦点；整轮参与者必须等正式发射计划聚合完成后再发布。
Assert-True (
    ($bridgeText -match 'PublishAttackParticipants') -and
    ($bridgeText -match 'emissionPlan\.StepSourceResultIds') -and
    ($bridgeText -match 'CollectCastResultIds\(context\.Step\)') -and
    ($bridgeText -match 'CollectEmitSourceResultIds\(context\.Step\)') -and
    ($bridgeText -notmatch 'CollectAttackParticipantResultIds\(context\.Plan') -and
    ($shootVerbText -match 'BindVerbEmissionPlan[\s\S]*AttackExecutionVisualRuntimeBridge\.PublishAttackParticipants\([\s\S]*emissionPlan')
) 'The bridge must publish round participants from the final emission plan while preserving step-level focus.'

# 阶段参与关系只能读整轮参与者；当前 cast 是逐步焦点，不得再决定整轮换图与显隐。
Assert-True (
    ($resolverText -match 'ActiveAttackParticipantResultIds') -and
    ($resolverText -notmatch 'ActiveCastResultIds') -and
    ($resolverText -match 'visualRuntimeState\s*!=\s*null\s*&&\s*visualRuntimeState\.HasExecutionState[\s\S]*ActiveAttackParticipantResultIds\s*==\s*null[\s\S]*return null;') -and
    ($resolverText -match 'else[\s\S]*roots\.Add\(token\.ResultId\)')
) 'Weapon stages must use whole-round participants, with the formal host token reserved for recovery fallback.'

Write-Output 'DualWeaponRoundVisualParticipantsSmokeTests PASS'
