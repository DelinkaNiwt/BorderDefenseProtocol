$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-Contains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) { throw "$Message | file=$Path | pattern=$Pattern" }
}

function Assert-Exists {
    param([string]$Path, [string]$Message)
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Message | missing=$Path" }
}

$formalExpressionResult = Join-Path $projectRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$indirectContext = Join-Path $projectRoot 'Source\BDP\Core\Permissions\IndirectRangedAttackPermissionContext.cs'
$indirectGrant = Join-Path $projectRoot 'Source\BDP\Core\Permissions\IndirectRangedAttackPermissionGrant.cs'
$indirectResolver = Join-Path $projectRoot 'Source\BDP\Core\Permissions\IIndirectRangedAttackPermissionResolver.cs'
$indirectRegistry = Join-Path $projectRoot 'Source\BDP\Core\Permissions\IndirectRangedAttackPermissionResolverRegistry.cs'
$indirectAccess = Join-Path $projectRoot 'Source\BDP\Core\Permissions\IndirectRangedAttackPermissionAccess.cs'
$targetingSource = Join-Path $projectRoot 'Source\BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$abilityVerb = Join-Path $projectRoot 'Source\BDP\Core\Abilities\BdpVerb_AbilityShoot.cs'
$formalHostVerb = Join-Path $projectRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$dualStages = Join-Path $projectRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
$projectileInit = Join-Path $projectRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$consoleReceiver = Join-Path $projectRoot 'Source\BDP.Content\OperatorConsole\DirectTargetLineOfSightPermissionReceiver.cs'
$floatingReceiver = Join-Path $projectRoot 'Source\BDP.Content\FloatingFlight\FloatingFlightIndirectAttackPermissionReceiver.cs'
$bootstrap = Join-Path $projectRoot 'Source\BDP.Content\ContentBootstrap.cs'
$shootVerb = Join-Path $projectRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'

Assert-Contains $formalExpressionResult 'SupportsIndirectTargeting' '正式远程表达必须暴露通用间接攻击能力'
Assert-Exists $indirectContext '必须存在间接攻击许可上下文'
Assert-Exists $indirectGrant '必须存在间接攻击许可结果'
Assert-Exists $indirectResolver '必须存在间接攻击许可解析器接口'
Assert-Exists $indirectRegistry '必须存在间接攻击许可注册表'
Assert-Exists $indirectAccess '必须存在间接攻击许可访问入口'
Assert-Contains $indirectContext 'LocalTargetInfo' '许可上下文必须携带具体目标'
Assert-Contains $indirectContext 'TargetIsRoofed' '许可上下文必须携带目标屋顶事实'
Assert-Contains $indirectGrant 'AllowRoofedTargets' '许可结果必须声明屋顶后置条件'
Assert-Contains $indirectGrant 'ProjectileInteractionPolicy' '许可结果必须能携带投射物交互策略'
Assert-Contains $indirectRegistry 'TargetIsRoofed[\s\S]*AllowRoofedTargets' '许可注册表必须统一执行屋顶后置条件'
Assert-Contains $targetingSource 'ShouldRelaxDirectTargetLineOfSight' '目标选择必须使用目标感知直射许可入口'
Assert-Contains $abilityVerb 'ShouldRelaxDirectTargetLineOfSight[\s\S]*targ' 'Ability 宿主必须使用具体目标查询许可'
Assert-Contains $formalHostVerb 'ShouldRelaxDirectTargetLineOfSight[\s\S]*targ' 'FormalHost 宿主必须使用具体目标查询许可'
Assert-Contains $dualStages 'semanticTarget' 'dual 执行入口必须使用具体目标查询许可'
Assert-Contains $projectileInit 'IndirectRangedAttackPermissionAccess' 'ProjectileInit 必须复用间接攻击许可'
Assert-Contains $projectileInit 'AllowNonTargetPassThrough' 'ProjectileInit 必须能合并统一非目标放行策略'
Assert-Contains $consoleReceiver 'IIndirectRangedAttackPermissionResolver' '控制台必须接入通用许可协议'
Assert-Exists $floatingReceiver '浮空必须有独立的 Content 许可接收器'
Assert-Contains $floatingReceiver 'SelfActive' '浮空接收器必须读取 Pawn 自身浮空许可'
Assert-Contains $floatingReceiver 'AllowRoofedTargets = false' '浮空许可不得放行屋顶下目标'
Assert-Contains $floatingReceiver 'AllowNonTargetPassThrough' '浮空许可必须声明统一非目标放行策略'
Assert-Contains $bootstrap 'IndirectRangedAttackPermissionResolverRegistry.Register' 'Content 启动必须注册通用许可接收器'

$shootVerbContent = Get-Content -LiteralPath $shootVerb -Raw
$emitStart = $shootVerbContent.IndexOf('internal bool TryEmitPlan')
$emitEnd = $shootVerbContent.IndexOf('protected override bool TryCastShot', $emitStart)
if ($emitStart -lt 0 -or $emitEnd -le $emitStart) {
    throw "无法定位 TryEmitPlan 发射阶段 | file=$shootVerb"
}
$emitBody = $shootVerbContent.Substring($emitStart, $emitEnd - $emitStart)
if ($emitBody -notmatch 'IndirectRangedAttackPermissionAccess\.TryResolve') {
    throw "实际发射阶段必须复用目标感知间接攻击许可 | file=$shootVerb"
}
if ($emitBody -notmatch 'new ShootLine\(caster\.Position, navigationTarget\.Cell\)') {
    throw "非直视许可通过后必须为原版 Launch 构造合成射击线 | file=$shootVerb"
}

Write-Output 'RangedIndirectAttackPermissionSmokeTests: PASS'
