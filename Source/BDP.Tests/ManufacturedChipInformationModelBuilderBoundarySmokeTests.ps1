# 成品芯片共享信息模型构建器的边界测试。
# 本测试只检查共享设施与两个消费者的依赖方向，不验证具体界面排版。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sharedBuilderPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInformationModelBuilder.cs"
$previewBuilderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewBuilder.cs"
$assemblyBuilderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs"

Assert-True (Test-Path -LiteralPath $sharedBuilderPath) "缺少成品芯片共享信息模型构建器。"

$sharedBuilderText = Get-Utf8Text $sharedBuilderPath
$previewBuilderText = Get-Utf8Text $previewBuilderPath
$assemblyBuilderText = Get-Utf8Text $assemblyBuilderPath

Assert-True ($sharedBuilderText -match "new ChipInformationRequest") "共享构建器必须以统一请求承载声明配置、最终配置与附加描述。"
Assert-True ($sharedBuilderText -match "ChipInformationRuntimeInputAdapter\.CreateStatic") "共享构建器必须为静态成品提供统一运行时输入。"
Assert-True ($sharedBuilderText -match "ChipInformationSurfaceAccess\.Read") "共享构建器必须通过统一信息读取面生成模型。"
Assert-True ($sharedBuilderText -match "ChipArmamentFormInformationAdapter\.CreateBlocks") "共享构建器必须保留武装构型信息块。"
Assert-True ($sharedBuilderText -match "ResolvedDescription") "共享构建器必须保留功能与武装构型描述。"
Assert-True ($sharedBuilderText -notmatch "Widgets|Window_|Panel_") "共享构建器不得依赖具体窗口和绘制 API（应用程序接口）。"

Assert-True ($previewBuilderText -match "ManufacturedChipInformationModelBuilder\.Build") "制造预览必须调用共享成品芯片信息模型构建器。"
Assert-True ($previewBuilderText -notmatch "ChipInformationDefinitionInputAdapter|ChipInformationInputComposer|ChipInformationSurfaceAccess") "制造预览不得继续直接持有 Core 信息模型构建逻辑。"
Assert-True ($assemblyBuilderText -match "ManufacturedChipInformationModelBuilder\.Build") "装配台详情必须调用同一共享构建器。"
Assert-True ($assemblyBuilderText -notmatch "ChipManufacturingPreviewBuilder\.Build") "装配台详情不得借道制造预览获取信息模型。"

Write-Host "PASS: 成品芯片共享信息模型构建边界已锁定。"
