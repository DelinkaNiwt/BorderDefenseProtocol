param([string]$AssemblyDirectory)
$ErrorActionPreference = 'Stop'
# 调用真实核心推进方法；隔离游戏依赖，不代替游戏内闪帧验收。
$taskModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $AssemblyDirectory) { $AssemblyDirectory = Join-Path $taskModRoot '1.6/Assemblies' }
$taskManaged = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$taskHarmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/1.6/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-melee-handoff-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $taskReferences = @("$AssemblyDirectory/BDP.Core.dll", "$taskManaged/Assembly-CSharp.dll", "$taskManaged/UnityEngine.CoreModule.dll", "$taskManaged/Unity.Mathematics.dll", "$taskManaged/netstandard.dll", "$taskHarmony/0Harmony.dll")
    $taskArguments = @('/nologo', '/nowarn:1684', '/target:exe', "/out:$taskOutput") + @($taskReferences | ForEach-Object { "/reference:$_" }) + @((Join-Path $PSScriptRoot 'MeleeRoundHandoffBehaviorTests.cs'))
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' @taskArguments
    if ($LASTEXITCODE -ne 0) { throw '近战轮次交接夹具编译失败。' }
    & $taskOutput $taskManaged $taskHarmony $AssemblyDirectory
    if ($LASTEXITCODE -ne 0) { throw '近战轮次交接行为验证失败。' }
} finally { Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue }
