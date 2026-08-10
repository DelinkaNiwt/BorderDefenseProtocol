# 事项09B：紧急脱离业务必须位于 Content，Core 只保留中性崩解扩展入口。

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'

$coreContractPath = Join-Path $coreRoot 'CombatBody\External\ICombatBodyCollapseExtensionProvider.cs'
$coreRegistryPath = Join-Path $coreRoot 'CombatBody\External\CombatBodyCollapseExtensionRegistry.cs'
$coreSessionPath = Join-Path $coreRoot 'CombatBodySession\CombatBodySessionService.cs'
$coreExitPath = Join-Path $coreRoot 'CombatBodySession\CombatBodyExitTransaction.cs'
$coreHostStatePath = Join-Path $coreRoot 'CombatBody\Bridge\HostState.cs'
$coreTrionProviderPath = Join-Path $coreRoot 'CombatBody\External\CombatBodyTrionGizmoExtensionProvider.cs'
$contentBootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'

Assert-True (Test-Path -LiteralPath $coreContractPath) 'Core must expose a neutral collapse extension contract.'
Assert-True (Test-Path -LiteralPath $coreRegistryPath) 'Core must expose a neutral collapse extension registry.'

$contentEscapeRoot = Join-Path $contentRoot 'CombatBody\Escape'
$contentRequiredNames = @(
    'Building_EmergencyEscapeBeacon.cs',
    'CombatBodyEmergencyEscapeEffects.cs',
    'CombatBodyEmergencyEscapeRouter.cs',
    'CombatBodyEmergencyEscapeResolution.cs',
    'CombatBodyEmergencyEscapeResolver.cs',
    'CombatBodyEmergencyEscapeService.cs',
    'CombatBodyEmergencyEscapeBadgeState.cs',
    'CombatBodyEmergencyEscapeBadgeStateResolver.cs',
    'CompCombatBodyEmergencyEscapeState.cs',
    'CombatBodyEmergencyEscapeExtensionProvider.cs',
    'CombatBodyEmergencyEscapeGizmoExtensionProvider.cs'
)

foreach ($name in $contentRequiredNames) {
    Assert-True (Test-Path -LiteralPath (Join-Path $contentEscapeRoot $name)) "Content 缺少紧急脱离业务文件：$name"
}

$forbiddenCorePaths = @(
    (Join-Path $coreRoot 'CombatBody\Escape'),
    (Join-Path $coreRoot 'CombatBody\Flow\CombatBodyEmergencyEscapeResolution.cs'),
    (Join-Path $coreRoot 'CombatBody\Flow\CombatBodyEmergencyEscapeResolver.cs'),
    (Join-Path $coreRoot 'CombatBody\Flow\CombatBodyEmergencyEscapeService.cs'),
    (Join-Path $coreRoot 'CombatBody\External\CombatBodyEmergencyEscapeBadgeState.cs'),
    (Join-Path $coreRoot 'CombatBody\External\CombatBodyEmergencyEscapeBadgeStateResolver.cs')
)

foreach ($path in $forbiddenCorePaths) {
    if (Test-Path -LiteralPath $path -PathType Container) {
        $remainingFiles = @(Get-ChildItem -LiteralPath $path -File -Recurse)
        Assert-True ($remainingFiles.Count -eq 0) "Core 仍保留紧急脱离业务文件：$path"
    }
    else {
        Assert-True (-not (Test-Path -LiteralPath $path)) "Core 仍保留紧急脱离业务路径：$path"
    }
}

$coreSessionText = Get-Content -LiteralPath $coreSessionPath -Raw -Encoding utf8
$coreExitText = Get-Content -LiteralPath $coreExitPath -Raw -Encoding utf8
$coreHostStateText = Get-Content -LiteralPath $coreHostStatePath -Raw -Encoding utf8
$coreTrionProviderText = Get-Content -LiteralPath $coreTrionProviderPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $contentBootstrapPath -Raw -Encoding utf8

Assert-True ($coreSessionText -notmatch 'EmergencyEscape|emergencyEscape') 'Core Session 不得直接持有紧急脱离业务。'
Assert-True ($coreExitText -notmatch 'EmergencyEscape|emergencyEscape') 'Core ExitTransaction 不得直接调用紧急脱离业务。'
Assert-True ($coreHostStateText -notmatch 'CachedCollapseEmergencyEscape|CombatBodyEmergencyEscapeResolution') 'Core HostState 不得保存紧急脱离专用缓存。'
Assert-True ($coreTrionProviderText -notmatch 'EmergencyEscape|紧急脱离') 'Core Trion 徽标提供器不得持有紧急脱离徽标业务。'

Assert-True ($coreSessionText -match 'CombatBodyCollapseExtensionRegistry\.(Prepare|Execute|Clear)') 'Core Session 必须通过中性注册表接入崩解扩展。'
Assert-True ($coreExitText -match 'CombatBodyCollapseExtensionRegistry\.Execute') 'Core ExitTransaction 必须通过中性注册表执行崩解扩展。'
Assert-True ($bootstrapText -match 'CombatBodyCollapseExtensionRegistry\.Register') 'ContentBootstrap 必须注册紧急脱离崩解扩展。'
Assert-True ($bootstrapText -match 'CombatBodyEmergencyEscapeGizmoExtensionProvider') 'ContentBootstrap 必须注册紧急脱离徽标扩展。'

$contentBeaconDefPath = Join-Path $repoRoot '1.6\Content\Defs\Buildings\CombatBody\ThingDefs_EmergencyEscapeBeacon.xml'
$contentTexturePath = Join-Path $repoRoot '1.6\Content\Textures\UI\CombatBody\EmergencyEscapeStatus.png'
Assert-True (Test-Path -LiteralPath $contentBeaconDefPath) '紧急脱离信标 Def 必须位于 Content/Defs。'
Assert-True (Test-Path -LiteralPath $contentTexturePath) '紧急脱离徽标贴图必须位于 Content/Textures。'

Write-Output 'CombatBodyEmergencyEscapeContentBoundary PASS'
