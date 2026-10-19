$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$accessPath = Join-Path $sourceRoot 'BDP\Core\Permissions\DirectTargetLineOfSightPermissionAccess.cs'
$resolverPath = Join-Path $sourceRoot 'BDP\Core\Permissions\DirectTargetLineOfSightPermissionResolverRegistry.cs'
$mapPermissionPath = Join-Path $sourceRoot 'BDP\Core\Permissions\MapPermissionMapComponent.cs'
$consoleBuildingPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\Building_OperatorConsole.cs'
$consoleProviderPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\OperatorConsolePermissionProvider.cs'
$trionProviderPath = Join-Path $sourceRoot 'BDP.Content\Trion\Facility\TrionRelayCorePermissionProvider.cs'
$abilityShootPath = Join-Path $sourceRoot 'BDP\Core\Abilities\BdpVerb_AbilityShoot.cs'
$sourceDeclarationPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs'
$abilityHostPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$runtimeCoordinatorPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'

foreach ($path in @($accessPath, $resolverPath, $mapPermissionPath, $consoleBuildingPath, $consoleProviderPath, $trionProviderPath, $abilityShootPath, $sourceDeclarationPath, $abilityHostPath, $runtimeCoordinatorPath)) {
    Assert-True (Test-Path $path) "缺少风刃直视诊断验证文件：$path"
}

$access = Get-Content -Raw $accessPath
$resolver = Get-Content -Raw $resolverPath
$mapPermission = Get-Content -Raw $mapPermissionPath
$consoleBuilding = Get-Content -Raw $consoleBuildingPath
$consoleProvider = Get-Content -Raw $consoleProviderPath
$trionProvider = Get-Content -Raw $trionProviderPath
$abilityShoot = Get-Content -Raw $abilityShootPath
$sourceDeclaration = Get-Content -Raw $sourceDeclarationPath
$abilityHost = Get-Content -Raw $abilityHostPath
$runtimeCoordinator = Get-Content -Raw $runtimeCoordinatorPath

Assert-True ($access -match 'BdpDiagnostics\.AttackExecutionThrottled' -and $access -match 'DeclaredPermissionKeys' -and $access -match 'ShouldRelaxDirectTargetLineOfSight') '直视许可访问层必须记录结果 ID、声明键和最终解析结果。'
Assert-True ($resolver -match 'BdpDiagnostics\.AttackExecutionThrottled' -and $resolver -match 'resolver') '直视许可解析器注册表必须记录解析器链路结果。'
Assert-True ($mapPermission -match 'BdpDiagnostics\.Throttled' -and $mapPermission -match 'permissionKey') '地图许可查询必须记录许可键、提供者数量和查询结果。'
Assert-True ($consoleBuilding -match 'BdpDiagnostics\.(Once|Throttled)' -and $consoleBuilding -match 'MapPermissionMapComponent') '操作员控制台必须记录许可提供器是否登记到地图索引。'
Assert-True ($consoleProvider -match 'BdpDiagnostics\.Throttled' -and $consoleProvider -match 'IsOperational') '操作员控制台许可提供器必须记录运作判定。'
Assert-True ($trionProvider -match 'BdpDiagnostics\.Throttled' -and $trionProvider -match 'IsActive') 'Trion 中继核心许可提供器必须记录上游运作判定。'
Assert-True ($abilityShoot -match 'BdpDiagnostics\.AttackExecutionThrottled' -and $abilityShoot -match 'relaxDirectTargetLineOfSight') '风刃 Ability 最终目标判定必须记录是否走放宽直视分支。'
Assert-True ($sourceDeclaration -match 'BdpDiagnostics\.Throttled' -and $sourceDeclaration -match 'stage=source_declaration' -and $sourceDeclaration -match 'DeclaredPermissionKeys' -and $sourceDeclaration -match 'OutputChannel\.Ability' -and $sourceDeclaration -match 'AbilityDefName') '表达式来源声明边界必须记录 Ability 的声明许可键。'
Assert-True ($abilityHost -match 'BdpDiagnostics\.Throttled' -and $abilityHost -match 'stage=ability_publication' -and $abilityHost -match 'DeclaredPermissionKeys' -and $abilityHost -match 'ResultId' -and $abilityHost -match 'AbilityDefName') 'Ability 发布边界必须记录结果的声明许可键。'
Assert-True ($runtimeCoordinator -match 'BdpDiagnostics\.Throttled' -and $runtimeCoordinator -match '"projection_rebuild_enter"' -and $runtimeCoordinator -match '"projection_rebuild_result"' -and $runtimeCoordinator -match '"projection_published"' -and $runtimeCoordinator -match '"projection_host_sync_returned"') 'Trigger 投影生命周期必须记录重建、生成、发布和宿主同步四个大方向边界。'

Write-Host 'WindBladeDirectLosDiagnostics PASS'
