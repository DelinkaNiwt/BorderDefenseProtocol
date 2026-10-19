$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-RequiredFile {
    param([string]$Path)
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空路径跟随文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$patchRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Patches'
$followerPath = Join-Path $patchRoot 'Patch_PawnPathFollower_Steps_FloatingFlight.cs'
$doorPath = Join-Path $patchRoot 'Patch_PathUtility_GetDoorCost_FloatingFlight.cs'
$followerText = Read-RequiredFile $followerPath
$doorText = Read-RequiredFile $doorPath

Assert-True ($followerText -match 'Transpiler|CodeMatcher|CodeInstruction') '路径跟随补丁必须使用精确指令接入。'
Assert-True ($followerText -match 'CanWalkIntoCell|CanEnter|CanBypassWalkableCheck|IsNextCellWalkable') '路径跟随补丁必须提供浮空通行判定包装器。'
Assert-True ($followerText -match 'Roofed|roofed') '路径跟随补丁必须识别屋顶边界。'
Assert-True ($followerText -match 'RequestActualFlight|FloatingFlightStateController') '屋顶边界必须通过状态控制器切换飞行 Hediff。'
Assert-True ($followerText -match 'GroundedForRoofSegment|GetPresentationFactor') '屋顶交接必须保留地面段标记并等待降落表现完成。'
Assert-True ($followerText -match 'FloatingFlightModeResolver\.ResolveEffectiveMode|FloatingFlightMode\.Always') '始终浮空和仅移动浮空必须采用不同的屋顶边界策略。'
Assert-True ($followerText -match 'ResetToCurrentPosition') '始终浮空遇到异常屋顶节点时必须清理旧路径并重新寻路。'
Assert-True ($followerText -match 'Building|BlocksPawn|Door') '路径跟随补丁必须处理建筑和门的浮空通行。'
Assert-True ($followerText -match 'ResolveVanillaWalkableBy|AccessTools\.Method') '路径跟随补丁必须精确识别原版 WalkableBy，而不是全局替换通行规则。'
Assert-True (-not ($followerText -match 'HarmonyPatch\(typeof\(GenGrid\)')) '路径跟随补丁不得直接给全局 GenGrid.WalkableBy 打补丁。'

Assert-True ($doorText -match 'GetDoorCost') '必须窄范围接入原版 PathUtility.GetDoorCost。'
Assert-True ($doorText -match 'Roofed|roofed') '门代价补丁必须只处理露天浮空路径。'
Assert-True ($doorText -match 'result|__result') '门代价补丁必须显式返回浮空穿门代价。'

Write-Output 'FloatingFlightPathFollowerSmokeTests PASS'
