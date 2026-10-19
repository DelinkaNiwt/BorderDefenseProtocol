# 成品芯片信息身份合并冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$composerPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationInputComposer.cs"
$resolutionPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Model\ChipCombinationResolution.cs"
$resolverPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationResolver.cs"
$previewPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewBuilder.cs"
$sharedBuilderPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInformationModelBuilder.cs"

$composerText = Get-Utf8Text $composerPath
$resolutionText = Get-Utf8Text $resolutionPath
$resolverText = Get-Utf8Text $resolverPath
$previewText = Get-Utf8Text $previewPath
$sharedBuilderText = Get-Utf8Text $sharedBuilderPath

Assert-True ($composerText -match "MergeIdentity") "信息输入合并必须逐字段保留成品身份。"
Assert-True ($composerText -notmatch "baseInput\.Identity\s*\?\?\s*overlayInput\.Identity") "信息输入合并不得用运行时空身份覆盖成品身份。"
Assert-True ($resolutionText -match "SourceProfessionKey") "组合解析结果必须携带成品最终职业键。"
Assert-True ($resolverText -match "SourceProfessionKey\s*=\s*record\.ProfessionDefName") "组合解析结果必须直接使用成品记录的最终职业键。"
Assert-True ($sharedBuilderText -match "resolution\.SourceProfessionKey") "统一信息构建器必须使用组合解析结果中的最终职业键。"

Write-Host "PASS: 成品芯片职业身份不会在运行时与制造预览合并时丢失。"
