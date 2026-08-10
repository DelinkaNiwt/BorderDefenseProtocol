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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$chipEntryConfigPath = Join-Path $bdpSourceRoot 'Expressions\Config\ChipExpressionEntryConfig.cs'
$comboEntryConfigPath = Join-Path $bdpSourceRoot 'Combos\Config\ComboExpressionEntryConfig.cs'
$contractInterpreterPath = Join-Path $bdpSourceRoot 'Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$validatorPath = Join-Path $bdpSourceRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$runtimeContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedAttackExecutionContext.cs'

$chipEntryConfigText = Get-Content -LiteralPath $chipEntryConfigPath -Raw -Encoding utf8
$comboEntryConfigText = Get-Content -LiteralPath $comboEntryConfigPath -Raw -Encoding utf8
$contractInterpreterText = Get-Content -LiteralPath $contractInterpreterPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$runtimeContextText = Get-Content -LiteralPath $runtimeContextPath -Raw -Encoding utf8

Assert-True (
    ($chipEntryConfigText -notmatch 'AttackExecutionStyle\s+ExecutionStyle') -and
    ($comboEntryConfigText -notmatch 'AttackExecutionStyle\s+ExecutionStyle')
) 'Authoring config must stop exposing ExecutionStyle as a direct input field.'

Assert-True (
    ($contractInterpreterText -notmatch 'config\.ExecutionStyle') -and
    ($validatorText -notmatch 'entry\.ExecutionStyle')
) 'Interpreter and validator must stop reading old ExecutionStyle author input.'

Assert-True (
    ($comboEntryConfigText -notmatch 'ExecutionStyle\s*=\s*ExecutionStyle')
) 'Main mod must stop forwarding removed ExecutionStyle config through combo entry mapping.'

Write-Output 'ExpressionAuthoringBoundarySmokeTests PASS'
