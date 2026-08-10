# 检查 BDP.Development 只能单向依赖 Core、Content，正式层不得反向认识它。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModRoot
)

$ErrorActionPreference = "Stop"

function Resolve-RequiredPath
{
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Label 不存在：$Path" }
    return [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
}

function Get-ProjectReferences
{
    param([string]$ProjectPath)
    [xml]$project = Get-Content -Raw -Encoding UTF8 -LiteralPath $ProjectPath
    $namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
    $namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
    return @($project.SelectNodes("//msb:ProjectReference", $namespace) | ForEach-Object { [string]$_.Include })
}

function Assert-Sequence
{
    param([object[]]$Actual, [object[]]$Expected, [string]$Message)
    if ($Actual.Count -ne $Expected.Count) { throw $Message }
    for ($index = 0; $index -lt $Expected.Count; $index++)
    {
        if ([string]$Actual[$index] -ne [string]$Expected[$index]) { throw $Message }
    }
}

$resolvedModRoot = Resolve-RequiredPath $ModRoot "模组根目录"
$coreProject = Resolve-RequiredPath (Join-Path $resolvedModRoot "Source\BDP\BDP.csproj") "Core 工程"
$contentProject = Resolve-RequiredPath (Join-Path $resolvedModRoot "Source\BDP.Content\BDP.Content.csproj") "Content 工程"
$developmentProject = Resolve-RequiredPath (Join-Path $resolvedModRoot "Source\BDP.Development\BDP.Development.csproj") "Development 工程"

Assert-Sequence @(Get-ProjectReferences $coreProject) @() "Core 不得引用本模组的其它工程。"
Assert-Sequence @(Get-ProjectReferences $contentProject) @("..\BDP\BDP.csproj") "Content 必须且只能引用 Core。"
Assert-Sequence @(Get-ProjectReferences $developmentProject) @("..\BDP\BDP.csproj", "..\BDP.Content\BDP.Content.csproj") "Development 必须且只能单向引用 Core、Content。"

$formalFiles = New-Object System.Collections.Generic.List[IO.FileInfo]
$formalSourceRoots = @(
    (Join-Path $resolvedModRoot "Source\BDP"),
    (Join-Path $resolvedModRoot "Source\BDP.Content")
)
foreach ($sourceRoot in $formalSourceRoots)
{
    Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch "[\\/](obj|bin)[\\/]" } |
        ForEach-Object { $formalFiles.Add($_) }
}

$formalXmlRoots = @(
    (Join-Path $resolvedModRoot "1.6\Defs"),
    (Join-Path $resolvedModRoot "1.6\Content"),
    (Join-Path $resolvedModRoot "Languages")
)
foreach ($xmlRoot in $formalXmlRoots)
{
    if (Test-Path -LiteralPath $xmlRoot)
    {
        Get-ChildItem -LiteralPath $xmlRoot -Recurse -File -Filter "*.xml" |
            ForEach-Object { $formalFiles.Add($_) }
    }
}

$mentions = New-Object System.Collections.Generic.List[string]
foreach ($file in $formalFiles)
{
    $matchingLines = @(Select-String -LiteralPath $file.FullName -SimpleMatch "BDP.Development")
    foreach ($matchingLine in $matchingLines)
    {
        $mentions.Add("$($file.FullName):$($matchingLine.LineNumber)")
    }
}
if ($mentions.Count -gt 0)
{
    throw "正式源码或 XML 不得写出 BDP.Development：`n$($mentions -join "`n")"
}

$developmentAssemblies = Join-Path $resolvedModRoot "1.6\Development\Assemblies"
if (Test-Path -LiteralPath $developmentAssemblies)
{
    $copiedFormalAssemblies = @(Get-ChildItem -LiteralPath $developmentAssemblies -File | Where-Object {
        $_.Name -in @("BDP.Core.dll", "BDP.Core.pdb", "BDP.Content.dll", "BDP.Content.pdb")
    })
    if ($copiedFormalAssemblies.Count -gt 0)
    {
        throw "Development 输出目录含有正式程序集副本：`n$($copiedFormalAssemblies.FullName -join "`n")"
    }
}

Write-Host "PASS: BDP Development 单向依赖、正式源码/XML 和输出目录边界正确。"
