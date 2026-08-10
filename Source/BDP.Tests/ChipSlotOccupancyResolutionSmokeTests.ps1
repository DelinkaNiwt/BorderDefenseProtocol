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
$assemblyPath = Join-Path $repoRoot '1.6\Assemblies\BDP.Core.dll'
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\839005762\1.6\Assemblies\0Harmony.dll'

Assert-True (Test-Path -LiteralPath $assemblyPath) `
    'BDP.Core.dll must exist before the slot-occupancy resolution smoke test.'

$dependencyPaths = @(
    (Join-Path $managedRoot 'UnityEngine.CoreModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.dll'),
    $harmonyPath,
    (Join-Path $managedRoot 'Assembly-CSharp.dll')
)
foreach ($dependencyPath in $dependencyPaths) {
    if (Test-Path -LiteralPath $dependencyPath) {
        [void][System.Reflection.Assembly]::LoadFrom($dependencyPath)
    }
}

$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$regionType = $assembly.GetType('BDP.Core.Chips.ChipSlotRegion', $true)
$occupancyType = $assembly.GetType('BDP.Core.Chips.ChipSlotOccupancy', $false)
Assert-True ($null -ne $occupancyType) 'ChipSlotOccupancy runtime enum must exist.'

$sideType = $assembly.GetType('BDP.Core.Trigger.TriggerSide', $true)
$serviceType = $assembly.GetType('BDP.Core.Trigger.TriggerService', $true)
$bindingFlags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
$resolveMethod = $serviceType.GetMethod('IsSlotOccupancyAllowed', $bindingFlags)
Assert-True ($null -ne $resolveMethod) `
    'TriggerService must expose one private slot-occupancy decision boundary.'

$mainSub = [Enum]::Parse($regionType, 'MainSub')
$special = [Enum]::Parse($regionType, 'Special')
$single = [Enum]::Parse($occupancyType, 'Single')
$pairedHands = [Enum]::Parse($occupancyType, 'PairedHands')
$unspecified = [Enum]::Parse($occupancyType, 'Unspecified')
$unknown = [Enum]::ToObject($occupancyType, 99)
$mainSide = [Enum]::Parse($sideType, 'Main')
$subSide = [Enum]::Parse($sideType, 'Sub')
$specialSide = [Enum]::Parse($sideType, 'Special')

function Test-Occupancy {
    param(
        [object]$Region,
        [object]$Occupancy,
        [object]$Side
    )

    return [bool]$resolveMethod.Invoke(
        $null,
        [object[]]@($Region, $Occupancy, $Side))
}

Assert-True (Test-Occupancy $mainSub $single $mainSide) `
    'MainSub plus Single must allow the main slot.'
Assert-True (Test-Occupancy $mainSub $single $subSide) `
    'MainSub plus Single must allow the sub slot.'
Assert-True (Test-Occupancy $mainSub $pairedHands $mainSide) `
    'MainSub plus PairedHands must allow a main-side root.'
Assert-True (Test-Occupancy $mainSub $pairedHands $subSide) `
    'MainSub plus PairedHands must allow a sub-side root.'
Assert-True (Test-Occupancy $special $single $specialSide) `
    'Special plus Single must allow the special slot.'
Assert-True (-not (Test-Occupancy $special $pairedHands $specialSide)) `
    'Special plus PairedHands must be rejected.'
Assert-True (-not (Test-Occupancy $mainSub $single $specialSide)) `
    'MainSub occupancy must reject the special slot.'
Assert-True (-not (Test-Occupancy $special $single $mainSide)) `
    'Special occupancy must reject a hand slot.'
Assert-True (-not (Test-Occupancy $mainSub $unspecified $mainSide)) `
    'Unspecified occupancy must be rejected.'
Assert-True (-not (Test-Occupancy $mainSub $unknown $mainSide)) `
    'Unknown occupancy values must be rejected.'

Write-Output 'ChipSlotOccupancyResolutionSmokeTests PASS'
