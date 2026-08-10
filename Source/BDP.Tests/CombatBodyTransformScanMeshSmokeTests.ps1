$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$meshCachePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyScanMeshCache.cs'

Assert-True -Condition (Test-Path -LiteralPath $meshCachePath) -Message '缺少扫描裁切网格缓存。'
$meshCacheText = Get-Content -LiteralPath $meshCachePath -Raw -Encoding utf8

Assert-True -Condition ($meshCacheText -match 'private const int CutStepCount = 24;') -Message '裁切必须固定为 24 档。'
Assert-True -Condition ($meshCacheText -match 'new Mesh\[2, 2, CutStepCount \+ 1\]') -Message '缓存必须覆盖上下段、翻转和 0..24 档。'
Assert-True -Condition ($meshCacheText -match 'internal static Mesh GetMesh\(bool keepUpper, bool flipped, float normalizedCut\)') -Message '缺少内部网格读取面。'
Assert-True -Condition ($meshCacheText -match 'Mathf\.Clamp01\(normalizedCut\)') -Message '读取面必须夹取裁切值。'
Assert-True -Condition ($meshCacheText -match 'Mathf\.RoundToInt\([^\r\n]*CutStepCount') -Message '读取面必须量化到固定档位。'
Assert-True -Condition ($meshCacheText -match 'float bottomV = keepUpper \? cut : 0f;') -Message '上段网格的 UV 下界必须跟随裁切值。'
Assert-True -Condition ($meshCacheText -match 'float topV = keepUpper \? 1f : cut;') -Message '下段网格的 UV 上界必须跟随裁切值。'
Assert-True -Condition ($meshCacheText -notmatch 'GetMesh[\s\S]*new Mesh\(') -Message 'GetMesh 每帧读取路径不得创建网格。'

Write-Output 'CombatBodyTransformScanMesh PASS'
