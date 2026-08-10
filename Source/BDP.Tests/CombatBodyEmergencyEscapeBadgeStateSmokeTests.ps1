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

# 紧急脱离三态属于 CombatBody 业务适配，Trion 核心只能消费通用徽标数据。
$statePath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeBadgeState.cs'
$stateResolverPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeBadgeStateResolver.cs'
$escapeResolverPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeResolver.cs'
$providerPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeGizmoExtensionProvider.cs'
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$texturePath = Join-Path $repoRoot '1.6\Content\Textures\UI\CombatBody\EmergencyEscapeStatus.png'

Assert-True (Test-Path -LiteralPath $statePath) 'Emergency escape badge state enum must exist.'
Assert-True (Test-Path -LiteralPath $stateResolverPath) 'Emergency escape badge state resolver must exist.'
Assert-True (Test-Path -LiteralPath $escapeResolverPath) 'CombatBodyEmergencyEscapeResolver.cs must exist.'
Assert-True (Test-Path -LiteralPath $providerPath) 'CombatBodyTrionGizmoExtensionProvider.cs must exist.'
Assert-True (Test-Path -LiteralPath $texturePath) 'Emergency escape badge texture must exist.'

$stateText = Get-Content -LiteralPath $statePath -Raw -Encoding utf8
$stateResolverText = Get-Content -LiteralPath $stateResolverPath -Raw -Encoding utf8
$escapeResolverText = Get-Content -LiteralPath $escapeResolverPath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Recurse -Filter '*.cs' | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

# 三态模型必须明确区分无芯片、已搭载未就绪和已就绪。
Assert-True ($stateText -match '\bNotInstalled\b') 'Badge state must represent no mounted emergency escape chip.'
Assert-True ($stateText -match '\bInstalledNotReady\b') 'Badge state must represent a mounted but inactive emergency escape chip.'
Assert-True ($stateText -match '\bReady\b') 'Badge state must represent ready emergency escape.'

# 状态解析器只聚合正式读取面，并在崩解阶段保留入口缓存真值。
Assert-True ($stateResolverText -match 'TriggerSurfaceAccess\.ResolveLoadoutReader\(pawn\)') 'Mounted state must read Trigger formal loadout.'
Assert-True ($stateResolverText -match 'GetModExtension<ChipExpressionConfig>\(\)') 'Mounted state must read the formal chip definition.'
Assert-True ($stateResolverText -match 'IsBindingMirror') 'Mounted state must ignore binding mirror duplicates.'
Assert-True ($stateResolverText -match 'ChipExpressionEntryKindConfig\.Passive') 'Mounted state must require a Passive declaration.'
Assert-True ($stateResolverText -match 'EmergencyEscapePassiveKey') 'Mounted state must share the formal emergency escape key.'
Assert-True (
    ($stateResolverText -match 'ContainsEmergencyEscape\(config\.Entries\)') -and
    ($stateResolverText -notmatch 'ContainsEmergencyEscape\(config\.Modes\)') -and
    ($stateResolverText -notmatch 'ChipExpressionModeOperationConfig|\.Operations\b')
) 'Mounted state must scan the unified entry catalog instead of removed mode operations.'
Assert-True ($stateResolverText -match 'CompCombatBodyEmergencyEscapeState') 'Collapsing state must read Content-owned cached readiness.'
Assert-True ($stateResolverText -match 'CombatBodyEmergencyEscapeResolver') 'Ready state must reuse the formal emergency escape resolver.'
Assert-True ($escapeResolverText -match 'internal const string EmergencyEscapePassiveKey = "EmergencyEscape";') 'Emergency escape key must have one shared declaration.'

# CombatBody 徽标提供器只消费三态结果，不回头扫描来源系统。
Assert-True ($providerText -match 'CombatBodyEmergencyEscapeBadgeStateResolver') 'Content badge provider must consume the three-state resolver.'
Assert-True ($providerText -notmatch 'case\s+CombatBodyEmergencyEscapeBadgeState\.NotInstalled') 'No-chip state must suppress the emergency escape badge.'
Assert-True ($providerText -match 'CombatBodyEmergencyEscapeBadgeState\.InstalledNotReady') 'Mounted inactive state must remain distinct.'
Assert-True ($providerText -match 'CombatBodyEmergencyEscapeBadgeState\.Ready') 'Ready state must remain distinct.'
Assert-True ($providerText -match '紧急脱离：未就绪') 'Mounted inactive badge must explain that emergency escape is not ready.'
Assert-True ($providerText -match '紧急脱离：已就绪') 'Ready badge must explain that emergency escape is ready.'
Assert-True ($providerText -match 'ContentFinder<Texture2D>\.Get\("UI/CombatBody/EmergencyEscapeStatus"\)') 'CombatBody provider must own the emergency escape texture.'
Assert-True ($providerText -match 'icon:\s*EmergencyEscapeIcon') 'Emergency escape badge must pass its own texture through the generic contract.'
Assert-True ($providerText -notmatch 'TriggerSurfaceAccess|ChipSurfaceAccess|ExpressionSurfaceAccess') 'CombatBody badge provider must not scan source systems directly.'

# Trion 核心保持中性，不认识紧急脱离键、状态或专用图形。
Assert-True ($trionText -notmatch 'EmergencyEscape|emergency_escape|紧急脱离') 'Trion core must not know emergency escape business.'

Write-Output 'CombatBodyEmergencyEscapeBadgeStateSmokeTests PASS'
