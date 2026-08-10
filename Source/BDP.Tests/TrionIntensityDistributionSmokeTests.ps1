$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$definitionPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Intensity\TrionIntensityDistributionDef.cs'
$generatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Intensity\TrionIntensityGenerator.cs'
$xmlPath = Join-Path $repoRoot '1.6\Defs\Trion\TrionIntensityDistributionDefs.xml'

Assert-True (Test-Path -LiteralPath $definitionPath) `
    '07B must provide an independent TrionIntensityDistributionDef.'
Assert-True (Test-Path -LiteralPath $generatorPath) `
    '07B must provide an independent TrionIntensityGenerator.'
Assert-True (Test-Path -LiteralPath $xmlPath) `
    '07B must provide the approved data-driven Trion intensity distribution XML.'

$definitionText = Get-Content -LiteralPath $definitionPath -Raw -Encoding utf8
$generatorText = Get-Content -LiteralPath $generatorPath -Raw -Encoding utf8
[xml]$xml = Get-Content -LiteralPath $xmlPath -Raw -Encoding utf8
$entries = @($xml.Defs.'BDP.Core.Trion.Intensity.TrionIntensityDistributionDef'.values.li)

Assert-True (
    ($definitionText -match 'class\s+TrionIntensityWeight') -and
    ($definitionText -match 'int\s+intensity') -and
    ($definitionText -match 'float\s+weight')
) 'The distribution must carry explicit integer intensity and relative weight entries.'

$expectedWeights = @(5, 10, 20, 25, 20, 10, 5, 3, 1.5, 0.5)
Assert-True ($entries.Count -eq 10) 'The formal intensity distribution must contain exactly ten entries.'
for ($index = 0; $index -lt 10; $index++) {
    Assert-True ([int]$entries[$index].intensity -eq ($index + 1)) `
        "Intensity entry $($index + 1) must keep the approved integer value."
    Assert-True ([double]$entries[$index].weight -eq [double]$expectedWeights[$index]) `
        "Intensity entry $($index + 1) must keep the approved weight."
}

Assert-True (
    ($generatorText -match 'FallbackIntensity\s*=\s*4') -and
    ($generatorText -match 'HashSet<int>') -and
    ($generatorText -match 'float\.IsNaN') -and
    ($generatorText -match 'float\.IsInfinity') -and
    ($generatorText -match 'intensity\s*<\s*1') -and
    ($generatorText -match 'intensity\s*>\s*10')
) 'The generator must reject duplicate, non-finite, non-positive, and out-of-range authoring.'

Write-Output 'TrionIntensityDistributionSmokeTests PASS'
