$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Config\ChipExpressionEntryConfig.cs'
$contractPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Contract\ChipExpressionEntryContract.cs'
$resultPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$snapshotBuilderPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$receiverPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\DirectTargetLineOfSightPermissionReceiver.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'
foreach ($path in @($configPath,$contractPath,$resultPath,$snapshotBuilderPath,$receiverPath)) {
    Assert-True (Test-Path $path) "缺少芯片许可接收链文件：$path"
}
$config = Get-Content -Raw $configPath
$contract = Get-Content -Raw $contractPath
$result = Get-Content -Raw $resultPath
$snapshotBuilder = Get-Content -Raw $snapshotBuilderPath
$receiver = Get-Content -Raw $receiverPath
$bootstrap = Get-Content -Raw $bootstrapPath
Assert-True ($config -match 'DeclaredPermissionKeys') '芯片表达配置必须支持显式声明许可键列表。'
Assert-True ($contract -match 'DeclaredPermissionKeys') '芯片表达契约必须传播显式声明许可键列表。'
Assert-True ($result -match 'DeclaredPermissionKeys') '正式表达结果必须保留显式声明许可键快照。'
Assert-True ($snapshotBuilder -match 'NotifyPermissionReceivers' -and $snapshotBuilder -match 'ChipFunctionPermissionReceiverRegistry\.Notify') '表达总表必须向当前 Pawn 的全部正式结果通知许可接收器。'
Assert-True ($receiver -match 'IChipFunctionPermissionReceiver' -and $receiver -match 'DeclaredPermissionKeys' -and $receiver -match 'HasPermission') '业务接收器必须按显式声明读取动态许可。'
Assert-True ($receiver -notmatch 'RequiresDirectTargetLineOfSight\s*=\s*false') '业务接收器不得全局改写正式 LOS 规格。'
Assert-True ($bootstrap -match 'DirectTargetLineOfSightPermissionReceiver' -and $bootstrap -match 'DirectTargetLineOfSightPermissionResolverRegistry') 'ContentBootstrap 必须注册许可业务接收器及其动态解析器。'

Write-Host 'ChipPermissionOptIn PASS'
