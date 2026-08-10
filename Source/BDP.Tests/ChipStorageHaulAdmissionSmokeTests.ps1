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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP.Content'

$workGiverPath = Join-Path $bdpSourceRoot 'Assembly\Job\WorkGiver_HaulToChipStorage.cs'
$jobDriverPath = Join-Path $bdpSourceRoot 'Assembly\Job\JobDriver_HaulToChipStorage.cs'
$providerPath = Join-Path $bdpSourceRoot 'Assembly\Facility\DefaultAssemblerFacilityProvider.cs'
$chipContainerPath = Join-Path $bdpSourceRoot 'Assembly\Building\CompChipContainer.cs'

Assert-True (Test-Path -LiteralPath $workGiverPath) 'WorkGiver_HaulToChipStorage must exist.'
Assert-True (Test-Path -LiteralPath $jobDriverPath) 'JobDriver_HaulToChipStorage must exist.'
Assert-True (Test-Path -LiteralPath $providerPath) 'DefaultAssemblerFacilityProvider must exist.'
Assert-True (Test-Path -LiteralPath $chipContainerPath) 'CompChipContainer must exist.'

$workGiverText = Get-Content -LiteralPath $workGiverPath -Raw -Encoding utf8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$chipContainerText = Get-Content -LiteralPath $chipContainerPath -Raw -Encoding utf8

Assert-True (
    $workGiverText -match 'CompChipContainer' -and
    $workGiverText -match 'CanAcceptMore'
) 'Chip hauling must still require a real chip container with free capacity.'

Assert-True (
    $workGiverText -notmatch 'CanBeActive'
) 'Chip hauling admission must not depend on powered/flicked-on CompFacility.CanBeActive.'

Assert-True (
    $workGiverText -notmatch 'holdingOwner\s*!=\s*null'
) 'Chip hauling admission must not reject normal spawned ground things by checking holdingOwner.'

Assert-True (
    $jobDriverText -notmatch 'this\.FailOnDespawnedNullOrForbidden\(ChipIndex\)'
) 'Chip JobDriver must not fail the whole job after the chip is carried off the map.'

Assert-True (
    $jobDriverText -match 'GotoThing\(ChipIndex,\s*PathEndMode\.Touch\)[\s\S]*?\.FailOnDespawnedNullOrForbidden\(ChipIndex\)'
) 'Chip JobDriver must only require spawned chip before the pawn reaches the chip.'

Assert-True (
    $providerText -match 'CanBeActive'
) 'Assembler facility reading must still depend on active connected facilities.'

Assert-True (
    $chipContainerText -match '\[StaticConstructorOnStartup\]'
) 'CompChipContainer must mark static texture loading with StaticConstructorOnStartup.'

Write-Output 'ChipStorageHaulAdmissionSmokeTests PASS'
