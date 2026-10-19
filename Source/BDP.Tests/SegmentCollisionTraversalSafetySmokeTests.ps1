# 验证客观穿格扫描在异常数值和近似边界相等时仍然有界收敛。
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs'
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8

# 这个坐标组来自原算法的确定性失收敛案例：旧逻辑会在目标格外继续推进。
$reproduction = @(
    'startCell',
    'endCell',
    'MaxTraversedCellCount',
    'float.IsNaN',
    'float.IsInfinity',
    'canStepX',
    'canStepZ'
)

foreach ($marker in $reproduction) {
    Assert-True ($serviceText -match [regex]::Escape($marker)) "穿格扫描缺少安全收敛边界：$marker。"
}

Assert-True (
    $serviceText -match 'iterationCount' -or
    $serviceText -match 'traversalCount' -or
    $serviceText -match 'maxIterations'
) '穿格扫描必须有独立于输入路径的最大迭代保护，不能只依赖 while 的终点条件。'

Assert-True (
    $serviceText -match 'currentX\s*!=\s*endCell\.x' -and
    $serviceText -match 'currentZ\s*!=\s*endCell\.z'
) '每个轴到达终点后都必须停止继续沿该轴推进，避免近似相等分支越过终点。'

Write-Output 'SegmentCollisionTraversalSafetySmokeTests PASS'
