$ErrorActionPreference = 'Stop'

$configPath = Join-Path $PSScriptRoot '..\BDP.Content\RangedModules\Debuff\RangedDebuffConfig.cs'
$modulePath = Join-Path $PSScriptRoot '..\BDP.Content\RangedModules\Debuff\RangedDebuffModule.cs'
$initContributionPath = Join-Path $PSScriptRoot '..\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs'
$visualBuilderPath = Join-Path $PSScriptRoot '..\BDP\Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$drawPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$xmlPath = Join-Path $PSScriptRoot '..\..\1.6\Content\Defs\RangedModuleDef\RangedDebuff.xml'

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
$moduleText = Get-Content -LiteralPath $modulePath -Raw -Encoding UTF8
$initContributionText = Get-Content -LiteralPath $initContributionPath -Raw -Encoding UTF8
$visualBuilderText = Get-Content -LiteralPath $visualBuilderPath -Raw -Encoding UTF8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding UTF8
$xmlText = Get-Content -LiteralPath $xmlPath -Raw -Encoding UTF8

if ($configText -notmatch 'ProjectileSpeedFactor' -and
    $configText -notmatch 'ProjectileSpeedMultiplier') {
    throw '减益模块缺少投射物速度倍率配置。'
}

if ($moduleText -notmatch 'InitialSpeedFactorMultiplier') {
    throw '减益模块没有把速度倍率接入现有 ProjectileInit（投射物初始化）设施。'
}

if ($initContributionText -notmatch 'InitialSpeedFactorMultiplier') {
    throw 'Core 投射物初始化贡献缺少速度倍率设施。'
}

if ($configText -notmatch 'WeaponVisual' -and
    $configText -notmatch 'VisualPreset') {
    throw '减益模块缺少可选的已激活武器贴图表现配置。'
}

if ($visualBuilderText -notmatch 'RangedModule' -and
    $visualBuilderText -notmatch 'VisualOverride') {
    throw '武器贴图覆写没有接入已有视觉投影构建层。'
}

if ($drawPatchText -notmatch 'DrawEquipmentAiming') {
    throw '必须保留现有 PawnRenderUtility.DrawEquipmentAiming 绘制入口。'
}

if ($xmlText -notmatch 'ProjectileSpeed' -and $xmlText -notmatch 'SpeedFactor') {
    throw '正式减益 XML 没有声明投射物速度配置字段。'
}

Write-Output 'RangedDebuffWeaponVisualAndSpeedBoundarySmokeTests PASS'
