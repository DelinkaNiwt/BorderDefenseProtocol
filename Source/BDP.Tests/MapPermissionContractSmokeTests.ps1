$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$permissionRoot = Join-Path $sourceRoot 'BDP\Core\Permissions'
$requiredFiles = @(
    'IMapPermissionProvider.cs',
    'IMapPermissionReader.cs',
    'MapPermissionContext.cs',
    'MapPermissionMapComponent.cs',
    'MapPermissionAccess.cs'
)

foreach ($file in $requiredFiles) {
    Assert-True (Test-Path (Join-Path $permissionRoot $file)) "Core 缺少地图许可契约文件：$file"
}

$allText = ($requiredFiles | ForEach-Object { Get-Content -Raw (Join-Path $permissionRoot $_) }) -join "`n"
Assert-True ($allText -match 'interface\s+IMapPermissionProvider') '缺少地图许可提供接口。'
Assert-True ($allText -match 'interface\s+IMapPermissionReader') '缺少地图许可读取接口。'
Assert-True ($allText -match 'class\s+MapPermissionMapComponent') '缺少地图许可索引组件。'
Assert-True ($allText -notmatch 'Trion|OperatorConsole|免直视') 'Core 许可设施不得耦合具体业务名称。'

Write-Host 'MapPermissionContract PASS'
