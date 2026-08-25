# 战斧路线目标的间接爆炸范围预览测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$targetingSourcePath = Join-Path $modRoot "Source\BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs"
$targetingSourceText = Get-Utf8Text $targetingSourcePath

Assert-True ($targetingSourceText -match 'DrawVanillaFieldRadius\([^\r\n]*context\.Result') "预览必须把正式结果传入爆炸范围绘制。"
Assert-True ($targetingSourceText -match 'RequiresDirectTargetLineOfSight') "间接路线预览必须读取正式的目标直射要求。"
Assert-True ($targetingSourceText -match 'RenderPredictedAreaOfEffect\([\s\r\n]*target\.Cell') "间接路线爆炸预览必须以最终目标格绘制。"

$neutralBoundaryMatch = [regex]::Match(
    $targetingSourceText,
    'private static bool CanEnterNeutralTargetingBoundary\([\s\S]*?private static bool CanValidateTargetAtNeutralBoundary')
Assert-True $neutralBoundaryMatch.Success "必须能定位目标选择器的前置目标过滤边界。"
$neutralBoundaryText = $neutralBoundaryMatch.Value
Assert-True ($neutralBoundaryText -match 'ResolvedVerbSpec') "目标选择器前置过滤必须读取正式 Verb 规格。"
Assert-True ($neutralBoundaryText -match 'RequiresDirectTargetLineOfSight') "非直射攻击不能在进入范围绘制前沿用原版直射过滤。"
Assert-True ($neutralBoundaryText -match 'IsOutOfRange') "绕过直射过滤时仍必须保留射程限制。"

Write-Host "PASS: 战斧手动锚点和自动路线可在非直视最终目标时预览爆炸范围。"
