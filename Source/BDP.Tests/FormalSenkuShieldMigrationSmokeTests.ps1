$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$mainRoot = $repoRoot
$devRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'

$chipDefs = Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'
$shieldChipDefs = Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\Shield\ThingDefs_Chip_EnergyShield.xml'
$comboDefs = Join-Path $mainRoot '1.6\Content\Defs\Pawn\Combos\SenkuKogetsu\ComboDefs_SenkuKogetsu.xml'
$abilityDefs = Join-Path $mainRoot '1.6\Content\Defs\Abilities\SenkuKogetsu\AbilityDefs_SenkuKogetsu.xml'
$waveDefs = Join-Path $mainRoot '1.6\Content\Defs\Things\Effects\SenkuKogetsu\ThingDefs_SenkuKogetsuWave.xml'
$visualDefs = Join-Path $mainRoot '1.6\Content\Defs\Pawn\Expressions\SenkuKogetsu\ExpressionVisualPresetDefs_SenkuKogetsu.xml'
$shieldDefs = Join-Path $mainRoot '1.6\Content\Defs\Health\Shield\HediffDefs_EnergyShield.xml'
$classificationDefs = Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\ChipClassificationDefs.xml'

foreach ($path in @($chipDefs, $shieldChipDefs, $comboDefs, $abilityDefs, $waveDefs, $visualDefs, $shieldDefs, $classificationDefs)) {
    Assert-True (Test-Path -LiteralPath $path) "主模组缺少正式迁移文件：$path"
}

$chipText = Get-Content -LiteralPath $chipDefs -Raw -Encoding utf8
$shieldChipText = Get-Content -LiteralPath $shieldChipDefs -Raw -Encoding utf8
$comboText = Get-Content -LiteralPath $comboDefs -Raw -Encoding utf8
$abilityText = Get-Content -LiteralPath $abilityDefs -Raw -Encoding utf8
$waveText = Get-Content -LiteralPath $waveDefs -Raw -Encoding utf8
$visualText = Get-Content -LiteralPath $visualDefs -Raw -Encoding utf8
$shieldText = Get-Content -LiteralPath $shieldDefs -Raw -Encoding utf8
$classificationText = Get-Content -LiteralPath $classificationDefs -Raw -Encoding utf8

Assert-True (($chipText -match '<defName>BDP_Chip_Kogetsu</defName>') -and
    ($chipText -match '<Category>BDP_ChipCategory_Weapon</Category>') -and
    ($chipText -match '<li>BDP_ChipTag_AttackerUse</li>') -and
    ($chipText -match '<li>BDP_ChipTag_Entity</li>')) '弧月正式画像不完整。'
Assert-True (($chipText -match '<defName>BDP_Chip_Senku</defName>') -and
    ($chipText -match '<Category>BDP_ChipCategory_Ability</Category>') -and
    ($chipText -match '<li>BDP_ChipTag_Offensive</li>') -and
    ($chipText -match '<li>BDP_ChipTag_KogetsuExclusive</li>') -and
    ($chipText -match '<li>BDP_ChipTag_EnergyForm</li>')) '旋空正式画像不完整。'
Assert-True (($shieldChipText -match '<defName>BDP_Chip_EnergyShield</defName>') -and
    ($shieldChipText -match '<Category>BDP_ChipCategory_Defense</Category>') -and
    ($shieldChipText -match '<DisplayLabel>能量护盾</DisplayLabel>')) '护盾正式画像或表达名称不完整。'
Assert-True (($shieldText -match '<label>能量护盾</label>') -and
    ($shieldText -match '<label>全防御能量护盾</label>')) '护盾状态阶段名称不完整。'
Assert-True (($comboText -match '<defName>BDP_Combo_SenkuKogetsu</defName>') -and
    ($abilityText -match '<defName>BDP_Ability_SenkuKogetsu</defName>') -and
    ($waveText -match '<label>旋空弧月</label>')) '旋空弧月正式结果名称不完整。'
Assert-True (($visualText -match '<defName>BDP_Visual_Kogetsu</defName>') -and
    ($visualText -match 'Things/Trigger/Chip/Kogetsu/kogetsu_handle')) '弧月正式视觉预设不完整。'
Assert-True (($classificationText -notmatch 'BDP_Dev_UnreviewedChip') -and
    ($classificationText -match 'BDP_ChipTag_EnergyForm')) '主模组分类定义仍残留候选分类或缺少正式标签。'

$devForbidden = @(
    '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_SenkuKogetsu.xml',
    '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChip_Shield.xml',
    '1.6\Defs\Pawn\Combos\Test\ComboDefs_TestSenkuKogetsu.xml',
    '1.6\Defs\Abilities\Expression\Test\AbilityDefs_TestSenkuKogetsu.xml',
    'Source\BDP.DevHarness\Shield',
    'Source\BDP.DevHarness\SenkuKogetsu'
)
foreach ($relativePath in $devForbidden) {
    $path = Join-Path $devRoot $relativePath
    if (Test-Path -LiteralPath $path -PathType Container) {
        $remainingFiles = @(Get-ChildItem -LiteralPath $path -File -Recurse)
        Assert-True ($remainingFiles.Count -eq 0) "DevHarness 仍保留已迁移内容：$relativePath"
    }
    else {
        Assert-True (-not (Test-Path -LiteralPath $path)) "DevHarness 仍保留已迁移内容：$relativePath"
    }
}

$oldCandidateVisualPath = Join-Path $devRoot '1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml'
if (Test-Path -LiteralPath $oldCandidateVisualPath) {
    $oldVisualText = Get-Content -LiteralPath $oldCandidateVisualPath -Raw -Encoding utf8
    Assert-True ($oldVisualText -notmatch 'BDP_TestVisual_Kogetsu') 'DevHarness 仍保留已迁移的弧月视觉预设。'
}

Write-Output 'FormalSenkuShieldMigrationSmokeTests PASS'
