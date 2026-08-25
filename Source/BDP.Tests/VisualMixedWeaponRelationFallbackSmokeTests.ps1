$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 为内部自动属性赋值。
function Set-Property
{
    param($Target, [string]$Name, $Value)
    $property = $Target.GetType().GetProperty(
        $Name,
        [Reflection.BindingFlags]'Public,NonPublic,Instance')
    $property.SetValue($Target, $Value)
}

# 构造一条来自指定芯片实例的单侧远程武器结果。
function New-WeaponResult
{
    param([string]$Id, [string]$ChipThingId)

    $source = [Activator]::CreateInstance($sourceReferenceType, $true)
    Set-Property $source 'ChipThingId' $ChipThingId
    Set-Property $source 'ChipDefName' ('Test_' + $ChipThingId)
    Set-Property $source 'Side' $mainSide
    Set-Property $source 'SlotIndex' 0

    $result = [Activator]::CreateInstance($formalResultType, $true)
    Set-Property $result 'Id' $Id
    Set-Property $result 'ResultKind' $verbResultKind
    Set-Property $result 'WeaponMode' $rangedWeaponMode
    Set-Property $result 'SourceReference' $source
    Set-Property $result 'CompositeKind' $noneCompositeKind
    Set-Property $result 'VisualPresetDefName' 'Test_Single'
    Set-Property $result 'CompositeVisualPresetDefName' 'Test_Dual'
    Set-Property $result 'IsAvailable' $true
    Set-Property $result 'CanProject' $true
    return $result
}

# 构造一条可用的 Combo（组合）正式结果。
function New-ComboResult
{
    param([string]$VisualPresetDefName)

    $result = [Activator]::CreateInstance($formalResultType, $true)
    Set-Property $result 'Id' 'combo'
    Set-Property $result 'ResultKind' $verbResultKind
    Set-Property $result 'WeaponMode' $rangedWeaponMode
    Set-Property $result 'CompositeKind' $comboCompositeKind
    Set-Property $result 'VisualPresetDefName' $VisualPresetDefName
    Set-Property $result 'IsAvailable' $true
    Set-Property $result 'CanProject' $true
    return $result
}

# 构造一条可用的正式 DualWeapon（双武器）结果。
function New-DualWeaponResult
{
    $result = [Activator]::CreateInstance($formalResultType, $true)
    Set-Property $result 'Id' 'dual'
    Set-Property $result 'ResultKind' $verbResultKind
    Set-Property $result 'WeaponMode' $rangedWeaponMode
    Set-Property $result 'CompositeKind' $dualCompositeKind
    Set-Property $result 'IsAvailable' $true
    Set-Property $result 'CanProject' $true
    return $result
}

# 构造不可变结果表并调用真实视觉投影器。
function Invoke-Projection
{
    param([object[]]$Results)

    $listType = [Collections.Generic.List``1].MakeGenericType($formalResultType)
    $list = [Activator]::CreateInstance($listType)
    foreach ($result in $Results) { [void]$list.Add($result) }

    $snapshot = [Activator]::CreateInstance($snapshotType, $true)
    Set-Property $snapshot 'Results' $list.AsReadOnly()
    return $buildMethod.Invoke($builder, @($snapshot))
}

$modRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll'))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'))

$formalResultType = $coreAssembly.GetType('BDP.Core.Expressions.FormalExpressionResult', $true)
$sourceReferenceType = $coreAssembly.GetType('BDP.Core.Expressions.ExpressionSourceReference', $true)
$snapshotType = $coreAssembly.GetType('BDP.Core.Expressions.ExpressionSnapshot', $true)
$builderType = $coreAssembly.GetType('BDP.Core.Expressions.DefaultVisualProjectionBuilder', $true)
$resultKindType = $coreAssembly.GetType('BDP.Core.Expressions.ExpressionResultKind', $true)
$weaponModeType = $coreAssembly.GetType('BDP.Core.Expressions.WeaponExpressionMode', $true)
$compositeKindType = $coreAssembly.GetType('BDP.Core.Expressions.CompositeExpressionKind', $true)
$relationKindType = $coreAssembly.GetType('BDP.Core.Expressions.VisualExpressionRelationKind', $true)
$triggerSideType = $coreAssembly.GetType('BDP.Core.Trigger.TriggerSide', $true)

$verbResultKind = [Enum]::Parse($resultKindType, 'Verb')
$rangedWeaponMode = [Enum]::Parse($weaponModeType, 'Ranged')
$noneCompositeKind = [Enum]::Parse($compositeKindType, 'None')
$dualCompositeKind = [Enum]::Parse($compositeKindType, 'DualWeapon')
$comboCompositeKind = [Enum]::Parse($compositeKindType, 'Combo')
$singleSideRelation = [Enum]::Parse($relationKindType, 'SingleSide')
$dualWeaponRelation = [Enum]::Parse($relationKindType, 'DualWeapon')
$comboRelation = [Enum]::Parse($relationKindType, 'Combo')
$mainSide = [Enum]::Parse($triggerSideType, 'Main')

$builder = [Activator]::CreateInstance($builderType, $true)
$buildMethod = $builderType.GetMethod('Build', [Reflection.BindingFlags]'Public,NonPublic,Instance')

$rifle = New-WeaponResult 'rifle' 'rifle-chip'
$shotgun = New-WeaponResult 'shotgun' 'shotgun-chip'

$singleProjection = Invoke-Projection @($rifle)
Assert-True ($singleProjection.RelationKind -eq $singleSideRelation) `
    '单个武器芯片实例必须保持 SingleSide（单侧）视觉。'

$mixedProjection = Invoke-Projection @($rifle, $shotgun)
Assert-True ($mixedProjection.RelationKind -eq $dualWeaponRelation) `
    '两个不同武器芯片实例即使没有正式 DualWeapon 攻击结果，也必须采用 DualWeapon（双武器）视觉。'

$visualLessComboProjection = Invoke-Projection @($rifle, $shotgun, (New-ComboResult $null))
Assert-True ($visualLessComboProjection.RelationKind -eq $dualWeaponRelation) `
    '没有声明视觉预设的 Combo（组合）不得阻断双武器视觉回退。'

$visualComboProjection = Invoke-Projection @($rifle, $shotgun, (New-ComboResult 'Test_Combo'))
Assert-True ($visualComboProjection.RelationKind -eq $comboRelation) `
    '显式声明视觉预设的可用 Combo（组合）必须保持最高视觉优先级。'

$visualComboAfterDualProjection = Invoke-Projection @(
    $rifle,
    $shotgun,
    (New-DualWeaponResult),
    (New-ComboResult 'Test_Combo'))
Assert-True ($visualComboAfterDualProjection.RelationKind -eq $comboRelation) `
    '显式 Combo（组合）的视觉优先级不得依赖正式结果表中的排列顺序。'

Write-Output 'VisualMixedWeaponRelationFallbackSmokeTests PASS'
