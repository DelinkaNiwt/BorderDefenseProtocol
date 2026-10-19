# BDP 近战连续多击运行时边界契约冒烟测试。
# 这里先锁定“原版 burst 生命周期 + BDP 一次起手”的设计边界，避免修复时继续把每一击当成独立 Job。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$interpreterPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Contract\ChipExpressionContractInterpreter.cs"
$meleeSurfacePath = Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\MeleeToolSurfaceBuilder.cs"
$stagesPath = Join-Path $modRoot "Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs"
$jobDriverPath = Join-Path $modRoot "Source\BDP\Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs"
$meleeVerbPath = Join-Path $modRoot "Source\BDP\Core\Verbs\BdpVerb_MeleeAttackDamage.cs"

foreach ($path in @($interpreterPath, $meleeSurfacePath, $stagesPath, $jobDriverPath, $meleeVerbPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少近战运行时实现文件：$path"
}

$interpreterText = Get-Utf8Text $interpreterPath
$meleeSurfaceText = Get-Utf8Text $meleeSurfacePath
$interpreterText = $interpreterText + "`n" + $meleeSurfaceText
$stagesText = Get-Utf8Text $stagesPath
$jobDriverText = Get-Utf8Text $jobDriverPath
$meleeVerbText = Get-Utf8Text $meleeVerbPath

$resolveManeuverStart = $meleeSurfaceText.IndexOf("internal static ManeuverDef ResolveManeuver", [System.StringComparison]::Ordinal)
$resolveManeuverEnd = $meleeSurfaceText.IndexOf("internal static DamageDef ResolveDamageDef", $resolveManeuverStart, [System.StringComparison]::Ordinal)
Assert-True ($resolveManeuverStart -ge 0 -and $resolveManeuverEnd -gt $resolveManeuverStart) "无法定位近战 Maneuver 解析方法。"
$resolveManeuverText = $meleeSurfaceText.Substring($resolveManeuverStart, $resolveManeuverEnd - $resolveManeuverStart)

Assert-True ($interpreterText -match "burstShotCount") "近战 HitCount 必须投影到原版 burstShotCount。"
Assert-True ($interpreterText -match "ticksBetweenBurstShots") "近战 HitIntervalTicks 必须投影到原版 ticksBetweenBurstShots。"
Assert-True (($interpreterText -match "ResolveVerbProps\(config, tool, maneuver, damageDef\)") -and ($interpreterText -match "return maneuver\.verb")) "近战 VerbProps 必须以已解析的 ManeuverDef.verb 为基准。"
Assert-True ($resolveManeuverText -match "\.Maneuvers") "近战 Maneuver 必须优先从 Tool.Maneuvers 解析。"
Assert-True ($resolveManeuverText -notmatch "return allDefs\[0\]") "不能用无关 ManeuverDef 列表第一项作为近战回退。"
Assert-True ($resolveManeuverText -notmatch "return allDefs\.Count > 0") "不能把任意 ManeuverDef 作为近战回退。"

$appendMeleeStart = $stagesText.IndexOf("private static void AppendMeleeCasts", [System.StringComparison]::Ordinal)
$appendMeleeEnd = $stagesText.IndexOf("private static AttackExecutionCast BuildCast", $appendMeleeStart, [System.StringComparison]::Ordinal)
Assert-True ($appendMeleeStart -ge 0 -and $appendMeleeEnd -gt $appendMeleeStart) "无法定位近战 cast 构造方法。"
$appendMeleeText = $stagesText.Substring($appendMeleeStart, $appendMeleeEnd - $appendMeleeStart)
Assert-True ($appendMeleeText -notmatch "for \(int i = 0; i < meleeHitCount; i\+\+\)") "近战多击不能继续展开成多个独立计划层 cast。"

Assert-True ($jobDriverText -match "TryStartCastOn\(") "近战 JobDriver 必须以 TryStartCastOn 的返回值确认攻击真正开始。"
Assert-True ($jobDriverText -notmatch "pawn\.meleeVerbs\.TryMeleeAttack\(") "近战 JobDriver 不得用 Pawn_MeleeVerbs.TryMeleeAttack 推进连续多击。"
Assert-True ($jobDriverText -notmatch "TryConsumeOwnedCooldownStance") "近战 JobDriver 不得主动吞掉原版冷却以推进下一击。"

Assert-True ($meleeVerbText -match "TryCastShot") "BDP 近战 Verb 必须保留每一击独立的 TryCastShot 边界。"
Assert-True ($meleeVerbText -match "CanHitTarget") "每一击开始前必须重新验证目标仍在合法近战范围。"

Write-Host "PASS: BDP 近战连续多击已满足原版 burst 生命周期边界契约。"
