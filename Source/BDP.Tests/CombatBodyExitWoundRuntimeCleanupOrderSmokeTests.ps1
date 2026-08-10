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

# 战斗体退出事务必须在肉身恢复与相位退出完成后，最终清理伤口派生运行时。
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
Assert-True (Test-Path -LiteralPath $exitTransactionPath) 'CombatBodyExitTransaction.cs must exist.'

$exitText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8
$normalized = $exitText -replace '\s+', ''
$cooldownCall = 'rawCombatBodyService.EnterCooldown(ResolveCooldownTicks(exitMode),ResolveExitReason(exitMode));'
$woundClearCall = 'owner.WoundRuntime.ClearActiveRuntime(ownerPawn);'
$cooldownIndex = $normalized.IndexOf($cooldownCall)
$woundClearIndex = $normalized.IndexOf($woundClearCall)
$woundClearCount = ([regex]::Matches($normalized, [regex]::Escape($woundClearCall))).Count

Assert-True ($cooldownIndex -ge 0) 'Combat body exit must enter cooldown through the raw service.'
Assert-True ($woundClearIndex -gt $cooldownIndex) 'Wound runtime must be cleared after body restore and phase exit.'
Assert-True ($woundClearCount -eq 1) 'Wound runtime must have exactly one final cleanup in the exit transaction.'

Write-Output 'CombatBodyExitWoundRuntimeCleanupOrderSmokeTests PASS'
