# Core、Content 对 Development 的二进制与计划独立性冒烟测试。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreAssemblyPath = Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"
$contentAssemblyPath = Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"
$chipPlanPath = Join-Path $modRoot "docs\plans\2026-08-10-BDP芯片制造台重做实施计划.md"

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-AssemblyReferenceNames
{
    param([string]$AssemblyPath)
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($AssemblyPath))
    return @($assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
}

Assert-True (Test-Path -LiteralPath $coreAssemblyPath) "缺少 BDP.Core.dll。"
Assert-True (Test-Path -LiteralPath $contentAssemblyPath) "缺少 BDP.Content.dll。"
Assert-True (Test-Path -LiteralPath $chipPlanPath) "缺少芯片制造台实施计划。"

$coreReferences = @(Get-AssemblyReferenceNames $coreAssemblyPath)
$contentReferences = @(Get-AssemblyReferenceNames $contentAssemblyPath)
$chipPlanText = Get-Content -Raw -Encoding UTF8 -LiteralPath $chipPlanPath

Assert-True ($coreReferences -notcontains "BDP.Development") "Core DLL 不得引用 Development。"
Assert-True ($contentReferences -notcontains "BDP.Development") "Content DLL 不得引用 Development。"
Assert-True ($chipPlanText -match 'BDP开发辅助程序集实施计划') "芯片制造台计划必须声明开发辅助程序集为前置。"
Assert-True ($chipPlanText -match 'Source/BDP\.Development/ChipManufacturing') "游戏内诊断和临时测试必须落到 Development。"
Assert-True ($chipPlanText -match 'Source/BDP\.Tests') "PowerShell 自动测试必须继续属于 BDP.Tests。"
Assert-True ($chipPlanText -notmatch 'DevHarness') "芯片制造台计划不得重新依赖已退役测试模组。"
Assert-True ($chipPlanText -match '删除 Development DLL 后主模组独立加载') "最终验收必须覆盖移除 Development 后的正式独立运行。"

Write-Host "PASS: Core、Content 二进制与芯片实施计划均不反向依赖 Development。"
