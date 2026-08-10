$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$requirementsRoot = Join-Path $repoRoot 'Source\BDP\Core\Requirements'
$coreRoot = Join-Path $repoRoot 'Source\BDP'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$candidateDefs = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs'

$requiredFiles = @(
    'PawnRequirement.cs',
    'PawnRequirementSnapshot.cs',
    'PawnRequirementCheckResult.cs',
    'PawnRequirementEvaluator.cs',
    'PawnRequirementListValidator.cs',
    'TrionIntensityRequirement.cs',
    'SkillLevelRequirement.cs'
)

foreach ($file in $requiredFiles) {
    Assert-True (
        Test-Path -LiteralPath (Join-Path $requirementsRoot $file)
    ) "Core must provide the neutral requirement file: $file"
}

$productionText = (
    Get-ChildItem -LiteralPath $coreRoot,$contentRoot -Recurse -Filter '*.cs' |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
) -join "`n"

Assert-True (
    $productionText -notmatch '\bChipActivationRequirement(?:Snapshot|CheckResult)?\b'
) 'Production code must remove the old chip-specific requirement model names.'
Assert-True (
    $productionText -match 'List<PawnRequirement>\s+ActivationRequirements'
) 'Chip activation authoring must consume the neutral PawnRequirement base type.'
Assert-True (
    ($productionText -match 'class\s+ChipActivationRequirementService') -and
    ($productionText -match 'PawnRequirementEvaluator')
) 'The chip service must remain a thin adapter over the neutral evaluator.'

$candidateText = (
    Get-ChildItem -LiteralPath $candidateDefs -Recurse -Filter '*.xml' |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
) -join "`n"

Assert-True (
    $candidateText -notmatch 'BDP\.Core\.Chips\.Requirements'
) 'Current candidate XML must remove the old chip-owned requirement namespace.'
Assert-True (
    $candidateText -match 'Class="BDP\.Core\.Requirements\.TrionIntensityRequirement"'
) 'Current candidate XML must use the neutral Core requirement namespace.'

Write-Output 'SharedPawnRequirementBoundarySmokeTests PASS'
