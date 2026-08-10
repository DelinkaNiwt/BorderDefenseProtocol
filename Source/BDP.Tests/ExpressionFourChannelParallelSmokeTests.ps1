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

$resultKindPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionResultKind.cs'
$entryKindPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Contract\ChipExpressionEntryKind.cs'
$channelIndexPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionChannelIndex.cs'
$channelIndexBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\ExpressionChannelIndexBuilder.cs'
$projectionStatePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerCombatProjectionState.cs'
$projectionBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerCombatProjectionBuilder.cs'
$readerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Contracts\IExpressionReader.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'

$resultKindText = Get-Content -LiteralPath $resultKindPath -Raw -Encoding utf8
$entryKindText = Get-Content -LiteralPath $entryKindPath -Raw -Encoding utf8
$projectionStateText = Get-Content -LiteralPath $projectionStatePath -Raw -Encoding utf8
$projectionBuilderText = Get-Content -LiteralPath $projectionBuilderPath -Raw -Encoding utf8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8

Assert-True ($resultKindText -match 'Verb') 'ExpressionResultKind must keep Verb.'
Assert-True ($resultKindText -match 'Ability') 'ExpressionResultKind must keep Ability.'
Assert-True ($resultKindText -match 'Hediff') 'ExpressionResultKind must keep Hediff.'
Assert-True ($resultKindText -match 'Passive') 'ExpressionResultKind must keep Passive.'

Assert-True ($entryKindText -match 'PrimaryVerb') 'Authoring must keep PrimaryVerb.'
Assert-True ($entryKindText -match 'SecondaryVerb') 'Authoring must keep SecondaryVerb.'
Assert-True ($entryKindText -match 'Ability') 'Authoring must keep Ability.'
Assert-True ($entryKindText -match 'Hediff') 'Authoring must keep Hediff.'
Assert-True ($entryKindText -match 'Passive') 'Authoring must keep Passive.'

Assert-True (Test-Path -LiteralPath $channelIndexPath) 'ExpressionChannelIndex must exist.'
Assert-True (Test-Path -LiteralPath $channelIndexBuilderPath) 'ExpressionChannelIndexBuilder must exist.'

$channelIndexText = Get-Content -LiteralPath $channelIndexPath -Raw -Encoding utf8

Assert-True ($channelIndexText -match 'VerbResults') 'Channel index must expose VerbResults.'
Assert-True ($channelIndexText -match 'AbilityResults') 'Channel index must expose AbilityResults.'
Assert-True ($channelIndexText -match 'HediffResults') 'Channel index must expose HediffResults.'
Assert-True ($channelIndexText -match 'PassiveResults') 'Channel index must expose PassiveResults.'
Assert-True ($channelIndexText -match 'PassiveResultsByKey') 'Channel index must index Passive by key.'
Assert-True ($projectionStateText -match 'ExpressionChannelIndex') 'Combat projection state must carry channel index.'
Assert-True ($projectionBuilderText -match 'ExpressionChannelIndexBuilder') 'Combat projection builder must build channel index.'

Assert-True ($readerText -match 'GetExpressionResults') 'Reader must expose neutral expression result query.'
Assert-True ($readerText -match 'GetVerbResults') 'Reader must expose Verb result query.'
Assert-True ($readerText -match 'GetAbilityResults') 'Reader must expose Ability result query.'
Assert-True ($readerText -match 'GetHediffResults') 'Reader must expose Hediff result query.'
Assert-True ($readerText -match 'GetPassiveResults') 'Reader must expose Passive result query.'
Assert-True ($readerText -match 'HasPassiveKey') 'Reader must expose Passive key check.'
Assert-True ($readerText -match 'TryGetPassive') 'Reader must expose single Passive lookup.'
Assert-True ($serviceText -match 'ChannelIndex') 'ExpressionService must read from published channel index.'

Write-Output 'ExpressionFourChannelParallelSmokeTests PASS'
