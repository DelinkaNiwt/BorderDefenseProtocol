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

$readerPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\ITrionReader.cs'
$commandsPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\ITrionCommands.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionService.cs'
$compPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'

Assert-True (Test-Path -LiteralPath $readerPath) 'ITrionReader.cs must exist.'
Assert-True (Test-Path -LiteralPath $commandsPath) 'ITrionCommands.cs must exist.'
Assert-True (Test-Path -LiteralPath $servicePath) 'TrionService.cs must exist.'
Assert-True (Test-Path -LiteralPath $compPath) 'CompTrion.cs must exist.'

$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$commandsText = Get-Content -LiteralPath $commandsPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$compText = Get-Content -LiteralPath $compPath -Raw -Encoding utf8

Assert-True ($readerText -match 'using System\.Collections\.Generic;') 'ITrionReader must import generic collections for the drain snapshot.'
Assert-True ($readerText -match 'IReadOnlyDictionary<TrionDrainKey,\s*float>\s+GetDrainSnapshot\(\)') 'ITrionReader must expose a read-only drain snapshot.'
Assert-True ($commandsText -notmatch 'GetDrainSnapshot') 'ITrionCommands must not own read-only drain snapshot access.'
Assert-True ($serviceText -match 'public\s+IReadOnlyDictionary<TrionDrainKey,\s*float>\s+GetDrainSnapshot\(\)') 'TrionService must implement the shared reader snapshot method.'
Assert-True ($compText -match 'new\s+Dictionary<TrionDrainKey,\s*float>\s*\(\s*drainRegistry\s*\)') 'CompTrion.GetDrainSnapshot must return a copy instead of the live registry.'
Assert-True ($compText -match '单位为 Trion/秒') 'CompTrion drain registry comment must document per-second units.'
Assert-True ($compText -notmatch 'value 是每天消耗量') 'CompTrion drain registry comment must not describe drain values as per-day values.'

Write-Output 'TrionDrainSnapshotReaderSmokeTests PASS'
