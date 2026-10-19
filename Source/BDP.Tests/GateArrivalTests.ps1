$ErrorActionPreference = 'Stop'
# 测试真实业务的时序契约；材质、地图与队伍生命周期另由开发入口实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-gate-arrival-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'GateArrivalTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '黑洞门契约夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '黑洞门时序契约验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
