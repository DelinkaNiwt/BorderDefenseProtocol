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
$contractsPath = Join-Path $coreRoot 'Projectiles\Visual\ProjectileVisualAttachmentContracts.cs'
$hostPath = Join-Path $coreRoot 'Projectiles\Visual\ProjectileVisualAttachmentHost.cs'
$projectilePath = Join-Path $coreRoot 'Projectiles\BdpProjectile.cs'
$planContributionPath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs'
$planPath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$stagePath = Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$configPath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffConfig.cs'
$modulePath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffModule.cs'
$extensionPath = Join-Path $contentRoot 'Projectiles\BeamTrail\BeamTrailExtension.cs'
$snapshotPath = Join-Path $contentRoot 'Projectiles\BeamTrail\BeamTrailAppearanceSnapshot.cs'

foreach ($path in @(
        $appearancePath,
        $contractsPath,
        $hostPath,
        $projectilePath,
        $planContributionPath,
        $planPath,
        $stagePath,
        $configPath,
        $modulePath,
        $extensionPath,
        $snapshotPath,
        $xmlPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('缺少拖尾颜色实现文件：' + $path)
}

$appearanceText = Read-Source $appearancePath
$contractsText = Read-Source $contractsPath
$hostText = Read-Source $hostPath
$projectileText = Read-Source $projectilePath
$planContributionText = Read-Source $planContributionPath
$planText = Read-Source $planPath
$stageText = Read-Source $stagePath
$configText = Read-Source $configPath
$moduleText = Read-Source $modulePath
$extensionText = Read-Source $extensionPath
$snapshotText = Read-Source $snapshotPath
$xmlText = Read-Source $xmlPath

Assert-True (
    ($appearanceText -match 'class\s+ProjectileVisualAppearanceOverrides') -and
    ($appearanceText -match 'HasTrailColor') -and
    ($appearanceText -match 'TrailColor')
) 'Core 必须提供中性的投射物视觉外观覆盖。'

Assert-True ($appearanceText -notmatch 'BeamTrail|LeadShot|RangedDebuff') 'Core 视觉覆盖类型不得依赖 Content 业务名称。'

Assert-True (
    ($contractsText -match 'CreateAttachment\(\s*ProjectileVisualAppearanceOverrides') -and
    ($hostText -match 'CreateAttachment\(.*visualAppearanceOverrides')
) '视觉提供器必须接收可选的中性外观覆盖。'

Assert-True (
    ($planContributionText -match 'HasTrailColorOverride') -and
    ($planContributionText -match 'TrailColorOverride') -and
    ($stageText -match 'HasTrailColorOverride') -and
    ($stageText -match 'TrailColorOverride') -and
    ($planText -match 'HasTrailColorOverride') -and
    ($planText -match 'TrailColorOverride') -and
    ($planText -match 'Scribe_Values\.Look')
) 'ProjectileInit 必须承载、合并并保存拖尾颜色覆盖。'

Assert-True (
    ($projectileText -match 'VisualAppearanceOverrides') -and
    ($hostText -match 'VisualAppearanceOverrides')
) 'BdpProjectile 必须把冻结的视觉覆盖传给视觉宿主。'

Assert-True (
    ($configText -match 'HasProjectileTrailColor') -and
    ($configText -match 'ProjectileTrailColor') -and
    ($moduleText -match 'HasProjectileTrailColor') -and
    ($moduleText -match 'ProjectileTrailColor') -and
    ($moduleText -match 'HasTrailColorOverride')
) '远程减益模块必须在配置和 ProjectileInit 提交拖尾颜色需求。'

Assert-True (
    ($extensionText -match 'ProjectileVisualAppearanceOverrides') -and
    ($snapshotText -match 'ProjectileVisualAppearanceOverrides') -and
    ($snapshotText -match 'TrailColor')
) 'BeamTrail 必须消费中性外观覆盖，而不是让 Core 认识 BeamTrail。'

Assert-True (
    ($xmlText -match '<HasProjectileTrailColor>') -or
    ($configText -match 'HasProjectileTrailColor')
) '远程减益配置仍必须保留可选的拖尾颜色覆盖接口。'

Assert-True (
    ($xmlText -match '<HasProjectileTrailCore>true</HasProjectileTrailCore>') -and
    ($xmlText -match '<ProjectileTrailCoreColor>\(0\.12, 0\.12, 0\.12, 1\)</ProjectileTrailCoreColor>')
) '铅弹必须改用独立的灰黑色拖尾内芯，而不是覆盖发光外层。'

Write-Output 'ProjectileTrailColorOverrideBoundarySmokeTests PASS'
