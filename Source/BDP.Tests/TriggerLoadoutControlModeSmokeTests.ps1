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
$modePath = Join-Path $coreRoot 'Trigger\Defs\TriggerLoadoutControlMode.cs'
$propertiesPath = Join-Path $coreRoot 'Trigger\State\CompProperties_TriggerBody.cs'
$compPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.cs'
$readerContractPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutReader.cs'
$readerSurfacePath = Join-Path $coreRoot 'Trigger\Access\Surfaces\TriggerFormalSurfaces.cs'
$commandsSurfacePath = Join-Path $coreRoot 'Trigger\Access\Surfaces\TriggerFormalSurfaces.cs'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$transactionPath = Join-Path $contentRoot 'Assembly\Interaction\TriggerAssemblyTransaction.cs'
$assemblerPath = Join-Path $contentRoot 'Assembly\Building\Building_TriggerAssembler.cs'
$windowPath = Join-Path $contentRoot 'Assembly\Window\Window_TriggerAssembly.cs'
$bodyDefPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Equipment\Trigger\ThingDefs_TriggerBodies.xml'

foreach ($path in @($modePath, $propertiesPath, $compPath, $readerContractPath, $readerSurfacePath, $transactionPath, $assemblerPath, $windowPath, $bodyDefPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required loadout control file is missing: ' + $path)
}

$modeText = Get-Content -LiteralPath $modePath -Raw -Encoding UTF8
$propertiesText = Get-Content -LiteralPath $propertiesPath -Raw -Encoding UTF8
$compText = Get-Content -LiteralPath $compPath -Raw -Encoding UTF8
$readerContractText = Get-Content -LiteralPath $readerContractPath -Raw -Encoding UTF8
$readerSurfaceText = Get-Content -LiteralPath $readerSurfacePath -Raw -Encoding UTF8
$transactionText = Get-Content -LiteralPath $transactionPath -Raw -Encoding UTF8
$assemblerText = Get-Content -LiteralPath $assemblerPath -Raw -Encoding UTF8
$windowText = Get-Content -LiteralPath $windowPath -Raw -Encoding UTF8
$bodyDefText = Get-Content -LiteralPath $bodyDefPath -Raw -Encoding UTF8
$legacyModeName = 'Definition' + 'Fixed'
$legacyRejectCode = 'definition' + '_fixed'

Assert-True (
    ($modeText -match 'public\s+enum\s+TriggerLoadoutControlMode') -and
    ($modeText -match 'PlayerConfigurable') -and
    ($modeText -match 'PlayerNonConfigurable') -and
    ($modeText -notmatch $legacyModeName)
) 'Core must define exactly the two current trigger loadout control meanings.'

Assert-True ($propertiesText -match 'TriggerLoadoutControlMode\s+loadoutControlMode') `
    'CompProperties_TriggerBody must expose the neutral loadout control mode.'

Assert-True (
    ($readerContractText -match 'TriggerLoadoutControlMode\s+LoadoutControlMode') -and
    ($readerSurfaceText -match 'LoadoutControlMode')
) 'The formal Trigger reader must expose the loadout control mode to Core consumers.'

Assert-True (
    ($compText -match 'loadoutControlMode\s*==\s*TriggerLoadoutControlMode\.PlayerConfigurable') -and
    ($compText -match 'AllowsPlayerLoadoutConfiguration') -and
    ($compText -match 'TryLoadChip[\s\S]*player_non_configurable') -and
    ($compText -match 'TryUnloadChip[\s\S]*player_non_configurable') -and
    ($compText -notmatch $legacyRejectCode)
) 'The owner command boundary must reject player load and unload requests for fixed loadouts.'

Assert-True (
    ($transactionText -match 'LoadoutControlMode') -and
    ($transactionText -match 'PlayerNonConfigurable') -and
    ($transactionText -notmatch $legacyModeName)
) 'Assembly transactions must reject fixed loadouts before changing storage or slots.'

Assert-True (
    ($assemblerText -match 'LoadoutControlMode') -and
    ($assemblerText -match 'PlayerNonConfigurable') -and
    ($windowText -match 'LoadoutControlMode') -and
    ($windowText -match 'PlayerNonConfigurable') -and
    ($assemblerText -notmatch $legacyModeName) -and
    ($windowText -notmatch $legacyModeName)
) 'Both the assembly entry and open window must enforce the fixed-loadout boundary.'

Assert-True ($bodyDefText -match '<loadoutControlMode>PlayerConfigurable</loadoutControlMode>') `
    'Border Standard Trigger Body must explicitly remain player configurable.'

Write-Output 'TriggerLoadoutControlModeSmokeTests PASS'
