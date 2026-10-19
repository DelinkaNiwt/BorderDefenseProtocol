$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$extensionPath = Join-Path $formation 'Defs\BlackTriggerFormationExtension.cs'
$adaptedAbilityPath = Join-Path $formation 'Defs\BlackTriggerAdaptedAbility.cs'
$abilitySnapshotPath = Join-Path $formation 'Model\BlackTriggerAbilitySnapshot.cs'
$makerSnapshotPath = Join-Path $formation 'Model\BlackTriggerMakerSnapshot.cs'
$factoryPath = Join-Path $formation 'Snapshot\BlackTriggerMakerSnapshotFactory.cs'
$calculatorPath = Join-Path $formation 'Matching\BlackTriggerAbilityWeightCalculator.cs'
$matcherPath = Join-Path $formation 'Matching\BlackTriggerFormationMatcher.cs'
$candidatePath = Join-Path $formation 'Model\BlackTriggerCandidatePlan.cs'
$validatorPath = Join-Path $formation 'Validation\BlackTriggerFormationDefValidator.cs'
$windowPath = Join-Path $formation 'Presentation\Window_BlackTriggerFormationDecision.cs'
$aboutPath = Join-Path $root 'About\About.xml'
$testDefPath = Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml'

foreach ($path in @($extensionPath, $adaptedAbilityPath, $abilitySnapshotPath, $makerSnapshotPath, $factoryPath, $calculatorPath, $matcherPath, $candidatePath, $validatorPath, $windowPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少能力权重实现文件：$path"
}

$extension = Get-Content -LiteralPath $extensionPath -Raw -Encoding utf8
$adaptedAbility = Get-Content -LiteralPath $adaptedAbilityPath -Raw -Encoding utf8
$abilitySnapshot = Get-Content -LiteralPath $abilitySnapshotPath -Raw -Encoding utf8
$makerSnapshot = Get-Content -LiteralPath $makerSnapshotPath -Raw -Encoding utf8
$factory = Get-Content -LiteralPath $factoryPath -Raw -Encoding utf8
$calculator = Get-Content -LiteralPath $calculatorPath -Raw -Encoding utf8
$matcher = Get-Content -LiteralPath $matcherPath -Raw -Encoding utf8
$candidate = Get-Content -LiteralPath $candidatePath -Raw -Encoding utf8
$validator = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$window = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$about = Get-Content -LiteralPath $aboutPath -Raw -Encoding utf8
$testDef = Get-Content -LiteralPath $testDefPath -Raw -Encoding utf8

Assert-True ($extension -match 'adaptedAbilities') '形成扩展必须声明适应能力列表。'
Assert-True ($adaptedAbility -match 'abilityDefName' -and $adaptedAbility -match 'sourcePackageId') '适应能力必须使用字符串 DefName 和可选来源包标识。'
Assert-True ($abilitySnapshot -match 'AbilityDefName' -and $abilitySnapshot -match 'StableKey') '来源能力快照必须保存稳定能力键。'
Assert-True ($makerSnapshot -match 'SourceAbilities') '来源人物快照必须保存能力列表。'
Assert-True ($factory -match 'SourceAbilities' -and $factory -match '\.abilities\?\.AllAbilitiesForReading') '快照工厂必须从人物实际能力冻结来源能力。'
Assert-True ($calculator -match 'GetNamedSilentFail' -and $calculator -match 'WeightPerMatch' -and $calculator -match 'matches\.Count\s*\*\s*WeightPerMatch') '能力权重必须按已加载 Def 解析并按每项加三。'
Assert-True ($matcher -match 'MatchedAbilityKeys' -and $matcher -match 'SourceAbilities') '匹配器必须把能力命中写入候选计划。'
Assert-True ($candidate -match 'MatchedAbilityKeys') '候选计划必须保存命中能力。'
Assert-True ($validator -match 'adaptedAbilities' -and $validator -match 'ResolveLoadedDef') '定义校验必须允许缺失能力并按已加载 Def 检查。'
Assert-True ($window -notmatch 'MatchedAbilityKeys|adaptedAbilities|SourceAbilities') '玩家决策窗口不得显示能力权重数据。'
Assert-True ($about -notmatch 'ludeon\.rimworld\.anomaly') 'About.xml 不得新增 Anomaly 硬依赖。'
Assert-True ($testDef -match '<adaptedAbilities>' -and $testDef -match 'PsychicSlaughter' -and $testDef -match 'sourcePackageId') '开发测试定义必须覆盖可选 Anomaly 能力声明。'

Write-Output 'BlackTriggerAbilityWeightSmokeTests PASS'
exit 0
