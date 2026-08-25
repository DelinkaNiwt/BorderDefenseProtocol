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

$matcherPath = Join-Path $bdpSourceRoot 'Core\Expressions\Utilities\ExpressionSourceReferenceMatcher.cs'
$builderPath = Join-Path $bdpSourceRoot 'Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'

Assert-True (Test-Path -LiteralPath $matcherPath) 'ExpressionSourceReferenceMatcher must exist.'
Assert-True (Test-Path -LiteralPath $builderPath) 'DefaultVisualProjectionBuilder must exist.'

$matcherText = Get-Content -LiteralPath $matcherPath -Raw -Encoding utf8
$builderText = Get-Content -LiteralPath $builderPath -Raw -Encoding utf8

Assert-True (
    ($matcherText -match 'BuildChipInstanceKey\(ExpressionSourceReference sourceReference\)') -and
    ($matcherText -match 'AreSameChipInstance\(')
) 'The shared matcher must expose chip identity key construction and equality checks.'

Assert-True (
    ($matcherText -match 'sourceReference\.ChipThingId') -and
    ($matcherText -match 'sourceReference\.Side') -and
    ($matcherText -match 'sourceReference\.SlotIndex') -and
    ($matcherText -match 'sourceReference\.ChipDefName')
) 'Chip identity must prefer the live thing id and retain the side/slot/def fallback used after load.'

Assert-True (
    ($builderText -match 'ExpressionSourceReferenceMatcher\.BuildChipInstanceKey') -and
    ($builderText -notmatch 'BuildWeaponChipInstanceKey')
) 'Visual projection must use the shared source matcher instead of maintaining a private identity rule.'

Write-Output 'ExpressionSourceReferenceMatcherSmokeTests PASS'
