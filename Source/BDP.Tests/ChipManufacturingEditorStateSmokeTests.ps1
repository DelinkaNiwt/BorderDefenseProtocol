# 芯片制造编辑会话与草稿恢复测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$uiRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI"
$requiredFiles = @(
    "ChipManufacturingEditorState.cs",
    "ChipManufacturingDraft.cs",
    "ChipManufacturingDraftKey.cs",
    "ChipManufacturingListModel.cs",
    "Window_ChipPresetInfo.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $uiRoot $fileName)) "缺少制造编辑组件：$fileName"
}

$stateText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingEditorState.cs")
$draftText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingDraft.cs")
$keyText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingDraftKey.cs")
$listText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingListModel.cs")

Assert-True ($stateText -match 'Dictionary<ChipManufacturingDraftKey,\s*ChipManufacturingDraft>') "不同分类/职业路径必须分别保存草稿。"
Assert-True ($stateText -match 'Clear') "关闭页签必须能清空整次编辑会话。"
Assert-True ($keyText -match 'CategoryDefName') "草稿键必须包含主分类。"
Assert-True ($keyText -match 'ProfessionDefName') "草稿键必须包含可空职业。"
Assert-True ($draftText -match 'ChipCombinationRecord') "草稿必须直接编辑统一组合记录。"
Assert-True ($draftText -match 'Quantity') "每条草稿路径必须独立保留制造数量。"
Assert-True ($draftText -match 'ChipCombinationSelectionRules\.TrySelect') "点击动作必须复用集中选择规则。"
Assert-True ($draftText -match 'ChipCombinationSelectionRules\.RemoveAt') "取消形态必须复用集中前移规则。"
Assert-True ($draftText -match 'ChipCombinationSelectionRules\.Swap') "交换形态必须复用集中顺序规则。"
Assert-True ($listText -match 'BDP_ChipCategory_Weapon[\s\S]*BDP_ChipCategory_Defense[\s\S]*BDP_ChipCategory_Ability[\s\S]*BDP_ChipCategory_Status[\s\S]*BDP_ChipCategory_Passive') "五主分类顺序必须固定。"
Assert-True ($listText -match 'CanUseAction') "枪手显示射手动作必须复用职业单向接纳规则。"

Write-Host "PASS: 制造编辑状态按分类与职业恢复草稿，并复用集中动作选择规则。"
