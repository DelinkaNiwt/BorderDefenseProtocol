$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$builderPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$builderText = Get-Content -LiteralPath $builderPath -Raw -Encoding UTF8

$collectMatch = [regex]::Match(
    $builderText,
    'private\s+static\s+List<VisualResidentEntry>\s+CollectResidentEntries[\s\S]*?(?=private\s+static\s+VisualExpressionRelationKind)')
Assert-True $collectMatch.Success '必须保留常驻视觉条目收集边界。'

$collectText = $collectMatch.Value
Assert-True (-not ($collectText -match 'ResultKind\s*!=\s*ExpressionResultKind\.Verb')) `
    '带预设的 Hediff（健康状态）或 Passive（被动）条目也必须能进入常驻视觉。'
Assert-True (
    ($collectText -match 'VisualPresetDefName') -and
    ($collectText -match 'CompositeKind\s*!=\s*CompositeExpressionKind\.None') -and
    ($collectText -match '!entry\.IsAvailable') -and
    ($collectText -match '!entry\.CanProject')
) '非 Verb 视觉仍必须满足单侧、可用、可投影和显式视觉预设约束。'

Write-Output 'NonVerbResidentVisualSmokeTests PASS'
