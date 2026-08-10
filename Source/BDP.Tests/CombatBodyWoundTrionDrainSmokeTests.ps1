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

$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'
$runtimePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'
Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundRuntime.cs must exist.'

$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
$runtimeText = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8

Assert-True ($utilityText -match 'new\s+TrionDrainKey\s*\(\s*"CombatBody"\s*,\s*"Wound"') 'Wound drain key must use CombatBody/Wound domain.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.BuildDrainKey') 'Binding must use the shared wound drain key builder.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.TryResolveDrainPerSecond') 'Binding must calculate drain through the shared policy metric resolver.'
Assert-True ($utilityText -match 'CombatBodyWoundTrionDrainMetric\.Severity') 'Drain must support severity metric.'
Assert-True ($utilityText -match 'ReadRawBleedRate') 'Drain must retain raw bleed rate mode.'
Assert-True ($utilityText -match 'trionDrainEnabled') 'Drain must honor enabled setting.'
Assert-True ($utilityText -match 'trionDrainPerRawBleedRatePerSecond') 'Drain must honor raw bleed-rate per-second setting.'
Assert-True ($utilityText -match 'trionDrainPerSeverityPerSecond') 'Drain must honor severity per-second setting.'
Assert-True ($utilityText -match 'includeMissingPartBleedPotential') 'Drain must honor missing-part policy setting.'
Assert-True ($bindingText -match 'RegisterDrain') 'Binding must register drain when enabled.'
Assert-True ($bindingText -match 'UnregisterDrain') 'Binding must unregister drain.'
Assert-True ($runtimeText -match 'RebuildActiveWounds') 'Runtime must support full rebuild.'
Assert-True ($runtimeText -match 'ClearActiveRuntime') 'Runtime must support clear.'
Assert-True ($trionText -notmatch 'CombatBodyWound') 'Trion core must not reference wound business.'

Write-Output 'CombatBodyWoundTrionDrainSmokeTests PASS'
