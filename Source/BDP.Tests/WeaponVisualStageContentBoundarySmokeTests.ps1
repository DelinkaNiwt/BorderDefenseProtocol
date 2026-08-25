$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentVisualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$stageConfigPath = Join-Path $coreRoot 'Expressions\Config\ExpressionVisualStageOverrideConfig.cs'
$presetPath = Join-Path $coreRoot 'Expressions\Config\ExpressionVisualPresetDef.cs'
$stageEnumPath = Join-Path $coreRoot 'Trigger\Visual\WeaponVisualActionStage.cs'

Assert-True (Test-Path -LiteralPath $contentVisualPath) 'Formal expression visual content must remain in the main Content Visual.xml.'
Assert-True (Test-Path -LiteralPath $stageConfigPath) 'Core must expose the neutral stage override config.'
Assert-True (Test-Path -LiteralPath $presetPath) 'Core must expose optional StageVisuals on expression visual presets.'

$stageConfigText = Get-Content -LiteralPath $stageConfigPath -Raw -Encoding utf8
$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8
$stageEnumText = Get-Content -LiteralPath $stageEnumPath -Raw -Encoding utf8
$contentVisualText = Get-Content -LiteralPath $contentVisualPath -Raw -Encoding utf8
$contentVisualXml = [xml]$contentVisualText

Assert-True (
    ($stageConfigText -match 'WeaponVisualActionStage\s+Stage') -and
    ($stageConfigText -match 'bool\s+Visible\s*=\s*true') -and
    ($stageConfigText -match 'GraphicData\s+GraphicData') -and
    ($stageConfigText -match 'List<ExpressionVisualOverlayLayerConfig>\s+OverlayLayers') -and
    ($presetText -match 'List<ExpressionVisualStageOverrideConfig>\s+StageVisuals')
) 'Core must provide only the reusable optional stage-override capability.'

$coreBusinessSurface = $stageConfigText + "`n" + $presetText + "`n" + $stageEnumText
Assert-True (
    ($coreBusinessSurface -notmatch '<texPath>') -and
    ($coreBusinessSurface -notmatch 'Visible\s*=\s*false') -and
    ($coreBusinessSurface -notmatch 'Warmup[\s\S]{0,120}GraphicData') -and
    ($coreBusinessSurface -notmatch 'Firing[\s\S]{0,120}Visible')
) 'Core must not hard-code a concrete weapon texture or warmup/firing visibility policy.'

$presetNodes = @($contentVisualXml.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef')
Assert-True ($presetNodes.Count -gt 0) 'Existing formal visual presets must continue loading without mandatory StageVisuals migration.'

foreach ($presetNode in $presetNodes) {
    if ([string]$presetNode.Abstract -eq 'True') {
        continue
    }

    Assert-True (
        -not [string]::IsNullOrWhiteSpace([string]$presetNode.defName)
    ) 'Every existing formal visual preset must remain legal even when StageVisuals is absent.'
}

$stageBlocks = [regex]::Matches($contentVisualText, '(?s)<StageVisuals>.*?</StageVisuals>')
foreach ($stageBlock in $stageBlocks) {
    $entries = [regex]::Matches($stageBlock.Value, '(?s)(<!--.*?-->)\s*<li>\s*<Stage>[^<]+</Stage>')
    $stageCount = [regex]::Matches($stageBlock.Value, '<li>\s*<Stage>').Count
    Assert-True (
        $entries.Count -eq $stageCount
    ) 'Every future formal StageVisuals entry must carry an adjacent Chinese XML comment.'
}

$retiredModuleName = 'BorderDefenseProtocol.' + 'DevHarness'
$featureFiles = @(
    $stageConfigPath,
    $presetPath,
    $stageEnumPath,
    $contentVisualPath
)
foreach ($featureFile in $featureFiles) {
    $featureText = Get-Content -LiteralPath $featureFile -Raw -Encoding utf8
    Assert-True (
        $featureText -notmatch [regex]::Escape($retiredModuleName)
    ) 'Weapon-stage facilities and formal Content must not depend on the retired companion module.'
}

Write-Output 'WeaponVisualStageContentBoundarySmokeTests PASS'
