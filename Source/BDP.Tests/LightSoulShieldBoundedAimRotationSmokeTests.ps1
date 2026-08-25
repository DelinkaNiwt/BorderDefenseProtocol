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

function Get-RequiredNode {
    param(
        [System.Xml.XmlNode]$Parent,
        [string]$XPath,
        [string]$Message
    )

    $node = $Parent.SelectSingleNode($XPath)
    Assert-True ($null -ne $node) $Message
    return $node
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$presetPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualPresetDef.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$visualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'

Assert-True (Test-Path -LiteralPath $presetPath) '视觉预设定义必须存在。'
Assert-True (Test-Path -LiteralPath $resolverPath) '视觉姿态解析器必须存在。'
Assert-True (Test-Path -LiteralPath $visualPath) '正式视觉 XML 必须存在。'

$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding UTF8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8

Assert-True ($presetText -match 'public\s+float\s+AimRotationLimit\s*=\s*0f\s*;') `
    '视觉预设必须提供默认关闭的 AimRotationLimit（瞄准旋转限幅）。'
Assert-True (($resolverText -match 'ResolveAimAngle') -and
    ($resolverText -match 'Mathf\.DeltaAngle') -and
    ($resolverText -match 'Mathf\.Clamp') -and
    ($resolverText -match '143f') -and
    ($resolverText -match '217f')) `
    '姿态解析器必须按四向原版持械基准计算有限目标跟随角。'
Assert-True ($resolverText -match 'PoseSample\.DrawLoc\s*\+\s*offset\.WorldOffset') `
    '有限旋转不得把连续目标位置改回离散四向位置。'

foreach ($defName in @(
    'BDP_Visual_LightSoulShieldGuard',
    'BDP_Visual_LightSoulShieldGuard_Dual')) {
    $node = Get-RequiredNode $visualXml `
        ('/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="' + $defName + '"]') `
        ('缺少光魂举盾视觉预设：' + $defName)
    Assert-True ([single]$node.AimRotationLimit -eq [single]15) `
        ($defName + ' 必须启用 15 度瞄准旋转限幅。')
}

Write-Output 'LightSoulShieldBoundedAimRotationSmokeTests PASS'
