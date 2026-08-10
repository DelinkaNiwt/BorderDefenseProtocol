$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent $PSScriptRoot
$generatorPath = Join-Path $root 'BDP\Core\Trion\Capacity\TrionCapacityPotentialGenerator.cs'
$defPath = Join-Path (Split-Path -Parent $root) '1.6\Defs\Trion\TrionCapacityPotentialDistributionDefs.xml'

Assert-True (Test-Path $generatorPath) '缺少潜在容量生成器。'
Assert-True (Test-Path $defPath) '缺少潜在容量分布定义。'

$generatorText = Get-Content -Raw $generatorPath
$defText = Get-Content -Raw $defPath
$defXml = [xml]$defText
$distribution = $defXml.Defs.'BDP.Core.Trion.Capacity.TrionCapacityPotentialDistributionDef'
$bands = @($distribution.bands.li)
$expectedWeights = @(5, 20, 30, 20, 10, 7, 5, 2, 1)

Assert-True ($generatorText -match 'QuantizationUnit') '潜在容量必须使用配置的量化单位。'
Assert-True ($generatorText -match 'Rand\.RangeInclusive') '档内离散容量必须等概率抽取。'
Assert-True ($generatorText -notmatch 'Gaussian|GenerateCoreCapacity|GenerateBiasedRange') '不得保留钟形或偏斜抽样。'
Assert-True ($defText -notmatch 'coreChance|geniusChance|exceptionalChance|jackpotChance|coreCenter|coreWidth') '不得保留旧概率模型字段。'
Assert-True ($defText -match '<quantizationUnit>100</quantizationUnit>') '容量必须以 100 为单位。'
Assert-True ($bands.Count -eq 9) '必须恰好定义九个生成档位。'

for ($index = 0; $index -lt $expectedWeights.Count; $index++) {
    Assert-True ([float]$bands[$index].weight -eq $expectedWeights[$index]) "第 $($index + 1) 档权重不符合正式配置。"
}

$lastBand = $bands[-1]
Assert-True (
    ([int]$lastBand.minimumCapacity -eq 5000) -and
    ([int]$lastBand.maximumCapacity -eq 5000)
) '最后一档必须固定生成5000。'

Write-Host 'PASS: Trion 潜在容量分布约束成立。'
