$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 为内部自动属性或属性赋值。
function Set-Property
{
    param($Target, [string]$Name, $Value)
    $property = $Target.GetType().GetProperty(
        $Name,
        [Reflection.BindingFlags]'Public,NonPublic,Instance')
    $property.SetValue($Target, $Value)
}

# 构造一条没有待命视觉、但可作为组合技阶段绘制来源的远程结果。
function New-SourceResult
{
    $sourceReference = [Activator]::CreateInstance($sourceReferenceType, $true)
    Set-Property $sourceReference 'ChipThingId' 'trion-main-chip'
    Set-Property $sourceReference 'ChipDefName' 'Test_TrionChip'
    Set-Property $sourceReference 'Side' $mainSide
    Set-Property $sourceReference 'SlotIndex' 0

    $result = [Activator]::CreateInstance($formalResultType, $true)
    Set-Property $result 'Id' 'source-main'
    Set-Property $result 'OutputChannel' $verbOutputChannel
    Set-Property $result 'WeaponMode' $rangedWeaponMode
    Set-Property $result 'CompositeKind' $noneCompositeKind
    Set-Property $result 'SourceReference' $sourceReference
    Set-Property $result 'IsAvailable' $true
    Set-Property $result 'CanProject' $true
    return $result
}

# 构造一条只有组合技阶段视觉预设的组合结果。
function New-ComboResult
{
    $result = [Activator]::CreateInstance($formalResultType, $true)
    Set-Property $result 'Id' 'combo'
    Set-Property $result 'OutputChannel' $verbOutputChannel
    Set-Property $result 'WeaponMode' $rangedWeaponMode
    Set-Property $result 'CompositeKind' $comboCompositeKind
    Set-Property $result 'ComboAttackVisualPresetDefName' 'BDP_Visual_TrionCube'
    Set-Property $result 'IsAvailable' $true
    Set-Property $result 'CanProject' $true
    return $result
}

# 构造 Combo（组合）结果到来源结果的正式引用。
function New-ComboReference
{
    $reference = [Activator]::CreateInstance($compositeReferenceType, $true)
    Set-Property $reference 'CompositeId' 'combo'
    Set-Property $reference 'CompositeKind' $comboCompositeKind
    Set-Property $reference 'SourceResultIds' ([Collections.Generic.List[string]]@('source-main')).AsReadOnly()
    Set-Property $reference 'MainSourceResultId' 'source-main'
    return $reference
}

# 调用真实视觉投影器。
function Invoke-Projection
{
    $resultList = [Collections.Generic.List``1].MakeGenericType($formalResultType)
    $results = [Activator]::CreateInstance($resultList)
    [void]$results.Add((New-SourceResult))
    [void]$results.Add((New-ComboResult))

    $referenceList = [Collections.Generic.List``1].MakeGenericType($compositeReferenceType)
    $references = [Activator]::CreateInstance($referenceList)
    [void]$references.Add((New-ComboReference))

    $snapshot = [Activator]::CreateInstance($snapshotType, $true)
    Set-Property $snapshot 'Results' $results.AsReadOnly()
    Set-Property $snapshot 'CompositeReferences' $references.AsReadOnly()
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
$compositeReferenceType = $coreAssembly.GetType('BDP.Core.Expressions.CompositeExpressionReference', $true)
$builderType = $coreAssembly.GetType('BDP.Core.Expressions.DefaultVisualProjectionBuilder', $true)
$projectionType = $coreAssembly.GetType('BDP.Core.Expressions.VisualExpressionProjection', $true)
$bindingType = $coreAssembly.GetType('BDP.Core.Expressions.ComboAttackVisualBinding', $true)
$residentEntryType = $coreAssembly.GetType('BDP.Core.Expressions.VisualResidentEntry', $true)
$stageSnapshotType = $coreAssembly.GetType('BDP.Core.Trigger.Visual.WeaponVisualStageSnapshot', $true)
$stageType = $coreAssembly.GetType('BDP.Core.Trigger.Visual.WeaponVisualActionStage', $true)
$resultKindType = $coreAssembly.GetType('BDP.Core.Expressions.ExpressionOutputChannel', $true)
$weaponModeType = $coreAssembly.GetType('BDP.Core.Expressions.WeaponExpressionMode', $true)
$compositeKindType = $coreAssembly.GetType('BDP.Core.Expressions.CompositeExpressionKind', $true)
$relationKindType = $coreAssembly.GetType('BDP.Core.Expressions.VisualExpressionRelationKind', $true)
$renderModeType = $coreAssembly.GetType('BDP.Core.Expressions.HostEquipmentRenderMode', $true)
$triggerSideType = $coreAssembly.GetType('BDP.Core.Trigger.TriggerSide', $true)
$effectiveResolverType = $coreAssembly.GetType('BDP.Core.Trigger.Visual.TriggerVisualEffectiveEntryResolver', $true)

$verbOutputChannel = [Enum]::Parse($resultKindType, 'Verb')
$rangedWeaponMode = [Enum]::Parse($weaponModeType, 'Ranged')
$noneCompositeKind = [Enum]::Parse($compositeKindType, 'None')
$comboCompositeKind = [Enum]::Parse($compositeKindType, 'Combo')
$noneRelation = [Enum]::Parse($relationKindType, 'None')
$replaceRenderMode = [Enum]::Parse($renderModeType, 'Replace')
$mainSide = [Enum]::Parse($triggerSideType, 'Main')
$idleStage = [Enum]::Parse($stageType, 'Idle')
$warmupStage = [Enum]::Parse($stageType, 'Warmup')
$firingStage = [Enum]::Parse($stageType, 'Firing')
$cooldownStage = [Enum]::Parse($stageType, 'Cooldown')

$builder = [Activator]::CreateInstance($builderType, $true)
$buildMethod = $builderType.GetMethod('Build', [Reflection.BindingFlags]'Public,NonPublic,Instance')
$projection = Invoke-Projection

Assert-True ($projection.RelationKind -eq $noneRelation) `
    '只有组合技阶段视觉候选时，待命视觉关系不得伪装成普通单侧关系。'
Assert-True ($projection.HostEquipmentRenderMode -eq $replaceRenderMode) `
    '组合技阶段视觉候选必须进入完整绘制入口，不能被单武器贴图替换路径绕过。'
Assert-True ($projection.ComboAttackVisualBindings.Count -eq 1) `
    '只有组合技阶段视觉预设时，正式视觉投影必须建立一条组合技视觉绑定。'
Assert-True ($projection.ResidentEntries.Count -eq 1) `
    '只有组合技阶段视觉预设时，绑定归属来源仍必须进入阶段绘制候选。'
Assert-True ([string]$projection.ResidentEntries[0].ResultId -eq 'source-main') `
    '组合技阶段绘制候选必须绑定到复合引用声明的主侧来源结果。'

$resolveMethod = @($effectiveResolverType.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static') |
    Where-Object { $_.Name -eq 'ResolveComboAttackVisualBinding' }) | Select-Object -First 1
$stageConstructor = $stageSnapshotType.GetConstructor(
    [Reflection.BindingFlags]'Public,NonPublic,Instance',
    $null,
    [Type[]]@(
        $stageType,
        [single],
        [int],
        [string],
        [string],
        [string],
        [int]),
    $null)
$bindingForWarmup = $resolveMethod.Invoke(
    $null,
    @(
        $projection,
        $stageConstructor.Invoke(@($warmupStage, [single]0.5, 5, 'source-main', 'combo', 'attack', 1))))
$bindingForFiring = $resolveMethod.Invoke(
    $null,
    @(
        $projection,
        ($firingSnapshot = $stageConstructor.Invoke(@($firingStage, [single]0, 3, 'source-main', 'combo', 'attack', 1)))))
$bindingForCooldown = $resolveMethod.Invoke(
    $null,
    @(
        $projection,
        $stageConstructor.Invoke(@($cooldownStage, [single]0.8, 2, 'source-main', 'combo', 'attack', 1))))
$bindingForIdle = $resolveMethod.Invoke(
    $null,
    @(
        $projection,
        $stageConstructor.Invoke(@($idleStage, [single]0, 0, 'source-main', 'combo', 'attack', 1))))
$bindingForNonComboHost = $resolveMethod.Invoke(
    $null,
    @(
        $projection,
        $stageConstructor.Invoke(@($warmupStage, [single]0.5, 5, 'source-main', 'source-main', 'attack', 1))))

$findLaunchSourceMethod = @($effectiveResolverType.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static') |
    Where-Object { $_.Name -eq 'FindLaunchSourceEntry' }) | Select-Object -First 1
$comboEntry = [Activator]::CreateInstance($residentEntryType, $true)
Set-Property $comboEntry 'ResultId' 'combo'
$subEntry = [Activator]::CreateInstance($residentEntryType, $true)
Set-Property $subEntry 'ResultId' 'source-sub'
$residentListType = [Collections.Generic.List``1].MakeGenericType($residentEntryType)
$residentEntries = [Activator]::CreateInstance($residentListType)
[void]$residentEntries.Add($projection.ResidentEntries[0])
[void]$residentEntries.Add($comboEntry)
[void]$residentEntries.Add($subEntry)
Set-Property $projection 'ResidentEntries' $residentEntries.AsReadOnly()
$launchSourceEntry = $findLaunchSourceMethod.Invoke($null, @($projection, 'combo'))
$resolveLaunchSourceMethod = @($effectiveResolverType.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static') |
    Where-Object { $_.Name -eq 'ResolveLaunchSourceEntry' }) | Select-Object -First 1
$launchSourceForSub = $resolveLaunchSourceMethod.Invoke($null, @($projection, $subEntry, $firingSnapshot))

Assert-True ($null -ne $bindingForWarmup -and $null -ne $bindingForFiring -and $null -ne $bindingForCooldown) `
    '组合技视觉绑定必须覆盖预热、射击、冷却三个攻击阶段。'
Assert-True ($null -eq $bindingForIdle) `
    '待命阶段不得消费组合技攻击视觉绑定。'
Assert-True ($null -eq $bindingForNonComboHost) `
    '非组合技宿主不得消费组合技攻击视觉绑定。'
Assert-True ($null -ne $launchSourceEntry -and [string]$launchSourceEntry.ResultId -eq 'source-main') `
    '发射计划携带组合结果标识时，视觉根点必须回到组合技绑定的来源绘制条目。'
Assert-True ($null -ne $launchSourceForSub -and [string]$launchSourceForSub.ResultId -eq 'source-main') `
    '组合技实际发射副侧来源时，视觉根点也必须复用主侧 Owner 的最终视觉条目。'

Write-Output 'ComboAttackVisualProjectionRuntimeSmokeTests PASS'
