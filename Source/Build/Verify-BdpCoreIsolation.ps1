# 检查 BDP Core（核心）是否依赖任何其他本模组程序集。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoreProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$CoreAssemblyPath,

    [Parameter(Mandatory = $true)]
    [string]$ModSourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$CoreSourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$CoreDefsRoot
)

$ErrorActionPreference = "Stop"

# 把输入路径解析为稳定的绝对路径，并确认目标存在。
function Resolve-RequiredPath
{
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path))
    {
        throw "$Label 不存在：$Path"
    }

    return [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
}

# 读取一个旧式 C# 工程声明的程序集身份和工程引用。
function Get-ProjectMetadata
{
    param(
        [string]$ProjectPath
    )

    [xml]$projectXml = Get-Content -Raw -LiteralPath $ProjectPath
    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($projectXml.NameTable)
    $namespaceManager.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

    $assemblyNode = $projectXml.SelectSingleNode("//msb:AssemblyName", $namespaceManager)
    $rootNamespaceNode = $projectXml.SelectSingleNode("//msb:RootNamespace", $namespaceManager)
    $referenceNodes = @($projectXml.SelectNodes("//msb:ProjectReference", $namespaceManager))

    $assemblyName = if ($null -ne $assemblyNode -and -not [string]::IsNullOrWhiteSpace($assemblyNode.InnerText))
    {
        $assemblyNode.InnerText.Trim()
    }
    else
    {
        [IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    }

    $rootNamespace = if ($null -ne $rootNamespaceNode)
    {
        $rootNamespaceNode.InnerText.Trim()
    }
    else
    {
        ""
    }

    return [PSCustomObject]@{
        ProjectPath = [IO.Path]::GetFullPath($ProjectPath)
        ProjectDirectory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($ProjectPath))
        AssemblyName = $assemblyName
        RootNamespace = $rootNamespace
        ProjectReferences = @($referenceNodes | ForEach-Object { $_.Include })
    }
}

# 从一个工程的实际 C# 源码中提取声明过的命名空间。
function Get-DeclaredNamespaces
{
    param(
        [string]$ProjectDirectory
    )

    $namespaces = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::Ordinal)
    $sourceFiles = Get-ChildItem -LiteralPath $ProjectDirectory -Filter "*.cs" -File -Recurse |
        Where-Object {
            $_.FullName -notmatch "[\\/](obj|bin)[\\/]"
        }

    foreach ($sourceFile in $sourceFiles)
    {
        $sourceText = Get-Content -Raw -LiteralPath $sourceFile.FullName
        $matches = [regex]::Matches(
            $sourceText,
            "\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)",
            [Text.RegularExpressions.RegexOptions]::Multiline)

        foreach ($match in $matches)
        {
            $null = $namespaces.Add($match.Groups[1].Value)
        }
    }

    return @($namespaces)
}

# 从 DLL 字节读取正式 AssemblyRef（程序集引用表），避免锁定发布文件。
function Get-AssemblyReferenceNames
{
    param(
        [string]$AssemblyPath
    )

    $assemblyBytes = [IO.File]::ReadAllBytes($AssemblyPath)
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($assemblyBytes)

    return @($assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
}

# 在核心源码和核心 Def XML 中寻找其他本模组的明确名称。
function Find-SpecificNameMentions
{
    param(
        [string]$SourceRoot,
        [string]$DefsRoot,
        [string[]]$SpecificNames
    )

    $results = New-Object System.Collections.Generic.List[object]
    $files = @()

    if (Test-Path -LiteralPath $SourceRoot)
    {
        $files += @(Get-ChildItem -LiteralPath $SourceRoot -Filter "*.cs" -File -Recurse |
            Where-Object { $_.FullName -notmatch "[\\/](obj|bin)[\\/]" })
    }

    if (Test-Path -LiteralPath $DefsRoot)
    {
        $files += @(Get-ChildItem -LiteralPath $DefsRoot -Filter "*.xml" -File -Recurse)
    }

    foreach ($file in $files)
    {
        $lines = @(Get-Content -LiteralPath $file.FullName)
        for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++)
        {
            foreach ($specificName in $SpecificNames)
            {
                $pattern = "(?<![A-Za-z0-9_.])" + [regex]::Escape($specificName) + "(?=\.|[^A-Za-z0-9_]|$)"
                if ($lines[$lineIndex] -match $pattern)
                {
                    $results.Add([PSCustomObject]@{
                        Name = $specificName
                        File = $file.FullName
                        Line = $lineIndex + 1
                    })
                }
            }
        }
    }

    return $results.ToArray()
}

$resolvedCoreProjectPath = Resolve-RequiredPath $CoreProjectPath "核心工程"
$resolvedCoreAssemblyPath = Resolve-RequiredPath $CoreAssemblyPath "核心程序集"
$resolvedModSourceRoot = Resolve-RequiredPath $ModSourceRoot "模组源码根目录"
$resolvedCoreSourceRoot = Resolve-RequiredPath $CoreSourceRoot "核心源码根目录"
$resolvedCoreDefsRoot = Resolve-RequiredPath $CoreDefsRoot "核心 Def 根目录"

$projectFiles = @(Get-ChildItem -LiteralPath $resolvedModSourceRoot -Filter "*.csproj" -File -Recurse)
$projectCatalog = @($projectFiles | ForEach-Object { Get-ProjectMetadata $_.FullName })
$coreProject = $projectCatalog |
    Where-Object { $_.ProjectPath.Equals($resolvedCoreProjectPath, [StringComparison]::OrdinalIgnoreCase) } |
    Select-Object -First 1

if ($null -eq $coreProject)
{
    throw "核心工程不在模组源码工程名单中：$resolvedCoreProjectPath"
}

$duplicateAssemblyNames = @($projectCatalog |
    Group-Object { $_.AssemblyName.ToUpperInvariant() } |
    Where-Object { $_.Count -gt 1 })

if ($duplicateAssemblyNames.Count -gt 0)
{
    $duplicateText = $duplicateAssemblyNames |
        ForEach-Object { ($_.Group | ForEach-Object { $_.AssemblyName }) -join ", " }
    throw "本模组存在重复程序集名称：$($duplicateText -join "; ")"
}

$otherProjects = @($projectCatalog |
    Where-Object { -not $_.ProjectPath.Equals($resolvedCoreProjectPath, [StringComparison]::OrdinalIgnoreCase) })
$otherProjectPaths = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::OrdinalIgnoreCase)
$otherAssemblyNames = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::OrdinalIgnoreCase)

foreach ($otherProject in $otherProjects)
{
    $null = $otherProjectPaths.Add($otherProject.ProjectPath)
    $null = $otherAssemblyNames.Add($otherProject.AssemblyName)
}

$violations = New-Object System.Collections.Generic.List[string]

foreach ($projectReference in $coreProject.ProjectReferences)
{
    $referencedProjectPath = [IO.Path]::GetFullPath((Join-Path $coreProject.ProjectDirectory $projectReference))
    if ($otherProjectPaths.Contains($referencedProjectPath))
    {
        $referencedProject = $otherProjects |
            Where-Object { $_.ProjectPath.Equals($referencedProjectPath, [StringComparison]::OrdinalIgnoreCase) } |
            Select-Object -First 1
        $violations.Add("工程引用：Core 工程引用了本模组程序集 $($referencedProject.AssemblyName)（$referencedProjectPath）。")
    }
}

$assemblyReferenceNames = @(Get-AssemblyReferenceNames $resolvedCoreAssemblyPath)
foreach ($assemblyReferenceName in $assemblyReferenceNames)
{
    if ($otherAssemblyNames.Contains($assemblyReferenceName))
    {
        $violations.Add("DLL 引用表：$($coreProject.AssemblyName) 引用了本模组程序集 $assemblyReferenceName。")
    }
}

$coreDeclaredNamespaces = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::Ordinal)
if (-not [string]::IsNullOrWhiteSpace($coreProject.RootNamespace))
{
    $null = $coreDeclaredNamespaces.Add($coreProject.RootNamespace)
}
foreach ($coreNamespace in @(Get-DeclaredNamespaces $coreProject.ProjectDirectory))
{
    $null = $coreDeclaredNamespaces.Add($coreNamespace)
}

$specificNames = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::Ordinal)
foreach ($otherProject in $otherProjects)
{
    if (-not [string]::IsNullOrWhiteSpace($otherProject.AssemblyName))
    {
        $null = $specificNames.Add($otherProject.AssemblyName)
    }

    if (-not [string]::IsNullOrWhiteSpace($otherProject.RootNamespace) -and
        -not $coreDeclaredNamespaces.Contains($otherProject.RootNamespace))
    {
        $null = $specificNames.Add($otherProject.RootNamespace)
    }

    foreach ($otherNamespace in @(Get-DeclaredNamespaces $otherProject.ProjectDirectory))
    {
        if (-not $coreDeclaredNamespaces.Contains($otherNamespace))
        {
            $null = $specificNames.Add($otherNamespace)
        }
    }
}

$specificMentions = @(Find-SpecificNameMentions `
    -SourceRoot $resolvedCoreSourceRoot `
    -DefsRoot $resolvedCoreDefsRoot `
    -SpecificNames @($specificNames | Sort-Object { $_.Length } -Descending))

foreach ($specificMention in $specificMentions)
{
    $violations.Add("明确名称：核心文件 $($specificMention.File):$($specificMention.Line) 写出了其他本模组专属名称 $($specificMention.Name)。")
}

if ($violations.Count -gt 0)
{
    throw "BDP Core 零本模组程序集依赖检查失败：`n$($violations -join "`n")"
}

Write-Host "PASS: BDP.Core.dll 未依赖任何其他本模组程序集。"
