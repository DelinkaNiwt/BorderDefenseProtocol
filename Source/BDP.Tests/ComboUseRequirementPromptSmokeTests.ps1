$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$trackerPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Requirements\ComboUseRequirementNoticeTracker.cs'
$coordinatorText = Get-Content -LiteralPath (
    Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
) -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $trackerPath
) 'Core must provide a per-trigger Combo blocked-notice tracker.'

$trackerText = Get-Content -LiteralPath $trackerPath -Raw -Encoding utf8
Assert-True (
    ($trackerText -match 'HashSet<string>') -and
    ($trackerText -match 'MessageTypeDefOf\.CautionInput') -and
    ($trackerText -match '暂时无法使用') -and
    ($trackerText -match 'Failures')
) 'The tracker must show one yellow message containing every failure.'
Assert-True (
    ($trackerText -match '\.Remove\(') -and
    ($trackerText -notmatch '恢复可用')
) 'Satisfied or absent Combos must clear the latch without a recovery message.'
Assert-True (
    $coordinatorText -match 'ComboUseRequirementNoticeTracker'
) 'The runtime coordinator must own the notice tracker instead of using global static state.'

Write-Output 'ComboUseRequirementPromptSmokeTests PASS'
