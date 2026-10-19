$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$detailBuilderPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs'
$requestAdapterPath = Join-Path $repoRoot 'Source\BDP\Core\Information\Input\ChipInformationInstanceRequestAdapter.cs'
$windowPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\Window_TriggerAssembly.cs'
$panelPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\Panel_ChipDetail.cs'

$detailBuilderText = Get-Content $detailBuilderPath -Raw -Encoding UTF8
$requestAdapterText = Get-Content $requestAdapterPath -Raw -Encoding UTF8
$windowText = Get-Content $windowPath -Raw -Encoding UTF8
$panelText = Get-Content $panelPath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($requestAdapterText -match 'ChipInformationRequest') '成品实例必须有统一请求适配器。'
Assert-True ($requestAdapterText -match 'DeclaredConfig' -and $requestAdapterText -match 'EffectiveConfig') '请求必须同时携带声明配置和有效配置。'
Assert-True ($detailBuilderText -match 'ChipInformationSurfaceAccess\.Read\(request\)') '装配详情必须调用统一请求读取入口。'
Assert-True ($windowText -match 'BuildSelectedChipRuntimeState') '装配窗口必须把当前形态、姿态和激活状态传给详情构建器。'
Assert-True ($windowText -match 'GetChipModeKey' -and $windowText -match 'GetChipStanceKey') '运行时上下文必须来自 Trigger 正式读取口。'
Assert-True ($panelText -match 'ChipInformationComparisonModel') '详情面板必须只接收统一比较模型。'
Assert-True ($panelText -notmatch 'ChipInformationDefinitionInputAdapter|ChipInformationInstanceInputAdapter') '详情面板不得自行适配定义或实例。'

Write-Host 'ChipFeatureTriggerAssemblyPreview PASS'
