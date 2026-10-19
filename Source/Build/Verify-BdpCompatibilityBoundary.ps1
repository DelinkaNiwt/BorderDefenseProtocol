# 检查 BDP 外部兼容程序集只依赖 Core，并且不硬引用外部兼容模组。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CompatibilityProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$CompatibilityAssemblyPath,

    [Parameter(Mandatory = $true)]
    [string]$ModSourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$CoreAssemblyPath
)

$ErrorActionPreference = "Stop"

function Resolve-RequiredPath
{
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Label 不存在：$Path" }
    return [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
}

$projectPath = Resolve-RequiredPath $CompatibilityProjectPath "Compatibility 工程"
$assemblyPath = Resolve-RequiredPath $CompatibilityAssemblyPath "Compatibility 程序集"
$sourceRoot = Resolve-RequiredPath $ModSourceRoot "模组源码根目录"
$coreAssembly = Resolve-RequiredPath $CoreAssemblyPath "Core 程序集"

[xml]$projectXml = Get-Content -Raw -LiteralPath $projectPath
$ns = New-Object System.Xml.XmlNamespaceManager($projectXml.NameTable)
$ns.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
$references = @($projectXml.SelectNodes("//msb:ProjectReference", $ns) | ForEach-Object { $_.Include })
if ($references.Count -ne 1 -or $references[0] -ne "..\BDP\BDP.csproj")
{
    throw "Compatibility 工程必须且只能通过工程引用依赖 BDP.Core。"
}

$assemblyBytes = [IO.File]::ReadAllBytes($assemblyPath)
# PowerShell Core（跨平台 PowerShell）已移除 ReflectionOnlyLoad；只加载字节仍足以读取引用表，且不会执行程序集入口。
$assembly = [Reflection.Assembly]::Load($assemblyBytes)
$referenceNames = @($assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
if ($referenceNames -contains "BDP.Content" -or $referenceNames -contains "BDP.Development")
{
    throw "Compatibility 程序集不得依赖 BDP.Content 或 BDP.Development。"
}
if ($referenceNames -contains "NiceInventoryTab")
{
    throw "Compatibility 程序集不得硬引用 NiceInventoryTab。"
}

$compatSource = Join-Path ([IO.Path]::GetDirectoryName($projectPath)) ""
$sourceFiles = @(Get-ChildItem -LiteralPath $compatSource -Recurse -File -Filter "*.cs")
foreach ($file in $sourceFiles)
{
    $text = Get-Content -Raw -LiteralPath $file.FullName
    if ($text -match "using\s+NiceInventoryTab|Andromeda\.NiceInventoryTab\.[A-Za-z_]")
    {
        throw "Compatibility 源码存在 NIT 编译时类型依赖：$($file.FullName)"
    }
}

if (-not (Test-Path -LiteralPath $coreAssembly))
{
    throw "Core 程序集路径无效：$coreAssembly"
}

Write-Host "PASS: BDP.Compatibility.dll 依赖边界成立。"
