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
$continuationPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbContinuationPlanner.cs'
$continuationText = Read-Source $continuationPath

# 契约:续射请求快照必须优先使用宿主保留的完整复合快照。
# 逐射 dual 的 HostModuleSession 只有主手单侧状态,直接导出会话会丢副手侧路线引导状态,
# 导致副手泳道重建时绕行路径丢失、整轮 burst 只出第一发。
Assert-True (
    $continuationText -match 'AttackContextSnapshot attackContextSnapshot = verb\.HostAttackContextSnapshot != null[\s\S]*?CreateAttackContextSnapshot\(moduleSession\);'
) 'Ranged continuation must prefer the host-complete AttackContextSnapshot over exporting the resident module session.'

Assert-True (
    ($continuationText -match '逐射 dual 的宿主会话是单侧\(主手\)泳道') -or
    ($continuationText -match '丢副手侧路线引导状态')
) 'The host-snapshot priority must be documented with the dual sequential single-lane reason in code.'

# 契约:现有会话解析优先级(正式会话 > 快照重建 > staged)必须保持不变。
Assert-True (
    $continuationText -match 'if \(verb\.HostModuleSession != null\)' -and
    $continuationText -match 'TryCreateSnapshotBackedModuleSession' -and
    $continuationText -match 'source = "staged_entry"'
) 'Existing module-session resolution priority must be preserved.'

Write-Output 'RangedContinuationCompleteSnapshotPrioritySmokeTests PASS'
