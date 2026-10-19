$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$trionDef = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ThingDef\Buildings\TrionFacility.xml'
$consoleDef = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ThingDef\Buildings\OperatorConsole.xml'
$trionBuilding = Join-Path $contentRoot 'Trion\Facility\Building_TrionFacility.cs'
$trionResource = Join-Path $contentRoot 'Trion\Facility\CompTrionFacilityResource.cs'
$consoleBuilding = Join-Path $contentRoot 'OperatorConsole\Building_OperatorConsole.cs'
$consoleComp = Join-Path $contentRoot 'OperatorConsole\CompOperatorConsole.cs'

foreach ($path in @($trionDef, $consoleDef, $trionBuilding, $trionResource, $consoleBuilding, $consoleComp)) {
    Assert-True (Test-Path $path) "缺少建筑发光验证文件：$path"
}

$trionDefText = Get-Content -Raw $trionDef
$consoleDefText = Get-Content -Raw $consoleDef
$trionBuildingText = Get-Content -Raw $trionBuilding
$trionResourceText = Get-Content -Raw $trionResource
$consoleBuildingText = Get-Content -Raw $consoleBuilding
$consoleCompText = Get-Content -Raw $consoleComp

Assert-True ($trionDefText -match 'CompProperties_Glower' -and $trionDefText -match '<glowRadius>4\.9</glowRadius>' -and $trionDefText -match '<glowColor>\([^<]+</glowColor>') 'Trion 中继核心必须声明原版小范围发光组件、半径和颜色。'
Assert-True ($consoleDefText -match 'CompProperties_Glower' -and $consoleDefText -match '<glowRadius>3\.5</glowRadius>' -and $consoleDefText -match '<glowColor>\([^<]+</glowColor>') '操作员控制台必须声明原版小范围发光组件、半径和颜色。'
Assert-True ($trionBuildingText -match 'class\s+Building_TrionFacility\s*:\s*Building\s*,\s*IThingGlower' -and $trionBuildingText -match 'ShouldBeLitNow\s*\(') 'Trion 中继核心必须由建筑实际运作状态实现 IThingGlower。'
Assert-True ($consoleBuildingText -match 'class\s+Building_OperatorConsole\s*:\s*Building\s*,\s*IThingGlower' -and $consoleBuildingText -match 'ShouldBeLitNow\s*\(') '操作员控制台必须由建筑实际运作状态实现 IThingGlower。'
Assert-True ($trionBuildingText -match '(?s)ShouldBeLitNow\s*\(.*?IsOperational' -and $consoleBuildingText -match '(?s)ShouldBeLitNow\s*\(.*?IsOperational') '两个建筑的点灯条件必须读取各自实际运作状态。'
Assert-True ($trionResourceText -match 'RefreshOperationalGlow' -and $trionResourceText -match 'RefreshOperationalGlowIfChanged') 'Trion 资源状态改变后必须刷新原版光照网格。'
Assert-True ($consoleCompText -match 'RefreshOperationalGlow' -and $consoleCompText -match 'RefreshOperationalGlowIfChanged') '操作员心跳或强制运作状态改变后必须刷新原版光照网格。'

Write-Host 'OperationalBuildingGlow PASS'
