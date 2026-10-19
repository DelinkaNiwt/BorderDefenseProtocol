$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-CompTickBlock {
    param([string]$Text)

    $match = [regex]::Match(
        $Text,
        'public\s+override\s+void\s+CompTick\(\)(?<body>.*?)(?=\r?\n\s*///\s*<summary>)',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    Assert-True $match.Success '未找到目标组件 CompTick。'
    return $match.Groups['body'].Value
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$trionText = Get-Content -Raw (Join-Path $sourceRoot 'BDP.Content\Trion\Facility\CompTrionFacilityResource.cs')
$consoleText = Get-Content -Raw (Join-Path $sourceRoot 'BDP.Content\OperatorConsole\CompOperatorConsole.cs')
$trionTick = Get-CompTickBlock $trionText
$consoleTick = Get-CompTickBlock $consoleText

Assert-True ($trionTick -notmatch 'RefreshOperationalGlow\(\)') 'Trion 中继核心不得在每个组件 Tick 无条件重算光照。'
Assert-True ($consoleTick -notmatch 'RefreshOperationalGlow\(\)') '操作员控制台不得在每个组件 Tick 无条件重算光照。'
Assert-True ($consoleText -notmatch 'ReceiveCompSignal') '操作员控制台不应重复实现原版电力光照信号处理。'

$coreRoot = $sourceRoot
$permissionMapText = Get-Content -Raw (Join-Path $coreRoot 'BDP\Core\Permissions\MapPermissionMapComponent.cs')
$operatorBuildingText = Get-Content -Raw (Join-Path $sourceRoot 'BDP.Content\OperatorConsole\Building_OperatorConsole.cs')
$trionBuildingText = Get-Content -Raw (Join-Path $sourceRoot 'BDP.Content\Trion\Facility\Building_TrionFacility.cs')
$trionMapText = Get-Content -Raw (Join-Path $sourceRoot 'BDP.Content\Trion\Facility\TrionFacilityMapComponent.cs')
Assert-True ($trionBuildingText -match 'NotifyOperationalStateChanged' -and $trionMapText -match 'NotifyPermissionStateChanged') 'Trion 运作状态变化必须由地图层聚合后通知下游光照刷新。'
Assert-True ($permissionMapText -match 'RegisterListener') '地图许可索引必须提供状态变化监听登记入口。'
Assert-True ($permissionMapText -match 'NotifyPermissionStateChanged') '地图许可索引必须提供状态变化通知入口。'
Assert-True ($operatorBuildingText -match 'RegisterListener') '操作员控制台必须登记地图许可状态监听。'
Assert-True ($operatorBuildingText -match 'UnregisterListener') '操作员控制台离图时必须解除地图许可状态监听。'
Assert-True ($trionMapText -match 'TrionRelayCorePermissionKey') 'Trion 地图聚合通知必须使用自己的稳定许可键。'

Write-Host 'OperationalBuildingGlowSynchronization PASS'
