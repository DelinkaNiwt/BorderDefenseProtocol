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

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空存档文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$modePath = Join-Path $floatingRoot 'FloatingFlightMode.cs'
$recordPath = Join-Path $floatingRoot 'FloatingFlightRecord.cs'
$ledgerPath = Join-Path $floatingRoot 'GameComponent_FloatingFlight.cs'
$servicePath = Join-Path $floatingRoot 'FloatingFlightLedgerService.cs'
$allPaths = @($modePath, $recordPath, $ledgerPath, $servicePath)
$allSource = ($allPaths | ForEach-Object { Read-RequiredFile $_ }) -join "`n"

$modeText = Read-RequiredFile $modePath
$recordText = Read-RequiredFile $recordPath
$ledgerText = Read-RequiredFile $ledgerPath
$serviceText = Read-RequiredFile $servicePath

Assert-True ($modeText -match 'enum\s+FloatingFlightMode') '必须有独立的三态浮空偏好枚举。'
Assert-True ($modeText -match 'Off\s*=\s*0') '关闭浮空必须是明确偏好值。'
Assert-True ($modeText -match 'MoveOnly\s*=\s*1') '仅移动时浮空必须是明确偏好值。'
Assert-True ($modeText -match 'Always\s*=\s*2') '始终浮空必须是明确偏好值。'

Assert-True ($recordText -match 'class\s+FloatingFlightRecord\s*:\s*IExposable') 'Pawn 浮空偏好必须由可深度存档记录承载。'
Assert-True ($recordText -match '\bPawn\s+Pawn\b') '记录必须保存 Pawn 引用。'
Assert-True ($recordText -match 'PawnThingId|ThingID') '记录必须保存 Pawn 稳定 ThingID 回退。'
Assert-True ($recordText -match 'PreferredMode\s*=\s*FloatingFlightMode\.Off') '首次记录默认必须关闭浮空。'
Assert-True ($recordText -match 'TransitionStartFactor') '记录必须保存视觉过渡起点。'
Assert-True ($recordText -match 'TransitionStartTick') '记录必须保存视觉过渡起始 tick。'
Assert-True ($recordText -match 'PendingSafeLanding') '记录必须保存安全降落等待标记。'
Assert-True ($recordText -match 'Scribe_References\.Look') '记录必须使用 Scribe_References 保存 Pawn。'
Assert-True ($recordText -match 'Scribe_Values\.Look') '记录必须使用 Scribe_Values 保存值字段。'
Assert-True (-not ($recordText -match '\b(IsFlying|isFlying|Flying)\b')) '记录不得复制实际飞行布尔状态。'
Assert-True (-not ($recordText -match '\b(Drafted|drafted|Moving|moving|Roof|roof|Path|path|Target|target)\b')) '记录不得保存原版实时路径状态。'

Assert-True ($ledgerText -match 'class\s+GameComponent_FloatingFlight\s*:\s*GameComponent') '浮空偏好必须使用 GameComponent。'
Assert-True ($ledgerText -match 'List\s*<\s*FloatingFlightRecord\s*>') 'GameComponent 必须维护 Pawn 记录集合。'
Assert-True ($ledgerText -match 'Scribe_Collections\.Look') 'GameComponent 必须深度保存记录集合。'
Assert-True ($ledgerText -match 'GetOrCreateRecord') 'GameComponent 必须提供按 Pawn 查找或创建入口。'
Assert-True (-not ($allSource -match 'class\s+(?:Comp|ThingComp|HediffComp)\w*\b')) '浮空偏好不得通过 Pawn ThingComp 注入。'
Assert-True (-not ($allSource -match '\bMap\s+[A-Za-z_]')) '浮空偏好账本不得绑定单张地图。'

Assert-True ($serviceText -match 'Current\.Game') '账本服务必须从当前存档解析 GameComponent。'
Assert-True ($serviceText -match 'GetComponent<GameComponent_FloatingFlight>') '账本服务必须读取正式浮空 GameComponent。'

Write-Output 'FloatingFlightPersistenceSmokeTests PASS'
