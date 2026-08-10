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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$propertiesPath = Join-Path $coreRoot 'Trigger\State\CompProperties_TriggerBody.cs'
$entryPath = Join-Path $coreRoot 'Trigger\Defs\TriggerFixedLoadoutEntry.cs'
$validatorPath = Join-Path $coreRoot 'Trigger\Loadout\TriggerFixedLoadoutValidator.cs'
$switchServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'
$designPath = Join-Path $repoRoot 'docs\plans\2026-08-01-BDP事项08A固定芯片初始装载设计.md'

foreach ($path in @($propertiesPath, $entryPath, $validatorPath, $switchServicePath, $designPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required fixed-loadout definition file is missing: ' + $path)
}

$propertiesText = Get-Content -LiteralPath $propertiesPath -Raw -Encoding UTF8
$entryText = Get-Content -LiteralPath $entryPath -Raw -Encoding UTF8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding UTF8
$switchServiceText = Get-Content -LiteralPath $switchServicePath -Raw -Encoding UTF8
$designText = Get-Content -LiteralPath $designPath -Raw -Encoding UTF8

Assert-True (
    ($propertiesText -match 'List<\s*TriggerFixedLoadoutEntry\s*>\s+fixedLoadout') -and
    ($propertiesText -match 'fixedLoadout') -and
    ($propertiesText -match 'TriggerFixedLoadoutValidator')
) 'CompProperties_TriggerBody must expose the nullable fixed-loadout list and forward its errors.'

Assert-True (
    ($entryText -match 'sealed\s+class\s+TriggerFixedLoadoutEntry') -and
    ($entryText -match 'TriggerSide\s+side') -and
    ($entryText -match 'int\s+slotNumber') -and
    ($entryText -match 'ThingDef\s+chipDef') -and
    ($entryText -match '\(TriggerSide\)\s*\(\s*-1\s*\)')
) 'Each fixed-loadout entry must expose side, one-based slot number, chip Def, and an explicit missing-side sentinel.'

Assert-True (
    ($validatorText -match 'class\s+TriggerFixedLoadoutValidator') -and
    ($validatorText -match 'fixedLoadout') -and
    ($validatorText -match 'slotNumber') -and
    ($validatorText -match 'chipDef') -and
    ($validatorText -notmatch 'loadoutControlMode')
) 'The fixed-loadout validator must validate structure independently of player control mode.'

Assert-True (
    ($validatorText -match 'IsSlotOccupancyAllowed') -and
    ($validatorText -match 'HashSet') -and
    ($validatorText -match 'PairedHands')
) 'Fixed-loadout validation must reuse the shared occupancy rule and reserve physical slots, including paired hands.'

Assert-True ($switchServiceText -match 'internal\s+static\s+bool\s+IsSlotOccupancyAllowed') `
    'The runtime occupancy decision must be an internal shared boundary for definition validation.'

Assert-True (
    ($designText -match 'fixedLoadout') -and
    ($designText -match 'PlayerConfigurable.*PlayerNonConfigurable') -and
    ($designText -match '不写或写空')
) 'The design must document nullable fixed-loadout semantics for both player-control modes.'

Write-Output 'TriggerFixedLoadoutDefinitionSmokeTests PASS'
