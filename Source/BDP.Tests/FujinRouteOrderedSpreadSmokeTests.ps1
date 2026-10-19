# 本脚本使用 UTF-8 BOM，确保 Windows PowerShell 正确读取中文断言。
$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$distanceRoot = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\DistanceRevector'
$distanceConfigText = Read-Source (Join-Path $distanceRoot 'DistanceRevectorModule.cs')
$fujinXmlText = Read-Source (Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedModules.xml')

Assert-True (
    ($distanceConfigText -match 'public\s+float\s+IntermediateSpreadRadius\s*=\s*0\.625f') -and
    ($distanceConfigText -match 'public\s+float\s+FinalSpreadRadius\s*=\s*0\.30f') -and
    ($distanceConfigText -match 'public\s+float\s+HighAccuracySpreadScale\s*=\s*0\.25f')
) '风刃距离重定向必须冻结毒蛇式有序散布参数。'

Assert-True (
    ($distanceConfigText -match 'context\.AccuracySnapshot') -and
    ($distanceConfigText -match 'OrderedSpreadSampler\.ResolveScaledRadius') -and
    ($distanceConfigText -match 'OrderedSpreadSampler\.SampleOffset') -and
    ($distanceConfigText -match 'track\.NextCheckpointIndex') -and
    ($distanceConfigText -match 'context\.AttackInstanceId') -and
    ($distanceConfigText -match 'context\.ResultId')
) '风刃 Arrival 必须按冻结精度、攻击实例、投射物序号和检查点调用稳定散布。'

Assert-True (
    ($distanceConfigText -match 'NextTarget\s*=\s*liveTarget') -and
    ($distanceConfigText -match 'NextBindingTarget\s*=\s*liveTarget') -and
    ($distanceConfigText -notmatch 'GenSight\.LineOfSight|CanBeSeenOverFast|SpreadSafetyShrinkSteps')
) '风刃散布不得改变目标绑定，也不得引入毒蛇的障碍物安全裁定。'

Assert-True (
    ($fujinXmlText -match '<IntermediateSpreadRadius>0\.625</IntermediateSpreadRadius>') -and
    ($fujinXmlText -match '<FinalSpreadRadius>0\.30</FinalSpreadRadius>') -and
    ($fujinXmlText -match '<HighAccuracySpreadScale>0\.25</HighAccuracySpreadScale>')
) '风刃 Def 必须声明毒蛇式有序散布默认值。'

Write-Output 'FujinRouteOrderedSpreadSmokeTests PASS'
