$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'

Assert-True (Test-Path -LiteralPath $gizmoPath) 'Gizmo_TrionStatus.cs must exist.'

$text = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8

Assert-True ($text -match 'BaseCardWidth') 'Trion gizmo must keep a named base card width for the left resource area.'
Assert-True ($text -match 'PanelSpacing') 'Trion gizmo must define spacing between Trion base area and extension panel.'
Assert-True ($text -match 'ResolvePanelExtension') 'Trion gizmo must resolve a panel extension provider.'
Assert-True ($text -match 'GetPanelProviders\(\)') 'Trion gizmo must read panel providers from TrionGizmoExtensionRegistry.'
Assert-True ($text -match 'DrawPanelExtension') 'Trion gizmo must isolate panel drawing in a helper.'
Assert-True ($text -match 'provider\.DrawPanel') 'Trion gizmo must delegate right-side panel rendering to the provider.'
Assert-True ($text -match 'baseRect') 'Trion gizmo must keep a dedicated baseRect for the Trion resource area.'
Assert-True ($text -match 'panelRect') 'Trion gizmo must create a dedicated panelRect for extension content.'
Assert-True ($text -match 'GetWidth\(maxWidth\)') 'GizmoOnGUI must size outerRect from GetWidth(maxWidth).'
Assert-True ($text -match 'BaseCardWidth\s*\+\s*PanelSpacing\s*\+\s*panelWidth') 'GetWidth must add panel width to the base Trion width.'

Assert-True ($text -match 'CollectBadges') 'Existing badge collection must remain.'
Assert-True ($text -match 'CreateFrozenBadge') 'Existing frozen badge must remain.'
Assert-True ($text -match 'BuildTooltip') 'Existing Trion tooltip must remain.'
Assert-True ($text -notmatch 'BDP\.Core\.Trigger') 'Main Trion gizmo must not reference Trigger namespace.'
Assert-True ($text -notmatch 'BDP\.DevHarness') 'Main Trion gizmo must not reference DevHarness namespace.'

Write-Output 'TrionGizmoPanelLayoutSmokeTests PASS'
