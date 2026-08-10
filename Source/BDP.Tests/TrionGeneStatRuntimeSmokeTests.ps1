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

$compTrionPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'
$readerPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\ITrionReader.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionService.cs'
$injectorPath = Join-Path $repoRoot 'Source\BDP\Core\Bootstrap\Injectors\PawnTrionCompInjector.cs'
$statDefOfPath = Join-Path $repoRoot 'Source\BDP\Core\Genes\TrionStatDefOf.cs'

$compTrionText = Get-Content -LiteralPath $compTrionPath -Raw -Encoding utf8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$injectorText = Get-Content -LiteralPath $injectorPath -Raw -Encoding utf8
$statDefOfText = Get-Content -LiteralPath $statDefOfPath -Raw -Encoding utf8

Assert-True (
    ($injectorText -match 'baseMax = 0f') -and
    ($injectorText -match 'recoveryPerDay = 0f') -and
    ($injectorText -notmatch 'baseMax = 100f')
) 'PawnTrionCompInjector must become a dormant Trion carrier for humanlike pawns.'

Assert-True (
    $compTrionText -match 'parent is Pawn'
) 'CompTrion must branch on Pawn hosts when resolving derived Trion stats.'

Assert-True (
    ($compTrionText -match 'GetStatValue\(TrionStatDefOf\.BDP_TrionCapacity') -and
    ($compTrionText -match 'GetStatValue\(TrionStatDefOf\.BDP_TrionRecoveryRate')
) 'CompTrion must read Trion capacity and recovery from Pawn stats.'

Assert-True (
    ($compTrionText -match 'Props\.baseMax') -and
    ($compTrionText -match 'Props\.recoveryPerDay')
) 'CompTrion must keep non-Pawn hosts on CompProperties_Trion defaults.'

Assert-True (
    $compTrionText -match 'RefreshDerivedStats'
) 'CompTrion must expose a derived stat refresh entry.'

Assert-True (
    $readerText -match 'float RecoveryPerDay \{ get; \}'
) 'ITrionReader must expose RecoveryPerDay for GUI display.'

Assert-True (
    $serviceText -match 'public float RecoveryPerDay'
) 'TrionService must forward RecoveryPerDay.'

Assert-True (
    ($statDefOfText -match 'BDP_TrionCapacity') -and
    ($statDefOfText -match 'BDP_TrionRecoveryRate')
) 'Trion stat runtime must bind to the shared Trion stat DefOf.'

Write-Output 'TrionGeneStatRuntime PASS'
