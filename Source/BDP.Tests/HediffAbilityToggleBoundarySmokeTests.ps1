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
$presetPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$abilityPath = Join-Path $repoRoot '1.6\Content\Defs\AbilityDef\Chameleon.xml'
$hediffSyncPath = Join-Path $coreRoot 'Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs'
$drainPath = Join-Path $coreRoot 'Expressions\Runtime\ExpressionSustainDrainService.cs'
$togglePropertiesPath = Join-Path $coreRoot 'Abilities\CompProperties_AbilityEffect_ToggleExpressionHediff.cs'
$toggleEffectPath = Join-Path $coreRoot 'Abilities\CompAbilityEffect_ToggleExpressionHediff.cs'

foreach ($path in @(
    $presetPath,
    $abilityPath,
    $hediffSyncPath,
    $drainPath,
    $togglePropertiesPath,
    $toggleEffectPath
)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少通用 Hediff 按钮控制实现文件：$path"
}

$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8
$abilityText = Get-Content -LiteralPath $abilityPath -Raw -Encoding utf8
$hediffSyncText = Get-Content -LiteralPath $hediffSyncPath -Raw -Encoding utf8
$drainText = Get-Content -LiteralPath $drainPath -Raw -Encoding utf8
$togglePropertiesText = Get-Content -LiteralPath $togglePropertiesPath -Raw -Encoding utf8
$toggleEffectText = Get-Content -LiteralPath $toggleEffectPath -Raw -Encoding utf8

$chameleonMatch = [regex]::Match(
    $presetText,
    '(?s)<BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>\s*<defName>BDP_ChipFunction_Chameleon</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
Assert-True $chameleonMatch.Success '变色龙正式芯片功能定义不存在。'
$chameleon = $chameleonMatch.Value

Assert-True ($chameleon -match '<Category>BDP_ChipCategory_GeneralSkill</Category>') `
    '变色龙必须归入通用技能类。'
Assert-True ($chameleon -match '<EntryType>Hediff</EntryType>') `
    '变色龙必须保留 Hediff 输出通道。'
Assert-True ($chameleon -match '<HediffControlMode>AbilityButton</HediffControlMode>') `
    '变色龙 Hediff 必须声明能力按钮控制模式。'
Assert-True ($chameleon -match '<EntryType>Ability</EntryType>') `
    '变色龙必须声明能力按钮表达条目。'
Assert-True ($chameleon -match '<AbilityDefName>BDP_Ability_Chameleon</AbilityDefName>') `
    '变色龙必须绑定通用能力按钮宿主。'

Assert-True ($abilityText -match '<defName>BDP_Ability_Chameleon</defName>') `
    '变色龙能力定义不存在。'
Assert-True ($abilityText -match 'BDP\.Core\.Abilities\.CompProperties_AbilityEffect_ApplyExpressionHediff') `
    '变色龙能力必须使用 Core 通用一次性 Hediff 应用组件。'
Assert-True ($abilityText -match '<hediffDefName>BDP_Hediff_Chameleon</hediffDefName>') `
    '变色龙能力必须通过配置指定目标 Hediff，而不是在 Core 中硬编码。'

Assert-True ($togglePropertiesText -match 'class CompProperties_AbilityEffect_ToggleExpressionHediff') `
    'Core 必须提供通用 Hediff 切换组件配置。'
Assert-True ($togglePropertiesText -match 'hediffDefName') `
    '通用 Hediff 切换组件必须从 AbilityDef 配置目标 Hediff。'
Assert-True ($toggleEffectText -match 'GetFirstHediffOfDef') `
    '通用 Hediff 切换组件必须按实际宿主存在状态执行切换。'
Assert-True ($toggleEffectText -match 'BdpExpressionHostHediff') `
    '通用 Hediff 切换组件必须拒绝非表达宿主 Hediff。'

Assert-True ($hediffSyncText -match 'HediffControlMode') `
    'Hediff 同步器必须识别 Hediff 控制模式。'
Assert-True ($hediffSyncText -match 'AbilityButton') `
    'Hediff 同步器必须保留 AbilityButton 分支。'
Assert-True ($drainText -match 'ExpressionOutputChannel\.Hediff') `
    '持续消耗必须对 Hediff 输出通道执行实际宿主检查。'
Assert-True ($drainText -match 'GetFirstHediffOfDef') `
    '持续消耗必须读取实际 Hediff，而不是只读表达式可用状态。'

Write-Output 'HediffAbilityToggleBoundarySmokeTests PASS'
