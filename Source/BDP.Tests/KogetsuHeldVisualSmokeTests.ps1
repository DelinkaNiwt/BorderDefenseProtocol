$ErrorActionPreference = 'Stop'

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

# 定位主模组 Content 的弧月定义、视觉预设与正式贴图。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Pawn\Expressions\SenkuKogetsu\ExpressionVisualPresetDefs_SenkuKogetsu.xml'
$chipDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'
$handleTexturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Trigger\Chip\Kogetsu\kogetsu_handle.png'
$bladeTexturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Trigger\Chip\Kogetsu\kogetsu_blade.png'

Assert-True (Test-Path -LiteralPath $visualDefsPath) '主模组弧月视觉预设文件必须存在。'
Assert-True (Test-Path -LiteralPath $chipDefsPath) '主模组弧月芯片定义必须存在。'
Assert-True (Test-Path -LiteralPath $handleTexturePath) '主模组弧月手柄贴图必须存在。'
Assert-True (Test-Path -LiteralPath $bladeTexturePath) '主模组弧月刀刃贴图必须存在。'

$visualDefs = [xml](Get-Content -Raw -Encoding utf8 -LiteralPath $visualDefsPath)
$chipDefs = [xml](Get-Content -Raw -Encoding utf8 -LiteralPath $chipDefsPath)

# 弧月双层手持视觉的第一步结果必须保持不变。
$kogetsuPreset = @($visualDefs.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Visual_Kogetsu' }

Assert-True ($null -ne $kogetsuPreset) '主模组必须声明 BDP_Visual_Kogetsu 视觉预设。'

Assert-True (
    ($kogetsuPreset.GraphicData.texPath -eq 'Things/Trigger/Chip/Kogetsu/kogetsu_handle') -and
    ($kogetsuPreset.GraphicData.graphicClass -eq 'Graphic_Single') -and
    ($kogetsuPreset.GraphicData.shaderType -eq 'Cutout') -and
    ($kogetsuPreset.GraphicData.drawSize -eq '(1.2, 1.2)')
) '弧月主层必须使用旧版手柄贴图、透明裁切材质和 1.2 尺寸。'

$overlayLayers = @($kogetsuPreset.OverlayLayers.li)
Assert-True (
    ($overlayLayers.Count -eq 1) -and
    ($overlayLayers[0].LayerId -eq 'kogetsu_blade') -and
    ($overlayLayers[0].GraphicData.texPath -eq 'Things/Trigger/Chip/Kogetsu/kogetsu_blade') -and
    ($overlayLayers[0].GraphicData.graphicClass -eq 'Graphic_Single') -and
    ($overlayLayers[0].GraphicData.shaderType -eq 'MoteGlow') -and
    ($overlayLayers[0].GraphicData.color -eq '(1.0, 1.0, 0.95)') -and
    ($overlayLayers[0].GraphicData.drawSize -eq '(1.2, 1.2)')
) '弧月附加层必须使用旧版刀刃贴图、发光材质、黄白色和 1.2 尺寸。'

Assert-True (
    $null -eq $kogetsuPreset.DrawScale
) '弧月继续继承主模组默认绘制缩放。'

# 弧月近战表达条目只引用该视觉预设，不增加复合预设或强制压制配置。
$kogetsuThingDef = @($chipDefs.Defs.ThingDef) |
    Where-Object { $_.defName -eq 'BDP_Chip_Kogetsu' }
$kogetsuChipConfig = @($kogetsuThingDef.modExtensions.li) |
    Where-Object { $_.Class -eq 'BDP.Core.Chips.ChipDefinitionConfig' }
$kogetsuEntry = @($kogetsuChipConfig.Expression.Entries.li) |
    Where-Object { $_.Id -eq 'kogetsu_primary' }

Assert-True ($null -ne $kogetsuEntry) '弧月近战表达条目必须存在。'
Assert-True (
    ($kogetsuEntry.Presentation.VisualPresetDefName -eq 'BDP_Visual_Kogetsu') -and
    ($null -eq $kogetsuEntry.Presentation.CompositeVisualPresetDefName) -and
    ($null -eq $kogetsuEntry.Presentation.ForceSuppressHostEquipment)
) '弧月表达条目必须只引用双层视觉预设，不得提前增加复合视觉或强制压制配置。'

Write-Output 'KogetsuHeldVisualSmokeTests PASS'
