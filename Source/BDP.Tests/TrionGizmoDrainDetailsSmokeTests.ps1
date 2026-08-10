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
$repoRoot = Split-Path -Parent $sourceRoot

$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'

Assert-True (Test-Path -LiteralPath $gizmoPath) 'Gizmo_TrionStatus.cs must exist.'

$gizmoText = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8
$tooltipMatch = [regex]::Match($gizmoText, 'private\s+string\s+BuildTooltip\s*\(\s*\)\s*\{(?<body>.*?)^\s*\}', 'Singleline, Multiline')
Assert-True $tooltipMatch.Success 'Gizmo_TrionStatus must keep a private BuildTooltip method.'

$tooltipBody = $tooltipMatch.Groups['body'].Value

Assert-True ($tooltipBody -notmatch '可用:') 'Trion gizmo tooltip must not repeat visible available amount.'
Assert-True ($tooltipBody -notmatch '当前:') 'Trion gizmo tooltip must not repeat visible current amount.'
Assert-True ($tooltipBody -notmatch '总量:') 'Trion gizmo tooltip must not repeat max amount.'
Assert-True ($tooltipBody -notmatch '锁定:') 'Trion gizmo tooltip must not repeat allocated amount.'
Assert-True ($tooltipBody -notmatch '预测锁定:') 'Trion gizmo tooltip must not repeat reserved amount.'
Assert-True ($tooltipBody -notmatch '恢复:') 'Trion gizmo tooltip must not repeat recovery overview.'
Assert-True ($tooltipBody -notmatch '恢复冻结:') 'Trion gizmo tooltip must not repeat frozen overview.'
Assert-True ($gizmoText -match 'GetDrainSnapshot\(\)') 'Trion gizmo tooltip must read drain details through ITrionReader.GetDrainSnapshot().'
Assert-True ($gizmoText -match '当前没有持续流失。') 'Trion gizmo tooltip must provide an empty-state line for no active drains.'
Assert-True ($gizmoText -match '来源列表') 'Trion gizmo tooltip must explicitly label the drain source list.'
Assert-True ($gizmoText -match 'BuildDrainSourceLine') 'Trion gizmo tooltip must list individual drain source lines.'
Assert-True ($gizmoText -match 'ResolveDrainSourceLabel') 'Trion gizmo tooltip must translate drain keys to Chinese source descriptions.'
Assert-True ($gizmoText -notmatch 'totalsByGroup') 'Trion gizmo tooltip must not collapse details into grouped totals while debugging.'
Assert-True ($gizmoText -notmatch 'BuildDrainGroupLabel') 'Trion gizmo tooltip must not display raw Domain/Channel group labels.'
Assert-True ($gizmoText -match '战斗体伤口流失') 'Trion gizmo tooltip must name combat body wound drains in Chinese.'
Assert-True ($gizmoText -match '战斗体维持消耗') 'Trion gizmo tooltip must name combat body maintenance drain in Chinese.'
Assert-True ($gizmoText -match '状态表达持续消耗') 'Trion gizmo tooltip must name Hediff expression drains in Chinese.'
Assert-True ($gizmoText -match '能力表达持续消耗') 'Trion gizmo tooltip must name Ability expression drains in Chinese.'
Assert-True ($gizmoText -match '被动表达持续消耗') 'Trion gizmo tooltip must name Passive expression drains in Chinese.'
Assert-True ($gizmoText -match '攻击表达持续消耗') 'Trion gizmo tooltip must name Verb expression drains in Chinese.'
Assert-True ($gizmoText -match '未知来源持续消耗') 'Trion gizmo tooltip must provide a Chinese fallback for unknown drain keys.'
Assert-True ($gizmoText -match '调试ID') 'Trion gizmo tooltip must include debug ids while source-list debugging is needed.'
Assert-True ($gizmoText -match 'Trion流失详情') 'Trion gizmo tooltip must be labeled as drain details.'
Assert-True ($gizmoText -match 'BuildTooltipId') 'Trion gizmo tooltip ids must be built through a dedicated stable helper.'
Assert-True ($gizmoText -match 'thingIDNumber') 'Trion gizmo tooltip ids must use the owning Thing identity so repeated new gizmos keep the same tooltip id.'
Assert-True ($gizmoText -notmatch 'Gen\.HashCombineInt\s*\(\s*GetHashCode\(\)') 'Trion gizmo tooltip ids must not use the short-lived Gizmo instance hash code.'

Write-Output 'TrionGizmoDrainDetailsSmokeTests PASS'
