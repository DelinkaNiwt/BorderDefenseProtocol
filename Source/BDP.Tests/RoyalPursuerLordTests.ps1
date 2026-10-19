$ErrorActionPreference = 'Stop'
# 离线验证实际队伍业务；地图失能与任务胜利信号仍需游戏实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-royal-pursuers-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'RoyalPursuerLordTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '追兵队伍夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '追兵队伍行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
