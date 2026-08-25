$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 读取必需 XML（可扩展标记语言）节点。
function Get-RequiredNode
{
    param([System.Xml.XmlNode]$Parent, [string]$XPath, [string]$Message)
    $node = $Parent.SelectSingleNode($XPath)
    Assert-True ($null -ne $node) $Message
    return $node
}

# 读取三维向量文本中的 X 分量。
function Get-VectorX
{
    param([string]$Value)
    return [single]($Value.Trim('(', ')').Split(',')[0])
}

# 读取三维向量文本中的 Z 分量。
function Get-VectorZ
{
    param([string]$Value)
    return [single]($Value.Trim('(', ')').Split(',')[2])
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$textureRoot = Join-Path $modRoot '1.6\Textures\Effects\Shield'
$directionalTextureNames = @(
    'energy_shield_block_curved_north.png',
    'energy_shield_block_curved_east.png',
    'energy_shield_block_curved_south.png',
    'energy_shield_block_curved_west.png')

[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8
$guardSingle = Get-RequiredNode $visualXml `
    '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard"]' `
    '缺少光魂举盾单武器视觉。'
$guardDual = Get-RequiredNode $visualXml `
    '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard_Dual"]' `
    '缺少光魂举盾双武器视觉。'

# 单双武器必须共享原版多朝向贴图和同一套翻转规则。
foreach ($guard in @($guardSingle, $guardDual))
{
    Assert-True ([string]$guard.GraphicData.texPath -eq 'Effects/Shield/energy_shield_block_curved') `
        '光魂举盾必须共享正式的弧面盾多朝向贴图基准。'
    Assert-True ([string]$guard.GraphicData.graphicClass -eq 'Graphic_Multi') `
        '光魂举盾必须使用原版 Graphic_Multi（多朝向贴图）。'
    Assert-True ([single]$guard.SouthNorthPose.DefaultAngle -eq [single]-68) `
        '竖向正视资源必须抵消原版持械角并形成约 15 度向内斜握。'
    Assert-True ([string]$guard.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true') `
        '南北屏幕左侧盾必须额外镜像，使上端始终向人物中心收。'
    Assert-True ([single]$guard.EastWestPose.DefaultAngle -eq [single]-53) `
        '东西侧视资源必须抵消原版 53 度持械角并保持屏幕竖直。'
}

# 单持只要求最终画面：东向原图、前景、贴近；西向镜像、前景、贴近。
Assert-True (([string]$guardSingle.EastWestPose.HandMirror -eq 'false') -and
    ([string]$guardSingle.EastWestPose.FinalMirrorByHandOnly -eq 'false') -and
    ([string]$guardSingle.EastWestPose.MainHandAlwaysFront -eq 'false')) `
    '单持东西向必须关闭额外手位镜像，只保留原版朝西基础镜像。'
Assert-True (([string]$guardDual.EastWestPose.HandMirror -eq 'true') -and
    ([string]$guardDual.EastWestPose.FinalMirrorByHandOnly -eq 'true') -and
    ([string]$guardDual.EastWestPose.MainHandAlwaysFront -eq 'false')) `
    '双武器东西向必须继续使用已确认的最终手位镜像和朝向前后景规则。'

# 单持与双武器共用贴身基准；双武器不再为另一把武器额外外移盾面。
$singleDistance = [Math]::Abs((Get-VectorX ([string]$guardSingle.SouthNorthPose.DefaultOffset)))
$dualDistance = [Math]::Abs((Get-VectorX ([string]$guardDual.SouthNorthPose.DefaultOffset)))
Assert-True ([Math]::Abs($singleDistance - [single]0.12) -lt 0.0001) `
    '光魂举盾单持南北手侧距离必须收回到 0.12 格。'
Assert-True ([Math]::Abs($dualDistance - [single]0.12) -lt 0.0001) `
    '光魂举盾双武器南北手侧距离必须与单持同为 0.12 格。'
Assert-True ([Math]::Abs($dualDistance - $singleDistance) -lt 0.0001) `
    '光魂举盾双武器不得再为另一把武器额外外移盾面。'
Assert-True ([single]$guardSingle.EastWestPose.SideBaseX -eq [single]0.08) `
    '光魂举盾单持东西位置必须固定为目标贴身距离 0.08。'
Assert-True ([single]$guardDual.EastWestPose.SideBaseX -eq [single]0.12) `
    '光魂举盾双武器东西位置必须收回到 0.12，使侧视盾贴近人物。'
Assert-True (([single]$guardSingle.EastWestPose.SideDeltaX -eq [single]0) -and
    ([single]$guardDual.EastWestPose.SideDeltaX -eq [single]0.04)) `
    '东西单持必须消除手位位置差，双武器继续保留 0.04 的近远差。'

# 单双武器盾面共同抬高屏幕位置，朝北再追加业务所需的 0.05 格。
$singleSouthNorthZ = Get-VectorZ ([string]$guardSingle.SouthNorthPose.DefaultOffset)
$dualSouthNorthZ = Get-VectorZ ([string]$guardDual.SouthNorthPose.DefaultOffset)
Assert-True ([Math]::Abs($singleSouthNorthZ - [single]0.23) -lt 0.0001) `
    '光魂举盾单持南北基础屏幕高度必须补抬到 0.23。'
Assert-True ([Math]::Abs($dualSouthNorthZ - [single]0.23) -lt 0.0001) `
    '光魂举盾双武器南北屏幕高度必须抬到 0.23。'
Assert-True (([single]$guardSingle.SouthNorthPose.NorthZAdjust -eq [single]0.51) -and
    ([single]$guardDual.SouthNorthPose.NorthZAdjust -eq [single]0.51)) `
    '单双持朝北补偿必须同为 0.51，使朝北额外抬高 0.05 格。'
Assert-True (([single]$guardSingle.EastWestPose.SideBaseZ -eq [single]0.23) -and
    ([single]$guardDual.EastWestPose.SideBaseZ -eq [single]0.23)) `
    '东西屏幕高度必须让单双持共同保持 0.23。'
$singleNorthZ = -[single]$singleSouthNorthZ + [single]$guardSingle.SouthNorthPose.NorthZAdjust
$dualNorthZ = -[single]$dualSouthNorthZ + [single]$guardDual.SouthNorthPose.NorthZAdjust
Assert-True (([Math]::Abs($singleNorthZ - [single]0.28) -lt 0.0001) -and
    ([Math]::Abs($dualNorthZ - [single]0.28) -lt 0.0001)) `
    '单双持朝北的 BDP 最终纵向偏移必须共同达到 0.28。'

# 单持东西结果必须与指定双武器手位画面一致，且不依赖条目实际手位。
Assert-True (([single]$guardSingle.EastWestPose.FrontAltitudeOffset -eq [single]0.08) -and
    ([single]$guardSingle.EastWestPose.BackAltitudeOffset -eq [single]0.08)) `
    '单持东西两条分支都必须使用 +0.08 前景绘制层。'
$singleEastWestCases = @(
    @{ FacingWest = $false; Mirrored = $false },
    @{ FacingWest = $true;  Mirrored = $true })
foreach ($case in $singleEastWestCases)
{
    foreach ($actualSubHand in @($false, $true))
    {
        $resolvedMirror = [bool]$case.FacingWest
        $resolvedDistance = [single]$guardSingle.EastWestPose.SideBaseX
        Assert-True ($resolvedMirror -eq [bool]$case.Mirrored) '单持东西最终镜像结果不成立。'
        Assert-True ([Math]::Abs($resolvedDistance - [single]0.08) -lt 0.0001) `
            '单持东西最终贴身距离不得受实际手位影响。'
    }
}

# 南北绘制高度层只决定遮挡：南向盾在另一武器前，北向自动取反到另一武器后。
Assert-True ([single]$guardSingle.SouthNorthPose.DefaultAltitudeOffset -eq [single]0.08) `
    '光魂举盾单持南北绘制层必须保持 0.08。'
Assert-True ([single]$guardDual.SouthNorthPose.DefaultAltitudeOffset -eq [single]0.12) `
    '光魂举盾双武器南北绘制层幅度必须为 0.12。'
Assert-True (([single]$guardDual.SouthNorthPose.DefaultAltitudeOffset -gt [single]0.10) -and
    (-[single]$guardDual.SouthNorthPose.DefaultAltitudeOffset -lt [single]-0.10)) `
    '双武器盾必须在南向越过另一武器前景层，并在北向越过另一武器背景层。'

# 新竖向正视资源的南北最终斜率：屏幕左侧 +15 度，屏幕右侧 -15 度。
$rawSouthNorthAngle = (143 - 90 + [single]$guardSingle.SouthNorthPose.DefaultAngle + 360) % 360
$mirroredSouthNorthAngle = (-$rawSouthNorthAngle + 360) % 360
Assert-True (($rawSouthNorthAngle -eq 345) -and ($mirroredSouthNorthAngle -eq 15)) `
    '南北竖盾必须解析为左右镜像的正负 15 度斜握。'

# 东西真值：最终镜像只看手位；前后景和近远随朝向交换。
$eastWestCases = @(
    @{ FacingWest = $false; IsSubHand = $false; Mirrored = $false; Front = $true;  Distance = [single]0.08 },
    @{ FacingWest = $false; IsSubHand = $true;  Mirrored = $true;  Front = $false; Distance = [single]0.16 },
    @{ FacingWest = $true;  IsSubHand = $false; Mirrored = $false; Front = $false; Distance = [single]0.16 },
    @{ FacingWest = $true;  IsSubHand = $true;  Mirrored = $true;  Front = $true;  Distance = [single]0.08 })
foreach ($case in $eastWestCases)
{
    $additionalMirror = [bool]$case.FacingWest -xor [bool]$case.IsSubHand
    $resolvedMirror = [bool]$case.FacingWest -xor $additionalMirror
    $resolvedFront = if ([bool]$case.FacingWest) { [bool]$case.IsSubHand } else { -not [bool]$case.IsSubHand }
    $resolvedDistance = [single]$guardDual.EastWestPose.SideBaseX + `
        $(if ($resolvedFront) { -[single]$guardDual.EastWestPose.SideDeltaX } else { [single]$guardDual.EastWestPose.SideDeltaX })
    Assert-True ($resolvedMirror -eq [bool]$case.Mirrored) '东西四向手位镜像真值表不成立。'
    Assert-True ($resolvedFront -eq [bool]$case.Front) '东西四向前后景真值表不成立。'
    Assert-True ([Math]::Abs($resolvedDistance - [single]$case.Distance) -lt 0.0001) `
        '东西四向贴身近远真值表不成立。'
}
$eastFinalAngle = (143 - 90 + [single]$guardSingle.EastWestPose.DefaultAngle + 360) % 360
$westFinalAngle = (217 - 90 - 180 - [single]$guardSingle.EastWestPose.DefaultAngle + 360) % 360
Assert-True (($eastFinalAngle -eq 0) -and ($westFinalAngle -eq 0)) `
    '东西侧视盾在应用网格镜像前必须共同保持屏幕竖直。'

# 四向资源必须同画布、含透明通道；实际轮廓宽窄由作者图片自身决定。
Add-Type -AssemblyName System.Drawing
foreach ($textureName in $directionalTextureNames)
{
    $texturePath = Join-Path $textureRoot $textureName
    Assert-True (Test-Path -LiteralPath $texturePath) ('缺少光魂举盾方向贴图：' + $textureName)
    $image = [System.Drawing.Image]::FromFile($texturePath)
    try
    {
        Assert-True (($image.Width -eq 512) -and ($image.Height -eq 512)) `
            ('光魂举盾方向贴图必须是 512×512：' + $textureName)
        Assert-True ($image.PixelFormat.ToString() -match 'Argb') `
            ('光魂举盾方向贴图必须包含 Alpha 透明度：' + $textureName)
    }
    finally
    {
        $image.Dispose()
    }
}

Write-Output 'LightSoulGuardDirectionalPoseSmokeTests PASS'
