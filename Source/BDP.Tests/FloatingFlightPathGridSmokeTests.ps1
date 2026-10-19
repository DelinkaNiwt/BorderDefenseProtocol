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

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空寻路文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$pathingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Pathing'
$patchRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Patches'
$kindPath = Join-Path $pathingRoot 'FloatingFlightPathKind.cs'
$utilityPath = Join-Path $pathingRoot 'FloatingFlightPathUtility.cs'
$hybridPath = Join-Path $pathingRoot 'PathGrid_FloatingHybrid.cs'
$alwaysPath = Join-Path $pathingRoot 'PathGrid_FloatingAlways.cs'
$contextPatchPath = Join-Path $patchRoot 'Patch_FloatingFlightPathContext.cs'
$roofEventPath = Join-Path $pathingRoot 'MapComponent_FloatingFlightPathing.cs'

$kindText = Read-RequiredFile $kindPath
$utilityText = Read-RequiredFile $utilityPath
$hybridText = Read-RequiredFile $hybridPath
$alwaysText = Read-RequiredFile $alwaysPath
$contextText = Read-RequiredFile $contextPatchPath
$roofText = Read-RequiredFile $roofEventPath
$allGridText = $hybridText + "`n" + $alwaysText
$allPathText = $allGridText + "`n" + $utilityText

Assert-True ($kindText -match 'FloatingFlightPathKind') '必须定义独立的浮空路径种类。'
Assert-True ($kindText -match 'Vanilla\s*=\s*0' -and $kindText -match 'Hybrid\s*=\s*1' -and $kindText -match 'Always\s*=\s*2') '浮空路径种类必须包含原版、混合和始终浮空。'
Assert-True ($utilityText -match 'ResolvePathKind') '必须集中解析 Pawn 当前路径种类。'
Assert-True ($utilityText -match 'PendingSafeLanding') '安全降落期间必须优先使用混合路径。'
Assert-True ($utilityText -match 'FloatingFlightMode\.MoveOnly' -and $utilityText -match 'FloatingFlightMode\.Always') '路径种类必须区分仅移动与始终浮空。'
Assert-True ($utilityText -match 'Get\(' -and $utilityText -match 'FloatingFlightDefOf') '路径工具必须从正式 DefOf 解析自定义网格。'

Assert-True ($hybridText -match 'class\s+PathGrid_FloatingHybrid\s*:\s*PathGrid') '混合网格必须继承原版 PathGrid。'
Assert-True ($alwaysText -match 'class\s+PathGrid_FloatingAlways\s*:\s*PathGrid') '始终浮空网格必须继承原版 PathGrid。'
Assert-True ($allGridText -match 'CalculatedCostAt') '两套浮空网格必须覆写原版代价计算入口。'
Assert-True ($hybridText -match 'Roofed|roofed|Normal') '混合网格必须把屋顶格交给原版地面规则。'
Assert-True ($alwaysText -match 'ImpassableCost|10000') '始终浮空网格必须把所有屋顶格标记为不可通行。'
Assert-True ($allPathText -match 'Fire|fire') '浮空网格必须保留火焰避让代价。'
Assert-True (-not ($allPathText -match 'forcePassableByFlyingPawns')) '浮空网格不能依赖原版有限的 forcePassableByFlyingPawns 标记。'

Assert-True ($contextText -match 'GetPathContext') '必须接入 Pawn.GetPathContext。'
Assert-True ($contextText -match 'Pathing.*For|For.*TraverseParms|TraverseParms') '必须接入带 Pawn 的 Pathing.For。'
Assert-True ($contextText -match 'ResolvePathKind') '上下文补丁必须复用集中路径种类解析。'
Assert-True ($roofText -match 'RoofChanged\s*\+=') '必须订阅原版真实屋顶变化事件。'
Assert-True ($roofText -match 'MapRemoved' -and $roofText -match 'RoofChanged\s*-=') '地图移除必须退订屋顶事件。'
Assert-True ($roofText -match 'RecalculatePerceivedPathCostAt') '屋顶变化必须重算两套浮空网格。'

Write-Output 'FloatingFlightPathGridSmokeTests PASS'
