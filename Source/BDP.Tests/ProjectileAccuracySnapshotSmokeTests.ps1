# 本脚本使用 UTF-8 BOM，确保 Windows PowerShell 正确读取中文断言。
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
$bdpCoreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$snapshotPath = Join-Path $bdpCoreRoot 'AttackExecution\RangedProtocol\Model\ProjectileAccuracySnapshot.cs'
$planPath = Join-Path $bdpCoreRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$arrivalContextPath = Join-Path $bdpCoreRoot 'Projectiles\RangedFlightProtocol\Arrival\ArrivalStageContext.cs'
$verbPath = Join-Path $bdpCoreRoot 'Verbs\BdpVerb_Shoot.cs'
$snapshotText = if (Test-Path -LiteralPath $snapshotPath) {
    Read-Source $snapshotPath
} else {
    ''
}
$planText = Read-Source $planPath
$arrivalContextText = Read-Source $arrivalContextPath
$verbText = Read-Source $verbPath

Assert-True (Test-Path -LiteralPath $snapshotPath) `
    'ProjectileAccuracySnapshot.cs must exist.'

Assert-True (
    ($snapshotText -match 'public\s+sealed\s+class\s+ProjectileAccuracySnapshot\s*:\s*IExposable') -and
    ($snapshotText -match 'public\s+bool\s+IsAvailable') -and
    ($snapshotText -match 'public\s+float\s+StandardAimChance') -and
    ($snapshotText -match 'public\s+float\s+IgnoringPostureAimChance') -and
    ($snapshotText -match 'public\s+float\s+PassCoverChance') -and
    ($snapshotText -match 'public\s+float\s+ForcedMissRadius') -and
    ($snapshotText -match 'public\s+float\s+AccuracyFactor') -and
    ($snapshotText -match 'ProjectileAccuracySnapshot\s+CloneTyped') -and
    ($snapshotText -match 'void\s+ExposeData\s*\(')
) 'Accuracy snapshot must expose and persist neutral original-shot facts.'

Assert-True (
    ($planText -match 'public\s+ProjectileAccuracySnapshot\s+AccuracySnapshot') -and
    ($planText -match 'Scribe_Deep\.Look\(ref accuracySnapshot,\s*"accuracySnapshot"\)')
) 'ProjectileInitPlan must persist the per-projectile accuracy snapshot.'

Assert-True (
    $arrivalContextText -match 'public\s+ProjectileAccuracySnapshot\s+AccuracySnapshot\s*\{\s*get;\s*\}'
) 'ArrivalStageContext must expose the frozen accuracy snapshot read-only.'

Assert-True (
    ($verbText -match 'ShotReport\s+shotReport') -and
    ($verbText -match 'CaptureAccuracySnapshot\(plan,\s*shotReport,\s*accuracyFactor,\s*forcedMissRadius\)') -and
    ($verbText -match 'private\s+static\s+void\s+CaptureAccuracySnapshot')
) 'BdpVerb_Shoot must capture accuracy facts immediately after the original ShotReport.'

Assert-True (
    ([regex]::Matches($verbText, 'AccuracySnapshot\s*=')).Count -eq 1
) 'The formal ranged host must have one accuracy-snapshot assignment point.'

Assert-True (
    ($snapshotText -notmatch 'BDP\.Content') -and
    ($snapshotText -notmatch 'Viper|RoutePath|Anchor|Convergence')
) 'Core accuracy snapshot must not contain Viper route business naming.'

Write-Output 'ProjectileAccuracySnapshotSmokeTests PASS'
