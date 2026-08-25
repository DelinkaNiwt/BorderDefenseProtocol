$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$drawPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding utf8

Assert-True (
    ($drawPatchText -match 'WeaponVisualStageResolver') -and
    ($drawPatchText -match 'TryHandleSingleWeaponTextureReplacement') -and
    ($drawPatchText -notmatch 'TryDrawSingleWeaponTextureReplacement')
) 'Both draw modes must share the stage resolver, and texture-only handling must describe handled-without-drawing semantics.'

Assert-True (
    ($drawPatchText -match 'bool handledAnyEntry = false') -and
    ($drawPatchText -match 'handledAnyEntry = true') -and
    ($drawPatchText -match 'return handledAnyEntry') -and
    ($drawPatchText -notmatch 'bool drewAny = false')
) 'Full replacement must track successfully handled poses, not only meshes actually drawn.'

Assert-True (
    ($drawPatchText -match 'WeaponStageSnapshot\s*=\s*weaponStageSnapshot') -and
    ($drawPatchText -match 'ResolveStageVisibility\(weaponStageSnapshot\.Stage\)')
) 'Complete and texture-only pose requests must carry the shared stage snapshot and read preset visibility.'

$residentMethod = [regex]::Match(
    $drawPatchText,
    '(?s)private static bool DrawResidentEntries\(.*?return handledAnyEntry;\s*\}').Value
Assert-True (
    ($residentMethod -match 'pose == null \|\| !pose\.IsValid[\s\S]*continue;') -and
    ($residentMethod -match 'handledAnyEntry = true;[\s\S]*ResolveStageVisibility[\s\S]*continue;[\s\S]*ApplyVanillaRecoil')
) 'A valid hidden pose must count as handled before drawing is skipped, while invalid poses still allow vanilla fallback.'

$singleMethod = [regex]::Match(
    $drawPatchText,
    '(?s)private static bool TryHandleSingleWeaponTextureReplacement\(.*?return true;\s*\}').Value
Assert-True (
    ($singleMethod -match 'WeaponStageResolver\.Resolve') -and
    ($singleMethod -match 'PoseResolver\.ResolveTextureOnly') -and
    ($singleMethod -match '!preset\.ResolveStageVisibility\(weaponStageSnapshot\.Stage\)[\s\S]*return true;')
) 'A valid hidden texture-only pose must suppress the vanilla equipment without drawing the replacement or overlays.'

Assert-True (
    ($drawPatchText -match 'case HostEquipmentRenderMode\.Keep:') -and
    ($drawPatchText -match 'case HostEquipmentRenderMode\.Suppress:')
) 'Existing Keep and Suppress host-equipment policies must remain explicit.'

Write-Output 'WeaponVisualStageDrawIntegrationSmokeTests PASS'
