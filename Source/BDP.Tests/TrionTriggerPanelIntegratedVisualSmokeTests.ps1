$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$trionGizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'
$panelProviderPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'

Assert-True (Test-Path -LiteralPath $trionGizmoPath) 'Trion gizmo source must exist.'
Assert-True (Test-Path -LiteralPath $panelProviderPath) 'Trion trigger panel provider source must exist.'

$trionText = Get-Content -LiteralPath $trionGizmoPath -Raw -Encoding utf8
$panelText = Get-Content -LiteralPath $panelProviderPath -Raw -Encoding utf8

Assert-True ($trionText -match 'PanelDividerColor') 'Trion gizmo must define a subtle panel divider color.'
Assert-True ($trionText -match 'DrawPanelDivider') 'Trion gizmo must draw the left/right separation itself.'
Assert-True ($trionText -match 'DrawPanelDivider\s*\(\s*baseRect\s*,\s*panelRect\s*\)') 'Trion gizmo must draw the divider between base and panel rects.'

Assert-True ($panelText -notmatch 'Widgets\.DrawBox\s*\(\s*panelRect\s*,\s*1\s*\)') 'Chip panel must not draw a hard full-panel border.'
Assert-True ($panelText -match 'DrawSlotRecess') 'Chip slots must use a soft recessed cell background.'
Assert-True ($panelText -notmatch 'DrawSlotStateAccent') 'Chip slots must not draw a left-side state accent line.'
Assert-True ($panelText -match 'DrawSoftSlotBorder') 'Chip slots must keep only a low-contrast soft border.'
Assert-True ($panelText -notmatch 'slot\.IsActive\s*\?\s*2\s*:\s*1') 'Chip slot border thickness must not jump to a hard active frame.'
Assert-True ($panelText -match 'SlotInnerColor') 'Chip slots must define a calmer inner fill color.'
Assert-True ($panelText -match 'EmptySlotBorderColor') 'Empty slots must use their own low-contrast border color.'

Assert-True ($panelText -notmatch 'xMax\s*-\s*width') 'Winddown progress must not anchor to the right side.'
Assert-True ($panelText -match 'DrawWinddownProgressBar[\s\S]*new Rect\s*\(\s*rect\.x\s*,\s*rect\.y\s*,\s*width\s*,\s*rect\.height\s*\)') 'Winddown progress must remain left-anchored while shrinking.'

Write-Output 'TrionTriggerPanelIntegratedVisualSmokeTests PASS'
