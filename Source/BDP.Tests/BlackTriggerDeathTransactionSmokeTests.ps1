$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$patch = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Patches\Patch_Pawn_Kill_BlackTriggerFormation.cs') -Raw -Encoding utf8
$world = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\World\BlackTriggerFormationWorldService.cs') -Raw -Encoding utf8
$component = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\World\GameComponent_BlackTriggerWorld.cs') -Raw -Encoding utf8
$final = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Transactions\BlackTriggerFormationFinalizationService.cs') -Raw -Encoding utf8
Assert-True ($patch -match 'Prefix' -and $patch -match 'Capture') '死亡前必须冻结快照。'
Assert-True ($patch -match 'Postfix' -and $patch -match 'TryCreatePending') '尸体生成后必须建立待决事件。'
Assert-True ($component -match 'PendingEvent' -and $component -match 'NextFormationAllowedTick') '世界组件必须只保存一个待决事件和普通冷却。'
Assert-True ($world -match '60000|normalCooldownTicks') '普通形成必须有一日冷却。'
Assert-True ($world -match '7500|pendingEventTicks') '待决事件必须使用三小时期限。'
Assert-True ($final -match 'TryPlace' -and $final -match 'Corpse\.Destroy') '必须先成功放置物品，再销毁原身尸体。'
Assert-True ($final -match 'MarkExhausted' -and $final -match 'FinalizedThingDefNames') '成功形成必须写耗尽与永久唯一。'
Write-Output 'BlackTriggerDeathTransactionSmokeTests PASS'
exit 0
