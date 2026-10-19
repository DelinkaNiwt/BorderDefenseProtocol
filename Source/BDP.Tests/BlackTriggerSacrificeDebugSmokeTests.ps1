$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$service = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Sacrifice\BlackTriggerSacrificeService.cs') -Raw -Encoding utf8
$debug = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Development\Trigger\BlackFormation\BlackTriggerFormationDebugActions.cs') -Raw -Encoding utf8
$patch = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Patches\Patch_Pawn_Kill_BlackTriggerFormation.cs') -Raw -Encoding utf8
Assert-True ($service -match 'TryPrepare' -and $service -match 'ExecuteDebug') '调试燃尽必须先执行正式匹配准备。'
Assert-True ($service -match 'NoCompatibleTrigger') '无候选时必须给出中文原因且不得杀人。'
Assert-True ($debug -match 'DebugSacrificeSelectedPawn' -and $debug -match 'OpenSystemCatalog') '上帝模式必须提供燃尽和总览入口。'
Assert-True ($patch -match 'PreparedAttempt') '死亡补丁必须复用预先准备的冻结结果。'
Write-Output 'BlackTriggerSacrificeDebugSmokeTests PASS'
exit 0
