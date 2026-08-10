# 芯片组合记录与选择规则冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$modelRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Model"
$rulesPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationSelectionRules.cs"

$requiredModelFiles = @(
    "ChipCombinationRecord.cs",
    "IChipCombinationRecordHolder.cs",
    "ChipCombinationResolutionStatus.cs",
    "ChipCombinationFailureReason.cs",
    "ChipCombinationResolution.cs"
)
foreach ($fileName in $requiredModelFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $modelRoot $fileName)) "缺少组合模型：$fileName"
}
Assert-True (Test-Path -LiteralPath $rulesPath) "缺少芯片组合选择规则。"

$recordText = Get-Utf8Text (Join-Path $modelRoot "ChipCombinationRecord.cs")
$rulesText = Get-Utf8Text $rulesPath
foreach ($field in @("CategoryDefName", "ProfessionDefName", "OrderedActionPresetDefNames", "GunShellDefName", "LastResolvedLabel"))
{
    Assert-True ($recordText -match $field) "组合记录缺少字段：$field"
}
Assert-True ($recordText -notmatch 'Material|Ingredient|WorkAmount|Quantity|ResolvedConfig') "组合记录不得保存材料、工作量、数量或完整配置。"
Assert-True ($recordText -match 'SameConfigurationAs') "组合记录缺少顺序敏感配置比较。"
Assert-True ($recordText -match 'ExposeData') "组合记录必须可存档。"

Assert-True ($rulesText -match 'BDP_ChipProfession_Gunner') "选择规则必须识别枪手最多两个动作。"
Assert-True ($rulesText -match 'acceptedActionProfessions') "职业兼容必须来自 Def 单向接纳表。"
Assert-True ($rulesText -match 'Modes\.Count\s*>\s*1') "内置多形态必须由解释配置的形态数量判断。"
Assert-True ($rulesText -match 'MaxActionCount') "选择规则必须公开最大动作数。"
Assert-True ($rulesText -match 'Swap') "选择规则必须支持交换形态顺序。"
Assert-True ($rulesText -match 'RemoveAt') "取消形态一必须通过列表前移保留形态二。"
Assert-True ($rulesText -notmatch 'ChipTag') "职业选择规则不得重新依赖普通标签。"

Write-Host "PASS: 组合记录只保存玩家选择，职业、动作数、多形态和顺序规则集中定义。"
