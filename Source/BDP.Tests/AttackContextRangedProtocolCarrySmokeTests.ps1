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

$entryPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\RangedAttackEntry.cs'
$aimContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Aim\AimStageContext.cs'
$prepareContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\PrepareStageContext.cs'
$fireContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Fire\FireStageContext.cs'
$projectileInitContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageContext.cs'
$projectileInitServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$protocolServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$entryText = Read-Source $entryPath
$aimContextText = Read-Source $aimContextPath
$prepareContextText = Read-Source $prepareContextPath
$fireContextText = Read-Source $fireContextPath
$projectileInitContextText = Read-Source $projectileInitContextPath
$projectileInitServiceText = Read-Source $projectileInitServicePath
$protocolServiceText = Read-Source $protocolServicePath

Assert-True (
    ($entryText -match 'AttackContext\s+AttackContext') -and
    ($entryText -notmatch 'ConfirmedInput') -and
    ($entryText -notmatch 'ConfirmedInteraction') -and
    ($entryText -notmatch 'RangedModuleContextSnapshot')
) 'RangedAttackEntry must carry only AttackContext as the front-half context trunk.'

Assert-True (
    ($aimContextText -match 'public\s+AttackContext\s+AttackContext') -and
    ($prepareContextText -match 'public\s+AttackContext\s+AttackContext') -and
    ($fireContextText -match 'public\s+AttackContext\s+AttackContext') -and
    ($projectileInitContextText -match 'public\s+AttackContext\s+AttackContext')
) 'Aim, Prepare, Fire, and ProjectileInit stage contexts must all expose AttackContext.'

Assert-True (
    ($projectileInitServiceText -match 'AttackContextSnapshot\s*=\s*entry\s*!=\s*null\s*&&\s*entry\.AttackContext\s*!=\s*null\s*\?\s*entry\.AttackContext\.ToSnapshot\(\)') -and
    ($projectileInitServiceText -notmatch 'entry\s*!=\s*null\s*\?\s*entry\.ConfirmedInput') -and
    ($projectileInitServiceText -notmatch 'entry\s*!=\s*null\s*\?\s*entry\.ConfirmedInteraction')
) 'ProjectileInitStageService must freeze AttackContext directly and must not read old confirmed snapshot fields.'

Assert-True (
    ($protocolServiceText -match 'AttackContext\.FromSnapshot\(request\.AttackContextSnapshot\)') -and
    ($protocolServiceText -notmatch 'request\.ModuleSession')
) 'RangedAttackProtocolService must rebuild front-half runtime from AttackContextSnapshot instead of carrying a request-level module session.'

Write-Output 'AttackContextRangedProtocolCarrySmokeTests PASS'
