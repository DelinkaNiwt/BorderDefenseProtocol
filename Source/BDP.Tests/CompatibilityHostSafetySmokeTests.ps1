# 外部兼容宿主的安全和故障隔离测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$compatRoot = Join-Path $modRoot "Source\BDP.Compatibility"
$hostPath = Join-Path $compatRoot "CompatibilityHost.cs"
$adapterPath = Join-Path $compatRoot "IExternalModCompatibilityAdapter.cs"
$statePath = Join-Path $compatRoot "CompatibilityAdapterState.cs"
$diagnosticsPath = Join-Path $compatRoot "CompatibilityDiagnostics.cs"
$nitAdapterPath = Join-Path $compatRoot "NiceInventoryTab\NiceInventoryTabAdapter.cs"

foreach ($path in @($hostPath, $adapterPath, $statePath, $diagnosticsPath, $nitAdapterPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少兼容宿主文件：$path"
}

$text = ($hostPath, $adapterPath, $statePath, $diagnosticsPath, $nitAdapterPath | ForEach-Object { Get-Utf8Text $_ }) -join "`n"
foreach ($marker in @(
    "IExternalModCompatibilityAdapter",
    "TryInitialize",
    "Disabled",
    "Exception",
    "Once",
    "Idempot",
    "Unpatch",
    "独立 Harmony ID"
)) {
    Assert-True ($text -match [regex]::Escape($marker)) "兼容宿主缺少安全边界标记：$marker"
}

Assert-True ($text -match "catch") "兼容宿主必须捕获适配器初始化或运行时异常。"
Assert-True ($text -notmatch "Scribe\\.|ExposeData") "兼容宿主不得引入存档状态。"

Write-Host "PASS: 外部兼容宿主故障隔离边界成立。"
