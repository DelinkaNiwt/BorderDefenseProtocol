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

$addonInterfacePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\IRangedStageAddonModule.cs'
$addonContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedStageAddonContext.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$manualResolverPath = Join-Path $bdpSourceRoot 'Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$protocolServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$flightProtocolServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\RangedFlightProtocolService.cs'

$addonInterfaceText = Read-Source $addonInterfacePath
$addonContextText = Read-Source $addonContextPath
$sessionText = Read-Source $sessionPath
$targetingSourceText = Read-Source $targetingSourcePath
$manualResolverText = Read-Source $manualResolverPath
$protocolServiceText = Read-Source $protocolServicePath
$flightProtocolServiceText = Read-Source $flightProtocolServicePath

Assert-True ($addonInterfaceText -match 'public\s+interface\s+IRangedStageAddonModule') 'Addon interface must be public for authors.'
Assert-True ($addonInterfaceText -match 'void\s+AfterStage\s*\(\s*in\s+RangedStageAddonContext\s+context\s*\)') 'Addon interface must expose AfterStage(in RangedStageAddonContext context).'

Assert-True ($addonContextText -match 'public\s+readonly\s+struct\s+RangedStageAddonContext') 'Addon context must be public for authors.'
Assert-True ($addonContextText -match 'public\s+RangedStageKind\s+Stage') 'Addon context must expose Stage.'
Assert-True ($addonContextText -match 'public\s+string\s+AttackInstanceId') 'Addon context must expose AttackInstanceId.'
Assert-True ($addonContextText -match 'public\s+string\s+ResultId') 'Addon context must expose ResultId.'
Assert-True ($addonContextText -match 'public\s+int\s+EmitIndex') 'Addon context must expose EmitIndex.'
Assert-True ($addonContextText -match 'public\s+AttackContextSnapshot\s+AttackContextSnapshot') 'Addon context must expose AttackContextSnapshot.'
Assert-True ($addonContextText -notmatch 'SharedState') 'Addon context must not expose SharedState.'
Assert-True ($addonContextText -match 'public\s+enum\s+RangedStageKind') 'Stage kind enum must be public for authors.'

Assert-True ($sessionText -match 'GetAddonModules') 'Module session must be able to filter addon modules.'
Assert-True ($manualResolverText -match 'GetAddonModules') 'ManualEntry host must dispatch addon modules.'
Assert-True ($targetingSourceText -match 'GetAddonModules') 'Targeting host must dispatch addon modules.'
Assert-True ($protocolServiceText -match 'GetAddonModules') 'Ranged attack protocol must pass addon modules into stage services.'
Assert-True ($flightProtocolServiceText -match 'GetAddonModules') 'Flight protocol must pass addon modules into stage services.'

Write-Output 'RangedModuleStageAddonBoundarySmokeTests PASS'
