# BDP 正式发布残留检查器冒烟测试。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$verifierPath = Join-Path $modRoot "Source\Build\Verify-BdpReleaseReady.ps1"
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("BDP-ReleaseReadinessTests-" + [Guid]::NewGuid().ToString("N"))

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function New-ReleaseFixture
{
    param([string]$Root)
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "1.6\Assemblies") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $Root "1.6\Development") -Force
    [IO.File]::WriteAllBytes((Join-Path $Root "1.6\Assemblies\BDP.Core.dll"), [byte[]]@(1))
    [IO.File]::WriteAllBytes((Join-Path $Root "1.6\Assemblies\BDP.Content.dll"), [byte[]]@(1))
    [IO.File]::WriteAllText((Join-Path $Root "1.6\Development\.keep"), "keep", [Text.UTF8Encoding]::new($false))
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

Assert-True (Test-Path -LiteralPath $verifierPath) "缺少正式发布残留检查器。"

try
{
    $cleanFixture = Join-Path $temporaryRoot "clean"
    New-ReleaseFixture $cleanFixture
    Assert-True ((Invoke-Verifier $cleanFixture).ExitCode -eq 0) "只有 Core/Content 且 Development 为空时应通过。"

    $dllFixture = Join-Path $temporaryRoot "development-dll"
    New-ReleaseFixture $dllFixture
    $null = New-Item -ItemType Directory -Path (Join-Path $dllFixture "1.6\Development\Assemblies") -Force
    [IO.File]::WriteAllBytes((Join-Path $dllFixture "1.6\Development\Assemblies\BDP.Development.dll"), [byte[]]@(1))
    Assert-True ((Invoke-Verifier $dllFixture).ExitCode -ne 0) "残留 BDP.Development.dll 时必须失败。"

    $pdbFixture = Join-Path $temporaryRoot "development-pdb"
    New-ReleaseFixture $pdbFixture
    $null = New-Item -ItemType Directory -Path (Join-Path $pdbFixture "1.6\Development\Assemblies") -Force
    [IO.File]::WriteAllBytes((Join-Path $pdbFixture "1.6\Development\Assemblies\BDP.Development.pdb"), [byte[]]@(1))
    Assert-True ((Invoke-Verifier $pdbFixture).ExitCode -ne 0) "残留 BDP.Development.pdb 时必须失败。"

    $defFixture = Join-Path $temporaryRoot "development-def"
    New-ReleaseFixture $defFixture
    $null = New-Item -ItemType Directory -Path (Join-Path $defFixture "1.6\Development\Defs") -Force
    [IO.File]::WriteAllText((Join-Path $defFixture "1.6\Development\Defs\Temporary.xml"), "<Defs />", [Text.UTF8Encoding]::new($false))
    Assert-True ((Invoke-Verifier $defFixture).ExitCode -ne 0) "残留开发 Def 时必须失败。"
}
finally
{
    if (Test-Path -LiteralPath $temporaryRoot)
    {
        $resolved = [IO.Path]::GetFullPath($temporaryRoot)
        Assert-True ((Split-Path -Leaf $resolved).StartsWith("BDP-ReleaseReadinessTests-")) "拒绝删除不受控临时目录。"
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

Write-Host "PASS: 正式发布残留检查器能放行干净目录并拒绝 DLL、PDB 与开发 Def。"
