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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP.Content'

$transactionPath = Join-Path $bdpSourceRoot 'Assembly\Interaction\TriggerAssemblyTransaction.cs'
$resultPath = Join-Path $bdpSourceRoot 'Assembly\Interaction\TriggerAssemblyOperationResult.cs'

Assert-True (Test-Path -LiteralPath $transactionPath) 'TriggerAssemblyTransaction must exist.'
Assert-True (Test-Path -LiteralPath $resultPath) 'TriggerAssemblyOperationResult must exist.'

$transactionText = Get-Content -LiteralPath $transactionPath -Raw -Encoding utf8
$resultText = Get-Content -LiteralPath $resultPath -Raw -Encoding utf8

Assert-True (
    $transactionText -match 'class\s+TriggerAssemblyTransaction'
) 'TriggerAssemblyTransaction class must exist.'

Assert-True (
    ($transactionText -match 'ITriggerLoadoutReader') -and
    ($transactionText -match 'ITriggerLoadoutCommands')
) 'Transaction layer must depend on ITriggerLoadoutReader and ITriggerLoadoutCommands.'

Assert-True (
    $transactionText -notmatch 'CompTriggerBody|chipContainer|SetLoadedChip|formalChipContainer'
) 'Transaction layer must not directly edit CompTriggerBody internals.'

$firstLoadedChipRead = $transactionText.IndexOf('LoadedChip')
$firstUnloadCall = $transactionText.IndexOf('TryUnloadChip')
Assert-True (
    ($firstLoadedChipRead -ge 0) -and
    ($firstUnloadCall -gt $firstLoadedChipRead)
) 'Unload logic must read LoadedChip before calling TryUnloadChip.'

Assert-True (
    $transactionText -match 'TryLoadFromStorage[\s\S]*TryTakeChip[\s\S]*TryLoadChip[\s\S]*(TryStoreChip|DropChipNearAssembler)'
) 'Load transaction must return or drop the chip when TryLoadChip fails.'

Assert-True (
    $transactionText -match 'TryReplaceFromStorage[\s\S]*oldChip[\s\S]*TryLoadChip\(\s*targetSide\s*,\s*targetIndex\s*,\s*oldChip\s*\)'
) 'Replace transaction must try to restore the old chip if loading the new chip fails.'

Assert-True (
    $transactionText -match 'TryMoveOrSwapSlot[\s\S]*(TryLoadChip|TryUnloadChip)'
) 'Slot move/swap transaction must use loadout commands rather than direct slot mutation.'

Assert-True (
    ($resultText -match 'bool\s+Success') -and
    ($resultText -match 'string\s+ReasonCode') -and
    ($resultText -match 'string\s+Message')
) 'TriggerAssemblyOperationResult must expose Success, ReasonCode, and Message.'

Write-Output 'TriggerAssemblyTransaction PASS'
