# 芯片制造右栏材料提交与真实账单队列测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$uiRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI"
$requiredFiles = @(
    "ChipManufacturingOrderPanel.cs",
    "ChipManufacturingQueuePanel.cs",
    "ChipBillQueueOperations.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $uiRoot $fileName)) "缺少右栏队列组件：$fileName"
}

$orderText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingOrderPanel.cs")
$queueText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingQueuePanel.cs")
$operationsText = Get-Utf8Text (Join-Path $uiRoot "ChipBillQueueOperations.cs")
$windowText = Get-Utf8Text (Join-Path $uiRoot "Window_ChipManufacturing.cs")
$allText = $orderText + "`n" + $queueText + "`n" + $operationsText

Assert-True ($operationsText -match 'BillStack\.MaxCount') "队列满时必须禁止加入。"
Assert-True ($operationsText -match 'ChipCombinationResolutionStatus\.Valid') "配置不完整或非法时必须禁止加入。"
Assert-True ($operationsText -match 'quantity\s*>\s*0') "数量必须是正整数。"
Assert-True ($operationsText -notmatch 'available|shortage|缺少材料') "材料不足不得进入加入任务的硬门槛。"
Assert-True ($operationsText -match 'new\s+Bill_ChipProduction') "每次加入必须新建独立芯片账单。"
Assert-True ($operationsText -match 'repeatMode\s*=\s*BillRepeatModeDefOf\.RepeatCount') "芯片账单必须固定为有限次数。"
Assert-True ($operationsText -match 'SetStoreMode\s*\(BillStoreModeDefOf\.DropOnFloor') "产物必须默认原地落地。"
Assert-True ($operationsText -match 'BillStack\.Reorder|stack\.Reorder') "队列重排必须调用原版 BillStack.Reorder。"
Assert-True ($operationsText -match 'BillStack\.Delete|stack\.Delete') "删除必须调用原版 BillStack.Delete。"
Assert-True ($operationsText -match '\.suspended') "暂停必须使用原版 bill.suspended。"
Assert-True ($operationsText -match 'repeatCount') "剩余数量只能修改有限账单 repeatCount。"
Assert-True ($operationsText -match 'Clone\(\)') "载入配置必须复制记录，不能修改原账单。"

Assert-True ($orderText -match 'ingredient\.count[\s\S]*total') "右栏上半必须显示单枚与总材料。"
Assert-True ($orderText -match 'Color\.yellow') "材料不足必须以黄色提醒。"
Assert-True ($orderText -match 'public\s+float\s+Draw\s*\(') "材料面板必须返回按实际内容计算的占用高度。"
Assert-True ($orderText -notmatch 'Widgets\.DrawMenuSection') "材料区不得再嵌套完整重框。"
Assert-True ($orderText -match 'Text\.CalcHeight\s*\(\s*line') "材料行必须按实际文本换行高度计量。"
Assert-True ($orderText -match 'BeginScrollView') "较矮窗口中的材料区必须可滚动，不能覆盖队列。"
Assert-True ($orderText -match 'return\s+height\s*\+\s*6f\s*\+\s*36f\s*\+\s*32f') "材料区总高度不得重复计算制造数量输入行。"
Assert-True ($queueText -match 'BillStack') "右栏下半必须读取制造台真实账单栈。"
Assert-True ($queueText -match 'BDP_ChipManufacturing_Queue_Completed') "剩余数为零必须显示已完成。"
Assert-True ($queueText -match 'Text\.CalcHeight') "队列完整名称必须按实际宽度测量高度。"
Assert-True ($queueText -match 'CalculateRowHeight') "队列卡片必须按标题和操作内容动态计算高度。"
Assert-True ($queueText -notmatch 'rowHeight\s*=\s*102') "队列不得继续使用会裁切内容的固定卡片高度。"
Assert-True ($queueText -notmatch 'Widgets\.DrawMenuSection') "队列区不得再嵌套完整重框。"
Assert-True ($queueText -match 'rect\.height\s*<=\s*26f') "队列高度不足时必须提前返回，禁止负高度滚动区。"
Assert-True ($windowText -match 'DrawRightColumn[\s\S]*Widgets\.DrawMenuSection') "右栏必须统一绘制单一外框。"
Assert-True ($windowText -match 'DrawLineHorizontal') "材料与队列之间必须使用轻量分隔线。"
Assert-True ($windowText -match 'MinimumQueueSectionHeight') "右栏必须为队列保留最小可视高度。"
foreach ($forbidden in @("无限制作", "制作至库存", "指定工人", "材料半径", "输出区", "Forever", "TargetCount"))
{
    Assert-True ($allText -notmatch [regex]::Escape($forbidden)) "右栏不得出现未批准控件：$forbidden"
}

Write-Host "PASS: 右栏材料不足只警告，完整正数量组合生成独立有限账单，并直接操作真实账单队列。"
