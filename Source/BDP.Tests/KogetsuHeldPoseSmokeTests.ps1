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

# 定位主模组 Content 的弧月视觉预设。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Pawn\Expressions\SenkuKogetsu\ExpressionVisualPresetDefs_SenkuKogetsu.xml'

Assert-True (Test-Path -LiteralPath $visualDefsPath) '主模组必须存在弧月视觉预设文件。'

$visualDefs = [xml](Get-Content -Raw -Encoding utf8 -LiteralPath $visualDefsPath)
$kogetsuPreset = @($visualDefs.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Visual_Kogetsu' }

Assert-True ($null -ne $kogetsuPreset) '主模组必须声明 BDP_Visual_Kogetsu 视觉预设。'

# 南北参数必须逐项映射旧版弧月配置。
$southNorth = $kogetsuPreset.SouthNorthPose
Assert-True (
    ($southNorth.DefaultOffset -eq '(-0.20, 0, 0.1)') -and
    ($southNorth.DefaultAngle -eq '-50') -and
    ($southNorth.DefaultAltitudeOffset -eq '0.05') -and
    ($southNorth.SouthZAdjust -eq '-0.05') -and
    ($southNorth.NorthZAdjust -eq '0.05') -and
    ($southNorth.SubHandAngleOffset -eq '15')
) '弧月南北姿态必须逐项复现旧版。'

# 东西参数必须逐项映射旧版弧月配置。
$eastWest = $kogetsuPreset.EastWestPose
Assert-True (
    ($eastWest.SideBaseX -eq '0.08') -and
    ($eastWest.SideDeltaX -eq '0.03') -and
    ($eastWest.FrontAltitudeOffset -eq '0.05') -and
    ($eastWest.BackAltitudeOffset -eq '-0.05') -and
    ($eastWest.DefaultAngle -eq '-50') -and
    ($eastWest.SubHandAngleOffset -eq '15')
) '弧月东西姿态必须逐项复现旧版。'

# 与主模组姿态骨架默认值相同的字段必须省略，避免重复配置。
Assert-True (
    ($null -eq $southNorth.AimMirror) -and
    ($null -eq $southNorth.HandMirror) -and
    ($null -eq $southNorth.MirrorOnNorth) -and
    ($null -eq $eastWest.SideDeltaZ) -and
    ($null -eq $eastWest.AimMirror) -and
    ($null -eq $eastWest.HandMirror)
) '与骨架默认值相同的姿态字段必须省略。'

# 第二步不得改变第一步已经确认的手柄和发光刀刃。
$overlayLayers = @($kogetsuPreset.OverlayLayers.li)
Assert-True (
    ($kogetsuPreset.GraphicData.texPath -eq 'Things/Trigger/Chip/Kogetsu/kogetsu_handle') -and
    ($kogetsuPreset.GraphicData.shaderType -eq 'Cutout') -and
    ($kogetsuPreset.GraphicData.drawSize -eq '(1.2, 1.2)') -and
    ($overlayLayers.Count -eq 1) -and
    ($overlayLayers[0].GraphicData.texPath -eq 'Things/Trigger/Chip/Kogetsu/kogetsu_blade') -and
    ($overlayLayers[0].GraphicData.shaderType -eq 'MoteGlow') -and
    ($overlayLayers[0].GraphicData.drawSize -eq '(1.2, 1.2)')
) '第二步必须保留第一步手柄与发光刀刃的视觉配置。'

Write-Output 'KogetsuHeldPoseSmokeTests PASS'
