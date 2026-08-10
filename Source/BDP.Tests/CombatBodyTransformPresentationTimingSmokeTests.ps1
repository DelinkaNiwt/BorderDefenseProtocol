$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-Section {
    param(
        [string]$Text,
        [string]$StartMarker,
        [string]$EndMarker
    )

    $start = $Text.IndexOf($StartMarker, [System.StringComparison]::Ordinal)
    $end = $Text.IndexOf($EndMarker, $start + $StartMarker.Length, [System.StringComparison]::Ordinal)
    Assert-True ($start -ge 0 -and $end -gt $start) "无法提取代码段：$StartMarker"
    return $Text.Substring($start, $end - $start)
}

function Assert-Before {
    param(
        [string]$Text,
        [string]$First,
        [string]$Second,
        [string]$Message
    )

    $firstIndex = $Text.IndexOf($First, [System.StringComparison]::Ordinal)
    $secondIndex = $Text.IndexOf($Second, [System.StringComparison]::Ordinal)
    Assert-True ($firstIndex -ge 0 -and $secondIndex -ge 0 -and $firstIndex -lt $secondIndex) $Message
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$interfacePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Presentation\ICombatBodyTransformPresentationProvider.cs'
$activationPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$exitPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$statePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\State\CombatBodyState.cs'

$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$interfaceText = Get-Content -LiteralPath $interfacePath -Raw -Encoding utf8
$activationText = Get-Content -LiteralPath $activationPath -Raw -Encoding utf8
$exitText = Get-Content -LiteralPath $exitPath -Raw -Encoding utf8
$stateText = Get-Content -LiteralPath $statePath -Raw -Encoding utf8

$enterSection = Get-Section $bridgeText 'public void ApplyCombatBodyTransformation()' 'public void RestoreFromCombatBody()'
$exitSection = Get-Section $bridgeText 'public void RestoreFromCombatBody()' 'private void ApplyFrontReplacement('

Assert-Before $enterSection 'CombatBodyTransformPresentationRegistry.NotifyBegin' 'snapshotService?.Capture' '进入表现通知必须发生在抓快照和脱原衣之前。'
Assert-Before $enterSection 'hostState.TransformationApplied = true' 'CombatBodyTransformPresentationRegistry.NotifyEnd' '进入完成通知必须晚于真实换装。'
Assert-Before $exitSection 'CombatBodyTransformPresentationRegistry.NotifyBegin' 'RemoveCombatBodyActiveHediff' '离开表现通知必须发生在移除状态和恢复衣物之前。'
Assert-Before $exitSection 'RestoreInvalidLegacyCombatBody();' 'CombatBodyTransformPresentationRegistry.NotifyEnd' '旧档安全解除完成通知必须晚于事务收敛。'
$validExitSection = Get-Section $exitSection 'ExtinguishFire();' 'private bool HasValidRollbackSnapshot()'
Assert-Before $validExitSection 'hostState.TransformationApplied = false' 'CombatBodyTransformPresentationRegistry.NotifyEnd' '完整解除完成通知必须晚于真实恢复。'
Assert-True ($enterSection -match 'CombatBodyTransformDirection\.Enter') '进入路径必须广播Enter方向。'
Assert-True ($exitSection -match 'CombatBodyTransformDirection\.Exit') '离开路径必须广播Exit方向。'
Assert-True ($interfaceText -match 'void End\(Pawn pawn, CombatBodyTransformDirection direction\);') '表现接口必须提供换装后通知。'
Assert-True ($activationText -notmatch 'CombatBodyTransformPresentationRegistry') '激活事务不得持有表现广播职责。'
Assert-True ($exitText -notmatch 'CombatBodyTransformPresentationRegistry') '退出事务不得持有表现广播职责。'
Assert-True ($stateText -notmatch 'CombatBodyTransformPresentationRegistry') '相位真值不得持有表现广播职责。'

Write-Output 'CombatBodyTransformPresentationTiming PASS'
