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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs\Trigger'

$rhythmConfigPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ChipAttackExecutionRhythmConfig.cs'
$interpreterPath = Join-Path $bdpSourceRoot 'Core\Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$validatorPath = Join-Path $bdpSourceRoot 'Core\Chips\Validation\DefaultChipDefinitionValidator.cs'
$devHarnessChipDefsPath = Join-Path $devHarnessRoot 'ThingDefs_BDP_TestChips.xml'

$rhythmConfigText = Get-Content -LiteralPath $rhythmConfigPath -Raw -Encoding utf8
$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$devHarnessChipDefsText = Get-Content -LiteralPath $devHarnessChipDefsPath -Raw -Encoding utf8

Assert-True (
    $rhythmConfigText -match '\bNormal\b'
) 'ChipAttackExecutionRhythmConfig must expose a Normal rhythm for the unified melee author contract.'

Assert-True (
    $rhythmConfigText -notmatch '\bSingle\b' -and $rhythmConfigText -notmatch '\bMulti\b'
) 'ChipAttackExecutionRhythmConfig must remove legacy melee Single and Multi rhythm values completely.'

Assert-True (
    $interpreterText -match 'TranslateMeleeRhythm\(hitCount\)'
) 'Interpreter must pass HitCount into melee rhythm derivation instead of reading Single or Multi declarations.'

$hasHitCountDerivedMeleeRhythmSignature = $interpreterText -match 'private static MeleeExecutionRhythm TranslateMeleeRhythm\(int hitCount\)'
$hasHitCountBranch = $interpreterText -match 'hitCount\s*>\s*1'

Assert-True (
    $hasHitCountDerivedMeleeRhythmSignature -and $hasHitCountBranch
) 'Interpreter melee rhythm derivation must be owned by HitCount.'

Assert-True (
    $validatorText -match 'ChipAttackExecutionRhythmConfig\.Normal'
) 'Chip definition validator must accept the unified Normal melee rhythm declaration.'

$meleeBlockMatch = [regex]::Match(
    $devHarnessChipDefsText,
    '(?s)<defName>BDP_TestChipMelee</defName>.*?<Execution>(.*?)</Execution>')

Assert-True (
    $meleeBlockMatch.Success
) 'DevHarness melee chip must still expose an Execution block.'

$meleeExecutionText = $meleeBlockMatch.Groups[1].Value

Assert-True (
    $meleeExecutionText -notmatch '<Rhythm>'
) 'DevHarness melee chip must prove that melee Rhythm is now optional by omitting it.'

Assert-True (
    $meleeExecutionText -match '<HitCount>[1-9]\d*</HitCount>'
) 'DevHarness melee chip must keep HitCount as the melee segment count source of truth.'

Write-Output 'MeleeNormalRhythmContract PASS'
