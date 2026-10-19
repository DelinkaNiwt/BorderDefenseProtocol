$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$facilityRoot = Join-Path $sourceRoot 'BDP.Content\Trion\Facility'
$providerPath = Join-Path $facilityRoot 'TrionRelayCorePermissionProvider.cs'
$mapPath = Join-Path $facilityRoot 'TrionFacilityMapComponent.cs'
Assert-True (Test-Path $providerPath) '缺少 Trion 中继核心许可提供器。'
$providerText = Get-Content -Raw $providerPath
$mapText = Get-Content -Raw $mapPath
Assert-True ($providerText -match 'IMapPermissionProvider') 'Trion 提供器必须实现 Core 中性许可接口。'
Assert-True ($providerText -match 'BDP\.TrionRelayCore\.Active') 'Trion 提供器必须发布固定上游许可键。'
Assert-True ($providerText -match 'HasActivePlayerTrionFacility') 'Trion 提供器必须复用现有设施活动判定。'
Assert-True ($mapText -match 'MapPermission') 'Trion 地图组件必须接入地图许可索引。'

Write-Host 'TrionRelayCorePermission PASS'
