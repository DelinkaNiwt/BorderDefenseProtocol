$ErrorActionPreference = 'Stop'
# 对真实正式程序集执行行为夹具；不构建或部署模组 DLL（动态链接库）。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-floating-shield-boundary-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$output" "/r:$managed\Assembly-CSharp.dll" "/r:$managed\UnityEngine.CoreModule.dll" "/r:$harmony\0Harmony.dll" (Join-Path $PSScriptRoot 'FloatingFlightShieldBoundaryBehaviorTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '浮空与护盾边界夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '浮空与护盾边界行为测试失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
