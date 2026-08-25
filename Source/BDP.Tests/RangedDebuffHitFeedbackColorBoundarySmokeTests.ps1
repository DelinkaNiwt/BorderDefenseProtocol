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

$configPath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffConfig.cs'
$modulePath = Join-Path $contentRoot 'RangedModules\Debuff\RangedDebuffModule.cs'
$impactPlanPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Model\ImpactPlan.cs'
$impactContributionPath = Join-Path $coreRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'
$projectilePath = Join-Path $coreRoot 'Projectiles\BdpProjectile.cs'
$visualPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnRenderer_BdpHitFeedbackColor.cs'
$moduleXmlPath = Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedDebuff.xml'

foreach ($path in @($configPath, $modulePath, $impactPlanPath, $impactContributionPath, $projectilePath, $visualPatchPath, $moduleXmlPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('缺少命中反馈颜色实现文件：' + $path)
}

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
$moduleText = Get-Content -LiteralPath $modulePath -Raw -Encoding UTF8
$impactPlanText = Get-Content -LiteralPath $impactPlanPath -Raw -Encoding UTF8
$impactContributionText = Get-Content -LiteralPath $impactContributionPath -Raw -Encoding UTF8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$visualPatchText = Get-Content -LiteralPath $visualPatchPath -Raw -Encoding UTF8
$moduleXmlText = Get-Content -LiteralPath $moduleXmlPath -Raw -Encoding UTF8

Assert-True ($configText -match 'HasHitFeedbackColor' -and $configText -match 'HitFeedbackColor') '远程减益配置缺少可选命中反馈颜色接口。'
Assert-True ($moduleText -match 'HasHitFeedbackColor' -and $moduleText -match 'HitFeedbackColor') '远程减益模块没有读取可选命中反馈颜色。'
Assert-True ($impactPlanText -match 'HasHitFeedbackColor' -and $impactPlanText -match 'HitFeedbackColor') 'ImpactPlan 缺少中性命中反馈颜色策略。'
Assert-True ($impactContributionText -match 'HasHitFeedbackColor' -and $impactContributionText -match 'HitFeedbackColor') 'ImpactContribution 缺少中性命中反馈颜色策略。'
Assert-True ($projectileText -match 'ApplySuppressedHitFeedback\s*\(' -and $projectileText -match 'HitFeedbackColor') '投射物命中反馈没有消费颜色策略。'
Assert-True ($visualPatchText -match 'PawnRenderer' -and $visualPatchText -match 'OverrideMaterialIfNeeded' -and $visualPatchText -match 'GetDrawParms') '原版受击材质与颜色入口没有接线。'

$leadWeightBlock = [regex]::Match(
    $moduleXmlText,
    '(?s)<defName>BDP_RangedDebuff_LeadWeight</defName>.*?</BDP.Core.AttackExecution.BdpRangedAttackModuleDef>').Value
Assert-True ($leadWeightBlock -match '<HasHitFeedbackColor>true</HasHitFeedbackColor>') '铅块负重没有启用命中反馈颜色。'
Assert-True ($leadWeightBlock -match '<HitFeedbackColor>\(0\.12,\s*0\.12,\s*0\.12,\s*1\)</HitFeedbackColor>') '铅块负重没有配置灰黑色命中反馈。'

foreach ($defName in @('BDP_RangedDebuff_DirectNoDamage', 'BDP_RangedDebuff_AreaNoDamage')) {
    $block = [regex]::Match(
        $moduleXmlText,
        '(?s)<defName>' + $defName + '</defName>.*?</BDP.Core.AttackExecution.BdpRangedAttackModuleDef>').Value
    Assert-True ($block -notmatch '<HasHitFeedbackColor>true</HasHitFeedbackColor>') ($defName + ' 不应被强制启用灰黑色命中反馈。')
}

Write-Output 'RangedDebuffHitFeedbackColorBoundarySmokeTests PASS'
