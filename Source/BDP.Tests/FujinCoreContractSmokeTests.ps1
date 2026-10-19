$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$policyText = Read-Source (Join-Path $coreRoot 'Projectiles\Interaction\ProjectileInteractionPolicy.cs')
$initText = Read-Source (Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs')
$projectileText = Read-Source (Join-Path $coreRoot 'Projectiles\BdpProjectile.cs')
$impactText = Read-Source (Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactStageContext.cs')
$damagePlanText = Read-Source (Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\DamagePlan.cs')
$abilityText = Read-Source (Join-Path $coreRoot 'Abilities\BdpVerb_AbilityShoot.cs')
$capabilityResolverText = Read-Source (Join-Path $coreRoot 'Expressions\Capability\ChipExpressionCapabilityResolver.cs')
$interpreterText = Read-Source (Join-Path $coreRoot 'Expressions\Contract\ChipExpressionContractInterpreter.cs')
$revectorText = Read-Source (Join-Path $contentRoot 'RangedModules\DistanceRevector\DistanceRevectorModule.cs')

Assert-True ($abilityText -notmatch '(?i)fujin') 'Core Ability 远程宿主不得保留风刃业务名称。'

Assert-True ($policyText -match 'AllowNonTargetPassThrough') '必须存在统一的非目标放行策略。'
Assert-True (($initText -match 'AllowNonTargetPassThrough') -and
    ($projectileText -match 'CurrentInteractionPolicy')) `
    '统一非目标放行策略必须能从初始化计划传到投射物。'
Assert-True ($projectileText -match 'ResolveContinuationBindingFlightPathSnapshot') `
    '后续折线段必须经过统一路径绑定入口。'
Assert-True ($projectileText -match 'ShouldBypassObjectiveBlockers') `
    '后续折线段必须保留原版越顶投射物的客观障碍快速路径。'
Assert-True ($impactText -notmatch 'FinalFlightDirection|DamageFactor') `
    'Impact 上下文不得保留仅供风刃方向斩击使用的方向和伤害倍率字段。'
Assert-True ($damagePlanText -notmatch 'TargetThing') `
    'DamagePlan 不得保留仅供风刃方向斩击使用的显式额外目标字段。'
Assert-True ($projectileText -notmatch 'extraTarget|extra\.TargetThing') `
    '投射物额外伤害执行不得保留风刃方向斩击的显式目标分支。'
Assert-True ($abilityText -match 'BdpAbilityTrionCostCommitter') 'Ability 宿主必须复用现有 Trion 扣费入口。'
Assert-True ($abilityText -match 'RangedAttackProtocol') 'Ability 宿主必须复用既有远程攻击协议。'
Assert-True ($abilityText -notmatch 'DefaultRangedAttackExecutor.*CanExecute.*Ability') `
    '不得放宽通用执行器而让所有 Ability 自动远程化。'
Assert-True (
    ($capabilityResolverText -match 'EntryType\s*==\s*ChipExpressionEntryTypeConfig\.Ability') -and
    ($capabilityResolverText -match 'IsExplicitWeaponMode\(entry\.WeaponMode\)') -and
    ($capabilityResolverText -match 'mode\s*==\s*VerbExpressionModeConfig\.Ranged')
) '显式声明 Ranged 的 Ability 条目必须保留远程武器模式，普通 Ability 不得被自动远程化。'
Assert-True ($interpreterText -match 'EntryType == ChipExpressionEntryTypeConfig\.Ability') `
    '独立 Ability 条目必须能从自身 AbilityDef 读取远程规格。'
Assert-True ($interpreterText -match 'WeaponMode == VerbExpressionModeConfig\.Ranged') `
    'AbilityDef 远程规格回退必须仅对显式远程 Ability 生效，不得触碰旧的非远程 Ability。'
Assert-True ($interpreterText -match 'FromAbilityDef') '独立 Ability 条目必须调用 AbilityDef 远程规格工厂。'
Assert-True ($revectorText -match 'IArrivalStageModule') '距离重定向必须只在 Arrival 阶段接管分段。'
Assert-True ($revectorText -match 'InitialSegmentTriggerRatio') '距离重定向必须使用首段触发比例。'
Assert-True ($revectorText -notmatch 'IHitStageModule') '距离重定向不得接管最终命中判定。'

Write-Output 'FujinCoreContractSmokeTests PASS'
