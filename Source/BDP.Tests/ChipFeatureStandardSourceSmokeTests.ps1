# BDP 标准功能来源配置与 Worker 冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sourceRoot = Join-Path $modRoot "Source\BDP.Content\Information\Features"
$configRoot = Join-Path $sourceRoot "Sources"
$workerRoot = Join-Path $sourceRoot "Workers"

foreach ($fileName in @(
    "ExpressionEntryFeatureSourceConfig.cs",
    "AbilityMeleeAttackFeatureSourceConfig.cs",
    "AbilityLifecycleFeatureSourceConfig.cs",
    "RangedAttackFeatureSourceConfig.cs",
    "MeleeAttackMethodFeatureSourceConfig.cs",
    "ShieldFeatureSourceConfig.cs",
    "MovementMethodFeatureSourceConfig.cs",
    "ExtraEffectFeatureSourceConfig.cs"
)) {
    Assert-True (Test-Path -LiteralPath (Join-Path $configRoot $fileName)) `
        "缺少标准来源配置：$fileName"
}

foreach ($fileName in @(
    "AbilityMeleeAttackFeatureSourceWorker.cs",
    "AbilityLifecycleFeatureSourceWorker.cs",
    "RangedAttackFeatureSourceWorker.cs",
    "MeleeAttackMethodFeatureSourceWorker.cs",
    "ShieldFeatureSourceWorker.cs",
    "MovementMethodFeatureSourceWorker.cs",
    "ExtraEffectFeatureSourceWorker.cs"
)) {
    Assert-True (Test-Path -LiteralPath (Join-Path $workerRoot $fileName)) `
        "缺少标准来源 Worker：$fileName"
}

foreach ($fileName in @(
    "ExpressionEntryFeatureSourceWorker.cs",
    "ComboExpressionFeatureSourceConfig.cs",
    "ComboExpressionFeatureSourceWorker.cs",
    "VerbFeatureSourceConfig.cs",
    "VerbFeatureSourceWorker.cs",
    "MeleeToolFeatureSourceConfig.cs",
    "MeleeToolFeatureSourceWorker.cs",
    "AbilityFeatureSourceConfig.cs",
    "AbilityFeatureSourceWorker.cs",
    "HediffFeatureSourceConfig.cs",
    "HediffFeatureSourceWorker.cs",
    "PassiveFeatureSourceConfig.cs",
    "PassiveFeatureSourceWorker.cs",
    "RangedModuleFeatureSourceConfig.cs",
    "RangedModuleFeatureSourceWorker.cs"
)) {
    $directory = if ($fileName -match 'Config') { $configRoot } else { $workerRoot }
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $directory $fileName))) `
        "语义来源迁移后不应保留通用技术路线来源：$fileName"
}

$configText = (Get-ChildItem -LiteralPath $configRoot -Filter "*.cs" |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"
$workerText = (Get-ChildItem -LiteralPath $workerRoot -Filter "*.cs" |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"
$coreSourceText = Get-Content -LiteralPath (Join-Path $modRoot "Source\BDP\Core\Information\Features\Config\ChipFeatureSourceConfig.cs") -Raw -Encoding UTF8

Assert-True (($configText + $coreSourceText) -match "EntryId") "标准来源必须支持表达条目稳定键定位。"
Assert-True ($configText -match "typeof\(.*Worker\)") `
    "标准来源配置应提供默认 WorkerClass，普通 XML 不必重复内部注册。"
Assert-True ($workerText -match "IChipFeatureSourceWorker") "标准来源 Worker 必须实现 Core 契约。"
Assert-True ($workerText -match "ChipFeatureValueContribution") `
    "标准 Worker 必须返回类型化属性贡献。"
Assert-True ($workerText -match "DefDatabase<ChipFeaturePropertyDef>") `
    "标准 Worker 必须按属性 Def 语义输出，而不是返回无类型字典。"
Assert-True ($workerText -notmatch "ChipInformationDetailRenderer|Window_|Gizmo") `
    "标准来源 Worker 不得依赖 UI。"
Assert-True ($workerText -notmatch "CanProvide|Register\(") `
    "标准来源不得使用顺序探测或手工全局注册。"
Assert-True ($workerText -match "VanillaMeleeStatProjection.TryCreate") `
    "近战语义来源必须复用现有原版近战统计投影。"

Write-Host "PASS: BDP 标准功能来源配置和 Worker 覆盖当前实现路线。"
