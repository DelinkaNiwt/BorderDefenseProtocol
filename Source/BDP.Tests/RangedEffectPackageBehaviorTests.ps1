param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest $RimWorldVersion
$baseType = $RangedContent.GetType('BDP.Content.RangedModules.Debuff.Effects.RangedDebuffEffectDef')
Assert-Ranged ($null -ne $baseType) '缺少独立效果包定义入口；崩解与负重仍没有各自的效果包。'
Assert-Ranged (-not $baseType.IsAbstract) '原版只为首个非抽象Def基类建立数据库，共同效果入口不能是抽象类。'
$configType = $RangedContent.GetType('BDP.Content.RangedModules.Debuff.RangedDebuffConfig', $true)
$config = [Activator]::CreateInstance($configType)
$effectField = $configType.GetField('EffectDef')
Assert-Ranged ($null -ne $effectField) '远程减益宿主必须引用效果包。'
Assert-Ranged ($null -eq $configType.GetField('HitPartSelectionMode')) '减益宿主不应保留部位选择配置。'
foreach ($name in @('BodyPartDisintegrationEffectDef','LeadWeightEffectDef')) {
    $type = $RangedContent.GetType("BDP.Content.RangedModules.Debuff.Effects.$name", $true)
    Assert-Ranged ($type.IsSubclassOf($baseType)) "$name 必须是独立效果包。"
    $effect = [Activator]::CreateInstance($type)
    $effectField.SetValue($config, $effect)
    $clone = $configType.GetMethod('CloneTyped').Invoke($config, @())
    Assert-Ranged ([object]::ReferenceEquals($effectField.GetValue($clone), $effect)) '克隆保留共享的无状态效果Def引用。'
    Assert-Ranged (-not $type.GetMethod('Apply').Invoke($effect, @($null))) '没有命中上下文不能产生效果。'
}
$collapse = [Activator]::CreateInstance($RangedContent.GetType('BDP.Content.RangedModules.Debuff.Effects.BodyPartDisintegrationEffectDef', $true))
$collapse.hitsToDisintegrate = 4
$increment = [single]$collapse.GetType().GetMethod('SeverityIncrement').Invoke($collapse, @())
Assert-Ranged ([Math]::Abs($increment * 4 - 1) -lt 0.000001) '四次等份必须达到满值。'
$plan = [Activator]::CreateInstance($RangedCore.GetType('BDP.Core.Projectiles.RangedFlightProtocol.Model.ExtraEffectPlan', $true))
$modeProperty = $plan.GetType().GetProperty('PartTargetMode')
Assert-Ranged ($null -ne $modeProperty) '效果计划必须以中性作用域声明部位需求。'
$modeProperty.SetValue($plan, [Enum]::Parse($modeProperty.PropertyType, 'ImpactPart'))
$clonePlan = $plan.CloneForTarget($null, [Verse.IntVec3]::Zero)
Assert-Ranged ($clonePlan.PartTargetMode.ToString() -eq 'ImpactPart') '逐目标克隆不能丢失部位作用域。'
$selectionType = $RangedContent.GetType('BDP.Content.RangedModules.VitalHit.VitalHitConfig', $true)
$selection = [Activator]::CreateInstance($selectionType)
$selection.ExactBodyPartOccurrenceIndex = 2
$selection.SelectionMode = [Enum]::Parse($selection.SelectionMode.GetType(), 'ExactDefOccurrence')
$selectionClone = $selection.CloneTyped()
Assert-Ranged ($selectionClone.ExactBodyPartOccurrenceIndex -eq 2 -and $selectionClone.SelectionMode.ToString() -eq 'ExactDefOccurrence') '精确选择参数必须由独立部位模块克隆保存。'
# 只测试事件数据流，不初始化Unity贴图、地图或人物健康系统。
$refs = @($RangedCore.Location, $RangedContent.Location, [Verse.Thing].Assembly.Location, [UnityEngine.Vector3].Assembly.Location) + @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -Path (Join-Path $PSScriptRoot 'TestSupport/RangedEffectProbe.cs') -ReferencedAssemblies $refs
$probe = [RangedEffectProbe]::new()
[void][BDP.Core.Projectiles.RangedFlightProtocol.Effects.ExtraEffectPlanExecutorRegistry]::TryRegister($probe)
$event = [BDP.Core.Projectiles.RangedFlightProtocol.Impact.AttackTargetEvent]::new()
$event.TargetThing = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Verse.Thing])
$event.Map = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Verse.Map])
$event.HitPart = [Verse.BodyPartRecord]::new()
$plan.EffectKind = $probe.EffectKind
$plan.TargetScope = [BDP.Core.Projectiles.RangedFlightProtocol.Model.ExtraEffectTargetScope]::AttackTargetEvents
$event.ExtraEffects = [Collections.Generic.List[BDP.Core.Projectiles.RangedFlightProtocol.Model.ExtraEffectPlan]]::new()
$event.ExtraEffects.Add($plan)
[void][BDP.Core.Projectiles.RangedFlightProtocol.Impact.AttackTargetEventDispatcher]::Dispatch($event)
Assert-Ranged ([object]::ReferenceEquals($probe.LastPart, $event.HitPart) -and [object]::ReferenceEquals($probe.LastPlanPart, $event.HitPart)) '直接命中事实必须同时进入效果上下文与逐目标计划。'
$event.Source = [BDP.Core.Projectiles.RangedFlightProtocol.Impact.AttackTargetEventSource]::ProducedTarget
[void][BDP.Core.Projectiles.RangedFlightProtocol.Impact.AttackTargetEventDispatcher]::Dispatch($event)
Assert-Ranged ($null -eq $probe.LastPart -and $null -eq $probe.LastPlanPart) '范围生产者的逐目标事件不能继承直接命中部位。'
$packageProbe = [RangedPackageProbe]::new()
$packageProbe.defName = 'BDP_Test_RangedPackageProbe'
[Verse.DefDatabase[BDP.Content.RangedModules.Debuff.Effects.RangedDebuffEffectDef]]::Add($packageProbe)
$adapter = [BDP.Content.RangedModules.Debuff.Effects.RangedDebuffEffectExecutor]::new()
$context = [BDP.Core.Projectiles.RangedFlightProtocol.Effects.ExtraEffectExecutionContext]::new()
$context.TargetThing = $event.TargetThing
$plan.Parameters['effectDef'] = $packageProbe.defName
$plan.Parameters['targetFilter'] = 'PawnsOnly'
Assert-Ranged (-not $adapter.TryExecute($plan, $context) -and $packageProbe.Calls -eq 0) '只限人物的过滤必须在效果包调用前生效。'
$plan.Parameters['targetFilter'] = 'AnyThing'
Assert-Ranged ($adapter.TryExecute($plan, $context) -and $packageProbe.Calls -eq 1) '通用适配器必须从共同原版数据库解析并调用独立效果包。'
Write-Output "通过：$RimWorldVersion 效果包契约、克隆、四次叠层参数、真实派发器部位传递与范围隔离；健康效果另需游戏内验证。"
