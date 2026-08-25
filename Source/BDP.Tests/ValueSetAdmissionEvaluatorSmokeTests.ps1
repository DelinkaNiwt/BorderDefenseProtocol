$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$rulePath = Join-Path $modRoot "Source\BDP\Core\Admission\ValueSetAdmissionRule.cs"
$resultPath = Join-Path $modRoot "Source\BDP\Core\Admission\ValueSetAdmissionResult.cs"
$evaluatorPath = Join-Path $modRoot "Source\BDP\Core\Admission\ValueSetAdmissionEvaluator.cs"

Assert-True (Test-Path -LiteralPath $rulePath) "缺少通用集合准入规则。"
Assert-True (Test-Path -LiteralPath $resultPath) "缺少通用集合准入结果。"
Assert-True (Test-Path -LiteralPath $evaluatorPath) "缺少通用集合准入求值器。"

$ruleText = Get-Utf8Text $rulePath
$evaluatorText = Get-Utf8Text $evaluatorPath
foreach ($member in @("AllowedAny", "RequiredAll", "DeniedAny"))
{
    Assert-True ($ruleText -match "List<string>\s+$member") "准入规则缺少成员：$member"
}
Assert-True ($evaluatorText -match "StringComparer\.OrdinalIgnoreCase") "集合准入必须忽略 DefName 大小写。"
Assert-True ($evaluatorText.IndexOf("DeniedAny") -lt $evaluatorText.IndexOf("AllowedAny")) "黑名单必须先于白名单求值。"
Assert-True ($evaluatorText.IndexOf("AllowedAny") -lt $evaluatorText.IndexOf("RequiredAll")) "白名单必须先于必须项求值。"

Write-Host "PASS: 通用集合准入按黑名单、白名单和必须项稳定求值。"
