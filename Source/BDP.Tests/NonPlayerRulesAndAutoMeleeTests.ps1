param([switch]$AutomaticOnly)
$ErrorActionPreference = 'Stop'
# 离线夹具只调用当前真实程序集；游戏界面与任务切换仍需进入地图实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-non-player-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'NonPlayerRulesAndAutoMeleeTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '非玩家规则与自动近战夹具编译失败。' }
    $mode = if ($AutomaticOnly) { 'auto' } else { 'all' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies') $mode
    if ($LASTEXITCODE -ne 0) { throw '非玩家规则与自动近战行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
