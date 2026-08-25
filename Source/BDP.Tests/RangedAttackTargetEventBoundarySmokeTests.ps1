$ErrorActionPreference = 'Stop'

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

$scopePath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\ExtraEffectTargetScope.cs'
$eventPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Impact\AttackTargetEvent.cs'
$dispatcherPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Impact\AttackTargetEventDispatcher.cs'
$impactPlanPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\ImpactPlan.cs'
$impactContributionPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'
$areaExplosionPath = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\AreaExplosion\AreaExplosionModule.cs'
$projectilePath = Join-Path $coreRoot 'Projectiles\BdpProjectile.cs'
$explosionPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs'
$debuffConfigPath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffConfig.cs'
$debuffModulePath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffModule.cs'
$debuffXmlPath = Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedDebuff.xml'

foreach ($path in @($scopePath, $eventPath, $dispatcherPath, $impactPlanPath, $impactContributionPath, $projectilePath, $explosionPatchPath, $areaExplosionPath, $debuffConfigPath, $debuffModulePath, $debuffXmlPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('缺少目标事件实现文件：' + $path)
}

$scopeText = Get-Content -LiteralPath $scopePath -Raw -Encoding UTF8
$eventText = Get-Content -LiteralPath $eventPath -Raw -Encoding UTF8
$dispatcherText = Get-Content -LiteralPath $dispatcherPath -Raw -Encoding UTF8
$impactPlanText = Get-Content -LiteralPath $impactPlanPath -Raw -Encoding UTF8
$impactContributionText = Get-Content -LiteralPath $impactContributionPath -Raw -Encoding UTF8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$areaExplosionText = Get-Content -LiteralPath $areaExplosionPath -Raw -Encoding UTF8
$explosionPatchText = Get-Content -LiteralPath $explosionPatchPath -Raw -Encoding UTF8
$debuffConfigText = Get-Content -LiteralPath $debuffConfigPath -Raw -Encoding UTF8
$debuffModuleText = Get-Content -LiteralPath $debuffModulePath -Raw -Encoding UTF8
$debuffXmlText = Get-Content -LiteralPath $debuffXmlPath -Raw -Encoding UTF8

Assert-True ($scopeText -match 'AttackTargetEvents') '额外效果范围缺少通用攻击目标事件来源。'
Assert-True ($eventText -match 'class AttackTargetEvent') 'Core 缺少攻击目标事件模型。'
Assert-True ($dispatcherText -match 'class AttackTargetEventDispatcher' -and $dispatcherText -match 'Dispatch') 'Core 缺少通用攻击目标事件派发器。'
Assert-True ($impactPlanText -match 'PreserveTargetResolutionWhenDamageSuppressed') 'ImpactPlan 缺少伤害取消时继续目标解析策略。'
Assert-True ($impactPlanText -match 'ProducesAttackTargetEvents') 'ImpactPlan 缺少攻击目标生产者标记。'
Assert-True ($impactContributionText -match 'PreserveTargetResolutionWhenDamageSuppressed') 'ImpactContribution 缺少伤害取消时继续目标解析策略。'
Assert-True ($impactContributionText -match 'ProducesAttackTargetEvents') 'ImpactContribution 缺少攻击目标生产者标记。'
Assert-True ($projectileText -match 'AttackTargetEventDispatcher') '直接命中入口没有接入通用目标事件派发器。'
Assert-True ($explosionPatchText -match 'AttackTargetEventDispatcher') '逐目标生产入口没有接入通用目标事件派发器。'
Assert-True ($areaExplosionText -match 'ProducesAttackTargetEvents') '范围攻击生产模块没有声明自己会产生攻击目标事件。'
Assert-True ($debuffConfigText -match 'PreserveTargetResolutionWhenDamageSuppressed') '远程减益配置缺少目标解析保留字段。'
Assert-True ($debuffModuleText -match 'PreserveTargetResolutionWhenDamageSuppressed') '远程减益模块没有提交目标解析保留策略。'
Assert-True ($debuffXmlText -match '<TargetScope>AttackTargetEvents</TargetScope>') '铅弹正式减益没有使用通用攻击目标事件范围。'

# 派发器不得以目标 Thing 建立唯一集合；每次 Dispatch 调用都必须允许重复目标事件。
Assert-True ($dispatcherText -notmatch 'HashSet\s*<\s*Thing\s*>' -and $dispatcherText -notmatch 'Distinct\s*\(') '通用目标事件派发器不得自行按目标去重。'
$dedupeIndex = $explosionPatchText.IndexOf('damagedThings.Contains(t)', [System.StringComparison]::Ordinal)
$dispatchIndex = $explosionPatchText.IndexOf('AttackTargetEventDispatcher.Dispatch', [System.StringComparison]::Ordinal)
Assert-True ($dedupeIndex -ge 0 -and $dispatchIndex -gt $dedupeIndex) '生产者原版去重边界必须先于通用目标事件派发。'

# 普通范围伤害必须放行原版 ExplosionDamageThing；只有抑制原版伤害的分支才能手动登记 damagedThings。
# 否则原版方法会再次命中去重条件，直接跳过 TakeDamage。
$normalBranchIndex = $explosionPatchText.IndexOf('if (!impactContext.SuppressCurrentAreaDamage)', [System.StringComparison]::Ordinal)
$manualRegistrationIndex = $explosionPatchText.IndexOf('damagedThings.Add(t)', [System.StringComparison]::Ordinal)
Assert-True ($normalBranchIndex -ge 0 -and $manualRegistrationIndex -gt $normalBranchIndex) '普通范围伤害不得在进入抑制分支前手动登记 damagedThings。'

Write-Output 'RangedAttackTargetEventBoundarySmokeTests PASS'
