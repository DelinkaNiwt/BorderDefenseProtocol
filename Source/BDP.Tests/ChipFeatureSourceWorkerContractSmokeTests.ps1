# 芯片功能来源 Worker 契约冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sourceRoot = Join-Path $modRoot "Source\BDP\Core\Information\Features\Sources"
$requiredFiles = @(
    "IChipFeatureSourceWorker.cs",
    "ChipFeatureSourceWorkerFactory.cs",
    "ChipFeatureReadContext.cs",
    "..\Model\ChipFeatureValueContribution.cs",
    "..\Model\ChipFeatureValueLayer.cs"
)

foreach ($fileName in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $sourceRoot $fileName)) `
        "缺少来源 Worker 契约文件：$fileName"
}

$sourceText = ($requiredFiles | ForEach-Object {
    Get-Utf8Text (Join-Path $sourceRoot $_)
}) -join "`n"

Assert-True ($sourceText -match "interface\s+IChipFeatureSourceWorker") `
    "缺少 IChipFeatureSourceWorker。"
Assert-True ($sourceText -match "IReadOnlyList<ChipFeatureValueContribution>") `
    "Worker 必须返回类型化贡献集合。"
Assert-True ($sourceText -match "ChipFeatureSourceConfig\s+source") `
    "Worker 必须接收来源配置。"
Assert-True ($sourceText -match "ChipFeatureReadContext\s+context") `
    "Worker 必须接收只读读取上下文。"
Assert-True ($sourceText -match "WorkerClass") `
    "来源工厂必须依据 XML WorkerClass 创建 Worker。"
Assert-True ($sourceText -match "IsAssignableFrom") `
    "来源工厂必须校验 Worker 接口类型。"
Assert-True ($sourceText -match "Activator\.CreateInstance") `
    "来源工厂必须支持第三方 XML 类型挂接。"
Assert-True ($sourceText -match "Dictionary<Type,\s*IChipFeatureSourceWorker>") `
    "无状态 Worker 应按类型缓存，避免每次解析重复创建。"
Assert-True ($sourceText -notmatch "CanProvide") `
    "来源系统不得使用 CanProvide 首个命中裁定。"
Assert-True ($sourceText -notmatch "ChipInformationDetailRenderer|Window|Gizmo") `
    "来源 Worker 契约不得依赖界面。"

Write-Host "PASS: 芯片功能来源 Worker 契约支持 XML 挂接、类型校验和异常隔离。"
