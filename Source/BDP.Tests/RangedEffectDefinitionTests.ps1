param([ValidateSet('1.6','1.7')][string]$RimWorldVersion = '1.6')
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[xml]$presets = Get-Content (Join-Path $modRoot 'Common/Content/Defs/ChipManufacturingPresetDef/Presets.xml') -Raw
[xml]$effects = Get-Content (Join-Path $modRoot 'Common/Content/Defs/RangedDebuffEffectDef/Effects.xml') -Raw
[xml]$modules = Get-Content (Join-Path $modRoot 'Common/Content/Defs/RangedModuleDef/RangedDebuff.xml') -Raw
Assert-Ranged ($modules.Defs.ChildNodes.Where({$_.NodeType -eq 'Element'}).Count -eq 1) '正式减益必须只有一个宿主定义。'
$collapse = $effects.SelectSingleNode("/Defs/*[defName='BDP_DebuffEffect_BodyPartDisintegration']")
$lead = $effects.SelectSingleNode("/Defs/*[defName='BDP_DebuffEffect_LeadWeight']")
Assert-Ranged ([int]$collapse.hitsToDisintegrate -eq 4) '崩解必须同部位四次满层。'
Assert-Ranged ([decimal]$lead.severityPerHit -eq 0.1 -and [decimal]$lead.severityPerExplosionHit -eq 0.01) '负重必须分别为直击10%和爆炸1%。'
foreach ($name in @('BDP_ChipFunction_LeadShotModified','BDP_ChipFunction_LeadShotOppositeSide')) {
    $mount = $presets.SelectSingleNode("/Defs/*[defName='$name']//RangedModuleAugmentations//li[moduleDef='BDP_RangedDebuffModule']")
    Assert-Ranged ($null -ne $mount) "$name 必须挂同一通用宿主。"
    Assert-Ranged ($mount.config.EffectDef -eq $lead.defName -and [single]$mount.config.ProjectileSpeedMultiplier -eq 0.5) '负重包挂载必须保留原有半速。'
    Assert-Ranged ($mount.config.HasHitFeedbackColor -eq 'true' -and $mount.config.HasProjectileTrailCore -eq 'true') '铅弹的颜色与拖尾内芯声明不能丢失。'
}
$alektor = $presets.SelectSingleNode("/Defs/*[defName='BDP_ChipFunction_Alektor']//RangedModules")
Assert-Ranged ($alektor.SelectSingleNode("li[moduleDef='BDP_VitalHitModule']/config/SelectionMode").InnerText -eq 'AvoidVital') '卵之冠必须使用原有避开要害策略。'
Assert-Ranged ([decimal]$alektor.SelectSingleNode("li[moduleDef='BDP_VitalHitModule']/config/VitalHitChance").InnerText -eq [decimal]0.7) '卵之冠必须有70%使用模块部位选择、30%交给原版随机。'
Assert-Ranged ($alektor.SelectSingleNode("li[moduleDef='BDP_RangedDebuffModule']/config/EffectDef").InnerText -eq $collapse.defName) '崩解包不能另建一个业务宿主。'
[xml]$dev = Get-Content (Join-Path $modRoot 'Common/Development/Defs/ChipManufacturingPresetDef/ExactBodyPartRemoval.xml') -Raw
$exact = $dev.SelectNodes("//li[moduleDef='BDP_VitalHitModule']/config[SelectionMode='ExactDefOccurrence']")
$hosts = $dev.SelectNodes("//li[moduleDef='BDP_RangedDebuffModule']/config[EffectDef='BDP_DebuffEffect_ExactBodyPartRemoval']")
Assert-Ranged ($exact.Count -eq 64 -and $hosts.Count -eq 64) '开发精确部位的64种挂载必须全部迁移为选择与效果分离。'
# 对照旧挂载及旧定义逐个核对部位和出现序号，数量相同不能证明迁移正确。
[xml]$baselineDev = (git -C $modRoot show '00a46b060:模组工程/BorderDefenseProtocol/Common/Development/Defs/ChipManufacturingPresetDef/ExactBodyPartRemoval.xml') -join "`n"
Assert-Ranged ($LASTEXITCODE -eq 0) '无法读取精确部位挂载基线。'
[xml]$baselineModules = (git -C $modRoot show '00a46b060:模组工程/BorderDefenseProtocol/Common/Development/Defs/RangedModuleDef/ExactBodyPartRemoval.xml') -join "`n"
Assert-Ranged ($LASTEXITCODE -eq 0) '无法读取精确部位定义基线。'
foreach ($before in $baselineDev.SelectNodes('//Entries/li[RangedModuleAugmentations]')) {
    $moduleName = $before.SelectSingleNode('RangedModuleAugmentations/li/Modules/li/moduleDef').InnerText
    $previous = $baselineModules.SelectSingleNode("/Defs/*[defName='$moduleName']/defaultConfig")
    $current = $dev.SelectSingleNode("//Entries/li[Id='$($before.Id)']//li[moduleDef='BDP_VitalHitModule']/config")
    Assert-Ranged ($null -ne $previous -and $null -ne $current -and
        $previous.ExactBodyPartDef -eq $current.ExactBodyPartDef -and
        $previous.ExactBodyPartOccurrenceIndex -eq $current.ExactBodyPartOccurrenceIndex) "精确部位迁移不一致：$($before.Id)"
}
foreach ($mount in $presets.SelectNodes("//li[moduleDef='BDP_RangedDebuffModule']/config") + $hosts) {
    Assert-Ranged ($null -eq $mount.SelectSingleNode('HitPartSelectionMode|ExactBodyPartDef|Severity|MaximumSeverityAction')) '减益挂载不得残留选择、层数或阈值动作配置。'
}
Write-Output "通过：$RimWorldVersion 共享效果定义、两种铅弹挂载、卵之冠和64种精确选择迁移。"
