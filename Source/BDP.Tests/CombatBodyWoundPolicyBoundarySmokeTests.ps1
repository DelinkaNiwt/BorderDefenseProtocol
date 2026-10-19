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

$woundRoot = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds'
$defPath = Join-Path $woundRoot 'CombatBodyWoundPolicyDef.cs'
$policyPath = Join-Path $woundRoot 'CombatBodyWoundPolicy.cs'

Assert-True (Test-Path -LiteralPath $defPath) 'CombatBodyWoundPolicyDef.cs must exist.'
Assert-True (Test-Path -LiteralPath $policyPath) 'CombatBodyWoundPolicy.cs must exist.'

$defText = Get-Content -LiteralPath $defPath -Raw -Encoding utf8
$policyText = Get-Content -LiteralPath $policyPath -Raw -Encoding utf8

Assert-True ($defText -match 'class\s+CombatBodyWoundPolicyDef\s*:\s*Def') 'Policy def must inherit Verse.Def.'
Assert-True ($defText -match 'suppressIndividualBleeding\s*=\s*true') 'Code fallback must suppress individual bleeding.'
Assert-True ($defText -match 'trionDrainEnabled\s*=\s*false') 'Code fallback must keep Trion wound drain disabled.'
Assert-True ($defText -match 'trionDrainPerDamagePerSecond\s*=\s*0f') '未加载配置时部位有效损伤的流失倍率必须为零。'
Assert-True ($defText -notmatch 'trionDrainMetric|trionDrainPerSeverityPerSecond|trionDrainPerRawBleedRatePerSecond') '策略只保留部位有效损伤口径。'
Assert-True ($policyText -match 'CombatBodySurfaceAccess\.ResolveReader') 'Policy must use CombatBody reader surface.'
Assert-True ($policyText -match 'CombatBodyPhase\.Active') 'Policy must check CombatBodyPhase.Active.'
Assert-True ($policyText -match 'IsCombatBodyWoundRuntimeApplicable\(Pawn pawn\)') 'Policy must expose wound runtime applicability.'
Assert-True ($policyText -match 'CombatBodyPhase\.Collapsing') 'Wound runtime applicability must include CombatBodyPhase.Collapsing.'
Assert-True ($policyText -match 'reader\.Phase\s*==\s*CombatBodyPhase\.Active\s*\|\|\s*reader\.Phase\s*==\s*CombatBodyPhase\.Collapsing') 'Wound runtime must apply during Active and Collapsing phases.'
Assert-True ($policyText -notmatch 'BDP_CombatBodyActive') 'Policy must not use the active Hediff as truth.'

Write-Output 'CombatBodyWoundPolicyBoundarySmokeTests PASS'
