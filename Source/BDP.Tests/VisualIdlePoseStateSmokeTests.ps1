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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Config\ExpressionVisualSouthNorthPoseConfig.cs'
$eastWestConfigPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Config\ExpressionVisualEastWestPoseConfig.cs'
$resolverPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\VisualPoseResolver.cs'

Assert-True (Test-Path -LiteralPath $configPath) '南北视觉姿态配置必须存在。'
Assert-True (Test-Path -LiteralPath $eastWestConfigPath) '东西视觉姿态配置必须存在。'
Assert-True (Test-Path -LiteralPath $resolverPath) '视觉姿态解析器必须存在。'

$configText = Get-Content -Raw -Encoding utf8 -LiteralPath $configPath
$eastWestConfigText = Get-Content -Raw -Encoding utf8 -LiteralPath $eastWestConfigPath
$resolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $resolverPath

Assert-True (
    ($configText -match 'public bool HandMirrorOnlyWhenIdle = false;') -and
    ($configText -notmatch 'DecorativeAngleOnlyWhenIdle')
) '南北姿态必须保留静默手侧镜像配置，并移除已无使用者的静默装饰角配置。'

Assert-True (
    ($resolverText -match 'bool isAnyExecutionActive = IsAnyExecutionActive\(request\);') -and
    ($resolverText -match 'pose\.HandMirrorOnlyWhenIdle\s*&& !isAnyExecutionActive') -and
    ($resolverText -notmatch 'DecorativeAngleOnlyWhenIdle')
) '南北姿态必须只按整轮执行态裁定静默镜像。'

Assert-True (
    ($eastWestConfigText -match 'public bool HandMirror = false;') -and
    ($resolverText -match 'bool handMirror = pose\.HandMirror && isSubHand;') -and
    ($resolverText -match 'ForceHandMirror = pose\.HandMirror')
) '东西姿态必须默认关闭手侧镜像，并仅在作者显式开启时强制应用副手镜像。'

Assert-True (
    ($resolverText -match 'request\?\.RuntimeState\?\.HasExecutionState == true') -and
    ($resolverText -notmatch 'ForceHandMirrorWhenInactive') -and
    ($resolverText -notmatch 'pose\.HandMirrorOnlyWhenIdle\s*&& !request\.IsExecutionActive')
) '静默姿态不得再由单条视觉是否命中执行焦点决定。'

Write-Output 'VisualIdlePoseStateSmokeTests PASS'
