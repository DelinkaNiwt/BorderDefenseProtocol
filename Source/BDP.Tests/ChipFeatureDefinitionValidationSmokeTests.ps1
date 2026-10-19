# 芯片功能声明定义校验冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$validationPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Validation\ChipFeatureDefinitionValidator.cs"
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureResolver.cs"

Assert-True (Test-Path -LiteralPath $validationPath) "缺少功能定义校验器。"
Assert-True (Test-Path -LiteralPath $resolverPath) "缺少功能解析器。"

$validationText = Get-Utf8Text $validationPath
$resolverText = Get-Utf8Text $resolverPath

foreach ($token in @(
    "duplicate",
    "Feature",
    "Sources",
    "Children",
    "EntryIds",
    "WorkerClass",
    "Depth"
)) {
    Assert-True ($validationText -match $token) "定义校验器缺少关键校验路径：$token"
}

Assert-True ($validationText -match "ChipInformationDiagnostic") "校验失败必须转为诊断记录。"
Assert-True ($validationText -match "ChipInformationDiagnosticSeverity\.Error") "BDP 定义错误必须是 Error。"
Assert-True ($resolverText -match "ChipFeatureSourceWorkerFactory\.TryCreate") `
    "解析器必须按来源 WorkerClass 精确创建来源 Worker。"
Assert-True ($resolverText -match "ChipFeatureContributionMergeService\.Merge") `
    "解析器必须通过统一裁定器处理属性贡献。"
Assert-True ($resolverText -match "Children") "解析器必须递归处理子功能。"
Assert-True ($resolverText -match "maxDepth|MaximumDepth") "解析器必须具备递归深度保护。"

Write-Host "PASS: 芯片功能定义校验和递归解析边界已表达。"
