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

$stopRequestPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedStageStopRequest.cs'
$stopRequestText = Read-Source $stopRequestPath

Assert-True ($stopRequestText -match 'public\s+sealed\s+class\s+RangedStageStopRequest') 'Stop request must be public for author modules.'
Assert-True ($stopRequestText -match 'public\s+enum\s+RangedStageStopScope') 'Stop scope enum must be public for author modules.'
Assert-True ($stopRequestText -match 'public\s+bool\s+IsRequested') 'Stop request must expose IsRequested.'
Assert-True ($stopRequestText -match 'public\s+string\s+Reason') 'Stop request must expose Reason.'
Assert-True ($stopRequestText -match 'public\s+RangedStageStopScope\s+Scope') 'Stop request must expose Scope.'
Assert-True ($stopRequestText -match 'Stage') 'Stop scope must include Stage.'
Assert-True ($stopRequestText -match 'Attack') 'Stop scope must include Attack.'
Assert-True ($stopRequestText -match 'Projectile') 'Stop scope must include Projectile.'

$stageFiles = @(
    'AttackExecution\TargetingProtocol\Model\ManualEntryRecord.cs',
    'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs',
    'AttackExecution\TargetingProtocol\Model\PreviewRecord.cs',
    'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs',
    'AttackExecution\RangedProtocol\Aim\AimContribution.cs',
    'AttackExecution\RangedProtocol\Prepare\PrepareContribution.cs',
    'AttackExecution\RangedProtocol\Fire\FireContribution.cs',
    'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs',
    'Projectiles\RangedFlightProtocol\Flight\FlightContribution.cs',
    'Projectiles\RangedFlightProtocol\Arrival\ArrivalContribution.cs',
    'Projectiles\RangedFlightProtocol\Hit\HitContribution.cs',
    'Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'
)

foreach ($relativePath in $stageFiles) {
    $text = Read-Source (Join-Path $bdpSourceRoot $relativePath)
    Assert-True ($text -match 'public\s+RangedStageStopRequest\s+Stop\s*\{') "$relativePath must expose unified Stop request."
}

$oldAbortFiles = @(
    'AttackExecution\RangedProtocol\Aim\AimContribution.cs',
    'AttackExecution\RangedProtocol\Prepare\PrepareContribution.cs',
    'AttackExecution\RangedProtocol\Fire\FireContribution.cs'
)

foreach ($relativePath in $oldAbortFiles) {
    $text = Read-Source (Join-Path $bdpSourceRoot $relativePath)
    Assert-True ($text -notmatch 'AbortRequested') "$relativePath must not keep AbortRequested as a parallel author stop API."
    Assert-True ($text -notmatch 'AbortReason') "$relativePath must not keep AbortReason as a parallel author stop API."
}

Write-Output 'RangedModuleStageStopBoundarySmokeTests PASS'
