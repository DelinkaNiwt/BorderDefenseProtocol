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

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $sourceRoot
$homingRoot = Join-Path $projectRoot 'Source\BDP.Content\RangedModules\Homing'

$moduleText = Read-Source (Join-Path $homingRoot 'HomingModule.cs')
$stateText = Read-Source (Join-Path $homingRoot 'HomingState.cs')
$pathBuilderText = Read-Source (Join-Path $homingRoot 'HomingPathBuilder.cs')

Assert-True (
    ($moduleText -match 'IsClosingHitWindowCandidate') -and
    ($moduleText -match 'enter_hit_check_closing')
) 'Homing arrival decisions must accept a closing target inside the hit window before loss handling.'

Assert-True (
    ($moduleText -match 'BuildTerminalApproachPath') -and
    ($moduleText -notmatch 'HomingPathBuilder\.BuildReleasePath')
) 'A still-valid target inside hard release distance must use terminal pursuit instead of unconditional forward release.'

Assert-True (
    ($stateText -match 'LastTargetObservationTick') -and
    ($stateText -match 'lastTargetObservationTick')
) 'Homing state must persist the last target observation tick for time-aware prediction.'

Assert-True (
    ($stateText -match 'HasExpectedTargetPosition') -and
    ($stateText -match 'ExpectedTargetPosition')
) 'Homing state must persist the generic expected target position used by the pursuit controller.'

Assert-True (
    ($moduleText -match 'targetPredictionResidual') -and
    ($moduleText -match '(?s)IsTargetLost\s*\([^)]*targetPredictionResidual')
) 'Homing arrival decisions must calculate and pass pursuit prediction residual into loss handling.'

Assert-True (
    ($moduleText -match '(?s)private\s+static\s+bool\s+IsTargetLost\s*\([^)]*float\s+targetPredictionResidual') -and
    ($moduleText -match '(?s)targetPredictionResidual\s*<=.*?return\s+false')
) 'Homing loss must not be triggered by the current angle alone when the pursuit residual is within tolerance.'

Assert-True (
    ($moduleText -notmatch 'TargetHasMoved|TargetMoved|PawnMoved|targetMoved') -and
    ($stateText -notmatch 'TargetHasMoved|TargetMoved|PawnMoved|targetMoved')
) 'Homing must not encode the requested behavior as an explicit target-moved or Pawn-moved scenario branch.'

Assert-True (
    ($pathBuilderText -match 'float observationTicks') -and
    ($pathBuilderText -match 'float nextSegmentTicks') -and
    ($pathBuilderText -match 'nextSegmentTicks / safeObservationTicks')
) 'Homing prediction must use observed target motion over the next segment duration.'

Write-Output 'HomingPursuitDecisionSmokeTests PASS'
