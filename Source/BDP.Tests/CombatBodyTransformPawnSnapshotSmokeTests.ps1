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
$snapshotPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyPawnVisualSnapshot.cs'
$capturePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyPawnVisualCapture.cs'
$suppressionPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyPawnRenderSuppression.cs'
$patchPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\Patch_PawnRenderer_RenderPawnAt_CombatBodyScan.cs'

Assert-True -Condition (Test-Path -LiteralPath $snapshotPath) -Message '缺少完整人物视觉快照类型。'
Assert-True -Condition (Test-Path -LiteralPath $capturePath) -Message '缺少完整人物视觉捕获器。'
Assert-True -Condition (Test-Path -LiteralPath $suppressionPath) -Message '缺少完整人物绘制替代状态。'
Assert-True -Condition (Test-Path -LiteralPath $patchPath) -Message '缺少完整人物绘制入口补丁。'

$snapshotText = Get-Content -LiteralPath $snapshotPath -Raw -Encoding utf8
$captureText = Get-Content -LiteralPath $capturePath -Raw -Encoding utf8
$suppressionText = Get-Content -LiteralPath $suppressionPath -Raw -Encoding utf8
$patchText = Get-Content -LiteralPath $patchPath -Raw -Encoding utf8
$combinedText = $snapshotText + "`n" + $captureText

Assert-True -Condition ($captureText -match 'private const int SnapshotSize = 128;') -Message '快照必须沿用原版人物图集的 128 尺寸。'
Assert-True -Condition ($captureText -match 'Find\.PawnCacheRenderer\.RenderPawn') -Message '必须复用原版完整人物缓存渲染器。'
Assert-True -Condition ($captureText -match 'renderTree\.SetDirty\(\)') -Message '捕获前必须重建当前完整人物渲染树。'
Assert-True -Condition ($captureText -match 'RenderTextureFormat\.ARGB32') -Message '快照必须支持透明背景。'
Assert-True -Condition ($captureText -match 'Stack<CombatBodyPawnVisualSnapshot>') -Message '快照资源必须有限复用。'
Assert-True -Condition ($captureText -match 'private const int MaxRetainedSnapshots = 8;') -Message '快照池必须限制空闲资源数量。'
Assert-True -Condition ($captureText -match 'internal static void Release') -Message '所有异常路径必须可归还快照。'
Assert-True -Condition ($captureText -match 'previousCameraPosition[\s\S]*previousOrthographicSize[\s\S]*previousTargetTexture') -Message '捕获器必须保存原版缓存相机的完整临时状态。'
Assert-True -Condition ($captureText -match 'finally[\s\S]*transform\.position = previousCameraPosition[\s\S]*orthographicSize = previousOrthographicSize[\s\S]*targetTexture = previousTargetTexture') -Message '原版缓存渲染异常时必须恢复相机位置、缩放和目标纹理。'
Assert-True -Condition ($captureText -match 'PawnPosture\.Standing') -Message '当前完整快照方案必须限制为站立人物。'
Assert-True -Condition ($combinedText -notmatch 'ApparelGraphicRecordGetter|PawnRenderNodeWorker_Apparel') -Message '完整人物捕获不得理解衣物节点。'
Assert-True -Condition ($suppressionText -match 'Dictionary<int, SuppressionState>') -Message '替代状态必须按 Pawn 隔离。'
Assert-True -Condition ($suppressionText -match 'HideMotes') -Message '隐藏 Mote 时必须立即恢复原版人物绘制。'
Assert-True -Condition ($patchText -match 'HarmonyPatch\(typeof\(PawnRenderer\), nameof\(PawnRenderer\.RenderPawnAt\)\)') -Message '只允许补丁完整人物绘制入口。'
Assert-True -Condition ($patchText -match 'Pawn ___pawn') -Message '补丁必须使用 Harmony 私有字段注入取得目标 Pawn。'
Assert-True -Condition ($patchText -notmatch 'PawnRenderNodeWorker_Apparel') -Message '完整人物替代补丁不得接触衣物节点。'
Assert-True -Condition (($suppressionText + $patchText) -notmatch 'AccessTools|BindingFlags') -Message '完整人物替代链不得使用运行时反射。'

Write-Output 'CombatBodyTransformPawnSnapshot PASS'
