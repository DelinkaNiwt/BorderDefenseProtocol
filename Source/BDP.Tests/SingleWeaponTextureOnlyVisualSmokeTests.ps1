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

$visualProjectionBuilderPath = Join-Path $bdpSourceRoot 'Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$visualProjectionPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualExpressionProjection.cs'
$visualResidentEntryPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualResidentEntry.cs'
$hostEquipmentRenderModePath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\HostEquipmentRenderMode.cs'
$drawPatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'

Assert-True (Test-Path -LiteralPath $visualProjectionBuilderPath) 'DefaultVisualProjectionBuilder must exist.'
Assert-True (Test-Path -LiteralPath $visualProjectionPath) 'VisualExpressionProjection must exist.'
Assert-True (Test-Path -LiteralPath $visualResidentEntryPath) 'VisualResidentEntry must exist.'
Assert-True (Test-Path -LiteralPath $hostEquipmentRenderModePath) 'HostEquipmentRenderMode must exist.'
Assert-True (Test-Path -LiteralPath $drawPatchPath) 'DrawEquipmentAiming visual patch must exist.'

$visualProjectionBuilderText = Get-Content -LiteralPath $visualProjectionBuilderPath -Raw -Encoding utf8
$visualProjectionText = Get-Content -LiteralPath $visualProjectionPath -Raw -Encoding utf8
$visualResidentEntryText = Get-Content -LiteralPath $visualResidentEntryPath -Raw -Encoding utf8
$hostEquipmentRenderModeText = Get-Content -LiteralPath $hostEquipmentRenderModePath -Raw -Encoding utf8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding utf8

Assert-True (
    ($visualProjectionText -match 'int\s+ActiveWeaponChipInstanceCount') -and
    ($visualProjectionBuilderText -match 'CountActiveWeaponChipInstances\(snapshot\)')
) 'Visual projection must carry the active weapon chip instance count, not infer single-weapon mode from result count.'

Assert-True (
    ($visualProjectionBuilderText -match 'HashSet<string>') -and
    ($visualProjectionBuilderText -match 'BuildWeaponChipInstanceKey') -and
    ($visualProjectionBuilderText -match 'SourceReference\.ChipThingId') -and
    ($visualProjectionBuilderText -match 'SourceReference\.Side') -and
    ($visualProjectionBuilderText -match 'SourceReference\.SlotIndex')
) 'Single-weapon detection must count distinct source chip instances with slot fallback, so one chip with primary/secondary verbs still counts as one weapon chip.'

Assert-True (
    ($hostEquipmentRenderModeText -match 'ReplaceTextureOnly') -and
    ($visualProjectionBuilderText -match 'activeWeaponChipInstanceCount\s*==\s*1') -and
    ($visualProjectionBuilderText -match 'HostEquipmentRenderMode\.ReplaceTextureOnly')
) 'A single active weapon chip instance must select a texture-only replacement mode instead of the full BDP visual replacement mode.'

Assert-True (
    ($visualProjectionBuilderText -match 'ResolveExecutionFocusPolicy') -and
    ($visualProjectionBuilderText -match 'ResolveMuzzleFollowPolicy') -and
    ($visualProjectionBuilderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualExecutionFocusPolicy\.None') -and
    ($visualProjectionBuilderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualMuzzleFollowPolicy\.None')
) 'Single-weapon texture-only mode must not publish execution-focus or muzzle-follow processing.'

Assert-True (
    ($visualResidentEntryText -match 'VerbAttackRole\s+VerbAttackRole') -and
    ($visualProjectionBuilderText -match 'VerbAttackRole\s*=\s*entry\.VerbAttackRole')
) 'Visual resident entries must retain the verb role so one chip with multiple weapon verbs can choose the primary texture deterministically.'

Assert-True (
    ($drawPatchText -match 'HostEquipmentRenderMode\.ReplaceTextureOnly') -and
    ($drawPatchText -match 'TryDrawSingleWeaponTextureReplacement') -and
    ($drawPatchText -match 'SelectTextureOnlyEntry') -and
    ($drawPatchText -match 'ResolveTextureOnlyPreset') -and
    ($drawPatchText -match 'DrawTextureOnlyReplacement')
) 'DrawEquipmentAiming patch must route single-weapon mode through a dedicated texture-only path.'

$textureOnlyMatch = [regex]::Match(
    $drawPatchText,
    '(?s)private static bool TryDrawSingleWeaponTextureReplacement\(.*?\n        \}')

Assert-True ($textureOnlyMatch.Success) 'TryDrawSingleWeaponTextureReplacement must exist as a small, inspectable helper.'

$textureOnlyBody = $textureOnlyMatch.Value
Assert-True (
    ($textureOnlyBody -notmatch 'PoseResolver') -and
    ($textureOnlyBody -notmatch 'VisualPoseRequest') -and
    ($textureOnlyBody -notmatch 'ResolveExecutionActive') -and
    ($textureOnlyBody -notmatch 'ResolveMuzzleActive')
) 'Texture-only replacement must not call the dual-weapon pose, execution-focus, or muzzle-focus path.'

Assert-True (
    $drawPatchText -match 'DrawTextureOnlyReplacement\(\s*equipment,\s*triggerBody,\s*entry,'
) '单武器贴图替换必须把 TriggerBody 与已选视觉条目交给后坐力计算。'

$singleDrawMethod = [regex]::Match(
    $drawPatchText,
    '(?s)private static void DrawTextureOnlyReplacement\(.*?\r?\n        \}\r?\n\r?\n        /// <summary>').Value

Assert-True (
    ($singleDrawMethod -match 'triggerBody\.VerbHostManager\.TryGetByResultId\(\s*entry\.ResultId,') -and
    ($singleDrawMethod -match 'EquipmentUtility\.Recoil\(\s*equipment\.def,\s*binding\.RangedVerb,') -and
    ($singleDrawMethod -notmatch 'EquipmentUtility\.GetRecoilVerb') -and
    ($singleDrawMethod -notmatch 'compEquippable\.AllVerbs')
) '单武器必须读取来源正式 RangedVerb，不得继续读取宿主装备 AllVerbs。'

Assert-True (
    ($drawPatchText -match 'EquipmentUtility\.Recoil') -and
    ($drawPatchText -match 'Graphic_StackCount') -and
    ($drawPatchText -match 'MatSingleFor')
) 'Texture-only replacement must mirror vanilla DrawEquipmentAiming pose/recoil rules and only swap the drawn material.'

Write-Output 'SingleWeaponTextureOnlyVisualSmokeTests PASS'
