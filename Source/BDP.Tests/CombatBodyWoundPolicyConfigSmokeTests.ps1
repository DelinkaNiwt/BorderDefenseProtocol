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

$policyXmlPath = Join-Path $repoRoot '1.6\Defs\CombatBodyDef\Config.xml'
$activeXmlPath = Join-Path $repoRoot '1.6\Defs\HediffDef\CombatBody.xml'

Assert-True (Test-Path -LiteralPath $policyXmlPath) 'CombatBodyWoundPolicyDefs.xml must exist.'

[xml]$policyXml = Get-Content -LiteralPath $policyXmlPath -Raw -Encoding utf8
$policy = $policyXml.Defs.'BDP.Core.CombatBody.Wounds.CombatBodyWoundPolicyDef' |
    Where-Object { $_.defName -eq 'BDP_DefaultCombatBodyWoundPolicy' } |
    Select-Object -First 1

Assert-True ($null -ne $policy) 'Default combat body wound policy def must exist.'
Assert-True ($policy.suppressIndividualBleeding -eq 'true') 'Default must suppress individual bleeding.'
Assert-True ($policy.trionDrainEnabled -eq 'true') 'Default Trion wound drain must be enabled.'
Assert-True ([float]$policy.trionDrainPerDamagePerSecond -eq 1) '默认每点部位有效损伤每秒流失一点 Trion。'

[xml]$activeXml = Get-Content -LiteralPath $activeXmlPath -Raw -Encoding utf8
$active = $activeXml.Defs.HediffDef | Where-Object { $_.defName -eq 'BDP_CombatBodyActive' } | Select-Object -First 1
$immuneItems = @($active.stages.li.makeImmuneTo.li)
Assert-True ($immuneItems -contains 'WoundInfection') 'Combat body active should make pawn immune to wound infection.'

Write-Output 'CombatBodyWoundPolicyConfigSmokeTests PASS'
