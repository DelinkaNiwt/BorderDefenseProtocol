$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$formalResultPath = Join-Path $coreRoot 'Expressions\Model\FormalExpressionResult.cs'
$comboFactoryPath = Join-Path $coreRoot 'Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$adapterPath = Join-Path $coreRoot 'Information\Features\Projection\ComboFeatureInformationSnapshotBuilder.cs'
$presentationBuilderPath = Join-Path $coreRoot 'Trigger\Projection\TriggerPresentationBuilder.cs'
$presentationStatePath = Join-Path $coreRoot 'Trigger\Runtime\TriggerPresentationState.cs'
$surfaceAccessPath = Join-Path $coreRoot 'Expressions\Access\Surfaces\ExpressionSurfaceAccess.cs'
$publishedBuilderPath = Join-Path $coreRoot 'Expressions\Access\Surfaces\ExpressionPublishedSnapshotBuilder.cs'
$publishedResultPath = Join-Path $coreRoot 'Expressions\Model\ExpressionPublishedResultSnapshot.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Read-Source([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8
}

$formalResult = Read-Source $formalResultPath
$comboFactory = Read-Source $comboFactoryPath
$adapter = Read-Source $adapterPath
$presentationBuilder = Read-Source $presentationBuilderPath
$presentationState = Read-Source $presentationStatePath
$surfaceAccess = Read-Source $surfaceAccessPath
$publishedBuilder = Read-Source $publishedBuilderPath
$publishedResult = Read-Source $publishedResultPath

Assert-True ($formalResult -match 'string SourceEntryId') '正式组合结果必须保留其来源表达条目 Id，不能从生成后的结果 Id 反向猜测。'
Assert-True ($comboFactory -match 'SourceEntryId\s*=\s*entry\.Id') '组合结果工厂必须从已求值条目复制稳定来源 Id。'
Assert-True ($adapter -match 'ComboDef' -and $adapter -match 'FormalExpressionResult') '组合功能投影必须连接组合 Def 与已成立的正式结果。'
Assert-True ($adapter -notmatch 'ComboExpressionEntryConfig\s+entry\s*[,=)]') '组合功能投影不得从作者原始 ComboExpressionEntryConfig 重建最终结果。'
Assert-True ($adapter -match 'comboDef\.Features' -and $adapter -match 'new ChipFeatureResolver\(\)') '组合定义自己的 Features 必须交给统一功能解析器。'
Assert-True ($adapter -notmatch 'BDP_Feature_|BDP_FeatureGroup_') 'Core 组合适配器不得按具体功能语义或信息组名称分支。'
Assert-True ($adapter -match 'new\[\]\s*\{\s*formalResult\.SourceEntryId\s*\}') '每条组合结果只解析与其正式来源条目对应的声明。'
Assert-True ($adapter -match 'result\.VerbProps' -and $adapter -match 'result\.ResolvedVerbSpec' -and $adapter -match 'result\.Tool' -and $adapter -match 'result\.ExecutionStyle' -and $adapter -match 'result\.Trion' -and $adapter -match 'result\.RangedModules') '适配器必须从正式结果投影各来源 Worker 所需的最终字段。'
Assert-True ($presentationBuilder -match 'ComboFeatureInformationSnapshotBuilder') '组合功能树必须在正式表现发布构建阶段预先解析。'
Assert-True ($presentationState -match 'ComboFeatureInformations') '发布状态必须保存按结果 Id 索引的组合功能信息。'
Assert-True ($surfaceAccess -match 'PublishedPresentationProjection' -and $surfaceAccess -match 'ExpressionPublishedSnapshotBuilder\.Build') '公开结果读取必须复用已发布表现状态，不能在读取时重新计算组合。'
Assert-True ($surfaceAccess -match 'ProjectionVersion\s*==\s*projection\.ProjectionVersion') '只有和当前正式战斗投影同版本的组合功能结果才能对外读取。'
Assert-True ($surfaceAccess -notmatch 'TriggerPresentationBuilder|ComboFeatureInformationSnapshotBuilder|ChipFeatureResolver') '公开读取口不得重建或重新解析组合功能树。'
Assert-True ($publishedBuilder -match 'FeatureInformation' -and $publishedBuilder -match 'ComboFeatureInformations') '公开结果快照必须接上对应结果的功能树。'
Assert-True ($publishedResult -match 'ChipFeatureResolutionResult\s+FeatureInformation') '公开组合结果必须单独暴露中性功能树结果，而不是把组合引用混进芯片功能树。'

Write-Output 'ChipFeatureComboInformationAdapter PASS'
