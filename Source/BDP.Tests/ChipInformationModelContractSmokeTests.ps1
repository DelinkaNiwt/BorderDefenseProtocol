# 芯片信息模型核心契约冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$informationRoot = Join-Path $modRoot "Source\BDP\Core\Information"
$valueStatePath = Join-Path $informationRoot "Model\ChipInformationValueState.cs"
$requiredFiles = @(
    "ChipInformationParser.cs",
    "ChipInformationService.cs",
    "Model\ChipInformationModel.cs",
    "Model\ChipInformationParseInput.cs",
    "Model\ChipInformationNode.cs",
    "Model\ChipInformationProperty.cs",
    "Model\ChipInformationValue.cs",
    "Model\ChipInformationValueDisplaySourceKind.cs",
    "Model\ChipInformationProvenance.cs",
    "Rules\IChipInformationRule.cs",
    "Rules\ChipInformationRuleRegistry.cs"
)

foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $informationRoot $fileName)) "缺少芯片信息模型文件：$fileName"
}

$modelText = ($requiredFiles | ForEach-Object { Get-Utf8Text (Join-Path $informationRoot $_) }) -join "`n"
Assert-True ($modelText -match "class\s+ChipInformationModel") "缺少 ChipInformationModel（芯片信息模型）。"
Assert-True ($modelText -match "class\s+ChipInformationParseInput") "缺少 ChipInformationParseInput（芯片信息解析输入）。"
Assert-True ($modelText -match "class\s+ChipInformationNode") "缺少统一信息节点模型。"
Assert-True ($modelText -match "class\s+ChipInformationProperty") "缺少统一信息属性模型。"
Assert-True ($modelText -match "class\s+ChipInformationProvenance") "缺少字段来源链模型。"
Assert-True ($modelText -match "IReadOnlyList") "信息模型必须使用只读集合。"
Assert-True (-not (Test-Path -LiteralPath $valueStatePath)) "最终信息树不得提供解释性状态枚举。"
Assert-True ($modelText -notmatch "ChipInformationValueState|ValueState") "最终信息模型只表达客观数据。"
Assert-True ($modelText -notmatch "Dictionary<string,\s*object>") "信息模型不得退化为无类型字典。"

Write-Host "PASS: 芯片信息模型核心类型契约存在且保持只读、类型化边界。"
