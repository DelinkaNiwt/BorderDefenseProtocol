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

$requestPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionRequest.cs'
$preparedContextPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionPreparedContext.cs'

$requestText = Read-Source $requestPath
$preparedContextText = Read-Source $preparedContextPath

Assert-True (
    $requestText -match 'AttackContextSnapshot'
) 'AttackExecutionRequest must carry AttackContextSnapshot.'

Assert-True (
    $requestText -notmatch 'RangedAttackModuleSession\s+ModuleSession'
) 'AttackExecutionRequest must stop carrying ModuleSession.'

Assert-True (
    $requestText -notmatch 'ConfirmedInputSnapshot'
) 'AttackExecutionRequest must stop carrying ConfirmedInputSnapshot.'

Assert-True (
    $requestText -notmatch 'ConfirmedInteractionSnapshot'
) 'AttackExecutionRequest must stop carrying ConfirmedInteractionSnapshot.'

Assert-True (
    $preparedContextText -match 'AttackContextSnapshot'
) 'AttackExecutionPreparedContext must expose AttackContextSnapshot.'

Assert-True (
    $preparedContextText -notmatch 'RangedAttackModuleSession\s+ModuleSession'
) 'AttackExecutionPreparedContext must stop exposing ModuleSession.'

Assert-True (
    $preparedContextText -notmatch 'ConfirmedInputSnapshot'
) 'AttackExecutionPreparedContext must stop exposing ConfirmedInputSnapshot.'

Assert-True (
    $preparedContextText -notmatch 'ConfirmedInteractionSnapshot'
) 'AttackExecutionPreparedContext must stop exposing ConfirmedInteractionSnapshot.'

Write-Output 'AttackContextRequestCarrySmokeTests PASS'
