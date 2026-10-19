# BDP 独立高角射击模块契约冒烟测试。
# 该测试确认高角规则归属远程模块，旧 DefModExtension（定义扩展）耦合已经移除。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreRoot = Join-Path $modRoot "Source\BDP\Core"
$contentRoot = Join-Path $modRoot "Source\BDP.Content"
$modulePath = Join-Path $contentRoot "RangedModules\HighAngleShooting\HighAngleShootingModule.cs"
$configPath = Join-Path $contentRoot "RangedModules\HighAngleShooting\HighAngleShootingConfig.cs"
$targetingSourcePath = Join-Path $coreRoot "AttackExecution\AttackExecutionTargetingSource.cs"
$profilePath = Join-Path $coreRoot "Projectiles\Visual\ProjectileVisualTrajectoryProfile.cs"
$planPath = Join-Path $coreRoot "AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs"
$contributionPath = Join-Path $coreRoot "AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs"
$stagePath = Join-Path $coreRoot "AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs"
$projectilePath = Join-Path $coreRoot "Projectiles\BdpProjectile.cs"
$shootPath = Join-Path $coreRoot "Verbs\BdpVerb_Shoot.cs"
$projectilesDefPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Projectiles\Projectiles.xml"
$highAngleDefPath = Join-Path $modRoot "1.6\Content\Defs\RangedModuleDef\HighAngleShooting.xml"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\RangedModules.xml"
$houndDefPath = Join-Path $modRoot "1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$hornetDefPath = Join-Path $modRoot "1.6\Content\Defs\ComboDef\Hornet.xml"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

Assert-True (Test-Path -LiteralPath $modulePath) "缺少独立高角射击模块。"
Assert-True (Test-Path -LiteralPath $configPath) "缺少独立高角射击配置。"
Assert-True (Test-Path -LiteralPath $profilePath) "缺少中性投射物视觉轨迹参数承载。"

$oldExtensionPath = Join-Path $coreRoot "Projectiles\Visual\ProjectileVisualArcExtension.cs"
$oldUtilityPath = Join-Path $coreRoot "Projectiles\Visual\ProjectileVisualArcUtility.cs"
Assert-True (-not (Test-Path -LiteralPath $oldExtensionPath)) "旧高角 DefModExtension 仍然存在。"
Assert-True (-not (Test-Path -LiteralPath $oldUtilityPath)) "旧高角视觉工具仍然存在。"

$moduleSource = Get-Content -Raw -LiteralPath $modulePath -Encoding UTF8
$configSource = Get-Content -Raw -LiteralPath $configPath -Encoding UTF8
$targetingSource = Get-Content -Raw -LiteralPath $targetingSourcePath -Encoding UTF8
$showRejectReasonStart = $targetingSource.IndexOf("private void ShowRejectReason")
$showRejectReasonEnd = $targetingSource.IndexOf("/// <summary>", $showRejectReasonStart + 1)
$showRejectReasonSource = if ($showRejectReasonStart -ge 0 -and $showRejectReasonEnd -gt $showRejectReasonStart) {
    $targetingSource.Substring($showRejectReasonStart, $showRejectReasonEnd - $showRejectReasonStart)
} else {
    ""
}
$profileSource = Get-Content -Raw -LiteralPath $profilePath -Encoding UTF8
$planSource = Get-Content -Raw -LiteralPath $planPath -Encoding UTF8
$contributionSource = Get-Content -Raw -LiteralPath $contributionPath -Encoding UTF8
$stageSource = Get-Content -Raw -LiteralPath $stagePath -Encoding UTF8
$projectileSource = Get-Content -Raw -LiteralPath $projectilePath -Encoding UTF8
$shootSource = Get-Content -Raw -LiteralPath $shootPath -Encoding UTF8
$projectilesDef = Get-Content -Raw -LiteralPath $projectilesDefPath -Encoding UTF8
$highAngleDef = Get-Content -Raw -LiteralPath $highAngleDefPath -Encoding UTF8
$languageSource = Get-Content -Raw -LiteralPath $languagePath -Encoding UTF8
$houndDef = Get-Content -Raw -LiteralPath $houndDefPath -Encoding UTF8
$hornetDef = Get-Content -Raw -LiteralPath $hornetDefPath -Encoding UTF8

Assert-True ($moduleSource -match "IProjectileInitStageModule") "高角模块必须通过 ProjectileInit 阶段输出每发结果。"
Assert-True ($moduleSource -match "IConfirmStageModule") "高角模块必须能在 Confirm 阶段拒绝手动下单。"
Assert-True ($moduleSource -match "ITargetingStageModule") "高角模块必须在当前目标阶段拒绝不可用位置。"
Assert-True ($moduleSource -match "HasCurrentTargetLegalityOverride") "高角模块必须接入当前候选点合法性覆盖。"
Assert-True ($moduleSource -match "CurrentTargetIsLegal") "高角模块必须输出当前候选点合法性。"
Assert-True ($moduleSource -match "CurrentTargetRejectReason") "高角模块必须输出当前候选点拒绝原因。"
Assert-True ($moduleSource -match "GenSight\.LineOfSight") "高角模块必须按射击者与目标的可视性决定模式。"
Assert-True ($moduleSource -match "AllowTargetUnderThinRoof") "高角模块必须读取薄屋顶目标开关。"
Assert-True ($moduleSource -match "Roofed") "高角模块必须检查发射者所在格是否有屋顶。"
Assert-True ($moduleSource -match "isThickRoof") "高角模块必须使用原版厚屋顶标记判断目标。"
Assert-True ($moduleSource -match "Stop\.IsRequested") "高角模块必须在正式初始化阶段提交硬拒绝。"
Assert-True ($moduleSource -match "VisualTrajectoryProfile") "高角模块必须提交视觉轨迹参数。"
Assert-True ($moduleSource -match "OverrideProjectileDef") "高角模块必须能选择高角投射物变体。"
Assert-True ($moduleSource -notmatch "Homing|OperatorConsole|DirectTargetLineOfSight") "高角模块不得接管追踪或许可业务。"
Assert-True ($showRejectReasonSource -match "message\.Translate\(\)\.ToString\(\)") "通用目标适配层必须在显示确认拒绝原因前解析语言键。"
Assert-True ($targetingSource -match "CurrentTargetRejectReason\.Translate\(\)\.ToString\(\)") "通用目标适配层必须在显示当前候选点拒绝原因前解析语言键。"
Assert-True ($configSource -match "ArcHeight" -and $configSource -match "StartHeight" -and $configSource -match "EndHeight") "高角配置必须拥有基本绘制参数。"
Assert-True ($configSource -match "AllowTargetUnderThinRoof") "高角配置必须声明薄屋顶目标开关。"
Assert-True ($configSource -match "allowTargetUnderThinRoof") "高角配置必须持久化薄屋顶目标开关。"
Assert-True ($configSource -notmatch "DefModExtension|positionCurve|heightCurve|arcCurve") "高角配置不得退回旧 Def 扩展或多曲线耦合。"

Assert-True ($planSource -match "VisualTrajectoryProfile") "初始化计划必须承载视觉轨迹参数。"
Assert-True ($contributionSource -match "HasOverrideProjectileDef" -and $contributionSource -match "HasVisualTrajectoryProfile") "初始化贡献必须支持投射物覆盖和视觉轨迹覆盖。"
Assert-True ($stageSource -match "OverrideProjectileDef" -and $stageSource -match "VisualTrajectoryProfile") "初始化阶段服务必须合并模块贡献。"
Assert-True ($projectileSource -notmatch "ProjectileVisualArcExtension|ProjectileVisualArcUtility|GetModExtension<ProjectileVisual") "投射物宿主不得再读取高角 Def 扩展或旧高角工具。"
Assert-True ($projectileSource -match "VisualTrajectoryProfile") "投射物宿主必须只消费计划中的中性视觉轨迹参数。"
Assert-True ($profileSource -match "Vector3\.forward\s*\*\s*EvaluateOffset") "视觉弧线必须沿原版迫击炮使用的 z 轴偏移。"

$tryEmitStart = $shootSource.IndexOf("internal bool TryEmitPlan(ProjectileInitPlan plan)")
$tryCastStart = $shootSource.IndexOf("protected override bool TryCastShot()", $tryEmitStart)
$tryEmitSource = if ($tryEmitStart -ge 0 -and $tryCastStart -gt $tryEmitStart) {
    $shootSource.Substring($tryEmitStart, $tryCastStart - $tryEmitStart)
} else {
    ""
}
$projectileDefPosition = $tryEmitSource.IndexOf("ThingDef projectileDef = plan.ProjectileDef")
$shootLinePosition = $tryEmitSource.IndexOf("ShootLine resultingLine;")
Assert-True ($projectileDefPosition -ge 0 -and $projectileDefPosition -lt $shootLinePosition) "发射入口必须先解析本发投射物 Def，再决定是否需要直视线。"
Assert-True ($tryEmitSource -match "projectileDef\.projectile\s*!=\s*null\s*&&\s*projectileDef\.projectile\.flyOverhead") "发射入口必须识别原版 flyOverhead 投射物可绕过直视线硬拦截。"
Assert-True ($tryEmitSource -match "stopBurstWithoutLos\s*&&\s*!hasLine\s*&&\s*!allowsMissingShootLine") "高角计划必须只绕过原版 stopBurstWithoutLos 直视线硬拦截。"

Assert-True ($projectilesDef -match "BDP_Projectile_Tracking_Overhead") "缺少追踪弹高角变体。"
Assert-True ($projectilesDef -match "BDP_Projectile_Tracking_Overhead(?s).*?<flyOverhead>true</flyOverhead>") "高角追踪弹必须启用原版 flyOverhead。"
Assert-True ($projectilesDef -notmatch "AllowTargetUnderThinRoof") "薄屋顶开关必须归属高角模块配置，不得写入投射物 Def。"
Assert-True ($highAngleDef -match "<AllowTargetUnderThinRoof>false</AllowTargetUnderThinRoof>") "高角 Def 默认必须禁止薄屋顶目标。"
Assert-True ($languageSource -match "BDP_HighAngle_SourceUnderRoof") "缺少发射者屋顶拒绝文案。"
Assert-True ($languageSource -match "BDP_HighAngle_TargetUnderThickRoof") "缺少厚屋顶目标拒绝文案。"
Assert-True ($languageSource -match "BDP_HighAngle_TargetUnderThinRoof") "缺少薄屋顶目标拒绝文案。"
$trackingParent = [regex]::Match($projectilesDef, '(?s)<ThingDef ParentName="BaseBullet" Name="BDP_Projectile_Tracking_Base">.*?</ThingDef>')
Assert-True ($trackingParent.Success -and $trackingParent.Value -match '<defName>BDP_Projectile_Tracking</defName>') "追踪弹必须声明可供 XML 继承使用的独立父节点名称。"
Assert-True ($projectilesDef -match '<ThingDef ParentName="BDP_Projectile_Tracking_Base">') "高角追踪弹必须继承追踪弹的 XML 父节点。"
Assert-True ($houndDef -match "BDP_HighAngleShootingModule") "猎犬必须挂载独立高角射击模块。"
Assert-True ($hornetDef -match "BDP_HighAngleShootingModule") "黄蜂必须挂载独立高角射击模块。"

$boundaryLeak = Get-ChildItem -LiteralPath $coreRoot -Recurse -File -Filter "*.cs" |
    Select-String -Pattern "BDP\.(Content|Development)" |
    Select-Object -First 1
Assert-True ($null -eq $boundaryLeak) "高角模块不得让 BDP.Core 依赖 Content 或 Development。"

Write-Host "PASS: 独立高角射击模块契约符合预期。"
