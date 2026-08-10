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

# 事项08退役边界：旧候选装载按钮消失，正式形态按钮继续拥有自己的右键入口。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$devHarnessSourceRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'
$formalModeCommandPath = Join-Path $mainModRoot 'Source\BDP.Content\Trigger\UI\ChipModes\Command_ChipMode.cs'
$retiredTypeName = 'Command_ActionWithRightClickMenu'

$candidateSourceFiles = Get-ChildItem -LiteralPath $devHarnessSourceRoot -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$remainingReferences = @(
    $candidateSourceFiles |
        Select-String -SimpleMatch $retiredTypeName |
        ForEach-Object { $_.Path }
)

Assert-True ($remainingReferences.Count -eq 0) (
    'The retired DevHarness right-click command must not remain in candidate runtime source. Found: ' +
    ($remainingReferences -join ', '))

Assert-True (Test-Path -LiteralPath $formalModeCommandPath -PathType Leaf) `
    'The formal Content chip-mode command must remain available.'
$formalModeCommandText = Get-Content -LiteralPath $formalModeCommandPath -Raw -Encoding utf8
Assert-True (
    ($formalModeCommandText -match 'class\s+Command_ChipMode') -and
    ($formalModeCommandText -match 'override\s+IEnumerable<FloatMenuOption>\s+RightClickFloatMenuOptions')
) 'The formal Content chip-mode command must retain its independent right-click menu entry.'

Write-Output 'DevHarnessLegacyRightClickCommandRetirementSmokeTests PASS'
