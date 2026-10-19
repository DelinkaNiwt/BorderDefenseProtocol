# 武装型解析、选择和 Core 中性来源边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$manufacturingRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing"
$coreRoot = Join-Path $modRoot "Source\BDP"
$recordPath = Join-Path $manufacturingRoot "Model\ChipCombinationRecord.cs"
$resolutionPath = Join-Path $manufacturingRoot "Model\ChipCombinationResolution.cs"
$resolverPath = Join-Path $manufacturingRoot "Resolution\ChipCombinationResolver.cs"
$lookupPath = Join-Path $manufacturingRoot "Resolution\ChipManufacturingDefLookup.cs"
$selectionPath = Join-Path $manufacturingRoot "Resolution\ChipCombinationSelectionRules.cs"
$listPath = Join-Path $manufacturingRoot "UI\ChipManufacturingListModel.cs"
$windowPath = Join-Path $manufacturingRoot "UI\Window_ChipManufacturing.cs"
$labelPath = Join-Path $manufacturingRoot "UI\ChipManufacturingDefLabelResolver.cs"
$previewPath = Join-Path $manufacturingRoot "UI\ChipManufacturingPreviewBuilder.cs"
$coreFiles = Get-ChildItem -LiteralPath $coreRoot -Recurse -Filter "*.cs"
$coreText = ($coreFiles | ForEach-Object { Get-Utf8Text $_.FullName }) -join "`n"

$recordText = Get-Utf8Text $recordPath
$resolutionText = Get-Utf8Text $resolutionPath
$resolverText = Get-Utf8Text $resolverPath
$lookupText = Get-Utf8Text $lookupPath
$selectionText = Get-Utf8Text $selectionPath
$listText = Get-Utf8Text $listPath
$windowText = Get-Utf8Text $windowPath
$labelText = Get-Utf8Text $labelPath
$previewText = Get-Utf8Text $previewPath

Assert-True ($recordText -match 'ArmamentFormDefName') "组合记录必须保存显式武装型键。"
Assert-True ($recordText -notmatch 'GunShellDefName|legacyGunShellDefName') "组合记录不得继续保留旧枪壳字段或迁移别名。"
Assert-True ($resolutionText -match 'ArmamentForm') "解析结果必须暴露有效武装型。"
Assert-True ($resolverText -match 'implicitDefault|FindImplicit') "解析器必须参与隐藏默认武装型解析。"
Assert-True ($resolverText -match 'includeInProductLabel|showInManufacturing') "解析器必须区分逻辑有效与玩家可见武装型。"
Assert-True ($lookupText -match 'CanUseArmamentForm') "默认型查找必须复用构型功能适用范围。"
Assert-True ($selectionText -match 'maxFunctionCount|MaxFunctionCount') "功能数量必须由武装型数据能力决定。"
Assert-True ($selectionText -match 'form\.maxFunctionCount|forms\.Count') "枪械路径的双芯片功能模板能力必须来自数据。"
Assert-True ($selectionText -notmatch 'GunnerProfessionDefName|BDP_ChipProfession_Gunner') "功能数量规则不得硬编码枪手。"
Assert-True ($listText -match 'GetArmamentForms') "制造台必须通过通用武装型列表入口。"
Assert-True ($listText -match 'showInManufacturing') "制造台列表必须过滤隐藏默认型。"
Assert-True ($listText -match 'CanUseArmamentForm|CanUseArmamentFormFunction') "制造台必须按所选芯片功能模板过滤不适用构型。"
Assert-True ($windowText -notmatch 'IsGunner|GetGunShells|DrawGunShellSection') "制造台不得继续按枪手硬编码武装型区域。"
Assert-True ($labelText -match 'BDP_ChipManufacturing_ArmamentFormSuffix') "玩家可见武装构型名称必须使用语言包统一后缀。"
Assert-True ($previewText -match 'ManufacturedChipInformationModelBuilder\.Build' -and
    $previewText -match 'MapSemanticFeatures' -and
    $previewText -match 'InformationModel') "制造预览必须通过统一信息模型读取声明/有效配置和语义功能，不得再为隐藏默认型单独造展示分组。"
Assert-True ($coreText -match 'AllowedSourceVariants|DeniedSourceVariants') "Core 来源准入必须使用中性来源变体名称。"
Assert-True ($coreText -notmatch 'AllowedArmamentForms|DeniedArmamentForms|ArmamentFormKey') "Core 不得继续暴露构型准入专名。"

Write-Host "PASS: 武装型解析、数据化功能数量、隐藏默认型和 Core 中性来源边界存在。"
