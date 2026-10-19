# 芯片功能形态/姿态上下文投影冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$projectorPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureContextProjector.cs"
$contextPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureContextProjectionResult.cs"
$readContextPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Sources\ChipFeatureReadContext.cs"

Assert-True (Test-Path -LiteralPath $projectorPath) "缺少功能上下文投影器。"
Assert-True (Test-Path -LiteralPath $contextPath) "缺少功能上下文投影结果。"

$projectorText = Get-Utf8Text $projectorPath
$contextText = Get-Utf8Text $contextPath
$readContextText = Get-Utf8Text $readContextPath

Assert-True ($projectorText -match "ActiveEntryIds") "上下文投影必须依据 ActiveEntryIds。"
Assert-True ($projectorText -match "Modes" -and $projectorText -match "Stances") `
    "上下文投影必须处理形态和姿态。"
Assert-True ($projectorText -match "mode\.ModeKey") "姿态上下文必须保留父形态键。"
Assert-True ($projectorText -match "ChipFeatureResolver") "上下文投影必须复用功能解析器。"
Assert-True ($contextText -match "ChipInformationNode") "投影结果必须直接输出统一信息节点。"
Assert-True ($contextText -match "Diagnostics") "上下文投影必须保留诊断。"
Assert-True ($readContextText -match "ActiveEntryIds") "读取上下文必须向来源 Worker 暴露当前活动条目。"
Assert-True ($projectorText -notmatch "EntryType|VerbProps|AbilityDefName|HediffDefName") `
    "上下文投影不得按技术类型推断玩家功能。"

Write-Host "PASS: 芯片功能形态/姿态上下文投影边界已表达。"
