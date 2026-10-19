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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'

$modePath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\HitPartResolutionMode.cs'
$damagePlanPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\DamagePlan.cs'
$resolverContextPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\DamageAmountResolutionContext.cs'
$resolverPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\IDamageAmountResolver.cs'
$projectilePath = Join-Path $coreRoot 'Projectiles\BdpProjectile.cs'
$vitalHitPath = Join-Path $contentRoot 'RangedModules\VitalHit\VitalHitModule.cs'

Assert-True (Test-Path -LiteralPath $modePath) '缺少 Core 命中部位解析策略定义。'
Assert-True (Test-Path -LiteralPath $resolverContextPath) '缺少 Core 伤害量解析上下文。'
Assert-True (Test-Path -LiteralPath $resolverPath) '缺少 Core 伤害量解析器接口。'

$damagePlanText = Get-Content -LiteralPath $damagePlanPath -Raw -Encoding utf8
$resolverContextText = Get-Content -LiteralPath $resolverContextPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$vitalHitText = Get-Content -LiteralPath $vitalHitPath -Raw -Encoding utf8

Assert-True ($damagePlanText -match 'HitPartResolutionMode') 'DamagePlan 必须暴露命中部位解析策略。'
Assert-True ($damagePlanText -match 'IDamageAmountResolver') 'DamagePlan 必须暴露中性伤害量解析器。'
Assert-True (($resolverContextText -match 'BodyPartRecord') -and ($resolverContextText -match 'HitPart')) '伤害量解析上下文必须暴露最终命中部件。'
Assert-True (($resolverText -match 'interface') -and ($resolverText -match 'IDamageAmountResolver')) 'Core 必须提供中性伤害量解析器接口。'
Assert-True ($projectileText -match 'ResolveExtraDamageHitPart') '额外伤害执行路径必须解析命中部位策略。'
Assert-True (($projectileText -match 'ResolveExtraDamageHitPart') -and ($projectileText -match 'primaryDamageResolution')) '额外伤害必须复用主伤害结果解析出的命中部位。'
Assert-True ($projectileText -match 'DamageResult\.LastHitPart') 'Core 必须读取原版 DamageResult 的实际命中部件。'
Assert-True ($projectileText -match 'AmountResolver') 'Core 必须在正式伤害前解析延迟伤害量。'
Assert-True ($projectileText -notmatch 'VitalHitModule|VitalHitConfig|HitPartPolicyResolver') 'Core 投射物执行层不得引用要害业务模块。'
Assert-True ($vitalHitText -match 'HitPartOverride') '要害模块必须继续只提交中性命中部位覆盖。'

Write-Output 'RangedExtraDamageHitPartInheritanceSmokeTests PASS'
