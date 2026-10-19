param([string]$AssemblyDirectory)
$ErrorActionPreference = 'Stop'
# 只验证真实入口与作用域；地图伤害、颜色显示及第三方回调需要游戏实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $AssemblyDirectory) { $AssemblyDirectory = Join-Path $modRoot '1.6/Assemblies' }
$managed = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$harmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-damage-request-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /target:exe "/out:$taskOutput" "/reference:$harmony/0Harmony.dll" (Join-Path $PSScriptRoot 'DamageRequestBoundaryTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '承伤边界夹具编译失败。' }
    & $taskOutput $managed $harmony $AssemblyDirectory
    if ($LASTEXITCODE -ne 0) { throw '承伤边界行为验证失败。' }
} finally { Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue }
