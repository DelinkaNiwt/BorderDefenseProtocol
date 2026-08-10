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

# 定位 DevHarness（伴生测试模组）的视觉预设文件。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modProjectsRoot = Split-Path -Parent $repoRoot
$visualPresetDefsPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml'
$southNorthPoseSourcePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualSouthNorthPoseConfig.cs'
$eastWestPoseSourcePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualEastWestPoseConfig.cs'

Assert-True (Test-Path -LiteralPath $visualPresetDefsPath) 'DevHarness visual preset XML must exist.'
Assert-True (Test-Path -LiteralPath $southNorthPoseSourcePath) 'South/North visual pose config source must exist.'
Assert-True (Test-Path -LiteralPath $eastWestPoseSourcePath) 'East/West visual pose config source must exist.'

$visualPresetDefsText = Get-Content -Raw -Encoding utf8 -LiteralPath $visualPresetDefsPath
$southNorthPoseSourceText = Get-Content -Raw -Encoding utf8 -LiteralPath $southNorthPoseSourcePath
$eastWestPoseSourceText = Get-Content -Raw -Encoding utf8 -LiteralPath $eastWestPoseSourcePath
$visualPresetDefs = [xml]$visualPresetDefsText
$weaponVisualPresets = @($visualPresetDefs.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef')
$viperPreset = $weaponVisualPresets |
    Where-Object { $_.defName -eq 'BDP_TestVisual_PathLatchVolley' }

# 贴图、枪口前向距离和额外世界偏移保持不变；主副侧不再添加横向偏移。
Assert-True (
    ($viperPreset -ne $null) -and
    ($viperPreset.GraphicData.texPath -eq 'Things/Trigger/Chip/viper_salvo') -and
    ($viperPreset.GraphicData.graphicClass -eq 'Graphic_Single') -and
    ($viperPreset.GraphicData.drawSize -eq '(1, 1)')
) '毒蛇齐射应保留贴图、尺寸和枪口前向距离，同时省略副侧覆盖并取消主副侧横向偏移。'

# 框架默认不再为副侧附加角度。
Assert-True (
    ($southNorthPoseSourceText -match 'public float SubHandAngleOffset = 0f;') -and
    ($eastWestPoseSourceText -match 'public float SubHandAngleOffset = 0f;')
) '南北与东西视觉姿态默认值均不得额外旋转副侧。'

# 全部现有武器视觉预设统一省略缩放、分朝向姿态和副侧枪口覆盖。
$expectedMuzzleOffsets = [ordered]@{
    BDP_TestVisual_RangedSequential = '(0, 0, 0.68)'
    BDP_TestVisual_RangedSequential_Composite = '(0, 0, 0.72)'
    BDP_TestVisual_RangedVolley = '(0, 0, 0.58)'
    BDP_TestVisual_RangedVolley_Composite = '(0, 0, 0.61)'
    BDP_TestVisual_PathLatchVolley = '(0, 0, 0.68)'
}

Assert-True (
    $weaponVisualPresets.Count -eq ($expectedMuzzleOffsets.Count + 1)
) 'DevHarness 应保留 5 个远程武器视觉预设，并新增 1 个弧月双层视觉预设。'

foreach ($presetName in $expectedMuzzleOffsets.Keys) {
    $preset = $weaponVisualPresets | Where-Object { $_.defName -eq $presetName }
    Assert-True (
        ($preset -ne $null) -and
        ($null -eq $preset.DrawScale) -and
        ($null -eq $preset.SouthNorthPose) -and
        ($null -eq $preset.EastWestPose) -and
        ($preset.Muzzle.IsRangedWeapon -eq 'true') -and
        ($preset.Muzzle.MuzzleOffset -eq $expectedMuzzleOffsets[$presetName]) -and
        ($null -eq $preset.Muzzle.HasSubHandMuzzleOffsetOverride) -and
        ($null -eq $preset.Muzzle.SubHandMuzzleOffsetOverride) -and
        ($preset.Muzzle.ExtraWorldOffset -eq '(0, 0, 0)')
    ) "$presetName 应回退默认缩放与姿态，保留零横向枪口前向距离，并省略副侧枪口覆盖。"
}

Assert-True (
    [regex]::Matches($visualPresetDefsText, '<SubHandAngleOffset>').Count -eq 0
) '全部武器视觉预设都不应显式配置副侧角度偏移。'

Write-Output 'DevHarnessViperVolleyVisualDefaultsSmokeTests PASS'
