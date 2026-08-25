$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$configPath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboSourceAdmissionConfig.cs"
$contractPath = Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboSourceAdmissionContract.cs"
Assert-True (Test-Path -LiteralPath $configPath) "缺少 Combo 来源准入配置。"
Assert-True (Test-Path -LiteralPath $contractPath) "缺少 Combo 来源准入契约。"

$configText = Get-Utf8Text $configPath
$defText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Defs\ComboDef.cs")
$definitionConfigText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboDefinitionConfig.cs")
$definitionContractText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboDefinitionContract.cs")
$resolverText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboDefinitionContractResolver.cs")
$validatorText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Validation\ComboDefinitionValidator.cs")

foreach ($member in @("AllowedProfessions", "DeniedProfessions", "AllowedCategories", "DeniedCategories", "AllowedTags", "RequiredTags", "DeniedTags", "AllowedSourceVariants", "DeniedSourceVariants"))
{
    Assert-True ($configText -match "List<string>\s+$member") "Combo 来源准入缺少字段：$member"
}
foreach ($member in @("FirstSourceAdmission", "SecondSourceAdmission"))
{
    Assert-True ($defText -match $member) "ComboDef 缺少：$member"
    Assert-True ($definitionConfigText -match $member) "Combo 配置镜像缺少：$member"
    Assert-True ($definitionContractText -match $member) "Combo 正式契约缺少：$member"
    Assert-True ($resolverText -match $member) "Combo 契约解析器未复制：$member"
}
Assert-True ($defText -match "RequireSameSourceVariant\s*=\s*true") "ComboDef 必须默认要求两个来源项使用同一来源变体。"
Assert-True ($definitionConfigText -match "RequireSameSourceVariant") "Combo 配置镜像缺少来源变体一致性条件。"
Assert-True ($definitionContractText -match "RequireSameSourceVariant") "Combo 正式契约缺少来源变体一致性条件。"
Assert-True ($resolverText -match "RequireSameSourceVariant") "Combo 契约解析器未复制来源变体一致性条件。"
Assert-True ($validatorText -match "ValidateSourceAdmission") "Combo 校验器必须检查来源准入列表。"
Assert-True ($validatorText -match "IsNullOrWhiteSpace") "Combo 校验器必须拒绝空白身份键。"

Write-Host "PASS: Combo 第一、第二来源项的准入配置和契约字段完整。"
