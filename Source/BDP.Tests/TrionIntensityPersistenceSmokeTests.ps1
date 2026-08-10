$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$compPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'
$readerPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\ITrionReader.cs'
$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Intensity\TrionIntensityUtility.cs'
$statPath = Join-Path $repoRoot '1.6\Defs\Stats\Trion\StatDefs_Trion.xml'

$compText = Get-Content -LiteralPath $compPath -Raw -Encoding utf8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$statText = Get-Content -LiteralPath $statPath -Raw -Encoding utf8

Assert-True (
    ($compText -match 'private\s+int\s+innateTrionIntensity') -and
    ($compText -match 'private\s+bool\s+trionIntensityInitialized') -and
    ($compText -match 'EnsureTrionIntensityInitialized') -and
    ($compText -match 'Scribe_Values\.Look\(ref innateTrionIntensity,\s*"innateTrionIntensity"') -and
    ($compText -match 'Scribe_Values\.Look\(ref trionIntensityInitialized,\s*"trionIntensityInitialized"')
) 'CompTrion must persist one independently initialized innate Trion intensity.'

Assert-True (
    ($compText -match 'RaceProps\.Humanlike') -and
    ($readerText -match 'InnateTrionIntensity')
) 'Only humanlike pawns may expose the lazily initialized innate intensity through the formal reader.'

Assert-True (Test-Path -LiteralPath $utilityPath) `
    '07B must provide one effective-intensity read utility for UI and activation rules.'
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
Assert-True (
    ($utilityText -match 'Mathf\.FloorToInt') -and
    ($utilityText -match 'Mathf\.Max\(0')
) 'Effective Trion intensity must use one floor-and-clamp rule.'

Assert-True (
    ($statText -match '<defName>BDP_TrionIntensity</defName>') -and
    ($statText -match 'StatPart_TrionInnateIntensity') -and
    ($statText -match 'StatPart_TrionIntensityFloor')
) 'The original StatDef pipeline must carry innate intensity and final integer normalization.'

Write-Output 'TrionIntensityPersistenceSmokeTests PASS'
