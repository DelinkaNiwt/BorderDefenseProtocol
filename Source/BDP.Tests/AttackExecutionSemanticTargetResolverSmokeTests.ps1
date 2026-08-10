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

$resolverPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionSemanticTargetResolver.cs'
$stagesPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionService.Stages.cs'
$protocolPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$resolverExists = Test-Path -LiteralPath $resolverPath
$resolverText = if ($resolverExists) { Read-Source $resolverPath } else { '' }
$stagesText = Read-Source $stagesPath
$protocolText = Read-Source $protocolPath

Assert-True $resolverExists 'AttackExecutionSemanticTargetResolver.cs must exist.'

Assert-True (
    ($resolverText -match 'internal\s+static\s+class\s+AttackExecutionSemanticTargetResolver') -and
    ($resolverText -match 'ConfirmedTargetSnapshot') -and
    ($resolverText -match 'SemanticTarget') -and
    ($resolverText -match 'return\s+confirmedTarget\.SemanticTarget')
) 'Resolver must read ConfirmedTargetSnapshot and prefer SemanticTarget.'

Assert-True (
    $stagesText -match 'AttackExecutionSemanticTargetResolver\.Resolve'
) 'AttackExecutionService.Stages must use the shared semantic target resolver.'

Assert-True (
    $protocolText -match 'AttackExecutionSemanticTargetResolver\.Resolve'
) 'RangedAttackProtocolService must use the shared semantic target resolver.'

Assert-True (
    $stagesText -notmatch 'private\s+static\s+LocalTargetInfo\s+ResolveSemanticTarget'
) 'AttackExecutionService.Stages must not keep a local semantic target resolver.'

Assert-True (
    $protocolText -notmatch 'private\s+static\s+LocalTargetInfo\s+ResolveSemanticTarget'
) 'RangedAttackProtocolService must not keep a local semantic target resolver.'

Write-Output 'AttackExecutionSemanticTargetResolverSmokeTests PASS'
