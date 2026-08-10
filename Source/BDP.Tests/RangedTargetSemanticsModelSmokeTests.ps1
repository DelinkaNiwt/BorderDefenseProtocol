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
$repoRoot = Split-Path -Parent $sourceRoot
$modelPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedProjectileTargetSemantics.cs'
$modelText = Read-Source $modelPath

Assert-True (
    ($modelText -match 'sealed\s+class\s+RangedProjectileTargetSemantics\s*:\s*IExposable') -and
    ($modelText -match 'LocalTargetInfo\s+IntentFinalTarget') -and
    ($modelText -match 'Vector3\s+IntentFinalPoint') -and
    ($modelText -match 'LocalTargetInfo\s+IntentFirstTarget') -and
    ($modelText -match 'Vector3\s+IntentFirstPoint') -and
    ($modelText -match 'LocalTargetInfo\s+LiveFinalTarget') -and
    ($modelText -match 'Vector3\s+LiveFinalPoint') -and
    ($modelText -match 'LocalTargetInfo\s+LiveNextTarget') -and
    ($modelText -match 'Vector3\s+LiveNextPoint')
) 'RangedProjectileTargetSemantics must expose the A-H target/reference and true-point fields.'

Assert-True (
    ($modelText -match 'CreateFromTargets') -and
    ($modelText -match 'Clone\(\)') -and
    ($modelText -match 'Scribe_TargetInfo\.Look') -and
    ($modelText -match 'Scribe_Values\.Look\(ref\s+\w+,\s*"intentFinalPoint"') -and
    ($modelText -match 'Scribe_Values\.Look\(ref\s+\w+,\s*"liveNextPoint"') -and
    ($modelText -notmatch 'IntentFinalCell|IntentFirstCell|LiveFinalCell|LiveNextCell')
) 'Target semantics must be cloneable, saveable, and use Point naming instead of Cell naming.'

Write-Output 'RangedTargetSemanticsModelSmokeTests PASS'
