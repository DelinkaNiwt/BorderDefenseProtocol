# 芯片制造专用窗口、制造台入口与三栏结构测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$windowPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\Window_ChipManufacturing.cs"
$tabPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Building\ITab_ChipManufacturing.cs"
$buildingPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Building\Building_ChipFabricator.cs"
$buildingDefPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Buildings\ChipFabricator.xml"

Assert-True (Test-Path -LiteralPath $windowPath) "缺少芯片制造专用窗口。"
Assert-True (-not (Test-Path -LiteralPath $tabPath)) "芯片制造界面不得继续使用检查页签。"

$windowText = Get-Utf8Text $windowPath
$buildingText = Get-Utf8Text $buildingPath
$buildingDefText = Get-Utf8Text $buildingDefPath
Assert-True ($windowText -match 'class\s+Window_ChipManufacturing\s*:\s*Window') "制造界面必须是原版专用窗口。"
Assert-True ($windowText -match 'TargetWidth\s*=\s*1080') "窗口目标宽度应收紧到约 1080。"
Assert-True ($windowText -match 'TargetHeight\s*=\s*680') "窗口目标高度应收紧到约 680。"
Assert-True ($windowText -match 'CategoryTabsHeight\s*=\s*32') "主分类页签必须压缩高度。"
Assert-True ($windowText -match 'ProfessionTabsHeight\s*=\s*28') "职业页签必须压缩高度。"
Assert-True ($windowText -match 'LeftColumnRatio\s*=\s*0\.25') "左栏宽度应约占四分之一。"
Assert-True ($windowText -match 'RightColumnRatio\s*=\s*0\.25') "右栏宽度应约占四分之一。"
Assert-True ($windowText -match 'InitialSize[\s\S]*screenWidth[\s\S]*screenHeight') "窗口必须按当前 UI 屏幕限制初始尺寸。"
Assert-True ($windowText -match 'forcePause\s*=\s*true') "打开制造窗口期间必须使用原版强制暂停。"
Assert-True ($windowText -match 'draggable\s*=\s*true') "制造窗口必须允许拖动。"
Assert-True ($windowText -match 'DrawCategoryTabs') "顶部必须绘制五个主分类。"
Assert-True ($windowText -match 'DrawProfessionTabs') "武装类必须绘制四个职业。"
Assert-True ($windowText -match 'DrawDefTabs[\s\S]*Widgets\.ButtonInvisible') "顶部筛选必须使用平面页签点击区。"
Assert-True ($windowText -match 'DrawDefTabs[\s\S]*DrawHighlightSelected') "平面页签必须保留明确选中反馈。"
Assert-True ($windowText -match 'shells\.Count\s*\*\s*PresetRowHeight') "枪壳区域必须按实际条目数计算高度。"
Assert-True ($windowText -match 'MinimumActionSectionHeight') "左栏必须为动作列表保留最小可视高度。"
Assert-True ($windowText -match 'Mathf\.Min\s*\(\s*desiredGunShellHeight') "枪壳区域必须受左栏实际可用高度限制。"
Assert-True ($windowText -match 'DrawGunShellSection[\s\S]*DrawActionSection') "枪壳区必须位于动作列表上方。"
Assert-True ($windowText -match 'DrawMiddleColumn') "窗口必须保留中栏规格区域。"
Assert-True ($windowText -match 'DrawRightColumn') "窗口必须保留右栏材料与队列区域。"
Assert-True ($windowText -match 'Window_ChipPresetInfo') "行内 i 按钮必须打开信息卡弹窗。"
Assert-True ($windowText -notmatch 'DescriptionPreview|固定动作说明|selectedDescription') "窗口不得保留固定动作说明占位区。"
Assert-True ($windowText -match 'PostClose[\s\S]*Clear') "关闭窗口必须清空会话草稿。"
Assert-True ($buildingText -match 'GetGizmos\s*\(') "制造台必须提供底部 Gizmo 命令。"
Assert-True ($buildingText -match 'new\s+Window_ChipManufacturing\s*\(\s*this\s*\)') "制造台 Gizmo 必须打开自身对应的制造窗口。"
Assert-True ($buildingDefText -notmatch 'ITab_ChipManufacturing') "制造台 Def 不得继续注册制造检查页签。"

Write-Host "PASS: 制造台通过 Gizmo 打开可拖动、暂停且自适应屏幕的三栏制造窗口。"
