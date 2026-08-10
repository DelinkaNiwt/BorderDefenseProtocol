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

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$privateContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\IRangedModulePrivateContext.cs'
$slotPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSlot.cs'
$attackContextPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContext.cs'
$runtimeContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeContext.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$targetingRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs'
$previewRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\PreviewRecord.cs'
$confirmRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs'
$prepareContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\PrepareStageContext.cs'
$rangedProtocolPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$projectileInitContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageContext.cs'
$flightContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Flight\FlightStageContext.cs'
$arrivalContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Arrival\ArrivalStageContext.cs'
$hitContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Hit\HitStageContext.cs'
$impactContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactStageContext.cs'

$privateContextText = if (Test-Path -LiteralPath $privateContextPath) { Read-Source $privateContextPath } else { '' }
$slotText = if (Test-Path -LiteralPath $slotPath) { Read-Source $slotPath } else { '' }
$attackContextText = if (Test-Path -LiteralPath $attackContextPath) { Read-Source $attackContextPath } else { '' }
$runtimeContextText = Read-Source $runtimeContextPath
$sessionText = Read-Source $sessionPath
$targetingRecordText = Read-Source $targetingRecordPath
$previewRecordText = Read-Source $previewRecordPath
$confirmRecordText = Read-Source $confirmRecordPath
$prepareContextText = Read-Source $prepareContextPath
$rangedProtocolText = Read-Source $rangedProtocolPath
$projectileInitContextText = Read-Source $projectileInitContextPath
$flightContextText = Read-Source $flightContextPath
$arrivalContextText = Read-Source $arrivalContextPath
$hitContextText = Read-Source $hitContextPath
$impactContextText = Read-Source $impactContextPath

Assert-True (Test-Path -LiteralPath $privateContextPath) 'IRangedModulePrivateContext.cs must exist.'
Assert-True (Test-Path -LiteralPath $slotPath) 'RangedAttackModuleSlot.cs must exist.'
Assert-True (Test-Path -LiteralPath $attackContextPath) 'AttackContext.cs must exist.'

Assert-True (
    ($privateContextText -match 'interface\s+IRangedModulePrivateContext') -and
    ($privateContextText -match 'IAttackContextNode')
) 'Private context marker must exist.'

Assert-True (
    ($slotText -match 'class\s+RangedAttackModuleSlot') -and
    ($slotText -match 'MountIndex') -and
    ($slotText -match 'IRangedAttackModuleRuntime') -and
    ($slotText -notmatch 'PrivateContext')
) 'Module slot must carry mount index and runtime only.'

Assert-True (
    ($runtimeContextText -match 'public\s+int\s+MountIndex')
) 'Runtime initialize context must expose MountIndex.'

Assert-True (
    ($sessionText -match 'IReadOnlyList<RangedAttackModuleSlot>\s+Slots') -and
    ($sessionText -match 'AttackContext\s+AttackContext') -and
    ($sessionText -match 'GetPrivateContext') -and
    ($sessionText -match 'GetOrCreatePrivateContext') -and
    ($sessionText -match 'TryGetPrivateContext') -and
    ($sessionText -notmatch 'RangedModuleContextSnapshot')
) 'Module session must own slots plus AttackContext-backed private-context access helpers.'

$authorSurfaceTexts = @(
    $targetingRecordText,
    $previewRecordText,
    $confirmRecordText,
    $prepareContextText,
    $projectileInitContextText,
    $flightContextText,
    $arrivalContextText,
    $hitContextText,
    $impactContextText
)

foreach ($text in $authorSurfaceTexts) {
    Assert-True (
        ($text -match 'GetPrivateContext') -and
        ($text -match 'GetOrCreatePrivateContext') -and
        ($text -match 'TryGetPrivateContext')
    ) 'Each author surface must expose module-private-context access helpers.'
}

Assert-True (
    ($attackContextText -match 'class\s+AttackContext') -and
    ($attackContextText -match 'GetOrCreate')
) 'Unified AttackContext must exist for author surfaces to route private nodes.'

Assert-True (
    $rangedProtocolText -match 'SessionResult = lane\.SourceResult'
) 'Dual lane protocol rebuild must bind the lane session surface to the lane source result for private-context isolation.'

Write-Output 'RangedModulePrivateContextBoundarySmokeTests PASS'
