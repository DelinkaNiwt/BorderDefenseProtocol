# 职业筛选跨主分类生效，武装构型仍保留自己的分类边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$listPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingListModel.cs'
$windowPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\Window_ChipManufacturing.cs'
$rulesPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationSelectionRules.cs'
$validatorPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Validation\ChipManufacturingDefValidator.cs'

$listText = Get-Utf8Text $listPath
$windowText = Get-Utf8Text $windowPath
$rulesText = Get-Utf8Text $rulesPath
$validatorText = Get-Utf8Text $validatorPath

Assert-True (
    ($listText -notmatch 'GetProfessions\(ChipCategoryDef category\)') -and
    ($listText -notmatch 'GetNeutralFunctionCount\(ChipCategoryDef category\)') -and
    ($listText -match 'CanUseFunction\(profession, function\)') -and
    ($listText -notmatch '仅武装分类继续应用职业')
) '制造列表必须让所有主分类复用同一职业筛选规则。'

Assert-True (
    ($windowText -match 'if\s*\(!isOverview\)\s*\{\s*y\s*\+=\s*DrawProfessionTabs') -and
    ($windowText -match 'ChipProfessionDef profession = professions\.Contains\(editorState\.CurrentProfession\)') -and
    ($windowText -notmatch 'ChipProfessionDef profession = IsWeapon\(category\)') -and
    ($windowText -notmatch 'IsWeapon\(editorState\.CurrentCategory\)')
) '制造窗口必须在所有主分类显示职业路径并保存职业选择。'

Assert-True (
    ($rulesText -match 'if\s*\(function\.profession == null\)\s*\{\s*return true;') -and
    ($rulesText -match 'acceptedSourceProfessions') -and
    ($rulesText -notmatch 'IsWeaponFunction|WeaponCategoryDefName')
) '职业兼容必须直接读取功能定义职业，不能再由武装分类决定。'

Assert-True (
    $validatorText -notmatch 'Category\.defName == "BDP_ChipCategory_Weapon"[\s\S]*function\.profession == null'
) '制造定义校验不得把职业字段绑定到武装分类。'

Write-Output 'CrossCategoryProfessionManufacturingSmokeTests PASS'
