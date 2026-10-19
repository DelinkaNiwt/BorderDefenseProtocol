$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$corePermissionRoot = Join-Path $sourceRoot 'BDP\Core\Permissions'
$receiverPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\DirectTargetLineOfSightPermissionReceiver.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'
$formalHostPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$shootPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_Shoot.cs'
$abilityShootPath = Join-Path $sourceRoot 'BDP\Core\Abilities\BdpVerb_AbilityShoot.cs'
$targetingPath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$stagesPath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
foreach ($path in @(
        (Join-Path $corePermissionRoot 'IDirectTargetLineOfSightPermissionResolver.cs'),
        (Join-Path $corePermissionRoot 'DirectTargetLineOfSightPermissionResolverRegistry.cs'),
        (Join-Path $corePermissionRoot 'DirectTargetLineOfSightPermissionAccess.cs'),
        $receiverPath, $bootstrapPath, $formalHostPath, $shootPath, $abilityShootPath, $targetingPath, $stagesPath)) {
    Assert-True (Test-Path $path) "缺少动态直视许可接入文件：$path"
}

$resolver = Get-Content -Raw (Join-Path $corePermissionRoot 'IDirectTargetLineOfSightPermissionResolver.cs')
$resolverRegistry = Get-Content -Raw (Join-Path $corePermissionRoot 'DirectTargetLineOfSightPermissionResolverRegistry.cs')
$access = Get-Content -Raw (Join-Path $corePermissionRoot 'DirectTargetLineOfSightPermissionAccess.cs')
$receiver = Get-Content -Raw $receiverPath
$bootstrap = Get-Content -Raw $bootstrapPath
$formalHost = Get-Content -Raw $formalHostPath
$shoot = Get-Content -Raw $shootPath
$abilityShoot = Get-Content -Raw $abilityShootPath
$targeting = Get-Content -Raw $targetingPath
$stages = Get-Content -Raw $stagesPath

Assert-True ($resolver -match 'interface\s+IDirectTargetLineOfSightPermissionResolver' -and $resolver -match 'MapPermissionContext') 'Core 必须提供中性直视许可解析器契约。'
Assert-True ($resolverRegistry -match 'ShouldRelaxDirectTargetLineOfSight' -and $resolverRegistry -match 'IDirectTargetLineOfSightPermissionResolver') 'Core 必须提供中性直视许可解析器查询注册表。'
Assert-True ($access -match 'DeclaredPermissionKeys' -and $access -match 'MapPermissionContext' -and $access -match 'DirectTargetLineOfSightPermissionResolverRegistry') '许可访问辅助必须从正式结果构造运行时上下文并即时查询。'
Assert-True ($receiver -match 'IChipFunctionPermissionReceiver' -and $receiver -match 'IDirectTargetLineOfSightPermissionResolver' -and $receiver -match 'DeclaredPermissionKeys' -and $receiver -match 'HasPermission') 'Content 接收器必须同时参与事实通知和动态直视解析。'
Assert-True ($bootstrap -match 'ChipFunctionPermissionReceiverRegistry\.Register' -and $bootstrap -match 'DirectTargetLineOfSightPermissionResolverRegistry\.Register') 'ContentBootstrap 必须注册两类接收入口。'
Assert-True ($formalHost -match 'DirectTargetLineOfSightPermissionAccess') '普通 formal host 必须读取动态直视许可。'
Assert-True ($shoot -match 'TryFindShootLineFromTo' -and $shoot -match 'CanHitTargetFrom') '普通远程宿主必须保留现有非虚拟射击线 fallback 边界。'
Assert-True ($abilityShoot -match 'DirectTargetLineOfSightPermissionAccess' -and $abilityShoot -match 'TryPrepareSingleShotSurface') 'Ability 射击必须在表面准备和目标判定链读取动态直视许可。'
Assert-True ($targeting -match 'DirectTargetLineOfSightPermissionAccess' -and $targeting -match 'IsOutOfRange') '手动目标边界必须动态放宽直视但保留射程检查。'
Assert-True ($stages -match 'DirectTargetLineOfSightPermissionAccess') 'dual 执行侧必须读取同一动态直视许可。'

$presetsPath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$presets = Get-Content -Raw $presetsPath
$fujinBlock = [regex]::Match($presets, '(?s)<defName>BDP_ChipFunction_FujinRangedStrike</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
Assert-True ($fujinBlock.Success -and $fujinBlock.Value -match 'DeclaredPermissionKeys' -and $fujinBlock.Value -match 'BDP\.OperatorConsole\.Active') '风刃必须显式接入操作员控制台许可。'

Write-Host 'DynamicDirectTargetLineOfSightPermission PASS'
