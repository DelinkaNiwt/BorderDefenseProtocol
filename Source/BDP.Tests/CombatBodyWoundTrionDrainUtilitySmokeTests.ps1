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

$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'

Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'

$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8

Assert-True ($utilityText -match 'internal\s+static\s+class\s+CombatBodyWoundTrionDrainUtility') 'Wound drain query must live in a small internal static utility.'
Assert-True ($utilityText -match 'TryResolveDrainPerSecond\s*\(\s*Hediff\s+hediff,\s*out\s+float\s+drainPerSecond\s*\)') 'Wound drain utility must expose TryResolveDrainPerSecond(Hediff, out float).'
Assert-True ($utilityText -match 'CombatBodyWoundTrionDrainMetric\.Severity') 'Wound drain utility must keep severity metric support.'
Assert-True ($utilityText -match 'ReadRawBleedRate') 'Wound drain utility must keep raw bleed-rate metric support.'
Assert-True ($utilityText -match 'includeMissingPartBleedPotential') 'Wound drain utility must keep missing-part policy handling.'
Assert-True ($utilityText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(hediff\.pawn\)') 'Wound drain utility must keep drain lifecycle aligned with Active and Collapsing spray applicability.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.TryResolveDrainPerSecond') 'Wound drain binding must use the shared read-only utility.'
Assert-True ($bindingText -notmatch 'private\s+static\s+float\s+ResolveDrainPerSecond') 'Wound drain binding must not keep a duplicate private drain calculation.'

Write-Output 'CombatBodyWoundTrionDrainUtilitySmokeTests PASS'
