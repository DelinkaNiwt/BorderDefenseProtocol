$ErrorActionPreference = 'Stop'

$sourceRoot = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol'
$registryPath = Join-Path $sourceRoot 'Effects\ExtraEffectPlanExecutorRegistry.cs'
$executorPath = Join-Path $sourceRoot 'Effects\IExtraEffectPlanExecutor.cs'
$impactServicePath = Join-Path $sourceRoot 'Impact\ImpactStageService.cs'

if (-not (Test-Path -LiteralPath $registryPath) -or -not (Test-Path -LiteralPath $executorPath)) {
    throw '额外效果执行器注册设施尚未建立。'
}

$registry = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8
$executor = Get-Content -LiteralPath $executorPath -Raw -Encoding UTF8
$impactService = Get-Content -LiteralPath $impactServicePath -Raw -Encoding UTF8

foreach ($member in @('TryRegister', 'TryExecute')) {
    if ($registry -notmatch ('\b' + $member + '\b')) {
        throw ('执行器注册表缺少成员：' + $member)
    }
}

if ($executor -notmatch 'EffectKind' -or $executor -notmatch 'TryExecute') {
    throw '效果执行器契约缺少效果键或执行入口。'
}

if ($impactService -notmatch 'ExtraEffectsToAppend' -or $impactService -notmatch 'ExtraEffects\.AddRange') {
    throw 'ImpactStageService 尚未按模块顺序合并额外效果。'
}

if ($impactService -notmatch 'SuppressAllProjectileImpact') {
    throw 'ImpactStageService 尚未处理全量伤害抑制优先级。'
}

Write-Output 'RangedExtraEffectExecutorBoundarySmokeTests PASS'
