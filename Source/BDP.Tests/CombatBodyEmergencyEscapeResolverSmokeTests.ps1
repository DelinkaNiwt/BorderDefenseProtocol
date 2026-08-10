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

$resolverPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeResolver.cs'
$resolutionPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeResolution.cs'
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$hostStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\HostState.cs'
$providerPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeExtensionProvider.cs'
$expressionReaderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Contracts\IExpressionReader.cs'
$resultKindPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionResultKind.cs'
$devHarnessChipDefsPath = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_PassiveMixed.xml'

Assert-True -Condition (Test-Path -LiteralPath $resolverPath) -Message 'CombatBodyEmergencyEscapeResolver must exist.'
Assert-True -Condition (Test-Path -LiteralPath $resolutionPath) -Message 'CombatBodyEmergencyEscapeResolution must exist.'

$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$resolutionText = Get-Content -LiteralPath $resolutionPath -Raw -Encoding utf8
$exitTransactionText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8
$hostStateText = Get-Content -LiteralPath $hostStatePath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$expressionReaderText = Get-Content -LiteralPath $expressionReaderPath -Raw -Encoding utf8
$resultKindText = Get-Content -LiteralPath $resultKindPath -Raw -Encoding utf8
$devHarnessChipDefsText = Get-Content -LiteralPath $devHarnessChipDefsPath -Raw -Encoding utf8

Assert-True -Condition ($expressionReaderText -match 'TriggerCombatProjectionState GetCombatProjection\(Pawn pawn\);') -Message 'Expression reader must expose the published combat projection.'
Assert-True -Condition ($expressionReaderText -match 'GetPassiveResults') -Message 'Expression reader must expose formal Passive queries.'
Assert-True -Condition ($expressionReaderText -match 'HasPassiveKey') -Message 'Expression reader must expose Passive key checks.'
Assert-True -Condition ($expressionReaderText -match 'TryGetPassive') -Message 'Expression reader must expose single Passive lookup.'
Assert-True -Condition ($resultKindText -match 'Passive') -Message 'ExpressionResultKind must include Passive.'

Assert-True -Condition ($resolverText -match 'ExpressionSurfaceAccess\.ResolvePublishedProjection\(pawn\)') -Message 'Resolver must read the public published expression projection.'
Assert-True -Condition ($resolverText -match 'PassiveResultsByKey') -Message 'Resolver must use the public Passive result index.'
Assert-True -Condition ($resolverText -match 'TryGetCompositeReference') -Message 'Emergency escape resolver must read public composite references when passive result is composite.'
Assert-True -Condition ($resolverText -notmatch 'result\.ResultKind != ExpressionResultKind\.Passive') -Message 'Resolver must stop owning raw Passive scan logic.'
Assert-True -Condition ($resolverText -notmatch 'ChipSurfaceAccess|ChipTrionContract|SupportsEmergencyEscape|TriggerSurfaceAccess\.ResolveLoadoutReader') -Message 'Resolver must not scan chip declaration or Trigger loadout internals directly.'
Assert-True -Condition ($resolverText -notmatch 'TriggerCollapse|EnterCooldown|RequestRelease|RequestDeactivate') -Message 'Resolver must not execute CombatBody flow directly.'
Assert-True -Condition ($resolutionText -match 'public bool IsAvailable;') -Message 'Resolution must expose IsAvailable.'
Assert-True -Condition ($resolutionText -match 'public List<ExpressionPublishedSourceReference> SourceReferences') -Message 'Emergency escape resolution must retain multi-source references.'
Assert-True -Condition ($resolutionText -notmatch 'public ExpressionPublishedSourceReference SourceReference') -Message 'Emergency escape resolution must stop exposing a single source reference.'

Assert-True -Condition ($hostStateText -notmatch 'CachedCollapseEmergencyEscape|CombatBodyEmergencyEscapeResolution') -Message 'HostState must not persist a business-specific emergency escape resolution.'
Assert-True -Condition ($exitTransactionText -match 'CombatBodyCollapseExtensionRegistry\.Execute') -Message 'CombatBodyExitTransaction must execute the neutral collapse extension registry.'
Assert-True -Condition ($exitTransactionText -notmatch 'EmergencyEscape|emergencyEscape') -Message 'CombatBodyExitTransaction must not directly reference emergency escape.'

Assert-True -Condition ($providerText -match 'CombatBodyEmergencyEscapeResolver') -Message 'Content emergency escape extension must use the formal resolver.'
Assert-True -Condition ($devHarnessChipDefsText -match '<Kind>Passive</Kind>[\s\S]*<PassiveKey>EmergencyEscape</PassiveKey>') -Message 'DevHarness test chips must provide an EmergencyEscape passive expression sample.'

Write-Output 'CombatBodyEmergencyEscapeResolver PASS'
