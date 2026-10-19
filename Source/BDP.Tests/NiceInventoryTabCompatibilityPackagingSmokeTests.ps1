# NIT 兼容程序集发布包装测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$assemblyRoot = Join-Path $modRoot "1.6\Assemblies"
$compatAssembly = Join-Path $assemblyRoot "BDP.Compatibility.dll"
$nitAssembly = Join-Path $assemblyRoot "NiceInventoryTab.dll"

Assert-True (Test-Path -LiteralPath $compatAssembly) "缺少正式发布的 BDP.Compatibility.dll。"
Assert-True (-not (Test-Path -LiteralPath $nitAssembly)) "BDP 发布目录不得打包 NIT DLL。"

Write-Host "PASS: NIT 兼容程序集发布包装边界成立。"
