$ErrorActionPreference = "Stop"

# 断言指定条件成立。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 在 XML 中查找指定 Def，避免测试依赖定义顺序。
function Find-Def {
    param(
        [xml]$Document,
        [string]$DefName
    )

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

# 读取 XML 节点文本；缺失节点返回空字符串。
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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$triggerPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Equipment\Trigger.xml"
$visualPath = Join-Path $modRoot "1.6\Content\Defs\ExpressionDef\Visual.xml"
$triggerTexturePath = Join-Path $modRoot "1.6\Textures\Things\Equipment\Trigger\BDP_FujinTriggerBody.png"
$handleTexturePath = Join-Path $modRoot "1.6\Textures\Things\Trigger\Chip\Fujin\fujin_handle.png"
$bladeTexturePath = Join-Path $modRoot "1.6\Textures\Things\Trigger\Chip\Fujin\fujin_blade.png"

foreach ($path in @($triggerPath, $visualPath, $triggerTexturePath, $handleTexturePath, $bladeTexturePath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ("贴图配置或资源不存在：" + $path)
}

[xml]$triggerXml = Get-Content -LiteralPath $triggerPath -Raw -Encoding UTF8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8
$neighborTest = Find-Def $triggerXml "BDP_TriggerBody_Fujin"
$entityVisual = Find-Def $visualXml "BDP_Visual_Fujin"

Assert-True ($null -ne $neighborTest) "缺少风刃触发体定义。"
Assert-True ($null -ne $entityVisual) "缺少风刃视觉预设。"
Assert-True ((Get-NodeText $neighborTest "graphicData/texPath") -eq "Things/Equipment/Trigger/BDP_FujinTriggerBody") `
    "风刃触发体必须引用专用贴图。"
Assert-True ((Get-NodeText $entityVisual "GraphicData/texPath") -eq "Things/Trigger/Chip/Fujin/fujin_handle") `
    "风刃主层必须引用专用手柄贴图。"
Assert-True ((Get-NodeText $entityVisual "OverlayLayers/li/GraphicData/texPath") -eq "Things/Trigger/Chip/Fujin/fujin_blade") `
    "风刃覆盖层必须引用专用光刃贴图。"
Assert-True ((Get-NodeText $entityVisual "Grip/UseAsPoseOrigin") -eq "true") `
    "风刃·实体刀必须以握持点作为贴图定位原点。"
Assert-True ((Get-NodeText $entityVisual "Grip/GripOffset") -eq "(0.15625, 0, -0.1)") `
    "风刃·实体刀必须按贴图画布比例记录握把与光刃交接处。"

# Def 中只能写模组内相对路径，不得把参考资源的绝对路径写入游戏配置。
Assert-True ($triggerXml.OuterXml -notmatch "参考资源|C:/NiwtDatas|C:\\NiwtDatas") `
    "触发体 Def 不得写入外部参考资源绝对路径。"
Assert-True ($visualXml.OuterXml -notmatch "参考资源|C:/NiwtDatas|C:\\NiwtDatas") `
    "视觉 Def 不得写入外部参考资源绝对路径。"

Write-Output "FujinAndTriggerTextureAuthoringSmokeTests PASS"
