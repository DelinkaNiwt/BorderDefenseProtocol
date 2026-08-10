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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'

$payloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'

$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8

Assert-True (
    ($payloadText -notmatch 'ExpressionChipExtensionSnapshot') -and
    ($payloadText -notmatch
        'IReadOnlyList<ExpressionChipExtensionSnapshot>\s+Extensions') -and
    ($payloadText -notmatch 'CloneExtensions')
) 'Expression runtime payload must not retain a generic chip-extension snapshot lane.'

Assert-True (
    ($collectorText -notmatch 'BuildExtensionSnapshots') -and
    ($collectorText -notmatch 'contract\.Extensions')
) 'Expression source collection must not copy arbitrary static chip extensions into runtime payloads.'

Write-Output 'ChipExtensionRuntimeIsolationSmokeTests PASS'
