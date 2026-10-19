$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$contentFiles = (Get-ChildItem -LiteralPath $contentRoot -Recurse -Filter '*.cs' | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw
}) -join "`n"
$modRoot = Split-Path -Parent $sourceRoot
$contentProject = Get-Content (Join-Path $sourceRoot 'BDP.Content\BDP.Content.csproj') -Raw
$coreProject = Get-Content (Join-Path $sourceRoot 'BDP\BDP.csproj') -Raw

Assert-True ($contentFiles -match 'BDP_Hediff_FloatingFlight') '正式浮空实现必须以飞行 Hediff 为事实载体。'
Assert-True ($contentFiles -match 'GameComponent_FloatingFlight') '正式浮空偏好必须由 GameComponent 保存。'
Assert-True (-not ($contentFiles -match 'class\s+Comp_')) '正式浮空功能不应新增 Pawn ThingComp。'
Assert-True ($contentFiles -match 'FloatingFlightGizmoProvider') '正式浮空按钮必须已接线。'
Assert-True ($contentFiles -match 'FloatingFlightPresentationUtility') '正式浮空视觉必须已接线。'
Assert-True ($contentFiles -match 'Reachability|FloodFill') '正式浮空寻路必须已接线。'
Assert-True ($contentFiles -match 'Roofed') '正式浮空实现必须统一识别所有屋顶。'
Assert-True ($contentFiles -match 'sustainTrionPerSecond') '未来资源消耗必须保留配置字段。'
Assert-True (-not ($coreProject -match 'BDP.Content|FloatingFlight')) 'Core 工程不得依赖浮空业务或 Content。'
Assert-True ($contentProject -match 'BDP.csproj') 'Content 必须建立在 Core 正式基础设施之上。'

Write-Output 'FloatingFlightIntegrationSmokeTests PASS'
