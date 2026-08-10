# BDP 统一模组双程序集结构冒烟测试。
# 本测试只检查事项 01 的物理外壳，不检查后续业务重命名与参数重做。

$ErrorActionPreference = "Stop"

$mainModRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

$coreProjectPath = Join-Path $mainModRoot "Source\BDP\BDP.csproj"
$contentProjectPath = Join-Path $mainModRoot "Source\BDP.Content\BDP.Content.csproj"
$loadFoldersPath = Join-Path $mainModRoot "LoadFolders.xml"
$aboutPath = Join-Path $mainModRoot "About\About.xml"
$assembliesPath = Join-Path $mainModRoot "1.6\Assemblies"

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

Assert-True (Test-Path $coreProjectPath) "缺少核心程序集工程。"
Assert-True (Test-Path $contentProjectPath) "缺少统一模组内的内容程序集工程。"
Assert-True (Test-Path $loadFoldersPath) "缺少统一模组加载目录配置。"
Assert-True (Test-Path $aboutPath) "缺少统一模组说明。"

[xml]$coreProject = Get-Content -Raw $coreProjectPath
[xml]$contentProject = Get-Content -Raw $contentProjectPath
[xml]$loadFolders = Get-Content -Raw $loadFoldersPath
[xml]$about = Get-Content -Raw $aboutPath

$projectNamespace = New-Object System.Xml.XmlNamespaceManager($coreProject.NameTable)
$projectNamespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$coreAssemblyName = $coreProject.SelectSingleNode("//msb:AssemblyName", $projectNamespace).InnerText
Assert-True ($coreAssemblyName -eq "BDP.Core") "核心工程必须输出 BDP.Core.dll。"

$contentProjectNamespace = New-Object System.Xml.XmlNamespaceManager($contentProject.NameTable)
$contentProjectNamespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$contentAssemblyName = $contentProject.SelectSingleNode("//msb:AssemblyName", $contentProjectNamespace).InnerText
$contentOutputPaths = @($contentProject.SelectNodes("//msb:OutputPath", $contentProjectNamespace) | ForEach-Object { $_.InnerText })
$contentProjectReferences = @($contentProject.SelectNodes("//msb:ProjectReference", $contentProjectNamespace) | ForEach-Object { $_.Include })

Assert-True ($contentAssemblyName -eq "BDP.Content") "内容工程必须输出 BDP.Content.dll。"
Assert-True ($contentOutputPaths.Count -eq 2) "内容工程应同时声明发布和调试输出目录。"
Assert-True (($contentOutputPaths | Where-Object { $_ -ne "..\..\1.6\Assemblies\" }).Count -eq 0) "内容程序集必须输出到统一主模组的 Assemblies 目录。"
Assert-True ($contentProjectReferences.Count -eq 1) "内容工程应且只应引用一个本地项目。"
Assert-True ($contentProjectReferences[0] -eq "..\BDP\BDP.csproj") "内容工程必须单向引用核心工程。"

$coreReferences = @($coreProject.SelectNodes("//msb:ProjectReference", $projectNamespace))
Assert-True ($coreReferences.Count -eq 0) "核心工程不得引用内容工程。"

$loadedFolders = @($loadFolders.loadFolders.'v1.6'.li | ForEach-Object { [string]$_ })
Assert-True ($loadedFolders.Count -ge 3) "主模组缺少正式加载目录。"
Assert-True ($loadedFolders[0] -eq "/") "统一模组必须继续加载根目录。"
Assert-True ($loadedFolders[1] -eq "1.6") "统一模组必须继续加载原主模组的 1.6 目录。"
Assert-True ($loadedFolders[2] -eq "1.6/Content") "统一模组必须加载正式 Content 补充目录。"

Assert-True ($about.ModMetaData.packageId -eq "Niwt.BDP") "统一模组必须继续使用唯一正式 packageId。"
Assert-True ($about.ModMetaData.description -notmatch "测试模组|后置测试") "统一模组说明不得继续要求第二个测试模组。"

Assert-True (Test-Path (Join-Path $assembliesPath "BDP.Core.dll")) "统一模组缺少 BDP.Core.dll。"
Assert-True (Test-Path (Join-Path $assembliesPath "BDP.Content.dll")) "统一模组缺少 BDP.Content.dll。"
Assert-True (-not (Test-Path (Join-Path $assembliesPath "BDP.dll"))) "统一模组仍残留旧 BDP.dll。"
Assert-True (-not (Test-Path (Join-Path $assembliesPath "BDP.DevHarness.dll"))) "统一模组仍残留旧 BDP.DevHarness.dll。"

Write-Host "PASS: BDP 统一模组双程序集物理结构符合事项 01 契约。"
