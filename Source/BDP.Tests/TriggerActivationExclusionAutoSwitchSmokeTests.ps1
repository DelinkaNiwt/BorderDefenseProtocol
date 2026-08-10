$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$servicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'
$transitionPath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'

$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$transitionText = Get-Content -LiteralPath $transitionPath -Raw -Encoding utf8

Assert-True (
    ($serviceText -match 'FindActivationBlockers') -and
    ($serviceText -match 'CancelConflictingPendingTargets') -and
    ($serviceText -notmatch 'public\s+bool\s+HasExclusionConflict') -and
    ($serviceText -notmatch 'Activation rejected: exclusion conflict')
) 'Activation requests must discover blockers and physically remove the obsolete reject-only conflict query.'

Assert-True (
    ($transitionText -match 'BeginActivationBlockerDeactivations') -and
    ($transitionText -match 'foreach\s*\(\s*TriggerSlotState\s+blocker') -and
    ($transitionText -match 'BuildWaitingForConflictsContext')
) 'The transition service must start all blocker winddowns and retain one waiting target.'
Assert-True (
    ($transitionText -match 'ResolveWaitingForConflicts') -and
    ($transitionText -match 'resolveActivationBlockers') -and
    ($transitionText -match 'WaitingForConflicts')
) 'Waiting resolution must rescan live blockers before warmup.'
Assert-True (
    ($transitionText -match 'IsSamePendingTarget') -and
    ($transitionText -match 'PreserveDeactivatingWithoutTarget') -and
    ($transitionText -match 'CancelConflictingPendingTargets')
) 'Repeated targets, retargeting, and preserved winddowns must have explicit transition boundaries.'
Assert-True (
    ($transitionText -match 'ShouldUseSynchronizedHandTransition') -and
    ($transitionText -match 'ActivateSynchronizedTargets') -and
    ($transitionText -match 'targetChipThingId')
) 'Paired-hand targets must keep synchronized transitions and one target identity.'

$assemblyPath = Join-Path $repoRoot '1.6\Assemblies\BDP.Core.dll'
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\839005762\1.6\Assemblies\0Harmony.dll'
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
$transitionType = $assembly.GetType('BDP.Core.Trigger.TriggerSwitchTransitionService', $true)
$flags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::Public
$windingBuilder = $transitionType.GetMethod('BuildDeactivatingContext', $flags)
$waitingBuilder = $transitionType.GetMethod('BuildWaitingForConflictsContext', $flags)
Assert-True ($null -ne $waitingBuilder) 'Waiting context builder must exist at runtime.'

$first = $windingBuilder.Invoke($null, [object[]]@(30, 100, 1, 0, 'target-chip'))
$second = $windingBuilder.Invoke($null, [object[]]@(75, 100, -1, 0, $null))
Assert-True ($first.phaseEndTick -eq 130) `
    'A same-side blocker must retain its own 30-tick winddown from the command tick.'
Assert-True ($second.phaseEndTick -eq 175) `
    'A cross-side blocker must retain its own 75-tick winddown from the same command tick.'

$waiting = $waitingBuilder.Invoke($null, [object[]]@(1, 'target-chip'))
Assert-True ($waiting.phaseEndTick -eq 0) `
    'Waiting must not invent a fixed completion tick.'
Assert-True ($waiting.targetChipThingId -eq 'target-chip') `
    'Waiting must retain the exact target identity.'

$slotType = $assembly.GetType('BDP.Core.Trigger.TriggerSlotState', $true)
$sideType = $assembly.GetType('BDP.Core.Trigger.TriggerSide', $true)
$switchContextType = $assembly.GetType('BDP.Core.Trigger.SwitchContext', $true)
$switchRequestContextType = $assembly.GetType('BDP.Core.Trigger.TriggerSwitchContext', $true)
$serviceType = $assembly.GetType('BDP.Core.Trigger.TriggerService', $true)
$gameAssembly = [AppDomain]::CurrentDomain.GetAssemblies() |
    Where-Object { $_.GetName().Name -eq 'Assembly-CSharp' } |
    Select-Object -First 1
$thingType = $gameAssembly.GetType('Verse.Thing', $true)
$thingDefType = $gameAssembly.GetType('Verse.ThingDef', $true)
$defType = $gameAssembly.GetType('Verse.Def', $true)
$thingIdProperty = $thingType.GetProperty('ThingID')
$disableReasonType = $assembly.GetType('BDP.Core.Trigger.TriggerDisableReason', $true)
$instanceFlags = [System.Reflection.BindingFlags]::Instance `
    -bor [System.Reflection.BindingFlags]::Public `
    -bor [System.Reflection.BindingFlags]::NonPublic

function New-Delegate {
    param(
        [Type]$DelegateType,
        [scriptblock]$Body
    )

    return [System.Management.Automation.LanguagePrimitives]::ConvertTo(
        $Body,
        $DelegateType,
        [System.Globalization.CultureInfo]::InvariantCulture)
}

function Set-ContextDelegate {
    param(
        [object]$Context,
        [string]$FieldName,
        [scriptblock]$Body
    )

    $field = $switchRequestContextType.GetField($FieldName, $instanceFlags)
    $field.SetValue($Context, (New-Delegate $field.FieldType $Body))
}

# 用真实 Thing、真实槽位和正式 RequestActivate/到期结算入口复现成对主副槽前摇。
$chip = [System.Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingType)
$chipDef = [System.Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingDefType)
$defType.GetField('defName', $instanceFlags).SetValue($chipDef, 'BDP_PairedRuntimeTestChip')
$thingType.GetField('def', $instanceFlags).SetValue($chip, $chipDef)
$thingType.GetField('thingIDNumber', $instanceFlags).SetValue($chip, 91001)
$chipThingId = $thingType.GetProperty('ThingID', $instanceFlags).GetValue($chip, $null)

$mainSide = [Enum]::Parse($sideType, 'Main')
$subSide = [Enum]::Parse($sideType, 'Sub')
$specialSide = [Enum]::Parse($sideType, 'Special')
$mainSlot = [Activator]::CreateInstance($slotType, [object[]]@($mainSide, 0))
$subSlot = [Activator]::CreateInstance($slotType, [object[]]@($subSide, 0))
$slotType.GetMethod('SetLoadedChip').Invoke($mainSlot, [object[]]@($chip))
$slotType.GetMethod('SetLoadedChip').Invoke($subSlot, [object[]]@($chip))
$slotType.GetMethod('SetBinding').Invoke(
    $mainSlot,
    [object[]]@($false, $mainSide, 0, $subSide, 0))
$slotType.GetMethod('SetBinding').Invoke(
    $subSlot,
    [object[]]@($true, $mainSide, 0, $mainSide, 0))

$slots = @{
    Main = $mainSlot
    Sub = $subSlot
}
$contexts = @{
    Main = $null
    Sub = $null
    Special = $null
}
$slotListType = [System.Collections.Generic.List``1].MakeGenericType($slotType)
$emptySlots = [Activator]::CreateInstance($slotListType)
$activationCommitCount = [int[]]@(0)
$activationCommitSide = [string[]]@('')

$requestContext = [Activator]::CreateInstance($switchRequestContextType, $true)
Set-ContextDelegate $requestContext 'GetSlot' {
    param($side, $index)
    if ($index -ne 0) {
        return $null
    }

    return $slots[$side.ToString()]
}
Set-ContextDelegate $requestContext 'GetActiveSlot' {
    param($side)
    $slot = $slots[$side.ToString()]
    if ($null -ne $slot -and $slot.IsActive) {
        return $slot
    }

    return $null
}
Set-ContextDelegate $requestContext 'GetActiveSlotRaw' {
    param($side)
    $slot = $slots[$side.ToString()]
    if ($null -ne $slot -and $slot.IsActive) {
        return $slot
    }

    return $null
}
Set-ContextDelegate $requestContext 'GetSwitchContext' {
    param($side)
    return $contexts[$side.ToString()]
}
Set-ContextDelegate $requestContext 'SetSwitchContext' {
    param($side, $context)
    $contexts[$side.ToString()] = $context
}
Set-ContextDelegate $requestContext 'FindActivationBlockers' {
    param($slot)
    return $emptySlots
}
Set-ContextDelegate $requestContext 'HasActivationExclusionConflict' {
    param($left, $right)
    return $false
}
Set-ContextDelegate $requestContext 'IsPendingTargetValid' {
    param($slot)
    return $null -ne $slot -and $null -ne $slot.LoadedChip
}
Set-ContextDelegate $requestContext 'ResolveChipActivationDelayTicks' {
    param($loadedChip)
    return 60
}
Set-ContextDelegate $requestContext 'ResolveChipDeactivationDelayTicks' {
    param($loadedChip)
    return 30
}
Set-ContextDelegate $requestContext 'NotifySlotActivationCommitted' {
    param($side, $index, $loadedChip)
    $activationCommitCount[0]++
    $activationCommitSide[0] = $side.ToString()
}
Set-ContextDelegate $requestContext 'NotifySlotDeactivated' {
    param($side, $index, $loadedChip)
}
$switchRequestContextType.GetField('CurrentTick', $instanceFlags).SetValue($requestContext, 100)

$service = [Activator]::CreateInstance($serviceType, $true)
$requestActivate = $serviceType.GetMethod('RequestActivate', $instanceFlags)
$accepted = [bool]$requestActivate.Invoke(
    $service,
    [object[]]@($requestContext, $mainSide, 0))
Assert-True $accepted 'Formal RequestActivate must accept a valid paired-hand target.'
Assert-True (
    $contexts.Main.phase.ToString() -eq 'Activating' -and
    $contexts.Sub.phase.ToString() -eq 'Activating' -and
    $contexts.Main.phaseEndTick -eq 160 -and
    $contexts.Sub.phaseEndTick -eq 160
) 'A paired-hand target must enter synchronized non-zero warmup on both sides.'

$resolveDue = $transitionType.GetMethod('ResolveDueSwitchTransitions', $flags)
$resolveParameters = $resolveDue.GetParameters()
$getSlot = New-Delegate $resolveParameters[5].ParameterType {
    param($side, $index)
    if ($index -ne 0) {
        return $null
    }

    return $slots[$side.ToString()]
}
$getActiveSlotRaw = New-Delegate $resolveParameters[6].ParameterType {
    param($side)
    $slot = $slots[$side.ToString()]
    if ($null -ne $slot -and $slot.IsActive) {
        return $slot
    }

    return $null
}
$getSwitchContext = New-Delegate $resolveParameters[3].ParameterType {
    param($side)
    return $contexts[$side.ToString()]
}
$setSwitchContext = New-Delegate $resolveParameters[4].ParameterType {
    param($side, $context)
    $contexts[$side.ToString()] = $context
}
$resolveBlockers = New-Delegate $resolveParameters[7].ParameterType {
    param($slot)
    return $emptySlots
}
$pendingValid = New-Delegate $resolveParameters[8].ParameterType {
    param($slot)
    return $null -ne $slot -and $null -ne $slot.LoadedChip
}
$deactivateBound = New-Delegate $resolveParameters[9].ParameterType {
    param($slot)
}
$notifyActivated = New-Delegate $resolveParameters[11].ParameterType {
    param($side, $index, $loadedChip)
    $activationCommitCount[0]++
    $activationCommitSide[0] = $side.ToString()
}
$notifyDeactivated = New-Delegate $resolveParameters[12].ParameterType {
    param($side, $index, $loadedChip)
}
$warmupResolver = New-Delegate $resolveParameters[1].ParameterType {
    param($loadedChip)
    return 60
}
$winddownResolver = New-Delegate $resolveParameters[2].ParameterType {
    param($loadedChip)
    return 30
}

# 这里故意与 CompTriggerBody 的正式接线一致：同步激活委托传 null。
$resolveDue.Invoke(
    $null,
    [object[]]@(
        160,
        $warmupResolver,
        $winddownResolver,
        $getSwitchContext,
        $setSwitchContext,
        $getSlot,
        $getActiveSlotRaw,
        $resolveBlockers,
        $pendingValid,
        $deactivateBound,
        $null,
        $notifyActivated,
        $notifyDeactivated))
Assert-True ($mainSlot.IsActive -and $subSlot.IsActive) `
    'A paired-hand target must become active after its synchronized warmup expires.'
Assert-True ($activationCommitCount[0] -eq 1 -and $activationCommitSide[0] -eq 'Main') `
    'Paired-hand warmup completion must publish one activation commit from the root slot.'
Assert-True ($null -eq $contexts.Main -and $null -eq $contexts.Sub) `
    'Paired-hand warmup completion must clear both synchronized contexts.'

# 镜像侧在前摇期间失效时，成对目标必须保持双侧全关且不发布成功通知。
$mainSlot.SetActive($false)
$subSlot.SetActive($false)
$subSlot.SetDisabled(
    $true,
    [Enum]::Parse($disableReasonType, 'CombatBodyUnavailable'))
$contexts.Main = $null
$contexts.Sub = $null
$activationCommitCount[0] = 0
$activationCommitSide[0] = ''
$switchRequestContextType.GetField('CurrentTick', $instanceFlags).SetValue(
    $requestContext,
    200)
$accepted = [bool]$requestActivate.Invoke(
    $service,
    [object[]]@($requestContext, $mainSide, 0))
Assert-True (
    $accepted -and
    $contexts.Main.phaseEndTick -eq 260 -and
    $contexts.Sub.phaseEndTick -eq 260
) 'The paired target must enter warmup before its mirror becomes an invalid commit target.'
$resolveDue.Invoke(
    $null,
    [object[]]@(
        260,
        $warmupResolver,
        $winddownResolver,
        $getSwitchContext,
        $setSwitchContext,
        $getSlot,
        $getActiveSlotRaw,
        $resolveBlockers,
        $pendingValid,
        $deactivateBound,
        $null,
        $notifyActivated,
        $notifyDeactivated))
Assert-True (-not $mainSlot.IsActive -and -not $subSlot.IsActive) `
    'A disabled paired mirror must never leave only the root slot active.'
Assert-True ($activationCommitCount[0] -eq 0) `
    'A failed paired activation must not publish a root activation commit.'
Assert-True ($null -eq $contexts.Main -and $null -eq $contexts.Sub) `
    'An invalid paired target must clear both synchronized contexts.'
$subSlot.SetDisabled(
    $false,
    [Enum]::Parse($disableReasonType, 'None'))

# 走同一正式请求与结算入口，验证多个阻挡者、实时复查和完整前摇。
$nextRuntimeThingId = [int[]]@(92000)
function New-RuntimeThing {
    param([string]$DefName)

    $nextRuntimeThingId[0]++
    $runtimeThing = [System.Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingType)
    $runtimeDef = [System.Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingDefType)
    [void]$defType.GetField('defName', $instanceFlags).SetValue($runtimeDef, $DefName)
    [void]$thingType.GetField('def', $instanceFlags).SetValue($runtimeThing, $runtimeDef)
    [void]$thingType.GetField('thingIDNumber', $instanceFlags).SetValue(
        $runtimeThing,
        $nextRuntimeThingId[0])
    return $runtimeThing
}

function New-RuntimeSlot {
    param(
        [object]$Side,
        [int]$Index,
        [object]$LoadedChip,
        [bool]$Active
    )

    $runtimeSlot = [Activator]::CreateInstance($slotType, [object[]]@($Side, $Index))
    [void]$slotType.GetMethod('SetLoadedChip').Invoke($runtimeSlot, [object[]]@($LoadedChip))
    [void]$slotType.GetMethod('SetActive').Invoke($runtimeSlot, [object[]]@($Active))
    return $runtimeSlot
}

$blockerAChip = New-RuntimeThing 'BDP_RuntimeBlockerA'
$targetBChip = New-RuntimeThing 'BDP_RuntimeTargetB'
$blockerCChip = New-RuntimeThing 'BDP_RuntimeBlockerC'
$blockerDChip = New-RuntimeThing 'BDP_RuntimeBlockerD'
$blockerA = New-RuntimeSlot $mainSide 0 $blockerAChip $true
$targetB = New-RuntimeSlot $mainSide 1 $targetBChip $false
$blockerC = New-RuntimeSlot $subSide 0 $blockerCChip $true
$blockerD = New-RuntimeSlot $specialSide 0 $blockerDChip $true
$multiSlots = @{
    'Main:0' = $blockerA
    'Main:1' = $targetB
    'Sub:0' = $blockerC
    'Special:0' = $blockerD
}
$multiContexts = @{
    Main = $null
    Sub = $null
    Special = $null
}
$blockerAThingId = $thingIdProperty.GetValue($blockerAChip, $null)
$targetBThingId = $thingIdProperty.GetValue($targetBChip, $null)
$blockerCThingId = $thingIdProperty.GetValue($blockerCChip, $null)
$blockerDThingId = $thingIdProperty.GetValue($blockerDChip, $null)
$winddownByThingId = @{
    $blockerAThingId = 30
    $blockerCThingId = 75
    $blockerDThingId = 45
}
$multiActivationCommits = [int[]]@(0)

$multiRequestContext = [Activator]::CreateInstance($switchRequestContextType, $true)
Set-ContextDelegate $multiRequestContext 'GetSlot' {
    param($side, $index)
    return $multiSlots[$side.ToString() + ':' + $index]
}
Set-ContextDelegate $multiRequestContext 'GetActiveSlot' {
    param($side)
    foreach ($runtimeSlot in $multiSlots.Values) {
        if ($runtimeSlot.Side.ToString() -eq $side.ToString() -and $runtimeSlot.IsActive) {
            return $runtimeSlot
        }
    }

    return $null
}
Set-ContextDelegate $multiRequestContext 'GetActiveSlotRaw' {
    param($side)
    foreach ($runtimeSlot in $multiSlots.Values) {
        if ($runtimeSlot.Side.ToString() -eq $side.ToString() -and $runtimeSlot.IsActive) {
            return $runtimeSlot
        }
    }

    return $null
}
Set-ContextDelegate $multiRequestContext 'GetSwitchContext' {
    param($side)
    return $multiContexts[$side.ToString()]
}
Set-ContextDelegate $multiRequestContext 'SetSwitchContext' {
    param($side, $context)
    $multiContexts[$side.ToString()] = $context
}
Set-ContextDelegate $multiRequestContext 'FindActivationBlockers' {
    param($target)
    $currentBlockers = [Activator]::CreateInstance($slotListType)
    foreach ($runtimeSlot in $multiSlots.Values) {
        if ($runtimeSlot -ne $target -and $runtimeSlot.IsActive) {
            [void]$currentBlockers.Add($runtimeSlot)
        }
    }

    return ,$currentBlockers
}
Set-ContextDelegate $multiRequestContext 'HasActivationExclusionConflict' {
    param($left, $right)
    return $left -ne $right
}
Set-ContextDelegate $multiRequestContext 'IsPendingTargetValid' {
    param($slot)
    return $null -ne $slot -and $null -ne $slot.LoadedChip
}
Set-ContextDelegate $multiRequestContext 'ResolveChipActivationDelayTicks' {
    param($loadedChip)
    return 60
}
Set-ContextDelegate $multiRequestContext 'ResolveChipDeactivationDelayTicks' {
    param($loadedChip)
    return $winddownByThingId[$thingIdProperty.GetValue($loadedChip, $null)]
}
Set-ContextDelegate $multiRequestContext 'NotifySlotActivationCommitted' {
    param($side, $index, $loadedChip)
    $multiActivationCommits[0]++
}
Set-ContextDelegate $multiRequestContext 'NotifySlotDeactivated' {
    param($side, $index, $loadedChip)
}
$switchRequestContextType.GetField('CurrentTick', $instanceFlags).SetValue(
    $multiRequestContext,
    100)
$multiGetSlotDelegate = $switchRequestContextType.GetField(
    'GetSlot',
    $instanceFlags).GetValue($multiRequestContext)
$resolvedMultiTarget = $multiGetSlotDelegate.DynamicInvoke($mainSide, 1)
Assert-True ($resolvedMultiTarget.GetType() -eq $slotType) `
    'The runtime state machine must resolve one concrete target slot.'

$accepted = [bool]$requestActivate.Invoke(
    $service,
    [object[]]@($multiRequestContext, $mainSide, 1))
Assert-True $accepted 'Formal RequestActivate must accept the target behind three blockers.'
Assert-True (
    $multiContexts.Main.phaseEndTick -eq 130 -and
    $multiContexts.Sub.phaseEndTick -eq 175 -and
    $multiContexts.Special.phaseEndTick -eq 145
) 'All blockers must start their own winddown from the same command tick.'
Assert-True (
    $multiContexts.Main.targetSlotIndex -eq 1 -and
    $multiContexts.Main.targetChipThingId -eq $targetBThingId
) 'The same-side blocker winddown must retain the pending target identity.'

# 重复请求同一目标不得重置任何已有关闭计时。
$switchRequestContextType.GetField('CurrentTick', $instanceFlags).SetValue(
    $multiRequestContext,
    105)
$accepted = [bool]$requestActivate.Invoke(
    $service,
    [object[]]@($multiRequestContext, $mainSide, 1))
Assert-True (
    $accepted -and
    $multiContexts.Main.phaseEndTick -eq 130 -and
    $multiContexts.Sub.phaseEndTick -eq 175 -and
    $multiContexts.Special.phaseEndTick -eq 145
) 'A repeated pending-target request must not reset blocker winddowns.'

$multiGetSlot = New-Delegate $resolveParameters[5].ParameterType {
    param($side, $index)
    return $multiSlots[$side.ToString() + ':' + $index]
}
$multiGetActive = New-Delegate $resolveParameters[6].ParameterType {
    param($side)
    foreach ($runtimeSlot in $multiSlots.Values) {
        if ($runtimeSlot.Side.ToString() -eq $side.ToString() -and $runtimeSlot.IsActive) {
            return $runtimeSlot
        }
    }

    return $null
}
$multiGetContext = New-Delegate $resolveParameters[3].ParameterType {
    param($side)
    return $multiContexts[$side.ToString()]
}
$multiSetContext = New-Delegate $resolveParameters[4].ParameterType {
    param($side, $context)
    $multiContexts[$side.ToString()] = $context
}
$multiResolveBlockers = New-Delegate $resolveParameters[7].ParameterType {
    param($target)
    $currentBlockers = [Activator]::CreateInstance($slotListType)
    foreach ($runtimeSlot in $multiSlots.Values) {
        if ($runtimeSlot -ne $target -and $runtimeSlot.IsActive) {
            [void]$currentBlockers.Add($runtimeSlot)
        }
    }

    return ,$currentBlockers
}
$multiPendingValid = New-Delegate $resolveParameters[8].ParameterType {
    param($slot)
    return $null -ne $slot -and $null -ne $slot.LoadedChip
}
$multiDeactivateBound = New-Delegate $resolveParameters[9].ParameterType {
    param($slot)
    $slot.SetActive($false)
}
$multiNotifyActivated = New-Delegate $resolveParameters[11].ParameterType {
    param($side, $index, $loadedChip)
    $multiActivationCommits[0]++
}
$multiNotifyDeactivated = New-Delegate $resolveParameters[12].ParameterType {
    param($side, $index, $loadedChip)
}
$multiActivationDelay = New-Delegate $resolveParameters[1].ParameterType {
    param($loadedChip)
    return 60
}
$multiDeactivationDelay = New-Delegate $resolveParameters[2].ParameterType {
    param($loadedChip)
    return $winddownByThingId[$thingIdProperty.GetValue($loadedChip, $null)]
}

function Resolve-MultiAt {
    param([int]$Tick)

    $resolveDue.Invoke(
        $null,
        [object[]]@(
            $Tick,
            $multiActivationDelay,
            $multiDeactivationDelay,
            $multiGetContext,
            $multiSetContext,
            $multiGetSlot,
            $multiGetActive,
            $multiResolveBlockers,
            $multiPendingValid,
            $multiDeactivateBound,
            $null,
            $multiNotifyActivated,
            $multiNotifyDeactivated))
}

Resolve-MultiAt 130
Assert-True (-not $blockerA.IsActive -and -not $targetB.IsActive) `
    'The target must remain off when only the same-side blocker has finished.'
Resolve-MultiAt 145
Assert-True (-not $blockerD.IsActive -and $blockerC.IsActive -and -not $targetB.IsActive) `
    'The target must continue waiting for the slowest original blocker.'

# 等待期间新出现的阻挡者必须在发现刻开始自己的完整关闭。
$blockerD.SetActive($true)
Resolve-MultiAt 150
Assert-True (
    $multiContexts.Special.phase.ToString() -eq 'Deactivating' -and
    $multiContexts.Special.phaseEndTick -eq 195
) 'A blocker appearing during waiting must begin a fresh winddown from the rescan tick.'
Resolve-MultiAt 175
Assert-True (-not $blockerC.IsActive -and $blockerD.IsActive -and -not $targetB.IsActive) `
    'The target must not warm up while the new blocker remains active.'
Resolve-MultiAt 195
Assert-True (-not $blockerD.IsActive -and -not $targetB.IsActive) `
    'The target must remain off until every blocker is formally deactivated.'
Resolve-MultiAt 196
Assert-True (
    $multiContexts.Main.phase.ToString() -eq 'Activating' -and
    $multiContexts.Main.phaseEndTick -eq 256
) 'The target must start its full warmup only after the last blocker is gone.'
Resolve-MultiAt 255
Assert-True (-not $targetB.IsActive) 'The target must not activate before full warmup expires.'
Resolve-MultiAt 256
Assert-True ($targetB.IsActive -and $multiActivationCommits[0] -eq 1) `
    'The target must activate once after the complete warmup.'

# 独立场景验证同侧覆盖、跨侧非冲突并行和跨侧冲突覆盖。
$targetEChip = New-RuntimeThing 'BDP_RuntimeTargetE'
$targetFChip = New-RuntimeThing 'BDP_RuntimeTargetF'
$targetGChip = New-RuntimeThing 'BDP_RuntimeTargetG'
$targetE = New-RuntimeSlot $mainSide 1 $targetEChip $false
$targetReplacement = New-RuntimeSlot $mainSide 2 $targetBChip $false
$targetF = New-RuntimeSlot $subSide 1 $targetFChip $false
$targetG = New-RuntimeSlot $specialSide 0 $targetGChip $false
$selectionSlots = @{
    'Main:1' = $targetE
    'Main:2' = $targetReplacement
    'Sub:1' = $targetF
    'Special:0' = $targetG
}
$selectionContexts = @{
    Main = $null
    Sub = $null
    Special = $null
}
$selectionConflicts = [bool[]]@($false)
$selectionContext = [Activator]::CreateInstance($switchRequestContextType, $true)
Set-ContextDelegate $selectionContext 'GetSlot' {
    param($side, $index)
    return $selectionSlots[$side.ToString() + ':' + $index]
}
Set-ContextDelegate $selectionContext 'GetActiveSlot' {
    param($side)
    return $null
}
Set-ContextDelegate $selectionContext 'GetActiveSlotRaw' {
    param($side)
    return $null
}
Set-ContextDelegate $selectionContext 'GetSwitchContext' {
    param($side)
    return $selectionContexts[$side.ToString()]
}
Set-ContextDelegate $selectionContext 'SetSwitchContext' {
    param($side, $context)
    $selectionContexts[$side.ToString()] = $context
}
Set-ContextDelegate $selectionContext 'FindActivationBlockers' {
    param($slot)
    return $emptySlots
}
Set-ContextDelegate $selectionContext 'HasActivationExclusionConflict' {
    param($left, $right)
    return $selectionConflicts[0]
}
Set-ContextDelegate $selectionContext 'IsPendingTargetValid' {
    param($slot)
    return $null -ne $slot -and $null -ne $slot.LoadedChip
}
Set-ContextDelegate $selectionContext 'ResolveChipActivationDelayTicks' {
    param($loadedChip)
    return 60
}
Set-ContextDelegate $selectionContext 'ResolveChipDeactivationDelayTicks' {
    param($loadedChip)
    return 30
}
Set-ContextDelegate $selectionContext 'NotifySlotActivationCommitted' {
    param($side, $index, $loadedChip)
}
Set-ContextDelegate $selectionContext 'NotifySlotDeactivated' {
    param($side, $index, $loadedChip)
}

$currentTickField = $switchRequestContextType.GetField('CurrentTick', $instanceFlags)
$currentTickField.SetValue($selectionContext, 200)
[void]$requestActivate.Invoke($service, [object[]]@($selectionContext, $mainSide, 1))
$mainOriginalEndTick = $selectionContexts.Main.phaseEndTick
$currentTickField.SetValue($selectionContext, 201)
[void]$requestActivate.Invoke($service, [object[]]@($selectionContext, $subSide, 1))
Assert-True (
    $selectionContexts.Main.targetSlotIndex -eq 1 -and
    $selectionContexts.Main.phaseEndTick -eq $mainOriginalEndTick -and
    $selectionContexts.Sub.targetSlotIndex -eq 1
) 'Non-conflicting cross-side pending requests must remain in parallel.'

$currentTickField.SetValue($selectionContext, 205)
[void]$requestActivate.Invoke($service, [object[]]@($selectionContext, $mainSide, 1))
Assert-True ($selectionContexts.Main.phaseEndTick -eq $mainOriginalEndTick) `
    'Repeating the same warmup target must not reset its timer.'
$currentTickField.SetValue($selectionContext, 206)
[void]$requestActivate.Invoke($service, [object[]]@($selectionContext, $mainSide, 2))
Assert-True (
    $selectionContexts.Main.targetSlotIndex -eq 2 -and
    $selectionContexts.Main.phaseEndTick -eq 266 -and
    $selectionContexts.Sub.targetSlotIndex -eq 1
) 'A later same-side target must replace only the old same-side pending target.'

$selectionConflicts[0] = $true
$currentTickField.SetValue($selectionContext, 207)
[void]$requestActivate.Invoke($service, [object[]]@($selectionContext, $specialSide, 0))
Assert-True (
    $null -eq $selectionContexts.Main -and
    $null -eq $selectionContexts.Sub -and
    $selectionContexts.Special.targetSlotIndex -eq 0
) 'A later cross-side conflicting target must replace all conflicting pending targets.'

Write-Output 'TriggerActivationExclusionAutoSwitchSmokeTests PASS'
