$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$extensionPath = Join-Path $formation 'Defs\BlackTriggerFormationExtension.cs'
$adaptedTraitPath = Join-Path $formation 'Defs\BlackTriggerAdaptedTrait.cs'
$traitSnapshotPath = Join-Path $formation 'Model\BlackTriggerTraitSnapshot.cs'
$makerSnapshotPath = Join-Path $formation 'Model\BlackTriggerMakerSnapshot.cs'
$factoryPath = Join-Path $formation 'Snapshot\BlackTriggerMakerSnapshotFactory.cs'
$calculatorPath = Join-Path $formation 'Matching\BlackTriggerTraitWeightCalculator.cs'
$matcherPath = Join-Path $formation 'Matching\BlackTriggerFormationMatcher.cs'
$candidatePath = Join-Path $formation 'Model\BlackTriggerCandidatePlan.cs'
$randomPath = Join-Path $formation 'Matching\BlackTriggerStableRandom.cs'
$finalizationPath = Join-Path $formation 'Transactions\BlackTriggerFormationFinalizationService.cs'
$validatorPath = Join-Path $formation 'Validation\BlackTriggerFormationDefValidator.cs'
$windowPath = Join-Path $formation 'Presentation\Window_BlackTriggerFormationDecision.cs'
$testDefPath = Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml'

foreach ($path in @($extensionPath, $adaptedTraitPath, $traitSnapshotPath, $makerSnapshotPath, $factoryPath, $calculatorPath, $matcherPath, $candidatePath, $randomPath, $finalizationPath, $validatorPath, $windowPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少特性权重实现文件：$path"
}

$extension = Get-Content -LiteralPath $extensionPath -Raw -Encoding utf8
$adaptedTrait = Get-Content -LiteralPath $adaptedTraitPath -Raw -Encoding utf8
$traitSnapshot = Get-Content -LiteralPath $traitSnapshotPath -Raw -Encoding utf8
$makerSnapshot = Get-Content -LiteralPath $makerSnapshotPath -Raw -Encoding utf8
$factory = Get-Content -LiteralPath $factoryPath -Raw -Encoding utf8
$calculator = Get-Content -LiteralPath $calculatorPath -Raw -Encoding utf8
$matcher = Get-Content -LiteralPath $matcherPath -Raw -Encoding utf8
$candidate = Get-Content -LiteralPath $candidatePath -Raw -Encoding utf8
$random = Get-Content -LiteralPath $randomPath -Raw -Encoding utf8
$finalization = Get-Content -LiteralPath $finalizationPath -Raw -Encoding utf8
$validator = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$window = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$testDef = Get-Content -LiteralPath $testDefPath -Raw -Encoding utf8

Assert-True ($extension -match 'adaptedTraits') '形成扩展必须声明适应特性列表。'
Assert-True ($adaptedTrait -match 'TraitDef\s+trait' -and $adaptedTrait -match 'degree') '适应特性必须支持 TraitDef 与可选 degree。'
Assert-True ($traitSnapshot -match 'WasSuppressed' -and $traitSnapshot -match 'StableKey') '来源特性快照必须保存压制状态和稳定键。'
Assert-True ($makerSnapshot -match 'SourceTraits') '来源人物快照必须保存完整自身特性。'
Assert-True ($factory -match 'Trait_BlackTriggerInherited' -and $factory -match 'SourceTraits') '快照工厂必须排除临时继承特性并冻结来源特性。'
Assert-True ($calculator -match 'WasSuppressed' -and $calculator -match 'matches\.Count\s*\*\s*WeightPerMatch' -and $calculator -match 'WeightPerMatch\s*=\s*3') '特性权重计算必须忽略压制项并按每项加三。'
Assert-True ($matcher -match 'MatchedTraitKeys' -and $matcher -match 'FormationWeight') '匹配器必须把特性权重写入候选计划。'
Assert-True ($candidate -match 'FormationWeight' -and $candidate -match 'MatchedTraitKeys') '候选计划必须保存最终权重和命中特性。'
Assert-True ($random -match 'PickWeighted' -and $random -match 'FormationWeight') '稳定随机工具必须提供按权重抽取。'
Assert-True ($finalization -match 'PickWeighted') '最终化必须消费稳定加权抽取。'
Assert-True ($validator -match 'adaptedTraits' -and $validator -match 'DataAtDegree') '定义校验必须检查适应特性与 degree。'
Assert-True ($window -notmatch 'FormationWeight|MatchedTraitKeys|adaptedTraits') '玩家决策窗口不得显示特性权重数据。'
Assert-True ($testDef -match '<adaptedTraits>' -and $testDef -match '<trait>Psychopath</trait>' -and $testDef -match '<degree>') '开发测试黑触发器必须覆盖特性与 degree 声明。'

Write-Output 'BlackTriggerTraitWeightSmokeTests PASS'
exit 0
