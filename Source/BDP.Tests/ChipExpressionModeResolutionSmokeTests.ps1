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

function Get-FieldValue {
    param(
        [object]$Target,
        [string]$Name
    )

    return $Target.GetType().GetField($Name).GetValue($Target)
}

function Set-FieldValue {
    param(
        [object]$Target,
        [string]$Name,
        [object]$Value
    )

    $field = $Target.GetType().GetField($Name)
    Assert-True ($null -ne $field) ("Required field is missing: " + $Target.GetType().FullName + "." + $Name)
    $field.SetValue($Target, $Value)
}

function New-TypedList {
    param(
        [Type]$ElementType
    )

    $listType = [System.Collections.Generic.List``1].MakeGenericType($ElementType)
    return ,([Activator]::CreateInstance($listType))
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$assemblyPath = Join-Path $repoRoot '1.6\Assemblies\BDP.Core.dll'
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\839005762\1.6\Assemblies\0Harmony.dll'

Assert-True (Test-Path -LiteralPath $assemblyPath) 'BDP.Core.dll must exist before the runtime mode resolution smoke test.'

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
$configType = $assembly.GetType('BDP.Core.Expressions.ChipExpressionConfig', $true)
$entryType = $assembly.GetType('BDP.Core.Expressions.ChipExpressionEntryConfig', $true)
$modeType = $assembly.GetType('BDP.Core.Expressions.ChipExpressionModeConfig', $true)
$entryKindType = $assembly.GetType('BDP.Core.Expressions.ChipExpressionEntryKindConfig', $true)
$relationKindType = $assembly.GetType('BDP.Core.Expressions.ChipExpressionRelationKindConfig', $true)
$interpreterType = $assembly.GetType('BDP.Core.Expressions.DefaultChipExpressionContractInterpreter', $true)

Assert-True ($null -ne $configType.GetField('DefaultModeKey')) `
    'ChipExpressionConfig must expose DefaultModeKey.'
Assert-True ($null -ne $modeType.GetField('ActiveEntryIds')) `
    'ChipExpressionModeConfig must expose ActiveEntryIds.'
Assert-True ($null -ne $modeType.GetField('DisplayLabel')) `
    'ChipExpressionModeConfig must expose DisplayLabel.'
Assert-True ($null -ne $modeType.GetField('GizmoIconTexPath')) `
    'ChipExpressionModeConfig must expose GizmoIconTexPath.'

$passiveKind = [Enum]::Parse($entryKindType, 'Passive')
$independentRelation = [Enum]::Parse($relationKindType, 'Independent')
$attachedRelation = [Enum]::Parse($relationKindType, 'Attached')

function New-Entry {
    param(
        [string]$Id,
        [object]$RelationKind = $independentRelation,
        [string]$ParentEntryId = $null
    )

    $entry = [Activator]::CreateInstance($entryType)
    Set-FieldValue $entry 'Id' $Id
    Set-FieldValue $entry 'Kind' $passiveKind
    Set-FieldValue $entry 'PassiveKey' ($Id + '_passive')
    Set-FieldValue $entry 'RelationKind' $RelationKind
    Set-FieldValue $entry 'ParentEntryId' $ParentEntryId
    return $entry
}

function New-Mode {
    param(
        [string]$ModeKey,
        [string[]]$ActiveEntryIds,
        [string]$DisplayLabel = ($ModeKey + '形态'),
        [string]$GizmoIconTexPath = $null
    )

    $mode = [Activator]::CreateInstance($modeType)
    Set-FieldValue $mode 'ModeKey' $ModeKey
    Set-FieldValue $mode 'DisplayLabel' $DisplayLabel
    Set-FieldValue $mode 'GizmoIconTexPath' $GizmoIconTexPath
    $ids = New-TypedList ([string])
    if ($null -ne $ActiveEntryIds) {
        foreach ($id in $ActiveEntryIds) {
            [void]$ids.Add($id)
        }
    }
    Set-FieldValue $mode 'ActiveEntryIds' $ids
    return $mode
}

function New-Config {
    param(
        [object[]]$Entries,
        [object[]]$Modes = $null,
        [string]$DefaultModeKey = $null
    )

    $config = [Activator]::CreateInstance($configType)
    $entryList = New-TypedList $entryType
    if ($null -ne $Entries) {
        foreach ($entry in $Entries) {
            [void]$entryList.Add($entry)
        }
    }
    Set-FieldValue $config 'Entries' $entryList
    Set-FieldValue $config 'DefaultModeKey' $DefaultModeKey

    if ($null -ne $Modes) {
        $modeList = New-TypedList $modeType
        foreach ($mode in $Modes) {
            [void]$modeList.Add($mode)
        }
        Set-FieldValue $config 'Modes' $modeList
    }
    return $config
}

$resolveMethod = $interpreterType.GetMethod(
    'ResolveUncached',
    [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic)
Assert-True ($null -ne $resolveMethod) 'Interpreter must retain a private uncached resolution boundary.'

function Resolve-Config {
    param(
        [object]$Config,
        [string]$CurrentModeKey = $null
    )

    return $resolveMethod.Invoke($null, [object[]]@($Config, $CurrentModeKey))
}

function Get-ResolvedIds {
    param(
        [object]$Resolved
    )

    $contract = Get-FieldValue $Resolved 'Contract'
    $entries = Get-FieldValue $contract 'Entries'
    return @($entries | ForEach-Object { Get-FieldValue $_ 'Id' })
}

function Assert-Sequence {
    param(
        [string[]]$Actual,
        [string[]]$Expected,
        [string]$Message
    )

    Assert-True (($Actual -join '|') -eq ($Expected -join '|')) `
        ($Message + " Expected=" + ($Expected -join ',') + " Actual=" + ($Actual -join ','))
}

function Assert-Valid {
    param(
        [object]$Resolved,
        [string]$Message
    )

    $validation = Get-FieldValue $Resolved 'Validation'
    Assert-True ([bool](Get-FieldValue $validation 'IsValid')) $Message
}

function Assert-Invalid {
    param(
        [object]$Config,
        [string]$Message
    )

    $resolved = Resolve-Config $Config
    $validation = Get-FieldValue $resolved 'Validation'
    Assert-True (-not [bool](Get-FieldValue $validation 'IsValid')) $Message
}

$singleConfig = New-Config @(
    (New-Entry 'dash'),
    (New-Entry 'shield_guard'),
    (New-Entry 'blade_melee')
)
$singleResolved = Resolve-Config $singleConfig
Assert-Valid $singleResolved 'Single-mode entries must resolve as a valid contract.'
Assert-Sequence (Get-ResolvedIds $singleResolved) @('dash', 'shield_guard', 'blade_melee') `
    'Single-mode chips must publish all entries in catalog order.'
$singleContract = Get-FieldValue $singleResolved 'Contract'
foreach ($entry in (Get-FieldValue $singleContract 'Entries')) {
    Assert-True ([string]::IsNullOrWhiteSpace([string](Get-FieldValue $entry 'ModeKey'))) `
        'Single-mode resolved entries must not carry a mode key.'
}

$multiConfig = New-Config `
    @((New-Entry 'dash'), (New-Entry 'shield_guard'), (New-Entry 'blade_melee')) `
    @((New-Mode 'shield' @('dash', 'shield_guard')), (New-Mode 'blade' @('dash', 'blade_melee'))) `
    'shield'

$defaultResolved = Resolve-Config $multiConfig
Assert-Valid $defaultResolved 'A valid default mode must resolve.'
Assert-Sequence (Get-ResolvedIds $defaultResolved) @('dash', 'shield_guard') `
    'Missing runtime mode must use the configured default mode.'
foreach ($entry in (Get-FieldValue (Get-FieldValue $defaultResolved 'Contract') 'Entries')) {
    Assert-True ((Get-FieldValue $entry 'ModeKey') -eq 'shield') `
        'Default-mode entries must carry the effective mode key.'
}

$bladeResolved = Resolve-Config $multiConfig 'blade'
Assert-Valid $bladeResolved 'An explicit valid runtime mode must resolve.'
Assert-Sequence (Get-ResolvedIds $bladeResolved) @('dash', 'blade_melee') `
    'Explicit runtime mode must override the configured default.'

$unknownResolved = Resolve-Config $multiConfig 'missing_mode'
Assert-Valid $unknownResolved 'Unknown runtime mode must safely fall back to the default mode.'
Assert-Sequence (Get-ResolvedIds $unknownResolved) @('dash', 'shield_guard') `
    'Unknown runtime mode fallback must publish the default mode entries.'
$unknownWarnings = Get-FieldValue (Get-FieldValue $unknownResolved 'Validation') 'Warnings'
Assert-True ($unknownWarnings.Count -gt 0) 'Unknown runtime mode fallback must emit a diagnostic warning.'

Assert-Invalid (New-Config @((New-Entry 'dash')) $null 'shield') `
    'Single-mode config must reject DefaultModeKey.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @('dash')))) `
    'Multi-mode config must require DefaultModeKey.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @('dash'))) 'missing') `
    'DefaultModeKey must reference a real mode.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode '' @('dash'))) '') `
    'ModeKey must not be blank.'
Assert-Invalid (
    New-Config `
        @((New-Entry 'dash')) `
        @((New-Mode 'shield' @('dash') -DisplayLabel '')) `
        'shield'
) 'A multi-mode player label must be required.'
$missingIconConfig = New-Config `
    @((New-Entry 'dash')) `
    @((New-Mode 'shield' @('dash') -DisplayLabel '护盾形态' -GizmoIconTexPath $null)) `
    'shield'
Assert-Valid (Resolve-Config $missingIconConfig) `
    'A multi-mode gizmo icon path must remain optional.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @('dash')), (New-Mode 'SHIELD' @('dash'))) 'shield') `
    'ModeKey comparison must reject case-insensitive duplicates.'
Assert-Invalid (New-Config @((New-Entry ''), (New-Entry 'dash'))) `
    'Entry Id must not be blank.'
Assert-Invalid (New-Config @((New-Entry 'dash'), (New-Entry 'DASH'))) `
    'Entry Id comparison must reject case-insensitive duplicates.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @())) 'shield') `
    'Mode must not be empty.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @('missing'))) 'shield') `
    'Mode must not reference a missing entry.'
Assert-Invalid (New-Config @((New-Entry 'dash')) @((New-Mode 'shield' @('dash', 'DASH'))) 'shield') `
    'Mode must not repeat the same entry.'
Assert-Invalid (New-Config @((New-Entry 'dash'), (New-Entry 'unused')) @((New-Mode 'shield' @('dash'))) 'shield') `
    'Every multi-mode catalog entry must be referenced.'
Assert-Invalid (New-Config @((New-Entry 'child' $attachedRelation 'parent'))) `
    'Attached entry must include its parent in a single-mode catalog.'
Assert-Invalid (New-Config @((New-Entry 'child' $attachedRelation 'parent'), (New-Entry 'parent'))) `
    'Attached entry must follow its parent in a single-mode catalog.'
Assert-Invalid (
    New-Config `
        @((New-Entry 'parent'), (New-Entry 'child' $attachedRelation 'parent')) `
        @((New-Mode 'shield' @('child', 'parent'))) `
        'shield'
) 'Attached entry must follow its parent in each mode selection.'

Write-Output 'ChipExpressionModeResolutionSmokeTests PASS'
