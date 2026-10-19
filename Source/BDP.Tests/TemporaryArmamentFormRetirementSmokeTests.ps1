# 临时攻击手武装构型退役边界冒烟测试。

$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$armamentPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$keyedPath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml'

Assert-True (Test-Path -LiteralPath $armamentPath) '武装构型 Def 文件必须存在。'
Assert-True (Test-Path -LiteralPath $visualPath) '视觉预设 Def 文件必须存在。'
Assert-True (Test-Path -LiteralPath $keyedPath) '武装构型 Keyed 语言包必须存在。'

[xml]$armamentXml = Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8
$keyedText = Get-Content -LiteralPath $keyedPath -Raw -Encoding utf8

$armamentType = 'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef'
$visualType = 'BDP.Core.Expressions.ExpressionVisualPresetDef'
$temporaryForm = @($armamentXml.Defs.$armamentType) |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_Temporary' } |
    Select-Object -First 1
$temporaryVisual = @($visualXml.Defs.$visualType) |
    Where-Object { $_.defName -eq 'BDP_Visual_Temporary_BreachAxe' } |
    Select-Object -First 1
$trionForm = @($armamentXml.Defs.$armamentType) |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_TrionCube' } |
    Select-Object -First 1

Assert-True ($null -eq $temporaryForm) '临时攻击手武装型必须已从正式 Def 中移除。'
Assert-True ($null -eq $temporaryVisual) '临时破墙斧视觉预设必须已从正式 Def 中移除。'
Assert-True ($null -ne $trionForm) '清理临时型不得删除 Trion 立方体隐藏默认型。'
Assert-True (-not ($keyedText -match '<BDP_ArmamentForm_Temporary>')) '临时攻击手名称语言键必须已清理。'
Assert-True (-not ($keyedText -match '<BDP_ArmamentForm_Temporary_Description>')) '临时攻击手说明语言键必须已清理。'

Write-Host 'TemporaryArmamentFormRetirementSmokeTests PASS'
