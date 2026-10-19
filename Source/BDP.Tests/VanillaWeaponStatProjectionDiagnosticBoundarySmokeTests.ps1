# 原版武器统计投影诊断边界测试：确认诊断只存在 Development，并且不注册临时 Def。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$diagnosticPath = Join-Path $modRoot "Source\BDP.Development\Information\VanillaWeaponStatProjectionDiagnostic.cs"
$surfacePath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurface.cs"
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceFactory.cs"
$readerPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceReader.cs"
Assert-True (Test-Path -LiteralPath $diagnosticPath) "缺少原版武器统计投影诊断器。"
Assert-True (Test-Path -LiteralPath $surfacePath) "缺少 Core 原版武器统计表面。"
Assert-True (Test-Path -LiteralPath $factoryPath) "缺少 Core 原版武器统计表面工厂。"
Assert-True (Test-Path -LiteralPath $readerPath) "缺少 Core 原版武器统计读取适配器。"

$diagnosticText = Get-Utf8Text $diagnosticPath
$surfaceText = Get-Utf8Text $surfacePath
$factoryText = Get-Utf8Text $factoryPath
$readerText = Get-Utf8Text $readerPath
foreach ($marker in @(
    "ChipExpressionContractSurfaceAccess",
    "ChipExpressionCapabilityResolver",
    "VanillaWeaponStatSurfaceFactory",
    "VanillaWeaponStatSurfaceReader",
    "DefDatabase<ThingDef>.GetNamedSilentFail",
    "DebugAction",
    "AllowedGameStates.Playing"
)) {
    Assert-True ($diagnosticText -match [regex]::Escape($marker)) ("诊断器缺少原版读取链标记：" + $marker)
}

foreach ($forbidden in @(
    "DefDatabase<ThingDef>.Add",
    "GenSpawn.Spawn",
    "Scribe.",
    "HarmonyPatch",
    "Dictionary<ThingDef"
)) {
    Assert-True ($diagnosticText -notmatch [regex]::Escape($forbidden)) ("诊断器不得包含持久化或注入设施：" + $forbidden)
}

foreach ($marker in @(
    "SpecialDisplayStats",
    "MeleeWeapon_AverageDPS",
    "MeleeWeapon_AverageArmorPenetration",
    "DefDatabase<StatDef>.GetNamedSilentFail"
)) {
    Assert-True (($factoryText + "`n" + $readerText) -match [regex]::Escape($marker)) ("Core 原版读取适配器缺少标记：" + $marker)
}

Assert-True ($surfaceText -match "ThingDef") "Core 原版统计表面必须持有原版 ThingDef 读取对象。"

foreach ($sourceRoot in @(
    (Join-Path $modRoot "Source\BDP"),
    (Join-Path $modRoot "Source\BDP.Content")
)) {
    $leaks = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter "*.cs" |
        Select-String -Pattern "VanillaWeaponStatProjectionDiagnostic" -SimpleMatch
    Assert-True ($null -eq $leaks) ("原版投影诊断器不得泄漏到 Core/Content：" + $sourceRoot)
}

Write-Output 'VanillaWeaponStatProjectionDiagnosticBoundarySmokeTests PASS'
