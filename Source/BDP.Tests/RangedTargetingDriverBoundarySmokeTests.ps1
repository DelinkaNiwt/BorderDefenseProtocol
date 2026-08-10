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

$driverPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInteractionDriver.cs'
$driveResultPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInteractionDriveResult.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'

$driverText = if (Test-Path -LiteralPath $driverPath) { Get-Content -LiteralPath $driverPath -Raw -Encoding utf8 } else { '' }
$driveResultText = if (Test-Path -LiteralPath $driveResultPath) { Get-Content -LiteralPath $driveResultPath -Raw -Encoding utf8 } else { '' }
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $driverPath
) 'TargetingInteractionDriver.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $driveResultPath
) 'TargetingInteractionDriveResult.cs must exist.'

Assert-True (
    ($driverText -match 'class\s+TargetingInteractionDriver') -and
    ($driverText -match 'TargetingRecord') -and
    ($driverText -match 'TargetingInteractionDriveResult')
) 'TargetingInteractionDriver must turn the current targeting record into a neutral next-step drive result.'

Assert-True (
    ($driveResultText -match 'class\s+TargetingInteractionDriveResult') -and
    ($driveResultText -match 'TargetingRecord') -and
    ($driveResultText -match 'KeepTargeting') -and
    ($driveResultText -match 'EnterConfirm') -and
    ($driveResultText -match 'CancelTargeting')
) 'TargetingInteractionDriveResult must describe keep-targeting, enter-confirm, and cancel-targeting outcomes.'

Assert-True (
    ($targetingSourceText -match 'TargetingInteractionDriver') -and
    ($targetingSourceText -match 'TargetingInteractionDriveResult')
) 'AttackExecutionTargetingSource must depend on the neutral interaction driver instead of only local ad-hoc branching.'

Assert-True (
    ($targetingSourceText -match 'DestinationSelector\s*=>') -and
    ($targetingSourceText -notmatch 'DestinationSelector\s*=>\s*null')
) 'DestinationSelector must no longer be permanently null after the driver bridge is introduced.'

Assert-True (
    ($targetingSourceText -match 'OrderForceTarget\(LocalTargetInfo target\)[\s\S]*TargetingInputFrame') -and
    ($targetingSourceText -match 'OrderForceTarget\(LocalTargetInfo target\)[\s\S]*TargetingInteractionDriver') -and
    ($targetingSourceText -match 'OrderForceTarget\(LocalTargetInfo target\)[\s\S]*EnterConfirm')
) 'OrderForceTarget must drive one real input round first and only then decide whether to confirm or continue.'

Assert-True (
    ($targetingSourceText -notmatch 'BuildConfirmRecord\(ResolvedTargetingContext context, LocalTargetInfo target\)[\s\S]*ConfirmRequested\s*=\s*true')
) 'BuildConfirmRecord must no longer fabricate a fresh confirm-requested targeting frame.'

Write-Output 'RangedTargetingDriverBoundarySmokeTests PASS'
