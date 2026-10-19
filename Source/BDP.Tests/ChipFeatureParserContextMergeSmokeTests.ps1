$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$parserPath = Join-Path $modRoot 'Source\BDP\Core\Information\ChipInformationParser.cs'
$definitionAdapterPath = Join-Path $modRoot 'Source\BDP\Core\Information\Input\ChipInformationDefinitionInputAdapter.cs'
$parserText = Get-Content $parserPath -Raw -Encoding UTF8
$definitionAdapterText = Get-Content $definitionAdapterPath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($parserText -match 'ParseInternal\(input,\s*true,\s*request\)') `
    '带功能请求解析时仍须解析原有形态/姿态通用信息块。'
Assert-True ($parserText -match 'contexts = MergeFeatureContexts\(contexts,\s*projection\.Contexts\)') `
    '功能上下文必须与原有上下文信息块合并，不得整体替换。'
Assert-True ($parserText -match '(?s)MergeFeatureContexts.*?MergeContextNode') `
    '同键上下文必须合并到统一节点树，保留两路内容。'
Assert-True ($parserText -match 'ChipInformationNode\s+featureContext') `
    '功能上下文必须直接使用统一信息节点。'
Assert-True ($definitionAdapterText -match 'mode\.ModeKey \+ "/" \+ stance\.StanceKey') `
    '内置姿态上下文键必须与功能树、组合引用的父/子路径格式一致。'

Write-Output 'ChipFeatureParserContextMerge PASS'
