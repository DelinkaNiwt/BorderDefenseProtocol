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
    ($configText -match 'public bool DecorativeAngleOnlyWhenIdle = false;')
) '南北姿态必须保留默认关闭的静默手侧镜像和装饰角配置。'

Assert-True (
    ([regex]::Matches($resolverText, 'bool isAttackStanceActive = IsAnyAttackStanceActive\(request\);').Count -ge 2) -and
    ($resolverText -match 'pose\.HandMirrorOnlyWhenIdle\s*&& !isAttackStanceActive') -and
    ($resolverText -match 'pose\.DecorativeAngleOnlyWhenIdle\s*&& isAttackStanceActive')
) '南北与东西姿态的静默镜像、装饰角都必须只按同一实时 Pawn 攻击姿态裁定。'

Assert-True (
    ($eastWestConfigText -match 'public bool HandMirror = false;') -and
    ($resolverText -match 'bool handMirror = pose\.HandMirror\s*&&\s*\(pose\.FinalMirrorByHandOnly') -and
    ($resolverText -match 'ForceHandMirror = pose\.HandMirror')
) '东西姿态必须默认关闭手侧镜像，并仅在作者显式开启时强制应用副手镜像。'

Assert-True (
    ($resolverText -match 'return request\?\.IsMatchingAttackStanceActive == true;') -and
    ($resolverText -notmatch 'RuntimeState\?\.HasExecutionState') -and
    ($resolverText -notmatch 'ForceHandMirrorWhenInactive') -and
    ($resolverText -notmatch 'pose\.HandMirrorOnlyWhenIdle\s*&& !request\.IsExecutionActive')
) '静默姿态必须读取共享的实时攻击姿态标记，不能使用残留的运行时记录或单条焦点。'

Write-Output 'VisualIdlePoseStateSmokeTests PASS'
