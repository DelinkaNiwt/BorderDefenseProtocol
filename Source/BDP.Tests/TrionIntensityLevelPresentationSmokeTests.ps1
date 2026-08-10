$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$utilityText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Trion\Intensity\TrionIntensityUtility.cs'
) -Raw -Encoding utf8
$compText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'
) -Raw -Encoding utf8
$assessmentText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP.Content\Trion\Talent\CompTrionTalentAssessment.cs'
) -Raw -Encoding utf8
$requirementPath = Join-Path $repoRoot 'Source\BDP\Core\Requirements\TrionIntensityRequirement.cs'
$statText = Get-Content -LiteralPath (
    Join-Path $repoRoot '1.6\Defs\Stats\Trion\StatDefs_Trion.xml'
) -Raw -Encoding utf8

Assert-True (
    ($utilityText -match 'static\s+string\s+FormatLevel\s*\(\s*int\s+value\s*\)') -and
    ($utilityText -match '\+\s*"级"')
) 'TrionIntensityUtility must provide the single integer level formatter.'
Assert-True (
    ($assessmentText -match 'TrionIntensityUtility\.GetEffective\(pawn\)') -and
    ($assessmentText -match 'TrionIntensityUtility\.FormatLevel') -and
    ($assessmentText -match 'HasActiveTrionGland')
) 'The assessed info entry must remain after gland implantation and show the effective level.'
Assert-True (
    ($assessmentText -match 'TrionIntensityUtility\.FormatLevel') -and
    (Test-Path -LiteralPath $requirementPath) -and
    ((Get-Content -LiteralPath $requirementPath -Raw -Encoding utf8) -match 'TrionIntensityUtility\.FormatLevel')
) 'Assessment and requirement text must use the same Trion intensity formatter.'
Assert-True (
    $statText -match '(?s)<defName>BDP_TrionIntensity</defName>.*?<formatString>\{0\}级</formatString>'
) 'The vanilla StatDef presentation must append the 级 unit.'

Write-Output 'TrionIntensityLevelPresentationSmokeTests PASS'
