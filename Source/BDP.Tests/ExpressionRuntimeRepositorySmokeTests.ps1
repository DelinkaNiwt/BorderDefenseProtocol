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

$runtimeRepositoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ExpressionRuntimeRepository.cs'
$comboRuntimeIndexPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs'
$chipDefinitionCachePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ChipDefinitionCache.cs'
$expressionContractCachePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ExpressionContractCache.cs'
$expressionSurfaceAccessPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionSurfaceAccess.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$triggerRuntimeServicesPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeServices.cs'
$chipSurfaceAccessPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Access\ChipSurfaceAccess.cs'
$chipDefinitionReaderPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Access\ChipDefinitionReaderSurface.cs'
$contractInterpreterPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$comboSurfaceAccessPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboSurfaceAccess.cs'
$comboReaderSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboDefinitionReaderSurface.cs'

$runtimeRepositoryText = if (Test-Path -LiteralPath $runtimeRepositoryPath) { Get-Content -LiteralPath $runtimeRepositoryPath -Raw -Encoding utf8 } else { '' }
$comboRuntimeIndexText = if (Test-Path -LiteralPath $comboRuntimeIndexPath) { Get-Content -LiteralPath $comboRuntimeIndexPath -Raw -Encoding utf8 } else { '' }
$chipDefinitionCacheText = if (Test-Path -LiteralPath $chipDefinitionCachePath) { Get-Content -LiteralPath $chipDefinitionCachePath -Raw -Encoding utf8 } else { '' }
$expressionContractCacheText = if (Test-Path -LiteralPath $expressionContractCachePath) { Get-Content -LiteralPath $expressionContractCachePath -Raw -Encoding utf8 } else { '' }
$expressionSurfaceAccessText = Get-Content -LiteralPath $expressionSurfaceAccessPath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$triggerRuntimeServicesText = if (Test-Path -LiteralPath $triggerRuntimeServicesPath) { Get-Content -LiteralPath $triggerRuntimeServicesPath -Raw -Encoding utf8 } else { '' }
$chipSurfaceAccessText = Get-Content -LiteralPath $chipSurfaceAccessPath -Raw -Encoding utf8
$chipDefinitionReaderText = Get-Content -LiteralPath $chipDefinitionReaderPath -Raw -Encoding utf8
$contractInterpreterText = Get-Content -LiteralPath $contractInterpreterPath -Raw -Encoding utf8
$comboSurfaceAccessText = Get-Content -LiteralPath $comboSurfaceAccessPath -Raw -Encoding utf8
$comboReaderSurfaceText = Get-Content -LiteralPath $comboReaderSurfacePath -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $runtimeRepositoryPath
) 'Task 3 must introduce ExpressionRuntimeRepository as the unified holder for static expression runtime dependencies.'

Assert-True (
    Test-Path -LiteralPath $comboRuntimeIndexPath
) 'Task 3 must introduce ComboRuntimeIndex to index unordered chip pairs.'

Assert-True (
    Test-Path -LiteralPath $chipDefinitionCachePath
) 'Task 3 must introduce ChipDefinitionCache to cache chip definition reads by ThingDef.'

Assert-True (
    Test-Path -LiteralPath $expressionContractCachePath
) 'Task 3 must introduce ExpressionContractCache to cache interpreted expression contracts by chip definition and mode.'

Assert-True (
    Test-Path -LiteralPath $triggerRuntimeServicesPath
) 'Task 3 must introduce TriggerRuntimeServices as the owner-held runtime root.'

Assert-True (
    ($runtimeRepositoryText -match 'sealed class ExpressionRuntimeRepository') -and
    ($runtimeRepositoryText -match 'ComboRuntimeIndex') -and
    ($runtimeRepositoryText -match 'ChipDefinitionCache') -and
    ($runtimeRepositoryText -match 'ExpressionContractCache') -and
    ($runtimeRepositoryText -match 'ExpressionSnapshotBuilder')
) 'ExpressionRuntimeRepository must hold combo index, chip definition cache, expression contract cache, and the reused snapshot builder chain.'

Assert-True (
    ($comboRuntimeIndexText -match 'sealed class ComboRuntimeIndex') -and
    ($comboRuntimeIndexText -match 'FindMatch\(') -and
    ($comboRuntimeIndexText -match 'BuildUnorderedPairKey')
) 'ComboRuntimeIndex must expose unordered-pair combo lookup.'

Assert-True (
    ($chipDefinitionCacheText -match 'sealed class ChipDefinitionCache') -and
    ($chipDefinitionCacheText -match 'ThingDef') -and
    ($chipDefinitionCacheText -match 'GetOrAdd')
) 'ChipDefinitionCache must cache definition reads by ThingDef.'

Assert-True (
    ($expressionContractCacheText -match 'sealed class ExpressionContractCache') -and
    ($expressionContractCacheText -match 'ThingDef') -and
    ($expressionContractCacheText -match 'modeKey') -and
    ($expressionContractCacheText -match 'GetOrAdd')
) 'ExpressionContractCache must cache contract interpretation by chip definition and mode key.'

Assert-True (
    ($triggerRuntimeServicesText -match 'ExpressionRuntimeRepository') -and
    ($triggerRuntimeServicesText -match 'ExpressionService') -and
    ($triggerRuntimeServicesText -match 'AttackExecutionService') -and
    ($triggerRuntimeServicesText -match 'RangedAttackProtocolService') -and
    ($triggerRuntimeServicesText -match 'RangedAttackTrionGate')
) 'TriggerRuntimeServices must hold expression and attack runtime services under one Trigger owner.'

Assert-True (
    ($expressionSurfaceAccessText -match 'ResolveRuntimeRepository\(Pawn pawn\)') -and
    ($expressionSurfaceAccessText -match 'triggerBody != null \? triggerBody\.RuntimeServices\?\.ExpressionRuntimeRepository : null')
) 'ExpressionSurfaceAccess must resolve ExpressionRuntimeRepository from the current Trigger owner runtime root.'

Assert-True (
    ($chipSurfaceAccessText -match 'ChipDefinitionCache') -and
    ($chipDefinitionReaderText -match 'ChipDefinitionCache') -and
    ($chipDefinitionReaderText -match 'GetOrAdd')
) 'Chip definition reads must be routed through ChipDefinitionCache instead of rebuilding every time.'

Assert-True (
    ($comboSurfaceAccessText -match 'ComboRuntimeIndex') -and
    ($comboReaderSurfaceText -match 'ComboRuntimeIndex') -and
    ($comboReaderSurfaceText -match 'FindMatch\(') -and
    ($comboReaderSurfaceText -notmatch 'for\s*\(\s*int\s+i\s*=\s*0;\s*i\s*<\s*DefDatabase<ComboDef>\.AllDefsListForReading\.Count')
) 'ComboDefinitionReaderSurface.FindMatch(...) must stop linearly scanning DefDatabase<ComboDef>.AllDefsListForReading.'

Assert-True (
    ($contractInterpreterText -match 'ExpressionContractCache') -and
    ($contractInterpreterText -match 'GetOrAdd')
) 'DefaultChipExpressionContractInterpreter must route contract interpretation through ExpressionContractCache.'

Assert-True (
    ($expressionSurfaceText -match 'runtimeRepository') -and
    ($expressionSurfaceText -match 'runtimeRepository\.SnapshotBuilder') -and
    ($expressionSurfaceText -notmatch 'new DefaultChipExpressionContractInterpreter\(\)') -and
    ($expressionSurfaceText -notmatch 'new DefaultExpressionSourceDeclarationProvider') -and
    ($expressionSurfaceText -notmatch 'new ExpressionSnapshotBuilder')
) 'ExpressionService must reuse the shared runtime repository instead of rebuilding static dependencies on each snapshot build.'

Write-Output 'ExpressionRuntimeRepositorySmokeTests PASS'
