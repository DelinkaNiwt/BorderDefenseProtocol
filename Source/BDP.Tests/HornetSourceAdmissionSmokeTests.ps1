$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$modRoot = Get-BdpModRoot
$hornetPath = Join-Path $modRoot "1.6\Content\Defs\ComboDef\Hornet.xml"
Assert-True (Test-Path -LiteralPath $hornetPath) "缺少黄蜂组合技定义。"

$hornetText = Get-Utf8Text $hornetPath
Assert-True (($hornetText | Select-String -Pattern "<FirstSourceAdmission>" -AllMatches).Matches.Count -eq 1) "黄蜂必须声明一份第一来源项准入。"
Assert-True (($hornetText | Select-String -Pattern "<SecondSourceAdmission>" -AllMatches).Matches.Count -eq 1) "黄蜂必须声明一份第二来源项准入。"
Assert-True (($hornetText | Select-String -Pattern "<li>BDP_ChipProfession_Shooter</li>" -AllMatches).Matches.Count -eq 2) "黄蜂两侧都必须只允许射手最终职业。"
Assert-True (($hornetText | Select-String -Pattern "<li>BDP_ChipCategory_Weapon</li>" -AllMatches).Matches.Count -eq 2) "黄蜂两侧都必须要求武装分类。"

[xml]$hornetText | Out-Null
Write-Host "PASS: 黄蜂只允许射手武装芯片。"
