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

# 读取可省略的姿态补偿；未声明时沿用运行时默认值零。
function Get-OptionalAdjust {
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

# 读取具体预设 defName 或抽象预设 Name，供失败信息定位。
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

# 当前正式配置共有七个直接覆盖南北姿态的预设；它们都必须保留原版南北高度差。
$explicitPosePresets = @($visualXml.SelectNodes(
    '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[SouthNorthPose]'))
$expectedExplicitPresetNames = @(
    'BDP_Visual_Kogetsu',
    'BDP_Visual_LightSoulHeavyBlade',
    'BDP_Visual_LightSoulShieldGuard',
    'BDP_Visual_LightSoulShieldGuard_Dual',
    'BDP_Visual_LightSoulShieldMobile',
    'BDP_Visual_LightSoulShieldMobile_Dual',
    'BDP_VisualBase_RangedMedium_Dual'
)
$actualExplicitPresetNames = @($explicitPosePresets | ForEach-Object { Get-PresetName $_ } | Sort-Object)

Assert-True (
    ($actualExplicitPresetNames -join '|') -eq (($expectedExplicitPresetNames | Sort-Object) -join '|')
) '正式视觉配置中的显式南北姿态预设集合发生了未评估的变化。'

# 原版成年人物空闲持械点：朝北比朝南高 0.11 格。
$vanillaSouthZ = [single]-0.22
$vanillaNorthZ = [single]-0.11
$vanillaNorthElevation = [single]0.11
$tolerance = [single]0.0001
$northExtraByPreset = @{
    'BDP_Visual_LightSoulShieldGuard' = [single]0.05
    'BDP_Visual_LightSoulShieldGuard_Dual' = [single]0.05
}

foreach ($preset in $explicitPosePresets) {
    $presetName = Get-PresetName $preset
    $pose = $preset.SouthNorthPose
    $defaultOffsetZ = Get-DefaultOffsetZ $pose
    $southAdjust = Get-OptionalAdjust $pose 'SouthZAdjust'
    $northAdjust = Get-OptionalAdjust $pose 'NorthZAdjust'

    # BDP 解析器在朝北时反转基础 Z；只有明确声明的举盾预设允许再额外抬高 0.05 格。
    $bdpSouthOffsetZ = $defaultOffsetZ + $southAdjust
    $bdpNorthOffsetZ = -$defaultOffsetZ + $northAdjust
    $expectedNorthExtra = if ($northExtraByPreset.ContainsKey($presetName)) {
        [single]$northExtraByPreset[$presetName]
    } else {
        [single]0
    }
    Assert-True (
        [Math]::Abs(($bdpNorthOffsetZ - $bdpSouthOffsetZ) - $expectedNorthExtra) -lt $tolerance
    ) "$presetName 的朝北额外纵向偏移不符合声明：South=$bdpSouthOffsetZ，North=$bdpNorthOffsetZ，ExpectedExtra=$expectedNorthExtra。"

    # 合并原版持械点后，最终朝北高度差等于原版 0.11 格加上显式业务额外值。
    $southFinalZ = $vanillaSouthZ + $bdpSouthOffsetZ
    $northFinalZ = $vanillaNorthZ + $bdpNorthOffsetZ
    Assert-True (
        [Math]::Abs(($northFinalZ - $southFinalZ) - ($vanillaNorthElevation + $expectedNorthExtra)) -lt $tolerance
    ) "$presetName 的最终朝北高度差不正确：South=$southFinalZ，North=$northFinalZ。"
}

# 只替换贴图的单武器继续走原版姿态，不为本次修复新增姿态覆盖。
$textureOnlyPresetNames = @(
    'BDP_Visual_Pistol',
    'BDP_Visual_AssaultRifle',
    'BDP_Visual_RangedWeaponReference',
    'BDP_Visual_Shotgun',
    'BDP_Visual_SniperRifle'
)

foreach ($presetName in $textureOnlyPresetNames) {
    $preset = $visualXml.SelectSingleNode(
        "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='$presetName']")
    Assert-True ($null -ne $preset) "缺少只替换贴图的单武器预设：$presetName。"
    Assert-True (
        ($null -eq $preset.SouthNorthPose) -and ($null -eq $preset.EastWestPose)
    ) "$presetName 必须继续沿用原版姿态。"
}

Write-Output 'VisualNorthElevationParitySmokeTests PASS'
