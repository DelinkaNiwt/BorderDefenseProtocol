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
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$escapeServicePath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeService.cs'

$exitText = Read-Source $exitTransactionPath
$escapeText = Read-Source $escapeServicePath

# 契约:取消征召必须是崩解主链(Collapse 分支)的固定步骤,在紧急脱离执行之后等效执行。
Assert-True (
    $exitText -match 'CombatBodyCollapseExtensionRegistry\.Execute\(ownerPawn\);\s*[\s\S]*?TryReleaseDraft\(ownerPawn\);'
) 'Draft release must remain a fixed step of the collapse main chain, executed after the neutral collapse extension.'

# 契约:取消征召不得再挂在紧急脱离分支内部。
Assert-True (
    $escapeText -notmatch 'drafter\.Drafted'
) 'Emergency escape service must no longer own draft release; it belongs to the collapse main chain.'

# 契约:崩解主链必须提供征召释放助手,并且手动退出(Release)分支不执行征召释放。
Assert-True (
    ($exitText -match 'private static void TryReleaseDraft\(Pawn ownerPawn\)') -and
    ($exitText -match 'ownerPawn\?\.drafter != null') -and
    ($exitText -match 'ownerPawn\.drafter\.Drafted = false;') -and
    ($exitText -match 'if \(exitMode == CombatBodySessionExitMode\.Release\)\s*\{[\s\S]*?DeactivateAllSlots\(triggerLoadoutCommands\);\s*\}') -and
    ($exitText -notmatch 'if \(exitMode == CombatBodySessionExitMode\.Release\)\s*\{[^}]*TryReleaseDraft')
) 'TryReleaseDraft must live in the Collapse branch only; Release manual exit must not release draft.'

Write-Output 'CombatBodyCollapseDraftReleaseMainChainSmokeTests PASS'
