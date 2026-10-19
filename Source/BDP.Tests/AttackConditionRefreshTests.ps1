$ErrorActionPreference = 'Stop'
# 使用真实 Core（核心）程序集验证攻击续接；实际作业和姿态时序仍需游戏实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-attack-condition-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$output" (Join-Path $PSScriptRoot 'AttackConditionRefreshTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '攻击条件刷新测试编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '攻击条件刷新行为测试失败。' }
} finally { Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue }
