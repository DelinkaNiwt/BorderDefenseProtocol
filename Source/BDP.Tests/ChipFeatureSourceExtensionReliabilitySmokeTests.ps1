# 芯片功能来源扩展校验和制造复制可靠性冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sourceConfigPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Config\ChipFeatureSourceConfig.cs"
$validatorPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Validation\ChipFeatureDefinitionValidator.cs"
$expressionSourcePath = Join-Path $modRoot "Source\BDP.Content\Information\Features\Sources\ExpressionEntryFeatureSourceConfig.cs"
$mergePath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipFeatureMergeService.cs"

foreach ($path in @($sourceConfigPath, $validatorPath, $expressionSourcePath, $mergePath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少功能来源扩展可靠性文件：$path"
}

$sourceConfigText = Get-Utf8Text $sourceConfigPath
$validatorText = Get-Utf8Text $validatorPath
$expressionSourceText = Get-Utf8Text $expressionSourcePath
$mergeText = Get-Utf8Text $mergePath

Assert-True ($sourceConfigText -match "virtual\s+bool\s+RequiresExpressionEntryReference") `
    "来源基类必须允许自定义来源声明是否依赖表达条目。"
Assert-True ($sourceConfigText -match "CloneForRemap\(") `
    "来源基类必须提供统一制造复制入口。"
Assert-True ($sourceConfigText -match "MemberwiseClone") `
    "来源复制必须保留第三方派生配置字段。"
Assert-True ($sourceConfigText -match "entryMapper") `
    "来源复制必须支持重映射表达条目 Id。"
Assert-True ($validatorText -match "RequiresExpressionEntryReference") `
    "校验器必须尊重来源对表达条目引用的声明。"
Assert-True ($expressionSourceText -match "override\s+bool\s+RequiresExpressionEntryReference") `
    "BDP 表达条目来源必须继续要求有效的表达条目引用。"
Assert-True ($mergeText -match "CloneForRemap\(") `
    "制造合并必须通过统一复制入口保留派生来源字段。"
Assert-True ($mergeText -match "Groups\s*=\s*CloneGroups") `
    "制造合并必须递归复制功能信息组。"
Assert-True ($mergeText -match "Subgroups\s*=\s*CloneSubgroups") `
    "制造合并必须复制信息组的一层子组。"
Assert-True ($mergeText -match "CloneSources\(item\.Sources") `
    "制造合并必须复制信息组自己的来源集合。"

Write-Host "PASS: 自定义功能来源可省略表达条目引用且制造复制保留派生字段。"
