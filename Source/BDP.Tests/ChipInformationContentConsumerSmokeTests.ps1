# 制造台与装配台共同消费统一芯片信息模型的边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$builderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewBuilder.cs"
$sharedBuilderPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInformationModelBuilder.cs"
$manufacturedChipPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs"
$triggerInformationPath = Join-Path $modRoot "Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs"
$panelPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\Panel_ChipDetail.cs"
$detailBuilderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs"
$modelPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewModel.cs"

$builderText = Get-Utf8Text $builderPath
$sharedBuilderText = Get-Utf8Text $sharedBuilderPath
$manufacturedChipText = Get-Utf8Text $manufacturedChipPath
$triggerInformationText = Get-Utf8Text $triggerInformationPath
$panelText = Get-Utf8Text $panelPath
$detailBuilderText = Get-Utf8Text $detailBuilderPath
$modelText = Get-Utf8Text $modelPath

Assert-True ($sharedBuilderText -match 'ChipInformationSurfaceAccess\.Read') '共享构建器必须通过统一读取面。'
Assert-True ($builderText -match 'ChipInformationModel') '制造消费者必须接收统一信息模型。'
Assert-True ($builderText -match 'ManufacturedChipInformationModelBuilder\.Build') '制造消费者必须调用共享成品芯片信息模型构建器。'
Assert-True ($panelText -notmatch 'ChipInformationSurfaceAccess\.Read') '当前为空的装配详情区不得读取芯片信息模型。'
Assert-True ($panelText -notmatch 'ChipInformationInstanceInputAdapter') '当前为空的装配详情区不得适配芯片详情。'
Assert-True ($detailBuilderText -match 'ChipInformationInstanceRequestAdapter') '装配详情构建器必须把成品实例转换为统一请求。'
Assert-True ($detailBuilderText -match 'ChipInformationSurfaceAccess\.Read\(request\)') '装配详情构建器必须通过统一请求读取信息模型。'
Assert-True ($builderText -notmatch 'ChipInformationDefinitionInputAdapter|ChipInformationInputComposer|ChipInformationSurfaceAccess') '制造消费者不得直接持有 Core 信息模型构建逻辑。'
Assert-True ($builderText -notmatch 'VerbProperties|ChipArmamentFormOverrides') '制造消费者不得解析原始战斗字段。'
Assert-True ($panelText -notmatch 'ChipSnapshotAccess|ChipActivationRequirementService') '装配消费者不得绕过信息模型读取芯片详情。'
Assert-True ($modelText -match 'ChipInformationModel') '制造视图模型必须保留信息模型引用。'
Assert-True ($manufacturedChipText -match 'IChipInstanceDeclaredDefinitionProvider' -and
    $manufacturedChipText -match 'TryGetDeclaredChipDefinition') '成品芯片必须能向信息模型提供武装构型前的基础定义。'
Assert-True ($triggerInformationText -match 'ChipInformationInstanceInputAdapter') '激活芯片信息输入必须通过通用实例适配器同时合并基础定义与当前有效定义。'

Write-Host "PASS: 制造台与装配台均通过统一信息请求消费芯片功能模型。"
