$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$formalSourceRoots = @(
    (Join-Path $repoRoot 'Source\BDP'),
    (Join-Path $repoRoot 'Source\BDP.Content'),
    (Join-Path $repoRoot 'Source\BDP.Development')
)
$coreService = Get-Content (Join-Path $repoRoot 'Source\BDP\Core\Information\ChipInformationService.cs') -Raw -Encoding UTF8
$parser = Get-Content (Join-Path $repoRoot 'Source\BDP\Core\Information\ChipInformationParser.cs') -Raw -Encoding UTF8
$renderer = Get-Content (Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs') -Raw -Encoding UTF8
$snapshot = Get-Content (Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedResultSnapshot.cs') -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

$retiredNames = @(
    'RangedVerbInformationRule',
    'MeleeToolInformationRule',
    'AbilityInformationRule',
    'HediffInformationRule',
    'PassiveInformationRule',
    'ChipInformationExtensionRegistry',
    'ChipInformationAbilityCompReflectionProvider',
    'ChipInformationExpressionBlockBuilder'
)
foreach ($name in $retiredNames)
{
    $matches = @()
    foreach ($formalRoot in $formalSourceRoots)
    {
        $matches += @(rg -n $name $formalRoot -g '*.cs' 2>$null)
    }
    Assert-True ($matches.Count -eq 0) "旧信息主链仍有正式源码引用：$name"
}

Assert-True ($coreService -notmatch 'RangedVerbInformationRule|MeleeToolInformationRule|AbilityInformationRule|HediffInformationRule|PassiveInformationRule') '默认服务不得注册技术类型规则。'
Assert-True ($parser -match 'ChipFeatureResolver') '解析器必须保留语义功能解析。'
Assert-True ($renderer -match 'FeatureRendererRegistry') '详情 UI 必须通过语义展示器注册表。'
Assert-True ($renderer -notmatch 'GetShieldPresentationBlock|IsPrototypeChannel') '详情 UI 不得再按旧技术块识别护盾或功能。'
Assert-True ($snapshot -notmatch 'InformationBlocks') '表达发布快照不得再发布信息块。'

Write-Host 'ChipFeatureLegacyPathRetirement PASS'
