$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$corePermissionRoot = Join-Path $sourceRoot 'BDP\Core\Permissions'
$targetingRoot = Join-Path $sourceRoot 'BDP\Core\AttackExecution'
$receiverPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\DirectTargetLineOfSightPermissionReceiver.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'
$targetingPath = Join-Path $targetingRoot 'AttackExecutionTargetingSource.cs'
$houndPath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$hornetPath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ComboDef\Hornet.xml'

foreach ($path in @(
        (Join-Path $corePermissionRoot 'IGroundTargetingPermissionResolver.cs'),
        (Join-Path $corePermissionRoot 'GroundTargetingPermissionResolverRegistry.cs'),
        (Join-Path $corePermissionRoot 'GroundTargetingPermissionAccess.cs'),
        (Join-Path $targetingRoot 'TargetingParametersCopyUtility.cs'),
        $receiverPath, $bootstrapPath, $targetingPath, $houndPath, $hornetPath)) {
    Assert-True (Test-Path $path) "缺少地面目标许可接入文件：$path"
}

$resolver = Get-Content -Raw (Join-Path $corePermissionRoot 'IGroundTargetingPermissionResolver.cs')
$registry = Get-Content -Raw (Join-Path $corePermissionRoot 'GroundTargetingPermissionResolverRegistry.cs')
$access = Get-Content -Raw (Join-Path $corePermissionRoot 'GroundTargetingPermissionAccess.cs')
$copy = Get-Content -Raw (Join-Path $targetingRoot 'TargetingParametersCopyUtility.cs')
$receiver = Get-Content -Raw $receiverPath
$bootstrap = Get-Content -Raw $bootstrapPath
$targeting = Get-Content -Raw $targetingPath
$hound = Get-Content -Raw $houndPath
$hornet = Get-Content -Raw $hornetPath

Assert-True ($resolver -match 'interface\s+IGroundTargetingPermissionResolver' -and $resolver -match 'TryResolveGroundTargeting') 'Core 必须提供中性地面目标许可解析器契约。'
Assert-True ($registry -match 'TryResolveGroundTargeting' -and $registry -match 'IGroundTargetingPermissionResolver') 'Core 必须提供地面目标许可解析器注册表。'
Assert-True ($access -match 'DeclaredPermissionKeys' -and $access -match 'GroundTargetingPermissionResolverRegistry') 'Core 地面目标许可访问入口必须从正式结果构造运行时上下文。'
Assert-True ($receiver -match 'IGroundTargetingPermissionResolver' -and $receiver -match 'TryResolveGroundTargeting' -and $receiver -match 'HasPermission') '操作员控制台接收器必须提供地面目标许可查询。'
Assert-True ($bootstrap -match 'GroundTargetingPermissionResolverRegistry\.Register') 'ContentBootstrap 必须注册地面目标许可解析器。'
Assert-True ($copy -match 'class\s+TargetingParametersCopyUtility' -and $copy -match 'onlyTargetFactions' -and $copy -match 'validator') '目标参数必须完整复制，不能只复制空地开关。'
Assert-True ($targeting -match 'TargetingParametersCopyUtility\.Clone' -and $targeting -match 'GroundTargetingPermissionAccess' -and $targeting -match 'ApplyGroundTargetingPermission') '目标阶段必须先使用副本，并在模块执行后应用许可门控。'
Assert-True ($hound -match '(?s)<defName>BDP_ChipFunction_Hound</defName>.*?<DeclaredPermissionKeys>\s*<li>BDP\.OperatorConsole\.Active</li>\s*</DeclaredPermissionKeys>') '猎犬必须声明操作员控制台许可键。'
Assert-True ($hornet -match '<DeclaredPermissionKeys>\s*<li>BDP\.OperatorConsole\.Active</li>\s*</DeclaredPermissionKeys>') '黄蜂必须声明操作员控制台许可键。'
Assert-True ($receiver -notmatch 'HighAngleShooting|HomingModule') '许可接收器不得耦合高角模块或追踪模块。'

Write-Host 'GroundTargetingPermission PASS'
