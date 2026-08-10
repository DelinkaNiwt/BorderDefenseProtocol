# 弹射砍击不得在 Def 加载期通过字段初值访问 DefOf。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$propertiesPath = Join-Path $modRoot "Source\BDP.Content\BounceSlash\CompAbilityEffect_BounceSlash.cs"
$flyerPath = Join-Path $modRoot "Source\BDP.Content\BounceSlash\PawnFlyer_BounceSlash.cs"
$abilityDefPath = Join-Path $modRoot "1.6\Content\Defs\AbilityDef\BounceSlash.xml"

$propertiesText = Get-Utf8Text $propertiesPath
$flyerText = Get-Utf8Text $flyerPath
$abilityDefText = Get-Utf8Text $abilityDefPath

Assert-True ($propertiesText -notmatch 'DamageDef\s+damageDef\s*=\s*DamageDefOf\.') `
    "CompProperties_BounceSlash 不得在 Def 加载期使用 DamageDefOf 字段初值。"
Assert-True ($flyerText -notmatch 'DamageDef\s+damageDef\s*=\s*DamageDefOf\.') `
    "PawnFlyer_BounceSlash 不得使用 DamageDefOf 字段初值。"
Assert-True ($abilityDefText -match '<damageDef>Cut</damageDef>') `
    "弹射砍击 AbilityDef 必须显式配置 Cut 伤害类型。"
Assert-True ($flyerText -match 'damageDef\s*\?\?\s*DamageDefOf\.Cut') `
    "实际造成伤害时必须保留 Cut 运行期兜底。"

Write-Host "PASS: 弹射砍击仅在 Def 初始化完成后的运行阶段访问 DamageDefOf。"
