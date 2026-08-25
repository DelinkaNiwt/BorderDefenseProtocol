$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 定位单武器贴图替换补丁。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$drawPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'

Assert-True (Test-Path -LiteralPath $drawPatchPath) '单武器贴图替换补丁必须存在。'

$drawPatchText = Get-Content -Raw -Encoding utf8 -LiteralPath $drawPatchPath

# 单武器仍须沿用现有 ReplaceTextureOnly（只替换贴图）入口与原版后坐力。
Assert-True (
    ($drawPatchText -match 'HostEquipmentRenderMode\.ReplaceTextureOnly') -and
    ($drawPatchText -match 'TryHandleSingleWeaponTextureReplacement') -and
    ($drawPatchText -match 'EquipmentUtility\.Recoil')
) '单武器必须继续走只替换贴图入口，并保留原版后坐力计算。'

# 单武器附加层必须使用同一份已经解析的原版姿态，阶段隐藏时由上层整套跳过。
$overlayDrawMatch = [regex]::Match(
    $drawPatchText,
    '(?s)private static void DrawTextureOnlyOverlayPoses\(.*?\n        \}')

Assert-True ($overlayDrawMatch.Success) '单武器贴图替换路径必须提供独立的附加层绘制帮助方法。'

$overlayDrawBody = $overlayDrawMatch.Value
Assert-True (
    ($overlayDrawBody -match 'ResolvedVisualOverlayPose') -and
    ($overlayDrawBody -match 'DrawTextureOnlyGraphic')
) '单武器附加层必须直接消费与主贴图同次解析得到的附加层姿态。'

# 主层与附加层必须复用同一份已解析原版姿态，避免重复计算导致错位。
Assert-True (
    ($drawPatchText -match 'DrawTextureOnlyGraphic') -and
    ($drawPatchText -match 'DrawTextureOnlyOverlayPoses\(sourceThing, pose\.OverlayPoses\)') -and
    ($drawPatchText -match 'ResolveStageVisibility')
) '主层与附加层必须复用同一份原版网格、位置和角度。'

Write-Output 'SingleWeaponOverlayVisualSmokeTests PASS'
