$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$candidateRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness'
$inventoryPath = Join-Path $mainModRoot 'docs\需求说明\2026-04-24-BDP标准芯片XML字段全表.md'

$candidateXmlFiles = Get-ChildItem -LiteralPath (Join-Path $candidateRoot '1.6') -Filter '*.xml' -Recurse
$candidateText = ($candidateXmlFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"
$inventoryText = Get-Content -LiteralPath $inventoryPath -Raw -Encoding utf8

Assert-True ($candidateText -notmatch '<ExclusionTags>') `
    'Candidate XML must remove the legacy ExclusionTags field.'
Assert-True ($candidateText -notmatch '\bDualWieldTest\b') `
    'The temporary DualWieldTest marker must be deleted.'
Assert-True ($candidateText -notmatch '<ChipExclusionGroupDef>') `
    'This item must not invent a concrete business exclusion group.'
Assert-True (
    ($inventoryText -match '<ActivationExclusionGroups>') -and
    ($inventoryText -match 'ChipExclusionGroupDef') -and
    ($inventoryText -notmatch '<ExclusionTags>')
) 'The field inventory must document strong typed activation exclusion groups only.'

Write-Output 'DevHarnessChipActivationExclusionAuthoringSmokeTests PASS'
