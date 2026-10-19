$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-RequiredFile {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空征召同步文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$patchPath = Join-Path $floatingRoot 'Patches\Patch_PawnDraftController_FloatingFlight.cs'
$controllerPath = Join-Path $floatingRoot 'FloatingFlightStateController.cs'
$landingPath = Join-Path $floatingRoot 'FloatingFlightLandingUtility.cs'

$patchText = Read-RequiredFile $patchPath
$controllerText = Read-RequiredFile $controllerPath
$landingText = Read-RequiredFile $landingPath

Assert-True ($patchText -match 'Pawn_DraftController' -and $patchText -match 'set_Drafted|PropertySetter') '必须在原版征召属性切换入口接入浮空同步。'
Assert-True ($patchText -match 'Prefix' -and $patchText -match 'SynchronizeForDraftChange') '取消征召前必须调用即时浮空状态同步。'
Assert-True ($patchText -match 'if\s*\(\s*value\s*\)' -and $patchText -match 'value\s*\)[\s\S]*?return') '征召 setter 前置阶段不得提前添加浮空 Hediff，只允许处理取消征召。'
Assert-True ($controllerText -match 'SynchronizeForDraftChange') '状态控制器必须提供征召切换即时同步入口。'
Assert-True ($controllerText -match 'TryFindNearestLegalCell') '非法格取消征召必须寻找最近合法地面格。'
Assert-True ($controllerText -match 'forceGrounding|ForceGrounding|Immediate') '取消征召必须使用立即收地路径，不得只设置 PendingSafeLanding。'
Assert-True ($landingText -match 'TryFindNearestLegalCell') '安全落地工具必须提供最近合法地面格查询。'

Write-Output 'FloatingFlightDraftSynchronizationSmokeTests PASS'
