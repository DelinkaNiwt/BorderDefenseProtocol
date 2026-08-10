$ErrorActionPreference = "Stop"

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
$modRoot = Split-Path -Parent $sourceRoot
$presetPath = Join-Path $modRoot '1.6\Content\Defs\CombatBody\TemporaryFrontPreset.xml'
$configPath = Join-Path $modRoot '1.6\Defs\CombatBodyDef\Config.xml'

Assert-True -Condition (Test-Path -LiteralPath $presetPath) -Message '缺少临时战斗体服装观察预设。'

[xml]$presetXml = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8
$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8
$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8

Assert-True -Condition ($presetText -match '<BDP\.Core\.CombatBody\.CombatBodyFrontPresetDef>') -Message '临时文件必须只声明正式前台预设 Def。'
Assert-True -Condition ($presetText -match '<defName>BDP_TemporaryCombatBodyObservationPreset</defName>') -Message '临时预设 DefName 与全局配置不一致。'
Assert-True -Condition ($configText -match '<frontPresetDefName>BDP_TemporaryCombatBodyObservationPreset</frontPresetDefName>') -Message '全局配置必须指向临时观察预设。'

$expectedApparel = @(
    'Apparel_FlakPants',
    'Apparel_FlakVest',
    'Apparel_FlakJacket',
    'Apparel_PowerArmorHelmet'
)

foreach ($apparelDefName in $expectedApparel) {
    Assert-True -Condition ($presetText -match "<li>$apparelDefName</li>") -Message "临时预设缺少原版衣物：$apparelDefName"
}

Assert-True -Condition ($presetText -notmatch '<ThingDef') -Message '临时预设不得复制或修改原版衣物 ThingDef。'

Write-Output 'CombatBodyTemporaryFrontPreset PASS'
