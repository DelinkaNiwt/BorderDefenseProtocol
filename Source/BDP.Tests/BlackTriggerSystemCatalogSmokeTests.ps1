$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$catalog = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Development\Trigger\BlackFormation\Window_BlackTriggerSystemCatalog.cs') -Raw -Encoding utf8
$formatter = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Development\Trigger\BlackFormation\BlackTriggerFormationDiagnosticFormatter.cs') -Raw -Encoding utf8
$world = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\World\GameComponent_BlackTriggerWorld.cs') -Raw -Encoding utf8
foreach ($tab in @('Definition','CurrentEvent','WorldState')) { Assert-True ($catalog -match $tab) "系统总览缺少页签：$tab" }
Assert-True ($catalog -match 'BeginScrollView' -and $catalog -match 'ButtonInvisible') '总览必须有滚动区和整行选择热区。'
Assert-True ($formatter -match 'MatchedRoutes' -and $formatter -match 'AssignedEmphasis') '诊断必须显示路线真值和侧重分配。'
Assert-True ($world -match 'PendingEvent' -and $world -match 'FinalizedThingDefNames') '总览必须能读取唯一待决事件和永久账本。'
Write-Output 'BlackTriggerSystemCatalogSmokeTests PASS'
exit 0
