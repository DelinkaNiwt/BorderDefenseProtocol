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

function Read-RequiredText {
    param([string]$Path, [string]$Message)

    Assert-True (Test-Path -LiteralPath $Path) $Message
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Find-Def {
    param([xml]$Document, [string]$DefName)

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$moduleRoot = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\ExtraDamage'
$modulePath = Join-Path $moduleRoot 'RangedExtraDamageModule.cs'
$configPath = Join-Path $moduleRoot 'RangedExtraDamageConfig.cs'
$defPath = Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\ExtraDamage.xml'
$presetPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'

$moduleText = Read-RequiredText $modulePath '缺少通用远程额外伤害模块。'
$configText = Read-RequiredText $configPath '缺少通用远程额外伤害配置。'
$defText = Read-RequiredText $defPath '缺少通用远程额外伤害模块 Def。'
$presetText = Read-RequiredText $presetPath '缺少芯片功能预设文件。'

Assert-True ($moduleText -match 'class\s+RangedExtraDamageModule') '通用模块类型名称不正确。'
Assert-True ($moduleText -match 'IRangedAttackModuleRuntime') '通用模块必须实现远程模块运行时接口。'
Assert-True ($moduleText -match 'IImpactStageModule') '通用模块必须接入 Impact 阶段。'
Assert-True ($moduleText -match 'ExtraDamagesToAppend') '通用模块必须提交额外伤害计划，而不是直接调用 TakeDamage。'
Assert-True ($moduleText -notmatch 'TakeDamage\s*\(') '通用模块不得直接调用 TakeDamage。'
Assert-True ($moduleText -match 'GetNotMissingParts') '身体部件百分比模式必须排除缺失部件。'
Assert-True ($moduleText -match 'GetMaxHealth\s*\(') '身体部件百分比模式必须使用部件最大血量。'
Assert-True ($moduleText -match 'HitPartMaxHealthPercent') '通用模块必须支持命中部件最大血量百分比模式。'
Assert-True ($moduleText -match 'IDamageAmountResolver') '命中部件百分比模式必须使用 Core 中性伤害量解析器。'
Assert-True ($moduleText -match 'HitPart\.def\.GetMaxHealth') '命中部件百分比模式必须按实际命中部件最大血量计算。'
Assert-True ($moduleText -notmatch 'VitalHitModule|VitalHitConfig|HitPartPolicyResolver') '通用模块不得引用要害命中业务。'
Assert-True ($configText -match 'BodyPartMaxHealthSumPercent') '配置必须支持身体部件最大血量总和百分比模式。'
Assert-True ($configText -match 'HitPartMaxHealthPercent') '配置必须支持命中部件最大血量百分比模式。'
Assert-True ($configText -match 'HitPartResolutionMode') '配置必须暴露中性命中部位解析策略。'
Assert-True ($defText -match 'RangedExtraDamageModule') '模块 Def 必须指向通用运行时类型。'
Assert-True ($defText -match '<defName>BDP_RangedExtraDamageModule</defName>') '模块 Def 必须使用通用中性 DefName。'
Assert-True ($defText -match '<AmountMode>Fixed</AmountMode>') '模块 Def 默认配置必须保持中性。'
Assert-True ($defText -notmatch 'BDP_RangedExtraDamage_BodyPartMaxHealthPercent|BDP_RangedExtraDamage_HitPartMaxHealthPercent') '模块 Def 不得携带风刃特制预设名称。'
Assert-True ($defText -notmatch '<DamageDef>Cut</DamageDef>|<Ratio>0\.10</Ratio>') '模块 Def 不得携带风刃专属伤害类型或比例。'
Assert-True ($presetText -match 'BDP_RangedExtraDamageModule') '风刃必须挂载通用额外伤害模块。'
Assert-True ($presetText -match 'RangedExtraDamageConfig') '风刃必须在自身挂载处提供额外伤害配置。'
Assert-True ($presetText -match '<AmountMode>HitPartMaxHealthPercent</AmountMode>') '风刃挂载配置必须选择命中部件百分比模式。'
Assert-True ($presetText -match '<Ratio>0\.10</Ratio>') '风刃挂载配置必须设置 10% 比例。'

Write-Output 'RangedExtraDamageModuleSmokeTests PASS'
