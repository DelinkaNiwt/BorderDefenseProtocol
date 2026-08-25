$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$gimletText = Get-Utf8Text (Join-Path $modRoot "1.6\Content\Defs\ComboDef\Gimlet.xml")
$surfaceText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Combos\Access\ComboSurfaceAccess.cs")
$resolverText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs")

Assert-True (($gimletText | Select-String -Pattern "<FirstSourceAdmission>" -AllMatches).Matches.Count -eq 1) "飞锥必须声明一份第一来源项准入。"
Assert-True (($gimletText | Select-String -Pattern "<SecondSourceAdmission>" -AllMatches).Matches.Count -eq 1) "飞锥必须声明一份第二来源项准入。"
Assert-True (($gimletText | Select-String -Pattern "<li>BDP_ChipProfession_Shooter</li>" -AllMatches).Matches.Count -eq 2) "飞锥两侧都必须只允许射手最终职业。"
Assert-True (($gimletText | Select-String -Pattern "<li>BDP_ChipCategory_Weapon</li>" -AllMatches).Matches.Count -eq 2) "飞锥两侧都必须要求武装分类。"
Assert-True ($surfaceText -match "out string failureReason") "Combo 匹配入口必须公开集中失败摘要。"
Assert-True ($resolverText -match "matchFailureReason") "表达层诊断必须记录 Combo 匹配失败摘要。"

[xml]$gimletText | Out-Null
Write-Host "PASS: 飞锥只允许射手武装芯片，并保留集中匹配诊断。"
