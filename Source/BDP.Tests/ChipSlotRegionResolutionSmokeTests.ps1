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
    'BDP.Core.dll must exist before the slot-region resolution smoke test.'

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
$regionType = $assembly.GetType('BDP.Core.Chips.ChipSlotRegion', $false)
Assert-True ($null -ne $regionType) 'ChipSlotRegion runtime enum must exist.'

$sideType = $assembly.GetType('BDP.Core.Trigger.TriggerSide', $true)
$serviceType = $assembly.GetType('BDP.Core.Trigger.TriggerService', $true)
$bindingFlags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
$resolveMethod = $serviceType.GetMethod('IsSlotRegionAllowed', $bindingFlags)
Assert-True ($null -ne $resolveMethod) `
    'TriggerSwitchService must retain one private slot-region decision boundary.'

$unspecified = [Enum]::Parse($regionType, 'Unspecified')
$mainSub = [Enum]::Parse($regionType, 'MainSub')
$special = [Enum]::Parse($regionType, 'Special')
$mainSide = [Enum]::Parse($sideType, 'Main')
$subSide = [Enum]::Parse($sideType, 'Sub')
$specialSide = [Enum]::Parse($sideType, 'Special')

function Test-RegionSide {
    param(
        [object]$Region,
        [object]$Side
    )

    return [bool]$resolveMethod.Invoke($null, [object[]]@($Region, $Side))
}

Assert-True (Test-RegionSide $mainSub $mainSide) `
    'MainSub region must allow the main slot.'
Assert-True (Test-RegionSide $mainSub $subSide) `
    'MainSub region must allow the sub slot.'
Assert-True (-not (Test-RegionSide $mainSub $specialSide)) `
    'MainSub region must reject the special slot.'
Assert-True (-not (Test-RegionSide $special $mainSide)) `
    'Special region must reject the main slot.'
Assert-True (-not (Test-RegionSide $special $subSide)) `
    'Special region must reject the sub slot.'
Assert-True (Test-RegionSide $special $specialSide) `
    'Special region must allow the special slot.'
Assert-True (-not (Test-RegionSide $unspecified $mainSide)) `
    'Unspecified region must reject the main slot.'
Assert-True (-not (Test-RegionSide $unspecified $subSide)) `
    'Unspecified region must reject the sub slot.'
Assert-True (-not (Test-RegionSide $unspecified $specialSide)) `
    'Unspecified region must reject the special slot.'

Write-Output 'ChipSlotRegionResolutionSmokeTests PASS'
