# 芯片制造台上帝模式批量完成调试功能测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$buildingPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Building\Building_ChipFabricator.cs"
$servicePath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Debug\ChipFabricatorDebugCompletionService.cs"
$reportPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Debug\ChipFabricatorDebugCompletionReport.cs"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml"

Assert-True (Test-Path -LiteralPath $servicePath) "缺少芯片制造台批量完成调试服务。"
Assert-True (Test-Path -LiteralPath $reportPath) "缺少芯片制造台批量完成结果模型。"

$buildingText = Get-Utf8Text $buildingPath
$serviceText = Get-Utf8Text $servicePath
$languageText = Get-Utf8Text $languagePath

Assert-True ($buildingText -match 'DebugSettings\.godMode') "调试完成按钮必须只受上帝模式控制。"
Assert-True ($buildingText -match 'ChipFabricatorDebugCompletionService\.CompleteAll') "制造台 Gizmo 必须调用集中调试完成服务。"
Assert-True ($serviceText -match 'List<Bill_ChipProduction>') "服务必须快照并处理全部芯片账单。"
Assert-True ($serviceText -match 'repeatCount') "服务必须按账单剩余数量批量完成。"
Assert-True ($serviceText -match 'ChipCombinationResolutionStatus\.Valid') "服务只能生成当前仍有效的芯片组合。"
Assert-True ($serviceText -match 'ThingMaker\.MakeThing') "服务必须直接建立正式成品。"
Assert-True ($serviceText -match 'InitializeFromBill') "调试成品必须复制账单组合记录。"
Assert-True ($serviceText -match 'GenPlace\.TryPlaceThing[\s\S]*InteractionCell') "调试成品必须落在制造台交互格附近。"
Assert-True ($serviceText -match 'BillStack[\s\S]*\.Delete\s*\(') "成功完成后必须通过原版账单栈删除账单。"
Assert-True ($serviceText -match 'BoundUft') "服务必须处理已经绑定的原版半成品。"
Assert-True ($serviceText -match 'ingredients') "已进入半成品的材料必须退回。"
Assert-True ($serviceText -match 'EndCurrentOrQueuedJob') "服务必须终止正在执行或排队等待的相关工作。"
Assert-True ($serviceText -match 'if\s*\(!TryReclaimBoundUnfinished') "半成品材料无法完整退回时必须中止该账单的调试完成。"
Assert-True ($serviceText -match 'TryPlaceThing[\s\S]*RemoveAt') "材料必须成功落地后才能从半成品记录移除。"
Assert-True ($serviceText -match 'JobCondition\.Incompletable,\s*false,\s*false') "中断制造工作时不得立即重新接取账单。"
Assert-True ($serviceText -notmatch 'ChipManufacturingCost|JobMaker|Learn\s*\(') "调试完成不得查询材料成本、伪造制作 Job 或授予经验。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_DebugCompleteAll') "语言包缺少调试完成按钮文本。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_DebugCompleteResult') "语言包缺少调试完成汇总文本。"

Write-Host "PASS: 上帝模式按钮无材料与工时消耗地完成全部有效芯片账单。"
