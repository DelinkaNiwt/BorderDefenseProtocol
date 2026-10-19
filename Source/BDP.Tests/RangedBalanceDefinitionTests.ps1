param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
# 数据检查只读共享资源，不用另一版本的数据库替代目标版本运行验证。
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[xml]$projectiles = Get-Content -LiteralPath (Join-Path $modRoot 'Common/Content/Defs/ThingDef/Projectiles/Projectiles.xml') -Raw
function Get-Projectile([string]$Name) { $projectiles.SelectSingleNode("/Defs/ThingDef[defName='$Name']/projectile") }
# 对照固定的需求前基线检查所有投射物标量；只豁免需求明确改变的字段。
# 这里只展开本文件的标量父项，不模拟原版定义加载、列表合并或运行时补丁。
[xml]$baselineProjectiles = (git -C $modRoot show '00a46b060:模组工程/BorderDefenseProtocol/Common/Content/Defs/ThingDef/Projectiles/Projectiles.xml') -join "`n"
Assert-Ranged ($LASTEXITCODE -eq 0) '无法读取修改前基线，不能跳过范围检查。'
function Get-ProjectileScalars([System.Xml.XmlElement]$Node, [xml]$Document) {
    $values = @{}
    if ($Node.ParentName) {
        $parent = $Document.SelectSingleNode("/Defs/ThingDef[@Name='$($Node.ParentName)']")
        if ($null -ne $parent) { $values = Get-ProjectileScalars $parent $Document }
    }
    foreach ($field in $Node.SelectNodes('projectile/*')) { $values[$field.Name] = $field.InnerText }
    return $values
}
$approvedChanges = @{
    'BDP_Projectile_SniperStandard/damageAmountBase' = '40'
    'BDP_Projectile_Tracking/damageAmountBase' = '8'
    'BDP_Projectile_Tracking_Overhead/damageAmountBase' = '8'
    'BDP_Projectile_Explosive/damageAmountBase' = '6'
    'BDP_Projectile_Explosive/armorPenetrationBase' = '0'
    'BDP_Projectile_Explosive/explosionRadius' = '1.9'
    'BDP_Projectile_Explosive/speed' = '80'
    'BDP_Projectile_BodyCollapse/speed' = '20'
    'BDP_Projectile_Variable_Explosive/speed' = '120'
}
$runtimeResolvedFields = @('damageAmountBase', 'armorPenetrationBase', 'explosionRadius')
foreach ($before in $baselineProjectiles.SelectNodes('/Defs/ThingDef')) {
    $name = $before.defName
    $after = $projectiles.SelectSingleNode("/Defs/ThingDef[defName='$name']")
    Assert-Ranged ($null -ne $after) "投射物被意外删除：$name"
    $previous = Get-ProjectileScalars $before $baselineProjectiles
    $current = Get-ProjectileScalars $after $projectiles
    foreach ($field in @(@($previous.Keys) + @($current.Keys) | Sort-Object -Unique)) {
        # 战斧的这三项由下方真实生产解析器验证，其余继承字段照常与基线比较。
        if ($name -eq 'BDP_Projectile_Variable_Explosive' -and $field -in $runtimeResolvedFields) { continue }
        $key = "$name/$field"
        $expected = if ($approvedChanges.ContainsKey($key)) { $approvedChanges[$key] } else { $previous[$field] }
        Assert-Ranged ($current[$field] -eq $expected) "超出需求的投射物参数变化：$key，预期=$expected，实际=$($current[$field])"
    }
}
Assert-Ranged ([single](Get-Projectile 'BDP_Projectile_Tracking').damageAmountBase -eq 8) '猎犬基础伤害应为8，当前定义尚未调整。'
Assert-Ranged ([single](Get-Projectile 'BDP_Projectile_BodyCollapse').speed -eq 20) '卵之冠速度应为20。'
Assert-Ranged ([single](Get-Projectile 'BDP_Projectile_SniperStandard').damageAmountBase -eq 40) '朱鹮伤害应为40。'
$explosive = Get-Projectile 'BDP_Projectile_Explosive'
Assert-Ranged ([single]$explosive.damageAmountBase -eq 6 -and [single]$explosive.armorPenetrationBase -eq 0 -and [decimal]$explosive.explosionRadius -eq 1.9 -and [single]$explosive.speed -eq 80) '美特拉应为6／0／1.9／80。'
Initialize-RangedVersionTest $RimWorldVersion
$tomahawkNode = $projectiles.SelectSingleNode("/Defs/ThingDef[defName='BDP_Projectile_Variable_Explosive']")
Assert-Ranged ($tomahawkNode.ParentName -eq 'BDP_Projectile_Variable_Base' -and $null -eq $tomahawkNode.projectile.SelectSingleNode('speed|stoppingPower')) '战斧弹道必须直接继承毒蛇，不能保存重复弹速。'
# 离线只创建字段解析用的数据容器，绕过ThingDef构造时的Unity贴图初始化；这不是游戏Def加载验收。
$source = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Verse.ThingDef]); $sourceProjectile = [Verse.ProjectileProperties]::new()
$target = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Verse.ThingDef]); $targetProjectile = [Verse.ProjectileProperties]::new()
[Verse.ThingDef].GetField('projectile').SetValue($source, $sourceProjectile)
[Verse.ThingDef].GetField('projectile').SetValue($target, $targetProjectile)
# 从实际共享定义读取战斧继承值，不能手填120掩盖来源弹速误改。
$targetProjectile.speed = [single](Get-ProjectileScalars $tomahawkNode $projectiles)['speed']
$damage = [Verse.ProjectileProperties].GetField('damageAmountBase', 'Instance,NonPublic')
$armor = [Verse.ProjectileProperties].GetField('armorPenetrationBase', 'Instance,NonPublic')
$resolver = $RangedContent.GetType('BDP.Content.Combos.TomahawkProjectileDefinitionResolver', $true).GetMethod('Apply')
foreach ($values in @(@(6,0,1.9), @(23,0.75,3.5))) {
    $damage.SetValue($sourceProjectile, [int]$values[0]); $armor.SetValue($sourceProjectile, [single]$values[1])
    $sourceProjectile.explosionRadius = [single]$values[2]
    $resolver.Invoke($null, @($target,$source))
    Assert-Ranged ($damage.GetValue($targetProjectile) -eq $values[0] -and $armor.GetValue($targetProjectile) -eq [single]$values[1] -and $targetProjectile.explosionRadius -eq [single]$values[2]) '战斧解析器必须实际跟随来源改变。'
    Assert-Ranged ($targetProjectile.speed -eq 120) '美特拉数据不能覆盖战斧继承的毒蛇弹速。'
}
[xml]$presets = Get-Content (Join-Path $modRoot 'Common/Content/Defs/ChipManufacturingPresetDef/Presets.xml') -Raw
[xml]$baselinePresets = (git -C $modRoot show '00a46b060:模组工程/BorderDefenseProtocol/Common/Content/Defs/ChipManufacturingPresetDef/Presets.xml') -join "`n"
Assert-Ranged ($LASTEXITCODE -eq 0) '无法读取射击配置基线。'
$attackFields = '//VerbProps | //Execution | //Trion | //ProjectileOverrides'
Assert-Ranged ((@($presets.SelectNodes($attackFields) | ForEach-Object OuterXml) -join "`n") -eq
    (@($baselinePresets.SelectNodes($attackFields) | ForEach-Object OuterXml) -join "`n")) '基础武装的精度、射程、连射、节奏或费用被意外修改。'
Assert-Ranged ($null -ne $presets.SelectSingleNode("//Entries/li[Id='toki_primary']/RangedModules/li[moduleDef='BDP_VitalHitModule']")) '朱鹮必须挂载现有优先要害模块。'
[xml]$hornet = Get-Content (Join-Path $modRoot 'Common/Content/Defs/ComboDef/Hornet.xml') -Raw
$houndEntry = $presets.SelectSingleNode("/Defs/*[defName='BDP_ChipFunction_Hound']//Entries/li[Id='tracking_primary']")
$hornetEntry = $hornet.SelectSingleNode('/Defs/*/Expression/Entries/li')
$houndModules = @($houndEntry.RangedModules.li | ForEach-Object moduleDef)
$hornetModules = @($hornetEntry.RangedModules.li | ForEach-Object moduleDef)
Assert-Ranged (($houndModules -join ',') -eq 'BDP_HomingModule_Weak,BDP_HighAngleShootingModule') '猎犬必须按追踪、高角射击的顺序使用弱追踪。'
Assert-Ranged (($hornetModules -join ',') -eq 'BDP_HomingModule_Strong,BDP_HighAngleShootingModule') '黄蜂必须按追踪、高角射击的顺序使用强追踪。'
Assert-Ranged ($null -eq $hornetEntry.VerbProps) '黄蜂不应另写弹种、连射等覆盖参数。'
[xml]$routes = Get-Content (Join-Path $modRoot 'Common/Content/Defs/RangedModuleDef/RoutePath.xml') -Raw
Assert-Ranged ($null -eq $routes.SelectSingleNode("/Defs/*[defName='BDP_RoutePathModule_ManualSingle']")) '猎犬和黄蜂的单锚点路线模块定义必须已移除。'
# 使用生产组合字段解析器执行来自共享定义的跟随声明，并变更第一来源。
$entryType = $RangedCore.GetType('BDP.Core.Combos.ComboExpressionEntryConfig', $true)
$entry = [Activator]::CreateInstance($entryType)
$resolutionField = $entryType.GetField('VerbPropsResolve')
$resolution = [Activator]::CreateInstance($resolutionField.FieldType)
foreach ($node in $hornetEntry.VerbPropsResolve.ChildNodes) {
    if ($node.NodeType -ne 'Element') { continue }
    $field = $resolution.GetType().GetField($node.Name)
    $enumType = [Nullable]::GetUnderlyingType($field.FieldType)
    $field.SetValue($resolution, [Enum]::Parse($enumType, $node.InnerText))
}
$resolutionField.SetValue($entry, $resolution)
$formalType = $RangedCore.GetType('BDP.Core.Expressions.FormalExpressionResult', $true)
$firstSource = [Activator]::CreateInstance($formalType)
$verb = [Verse.VerbProperties]::new()
$formalType.GetProperty('VerbProps').SetValue($firstSource, $verb)
$contractType = $RangedCore.GetType('BDP.Core.Combos.ComboDefinitionContractResolver', $true)
$contract = [Activator]::CreateInstance($contractType)
$specFactory = $RangedCore.GetType('BDP.Core.Expressions.ResolvedVerbSpecFactory', $true)
$resolveSpec = $specFactory.GetMethod('ResolveComboSpec', [Reflection.BindingFlags]'Static,NonPublic')
foreach ($burst in @(1,7)) {
    $verb.burstShotCount = $burst; $verb.range = [single](40 + $burst)
    $verb.defaultProjectile = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Verse.ThingDef])
    $verb.minRange = [single]($burst / 10.0); $verb.warmupTime = [single]($burst / 20.0)
    $verb.ticksBetweenBurstShots = 10 + $burst; $verb.defaultCooldownTime = [single]($burst / 30.0)
    $verb.accuracyTouch = [single]0.98; $verb.accuracyShort = [single]0.87
    $verb.accuracyMedium = [single]0.76; $verb.accuracyLong = [single]0.65
    $resolved = $contractType.GetMethod('ResolveVerbProps').Invoke($contract, @($entry,$firstSource,$firstSource))
    $resolvedBurst = $resolved.GetType().GetField('BurstShotCount').GetValue($resolved)
    $resolvedRange = $resolved.GetType().GetField('Range').GetValue($resolved)
    Assert-Ranged ($resolvedBurst.GetType().GetField('ResolvedValue').GetValue($resolvedBurst) -eq $burst -and $resolvedRange.GetType().GetField('ResolvedValue').GetValue($resolvedRange) -eq (40+$burst)) '黄蜂运行时连射与射程没有随猎犬来源改变。'
    # 继续走生产规格生成入口，验证未声明的弹种和其他参数也确实保留第一来源。
    $spec = $resolveSpec.Invoke($null, @($verb,$null,$resolved,$null))
    Assert-Ranged ([object]::ReferenceEquals($spec.GetType().GetProperty('ProjectileDef').GetValue($spec), $verb.defaultProjectile)) '黄蜂投射物没有随猎犬来源改变。'
    $scalarFields = @{
        Range='range'; MinRange='minRange'; WarmupTime='warmupTime'; BurstShotCount='burstShotCount'
        TicksBetweenBurstShots='ticksBetweenBurstShots'; DefaultCooldownTime='defaultCooldownTime'
        ForcedMissRadius='ForcedMissRadius'; AccuracyTouch='accuracyTouch'; AccuracyShort='accuracyShort'
        AccuracyMedium='accuracyMedium'; AccuracyLong='accuracyLong'
    }
    foreach ($field in $scalarFields.Keys) {
        Assert-Ranged ($spec.GetType().GetProperty($field).GetValue($spec) -eq $verb.($scalarFields[$field])) "黄蜂参数未跟随猎犬：$field"
    }
}
Write-Output "通过：$RimWorldVersion 全部投射物基线范围、基础射击参数保留、六项配置、战斧来源及黄蜂完整规格跟随。"
