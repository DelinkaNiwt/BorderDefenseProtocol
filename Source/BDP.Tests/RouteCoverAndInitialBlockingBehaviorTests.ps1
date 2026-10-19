param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest $RimWorldVersion

$staticHidden = [Reflection.BindingFlags]'Static,NonPublic'
$planType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedProtocol.Model.ProjectileInitPlan',$true)
$serviceType = $RangedCore.GetType('BDP.Core.Projectiles.BdpProjectileLaunchService',$true)
$freeze = $serviceType.GetMethod('FreezeCoverLaunchResolution',$staticHidden)
Assert-Ranged ($null -ne $freeze) '掩体裁定仍未区分首段导航与末端命中结果。'
$apply = [BDP.Content.RangedModules.RoutePath.RouteSegmentResolver].GetMethod('ApplyLaunchResolution')
Assert-Ranged ($null -ne $apply) '路线缺少消费冻结末端结果的入口。'

# 两个锚点与原人物目标；灌木只应替换最终段，不能改动锚点或把本发恢复为命中人物。
$final = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(20,0,10))
$first = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(2,0,2))
$cover = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(19,0,10))
$coverPoint = [UnityEngine.Vector3]::new(19.5,0,10.5)
$plan = [Activator]::CreateInstance($planType,$true)
$usedTarget = $freeze.Invoke($null,[object[]]@($plan,$first,$final,$cover,$coverPoint,$null))
$resolution = $planType.GetProperty('LaunchResolution').GetValue($plan)
Assert-Ranged ($usedTarget.Cell -eq $first.Cell -and $resolution.ActualLaunchTarget.Cell -eq $first.Cell) '掩体分支跳过首个锚点直接朝掩体发射。'
Assert-Ranged (-not $resolution.CanHitIntendedTarget -and $resolution.ContinuationMode.ToString() -eq 'IntendedRoute') '允许沿路径飞行不能恢复原目标命中资格。'
Assert-Ranged ($resolution.TerminalTarget.Cell -eq $cover.Cell -and ($resolution.TerminalDestination-$coverPoint).sqrMagnitude -lt 0.000001) '冻结结果丢失所选掩体或真实末端坐标。'

$route = [BDP.Content.RangedModules.RoutePath.RoutePathContext]::new()
$route.HasFinalTarget = $true
$route.FinalTarget = $final
$route.Anchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell($first.Cell))
$route.Anchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell([Verse.IntVec3]::new(12,0,2)))
$route.CurrentLegIndex = 0
$route.FinalSpreadRadius = [single]0.3
$route.IntermediateSpreadRadius = [single]0
$apply.Invoke($null,[object[]]@($route,$resolution))
$resolver = [BDP.Content.RangedModules.RoutePath.RouteSegmentResolver]
$arguments = [object[]]@($route,[UnityEngine.Vector3]::new(2.5,0,2.5),$null,'cover-attack','viper',0,$null,$null)
Assert-Ranged ([bool]$resolver.GetMethod('TryResolveContinuation').Invoke($null,$arguments)) '掩体结果没有保留中间续段。'
Assert-Ranged ($arguments[6].Cell -eq [Verse.IntVec3]::new(12,0,2)) '末端覆盖错误地改变中间锚点。'
Assert-Ranged ([BDP.Content.RangedModules.RoutePath.RouteSegmentResolver]::TryAdvanceLeg($route)) '中间锚点无法推进。'
$arguments[1] = [UnityEngine.Vector3]::new(12.5,0,2.5)
Assert-Ranged ([bool]$resolver.GetMethod('TryResolveContinuation').Invoke($null,$arguments)) '掩体结果没有最终续段。'
Assert-Ranged ($arguments[6].Cell -eq $cover.Cell -and ($arguments[7].End-$coverPoint).sqrMagnitude -lt 0.000001) '最终段没有准确抵达被冻结的掩体，或被再次散布推开。'
Assert-Ranged ([BDP.Content.RangedModules.RoutePath.RouteSegmentResolver]::TryAdvanceLeg($route)) '掩体终段无法推进。'
Assert-Ranged (-not [bool]$resolver.GetMethod('TryResolveContinuation').Invoke($null,$arguments)) '掩体命中后仍试图回接原人物目标。'

# 无中间导航时保持原版直射掩体，不能先飞到人物处再回头。
$directPlan = [Activator]::CreateInstance($planType,$true)
$directTarget = $freeze.Invoke($null,[object[]]@($directPlan,$final,$final,$cover,$coverPoint,$null))
Assert-Ranged ($directTarget.Cell -eq $cover.Cell) '无锚点射击没有直接采用掩体落点。'
Assert-Ranged ($planType.GetProperty('LaunchResolution').GetValue($directPlan).ContinuationMode.ToString() -eq 'None') '无中间导航的掩体结果错误地开放追踪续飞。'

# 普通命中不覆盖路线；随机偏射继续只应用既有冻结位移。
$normal = [BDP.Core.AttackExecution.RangedProtocol.Model.ProjectileLaunchResolution]::new()
$untouched = [BDP.Content.RangedModules.RoutePath.RoutePathContext]::new()
$untouched.HasFinalTarget = $true
$untouched.FinalTarget = $final
$apply.Invoke($null,[object[]]@($untouched,$normal))
Assert-Ranged ($untouched.FinalTarget.Cell -eq $final.Cell -and $untouched.FinalSpreadRadius -eq [single]0.3) '普通路线受到掩体修复影响。'
$normal.ContinuationMode = [BDP.Core.AttackExecution.RangedProtocol.Model.ProjectileLaunchContinuationMode]::WildMissOffsetRoute
$normal.WildMissOffset = [UnityEngine.Vector3]::new(3,0,1)
$apply.Invoke($null,[object[]]@($untouched,$normal))
Assert-Ranged ($untouched.HasWildMissOffset -and ($untouched.WildMissOffset-$normal.WildMissOffset).sqrMagnitude -lt 0.000001) '随机偏射的冻结位移丢失。'

# 实际发射与路线模块必须使用上述生产入口；首段与续段绑定共用阻挡准入。
$source = Join-Path $RangedModRoot 'Source'
$verbText = Get-Content -LiteralPath (Join-Path $source 'BDP/Core/Verbs/BdpVerb_Shoot.cs') -Raw
$moduleText = Get-Content -LiteralPath (Join-Path $source 'BDP.Content/RangedModules/RoutePath/RoutePathModule.cs') -Raw
$projectileText = Get-Content -LiteralPath (Join-Path $source 'BDP/Core/Projectiles/BdpProjectile.cs') -Raw
Assert-Ranged ($verbText -match 'BdpProjectileLaunchService\.FreezeCoverLaunchResolution' -and $moduleText -match 'RouteSegmentResolver\.ApplyLaunchResolution') '正式发射或路线未接入已验证的冻结结果入口。'
$launch = [regex]::Match($projectileText,'public override void Launch\([\s\S]*?NotifyVisualLaunch\(\);').Value
Assert-Ranged ($launch -match 'ResolveBindingFlightPathSnapshot') '首段仍然直接绑定整条路径，近墙只能等到段末补查。'
Assert-Ranged ($projectileText -match 'ResolveBindingFlightPathSnapshot\(nextFlightPathSnapshot,\s*true\)') '续段没有复用相同阻挡规则或丢失首格例外。'

# 独立C#夹具避开游戏类型仅大小写不同成员引起的PowerShell适配冲突。
$refs = @($RangedCore.Location,[Verse.Thing].Assembly.Location,[UnityEngine.Vector3].Assembly.Location) + @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -Path (Join-Path $PSScriptRoot 'TestSupport/ProjectileBlockingProbe.cs') -ReferencedAssemblies $refs
[ProjectileBlockingProbe]::Run()

# 真实路线模块必须独占被冻结的掩体末段，并在末端到达时明确停止，禁止追踪模块回接原人物。
$hidden = [Reflection.BindingFlags]'Instance,NonPublic'
$module = [BDP.Content.RangedModules.RoutePath.RoutePathModule]::new()
$moduleConfig = [BDP.Content.RangedModules.RoutePath.RoutePathConfig]::new()
$moduleConfig.ReleaseControlAfterLastAnchor = $true
$runtimeContext = [BDP.Core.AttackExecution.RangedAttackModuleRuntimeContext]::new()
$runtimeContext.GetType().GetProperty('Config').SetValue($runtimeContext,$moduleConfig)
([BDP.Core.AttackExecution.IRangedAttackModuleRuntime]$module).Initialize($runtimeContext)
$state = [BDP.Content.RangedModules.RoutePath.RoutePathState]::new()
$state.PathSnapshot = $route.CloneTyped()
$state.PathSnapshot.CurrentLegIndex = 0
$state.PathSnapshot.Anchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell([Verse.IntVec3]::new(14,0,2)))
$keyType = $RangedCore.GetType('BDP.Core.AttackExecution.AttackContextKeys',$true)
$key = $keyType.GetMethod('GetModulePrivateKey',$staticHidden).Invoke($null,[object[]]@(0))
$nodes = [Collections.Generic.Dictionary[string,BDP.Core.AttackExecution.IAttackContextNode]]::new()
$nodes.Add($key,$state)
$snapshot = [BDP.Core.AttackExecution.AttackContextSnapshot].GetMethod('Create',$staticHidden).Invoke($null,[object[]]@($nodes))
$modulePlan = [Activator]::CreateInstance($planType,$true)
$planType.GetProperty('AttackContextSnapshot').SetValue($modulePlan,$snapshot)
$planType.GetProperty('LaunchResolution').SetValue($modulePlan,$resolution)
$sessionType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedAttackModuleSession',$true)
$session = [Activator]::CreateInstance($sessionType,$true)
$slot = [BDP.Core.AttackExecution.RangedModules.Runtime.RangedAttackModuleSlot]::new()
$slot.Runtime = $module
$slot.MountIndex = 0
$slots = [Collections.Generic.List[BDP.Core.AttackExecution.RangedModules.Runtime.RangedAttackModuleSlot]]::new()
$slots.Add($slot)
$sessionType.GetProperty('Slots').SetValue($session,$slots)
$contextType = [BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalStageContext]
$constructor = $contextType.GetConstructors($hidden)[0]
$arrivalContext = $constructor.Invoke([object[]]@([ProjectileBlockingProbe]::CreateArrivalProjectile(),$modulePlan,$null,$module,$session,$false))
$contribute = $module.GetType().GetMethod('BDP.Core.Projectiles.RangedFlightProtocol.Arrival.IArrivalStageModule.Contribute',$hidden)
$contribution = [BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalContribution]::new()
$contribute.Invoke($module,[object[]]@($arrivalContext,$contribution))
Assert-Ranged ($contribution.ExclusiveFlightControl -and $contribution.OverrideContinueFlight) '掩体路线未独占剩余锚点，追踪模块可覆盖路线。'
$frozenState = $snapshot.GetType().GetMethod('Get').MakeGenericMethod([BDP.Content.RangedModules.RoutePath.RoutePathState]).Invoke($snapshot,[object[]]@($key))
$frozenState.PathSnapshot.CurrentLegIndex = $frozenState.PathSnapshot.Anchors.Count
$terminalContribution = [BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalContribution]::new()
$contribute.Invoke($module,[object[]]@($arrivalContext,$terminalContribution))
Assert-Ranged ($terminalContribution.ExclusiveFlightControl -and $terminalContribution.HasOverrideContinueFlight -and -not $terminalContribution.OverrideContinueFlight) '到达掩体后没有明确停止，追踪模块仍可继续回接原人物。'

# 自动左右分流重建本发快照后，仍须应用同一冻结末端；奇数发选择右侧锚点。
$frozenState.ConfirmedSnapshot.PathSource = [BDP.Content.RangedModules.RoutePath.RoutePathSource]::Auto
$frozenState.ConfirmedSnapshot.HasFinalTarget = $true
$frozenState.ConfirmedSnapshot.FinalTarget = $final
foreach ($cell in @([Verse.IntVec3]::new(2,0,2),[Verse.IntVec3]::new(12,0,2))) {
    $frozenState.ConfirmedSnapshot.AutoLeftAnchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell($cell))
}
foreach ($cell in @([Verse.IntVec3]::new(2,0,18),[Verse.IntVec3]::new(12,0,18))) {
    $frozenState.ConfirmedSnapshot.AutoRightAnchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell($cell))
}
$frozenState.PathSnapshot.AssignedEmitIndex = -1
$planType.GetProperty('EmitSequence').SetValue($modulePlan,1)
$autoContext = $constructor.Invoke([object[]]@([ProjectileBlockingProbe]::CreateArrivalProjectile(),$modulePlan,$null,$module,$session,$false))
$autoContribution = [BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalContribution]::new()
$contribute.Invoke($module,[object[]]@($autoContext,$autoContribution))
Assert-Ranged ($autoContribution.NextTarget.Cell -eq [Verse.IntVec3]::new(12,0,18) -and $frozenState.PathSnapshot.FinalTarget.Cell -eq $cover.Cell) '自动分流重建快照后丢失本发锚点或冻结掩体结果。'

# 导航切换为掩体后，仍须保留被禁止命中的原意图目标。
$intended = $resolution.ResolveIntendedTarget($final,$cover)
Assert-Ranged ($intended.Cell -eq $final.Cell) '切换到掩体后原人物被重分类为可命中的非目标。'
Write-Output "通过：$RimWorldVersion 掩体结果保留锚点、末段抵达掩体、原目标资格冻结、直射与偏射边界、首续段统一准入。"
