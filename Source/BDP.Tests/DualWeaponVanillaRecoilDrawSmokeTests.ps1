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
$drawPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding utf8

Assert-True (
    $drawPatchText -match 'ApplyVanillaRecoil\(\s*equipment,\s*triggerBody,\s*entry,\s*sample,\s*pose\);\s*DrawPose\(pose\);'
) '每个双武器视觉条目必须在绘制前独立应用原版后坐力。'

$methodMatch = [regex]::Match(
    $drawPatchText,
    '(?s)private static void ApplyVanillaRecoil\(.*?\r?\n        \}\r?\n\r?\n        /// <summary>')

Assert-True ($methodMatch.Success) '绘制补丁必须提供最小的原版后坐力适配成员。'
$methodText = $methodMatch.Value

Assert-True (
    ($methodText -match 'triggerBody\.VerbHostManager\.TryGetByResultId\(\s*entry\.ResultId,') -and
    ($methodText -match 'EquipmentUtility\.Recoil\(\s*equipment\.def,\s*binding\.RangedVerb,') -and
    ($methodText -match 'sample\.AimAngle')
) '后坐力必须按条目 ResultId 读取来源正式 Verb，并直接调用原版 EquipmentUtility.Recoil。'

Assert-True (
    ($methodText -match 'pose\.DrawPosition \+= drawOffset;') -and
    ($methodText -match 'pose\.DrawAngle \+= angleOffset;') -and
    ($methodText -match 'overlay\.DrawPosition \+= drawOffset;') -and
    ($methodText -match 'overlay\.DrawAngle \+= angleOffset;')
) '主贴图与附加层必须作为同一件武器应用相同后坐力。'

Assert-True (
    ($methodText -notmatch 'MuzzleAnchor') -and
    ($methodText -notmatch 'LaunchOrigin') -and
    ($methodText -notmatch 'Projectile')
) '视觉后坐力不得修改枪口锚点、发射原点或投射物。'

Write-Output 'DualWeaponVanillaRecoilDrawSmokeTests PASS'
