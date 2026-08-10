$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$productionPaths = @(
    (Join-Path $repoRoot 'Source\BDP\Core\Trion'),
    (Join-Path $repoRoot 'Source\BDP\Core\Genes'),
    (Join-Path $repoRoot 'Source\BDP.Content\Trion\Talent'),
    (Join-Path $repoRoot '1.6\Defs'),
    (Join-Path $repoRoot '1.6\Content\Defs\Trion')
)
$productionFiles = $productionPaths |
    ForEach-Object { Get-ChildItem -LiteralPath $_ -Recurse -File -Include '*.cs', '*.xml' }
$productionText = ($productionFiles |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"

Assert-True (
    ($productionText -match 'TrionTalentAssessment') -and
    ($productionText -match 'TrionCapacityPotential') -and
    ($productionText -match 'TrionIntensity') -and
    ($productionText -match 'TrionTalentAssessmentCompleted')
) '07B must introduce the approved Trion talent, capacity-potential, intensity, and completion names.'

Assert-True (
    ($productionText -notmatch '\bTrionAssessment') -and
    ($productionText -notmatch '\bTrionPotential') -and
    ($productionText -notmatch '\bPotentialAssessed\b') -and
    ($productionText -notmatch 'BDP_TrionPotential_')
) 'Current production source and defs must physically remove the old ambiguous Trion names.'

Assert-True (
    ($productionText -match 'Trion天赋检测') -and
    ($productionText -match 'Trion容量潜质') -and
    ($productionText -match 'Trion释放力')
) 'Player-facing Trion talent terminology must use the approved Chinese names.'

Assert-True (
    $productionText -notmatch 'defName>BDP_TrionTalent<'
) 'TrionTalent is an umbrella assessment concept and must not become an aggregate numeric StatDef.'

Write-Output 'TrionTalentNamingBoundarySmokeTests PASS'
