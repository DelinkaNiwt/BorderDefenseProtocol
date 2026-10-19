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

function Get-ThingDef {
    param(
        [System.Xml.XmlDocument]$Document,
        [string]$DefName
    )

    $node = $Document.SelectSingleNode('/Defs/ThingDef[defName="' + $DefName + '"]')
    Assert-True ($null -ne $node) ('缺少触发体 ThingDef：' + $DefName)
    return $node
}

function Get-CompProperties {
    param(
        [System.Xml.XmlElement]$ThingDef
    )

    $node = $ThingDef.SelectSingleNode('./comps/li[@Class="BDP.Core.Trigger.CompProperties_TriggerBody"]')
    Assert-True ($null -ne $node) ('触发体缺少 CompProperties_TriggerBody：' + $ThingDef.defName)
    return $node
}

function Get-Value {
    param(
        [System.Xml.XmlNode]$Parent,
        [string]$Name
    )

    $node = $Parent.SelectSingleNode('./' + $Name)
    if ($null -ne $node) {
        return [string]$node.InnerText
    }

    return $null
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$triggerPath = Join-Path $modRoot '1.6\Content\Defs\ThingDef\Equipment\Trigger.xml'
$chipPath = Join-Path $modRoot '1.6\Content\Defs\ThingDef\Items\Chips.xml'
$functionPath = Join-Path $modRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$languagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ThingDef\BDP_Things.xml'
$neighborTexturePath = Join-Path $modRoot '1.6\Textures\Things\Equipment\Trigger\BDP_FujinTriggerBody.png'

Assert-True (Test-Path -LiteralPath $triggerPath -PathType Leaf) ('触发体定义文件不存在：' + $triggerPath)
Assert-True (Test-Path -LiteralPath $chipPath -PathType Leaf) ('动态芯片定义文件不存在：' + $chipPath)
Assert-True (Test-Path -LiteralPath $functionPath -PathType Leaf) ('芯片功能预设文件不存在：' + $functionPath)
Assert-True (Test-Path -LiteralPath $languagePath -PathType Leaf) ('触发体语言文件不存在：' + $languagePath)
Assert-True (Test-Path -LiteralPath $neighborTexturePath -PathType Leaf) ('近界测试触发体贴图不存在：' + $neighborTexturePath)

[xml]$triggerXml = Get-Content -LiteralPath $triggerPath -Raw -Encoding UTF8
[xml]$chipXml = Get-Content -LiteralPath $chipPath -Raw -Encoding UTF8
[xml]$functionXml = Get-Content -LiteralPath $functionPath -Raw -Encoding UTF8
[xml]$languageXml = Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8

$standard = Get-ThingDef $triggerXml 'BDP_TriggerBody_BorderStandard'
$borderSpecial = Get-ThingDef $triggerXml 'BDP_TriggerBody_BorderSpecial'
$neighborTest = Get-ThingDef $triggerXml 'BDP_TriggerBody_Fujin'

$standardComp = Get-CompProperties $standard
Assert-True ((Get-Value $standardComp 'mainSlotCount') -eq '4' -and
    (Get-Value $standardComp 'subSlotCount') -eq '4' -and
    (Get-Value $standardComp 'specialSlotCount') -eq '2') `
    '边境标准触发体必须保持 4/4/2 槽位。'

$borderComp = Get-CompProperties $borderSpecial
Assert-True ((Get-Value $borderComp 'triggerCategory') -eq 'BDP_TriggerCategory_Border' -and
    (Get-Value $borderComp 'loadoutControlMode') -eq 'PlayerConfigurable' -and
    (Get-Value $borderComp 'mainSlotCount') -eq '7' -and
    (Get-Value $borderComp 'subSlotCount') -eq '7' -and
    (Get-Value $borderComp 'specialSlotCount') -eq '2') `
    '边境特制触发体必须使用边境类别、可配置权限和 7/7/2 槽位。'
Assert-True ($null -ne $borderSpecial.SelectSingleNode('./modExtensions/li[@Class="BDP.Content.Trigger.UI.TriggerLoadoutPanelExtension"]')) `
    '边境特制触发体必须保留主副槽控制面板扩展。'

$neighborComp = Get-CompProperties $neighborTest
Assert-True ((Get-Value $neighborComp 'triggerCategory') -eq 'BDP_TriggerCategory_Black' -and
    (Get-Value $neighborComp 'loadoutControlMode') -eq 'PlayerNonConfigurable' -and
    (Get-Value $neighborComp 'mainSlotCount') -eq '1' -and
    (Get-Value $neighborComp 'subSlotCount') -eq '0' -and
    (Get-Value $neighborComp 'specialSlotCount') -eq '6') `
    '近界测试触发体必须使用黑类别、不可配置权限和 1/0/6 槽位。'
Assert-True ($null -eq $neighborTest.SelectSingleNode('./modExtensions/li[@Class="BDP.Content.Trigger.UI.TriggerLoadoutPanelExtension"]')) `
    '近界测试触发体不应提供玩家芯片控制面板扩展。'
Assert-True ((Get-Value $neighborTest 'graphicData/texPath') -eq 'Things/Equipment/Trigger/BDP_FujinTriggerBody') `
    '风刃触发体必须使用独立的校正后贴图。'

$fixedEntries = @($neighborComp.SelectNodes('./fixedLoadout/li'))
Assert-True ($fixedEntries.Count -eq 2) '风刃触发体必须只预装两枚特殊槽芯片。'
Assert-True ((Get-Value $fixedEntries[0] 'side') -eq 'Special' -and
    (Get-Value $fixedEntries[0] 'slotNumber') -eq '1' -and
    (Get-Value $fixedEntries[0] 'chipDef') -eq 'BDP_Chip_Custom' -and
    (Get-Value $fixedEntries[0] 'initializationKey') -eq 'BDP_ChipFunction_FujinRangedStrike') `
    '特殊槽 1 必须预装风刃·远程斩击功能预设。'
Assert-True ((Get-Value $fixedEntries[1] 'side') -eq 'Special' -and
    (Get-Value $fixedEntries[1] 'slotNumber') -eq '2' -and
    (Get-Value $fixedEntries[1] 'chipDef') -eq 'BDP_Chip_Custom' -and
    (Get-Value $fixedEntries[1] 'initializationKey') -eq 'BDP_ChipFunction_Fujin') `
    '特殊槽 2 必须预装风刃功能预设。'
Assert-True ($neighborComp.SelectNodes('./fixedLoadout/li[initializationKey="BDP_ChipFunction_EnergyShield_Auto" or initializationKey="BDP_ChipFunction_EmergencyEscape"]').Count -eq 0) `
    '风刃触发体不得预装自动护盾或紧急脱离。'

Assert-True ($null -ne $chipXml.SelectSingleNode('/Defs/*[defName="BDP_Chip_Custom"]')) `
    '预装动态芯片必须引用现有 BDP_Chip_Custom 实体芯片 Def。'
foreach ($functionDefName in @('BDP_ChipFunction_FujinRangedStrike', 'BDP_ChipFunction_Fujin')) {
    $functionNode = $functionXml.SelectSingleNode('/Defs/*[defName="' + $functionDefName + '"]')
    Assert-True ($null -ne $functionNode) ('缺少芯片功能预设：' + $functionDefName)
    Assert-True ((Get-Value $functionNode 'config/Loadout/SlotRegion') -eq 'Special' -and
        (Get-Value $functionNode 'config/Loadout/SlotOccupancy') -eq 'Single') `
        ('特殊预装功能必须声明为特殊槽单槽芯片：' + $functionDefName)
}
$entityBladeFunction = $functionXml.SelectSingleNode('/Defs/*[defName="BDP_ChipFunction_Fujin"]')
Assert-True ($null -ne $entityBladeFunction) '缺少芯片功能预设：BDP_ChipFunction_Fujin'
Assert-True ((Get-Value $entityBladeFunction 'config/Loadout/SlotRegion') -eq 'Special' -and
    (Get-Value $entityBladeFunction 'config/Loadout/SlotOccupancy') -eq 'Single') `
    '风刃必须声明为特殊槽单槽芯片。'
Assert-True ($null -ne $languageXml.LanguageData.SelectSingleNode('./BDP_TriggerBody_BorderSpecial.label')) `
    '语言包缺少边境特制触发体名称。'
Assert-True ($null -ne $languageXml.LanguageData.SelectSingleNode('./BDP_TriggerBody_BorderSpecial.description')) `
    '语言包缺少边境特制触发体说明。'
Assert-True ($null -ne $languageXml.LanguageData.SelectSingleNode('./BDP_TriggerBody_Fujin.label')) `
    '语言包缺少风刃触发体名称。'
Assert-True ($null -ne $languageXml.LanguageData.SelectSingleNode('./BDP_TriggerBody_Fujin.description')) `
    '语言包缺少风刃触发体说明。'

Write-Output 'TriggerBodyVariantsDefinitionSmokeTests PASS'
