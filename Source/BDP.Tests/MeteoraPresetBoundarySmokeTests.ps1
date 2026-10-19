$ErrorActionPreference = 'Stop'

$modRoot = Join-Path $PSScriptRoot '..\..'
$presetPath = Join-Path $modRoot 'Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$projectilePath = Join-Path $modRoot 'Common\Content\Defs\ThingDef\Projectiles\Projectiles.xml'
$languagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ThingDef\BDP_Things.xml'
if (-not (Test-Path -LiteralPath $presetPath)) {
throw ('缺少芯片功能模板 XML：' + $presetPath)
}
if (-not (Test-Path -LiteralPath $projectilePath)) {
    throw ('缺少投射物 XML：' + $projectilePath)
}
if (-not (Test-Path -LiteralPath $languagePath)) {
    throw ('缺少简体中文语言包 XML：' + $languagePath)
}

$xml = [xml](Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8)
$projectileXml = [xml](Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8)
$languageXml = [xml](Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8)
$meteora = $xml.SelectSingleNode("//*[defName='BDP_ChipFunction_Meteora']")
$asteroid = $xml.SelectSingleNode("//*[defName='BDP_ChipFunction_Asteroid']")
$explosiveProjectile = $projectileXml.SelectSingleNode("//*[defName='BDP_Projectile_Explosive']")
if ($null -eq $meteora -or $null -eq $asteroid) {
throw '美特拉或小行星芯片功能模板不存在。'
}
if ($null -eq $explosiveProjectile) {
    throw '炸裂弹投射物 Def 不存在。'
}

if ($explosiveProjectile.thingClass -ne 'BDP.Core.Projectiles.BdpProjectile') {
    throw '炸裂弹必须继续使用 BdpProjectile，以保留 BDP 投射物协议。'
}

if ($explosiveProjectile.projectile.damageDef -ne 'Bomb') {
    throw '炸裂弹必须使用 Bomb 伤害定义。'
}

if ([int]$explosiveProjectile.projectile.damageAmountBase -ne 6) {
    throw '炸裂弹的基础伤害必须按本次平衡为 6。'
}

if ([math]::Abs(([double]$explosiveProjectile.projectile.armorPenetrationBase) - 0.0) -gt 0.0001) {
    throw '炸裂弹的护甲穿透必须为 0。'
}

if ([math]::Abs(([double]$explosiveProjectile.projectile.explosionRadius) - 1.9) -gt 0.0001) {
    throw '炸裂弹的爆炸半径必须为 1.9。'
}

$localizedProjectileLabel = $languageXml.SelectSingleNode("//*[local-name()='BDP_Projectile_Explosive.label']")
if ($null -eq $localizedProjectileLabel -or $localizedProjectileLabel.InnerText -ne '炸裂弹') {
    throw '炸裂弹必须在简体中文语言包中显示为“炸裂弹”。'
}

if ($meteora.profession -ne $asteroid.profession) {
    throw '美特拉必须沿用小行星的职业分支。'
}

if ($meteora.config.Loadout.SlotRegion -ne $asteroid.config.Loadout.SlotRegion) {
    throw '美特拉必须沿用小行星的槽位区域。'
}

if ($meteora.label -ne '美特拉' -or $meteora.config.Expression.Entries.li.DisplayLabel -ne '美特拉') {
    throw '美特拉的玩家可见名称不得包含英文标识括号。'
}

$meteoraEntry = $meteora.config.Expression.Entries.li
if ($meteoraEntry.VerbProps.defaultProjectile -ne 'BDP_Projectile_Explosive') {
    throw '美特拉必须使用炸裂弹投射物。'
}

$moduleNames = @($meteoraEntry.SelectNodes('./RangedModules/li') | ForEach-Object { $_.moduleDef })
if ($moduleNames.Count -gt 0) {
    throw '美特拉的固定爆炸必须由炸裂弹投射物 Def 提供，不得继续挂载远程模块。'
}

Write-Output 'MeteoraPresetBoundarySmokeTests PASS'
