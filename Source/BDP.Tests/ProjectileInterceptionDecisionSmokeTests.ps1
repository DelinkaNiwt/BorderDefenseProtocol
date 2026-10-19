$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-Contains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    $content = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    if ($content -notmatch $Pattern) {
        throw "$Message | file=$Path | pattern=$Pattern"
    }
}

function Assert-NotExists {
    param([string]$Path, [string]$Message)
    if (Test-Path -LiteralPath $Path) {
        throw "$Message | path=$Path"
    }
}

$policyPath = Join-Path $projectRoot 'Source\BDP\Core\Projectiles\Interaction\ProjectileInteractionPolicy.cs'
$decisionPath = Join-Path $projectRoot 'Source\BDP\Core\Projectiles\Interaction\ProjectileInterceptionDecision.cs'
$segmentPath = Join-Path $projectRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs'
$shootPath = Join-Path $projectRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$abilityPath = Join-Path $projectRoot 'Source\BDP\Core\Abilities\BdpVerb_AbilityShoot.cs'
$floatingPath = Join-Path $projectRoot 'Source\BDP.Content\FloatingFlight\FloatingFlightIndirectAttackPermissionReceiver.cs'
$distanceRevectorPath = Join-Path $projectRoot 'Source\BDP.Content\RangedModules\DistanceRevector\DistanceRevectorModule.cs'
$vanillaPatchPath = Join-Path $projectRoot 'Source\BDP\Patches\Patch_Projectile_InterceptionDecision.cs'

if (-not (Test-Path -LiteralPath $decisionPath)) {
    throw "统一拦截判定器必须存在 | missing=$decisionPath"
}

Assert-Contains $policyPath 'AllowNonTargetPassThrough' '策略必须声明统一的非目标放行语义'
Assert-Contains $decisionPath 'ResolveIntendedLaunchHitFlags' '统一判定器必须提供原版 flags 适配入口'
Assert-Contains $decisionPath 'ShouldPassThroughNonTarget' '统一判定器必须提供 BDP 扫描判定入口'
Assert-Contains $segmentPath 'ProjectileInterceptionDecision\.ShouldPassThroughNonTarget' 'BDP 几何扫描必须调用统一判定器'
Assert-Contains $shootPath 'ProjectileInterceptionDecision\.ResolveIntendedLaunchHitFlags' '普通远程发射入口必须调用统一 flags 适配器'
Assert-Contains $abilityPath 'ProjectileInterceptionDecision\.ResolveIntendedLaunchHitFlags' 'Ability 远程发射入口必须调用统一 flags 适配器'
Assert-Contains $floatingPath 'AllowNonTargetPassThrough' '浮空许可必须声明统一非目标放行语义'
Assert-Contains $distanceRevectorPath 'AllowNonTargetPassThrough' '风刃距离重定向必须复用统一非目标放行语义'
Assert-NotExists $vanillaPatchPath '第一阶段不得新增原版 Projectile 拦截 Harmony 补丁'

Write-Output 'ProjectileInterceptionDecisionSmokeTests: PASS'
