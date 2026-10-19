$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$scopeConfigPath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboSourceScopeConfig.cs"
$scopeContractPath = Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboSourceScopeContract.cs"
$defPath = Join-Path $modRoot "Source\BDP\Core\Combos\Defs\ComboDef.cs"
$definitionConfigPath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboDefinitionConfig.cs"
$definitionContractPath = Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboDefinitionContract.cs"
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboDefinitionContractResolver.cs"
$validatorPath = Join-Path $modRoot "Source\BDP\Core\Combos\Validation\ComboDefinitionValidator.cs"

Assert-True (Test-Path -LiteralPath $scopeConfigPath) "缺少 Combo 来源形态姿态范围配置。"
Assert-True (Test-Path -LiteralPath $scopeContractPath) "缺少 Combo 来源形态姿态范围契约。"

$scopeConfigText = Get-Utf8Text $scopeConfigPath
$scopeContractText = Get-Utf8Text $scopeContractPath
$defText = Get-Utf8Text $defPath
$definitionConfigText = Get-Utf8Text $definitionConfigPath
$definitionContractText = Get-Utf8Text $definitionContractPath
$resolverText = Get-Utf8Text $resolverPath
$validatorText = Get-Utf8Text $validatorPath

foreach ($member in @("ModeKey", "StanceKey"))
{
    Assert-True ($scopeConfigText -match "string\s+$member") "来源范围配置缺少：$member"
    Assert-True ($scopeContractText -match "string\s+$member") "来源范围契约缺少：$member"
}
foreach ($member in @("FirstSourceScope", "SecondSourceScope"))
{
    Assert-True ($defText -match $member) "ComboDef 缺少：$member"
    Assert-True ($definitionConfigText -match $member) "Combo 配置镜像缺少：$member"
    Assert-True ($definitionContractText -match $member) "Combo 正式契约缺少：$member"
    Assert-True ($resolverText -match $member) "Combo 契约解析器未复制：$member"
}
Assert-True ($resolverText -match "ResolveSourceScope") "Combo 契约解析器必须解析来源范围。"
Assert-True ($validatorText -match "ValidateSourceScope") "Combo 校验器必须检查来源形态姿态范围。"
Assert-True ($validatorText -match "StanceKey") "Combo 校验器必须检查姿态依赖形态。"

Write-Host "PASS: Combo 第一、第二来源项的形态姿态范围配置和契约字段完整。"
