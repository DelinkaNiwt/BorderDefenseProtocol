# 蝎子+蚱蜢弹射砍击组合来源与中文名称冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

function Get-DefNode
{
    param(
        [System.Xml.XmlDocument]$Document,
        [string]$DefName
    )

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName }) | Select-Object -First 1
}

$modRoot = Get-BdpModRoot
$presetPath = Join-Path $modRoot "1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$comboPath = Join-Path $modRoot "1.6\Content\Defs\ComboDef\BounceSlash.xml"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml"

Assert-True (Test-Path -LiteralPath $presetPath) "缺少芯片功能模板定义。"
Assert-True (Test-Path -LiteralPath $comboPath) "缺少弹射砍击组合技定义。"
Assert-True (Test-Path -LiteralPath $languagePath) "缺少简体中文语言包。"

[xml]$presetDocument = Get-Utf8Text $presetPath
[xml]$comboDocument = Get-Utf8Text $comboPath
[xml]$languageDocument = Get-Utf8Text $languagePath

$scorpion = Get-DefNode $presetDocument "BDP_ChipFunction_Scorpion"
$combo = Get-DefNode $comboDocument "BDP_Combo_BounceSlash"
Assert-True ($null -ne $scorpion) "缺少蝎子功能模板。"
Assert-True ($null -ne $combo) "缺少弹射砍击组合技。"

# 蝎子与弹射砍击的玩家可见中文名应与语言键一致。
Assert-True ($scorpion.label -eq "蝎子") "蝎子功能模板中文名必须是蝎子。"
Assert-True ($scorpion.labelKey -eq "BDP_ChipFunction_Scorpion") "蝎子功能模板标识不得改变。"
Assert-True ([string]$languageDocument.LanguageData.BDP_ChipFunction_Scorpion -eq "蝎子") "蝎子芯片语言键中文名必须是蝎子。"
Assert-True ([string]$languageDocument.LanguageData.BDP_Expression_Scorpion -eq "蝎子") "蝎子表达语言键中文名必须是蝎子。"

# 弹射砍击当前由蚱蜢和蝎子组合，组合技标识按结果命名。
Assert-True ($combo.firstSourceKey -eq "BDP_ChipFunction_Grasshopper") "弹射砍击第一来源必须保持蚱蜢。"
Assert-True ($combo.secondSourceKey -eq "BDP_ChipFunction_Scorpion") "弹射砍击第二来源必须改为蝎子。"
Assert-True ($combo.secondSourceKey -ne "BDP_ChipFunction_Kogetsu") "弹射砍击不得继续匹配弧月。"

Write-Output 'ScorpionBounceSlashComboSmokeTests PASS'
