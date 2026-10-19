$ErrorActionPreference = 'Stop'
# 编译最小真实资源行为夹具，不启动游戏界面。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-trion-rules-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'HostileTrionResourceRulesTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '资源规则测试编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '资源规则行为测试失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
