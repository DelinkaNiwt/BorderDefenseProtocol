$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-RequiredFile {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空模式文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$capabilityPath = Join-Path $floatingRoot 'FloatingFlightCapabilityResolver.cs'
$providerPath = Join-Path $floatingRoot 'IFloatingFlightNonPlayerModeProvider.cs'
$registryPath = Join-Path $floatingRoot 'FloatingFlightNonPlayerModeProviderRegistry.cs'
$resolverPath = Join-Path $floatingRoot 'FloatingFlightModeResolver.cs'
$utilityPath = Join-Path $floatingRoot 'FloatingFlightUtility.cs'

$capabilityText = Read-RequiredFile $capabilityPath
$providerText = Read-RequiredFile $providerPath
$registryText = Read-RequiredFile $registryPath
$resolverText = Read-RequiredFile $resolverPath
$utilityText = Read-RequiredFile $utilityPath

Assert-True ($capabilityText -match 'ExpressionSurfaceAccess\.ResolvePublishedProjection') '能力解析必须读取 Core 发布投影。'
Assert-True ($capabilityText -match 'PassiveResultsByIdentityKey') '能力解析必须读取被动身份结果集合。'
Assert-True ($capabilityText -match 'BDP_FloatingFlight') '能力解析必须使用浮空被动身份键。'
Assert-True ($capabilityText -match 'IsAvailable' -and $capabilityText -match 'IsPublished') '能力解析必须同时检查 IsAvailable 与 IsPublished。'

Assert-True ($providerText -match 'interface\s+IFloatingFlightNonPlayerModeProvider') '必须提供最小非玩家模式提供器接口。'
Assert-True ($providerText -match 'TryResolveMode') '非玩家模式提供器必须返回最终模式。'
Assert-True ($registryText -match 'Register') '非玩家模式注册表必须支持显式注册。'
Assert-True ($registryText -match 'Unregister') '非玩家模式注册表必须支持注销。'
Assert-True ($registryText -match 'TryResolveMode') '非玩家模式注册表必须按顺序尝试提供器。'

Assert-True ($resolverText -match 'ResolveEffectiveMode') '必须提供集中有效模式解析入口。'
Assert-True ($resolverText -match 'IsPlayerControlled|Faction\.OfPlayer') '玩家与非玩家必须在同一入口分流。'
Assert-True ($resolverText -match 'Drafted|drafted') '玩家未征召必须强制关闭浮空。'
Assert-True ($resolverText -match 'PreferredMode') '玩家已征召必须读取 Pawn 个体偏好。'
Assert-True ($resolverText -match 'FloatingFlightNonPlayerModeProviderRegistry') '非玩家模式必须通过可选提供器解析。'
Assert-True ($resolverText -match 'FloatingFlightMode\.Off') '无非玩家提供器时必须回退关闭浮空。'

Assert-True ($utilityText -match 'IsActuallyFlying') '必须提供统一实际飞行查询入口。'
Assert-True ($utilityText -match 'GetFirstHediffOfDef') '实际飞行查询必须只读取指定 Hediff。'
Assert-True (-not ($utilityText -match 'PreferredMode|pather\.Moving|Drafted')) '实际飞行查询不得读取偏好或其他实时状态。'

Write-Output 'FloatingFlightModeResolutionSmokeTests PASS'
