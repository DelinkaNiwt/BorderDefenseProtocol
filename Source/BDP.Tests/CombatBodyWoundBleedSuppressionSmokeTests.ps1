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

$rawPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRawMetrics.cs'
$injuryPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Hediff_Injury_BleedRate_CombatBodyWounds.cs'
$missingPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Hediff_MissingPart_BleedRate_CombatBodyWounds.cs'

Assert-True (Test-Path -LiteralPath $rawPath) 'CombatBodyWoundRawMetrics.cs must exist.'
Assert-True (Test-Path -LiteralPath $injuryPatchPath) 'Injury bleed patch must exist.'
Assert-True (Test-Path -LiteralPath $missingPatchPath) 'Missing part bleed patch must exist.'

$rawText = Get-Content -LiteralPath $rawPath -Raw -Encoding utf8
$injuryText = Get-Content -LiteralPath $injuryPatchPath -Raw -Encoding utf8
$missingText = Get-Content -LiteralPath $missingPatchPath -Raw -Encoding utf8

Assert-True ($rawText -match 'ReadRawBleedRate') 'Raw metrics must expose raw bleed read method.'
Assert-True ($rawText -match 'finally') 'Bypass must be cleared in finally.'
Assert-True ($injuryText -match 'Hediff_Injury') 'Injury patch must target Hediff_Injury.'
Assert-True ($missingText -match 'Hediff_MissingPart') 'Missing part patch must target Hediff_MissingPart.'
Assert-True ($injuryText -match 'MethodType\.Getter') 'Injury patch must target getter.'
Assert-True ($missingText -match 'MethodType\.Getter') 'Missing part patch must target getter.'
Assert-True ($injuryText -match 'IsBypassingBleedSuppression') 'Injury patch must honor bypass.'
Assert-True ($missingText -match 'IsBypassingBleedSuppression') 'Missing part patch must honor bypass.'
Assert-True ($injuryText -match 'ShouldSuppressIndividualBleeding') 'Injury patch must honor policy.'
Assert-True ($missingText -match 'ShouldSuppressIndividualBleeding') 'Missing part patch must honor policy.'
Assert-True ($injuryText -match '__result\s*=\s*0f') 'Injury patch must zero result.'
Assert-True ($missingText -match '__result\s*=\s*0f') 'Missing part patch must zero result.'

Write-Output 'CombatBodyWoundBleedSuppressionSmokeTests PASS'
