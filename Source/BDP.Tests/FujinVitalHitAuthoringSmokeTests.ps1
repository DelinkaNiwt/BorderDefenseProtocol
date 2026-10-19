$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Find-Def {
    param([xml]$Document, [string]$DefName)

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

function Get-NodeText {
    param([System.Xml.XmlNode]$Node, [string]$XPath)

    $child = $Node.SelectSingleNode($XPath)
    if ($child -eq $null) {
        return ""
    }

    return $child.InnerText
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$presetXml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml') -Raw -Encoding utf8)
$fujin = Find-Def $presetXml 'BDP_ChipFunction_FujinRangedStrike'
$entityBlade = Find-Def $presetXml 'BDP_ChipFunction_Fujin'

Assert-True ($fujin -ne $null) '缺少风刃芯片功能定义。'
Assert-True ($entityBlade -ne $null) '缺少风刃·实体刀芯片功能定义。'

$fujinEntry = @($fujin.config.Expression.Entries.li | Where-Object { $_.Id -eq 'fujin_ranged_strike' })[0]
$entityEntry = @($entityBlade.config.Expression.Entries.li | Where-Object { $_.Id -eq 'fujin_primary' })[0]
$remoteModules = @($fujinEntry.RangedModules.li.moduleDef)
$extraDamageMount = @($fujinEntry.RangedModules.li | Where-Object { $_.moduleDef -eq 'BDP_RangedExtraDamageModule' })[0]

Assert-True ($remoteModules -contains 'BDP_VitalHitModule') '风刃远程入口必须挂载要害命中模块。'
Assert-True ($extraDamageMount -ne $null) '风刃远程入口必须挂载通用额外伤害模块。'
Assert-True (($extraDamageMount.config.Class) -eq 'BDP.Content.RangedModules.ExtraDamage.RangedExtraDamageConfig') '风刃必须在挂载处使用通用额外伤害配置类型。'
Assert-True ((Get-NodeText $extraDamageMount 'config/AmountMode') -eq 'HitPartMaxHealthPercent') '风刃必须在自身配置命中部件百分比模式。'
Assert-True ([float](Get-NodeText $extraDamageMount 'config/Ratio') -eq [float]0.10) '风刃必须在自身配置 10% 比例。'
Assert-True ((Get-NodeText $extraDamageMount 'config/DamageDef') -eq 'Cut') '风刃必须在自身配置切割伤害类型。'
Assert-True ((Get-NodeText $extraDamageMount 'config/HitPartResolutionMode') -eq 'InheritImpactHitPart') '风刃必须在自身配置继承主命中部位。'
Assert-True ((Get-NodeText $entityEntry 'HitPartPolicy/PolicyKey') -eq 'PreferVital') '风刃·实体刀必须使用 PreferVital。'
Assert-True ([float](Get-NodeText $entityEntry 'HitPartPolicy/Chance') -eq [float]1) '风刃·实体刀必须配置 100% 要害优先概率。'
Assert-True ((Get-NodeText $entityEntry 'HitPartPolicy') -notmatch 'FallbackToVanilla') '近战策略不应再声明冗余的 FallbackToVanilla。'

Write-Output 'FujinVitalHitAuthoringSmokeTests PASS'
