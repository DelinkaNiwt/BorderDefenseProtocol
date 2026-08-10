$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'
$contentUiRoot = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI'

$providerPath = Join-Path $contentUiRoot 'TriggerLoadoutPanelProvider.cs'
$bootstrapPath = Join-Path $devHarnessRoot 'DevHarnessBootstrap.cs'

Assert-True (Test-Path -LiteralPath $providerPath) 'Content Trigger loadout panel provider must exist.'
Assert-True (Test-Path -LiteralPath $bootstrapPath) 'DevHarnessBootstrap.cs must exist.'

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8

Assert-True ($providerText -match 'class\s+TriggerLoadoutPanelProvider\s*:\s*ITrionGizmoPanelExtensionProvider') 'Provider must implement ITrionGizmoPanelExtensionProvider.'
Assert-True ($bootstrapText -notmatch 'TrionGizmoExtensionRegistry\.RegisterPanel') 'DevHarness must not register the formal Trion panel provider.'
Assert-True ($providerText -match 'GetModExtension<TriggerLoadoutPanelExtension>') 'Panel visibility must require the explicit Content permission extension.'

Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveLoadoutReader') 'Panel must read Trigger loadout through formal surface.'
Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveInteractionReader') 'Panel must read Trigger interaction through formal surface.'
Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveLoadoutCommands') 'Panel must submit commands through formal surface.'
Assert-True ($providerText -notmatch 'TryGetComp<CompTriggerBody>') 'Panel must not resolve CompTriggerBody directly.'
Assert-True ($providerText -notmatch '\.mainSlots|\.subSlots|\.specialSlots') 'Panel must not touch internal slot lists.'

Assert-True ($providerText -match 'GetSlots\s*\(\s*TriggerSide\.Main\s*\)') 'Panel must draw main slots.'
Assert-True ($providerText -match 'GetSlots\s*\(\s*TriggerSide\.Sub\s*\)') 'Panel must draw sub slots.'
Assert-True ($providerText -notmatch 'GetSlots\s*\(\s*TriggerSide\.Special\s*\)') 'Panel must not draw special side as slot cells.'

Assert-True ($providerText -match 'RequestActivate') 'Panel must support activation/switch through RequestActivate.'
Assert-True ($providerText -match 'RequestDeactivate') 'Panel must support deactivation through RequestDeactivate.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Activate') 'Panel must interpret Activate operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.SwitchTo') 'Panel must interpret SwitchTo operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Deactivate') 'Panel must interpret Deactivate operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Mirror') 'Panel must handle mirror slots without direct command.'

Assert-True ($providerText -match 'DrawWarmupProgressBar') 'Panel must keep warmup progress drawing isolated.'
Assert-True ($providerText -match 'DrawWinddownProgressBar') 'Panel must keep winddown progress drawing isolated.'
Assert-True ($providerText -notmatch 'xMax\s*-\s*width') 'Winddown progress must shrink from the right edge while staying left-anchored.'
Assert-True ($providerText -match 'BuildSlotTooltip') 'Panel must provide per-slot tooltip.'
Assert-True ($providerText -notmatch 'Prefs\.DevMode') 'Panel visibility must not be gated by DevMode.'

Write-Output 'DevHarnessTrionTriggerLoadoutPanelSmokeTests PASS'
