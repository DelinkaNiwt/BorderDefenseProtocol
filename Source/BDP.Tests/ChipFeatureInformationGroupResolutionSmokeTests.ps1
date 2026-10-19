# 芯片功能信息组解析、校验和上下文投影冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureResolver.cs"
$validatorPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Validation\ChipFeatureDefinitionValidator.cs"
$projectorPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureContextProjector.cs"
$nodePath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationNode.cs"

foreach ($path in @($resolverPath, $validatorPath, $projectorPath, $nodePath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少信息组解析所需文件：$path"
}

$resolverText = Get-Utf8Text $resolverPath
$validatorText = Get-Utf8Text $validatorPath
$projectorText = Get-Utf8Text $projectorPath
$nodeText = Get-Utf8Text $nodePath

Assert-True ($resolverText -match "ResolveGroup") "解析器必须解析功能信息组。"
Assert-True ($resolverText -match "ResolveSubgroups") "解析器必须解析信息组允许的一级子组。"
Assert-True ($resolverText -match '"/groups/"') "信息组路径必须固定为 <功能路径>/groups/<组 Id>。"
Assert-True ($resolverText -match "subgroupPath") "子组路径必须保留其所属父组。"
Assert-True ($resolverText -match "groupDeclaration\.Group\.Properties") `
    "组来源贡献必须按信息组 Def 的属性槽位筛选。"
Assert-True ([regex]::Matches($resolverText, "ResolveProperties\(").Count -ge 2) `
    "功能根与信息组必须复用同一属性解析和合并方法。"
Assert-True ($validatorText -match "GroupIdDuplicate") "同一功能内信息组 Id 重复时必须拒绝。"
Assert-True ($validatorText -match "GroupDefMissing") "信息组 Def 缺失时必须拒绝。"
Assert-True ($validatorText -match "GroupNotAccepted") "功能不得使用未在 Feature.Groups 中允许的信息组。"
Assert-True ($validatorText -match "SubgroupIdDuplicate") "同一信息组内子组 Id 不得重复。"
Assert-True ($validatorText -match "SubgroupDefMissing") "子组 Def 缺失时必须拒绝。"
Assert-True ($validatorText -match "SubgroupNotAccepted") "信息组不得使用未声明允许的子组。"
Assert-True ($projectorText -match "declaration\.Groups") "范围计算必须检查功能信息组来源。"
Assert-True ($projectorText -match "group\.Subgroups") "范围计算必须检查一级子组来源。"
Assert-True ($projectorText -match "declaration\.Children") "范围计算必须递归检查子功能来源。"
Assert-True ($projectorText -match "CollectEntryIds" -and $projectorText -match "FindCommonScope") `
    "功能、信息组、子组和子功能来源必须共同决定唯一归属范围。"
Assert-True ($resolverText -match 'groupPath') "统一信息组节点必须保留稳定路径。"
Assert-True ($nodeText -match "DefinitionKey") "统一信息组节点必须保留来源 Def 键。"
Assert-True ($resolverText -match "depth \+ 1") "信息组不得增加功能子树深度。"

Write-Host "PASS: 信息组校验、共享属性合并、稳定路径和上下文可达性已表达。"
