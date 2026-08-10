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

$policyXmlPath = Join-Path $repoRoot '1.6\Defs\Pawn\CombatBody\CombatBodyWoundPolicyDefs.xml'
$policyDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundPolicyDef.cs'
$metricPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainMetric.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'

Assert-True (Test-Path -LiteralPath $policyXmlPath) 'CombatBody wound policy XML must exist.'
Assert-True (Test-Path -LiteralPath $policyDefPath) 'CombatBodyWoundPolicyDef.cs must exist.'
Assert-True (Test-Path -LiteralPath $metricPath) 'CombatBodyWoundTrionDrainMetric.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'
Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'

[xml]$policyXml = Get-Content -LiteralPath $policyXmlPath -Raw -Encoding utf8
$policy = $policyXml.Defs.'BDP.Core.CombatBody.Wounds.CombatBodyWoundPolicyDef' |
    Where-Object { $_.defName -eq 'BDP_DefaultCombatBodyWoundPolicy' } |
    Select-Object -First 1

Assert-True ($null -ne $policy) 'Default combat body wound policy def must exist.'
Assert-True ($policy.trionDrainEnabled -eq 'true') 'Default Trion wound drain must be enabled for the severity trial.'
Assert-True ($policy.trionDrainMetric -eq 'Severity') 'Default Trion wound drain must use wound severity.'
Assert-True ([float]$policy.trionDrainPerSeverityPerSecond -eq 1) 'Default severity drain must be 1 Trion per second per severity.'

$policyDefText = Get-Content -LiteralPath $policyDefPath -Raw -Encoding utf8
$metricText = Get-Content -LiteralPath $metricPath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8

Assert-True ($metricText -match 'enum\s+CombatBodyWoundTrionDrainMetric') 'Drain metric enum must exist.'
Assert-True ($metricText -match 'RawBleedRate') 'Drain metric enum must retain raw bleed rate mode.'
Assert-True ($metricText -match 'Severity') 'Drain metric enum must support severity mode.'
Assert-True ($policyDefText -match 'trionDrainMetric') 'Policy def must expose the drain metric.'
Assert-True ($policyDefText -match 'trionDrainPerSeverityPerSecond') 'Policy def must expose severity-per-second scale.'
Assert-True (($bindingText + $utilityText) -notmatch 'SecondsPerDay') 'Wound drain must leave per-second authoring values in per-second Trion drains.'
Assert-True ($utilityText -match 'CombatBodyWoundTrionDrainMetric\.Severity') 'Shared wound drain query must branch on severity mode.'
Assert-True ($utilityText -match 'hediff\.Severity') 'Severity mode must read the wound severity.'
Assert-True ($utilityText -match 'trionDrainPerSeverityPerSecond') 'Severity mode must use the configured per-second scale.'
Assert-True ($utilityText -match 'ResolveDrainPerSecond') 'Shared wound drain query must resolve wound drain as per-second rate.'
Assert-True ($bindingText -match 'RegisterDrain\(key, drainPerSecond\)') 'Binding must register per-second wound drain directly.'

Write-Output 'CombatBodyWoundSeverityTrionDrainSmokeTests PASS'
