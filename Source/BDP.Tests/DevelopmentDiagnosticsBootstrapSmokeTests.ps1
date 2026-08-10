# Development 诊断日志接线冒烟测试。
# 验证 Verse 日志实现只存在于开发程序集，临时补丁受开发者模式约束。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$developmentRoot = Join-Path $modRoot "Source\BDP.Development"
$bootstrapPath = Join-Path $developmentRoot "DevelopmentBootstrap.cs"
$sinkPath = Join-Path $developmentRoot "Diagnostics\VerseLogDiagnosticSink.cs"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

Assert-True (Test-Path -LiteralPath $bootstrapPath) "缺少 DevelopmentBootstrap。"
Assert-True (Test-Path -LiteralPath $sinkPath) "Development 缺少 Verse 日志接收器。"

$bootstrapText = Get-Content -Raw -Encoding UTF8 -LiteralPath $bootstrapPath
$sinkText = Get-Content -Raw -Encoding UTF8 -LiteralPath $sinkPath

Assert-True ($sinkText -match 'class\s+VerseLogDiagnosticSink\s*:\s*IBdpDiagnosticSink') `
    "Development 日志接收器必须实现 Core 中性接口。"
Assert-True ($sinkText -match '\bLog\.Message\s*\(\s*message\s*\)') `
    "Development 日志接收器必须负责实际 Verse 日志输出。"
Assert-True ($bootstrapText -match 'BdpDiagnosticSinkRegistry\.Register\s*\(') `
    "Development 启动入口必须注册日志接收器。"
Assert-True ($bootstrapText -match 'Prefs\.DevMode') `
    "Development 临时补丁必须安全读取开发者模式。"
Assert-True ($bootstrapText -match '\.PatchAll\s*\(') `
    "Development 开启开发者模式时必须允许扫描临时 Harmony 补丁。"
Assert-True ($bootstrapText -match 'try\s*\{[\s\S]*BdpDiagnosticSinkRegistry\.Register') `
    "Development 启动接线必须隔离自身异常。"

Write-Host "PASS: Development 注册实际日志接收器，并仅在开发者模式启用临时补丁。"
