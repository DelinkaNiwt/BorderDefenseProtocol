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
$defPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Buildings\Assembly.xml'

Assert-True (Test-Path -LiteralPath $defPath) 'Assembly ThingDef file must exist.'

[xml]$defs = Get-Content -LiteralPath $defPath -Raw -Encoding utf8
$chipStorageDef = @($defs.Defs.ThingDef | Where-Object { $_.defName -eq 'BDP_ChipStorage' })
Assert-True ($chipStorageDef.Count -eq 1) 'BDP_ChipStorage ThingDef must exist exactly once.'

$facilityComp = @($chipStorageDef.comps.li | Where-Object { $_.Class -eq 'CompProperties_Facility' })
Assert-True ($facilityComp.Count -eq 1) 'BDP_ChipStorage must define exactly one CompProperties_Facility.'

Assert-True ([int]$facilityComp.maxSimultaneous -eq 100) 'BDP_ChipStorage must allow 100 simultaneous links.'
Assert-True (([string]$facilityComp.maxDistance).Trim() -eq '12.9') 'BDP_ChipStorage maxDistance must be 12.9.'
Assert-True (([string]$facilityComp.requiresLOS).Trim() -eq 'true') 'BDP_ChipStorage must require line of sight.'

$linkableBuildings = @($facilityComp.linkableBuildings.li | ForEach-Object { ([string]$_).Trim() })
Assert-True ($linkableBuildings -contains 'BDP_TriggerAssembler') 'BDP_ChipStorage must remain linkable to BDP_TriggerAssembler.'

Write-Output 'ChipStorageFacilityParitySmokeTests PASS'
