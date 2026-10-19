param([string]$CoreAssemblyDirectory)
$ErrorActionPreference = 'Stop'
# 调用真实 Core 与原版程序集；可指定私人编译目录，避免改动正式部署。
$taskModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $CoreAssemblyDirectory) { $CoreAssemblyDirectory = Join-Path $taskModRoot '1.6/Assemblies' }
$taskManaged = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$taskHarmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-core-patch-state-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $taskReferences = @((Join-Path $CoreAssemblyDirectory 'BDP.Core.dll'), (Join-Path $taskManaged 'Assembly-CSharp.dll'), (Join-Path $taskManaged 'UnityEngine.CoreModule.dll'), (Join-Path $taskManaged 'Unity.Mathematics.dll'), (Join-Path $taskManaged 'netstandard.dll'), (Join-Path $taskHarmony '0Harmony.dll'))
    # 老式离线编译器不解析游戏未使用的 Span 元信息；夹具没有调用这些 API。
    $taskArguments = @('/nologo', '/nowarn:1684', '/target:exe', "/out:$taskOutput") + @($taskReferences | ForEach-Object { "/reference:$_" }) + @((Join-Path $PSScriptRoot 'CorePatchTemporaryStateTests.cs'))
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' @taskArguments
    if ($LASTEXITCODE -ne 0) { throw 'Core 临时状态行为测试编译失败。' }
    & $taskOutput $taskManaged $taskHarmony $CoreAssemblyDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Core 临时状态行为测试失败。' }
} finally {
    Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue
}
