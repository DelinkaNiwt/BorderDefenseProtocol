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

$attackContextPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContext.cs'
$attackContextSnapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContextSnapshot.cs'
$attackContextNodePath = Join-Path $bdpSourceRoot 'AttackExecution\Context\IAttackContextNode.cs'

Assert-True (Test-Path -LiteralPath $attackContextPath) 'AttackContext.cs must exist.'
Assert-True (Test-Path -LiteralPath $attackContextSnapshotPath) 'AttackContextSnapshot.cs must exist.'
Assert-True (Test-Path -LiteralPath $attackContextNodePath) 'IAttackContextNode.cs must exist.'

$attackContextText = Read-Source $attackContextPath
$attackContextSnapshotText = Read-Source $attackContextSnapshotPath
$attackContextNodeText = Read-Source $attackContextNodePath

Assert-True (
    ($attackContextText -match 'class\s+AttackContext') -and
    ($attackContextText -match 'Get<') -and
    ($attackContextText -match 'TryGet<') -and
    ($attackContextText -match 'GetOrCreate<') -and
    ($attackContextText -match 'ToSnapshot')
) 'AttackContext must provide Get/TryGet/GetOrCreate/ToSnapshot as the minimum author surface.'

Assert-True (
    ($attackContextSnapshotText -match 'class\s+AttackContextSnapshot') -and
    ($attackContextSnapshotText -match 'Get<') -and
    ($attackContextSnapshotText -match 'TryGet<') -and
    ($attackContextSnapshotText -match 'ExposeData')
) 'AttackContextSnapshot must provide read-only access plus save/load support.'

Assert-True (
    ($attackContextNodeText -match 'interface\s+IAttackContextNode') -and
    ($attackContextNodeText -match 'IExposable') -and
    ($attackContextNodeText -match 'Clone')
) 'IAttackContextNode must inherit IExposable and require Clone().'

Assert-True (
    ($attackContextText -notmatch 'interface\s+IAttackContextKey') -and
    ($attackContextText -notmatch 'class\s+AttackContextKey') -and
    ($attackContextText -notmatch 'Scope')
) 'AttackContext must stay string-keyed and must not introduce complex key or scope systems.'

Write-Output 'AttackContextObjectModelSmokeTests PASS'
