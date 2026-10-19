param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest $RimWorldVersion
$contributionType = $RangedCore.GetType('BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalContribution', $true)
Assert-Ranged ($null -ne $contributionType.GetProperty('ExclusiveFlightControl')) '缺少段级独占贡献，后序追踪仍能覆盖路线。'
$contextType = $RangedCore.GetType('BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalStageContext', $true)
Assert-Ranged ($null -ne $contextType.GetProperty('PreviousExclusiveFlightControl')) '后序模块必须知道本段已由前序控制。'
$routeType = $RangedContent.GetType('BDP.Content.RangedModules.RoutePath.RoutePathConfig', $true)
$config = [Activator]::CreateInstance($routeType)
foreach ($fieldName in @('ReleaseControlAfterLastAnchor','AllowUnroutedFinalTarget')) {
    $field = $routeType.GetField($fieldName)
    Assert-Ranged ($null -ne $field) "缺少路线交接配置：$fieldName"
    Assert-Ranged (-not $field.GetValue($config)) '新路线交接行为必须默认关闭，保持毒蛇现有语义。'
    $field.SetValue($config, $true)
}
$clone = $config.CloneTyped()
Assert-Ranged ($clone.ReleaseControlAfterLastAnchor -and $clone.AllowUnroutedFinalTarget) '路线配置克隆必须保留交接行为。'
# 虚拟模块仅制造输入；调用真实合并服务验证控制顺序和无关贡献。
$refs = @($RangedCore.Location, [Verse.Thing].Assembly.Location, [UnityEngine.Vector3].Assembly.Location) + @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -Path (Join-Path $PSScriptRoot 'TestSupport/RangedFlightProbe.cs') -ReferencedAssemblies $refs
$first = [RangedFlightProbe]::new()
$first.Value = 1
$second = [RangedFlightProbe]::new()
$second.Value = 0.5
foreach ($exclusive in @($true, $false)) {
    $first.Exclusive = $exclusive
    $arrivalModules = [Collections.Generic.List[BDP.Core.Projectiles.RangedFlightProtocol.Arrival.IArrivalStageModule]]::new()
    $arrivalModules.Add($first); $arrivalModules.Add($second)
    $serviceType = $RangedCore.GetType('BDP.Core.Projectiles.RangedFlightProtocol.Arrival.ArrivalStageService', $true)
    $service = [Activator]::CreateInstance($serviceType, [object[]]@($arrivalModules, $null))
    $result = $serviceType.GetMethod('Execute').Invoke($service, [object[]]@($null,$null,$null,$null))
    $value = $result.GetType().GetProperty('NextDestination').GetValue($result).x
    Assert-Ranged ($value -eq $(if ($exclusive) {1} else {0.5})) '到达阶段没有遵守独占顺序或破坏原有后序覆盖。'
    Assert-Ranged ($second.ObservedPrevious -eq $exclusive) '到达上下文没有正确传递前序控制权。'
    Assert-Ranged ($result.GetType().GetProperty('Tags').GetValue($result).Count -eq 2) '独占不能吞掉后序非导航标签。'

    $initModules = [Collections.Generic.List[BDP.Core.AttackExecution.RangedProtocol.ProjectileInit.IProjectileInitStageModule]]::new()
    $initModules.Add($first); $initModules.Add($second)
    $serviceType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedProtocol.ProjectileInit.ProjectileInitStageService', $true)
    $service = [Activator]::CreateInstance($serviceType, [object[]]@($initModules,$null))
    $fire = [Activator]::CreateInstance($RangedCore.GetType('BDP.Core.AttackExecution.RangedProtocol.Model.FireRecord', $true))
    $emits = $fire.GetType().GetProperty('Emits').GetValue($fire)
    $emit = [Activator]::CreateInstance($emits.GetType().GetGenericArguments()[0])
    $emit.GetType().GetProperty('DamageFactor').SetValue($emit, [single]1)
    $emits.Add($emit)
    $plans = $serviceType.GetMethod('Execute').Invoke($service, [object[]]@($null,$null,$null,$fire))
    $plan = $plans[0]
    Assert-Ranged ($plan.GetType().GetProperty('InitialSegmentTriggerRatio').GetValue($plan) -eq $(if ($exclusive) {1} else {0.5})) '初始化首段比例被错误覆盖。'
    Assert-Ranged ($plan.GetType().GetProperty('InitialDamageFactor').GetValue($plan) -eq 4) '独占不能阻止无关伤害倍率合并。'
    Assert-Ranged ($second.ObservedPrevious -eq $exclusive) '初始化上下文没有正确传递逐发控制权。'
}
$routeState = [Activator]::CreateInstance($RangedContent.GetType('BDP.Content.RangedModules.RoutePath.RoutePathContext', $true))
$routeState.ControlReleased = $true
Assert-Ranged ($routeState.CloneTyped().ControlReleased) '逐发克隆不能丢失路线释放状态。'
$homingState = [Activator]::CreateInstance($RangedContent.GetType('BDP.Content.RangedModules.Homing.HomingState', $true))
$homingState.HasTakenFlightControl = $true
Assert-Ranged ($homingState.Clone().HasTakenFlightControl) '逐发克隆不能丢失追踪首次接管状态。'
Write-Output "通过：$RimWorldVersion 真实初始化／到达服务的独占与默认覆盖、无关贡献、逐发状态克隆。"
