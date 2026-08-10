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
$activeXmlPath = Join-Path $repoRoot '1.6\Defs\Health\CombatBody\HediffDefs_CombatBody.xml'

Assert-True (Test-Path -LiteralPath $policyXmlPath) 'CombatBodyWoundPolicyDefs.xml must exist.'

[xml]$policyXml = Get-Content -LiteralPath $policyXmlPath -Raw -Encoding utf8
$policy = $policyXml.Defs.'BDP.Core.CombatBody.Wounds.CombatBodyWoundPolicyDef' |
    Where-Object { $_.defName -eq 'BDP_DefaultCombatBodyWoundPolicy' } |
    Select-Object -First 1

Assert-True ($null -ne $policy) 'Default combat body wound policy def must exist.'
Assert-True ($policy.suppressIndividualBleeding -eq 'true') 'Default must suppress individual bleeding.'
Assert-True ($policy.trionDrainEnabled -eq 'true') 'Default Trion wound drain must be enabled.'
Assert-True ($policy.trionDrainMetric -eq 'Severity') 'Default Trion wound drain must use severity.'
Assert-True ([float]$policy.trionDrainPerRawBleedRatePerSecond -eq 0) 'Default raw bleed rate drain scale must stay 0.'
Assert-True ([float]$policy.trionDrainPerSeverityPerSecond -eq 1) 'Default severity drain must be 1 Trion per second per severity.'

[xml]$activeXml = Get-Content -LiteralPath $activeXmlPath -Raw -Encoding utf8
$active = $activeXml.Defs.HediffDef | Where-Object { $_.defName -eq 'BDP_CombatBodyActive' } | Select-Object -First 1
$immuneItems = @($active.stages.li.makeImmuneTo.li)
Assert-True ($immuneItems -contains 'WoundInfection') 'Combat body active should make pawn immune to wound infection.'

Write-Output 'CombatBodyWoundPolicyConfigSmokeTests PASS'
