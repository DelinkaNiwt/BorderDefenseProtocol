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
$poseRequestPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\VisualPoseRequest.cs'
$poseResolverPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$stageResolverPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\WeaponVisualStageResolver.cs'
$drawPatchPath = Join-Path $sourceRoot 'BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$launchOriginResolverPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\TriggerVisualLaunchOriginResolver.cs'
$diagnosticsAccessPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'

foreach ($path in @($poseRequestPath, $poseResolverPath, $stageResolverPath, $drawPatchPath, $launchOriginResolverPath, $diagnosticsAccessPath)) {
    Assert-True (Test-Path -LiteralPath $path) "Required visual source file is missing: $path"
}

$poseRequestText = Get-Content -LiteralPath $poseRequestPath -Raw -Encoding utf8
$poseResolverText = Get-Content -LiteralPath $poseResolverPath -Raw -Encoding utf8
$stageResolverText = Get-Content -LiteralPath $stageResolverPath -Raw -Encoding utf8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding utf8
$launchOriginResolverText = Get-Content -LiteralPath $launchOriginResolverPath -Raw -Encoding utf8
$diagnosticsAccessText = Get-Content -LiteralPath $diagnosticsAccessPath -Raw -Encoding utf8

Assert-True (
    $poseRequestText -match 'public bool IsMatchingAttackStanceActive\s*\{\s*get;\s*set;\s*\}'
) 'Pose requests must carry the live matched-attack stance truth independently of stored execution records.'
Assert-True (
    $poseRequestText -notmatch 'TriggerVisualRuntimeState\s+RuntimeState'
) 'Pose requests must not retain the old stored-runtime-state input used as a false current-attack signal.'

$attackStanceMethod = [regex]::Match(
    $stageResolverText,
    'internal bool IsMatchingAttackStanceActive\([\s\S]*?^        \}',
    [System.Text.RegularExpressions.RegexOptions]::Multiline).Value
Assert-True (
    $attackStanceMethod -match 'Stance_Warmup' -and
    $attackStanceMethod -match 'hostVerb\.Bursting' -and
    $attackStanceMethod -match 'Stance_Cooldown' -and
    $attackStanceMethod -match 'BdpVerb_FormalHostShoot' -and
    $attackStanceMethod -match 'BdpVerb_FormalHostMelee' -and
    $attackStanceMethod -match 'meleeVerb\.HostSessionToken' -and
    $attackStanceMethod -match 'visualRuntimeState\.HasExecutionState' -and
    $attackStanceMethod -match 'HasValidFormalSession'
) 'Only a live warmup, burst or cooldown stance with a matching ranged or melee BDP formal session may suppress idle rotation.'

$residentDrawMethod = [regex]::Match(
    $drawPatchText,
    'private static bool DrawResidentEntries\([\s\S]*?^        \}',
    [System.Text.RegularExpressions.RegexOptions]::Multiline).Value
$firstResidentLoop = $residentDrawMethod.IndexOf('for (int i = 0; i < visualProjection.ResidentEntries.Count; i++)')
$liveStanceCheck = $residentDrawMethod.IndexOf('WeaponStageResolver.IsMatchingAttackStanceActive(')
Assert-True (
    $residentDrawMethod.Length -gt 0 -and
    $liveStanceCheck -ge 0 -and
    $firstResidentLoop -gt $liveStanceCheck -and
    $residentDrawMethod -match 'IsMatchingAttackStanceActive\s*=\s*isMatchingAttackStanceActive'
) 'The draw path must compute one live attack truth before its entry loop and pass that same truth to both hands.'

Assert-True (
    $launchOriginResolverText -match 'IsMatchingAttackStanceActive\s*=\s*[\s\S]{0,160}?IsMatchingAttackStanceActive\('
) 'Live muzzle-anchor resolution must use the same attack-pose truth as equipment drawing.'

Assert-True (
    $diagnosticsAccessText -match 'IsMatchingAttackStanceActive\s*=\s*[\s\S]{0,160}?IsMatchingAttackStanceActive\('
) 'Visual diagnostics must resolve the same live attack pose shown by equipment drawing.'

$idlePoseHelper = [regex]::Match(
    $poseResolverText,
    'private static bool IsAnyAttackStanceActive\(VisualPoseRequest request\)([\s\S]*?)^        \}',
    [System.Text.RegularExpressions.RegexOptions]::Multiline).Value
Assert-True (
    $idlePoseHelper -match 'request\?\.IsMatchingAttackStanceActive\s*==\s*true' -and
    $idlePoseHelper -notmatch 'RuntimeState|HasExecutionState' -and
    $poseResolverText -match 'IsAnyAttackStanceActive\(request\)'
) 'Idle-only pose rules must read the live request flag, never infer a current attack from stored runtime state.'

$textureOnlyMethod = [regex]::Match(
    $poseResolverText,
    'public ResolvedVisualPose ResolveTextureOnly\(VisualPoseRequest request\)([\s\S]*?)^        \}',
    [System.Text.RegularExpressions.RegexOptions]::Multiline).Value
Assert-True (
    $textureOnlyMethod -match 'CalculateVanillaPose\(request\)' -and
    $textureOnlyMethod -notmatch 'CalculatePose\(request\)'
) 'Single-weapon texture replacement must retain its vanilla pose-only path.'

Write-Output 'VisualLiveAttackStanceRotationSmokeTests PASS'
