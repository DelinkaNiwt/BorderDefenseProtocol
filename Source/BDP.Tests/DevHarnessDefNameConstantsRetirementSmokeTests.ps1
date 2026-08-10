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

# 事项08退役边界：旧固定名称表消失，正式装配继续传递真实芯片实例。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$devHarnessSourceRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'
$assemblyWindowPath = Join-Path $mainModRoot 'Source\BDP.Content\Assembly\Window\Window_TriggerAssembly.cs'
$assemblyTransactionPath = Join-Path $mainModRoot 'Source\BDP.Content\Assembly\Interaction\TriggerAssemblyTransaction.cs'

$candidateSourceFiles = Get-ChildItem -LiteralPath $devHarnessSourceRoot -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$remainingReferences = @(
    $candidateSourceFiles |
        Select-String -SimpleMatch 'TriggerDevHarnessDefs' |
        ForEach-Object { $_.Path }
)

Assert-True ($remainingReferences.Count -eq 0) (
    'The retired DevHarness def-name constants must not remain in candidate runtime source. Found: ' +
    ($remainingReferences -join ', '))

$assemblyWindowText = Get-Content -LiteralPath $assemblyWindowPath -Raw -Encoding utf8
$assemblyTransactionText = Get-Content -LiteralPath $assemblyTransactionPath -Raw -Encoding utf8

Assert-True (
    ($assemblyWindowText -match 'GetAvailableChips\(\)') -and
    ($assemblyWindowText -match 'TryLoadFromStorage\([^;]*dragState\.Chip\)')
) 'The formal assembly window must select and submit actual stored chip instances.'

Assert-True (
    ($assemblyTransactionText -match 'TryLoadFromStorage\(TriggerSide\s+side,\s*int\s+slotIndex,\s*Thing\s+chip\)') -and
    ($assemblyTransactionText -match 'provider\.TryTakeChip\(chip\)') -and
    ($assemblyTransactionText -match 'commands\.TryLoadChip\(side,\s*slotIndex,\s*chip\)')
) 'The formal assembly transaction must move the selected Thing instead of looking up a hard-coded defName.'

Write-Output 'DevHarnessDefNameConstantsRetirementSmokeTests PASS'
