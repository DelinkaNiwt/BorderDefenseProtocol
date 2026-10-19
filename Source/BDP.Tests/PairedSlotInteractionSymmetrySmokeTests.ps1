$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    Assert-True ($Text.Contains($Expected)) $Message
}

function Get-BracedBlock {
    param([string]$Text, [string]$Marker)
    $start = $Text.IndexOf($Marker, [System.StringComparison]::Ordinal)
    Assert-True ($start -ge 0) "找不到代码块：$Marker"
    $open = $Text.IndexOf('{', $start)
    Assert-True ($open -ge 0) "代码块缺少左括号：$Marker"
    $depth = 0
    for ($i = $open; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq '{') { $depth++ }
        elseif ($Text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Text.Substring($open, $i - $open + 1) }
        }
    }
    throw "代码块缺少右括号：$Marker"
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$interpreter = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP\Core\Trigger\Interaction\TriggerInteractionInterpreter.cs') -Raw -Encoding utf8
$loadout = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs') -Raw -Encoding utf8
$layout = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_SlotLayout.cs') -Raw -Encoding utf8
$window = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Window_TriggerAssembly.cs') -Raw -Encoding utf8
$transaction = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content\Assembly\Interaction\TriggerAssemblyTransaction.cs') -Raw -Encoding utf8
$compatibility = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content\Assembly\Window\TriggerAssemblyChipCompatibility.cs') -Raw -Encoding utf8

$mirrorInteraction = Get-BracedBlock $interpreter 'if (slot.IsBindingMirror && controlSlot != null && controlSlot != slot)'
Assert-Contains $mirrorInteraction 'GetBindingMirrorSlotInteraction(side, slotIndex, controlSlot)' `
    '镜像槽必须复用根槽的正式交互结果。'
$mirrorInteractionFactory = Get-BracedBlock $interpreter 'private ITriggerSlotInteractionState GetBindingMirrorSlotInteraction('
Assert-Contains $mirrorInteractionFactory 'GetSlotInteraction(controlSlot.Side, controlSlot.Index, controlSlot)' `
    '镜像槽交互必须按根槽状态计算。'
Assert-Contains $mirrorInteractionFactory 'controlInteraction.OperationKind' `
    '镜像槽必须返回与根槽相同的可执行动作。'
Assert-True ($mirrorInteractionFactory -notmatch 'TriggerInteractionOperationKind\.Mirror') `
    '镜像槽不能再被标记为仅供查看的 Mirror 动作。'

$slotCell = Get-BracedBlock $loadout 'private static bool DrawSlotCell('
Assert-Contains $slotCell 'ResolveBindingRoot(slot, reader)' `
    '芯片面板的镜像槽显示状态必须读取绑定根槽。'
$loadoutTooltip = Get-BracedBlock $loadout 'private static string BuildSlotTooltip('
Assert-Contains $loadoutTooltip 'statusSlot.HasBindingPartner' `
    '芯片面板应对绑定双方对称显示配对信息。'
Assert-True ($loadoutTooltip -notmatch 'slot\.IsBindingMirror\s*\?') `
    '芯片面板提示不能只向镜像槽显示绑定信息。'
Assert-Contains $slotCell 'DrawSwitchProgress(slotRect, statusSlot.Side, statusSlot, reader)' `
    '芯片面板的切换状态提示必须使用根槽进度。'

$drawSlot = Get-BracedBlock $layout 'private static void DrawSlot('
Assert-Contains $drawSlot 'IsSameBindingSelection(slot, selectedSide, selectedIndex, reader)' `
    '选择根槽或镜像槽时，装配台必须同时高亮整组成对槽。'
Assert-True ($drawSlot -notmatch '!slot\.IsBindingMirror') `
    '镜像槽也应显示与根槽相同的可放置状态。'
Assert-True ($drawSlot -match '(?s)HasAvailablePairedPartner\(\s*dragState\.Chip,\s*slot,\s*reader\s*\)') `
    '装配台目标判断必须以完整槽位处理成对替换。'

$mouseInput = Get-BracedBlock $window 'private void HandleMouseInput('
Assert-True ($mouseInput -notmatch '!slotHit\.Slot\.IsBindingMirror') `
    '装配台应允许从镜像槽开始拖动芯片。'

foreach ($method in @(
    'internal TriggerAssemblyOperationResult TryLoadFromStorage(',
    'internal TriggerAssemblyOperationResult TryUnloadToStorage(',
    'internal TriggerAssemblyOperationResult TryMoveOrSwapSlot(',
    'internal TriggerAssemblyOperationResult TryReplaceFromStorage('
)) {
    $body = Get-BracedBlock $transaction $method
    Assert-Contains $body 'ResolveBindingRoot(' "装配事务必须先归一绑定槽：$method"
    Assert-True ($body -notmatch 'IsBindingMirror\)') "装配事务不能直接拒绝镜像槽：$method"
}

$rootResolver = Get-BracedBlock $transaction 'private ITriggerSlotState ResolveBindingRoot('
Assert-Contains $rootResolver 'BindingRootSide' '装配事务必须沿绑定关系找到唯一根槽。'
Assert-Contains $rootResolver 'BindingRootIndex' '装配事务必须按根槽索引归一镜像操作。'
Assert-Contains $rootResolver 'ReferenceEquals(root.LoadedChip, slot.LoadedChip)' `
    '装配事务只能归一到持有同一枚芯片的有效根槽。'
$assemblyTooltip = Get-BracedBlock $layout 'private static string BuildSlotTooltip('
Assert-Contains $assemblyTooltip 'BDP_Window_TriggerAssembly_PairedSlotRelation' `
    '装配台两侧提示必须以同一条配对关系显示芯片。'
Assert-True ($assemblyTooltip -notmatch 'IsBindingMirror') `
    '装配台提示不应向玩家暴露根槽/镜像槽的内部标志。'
$partnerCheck = Get-BracedBlock $compatibility 'internal static bool HasAvailablePairedPartner('
Assert-Contains $partnerCheck 'targetSlot.LoadedChip' `
    '替换成对芯片时，应把目标芯片自身的镜像槽视为事务卸载后可用。'

Write-Output 'PairedSlotInteractionSymmetry PASS'
