$ErrorActionPreference = 'Stop'

$modRoot = Join-Path $PSScriptRoot '..\..'
$contentRoot = Join-Path $modRoot '1.6\Content\Defs'
$modulePath = Join-Path $contentRoot 'RangedModuleDef\RangedDebuff.xml'
$hediffPath = Join-Path $contentRoot 'HediffDef\RangedDebuff.xml'
$presetPath = Join-Path $contentRoot 'ChipActionPresetDef\Presets.xml'

foreach ($path in @($modulePath, $hediffPath, $presetPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('缺少正式 XML：' + $path)
    }
    [xml](Get-Content -LiteralPath $path -Raw -Encoding UTF8) | Out-Null
}

$moduleText = Get-Content -LiteralPath $modulePath -Raw -Encoding UTF8
$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8

foreach ($defName in @('BDP_RangedDebuff_DirectNoDamage', 'BDP_RangedDebuff_AreaNoDamage', 'BDP_RangedDebuff_LeadWeight')) {
    if ($moduleText -notmatch ('<defName>' + $defName + '</defName>')) {
        throw ('缺少远程减益模块 Def：' + $defName)
    }
}

foreach ($field in @('TargetScope', 'TargetFilter', 'DamageSuppression', 'BypassProjectileInterceptors', 'BypassRegisteredDamageShields')) {
    if ($moduleText -notmatch ('<' + $field + '>')) {
        throw ('远程减益模块缺少配置字段：' + $field)
    }
}

if (($presetText -notmatch '<defName>BDP_Preset_LeadShot</defName>') -or
    ($presetText -notmatch '<RangedModuleAugmentations>')) {
    throw '铅弹被动芯片没有声明开放式远程增强。'
}

$leadShotBlock = [regex]::Match(
    $presetText,
    '(?s)<defName>BDP_Preset_LeadShot</defName>.*?</BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef>').Value
if ($leadShotBlock -notmatch '<SlotRegion>MainSub</SlotRegion>') {
    throw '铅弹必须占用主槽或副槽，不得占用特殊槽。'
}

foreach ($forbidden in @('BDP_Preset_Hound', 'BDP_GunClass_', '<TargetChip', '<TargetResult', '<TargetSlot')) {
    if ($leadShotBlock.Contains($forbidden)) {
        throw ('铅弹声明了不应存在的目标绑定：' + $forbidden)
    }
}

if ($leadShotBlock -notmatch '<DisplayNamePrefixMode>SourceExpressionLabel</DisplayNamePrefixMode>') {
    throw '铅弹没有声明动态名称前缀策略。'
}

Write-Output 'RangedDebuffXmlBoundarySmokeTests PASS'
