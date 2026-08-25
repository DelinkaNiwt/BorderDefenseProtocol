$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$entryPath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\DefaultAttackExecutionEntry.cs'
$gizmoResolverPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$attackSurfacePath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$entryText = Get-Content -LiteralPath $entryPath -Raw -Encoding UTF8
$gizmoResolverText = Get-Content -LiteralPath $gizmoResolverPath -Raw -Encoding UTF8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding UTF8

Assert-True (
    ($entryText -match 'CanAccept\(AttackExecutionRequest request\)') -and
    ($entryText -match '!request\.Pawn\.WorkTagIsDisabled\(WorkTags\.Violent\)')
) 'BDP 自定义攻击正式入口必须服从原版暴力工作类型禁用状态。'

Assert-True (
    $entryText -match 'CanAccept\(request\)[\s\S]*?AttackExecutionDiagnostics\.LogRejected'
) '禁攻检查必须发生在计划、扣费和动作开始之前。'

$violentGateIndex = $gizmoResolverText.IndexOf('pawn.WorkTagIsDisabled(WorkTags.Violent)')
$comboGateIndex = $gizmoResolverText.IndexOf('ComboUseRequirementService.Instance.Evaluate')
Assert-True ($violentGateIndex -ge 0) 'BDP 攻击按钮必须在进入目标选择前检查原版暴力工作类型。'
Assert-True (
    ($comboGateIndex -ge 0) -and ($violentGateIndex -lt $comboGateIndex)
) 'BDP 攻击按钮必须优先显示原版禁止暴力状态，再检查 Combo 使用条件。'
Assert-True (
    $gizmoResolverText -match 'IsIncapableOfViolence.*Translate'
) 'BDP 攻击按钮必须复用原版无法暴力提示文本。'

Assert-True (
    $attackSurfaceText -match 'TryGetAutoRangedVerb[\s\S]*?WorkTagIsDisabled\(WorkTags\.Violent\)'
) 'BDP 自动远程 Verb 读取入口必须服从原版暴力禁用，不能覆盖举盾搜索 Verb。'

Write-Output 'ViolenceDisabledAttackGateSmokeTests PASS'
