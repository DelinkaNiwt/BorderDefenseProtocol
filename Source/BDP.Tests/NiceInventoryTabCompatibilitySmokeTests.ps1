# NIT 可选兼容适配测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$nitRoot = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab"
$requiredFiles = @(
    "NiceInventoryTabAdapter.cs",
    "NiceInventoryTabDiscovery.cs",
    "NiceInventoryTabPatches.cs",
    "NiceInventoryTabProjectionAdapter.cs"
)

foreach ($fileName in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $nitRoot $fileName)) "缺少 NIT 适配文件：$fileName"
}

$text = ($requiredFiles | ForEach-Object { Get-Utf8Text (Join-Path $nitRoot $_) }) -join "`n"
foreach ($marker in @(
    "Andromeda.NiceInventoryTab",
    "EquippedItem",
    "UpdateStats",
    "DrawWeaponInfo",
    "BDP",
    "Fallback",
    "IsMelee",
    "IsRanged"
)) {
    Assert-True ($text -match [regex]::Escape($marker)) "NIT 适配缺少关键边界：$marker"
}

Assert-True ($text -match "GetType|AccessTools|GetMethod") "NIT 适配必须运行时发现类型和方法。"
Assert-True ($text -match "catch") "NIT 适配必须对外部模组异常回退。"
Assert-True ($text -notmatch "ThingDef\.IsRangedWeapon\s*=|ThingDef\.IsMeleeWeapon\s*=") "NIT 适配不得改写原版武器分类。"
Assert-True ($text -notmatch "SpecialDisplayStats") "NIT 适配不得通过信息面板入口递归读取。"

Write-Host "PASS: NIT 可选兼容适配边界成立。"
