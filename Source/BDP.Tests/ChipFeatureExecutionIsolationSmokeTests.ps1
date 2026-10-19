# 芯片功能语义不得反向参与玩法执行链的隔离基线测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$modRoot = Get-BdpModRoot
$coreRoot = Join-Path $modRoot "Source\BDP\Core"

# 这些目录是执行契约的主要边界；语义信息层可以读取它们的只读事实，但不得成为它们的行为依赖。
$executionRoots = @(
    (Join-Path $coreRoot "Expressions"),
    (Join-Path $coreRoot "Combos\Runtime"),
    (Join-Path $coreRoot "Trigger"),
    (Join-Path $coreRoot "Trion"),
    (Join-Path $coreRoot "Ability"),
    (Join-Path $coreRoot "Hediff")
)

$forbiddenExecutionReferences = @(
    "ChipFeatureResolver",
    "ResolvedChipFeature",
    "IChipFeatureSourceWorker",
    "ChipInformationDetailRenderer",
    "ChipFeatureRenderer"
)

foreach ($executionRoot in $executionRoots) {
    if (-not (Test-Path -LiteralPath $executionRoot)) {
        continue
    }

    $sourceText = Get-ChildItem -LiteralPath $executionRoot -Recurse -Filter "*.cs" |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }

    foreach ($forbiddenReference in $forbiddenExecutionReferences) {
        Assert-True (-not (($sourceText -join "`n") -match [regex]::Escape($forbiddenReference))) `
            "执行契约目录不应依赖信息语义实现：$forbiddenReference；目录：$executionRoot"
    }
}

$chipDefinitionPath = Join-Path $coreRoot "Chips\Config\ChipDefinitionConfig.cs"
Assert-True (Test-Path -LiteralPath $chipDefinitionPath) "缺少芯片定义配置入口。"

$chipDefinitionText = Get-Content -LiteralPath $chipDefinitionPath -Raw -Encoding UTF8
Assert-True ($chipDefinitionText -match "class\s+ChipDefinitionConfig") "缺少 ChipDefinitionConfig。"

Write-Host "PASS: 芯片功能语义保持在信息/定义边界，未反向依赖执行实现。"
