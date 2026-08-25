$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$snapshotPath = Join-Path $modRoot "Source\BDP\Core\Combos\Runtime\ComboSourceAdmissionSnapshot.cs"
$evaluatorPath = Join-Path $modRoot "Source\BDP\Core\Combos\Runtime\ComboSourceAdmissionEvaluator.cs"
$indexPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs"
Assert-True (Test-Path -LiteralPath $snapshotPath) "缺少 Combo 来源准入快照。"
Assert-True (Test-Path -LiteralPath $evaluatorPath) "缺少 Combo 来源准入求值器。"

$snapshotText = Get-Utf8Text $snapshotPath
$evaluatorText = Get-Utf8Text $evaluatorPath
$indexText = Get-Utf8Text $indexPath
foreach ($member in @("ProfessionKey", "CategoryKey", "TagKeys", "SourceVariantKey"))
{
    Assert-True ($snapshotText -match $member) "来源准入快照缺少：$member"
}
foreach ($dimension in @("AllowedProfessions", "DeniedProfessions", "AllowedCategories", "DeniedCategories", "AllowedTags", "RequiredTags", "DeniedTags", "AllowedSourceVariants", "DeniedSourceVariants"))
{
    Assert-True ($evaluatorText -match $dimension) "运行时准入未处理：$dimension"
}
Assert-True ($evaluatorText -match "ValueSetAdmissionEvaluator") "芯片准入必须复用通用集合筛选器。"
Assert-True ($evaluatorText -match "AreSourceVariantsCompatible") "来源准入求值器必须提供来源变体配对判断。"
Assert-True ($indexText -match "RequireSameSourceVariant") "Combo 索引必须读取来源变体一致性条件。"
Assert-True ($indexText -match "SourceVariantMismatch") "Combo 索引必须提供稳定的来源变体不一致诊断键。"
Assert-True ($indexText -match "MatchesAssignment") "Combo 索引必须检查具体来源项身份分配。"
Assert-True ($indexText -match "MatchesAssignment\([\s\S]*FirstSourceAdmission[\s\S]*SecondSourceAdmission") "Combo 索引必须尝试第一、第二来源项的正向分配。"
Assert-True ($indexText -match "SecondSourceAdmission[\s\S]*FirstSourceAdmission") "Combo 索引必须尝试第一、第二来源项的反向分配。"
Assert-True ($indexText -match "Dictionary<string, List<ComboDef>>") "相同动作对必须允许存在多份不同准入的 ComboDef。"

Write-Host "PASS: Combo 来源准入按成品最终身份和无序来源项分配稳定求值。"
