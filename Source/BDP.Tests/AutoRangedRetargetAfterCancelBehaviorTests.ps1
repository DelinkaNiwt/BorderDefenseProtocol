# 在独立进程显式加载一个游戏版本，调用真实宿主准备、原版取消和完整重置；不启动地图。
param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest -RimWorldVersion $RimWorldVersion

# 仅替代离线进程的入图/眩晕查询、配置、装备服务定位与日志展示，不替代会话准备、创建或重置。
Add-Type -TypeDefinition @'
/// <summary>独立验证进程的外部环境边界。</summary>
public static class RetargetTestBoundary
{
    /// <summary>当前样本是否全身忙碌。</summary>
    public static bool Busy;
    /// <summary>由样本注入的真实模块宿主；空值用于验证服务不可用。</summary>
    public static object RuntimeHost;
    /// <summary>避开命令环境对 Pawn 同名大小写成员的适配冲突。</summary>
    public static System.Type TypeOf(object value) { return value.GetType(); }
    /// <summary>绕开离线进程无法启动的完整模组配置。</summary>
    public static bool BusyBefore(ref bool __result) { __result = Busy; return false; }
    /// <summary>让无地图样本满足原版预热姿态的入图前提。</summary>
    public static bool SpawnedBefore(ref bool __result) { __result = true; return false; }
    /// <summary>使用真实模块宿主，仅替代人物装备服务的定位。</summary>
    public static bool RuntimeBefore(ref object __result) { __result = RuntimeHost; return false; }
    /// <summary>不运行依赖完整游戏加载流程的定义绑定初始化。</summary>
    public static bool SkipDefInitialization() { return false; }
    /// <summary>不为离线人物构造仅用于日志展示的完整姓名与种族数据。</summary>
    public static bool SkipDiagnostics() { return false; }
}
'@

function New-SampleObject([string]$name) { [Activator]::CreateInstance($RangedCore.GetType($name, $true)) }
function New-Uninitialized([type]$type) { [Runtime.Serialization.FormatterServices]::GetUninitializedObject($type) }
function Get-SampleValue($obj,[string]$name) {
    $field = [HarmonyLib.AccessTools]::Field([RetargetTestBoundary]::TypeOf($obj), $name)
    if ($field) { return ,$field.GetValue($obj) }
    return ,([HarmonyLib.AccessTools]::Property([RetargetTestBoundary]::TypeOf($obj), $name).GetValue($obj, $null))
}
function Set-SampleValue($obj,[string]$name,$value) {
    $field = [HarmonyLib.AccessTools]::Field([RetargetTestBoundary]::TypeOf($obj), $name)
    if ($field) { $field.SetValue($obj, $value) }
    else { [HarmonyLib.AccessTools]::Property([RetargetTestBoundary]::TypeOf($obj), $name).SetValue($obj, $value, $null) }
}
function Invoke-Sample($obj,[string]$name,[object[]]$arguments=@()) {
    [HarmonyLib.AccessTools]::Method([RetargetTestBoundary]::TypeOf($obj), $name).Invoke($obj, $arguments)
}
function Prepare-Sample($sample,$target) { Invoke-Sample $sample.Verb 'TryPrepareCastSession' @($target) }

# 实体身份相同与格子相同是两种独立合法输入；导航锚点故意与语义目标不同。
$oldEntity = New-Uninitialized ([Verse.Pawn])
$newEntity = New-Uninitialized ([Verse.Pawn])
$oldTarget = [Verse.LocalTargetInfo]::new($oldEntity)
$newTarget = [Verse.LocalTargetInfo]::new($newEntity)
$anchor = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(10,0,10))
$newCell = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(20,0,20))
$runtimeType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedAttackModuleRuntimeHost', $true)
$runtime = $runtimeType.GetConstructors()[0].Invoke([object[]]@($null))

# 真实绑定、投影和模块会话组成最小有效宿主；无需替代被测分支或正式重置。
function New-RetargetSample($semanticTarget) {
    [RetargetTestBoundary]::Busy = $false
    [RetargetTestBoundary]::RuntimeHost = $runtime
    $pawn = New-Uninitialized ([Verse.Pawn])
    Set-SampleValue $pawn 'thingIDNumber' 344
    $pawnDef = New-Uninitialized ([Verse.ThingDef])
    Set-SampleValue $pawnDef 'defName' 'SamplePawn'
    Set-SampleValue $pawn 'def' $pawnDef
    Set-SampleValue $pawn 'jobs' (New-Uninitialized ([Verse.AI.Pawn_JobTracker]))
    $stances = New-Uninitialized ([Verse.Pawn_StanceTracker])
    Set-SampleValue $stances 'curStance' ([Verse.Stance_Mobile]::new())
    Set-SampleValue $stances 'pawn' $pawn
    Set-SampleValue $pawn 'stances' $stances
    $verb = [BDP.Core.Verbs.BdpVerb_FormalHostShoot]::new()
    Set-SampleValue $verb 'caster' $pawn
    $owner = New-Uninitialized ($RangedCore.GetType('BDP.Core.Trigger.CompTriggerBody', $true))
    $managerType = $RangedCore.GetType('BDP.Core.VerbHosting.TriggerBodyVerbHostManager', $true)
    $manager = $managerType.GetConstructors()[0].Invoke(@($owner))
    Set-SampleValue $owner 'verbHostManager' $manager
    Set-SampleValue $verb 'owner' $owner
    $slot = [Enum]::Parse($RangedCore.GetType('BDP.Core.VerbHosting.BdpFormalVerbHostSlot'), 'SpecialPrimary')
    Set-SampleValue $verb 'slot' $slot
    $state = New-SampleObject 'BDP.Core.VerbHosting.BdpFormalVerbBindingState'
    Set-SampleValue $state 'IsAvailable' $true
    Set-SampleValue $state 'ResultId' 'sample.ranged'
    Set-SampleValue $state 'WeaponMode' ([Enum]::Parse($RangedCore.GetType('BDP.Core.Expressions.WeaponExpressionMode'), 'Ranged'))
    $binding = New-SampleObject 'BDP.Core.VerbHosting.BdpFormalVerbBinding'
    Set-SampleValue $binding 'State' $state
    Set-SampleValue $binding 'RangedVerb' $verb
    (Get-SampleValue $manager 'bindings').Add($slot, $binding)
    $result = New-SampleObject 'BDP.Core.Expressions.FormalExpressionResult'
    Set-SampleValue $result 'Id' 'sample.ranged'
    Set-SampleValue $result 'IsAvailable' $true
    $projection = New-SampleObject 'BDP.Core.Trigger.Runtime.TriggerCombatProjectionState'
    Set-SampleValue $projection 'ProjectionVersion' 11
    $indexType = [System.Collections.Generic.Dictionary``2].MakeGenericType([string], $result.GetType())
    $index = [Activator]::CreateInstance($indexType)
    $index.Add('sample.ranged', $result)
    Set-SampleValue $projection 'ResultIndex' $index
    $coordinator = New-Uninitialized ($RangedCore.GetType('BDP.Core.Trigger.Runtime.TriggerRuntimeCoordinator', $true))
    Set-SampleValue $coordinator 'currentCombatProjection' $projection
    Set-SampleValue $owner 'runtimeCoordinator' $coordinator
    $token = New-SampleObject 'BDP.Core.AttackExecution.AttackSessionToken'
    foreach ($entry in @{AttackInstanceId='old.attack';ResultId='sample.ranged';ProjectionVersion=11;OwnerPawnThingId='sample.pawn'}.GetEnumerator()) {
        Set-SampleValue $token $entry.Key $entry.Value
    }
    Set-SampleValue $verb 'hostSessionToken' $token
    Set-SampleValue $verb 'sessionTarget' $semanticTarget
    $module = New-SampleObject 'BDP.Core.AttackExecution.RangedAttackModuleSession'
    $context = New-SampleObject 'BDP.Core.AttackExecution.AttackContext'
    $confirmed = [BDP.Core.AttackExecution.ConfirmedTargetSnapshot]::new()
    $confirmed.SemanticTarget = $semanticTarget
    $confirmed.NavigationTarget = $anchor
    Invoke-Sample $context 'Set' @('confirmed.target',$confirmed)
    Set-SampleValue $module 'AttackContext' $context
    Set-SampleValue $verb 'hostModuleSession' $module
    $snapshot = Invoke-Sample $context 'ToSnapshot'
    Set-SampleValue $verb 'hostAttackContextSnapshot' $snapshot
    $windowType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedProtocol.Model.RangedVerbEmissionWindowPlan')
    $windows = [Array]::CreateInstance($windowType, 4)
    0..3 | ForEach-Object { $windows.SetValue([Activator]::CreateInstance($windowType), $_) }
    $plan = New-SampleObject 'BDP.Core.AttackExecution.RangedProtocol.Model.RangedVerbEmissionPlan'
    Set-SampleValue $plan 'Windows' $windows
    $cursor = Get-SampleValue $verb 'emissionCursor'
    Invoke-Sample $cursor 'BindVerbEmissionPlan' @($plan)
    Invoke-Sample (Get-SampleValue $verb 'roundState') 'Restore' @([single]5,[single]5,$true)
    [pscustomobject]@{Verb=$verb;Pawn=$pawn;Stances=$stances;State=$state;Cursor=$cursor;Plan=$plan;Token=$token;Module=$module;Snapshot=$snapshot}
}

$patch = [HarmonyLib.Harmony]::new('bdp.tests.auto_ranged_retarget_after_cancel')
try {
    $patch.Patch([Verse.Pawn_StanceTracker].GetProperty('FullBodyBusy').GetMethod, [HarmonyLib.HarmonyMethod]::new([RetargetTestBoundary].GetMethod('BusyBefore'))) | Out-Null
    $patch.Patch([HarmonyLib.AccessTools]::PropertyGetter([Verse.Thing],'Spawned'), [HarmonyLib.HarmonyMethod]::new([RetargetTestBoundary].GetMethod('SpawnedBefore'))) | Out-Null
    $surface = $RangedCore.GetType('BDP.Core.AttackExecution.AttackExecutionSurfaceAccess')
    $patch.Patch([HarmonyLib.AccessTools]::Method($surface,'ResolveRangedModuleRuntimeHost'), [HarmonyLib.HarmonyMethod]::new([RetargetTestBoundary].GetMethod('RuntimeBefore'))) | Out-Null
    $patch.Patch([HarmonyLib.AccessTools]::Method([RimWorld.DefOfHelper],'EnsureInitializedInCtor'), [HarmonyLib.HarmonyMethod]::new([RetargetTestBoundary].GetMethod('SkipDefInitialization'))) | Out-Null
    $diagnostics = $RangedCore.GetType('BDP.Core.AttackExecution.AttackExecutionDiagnostics')
    $patch.Patch([HarmonyLib.AccessTools]::Method($diagnostics,'LogVerbSessionCleared'), [HarmonyLib.HarmonyMethod]::new([RetargetTestBoundary].GetMethod('SkipDiagnostics'))) | Out-Null

    $sample = New-RetargetSample $oldTarget
    $oldDriver = New-Uninitialized ($RangedCore.GetType('BDP.Core.AttackExecution.JobDriver_BdpRangedAttackExecution'))
    Invoke-Sample $oldDriver 'CaptureCleanupOwnedSessionIfNeeded' @($sample.Verb)
    Assert-Ranged (Invoke-Sample $oldDriver 'ShouldCleanupCurrentVerbSession' @($sample.Verb)) '旧任务未锁存自己的会话，清理归属样本无效。'
    $warmup = New-Uninitialized ([Verse.Stance_Warmup])
    Set-SampleValue $warmup 'verb' $sample.Verb
    Set-SampleValue $sample.Stances 'curStance' $warmup
    Assert-Ranged (Get-SampleValue $sample.Verb 'WarmingUp') '原版未识别取消前的预热姿态，样本无效。'
    Invoke-Sample $sample.Stances 'CancelBusyStanceSoft'
    Assert-Ranged (-not (Get-SampleValue $sample.Verb 'WarmingUp')) '原版软取消未结束预热，样本无效。'
    Assert-Ranged (Invoke-Sample $sample.Cursor 'HasPendingEmissionPlan') '原版取消后没有保留计划，未触发故障前提。'
    Assert-Ranged (Prepare-Sample $sample $newTarget) '取消瞄准后，合法新目标仍被旧待发计划永久阻断。'
    Assert-Ranged (-not [object]::ReferenceEquals((Get-SampleValue $sample.Verb 'HostSessionToken'),$sample.Token)) '换目标复用了旧令牌。'
    Assert-Ranged (-not (Invoke-Sample $oldDriver 'ShouldCleanupCurrentVerbSession' @($sample.Verb))) '旧任务仍可清理换目标后的新会话。'
    Assert-Ranged ($null -eq (Get-SampleValue $sample.Verb 'HostModuleSession') -and $null -eq (Get-SampleValue $sample.Verb 'HostAttackContextSnapshot')) '换目标仍保留旧模块或确认快照。'
    Assert-Ranged (-not (Invoke-Sample $sample.Cursor 'HasPendingEmissionPlan')) '换目标未关闭旧发射游标。'
    Assert-Ranged (-not (Get-SampleValue (Get-SampleValue $sample.Verb 'roundState') 'HasCommittedRoundTrion')) '新目标继承旧轮已扣费标记。'
    $staged = Invoke-Sample $sample.Verb 'ResolveEntryModuleSession'
    Assert-Ranged ($null -ne $staged -and -not [object]::ReferenceEquals($staged,$sample.Module)) '未建立新的模块会话。'

    foreach ($target in @($oldTarget,$anchor)) {
        $same = New-RetargetSample $target
        Assert-Ranged (Prepare-Sample $same $target) '同目标预备攻击被拒绝。'
        Assert-Ranged ([object]::ReferenceEquals((Get-SampleValue $same.Verb 'HostAttackContextSnapshot'),$same.Snapshot)) '同目标确认路径被清空。'
        Assert-Ranged ([object]::ReferenceEquals((Get-SampleValue $same.Verb 'HostModuleSession'),$same.Module)) '同目标模块会话被替换。'
        Assert-Ranged (Get-SampleValue (Get-SampleValue $same.Verb 'roundState') 'HasCommittedRoundTrion') '同目标准备提前清掉了当前轮资源标记。'
    }
    $cell = New-RetargetSample $anchor
    Assert-Ranged (Prepare-Sample $cell $newCell) '不同格子目标仍被残留计划阻断。'
    foreach ($mode in @('burst','warmup','busy','binding','service')) {
        $blocked = New-RetargetSample $oldTarget
        if ($mode -eq 'burst') {
            Set-SampleValue $blocked.Verb 'state' ([Verse.VerbState]::Bursting)
            Set-SampleValue $blocked.Verb 'burstShotsLeft' 3
        }
        if ($mode -eq 'warmup') {
            $stance = New-Uninitialized ([Verse.Stance_Warmup])
            Set-SampleValue $stance 'verb' $blocked.Verb
            Set-SampleValue $blocked.Stances 'curStance' $stance
        }
        if ($mode -eq 'busy') { [RetargetTestBoundary]::Busy = $true }
        if ($mode -eq 'binding') { Set-SampleValue $blocked.State 'IsAvailable' $false }
        if ($mode -eq 'service') { [RetargetTestBoundary]::RuntimeHost = $null }
        $accepted = Prepare-Sample $blocked $newTarget
        Assert-Ranged (-not $accepted) "受保护或前置失败状态被覆盖：$mode，state=$(Get-SampleValue $blocked.Verb 'state')，result=$accepted"
        Assert-Ranged ([object]::ReferenceEquals((Get-SampleValue $blocked.Verb 'HostSessionToken'),$blocked.Token)) "失败起手清理了旧令牌：$mode"
        Assert-Ranged ([object]::ReferenceEquals((Get-SampleValue $blocked.Verb 'HostAttackContextSnapshot'),$blocked.Snapshot)) "失败起手破坏了旧路径：$mode"
    }
    $manual = New-RetargetSample $oldTarget
    $job = New-Uninitialized ([Verse.AI.Job])
    $jobDef = New-Uninitialized ([Verse.AI.Job].GetField('def').FieldType)
    Set-SampleValue $jobDef 'defName' 'BDP_RangedAttackExecution'
    $jobDatabase = [Verse.DefDatabase``1].MakeGenericType($jobDef.GetType())
    $jobDatabase.GetMethod('Add',[type[]]@($jobDef.GetType())).Invoke($null,@($jobDef))
    Set-SampleValue $job 'def' $jobDef
    Set-SampleValue (Get-SampleValue $manual.Pawn 'jobs') 'curJob' $job
    Assert-Ranged (Prepare-Sample $manual $newTarget) '手动正式任务未保留原专用入口。'
    Assert-Ranged ([object]::ReferenceEquals((Get-SampleValue $manual.Verb 'HostSessionToken'),$manual.Token)) '自动换目标逻辑覆盖了手动任务会话。'
    Write-Output "$RimWorldVersion：取消后实体/格子换目标、旧数据及资源标记清理、同目标确认输入、预热/连发/忙碌、前置失败及手动任务归属均通过。"
}
finally { $patch.UnpatchAll('bdp.tests.auto_ranged_retarget_after_cancel') }
