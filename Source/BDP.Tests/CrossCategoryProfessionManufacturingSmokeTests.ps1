# 职业筛选仅属于武装主分类的制造界面边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$listPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingListModel.cs'
$windowPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\Window_ChipManufacturing.cs'
$rulesPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationSelectionRules.cs'

$listText = Get-Utf8Text $listPath
$windowText = Get-Utf8Text $windowPath
$rulesText = Get-Utf8Text $rulesPath

Assert-True (
    ($listText -notmatch 'GetProfessions\(ChipCategoryDef category\)') -and
    ($listText -notmatch 'GetNeutralActionCount\(ChipCategoryDef category\)') -and
    ($listText -match 'action\?\.config\?\.Profile\?\.Category\s*==\s*category')
) '制造列表不得为非武装主分类生成职业路径。'

Assert-True (
    ($windowText -match '!isOverview\s*&&\s*IsWeapon\(editorState\.CurrentCategory\)') -and
    ($windowText -match 'IsWeapon\(category\)') -and
    ($windowText -notmatch 'BDP_ChipManufacturing_ProfessionGeneral')
) '制造窗口必须只在武装分类显示职业栏，进入防护等分类时使用空职业路径。'

Assert-True (
    ($rulesText -match 'IsWeaponAction\(action\)') -and
    ($rulesText -match 'if\s*\(!IsWeaponAction\(action\)\)\s*\{\s*return true;')
) '非武装动作必须忽略职业字段，防护分类才能直接显示并选择全部防护预设。'

Write-Output 'CrossCategoryProfessionManufacturingSmokeTests PASS'
