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
    ($drawPatchText -match 'TryDrawSingleWeaponTextureReplacement') -and
    ($drawPatchText -match 'EquipmentUtility\.Recoil')
) '单武器必须继续走只替换贴图入口，并保留原版后坐力计算。'

# 第一阶段新增一个小型附加层绘制边界，禁止借机切入完整视觉姿态管线。
$overlayDrawMatch = [regex]::Match(
    $drawPatchText,
    '(?s)private static void DrawTextureOnlyOverlayLayers\(.*?\n        \}')

Assert-True ($overlayDrawMatch.Success) '单武器贴图替换路径必须提供独立的附加层绘制帮助方法。'

$overlayDrawBody = $overlayDrawMatch.Value
Assert-True (
    ($overlayDrawBody -match 'preset\.OverlayLayers') -and
    ($overlayDrawBody -match 'layer\.ResolveGraphic\(false,\s*sourceThing\)') -and
    ($overlayDrawBody -match 'layer\.OnlyWhenActive') -and
    ($overlayDrawBody -match 'layer\.LocalOffset') -and
    ($overlayDrawBody -match 'layer\.AltitudeOffset') -and
    ($overlayDrawBody -match 'layer\.AngleOffset') -and
    ($overlayDrawBody -match 'layer\.DrawScale')
) '单武器附加层必须沿用未激活态语义和已有附加层变换字段。'

Assert-True (
    ($overlayDrawBody -notmatch 'VisualPoseResolver') -and
    ($overlayDrawBody -notmatch 'VisualPoseRequest') -and
    ($overlayDrawBody -notmatch 'ResolveExecutionActive') -and
    ($overlayDrawBody -notmatch 'ResolveMuzzleActive')
) '第一步不得进入完整视觉姿态、执行焦点或枪口焦点处理。'

# 主层与附加层必须复用同一份已解析原版姿态，避免重复计算导致错位。
Assert-True (
    ($drawPatchText -match 'DrawTextureOnlyGraphic') -and
    ($drawPatchText -match 'DrawTextureOnlyOverlayLayers\(\s*preset,\s*sourceThing,\s*mesh,\s*drawPosition,\s*drawAngle\)')
) '主层与附加层必须复用同一份原版网格、位置和角度。'

Write-Output 'SingleWeaponOverlayVisualSmokeTests PASS'
