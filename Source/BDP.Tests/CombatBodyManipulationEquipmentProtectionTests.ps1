$ErrorActionPreference = 'Stop'
# 运行真实装备销毁与正式补丁；地图受伤及恢复原身时序仍需游戏内复测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-manipulation-equipment-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" "/reference:$managed\netstandard.dll" "/reference:$harmony\0Harmony.dll" (Join-Path $PSScriptRoot 'CombatBodyManipulationEquipmentProtectionTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '操控丧失装备保护夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '操控丧失装备保护行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
