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
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$coreTrionRoot = Join-Path $coreRoot 'Trion'
$contentTalentRoot = Join-Path $contentRoot 'Trion\Talent'

$coreReaderPath = Join-Path $coreTrionRoot 'ITrionReader.cs'
$coreCommandsPath = Join-Path $coreTrionRoot 'ITrionCommands.cs'
$coreCompPath = Join-Path $coreTrionRoot 'CompTrion.cs'
$coreServicePath = Join-Path $coreTrionRoot 'TrionService.cs'

$contentStatePath = Join-Path $contentTalentRoot 'CompTrionTalentAssessment.cs'
$contentInjectorPath = Join-Path $contentTalentRoot 'PawnTrionTalentAssessmentInjector.cs'
$contentDetectorPath = Join-Path $contentTalentRoot 'Building_TrionDetector.cs'
$contentBandPath = Join-Path $contentTalentRoot 'Capacity\TrionCapacityPotentialBandDef.cs'

$coreReaderText = Get-Content -LiteralPath $coreReaderPath -Raw -Encoding utf8
$coreCommandsText = Get-Content -LiteralPath $coreCommandsPath -Raw -Encoding utf8
$coreCompText = Get-Content -LiteralPath $coreCompPath -Raw -Encoding utf8
$coreServiceText = Get-Content -LiteralPath $coreServicePath -Raw -Encoding utf8

Assert-True (-not (Test-Path -LiteralPath (Join-Path $coreTrionRoot 'Talent\Building_TrionDetector.cs'))) 'Trion detector building must not remain in Core.'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $coreTrionRoot 'Talent\TrionTalentAssessmentService.cs'))) 'Trion talent assessment service must not remain in Core.'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $coreTrionRoot 'Talent\Jobs\JobDriver_TrionTalentAssessment.cs'))) 'Trion talent assessment JobDriver must not remain in Core.'

Assert-True (Test-Path -LiteralPath $contentStatePath) 'Content must own the Trion talent assessment state component.'
Assert-True (Test-Path -LiteralPath $contentInjectorPath) 'Content must inject the assessment state component into humanlike Pawn defs.'
Assert-True (Test-Path -LiteralPath $contentDetectorPath) 'Content must own the Trion detector building.'
Assert-True (Test-Path -LiteralPath $contentBandPath) 'Content must own the player-facing capacity potential band definition.'

Assert-True ($coreReaderText -notmatch 'TrionTalentAssessmentCompleted') 'ITrionReader must not expose the detector-specific completion flag.'
Assert-True ($coreCommandsText -notmatch 'TryMarkTrionTalentAssessmentCompleted') 'ITrionCommands must not expose the detector-specific completion command.'
Assert-True ($coreCompText -notmatch 'trionTalentAssessmentCompleted|TrionTalentAssessmentCompleted|TryMarkTrionTalentAssessmentCompleted') 'CompTrion must not store or commit detector-specific state.'
Assert-True ($coreServiceText -notmatch 'TrionTalentAssessmentCompleted|TryMarkTrionTalentAssessmentCompleted') 'TrionService must not forward detector-specific state.'

$contentDefs = Join-Path $repoRoot '1.6\Content\Defs'
$contentDetectorDefPath = Join-Path $contentDefs 'Buildings\Trion\ThingDefs_TrionDetector.xml'
$contentPortableDefPath = Join-Path $contentDefs 'Things\Trion\ThingDefs_TrionPortableDetector.xml'
$contentJobDefPath = Join-Path $contentDefs 'Jobs\Trion\JobDefs_TrionTalentAssessment.xml'
$contentWorkGiverDefPath = Join-Path $contentDefs 'WorkGivers\Trion\WorkGiverDefs_TrionDetector.xml'
$contentBandDefPath = Join-Path $contentDefs 'Trion\Talent\TrionCapacityPotentialBandDefs.xml'

Assert-True (Test-Path -LiteralPath $contentDetectorDefPath) 'Trion detector ThingDef must be under Content/Defs.'
Assert-True (Test-Path -LiteralPath $contentPortableDefPath) 'Portable Trion detector ThingDef must be under Content/Defs.'
Assert-True (Test-Path -LiteralPath $contentJobDefPath) 'Trion talent assessment JobDefs must be under Content/Defs.'
Assert-True (Test-Path -LiteralPath $contentWorkGiverDefPath) 'Trion detector WorkGiverDefs must be under Content/Defs.'
Assert-True (Test-Path -LiteralPath $contentBandDefPath) 'Capacity potential band Defs must be under Content/Defs.'

Write-Output 'TrionTalentContentBoundary PASS'
