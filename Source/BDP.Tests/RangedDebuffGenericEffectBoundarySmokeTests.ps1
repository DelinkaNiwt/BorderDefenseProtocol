$ErrorActionPreference = 'Stop'

$planPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Model\ExtraEffectPlan.cs'
$registryPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Effects\ExtraEffectPlanExecutorRegistry.cs'
$hediffExecutorPath = Join-Path $PSScriptRoot '..\BDP.Content\RangedModules\Debuff\HediffExtraEffectExecutor.cs'

$planText = Get-Content -LiteralPath $planPath -Raw -Encoding UTF8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8
$hediffExecutorText = Get-Content -LiteralPath $hediffExecutorPath -Raw -Encoding UTF8

if ($planText -match '\bas Pawn\b' -or $planText -match 'Hediff') {
    throw 'Core 额外效果计划不应绑定 Pawn 或 Hediff。'
}

if ($registryText -notmatch 'EffectKind' -or $registryText -notmatch 'TryExecute') {
    throw 'Core 效果注册表必须按 EffectKind 保持可扩展。'
}

if ($hediffExecutorText -notmatch 'EffectKind' -or
    $hediffExecutorText -notmatch 'targetPawn' -or
    $hediffExecutorText -notmatch 'TargetFilter') {
    throw 'Hediff 执行器必须在 Content 层自我声明 Pawn 目标限制，不能由 Core 强制所有效果转 Pawn。'
}

if ($hediffExecutorText -match 'filter == RangedDebuffTargetFilter\.PawnsOnly\.ToString\(\)\s*&&\s*targetPawn == null') {
    throw 'Hediff 执行器不能保留不可达的 Pawn-only 二次筛选；目标类型应由具体执行器一次裁决。'
}

Write-Output 'RangedDebuffGenericEffectBoundarySmokeTests PASS'
