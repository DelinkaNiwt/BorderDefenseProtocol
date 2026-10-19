$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$presetPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$developmentPath = Join-Path $repoRoot '1.6\Development\Defs\ChipManufacturingPresetDef\ExactBodyPartRemoval.xml'
$obsoleteFeatures = @('BDP_Feature_Stealth', 'BDP_Feature_AttackModifier', 'BDP_Feature_ComboSupport')
$noIndependentFeatureDefinitions = @(
    'BDP_ChipFunction_Thruster',
    'BDP_ChipFunction_Senku',
    'BDP_ChipFunction_LeadShotModified',
    'BDP_ChipFunction_LeadShotOppositeSide'
)

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Get-Elements($node, [string]$name) {
    if ($null -eq $node) { return @() }
    return @($node.ChildNodes | Where-Object { $_.NodeType -eq 'Element' -and ($name -eq '*' -or $_.LocalName -eq $name) })
}

function Read-Definitions([string]$path) {
    [xml]$document = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    return @((Get-Elements $document.Defs '*'))
}

function Assert-Sources($owner, [string]$path, [System.Collections.Generic.HashSet[string]]$entryIds) {
    $sourceIds = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($source in (Get-Elements $owner.Sources 'li')) {
        $sourceId = [string]$source.Id
        Assert-True (-not [string]::IsNullOrWhiteSpace($sourceId)) "$path 存在空来源 Id。"
        Assert-True ($sourceIds.Add($sourceId)) "$path 来源 Id 重复：$sourceId"

        $references = @()
        if (-not [string]::IsNullOrWhiteSpace([string]$source.EntryId)) { $references += [string]$source.EntryId }
        foreach ($entryId in (Get-Elements $source.EntryIds 'li')) {
            if (-not [string]::IsNullOrWhiteSpace([string]$entryId.InnerText)) { $references += [string]$entryId.InnerText }
        }

        $sourceClass = [string]$source.GetAttribute('Class')
        if ($sourceClass -like 'BDP.Content.Information.Features.Sources.*' -and $references.Count -eq 0) {
            Assert-True $false "$path 的 BDP 标准来源缺少表达条目定位。"
        }
        foreach ($entryId in $references) {
            Assert-True ($entryIds.Contains($entryId)) "$path 引用了不存在的表达条目：$entryId"
        }
    }
}

function Assert-Subgroups($owner, [string]$path, [System.Collections.Generic.HashSet[string]]$entryIds) {
    $ids = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($subgroup in (Get-Elements $owner.Subgroups 'li')) {
        $id = [string]$subgroup.Id
        Assert-True (-not [string]::IsNullOrWhiteSpace($id)) "$path 存在空子组 Id。"
        Assert-True ($ids.Add($id)) "$path 子组 Id 重复：$id"
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$subgroup.Group)) "$path/$id 缺少 Group Def。"
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$subgroup.LabelKey)) "$path/$id 缺少可读标签键。"
        Assert-Sources $subgroup "$path/subgroups/$id" $entryIds
        Assert-True ((Get-Elements $subgroup.Children 'li').Count -eq 0) "$path/$id 子组不得包含子功能。"
        Assert-True ((Get-Elements $subgroup.Subgroups 'li').Count -eq 0) "$path/$id 子组不得继续嵌套。"
    }
}

function Assert-FeatureTree($featuresOwner, [string]$path, [System.Collections.Generic.HashSet[string]]$entryIds, [string[]]$scope) {
    $features = Get-Elements $featuresOwner 'li'
    if ($features.Count -eq 0) {
        Assert-True ($noIndependentFeatureDefinitions -contains $scope[0]) "$path 缺少 Features 声明。"
        return
    }

    $featureIds = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($feature in $features) {
        $id = [string]$feature.Id
        Assert-True (-not [string]::IsNullOrWhiteSpace($id)) "$path 存在空功能 Id。"
        Assert-True ($featureIds.Add($id)) "$path 功能 Id 重复：$id"
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$feature.Feature)) "$path/$id 缺少 Feature Def。"
        $allowedFields = @('Id', 'Feature', 'LabelKey', 'Sources', 'Groups', 'Subgroups', 'Children')
        foreach ($field in (Get-Elements $feature '*')) {
            Assert-True ($allowedFields -contains $field.LocalName) "$path/$id 不得内嵌实例值字段 $($field.LocalName)。"
        }

        Assert-Sources $feature "$path/$id" $entryIds
        Assert-Subgroups $feature "$path/$id" $entryIds

        $groupIds = New-Object 'System.Collections.Generic.HashSet[string]'
        foreach ($group in (Get-Elements $feature.Groups 'li')) {
            $groupId = [string]$group.Id
            Assert-True (-not [string]::IsNullOrWhiteSpace($groupId)) "$path/$id 存在空信息组 Id。"
            Assert-True ($groupIds.Add($groupId)) "$path/$id 信息组 Id 重复：$groupId"
            Assert-True (-not [string]::IsNullOrWhiteSpace([string]$group.Group)) "$path/$id/$groupId 缺少 Group Def。"
            Assert-Sources $group "$path/$id/groups/$groupId" $entryIds
            Assert-Subgroups $group "$path/$id/groups/$groupId" $entryIds
            Assert-True ((Get-Elements $group.Children 'li').Count -eq 0) "$path/$id/$groupId 信息组不得包含子功能。"
        }

        foreach ($child in (Get-Elements $feature.Children 'li')) {
            [xml]$childDocument = "<Features>$($child.OuterXml)</Features>"
            Assert-FeatureTree $childDocument.DocumentElement "$path/$id/children" $entryIds $scope
        }
    }
}

function Assert-Definition($definition, [string]$scopeName) {
    $defName = [string]$definition.defName
    Assert-True ($null -ne $definition.config.Features) "$scopeName/$defName 缺少 Features 容器。"
    $entryIds = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($entry in (Get-Elements $definition.config.Expression.Entries 'li')) {
        [void]$entryIds.Add([string]$entry.Id)
    }
    Assert-FeatureTree $definition.config.Features "$scopeName/$defName/features" $entryIds @($defName)
}

$formalDefinitions = @(Read-Definitions $presetPath)
$developmentDefinitions = @(Read-Definitions $developmentPath)
Assert-True ($formalDefinitions.Count -ge 20) "正式制造预设迁移覆盖不足，当前仅 $($formalDefinitions.Count) 项。"

foreach ($definition in $formalDefinitions) { Assert-Definition $definition 'Content' }
foreach ($definition in $developmentDefinitions) { Assert-Definition $definition 'Development' }

$allXml = (Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8) + (Get-Content -LiteralPath $developmentPath -Raw -Encoding UTF8)
foreach ($obsoleteFeature in $obsoleteFeatures) {
    Assert-True (-not $allXml.Contains($obsoleteFeature)) "芯片或 Development 原型仍引用旧语义：$obsoleteFeature"
}

Write-Host "ChipFeatureMigrationCoverage PASS: formal=$($formalDefinitions.Count) development=$($developmentDefinitions.Count)"
