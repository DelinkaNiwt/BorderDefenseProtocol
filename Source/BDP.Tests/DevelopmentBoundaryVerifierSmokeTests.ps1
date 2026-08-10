# Development 单向依赖边界检查器冒烟测试。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$verifierPath = Join-Path $modRoot "Source\Build\Verify-BdpDevelopmentBoundary.ps1"
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("BDP-DevelopmentBoundaryTests-" + [Guid]::NewGuid().ToString("N"))

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Invoke-Verifier
{
    param([string]$FixtureRoot)
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try
    {
        $output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $verifierPath -ModRoot $FixtureRoot 2>&1)
        return [PSCustomObject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
    }
    finally
    {
        $ErrorActionPreference = $previousPreference
    }
}

function New-ProjectFile
{
    param([string]$Path, [string]$AssemblyName, [string]$RootNamespace, [string[]]$References)
    $referenceXml = ($References | ForEach-Object { "<ProjectReference Include=`"$_`" />" }) -join ""
    $xml = "<Project xmlns=`"http://schemas.microsoft.com/developer/msbuild/2003`"><PropertyGroup><AssemblyName>$AssemblyName</AssemblyName><RootNamespace>$RootNamespace</RootNamespace></PropertyGroup><ItemGroup>$referenceXml</ItemGroup></Project>"
    [IO.File]::WriteAllText($Path, $xml, [Text.UTF8Encoding]::new($false))
}

function New-CleanFixture
{
    param([string]$Root)
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "Source\BDP") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "Source\BDP.Content") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "Source\BDP.Development") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "1.6\Development\Assemblies") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "1.6\Content") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "Languages") -Force
    New-ProjectFile (Join-Path $Root "Source\BDP\BDP.csproj") "BDP.Core" "BDP" @()
    New-ProjectFile (Join-Path $Root "Source\BDP.Content\BDP.Content.csproj") "BDP.Content" "BDP.Content" @("..\BDP\BDP.csproj")
    New-ProjectFile (Join-Path $Root "Source\BDP.Development\BDP.Development.csproj") "BDP.Development" "BDP.Development" @("..\BDP\BDP.csproj", "..\BDP.Content\BDP.Content.csproj")
    [IO.File]::WriteAllText((Join-Path $Root "Source\BDP\Core.cs"), "namespace BDP { public class CoreType { } }", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $Root "Source\BDP.Content\Content.cs"), "namespace BDP.Content { public class ContentType { } }", [Text.UTF8Encoding]::new($false))
}

Assert-True (Test-Path -LiteralPath $verifierPath) "缺少 Development 边界检查器。"

try
{
    $realResult = Invoke-Verifier $modRoot
    Assert-True ($realResult.ExitCode -eq 0) "真实工程应通过 Development 边界检查：$($realResult.Output)"

    $contentReferenceFixture = Join-Path $temporaryRoot "content-reference"
    New-CleanFixture $contentReferenceFixture
    New-ProjectFile (Join-Path $contentReferenceFixture "Source\BDP.Content\BDP.Content.csproj") "BDP.Content" "BDP.Content" @("..\BDP\BDP.csproj", "..\BDP.Development\BDP.Development.csproj")
    Assert-True ((Invoke-Verifier $contentReferenceFixture).ExitCode -ne 0) "Content 引用 Development 时必须失败。"

    $sourceMentionFixture = Join-Path $temporaryRoot "source-mention"
    New-CleanFixture $sourceMentionFixture
    [IO.File]::WriteAllText((Join-Path $sourceMentionFixture "Source\BDP.Content\Content.cs"), "namespace BDP.Content { public class ContentType { private string forbidden = `"BDP.Development`"; } }", [Text.UTF8Encoding]::new($false))
    Assert-True ((Invoke-Verifier $sourceMentionFixture).ExitCode -ne 0) "正式源码写出 Development 时必须失败。"

    $xmlMentionFixture = Join-Path $temporaryRoot "xml-mention"
    New-CleanFixture $xmlMentionFixture
    [IO.File]::WriteAllText((Join-Path $xmlMentionFixture "1.6\Content\Forbidden.xml"), "<Defs><Thing Class=`"BDP.Development.Forbidden`" /></Defs>", [Text.UTF8Encoding]::new($false))
    Assert-True ((Invoke-Verifier $xmlMentionFixture).ExitCode -ne 0) "正式 XML 写出 Development 时必须失败。"
}
finally
{
    if (Test-Path -LiteralPath $temporaryRoot)
    {
        $resolved = [IO.Path]::GetFullPath($temporaryRoot)
        Assert-True ((Split-Path -Leaf $resolved).StartsWith("BDP-DevelopmentBoundaryTests-")) "拒绝删除不受控临时目录。"
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

Write-Host "PASS: Development 单向依赖边界检查器能识别真实正例与三个隔离负例。"
