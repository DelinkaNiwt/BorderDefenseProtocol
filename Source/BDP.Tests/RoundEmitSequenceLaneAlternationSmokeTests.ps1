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
$modRoot = Split-Path -Parent $repoRoot
$bdpCoreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$devHarnessRoot = Join-Path $modRoot 'BorderDefenseProtocol.DevHarness'

$contextPath = Join-Path $bdpCoreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageContext.cs'
$planPath = Join-Path $bdpCoreRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$stageServicePath = Join-Path $bdpCoreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$arrivalContextPath = Join-Path $bdpCoreRoot 'Projectiles\RangedFlightProtocol\Arrival\ArrivalStageContext.cs'
$modulePath = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples\PathLatchModule.cs'
$statePath = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples\PathLatchState.cs'

$contextText = Read-Source $contextPath
$planText = Read-Source $planPath
$stageServiceText = Read-Source $stageServicePath
$arrivalContextText = Read-Source $arrivalContextPath
$moduleText = Read-Source $modulePath
$stateText = Read-Source $statePath

# 契约:阶段上下文必须按编排结构直接给出轮内序号基数(动作步序号 × 本窗口 emit 数),
# 不能依赖模块自增状态,保证每轮起手从 0 重新开始。
Assert-True (
    ($contextText -match 'public int EmitSequenceBase \{ get; \}') -and
    ($contextText -match 'entry\.RuntimeStep\.StepIndex \* EmitCount')
) 'ProjectileInitStageContext must expose EmitSequenceBase computed from step index and window emit count.'

# 契约:计划必须携带轮内发射序号,构建时按 基数 + emitIndex 计算,读档缺失回退 EmitIndex。
Assert-True (
    ($planText -match 'public int EmitSequence \{ get; set; \}') -and
    ($stageServiceText -match 'int emitSequenceBase = entry != null && entry\.RuntimeStep != null') -and
    ($stageServiceText -match 'EmitSequence = emitSequenceBase \+ emitIndex,') -and
    ($planText -match 'EmitSequence = emitSequence >= 0 \? emitSequence : emitIndex;')
) 'ProjectileInitPlan must carry round-local emit sequence computed from step position, with save/load fallback.'

# 契约:飞行/到达阶段必须按轮内序号解析子弹,逐射每窗口单发也能区分每发。
Assert-True (
    $arrivalContextText -match 'initPlan != null \? initPlan\.EmitSequence : 0'
) 'Arrival stage must resolve per-projectile ordinal from EmitSequence.'

# 契约:毒蛇模块必须用轮内序号(基址+emitIndex)选择自动路由左右路,并且不得引入跨轮累计计数器。
Assert-True (
    ($moduleText -match 'int roundEmitSequence = context\.EmitSequenceBase \+ emitIndex;') -and
    ($moduleText -match 'BuildConfirmedSnapshotForEmit\(confirmedSnapshot, roundEmitSequence\)') -and
    ($stateText -notmatch 'NextEmitSequence') -and
    ($moduleText -notmatch 'NextEmitSequence\+\+')
) 'PathLatch must alternate auto-route lanes by round-local emit sequence computed from orchestration, without any cross-round counter.'

Write-Output 'RoundEmitSequenceLaneAlternationSmokeTests PASS'
