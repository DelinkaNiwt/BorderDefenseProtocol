$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

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
    'BDP.Core.dll must exist before exclusion-group resolution smoke tests.'

@(
    (Join-Path $managedRoot 'UnityEngine.CoreModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $managedRoot 'UnityEngine.dll'),
    $harmonyPath,
    (Join-Path $managedRoot 'Assembly-CSharp.dll')
) | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        [void][System.Reflection.Assembly]::LoadFrom($_)
    }
}

$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$groupType = $assembly.GetType('BDP.Core.Chips.ChipExclusionGroupDef', $false)
Assert-True ($null -ne $groupType) 'ChipExclusionGroupDef runtime type must exist.'

$serviceType = $assembly.GetType('BDP.Core.Trigger.TriggerService', $true)
$flags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
$shareMethod = $serviceType.GetMethod('SharesActivationExclusionGroup', $flags)
Assert-True ($null -ne $shareMethod) `
    'TriggerService must expose one private shared-group decision boundary.'

$listType = [System.Collections.Generic.List``1].MakeGenericType($groupType)
$groupA = [Activator]::CreateInstance($groupType)
$groupB = [Activator]::CreateInstance($groupType)
$groupC = [Activator]::CreateInstance($groupType)

function New-GroupList {
    param([object[]]$Items)

    $list = [Activator]::CreateInstance($listType)
    foreach ($item in $Items) {
        [void]$list.Add($item)
    }

    return ,$list
}

function Test-SharesGroup {
    param([object]$Left, [object]$Right)

    return [bool]$shareMethod.Invoke($null, [object[]]@($Left, $Right))
}

Assert-True (-not (Test-SharesGroup (New-GroupList @()) (New-GroupList @()))) `
    'Two empty group lists must not conflict.'
Assert-True (-not (Test-SharesGroup (New-GroupList @($groupA)) (New-GroupList @()))) `
    'A group on only one side must not conflict.'
Assert-True (-not (Test-SharesGroup (New-GroupList @($groupA)) (New-GroupList @($groupB)))) `
    'Different Def identities must not conflict.'
Assert-True (Test-SharesGroup (New-GroupList @($groupA)) (New-GroupList @($groupA))) `
    'One shared Def identity must conflict.'
Assert-True (
    Test-SharesGroup `
        (New-GroupList @($groupA, $groupB)) `
        (New-GroupList @($groupC, $groupB))
) 'Any shared Def among multiple groups must conflict.'

$contractType = $assembly.GetType('BDP.Core.Chips.ChipLoadoutContract', $true)
$messageType = $assembly.GetType('BDP.Core.Chips.ChipDefinitionValidationMessage', $true)
$regionType = $assembly.GetType('BDP.Core.Chips.ChipSlotRegion', $true)
$occupancyType = $assembly.GetType('BDP.Core.Chips.ChipSlotOccupancy', $true)
$validatorType = $assembly.GetType('BDP.Core.Chips.ChipDefinitionValidator', $true)
$validateMethod = $validatorType.GetMethod('ValidateLoadout', $flags)
Assert-True ($null -ne $validateMethod) 'Loadout validation boundary must remain inspectable.'

$messageListType = [System.Collections.Generic.List``1].MakeGenericType($messageType)

function Get-ValidationCodes {
    param([object]$Groups)

    $loadout = [Activator]::CreateInstance($contractType)
    $contractType.GetField('SlotRegion').SetValue(
        $loadout,
        [Enum]::Parse($regionType, 'MainSub'))
    $contractType.GetField('SlotOccupancy').SetValue(
        $loadout,
        [Enum]::Parse($occupancyType, 'Single'))
    $contractType.GetField('ActivationExclusionGroups').SetValue($loadout, $Groups)
    $messages = [Activator]::CreateInstance($messageListType)
    [void]$validateMethod.Invoke($null, [object[]]@($loadout, $messages))

    return @($messages | ForEach-Object { $_.Code })
}

$nullGroups = New-GroupList @($groupA)
[void]$nullGroups.Add($null)
Assert-True ((Get-ValidationCodes $nullGroups) -contains 'ActivationExclusionGroupMissing') `
    'A null group entry must be a definition error.'
Assert-True (
    (Get-ValidationCodes (New-GroupList @($groupA, $groupA))) -contains
        'ActivationExclusionGroupDuplicate'
) 'A duplicate group Def must be a definition error.'

Write-Output 'ChipActivationExclusionGroupResolutionSmokeTests PASS'
