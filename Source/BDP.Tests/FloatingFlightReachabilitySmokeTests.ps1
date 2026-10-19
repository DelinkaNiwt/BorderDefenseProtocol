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

function Read-RequiredFile {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空可达性文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$pathingRoot = Join-Path $floatingRoot 'Pathing'
$patchRoot = Join-Path $floatingRoot 'Patches'
$reachabilityPath = Join-Path $pathingRoot 'FloatingFlightReachability.cs'
$reachabilityPatchPath = Join-Path $patchRoot 'Patch_Reachability_CanReach_FloatingFlight.cs'
$destinationPatchPath = Join-Path $patchRoot 'Patch_PathFinder_MakeDestination_FloatingFlight.cs'
$startPathPatchPath = Join-Path $patchRoot 'Patch_PawnPathFollower_StartPath_FloatingFlight.cs'

$reachabilityText = Read-RequiredFile $reachabilityPath
$utilityText = Read-RequiredFile (Join-Path $pathingRoot 'FloatingFlightPathUtility.cs')
$reachabilityPatchText = Read-RequiredFile $reachabilityPatchPath
$destinationPatchText = Read-RequiredFile $destinationPatchPath
$startPathPatchText = Read-RequiredFile $startPathPatchPath

Assert-True ($reachabilityText -match 'FloodFill') '浮空可达性必须使用格子 FloodFill。'
Assert-True ($reachabilityText -match 'WalkableFast|PathGrid') '浮空可达性必须服从当前自定义网格。'
Assert-True ($reachabilityText -match 'GetAllowedArea|AllowedArea|area') '浮空可达性必须保留玩家允许区域。'
Assert-True ($reachabilityText -match 'Danger|danger|maxDanger') '浮空可达性必须保留危险等级限制。'
Assert-True ($reachabilityText -match 'CanEndAt') '浮空可达性必须使用统一合法终点谓词。'
Assert-True ($reachabilityText -match 'Touch|ClosestTouch|OnCell') '浮空可达性必须覆盖原版三种终点模式。'
Assert-True ($utilityText -match 'Roofed|roofed') '始终浮空终点必须排除所有屋顶格。'

Assert-True ($reachabilityPatchText -match 'Reachability.*CanReach|CanReach') '必须窄范围接入原版 Reachability.CanReach。'
Assert-True ($reachabilityPatchText -match 'TraverseParms|pawn') '可达性补丁必须只处理带 Pawn 的浮空请求。'
Assert-True ($reachabilityPatchText -match 'FloatingFlightReachability') 'Reachability 补丁必须调用正式浮空可达性实现。'

Assert-True ($destinationPatchText -match 'PathFinder.*MakeDestination|MakeDestination') '必须接入原版终点矩形生成入口。'
Assert-True ($destinationPatchText -match 'excluded|Excluded') '终点补丁必须把非法停留格加入原版 excluded 列表。'
Assert-True ($destinationPatchText -match 'IsLegalLandingCell') '终点补丁必须复用统一的最终停留格规则。'
Assert-True ($destinationPatchText -match 'OccupiedRect') 'Touch 类终点必须排除目标物自身占用格。'

Assert-True ($startPathPatchText -match 'Transpiler|CodeMatcher|CodeInstruction') 'StartPath 必须使用精确指令替换接入浮空可达性。'
Assert-True ($startPathPatchText -match 'CanReachForStartPath') 'StartPath 必须调用浮空专用预检包装器。'
Assert-True ($startPathPatchText -match 'this\.pawn|pawn') 'StartPath 包装器必须获得当前路径 Pawn。'
Assert-True (-not ($startPathPatchText -match 'WalkableBy\s*\(')) '可达性接入不得全局改写 WalkableBy。'

Write-Output 'FloatingFlightReachabilitySmokeTests PASS'
