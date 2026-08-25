$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualRoot = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual'

$requestPath = Join-Path $visualRoot 'VisualPoseRequest.cs'
$resolverPath = Join-Path $visualRoot 'VisualPoseResolver.cs'
$launchPath = Join-Path $visualRoot 'TriggerVisualLaunchOriginResolver.cs'
$diagnosticsAccessPath = Join-Path $visualRoot 'Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'
$diagnosticsSnapshotPath = Join-Path $visualRoot 'Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'

$requestText = Get-Content -LiteralPath $requestPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$launchText = Get-Content -LiteralPath $launchPath -Raw -Encoding utf8
$diagnosticsAccessText = Get-Content -LiteralPath $diagnosticsAccessPath -Raw -Encoding utf8
$diagnosticsSnapshotText = Get-Content -LiteralPath $diagnosticsSnapshotPath -Raw -Encoding utf8

Assert-True (
    $requestText -match 'WeaponVisualStageSnapshot\s+WeaponStageSnapshot'
) 'VisualPoseRequest must carry the resolved weapon stage snapshot.'

Assert-True (
    ($resolverText -match 'ResolveWeaponStage\(request\)') -and
    ($resolverText -match 'ResolveGraphic\(\s*request\.IsExecutionActive,\s*weaponStage,\s*request\.SourceThing\)') -and
    ($resolverText -match 'ResolveGraphic\(false,\s*weaponStage,\s*request\.SourceThing\)')
) 'Both complete and texture-only pose paths must select their main graphic by the same resolved stage.'

Assert-True (
    ($resolverText -match 'ResolveGripAnchor\(request, calculation\)') -and
    ($resolverText -match 'ResolveMuzzleAnchor\(request, calculation\)')
) 'Stage-aware graphic selection must leave the existing grip and muzzle anchor formulas in place.'

Assert-True (
    ($launchText -match 'WeaponVisualStageResolver') -and
    ($launchText -match 'PublishedCombatProjection') -and
    ($launchText -match 'WeaponStageSnapshot\s*=')
) 'Live muzzle resolution must use the shared stage resolver and pass its snapshot into the pose request.'

Assert-True (
    ($diagnosticsAccessText -match 'WeaponVisualStageResolver') -and
    ($diagnosticsAccessText -match 'WeaponStageSnapshot\s*=') -and
    ($diagnosticsAccessText -match 'ResolveStageVisibility')
) 'Visual diagnostics must read the same stage snapshot and visibility rule as runtime visuals.'

Assert-True (
    ($diagnosticsSnapshotText -match 'public string WeaponActionStage') -and
    ($diagnosticsSnapshotText -match 'public float WeaponStageProgress01') -and
    ($diagnosticsSnapshotText -match 'public int WeaponStageTicksRemaining') -and
    ($diagnosticsSnapshotText -match 'public bool WeaponStageVisible')
) 'The public diagnostics DTO must expose readable stage, progress, remaining ticks and visibility.'

Write-Output 'WeaponVisualStagePoseIntegrationSmokeTests PASS'
