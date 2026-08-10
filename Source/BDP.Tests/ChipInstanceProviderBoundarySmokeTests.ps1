# Core 中性芯片实例提供器边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$coreRoot = Join-Path $modRoot "Source\BDP"
$providerPath = Join-Path $coreRoot "Core\Chips\External\IChipInstanceDefinitionProvider.cs"
$sourceProviderPath = Join-Path $coreRoot "Core\Chips\External\IChipSourceReferenceProvider.cs"
$snapshotPath = Join-Path $coreRoot "Core\Chips\External\ChipSourceReferenceSnapshot.cs"
$accessPath = Join-Path $coreRoot "Core\Chips\External\ChipInstanceSurfaceAccess.cs"

Assert-True (Test-Path -LiteralPath $providerPath) "Core 缺少中性芯片实例定义提供器。"
Assert-True (Test-Path -LiteralPath $sourceProviderPath) "Core 缺少中性芯片来源引用提供器。"
Assert-True (Test-Path -LiteralPath $snapshotPath) "Core 缺少中性芯片来源引用快照。"
Assert-True (Test-Path -LiteralPath $accessPath) "Core 缺少统一实例读取面。"

$providerText = Get-Utf8Text $providerPath
$sourceProviderText = Get-Utf8Text $sourceProviderPath
$accessText = Get-Utf8Text $accessPath
Assert-True ($providerText -match 'bool\s+TryGetChipDefinition\s*\(\s*out\s+ChipDefinitionConfig') "定义提供器签名错误。"
Assert-True ($sourceProviderText -match 'IReadOnlyList<string>\s+OrderedSourceKeys') "来源提供器缺少有序来源键。"
Assert-True ($sourceProviderText -match 'string\s+SourceVariantKey') "来源提供器缺少中性变体键。"
Assert-True ($sourceProviderText -match 'string\s+SourceVariantLabel') "来源提供器缺少中性变体标签。"
Assert-True ($accessText -match 'AllComps') "统一实例读取面必须只遍历 ThingWithComps.AllComps。"

$consumerFiles = @(
    "Core\Chips\Access\ChipDefinitionReaderSurface.cs",
    "Core\Chips\External\ChipSnapshotAccess.cs",
    "Core\Chips\Services\ChipActivationRequirementService.cs",
    "Core\Expressions\Contract\ChipExpressionContractInterpreter.cs",
    "Core\Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs",
    "Core\Expressions\Pipeline\ExpressionSourceCollector.cs",
    "Core\Expressions\Runtime\ComboRuntimeIndex.cs",
    "Core\Combos\Access\ComboSurfaceAccess.cs"
)
$consumerText = ($consumerFiles | ForEach-Object { Get-Utf8Text (Join-Path $coreRoot $_) }) -join "`n"
Assert-True ($consumerText -notmatch 'CompManufacturedChip') "Core 消费者不得直接识别具体制造 Comp。"
Assert-True ($consumerText -match 'ChipInstanceSurfaceAccess') "Core 消费者必须改用统一中性读取面。"

$legacyCompPath = Join-Path $coreRoot "Core\Chips\Access\CompManufacturedChip.cs"
Assert-True (-not (Test-Path -LiteralPath $legacyCompPath)) "Core 迁移期旧成品组件必须在最终收口时删除。"
$contentCompPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs"
$contentCompText = Get-Utf8Text $contentCompPath
Assert-True ($contentCompText -match 'IChipInstanceDefinitionProvider') "Content 成品组件必须实现定义提供器。"
Assert-True ($contentCompText -match 'IChipSourceReferenceProvider') "Content 成品组件必须实现来源提供器。"

Write-Host "PASS: Core 芯片实例消费者只依赖中性定义与来源提供器。"
