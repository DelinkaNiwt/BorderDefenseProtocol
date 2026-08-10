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

function Get-Section {
    param(
        [string]$Text,
        [string]$StartMarker,
        [string]$EndMarker
    )

    $start = $Text.IndexOf($StartMarker)
    $end = $Text.IndexOf($EndMarker, $start + $StartMarker.Length)
    Assert-True ($start -ge 0 -and $end -gt $start) "找不到代码区段：$StartMarker"
    return $Text.Substring($start, $end - $start)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$frontStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontState.cs'
$snapshotStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotState.cs'
$messagesPath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$frontStateText = Get-Content -LiteralPath $frontStatePath -Raw -Encoding utf8
$snapshotStateText = Get-Content -LiteralPath $snapshotStatePath -Raw -Encoding utf8
$messagesText = Get-Content -LiteralPath $messagesPath -Raw -Encoding utf8

Assert-True ($bridgeText -match 'private bool HasValidRollbackSnapshot\(\)') '宿主桥必须统一判断有效回滚快照。'
$validitySection = Get-Section $bridgeText 'private bool HasValidRollbackSnapshot()' 'private void RestoreInvalidLegacyCombatBody()'
Assert-True ($validitySection -match 'hostState\.TransformationApplied') '有效回滚凭据必须要求宿主变换已应用。'
Assert-True ($validitySection -match 'hostState\.HasSnapshot') '有效回滚凭据必须要求宿主拥有快照。'
Assert-True ($validitySection -match 'hostState\.SnapshotState\?\.IsCaptured == true') '有效回滚凭据必须要求快照已完成捕获。'

$restoreSection = Get-Section $bridgeText 'public void RestoreFromCombatBody()' 'private bool HasValidRollbackSnapshot()'
Assert-True ($restoreSection -match 'if \(!HasValidRollbackSnapshot\(\)\)[\s\S]*RestoreInvalidLegacyCombatBody\(\);[\s\S]*return;') '无有效快照时必须提前进入旧档安全解除。'
Assert-True ($restoreSection -match 'ExtinguishFire\(\)[\s\S]*RemoveCombatBodyEntryHediffs\(\)[\s\S]*RestoreFrontReplacement\(hostState\.FrontState\)[\s\S]*snapshotService\?\.Restore\(Pawn, hostState\)[\s\S]*FinalCleanupResidualHediffs') '有效快照必须保留原有完整恢复顺序。'

$legacySection = Get-Section $bridgeText 'private void RestoreInvalidLegacyCombatBody()' 'private void ClearInvalidHostTransactionRecords()'
Assert-True ($legacySection -match 'RemoveCombatBodyActiveHediff\(\)') '旧档安全解除必须移除战斗体激活标记。'
Assert-True ($legacySection -match 'PreserveInvalidTransactionItems\(\)[\s\S]*ClearInvalidHostTransactionRecords\(\)') '旧档安全解除必须先保全残留容器物品，再清理事务记录。'
Assert-True ($legacySection -match 'ClearInvalidHostTransactionRecords\(\)') '旧档安全解除必须收敛残缺事务记录。'
Assert-True ($legacySection -notmatch 'ExtinguishFire|RemoveCombatBodyEntryHediffs|RestoreFrontReplacement|snapshotService|FinalCleanupResidualHediffs') '旧档安全解除不得触碰无快照依据的角色实物或健康状态。'

$preserveSection = Get-Section $bridgeText 'private void PreserveInvalidTransactionItems()' 'private void ClearInvalidHostTransactionRecords()'
Assert-True ($preserveSection -match 'OriginalApparelContainer') '旧档安全解除必须保全残留的原衣物。'
Assert-True ($preserveSection -match 'OriginalInventoryContainer') '旧档安全解除必须保全残留的原背包物品。'
Assert-True ($preserveSection -match 'CombatApparelContainer') '旧档安全解除必须保全残留的前台服装。'
Assert-True ($preserveSection -match 'thing\.holdingOwner\?\.TryTransferToContainer\(thing, hostState\.SnapshotState\.RecoveredItemContainer\);') '残留物品必须先进入不参与会话重置的回收容器。'
Assert-True ($preserveSection -match 'TryRestoreRecoveredItemsToInventory\(\);') '安全解除必须尝试把已隔离物品归还当前背包。'
Assert-True ($bridgeText -match 'ApplyCombatBodyTransformation\(\)[\s\S]*TryRestoreRecoveredItemsToInventory\(\);[\s\S]*snapshotService\?\.Capture') '下一次激活前必须再次尝试归还回收容器物品。'

$frontSection = Get-Section $bridgeText 'private void RestoreFrontReplacement(CombatBodyFrontState frontState)' 'private void ApplyPresetFrontReplacement('
Assert-True ($frontSection -match 'HashSet<int> activeApparelThingIds = new HashSet<int>\(frontState\.ActiveApparelThingIds\);') '前台恢复必须建立本轮服装编号集合。'
Assert-True ($frontSection -match 'Where\(apparel => activeApparelThingIds\.Contains\(apparel\.thingIDNumber\)\)') '前台恢复只能筛选本轮记录的服装。'
Assert-True ($frontSection -notmatch 'List<Apparel> currentApparel = Pawn\.apparel\.WornApparel\.ToList\(\);') '前台恢复不得把全部当前服装视为战斗体资产。'
Assert-True ($frontStateText -match 'Bind\(IThingHolder holder\)[\s\S]*if \(activeApparelThingIds == null\)[\s\S]*activeApparelThingIds = new List<int>\(\);') '前台状态绑定时必须修复旧档缺失的服装编号列表。'
Assert-True ($snapshotStateText -match 'Bind\(IThingHolder holder\)[\s\S]*EnsureRecordedStates\(\);') '快照状态绑定时必须修复旧档缺失的记录集合。'
Assert-True ($snapshotStateText -match 'private ThingOwner<Thing> recoveredItemContainer;') '快照状态必须持有独立的旧档回收容器。'
Assert-True ($snapshotStateText -match 'public ThingOwner<Thing> RecoveredItemContainer => recoveredItemContainer;') '宿主桥必须能把残留实物转入回收容器。'
Assert-True ($snapshotStateText -match 'Scribe_Deep\.Look\(ref recoveredItemContainer, "recoveredItemContainer", this\);') '旧档回收容器必须参与存档。'
$sessionResetSection = Get-Section $snapshotStateText 'public void ClearSessionContainers()' 'public void ExposeData()'
Assert-True ($sessionResetSection -notmatch 'recoveredItemContainer') '新一轮快照不得清空旧档回收容器。'
$snapshotRepairSection = Get-Section $snapshotStateText 'private void EnsureRecordedStates()' 'public ThingOwner GetDirectlyHeldThings()'
Assert-True ($snapshotRepairSection -match 'if \(apparelLockedStates == null\)[\s\S]*apparelLockedStates = new Dictionary<int, bool>\(\);') '快照状态必须修复衣物锁定记录。'
Assert-True ($snapshotRepairSection -match 'if \(apparelForcedStates == null\)[\s\S]*apparelForcedStates = new Dictionary<int, bool>\(\);') '快照状态必须修复强制服装记录。'
Assert-True ($snapshotRepairSection -match 'if \(itemNotForSaleStates == null\)[\s\S]*itemNotForSaleStates = new Dictionary<int, bool>\(\);') '快照状态必须修复不可出售记录。'
Assert-True ($snapshotRepairSection -match 'if \(itemUnpackedCaravanStates == null\)[\s\S]*itemUnpackedCaravanStates = new Dictionary<int, bool>\(\);') '快照状态必须修复商队拆包记录。'
Assert-True ($snapshotRepairSection -match 'if \(needSnapshots == null\)[\s\S]*needSnapshots = new List<CombatBodySnapshotNeedRecord>\(\);') '快照状态必须修复需求记录。'
Assert-True ($snapshotRepairSection -match 'if \(hediffSnapshots == null\)[\s\S]*hediffSnapshots = new List<CombatBodySnapshotHediffRecord>\(\);') '快照状态必须修复健康记录。'

Assert-True ($bridgeText -match 'internal void ReconcileAfterLoad\(CombatBodyPhase phase\)') '宿主桥必须提供读档事务协调入口。'
Assert-True ($hostText -match 'PostLoadInit[\s\S]*EnsureInternalState\(\);[\s\S]*host\?\.ReconcileAfterLoad\(state\.Phase\);[\s\S]*combatBodySessionService\?\.RestoreAfterLoad\(\);') '读档后必须先协调宿主事务，再恢复会话运行时。'
Assert-True ($bridgeText -match 'BDP_Message_CombatBody_LegacyHostRecoveryWarning.*Translate') '残缺旧档必须输出可定位的一次性警告。'
Assert-True ($messagesText -match '<BDP_Message_CombatBody_LegacyHostRecoveryWarning>[^<]+\{0\}[^<]+\{1\}[^<]+\{2\}[^<]+\{3\}[^<]+</BDP_Message_CombatBody_LegacyHostRecoveryWarning>') '旧档协调警告必须提取到中文语言包并包含诊断参数。'

Write-Output 'CombatBodyLegacyHostRecovery PASS'
