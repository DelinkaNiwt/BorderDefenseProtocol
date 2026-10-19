$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Xml {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path) ("缺少风刃运行时配置：" + $Path)
    return [xml](Get-Content -LiteralPath $Path -Raw -Encoding utf8)
}

function Read-Source {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path) ("缺少风刃运行时源码：" + $Path)
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Find-Def {
    param([xml]$Document, [string]$DefName)

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$moduleSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\RangedModules\ImpactVisual\ImpactVisualModule.cs')
$executorSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\RangedModules\ImpactVisual\ImpactVisualEffectExecutor.cs')
$moteSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\RangedModules\ImpactVisual\Mote_ImpactBlade.cs')
$subEffecterSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\RangedModules\ImpactVisual\SubEffecter_ImpactBladeBurst.cs')
$planSource = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\RangedFlightProtocol\Model\ExtraEffectPlan.cs')
$dispatcherSource = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\RangedFlightProtocol\Impact\AttackTargetEventDispatcher.cs')
$projectileSource = Read-Source (Join-Path $sourceRoot 'BDP\Core\Projectiles\BdpProjectile.cs')
$bootstrapSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs')
$moduleDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedModules.xml')
$chipDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml')
$impactModule = Find-Def $moduleDocument 'BDP_RangedModule_ImpactVisual'
$chipFunction = Find-Def $chipDocument 'BDP_ChipFunction_FujinRangedStrike'
$entry = @($chipFunction.config.Expression.Entries.li | Where-Object { $_.Id -eq 'fujin_ranged_strike' })[0]

Assert-True ($moduleSource -match 'IImpactStageModule') '风刃命中视觉模块必须只接入 Impact 阶段。'
Assert-True ($moduleSource -match 'context\.IsValidHit') '模块必须拒绝无效命中。'
Assert-True ($moduleSource -match 'context\.HitThing\s*==\s*null') '模块必须对所有实际命中的 Thing 生效。'
Assert-True ($moduleSource -match 'ExtraEffectsToAppend') '模块必须复用额外效果计划。'
Assert-True ($moduleSource -match 'ExecutionTiming\s*=\s*ExtraEffectExecutionTiming\.BeforeDamage') `
    '命中特效必须声明在伤害前执行，避免目标被击杀后丢失。'
Assert-True ($moduleSource -match 'ExactPosition' -and $moduleSource -match 'ImpactExactXParameter' -and $moduleSource -match 'ImpactExactZParameter') `
    '模块必须在投射物销毁前缓存命中瞬间的精确平面位置。'
Assert-True ($moduleSource -match 'DrawPos' -and $moduleSource -match 'ImpactTargetXParameter' -and $moduleSource -match 'ImpactTargetZParameter') `
    '模块必须在投射物销毁前缓存命中瞬间的目标显示位置。'
Assert-True ($executorSource -match 'IExtraEffectPlanExecutor') '缺少风刃命中视觉执行器。'
Assert-True ($executorSource -match 'Thing\s+targetThing' -and $executorSource -notmatch 'targetPawn\.Spawned') `
    '命中特效执行器必须接收任意命中 Thing，不能把 Spawned Pawn 作为前置条件。'
Assert-True ($executorSource -match 'AttackInstanceId' -and $executorSource -match 'thingIDNumber') `
    '去重键必须同时包含攻击实例与受击 Thing。'
Assert-True ($executorSource -match 'SpawnMaintainedAtExactPosition' -and $executorSource -match 'AddEffecterToMaintain') `
    '执行器必须使用原版受维护 Effecter，并把首次生成锚定到命中瞬间位置。'
Assert-True ($executorSource -match 'TryResolveImpactExactPosition' -and $executorSource -match 'impactExactX') `
    '执行器必须读取命中瞬间缓存的精确位置。'
Assert-True ($executorSource -match 'TryResolveImpactTargetPosition' -and $executorSource -match 'impactTargetX') `
    '执行器必须读取命中瞬间缓存的目标显示位置。'
Assert-True ($executorSource -match 'TargetInfo\(sourceCell, map\)' -and $executorSource -match 'effecter\.offset') `
    '执行器必须把命中锚点所在格作为 Effecter 源目标，并把小数偏移传给动态刀刃子特效器。'
Assert-True ($executorSource -match 'hasTargetPosition' -and $executorSource -match 'impactAnchorPosition') `
    '执行器必须优先以命中瞬间目标位置作为刀刃阵列锚点。'
Assert-True ($bootstrapSource -match 'ImpactVisualEffectExecutor') 'Content 启动入口没有注册执行器。'
Assert-True ($moteSource -match 'class\s+Mote_ImpactBlade\s*:\s*Mote') '缺少自定义三角刀刃 Mote 类。'
Assert-True ($moteSource -match 'Graphics\.DrawMesh') '自定义刀刃必须直接绘制网格。'
Assert-True ($moteSource -match 'exactRotation') '自定义刀刃必须读取尖端倾角参数。'
Assert-True ($moteSource -match 'vertices' -and $moteSource -match 'triangles') '自定义刀刃必须明确建立三角形顶点和索引。'
Assert-True ($moteSource -match 'Destroy\(DestroyMode') '自定义刀刃必须在销毁时释放短命网格。'
Assert-True ($moteSource -match 'Configure' -and $moteSource -match 'StartDelayTicks') `
    '自定义刀刃 Mote 必须支持动态尺寸和错峰延迟。'
Assert-True ($moteSource -match 'BladeGrowthTicks\s*=\s*1f') `
    '单道刀刃必须在 1 Tick 内完成伸长。'
Assert-True ($subEffecterSource -match 'class\s+SubEffecter_ImpactBladeBurst\s*:\s*SubEffecter') `
    '缺少动态刀刃阵列子特效器。'
Assert-True ($subEffecterSource -match 'ResolveTargetThing' -and $subEffecterSource -match 'Vector2\.one') `
    '动态刀刃阵列必须支持非 Pawn 目标的默认尺寸。'
Assert-True ($subEffecterSource -match 'BodyGraphic' -and $subEffecterSource -match 'bodyGraphicScale') `
    '动态刀刃阵列必须读取目标身体图形和体型倍率。'
Assert-True ($subEffecterSource -match 'sourceTarget\.Cell' -and $subEffecterSource -match 'parent\.offset') `
    '动态刀刃阵列必须使用精确源目标和固定命中偏移。'
Assert-True ($subEffecterSource -match 'CeilToInt' -and $subEffecterSource -match 'MinimumBladeCount' -and $subEffecterSource -match 'MaximumBladeCount') `
    '动态刀刃阵列必须按目标宽度计算并限制刀刃数量。'
Assert-True ($subEffecterSource -match 'CoverageWidthFactor' -and $subEffecterSource -match 'BladeLengthFactor') `
    '动态刀刃阵列必须使用收敛后的目标覆盖范围和长度倍率。'
Assert-True ($subEffecterSource -match 'GroundContactOffset\s*=\s*0\.50f') `
    '动态刀刃阵列必须使用独立的固定地面接地偏移。'
Assert-True ($subEffecterSource -match 'rootZ\s*=\s*impactPosition\.z\s*-\s*GroundContactOffset') `
    '动态刀刃阵列的接地线必须从命中平面锚点计算，不能使用目标贴图高度下压。'
Assert-True ($subEffecterSource -notmatch 'targetHeight\s*\*\s*0\.5f' -and $subEffecterSource -notmatch 'ResolveBodyRootPosition') `
    '动态刀刃阵列不得把身体贴图平面下边界误当成脚底线。'
Assert-True ($subEffecterSource -match 'ShouldDrawRotated' -and $subEffecterSource -match 'Rotated\(\)') `
    '动态刀刃阵列必须按 Pawn 朝向匹配身体网格的宽高交换。'
Assert-True ($subEffecterSource -match 'ResolveRootOffset' -and $subEffecterSource -match 'ResolveBladeAngle') `
    '动态刀刃阵列必须分别解析非规整根部位置和混合倾角。'
Assert-True ($subEffecterSource -match 'BladeAnglePattern' `
        -and $subEffecterSource -match '-36f' `
        -and $subEffecterSource -match '24f' `
        -and $subEffecterSource -match '-28f' `
        -and $subEffecterSource -match '30f') `
    '动态刀刃阵列必须使用跨前后层的非单调交错倾角，不能让偶数层和奇数层分别向两侧展开。'
Assert-True ($subEffecterSource -match 'BladeAnglePattern\s*\[index\s*%\s*BladeAnglePattern\.Length\]') `
    '动态刀刃阵列的倾角必须按完整根部槽位分配，不能按前后层重新形成单向角度。'
Assert-True ($subEffecterSource -match 'index\s*==\s*0' `
        -and $subEffecterSource -match 'index\s*==\s*bladeCount\s*-\s*2' `
        -and $subEffecterSource -match '44f' `
        -and $subEffecterSource -match '-42f' `
        -and $subEffecterSource -match '34f') `
    '动态刀刃阵列必须只保留 1～2 道朝内穿刺的大倾角，并同时保留外刺方向。'
Assert-True ($subEffecterSource -match 'MaximumStaggerTicks\s*=\s*1') `
    '动态刀刃阵列的最晚起刺错峰不得超过 1 Tick。'
Assert-True ($subEffecterSource -match 'ThingMaker\.MakeThing' -and $subEffecterSource -match 'GenSpawn\.Spawn') `
    '动态刀刃阵列必须创建并生成独立 Mote。'
Assert-True ($subEffecterSource -match 'SubCleanup' -and $subEffecterSource -match 'Maintain') `
    '动态刀刃阵列必须维护并清理已生成 Mote。'
Assert-True ($planSource -match 'ExtraEffectExecutionTiming') '额外效果计划必须携带执行时序。'
Assert-True ($dispatcherSource -match 'ExtraEffectExecutionTiming' -and $dispatcherSource -match 'executionTiming') `
    '额外效果派发器必须支持伤害前效果。'
Assert-True ($projectileSource -match 'ExtraEffectExecutionTiming\.BeforeDamage' `
        -and $projectileSource -match 'ExtraEffectExecutionTiming\.AfterDamage') `
    '投射物命中流程必须分别派发伤害前和伤害后效果。'

Assert-True ($null -ne $impactModule) '缺少风刃命中视觉模块 Def。'
Assert-True ([string]$impactModule.runtimeClass -eq 'BDP.Content.RangedModules.ImpactVisual.ImpactVisualModule') `
    '风刃命中视觉模块 Def 的运行时类型不正确。'
Assert-True ([string]$impactModule.defaultConfig.EffecterDef -eq 'BDP_Effecter_ImpactBladeBurst') `
    '命中视觉模块必须绑定命中刀刃 Effecter。'
Assert-True ($null -ne $entry) 'Fujin 芯片功能必须提供 Ability 入口。'
$mountedModules = @($entry.RangedModules.li.moduleDef)
Assert-True ($mountedModules -contains 'BDP_RangedModule_DistanceRevector') '风刃·远程斩击必须保留距离重定向模块。'
Assert-True ($mountedModules -contains 'BDP_RangedModule_ImpactVisual') '风刃·远程斩击必须挂载命中视觉模块。'

Write-Output 'FujinImpactVisualRuntimeSmokeTests PASS'
