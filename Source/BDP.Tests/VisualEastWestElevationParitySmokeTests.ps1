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

# 按不受系统区域设置影响的格式读取单精度数值。
function ConvertTo-InvariantSingle {
    param(
        [string]$Value
    )

    return [single]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

# 读取南北姿态基础偏移中的 Z 分量。
function Get-DefaultOffsetZ {
    param(
        [System.Xml.XmlElement]$Pose
    )

    $parts = ([string]$Pose.DefaultOffset).Trim([char[]]'()').Split(',')
    Assert-True ($parts.Count -eq 3) ('无法解析南北姿态 DefaultOffset：' + [string]$Pose.DefaultOffset)
    return ConvertTo-InvariantSingle $parts[2].Trim()
}

# 读取可省略的单精度姿态字段；未声明时使用运行时默认值零。
function Get-OptionalSingle {
    param(
        [System.Xml.XmlElement]$Pose,
        [string]$ElementName
    )

    $node = $Pose.SelectSingleNode($ElementName)
    if ($null -eq $node) {
        return [single]0
    }

    return ConvertTo-InvariantSingle $node.InnerText.Trim()
}

# 读取具体预设 defName 或抽象预设 Name。
function Get-PresetName {
    param(
        [System.Xml.XmlElement]$Preset
    )

    $defNameNode = $Preset.SelectSingleNode('defName')
    if ($null -ne $defNameNode) {
        return $defNameNode.InnerText
    }

    return $Preset.GetAttribute('Name')
}

# 定位主模组正式视觉配置。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'

Assert-True (Test-Path -LiteralPath $visualPath) '主模组正式视觉配置必须存在。'
[xml]$visualXml = Get-Content -Raw -Encoding utf8 -LiteralPath $visualPath

# 所有正式显式四向姿态都必须满足同一套东西高度关系。
$explicitPosePresets = @($visualXml.SelectNodes(
    '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[SouthNorthPose and EastWestPose]'))
$expectedPresetNames = @(
    'BDP_Visual_Kogetsu',
    'BDP_Visual_LightSoulHeavyBlade',
    'BDP_Visual_LightSoulShieldGuard',
    'BDP_Visual_LightSoulShieldGuard_Dual',
    'BDP_Visual_LightSoulShieldMobile',
    'BDP_Visual_LightSoulShieldMobile_Dual',
    'BDP_VisualBase_RangedMedium_Dual'
)
$actualPresetNames = @($explicitPosePresets | ForEach-Object { Get-PresetName $_ } | Sort-Object)

Assert-True (
    ($actualPresetNames -join '|') -eq (($expectedPresetNames | Sort-Object) -join '|')
) '正式视觉配置中的显式四向姿态预设集合发生了未评估的变化。'

$tolerance = [single]0.0001

foreach ($preset in $explicitPosePresets) {
    $presetName = Get-PresetName $preset
    $southNorthPose = $preset.SouthNorthPose
    $eastWestPose = $preset.EastWestPose
    $sideBaseZNode = $eastWestPose.SelectSingleNode('SideBaseZ')

    Assert-True ($null -ne $sideBaseZNode) "$presetName 必须显式声明东西姿态共同 Z 基准。"

    $southCommonZ = (Get-DefaultOffsetZ $southNorthPose) +
        (Get-OptionalSingle $southNorthPose 'SouthZAdjust')
    $sideBaseZ = ConvertTo-InvariantSingle $sideBaseZNode.InnerText.Trim()
    $sideDeltaZ = Get-OptionalSingle $eastWestPose 'SideDeltaZ'

    # 东西姿态先应用共同基准，再让前景手降低、背景手抬高。
    $eastMainZ = $sideBaseZ - $sideDeltaZ
    $eastSubZ = $sideBaseZ + $sideDeltaZ
    $westMainZ = $sideBaseZ + $sideDeltaZ
    $westSubZ = $sideBaseZ - $sideDeltaZ
    $sideCenterZ = ($eastMainZ + $eastSubZ) / 2

    Assert-True (
        [Math]::Abs($sideCenterZ - $southCommonZ) -lt $tolerance
    ) "$presetName 的东西姿态中心必须与南向共同偏移一致。"
    Assert-True (
        ([Math]::Abs($eastMainZ - $westSubZ) -lt $tolerance) -and
        ([Math]::Abs($eastSubZ - $westMainZ) -lt $tolerance)
    ) "$presetName 的东西前后手高度必须互为镜像。"
}

# 中型远程双武器必须保留已经确认的前低后高 0.10 格透视差。
$mediumDual = $visualXml.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium_Dual']")
$mediumSideBaseZ = ConvertTo-InvariantSingle $mediumDual.EastWestPose.SideBaseZ
$mediumSideDeltaZ = ConvertTo-InvariantSingle $mediumDual.EastWestPose.SideDeltaZ
Assert-True (
    ([Math]::Abs($mediumSideBaseZ - [single]0.03) -lt $tolerance) -and
    ([Math]::Abs($mediumSideDeltaZ - [single]0.10) -lt $tolerance) -and
    ([Math]::Abs(($mediumSideBaseZ - $mediumSideDeltaZ) - [single]-0.07) -lt $tolerance) -and
    ([Math]::Abs(($mediumSideBaseZ + $mediumSideDeltaZ) - [single]0.13) -lt $tolerance)
) '中型远程双武器必须以 0.03 为共同基准，并保留前景 -0.10、背景 +0.10 的透视分离。'

# 只替换贴图的单武器继续走原版姿态，不为本次修复新增姿态覆盖。
foreach ($presetName in @(
    'BDP_Visual_Pistol',
    'BDP_Visual_AssaultRifle',
    'BDP_Visual_RangedWeaponReference',
    'BDP_Visual_Shotgun',
    'BDP_Visual_SniperRifle')) {
    $preset = $visualXml.SelectSingleNode(
        "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='$presetName']")
    Assert-True ($null -ne $preset) "缺少只替换贴图的单武器预设：$presetName。"
    Assert-True (
        ($null -eq $preset.SouthNorthPose) -and ($null -eq $preset.EastWestPose)
    ) "$presetName 必须继续沿用原版姿态。"
}

Write-Output 'VisualEastWestElevationParitySmokeTests PASS'
