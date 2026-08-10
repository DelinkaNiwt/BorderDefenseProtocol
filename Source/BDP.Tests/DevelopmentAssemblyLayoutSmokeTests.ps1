# BDP 开发辅助程序集独立布局冒烟测试。
# 本测试只约束 Task 1 的工程依赖、输出目录与加载顺序。

$ErrorActionPreference = "Stop"

$mainModRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreProjectPath = Join-Path $mainModRoot "Source\BDP\BDP.csproj"
$contentProjectPath = Join-Path $mainModRoot "Source\BDP.Content\BDP.Content.csproj"
$developmentProjectPath = Join-Path $mainModRoot "Source\BDP.Development\BDP.Development.csproj"
$loadFoldersPath = Join-Path $mainModRoot "LoadFolders.xml"
$developmentKeepPath = Join-Path $mainModRoot "1.6\Development\.keep"
$developmentAssembliesPath = Join-Path $mainModRoot "1.6\Development\Assemblies"

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

function Assert-Sequence
{
    param(
        [object[]]$Actual,
        [object[]]$Expected,
        [string]$Message
    )

    Assert-True ($Actual.Count -eq $Expected.Count) $Message
    for ($index = 0; $index -lt $Expected.Count; $index++)
    {
        Assert-True ([string]$Actual[$index] -eq [string]$Expected[$index]) $Message
    }
}

function Read-Project
{
    param([string]$Path)

    [xml]$project = Get-Content -Raw -LiteralPath $Path -Encoding UTF8
    $namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
    $namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

    return @{
        Document = $project
        Namespace = $namespace
    }
}

Assert-True (Test-Path -LiteralPath $coreProjectPath) "缺少 BDP.Core 工程。"
Assert-True (Test-Path -LiteralPath $contentProjectPath) "缺少 BDP.Content 工程。"
Assert-True (Test-Path -LiteralPath $developmentProjectPath) "缺少 BDP.Development 工程。"
Assert-True (Test-Path -LiteralPath $loadFoldersPath) "缺少主模组加载目录配置。"
Assert-True (Test-Path -LiteralPath $developmentKeepPath) "开发加载目录必须保留空目录标记。"

$coreProject = Read-Project $coreProjectPath
$contentProject = Read-Project $contentProjectPath
$developmentProject = Read-Project $developmentProjectPath

$coreProjectReferences = @($coreProject.Document.SelectNodes("//msb:ProjectReference", $coreProject.Namespace))
$contentProjectReferences = @($contentProject.Document.SelectNodes("//msb:ProjectReference", $contentProject.Namespace) | ForEach-Object { $_.Include })
$developmentAssemblyName = $developmentProject.Document.SelectSingleNode("//msb:AssemblyName", $developmentProject.Namespace).InnerText
$developmentRootNamespace = $developmentProject.Document.SelectSingleNode("//msb:RootNamespace", $developmentProject.Namespace).InnerText
$developmentFramework = $developmentProject.Document.SelectSingleNode("//msb:TargetFrameworkVersion", $developmentProject.Namespace).InnerText
$developmentLanguageVersion = $developmentProject.Document.SelectSingleNode("//msb:LangVersion", $developmentProject.Namespace).InnerText
$developmentOutputPaths = @($developmentProject.Document.SelectNodes("//msb:OutputPath", $developmentProject.Namespace) | ForEach-Object { $_.InnerText })
$developmentProjectReferences = @($developmentProject.Document.SelectNodes("//msb:ProjectReference", $developmentProject.Namespace) | ForEach-Object { $_.Include })
$developmentPrivateValues = @($developmentProject.Document.SelectNodes("//msb:ProjectReference/msb:Private", $developmentProject.Namespace) | ForEach-Object { $_.InnerText })
$developmentCleanupTarget = $developmentProject.Document.SelectSingleNode("//msb:Target[@Name='RemoveCopiedMainModAssemblies']", $developmentProject.Namespace)

Assert-True ($coreProjectReferences.Count -eq 0) "Core 不得引用本模组的其它项目。"
Assert-Sequence $contentProjectReferences @("..\BDP\BDP.csproj") "Content 必须且只能单向引用 Core。"
Assert-True ($developmentAssemblyName -eq "BDP.Development") "开发程序集名称错误。"
Assert-True ($developmentRootNamespace -eq "BDP.Development") "开发程序集根命名空间错误。"
Assert-True ($developmentFramework -eq "v4.8") "开发程序集必须使用 .NET Framework 4.8。"
Assert-True ($developmentLanguageVersion -eq "7.3") "开发程序集必须使用 C# 7.3。"
Assert-True ($developmentOutputPaths.Count -eq 2) "开发工程应同时声明发布和调试输出目录。"
Assert-True (($developmentOutputPaths | Where-Object { $_ -ne "..\..\1.6\Development\Assemblies\" }).Count -eq 0) "开发程序集必须输出到独立 Development 目录。"
Assert-Sequence $developmentProjectReferences @("..\BDP\BDP.csproj", "..\BDP.Content\BDP.Content.csproj") "Development 必须且只能单向引用 Core 和 Content。"
Assert-True ($developmentPrivateValues.Count -eq 2) "Development 的两个项目引用都必须声明本地复制策略。"
Assert-True (($developmentPrivateValues | Where-Object { $_ -ne "False" }).Count -eq 0) "Development 不得把 Core 或 Content 复制到开发目录。"
Assert-True ($null -ne $developmentCleanupTarget) "Development 缺少构建后主模组程序集清理目标。"
Assert-True ($developmentCleanupTarget.OuterXml -match "BDP\.Core\.dll") "Development 清理目标缺少 BDP.Core.dll。"
Assert-True ($developmentCleanupTarget.OuterXml -match "BDP\.Core\.pdb") "Development 清理目标缺少 BDP.Core.pdb。"
Assert-True ($developmentCleanupTarget.OuterXml -match "BDP\.Content\.dll") "Development 清理目标缺少 BDP.Content.dll。"
Assert-True ($developmentCleanupTarget.OuterXml -match "BDP\.Content\.pdb") "Development 清理目标缺少 BDP.Content.pdb。"

[xml]$loadFolders = Get-Content -Raw -LiteralPath $loadFoldersPath -Encoding UTF8
$loadedFolders = @($loadFolders.loadFolders.'v1.6'.li | ForEach-Object { [string]$_ })
Assert-Sequence $loadedFolders @("/", "1.6", "1.6/Content", "1.6/Development") "主模组必须在最后加载独立开发目录。"

Assert-True (Test-Path -LiteralPath $developmentAssembliesPath) "开发程序集输出目录尚未生成。"
$developmentAssemblyNames = @(Get-ChildItem -LiteralPath $developmentAssembliesPath -File | ForEach-Object { $_.Name } | Sort-Object)
$unexpectedDevelopmentAssemblies = @($developmentAssemblyNames | Where-Object { $_ -notin @("BDP.Development.dll", "BDP.Development.pdb") })
Assert-True ($unexpectedDevelopmentAssemblies.Count -eq 0) "开发输出目录含有计划外文件：$($unexpectedDevelopmentAssemblies -join ', ')。"
Assert-True ($developmentAssemblyNames -contains "BDP.Development.dll") "开发输出目录缺少 BDP.Development.dll。"

Write-Host "PASS: BDP.Development 工程、依赖方向、输出目录与加载顺序符合 Task 1 契约。"
