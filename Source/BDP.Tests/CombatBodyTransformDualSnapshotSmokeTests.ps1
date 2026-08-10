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
$providerPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyTransformScanPresentationProvider.cs'
$motePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\Mote_CombatBodyScan.cs'
$oldCapturePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelCapture.cs'
$oldRecordPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelDrawRecord.cs'
$oldSuppressionPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelRenderSuppression.cs'
$oldPatchPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\Patch_PawnRenderNodeWorker_Apparel_CombatBodyScan.cs'

Assert-True -Condition (Test-Path -LiteralPath $providerPath) -Message '缺少扫描表现提供器。'
Assert-True -Condition (Test-Path -LiteralPath $motePath) -Message '缺少扫描 Mote。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldCapturePath)) -Message '旧衣物捕获器仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldRecordPath)) -Message '旧衣物绘制记录仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldSuppressionPath)) -Message '旧衣物绘制抑制状态仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldPatchPath)) -Message '旧衣物节点补丁仍存在。'

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$moteText = Get-Content -LiteralPath $motePath -Raw -Encoding utf8

$captureCount = ([regex]::Matches($providerText, 'CombatBodyPawnVisualCapture\.Capture\(pawn\)')).Count
Assert-True -Condition ($captureCount -ge 2) -Message '提供器必须分别捕获变换前后两张完整人物快照。'
Assert-True -Condition ($providerText -match 'CombatBodyPawnVisualCapture\.Release') -Message '提供器的失败路径必须归还完整人物快照。'
Assert-True -Condition ($providerText -match 'try[\s\S]*ThingMaker\.MakeThing[\s\S]*finally[\s\S]*CombatBodyPawnVisualCapture\.Release') -Message 'Mote 创建与生成异常必须处于快照归还保护内。'
Assert-True -Condition ($providerText -match 'mote\.Configure\(pawn, direction, pending\.OutgoingSnapshot, incomingSnapshot\)') -Message 'Mote 必须同时接收退场与入场完整人物快照。'
Assert-True -Condition ($providerText -match 'Dictionary<int, int> activeScanUntilTickByPawnId') -Message '扫描提供器必须按 Pawn 隔离动画占用窗口。'
Assert-True -Condition ($providerText -match 'IsScanWindowActive\(pawn\)') -Message 'Begin 必须拒绝同 Pawn 动画窗口内的重复表现事务。'
Assert-True -Condition ($providerText -match 'activeScanUntilTickByPawnId\[pawn\.thingIDNumber\][\s\S]*Mote_CombatBodyScan\.DurationTicks') -Message '只有成功生成 Mote 后才能登记同长度动画窗口。'
Assert-True -Condition ($moteText -match 'internal const int DurationTicks = 10;') -Message '提供器与 Mote 必须共享 10 tick 时长真值。'
Assert-True -Condition ($moteText -match 'private CombatBodyPawnVisualSnapshot outgoingSnapshot;') -Message 'Mote 必须保存退场完整人物快照。'
Assert-True -Condition ($moteText -match 'private CombatBodyPawnVisualSnapshot incomingSnapshot;') -Message 'Mote 必须保存入场完整人物快照。'
Assert-True -Condition ($moteText -match 'CombatBodyPawnRenderSuppression\.Begin') -Message 'Mote 生成后必须用完整快照替代原版人物绘制。'
Assert-True -Condition ($moteText -match 'CombatBodyPawnRenderSuppression\.End') -Message 'Mote 销毁时必须恢复原版人物绘制。'
Assert-True -Condition ($moteText -match 'DrawSnapshot\(outgoingSnapshot, outgoingKeepUpper') -Message '扫描线一侧必须绘制退场完整人物快照。'
Assert-True -Condition ($moteText -match 'DrawSnapshot\(incomingSnapshot, !outgoingKeepUpper') -Message '扫描线另一侧必须绘制入场完整人物快照。'
$releaseCount = ([regex]::Matches($moteText, 'CombatBodyPawnVisualCapture\.Release')).Count
Assert-True -Condition ($releaseCount -ge 2) -Message 'Mote 销毁时必须归还两张完整人物快照。'
Assert-True -Condition (($providerText + $moteText) -notmatch 'CombatBodyApparel|PawnRenderNodeWorker_Apparel') -Message '完整人物扫描链不得残留衣物级实现。'

Write-Output 'CombatBodyTransformDualSnapshot PASS'
