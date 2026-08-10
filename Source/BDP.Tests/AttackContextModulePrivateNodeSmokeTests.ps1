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

$privateContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\IRangedModulePrivateContext.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$slotPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSlot.cs'
$snapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedModuleContextSnapshot.cs'
$attackContextKeysPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContextKeys.cs'

$privateContextText = Read-Source $privateContextPath
$sessionText = Read-Source $sessionPath
$slotText = Read-Source $slotPath
$attackContextKeysText = Read-Source $attackContextKeysPath

Assert-True (
    $privateContextText -match 'IRangedModulePrivateContext\s*:\s*IAttackContextNode'
) 'IRangedModulePrivateContext must become an AttackContext node protocol.'

Assert-True (
    ($sessionText -match 'AttackContext\s+AttackContext') -and
    ($sessionText -match 'GetModulePrivateKey') -and
    ($sessionText -notmatch 'RangedModuleContextSnapshot') -and
    ($sessionText -notmatch 'slot\.PrivateContext')
) 'RangedAttackModuleSession must route private-context access through AttackContext instead of a parallel snapshot or slot field.'

Assert-True (
    ($slotText -match 'class\s+RangedAttackModuleSlot') -and
    ($slotText -match 'MountIndex') -and
    ($slotText -match 'IRangedAttackModuleRuntime') -and
    ($slotText -notmatch 'PrivateContext')
) 'RangedAttackModuleSlot must only keep mount index and runtime.'

Assert-True (
    -not (Test-Path -LiteralPath $snapshotPath)
) 'RangedModuleContextSnapshot.cs must be deleted after private contexts move into AttackContext nodes.'

Assert-True (
    $attackContextKeysText -match 'ModulePrivatePrefix'
) 'AttackContextKeys must define the neutral module-private-context key prefix.'

Write-Output 'AttackContextModulePrivateNodeSmokeTests PASS'
