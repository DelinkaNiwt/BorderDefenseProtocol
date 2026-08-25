# Content 芯片制造定义模型冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$definitionRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Defs"
$classificationPath = Join-Path $modRoot "1.6\Content\Defs\ChipDef\Classification.xml"
$actionPresetsPath = Join-Path $modRoot "1.6\Content\Defs\ChipActionPresetDef\Presets.xml"
$armamentFormsPath = Join-Path $modRoot "1.6\Content\Defs\ChipArmamentFormDef\Presets.xml"

$classificationText = Get-Utf8Text $classificationPath
$actionText = Get-Utf8Text $actionPresetsPath
$armamentFormText = Get-Utf8Text $armamentFormsPath

foreach ($category in @("Weapon", "Defense", "Ability", "Status", "Passive"))
{
    Assert-True ($classificationText -match "BDP_ChipCategory_$category") "缺少主分类：$category"
}
foreach ($profession in @("Attacker", "Shooter", "Gunner", "Sniper"))
{
    Assert-True ($classificationText -match "BDP_ChipProfession_$profession") "缺少职业：$profession"
}
Assert-True ($classificationText -notmatch 'BDP_ChipTag_(AttackerUse|Shooter|Gunner|Sniper)') "职业不得继续作为普通标签。"
Assert-True ($classificationText -match 'BDP_ChipProfession_Gunner[\s\S]*BDP_ChipProfession_Shooter') "枪手必须单向接纳射手动作。"

$requiredFiles = @("ChipProfessionDef.cs", "ChipActionPresetDef.cs", "ChipArmamentFormDef.cs", "ChipArmamentFormOverrides.cs")
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $definitionRoot $fileName)) "缺少 Content 制造定义：$fileName"
}

$definitionText = ($requiredFiles | ForEach-Object { Get-Utf8Text (Join-Path $definitionRoot $_) }) -join "`n"
Assert-True ($definitionText -match 'ChipProfessionDef\s+profession') "动作预设缺少唯一职业字段。"
Assert-True ($definitionText -match 'List<ThingDefCountClass>\s+costList') "动作预设缺少材料字段。"
Assert-True ($definitionText -match 'float\s+additionalWorkAmount') "动作预设缺少附加工作量字段。"
Assert-True ($definitionText -match 'List<ThingDefCountClass>\s+additionalCost') "武装型缺少附加材料字段。"
Assert-True ($definitionText -match 'List<ChipProfessionDef>\s+compatibleProfessions') "武装型缺少职业兼容字段。"
Assert-True ($definitionText -notmatch 'additionalWorkTicks|fixedChipPreset|compatibleTags') "新定义不得保留旧工时、固定预设或标签兼容字段。"

Assert-True ($actionText -match 'BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipActionPresetDef') "动作 XML 必须使用 Content 定义类型。"
Assert-True ($actionText -notmatch 'BDP\.Core\.Chips\.ChipPresetDef|additionalWorkTicks') "动作 XML 不得继续使用 Core 旧定义。"
Assert-True ($armamentFormText -match 'BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipArmamentFormDef') "武装型 XML 必须使用 Content 定义类型。"
Assert-True ($armamentFormText -notmatch 'BDP\.Core\.Chips\.GunClassDef|compatibleTags|additionalWorkTicks') "武装型 XML 不得继续使用 Core 旧定义字段。"

Write-Host "PASS: Content 定义了五主分类、唯一职业、动作材料/工作量和武装型兼容模型。"
