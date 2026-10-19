$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Xml {
    param([string]$Path)

    return [xml](Get-Content -LiteralPath $Path -Raw -Encoding utf8)
}

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Find-Def {
    param(
        [xml]$Document,
        [string]$DefName
    )

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$abilityDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\AbilityDef\FujinRangedStrike.xml')
$projectileDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Projectiles\FujinRangedStrike.xml')
$beamTrailDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\BeamTrailDef\Presets.xml')
$moduleDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedModules.xml')
$chipDocument = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml')
$ability = Find-Def $abilityDocument 'BDP_Ability_FujinRangedStrike'
$projectile = Find-Def $projectileDocument 'BDP_Projectile_FujinRangedStrike'
$beamTrailPresets = @($beamTrailDocument.Defs.'BDP.Content.Projectiles.BeamTrail.BeamTrailPresetDef')
$brightMintLong = @($beamTrailPresets | Where-Object { $_.defName -eq 'BDP_TrailPreset_BrightMintLong' })[0]
$fujinTrail = @($beamTrailPresets | Where-Object { $_.defName -eq 'BDP_TrailPreset_SlimBrightGreen' })[0]
$distanceModule = Find-Def $moduleDocument 'BDP_RangedModule_DistanceRevector'
$impactModule = Find-Def $moduleDocument 'BDP_RangedModule_ImpactVisual'
$removedSlashModule = Find-Def $moduleDocument 'BDP_Fujin_DirectionalSlash'
$chipFunction = Find-Def $chipDocument 'BDP_ChipFunction_FujinRangedStrike'

Assert-True ($null -ne $ability) '缺少 Fujin AbilityDef。'
Assert-True ($null -ne $projectile) '缺少 Fujin 专用投射物 Def。'
Assert-True ($null -ne $brightMintLong) '缺少通用亮薄荷色长拖尾预设。'
Assert-True ($null -ne $fujinTrail) '缺少风刃专用细长拖尾预设。'
Assert-True ($null -ne $distanceModule) '缺少距离重定向模块 Def。'
Assert-True ($null -ne $impactModule) '缺少命中视觉模块 Def。'
Assert-True ($null -eq $removedSlashModule) 'Fujin 不得再声明方向斩击模块 Def。'
Assert-True ($null -ne $chipFunction) '缺少 Fujin 芯片功能 Def。'

Assert-True ([int]$chipFunction.config.Loadout.ActivationDelayTicks -eq 0) `
    '风刃·远程斩击激活延迟必须为 0。'
Assert-True ([int]$chipFunction.config.Loadout.DeactivationDelayTicks -eq 0) `
    '风刃·远程斩击停用延迟必须为 0。'

$verbProps = $ability.verbProperties
Assert-True ([int]$ability.cooldownTicksRange -eq 600) `
    '风刃·远程斩击能力冷却不得因本次改名调整。'
Assert-True ([float]$verbProps.warmupTime -eq [float]1.2) `
    '风刃·远程斩击攻击预热不得因本次改名调整。'
Assert-True ([string]$verbProps.verbClass -eq 'BDP.Core.Abilities.BdpVerb_AbilityShoot') `
    'Fujin 必须使用 Core Ability 远程射击宿主。'
Assert-True ([string]$verbProps.requireLineOfSight -eq 'true') `
    'Fujin 必须要求施法者到目标存在直接视线。'
Assert-True ([int]$verbProps.range -eq 500) `
    'Fujin 全图范围必须使用原版兼容的 500 格射程。'
Assert-True ([int]$verbProps.burstShotCount -eq 10) 'Fujin 默认齐射必须为 10 发。'
Assert-True ($null -eq $verbProps.maximumBurstShotCount) 'Fujin 不得声明人为齐射上限。'
Assert-True ([string]$verbProps.defaultProjectile -eq 'BDP_Projectile_FujinRangedStrike') `
    '风刃·远程斩击能力必须绑定对应投射物。'
foreach ($accuracyName in @('accuracyTouch', 'accuracyShort', 'accuracyMedium', 'accuracyLong')) {
    Assert-True ([float]$verbProps.$accuracyName -eq [float]0.8) "Fujin 的 $accuracyName 必须为 80%。"
}

Assert-True ([string]$projectile.thingClass -eq 'BDP.Core.Projectiles.BdpProjectile') `
    'Fujin 投射物必须复用 BDP 投射物宿主。'
Assert-True ([string]$projectile.projectile.damageDef -eq 'Cut') `
    'Fujin 主目标伤害必须使用原版 Cut。'
Assert-True ([int]$projectile.projectile.damageAmountBase -eq 40) `
    '风刃·远程斩击投射物伤害必须保持为 40。'
Assert-True ([math]::Abs(([double]$projectile.projectile.armorPenetrationBase) - 0.6) -lt 0.0001) `
    '风刃·远程斩击投射物护甲穿透必须保持为 60%。'
Assert-True ([int]$projectile.projectile.speed -eq 180) `
    '风刃·远程斩击投射物飞行速度必须保持为 180。'
Assert-True ([string]$projectile.projectile.flyOverhead -ne 'true') `
    'Fujin 不得开启原版越顶投射物分类，否则原版 HitFlags 会变为 None，无法命中目标 Pawn。'
Assert-True ([string]$projectile.modExtensions.li.preset -eq 'BDP_TrailPreset_SlimBrightGreen') `
    '风刃·远程斩击投射物必须使用亮绿色细长拖尾。'
Assert-True ([string]$fujinTrail.trailColor -eq '(0.45, 1.0, 0.55, 0.775)') `
    '风刃专用拖尾必须使用高饱和绿色。'
foreach ($fieldName in @('trailTexPath', 'segmentLifetimeTicks', 'startOpacity', 'fadeRatio', 'fadeExponent', 'altitudeOffset', 'startForwardOffset', 'debugLogging')) {
    Assert-True ([string]$fujinTrail.$fieldName -eq [string]$brightMintLong.$fieldName) `
        "风刃专用拖尾的 $fieldName 必须与通用长拖尾一致。"
}
Assert-True ([math]::Abs(([double]$fujinTrail.trailWidth * 2.0) - [double]$brightMintLong.trailWidth) -lt 0.0000001) `
    '风刃专用拖尾宽度必须是通用长拖尾的一半。'
Assert-True ([math]::Abs(([double]$fujinTrail.trailWidth) - 0.05525) -lt 0.0000001) `
    '风刃专用拖尾宽度必须为 0.05525。'

$entry = @($chipFunction.config.Expression.Entries.li | Where-Object { $_.Id -eq 'fujin_ranged_strike' })[0]
Assert-True ($null -ne $entry) 'Fujin 芯片功能必须提供 Ability 入口。'
Assert-True ([string]$entry.EntryType -eq 'Ability') 'Fujin 入口必须是 Ability。'
Assert-True ([string]$entry.AbilityDefName -eq 'BDP_Ability_FujinRangedStrike') `
    '风刃·远程斩击芯片功能必须绑定对应 AbilityDef。'
Assert-True ([string]$entry.WeaponMode -eq 'Ranged') 'Fujin Ability 入口必须使用远程武器模式。'
Assert-True ([string]$entry.DirectTargetLineOfSight -eq 'Required') `
    'Fujin 入口必须要求直接目标视线。'
Assert-True (@($entry.RangedModules.li).Count -eq 4) `
    '风刃·远程斩击入口必须包含既有的四个远程模块。'
$mountedModuleNames = @($entry.RangedModules.li.moduleDef)
Assert-True ($mountedModuleNames -contains 'BDP_RangedModule_DistanceRevector') `
    '风刃·远程斩击必须保留距离重定向远程模块。'
Assert-True ($mountedModuleNames -contains 'BDP_RangedModule_ImpactVisual') `
    '风刃·远程斩击必须挂载命中视觉远程模块。'
Assert-True ([string]$distanceModule.defaultConfig.CheckpointRatios.li[0] -eq '0.20') `
    'Fujin 第一次距离重定向必须从 20% 处开始。'
$originSpread = $entry.Execution.OriginSpread
Assert-True ($null -ne $originSpread) 'Fujin 必须声明发射点随机散布。'
Assert-True ([string]$originSpread.LateralMin -eq '-0.3') 'Fujin 发射点横向散布下限必须为 -0.3。'
Assert-True ([string]$originSpread.LateralMax -eq '0.3') 'Fujin 发射点横向散布上限必须为 0.3。'
Assert-True ([string]$originSpread.ForwardMin -eq '0') 'Fujin 发射点前后散布下限必须为 0。'
Assert-True ([string]$originSpread.ForwardMax -eq '0.105') 'Fujin 发射点前后散布上限必须为 0.105。'

$fujinSource = Read-Source (Join-Path $sourceRoot 'BDP\Core\Abilities\BdpVerb_AbilityShoot.cs')
$revectorSource = Read-Source (Join-Path $sourceRoot 'BDP.Content\RangedModules\DistanceRevector\DistanceRevectorModule.cs')
Assert-True ($fujinSource -match 'Verb_AbilityShoot') 'Ability 宿主必须继承原版 Verb_AbilityShoot。'
Assert-True ($fujinSource -match 'RangedAttackProtocol') 'Ability 宿主必须接入既有远程协议。'
Assert-True ($revectorSource -match 'IArrivalStageModule') '距离重定向必须使用 Arrival 阶段。'
Assert-True ($revectorSource -match 'InitialSegmentTriggerRatio') '距离重定向必须使用首段距离触发机制。'
Assert-True ($revectorSource -match 'OrderedSpreadSampler\.SampleOffset') '风刃距离重定向必须使用毒蛇式段级稳定散布。'
Assert-True ($revectorSource -match 'IntermediateSpreadRadius') '风刃距离重定向必须保留中间段散布配置。'

Write-Output 'FujinAuthoringSmokeTests PASS'
