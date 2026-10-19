$ErrorActionPreference = 'Stop'
# 最小离线夹具验证真实健康接线；实际受伤与倒地时序仍由游戏实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-downed-protection-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'CombatBodyDownedDeathProtectionTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '战斗体倒地保护夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '战斗体倒地保护行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
