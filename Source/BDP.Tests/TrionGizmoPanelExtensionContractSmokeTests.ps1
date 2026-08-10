$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$externalRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion\External'

$badgeProviderPath = Join-Path $externalRoot 'ITrionGizmoExtensionProvider.cs'
$panelProviderPath = Join-Path $externalRoot 'ITrionGizmoPanelExtensionProvider.cs'
$registryPath = Join-Path $externalRoot 'TrionGizmoExtensionRegistry.cs'

Assert-True (Test-Path -LiteralPath $badgeProviderPath) 'Existing badge provider interface must remain.'
Assert-True (Test-Path -LiteralPath $panelProviderPath) 'Trion panel extension provider interface must exist.'
Assert-True (Test-Path -LiteralPath $registryPath) 'Trion extension registry must exist.'

$badgeText = Get-Content -LiteralPath $badgeProviderPath -Raw -Encoding utf8
$panelText = Get-Content -LiteralPath $panelProviderPath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding utf8
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Recurse -Filter '*.cs' | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"

Assert-True ($badgeText -match 'IEnumerable<TrionGizmoExtensionBadge>\s+GetBadges') 'Badge provider GetBadges contract must remain unchanged.'
Assert-True ($panelText -match 'interface\s+ITrionGizmoPanelExtensionProvider') 'Panel provider interface must be named ITrionGizmoPanelExtensionProvider.'
Assert-True ($panelText -match 'float\s+GetWidth\s*\(\s*TrionGizmoExtensionContext\s+context\s*\)') 'Panel provider must expose GetWidth(context).'
Assert-True ($panelText -match 'GizmoResult\s+DrawPanel\s*\(') 'Panel provider must expose DrawPanel(...).'
Assert-True ($panelText -match 'Rect\s+panelRect') 'DrawPanel must receive a panel rect from the Trion Gizmo container.'
Assert-True ($panelText -match 'GizmoRenderParms\s+parms') 'DrawPanel must receive Gizmo render parms.'

Assert-True ($registryText -match 'List<ITrionGizmoExtensionProvider>') 'Registry must keep badge providers.'
Assert-True ($registryText -match 'List<ITrionGizmoPanelExtensionProvider>') 'Registry must keep panel providers separately.'
Assert-True ($registryText -match 'RegisterPanel\s*\(\s*ITrionGizmoPanelExtensionProvider') 'Registry must expose RegisterPanel for panel providers.'
Assert-True ($registryText -match 'UnregisterPanel\s*\(\s*ITrionGizmoPanelExtensionProvider') 'Registry must expose UnregisterPanel for panel providers.'
Assert-True ($registryText -match 'GetPanelProviders\s*\(') 'Registry must expose panel provider enumeration.'

Assert-True ($trionText -notmatch 'BDP\.DevHarness') 'Main Trion code must not reference DevHarness.'

Write-Output 'TrionGizmoPanelExtensionContractSmokeTests PASS'
