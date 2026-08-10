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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$runtimeInterfacePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\IRangedAttackModuleRuntime.cs'
$runtimeContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeContext.cs'
$runtimeResolverPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleResolver.cs'
$runtimeHostPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeHost.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$triggerRuntimeServicesPath = Join-Path $bdpSourceRoot 'Trigger\Runtime\TriggerRuntimeServices.cs'
$attackSurfacePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionSurfaceAccess.cs'
$attackProtocolServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$attackEntryPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\RangedAttackEntry.cs'
$projectileInitPlanPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$flightProtocolServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\RangedFlightProtocolService.cs'
$shootVerbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'

$runtimeInterfaceText = if (Test-Path -LiteralPath $runtimeInterfacePath) { Get-Content -LiteralPath $runtimeInterfacePath -Raw -Encoding utf8 } else { '' }
$runtimeContextText = if (Test-Path -LiteralPath $runtimeContextPath) { Get-Content -LiteralPath $runtimeContextPath -Raw -Encoding utf8 } else { '' }
$runtimeResolverText = if (Test-Path -LiteralPath $runtimeResolverPath) { Get-Content -LiteralPath $runtimeResolverPath -Raw -Encoding utf8 } else { '' }
$runtimeHostText = if (Test-Path -LiteralPath $runtimeHostPath) { Get-Content -LiteralPath $runtimeHostPath -Raw -Encoding utf8 } else { '' }
$sessionText = if (Test-Path -LiteralPath $sessionPath) { Get-Content -LiteralPath $sessionPath -Raw -Encoding utf8 } else { '' }
$triggerRuntimeServicesText = Get-Content -LiteralPath $triggerRuntimeServicesPath -Raw -Encoding utf8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$attackProtocolServiceText = Get-Content -LiteralPath $attackProtocolServicePath -Raw -Encoding utf8
$attackEntryText = Get-Content -LiteralPath $attackEntryPath -Raw -Encoding utf8
$projectileInitPlanText = Get-Content -LiteralPath $projectileInitPlanPath -Raw -Encoding utf8
$flightProtocolServiceText = Get-Content -LiteralPath $flightProtocolServicePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $runtimeInterfacePath) 'IRangedAttackModuleRuntime.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimeContextPath) 'RangedAttackModuleRuntimeContext.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimeResolverPath) 'RangedAttackModuleResolver.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimeHostPath) 'RangedAttackModuleRuntimeHost.cs must exist.'
Assert-True (Test-Path -LiteralPath $sessionPath) 'RangedAttackModuleSession.cs must exist.'

Assert-True (
    ($runtimeInterfaceText -match 'interface\s+IRangedAttackModuleRuntime') -and
    ($runtimeInterfaceText -match 'Initialize\s*\(\s*RangedAttackModuleRuntimeContext\s+context\s*\)')
) 'IRangedAttackModuleRuntime must expose Initialize(RangedAttackModuleRuntimeContext context).'

Assert-True (
    ($runtimeContextText -match 'Pawn\s+Pawn') -and
    ($runtimeContextText -match 'FormalExpressionResult\s+Result') -and
    ($runtimeContextText -match 'RangedModuleMountConfig\s+Mount') -and
    ($runtimeContextText -match 'BdpRangedAttackModuleDef\s+ModuleDef') -and
    ($runtimeContextText -match 'RangedModuleConfigNode\s+Config')
) 'RangedAttackModuleRuntimeContext must expose Pawn, Result, Mount, ModuleDef, and Config.'

Assert-True (
    ($runtimeResolverText -match 'class\s+RangedAttackModuleResolver') -and
    ($runtimeResolverText -match 'IRangedAttackModuleRuntime') -and
    ($runtimeResolverText -match 'Activator\.CreateInstance')
) 'RangedAttackModuleResolver must create runtime instances from module defs.'

Assert-True (
    ($runtimeHostText -match 'class\s+RangedAttackModuleRuntimeHost') -and
    ($runtimeHostText -match 'CreateSession')
) 'RangedAttackModuleRuntimeHost must expose CreateSession.'

Assert-True (
    ($sessionText -match 'class\s+RangedAttackModuleSession') -and
    ($sessionText -match 'IReadOnlyList<RangedModuleMountConfig>\s+Mounts') -and
    ($sessionText -match 'IReadOnlyList<RangedAttackModuleSlot>\s+Slots') -and
    ($sessionText -match 'AttackContext\s+AttackContext') -and
    ($sessionText -notmatch 'SharedState')
) 'RangedAttackModuleSession must hold ordered mount snapshots, runtime slots, and unified AttackContext only.'

Assert-True (
    ($triggerRuntimeServicesText -match 'RangedAttackModuleResolver') -and
    ($triggerRuntimeServicesText -match 'RangedAttackModuleRuntimeHost')
) 'TriggerRuntimeServices must own the ranged module resolver and runtime host.'

Assert-True (
    $attackSurfaceText -match 'ResolveRangedModuleRuntimeHost'
) 'AttackExecutionSurfaceAccess must expose the ranged module runtime host read surface.'

Assert-True (
    ($attackProtocolServiceText -match 'CreateSession\(request\.Pawn,\s*result\)') -and
    ($attackProtocolServiceText -match 'AttackContext\.FromSnapshot\(request\.AttackContextSnapshot\)') -and
    ($attackProtocolServiceText -notmatch 'request\.ModuleSession')
) 'RangedAttackProtocolService must rebuild session from runtime host and restore AttackContext from request snapshot.'

Assert-True (
    $attackProtocolServiceText -match 'CreateModuleSession\(request,\s*laneEntry\.SessionResult\)'
) 'Dual source-lane protocol path must rebuild each lane module session from the lane session result.'

Assert-True (
    $attackEntryText -match 'RangedAttackModuleSession\s+ModuleSession'
) 'RangedAttackEntry must carry the ranged module session.'

Assert-True (
    ($projectileInitPlanText -match 'AttackContextSnapshot\s+AttackContextSnapshot') -and
    ($projectileInitPlanText -notmatch 'RangedAttackModuleSession\s+ModuleSession')
) 'ProjectileInitPlan must carry only AttackContextSnapshot across the freeze boundary.'

Assert-True (
    ($flightProtocolServiceText -match 'CreateModuleSession\(ProjectileInitPlan\s+initPlan\)') -and
    ($flightProtocolServiceText -match 'CreateRangedModuleSession\(launcher,\s*result\)') -and
    ($flightProtocolServiceText -match 'ImportPrivateContexts\(initPlan\.AttackContextSnapshot\)') -and
    ($flightProtocolServiceText -match 'AttackContextSnapshot') -and
    ($flightProtocolServiceText -notmatch 'initPlan\.ModuleSession')
) 'RangedFlightProtocolService must rebuild flight-half session from init plan snapshot instead of carrying runtime session.'

Assert-True (
    ($shootVerbText -match 'AttackSessionToken\.Create\(\s*context\.Pawn \?\? CasterPawn,\s*context\.HostResultId') -and
    ($shootVerbText -match 'LogHostSessionBound\([^)]*context\.HostResultId')
) 'BdpVerb_Shoot must bind runtime sessions and diagnostics to the effective HostResultId.'

Assert-True (
    ($continuationPlannerText -match 'verb\.HostSessionToken\.ResultId') -and
    ($continuationPlannerText -match 'TryCreatePublishedRangedModuleSession\(\s*pawn,\s*verb\.HostSessionToken\.ResultId')
) 'RangedVerbContinuationPlanner must continue from the effective host session token result id.'

Assert-True (
    ($attackProtocolServiceText -match 'ResolveSessionResult\(request,\s*step,\s*result\)') -and
    ($attackProtocolServiceText -match 'step\?\.HostResultId') -and
    ($attackProtocolServiceText -match 'FindResult\(request,\s*step\.HostResultId\)')
) 'RangedAttackProtocolService must resolve the session result from step.HostResultId before falling back to the entry result.'

Write-Output 'RangedModuleRuntimeSessionSmokeTests PASS'
