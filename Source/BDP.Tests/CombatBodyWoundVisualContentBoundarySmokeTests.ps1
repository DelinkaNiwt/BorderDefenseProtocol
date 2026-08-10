$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$corePresentationRoot = Join-Path $coreRoot 'CombatBody\Wounds\Presentation'
$contentVisualRoot = Join-Path $contentRoot 'CombatBody\Wounds\Visuals'
$coreWoundRuntimePath = Join-Path $coreRoot 'CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$bootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'
$contentFleckPath = Join-Path $repoRoot '1.6\Content\Defs\Flecks\FleckDefs_WoundSpray.xml'
$oldFleckPath = Join-Path $repoRoot '1.6\Defs\Flecks\FleckDefs_WoundSpray.xml'

$presentationInterfacePath = Join-Path $corePresentationRoot 'ICombatBodyWoundPresentationProvider.cs'
$presentationRegistryPath = Join-Path $corePresentationRoot 'CombatBodyWoundPresentationRegistry.cs'
$providerPath = Join-Path $contentVisualRoot 'CombatBodyWoundSprayPresentationProvider.cs'
$runtimePath = Join-Path $contentVisualRoot 'CombatBodyWoundSprayRuntime.cs'
$emitterPath = Join-Path $contentVisualRoot 'CombatBodyWoundSprayEmitter.cs'
$fleckRefsPath = Join-Path $contentVisualRoot 'WoundSprayFleckDefs.cs'

foreach ($path in @(
    $presentationInterfacePath,
    $presentationRegistryPath,
    $providerPath,
    $runtimePath,
    $emitterPath,
    $fleckRefsPath,
    $contentFleckPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "缺少伤口视觉内容化文件：$path"
}

Assert-True (-not (Test-Path -LiteralPath (Join-Path $coreRoot 'CombatBody\Wounds\Visuals'))) 'Core仍保留伤口视觉业务目录。'
Assert-True (-not (Test-Path -LiteralPath $oldFleckPath)) '旧Core Fleck Def路径仍存在。'

$coreRuntimeText = Get-Content -LiteralPath $coreWoundRuntimePath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8
$corePresentationText = Get-Content -LiteralPath $presentationInterfacePath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $presentationRegistryPath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$contentVisualTexts = Get-ChildItem -LiteralPath $contentVisualRoot -Recurse -File -Filter '*.cs' |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }

Assert-True ($corePresentationText -match 'public interface ICombatBodyWoundPresentationProvider') 'Core必须提供中性伤口表现提供器接口。'
Assert-True ($registryText -match 'public static class CombatBodyWoundPresentationRegistry' -and $registryText -match 'Register') 'Core必须提供伤口表现注册表。'
Assert-True ($coreRuntimeText -match 'CombatBodyWoundPresentationRegistry' -and
    $coreRuntimeText -notmatch 'CombatBodyWoundSprayRuntime|CombatBodyWoundSprayEmitter|WoundSprayFleckDefs|Wounds\.Visuals') 'Core伤口运行时不得直接持有喷溅视觉实现。'
Assert-True ($providerText -match 'ICombatBodyWoundPresentationProvider' -and $providerText -match 'CombatBodyWoundSprayRuntime') 'Content必须通过提供器承载喷溅运行时。'
Assert-True (($contentVisualTexts -join "`n") -match 'namespace BDP\.Content\.CombatBody\.Wounds\.Visuals') '伤口喷溅视觉必须归属Content命名空间。'
Assert-True ($bootstrapText -match 'CombatBodyWoundPresentationRegistry\.Register' -and
    $bootstrapText -match 'CombatBodyWoundSprayPresentationProvider') 'Content启动入口必须注册喷溅视觉提供器。'

Write-Output 'CombatBodyWoundVisualContentBoundary PASS'
