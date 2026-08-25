$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$resolutionPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs"
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs"
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs"
$cloneServicePath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboExpressionEntryCloneService.cs"
$providerPath = Join-Path $modRoot "Source\BDP\Core\Expressions\External\IComboExpressionVariantModifierProvider.cs"
$registryPath = Join-Path $modRoot "Source\BDP\Core\Expressions\External\ComboExpressionVariantModifierRegistry.cs"
Assert-True (Test-Path -LiteralPath $resolutionPath) "缺少组合结果解析输入对象。"
Assert-True (Test-Path -LiteralPath $factoryPath) "缺少组合结果工厂。"
Assert-True (Test-Path -LiteralPath $resolverPath) "缺少组合表达解析器。"
Assert-True (Test-Path -LiteralPath $cloneServicePath) "缺少组合条目副本服务。"
Assert-True (Test-Path -LiteralPath $providerPath) "缺少组合条目变体修正接口。"
Assert-True (Test-Path -LiteralPath $registryPath) "缺少组合条目变体修正注册表。"

$resolutionText = Get-Utf8Text $resolutionPath
$factoryText = Get-Utf8Text $factoryPath
$resolverText = Get-Utf8Text $resolverPath
$cloneServiceText = Get-Utf8Text $cloneServicePath
$providerText = Get-Utf8Text $providerPath
$registryText = Get-Utf8Text $registryPath

Assert-True ($resolutionText -match "SourceVariantKey") "组合结果解析输入必须携带来源变体键。"
Assert-True ($resolutionText -match "SourceVariantLabel") "组合结果解析输入必须携带来源变体显示标签。"
Assert-True ($factoryText -match "SourceVariantKey") "组合结果工厂必须写入来源变体键。"
Assert-True ($factoryText -match "SourceVariantLabel") "组合结果工厂必须写入来源变体显示标签。"
Assert-True ($resolverText -match "BuildComboResult[\s\S]*SourceVariantKey") "组合解析入口必须把来源变体键传给结果工厂。"
Assert-True ($resolverText -match "BuildComboResult[\s\S]*SourceVariantLabel") "组合解析入口必须把来源变体标签传给结果工厂。"
Assert-True ($providerText -match "IComboExpressionVariantModifierProvider") "组合条目变体修正接口名称必须保持中性。"
Assert-True ($providerText -notmatch "ChipArmamentForm") "Core 组合条目变体修正接口不得暴露武装构型业务类型。"
Assert-True ($registryText -match "Register") "组合条目变体修正注册表必须提供注册入口。"
Assert-True ($registryText -match "Apply") "组合条目变体修正注册表必须提供应用入口。"
Assert-True ($cloneServiceText -match "Clone") "组合条目副本服务必须提供深复制入口。"
Assert-True ($cloneServiceText -match "VerbProps") "组合条目副本服务必须复制显式 VerbProps 覆盖层。"
Assert-True ($cloneServiceText -match "RangedModules") "组合条目副本服务必须复制远程模块列表。"
Assert-True ($resolverText -match "ComboExpressionVariantModifierRegistry") "组合解析器必须调用中性变体修正注册表。"

Write-Host "PASS: 组合结果来源变体元数据传播边界完整。"
