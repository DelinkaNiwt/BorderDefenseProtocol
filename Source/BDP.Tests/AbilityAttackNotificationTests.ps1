param([string]$AssemblyDirectory, [string]$DefinitionsDirectory)
$ErrorActionPreference = 'Stop'
# 真实原版连射和成功观察；实际能力效果、地图伤害和隐身移除另需游戏实测。
$taskModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $AssemblyDirectory) { $AssemblyDirectory = Join-Path $taskModRoot '1.6/Assemblies' }
if (-not $DefinitionsDirectory) { $DefinitionsDirectory = Join-Path $taskModRoot '1.6/Content/Defs/AbilityDef' }
$taskManaged = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$taskHarmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-ability-notification-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $taskReferences = @("$AssemblyDirectory/BDP.Core.dll", "$taskManaged/Assembly-CSharp.dll", "$taskManaged/UnityEngine.CoreModule.dll", "$taskManaged/Unity.Mathematics.dll", "$taskManaged/netstandard.dll", "$taskHarmony/0Harmony.dll")
    $taskArguments = @('/nologo', '/nowarn:1684', '/target:exe', "/out:$taskOutput") + @($taskReferences | ForEach-Object { "/reference:$_" }) + @((Join-Path $PSScriptRoot 'AbilityAttackNotificationTests.cs'))
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' @taskArguments
    if ($LASTEXITCODE -ne 0) { throw '能力攻击通知夹具编译失败。' }
    & $taskOutput $taskManaged $taskHarmony $AssemblyDirectory $DefinitionsDirectory
    if ($LASTEXITCODE -ne 0) { throw '能力攻击通知行为验证失败。' }
} finally { Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue }
