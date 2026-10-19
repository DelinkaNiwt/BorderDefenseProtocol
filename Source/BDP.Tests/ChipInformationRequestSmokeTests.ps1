$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$requestPath = Join-Path $repoRoot 'Source\BDP\Core\Information\Model\ChipInformationRequest.cs'
$adapterPath = Join-Path $repoRoot 'Source\BDP\Core\Information\Input\ChipInformationRequestInputAdapter.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Information\ChipInformationService.cs'
$surfacePath = Join-Path $repoRoot 'Source\BDP\Core\Information\Access\ChipInformationSurfaceAccess.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

$requestText = Get-Content -LiteralPath $requestPath -Raw -Encoding UTF8
$adapterText = Get-Content -LiteralPath $adapterPath -Raw -Encoding UTF8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding UTF8
$surfaceText = Get-Content -LiteralPath $surfacePath -Raw -Encoding UTF8

foreach ($member in @('DeclaredConfig','EffectiveConfig','Runtime','CurrentModeKey','CurrentStanceKey')) {
    Assert-True ($requestText -match $member) "统一请求缺少字段：$member"
}
Assert-True ($adapterText -match 'EffectiveConfig[\s\S]*DeclaredConfig') '请求适配器必须优先有效配置并回退声明配置。'
Assert-True ($serviceText -match 'Read\(ChipInformationRequest request\)') '信息服务必须提供统一请求入口。'
Assert-True ($surfaceText -match 'Read\(ChipInformationRequest request\)') '信息表面访问必须提供统一请求入口。'

Write-Host 'ChipInformationRequest PASS'
