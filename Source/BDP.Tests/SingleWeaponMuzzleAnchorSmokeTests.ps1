$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$drawPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$launchResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\TriggerVisualLaunchOriginResolver.cs'
$diagnosticsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'

$resolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $resolverPath
$drawPatchText = Get-Content -Raw -Encoding utf8 -LiteralPath $drawPatchPath
$launchResolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $launchResolverPath
$diagnosticsText = Get-Content -Raw -Encoding utf8 -LiteralPath $diagnosticsPath

Assert-True (
    ($resolverText -match 'ResolveTextureOnly\(VisualPoseRequest request\)') -and
    ($resolverText -match 'CalculateVanillaPose\(request\)') -and
    ($resolverText -match 'MuzzleAnchor = ResolveMuzzleAnchor\(request, calculation\)')
) '视觉姿态解析器必须提供按原版贴图姿态解算单武器枪口锚点的入口。'

$textureOnlyMethod = [regex]::Match(
    $resolverText,
    '(?s)public ResolvedVisualPose ResolveTextureOnly\(.*?\r?\n        \}\r?\n\r?\n        /// <summary>').Value
Assert-True (-not [string]::IsNullOrWhiteSpace($textureOnlyMethod)) '必须能定位单武器原版姿态解析成员。'
Assert-True (
    ($textureOnlyMethod -notmatch 'ResolveSouthNorthOffset') -and
    ($textureOnlyMethod -notmatch 'ResolveEastWestOffset') -and
    ($textureOnlyMethod -notmatch 'AlignDrawPositionToGrip')
) '单武器原版姿态不得进入双武器偏移、手侧裁决或握持姿态原点。'

Assert-True (
    $drawPatchText -match 'PoseResolver\.ResolveTextureOnly\(new VisualPoseRequest'
) '单武器绘制必须使用共享的原版姿态枪口解析结果。'

Assert-True (
    ($launchResolverText -match 'HostEquipmentRenderMode\.ReplaceTextureOnly') -and
    ($launchResolverText -match 'resolver\.TryResolveTextureOnlyMuzzleAnchor')
) '实际发射原点必须在单武器模式使用原版姿态枪口锚点。'

Assert-True (
    ($diagnosticsText -match 'HostEquipmentRenderMode\.ReplaceTextureOnly') -and
    ($diagnosticsText -match 'VisualPoseResolver\.ResolveTextureOnly\(request\)')
) '现有点位诊断必须在单武器模式显示同一原版姿态枪口锚点。'

Write-Output 'SingleWeaponMuzzleAnchorSmokeTests PASS'
