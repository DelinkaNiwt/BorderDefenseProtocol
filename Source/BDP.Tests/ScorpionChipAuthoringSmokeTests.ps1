# 毒蝎芯片定义契约冒烟测试。
# 毒蝎必须是弧月的独立功能模板，只改变近战连续攻击次数和内部间隔。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

function Get-FunctionDefNode
{
    param(
        [System.Xml.XmlDocument]$Document,
        [string]$DefName
    )

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName }) | Select-Object -First 1
}

function Get-NormalizedXml
{
    param($Node)

    if ($null -eq $Node)
    {
        return ""
    }

    if ($Node -is [System.Xml.XmlNode])
    {
        return ($Node.OuterXml -replace "\s", "")
    }

    return ([string]$Node -replace "\s", "")
}

function Assert-EqualValue
{
    param(
        $Actual,
        $Expected,
        [string]$Message
    )

    Assert-True ($Actual -eq $Expected) "$Message 实际值：$Actual，预期值：$Expected。"
}

$modRoot = Get-BdpModRoot
$presetPath = Join-Path $modRoot "1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml"

Assert-True (Test-Path -LiteralPath $presetPath) "缺少芯片功能模板定义：$presetPath"
Assert-True (Test-Path -LiteralPath $languagePath) "缺少简体中文语言包：$languagePath"

[xml]$presetDocument = Get-Utf8Text $presetPath
[xml]$languageDocument = Get-Utf8Text $languagePath
$kogetsu = Get-FunctionDefNode $presetDocument "BDP_ChipFunction_Kogetsu"
$scorpion = Get-FunctionDefNode $presetDocument "BDP_ChipFunction_Scorpion"
Assert-True ($null -ne $kogetsu) "缺少弧月基准功能模板。"
Assert-True ($null -ne $scorpion) "缺少毒蝎功能模板 BDP_ChipFunction_Scorpion。"

Assert-EqualValue $scorpion.label "蝎子" "蝎子玩家可见名称错误。"
Assert-EqualValue $scorpion.labelKey "BDP_ChipFunction_Scorpion" "毒蝎名称语言键错误。"
Assert-EqualValue $scorpion.descriptionKey "BDP_ChipFunction_Scorpion_Description" "毒蝎说明语言键错误。"

foreach ($field in @("profession", "additionalWorkAmount"))
{
    Assert-EqualValue $scorpion.$field $kogetsu.$field "毒蝎未复用弧月字段：$field。"
}

foreach ($section in @("costList", "config.Profile", "config.Loadout", "config.Trion", "config.ActivationRequirements"))
{
    $kogetsuNode = $kogetsu
    $scorpionNode = $scorpion
    foreach ($part in $section.Split('.'))
    {
        $kogetsuNode = $kogetsuNode.$part
        $scorpionNode = $scorpionNode.$part
    }

    Assert-EqualValue (Get-NormalizedXml $scorpionNode) (Get-NormalizedXml $kogetsuNode) "毒蝎未复用弧月结构：$section。"
}

$kogetsuEntry = @($kogetsu.config.Expression.Entries.li) | Select-Object -First 1
$scorpionEntry = @($scorpion.config.Expression.Entries.li) | Select-Object -First 1
Assert-True ($null -ne $kogetsuEntry -and $null -ne $scorpionEntry) "弧月或毒蝎缺少近战表达条目。"
Assert-EqualValue $scorpionEntry.Id "scorpion_primary" "毒蝎近战表达条目标识错误。"
Assert-EqualValue $scorpionEntry.EntryType "PrimaryVerb" "毒蝎表达类型错误。"
Assert-EqualValue $scorpionEntry.WeaponMode "Melee" "毒蝎表达武器模式错误。"
Assert-EqualValue $scorpionEntry.DisplayLabelKey "BDP_Expression_Scorpion" "毒蝎表达名称语言键错误。"
Assert-EqualValue $scorpionEntry.Presentation.VisualPresetDefName "BDP_Visual_Kogetsu" "毒蝎没有复用弧月视觉预设。"
Assert-EqualValue $scorpionEntry.Execution.HitCount "3" "毒蝎攻击次数必须为 3。"
Assert-EqualValue $scorpionEntry.Execution.HitIntervalTicks "5" "毒蝎攻击间隔必须为 5 tick。"

$kogetsuTool = @($kogetsuEntry.tools.li) | Select-Object -First 1
$scorpionTool = @($scorpionEntry.tools.li) | Select-Object -First 1
Assert-True ($null -ne $kogetsuTool -and $null -ne $scorpionTool) "弧月或毒蝎缺少近战 Tool。"
foreach ($field in @("capacities", "power", "cooldownTime", "armorPenetration"))
{
    Assert-EqualValue (Get-NormalizedXml $scorpionTool.$field) (Get-NormalizedXml $kogetsuTool.$field) "毒蝎 Tool 未复用弧月字段：$field。"
}
Assert-EqualValue $scorpionEntry.ToolLabelKeys.li "BDP_Tool_ScorpionBlade" "毒蝎 Tool 名称语言键错误。"

foreach ($key in @(
    "BDP_ChipFunction_Scorpion",
    "BDP_ChipFunction_Scorpion_Description",
    "BDP_Expression_Scorpion",
    "BDP_Tool_ScorpionBlade"))
{
    $value = $languageDocument.LanguageData.$key
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$value)) "缺少毒蝎语言键：$key。"
    Assert-True ([string]$value -notmatch "弧月") "毒蝎语言键不能显示弧月文案：$key。"
}

Write-Host "PASS: 毒蝎功能模板完整复用弧月，仅覆盖三击与 5 tick 间隔。"
