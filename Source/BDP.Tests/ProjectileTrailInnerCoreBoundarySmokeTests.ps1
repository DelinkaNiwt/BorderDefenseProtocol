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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

$modRoot = Join-Path $PSScriptRoot '..\..'
$coreRoot = Join-Path $modRoot 'Source\BDP\Core'
$contentRoot = Join-Path $modRoot 'Source\BDP.Content'
$xmlPath = Join-Path $modRoot '1.6\Content\Defs\RangedModuleDef\RangedDebuff.xml'

$appearancePath = Join-Path $coreRoot 'Projectiles\Visual\ProjectileVisualAppearanceOverrides.cs'
$planContributionPath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs'
$planPath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$stagePath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$projectilePath = Join-Path $coreRoot 'Projectiles\BdpProjectile.cs'
$configPath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffConfig.cs'
$modulePath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffModule.cs'
$snapshotPath = Join-Path $contentRoot 'Projectiles\BeamTrail\BeamTrailAppearanceSnapshot.cs'
$segmentPath = Join-Path $contentRoot 'Projectiles\BeamTrail\BeamTrailSegment.cs'
$mapComponentPath = Join-Path $contentRoot 'Projectiles\BeamTrail\BeamTrailMapComponent.cs'

foreach ($path in @(
        $appearancePath,
        $planContributionPath,
        $planPath,
        $stagePath,
        $projectilePath,
        $configPath,
        $modulePath,
        $snapshotPath,
        $segmentPath,
        $mapComponentPath,
        $xmlPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('缺少拖尾内芯实现文件：' + $path)
}

$appearanceText = Read-Source $appearancePath
$planContributionText = Read-Source $planContributionPath
$planText = Read-Source $planPath
$stageText = Read-Source $stagePath
$projectileText = Read-Source $projectilePath
$configText = Read-Source $configPath
$moduleText = Read-Source $modulePath
$snapshotText = Read-Source $snapshotPath
$segmentText = Read-Source $segmentPath
$mapComponentText = Read-Source $mapComponentPath
$xmlText = Read-Source $xmlPath

foreach ($field in @(
        'HasTrailCore',
        'TrailCoreColor',
        'TrailCoreWidthRatio',
        'TrailCoreOpacity')) {
    Assert-True ($appearanceText -match $field) ('Core 视觉覆盖缺少内芯字段：' + $field)
    Assert-True ($planContributionText -match $field) ('ProjectileInit 局部贡献缺少内芯字段：' + $field)
    Assert-True ($planText -match $field) ('ProjectileInit 计划缺少内芯字段：' + $field)
    Assert-True ($configText -match $field.Replace('TrailCore', 'ProjectileTrailCore')) ('Content 配置缺少内芯字段：' + $field)
    Assert-True ($snapshotText -match $field) ('拖尾快照缺少内芯字段：' + $field)
    Assert-True ($segmentText -match $field) ('拖尾线段缺少内芯字段：' + $field)
}

Assert-True ($stageText -match 'HasTrailCore' -and $stageText -match 'TrailCoreWidthRatio') 'ProjectileInit 阶段没有合并拖尾内芯字段。'
Assert-True ($projectileText -match 'TrailCoreOpacity') 'BdpProjectile 没有把冻结的拖尾内芯传给视觉宿主。'
Assert-True ($moduleText -match 'HasProjectileTrailCore' -and $moduleText -match 'ProjectileTrailCoreWidthRatio') '远程减益模块没有提交拖尾内芯配置。'
Assert-True ($snapshotText -match 'HasTrailCore' -and $snapshotText -match 'TrailCoreOpacity') 'BeamTrail 快照没有消费投射物拖尾内芯覆盖。'
Assert-True ($segmentText -match 'DrawCore' -and $segmentText -match 'TrailCoreWidthRatio') '拖尾线段没有独立绘制内芯的路径。'
Assert-True ($mapComponentText -match 'ShaderDatabase\.MoteGlow' -and $mapComponentText -match 'ShaderDatabase\.Transparent') '拖尾必须保留发光外层并为内芯使用透明材质。'
Assert-True ($mapComponentText -match 'DrawCore') '拖尾地图组件没有按两层顺序绘制内芯。'
Assert-True (
    ($mapComponentText -match 'outerMaterial\.renderQueue') -and
    ($mapComponentText -match 'MaterialPool\.MatFrom') -and
    ($mapComponentText -match 'renderQueue')
) '内芯必须使用高于外层发光材质的显式渲染队列。'

$leadShotMatch = [regex]::Match(
    $xmlText,
    '(?s)<defName>BDP_RangedDebuff_LeadWeight</defName>.*?</defaultConfig>')
Assert-True $leadShotMatch.Success '找不到铅块负重模块 XML 配置。'
$leadShotText = $leadShotMatch.Value
Assert-True ($leadShotText -match '<HasProjectileTrailCore>true</HasProjectileTrailCore>') '铅弹必须启用拖尾灰黑内芯。'
Assert-True ($leadShotText -match '<ProjectileTrailCoreColor>\(0\.12, 0\.12, 0\.12, 1\)</ProjectileTrailCoreColor>') '铅弹内芯必须使用灰黑色。'
Assert-True ($leadShotText -match '<ProjectileTrailCoreWidthRatio>0\.60</ProjectileTrailCoreWidthRatio>') '铅弹内芯必须加粗到外层的 60%。'
Assert-True ($leadShotText -notmatch '<HasProjectileTrailColor>true</HasProjectileTrailColor>') '铅弹不能再把原有发光外层整体改成灰黑色。'

Write-Output 'ProjectileTrailInnerCoreBoundarySmokeTests PASS'
