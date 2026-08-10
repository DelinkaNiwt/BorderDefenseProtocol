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

$chipTrionConfigPath = Join-Path $coreRoot 'Chips\Config\ChipTrionConfig.cs'
$triggerBindingPath = Join-Path $coreRoot 'Trigger\Runtime\TriggerTrionBindingService.cs'
$sourceTrionConfigPath = Join-Path $coreRoot 'Expressions\Config\ExpressionSourceTrionConfig.cs'
$runtimePayloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$publishedResultPath = Join-Path $coreRoot 'Expressions\Model\ExpressionPublishedResultSnapshot.cs'
$publishedBuilderPath = Join-Path $coreRoot 'Expressions\Access\Surfaces\ExpressionPublishedSnapshotBuilder.cs'
$sourceCollectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'
$comboExpressionConfigPath = Join-Path $coreRoot 'Combos\Config\ComboExpressionConfig.cs'
$comboExpressionFactoryPath = Join-Path $coreRoot 'Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'

$chipTrionConfigText = Get-Content -LiteralPath $chipTrionConfigPath -Raw -Encoding utf8
$triggerBindingText = Get-Content -LiteralPath $triggerBindingPath -Raw -Encoding utf8
$sourceTrionConfigText = Get-Content -LiteralPath $sourceTrionConfigPath -Raw -Encoding utf8
$runtimePayloadText = Get-Content -LiteralPath $runtimePayloadPath -Raw -Encoding utf8
$publishedResultText = Get-Content -LiteralPath $publishedResultPath -Raw -Encoding utf8
$publishedBuilderText = Get-Content -LiteralPath $publishedBuilderPath -Raw -Encoding utf8
$sourceCollectorText = Get-Content -LiteralPath $sourceCollectorPath -Raw -Encoding utf8
$comboExpressionConfigText = Get-Content -LiteralPath $comboExpressionConfigPath -Raw -Encoding utf8
$comboExpressionFactoryText = Get-Content -LiteralPath $comboExpressionFactoryPath -Raw -Encoding utf8

# 芯片本体激活费用是真实机制：必须继续由槽位激活服务一次性扣除。
Assert-True (
    $chipTrionConfigText -match 'public\s+float\s+ActivationCost\s*;'
) 'ChipTrionConfig must retain the chip-level ActivationCost field.'

Assert-True (
    ($triggerBindingText -match 'chipTrion\.ActivationCost') -and
    ($triggerBindingText -match 'TryConsume\(chipTrion\.ActivationCost\)')
) 'TriggerTrionBindingService must continue charging the chip-level ActivationCost.'

# 表达条目只保留实际使用费用与最低门槛，不得再声明第二个“激活费用”。
Assert-True (
    ($sourceTrionConfigText -notmatch '\bActivationCost\b') -and
    ($sourceTrionConfigText -match 'public\s+float\s+UseCost\s*;') -and
    ($sourceTrionConfigText -match 'public\s+float\s+MinimumRequired\s*;')
) 'ExpressionSourceTrionConfig must keep UseCost/MinimumRequired and remove ActivationCost.'

# 运行快照中允许芯片本体快照保留一份 ActivationCost，表达来源快照不得再保留第二份。
$runtimeActivationPropertyCount = [regex]::Matches(
    $runtimePayloadText,
    'public\s+float\s+ActivationCost\s*\{\s*get;\s*set;\s*\}'
).Count
Assert-True (
    $runtimeActivationPropertyCount -eq 1
) 'ExpressionRuntimePayload must carry ActivationCost only in the chip-level snapshot.'

Assert-True (
    ($publishedResultText -notmatch '\bTrionActivationCost\b') -and
    ($publishedBuilderText -notmatch '\bTrionActivationCost\b')
) 'Published expression results must stop exposing the unused expression ActivationCost.'

# 收集器允许复制一次芯片本体费用，不得再复制第二次表达来源费用。
$collectorActivationCopyCount = [regex]::Matches(
    $sourceCollectorText,
    'ActivationCost\s*=\s*trion\.ActivationCost'
).Count
Assert-True (
    $collectorActivationCopyCount -eq 1
) 'ExpressionSourceCollector must copy ActivationCost only for the chip-level snapshot.'

Assert-True (
    $comboExpressionConfigText -notmatch '\bActivationCostResolve\b'
) 'Combo expression Trion resolution must stop declaring ActivationCostResolve.'

Assert-True (
    ($comboExpressionFactoryText -notmatch '\bActivationCostResolve\b') -and
    ($comboExpressionFactoryText -notmatch '\bActivationCost\s*=')
) 'Combo expression result construction must stop resolving or forwarding expression ActivationCost.'

Write-Output 'ExpressionActivationCostOwnershipSmokeTests PASS'
