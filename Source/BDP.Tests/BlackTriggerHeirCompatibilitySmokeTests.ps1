$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$base = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$compatibility = Get-Content -LiteralPath (Join-Path $base 'Binding\BlackTriggerCompatibilityEvaluator.cs') -Raw -Encoding utf8
$snapshot = Get-Content -LiteralPath (Join-Path $base 'Snapshot\BlackTriggerMakerSnapshotFactory.cs') -Raw -Encoding utf8
$window = Get-Content -LiteralPath (Join-Path $base 'Presentation\Window_BlackTriggerFormationDecision.cs') -Raw -Encoding utf8
Assert-True ($snapshot -match 'Parent' -and $snapshot -match 'Child' -and $snapshot -match 'Spouse') '传承人必须覆盖父母、子女和配偶。'
Assert-True ($snapshot -match 'opinion >= 0f' -and $snapshot -match 'opinion >= 100f') '家庭与满好感候选必须使用不同关系门槛。'
Assert-True ($snapshot -notmatch 'other\.IsColonistPlayerControlled') '传承人不得受玩家派系限制。'
Assert-True ($window -match 'HeirLoadId') '玩家必须能选择或不指定传承人。'
Assert-True ($compatibility -match 'HeirLoadId') '传承人必须绕过内容适格条件。'
Assert-True ($compatibility -match 'HasOtherBoundBlackTrigger') '传承人旁路不得绕过一人一件。'
Write-Output 'BlackTriggerHeirCompatibilitySmokeTests PASS'
exit 0
