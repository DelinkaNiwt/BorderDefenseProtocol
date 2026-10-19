$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$renderRoot = Join-Path $repoRoot 'Source\BDP.Content\Information\Features\Rendering'
$shieldRenderer = Get-Content (Join-Path $renderRoot 'ShieldFeatureRenderer.cs') -Raw -Encoding UTF8
$genericRenderer = Get-Content (Join-Path $renderRoot 'GenericChipFeatureRenderer.cs') -Raw -Encoding UTF8
$registry = Get-Content (Join-Path $renderRoot 'ChipFeatureRendererRegistry.cs') -Raw -Encoding UTF8
$result = Get-Content (Join-Path $renderRoot 'ChipFeatureRenderResult.cs') -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True (($shieldRenderer -match 'BlockArc') -and ($shieldRenderer -match 'ShieldCapacity')) '护盾专用展示器必须识别已审议的抵挡角度与容量属性。'
Assert-True ($shieldRenderer -notmatch 'ShieldReflection|ShieldConversion|ShieldBypass|BlockTypes') '护盾专用展示器不得继续消费已否决的技术属性。'
Assert-True ($result -match 'ConsumedPropertyIds') '展示结果必须暴露专用展示器已消费的属性集合。'
Assert-True ($registry -match 'GenericChipFeatureRenderer') '注册表必须在专用展示器后追加通用回退。'
Assert-True ($genericRenderer -match 'feature\.Properties') '通用展示器必须直接读取统一属性。'
Assert-True (($result -match 'ChipInformationNode\s+Feature') -and ($result -match 'ChipInformationProperty\s+Property')) `
    '展示结果必须引用统一节点和属性，并保留属性状态。'

Write-Host 'ChipFeatureRendererFallback PASS'
