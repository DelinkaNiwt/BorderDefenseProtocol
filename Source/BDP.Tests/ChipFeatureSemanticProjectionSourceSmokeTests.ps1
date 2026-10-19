# 语义来源投影冒烟测试：各 Worker 只负责自身语义的正式信息。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sourceRoot = Join-Path $modRoot "Source\BDP.Content\Information\Features"
$configRoot = Join-Path $sourceRoot "Sources"
$workerRoot = Join-Path $sourceRoot "Workers"
$supportPath = Join-Path $workerRoot "ChipFeatureSourceWorkerSupport.cs"

$sourceNames = @(
    "AbilityLifecycleFeatureSourceConfig",
    "RangedAttackFeatureSourceConfig",
    "MeleeAttackMethodFeatureSourceConfig",
    "ShieldFeatureSourceConfig",
    "MovementMethodFeatureSourceConfig",
    "ExtraEffectFeatureSourceConfig"
)
$workerNames = @(
    "AbilityLifecycleFeatureSourceWorker",
    "RangedAttackFeatureSourceWorker",
    "MeleeAttackMethodFeatureSourceWorker",
    "ShieldFeatureSourceWorker",
    "MovementMethodFeatureSourceWorker",
    "ExtraEffectFeatureSourceWorker"
)

foreach ($name in $sourceNames) {
    $path = Join-Path $configRoot "$name.cs"
    Assert-True (Test-Path -LiteralPath $path) "缺少语义来源配置：$name"
}

foreach ($name in $workerNames) {
    $path = Join-Path $workerRoot "$name.cs"
    Assert-True (Test-Path -LiteralPath $path) "缺少语义来源 Worker：$name"
}

$sourcesText = (Get-ChildItem -LiteralPath $configRoot -Filter "*FeatureSourceConfig.cs" | ForEach-Object { Get-Utf8Text $_.FullName }) -join [Environment]::NewLine
$workersText = ($workerNames | ForEach-Object { Get-Utf8Text (Join-Path $workerRoot "$_.cs") }) -join [Environment]::NewLine
$supportText = Get-Utf8Text $supportPath

Assert-True ($sourcesText -match "ToolIndex") "重复近战方式必须能够稳定定位单个 Tool。"
Assert-True ($sourcesText -match "UseStackedValues") "护盾来源必须能区分单枚与聚合护盾状态。"
Assert-True ($supportText -match "EffectiveConfig") "来源必须优先读取制造后的有效配置。"
Assert-True ($supportText -match "FromNumber\(value\)" -and $supportText -match "FromDuration\(value\)") "合法零值必须可保留，缺值须独立处理。"
Assert-True ($workersText -notmatch "BDP_Property_UsageMode|BDP_Property_ProjectileDef|BDP_Property_AppliedHediff") "标准语义来源不得输出技术入口或 Def 身份属性。"
Assert-True ($workersText -notmatch "ChipInformationDetailRenderer|Window_|Gizmo") "语义来源不得依赖具体面板。"
Assert-True ($workersText -match "VanillaMeleeStatProjection.TryCreate") "近战来源必须复用原版有效伤害与冷却投影。"
Assert-True ($workersText -match "HediffCompProperties_EnergyShield") "BDP 护盾来源必须读取真实护盾业务参数，而非 Hediff 名称。"
Assert-True ($workersText -match "GetDamageAmount" -and $workersText -match "GetArmorPenetration") "远程来源必须通过原版投射物读取入口取值。"

Write-Host "PASS: 语义来源按职责输出，不依赖技术身份或界面。"
