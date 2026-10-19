$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$base = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$component = Get-Content -LiteralPath (Join-Path $base 'World\GameComponent_BlackTriggerWorld.cs') -Raw -Encoding utf8
$service = Get-Content -LiteralPath (Join-Path $base 'World\BlackTriggerFormationWorldService.cs') -Raw -Encoding utf8
Assert-True ($component -match 'GameComponent' -and $component -match 'PendingEvent') '世界状态必须保存唯一待决事件。'
Assert-True ($component -match 'FinalizedThingDefNames') '世界状态必须保存永久世界唯一账本。'
Assert-True ($component -notmatch '\bReservations\b|\bEvents\b') '不得保留多事件和预留列表。'
Assert-True ($service -match 'IsFinalized' -and $service -match 'PendingEvent') '形成服务必须过滤永久唯一并管理唯一事件。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $base 'World\BlackTriggerReservation.cs'))) '旧预留模型必须删除。'
Write-Output 'BlackTriggerUniquenessSmokeTests PASS'
exit 0
