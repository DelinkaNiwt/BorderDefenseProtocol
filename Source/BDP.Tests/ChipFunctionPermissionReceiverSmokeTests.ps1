$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$permissionRoot = Join-Path $sourceRoot 'BDP\Core\Permissions'
$contextPath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeContext.cs'
foreach ($file in @('IChipFunctionPermissionReceiver.cs','ChipFunctionPermissionReceiverRegistry.cs','IDirectTargetLineOfSightPermissionResolver.cs','DirectTargetLineOfSightPermissionResolverRegistry.cs')) {
    Assert-True (Test-Path (Join-Path $permissionRoot $file)) "Core 缺少芯片许可接收文件：$file"
}

$receiverText = Get-Content -Raw (Join-Path $permissionRoot 'IChipFunctionPermissionReceiver.cs')
$registryText = Get-Content -Raw (Join-Path $permissionRoot 'ChipFunctionPermissionReceiverRegistry.cs')
$resolverRegistryText = Get-Content -Raw (Join-Path $permissionRoot 'DirectTargetLineOfSightPermissionResolverRegistry.cs')
$contextText = Get-Content -Raw $contextPath
Assert-True ($receiverText -match 'Receive\s*\(') '许可接收器必须暴露 Receive 入口。'
Assert-True ($registryText -match 'Register' -and $registryText -match 'Clear') '许可接收注册表必须支持注册和清理。'
Assert-True ($resolverRegistryText -match 'Register' -and $resolverRegistryText -match 'Clear' -and $resolverRegistryText -match 'ShouldRelax') '动态直视许可解析器注册表必须支持注册、清理和查询。'
Assert-True ($contextText -match 'Permission|许可') '运行时上下文必须携带许可事实入口。'
Assert-True ($registryText -notmatch 'LineOfSight|免直视|OperatorConsole') 'Core 注册表不得实现具体业务效果。'
Assert-True ($resolverRegistryText -notmatch 'OperatorConsole|BDP_ChipFunction_FujinRangedStrike|BDP_ChipFunction_Fujin') 'Core 动态直视解析器注册表不得耦合具体业务名称。'

Write-Host 'ChipFunctionPermissionReceiver PASS'
