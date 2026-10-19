# 触发体自动攻击显示快照边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$informationRoot = Join-Path $modRoot "Source\BDP\Core\Information"
$snapshotPath = Join-Path $informationRoot "TriggerBodyAutomaticAttackDisplaySnapshot.cs"
$providerPath = Join-Path $informationRoot "TriggerBodyAutomaticAttackDisplayProvider.cs"
$publisherPath = Join-Path $informationRoot "TriggerBodyWeaponInfoPublisher.cs"

Assert-True (Test-Path -LiteralPath $snapshotPath) "缺少自动攻击显示快照类型。"
Assert-True (Test-Path -LiteralPath $providerPath) "缺少自动攻击显示快照提供器。"

$modelText = (Get-Utf8Text $snapshotPath) + "`n" + (Get-Utf8Text $providerPath)
foreach ($marker in @(
    "class TriggerBodyAutomaticAttackDisplaySnapshot",
    "ProjectionVersion",
    "WeaponMode",
    "Damage",
    "ArmorPenetration",
    "MeleeAttackCount",
    "MeleeAttackIntervalSeconds",
    "MeleeAverageDps"
)) {
    Assert-True ($modelText -match [regex]::Escape($marker)) "快照缺少字段或类型标记：$marker"
}

Assert-True ($modelText -match "IReadOnlyList") "快照的集合字段必须是只读集合。"
Assert-True ($modelText -notmatch "NiceInventoryTab|Andromeda\.NiceInventoryTab") "Core 快照不得依赖 NIT 名称。"

$publisherText = Get-Utf8Text $publisherPath
Assert-True ($publisherText -match "TriggerBodyAutomaticAttackDisplayProvider") "原版信息发布器必须消费共享属性快照提供器。"
Assert-True ($publisherText -notmatch "Parse.*StatDrawEntry|StatDrawEntry.*Parse") "原版信息发布器不得从文字条目反解析属性。"

Write-Host "PASS: 触发体自动攻击显示快照边界成立。"
