$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$parserPath = Join-Path $repoRoot 'Source\BDP\Core\Information\ChipInformationParser.cs'
$builderPath = Join-Path $repoRoot 'Source\BDP.Content\Information\ManufacturedChipInformationModelBuilder.cs'
$parserText = Get-Content -LiteralPath $parserPath -Raw -Encoding UTF8
$builderText = Get-Content -LiteralPath $builderPath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($parserText -match 'ChipFeatureResolver') '统一读取路径必须调用功能解析器。'
Assert-True ($parserText -match 'ChipFeatureContextProjector') '统一读取路径必须投影形态/姿态上下文。'
Assert-True ($parserText -match 'Features') '统一读取路径必须把功能结果写入模型。'
Assert-True ($builderText -match 'new ChipInformationRequest') '制造信息构建器必须构造统一请求。'
Assert-True ($builderText -match 'ChipInformationSurfaceAccess\.Read\(request\)') '制造信息构建器必须通过统一请求入口读取。'
Assert-True ($builderText -notmatch 'ChipInformationInputComposer\.Combine') '制造信息构建器不得再拼接旧双轨输入。'

Write-Host 'ChipFeatureReadPath PASS'
