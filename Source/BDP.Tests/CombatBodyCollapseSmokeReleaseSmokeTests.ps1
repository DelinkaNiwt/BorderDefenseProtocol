$ErrorActionPreference = 'Stop'

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
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'

Assert-True (
    Test-Path -LiteralPath $exitTransactionPath
) 'CombatBodyExitTransaction.cs 必须存在。'

$exitTransactionText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8

Assert-True (
    $exitTransactionText -match 'if \(exitMode == CombatBodySessionExitMode\.Collapse\)[\s\S]*RemoveCollapsePendingHediff\(ownerPawn\);[\s\S]*ReleaseCollapseSmoke\(ownerPawn\);[\s\S]*CombatBodyCollapseExtensionRegistry\.Execute\(ownerPawn\);'
) '被动崩解必须在移除倒计时状态后、崩解扩展执行前释放烟雾。'

Assert-True (
    $exitTransactionText -match 'private static void ReleaseCollapseSmoke\(Pawn ownerPawn\)[\s\S]*ownerPawn == null \|\| !ownerPawn\.Spawned \|\| ownerPawn\.Map == null[\s\S]*GenExplosion\.DoExplosion\(\s*ownerPawn\.Position,\s*ownerPawn\.Map,\s*2\.0f,\s*DamageDefOf\.Smoke,\s*null,\s*-1,\s*-1f,\s*null,\s*null,\s*null,\s*null,\s*null,\s*0f,\s*1,\s*GasType\.BlindSmoke,\s*postExplosionGasAmount:\s*8\);'
) '崩解烟雾必须安全复用原版 BlindSmoke，并仅为本次调用设置半径 2.0 格、初始浓度 8。'

Assert-True (
    $exitTransactionText -notmatch '1\.4f'
) '崩解烟雾不得继续保留旧半径 1.4 格。'

$releaseBranchMatch = [regex]::Match(
    $exitTransactionText,
    'if \(exitMode == CombatBodySessionExitMode\.Release\)(?<Body>[\s\S]*?)trionBinding\.ClearActiveRuntime\(\);')
Assert-True (
    $releaseBranchMatch.Success -and
    $releaseBranchMatch.Groups['Body'].Value -notmatch 'ReleaseCollapseSmoke'
) '主动解除分支不得释放崩解烟雾。'

Write-Output 'CombatBodyCollapseSmokeReleaseSmokeTests PASS'
