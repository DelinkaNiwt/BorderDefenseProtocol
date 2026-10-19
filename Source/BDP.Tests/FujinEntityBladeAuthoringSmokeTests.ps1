# 风刃·实体刀芯片定义与视觉配置冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

# 查找指定 Def，避免测试依赖 XML 中的顺序。
function Find-Def {
    param(
        [xml]$Document,
        [string]$DefName
    )

    return $Document.SelectNodes("/Defs/*") |
        Where-Object { $_.defName -eq $DefName } |
        Select-Object -First 1
}

# 读取 XML 节点文本；缺失节点返回空字符串，便于断言真实缺口。
function Get-NodeText {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$XPath
    )

    $child = $Node.SelectSingleNode($XPath)
    if ($null -eq $child) {
        return ""
    }

    return $child.InnerText
}

$modRoot = Get-BdpModRoot
$presetPath = Join-Path $modRoot "1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$visualPath = Join-Path $modRoot "1.6\Content\Defs\ExpressionDef\Visual.xml"
$languageRoot = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)"

[xml]$presetXml = Get-Utf8Text $presetPath
[xml]$visualXml = Get-Utf8Text $visualPath
$languageText = (Get-ChildItem -LiteralPath $languageRoot -Recurse -Filter "*.xml" | ForEach-Object {
    Get-Utf8Text $_.FullName
}) -join "`n"

$kogetsu = Find-Def $presetXml "BDP_ChipFunction_Kogetsu"
$entityBlade = Find-Def $presetXml "BDP_ChipFunction_Fujin"
$entityVisual = Find-Def $visualXml "BDP_Visual_Fujin"

Assert-True ($null -ne $kogetsu) "缺少弧月基准功能定义。"
Assert-True ($null -ne $entityBlade) "缺少风刃功能定义。"
Assert-True ($null -ne $entityVisual) "缺少风刃视觉预设。"

# 新芯片的制造台隐藏与特殊槽边界。
Assert-True ((Get-NodeText $entityBlade "showInManufacturing") -eq "false") `
    "风刃·实体刀必须从芯片制造台功能列表隐藏。"
Assert-True ((Get-NodeText $entityBlade "config/Loadout/SlotRegion") -eq "Special") `
    "风刃·实体刀必须使用特殊槽。"
Assert-True ((Get-NodeText $entityBlade "config/Loadout/SlotOccupancy") -eq "Single") `
    "风刃必须占用单个特殊槽。"
Assert-True ((Get-NodeText $entityBlade "config/Loadout/ActivationDelayTicks") -eq "0") `
    "风刃激活延迟必须为 0。"
Assert-True ((Get-NodeText $entityBlade "config/Loadout/DeactivationDelayTicks") -eq "0") `
    "风刃停用延迟必须为 0。"

# 弧月的功能属性必须原样复刻。
$samePaths = @(
    "profession",
    "costList/Silver",
    "costList/ComponentIndustrial",
    "additionalWorkAmount",
    "config/Profile/Category",
    "config/Profile/Tags/li",
    "config/Trion/CapacityCost",
    "config/Trion/ActivationCost",
    "config/ActivationRequirements/li/Minimum",
    "config/Expression/Entries/li/WeaponMode",
    "config/Expression/Entries/li/tools/li/capacities/li",
    "config/Expression/Entries/li/tools/li/cooldownTime"
)
foreach ($path in $samePaths) {
    Assert-True ((Get-NodeText $entityBlade $path) -eq (Get-NodeText $kogetsu $path)) `
        "风刃·实体刀未复刻弧月字段：$path"
}

Assert-True ((Get-NodeText $entityBlade "config/Expression/Entries/li/tools/li/power") -eq "30") `
    "风刃工具威力必须保持为 30。"
Assert-True ((Get-NodeText $entityBlade "config/Expression/Entries/li/tools/li/armorPenetration") -eq "0.35") `
    "风刃工具护甲穿透必须保持为 35%。"

Assert-True ((Get-NodeText $entityBlade "config/Expression/Entries/li/Presentation/VisualPresetDefName") -eq "BDP_Visual_Fujin") `
    "风刃必须引用独立的绿色刀刃视觉预设。"
Assert-True ((Get-NodeText $entityBlade "config/Expression/Entries/li/DisplayLabelKey") -eq "BDP_Expression_Fujin") `
    "风刃近战入口必须使用风刃语言键。"
Assert-True ((Get-NodeText $entityBlade "config/Expression/Entries/li/ToolLabelKeys/li") -eq "BDP_Tool_FujinRealBlade") `
    "风刃近战工具必须使用实刃语言键。"

# 视觉保留弧月全部姿态和贴图，只替换刀刃发光颜色。
Assert-True ((Get-NodeText $entityVisual "GraphicData/texPath") -eq "Things/Trigger/Chip/Fujin/fujin_handle") `
    "风刃必须使用专用手柄贴图。"
Assert-True ((Get-NodeText $entityVisual "GraphicData/drawSize") -eq (Get-NodeText (Find-Def $visualXml "BDP_Visual_Kogetsu") "GraphicData/drawSize")) `
    "风刃·实体刀必须复用弧月手柄尺寸。"
Assert-True ((Get-NodeText $entityVisual "SouthNorthPose") -eq (Get-NodeText (Find-Def $visualXml "BDP_Visual_Kogetsu") "SouthNorthPose")) `
    "风刃·实体刀必须复用弧月南北向姿态。"
Assert-True ((Get-NodeText $entityVisual "EastWestPose") -eq (Get-NodeText (Find-Def $visualXml "BDP_Visual_Kogetsu") "EastWestPose")) `
    "风刃·实体刀必须复用弧月东西向姿态。"
Assert-True ((Get-NodeText $entityVisual "OverlayLayers/li/GraphicData/texPath") -eq "Things/Trigger/Chip/Fujin/fujin_blade") `
    "风刃必须使用专用光刃贴图。"
Assert-True ((Get-NodeText $entityVisual "OverlayLayers/li/GraphicData/color") -eq "(0.72, 1.0, 0.76)") `
    "风刃·实体刀刀刃发光层必须使用更亮的绿色 RGB 颜色。"
Assert-True ((Get-NodeText $entityVisual "OverlayLayers/li/GraphicData/ignoreThingDrawColor") -eq "true") `
    "风刃·实体刀刀刃发光层不得被芯片实例颜色覆盖。"

# 玩家可见名称和说明必须由简体中文语言包提供。
$requiredKeys = @(
    "BDP_ChipFunction_Fujin",
    "BDP_ChipFunction_Fujin_Description",
    "BDP_Expression_Fujin",
    "BDP_Tool_FujinRealBlade"
)
foreach ($key in $requiredKeys) {
    Assert-True ($languageText -match ("<" + [regex]::Escape($key) + ">")) "语言包缺少键：$key"
}
Assert-True ($languageText -match '<BDP_ChipFunction_Fujin\.label>风刃</BDP_ChipFunction_Fujin\.label>') `
    "芯片功能语言包缺少风刃名称覆盖。"
Assert-True ($languageText -match '<BDP_Tool_FujinRealBlade>实刃</BDP_Tool_FujinRealBlade>') `
    "语言包缺少实刃工具名称。"

Write-Output "FujinEntityBladeAuthoringSmokeTests PASS"
