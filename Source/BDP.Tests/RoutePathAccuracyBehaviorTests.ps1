param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest $RimWorldVersion

# 读取真实生产方法；未实施时明确失败，不能用测试内的公式冒充生产行为。
$curveType = $RangedContent.GetType('BDP.Content.RangedModules.RoutePath.RouteAimAccuracyUtility', $false)
Assert-Ranged ($null -ne $curveType) '尚未实现正式的毒蛇精度曲线。'
$adjust = $curveType.GetMethod('AdjustChance')
Assert-Ranged ($null -ne $adjust) '缺少正式概率映射入口。'
function Invoke-Curve([single]$Chance, [single]$Shooter, [single]$ReferencePower = 4.01188474) {
    return [single]$adjust.Invoke($null, [object[]]@($Chance,$Shooter,[single]0.99,$ReferencePower,[single]1.58275869))
}

# 两个参照与等效属性；目标体型、掩体和真实碰撞不在这项纯概率检查中。
$distance = 30.9
$weapon = 0.50 + (0.22 - 0.50) * (30.9 - 25) / 15
$reference = Invoke-Curve ([Math]::Pow(0.99,$distance)) 0.99
$equivalent = Invoke-Curve ([Math]::Pow(0.99575,$distance)*$weapon) 0.99575
Assert-Ranged ([Math]::Abs($reference-0.995) -lt 0.000002) '100%武器精度与20级常态没有达到99.5%参照。'
Assert-Ranged ([Math]::Abs($equivalent-0.995) -lt 0.000002) '当前毒蛇与完整增强没有补足参照。'
$plain = Invoke-Curve ([Math]::Pow(0.99,$distance)*$weapon) 0.99
$gunlink = Invoke-Curve ([Math]::Pow(0.993125,$distance)*$weapon) 0.993125
$careful = Invoke-Curve ([Math]::Pow(0.994375,$distance)*$weapon) 0.994375
$sight = Invoke-Curve ([Math]::Pow(0.99665,$distance)*$weapon) 0.99665
Assert-Ranged ($plain -lt $gunlink -and $gunlink -lt $careful -and $careful -lt $equivalent -and $equivalent -lt $sight) '能力成长或超越参照的梯度不正确。'
Assert-Ranged ((Invoke-Curve 0 0.99) -eq 0 -and (Invoke-Curve 1 0.99) -eq 1) '0/1概率边界不能生成额外命中或失准。'
Assert-Ranged ([Math]::Abs((Invoke-Curve 0.37 0.99575 1)-0.37) -lt 0.000001) '中性配置必须保持原概率。'

# 正式配置必须克隆完整，其他路线使用者默认保持中性。
$configType = $RangedContent.GetType('BDP.Content.RangedModules.RoutePath.RoutePathConfig', $true)
$config = [Activator]::CreateInstance($configType)
Assert-Ranged ($config.AimReferenceErrorPower -eq 1) '未配置的路线模块不得自动得到命中增强。'
$config.AimReferenceAccuracy = [single]0.99
$config.AimReferenceErrorPower = [single]4.01188474
$config.AimErrorGrowthExponent = [single]1.58275869
$clone = $config.CloneTyped()
foreach ($name in @('AimReferenceAccuracy','AimReferenceErrorPower','AimErrorGrowthExponent')) {
    Assert-Ranged ($configType.GetField($name).GetValue($config) -eq $configType.GetField($name).GetValue($clone)) "配置克隆丢失$name。"
}
Assert-Ranged ($clone.HighAccuracySpreadScale -eq [single]0.25) '不能为了高命中抹掉有序散布。'

[xml]$presets = Get-Content -LiteralPath (Join-Path $RangedModRoot 'Common/Content/Defs/ChipManufacturingPresetDef/Presets.xml') -Raw
$viper = $presets.SelectSingleNode("//defName[text()='BDP_ChipFunction_Viper']/..")
$mount = $viper.SelectSingleNode(".//RangedModules/li[moduleDef='BDP_RoutePathModule']/config")
Assert-Ranged ($null -ne $mount -and [single]$mount.AimReferenceErrorPower -gt 1) '毒蛇没有单独开启XML曲线。'
Assert-Ranged ($presets.SelectNodes('//AimReferenceErrorPower').Count -eq 1) '精度增强误扩展到其他业务。'
Assert-Ranged ([single]$mount.HighAccuracySpreadScale -eq [single]0.25) '毒蛇XML必须保留25%基础散布。'

# 用真实宿主处理报告；确认既有倍率、体型与掩体不被重复映射。
$refs = @($RangedCore.Location, [Verse.Thing].Assembly.Location, [UnityEngine.Vector3].Assembly.Location) + @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -Path (Join-Path $PSScriptRoot 'TestSupport/AimChanceProbe.cs') -ReferencedAssemblies $refs
$probe = [AimChanceProbe]::new()
$probe.Value = [single]0.8
$slots = [Collections.Generic.List[BDP.Core.AttackExecution.RangedModules.Runtime.RangedAttackModuleSlot]]::new()
$slot = [BDP.Core.AttackExecution.RangedModules.Runtime.RangedAttackModuleSlot]::new()
$slot.Runtime = $probe
$slots.Add($slot)
$sessionType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedAttackModuleSession',$true)
$session = [Activator]::CreateInstance($sessionType,$true)
$sessionType.GetProperty('Slots').SetValue($session,$slots)
$hostType = $RangedCore.GetType('BDP.Core.Verbs.BdpVerb_Shoot',$true)
$shootVerb = [Activator]::CreateInstance($hostType)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$hostType.GetProperty('HostModuleSession',$flags).SetValue($shootVerb,$session)
$planType = $RangedCore.GetType('BDP.Core.AttackExecution.RangedProtocol.Model.ProjectileInitPlan',$true)
$plan = [Activator]::CreateInstance($planType,$true)
$planType.GetProperty('ResultId').SetValue($plan,'viper-source')
$report = [object][Verse.ShotReport]::new()
$reportType = $report.GetType()
foreach ($entry in @{factorFromShooterAndDist=1;factorFromEquipment=0.4;factorFromWeather=1;factorFromCoveringGas=1;factorFromTargetSize=0.5;coversOverallBlockChance=0.3}.GetEnumerator()) {
    $reportType.GetField($entry.Key,$flags).SetValue($report,[single]$entry.Value)
}
$resolve = $hostType.GetMethod('ResolveReportAccuracyFactor',$flags)
$capture = $hostType.GetMethod('CaptureAccuracySnapshot',[Reflection.BindingFlags]'Static,NonPublic')
$factor = [single]$resolve.Invoke($shootVerb,[object[]]@($plan,$report,[single]0.5))
Assert-Ranged ([Math]::Abs($factor-2) -lt 0.000001 -and $probe.Calls -eq 1 -and $probe.ObservedResultId -eq 'viper-source' -and [Math]::Abs($probe.ObservedChance-0.2) -lt 0.000001) '正式报告没有仅修正一次已含既有倍率的该发概率。'
$capture.Invoke($null,[object[]]@($plan,$report,$factor,[single]0))
$accuracy = $planType.GetProperty('AccuracySnapshot').GetValue($plan)
Assert-Ranged ([Math]::Abs($accuracy.StandardAimChance-0.8) -lt 0.000001 -and [Math]::Abs($accuracy.IgnoringPostureAimChance-0.4) -lt 0.000001 -and [Math]::Abs($accuracy.PassCoverChance-0.7) -lt 0.000001) '修正错误地覆盖了体型或掩体，或者没有进入正式快照。'
$probe.Value = 0
$factor = [single]$resolve.Invoke($shootVerb,[object[]]@($plan,$report,[single]0.5))
$capture.Invoke($null,[object[]]@($plan,$report,$factor,[single]0))
Assert-Ranged ($planType.GetProperty('AccuracySnapshot').GetValue($plan).StandardAimChance -eq 0) '零概率被快照错误恢复为默认倍率。'
$probe.Value = [single]::NaN
Assert-Ranged ([single]$resolve.Invoke($shootVerb,[object[]]@($plan,$report,[single]0.5)) -eq [single]0.5) '无效模块输出不得污染原版概率。'

# 真实路线模块先按单侧来源隔离；外侧发射不能借用毒蛇增强。
$route = [BDP.Content.RangedModules.RoutePath.RoutePathModule]::new()
$runtimeContext = [BDP.Core.AttackExecution.RangedAttackModuleRuntimeContext]::new()
$runtimeContext.Mount = [BDP.Core.AttackExecution.RangedModuleMountConfig]::new()
$runtimeContext.Mount.sourceResultId = 'viper-source'
$runtimeContext.GetType().GetProperty('Config').SetValue($runtimeContext,$config)
([BDP.Core.AttackExecution.IRangedAttackModuleRuntime]$route).Initialize($runtimeContext)
$context = [BDP.Core.AttackExecution.RangedProtocol.Aim.AimChanceContext]::new([Verse.Pawn]::new(),'other-source',[single]0.2)
$modifyRoute = $route.GetType().GetMethod('BDP.Core.AttackExecution.RangedProtocol.Aim.IAimChanceModifier.ModifyStandardAimChance',$flags)
Assert-Ranged ([single]$modifyRoute.Invoke($route,[object[]]@($context)) -eq [single]0.2) '混合双持的另一来源意外得到毒蛇增强。'

# 偏移只依赖冻结精度和稳定种子；接口不再接收地图，也不能回收随机偏射。
$resolverType = [BDP.Content.RangedModules.RoutePath.RouteSegmentResolver]
foreach ($methodName in @('BuildCurrentLegFlightPathSnapshot','TryResolveContinuation')) {
    Assert-Ranged (-not ($resolverType.GetMethod($methodName).GetParameters() | Where-Object { $_.ParameterType -eq [Verse.Map] })) '偏移生成仍在接收地图以干预墙体碰撞。'
}
$routeContext = [BDP.Content.RangedModules.RoutePath.RoutePathContext]::new()
$routeContext.Anchors.Add([BDP.Core.PathInput.PathAnchor]::FromCell([Verse.IntVec3]::new(3,0,1)))
$routeContext.HasFinalTarget = $true
$routeContext.FinalTarget = [Verse.LocalTargetInfo]::new([Verse.IntVec3]::new(6,0,1))
$accuracyState = [BDP.Core.AttackExecution.RangedProtocol.Model.ProjectileAccuracySnapshot]::new()
$accuracyState.IsAvailable = $true
$build = $resolverType.GetMethod('BuildCurrentLegFlightPathSnapshot')
function Get-SpreadPath([single]$Chance) {
    $accuracyState.StandardAimChance = $Chance
    return $build.Invoke($null,[object[]]@($routeContext,$accuracyState,[UnityEngine.Vector3]::new(0.5,0,1.5),'stable-attack','viper-source',0))
}
$lowPath = Get-SpreadPath 0.2
$highPath = Get-SpreadPath 0.995
$repeatPath = Get-SpreadPath 0.995
$nominalEnd = [UnityEngine.Vector3]::new(3.5,0,1.5)
$lowOffset = $lowPath.End - $nominalEnd
$highOffset = $highPath.End - $nominalEnd
Assert-Ranged ($highOffset.magnitude -gt 0 -and $highOffset.magnitude -lt $lowOffset.magnitude -and $highOffset.magnitude -le 0.15860 -and $lowOffset.magnitude -le 0.53126) '高档应减少偏移并保留散乱，低档不能得到额外回收。'
Assert-Ranged (($repeatPath.End-$highPath.End).sqrMagnitude -lt 0.00000001) '同一发同一段的偏移不稳定。'
$routeContext.HasWildMissOffset = $true
$routeContext.WildMissOffset = [UnityEngine.Vector3]::new(3,0,0)
$missPath = Get-SpreadPath 0.995
Assert-Ranged (($missPath.End-$highPath.End-$routeContext.WildMissOffset).sqrMagnitude -lt 0.00000001) '随机偏射路线被额外恢复为意图命中路线。'
$continueArguments = [object[]]@($routeContext,[UnityEngine.Vector3]::new(3.5,0,1.5),$accuracyState,'stable-attack','viper-source',0,$null,$null)
Assert-Ranged ([bool]$resolverType.GetMethod('TryResolveContinuation').Invoke($null,$continueArguments)) '随机偏射没有正常建立下一段。'
Assert-Ranged ($continueArguments[7].End.x -gt 9.4 -and ($continueArguments[7].End-[UnityEngine.Vector3]::new(9.5,0,1.5)).magnitude -le 0.07614) '最终续段丢失偏射位移或没有使用最终半径。'
Assert-Ranged ([BDP.Content.RangedModules.RoutePath.RouteSegmentResolver]::TryAdvanceLeg($routeContext)) '有效续段没有推进到最终段。'
Assert-Ranged (-not [bool]$resolverType.GetMethod('TryResolveContinuation').Invoke($null,$continueArguments)) '路线完成后仍尝试额外回接或续段。'
Write-Output "通过：$RimWorldVersion 毒蛇参照、梯度、XML隔离、真实报告与快照、来源隔离、纯精度散布与偏射保留。"
