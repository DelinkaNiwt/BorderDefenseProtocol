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

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空状态文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$controllerPath = Join-Path $floatingRoot 'FloatingFlightStateController.cs'
$landingPath = Join-Path $floatingRoot 'FloatingFlightLandingUtility.cs'
$componentPath = Join-Path $floatingRoot 'GameComponent_FloatingFlight.cs'
$recordPath = Join-Path $floatingRoot 'FloatingFlightRecord.cs'
$hediffPath = Join-Path $floatingRoot 'Hediff_FloatingFlight.cs'

$controllerText = Read-RequiredFile $controllerPath
$landingText = Read-RequiredFile $landingPath
$componentText = Read-RequiredFile $componentPath
$recordText = Read-RequiredFile $recordPath
$hediffText = Read-RequiredFile $hediffPath

Assert-True ($controllerText -match 'Reconcile\s*\(') '状态控制器必须提供统一 Reconcile 入口。'
Assert-True ($controllerText -match 'RequestActualFlight') '状态控制器必须提供实际飞行切换入口。'
Assert-True ($controllerText -match 'GetPresentationFactor') '状态控制器必须提供视觉高度计算入口。'
Assert-True ($controllerText -match 'Mathf\.MoveTowards|Mathf\.Lerp|MoveTowards|Lerp') '视觉过渡必须使用线性插值。'
Assert-True ($controllerText -match 'TransitionStartFactor' -and $controllerText -match 'TransitionStartTick') '反转必须使用记录中的动画锚点。'
Assert-True ($controllerText -match 'pather\.Moving') '仅移动时浮空必须只依赖原版路径执行状态。'
Assert-True ($controllerText -match 'PendingSafeLanding') '非法格关闭必须进入安全降落等待。'
Assert-True ($controllerText -match 'Downed|Dead|Destroyed|Spawned') '倒地、死亡和生命周期变化必须有立即归地处理。'
Assert-True ($controllerText -match 'GetFirstHediffOfDef|IsActuallyFlying') '状态控制必须以实际 Hediff 为唯一飞行事实。'
Assert-True (-not ($controllerText -match 'class\s+.*\bIsFlying\b|bool\s+isFlying')) '状态控制器不得保存第二份实际飞行布尔值。'

Assert-True ($landingText -match 'TryFind|Find.*Stand|Walkable') '安全降落工具必须寻找原版合法站立格。'
Assert-True ($landingText -match 'curPath|Peek') '安全降落必须优先沿当前路径找前向合法格。'
Assert-True ($landingText -match 'destination|Destination') '安全降落恢复必须保留原版路径目标语义。'

Assert-True ($componentText -match 'GameComponentTick') 'GameComponent 必须驱动已登记 Pawn 的状态收口。'
Assert-True ($componentText -match 'FloatingFlightStateController') 'GameComponent Tick 必须调用统一状态控制器。'
Assert-True ($recordText -match 'PendingSafeLanding') '记录必须保存安全降落等待标记。'
Assert-True ($hediffText -match 'PostAdd' -and $hediffText -match 'PostRemoved') 'Hediff 外部增删必须通知状态控制器以建立动画锚点。'

Write-Output 'FloatingFlightStateControllerSmokeTests PASS'
