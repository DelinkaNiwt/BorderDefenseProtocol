$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$informationRoot = Join-Path $sourceRoot 'BDP\Core\Information'
$referencePath = Join-Path $informationRoot 'Model\ChipInformationExtensionReference.cs'
$requestPath = Join-Path $informationRoot 'Model\ChipInformationRequest.cs'
$modelPath = Join-Path $informationRoot 'Model\ChipInformationModel.cs'
$nodeProjectorPath = Join-Path $informationRoot 'Model\ChipInformationNodeProjector.cs'
$parserPath = Join-Path $informationRoot 'ChipInformationParser.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Read-Source([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8
}

$referenceText = Read-Source $referencePath
$requestText = Read-Source $requestPath
$modelText = Read-Source $modelPath
$nodeProjectorText = Read-Source $nodeProjectorPath
$parserText = Read-Source $parserPath

Assert-True ($referenceText -match 'public sealed class ChipInformationExtensionReference') 'Core 必须提供中性的芯片信息扩展引用类型。'
foreach ($member in @('ProviderKey', 'ReferenceKindKey', 'ReferenceKey', 'ContextKey')) {
    Assert-True ($referenceText -match "public string $member") "扩展引用必须公开稳定键：$member。"
}
Assert-True ($referenceText -notmatch '\bComboDef\b|\bRangedAttack\b|\bShield\b') 'Core 扩展引用不能依赖具体业务类型或语义。'

Assert-True ($requestText -match 'IReadOnlyList<ChipInformationExtensionReference> ExtensionReferences') '统一芯片信息请求必须提供扩展引用入口。'
Assert-True ($requestText -match 'IEnumerable<ChipInformationExtensionReference> extensionReferences') '扩展引用应通过请求构造注入，而不是事后改写模型。'
Assert-True ($modelText -match 'ChipInformationNode\s+Root') '芯片模型必须只保留统一信息根节点。'
Assert-True ($nodeProjectorText -match 'CreateRelationNode') '扩展引用必须投影为统一关系节点。'
Assert-True ($parserText -match 'request\.ExtensionReferences') '统一解析器必须负责把引用投影进芯片信息树。'
Assert-True ($parserText -match 'ContextKey') '解析器必须依据稳定上下文键归属扩展引用。'
Assert-True ($parserText -match 'information\.extension_context_missing') '未知上下文引用必须产生诊断，不能静默提升到根级。'
Assert-True ($parserText -notmatch '\bComboDef\b') 'Core 解析器不能知道组合技定义。'

Write-Output 'ChipInformationExtensionReferenceContract PASS'
