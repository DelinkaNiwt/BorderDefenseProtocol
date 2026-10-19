# BDP 近战 burst（连续攻击）实际尝试诊断冒烟测试。
# 目标是保证游戏日志能区分“预设声明了三击”和“原版 Verb 实际调用了几次 TryCastShot”。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$diagnosticsPath = Join-Path $modRoot "Source\BDP\Core\AttackExecution\AttackExecutionDiagnostics.cs"
$meleeVerbPath = Join-Path $modRoot "Source\BDP\Core\Verbs\BdpVerb_MeleeAttackDamage.cs"

foreach ($path in @($diagnosticsPath, $meleeVerbPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少近战 burst 诊断实现文件：$path"
}

$diagnosticsText = Get-Utf8Text $diagnosticsPath
$meleeVerbText = Get-Utf8Text $meleeVerbPath

Assert-True ($diagnosticsText -match 'LogMeleeBurstShot\s*\(') `
    '近战诊断必须提供实际 burst shot（连续攻击单击）日志入口。'
Assert-True (
    ($diagnosticsText -match 'shotTick') -and
    ($diagnosticsText -match 'burstShotCount') -and
    ($diagnosticsText -match 'ticksBetweenBurstShots') -and
    ($diagnosticsText -match 'hitResult')
) '近战实际攻击日志必须同时记录游戏 tick、burst 总数、连续间隔和原版 TryCastShot 结果。'

$tryCastShotStart = $meleeVerbText.IndexOf('protected override bool TryCastShot', [System.StringComparison]::Ordinal)
Assert-True ($tryCastShotStart -ge 0) '无法定位 BDP 近战 TryCastShot（实际单击入口）。'
$tryCastShotText = $meleeVerbText.Substring($tryCastShotStart)
Assert-True ($tryCastShotText -match 'LogMeleeBurstShot\s*\(') `
    'BDP 近战 TryCastShot 必须记录每次实际进入原版近战命中流程的调用。'

Write-Host "PASS: BDP 近战 burst 实际单击诊断契约存在。"
