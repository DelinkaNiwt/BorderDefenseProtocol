# BDP 选择性迁移测试脚本路径冒烟测试。
# 候选业务测试应继续指向候选模组，主模组结构测试只检查正式外壳。

$ErrorActionPreference = "Stop"

$mainModRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$modsRoot = Split-Path -Parent $mainModRoot
$candidateModRoot = Join-Path $modsRoot "BorderDefenseProtocol.DevHarness"
$mainContentTests = Join-Path $mainModRoot "Source\BDP.Content.Tests"
$candidateTests = Join-Path $candidateModRoot "Source\BDP.DevHarness.Tests"
$candidateProject = Join-Path $candidateModRoot "Source\BDP.DevHarness\BDP.DevHarness.csproj"

if (Test-Path -LiteralPath $mainContentTests)
{
    throw "主模组仍包含未确认业务测试目录 Source\BDP.Content.Tests。"
}

if (-not (Test-Path -LiteralPath $candidateTests))
{
    throw "候选测试模组缺少自己的业务测试目录。"
}

if (-not (Test-Path -LiteralPath $candidateProject))
{
    throw "候选测试模组缺少 BDP.DevHarness.csproj。"
}

Write-Host "PASS: 候选业务测试与主模组正式结构测试保持物理隔离。"
