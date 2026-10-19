$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$projectorPath = Join-Path $contentRoot 'Information\Combos\ComboChipInformationReferenceProjector.cs'
$modelBuilderPath = Join-Path $contentRoot 'Information\ManufacturedChipInformationModelBuilder.cs'
$detailBuilderPath = Join-Path $contentRoot 'Assembly\Window\ChipAssemblyDetailModelBuilder.cs'
$windowPath = Join-Path $contentRoot 'Assembly\Window\Window_TriggerAssembly.cs'
$parserPath = Join-Path $sourceRoot 'BDP\Core\Information\ChipInformationParser.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Read-Source([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8
}

$projectorText = Read-Source $projectorPath
$modelBuilderText = Read-Source $modelBuilderPath
$detailBuilderText = Read-Source $detailBuilderPath
$windowText = Read-Source $windowPath
$parserText = Read-Source $parserPath

Assert-True ($projectorText -match 'ComboDef') 'Content 投影器必须读取正式组合技定义。'
Assert-True ($projectorText -match 'DefDatabase<ComboDef>\.AllDefsListForReading') '组合引用必须覆盖当前加载的全部 ComboDef，包括其它模组追加的定义。'
Assert-True ($projectorText -match 'firstSourceKey') '静态关联必须从第一来源功能模板反向查找。'
Assert-True ($projectorText -match 'secondSourceKey') '静态关联必须从第二来源功能模板反向查找。'
Assert-True ($projectorText -match 'StringComparison\.OrdinalIgnoreCase') '静态来源键匹配必须与正式组合匹配的大小写规则一致。'
Assert-True ($projectorText -match 'FirstSourceScope') '第一来源限定范围必须决定静态引用归属上下文。'
Assert-True ($projectorText -match 'SecondSourceScope') '第二来源限定范围必须决定静态引用归属上下文。'
Assert-True ($projectorText -match 'ChipInformationExtensionReference') '组合技应贡献中性引用，而非芯片功能语义。'
Assert-True ($projectorText -match 'DefinitionReferenceKindKey = "definition"' -and $projectorText -match 'RuntimeReferenceKindKey = "runtime-result"') '静态定义引用与正式运行结果引用必须保持不同类型。'
Assert-True ($projectorText -match 'ExpressionPublishedProjectionSnapshot') '运行时关联必须来自只读已发布表达快照。'
Assert-True ($projectorText -match 'ComboDefName' -and $projectorText -match 'ResultId') '运行时引用必须保留正式结果身份和 ComboDef 身份。'
Assert-True ($projectorText -match 'SourceResultIds' -and $projectorText -match 'ChipThingId') '运行时组合引用必须验证组合结果确实包含目标芯片。'
Assert-True ($projectorText -notmatch 'ResolvePublishedProjection\s*\(') '纯投影器不能自行取得或重建运行时快照。'
Assert-True ($modelBuilderText -match 'ComboChipInformationReferenceProjector') '静态芯片信息构建必须接入组合技定义贡献。'
Assert-True ($modelBuilderText -match 'ExtensionReferences') '组合技引用必须经统一请求入口注入。'
Assert-True ($detailBuilderText -match 'ExpressionPublishedProjectionSnapshot') '芯片详情构建入口必须能够接收可选的已发布运行时快照。'
Assert-True ($detailBuilderText -match 'chip\.ThingID') '运行时结果归属必须使用当前选中芯片实例身份。'
Assert-True ($detailBuilderText -match 'ChipInstanceSurfaceAccess\.ReadSourceReference\(chip\)' -and $detailBuilderText -match 'OrderedSourceKeys\[0\]') '通用实例分支必须从中性来源快照的首来源键取得静态组合关联身份。'
Assert-True ($modelBuilderText -match 'resolution\.FunctionDefs\[0\]') '成品组合必须使用制造记录中第一个功能来源作为正式组合身份。'
Assert-True ($projectorText -match '(?s)!string\.IsNullOrWhiteSpace\(activeStanceKey\)\s*&&\s*string\.IsNullOrWhiteSpace\(activeModeKey\)' -and $projectorText -match 'ResolveActiveContextKey\(activeModeKey, activeStanceKey\)') '没有形态键的单形态芯片仍须允许根级运行时引用，同时拒绝无父级的姿态引用。'
Assert-True ($parserText -match 'StringComparer\.OrdinalIgnoreCase') '扩展引用上下文键应与组合来源范围的大小写不敏感匹配一致。'
Assert-True ($windowText -match 'ExpressionSurfaceAccess\.ResolvePublishedProjection\(pawn\)') '触发器装配台只能从现有只读公开面读取当前快照。'
Assert-True ($windowText -match 'selectedChip,') '触发器装配详情必须把当前选中芯片交给统一模型构建器。'

Write-Output 'ComboChipInformationReferenceProjection PASS'
