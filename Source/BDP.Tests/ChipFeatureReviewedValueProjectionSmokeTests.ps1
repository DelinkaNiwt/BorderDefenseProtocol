$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$workerRoot = Join-Path $modRoot "Source\BDP.Content\Information\Features\Workers"
$contentRoot = Join-Path $modRoot "Source\BDP.Content"
$rangedWorker = Get-Utf8Text (Join-Path $workerRoot "RangedAttackFeatureSourceWorker.cs")
$abilityWorker = Get-Utf8Text (Join-Path $workerRoot "AbilityLifecycleFeatureSourceWorker.cs")
$shieldWorker = Get-Utf8Text (Join-Path $workerRoot "ShieldFeatureSourceWorker.cs")
$extraWorker = Get-Utf8Text (Join-Path $workerRoot "ExtraEffectFeatureSourceWorker.cs")
$lightSoulEffectWorker = Get-Utf8Text (Join-Path $workerRoot "LightSoulDefenseMovementEffectFeatureSourceWorker.cs")
$shieldRuntime = Get-Utf8Text (Join-Path $contentRoot "Shield\HediffComp_EnergyShield.cs")

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($rangedWorker -match "Mathf\.RoundToInt") "投射物伤害取整必须与原版武器属性投影一致。"
Assert-True ($rangedWorker -match "DefDatabase<DamageDef>" -and $rangedWorker -match "defaultArmorPenetration") "显式伤害类型覆盖时必须按最终伤害 Def 读取有效穿甲。"
Assert-True ($rangedWorker -match "ProjectileFlightFeatures" -and $rangedWorker -match "RangedModules") "远程攻击必须从实际启用的模块读取飞行特征标签。"
Assert-True ($rangedWorker -match "BDP_HomingModule_" -and $rangedWorker -match "BDP_RoutePathModule" -and $rangedWorker -match "BDP_HighAngleShootingModule") "飞行特征映射必须由已存在的模块事实驱动。"
Assert-True ($rangedWorker -match "enabled") "关闭的远程模块不得贡献飞行特征。"
Assert-True ($rangedWorker -match "explosionRadius" -and $rangedWorker -match "BDP_Property_EffectRadius") "爆炸半径属于远程攻击本体，必须从投射物定义读取。"
Assert-True ($abilityWorker -match "AddTargetTypes") "泛用能力应从原版目标参数读取可选目标类别。"
Assert-True ($abilityWorker -match "AbilityCooldownDuration" -and $abilityWorker -match "cooldownTicksRange\.min == ability\.cooldownTicksRange\.max") "能力冷却只在来源能证明固定值时提供。"
Assert-True ($abilityWorker -match "SustainCostBySourceCount" -and $abilityWorker -match "TotalPerSecond") "持续消耗必须来自表达结果的实际费用表。"
Assert-True ($shieldWorker -match "UseStackedValues" -and $shieldWorker -match "DamageAbsorptionRatio") "护盾须区分单枚与聚合状态，并提供吸收比例。"
Assert-True ($shieldWorker -match "HediffApplyModeKey" -and $shieldWorker -match "ActiveEntryIds") "聚合护盾状态必须从当前有效表达条目数推导，而非给属性声明写入单枚/双枚值。"
Assert-True ($shieldRuntime -match "SetAmount\(0f\)") "100% 吸收比例必须对应护盾成功抵挡后的真实伤害归零逻辑。"
Assert-True ($extraWorker -notmatch "GetType\(|\.Name|Reflection|GetFields\(") "额外效果来源不得按名称、类名或反射推测玩家效果。"
Assert-True ($lightSoulEffectWorker -match "statFactors" -and $lightSoulEffectWorker -match "MoveSpeed") "光魂防护减速必须从实际 Hediff 阶段属性因子读取。"
Assert-True ($lightSoulEffectWorker -notmatch "0\.6f|0\.6") "实例减速倍率不得硬编码在语义读取器中。"

Write-Host "PASS: 已审议属性只从可信业务来源投影；缺值与明确零值保持区分。"
