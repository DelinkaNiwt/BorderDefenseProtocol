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

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Expected,
        [string]$Message
    )

    Assert-True ($Text.Contains($Expected)) $Message
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$windowRoot = Join-Path $sourceRoot 'BDP.Content\Assembly\Window'
$capturePath = Join-Path $windowRoot 'AssemblyPawnPortraitCapture.cs'
$snapshotPath = Join-Path $windowRoot 'AssemblyPawnPortraitSnapshot.cs'
$woundPatchPath = Join-Path $windowRoot 'Patch_PawnRenderNodeWorker_OverlayWounds_AssemblyPortrait.cs'
$panelPath = Join-Path $windowRoot 'Panel_SlotLayout.cs'
$windowPath = Join-Path $windowRoot 'Window_TriggerAssembly.cs'

Assert-True (Test-Path -LiteralPath $capturePath -PathType Leaf) `
    '装配台必须有独立的 Pawn 正面肖像捕获入口。'
Assert-True (Test-Path -LiteralPath $snapshotPath -PathType Leaf) `
    '装配台必须拥有独立的运行时肖像快照资源。'

$capture = Get-Content -LiteralPath $capturePath -Raw -Encoding utf8
$snapshot = Get-Content -LiteralPath $snapshotPath -Raw -Encoding utf8
$woundPatch = Get-Content -LiteralPath $woundPatchPath -Raw -Encoding utf8
$panel = Get-Content -LiteralPath $panelPath -Raw -Encoding utf8
$window = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8

Assert-Contains $capture 'RenderPawn(' `
    '装配台肖像必须复用 RimWorld 原版 PawnCacheRenderer。'
Assert-Contains $capture 'Rot4.South' `
    '装配台肖像必须固定为面朝用户的正面朝向。'
Assert-Contains $capture '0f' `
    '装配台肖像必须使用直立角度，而不是继承当前动作倾角。'
Assert-Contains $woundPatch 'PawnRenderNodeWorker_OverlayWounds' `
    '装配台肖像必须明确屏蔽伤口覆盖层。'
Assert-Contains $woundPatch 'IsCapturingWithoutWounds' `
    '伤口覆盖层只能在装配台肖像捕获期间被屏蔽。'
Assert-Contains $snapshot 'RenderTexture' `
    '装配台肖像必须使用运行时 RenderTexture，而不是 PNG 文件。'
Assert-Contains $snapshot 'DestroyResources' `
    '装配台肖像快照必须提供底层 Unity 资源释放。'
Assert-Contains $panel 'GUI.DrawTexture' `
    '左栏必须把实际 Pawn 肖像绘制到中央区域。'
Assert-True ($panel -notmatch 'OperatorFigureRenderer\.Draw') `
    '左栏不得继续调用被放弃的矢量人物绘制器。'
Assert-Contains $window 'PostClose' `
    '装配台窗口关闭时必须释放 Pawn 肖像资源。'
Assert-Contains $window 'DestroyResources' `
    '装配台窗口必须在关闭生命周期中销毁肖像快照资源。'
Assert-True ($window -notmatch 'Texture2D\.EncodeToPNG|Application\.persistentDataPath|File\.WriteAllBytes') `
    '该方案不得生成或写入 PNG 文件。'

Write-Output 'AssemblyPawnPortrait PASS'
