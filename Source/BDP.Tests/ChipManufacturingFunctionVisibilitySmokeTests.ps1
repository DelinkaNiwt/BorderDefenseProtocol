# 芯片功能制作台可见性冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$definitionPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Defs\ChipManufacturingPresetDef.cs"
$listPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingListModel.cs"
$resolverPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationResolver.cs"
$presetPath = Join-Path $modRoot "1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$triggerPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Equipment\Trigger.xml"

$definitionText = Get-Utf8Text $definitionPath
$listText = Get-Utf8Text $listPath
$resolverText = Get-Utf8Text $resolverPath
$presetText = Get-Utf8Text $presetPath
$triggerText = Get-Utf8Text $triggerPath

Assert-True ($definitionText -match 'bool\s+showInManufacturing\s*=\s*true') `
    "ChipManufacturingPresetDef 必须提供默认值为 true 的制作台可见性字段。"
Assert-True (([regex]::Matches($listText, 'function\.showInManufacturing')).Count -ge 2) `
    "制造功能列表和功能数量统计都必须使用 showInManufacturing。"
Assert-True ($resolverText -notmatch 'showInManufacturing') `
    "芯片组合解析器不得把制作台可见性当成组合合法性条件。"

[xml]$presetXml = $presetText
$hiddenFunctionNames = @(
    "BDP_ChipFunction_FujinRangedStrike",
    "BDP_ChipFunction_Fujin"
)
foreach ($functionName in $hiddenFunctionNames)
{
    $node = $presetXml.SelectNodes("/Defs/*") |
        Where-Object { $_.Name -like "*ChipManufacturingPresetDef" -and $_.defName -eq $functionName } |
        Select-Object -First 1
    Assert-True ($null -ne $node) "缺少固定装载芯片功能定义：$functionName"
    Assert-True ([string]$node.showInManufacturing -eq "false") `
        "固定装载芯片功能必须声明 showInManufacturing=false：$functionName"
}

Assert-True ($triggerText -notmatch [regex]::Escape('<initializationKey>BDP_ChipFunction_EnergyShield_Auto</initializationKey>')) `
    "风刃固定预载不得继续包含自动护盾。"
Assert-True ($triggerText -notmatch [regex]::Escape('<initializationKey>BDP_ChipFunction_EmergencyEscape</initializationKey>')) `
    "风刃固定预载不得继续包含紧急脱离。"

foreach ($functionName in $hiddenFunctionNames)
{
    Assert-True ($triggerText -match [regex]::Escape("<initializationKey>$functionName</initializationKey>")) `
        "固定装载配置不得丢失初始化键：$functionName"
}

Write-Host "PASS: 固定装载芯片功能隐藏于制作台列表，但仍保留固定装载解析来源。"
