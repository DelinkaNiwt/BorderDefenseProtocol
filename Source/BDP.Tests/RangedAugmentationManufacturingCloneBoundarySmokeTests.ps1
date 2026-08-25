$ErrorActionPreference = 'Stop'

$modRoot = Join-Path $PSScriptRoot '..\..'
$clonePath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs'
if (-not (Test-Path -LiteralPath $clonePath)) {
    throw ('缺少制造表达复制服务：' + $clonePath)
}

$text = Get-Content -LiteralPath $clonePath -Raw -Encoding UTF8
if ($text -notmatch 'RangedModuleAugmentations\s*=\s*CloneRangedModuleAugmentations\(source\.RangedModuleAugmentations\)') {
    throw '制造成品复制条目时没有保留开放式远程增强声明。'
}

Write-Output 'RangedAugmentationManufacturingCloneBoundarySmokeTests PASS'
