$ErrorActionPreference = "Stop"

# 断言指定条件成立。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$projectilePath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Projectiles\FujinRangedStrike.xml"

Assert-True (Test-Path -LiteralPath $projectilePath -PathType Leaf) ("风刃投射物 Def 不存在：" + $projectilePath)

[xml]$projectileXml = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$projectile = $projectileXml.Defs.SelectSingleNode("*[defName='BDP_Projectile_FujinRangedStrike']")

Assert-True ($null -ne $projectile) "缺少风刃投射物 Def。"
Assert-True ([int]$projectile.projectile.damageAmountBase -eq 40) `
    "风刃投射物的基础伤害必须为 40。"

Write-Output "FujinProjectileDamageAuthoringSmokeTests PASS"
