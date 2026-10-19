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
foreach ($field in @("compatibleProfessions", "compatibleChipFunctionDefNames", "overrides", "projectileOverrides", "additionalCost", "additionalWorkAmount", "maxFunctionCount", "showInManufacturing", "includeInProductLabel", "implicitDefault"))
{
    Assert-True ($definitionText -match $field) "武装型缺少字段：$field"
}
foreach ($field in @("labelKey", "ResolvedLabel", "descriptionKey", "ResolvedDescription"))
{
    Assert-True ($definitionText -match $field) "武装型缺少本地化字段或解析：$field"
}
Assert-True ($definitionText -match 'maxFunctionCount\s*=\s*1') "默认功能数量必须是 1。"
Assert-True ($overrideText -match 'hitCount|hitIntervalTicks|manualEntryIconTexPath') "统一特征修正缺少现有表达条目字段。"
Assert-True ($validatorText -match 'compatibleChipFunctionDefNames') "定义校验必须检查功能适用范围。"
Assert-True ($validatorText -match 'ValidateImplicitDefaultConflicts') "定义校验必须检查隐式默认型冲突。"
Assert-True ($validatorText -match 'rangedModules|tools') "定义校验必须检查覆盖列表空项。"
Assert-True ($presetText -match 'ChipArmamentFormDef') "武装型 XML 必须使用通用定义类型。"
Assert-True ($presetText -match 'BDP_ArmamentForm_TrionCube') "射手远程武装必须有隐藏 Trion 立方体默认型。"
Assert-True ($presetText -match '<showInManufacturing>false</showInManufacturing>') "Trion 立方体默认型不得出现在制造台。"
Assert-True ($presetText -match '<includeInProductLabel>false</includeInProductLabel>') "Trion 立方体默认型不得进入芯片动态名称。"
$keyedPath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml"
Assert-True (Test-Path -LiteralPath $keyedPath) "武装型 Keyed 中文语言包必须存在。"
$keyedText = Get-Utf8Text $keyedPath
Assert-True (-not (Test-Path -LiteralPath (Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\ChipArmamentFormDef\Presets.xml"))) "武装型不得继续使用旧 DefInjected 语言包。"

Write-Host "PASS: 通用武装型定义、默认功能数量和隐藏 Trion 默认型边界存在。"
