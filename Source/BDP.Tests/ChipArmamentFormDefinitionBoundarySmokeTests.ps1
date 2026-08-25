# 武装型定义与可见性边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$definitionRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Defs"
$defRoot = Join-Path $modRoot "1.6\Content\Defs\ChipArmamentFormDef"
$definitionPath = Join-Path $definitionRoot "ChipArmamentFormDef.cs"
$overridePath = Join-Path $definitionRoot "ChipArmamentFormOverrides.cs"
$validatorPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Validation\ChipManufacturingDefValidator.cs"
$presetPath = Join-Path $defRoot "Presets.xml"

Assert-True (Test-Path -LiteralPath $definitionPath) "缺少通用武装型定义。"
Assert-True (Test-Path -LiteralPath $overridePath) "缺少通用武装型特征修正定义。"
Assert-True (Test-Path -LiteralPath $validatorPath) "缺少武装型定义校验器。"
Assert-True (Test-Path -LiteralPath $presetPath) "缺少通用武装型 Def。"
Assert-True (-not (Test-Path -LiteralPath (Join-Path $definitionRoot "ChipGunShellDef.cs"))) "正式定义不得继续使用枪壳类型名。"
Assert-True (-not (Test-Path -LiteralPath (Join-Path $definitionRoot "ChipGunShellOverrides.cs"))) "正式修正块不得继续使用枪壳类型名。"

$definitionText = Get-Utf8Text $definitionPath
$overrideText = Get-Utf8Text $overridePath
$validatorText = Get-Utf8Text $validatorPath
$presetText = Get-Utf8Text $presetPath

Assert-True ($definitionText -match 'class\s+ChipArmamentFormDef\s*:\s*Def') "武装型必须是 Content Def。"
foreach ($field in @("compatibleProfessions", "compatibleActionPresetDefNames", "overrides", "projectileOverrides", "additionalCost", "additionalWorkAmount", "maxActionCount", "showInManufacturing", "includeInProductLabel", "implicitDefault"))
{
    Assert-True ($definitionText -match $field) "武装型缺少字段：$field"
}
Assert-True ($definitionText -match 'maxActionCount\s*=\s*1') "默认动作数量必须是 1。"
Assert-True ($overrideText -match 'hitCount|hitIntervalTicks|manualEntryIconTexPath') "统一特征修正缺少现有表达动作字段。"
Assert-True ($validatorText -match 'compatibleActionPresetDefNames') "定义校验必须检查动作适用范围。"
Assert-True ($validatorText -match 'ValidateImplicitDefaultConflicts') "定义校验必须检查隐式默认型冲突。"
Assert-True ($validatorText -match 'rangedModules|tools') "定义校验必须检查覆盖列表空项。"
Assert-True ($presetText -match 'ChipArmamentFormDef') "武装型 XML 必须使用通用定义类型。"
Assert-True ($presetText -match 'BDP_ArmamentForm_TrionCube') "射手远程武装必须有隐藏 Trion 立方体默认型。"
Assert-True ($presetText -match '<showInManufacturing>false</showInManufacturing>') "Trion 立方体默认型不得出现在制造台。"
Assert-True ($presetText -match '<includeInProductLabel>false</includeInProductLabel>') "Trion 立方体默认型不得进入芯片动态名称。"
Assert-True ($presetText -match 'BDP_ArmamentForm_Temporary') "必须提供临时攻击手武装型测试样本。"
Assert-True ($presetText -match '<label>Temporary</label>') "临时攻击手武装型必须使用稳定的英文 Def 标签。"
Assert-True ($presetText -match '<description>Temporary attacker visual probe\.</description>') "临时攻击手武装型必须有可翻译说明。"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\ChipArmamentFormDef\Presets.xml"
$languageText = Get-Utf8Text $languagePath
Assert-True ($languageText -match '<BDP_ArmamentForm_Temporary\.label>临时</BDP_ArmamentForm_Temporary\.label>') "临时攻击手武装型必须显示为临时。"
Assert-True ($presetText -match '<compatibleActionPresetDefNames>[\s\S]*BDP_Preset_Kogetsu') "临时武装型必须只允许弧月动作。"
Assert-True ($presetText -match 'BDP_Visual_Temporary_BreachAxe') "临时武装型必须绑定破墙斧贴图视觉。"

Write-Host "PASS: 通用武装型定义、默认动作数量和隐藏 Trion 默认型边界存在。"
