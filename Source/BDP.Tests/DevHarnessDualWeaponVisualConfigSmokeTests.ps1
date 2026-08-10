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

function Get-ThingDefBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<ThingDef\s+ParentName=""ResourceBase"">\s*<defName>$DefName</defName>.*?</ThingDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-ExpressionEntryBlock {
    param(
        [string]$ThingDefBlock,
        [string]$EntryId
    )

    $match = [regex]::Match(
        $ThingDefBlock,
        "(?s)<Id>$EntryId</Id>.*?</li>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-VisualPresetBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<BDP\.Core\.Expressions\.ExpressionVisualPresetDef>\s*<defName>$DefName</defName>.*?</BDP\.Core\.Expressions\.ExpressionVisualPresetDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Assert-EntryPresentation {
    param(
        [string]$EntryBlock,
        [string]$ChipName,
        [string]$VisualPresetDefName,
        [string]$CompositeVisualPresetDefName,
        [string]$VisualPriority
    )

    Assert-True ($EntryBlock -ne $null) "$ChipName must keep its selected expression entry."
    Assert-True (
        ($EntryBlock -match '<Presentation>') -and
        ($EntryBlock -match "<VisualPresetDefName>$VisualPresetDefName</VisualPresetDefName>") -and
        ($EntryBlock -match "<CompositeVisualPresetDefName>$CompositeVisualPresetDefName</CompositeVisualPresetDefName>") -and
        ($EntryBlock -match '<ForceSuppressHostEquipment>true</ForceSuppressHostEquipment>') -and
        ($EntryBlock -match "<VisualPriority>$VisualPriority</VisualPriority>")
    ) "$ChipName must author the final dual-weapon visual Presentation block."
}

function Assert-VisualPreset {
    param(
        [string]$VisualBlock,
        [string]$DefName,
        [string]$PrimaryTexPath,
        [string]$ExpectedMuzzlePattern
    )

    Assert-True ($VisualBlock -ne $null) "$DefName must exist as a DevHarness visual preset."

    Assert-True (
        ($VisualBlock -match '<GraphicData>') -and
        ($VisualBlock -match "<texPath>$PrimaryTexPath</texPath>") -and
        ($VisualBlock -match '<drawSize>\(1, 1\)</drawSize>') -and
        ($VisualBlock -notmatch '<ActiveGraphicData>') -and
        ($VisualBlock -notmatch '<OverlayLayers>') -and
        ($VisualBlock -notmatch 'Things/Trigger/Visual') -and
        ($VisualBlock -notmatch '<DrawScale>') -and
        ($VisualBlock -notmatch '<SouthNorthPose>') -and
        ($VisualBlock -notmatch '<EastWestPose>') -and
        ($VisualBlock -match '<Muzzle>') -and
        ($VisualBlock -match '<IsRangedWeapon>true</IsRangedWeapon>') -and
        ($VisualBlock -match $ExpectedMuzzlePattern) -and
        ($VisualBlock -notmatch '<HasSubHandMuzzleOffsetOverride>') -and
        ($VisualBlock -notmatch '<SubHandMuzzleOffsetOverride>')
    ) "$DefName 应保留原贴图与枪口前向距离，并通过缺省节点继承缩放、姿态和副侧枪口位置。"
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'
$comboDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Combos\Test\ComboDefs_TestCombos.xml'
$visualDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml'
$visualTextureRoot = Join-Path $devHarnessRoot '1.6\Textures\Things\Trigger\Visual'

$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$comboDefsText = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $visualDefsPath) 'DevHarness must define visual presets for the existing sequential and volley chips.'
$visualDefsText = Get-Content -LiteralPath $visualDefsPath -Raw -Encoding utf8

$sequentialChipBlock = Get-ThingDefBlock $chipDefsText 'BDP_TestChipRanged'
$volleyChipBlock = Get-ThingDefBlock $chipDefsText 'BDP_TestChipRangedVolley'
$sequentialEntryBlock = Get-ExpressionEntryBlock $sequentialChipBlock 'test_ranged_primary'
$volleyEntryBlock = Get-ExpressionEntryBlock $volleyChipBlock 'test_ranged_volley_primary'

Assert-True (
    ($comboDefsText -match '<chipA>BDP_TestChipRanged</chipA>') -and
    ($comboDefsText -match '<chipB>BDP_TestChipRangedVolley</chipB>')
) 'The blind-check sample must reuse the existing sequential chip and volley chip without introducing a new chip.'

Assert-EntryPresentation `
    $sequentialEntryBlock `
    'BDP_TestChipRanged' `
    'BDP_TestVisual_RangedSequential' `
    'BDP_TestVisual_RangedSequential_Composite' `
    '10'

Assert-EntryPresentation `
    $volleyEntryBlock `
    'BDP_TestChipRangedVolley' `
    'BDP_TestVisual_RangedVolley' `
    'BDP_TestVisual_RangedVolley_Composite' `
    '20'

Assert-VisualPreset `
    (Get-VisualPresetBlock $visualDefsText 'BDP_TestVisual_RangedSequential') `
    'BDP_TestVisual_RangedSequential' `
    'Things/Item/Equipment/WeaponRanged/ChargeRifle' `
    '<MuzzleOffset>\(0, 0, 0\.68\)</MuzzleOffset>'

Assert-VisualPreset `
    (Get-VisualPresetBlock $visualDefsText 'BDP_TestVisual_RangedSequential_Composite') `
    'BDP_TestVisual_RangedSequential_Composite' `
    'Things/Item/Equipment/WeaponRanged/ChargeRifle' `
    '<MuzzleOffset>\(0, 0, 0\.72\)</MuzzleOffset>'

Assert-VisualPreset `
    (Get-VisualPresetBlock $visualDefsText 'BDP_TestVisual_RangedVolley') `
    'BDP_TestVisual_RangedVolley' `
    'Things/Item/Equipment/WeaponRanged/Autopistol' `
    '<MuzzleOffset>\(0, 0, 0\.58\)</MuzzleOffset>'

Assert-VisualPreset `
    (Get-VisualPresetBlock $visualDefsText 'BDP_TestVisual_RangedVolley_Composite') `
    'BDP_TestVisual_RangedVolley_Composite' `
    'Things/Item/Equipment/WeaponRanged/Autopistol' `
    '<MuzzleOffset>\(0, 0, 0\.61\)</MuzzleOffset>'

Assert-True ($visualDefsText -notmatch 'Things/Trigger/Visual') 'DevHarness visual presets must no longer reference generated temporary visual textures.'

if (Test-Path -LiteralPath $visualTextureRoot) {
    $remainingPngFiles = @(Get-ChildItem -LiteralPath $visualTextureRoot -File -Filter '*.png')
    Assert-True ($remainingPngFiles.Count -eq 0) 'Generated temporary visual texture png files must be removed from DevHarness.'
}

Write-Output 'DevHarnessDualWeaponVisualConfigSmokeTests PASS'
