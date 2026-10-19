# 只读检查 BDP 正式发布目录是否仍残留开发运行产物。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModRoot,

    # 正式发布检查必须与构建及打包的目标版本一致。
    [Parameter(Mandatory = $true)]
    [ValidateSet("1.6", "1.7")]
    [string]$RimWorldVersion
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ModRoot))
{
    throw "模组根目录不存在：$ModRoot"
}

$resolvedModRoot = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ModRoot).Path)
$assembliesRoot = Join-Path $resolvedModRoot "$RimWorldVersion\Assemblies"
if (-not (Test-Path -LiteralPath $assembliesRoot))
{
    throw "正式程序集目录不存在：$assembliesRoot"
}

$assemblyFiles = @(Get-ChildItem -LiteralPath $assembliesRoot -File)
$requiredAssemblies = @("BDP.Core.dll", "BDP.Content.dll")
foreach ($requiredAssembly in $requiredAssemblies)
{
    if ($assemblyFiles.Name -notcontains $requiredAssembly)
    {
        throw "正式程序集目录缺少：$requiredAssembly"
    }
}

$allowedAssemblyFiles = @(
    "BDP.Core.dll", "BDP.Core.pdb",
    "BDP.Content.dll", "BDP.Content.pdb",
    "BDP.Compatibility.dll", "BDP.Compatibility.pdb"
)
$residualPaths = New-Object System.Collections.Generic.List[string]
foreach ($assemblyFile in $assemblyFiles)
{
    if ($assemblyFile.Name -ne ".keep" -and $assemblyFile.Name -notin $allowedAssemblyFiles)
    {
        $residualPaths.Add($assemblyFile.FullName)
    }
}

$developmentRoots = @(
    (Join-Path $resolvedModRoot "Common\Development"),
    (Join-Path $resolvedModRoot "$RimWorldVersion\Development")
)
foreach ($developmentRoot in $developmentRoots)
{
    if (Test-Path -LiteralPath $developmentRoot)
    {
        Get-ChildItem -LiteralPath $developmentRoot -Recurse -File |
            Where-Object { $_.Name -ne ".keep" } |
            ForEach-Object { $residualPaths.Add($_.FullName) }
    }
}

if ($residualPaths.Count -gt 0)
{
    throw "正式发布目录仍含开发或计划外运行产物：`n$($residualPaths -join "`n")"
}

Write-Host "PASS: 正式发布目录只含 Core、Content、Compatibility 及可选调试符号，Development 无运行产物。"
