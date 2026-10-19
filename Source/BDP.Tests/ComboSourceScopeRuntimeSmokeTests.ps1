$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$snapshotPath = Join-Path $modRoot "Source\BDP\Core\Combos\Runtime\ComboSourceAdmissionSnapshot.cs"
$evaluatorPath = Join-Path $modRoot "Source\BDP\Core\Combos\Runtime\ComboSourceAdmissionEvaluator.cs"
$indexPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs"
$surfacePath = Join-Path $modRoot "Source\BDP\Core\Combos\Access\ComboSurfaceAccess.cs"
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs"

foreach ($path in @($snapshotPath, $evaluatorPath, $indexPath, $surfacePath, $resolverPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少 Combo 来源形态姿态运行时文件：$path"
}

$snapshotText = Get-Utf8Text $snapshotPath
$evaluatorText = Get-Utf8Text $evaluatorPath
$indexText = Get-Utf8Text $indexPath
$surfaceText = Get-Utf8Text $surfacePath
$resolverText = Get-Utf8Text $resolverPath

foreach ($member in @("ModeKey", "StanceKey"))
{
    Assert-True ($snapshotText -match $member) "来源运行时快照缺少：$member"
}
Assert-True ($evaluatorText -match "modeKey") "来源快照构建必须接收当前形态键。"
Assert-True ($evaluatorText -match "stanceKey") "来源快照构建必须接收当前姿态键。"
Assert-True ($indexText -match "FirstSourceScope") "Combo 索引必须读取第一来源范围。"
Assert-True ($indexText -match "SecondSourceScope") "Combo 索引必须读取第二来源范围。"
Assert-True ($indexText -match "ModeKey") "Combo 索引必须比较来源形态。"
Assert-True ($indexText -match "StanceKey") "Combo 索引必须比较来源姿态。"
Assert-True ($surfaceText -match "firstModeKey") "Combo 表面访问层必须传递第一来源形态。"
Assert-True ($surfaceText -match "secondModeKey") "Combo 表面访问层必须传递第二来源形态。"
Assert-True ($surfaceText -match "firstStanceKey") "Combo 表面访问层必须传递第一来源姿态。"
Assert-True ($surfaceText -match "secondStanceKey") "Combo 表面访问层必须传递第二来源姿态。"
Assert-True ($resolverText -match "GetChipModeKey") "组合候选解析必须读取来源形态。"
Assert-True ($resolverText -match "GetChipStanceKey") "组合候选解析必须读取来源姿态。"

Write-Host "PASS: Combo 来源形态姿态已接入来源快照、索引和组合候选解析。"
