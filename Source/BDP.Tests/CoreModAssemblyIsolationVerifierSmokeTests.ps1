# BDP Core（核心）零本模组程序集依赖检查器冒烟测试。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$verifierPath = Join-Path $modRoot "Source\Build\Verify-BdpCoreIsolation.ps1"
$coreProjectPath = Join-Path $modRoot "Source\BDP\BDP.csproj"
$coreAssemblyPath = Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"
$modSourceRoot = Join-Path $modRoot "Source"
$coreSourceRoot = Join-Path $modRoot "Source\BDP"
$coreDefsRoot = Join-Path $modRoot "1.6\Defs"

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

# 用独立 PowerShell 进程执行检查器并保留退出码与文本输出。
function Invoke-IsolationVerifier
{
    param(
        [string]$ProjectPath,
        [string]$AssemblyPath,
        [string]$SourceRoot,
        [string]$SourceCodeRoot,
        [string]$DefsRoot
    )

    $savedPreference = $ErrorActionPreference
    try
    {
        $ErrorActionPreference = "Continue"
        $output = & powershell.exe `
            -NoProfile `
            -ExecutionPolicy Bypass `
            -File $verifierPath `
            -CoreProjectPath $ProjectPath `
            -CoreAssemblyPath $AssemblyPath `
            -ModSourceRoot $SourceRoot `
            -CoreSourceRoot $SourceCodeRoot `
            -CoreDefsRoot $DefsRoot 2>&1
    }
    finally
    {
        $ErrorActionPreference = $savedPreference
    }

    return [PSCustomObject]@{
        ExitCode = $LASTEXITCODE
        Text = (($output | Out-String).Trim())
    }
}

# 以无 BOM（字节顺序标记）的 UTF-8 写入临时测试输入。
function Write-Utf8Fixture
{
    param(
        [string]$Path,
        [string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent))
    {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $Content, $encoding)
}

# 生成一个只声明程序集身份的最小旧式 C# 工程。
function New-MinimalProjectXml
{
    param(
        [string]$AssemblyName,
        [string]$RootNamespace,
        [string]$ProjectReference = ""
    )

    $referenceItem = ""
    if (-not [string]::IsNullOrWhiteSpace($ProjectReference))
    {
        $referenceItem = @"
  <ItemGroup>
    <ProjectReference Include="$ProjectReference" />
  </ItemGroup>
"@
    }

    return @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <AssemblyName>$AssemblyName</AssemblyName>
    <RootNamespace>$RootNamespace</RootNamespace>
  </PropertyGroup>
$referenceItem</Project>
"@
}

Assert-True (Test-Path -LiteralPath $verifierPath) "缺少核心零本模组程序集依赖检查器。"
Assert-True (Test-Path -LiteralPath $coreAssemblyPath) "缺少 BDP.Core.dll，无法执行真实引用检查。"

$realResult = Invoke-IsolationVerifier `
    -ProjectPath $coreProjectPath `
    -AssemblyPath $coreAssemblyPath `
    -SourceRoot $modSourceRoot `
    -SourceCodeRoot $coreSourceRoot `
    -DefsRoot $coreDefsRoot

Assert-True ($realResult.ExitCode -eq 0) "当前 Core 应通过零本模组程序集依赖检查：$($realResult.Text)"
Assert-True ($realResult.Text -match "PASS") "当前 Core 通过时应输出明确 PASS。"

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempBase ("BDP-CoreIsolation-" + [Guid]::NewGuid().ToString("N"))

try
{
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

    # 工程引用负例：Core 工程直接引用另一本模组工程。
    $projectCaseRoot = Join-Path $fixtureRoot "ProjectReferenceCase"
    $projectCaseCore = Join-Path $projectCaseRoot "Core\Core.csproj"
    $projectCaseOther = Join-Path $projectCaseRoot "Other\Other.csproj"
    $projectCaseSource = Join-Path $projectCaseRoot "Core\Source"
    $projectCaseDefs = Join-Path $projectCaseRoot "Core\Defs"
    Write-Utf8Fixture $projectCaseCore (New-MinimalProjectXml "BDP.Core" "BDP" "..\Other\Other.csproj")
    Write-Utf8Fixture $projectCaseOther (New-MinimalProjectXml "Fake.Other" "Fake.Other")
    New-Item -ItemType Directory -Path $projectCaseSource, $projectCaseDefs -Force | Out-Null

    $projectResult = Invoke-IsolationVerifier `
        -ProjectPath $projectCaseCore `
        -AssemblyPath $coreAssemblyPath `
        -SourceRoot $projectCaseRoot `
        -SourceCodeRoot $projectCaseSource `
        -DefsRoot $projectCaseDefs

    Assert-True ($projectResult.ExitCode -ne 0) "核心工程引用另一本模组工程时必须失败。"
    Assert-True ($projectResult.Text -match "工程引用" -and $projectResult.Text -match "Fake\.Other") "工程引用失败必须指出层级和目标程序集。"

    # DLL 引用负例：把 Core 已有的 Assembly-CSharp 引用临时声明为“另一本模组程序集”。
    # Content 当前不使用任何 Core 类型，因此正式 DLL 不应被强行制造无意义的 Core 引用来服务测试。
    $binaryCaseRoot = Join-Path $fixtureRoot "BinaryReferenceCase"
    $binaryCaseCore = Join-Path $binaryCaseRoot "Core\Core.csproj"
    $binaryCaseOther = Join-Path $binaryCaseRoot "Other\Other.csproj"
    $binaryCaseSource = Join-Path $binaryCaseRoot "Core\Source"
    $binaryCaseDefs = Join-Path $binaryCaseRoot "Core\Defs"
    Write-Utf8Fixture $binaryCaseCore (New-MinimalProjectXml "BDP.Core" "BDP")
    Write-Utf8Fixture $binaryCaseOther (New-MinimalProjectXml "Assembly-CSharp" "Verse")
    New-Item -ItemType Directory -Path $binaryCaseSource, $binaryCaseDefs -Force | Out-Null

    $binaryResult = Invoke-IsolationVerifier `
        -ProjectPath $binaryCaseCore `
        -AssemblyPath $coreAssemblyPath `
        -SourceRoot $binaryCaseRoot `
        -SourceCodeRoot $binaryCaseSource `
        -DefsRoot $binaryCaseDefs

    Assert-True ($binaryResult.ExitCode -ne 0) "受检 DLL 引用另一本模组 DLL 时必须失败。"
    Assert-True ($binaryResult.Text -match "DLL 引用表" -and $binaryResult.Text -match "Assembly-CSharp") "DLL 引用失败必须指出层级和目标程序集。"

    # 明确名称负例：Core 源码直接写出其他工程声明的内容命名空间。
    $nameCaseRoot = Join-Path $fixtureRoot "SpecificNameCase"
    $nameCaseCore = Join-Path $nameCaseRoot "Core\Core.csproj"
    $nameCaseOther = Join-Path $nameCaseRoot "Other\Other.csproj"
    $nameCaseSource = Join-Path $nameCaseRoot "Core\Source"
    $nameCaseDefs = Join-Path $nameCaseRoot "Core\Defs"
    Write-Utf8Fixture $nameCaseCore (New-MinimalProjectXml "BDP.Core" "BDP")
    Write-Utf8Fixture $nameCaseOther (New-MinimalProjectXml "Fake.Content" "BDP.Content")
    Write-Utf8Fixture (Join-Path $nameCaseRoot "Other\Feature.cs") "namespace BDP.DevHarness { internal sealed class Feature { } }"
    Write-Utf8Fixture (Join-Path $nameCaseSource "Leak.cs") "internal static class Leak { private const string TypeName = ""BDP.DevHarness.Feature""; }"
    New-Item -ItemType Directory -Path $nameCaseDefs -Force | Out-Null

    $nameResult = Invoke-IsolationVerifier `
        -ProjectPath $nameCaseCore `
        -AssemblyPath $coreAssemblyPath `
        -SourceRoot $nameCaseRoot `
        -SourceCodeRoot $nameCaseSource `
        -DefsRoot $nameCaseDefs

    Assert-True ($nameResult.ExitCode -ne 0) "Core 明确写出其他本模组专属命名空间时必须失败。"
    Assert-True ($nameResult.Text -match "明确名称" -and $nameResult.Text -match "BDP\.DevHarness" -and $nameResult.Text -match "Leak\.cs") "明确名称失败必须指出层级、名称和文件。"
}
finally
{
    if (Test-Path -LiteralPath $fixtureRoot)
    {
        $resolvedFixtureRoot = [IO.Path]::GetFullPath($fixtureRoot)
        Assert-True ($resolvedFixtureRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase)) "拒绝删除系统临时目录之外的测试目录。"
        Assert-True ((Split-Path $resolvedFixtureRoot -Leaf).StartsWith("BDP-CoreIsolation-", [StringComparison]::Ordinal)) "拒绝删除名称不符合约定的测试目录。"
        Remove-Item -LiteralPath $resolvedFixtureRoot -Recurse -Force
    }
}

Write-Host "PASS: BDP Core 零本模组程序集依赖检查器行为符合契约。"
