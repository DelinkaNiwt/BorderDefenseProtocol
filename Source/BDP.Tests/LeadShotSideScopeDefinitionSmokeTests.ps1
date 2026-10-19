# 铅弹双分支正式 Def 边界测试。

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

function Get-FunctionNode {
    param(
        [xml]$Document,
        [string]$DefName
    )

    return $Document.SelectSingleNode("/Defs/*[defName='$DefName']")
}

function Assert-FunctionBlock {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$ExpectedCategory,
        [string]$ExpectedScope,
        [string]$ExpectedEntryId,
        [string]$ExpectedPassiveIdentityKey,
        [string]$DisplayName,
        [string]$ExpectedLabelKey
    )

    Assert-True ($null -ne $Node) ($DisplayName + ' Def 必须存在。')
    $block = $Node.OuterXml
    Assert-True ($block -match '<Category>' + [regex]::Escape($ExpectedCategory) + '</Category>') `
        ($DisplayName + ' 的芯片分类不正确。')
    Assert-True ($block -match '<EntryType>Passive</EntryType>') `
        ($DisplayName + ' 的表达类型必须保持 Passive。')
    Assert-True ($block -match '<PassiveIdentityKey>' + [regex]::Escape($ExpectedPassiveIdentityKey) + '</PassiveIdentityKey>') `
        ($DisplayName + ' 的 PassiveIdentityKey 不正确。')
    Assert-True ($block -match '<Id>' + [regex]::Escape($ExpectedEntryId) + '</Id>') `
        ($DisplayName + ' 的表达 Id 不正确。')
    Assert-True ($block -match '<Scope>' + [regex]::Escape($ExpectedScope) + '</Scope>') `
        ($DisplayName + ' 的远程增强作用范围不正确。')
    Assert-True ($block -match '<moduleDef>BDP_RangedDebuffModule</moduleDef>') `
        ($DisplayName + ' 必须继续复用通用减益宿主并挂载负重包。')
    Assert-True ($block -match '<SlotRegion>MainSub</SlotRegion>') `
        ($DisplayName + ' 必须继续使用 MainSub 槽位区域。')
    Assert-True ($block -match '<labelKey>' + [regex]::Escape($ExpectedLabelKey) + '</labelKey>') `
        ($DisplayName + ' 缺少新的语言标签键。')
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$presetPath = Join-Path $repoRoot 'Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$gameplayPath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'

Assert-True (Test-Path -LiteralPath $presetPath -PathType Leaf) ('缺少正式芯片 Def：' + $presetPath)
Assert-True (Test-Path -LiteralPath $gameplayPath -PathType Leaf) ('缺少语言包：' + $gameplayPath)

[xml]$presetXml = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
$gameplayText = Get-Content -LiteralPath $gameplayPath -Raw -Encoding UTF8

$modifiedNode = Get-FunctionNode $presetXml 'BDP_ChipFunction_LeadShotModified'
$oppositeNode = Get-FunctionNode $presetXml 'BDP_ChipFunction_LeadShotOppositeSide'
Assert-FunctionBlock `
    $modifiedNode `
    'BDP_ChipCategory_Accessory' `
    'AllSides' `
    'lead_shot_modified_passive' `
    'BDP_LeadShotModified' `
    '铅弹·改' `
    'BDP_ChipFunction_LeadShotModified'
Assert-FunctionBlock `
    $oppositeNode `
    'BDP_ChipCategory_GeneralSkill' `
    'OppositeSide' `
    'lead_shot_opposite_side_passive' `
    'BDP_LeadShotOppositeSide' `
    '铅弹' `
    'BDP_ChipFunction_LeadShotOppositeSide'

Assert-True ($null -eq (Get-FunctionNode $presetXml 'BDP_ChipFunction_LeadShot')) `
    '旧铅弹 DefName 不得继续存在。'
Assert-True ($gameplayText -match 'BDP_ChipFunction_LeadShotModified') `
    '语言包必须包含铅弹·改的新语言键。'
Assert-True ($gameplayText -match 'BDP_ChipFunction_LeadShotOppositeSide') `
    '语言包必须包含铅弹的新语言键。'
$legacyLeadShotLanguageKey = 'BDP_ChipFunction_' + 'LeadShot'
Assert-True ($gameplayText -notmatch ([regex]::Escape($legacyLeadShotLanguageKey) + '>')) `
    '语言包不得继续使用旧铅弹语言键。'

Write-Output 'LeadShotSideScopeDefinitionSmokeTests PASS'
