$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-RequiredFile {
    param([string]$Path)
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空视觉文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$presentationRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Presentation'
$utilityText = Read-RequiredFile (Join-Path $presentationRoot 'FloatingFlightPresentationUtility.cs')
$patchText = Read-RequiredFile (Join-Path $presentationRoot 'Patch_FloatingFlightPresentation.cs')
$presentationText = $utilityText + "`n" + $patchText

Assert-True ($utilityText -match 'GetPresentationFactor') '视觉高度必须读取状态控制器的表现因子。'
Assert-True ($utilityText -match 'presentationHeight') '视觉高度必须来自 Hediff 配置字段。'
Assert-True ($utilityText -match 'bobAmplitude|bobPeriodTicks') '视觉工具必须保留可配置起伏参数。'
Assert-True ($utilityText -match 'GetBobPhase') '视觉工具必须为每个 Pawn 计算独立起伏相位。'
Assert-True ($utilityText -match 'thingIDNumber') '独立起伏相位必须来自 Pawn 的稳定唯一编号。'
Assert-True ($utilityText -match 'Mathf\.Sin\([\s\S]*GetBobPhase') '正弦起伏必须消费 Pawn 独立相位。'
Assert-True ($patchText -match 'Pawn_DrawTracker|DrawPos') '视觉偏移必须接入原版 Pawn_DrawTracker.DrawPos 绘制链。'
Assert-True ($patchText -match 'get_DrawPos') '视觉补丁必须定位 Pawn_DrawTracker.DrawPos 的 getter。'
Assert-True (-not ($patchText -match 'DynamicDrawPhaseAt')) '视觉偏移不得和原版 DrawPos 叠加两次。'
Assert-True ($patchText -match 'DrawShadowInternal') '视觉补丁必须单独处理阴影。'
Assert-True ($patchText -match 'FlightShadowMaterial') '阴影必须复用原版飞行影子材质。'
Assert-True ($patchText -match 'GetPresentationFactor') '阴影大小或位置必须读取 BDP 表现因子。'
Assert-True ($presentationText -match 'Spawned|Dead|Downed') '倒地、死亡或离图时不得抬高绘制。'
Assert-True (-not ($patchText -match '\bPawn\s*\.\s*Position\s*=|___pawn\s*\.\s*Position\s*=')) '视觉补丁不得改写 Pawn 逻辑位置。'

Write-Output 'FloatingFlightPresentationSmokeTests PASS'
