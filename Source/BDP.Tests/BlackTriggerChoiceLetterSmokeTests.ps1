$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$base = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$letter = Get-Content -LiteralPath (Join-Path $base 'Presentation\Letter_BlackTriggerFormation.cs') -Raw -Encoding utf8
$window = Get-Content -LiteralPath (Join-Path $base 'Presentation\Window_BlackTriggerFormationDecision.cs') -Raw -Encoding utf8
$world = Get-Content -LiteralPath (Join-Path $base 'World\BlackTriggerFormationWorldService.cs') -Raw -Encoding utf8
$final = Get-Content -LiteralPath (Join-Path $base 'Transactions\BlackTriggerFormationFinalizationService.cs') -Raw -Encoding utf8
Assert-True ($letter -match 'LetterWithTimeout' -and $letter -match 'CanDismissWithRightClick') '信件必须复用原版限时信件且不能右键丢弃。'
Assert-True ($letter -match 'ShouldAutomaticallyOpenLetter') '初始信件不得自动打开窗口。'
Assert-True ($window -match 'SelectedEmphasisDefName') '窗口只能选择匹配后的战斗侧重。'
Assert-True ($window -match 'SelectedTraitOptionKey') '窗口必须允许明确选择继承特性或不继承。'
foreach ($key in @('Formation_Abandon','Formation_Later','Formation_Confirm','Formation_Reject')) { Assert-True ($window -match $key) "窗口缺少操作：$key" }
Assert-True ($world -match 'ForceDecision') '到期必须进入强制决策，而不是自动结束。'
Assert-True ($final -match 'SendSuccessLetter') '成功形成必须发送蓝色结果信件。'
Write-Output 'BlackTriggerChoiceLetterSmokeTests PASS'
exit 0
