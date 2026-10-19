$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$corePermissionRoot = Join-Path $sourceRoot 'BDP\Core\Permissions'
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$providerPath = Join-Path $corePermissionRoot 'IPawnPermissionProvider.cs'
$pawnReaderPath = Join-Path $corePermissionRoot 'IPawnPermissionReader.cs'
$readerPath = Join-Path $corePermissionRoot 'IMapPermissionReader.cs'
$componentPath = Join-Path $corePermissionRoot 'MapPermissionMapComponent.cs'
$accessPath = Join-Path $corePermissionRoot 'MapPermissionAccess.cs'
$contextPath = Join-Path $corePermissionRoot 'MapPermissionContext.cs'
$keysPath = Join-Path $floatingRoot 'FloatingFlightPermissionKeys.cs'
$hediffPath = Join-Path $floatingRoot 'Hediff_FloatingFlight.cs'
$stateControllerPath = Join-Path $floatingRoot 'FloatingFlightStateController.cs'

foreach ($path in @($providerPath, $pawnReaderPath, $readerPath, $componentPath, $accessPath, $contextPath, $keysPath, $hediffPath, $stateControllerPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少 Pawn 自身许可实现文件：$path"
}

$coreText = @($providerPath, $pawnReaderPath, $readerPath, $componentPath, $accessPath, $contextPath | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw
}) -join "`n"
$pawnReaderText = Get-Content -LiteralPath $pawnReaderPath -Raw
$readerText = Get-Content -LiteralPath $readerPath -Raw
$componentText = Get-Content -LiteralPath $componentPath -Raw
$accessText = Get-Content -LiteralPath $accessPath -Raw
$contextText = Get-Content -LiteralPath $contextPath -Raw
$stateControllerText = Get-Content -LiteralPath $stateControllerPath -Raw
$hediffText = Get-Content -LiteralPath $hediffPath -Raw
$keysText = Get-Content -LiteralPath $keysPath -Raw

Assert-True ($coreText -match 'interface\s+IPawnPermissionProvider') 'Core 必须提供 Pawn 自身许可契约。'
Assert-True ($pawnReaderText -match 'interface\s+IPawnPermissionReader' -and $pawnReaderText -match 'HasSelfPermission') 'Core 必须提供独立的 Pawn 自身许可读取契约。'
Assert-True ($readerText -notmatch 'HasSelfPermission') '既有地图许可读取契约不得被 Pawn 自身许可扩展破坏。'
Assert-True ($componentText -match 'RegisterPawnProvider' -and $componentText -match 'UnregisterPawnProvider') '地图索引必须支持 Pawn 提供器生命周期登记。'
Assert-True ($componentText -match 'provider\.Pawn\s*==\s*pawn') 'Pawn 许可查询必须按 Pawn 实例隔离。'
Assert-True ($accessText -match 'HasSelfPermission') '静态许可访问入口必须暴露 Pawn 自身许可查询。'
Assert-True ($contextText -match 'HasSelfPermission') '运行时许可上下文必须暴露 Pawn 自身许可查询。'
Assert-True ($hediffText -match 'IPawnPermissionProvider' -and $hediffText -match 'PostAdd' -and $hediffText -match 'PostRemoved') '浮空 Hediff 必须作为自身许可提供器并绑定增删生命周期。'
Assert-True ($hediffText -match 'RegisterPawnProvider' -and $hediffText -match 'UnregisterPawnProvider') '浮空 Hediff 必须登记并撤销自身许可。'
Assert-True ($hediffText -match 'EnsureSelfPermissionRegistration') '浮空 Hediff 必须提供读档后可重复调用的登记入口。'
Assert-True ($stateControllerText -match 'EnsureSelfPermissionRegistration') '浮空状态收口必须在运行时重新登记读档恢复的浮空 Hediff。'
Assert-True ($keysText -match 'BDP\.FloatingFlight\.SelfActive') '浮空必须使用独立的自身许可键。'
Assert-True ($coreText -notmatch 'FloatingFlight|BDP\.Content|OperatorConsole|Trion') 'Core 自身许可层不得耦合具体业务。'

Write-Output 'PawnSelfPermissionContractSmokeTests PASS'
