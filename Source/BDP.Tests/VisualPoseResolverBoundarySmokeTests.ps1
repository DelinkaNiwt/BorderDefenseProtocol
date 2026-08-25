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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$poseResolverPath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\VisualPoseResolver.cs'
$poseRequestPath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\VisualPoseRequest.cs'
$resolvedPosePath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\ResolvedVisualPose.cs'
$resolvedOverlayPosePath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\ResolvedVisualOverlayPose.cs'
$resolvedMuzzleAnchorPath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\ResolvedMuzzleAnchor.cs'
$meshKindPath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\VisualMeshKind.cs'
$drawPatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$projectileInitStagePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$shootVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_Shoot.cs'

Assert-True (Test-Path -LiteralPath $poseResolverPath) 'VisualPoseResolver must exist as the single pose and muzzle anchor calculator.'
Assert-True (Test-Path -LiteralPath $poseRequestPath) 'VisualPoseRequest must exist as the final pose request contract.'
Assert-True (Test-Path -LiteralPath $resolvedPosePath) 'ResolvedVisualPose must exist as the final resolved draw pose contract.'
Assert-True (Test-Path -LiteralPath $resolvedOverlayPosePath) 'ResolvedVisualOverlayPose must exist as the final resolved overlay-pose contract.'
Assert-True (Test-Path -LiteralPath $resolvedMuzzleAnchorPath) 'ResolvedMuzzleAnchor must exist as the final resolved muzzle-anchor contract.'
Assert-True (Test-Path -LiteralPath $meshKindPath) 'VisualMeshKind must exist as the explicit mesh selection enum.'
Assert-True (Test-Path -LiteralPath $drawPatchPath) 'PawnRenderUtility.DrawEquipmentAiming visual patch must exist.'

$poseResolverText = Get-Content -LiteralPath $poseResolverPath -Raw -Encoding utf8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding utf8
$projectileInitStageText = Get-Content -LiteralPath $projectileInitStagePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8

Assert-True (
    ($poseResolverText -match 'AimMirror') -and
    ($poseResolverText -match 'HandMirror') -and
    ($poseResolverText -match 'FacingMirror') -and
    ($poseResolverText -match 'MirrorOnNorth') -and
    ($poseResolverText -match 'ResolvedMuzzleAnchor')
) 'VisualPoseResolver must separate aim mirror, hand mirror, north-facing mirror, and resolved muzzle anchor truth.'

Assert-True (
    ($poseResolverText -match 'ResolveWeaponStage\(request\)') -and
    ($poseResolverText -match 'ResolveGripAnchor\(request, calculation\)') -and
    ($poseResolverText -match 'ResolveMuzzleAnchor\(request, calculation\)')
) 'Stage-aware graphics must continue through the shared pose, grip and muzzle resolver instead of creating a parallel pose path.'

Assert-True (
    ($drawPatchText -match 'HarmonyPatch\(typeof\(PawnRenderUtility\),\s*"DrawEquipmentAiming"\)') -and
    ($drawPatchText -match 'EquipmentPoseSample') -and
    ($drawPatchText -match 'VisualPoseResolver') -and
    ($drawPatchText -match 'HostEquipmentRenderMode')
) 'The draw patch must hang on PawnRenderUtility.DrawEquipmentAiming and consume the final visual runtime contracts.'

Assert-True (
    ($shootVerbText -match 'ResolveLaunchRoot') -and
    ($projectileInitStageText -match 'SourceResultId') -and
    ($projectileInitStageText -notmatch 'plan\.HasAbsoluteOriginWorld\s*=\s*true;\s*[\r\n\s]*plan\.AbsoluteOriginWorld\s*=\s*resolution\.RootOriginWorld')
) 'The firing boundary must resolve the live visual muzzle by source result, while ProjectileInit must not freeze visual-driven roots into AbsoluteOriginWorld.'

Write-Output 'VisualPoseResolverBoundarySmokeTests PASS'
