# 芯片信息面板布局与芯片规格字段冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$publisherPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml"

$publisherText = Get-Utf8Text $publisherPath
$languageText = Get-Utf8Text $languagePath

Assert-True ($publisherText -match "CreateChipSpecificationsCategory") "芯片规格必须使用独立的信息分类。"
Assert-True ($publisherText -match "ChipSpecificationFieldKeys") "芯片规格字段必须有明确白名单。"
Assert-True ($publisherText -match "FormatMultilineValue") "多来源持续消耗必须由统一多行格式化方法输出。"
Assert-True ($publisherText -match "SustainCostBySourceCount") "芯片规格发布必须读取按来源数量的持续消耗。"
Assert-True ($publisherText -match "Environment\.NewLine|\\n") "持续消耗值必须保留换行。"
Assert-True ($publisherText -match "UseCost" -and $publisherText -match "MinimumRequired") "芯片规格必须包含单次使用消耗和最低要求。"
Assert-True ($languageText -match "BDP_Info_Category_ChipSpecifications") "芯片规格分类必须使用语言键。"
Assert-True ($languageText -match "BDP_Info_Field_SustainCost") "持续消耗条目必须使用语言键。"
Assert-True ($languageText -match "BDP_Info_Field_UseCost") "单次使用消耗必须使用语言键。"

Write-Output 'ChipInformationPanelLayoutSmokeTests PASS'
