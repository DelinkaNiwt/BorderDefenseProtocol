# BDP Core（核心）零本模组程序集依赖构建门禁冒烟测试。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreProjectPath = Join-Path $modRoot "Source\BDP\BDP.csproj"

# 断言条件成立，否则终止专项测试。
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

[xml]$coreProject = Get-Content -Raw -LiteralPath $coreProjectPath
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($coreProject.NameTable)
$namespaceManager.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$target = $coreProject.SelectSingleNode(
    "//msb:Target[@Name='VerifyBdpCoreModAssemblyIsolation']",
    $namespaceManager)

Assert-True ($null -ne $target) "核心工程缺少零本模组程序集依赖构建门禁。"
Assert-True ($target.AfterTargets -eq "Build") "核心依赖门禁必须在 Build（构建）完成后运行。"
Assert-True (
    $target.Condition -match "DesignTimeBuild" -and
    $target.Condition -match "!=" -and
    $target.Condition -match "true"
) "核心依赖门禁必须排除 IDE（集成开发环境）的设计器构建。"

$exec = $target.SelectSingleNode("msb:Exec", $namespaceManager)
Assert-True ($null -ne $exec) "核心依赖门禁缺少检查器执行命令。"

$command = [string]$exec.Command
Assert-True ($command -match "powershell\.exe") "核心依赖门禁必须用 Windows PowerShell 5.1 执行。"
Assert-True ($command -match "Verify-BdpCoreIsolation\.ps1") "核心依赖门禁没有调用正式检查器。"
Assert-True ($command -match "-CoreProjectPath") "核心依赖门禁没有传入核心工程路径。"
Assert-True ($command -match "-CoreAssemblyPath") "核心依赖门禁没有传入编译后的核心 DLL 路径。"
Assert-True ($command -match "-ModSourceRoot") "核心依赖门禁没有传入模组源码根目录。"
Assert-True ($command -match "-CoreSourceRoot") "核心依赖门禁没有传入核心源码根目录。"
Assert-True ($command -match "-CoreDefsRoot") "核心依赖门禁没有传入核心 Def（定义）根目录。"

Write-Host "PASS: BDP Core 零本模组程序集依赖检查已接入构建末尾。"
