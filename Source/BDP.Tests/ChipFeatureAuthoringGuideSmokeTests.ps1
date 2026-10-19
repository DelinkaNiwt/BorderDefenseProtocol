$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$guidePath = Join-Path $repoRoot 'docs\BDP芯片功能语义开发者指南.md'
$rawDiagnosticPath = Join-Path $repoRoot 'Source\BDP.Development\Information\ChipFeatureRawFactDiagnostic.cs'
$resolutionDiagnosticPath = Join-Path $repoRoot 'Source\BDP.Development\Information\ChipFeatureResolutionDiagnostic.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True (Test-Path -LiteralPath $guidePath) '缺少面向芯片功能语义作者的完整指南。'
$guideText = Get-Content -LiteralPath $guidePath -Raw -Encoding UTF8
$rawDiagnostic = Get-Content -LiteralPath $rawDiagnosticPath -Raw -Encoding UTF8
$resolutionDiagnostic = Get-Content -LiteralPath $resolutionDiagnosticPath -Raw -Encoding UTF8

foreach ($term in @('Feature', 'Sources', 'Groups', 'Subgroups', 'Children', '功能级', '信息组级', '组合结果', '形态', '姿态', '开发者诊断')) {
    Assert-True ($guideText.Contains($term)) "指南缺少关键职责或场景：$term。"
}
foreach ($example in @('远程攻击', '近战攻击', '护盾', '机动', '能力', '单一结果', '多结果', '第三方扩展')) {
    Assert-True ($guideText.Contains($example)) "指南缺少代表性声明示例：$example。"
}
foreach ($worker in @('RangedAttackFeatureSourceConfig', 'MeleeAttackMethodFeatureSourceConfig', 'ShieldFeatureSourceConfig', 'MovementMethodFeatureSourceConfig', 'AbilityLifecycleFeatureSourceConfig')) {
    Assert-True ($guideText.Contains($worker)) "指南缺少现有标准来源入口：$worker。"
}
Assert-True ($guideText -match 'MaximumDepth\s*=\s*2|最大深度为\s*2') '指南必须明确当前子功能最大深度。'
Assert-True ($guideText -match '不.*手填|禁止.*实例值') '指南必须明确禁止在语义来源声明中复制实例值。'
Assert-True ($rawDiagnostic -match 'feature\.Groups' -and $rawDiagnostic -match 'feature\.Subgroups') 'Development 原始事实摘要必须递归记录信息组和两类子组。'
Assert-True ($rawDiagnostic -match 'group\.Path' -and $rawDiagnostic -match 'State' -and $rawDiagnostic -match 'Provenance') '组诊断必须包含完整路径、缺值/冲突状态和来源链。'
Assert-True ($resolutionDiagnostic -match 'model\.Diagnostics') '开发者诊断必须保留被拒绝属性等解析诊断。'

Write-Output 'ChipFeatureAuthoringGuide PASS'
