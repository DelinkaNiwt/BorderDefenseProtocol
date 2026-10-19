param([string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
# 在真实程序集上核对显示补丁和泄露账本；实际贴图绘制留给游戏内验证。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $AssemblyPath) { $AssemblyPath = Join-Path $modRoot '1.6/Assemblies' }
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-leak-icon-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" "/reference:$managed\netstandard.dll" "/reference:$harmony\0Harmony.dll" (Join-Path $PSScriptRoot 'CombatBodyTrionLeakIconTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '泄露图标夹具编译失败。' }
    & $output $managed $harmony $AssemblyPath
    if ($LASTEXITCODE -ne 0) { throw '泄露图标行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
