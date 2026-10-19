$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$consoleRoot = Join-Path $sourceRoot 'BDP.Content\OperatorConsole'
$compText = Get-Content -Raw (Join-Path $consoleRoot 'CompOperatorConsole.cs')
$buildingText = Get-Content -Raw (Join-Path $consoleRoot 'Building_OperatorConsole.cs')
$providerText = Get-Content -Raw (Join-Path $consoleRoot 'OperatorConsolePermissionProvider.cs')
$workText = Get-Content -Raw (Join-Path $consoleRoot 'WorkGiver_OperateOperatorConsole.cs')
$languageText = Get-Content -Raw (Join-Path (Split-Path -Parent $sourceRoot) 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml')
$thingText = Get-Content -Raw (Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ThingDef\Buildings\OperatorConsole.xml')

Assert-True ($compText -match 'if\s*\(forcedOperational\)' -and $compText -match 'HasPower' -and $compText -match 'HasCurrentOperatorHeartbeat' -and $compText -match 'HasTrionRelayCorePermission') '控制台状态必须集中组合三项真实前置并支持强制覆盖。'
Assert-True ($providerText -match 'console\.IsOperational' -and $providerText -notmatch 'HasPower|HasCurrentOperatorHeartbeat|HasTrionRelayCorePermission') '下游许可提供器只能依赖控制台 IsOperational。'
Assert-True ($workText -match 'if\s*\(!forced\)' -and $workText -match '直接指派') '第一版操作工作必须只响应玩家直接指派。'
Assert-True ($buildingText -match 'CanBeOperatedBy[\s\S]*HasPawnOperationPrerequisites[\s\S]*HasOperationalPrerequisites') '控制台工作入口必须在缺电或缺少 Trion 许可时拒绝操作。'
Assert-True ($buildingText -match 'HasPawnOperationPrerequisites' -and $buildingText -match 'HasOperationalPrerequisites') '控制台必须拆分操作员资格与通电、Trion 许可前提，供入口显示准确失败原因。'
Assert-True ($workText -match 'JobFailReason\.Is' -and $workText -match 'BDP_OperatorConsole_CannotOperate_NoPower' -and $workText -match 'BDP_OperatorConsole_CannotOperate_NoTrionPermission') '缺电或缺少 Trion 许可时，直接指派入口必须保留为灰显禁用项并提供原因。'
Assert-True ($languageText -match 'BDP_OperatorConsole_CannotOperate_NoPower' -and $languageText -match 'BDP_OperatorConsole_CannotOperate_NoTrionPermission' -and $languageText -match 'BDP_OperatorConsole_CannotOperate_NoPowerAndTrion') '灰显入口失败原因必须来自简体中文语言包。'
Assert-True ($buildingText -match 'SpawnSetup' -and $buildingText -match 'DeSpawn' -and $buildingText -match 'MapPermissionMapComponent') '控制台必须随建筑生命周期登记和移除许可提供器。'
Assert-True ($thingText -match '<size>\(3,2\)</size>' -and $thingText -match '<drawSize>\(5,4\)</drawSize>' -and $thingText -match 'Things/Building/Misc/CommsConsole') '控制台 Def 必须保持确认的 3×2 占地、5×4 视觉尺寸和通信台贴图。'

Write-Host 'OperatorConsolePermissionChain PASS'
