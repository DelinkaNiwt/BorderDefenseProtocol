param([string]$AssemblyDirectory, [string]$DevelopmentAssemblyDirectory)
$ErrorActionPreference = 'Stop'
$taskModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $AssemblyDirectory) { $AssemblyDirectory = Join-Path $taskModRoot '1.6/Assemblies' }
if (-not $DevelopmentAssemblyDirectory) { $DevelopmentAssemblyDirectory = Join-Path $taskModRoot '1.6/Development/Assemblies' }
$taskManaged = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$taskHarmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies'
$taskForeign = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/3761725323/1.6/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-flashing-blades-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $taskReferences = @("$taskModRoot/1.6/Assemblies/BDP.Core.dll", "$taskModRoot/1.6/Assemblies/BDP.Content.dll", "$taskManaged/Assembly-CSharp.dll", "$taskManaged/UnityEngine.CoreModule.dll", "$taskManaged/Unity.Mathematics.dll", "$taskManaged/netstandard.dll", "$taskHarmony/0Harmony.dll")
    $taskArguments = @('/nologo', '/nowarn:1684', '/target:exe', "/out:$taskOutput") + @($taskReferences | ForEach-Object { "/reference:$_" }) + @((Join-Path $PSScriptRoot 'FlashingBladesCompatibilityBehaviorTests.cs'))
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' @taskArguments
    if ($LASTEXITCODE -ne 0) { throw '刀光兼容行为夹具编译失败。' }
    & $taskOutput $taskManaged $taskHarmony $AssemblyDirectory "$taskModRoot/1.6/Assemblies" $taskForeign $DevelopmentAssemblyDirectory
    if ($LASTEXITCODE -ne 0) { throw '刀光兼容行为检查失败。' }
} finally { Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue }
