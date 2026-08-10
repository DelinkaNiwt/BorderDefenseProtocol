# 芯片制造总览首页测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$uiRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI"
$windowPath = Join-Path $uiRoot "Window_ChipManufacturing.cs"
$overviewPath = Join-Path $uiRoot "ChipManufacturingOverviewPanel.cs"
$listPath = Join-Path $uiRoot "ChipManufacturingListModel.cs"
$languageRoot = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml"

Assert-True (Test-Path -LiteralPath $overviewPath) "制造台必须有独立的总览面板组件。"

$windowText = Get-Utf8Text $windowPath
$overviewText = Get-Utf8Text $overviewPath
$listText = Get-Utf8Text $listPath
$languageText = Get-Utf8Text $languageRoot

Assert-True ($windowText -match 'isOverview') "制造窗口必须显式保存当前是否为总览页。"
Assert-True ($windowText -match 'overviewPanel\.Draw') "制造窗口必须有总览绘制路径。"
Assert-True ($windowText -match 'ChipManufacturingOverviewPanel') "制造窗口必须使用独立总览面板组件。"
Assert-True ($windowText -match 'isOverview\s*=\s*true') "制造窗口打开时必须默认进入总览页。"
Assert-True ($windowText -match 'OverviewTab') "顶部导航必须包含制造总览入口。"
Assert-True ($overviewText -match 'GetActionCount') "总览分类卡片必须显示动作预设数量。"
Assert-True ($overviewText -match 'onCategorySelected') "总览分类卡片必须能够进入对应分类配置。"
Assert-True ($overviewText -match 'queuePanel.*Draw') "总览必须复用真实制造队列。"
Assert-True ($overviewText -match 'onConfigurationLoaded') "总览载入队列配置后必须能够离开总览进入配置页。"
Assert-True ($listText -match 'GetActionCount') "列表模型必须提供按主分类统计动作预设数量的读取面。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_OverviewTab') "语言包必须包含总览页签文本。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_Overview_ActionCount') "语言包必须包含总览动作数量文本。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_Queue_Empty') "语言包必须包含空队列状态文本。"

Write-Host "PASS: 芯片制造台默认进入总览，分类卡片可导航且总览复用真实队列。"
