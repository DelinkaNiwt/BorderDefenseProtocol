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
$bdpSourceRoot = Join-Path $repoRoot "Source\\BDP\\Core"

$contractsPath = Join-Path $bdpSourceRoot "Projectiles\\Visual\\ProjectileVisualAttachmentContracts.cs"
$hostPath = Join-Path $bdpSourceRoot "Projectiles\\Visual\\ProjectileVisualAttachmentHost.cs"
$projectilePath = Join-Path $bdpSourceRoot "Projectiles\\BdpProjectile.cs"
$planPath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\Model\\ProjectileInitPlan.cs"
$initServicePath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\ProjectileInit\\ProjectileInitStageService.cs"

$contractsText = if (Test-Path -LiteralPath $contractsPath) { Read-Source $contractsPath } else { '' }
$hostText = if (Test-Path -LiteralPath $hostPath) { Read-Source $hostPath } else { '' }
$projectileText = if (Test-Path -LiteralPath $projectilePath) { Read-Source $projectilePath } else { '' }
$planText = if (Test-Path -LiteralPath $planPath) { Read-Source $planPath } else { '' }
$initServiceText = if (Test-Path -LiteralPath $initServicePath) { Read-Source $initServicePath } else { '' }

Assert-True (Test-Path -LiteralPath $contractsPath) 'ProjectileVisualAttachmentContracts.cs must exist.'
Assert-True (Test-Path -LiteralPath $hostPath) 'ProjectileVisualAttachmentHost.cs must exist.'

Assert-True (
    ($contractsText -match 'interface\s+IProjectileVisualAttachmentProvider') -and
    ($contractsText -match 'IProjectileVisualAttachment\s+CreateAttachment') -and
    ($contractsText -match 'interface\s+IProjectileVisualAttachment') -and
    ($contractsText -match 'OnLaunch') -and
    ($contractsText -match 'OnFlightSample') -and
    ($contractsText -match 'OnRestored') -and
    ($contractsText -match 'OnTerminate')
) 'Projectile visual attachment contracts must expose provider and four lifecycle callbacks.'

Assert-True (
    ($contractsText -match 'struct\s+ProjectileVisualLaunchContext') -and
    ($contractsText -match 'struct\s+ProjectileVisualFlightSampleContext') -and
    ($contractsText -match 'struct\s+ProjectileVisualRestoreContext') -and
    ($contractsText -match 'struct\s+ProjectileVisualTerminateContext') -and
    ($contractsText -match 'LaunchOrigin') -and
    ($contractsText -match 'LaunchDirection') -and
    ($contractsText -match 'SampleStart') -and
    ($contractsText -match 'SampleEnd') -and
    ($contractsText -match 'CurrentPosition') -and
    ($contractsText -match 'TickDelta')
) 'Projectile visual attachment contexts must expose neutral launch, sample, restore, and terminate facts.'

Assert-True (
    ($contractsText -notmatch 'ProjectileInitPlan') -and
    ($contractsText -notmatch 'RangedAttackModuleSession')
) 'Projectile visual attachment contracts must not leak internal business types.'

Assert-True (
    ($hostText -match 'class\s+ProjectileVisualAttachmentHost') -and
    ($hostText -match 'Initialize') -and
    ($hostText -match 'NotifyLaunch') -and
    ($hostText -match 'NotifyFlightSample') -and
    ($hostText -match 'NotifyRestored') -and
    ($hostText -match 'NotifyTerminate') -and
    ($hostText -match 'IProjectileVisualAttachmentProvider')
) 'ProjectileVisualAttachmentHost must initialize providers and fan out four lifecycle events.'

Assert-True (
    ($projectileText -match 'ProjectileVisualAttachmentHost\s+visualAttachmentHost') -and
    ($projectileText -match 'protected\s+override\s+void\s+TickInterval\s*\(\s*int\s+delta\s*\)') -and
    ($projectileText -match 'NotifyLaunch') -and
    ($projectileText -match 'NotifyFlightSample') -and
    ($projectileText -match 'NotifyRestored') -and
    ($projectileText -match 'NotifyTerminate')
) 'BdpProjectile must own the visual attachment host and publish launch, flight sample, restore, and terminate events.'

Assert-True (
    ($projectileText -notmatch 'BeamTrail')
) 'BdpProjectile must stay neutral and must not know BeamTrail business names.'

Assert-True (
    ($hostText -match 'Initialize\s*\(\s*ThingDef\s+projectileDef,\s*IReadOnlyList<ThingDef>\s+visualAttachmentProviderDefs') -and
    ($hostText -match 'TryInitializeFromProviderDefs') -and
    ($hostText -match 'TryInitializeFromDef') -and
    ($projectileText -match 'launchPlan\.VisualAttachmentProviderDefs') -and
    ($projectileText -notmatch 'BeamTrail')
) 'Projectile visual host must support neutral source-def providers without knowing BeamTrail.'

Assert-True (
    ($planText -match 'List<ThingDef>\s+VisualAttachmentProviderDefs') -and
    ($planText -match 'Scribe_Collections\.Look\(ref visualAttachmentProviderDefs,\s*"visualAttachmentProviderDefs",\s*LookMode\.Def\)')
) 'ProjectileInitPlan must persist visual attachment provider defs for save-load restore.'

Assert-True (
    ($initServiceText -match 'ResolveVisualAttachmentProviderDef') -and
    ($initServiceText -match 'IProjectileVisualAttachmentProvider') -and
    ($initServiceText -match 'SourceReference\.ChipDefName')
) 'ProjectileInitStageService must freeze source chip defs that expose visual attachment providers.'

Write-Output 'ProjectileVisualAttachmentHostSmokeTests PASS'
