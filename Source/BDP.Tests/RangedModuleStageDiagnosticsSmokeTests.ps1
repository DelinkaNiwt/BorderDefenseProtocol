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

$diagnosticsPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Diagnostics\RangedModuleStageDiagnostics.cs'
$dispatcherPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedStageAddonDispatcher.cs'
$aimServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Aim\AimStageService.cs'
$prepareServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\PrepareStageService.cs'
$fireServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Fire\FireStageService.cs'

$diagnosticsText = Read-Source $diagnosticsPath
$dispatcherText = Read-Source $dispatcherPath
$aimServiceText = Read-Source $aimServicePath
$prepareServiceText = Read-Source $prepareServicePath
$fireServiceText = Read-Source $fireServicePath

Assert-True ($diagnosticsText -match 'internal\s+static\s+class\s+RangedModuleStageDiagnostics') 'Stage diagnostics hub must exist.'
Assert-True ($diagnosticsText -match 'LogStageStop') 'Stage diagnostics hub must expose LogStageStop.'
Assert-True ($diagnosticsText -match 'LogStageAddonError') 'Stage diagnostics hub must expose LogStageAddonError.'
Assert-True ($diagnosticsText -match 'LogStageContributionError') 'Stage diagnostics hub must expose LogStageContributionError.'

Assert-True ($dispatcherText -match 'LogStageAddonError') 'Addon dispatcher must report addon exceptions.'
Assert-True ($aimServiceText -match 'LogStageStop') 'Aim stage service must report stop diagnostics.'
Assert-True ($prepareServiceText -match 'LogStageStop') 'Prepare stage service must report stop diagnostics.'
Assert-True ($fireServiceText -match 'LogStageStop') 'Fire stage service must report stop diagnostics.'

Write-Output 'RangedModuleStageDiagnosticsSmokeTests PASS'
