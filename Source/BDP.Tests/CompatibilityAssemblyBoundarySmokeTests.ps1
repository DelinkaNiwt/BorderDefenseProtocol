# BDP 外部兼容程序集边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$compatRoot = Join-Path $modRoot "Source\BDP.Compatibility"
$projectPath = Join-Path $compatRoot "BDP.Compatibility.csproj"
$bootstrapPath = Join-Path $compatRoot "CompatibilityBootstrap.cs"

Assert-True (Test-Path -LiteralPath $projectPath) "缺少 BDP.Compatibility 工程。"
Assert-True (Test-Path -LiteralPath $bootstrapPath) "缺少兼容层启动器。"

[xml]$projectXml = Get-Content -Raw -LiteralPath $projectPath
$projectText = Get-Utf8Text $projectPath
Assert-True ($projectText -match "AssemblyName>BDP\.Compatibility<") "兼容程序集名称错误。"
Assert-True ($projectText -match "TargetFrameworkVersion>v4\.8<") "兼容程序集目标框架错误。"
Assert-True ($projectText -match "BDP\\BDP\.csproj") "兼容程序集必须依赖 BDP.Core 工程。"
Assert-True ($projectText -notmatch "BDP\.Content|BDP\.Development|NiceInventoryTab\.dll") "兼容程序集不得硬依赖 Content、Development 或 NIT DLL。"

$sourceText = (Get-ChildItem -LiteralPath $compatRoot -Recurse -File -Filter "*.cs" | ForEach-Object { Get-Utf8Text $_.FullName }) -join "`n"
Assert-True ($sourceText -notmatch "using\s+NiceInventoryTab") "兼容层不得编译引用 NIT 命名空间。"
Assert-True ($sourceText -notmatch "Andromeda\.NiceInventoryTab\.[A-Za-z_]") "NIT 类型不得进入兼容层静态类型签名。"
Assert-True ($sourceText -match "AccessTools|Assembly\.GetType|GetAssemblies") "兼容层必须通过运行时发现外部模组。"

Write-Host "PASS: BDP.Compatibility 程序集边界成立。"
